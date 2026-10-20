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

> **Novo molde (2026-10-19).** O combate segue o *Into the Breach* e a
> progressão o *Slay the Spire*: não há ataque básico (toda ação é uma skill),
> todo golpe de inimigo é anunciado um turno antes, os inimigos agem numa
> ordem fixa e numerada, a previsão mostra o resultado de uma ação antes de
> confirmar, os talentos mudam regras ou liberam skills, e cada skill pode
> melhorar uma vez (no acampamento ou ao vencer uma elite). O resumo para
> leitura está em [[resumo_das_mudancas]]; a referência do combate é
> `docs/design/sistema_combate.md`.

> **Ajustes de 2026-10-20.** Skills canalizadas (*Uses Walk*: gastam também o
> andar; o Meteoro deixa o Mago Exausto), golpe básico sem recarga e especial
> com recarga em todo inimigo, XP em explosão (no máximo 8 orbs por abate) e
> batalhas só normal ou de elite (a elite dá mais XP, um consumível e uma
> melhoria de skill; o mapa não mostra XP).

> Regra geral: **conteúdo e números moram em assets** (ScriptableObjects) em
> `Assets/Application/Settings/`. Para mudar o jogo, quase sempre basta
> editar ou criar um asset no Inspector; o código não precisa mudar.

## 1. Mapa dos sistemas

```
Menu principal ──► Run (RunManager) ──► Mapa de nós ──► Nó
                                            ▲            ├─ Batalha / Chefe ─► Grid (turnos, combate, terreno, IA)
                                            │            ├─ Acampamento (descansar ou treinar = melhorar uma skill)
                                            │            ├─ Talento (custa vida máxima)├─ XP ─► nível ─► oferta de talentos
                                            │            └─ Consumível                 │
                                            └──────────── vitória da batalha ◄─────────┘ (elite vencida: consumível e melhoria de skill)
Perfil (entre runs): classes liberadas, glossário, recordes, opções, dicas vistas
```

| Sistema | O que faz | Onde configurar |
|---|---|---|
| Personagens | Classes jogáveis e inimigos: atributos, skills, estados iniciais, visual | `Settings/Characters/` |
| Atributos e estados | Vida, dano, crítico, defesa... e estados (buffs/debuffs) com duração | configs dos personagens, `Settings/States/` |
| Combate | Fórmula de dano, defesa, crítico, distâncias | `Settings/Combat/CombatSettings` |
| Skills | Ações ativas com área, alcance e cooldown; golpes anunciados dos inimigos; variantes de melhoria | `Settings/Skills/` |
| Previsão | O que a ação mirada e os golpes anunciados fariam, desenhado no tabuleiro (só visual) | `Settings/Combat/ForecastSettings` |
| Visual das ações | Como skills e itens aparecem no tabuleiro (gesto, projétil, efeito por célula) | `Settings/Vfx/ActionVisuals/` |
| Inimigos e IA | Papéis, comportamentos, ações (curar, invocar, manter distância...) | `Settings/AI/` |
| Terreno | Obstáculos, células de perigo e de bônus | `Settings/Terrain/` |
| Consumíveis | Itens de uso único, sem gastar a ação | `Settings/Consumables/` |
| Run e mapa | Mapa de nós, geração de batalhas, elites, acampamento, melhorias de skill, custo do nó de talento | `Settings/Run/`, `Settings/Map/` |
| XP e talentos | Curva de XP, nível máximo, vida máxima por nível, ofertas, reroll/banir/pular, talentos | `Settings/Progression/`, `Settings/Talents/` |
| Meta e perfil | Liberação de classes, glossário, recordes, dicas, métricas | `Settings/Meta/`, `Settings/Tutorial/` |
| Interface | HUD, telas, modais, toque longo | `Settings/Hud/`, `Settings/Screens/`, `Settings/Modals/`, `Settings/Input/`, `Assets/Application/UI/` |
| Áudio | Efeitos e músicas por contexto | `Settings/Audio/AudioLibrary` |
| Idiomas | Textos em inglês, espanhol e português | `Assets/Application/Localization/` |
| Simulador de balanceamento | Bot que joga runs completas e gera um relatório ([[simulador_balanceamento]]) | `Settings/Simulation/BalanceSimulationSettings` |
| Monetização | Anúncios forçados nas pausas da run, anúncio recompensado antes de cada uso das ferramentas de talento e compra de remoção de anúncios ([[monetizacao]]) | `Settings/Monetization/MonetizationSettings` |

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

### Ícones
- Talentos, estados, papéis, consumíveis, nós do mapa, terreno e ícones da
  interface têm arte final em `Art/Sprites/Icons/`, `Art/Sprites/Terrain/` e
  `Art/Sprites/UI/`. Estilo, pastas e como gerar ícones novos no mesmo estilo:
  [[arte]]. Para trocar um ícone, substitua o PNG no mesmo caminho (mesmo nome)
  ou aponte o campo *Icon* do asset para o sprite novo. Ícones que aparecem no
  mundo (sobre inimigos) devem ter 8×8; os de interface, 16×16 (escala sempre
  inteira, pixel perfect).
- Os ícones das skills também têm arte final, em `Art/Sprites/Icons/Skills/`.
  Os visuais das skills continuam provisórios (`Art/Sprites/Placeholders/Vfx/`).
