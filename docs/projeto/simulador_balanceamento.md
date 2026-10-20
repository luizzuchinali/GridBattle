---
tags:
  - projeto
  - guia
  - balanceamento
created: 2026-10-12
---
# Simulador de balanceamento

O simulador joga **runs completas sozinho**, com as regras reais do jogo,
e mostra os números do balanceamento: quem vence, onde as runs terminam,
quanto dano cada batalha causa, quantos níveis o jogador ganha, que talentos
e skills aparecem. Serve para **comparar versões**: muda-se um asset, roda-se
de novo com as mesmas seeds e comparam-se os números.

Veja também: [[guia_sistemas]] (os outros sistemas) e [[plano_balanceamento]]
(o que já foi medido e decidido).

> **O bot é simples.** Ele não planeja vários turnos e não usa o terreno de
> propósito. Desde o novo molde (2026-10-19) ele lê a mesma previsão que o
> jogador vê e sai das células marcadas pelos golpes anunciados quando vale a
> pena, mas ainda joga um turno de cada vez. Um jogador vence mais que ele. Use os números para
> **comparar** versões, não como a taxa de vitória esperada de um jogador.

## 1. Como funciona

```
Menu ou script ──► BalanceSimulation (editor) ──► entra em Play Mode na SampleScene
                                                     │
                                                     ▼
                                     SimulationRunner (componente em Play Mode)
                                       │  liga o SimMode (sem animação, som, UI, salvamento)
                                       │  para cada classe × run: semente = Start Seed + i
                                       ▼
            RunManager.StartNewRun ──► mapa ──► batalha ──► talentos ──► ... ──► fim da run
                  ▲                     ▲          ▲            ▲
                  │                MapPolicy   BattleBot   TalentPolicy   (os "jogadores")
                  │
            SimulationCollector escuta os eventos do jogo ──► SimulationReport escreve os arquivos
```

- **Regras reais:** o simulador usa o mesmo `RunManager`, a mesma geração de
  mapa e batalhas, o `TurnManager`, o `CombatResolver`, a IA, as skills, o
  terreno, os consumíveis, o XP e os talentos do jogo. O bot age pelas mesmas
  entradas de um jogador (tocar numa célula, usar skill, usar item).
- **Sem o que é só visual (`SimMode`):** com o modo ligado, o jogo pula
  animações, textos flutuantes, orbs de XP, áudio, dicas, telas e modais, e
  não grava a run nem as métricas. Um turno inteiro (o jogador e todos os
  inimigos) se resolve de uma vez. Com o modo desligado, o jogo não muda.
- **Seguro:** não grava `run.json`, `metrics.jsonl` nem o perfil real (usa um
  perfil temporário, apagado no fim). Uma run salva antes continua lá. Tudo o
  que ele muda em memória (velocidade, taxa de quadros, perfil) volta ao
  normal no fim.
- **Determinístico:** a mesma seed gera o mesmo mapa e as mesmas batalhas.
  Os sorteios do bot usam uma seed própria (`Bot Seed`), separada da run.
  Duas simulações iguais dão o mesmo resultado.
- **Velocidade:** cerca de 5 a 10 runs por segundo.

### Os três "jogadores" do bot

- **Mapa (`MapPolicy`):**
  - entra sempre no chefe;
  - vai a um nó de cura (o acampamento) quando a vida está baixa;
  - evita o nó de talento (que custa vida máxima) com pouca vida;
  - no acampamento, descansa quando a vida está abaixo de `Camp Rest Below Hp
    Fraction` (ou quando não há skill para melhorar) e treina nos outros casos;
  - na melhoria de skill (treino no acampamento ou elite vencida), sorteia uma
    das skills que ainda podem melhorar e uma das variantes dela, com a seed do
    bot; sem skill para melhorar, recusa;
  - na oferta de consumível (o nó de consumível e, desde 2026-10-20, a
    recompensa da elite), pega a melhor opção (cura, depois dano, depois o
    resto) e, com o inventário cheio, troca o pior item ou recusa;
  - nas outras escolhas, sorteia pelo peso de cada tipo de nó;
  - nas batalhas, segue a política: `Normal` (batalha normal; elite só quando
    não há normal), `Elite` (batalha de elite; normal só quando não há elite)
    ou `Mixed` (qualquer uma). Até 2026-10-19 as políticas eram `Easy`,
    `Normal`, `Hard` e `Mixed`; os números ficaram, então um `Hard` salvo vira
    `Elite` e um `Easy` joga como `Normal`.
