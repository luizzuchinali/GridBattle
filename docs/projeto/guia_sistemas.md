---
tags:
  - projeto
  - guia
created: 2026-10-05
---
# Guia dos sistemas do GridBattle

Este guia explica **o que foi criado para implementar o [[GDD]]**, onde cada
coisa fica e **como usar e configurar tudo pelo editor**. A referência técnica
para quem programa é o `AGENTS.md`, na raiz do repositório. Os valores
iniciais das perguntas em aberto estão em [[valores_padrao_em_aberto]].

> Regra geral: **conteúdo e números moram em assets** (ScriptableObjects) em
> `Assets/Application/Settings/`. Para mudar o jogo, quase sempre basta
> editar ou criar um asset no Inspector; o código não precisa mudar.

## 1. Mapa dos sistemas

```
Menu principal ──► Run (RunManager) ──► Mapa de nós ──► Nó
                                            ▲            ├─ Batalha / Chefe ─► Grid (turnos, combate, terreno, IA)
                                            │            ├─ Cura                       │
                                            │            ├─ Talento (custo)            ├─ XP ─► nível ─► oferta de talentos
                                            │            └─ Consumível                 │
                                            └──────────── vitória da batalha ◄─────────┘
Perfil (entre runs): classes liberadas, glossário, recordes, opções, dicas vistas
```

| Sistema | O que faz | Onde configurar |
|---|---|---|
| Personagens | Classes jogáveis e inimigos: atributos, skills, estados iniciais, visual | `Settings/Characters/` |
| Atributos e estados | Vida, dano, crítico, defesa... e estados (buffs/debuffs) com duração | configs dos personagens, `Settings/States/` |
| Combate | Fórmula de dano, defesa, crítico, distâncias | `Settings/Combat/CombatSettings` |
| Skills | Ações ativas com área, alcance e cooldown | `Settings/Skills/` |
| Inimigos e IA | Papéis, comportamentos, ações (curar, invocar, manter distância...) | `Settings/AI/` |
| Terreno | Obstáculos, células de perigo e de bônus | `Settings/Terrain/` |
| Consumíveis | Itens de uso único, sem gastar a ação | `Settings/Consumables/` |
| Run e mapa | Mapa de nós, geração de batalhas, cura, custo do nó de talento | `Settings/Run/`, `Settings/Map/` |
| XP e talentos | Curva de XP, nível máximo, ofertas, reroll/banir/pular, talentos | `Settings/Progression/`, `Settings/Talents/` |
| Meta e perfil | Liberação de classes, glossário, recordes, dicas, métricas | `Settings/Meta/`, `Settings/Tutorial/` |
| Interface | HUD, telas, modais, toque longo | `Settings/Hud/`, `Settings/Screens/`, `Settings/Modals/`, `Settings/Input/`, `Assets/Application/UI/` |
| Áudio | Efeitos e músicas por contexto | `Settings/Audio/AudioLibrary` |
| Idiomas | Textos em inglês, espanhol e português | `Assets/Application/Localization/` |
| Simulador de balanceamento | Bot que joga runs completas e gera um relatório (seção 8) | `Settings/Simulation/BalanceSimulationSettings` |

## 2. Conceitos que valem para tudo

### Definições, ids e o banco de conteúdo
- Todo conteúdo (personagem, skill, estado, talento, consumível, terreno,
  papel, ação de IA, dica) tem um **id estável** (o GUID do asset). É isso que
  os salvamentos guardam: **renomear ou mover um asset não quebra saves**;
  apagar, sim.
- O `GameDatabase` (`Settings/Resources/GameDatabase`) lista todo o conteúdo e
  se atualiza sozinho quando assets são criados, movidos ou apagados. Se algo
  não aparecer no jogo: menu **GridBattle > Rebuild Game Database**.

### Configurações por sistema
- Cada sistema tem **um asset de configuração** (CombatSettings, RunSettings,
  MapGenerationSettings...). Eles ficam registrados no `GameSettings`
  (`Settings/Resources/GameSettings`) automaticamente. Todos os campos têm
  tooltip explicando o efeito e, quando é uma pergunta em aberto do GDD, qual
  é.

### Efeitos compostos no Inspector
- Estados, skills e consumíveis têm listas de **efeitos** com um seletor de
  tipo (dropdown). Para montar um estado "Fúria" com +30% de dano, por exemplo:
  crie o estado, adicione um efeito *Attribute Modifier Effect* e configure
  `Damage Dealt` Flat +0,3.