- Os visuais de skills e itens também são provisórios, desenhados por código
  (ver [Visual de uma skill ou item](#visual-de-uma-skill-ou-item)).

## 3. Criando conteúdo

> Menu de criação: botão direito na pasta > **Create > GridBattle > ...**

### Classe jogável
1. `Create > GridBattle > Characters > Player Character Config`.
2. Preencha:
   - visual: sprite e animator;
   - atributos base: vida, movimento, crítico, defesa e os atributos de
     skill;
   - **Has Basic Attack** desligado (o padrão do código é ligado): no novo
     molde agir é sempre usar uma skill, e *Attack Distance* e *Basic Attack
     Damage* não são usados;
   - `prefab = PlayerCharacter.prefab` e `characterClass`;
   - `battlesToUnlock`: batalhas vencidas, somadas entre runs, para liberar a
     classe;
   - `skills`: a skill inicial (Guerreiro: Golpe; Mago: Rajada de Vento;
     Ladino: Adaga Envenenada). Até 5 outras vêm de talentos
     (`TalentOfferSettings > Max Skill Talents` = 5; a barra tem 6 espaços);
   - `talentPool`: talentos próprios da classe.
3. Textos: Display Name e Description na tabela `Content`. A descrição
   aparece na janela de detalhes do personagem (seleção de personagem).
4. Adicione a classe em `GameStateManager > Playable Characters` (cena) e um
   card na seleção de personagem (`MainMenuScreen.uxml`, dentro de
   `#character-selection`, e `MainMenuScreenController.BindClass`).

### Inimigo
1. `Create > GridBattle > Characters > Enemy Config`.
2. Preencha:
   - visual: arte própria (ver [[arte]]); arte existente com `Tint` só para
     provisórios;
   - atributos, `xpReward` e `prefab = Enemy.prefab`; **Has Basic Attack**
     desligado;
   - `skills`: golpes anunciados (ver
     [Golpe anunciado de inimigo](#golpe-anunciado-de-inimigo)). Todo inimigo
     precisa de um golpe **especial** (recarga maior que 0) e de um **básico**
     (recarga 0), nessa ordem na lista: a IA tenta na ordem, então prepara o
     especial quando ele está pronto e o básico nos outros turnos. Assim há um
     golpe anunciado em todo turno em que algum alcança o jogador;
   - `behavior` (comportamento de IA: em geral uma ação *Approach* e *Use Skills*) e
     `role` (papel);
   - `threat`: custo de complexidade que o gerador de batalhas conta para o
     inimigo (0 = calculado);
   - `behaviorDescription`: texto do glossário e dos detalhes;
   - estados iniciais: por exemplo, Regeneração permanente para um inimigo
     regenerante.
3. Para entrar nas batalhas geradas, adicione o inimigo ao **pool** de
   `Settings/Map/BattleGenerationSettings` com profundidade mínima e máxima
   e peso.
4. Chefe: marque `isBoss` e coloque-o na lista do chefe em
   `BattleGenerationSettings`.
5. **Elite:** marque `isUnique` e coloque o inimigo no pool. Ele nunca é
   sorteado como inimigo comum; cada batalha de elite leva uma elite
   disponível na profundidade (sorteada pelo peso do pool; a ameaça dela sai
   do orçamento), e o mapa só cria nós de elite onde há uma disponível. Vencer
   a batalha dá mais XP, uma oferta de consumível (`RunSettings > Elite
   Consumable Offers`) e uma melhoria de skill (`RunSettings > Elite Grants
   Skill Upgrade`). Elites atuais: Ogro brutamontes (a partir do andar 4),
   Cavaleiro esqueleto (10) e Golem de lava (18).
6. Resistências (opcionais, no config):
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
  **lista de ações em ordem de prioridade**. O turno do inimigo tem um andar e
  um agir, como o do jogador: a lista é lida de novo depois de cada sucesso, o
  primeiro andar que funciona usa o andar do turno e a primeira outra ação que
  funciona usa o agir.
- A vez do inimigo começa disparando o golpe preparado no turno anterior, se
  houver (disparar não gasta o agir: `CombatSettings > Firing Uses Action`
  desligado). Depois ele anda e prepara o próximo golpe com *Use Skills*.
  Curar, proteger, invocar e enfraquecer são imediatos.
- Ações disponíveis (`Create > GridBattle > AI > Actions`):
  - Use Skills (prepara o golpe anunciado);
  - **Approach For Skill**: anda até uma célula de onde uma skill pronta
    alcança o jogador. *Preferred Distance* é a distância preferida até o
    jogador (1, 2 e 3 nos assets `ApproachMelee`, `ApproachMid` e
    `ApproachRanged`); *Avoid Marked Cells* evita as células marcadas pelos
    golpes de outros inimigos; *Avoid Hazards* evita terreno de perigo;
  - Apply State To Target (controlador);
  - Heal Ally e Buff Ally (suporte);
  - Summon (invocador);
  - Basic Attack, Chase Target e Keep Distance continuam existindo, mas
    nenhum comportamento os usa no novo molde.
- As ações de habilidade têm **cooldown em turnos do inimigo**, com a mesma
  regra das skills ("N turnos entre usos").
- Summon tem dois limites: invocados vivos ao mesmo tempo (`maxAlive`) e
  invocados por batalha (`maxTotalSummons`, 0 = sem limite). O segundo evita
  lutas sem fim quando o invocador fica fora de alcance.
- Exemplos prontos em `Settings/AI/`: ChaseAndAttack, Ranged, Skirmisher,
  Stationary (só *Use Skills*), Support, Summoner e Controller.
- **Ordem dos inimigos:** os inimigos agem um de cada vez numa ordem fixa
  durante a batalha toda (`Turns/ActingOrder`): a lista gerada da batalha,
  depois os invocados na ordem em que chegaram; empate por linha e depois por
  coluna. O número aparece sobre cada inimigo, na célula mirada de cada golpe
  anunciado e nos detalhes do inimigo ("Ordem 1 de 3"). Não há o que
  configurar; o próximo inimigo só começa depois das animações do anterior
  (mais a pausa `Enemy Stagger` do `TurnManager`).

### Skill
1. `Create > GridBattle > Skills > Skill Definition`.
2. Campos:
   - **tipo**: ofensiva, defensiva ou utilitária;
   - **dano** (fixo);
   - **forma da área**: Circle, Cross, Linear, Cone, Arc ou Perpendicular;
   - **tamanho**: de 1 a 9, ímpar nas formas centradas no alvo; *Linear*
     começa no alvo e aceita tamanho par (número de células);
   - **área de seleção** (onde a skill pode ser mirada, como no Dofus; a área de efeito é outra coisa,
     desenhada em volta da casa mirada):
     - *Selection Shape*:
       - *Circle*: todas as casas no alcance (um losango);
       - *Line*: só a linha e a coluna de quem usa (cruz, o "lançar em linha" do Dofus);
       - *Diagonal*: só as diagonais;
       - *Star*: linha e diagonal;
       - *Square*: o quadrado inteiro.

       A distância é contada ao longo da forma (um passo na diagonal vale 1).
     - *Min Range*: menor distância mirável. 0 = a própria casa também; 1 = não a própria casa; 2 = nem
       as vizinhas;
     - *Range*: maior distância. 0 = centrada no próprio personagem;
     - *Range Modifiable*: o atributo Alcance de skills soma ao máximo ("alcance modificável" do Dofus;
       desligue para golpes corpo a corpo). Talentos que mudam o alcance da skill valem sempre;
     - *Unlimited Range*: sem máximo (a forma e o mínimo continuam valendo);
     - *Target Cell*: qualquer casa, casa vazia (teleporte, armadilha) ou casa com personagem;

     ```
     Círculo         Linha (cruz)    Diagonal        Estrela         Quadrado
     . . X . .       . . X . .       X . . . X       X . X . X       X X X X X
     . X X X .       . . X . .       . X . X .       . X X X .       X X X X X
     X X @ X X       X X @ X X       . . @ . .       X X @ X X       X X @ X X
     . X X X .       . . X . .       . X . X .       . X X X .       X X X X X
     . . X . .       . . X . .       X . . . X       X . X . X       X X X X X
     ```
     (alcance 2; `@` = quem usa; com alcance mínimo 1, o `@` sai; com mínimo 2, saem também as casas vizinhas)

   - **cooldown**: ações do jogador entre usos;
   - **Uses Walk** (seção *Turn cost (player)*): skill **canalizada**. Usá-la
     gasta o agir e também o andar do turno, então ela só pode ser usada antes
     de andar (desfazer o andar devolve a chance) e o jogador não anda depois.
     Vale só para o jogador; a IA dos inimigos ignora. Para o contra "não anda
     no próximo turno", acrescente um efeito *Apply States* com destinatário
     *Caster* e o estado **Exausto** por 2 turnos (como no Meteoro). O bônus
     de duração dos talentos não alonga estados aplicados no próprio lançador.
     A ficha da skill mostra a nota "Usa o andar do turno..." e a linha "Em
     você" com os estados no lançador;
   - **quem é afetado** (*Affects*): inimigos, aliados, aliados e o próprio,
     só o próprio, todos ou *Everyone But Caster* (todos menos quem lança; o
     padrão dos golpes anunciados dos inimigos, que também acertam outros
     inimigos);
   - **Ignores Line Of Sight**: a skill não precisa ver o alvo (obstáculos e
     personagens no caminho não bloqueiam); por exemplo, algo lançado por
     cima. As outras só miram células em linha de visão;
   - **Requires Affected Target**: só deixa mirar onde atinge alguém.
     Desligado em todas: a skill pode ser mirada em qualquer célula do
     alcance (o destaque cobre todo o alcance);
   - **efeitos extras**: aplicar estados, curar, teletransportar,
     empurrar ou puxar, trocar de lugar, investir e criar terreno (ver
     abaixo);
   - **Telegraph (enemies)**: golpe anunciado, só para inimigos (ver
     [Golpe anunciado de inimigo](#golpe-anunciado-de-inimigo));
   - **Upgrades (player skills)**: as variantes de melhoria (ver
     [Variantes de melhoria](#variantes-de-melhoria));
   - **Visual** (seção *Look*): como a skill aparece no tabuleiro (ver
     [Visual de uma skill ou item](#visual-de-uma-skill-ou-item)). Vazio =
     a investida (o gesto *Lunge*) nas skills ofensivas.
3. Para quem fica a skill:
   - skill inicial da classe: lista `skills` da classe;
   - liberada por talento: campo `unlockedSkill` do talento;
   - skill de inimigo: lista `skills` do inimigo (a IA usa pela ação *Use
     Skills*).
   - os números de uma skill (dano, área, alcance, recarga, empurrão,
     duração dos estados) sempre passam por `EffectiveSkill`, que soma os
     modificadores dos estados de quem usa; a barra, os destaques, a previsão,
     a IA e o bot leem dele.
4. `Settings/Skills/SkillSettings`: 6 espaços na barra, se a skill inicial
   ocupa espaço e o cooldown mínimo.

#### Golpe anunciado de inimigo
Todo golpe de inimigo é anunciado um turno antes (o molde *Into the Breach*).
Na seção **Telegraph (enemies)** da skill:
- **Telegraphed**: o inimigo prepara a skill em vez de usá-la na hora. As
  células da área ficam marcadas no tabuleiro (fixadas no momento em que ele
  prepara e salvas com a batalha) e, quando o golpe dispara, acerta quem
  estiver nelas. O jogador nunca telegrafa: para ele a opção não vale;
- **Windup Turns**: turnos do inimigo até disparar (1 = no próximo turno
  dele; todos os golpes atuais usam 1);
- **Can Move While Winding Up**: o inimigo pode andar enquanto prepara
  (desligado = fica parado);
- **Cancelled By Displacement**: empurrar ou puxar quem prepara cancela o
  golpe (desligado = o golpe ainda dispara);
- **Interruptible By States**: um estado que impede skills (Atordoado,
  Congelado) cancela o golpe preparado (ligado por padrão). A previsão já
  mostra isso: ao mirar uma skill que atordoa ou congela quem prepara, o
  golpe some da previsão;
- **Telegraph Follows Caster**: empurrar, puxar ou trocar de lugar quem
  prepara leva as células marcadas junto, com a mesma forma e direção (ligado
  por padrão). É o que permite virar o golpe contra outros inimigos. Não vale
  quando *Cancelled By Displacement* está ligado.

Use **Affects = Everyone But Caster** para o golpe acertar também outros
inimigos. A morte de quem prepara também cancela o golpe. Todos os 43 golpes
de `Settings/Skills/Enemies` (26 de 2026-10-19 e 17 de 2026-10-20) são
anunciados com 1 turno, seguem quem prepara, são interrompíveis e não deixam
andar enquanto preparam. Cada inimigo tem um especial com recarga e um básico
sem recarga (ver [Inimigo](#inimigo)); `docs/design/inimigos.md` tem a lista
de cada um. A previsão e o bot do
simulador leem o golpe sozinhos; não há nada a configurar neles.

#### Variantes de melhoria
Cada skill do jogador pode melhorar **uma vez**, escolhendo 1 de 2 variantes
(no acampamento, ao treinar, ou ao vencer uma elite, depois da oferta de consumível):
1. Copie a skill para `Settings/Skills/<Classe>/Upgrades/` e mude o que
   melhora (dano, área, alcance, recarga, efeitos).
2. Na variante, aponte **Upgrade Of** para a skill original. Na original,
   coloque a variante na lista **Upgrades** (2 por skill).
3. Escreva nome e descrição da variante nos 3 idiomas (tabela `Content`).
4. A variante não melhora de novo. Talentos que modificam a original também
   valem para as variantes.
5. A janela de melhoria (modal *SkillUpgrade*) mostra sozinha as linhas da
   ficha que mudam.

Exemplo: Bola de Fogo → Bola de Fogo Explosiva ou Bola de Fogo Arqueada
(`Settings/Skills/Mage/Upgrades/`).

#### Skill que troca de lugar, investe ou cria terreno
Efeitos da lista de **efeitos extras** (em `MovementSkillEffects.cs`):
- **Swap Skill Effect**: quem usa troca de célula com o personagem da célula
  mirada (que precisa estar vivo e poder ser deslocado). Se o alvo morreu com
  o dano da skill, quem usa vai para a célula liberada. Exemplo: Passo das
  Sombras;
- **Dash To Target Skill Effect**: quem usa avança em linha reta (uma das 8
  direções) até a célula mirada, ou até a última célula livre antes dela.
  Ponha antes de um *Displace* para o empurrão sair da célula nova. Exemplo:
  Investida com Escudo;
- **Create Terrain Skill Effect**: deixa terreno temporário:
  - **Terrain**: o terreno (um `TerrainDefinition` comum, como `IceWall`, a
    Muralha de Gelo; não precisa entrar no pool do gerador);
  - **Cells**: *Target Cell* (a célula mirada), *Area Cells* (toda a área) ou
    *Caster Cell* (a célula de quem usa, depois de qualquer movimento da
    skill);
  - **Turns**: rodadas que o terreno dura (some antes da vez do jogador
    quando acabam; 0 = o resto da batalha);
  - **Replace Terrain**: também substitui terreno que já existe (desligado =
    só piso livre). Terreno que bloqueia nunca vai para uma célula ocupada.

  Exemplos: Bola de Fogo (Fogo na célula mirada, 2 rodadas), Muralha de Gelo
  (Muralha na área, 2 rodadas) e a Maldição do Pântano da Bruxa do pântano
  (Pântano venenoso na área, 2 rodadas).

Troca de lugar e investida não contam como andar (não marcam "andou neste
turno").

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
5. Para a skill ser do jogador, ponha-a nas skills iniciais da classe ou crie
   um talento que a libera (`unlockedSkill`) e coloque-o na `talentPool` da
   classe. Exemplos prontos: Golpe de Escudo, Rajada de Vento e Gancho (skills
   iniciais, `Settings/Skills/<Classe>`), Gancho do Goblin arpoador e Onda de
   Choque (`Settings/Skills/Enemies`).
6. Valores atuais da colisão: 1 de dano a quem é lançado, 0 por célula não
   percorrida, 1 a quem é atingido, ignora a defesa, a borda conta.

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
     de vida, escudo, impedir movimento/ataque/skills (o Exausto é só *Behavior
     Restriction* com *Prevent Movement*), mudar as ferramentas
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

No novo molde os talentos **mudam regras ou liberam skills**; os 48 talentos
de número (mais dano, mais área, menos recarga) saíram. Os que ficaram:
talentos que liberam skills (por exemplo Grimório → Meteoro, Criomancia →
Muralha de Gelo, Passo Pesado → Pisão), talentos de regra (abaixo), passivas
condicionais e os compartilhados.
4. Tags: `Create > GridBattle > Talents > Synergy Tag`.

#### Talento de regra
Um talento de regra é um estado com um efeito que o jogo consulta no ponto
onde a regra vale (`States/Effects/RuleTalentEffects.cs`). Crie o estado em
`Settings/States/Talents` com um destes efeitos e o talento como acima:

| Efeito | Regra | Campos | Talento atual |
|---|---|---|---|
| **Domino Effect** | quando um personagem empurrado ou puxado pelo portador bate em outro, esse outro também é empurrado na mesma direção (e leva a própria colisão) | *Distance*: células do segundo empurrão (1) | Dominó (Guerreiro) |
| **Displacement Trail Effect** | quem o portador empurra ou puxa deixa terreno temporário nas células que deixou para trás (só em piso livre) | *Terrain* (não pode bloquear movimento; Fogo), *Turns* (2) | Rastro de Chamas (Mago) |
| **Slippery Push Effect** | um alvo com um dos estados que o portador empurra desliza pelo menos *Slide Distance* células, até algo pará-lo; puxões não mudam | *States* (Congelado, Gelado), *Slide Distance* (6) | Gelo Escorregadio (Mago) |
| **Counterstrike Effect** | quando um golpe anunciado mirado no portador dispara e erra (ele saiu ou foi tirado das células), o inimigo que golpeou ganha os estados | *States* (Exposto, 2 turnos) | Contragolpe (compartilhado) |
| **Swap Step Effect** | o andar do portador também pode trocar de lugar com um personagem vizinho (8 vizinhos) que possa ser deslocado | nenhum | Passo Trocado (Ladino) |

O **Ímpeto** (compartilhado) é um *On Kill Effect* com **Grant Extra Walk**:
ao matar, o jogador ganha o andar de volta naquele turno.

O Gelo Escorregadio já vale na previsão (`Displacement.GetDistance`).
⚠️ O empurrão em cadeia do Dominó **ainda não entra na previsão**: o jogo o
aplica, mas a tela não o mostra antes de confirmar.

#### Talento que modifica uma skill
Nenhum talento atual usa este caminho (os talentos de número saíram no novo
molde), mas o efeito continua disponível:
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
     *States*, portador andou (ou não) neste turno antes do golpe, oponente isolado,
     oponente com vida cheia;
   - **Percent** (e **Flat**, só no dano causado) por contagem e por pilha.
2. Para recompensas ao matar use **On Kill Effect** (cura, reduz cooldowns, dá
   estados) e para aplicar estados nos golpes, **Apply State On Hit Effect**.
3. O talento concede o estado como acima (um posto = uma pilha). A adjacência
   vem de `CombatSettings.adjacencyMetric`.
4. Exemplos: `Settings/States/Talents/Predator`, `Assassin`, `ArcaneFocus`,
   `Shatter`, `Brittle` (dano condicional), `Bloodlust`, `ShadowFeast`,
   `SoulHarvest` (ao matar) e `EnvenomedBlades` (estado ao acertar).

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
   - peso de sorteio;
   - **Visual**: como o item aparece no tabuleiro (a Bomba voa em arco e
     explode na área).
3. Adicione ao pool de `Settings/Consumables/ConsumableSettings`. Lá também
   ficam o número de espaços, o que fazer com o inventário cheio, quantas
   opções o nó de consumível oferece e o limite de um por turno.

### Visual de uma skill ou item
A aparência de uma skill ou de um item é um asset à parte, o **Action
Visual** (`Create > GridBattle > Vfx > Action Visual`), ligado no campo
*Visual* da skill ou do consumível. É só aparência: as regras nunca o leem e
o simulador o ignora.

Em vez de uma animação da ação inteira, ele monta a ação com peças pequenas.
Uma animação inteira teria de ser desenhada duas vezes, para linhas e para
colunas, porque a célula é retangular.

1. **Caster**: o gesto de quem usa:
   - *Lunge*: a investida (o gesto que o antigo ataque básico usava);
   - *Glow*: pisca na cor *Glow Color* (magias e itens);
   - *None*: nada.
2. **Projectile** (opcional): quadros de algo que voa de quem usa até a
   célula mirada:
   - velocidade em células por segundo;
   - *Arc*: altura do arco, em células (a bomba usa cerca de 0,6);
   - *Faces Direction*: gira para a direção do voo (desligue para coisas
     redondas).
3. **Effect on each cell of the area**: quadros tocados em **cada célula da
   área**, numa ordem que desenha a forma:
   - *Order*:
     - *Simultaneous*: todas juntas;
     - *Ripple From Target*: em onda a partir da célula mirada (explosões);
     - *Away From Caster*: da célula mais perto para a mais longe (linhas,
       cones);
     - *Sweep*: de um lado para o outro, atravessando a direção do golpe
       (cortes em arco).
   - *Step Seconds*: intervalo entre um passo da ordem e o próximo; *Frame
     Seconds*: duração de cada quadro;
   - *Impact Frame*: o quadro em que o golpe acerta. A reação de quem é
     atingido, o número de dano, o efeito de morte e os orbs de XP esperam
     esse quadro na célula da pessoa;
   - *Cell Facing*:
     - *None*: arte redonda;
     - *Caster To Target*: cortes e estocadas;
     - *Away From Target*: empurrões a partir do centro. O vento da Rajada
       aponta para onde cada inimigo é empurrado.

     A reação de quem é atingido segue a mesma direção.
   - *Drop Pixels*: o efeito cai de cima antes de tocar (raio);
   - *Linger Seconds*: o último quadro fica e some aos poucos (fumaça,
     veneno);
   - *Tint*: cor multiplicada sobre os quadros.

**Arte:**
- cada quadro cabe num **quadrado da largura da célula** (hoje 24×24, PPU
  100, filtro *Point*), centrado. A mesma arte serve para linhas e colunas;
- arte com direção é desenhada **virada para a direita**, e o jogo a gira em
  passos de 90°. Nunca há rotação em outros ângulos nem escala fracionária
  (pixel perfect).

**Provisórios:** `GridBattle > Vfx > Generate Placeholder Visuals`:
- desenha quadros simples (impacto, fogo, gelo, veneno, fumaça, raio, vento,
  corte, onda, orbe, bomba, adaga e gancho) em `Art/Sprites/Placeholders/Vfx/`;
- cria um Action Visual por skill e um para a Bomba em
  `Settings/Vfx/ActionVisuals/`;
- só liga um visual onde o campo *Visual* está vazio; a sua escolha é
  mantida.

⚠️ A cada execução, o gerador **redesenha os PNGs e reescreve os campos dos
visuais provisórios**. Para a arte final, crie sprites e visuais com outros
nomes (ou troque os PNGs e não rode o gerador de novo).

### Terreno
1. `Create > GridBattle > Terrain > Terrain Definition`.
2. Campos:
   - tipo: obstáculo, perigo ou bônus;
   - se bloqueia movimento, se bloqueia a área de skills e se bloqueia a
     **linha de visão** (só para quem bloqueia movimento, como a Rocha);
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
| Tamanho e forma do mapa | `Map/MapGenerationSettings` | andares, andares de referência do balanceamento (`balanceFloorCount`: os valores por profundidade dos outros assets valem para esse tamanho e se esticam para o tamanho real), colunas, nós por andar, caminhos por nó, tipos de nó por profundidade, pesos de batalha normal e de elite (`normalWeight` 75, `eliteWeight` 25; elite só onde há uma elite na profundidade), `diversifyBattleChoices` |
| Força das batalhas | `Map/BattleGenerationSettings` | pool de inimigos, fórmula de ameaça, orçamento por profundidade (`baseBudget` + `budgetPerDepth`, ou uma curva de pontos andar → orçamento em `budgetPoints`) e por tipo de batalha (perfis Normal e Elite em `difficulties`), escala de vida/dano por profundidade, limites de inimigos e de papéis, posições, chefe; toda batalha de elite tem um inimigo `isUnique` do pool |
| Composição das lutas por andar | `Map/BattleGenerationSettings` | `compositionBands`: a partir de cada andar, mínimo de papéis diferentes por batalha e multiplicador do peso de cada papel no sorteio (por exemplo, mais atiradores e suportes no fim da run) |
| XP das batalhas | `Map/BattleGenerationSettings` | `xpSource` (`ThreatBudget`: XP pelo orçamento da batalha, normal < elite), `xpPerThreat`, `xpPerDepth`, multiplicador de XP de cada perfil (normal 1, elite 2); `xpBaseBudget`/`xpBudgetPerDepth` dão ao XP um orçamento próprio, para mudar a força das lutas sem mudar o ritmo de níveis |
| Curva de XP e nível máximo | `Progression/ProgressionSettings` | XP do nível 1→2 (150), acréscimo por nível (100), nível máximo (12), sobra de XP, vida máxima ganha a cada nível sem cura (`maxHpPerLevel`, 1), vida recuperada a cada nível (`levelUpHealFraction`, 0 no novo molde; 1 = vida cheia) |
| Ofertas de talento | `Talents/TalentOfferSettings` | opções por oferta, rerolls, banimentos e pulos por run, peso de sinergia, limite de talentos de skill, pool compartilhado |
| Fórmula de dano | `Combat/CombatSettings` | defesa fixa ou %, dano mínimo (`minimumDamage` 0: a armadura pode zerar um golpe de 1), arredondamento, métricas de distância, colisão de empurrões e puxões, se disparar um golpe anunciado gasta o agir do inimigo (`firingUsesAction`, desligado) |
| Previsão | `Combat/ForecastSettings` | só visual; ver [Previsão](#previsão) abaixo |
| Mirar e confirmar | `Input/GameplayInputSettings` | `confirmTargetedActions` (ligado): o 1º toque mira e mostra a previsão, o 2º toque na mesma célula (ou Confirmar) usa; desligado = usa no 1º toque |
| Linha de visão das skills | `Combat/CombatSettings` + cada skill e terreno | `skillsNeedLineOfSight` (liga a regra), `charactersBlockLineOfSight` (personagens bloqueiam); `ignoresLineOfSight` em cada skill; `blocksLineOfSight` em cada terreno |
| Acampamento, custo do nó de talento, elite, estados entre batalhas | `Run/RunSettings` | cura do acampamento (`healFraction` 0,4, mais `healFlat`); `campOffersSkillUpgrade` (o acampamento deixa escolher entre descansar e treinar = melhorar uma skill); `talentNodeCost` (`MaxHp`: perde vida máxima pelo resto da run) e `talentNodeCostMaxHp` (2; os campos `talentNodeCostHpFraction` e `talentNodeCostHpFlat` valem para os outros tipos de custo); recompensa da elite: `eliteConsumableOffers` (1: ofertas de consumível, escolher 1 de 3, antes da melhoria; 0 = nenhuma) e `eliteGrantsSkillUpgrade` (vencer uma batalha de elite dá uma melhoria); também a seed de depuração e o salvamento automático |
| Liberação de classes e glossário | `Meta/MetaSettings` + `battlesToUnlock` de cada classe | batalhas de runs perdidas contam, inimigos não vistos como "?", desistir conta como derrota, dicas, métricas |
| Toque longo | `Input/GameplayInputSettings` | tempo do toque longo |
| HUD, telas e modais | `Hud/HudSettings`, `Screens/ScreensSettings`, `Modals/ModalsSettings` | atributos na janela de detalhes; ícones e cores do mapa (batalha normal e de elite: `battleEliteIcon`, `eliteColor`; o XP dos nós saiu em 2026-10-20), consumíveis no mapa; confirmação ao pular talento e ao desistir, dicas sobre a escolha de talento, glossário durante a escolha |
| Movimento e ritmo | `Movement/GridMovementSettings`, `TurnManager` (objeto `Grid`) | estilo dos pulos, pausa entre um inimigo e o próximo (`enemyStagger`) |
| Orbs de XP | `Vfx/XpOrbsVfxSettings`, `XpRewardSystem` (objeto `XpRewardSystem`) | explosão (`burstRadius` 14, `burstRadiusVariance` 6, `burstAngleJitter` 0,5, `burstDuration` 0,22, `burstEase`), pausa (`holdDuration` 0,08), voo (`flightDuration` 0,45, `flightEase`, arco) e variação de partida (`launchJitter` 0,06); XP por orb (`xpPerPacket`, 5) e máximo de orbs por abate (`maxPackets`, 8: acima disso cada orb vale mais) |
| Sons | `Audio/AudioLibrary` | clipes, volume e variação por efeito; música por contexto |

### Previsão
A previsão (`Gameplay/Forecast`) mostra no tabuleiro o que vai acontecer
antes de o jogador confirmar, no estilo *Into the Breach*:
- as células de cada golpe anunciado, com contorno vermelho onde alguém seria
  atingido, e o número da ordem de quem lança na célula mirada do golpe;
- a ordem de cada inimigo sobre a cabeça dele e, enquanto ele prepara um
  golpe, o ícone da skill e os turnos que faltam;
- o dano que cada personagem levaria, piscando na barra de vida com o número
  (vermelho quando mataria);
- com uma skill ou item mirado: o dano e as mortes da ação, setas dos
  empurrões e puxões, fantasmas onde os personagens terminariam, e os golpes
  anunciados como ficariam depois (levados junto com quem foi deslocado,
  sumidos com quem morreria ou seria atordoado ou congelado).

A previsão não sorteia (sem crítico) e é calculada antes dos escudos. Ela usa
as mesmas regras da execução; a exceção conhecida é o Dominó (ver
[Talento de regra](#talento-de-regra)).

`Settings/Combat/ForecastSettings` (`Create > GridBattle > Settings >
Forecast Settings`) só muda a aparência:

| Campo | O que faz | Valor |
|---|---|---|
| *Show Acting Order* | número da ordem sobre cada inimigo | ligado |
| *Show Order On Strikes* | número de quem lança na célula mirada de cada golpe | ligado |
| *Strike Order Offset* | deslocamento desse número a partir do centro da célula, em pixels | (-11, 16) |
| *Strike Order Spacing* | pixels entre os números de vários golpes mirados na mesma célula (largo o bastante para "1 3" não parecer "13") | 9 |
| *Order Font Size*, *Order Color* | tamanho e cor dos números da ordem | 1,5, branco |
| *Later Strike Color* | cor do número de um golpe que só dispara numa fase de inimigos posterior | cinza claro |
| *Show Predicted Damage* | mostra nas barras de vida o dano previsto | ligado |
| *Blink Period* | segundos de uma piscada da parte da barra que o golpe tiraria | 0,6 |
| *Damage Text Color*, *Lethal Text Color*, *Damage Text Offset* | cores do dano previsto (normal e quando mata) e deslocamento do número | amarelo, vermelho, (11, -8) |
| *Arrow Color*, *Arrow Offset* | cor das setas de empurrão e puxão e quanto saem do centro da célula, em pixels | ciano, 10 |
| *Ghost Alpha* | opacidade do fantasma onde um personagem empurrado, puxado, trocado ou que investe terminaria | 0,45 |

## 5. Como o jogo flui

- **Menu:**
  1. A tela inicial ("toque para jogar") leva ao menu principal: Novo jogo,
     Continuar (só com uma run em andamento), Glossário e Opções.
  2. Novo jogo mostra a seleção de personagem. Tocar num card abre os
     detalhes da classe (descrição, atributos, skills iniciais); "Jogar"
     começa a run (com uma run em andamento, pede confirmação antes).
- **Run:**
  1. O jogador escolhe uma classe liberada.
  2. A run nasce com uma seed: o mapa e todas as batalhas são gerados nesse
     momento.
  3. No mapa, tocar num nó mostra a **prévia** ("Batalha" ou "Batalha de
     elite", papéis dos inimigos, terreno e, na elite, a recompensa em
     palavras; sem XP) e *Confirmar* entra no nó.
  4. A vida, a vida máxima extra (níveis e custo dos nós de talento), as
     skills (já melhoradas), os consumíveis e os talentos persistem entre os
     nós.
  5. **Acampamento** (o nó de cura): descansar cura 40% da vida máxima;
     treinar abre a melhoria de skill (escolher uma skill e 1 de 2
     variantes). Sem skill para melhorar, só dá para descansar. Recusar a
     melhoria volta à escolha.
  6. **Nó de talento:** custa 2 de vida máxima pelo resto da run e abre uma
     oferta de talentos.
  7. **Batalha de elite:** tem uma elite e dá mais XP; vencê-la abre a
     oferta de consumível ("Recompensa da elite", escolher 1 de 3) e depois a
     melhoria de skill. Recusar uma das duas abre mão só dela. Os andares 1 a
     3 só têm batalhas normais.
  8. A run termina com vitória no chefe, derrota ou desistência. A tela final
     mostra o resumo e as classes liberadas.
- **Batalha:**
  - Em cada turno o jogador anda uma vez, age uma vez (uma skill; não há
    ataque básico) e pode usar um consumível, em qualquer ordem. O turno só
    acaba sozinho quando nada disso é mais possível; senão, com "Encerrar
    turno" (sempre visível) ou tocando no próprio personagem. Dá para desfazer
    o andar enquanto não agiu. Uma skill canalizada (Meteoro e Tempestade
    Elétrica do Mago) gasta o agir e o andar: só antes de andar, e o Meteoro
    deixa o Mago sem andar no turno seguinte (Exausto).
  - Skill ou item com alvo: o 1º toque numa célula válida mira e mostra a
    previsão; o 2º toque na mesma célula ou *Confirmar* usa; *Cancelar*
    desfaz a mira.
  - Tocar num inimigo mostra até onde ele pode andar a partir de onde está.
  - Depois, cada inimigo age, um de cada vez, na ordem fixa numerada: dispara
    o golpe preparado (acerta quem estiver nas células marcadas, inimigos
    inclusive, menos quem lança), anda e prepara o próximo golpe. A batalha
    começa sem golpe preparado: cada inimigo prepara o primeiro no seu turno,
    depois da primeira vez do jogador.
  - No fim da rodada, o terreno temporário perde uma rodada.
  - A batalha acaba quando todos os inimigos morrem, sem limite de turnos.
- **Turno de cada personagem:**
  - no início, efeitos periódicos (veneno, regeneração) e terreno de início
    de turno;
  - no fim, terreno de fim de turno, cooldowns e a duração dos estados.
- **Subida de nível:**
  1. O XP chega quando os orbs alcançam a barra: os orbs de um abate saem
     juntos numa mini explosão e voam juntos (~0,8 s).
  2. Ao cruzar o limiar, o jogador ganha 1 de vida máxima (sem cura), o
     jogo pausa e oferece talentos.
  3. O jogador escolhe um, rerrola, bane ou pula.
  4. Com vários níveis de uma vez, as ofertas vêm em sequência.
- **Toque longo (ou clique direito)** numa célula mostra os detalhes da
  entidade ou do terreno: atributos, skills e estados com os turnos
  restantes; num inimigo, também a ordem ("Ordem 1 de 3") e o golpe
  preparado ("Preparando: <golpe> (em 1 turno)"). O mesmo gesto num botão de skill ou de item mostra a ficha dele
  (números com os talentos aplicados). O tempo do toque longo é o mesmo
  (`Input/GameplayInputSettings`).
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
- **Validar o conteúdo:** `GridBattle > Validate Content` confere, sem mudar
  nada: textos faltando em algum idioma, chaves de texto usadas no código que
  não existem na tabela, referências vazias (skills, efeitos, pools, ações de
  IA), talentos que nunca são oferecidos, inimigos que nunca aparecem, telas
  mal configuradas, ícones faltando e assets fora do banco de conteúdo. Cada
  problema sai no Console; clique para selecionar o asset. Rode depois de criar
  ou mudar conteúdo.
- **Checagens dos talentos:** `GridBattle > Talents > Run Checks`. **Ritmo de
  níveis:** `GridBattle > Talents > Simulate XP Progression`.
- **Simulador de balanceamento:** joga runs completas sozinho e mede o
  balanceamento. Veja [[simulador_balanceamento]].
- **Ver a navegação de UI ao vivo:** `Window > ZS > UI Navigation Debugger`.
- **Resetar o jogo (build de desenvolvimento e Editor):** *Opções > Desenvolvimento > Resetar o jogo*, depois
  de confirmar:
  - encerra a run em andamento e apaga o save;
  - apaga o perfil (classes, glossário, recordes, dicas vistas, opções, a compra de remover anúncios e os
    contadores de anúncios) e as métricas;
  - volta ao menu principal.

  No Editor, apaga os **seus** arquivos. Uma compra feita numa conta de loja volta na próxima consulta à
  loja. A opção não existe em build de produção.
- **Monetização no Editor:** anúncios e loja de teste (`MonetizationSettings > Editor Mode`). O menu
  `GridBattle > Monetization > Testing` marca ou desmarca a compra no perfil e zera os contadores dos
  anúncios forçados.

## 8. Simulador de balanceamento

O simulador joga runs completas sozinho, com as regras reais, e gera um
relatório para comparar versões do balanceamento. Rodar:
`GridBattle > Simulation > Run Balance Simulation` (opções em
`Settings/Simulation/BalanceSimulationSettings`).

Tudo sobre ele (como funciona, opções, relatório, fluxo de trabalho, uso por
script e cuidados ao criar sistemas novos) está em
[[simulador_balanceamento]].

## 9. Monetização

Anúncios do Unity LevelPlay e compra pelo Unity IAP (Google Play e App Store), só no Android e no iOS:
- **anúncio forçado** nas pausas da run (nunca na batalha), com carência, fase inicial com pouca frequência,
  frequência normal, tempo mínimo e um aviso antes;
- **anúncio recompensado** antes de cada uso de trocar, banir ou pular: o uso só acontece se o anúncio for
  assistido até o fim e conta no limite da run como sempre;
- **remoção de anúncios** no menu, nas Opções e na tela de derrota. Depois da compra, as ferramentas
  funcionam sem anúncio, com o mesmo limite.

Tudo é configurado em `Settings/Monetization/MonetizationSettings`. Como funciona e os valores iniciais
estão em [[monetizacao]]. O passo a passo para ligar os anúncios e a compra de verdade (painéis, chaves,
builds, testes no celular, privacidade) está em [[habilitar_anuncios_e_compras]].

## 10. Pendências conhecidas
- **Fonte pixel com acentos:** precisa de uma nova fonte (decisão de arte).
- **Conteúdo provisório:** inimigos de exemplo, chefe e talentos
  existem para os sistemas funcionarem e devem ser substituídos
  pelos definitivos. Os demais ícones já têm arte final (ver [[arte]]).
- **Balanceamento: pausado** (decisão de 2026-10-06; o jogo ainda vai
  mudar bastante). Os valores atuais são um ponto de partida (ver
  [[valores_padrao_em_aberto]]). O que foi feito, a última medição e o que
  falta (inclusive a ferramenta de metas e ajuste automático com uma janela
  para uso humano) estão em `docs/planos/plano_balanceamento.md`.
- **Monetização:** implementada (ver [[monetizacao]]). Faltam:
  - as chaves do LevelPlay e o produto nas lojas;
  - o perfil de build do iOS;
  - a tela de consentimento e o ATT (decisão sua).
- **Áudio:** sistema pronto, sem clipes.
- **Visual das skills e itens:** protótipo com arte provisória desenhada por
  código. Falta avaliar jogando e trocar pela arte final; as skills sem
  visual próprio continuam com a investida.
- **Previsão do Dominó:** o empurrão em cadeia ainda não aparece na previsão
  (`DisplacementResolver.Predict` não o calcula).
- **Novo molde sem balanceamento:** os números do novo molde (vida das
  classes, golpes, orçamento) são um ajuste provisório pelo simulador; o
  balanceamento continua pausado. Os 17 golpes de 2026-10-20, a elite com
  ×2,7 de XP e o custo das skills canalizadas também são provisórios.
- **Turnos sem golpe anunciado:** ainda acontecem quando nenhum golpe do
  inimigo alcança o jogador (o Totem do raio nunca anda), quando um inimigo de
  apoio cura, protege, invoca ou enfraquece, e no primeiro turno da batalha.