- **Batalha (`BattleBot`), por ordem de prioridade:**
  1. consumíveis (não gastam a ação): poção com pouca vida, bomba em grupos
     ou para matar, itens de suporte;
  2. um ataque que mata (mais abates, depois os papéis mais perigosos);
  3. uma skill de cura com pouca vida;
  4. ir atrás de um invocador que nada alcança;
  5. uma skill que acerta vários inimigos ou vale mais que as outras opções;
     uma skill de controle (atordoar, empurrar) quando vale mais que o dano,
     por exemplo quando tira o jogador de um golpe anunciado;
  6. uma skill defensiva com inimigos por perto;
  7. andar em direção ao melhor alvo, evitando perigo e preferindo bônus.

  O código ainda compara as skills com o ataque básico, mas nenhum personagem
  tem ataque básico no novo molde (`hasBasicAttack` desligado em todos), então
  essa opção nunca existe e o bot sempre age com skills.

  Como o turno tem um andar e um agir, o bot escolhe **pares (andar, agir)**: testa cada
  célula para onde pode andar (e a própria) sem mover nada de verdade
  (`GridController.ProbePosition`), acha a melhor ação a partir de cada uma
  com as mesmas prioridades acima e desconta o custo de terreno da célula;
  ficar parado vence empates. Se o melhor par pede andar, ele anda primeiro e
  age depois. Agindo sem andar, dá um passo em direção ao próximo alvo quando
  nada mais está ao alcance, e então encerra o turno. A coluna "Player actions"
  do relatório conta os turnos do jogador.

  Empurrões e puxões são avaliados pela mesma previsão do jogo (dano de
  colisão e de terreno, tirar um inimigo do alcance, trazer um atirador).
  As skills são lidas já com os talentos (dano, área, alcance, recarga).

  **Golpes anunciados.** O bot lê a mesma previsão que o jogador vê
  (`ActionForecast` e `ThreatForecast`, em `Gameplay/Forecast`):
  - **custo dos golpes:** cada opção paga o dano que os golpes preparados
    ainda causariam ao jogador onde a ação o deixa (`Telegraph Damage Weight`
    por ponto de dano; um golpe que mataria custa muito mais). O custo cai
    quando a ação mata, atordoa ou congela quem prepara o golpe (a previsão
    tira o golpe de quem seria parado, se a skill dele é interrompível),
    empurra quem prepara para longe (a área acompanha o empurrado) ou tira o
    jogador do lugar (investida, troca de lugar);
  - **golpes desviados:** o dano que os golpes causariam em outros inimigos
    depois da ação (o inimigo empurrado para a área de outro, ou o golpe
    levado junto com quem prepara) soma ao valor da ação, vezes
    `Redirected Strike Weight`. Só conta até a vida que a ação deixa no
    inimigo;
  - **desviar depois de agir:** agindo da própria célula com o andar ainda
    livre (e com algum lugar para onde andar: `CanWalkNow`), o bot conta só
    `Dodge After Acting Factor` do custo dos golpes, porque em geral ainda dá
    para sair das células marcadas depois. Não vale para as skills
    canalizadas, que gastam o andar.

  **Skills canalizadas** (desde 2026-10-20; o Meteoro e a Tempestade Elétrica
  do Mago): o bot só as considera da célula onde está, antes de andar, sem
  andar antes nem depois, e pula toda skill que
  `PlayerCharacterController.CanUseSkillNow` recusa, então nunca tenta algo
  que o controlador não aceita. Ele ainda **não pesa o Exausto** (ficar sem
  andar no turno seguinte), então pode usar o Meteoro mais do que um jogador
  usaria. Como o desconto de desviar agora usa `CanWalkNow`, o resultado com o
  jogador enraizado ou cercado mudou em relação às referências antigas, mesmo
  com as mesmas seeds.
- **Talentos (`TalentPolicy`):** escolhe a primeira opção, a que combina
  mais com a build (tags em comum) ou uma qualquer. Pode rerolar, banir ou
  pular quando nada combina, e pode ter talentos preferidos (para medir uma
  build específica).

## 2. Rodando pelo menu

1. Abra a cena `Assets/Scenes/SampleScene.unity` (a única do jogo).
2. `GridBattle > Simulation > Select Settings Asset` abre as opções
   (`Settings/Simulation/BalanceSimulationSettings`; o asset é criado no
   primeiro uso). Ajuste o que quiser (seção 3).
3. `GridBattle > Simulation > Run Balance Simulation`. O editor entra em
   Play Mode, mostra uma barra de progresso (com **Cancel**) e sai do Play
   Mode no fim. Com `Show Board` ligado, a tela mostra o tabuleiro e a HUD
   enquanto o bot joga (rápido, sem animações). Se algo impedir o começo
   (cena errada, erro), a simulação termina com uma mensagem no Console em
   vez de ficar parada em "starting".