### Textos e idiomas
- Nenhum texto do jogo fica no código. Nomes e descrições de conteúdo estão na
  tabela **Content**, e textos de interface na tabela **UI**:
  `Window > Asset Management > Localization Tables`.
- Em um asset de conteúdo, os campos **Display Name** e **Description**
  apontam para uma entrada da tabela (escolha a tabela `Content` e a chave). Os
  três idiomas são preenchidos na própria tabela.
- O idioma do jogo é o do sistema (inglês como reserva) até o jogador escolher
  outro nas Opções.
- ⚠️ A fonte pixel atual não tem acentos, ç, ñ, ¿ e ¡; esses caracteres saem
  em outra fonte até ela ser trocada.

### Sorteios e seed
- Toda a run é reproduzível: a mesma **seed** e as mesmas ações dão o mesmo
  mapa, as mesmas batalhas, críticos e ofertas. A seed não aparece para o
  jogador. Para depurar um cenário, fixe-a em `RunSettings > Debug Seed`
  (0 = aleatória).

### Ícones provisórios
- O conteúdo sem arte usa ícones gerados (formas coloridas) em
  `Art/Sprites/Placeholders/`. **Para trocar pela arte final, substitua o PNG
  no mesmo caminho** (mesmo nome) ou aponte o campo *Icon* do asset para o
  sprite novo. Ícones que aparecem no mundo (sobre inimigos) devem ter 8×8;
  os de interface, 16×16 (escala sempre inteira, pixel perfect).

## 3. Criando conteúdo

> Menu de criação: botão direito na pasta > **Create > GridBattle > ...**

### Classe jogável
1. `Create > GridBattle > Characters > Player Character Config`.
2. Preencha:
   - visual: sprite e animator;
   - atributos base: vida, movimento, alcance, dano, crítico, defesa e os
     atributos de skill;
   - `prefab = PlayerCharacter.prefab` e `characterClass`;
   - `battlesToUnlock`: batalhas vencidas, somadas entre runs, para liberar a
     classe;
   - `skills`: skills iniciais (toda classe começa com ao menos uma: Golpe,
     Nova de Gelo, Adaga Envenenada);
   - `talentPool`: talentos próprios da classe.
3. Textos: Display Name e Description na tabela `Content`.
4. Adicione a classe em `GameStateManager > Playable Characters` (cena) e um
   retrato no menu principal.

### Inimigo
1. `Create > GridBattle > Characters > Enemy Config`.
2. Preencha:
   - visual: arte própria, ou arte existente com `Tint`;
   - atributos, skills, `xpReward` e `prefab = Enemy.prefab`;
   - `behavior` (comportamento de IA) e `role` (papel);
   - `behaviorDescription`: texto do glossário e dos detalhes;
   - estados iniciais: por exemplo, Regeneração permanente para um inimigo
     regenerante.
3. Para entrar nas batalhas geradas, adicione o inimigo ao **pool** de
   `Settings/Map/BattleGenerationSettings` com profundidade mínima e máxima
   e peso.
4. Chefe: marque `isBoss` e coloque-o na lista do chefe em
   `BattleGenerationSettings`.
5. Resistências (opcionais, no config):
   - `canBeDisplaced` desmarcado: empurrões e puxões não o movem;
   - `immuneStates`: estados que ele ignora (o chefe ignora Atordoado);
   - `maxHpPercentDamageMultiplier`: quanto ele sofre da parte do dano
     periódico que depende da vida máxima (a Peçonha). 1 = normal, 0 = nada.