4. `GridBattle > Simulation > Open Output Folder` abre a pasta dos
   resultados. Com `Log Summary` ligado, o resumo também sai no Console.

> Não jogue no Editor enquanto o simulador roda: ele usa o Play Mode.

## 3. Opções (`BalanceSimulationSettings`)

| Grupo | Campo | O que faz |
|---|---|---|
| Lote | `Label` | Nome do lote, usado no relatório e no nome da pasta |
| | `Classes` | Classes simuladas (vazio = todas as jogáveis) |
| | `Runs Per Class` | Runs completas por classe (30–50 dá números estáveis; 100 para conferir) |
| | `Start Seed` | Seed da primeira run; a run *i* usa `Start Seed + i` em todas as classes, então as classes jogam os **mesmos mapas** |
| | `Bot Seed` | Seed dos sorteios do próprio bot (separada da run) |
| Mapa | `Policy` | `Normal` ou `Elite` (prefere esse tipo de batalha) ou `Mixed` (sorteia) |
| | `Heal Below Hp Fraction` | Vai a um nó de cura quando a vida está abaixo dessa fração |
| | `Talent Node Min Hp Fraction` | Só entra em nó de talento (que custa vida) acima dessa fração |
| | `Battle/Heal/Talent/Consumable Weight` | Chance relativa de cada tipo de nó no resto das escolhas |
| Talentos | `Policy` | `FirstOption` (primeira opção), `HighestSynergy` (mais tags em comum com a build) ou `Random` |
| | `Reroll/Ban/Skip Without Synergy` | Usa reroll, banimento ou pulo quando nenhuma opção combina com a build |
| | `Preferred Talents` | Nomes de assets de talento que o bot pega sempre que aparecem, antes da política (para medir uma build) |
| Mapa | `Camp Rest Below Hp Fraction` | No acampamento, descansa abaixo dessa fração da vida e treina (melhora uma skill) acima dela |
| Batalha | `Use Skills`, `Use Consumables` | Desligar as skills media só o ataque básico; no novo molde ninguém tem ataque básico, então o bot não teria o que fazer: deixe ligado |
| | `Skill Min Targets` | Usa uma skill de área quando acerta pelo menos esse número de inimigos (ou mata, ou vale mais que as outras opções) |
| | `Potion/Support Item/Heal Skill Hp Fraction` | Vida abaixo da qual usa poções, itens de suporte e skills de cura |
| | `Area Item Min Targets` | Inimigos mínimos para usar uma bomba |
| | `Displace Out Of Reach Value`, `Displace Pull In Value`, `Displace Terrain State Value` | Quanto vale (em vida) tirar do alcance um inimigo que poderia atacar, puxar um atirador para perto e mandar um inimigo para um terreno com estado |
| | `State Value Weight` | Quanto o dano ao longo do tempo de uma skill (veneno, queimadura) conta no valor dela |
| | `Telegraph Damage Weight` | Custo de terminar o turno numa célula marcada por um golpe anunciado, por ponto do dano que ele causaria (1 = fica quando a ação causa mais dano que o golpe; um golpe que mataria custa muito mais) |
| | `Redirected Strike Weight` | Valor de cada ponto de dano que um golpe anunciado causaria a outros inimigos depois da ação do bot (padrão 1) |
| | `Dodge After Acting Factor` | Parte do custo dos golpes que o bot conta quando age da própria célula e ainda pode andar (padrão 0,5; 1 = como se não pudesse mais andar) |
| | `Hazard Penalty`, `Bonus Reward` | Quanto o bot evita células de perigo e procura células de bônus |
| | `Focus Role Order` | Papéis que o bot mata primeiro (nome do asset do papel) |
| | `Rush Role Names` | Papéis que o bot vai buscar mesmo podendo atacar outro (ex.: invocador) |
| Modo assistido | `Restore Hp Before Battle` | Enche a vida antes de cada batalha: mede **só a dificuldade das lutas**, sem o desgaste da run |
| Limites | `Max Turns Per Battle`, `Stuck Frames`, `Max Seconds Per Run`, `Max Failed Actions` | Batalhas ou runs que passam disso são abortadas e aparecem no relatório |
| Velocidade | `Action Budget Ms`, `Time Scale`, `Uncap Frame Rate` | Ritmo da simulação (não muda as regras) |
| Tela | `Show Board` | Mostra o tabuleiro e a HUD enquanto o bot joga (ligado), em vez da tela inicial. Só muda o que aparece: os resultados são os mesmos |
| Saída | `Output Folder`, `Write Csv`, `Log Summary` | Pasta dentro de `persistentDataPath`, CSVs e resumo no Console |