### Papel de inimigo
- `Create > GridBattle > AI > Enemy Role`: nome, descrição, **ícone 8×8**
  (aparece sobre o inimigo), `threatFactor` (peso na fórmula de ameaça da
  geração) e `countsAsFrontline` (usado pela regra "toda batalha tem um
  inimigo de linha de frente").

### Comportamento de IA
- Um `EnemyBehavior` (`Create > GridBattle > AI > Enemy Behavior`) é uma
  **lista de ações em ordem de prioridade**: no turno do inimigo, a primeira
  que conseguir agir encerra o turno.
- Ações disponíveis (`Create > GridBattle > AI > Actions`):
  - Use Skills;
  - Basic Attack;
  - Chase Target;
  - Keep Distance (atirador);
  - Apply State To Target (controlador);
  - Heal Ally e Buff Ally (suporte);
  - Summon (invocador).
- As ações de habilidade têm **cooldown em turnos do inimigo**, com a mesma
  regra das skills ("N turnos entre usos").
- Summon tem dois limites: invocados vivos ao mesmo tempo (`maxAlive`) e
  invocados por batalha (`maxTotalSummons`, 0 = sem limite). O segundo evita
  lutas sem fim quando o invocador fica fora de alcance.
- Exemplos prontos em `Settings/AI/`: ChaseAndAttack, Ranged, Support,
  Summoner e Controller.

### Skill
1. `Create > GridBattle > Skills > Skill Definition`.
2. Campos:
   - **tipo**: ofensiva, defensiva ou utilitária;
   - **dano** (fixo);
   - **forma da área**: Circle, Cross, Linear, Cone, Arc ou Perpendicular;
   - **tamanho**: ímpar, de 1 a 9;
   - **alcance**: distância Manhattan até o alvo. 0 = centrada no próprio
     personagem; *Unlimited Range* = qualquer célula;
   - **cooldown**: ações do jogador entre usos;
   - **quem é afetado**: inimigos, aliados ou todos;
   - **efeitos extras**: aplicar estados, curar, teletransportar,
     empurrar ou puxar (ver abaixo).
3. Para quem fica a skill:
   - skill inicial da classe: lista `skills` da classe;
   - liberada por talento: campo `unlockedSkill` do talento;
   - skill de inimigo: lista `skills` do inimigo (a IA usa pela ação *Use
     Skills*).
4. `Settings/Skills/SkillSettings`: 6 espaços na barra, se a skill inicial
   ocupa espaço e o cooldown mínimo.

#### Skill que empurra ou puxa
1. Crie a skill como acima (dano pode ser baixo) e, na lista de **efeitos
   extras**, adicione **Displace Skill Effect** pelo seletor:
   - **mode**: *Away From Caster* (empurra para longe de quem lança),
     *Toward Caster* (puxa; o alvo para ao lado de quem lança, nunca em cima) ou
     *Away From Area Center* (para longe da célula mirada; quem está no centro
     vai para longe de quem lança);
   - **distance**: células percorridas (para antes se algo atrapalhar).
2. O efeito roda depois do dano. Obstáculo, borda do grid ou outro personagem
   no caminho param o alvo e causam **dano de colisão** (a ele e ao personagem
   atingido); o terreno de perigo ou bônus onde ele para age na hora.
3. Valores da colisão: `Settings/Combat/CombatSettings`, seção *Displacement*
   (dano, extra por célula não percorrida, dano ao personagem atingido, se
   ignora defesa, se a borda conta). Duração do deslize e do impacto:
   `GridMovementSettings`.
4. Para um **inimigo** usar a skill, coloque-a na lista `skills` dele (com a
   ação *Use Skills* no comportamento). A IA só a usa quando a pontuação do
   resultado previsto chega ao mínimo: ajuste os pesos em *Displacement* no
   asset `Settings/AI/Actions/UseSkills`.
5. Para a skill ser do jogador, crie um talento que a libera (`unlockedSkill`)
   e coloque-o na `talentPool` da classe. Exemplos prontos: Golpe de Escudo,
   Rajada de Vento e Gancho (`Settings/Skills/<Classe>`), Gancho do Arpoador e
   Onda de Choque (`Settings/Skills/Enemies`).

#### Personagem imóvel
- No `CharacterConfig`, desmarque **Can Be Displaced** (o Rei Goblin usa isso):
  empurrões e puxões não o movem, mas ele continua bloqueando e leva o dano de
  quem for lançado contra ele.

### Estado (buff/debuff)
1. `Create > GridBattle > States > State Definition`.
2. Configure:
   - tipo (buff/debuff);
   - **política de reaplicação**: renovar, somar duração, acumular pilhas ou
     ignorar;
   - se aparece nos detalhes;
   - **efeitos**: modificar atributos, dano ou cura por turno, espinhos, roubo
     de vida, escudo, impedir movimento/ataque/skills, mudar as ferramentas
     da run (opções, rerolls, banimentos, pulos), **modificar skills**
     (*Skill Modifier Effect*), **mudar o dano conforme a situação**
     (*Conditional Damage Effect*), reagir ao **abate** (*On Kill Effect*) ou
     **aplicar estados ao acertar** (*Apply State On Hit Effect*); ver abaixo.
3. A **duração não fica no estado**: quem aplica decide (talento =
   permanente; skill, terreno ou IA = N turnos). A duração conta turnos de quem
   carrega o estado e cai no fim do turno dele (GDD 3.1).

### Talento
1. `Create > GridBattle > Talents > Talent`.
2. Campos:
   - classes permitidas (vazio = todas);
   - nível mínimo e pré-requisitos;
   - **tags de sinergia**: aumentam a chance de oferta quando combinam com a
     build;
   - `maxRank`: quantas vezes pode ser escolhido;
   - **estados** que concede (aplicados permanentemente; cada rank soma uma
     pilha);
   - skill liberada;
   - peso base.
3. Coloque o talento no `talentPool` da classe ou no pool compartilhado
   (`Settings/Talents/TalentOfferSettings`).
4. Tags: `Create > GridBattle > Talents > Synergy Tag`.

#### Talento que modifica uma skill
1. Crie um estado (`Create > GridBattle > States > State Definition`, em
   `Settings/States/Talents`) com um **Skill Modifier Effect**:
   - **Skills**: as skills afetadas (vazio = todas as do portador);
   - por pilha: **Damage Flat** e **Damage Percent** (só skills que causam dano),
     **Area Steps** (cada passo soma 2 ao tamanho ímpar), **Range** (não vale
     para skills centradas no lançador nem de alcance ilimitado),
     **Cooldown Reduction** (nunca abaixo do mínimo), **Displacement** (células a
     mais em todo empurrão/puxão da skill) e **State Duration** (turnos a mais
     nos estados temporários que a skill aplica);
   - **Attached Effects**: efeitos que se somam aos da skill (por exemplo
     *Apply States Skill Effect* com um estado nocivo, ou um *Displace*).
     *Repeat Attached Per Stack* repete a lista uma vez por pilha (posto 2 de um
     estado que acumula = 2 pilhas).
2. Crie o talento (`maxRank` N, **estado: pilha 1, permanente**) com o estado
   acima; cada posto soma uma pilha. A *Max Stacks* do estado e a política
   *Add Stacks* precisam comportar os postos (o `TalentChecks` confere).
3. Se a skill vem de outro talento, coloque esse talento nos **pré-requisitos**:
   assim o modificador só é oferecido depois de a skill estar na barra.
4. Textos e ícone: como nos outros talentos; **escreva os números na descrição**
   ("Golpe causa +3 de dano por nível").
5. Os números efetivos (barra, destaque de alcance, IA, bot) vêm sozinhos: tudo
   passa por `EffectiveSkill`, nada a configurar.

#### Passiva condicional
1. Crie um estado com um **Conditional Damage Effect**:
   - **Direction**: *Dealt* (bônus no que o portador causa) ou *Taken* (no que ele
     recebe; percentual negativo = redução);
   - **Applies To**: tipos de dano (padrão: ataque básico e skill);
   - **Condition**: sempre, por inimigo adjacente (teto em *Max Count*), sem
     inimigo adjacente, oponente com estado nocivo, oponente com um dos
     *States*, portador andou (ou não) no turno anterior, oponente isolado,
     oponente com vida cheia;
   - **Percent** (e **Flat**, só no dano causado) por contagem e por pilha.
2. Para recompensas ao matar use **On Kill Effect** (cura, reduz cooldowns, dá
   estados) e para aplicar estados nos golpes, **Apply State On Hit Effect**.
3. O talento concede o estado como acima (um posto = uma pilha). A adjacência
   vem de `CombatSettings.adjacencyMetric`.
4. Exemplos: `Settings/States/Talents/MeleeFrenzy`, `HoldTheLine`, `Predator`,
   `HitAndRun`, `ShadowFeast`, `CoatedWeapon`.

#### Peso dos talentos genéricos
`Settings/Talents/TalentOfferSettings`, campo **Shared Pool Weight Multiplier**
(0,5): multiplica o peso do talento que só chega à classe pelo pool
compartilhado, para os talentos da classe aparecerem mais. 1 desliga o efeito.

### Consumível
1. `Create > GridBattle > Consumables > Consumable Definition`.
2. Campos:
   - alvo: *Self* (usado na hora) ou *Cell* (escolhe uma célula, com alcance e
     área como uma skill);
   - efeitos: curar, causar dano ou aplicar estados;
   - peso de sorteio.
3. Adicione ao pool de `Settings/Consumables/ConsumableSettings`. Lá também
   ficam o número de espaços, o que fazer com o inventário cheio, quantas
   opções o nó de consumível oferece e o limite de um por turno.

### Terreno
1. `Create > GridBattle > Terrain > Terrain Definition`.
2. Campos:
   - tipo: obstáculo, perigo ou bônus;
   - se bloqueia movimento e se bloqueia a área de skills;
   - **gatilho**: ao entrar, no início ou no fim do turno de quem está na
     célula;
   - dano e estados aplicados;
   - quem é afetado;
   - sprite e cor da célula.
3. Adicione ao pool de `Settings/Terrain/TerrainGenerationSettings`, que
   define quanto terreno aparece por faixa de profundidade.
4. Para testar no editor, preencha `Debug Terrain` no `GridController` e use
   **Reset grid**.

### Dica de tutorial
- `Create > GridBattle > Meta > Tutorial Tip`: título (Display Name), texto
  (Description) e o **gatilho** (primeira batalha, primeiro mapa, primeira
  subida de nível, primeiro nó de cura...).
- Cada dica aparece uma vez por perfil, e o jogador pode desligá-las nas
  Opções.
- Um gatilho novo precisa de uma chamada `TutorialService.Notify(...)` no
  código.

## 4. Ajustando o jogo

| Quero mudar... | Asset | Campos |
|---|---|---|
| Tamanho e forma do mapa | `Map/MapGenerationSettings` | andares, andares de referência do balanceamento (`balanceFloorCount`: os valores por profundidade dos outros assets valem para esse tamanho e se esticam para o tamanho real), colunas, nós por andar, caminhos por nó, tipos de nó por profundidade, pesos de dificuldade, `diversifyBattleChoices` |
| Força das batalhas | `Map/BattleGenerationSettings` | pool de inimigos, fórmula de ameaça, orçamento por profundidade (`baseBudget` + `budgetPerDepth`, ou uma curva de pontos andar → orçamento em `budgetPoints`) e por dificuldade, escala de vida/dano por profundidade, limites de inimigos e de papéis, posições, chefe |
| Composição das lutas por andar | `Map/BattleGenerationSettings` | `compositionBands`: a partir de cada andar, mínimo de papéis diferentes por batalha e multiplicador do peso de cada papel no sorteio (por exemplo, mais atiradores e suportes no fim da run) |
| XP das batalhas | `Map/BattleGenerationSettings` | `xpSource` (`ThreatBudget`: XP pelo orçamento da batalha, fácil < normal < difícil), `xpPerThreat`, `xpPerDepth`, multiplicador por dificuldade; `xpBaseBudget`/`xpBudgetPerDepth` dão ao XP um orçamento próprio, para mudar a força das lutas sem mudar o ritmo de níveis |
| Curva de XP e nível máximo | `Progression/ProgressionSettings` | XP do nível 1→2, acréscimo por nível, nível máximo, sobra de XP, vida recuperada a cada nível (`levelUpHealFraction`, 1 = vida cheia) |
| Ofertas de talento | `Talents/TalentOfferSettings` | opções por oferta, rerolls, banimentos e pulos por run, peso de sinergia, limite de talentos de skill, pool compartilhado |
| Fórmula de dano | `Combat/CombatSettings` | defesa fixa ou %, dano mínimo, arredondamento, métricas de distância |
| Cura, custo do nó de talento, estados entre batalhas | `Run/RunSettings` | também a seed de depuração e o salvamento automático |
| Liberação de classes e glossário | `Meta/MetaSettings` + `battlesToUnlock` de cada classe | batalhas de runs perdidas contam, inimigos não vistos como "?", desistir conta como derrota, dicas, métricas |
| Toque longo | `Input/GameplayInputSettings` | tempo do toque longo |
| HUD, telas e modais | `Hud/HudSettings`, `Screens/ScreensSettings`, `Modals/ModalsSettings` | atributos na janela de detalhes; ícones e cores do mapa, XP nos nós, consumíveis no mapa; confirmação ao pular talento e ao desistir, dicas sobre a escolha de talento, glossário durante a escolha |
| Movimento e ritmo | `Movement/GridMovementSettings`, `TurnManager` (objeto `Grid`) | estilo dos pulos, ritmo dos inimigos |
| Sons | `Audio/AudioLibrary` | clipes, volume e variação por efeito; música por contexto |

## 5. Como o jogo flui

- **Run:**
  1. O jogador escolhe uma classe liberada.
  2. A run nasce com uma seed: o mapa e todas as batalhas são gerados nesse
     momento.
  3. No mapa, tocar num nó mostra a **prévia** (tipo, dificuldade, XP, papéis
     dos inimigos e terreno) e *Confirmar* entra no nó.
  4. A vida persiste entre os nós.
  5. A run termina com vitória no chefe, derrota ou desistência. A tela final
     mostra o resumo e as classes liberadas.
- **Batalha:**
  - Cada ação do jogador consome a vez: andar, atacar ou usar skill. Usar
    consumível não consome, no máximo um por turno.
  - Depois que o jogador age, cada inimigo age, o mais próximo primeiro.
  - A batalha acaba quando todos os inimigos morrem, sem limite de turnos.
- **Turno de cada personagem:**
  - no início, efeitos periódicos (veneno, regeneração) e terreno de início
    de turno;
  - no fim, terreno de fim de turno, cooldowns e a duração dos estados.
- **Subida de nível:**
  1. O XP chega quando os orbs alcançam a barra.
  2. Ao cruzar o limiar, o jogo pausa e oferece talentos.
  3. O jogador escolhe um, rerrola, bane ou pula.
  4. Com vários níveis de uma vez, as ofertas vêm em sequência.
- **Toque longo (ou clique direito)** numa célula mostra os detalhes da
  entidade ou do terreno: atributos, skills e estados com os turnos
  restantes.
- **Pausa** (botão de menu, na batalha e no mapa): continuar, glossário,
  opções, desistir. O jogo fica pausado enquanto ela está aberta.
- **Glossário** (menu principal, mapa e pausa): classes com os talentos já
  escolhidos (os outros como "?") e inimigos enfrentados (atributos base,
  papel, comportamento, skills, estados).
- **Opções:** idioma, volume da música e dos efeitos, dicas ligadas ou
  desligadas, mostrar as dicas de novo.

## 6. Salvamentos e arquivos

| Arquivo (em `persistentDataPath`) | Conteúdo | Quando é escrito |
|---|---|---|
| `run.json` | A run em andamento (mapa, jogador, sorteios, batalha no início da vez) | Após cada nó, no início de cada vez do jogador e quando o app pausa ou fecha. Apagado no fim da run. |
| `profile.json` | Perfil: batalhas vencidas, classes liberadas, glossário, recordes, opções, dicas vistas | A cada mudança |
| `metrics.jsonl` | Métricas de design (GDD 9), só locais | Durante o jogo |

No Windows, os arquivos ficam em
`%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle`. Para
recomeçar do zero, apague os três.

## 7. Depuração e testes

- **Batalha rápida no editor:**
  - o `GridController` (objeto `Grid`) monta uma batalha com `Debug Player
    Config`, `DefaultEncounter` e `Debug Terrain`;
  - o botão **Reset grid** refaz essa batalha;
  - em Play Mode, essa batalha roda fora de uma run.
- **Pular o mapa:** `RunSettings > Auto Enter First Node` entra sozinho no
  primeiro nó disponível.
- **Repetir um cenário:** `RunSettings > Debug Seed`.
- **Itens de teste:** `ConsumableSettings > Debug Starting Consumables` (só
  fora de uma run).
- **Checagens dos talentos:** `GridBattle > Talents > Run Checks`. **Ritmo de
  níveis:** `GridBattle > Talents > Simulate XP Progression`.
- **Simulador de balanceamento:** joga runs completas sozinho e mede o
  balanceamento. Veja a seção 8.
- **Ver a navegação de UI ao vivo:** `Window > ZS > UI Navigation Debugger`.

## 8. Simulador de balanceamento

O simulador joga **runs completas** com as regras reais do jogo, sem
animações, sons, dicas nem salvamento. Um bot escolhe os nós do mapa, luta as
batalhas e escolhe os talentos. Serve para medir o balanceamento e comparar
versões: muda-se um asset, roda-se de novo e comparam-se os números.

> O bot é simples: não foge, não planeja e não usa o terreno a seu favor.
> Um jogador vence mais que ele. Use os números para **comparar** versões do
> balanceamento, não como a taxa de vitória esperada de um jogador.

### Rodando pelo menu

1. Abra a cena `Assets/Scenes/SampleScene.unity` (a única do jogo).
2. Ajuste as opções em `GridBattle > Simulation > Select Settings Asset`
   (abre `Settings/Simulation/BalanceSimulationSettings`; o asset é criado no
   primeiro uso).
3. `GridBattle > Simulation > Run Balance Simulation`. O editor entra em
   Play Mode, mostra uma barra de progresso (com **Cancel**) e sai do Play
   Mode no fim. São cerca de 5 runs por segundo.
4. `GridBattle > Simulation > Open Output Folder` abre a pasta dos
   resultados. Com `Log Summary` ligado, o resumo também sai no Console.

O simulador **não grava** `run.json`, `metrics.jsonl` nem o perfil real
(usa um perfil temporário, apagado no fim). Uma run salva antes continua lá.

### Opções (`BalanceSimulationSettings`)

| Grupo | Campo | O que faz |
|---|---|---|
| Lote | `Label` | Nome do lote, usado no relatório e no nome da pasta |
| | `Classes` | Classes simuladas (vazio = todas as jogáveis) |
| | `Runs Per Class` | Runs completas por classe (30–50 dá números estáveis) |
| | `Start Seed` | Seed da primeira run; a run *i* usa `Start Seed + i` em todas as classes, então as classes jogam os **mesmos mapas** |
| | `Bot Seed` | Seed dos sorteios do próprio bot (separada da run) |
| Mapa | `Policy` | `Easy`, `Normal` ou `Hard` (prefere essa dificuldade) ou `Mixed` (sorteia) |
| | `Heal Below Hp Fraction` | Vai a um nó de cura quando a vida está abaixo dessa fração |
| | `Talent Node Min Hp Fraction` | Só entra em nó de talento (que custa vida) acima dessa fração |
| | `Battle/Heal/Talent/Consumable Weight` | Chance relativa de cada tipo de nó no resto das escolhas |
| Talentos | `Policy` | `FirstOption` (primeira opção), `HighestSynergy` (mais tags em comum com a build) ou `Random` |
| | `Reroll/Ban/Skip Without Synergy` | Usa reroll, banimento ou pulo quando nenhuma opção combina com a build |
| | `Preferred Talents` | Nomes de assets de talento que o bot pega sempre que aparecem, antes da política (mede uma build, por exemplo os talentos de empurrar e puxar) |
| Batalha | `Use Skills`, `Use Consumables` | Desligue para medir só o ataque básico |
| | `Skill Min Targets` | Usa uma skill de área quando acerta pelo menos esse número de inimigos (ou mata, ou causa mais dano que o ataque básico) |
| | `Potion/Support Item/Heal Skill Hp Fraction` | Vida abaixo da qual usa poções, itens de suporte e skills de cura |
| | `Area Item Min Targets` | Inimigos mínimos para usar uma bomba |
| | `Displace Out Of Reach Value`, `Displace Pull In Value`, `Displace Terrain State Value` | Valor, em vida, de empurrar para fora do alcance um inimigo que poderia atacar, de puxar um atirador para dentro do alcance e de cada estado do terreno onde o inimigo é mandado (ou do qual é tirado, se for de bônus). O dano de colisão e de terreno já conta como dano |
| | `Hazard Penalty`, `Bonus Reward` | Quanto o bot evita células de perigo e procura células de bônus |
| | `Focus Role Order` | Papéis que o bot mata primeiro (por nome do asset do papel) |
| | `Rush Role Names` | Papéis que o bot vai buscar mesmo podendo atacar outro (ex.: invocador) |
| Modo assistido | `Restore Hp Before Battle` | Enche a vida antes de cada batalha: mede **só a dificuldade das lutas**, sem o desgaste da run |
| Limites | `Max Turns Per Battle`, `Stuck Frames`, `Max Seconds Per Run`, `Max Failed Actions` | Batalhas ou runs que passam disso são abortadas e aparecem no relatório |
| Velocidade | `Action Budget Ms`, `Time Scale`, `Uncap Frame Rate` | Ritmo da simulação (não muda as regras) |
| Saída | `Output Folder`, `Write Csv`, `Log Summary` | Pasta dentro de `persistentDataPath`, CSVs e resumo no Console |

### O relatório

Cada lote cria `persistentDataPath/Simulation/<data>_<rótulo>/` (no Windows,
`%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle/Simulation`):

- **`summary.md`**, para ler:
  - saúde da simulação (runs abortadas, exceções, erros no Console; tudo
    deve ser 0);
  - **resultado por classe**: taxa de vitória com intervalo de confiança de
    95%, profundidade e nível médios, batalhas vencidas, talentos, uso de
    skills e itens;
  - **onde as runs terminam** (andar da morte), a distribuição das mortes e
    a vitória contra o chefe, e o **nível ao entrar em cada andar**;
  - **batalhas por faixa de andares**: inimigos, papéis diferentes, terreno,
    invocações e uso de skills e empurrões em cada parte da run;
  - **batalhas por andar** e por dificuldade: dano recebido (em % da vida
    máxima), ações por batalha, XP planejado e ganho;
  - **XP por nó**: mínimo, média e máximo por andar e dificuldade, e as
    **inversões** (uma batalha mais fácil dando mais XP que uma mais difícil
    no mesmo andar; deve ser 0);
  - **inimigos**: quem causa mais dano, quem termina as runs, dano por papel
    e por tipo (ataque básico, espinhos, terreno...);
  - **talentos** oferecidos e escolhidos (com a parcela de talentos de classe e
    de genéricos por run), **skills** usadas, **consumíveis**
    e **terreno** ativado;
  - **empurrar e puxar**: por classe, por run, quantos inimigos e quantas
    vezes o jogador foram deslocados, colisões, dano de colisão causado e
    sofrido, abates por colisão e terrenos que reagiram na hora.
- **`runs.csv`** (uma linha por run) e **`battles.csv`** (uma linha por
  batalha), para planilhas.

### Fluxo de trabalho sugerido

1. Rode um lote de referência antes de mudar qualquer coisa (por exemplo,
   `Label = antes`, 50 runs por classe, `Mixed`).
2. Mude os assets (orçamento, escala, XP, skills, talentos...).
3. Rode de novo com as **mesmas seeds** (`Label = depois`) e compare os dois
   `summary.md`. Com as mesmas seeds, a diferença vem só da mudança.
4. Para separar "lutas difíceis demais" de "desgaste de vida entre lutas",
   compare com um lote de `Restore Hp Before Battle` ligado.
5. Para avaliar a escolha de risco e recompensa, rode a mesma classe com o
   mapa em `Easy` e em `Hard`.

### Rodando por script (vários lotes de uma vez)

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
        var dificil = BalanceSimulation.LoadOptions();   // cópia das opções do asset
        dificil.Label = "guerreiro-dificil";
        dificil.RunsPerClass = 50;
        dificil.Map.Policy = EMapPolicy.Hard;
        dificil.SetClasses(new[] { ECharacter.Warrior });

        var assistido = dificil.Clone();
        assistido.Label = "guerreiro-assistido";
        assistido.RestoreHpBeforeBattle = true;

        BalanceSimulation.RunBatches(new[] { dificil, assistido }, "resultado.txt");
        return "iniciado";
    }
}
```

- `RunBatches` roda todos os lotes numa sessão de Play Mode e escreve, além
  das pastas de cada lote, uma **tabela comparando os lotes**
  (`<data>_comparison.md`).
- O arquivo opcional (`resultado.txt`, na pasta temporária do sistema)
  recebe os resumos quando tudo termina. Um script pode esperar por ele para
  saber que a simulação acabou.
- `BalanceSimulation.Run(opções)` roda um lote só.

### Ferramenta complementar: progressão de XP

`GridBattle > Talents > Simulate XP Progression` não joga as batalhas: só
gera 300 mapas e soma o XP de caminhos que preferem batalhas fáceis, normais
ou difíceis. Ela mostra, em segundos, o nível que cada caminho alcança antes
do chefe. Use-a para ajustar a curva de XP e o XP das batalhas; use o
simulador para tudo que depende das lutas.

### Ao criar sistemas novos

Todo efeito visual, espera por tempo, som, dica, modal, métrica ou salvamento
novo precisa ser ignorado quando `SimMode.IsActive` (ver `AGENTS.md`). Sem
isso, o simulador trava esperando uma animação ou grava arquivos reais.

## 9. Pendências conhecidas
- **Fonte pixel com acentos:** precisa de uma nova fonte (decisão de arte).
- **Conteúdo provisório:** inimigos de exemplo, chefe, talentos e ícones
  existem para os sistemas funcionarem e devem ser substituídos pelos
  definitivos.
- **Balanceamento: pausado** (decisão de 2026-10-06; o jogo ainda vai
  mudar bastante). Os valores atuais são um ponto de partida (ver
  [[valores_padrao_em_aberto]]). O que foi feito, a última medição e o que
  falta (inclusive a ferramenta de metas e ajuste automático com uma janela
  para uso humano) estão em `docs/planos/plano_balanceamento.md`.
- **Monetização (anúncios e compra):** fora do escopo por enquanto.
- **Áudio:** sistema pronto, sem clipes.