## 4. O relatório

Cada lote cria `persistentDataPath/Simulation/<data>_<rótulo>/` (no Windows,
`%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle/Simulation`).

**`summary.md`**, para ler, de cima para baixo:

| Seção | O que olhar |
|---|---|
| Saúde da simulação | Runs abortadas, exceções e erros no Console: tudo deve ser 0 |
| Resultado por classe | Vitória (com intervalo de confiança de 95%), andar e nível médios, talentos, skills e itens por run |
| Onde as runs terminam | Andar das mortes, distribuição das mortes e vitória contra o chefe |
| Batalhas por faixa de andares | Inimigos, papéis diferentes, terreno, invocações, skills e empurrões em cada parte da run (mostra se as lutas finais pedem tática) |
| Nível ao entrar em cada andar | Ritmo de progressão |
| Batalhas por andar, normais e de elite | Dano recebido em % da vida máxima (colunas Normal, Elite e Boss), ações por batalha, XP planejado e ganho (colunas Normal e Elite) |
| XP por nó | XP por andar, nas batalhas normais e de elite, e as **inversões** (uma batalha normal dando mais XP que uma de elite no mesmo andar; deve ser 0). Até 2026-10-19 as colunas eram Easy, Normal e Hard |
| Inimigos | Quem causa mais dano, quem termina as runs, dano por papel e por tipo (skill, espinhos, terreno, colisão) |
| Talentos | Oferecidos, escolhidos, taxa de escolha e a parcela de talentos de classe × genéricos |
| Skills, itens e terreno | Uso de cada skill, consumíveis e terrenos ativados |
| Empurrar e puxar (*Pushing and pulling*) | Deslocamentos, colisões, dano de colisão, abates por colisão, terrenos que reagiram e, desde o novo molde, os golpes de inimigos em inimigos por run: *Enemy hits on enemies (dmg)* (dano que inimigos causaram a outros inimigos, sem contar colisões) e *Kills by enemies* (inimigos mortos pelo golpe de outro) |
| Runs abortadas | Classe, seed, motivo, andar, inimigos e turno de cada uma |

**`runs.csv`** (uma linha por run) e **`battles.csv`** (uma linha por
batalha), para planilhas. Com vários lotes de uma vez, sai também uma tabela
comparando os lotes (`<data>_comparison.md`).

### Lendo os números

- **50 runs por classe** dão um intervalo de confiança de cerca de ±10
  pontos na vitória. Para diferenças pequenas, use 100 runs ou mais.
- **Mesmas seeds, mesmos mapas:** compare sempre lotes com as mesmas seeds;
  a diferença vem só da mudança. Para evitar ajustar só para aqueles mapas,
  confira o resultado final num segundo conjunto de seeds (por exemplo,
  `Start Seed` 5000).
- **Desgaste × dificuldade:** se as runs morrem muito mas o modo assistido
  (`Restore Hp Before Battle`) vence fácil, o problema é a vida perdida entre
  as batalhas, não as lutas.
- **Risco e recompensa:** rode a mesma classe com o mapa em `Normal` e em
  `Elite` e compare nível no chefe e vitória. Lembre que os andares 1 a 3 não
  têm elite.
- **Golpes desviados:** *Enemy hits on enemies* e *Kills by enemies* mostram
  quanto a classe usa os golpes anunciados contra os próprios inimigos (o
  centro do molde *Into the Breach*). Na medição de 2026-10-19 (20 runs por
  classe, seeds 1000 a 1019) foram 2,4 / 1,8 / 6,0 de dano e 0,6 / 0,5 / 2,1
  mortes por run (Guerreiro / Mago / Ladino).

## 5. Fluxo de trabalho sugerido

1. Rode um lote de referência antes de mudar qualquer coisa (por exemplo,
   `Label = antes`, 50 runs por classe, `Mixed`).
2. Mude os assets (orçamento, escala, XP, skills, talentos...).
3. Rode de novo com as **mesmas seeds** (`Label = depois`) e compare os dois
   `summary.md`.
4. Se melhorou, confira num segundo conjunto de seeds com 100 runs por
   classe.
5. Registre o que mudou e por quê em [[plano_balanceamento]] e
   [[valores_padrao_em_aberto]].

## 6. Rodando por script (vários lotes de uma vez)

Num arquivo `.cs` fora de `Assets/`, rodado pela CLI `unity`
(`unity command run_script --file MeuLote.cs --entry MeuLote.Run`). O
editor entra e sai do Play Mode sozinho:

```csharp
using GridBattle.Editor.Simulation;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Simulation;

public static class MeuLote
{
    public static string Run()
    {
        var elite = BalanceSimulation.LoadOptions();   // cópia das opções do asset
        elite.Label = "guerreiro-elite";
        elite.RunsPerClass = 50;
        elite.Map.Policy = EMapPolicy.Elite;
        elite.SetClasses(new[] { ECharacter.Warrior });

        var assistido = elite.Clone();
        assistido.Label = "guerreiro-assistido";
        assistido.RestoreHpBeforeBattle = true;

        BalanceSimulation.RunBatches(new[] { elite, assistido }, "resultado.txt");
        return "iniciado";
    }
}
```

- `RunBatches` roda todos os lotes numa sessão de Play Mode.
- `BalanceSimulation.Run(opções)` roda um lote só.
- O arquivo opcional (`resultado.txt`, na pasta temporária do sistema)
  recebe os resumos quando tudo termina. Um script pode esperar por ele para
  saber que a simulação acabou.

## 7. Usos além do balanceamento

- **Prova de que uma refatoração não mudou o jogo:** rode um lote antes e
  outro depois, com as mesmas seeds. Os `runs.csv` e `battles.csv` devem ser
  iguais, tirando as colunas de tempo (milissegundos).
- **Prova de que conteúdo novo não mudou o jogo base:** desligue o conteúdo
  novo só em memória (por exemplo, volte os pools de talentos ao que eram) e
  confira que o resultado de referência se repete (ver `AGENTS.md`,
  Verificação).
- **Procurar travamentos:** runs abortadas por limite de turnos mostram
  lutas que não terminam (já achou a RatQueen invocando sem fim e o Xamã
  invencível na Pedra de proteção).

## 8. Ferramenta complementar: progressão de XP

`GridBattle > Talents > Simulate XP Progression` não joga as batalhas: só
gera 300 mapas e soma o XP de caminhos que preferem batalhas normais ou de
elite (até 2026-10-19: fáceis, normais ou difíceis). Mostra, em segundos, o
nível que cada caminho alcança antes do chefe e a tabela "mean XP Normal /
Elite" por andar (em 2026-10-20: de 41 a 420 na normal, dos andares 1 a 29, e
de 170 a 1134 na elite, dos andares 4 a 29). Use-a para ajustar a curva de XP e o XP das batalhas; use o
simulador para tudo que depende das lutas.

## 9. Ao criar sistemas novos

Todo efeito visual, espera por tempo, som, dica, modal, métrica ou salvamento
novo precisa ser ignorado quando `SimMode.IsActive` (ver `AGENTS.md`). Sem
isso, o simulador trava esperando uma animação ou grava arquivos reais.
Exemplos prontos: `Cell`, `GridMovementAnimator`, `GridEffectsAnimator`,
`TurnManager`, `XpOrbsVfx`, `AudioManager`, `RunManager`,
`GameFlowController`, `ActionVisualPlayer`, `ForecastView` e `EnemyIconsView`.

Uma fase nova da run que espera uma escolha do jogador (como o acampamento e a
melhoria de skill do novo molde) também precisa de uma resposta do bot: o
`SimulationRunner` responde a `ERunPhase.Camp`, `ERunPhase.SkillUpgrade` e
`ERunPhase.ConsumableOffer` (inclusive a da recompensa da elite) pelo
`MapPolicy`. Sem isso, a run fica parada nessa fase até o limite de tempo.

## 10. Onde está o código

| Parte | Arquivo |
|---|---|
| Menu, Play Mode, barra de progresso | `Scripts/Editor/Simulation/BalanceSimulation.cs` |
| Interruptor global | `Scripts/Gameplay/Simulation/SimMode.cs` |
| Laço das runs, limites, segurança | `Scripts/Gameplay/Simulation/SimulationRunner.cs` |
| Bots | `BattleBot.cs`, `MapPolicy.cs`, `TalentPolicy.cs` |
| Coleta e relatório | `SimulationCollector.cs`, `SimulationRecords.cs`, `SimulationReport.cs` |
| Opções | `SimulationOptions.cs`, `BalanceSimulationSettings.cs` |

## 11. O que ainda não existe

Planejado em [[plano_balanceamento]] para quando o balanceamento voltar:
metas em asset conferidas automaticamente a cada lote, uma varredura e
ajuste automático de parâmetros que grava os melhores valores, e uma janela
no Editor ("Balance Lab") para rodar, comparar, aplicar e reverter mudanças
sem script.
