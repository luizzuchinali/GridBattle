---
tags:
  - design
  - combate
  - sistemas
created: 2026-09-29
updated: 2026-10-20
aliases:
  - Sistema de Combate
  - Turnos
---

# Sistema de Combate

Documento de referência do combate em grid do GridBattle **como está
implementado**: configuração de personagens, atributos e estados, cálculo de
dano, skills, golpes anunciados, previsão, consumíveis, ações do jogador,
sistema de turnos, IA dos inimigos, terreno, morte, XP e talentos, e os pontos
de extensão. As regras de design vivem no [[GDD]] e nos subdocumentos; este
documento descreve como o código as realiza e quais são os valores atuais dos
assets.

> [!note] Molde novo de 2026-10-19 (implementado)
> O combate segue a direção do [[GDD]] de 2026-10-19 (Into the Breach com a run
> de Slay the Spire): **não há ataque básico** (agir é sempre usar uma skill),
> **todo golpe de inimigo é anunciado** um turno antes nas células que vai
> atingir, os inimigos agem numa **ordem fixa e numerada**, o jogador **mira e
> confirma** vendo a **previsão** do resultado, os **números são pequenos**
> (vida de 2 a 22, dano de 1 a 3, colisão 1), subir de nível dá **+1 de vida
> máxima sem curar**, há **talentos que mudam regras**, **melhorias de skill**
> (variantes), **acampamento**, **elites** e um nó de talento que custa vida
> máxima. O que ainda não existe está marcado como "não implementado" em cada
> seção.

> [!note] Mudanças de 2026-10-20 (implementadas)
> Pedidos do Luiz: magias **canalizadas** do Mago (o Meteoro e a Tempestade
> Elétrica gastam também o andar; o Meteoro deixa o Mago **Exausto** no turno
> seguinte), **um golpe anunciado a cada turno** (todo inimigo tem um golpe
> básico sem recarga e um especial com recarga), **XP em explosão** (as bolinhas
> de um abate saem juntas e voam juntas, em ~0,8 s) e batalhas só **normal** ou
> **de elite** (a elite dá mais XP, um consumível e uma melhoria de skill; o mapa
> não mostra XP).

> [!warning] Números provisórios
> O balanceamento está **pausado por decisão do Luiz**. Todos os valores de
> conteúdo deste documento (vida, dano, recargas, ameaça, XP, talentos) são
> provisórios: vieram da migração para o molde novo e de um ajuste inicial pelo
> simulador (commit `d481779`).

Os valores moram nos assets em `Assets/Application/Settings/`. Ao alterar um
asset, atualize a seção [Valores atuais dos assets](#valores-atuais-dos-assets).
Os valores iniciais das perguntas em aberto estão em [[valores_padrao_em_aberto]].

Ver também: [[GDD]], [[estados_e_atributos]], [[classes_e_skills]],
[[inimigos]], [[grid_e_terreno]], [[consumiveis]], [[xp_e_niveis]],
[[talentos_e_oferta]], [[balanceamento_e_geracao]], [[mapa_e_nos]],
[[interface]].

## Índice

1. [Visão geral](#visão-geral)
2. [Grid e células](#grid-e-células)
3. [Regras centrais (GridRules)](#regras-centrais-gridrules)
4. [Configuração de personagens](#configuração-de-personagens)
5. [Atributos e estados](#atributos-e-estados)
6. [Cálculo de dano](#cálculo-de-dano)
7. [Skills](#skills)
   - [Golpes anunciados (telegrafia)](#golpes-anunciados-telegrafia)
   - [Melhorias de skill (variantes)](#melhorias-de-skill-variantes)
8. [Empurrar e puxar (deslocamento)](#empurrar-e-puxar-deslocamento)
9. [Consumíveis em batalha](#consumíveis-em-batalha)
10. [Ações do jogador e entrada](#ações-do-jogador-e-entrada)
11. [Previsão do resultado](#previsão-do-resultado)
12. [Sistema de turnos](#sistema-de-turnos)
13. [IA dos inimigos](#ia-dos-inimigos)
14. [Terreno](#terreno)
15. [Morte e fim da batalha](#morte-e-fim-da-batalha)
16. [XP, nível e talentos](#xp-nível-e-talentos)
17. [Highlights visuais](#highlights-visuais)
18. [Pontos de extensão](#pontos-de-extensão)
19. [Valores atuais dos assets](#valores-atuais-dos-assets)

---

## Visão geral

O combate acontece em um **grid 2D de células** (6×6 por padrão). Cada célula
contém no máximo um entity (`GridEntity` → `Character` → `PlayerCharacter` ou
`Enemy`) e pode ter um terreno. Uma batalha é um nó do mapa da run (ver
[[mapa_e_nos]]): vence-se eliminando todos os inimigos, sem limite de turnos, e
perde-se ao morrer.

O jogo é **por turnos**. Em cada **turno global** o jogador joga primeiro e
depois cada inimigo joga o seu, **um de cada vez, numa ordem fixa** mostrada por
um número sobre a cabeça de cada um (`ActingOrder`). Cada entidade tem o seu
próprio turno dentro do turno global.

**Vez do jogador.** O jogador **anda uma vez** (até 2 casas) e **age uma vez**
(usa uma skill), na ordem que quiser, e ainda pode usar **um consumível**. Não
há ataque básico: as skills são a única ação (`CharacterConfig.hasBasicAttack`
desligado em todos os personagens). Toda skill mirada é usada em dois toques:
o primeiro **mira** e mostra a previsão (dano, mortes, empurrões, para onde vão
os golpes anunciados), o segundo (ou o botão **Confirmar**) executa. A vez
**só acaba sozinha quando nada mais é possível** (andar, agir e consumível já
usados ou impossíveis); senão acaba no botão **Encerrar** (sempre visível na vez
do jogador) ou tocando no próprio personagem, que também serve para esperar.

| Ação do jogador | No turno |
|---|---|
| Andar | usa o andar do turno (pode ser desfeito enquanto o agir não foi usado) |
| Trocar de lugar com um vizinho (talento Passo Trocado) | usa o andar; não pode ser desfeito |
| Usar skill (mirar e confirmar) | usa o agir do turno |
| Usar skill canalizada (Meteoro, Tempestade Elétrica e variantes) | usa o agir **e** o andar; só antes de andar, e não se anda depois (ver [Uso](#uso)) |
| Usar consumível | não usa o andar nem o agir; no máximo um por turno |
| Tocar num inimigo | mostra ou esconde até onde ele pode andar a partir de onde está; não gasta nada |
| Encerrar / tocar em si mesmo | encerra a vez |

Uma ação inválida não gasta nada.

**Vez de cada inimigo** (decisão de 2026-10-19):

1. **dispara** o golpe que preparou no turno anterior, nas células marcadas,
   contra quem estiver lá agora (inclusive outros inimigos); disparar não gasta
   nada (`CombatSettings.firingUsesAction` desligado);
2. **anda** (ação de aproximação da IA);
3. **prepara** o próximo golpe (usa o agir): as células que ele vai atingir
   ficam marcadas no grid até o próximo turno dele. Todo inimigo tem um golpe
   **básico sem recarga** e um **especial com recarga** (decisão de 2026-10-20),
   então prepara um golpe em todo turno em que algum deles alcança o jogador. As
   ações de apoio (curar, proteger, enfraquecer, invocar) continuam **imediatas**
   e também usam o agir.

O turno do jogador vira um quebra-cabeça: sair da área marcada, empurrar ou
puxar o atacante (a área acompanha quem prepara), empurrar outro inimigo para
dentro da área, matar quem prepara ou atordoá-lo, congelá-lo ou cegá-lo (o que
cancela o golpe). Veja [Golpes anunciados](#golpes-anunciados-telegrafia).

**Números pequenos.** Vida do jogador 14 a 18 no início (mais 1 por nível),
inimigos de 2 a 8 (o chefe 22), dano de skill de 1 a 3, colisão 1. A armadura
(Blindado) tira 1 de cada golpe e pode zerar um golpe de 1
(`CombatSettings.minimumDamage` = 0); colisões ignoram a armadura.

> [!note] Simulador depois do ajuste de 2026-10-19
> Rótulo "novo-molde-inicio-chefe", 20 runs por classe, seeds 1000 a 1019:
> vitórias de 60% (Guerreiro), 20% (Mago) e 55% (Ladino); 73% das derrotas antes
> do andar 10; o chefe foi vencido em 100% das vezes em que foi alcançado. Dano
> recebido por batalha, em % da vida máxima: 21% (andares 1 a 4), 16% (5 a 9),
> 10% a 15% (10 a 29) e 27% (chefe). Os números são só referência: o
> balanceamento está pausado.

> [!important]
> Jogador e inimigos seguem **exatamente as mesmas regras**: alcance,
> ocupação, dano, estados, skills e terreno passam pelas mesmas classes de
> regra (`GridRules`, `CombatResolver`, `StateContainer`, `SkillTargeting`).
> Nenhuma checagem é duplicada nos controllers.

> [!important]
> **Lógica não vive em controllers.** Os controllers (`PlayerCharacterController`,
> `EnemyController`, base `CharacterControllerBase<T>`) são apenas
> **roteadores**: leem o input (ou a IA configurada), perguntam ao
> character/grid se a ação é válida e executam via `Character` e
> `GridController`. Atributos, skills, estados, IA, terreno e consumíveis são
> **dados** (ScriptableObjects).

> [!important]
> **Lógica imediata, visual atrasado.** Ocupação das células, posição lógica,
> dano e morte mudam na hora; só o transform, os flashes e os efeitos animam.
> Regras, IA e previsão nunca dependem da posição visual.

Quem faz o quê:

| Peça | Responsabilidade |
|---|---|
| `GridController` | tabuleiro, spawn, ocupação, terreno (inclusive temporário), movimento, troca de lugar, captura e restauração da batalha |
| `GridRules` | cálculos entre characters e grid (alcance, passo, highlights, alcance de um inimigo no próximo turno) |
| `CombatResolver` / `DamageCalculator` | dano, cura, escudo, crítico, defesa, roubo de vida e espinhos; previsão sem sorteio (`PredictDamage`) |
| `CharacterStats` / `StateContainer` | atributos em jogo e estados ativos de um personagem |
| `SkillDefinition` / `EffectiveSkill` / `SkillTargeting` / `SkillArea` / `SkillCooldowns` | skills: dados, valores efetivos, mira, áreas e recargas |
| `SkillTelegraph` (em `Enemy`) | golpe anunciado: células marcadas, turnos até o disparo |
| `Displacement` / `DisplacementResolver` | empurrões e puxões (regras puras e aplicação) |
| `ConsumableRules` / `ConsumableExecutor` / `ConsumableInventory` | consumíveis: uso, mira e inventário |
| `TurnManager` / `ActingOrder` / `BattleController` / `TurnBlockers` | fluxo de turnos, ordem fixa dos inimigos, fim da batalha e pausas |
| `ActionForecast` / `ThreatForecast` / `ForecastView` | previsão da ação mirada e dos golpes anunciados, desenhada no tabuleiro |
| `EnemyBehavior` / `EnemyAction` / `EnemyMemory` | IA dos inimigos |
| `TerrainEffects` / `TerrainGenerator` | efeitos e geração do terreno |
| `XpRewardSystem` / `ProgressionSettings` / `TalentSession` | XP, nível e escolha de talentos |

## Grid e células

O `GridController` é dono do estado: matriz `Cell[,]`, tamanho (`Size`) e as
formas de montar o tabuleiro:

- **Batalha de uma run** (`InitializeBattle(spec, playerConfig, playerState)`):
  células do tamanho do `BattleSpec` (6×6 em `BattleGenerationSettings`), terreno
  do spec, jogador na célula do spec ((2, 2) por padrão) com o estado da run
  (nível, XP, skills, consumíveis, estados, vida e o bônus de vida máxima que
  persistem) e os inimigos do spec com a escala e o XP de cada um. O
  `RunManager` gera o spec (ver [[balanceamento_e_geracao]]).
- **Retomada** (`RestoreBattle(snapshot, ...)`): reconstrói exatamente a batalha
  salva (posições, vida, estados com os turnos restantes, recargas, memória da
  IA, invocados, **golpes preparados**, **ordem de ação** de cada inimigo,
  **terreno temporário** com as rodadas restantes e se um consumível já foi
  usado no turno).
- **Editor** (`InitializeGrid(playerConfig)`, no `Awake` em edit mode e no botão
  **Reset grid**): jogador em (2, 2) a partir do `debugPlayerConfig`, inimigos do
  `EncounterConfig` (`DefaultEncounter`: Morcego-olho, Crânio de fogo, Goblin,
  Rato e Slime) cada um na primeira célula livre da varredura, e a lista
  `debugTerrain`. Serve só para testar; as runs nunca usam isso.
- `ClearBoard()` limpa tudo (o mapa é mostrado sobre um mundo vazio).

A criação de personagens é centralizada em `CharacterFactory.Spawn(config
[, escala])`; inimigos em runtime também nascem por `GridController.SpawnEnemy`
(célula válida, caminhável e livre; usado pelo gerador de batalhas e pela
invocação). Cada inimigo que nasce recebe o seu **lugar na ordem de ação**
(`Enemy.ActOrder`): primeiro os da lista gerada, na ordem da lista, depois os
invocados, na ordem em que chegam (ver [Sistema de turnos](#sistema-de-turnos)).

Cada `Cell` tem:

- `GridPosition`: coordenada lógica;
- `HasContent` / `GetContent()` / `SetContent()` / `RemoveContent()`: o entity que
  a ocupa;
- `Terrain` e `IsBlocked`: o terreno e se ele bloqueia a célula;
  `TerrainTurnsLeft`: rodadas restantes de um terreno temporário (0 = permanente);
- highlight visual (`Highlighted` + `HighlightType`), flash de área e pulsos
  (chegada, rejeição, terreno), apenas visuais;
- marca de golpe anunciado (`Warned`, e `WarnHit` quando um personagem que o
  golpe afeta está nela) e o dano previsto piscando na barra de vida, desenhados
  pela previsão ([Previsão do resultado](#previsão-do-resultado)); só visuais;
- entrada: clique/toque (`CellTapEvent`), toque longo e clique direito
  (`CellLongPressEvent`).

O `GridController` expõe consultas e execuções de estado:

| Membro | O que faz |
|---|---|
| `IsValidPosition(pos)` | dentro dos limites |
| `IsFreePosition(pos)` | sem conteúdo e sem obstáculo |
| `IsWalkable(pos)` | dentro dos limites e sem obstáculo |
| `GetContent(pos)` / `GetCell(pos)` / `GetTerrain(pos)` | entity, célula ou terreno da posição |
| `HasBlockedCells`, `BlocksSkillArea(pos)` | há obstáculos; o terreno da célula bloqueia áreas de skill |
| `WalkEntity(personagem, pos)` | **execução centralizada do andar** (jogador e inimigos): segue o caminho de `GridRules.FindWalkPath` célula a célula, move o conteúdo na hora, atualiza `CurrentGridPos`, marca `MovedThisTurn`, levanta `EntityPassedCellEvent` para cada célula do caminho (menos o destino) e depois `EntityEnteredCellEvent` no destino, e anima um pulo por célula |
| `MoveEntity(entity, pos)` | movimento direto, sem caminho (o Teleporte); também marca `MovedThisTurn` e levanta `EntityEnteredCellEvent` |
| `UndoMove(...)` | desfaz o andar do jogador (sem `EntityEnteredCellEvent`), voltando pelo caminho |
| `DisplaceEntity(...)` | movimento forçado dos empurrões e puxões (desliza, não conta como andar) |
| `SwapCharacters(a, b, fonte)` | troca dois personagens de lugar: os dois pulam, as duas células reagem como ao entrar (`EntityEnteredCellEvent`) e sai `CharactersSwappedEvent`; não conta como andar |
| `SetTerrain(pos, terreno[, rodadas])` / `ApplyTerrain` / `ClearTerrain` | define o terreno (com rodadas > 0, temporário); recusa terreno que bloqueia movimento sobre célula ocupada |
| `TickTemporaryTerrain()` | passou uma rodada: o terreno temporário perde 1 e o que chega a 0 volta a ser chão |
| `ProbePosition(personagem, pos)` | consulta "e se estivesse ali" sem mover nada (IA, bot, alcance de um inimigo) |
| `CaptureBattle()` | foto exata da batalha (salva no início de cada vez do jogador) |
| `WaitForMovementsAsync`, `AnimationSpeed` | espera as animações e as acelera |

O `GridController` também adiciona a `ForecastView` (só em Play Mode), que desenha
a previsão no tabuleiro.

> [!note]
> No editor (fora do Play Mode), o `CharacterFactory` instancia os entities
> **linkados ao prefab-template** (`PrefabUtility.InstantiatePrefab`) e
> registra o config aplicado como override, então os clones na cena refletem
> mudanças no template. Em runtime, usa `Instantiate` normal.

> [!note]
> **Distâncias.** A métrica de cada alcance é configurável em `CombatSettings`
> (euclidiana, Manhattan ou Chebyshev), sem mudar código. Hoje o alcance de
> **andar** e o de **ataque** (que a IA usa para medir a distância preferida)
> são **euclidianos**; o de **skills e consumíveis** é **Manhattan**; a
> **adjacência** das passivas é **Chebyshev** (8 vizinhos). Com andar 2 o
> personagem alcança os 4 vizinhos ortogonais, as 4 diagonais adjacentes (≈ 1,41)
> e as células a 2 passos em linha reta, mas não a posição (2, 1) (≈ 2,24).

## Regras centrais (GridRules)

`Gameplay/Rules/GridRules.cs` é o **único lugar** com cálculos entre
characters e grid: classe estática, funções puras. As distâncias vêm de
`GridDistance` e da `CombatSettings`.

| Função | Responsabilidade |
|---|---|
| `IsInWalkRange(from, to, walkDistance)` | alcance de movimento (métrica de movimento) |
| `IsInAttackRange(from, to, attackDistance)` | alcance do ataque básico (métrica de ataque) |
| `CanWalkTo(grid, character, target)` | posição válida, caminhável, livre, em alcance e alcançável por um caminho que não cruza obstáculos nem personagens |
| `FindWalkPath(grid, from, to, walkDistance)` | o caminho do andar: as células cruzadas em ordem, sem a de partida e terminando no destino (o mais curto; empate pela ordem baixo, esquerda, direita, cima), ou nulo |
| `HasWalkPath(grid, from, to, walkDistance)` | existe `FindWalkPath`: caminho por células ortogonais livres (sem terreno que bloqueia e sem personagem, jogador ou inimigo), todas dentro do alcance de `from`; não dá para passar na diagonal entre dois obstáculos (desde 2026-10-20; antes, personagens não bloqueavam o caminho, só o destino) |
| `IsAttackTarget(grid, attacker, target)` | alvo válido do ataque básico: exige `CanBasicAttack` (ter ataque básico e nenhum estado que o impeça); **hoje sempre falso**, porque ninguém tem ataque básico |
| `FindStepToward(grid, character, target)` | passo guloso de um turno (usado pela ação `ChaseTarget`, que nenhum comportamento usa hoje) |
| `CountAdjacentOpponents` / `HasAdjacentAlly` | adjacência das passivas, pela métrica `adjacencyMetric` |
| `AreAllies(a, b)` | jogador × inimigos: os inimigos são aliados entre si |
| `GetHighlightInfos(grid, character, canMove, canAct)` | highlights Walk/Attack; sem andar ou sem agir no turno, a parte usada fica de fora (sem ataque básico, só Walk) |
| `GetWalkCells(grid, character)` | células para onde o personagem pode andar a partir de onde está (`CanWalkTo` dentro de `WalkDistance`); é o destaque do inimigo tocado |
| `GetReachCells(grid, character)` | células que o personagem pode golpear no próximo turno, andando antes; sem ataque básico é `GetSkillReachCells` (sem uso no jogo desde 2026-10-20) |
| `GetSkillReachCells(grid, character)` | células cobertas pela área de alguma skill pronta, mirada de qualquer célula para onde ele pode andar (e da própria), consultadas com `ProbePosition` |
| `GetSkillHighlightInfos(grid, caster, skill)` | highlights `SkillRange` das células onde a skill pode ser mirada |

Regras de negócio derivadas:

- **Um character nunca anda para uma célula ocupada ou bloqueada.**
- **Ninguém se atinge com a própria skill**: as skills do jogador afetam só
  inimigos (`ESkillTargetFilter.Enemies`) e as dos inimigos afetam **todos menos
  quem lança** (`EveryoneButCaster`), então um golpe de inimigo acerta outros
  inimigos que estiverem na área.
- O passo guloso mede a distância ao alvo em linha reta. Só quando há
  obstáculos ele passa a seguir a menor rota (vizinhança ortogonal) ao redor
  deles.

## Configuração de personagens

Cada character é definido por um **objeto de configuração** (ScriptableObject,
um `DisplayableDefinition` com `id`, nome e descrição localizados e ícone) que
dita **tudo** o que o diferencia. Não existe prefab por personagem: todos
compartilham um **prefab-template** e recebem o config no spawn
(`CharacterFactory`).

```
CharacterConfig (SO, abstrato)
├── visual: sprite, animatorController, tint
├── atributos base: maxHp, walkDistance
├── ataque básico: hasBasicAttack, attackDistance, basicAttackDamage   ← desligado em todos
├── ofensivo: critChance, critMultiplier, defensePenetration
├── defensivo: defense
├── modificadores de skill: skillDamageBonus, skillRange, cooldownReduction
├── deslocamento e imunidades: canBeDisplaced, immuneStates, maxHpPercentDamageMultiplier
├── skills: List<SkillDefinition>        ← skills iniciais
└── initialStates: List<StateGrant>      ← estados aplicados no spawn

PlayerCharacterConfig : CharacterConfig    (Create > GridBattle > Characters)
├── prefab: PlayerCharacter               ← template compartilhado
├── characterClass: ECharacter            ← Warrior / Mage / Rogue
├── battlesToUnlock                        ← batalhas vencidas (no perfil) para liberar
└── talentPool: List<TalentDefinition>    ← talentos próprios da classe

EnemyConfig : CharacterConfig              (Create > GridBattle > Characters)
├── prefab: Enemy                         ← template compartilhado
├── role: EnemyRoleDefinition             ← papel (ícone, nome, fator de ameaça)
├── isBoss                                 ← chefe do nó final
├── isUnique                               ← elite: nunca sorteado como inimigo comum
├── threat                                 ← ameaça escrita à mão (0 = calculada)
├── behavior: EnemyBehavior               ← IA (ver "IA dos inimigos")
├── behaviorDescription                    ← texto localizado do comportamento
└── xpReward                               ← só vale com a fonte de XP EnemyReward (não é a atual)
```

A curva de XP, o nível máximo e a vida por nível **não** ficam nos configs de
classe: são globais (`ProgressionSettings`; ver [XP, nível e talentos](#xp-nível-e-talentos)).

Prefabs-template em `Assets/Application/Prefabs/Entities/`:

```
Character.prefab          SpriteRenderer + Animator + CharacterView (base)
├── Enemy.prefab          (variant) + Enemy + EnemyController + EnemyIconsView
└── PlayerCharacter.prefab (variant) + PlayerCharacter + PlayerCharacterController
```

- `Character.Initialize(config, escala)` aplica o config: atributos base (com a
  escala), estados iniciais, vida cheia, nome do GameObject, lista de skills
  iniciais, recargas zeradas e visual (`CharacterView.Apply` define sprite,
  **tint** e `RuntimeAnimatorController`). O `Awake` continua inicializando a
  vida de characters colocados à mão na cena com um config atribuído.
- **`PlayerCharacter`** tem o que `Enemy` não tem: `PlayerConfig`, `Class`
  (somente jogadores têm classe), `Level` / `CurrentXp`, o bônus de vida máxima
  (`MaxHpBonus` = vida por nível + `RunMaxHpBonus`, o bônus ou custo que dura na
  run), o inventário de consumíveis e o contador de consumíveis usados no turno.
  Nível, XP e bônus vêm do estado da run no início de cada batalha.
- **`Enemy`** expõe `EnemyConfig`, `Role`, `XpReward` (o valor que o gerador de
  batalhas definiu; 0 em invocados), `IsSummoned`, `Summoner`, `ActOrder` (lugar
  na ordem de ação) e `Telegraph` (o golpe que está preparando, ou nulo).
- A classe escolhida no menu é resolvida pelo `GameStateManager`
  (`playableCharacters` e `CharacterClass`).
- `EnemyIconsView` mostra no inimigo, dentro da própria célula (nada cobre a
  barra de vida do vizinho): o **número da ordem de ação** num pequeno selo escuro
  no canto de cima à esquerda, o ícone do papel no canto de cima à direita e, abaixo
  dele, até 2 ícones de estados permanentes (blindado, regenerante, espinhoso:
  Mecânica 6). Enquanto o jogador toca nele (`SetFocused`), aparece sobre a cabeça,
  num selo, o **ícone do golpe que prepara**, com os turnos até o disparo só quando
  passam de 1 (decisão de 2026-10-20: menos informação em pouco espaço). Os ícones
  são nativos, ampliados só por fator inteiro (pixel perfect).

### Escala por profundidade

Os inimigos de uma batalha podem nascer com um `CharacterScaling` (multiplicador
de vida e de dano) que multiplica a **vida máxima base**, o **dano básico base**
e o **dano das skills** do personagem. Não é um estado: não aparece nem pode ser
removido. Fórmula linear, por profundidade `p`:
`multiplicador = 1 + taxa × (p − 1)` (há opção de crescimento composto).
Desde 2026-10-20 as taxas são **0,025 de vida e 0,035 de dano por andar**
(`BattleGenerationSettings.hpPerDepth` e `damagePerDepth`), na profundidade de
balanceamento (pedido do Luiz de 2026-10-20: o combate estava fácil demais; isso
deixa de lado o princípio de 2026-10-19 de não escalar os números; o Luiz confirmou que a escala fica, com os valores em teste). No
andar 30 a vida é ×1,73 e o dano ×2,02. Como os números são pequenos e
arredondados (`CombatSettings.RoundValue`), um golpe de 1 passa a dar 2 a partir
do andar 16 e um de 2 passa a dar 3 a partir do andar 9. De 2026-10-19 a
2026-10-20 as duas taxas foram 0. Invocados herdam a escala de quem os invocou.

### Criando um personagem novo

- **Inimigo:** `Create > GridBattle > Characters > Enemy Config`; preencher
  sprite, animator, atributos, `prefab = Enemy.prefab`, `role`, `behavior`,
  `behaviorDescription`, `threat`, as skills (golpes anunciados, ver
  [Golpes anunciados](#golpes-anunciados-telegrafia)) e, se for o caso,
  `initialStates`. Deixe `hasBasicAttack` desligado. Para aparecer nas batalhas,
  entra no pool de `BattleGenerationSettings` (com profundidade mínima/máxima e
  peso); um elite tem `isUnique` ligado. Nenhum prefab novo.
- **Personagem jogável:** `Create > GridBattle > Characters > Player Character
  Config`; `prefab = PlayerCharacter.prefab`, `characterClass`, `battlesToUnlock`,
  a skill inicial e `talentPool` (com um talento para cada skill liberada); adicionar à lista `playableCharacters` do
  `GameStateManager`.
- Só crie um prefab próprio (variant do template) se o personagem precisar de
  componentes extras.

> [!note]
> `ECharacter` vive em `GridBattle.Gameplay.Entities` (domínio de gameplay).
> A UI apenas referencia: o enum não pertence à camada de UI.

## Atributos e estados

Duas camadas descrevem e alteram um personagem (ver [[estados_e_atributos]]):
os **atributos** (valores base do config) e os **estados** (efeitos com nome e
duração). O valor em jogo é o base somado aos estados ativos.

### Atributos

`EAttribute` e `CharacterStats`. Frações são guardadas de 0 a 1 (0,1 = 10%).

| Atributo | Descrição | Base no config |
|---|---|---|
| Vida máxima (`MaxHp`) | quantidade máxima de vida (inteiro) | `maxHp` × escala; no jogador, mais 1 por nível acima do 1 e o bônus ou custo da run |
| Alcance de movimento (`WalkRange`) | distância que anda no andar do turno (inteiro); vale 0 enquanto um estado impede o movimento | `walkDistance` |
| Alcance de ataque (`AttackRange`) | distância do ataque básico (inteiro); só conta com ataque básico | `attackDistance` |
| Dano básico (`BasicDamage`) | dano do ataque básico (inteiro); só conta com ataque básico | `basicAttackDamage` × escala |
| Chance de crítico (`CritChance`) | probabilidade (0 a 1) de um golpe ser crítico | `critChance` |
| Multiplicador de crítico (`CritMultiplier`) | quanto o crítico multiplica o dano (mínimo 1) | `critMultiplier` |
| Penetração de defesa (`DefensePenetration`) | fração (0 a 1) da defesa do alvo que é ignorada | `defensePenetration` |
| Defesa (`Defense`) | reduz o dano recebido, fixa ou em porcentagem (inteiro) | `defense` |
| Bônus de dano de skills (`SkillDamageBonus`) | fração extra de dano das skills | `skillDamageBonus` |
| Alcance de skills (`SkillRange`) | soma à distância máxima de mira das skills de alcance modificável (inteiro) | `skillRange` |
| Redução de cooldown (`CooldownReduction`) | turnos tirados da recarga das skills (inteiro) | `cooldownReduction` |
| Dano causado (`DamageDealt`) | ajuste (fração) de todo o dano causado: −0,5 = causa 50% menos | só por estados (base 0) |
| Dano recebido (`DamageTaken`) | ajuste (fração) de todo o dano recebido: 0,2 = recebe 20% mais | só por estados (base 0) |

Fórmula: `valor = (base + Σ modificadores fixos) × (1 + Σ modificadores
percentuais)`. Nenhum atributo fica negativo, exceto `DamageDealt` e
`DamageTaken`. Os modificadores de um estado escalam com as suas pilhas. Quando um
estado muda a vida máxima, a vida atual nunca fica acima do novo máximo.

Hoje todos os personagens têm crítico 0, defesa base 0 e modificadores de skill
0: a defesa vem só do estado Blindado e o alcance de skills só do talento Longo
Alcance. Esquiva e tamanho da área das skills continuam fora do conjunto
([[estados_e_atributos]]).

### Estados

Um `StateDefinition` (asset, `Create > GridBattle > States > State Definition`)
tem um **nome**, um tipo (`Buff`, `Debuff` ou `Neutral`), uma **política de
reaplicação**, o máximo de pilhas, se aparece nos detalhes e uma lista de
**efeitos** escolhidos no Inspector (`[SerializeReference]`). A **duração não
faz parte do estado**: quem o aplica decide (talento, skill, terreno, consumível
ou inimigo), com um `StateGrant` (estado + permanente ou N turnos do portador +
pilhas). Cada estado ativo é um `StateInstance` (turnos restantes, pilhas, fonte
e, para escudos, os pontos restantes). Um personagem ignora os estados da sua
lista `immuneStates` (`StateContainer.Apply` devolve nulo).

| Efeito | O que faz |
|---|---|
| `AttributeModifierEffect` | altera atributos (valor fixo ou percentual); escala com as pilhas |
| `PeriodicDamageEffect` | dano fixo (+ fração da vida máxima) no início ou no fim de cada turno do portador |
| `PeriodicHealEffect` | cura fixa (+ fração da vida máxima) no início ou no fim de cada turno do portador |
| `ThornsEffect` | ao ser atingido por ataque, skill ou consumível, devolve dano fixo (+ fração do dano recebido) ao atacante |
| `LifeStealEffect` | ao causar dano com ataque, skill ou consumível, recupera uma fração dele como vida |
| `ShieldEffect` | vida temporária (valor fixo + fração da vida máxima) que absorve dano antes da vida real; o estado some quando ela esgota (configurável) |
| `BehaviorRestrictionEffect` | impede andar, o ataque básico e/ou usar skills (Exausto, Cego, Congelado e Atordoado); impedir skills **cancela o golpe preparado** de um inimigo |
| `RunModifierEffect` | altera as opções de talento, rerolls, banimentos ou pulos da run (só faz sentido no jogador) |
| `SkillModifierEffect` | muda os números das skills do portador (ver [Valores efetivos](#valores-efetivos-e-modificadores-effectiveskill)); nenhum asset atual usa |
| `ConditionalDamageEffect`, `OnKillEffect`, `ApplyStateOnHitEffect` | passivas (ver [Bônus condicionais](#bônus-condicionais-dos-estados-passivas)) |
| `DominoEffect`, `DisplacementTrailEffect`, `SlipperyPushEffect`, `CounterstrikeEffect`, `SwapStepEffect` | talentos de regra (ver [Talentos de regra](#talentos-de-regra)) |

Os efeitos têm ganchos opcionais: aplicar, reaplicar, remover, início do turno,
fim do turno, causar dano, receber dano e absorver dano.

> [!important]
> Os estados valem para **jogador e inimigos**, com o mesmo modelo. Inimigos
> podem nascer com estados permanentes (`initialStates`), que é como se
> constroem os inimigos que cobram a build (Mecânica 6): Blindado, Regeneração e
> Armadura de Espinhos.

#### Duração

A duração conta em **turnos da entidade portadora** (regra do GDD 3.1), seja
buff ou debuff, e não importa quem aplicou. No código:

- no **início** do turno da portadora (`Character.BeginTurn`), os efeitos de
  início de turno rodam (veneno, queimadura e regeneração, por exemplo);
- no **fim** do turno da portadora (`EntityTurnEndedEvent` e depois
  `Character.EndTurn`), as recargas das skills diminuem, os efeitos de fim de
  turno rodam e **por último** as durações diminuem em 1; o estado termina ao
  chegar a zero. Estados permanentes nunca diminuem;
- um estado aplicado **antes** de a portadora jogar no turno global atual conta
  esse turno como o primeiro; aplicado **depois**, o primeiro é o turno seguinte
  dela. Exemplo: o jogador congela um inimigo (Congelado, 1 turno) na sua vez; o
  próximo turno do inimigo é o primeiro, ele não anda nem usa skills, e o estado
  some no fim desse turno. O Exposto de 2 turnos do Contragolpe, aplicado no
  turno do próprio inimigo, dura até o fim do turno seguinte dele;
- os estados que o terreno aplica no fim do turno são aplicados **antes** do
  desconto, então o turno corrente conta: o Envenenado de 2 turnos do pântano
  perde 1 no mesmo instante e causa dano uma vez, no início do turno seguinte
  (a menos que o portador continue no pântano, que o renova);
- um estado que a skill aplica **no próprio lançador** (alvo `Caster` do
  `ApplyStatesSkillEffect`) não ganha o bônus de duração dos talentos
  (`StateDurationBonus`; desde 2026-10-20). Exemplo: o Meteoro deixa o Mago
  Exausto por 2: o turno em que lançou conta como o primeiro (2 → 1 no fim
  dele), o turno seguinte é o segundo (o Mago não anda) e o estado some no fim
  dele.

#### Reaplicação e acúmulo

Cada estado define a sua política (`EStackPolicy`). A padrão é renovar a
duração.

| Política | Reaplicar um estado que já está ativo |
|---|---|
| `RefreshDuration` (padrão) | mantém uma instância; a duração passa a ser a maior das duas (permanente vence) |
| `AddDuration` | mantém uma instância; as durações se somam |
| `AddStacks` | soma pilhas (até o máximo do estado) e renova a duração; os efeitos escalam com as pilhas |
| `IgnoreIfPresent` | a nova aplicação é ignorada |

Instâncias com **fontes diferentes** ficam separadas (por exemplo, uma por
talento: `sourceId` é o id do talento). Estados do terreno, de skills e da IA não
têm fonte.

#### Remoção

- por duração (acima) ou por efeito (escudo esgotado);
- `Remove`, `RemoveBySource` (os estados de um talento), `RemoveTemporary` e
  `Clear` no `StateContainer`;
- **ao mudar de batalha**, os estados com duração do jogador são removidos e os
  permanentes continuam (`RunSettings.temporaryStates`); os estados do config e os
  de talentos são reaplicados em cada batalha, e não viajam como estados comuns.

#### Catálogo atual (`Settings/States`)

| Estado | Tipo | Efeito | Onde aparece |
|---|---|---|---|
| Blindado | buff | defesa +1 (tira 1 de cada golpe; colisões ignoram) | Goblin blindado, Golem de pedra e Guarda Goblin (permanente) |
| Cego | debuff | não usa skills; cancela o golpe preparado | Bomba de Fumaça e Fumaça Arremessada (1 turno), Fumaça Densa (2 turnos), Olhar Ofuscante (1 turno) |
| Exausto | debuff | não anda (`PreventMovement`) | Meteoro e variantes, no próprio Mago (2 turnos: o seguinte ao lançamento) |
| Queimando | debuff | 1 de dano no início de cada turno do portador | Seta Flamejante, Punho de Magma e Explosão de Chamas (2 turnos), Chuva de fogo (1 turno); o Golem de lava é imune |
| Gelado | debuff | anda 1 casa a menos | Muralha de Gelo e variantes (2 turnos) |
| Exposto | debuff | leva +1 de dano de cada golpe, depois da defesa (skills, consumíveis, ataques e colisões) | Adaga de Arremesso e variantes (2 turnos), Olhar Maldito e Cuspe Ácido (2 turnos), Contragolpe (2 turnos) |
| Congelado | debuff | não anda nem usa skills; cancela o golpe preparado | Nova de Gelo, Prisão de Gelo e Congelamento Profundo (1 turno) |
| Roubo de Vida | buff | recupera 15% do dano causado | nenhum asset o aplica |
| Envenenado | debuff | 1 de dano no início de cada turno do portador | Adaga Envenenada, Adaga Perfurante, Passo Venenoso, Linha Venenosa e variantes, Lâminas Envenenadas, Mordida Pestilenta e Pântano Venenoso (2 turnos) |
| Regeneração | buff | recupera 1 de vida no início de cada turno do portador | Slime regenerante (permanente), Santuário de Cura e Tônico de Regeneração (3 turnos) |
| Escudo | buff | absorve o próximo 1 de dano; some ao esgotar | Xamã goblin (2 turnos), Pedra Protetora (2 turnos) |
| Atordoado | debuff | não anda, não usa skills nem o ataque básico; cancela o golpe preparado | Golpe de Escudo e variantes, Pisão Trovejante, Arremesso Atordoante, Tempestade Elétrica e variantes, Gancho Atordoante (1 turno); o Rei Goblin é imune |
| Armadura de Espinhos | buff | devolve 1 de dano a quem o atinge (colisões não disparam espinhos) | Rato espinhoso e Golem de lava (permanente) |
| Veneno letal | debuff | 1 de dano mais 5% da vida máxima no início do turno e −6% de dano causado, por pilha (até 5) | nenhum asset o aplica (sobrou do G3) |
| Fraquejado | debuff | causa 1 de dano a menos em cada golpe (antes da defesa; inclui colisões que causa) | Olho maldito (ação Enfraquecer, 1 turno), Adaga Debilitante (1 turno) |
| Enredado | debuff | anda 2 casas a menos | Teia (1 turno) |

Todos usam a política `RefreshDuration`, exceto Escudo (`IgnoreIfPresent`) e
Veneno letal (`AddStacks`). Os 19 estados de talentos (`Settings/States/Talents`,
um por talento que concede estado) estão em [Talentos](#talentos).

## Cálculo de dano

Todo dano e toda cura passam pelo `CombatResolver`: as skills, os consumíveis,
os estados, o terreno e as colisões (e o ataque básico, que hoje ninguém tem). O
cálculo em si (`DamageCalculator`) é puro e sem efeitos colaterais. O dano tem um
**tipo** (`EDamageKind`):

| Tipo | Origem | Pode ser crítico | Defesa | Dispara espinhos e roubo de vida |
|---|---|---|---|---|
| `BasicAttack` | ataque básico (sem uso hoje) | sim | sim | sim |
| `Skill` | skill (dano da skill escalado pelo atacante) | sim | sim | sim |
| `Consumable` | consumível (bomba) | sim | sim | sim |
| `Periodic` | dano ao longo do tempo (veneno, queimadura) | não | ignora (configurável) | não |
| `Thorns` | espinhos | não | ignora (configurável) | não |
| `Terrain` | célula de perigo | não | ignora (configurável) | não |
| `Collision` | empurrado ou puxado contra obstáculo, borda ou personagem | não | ignora (configurável) | não |
| `Pure` | dano exato de efeitos roteirizados | não | não | não (nenhum modificador vale) |

Ordem do cálculo (dano base ≤ 0 resulta em 0):

0. **Bônus condicionais dos estados** (ver [abaixo](#bônus-condicionais-dos-estados-passivas)):
   antes do cálculo, o `CombatResolver` pergunta aos estados do atacante e do
   alvo (`CombatResolver.Adjust`) e passa o resultado (`DamageAdjustment`) ao
   `DamageCalculator`, que continua puro.
1. **Fixo de quem causa:** `dano + bônus fixo causado` (mínimo 0). É aqui que
   entram o Fraquejado (−1) e os talentos de +1 (Foco Arcano, Quebradiço,
   Predador, Assassino, Estilhaçar).
2. **Skill:** `× (1 + bônus de dano de skills)` do atacante; depois
   `× (1 + Dano causado)` (mínimo 0) e `× (1 + bônus percentual condicional)`.
3. **Crítico** (ataque básico, skill e consumível): a chance é a do atacante,
   limitada pelo teto (`critChanceCap`). O sorteio só acontece se a chance for
   maior que 0 e usa o fluxo `Combat` da seed: personagens sem crítico (todos,
   hoje) não consomem sorteios. Em crítico, `× max(1, multiplicador de crítico)`.
4. **Defesa** (exceto `Periodic`, `Thorns`, `Terrain` e `Collision`, configurável): a
   penetração do atacante (fração) reduz a defesa efetiva,
   `defesa efetiva = defesa × (1 − penetração)`.
   - **Fixa** (padrão): `dano − defesa efetiva`.
   - **Percentual:** `dano × (1 − min(defesa efetiva / 100, teto))`, com teto de
     80%.
5. `× (1 + Dano recebido)` do alvo (mínimo 0), depois `+ fixo recebido`
   (mínimo 0; o Exposto soma 1 aqui, então vale mesmo contra armadura) e
   `× (1 + percentual recebido)` (passivas defensivas; negativo = menos dano).
6. **Arredondamento** (padrão: ao mais próximo) e **dano mínimo**
   (`minimumDamage`, hoje **0**): `max(dano mínimo, arredondado)`.

Depois do cálculo (`Character.ApplyHit`):

7. Os **escudos** do alvo absorvem primeiro (na ordem dos estados); o resto sai
   da vida. O `HitResult` guarda o dano total, o absorvido, o que foi à vida,
   se foi crítico e se matou.
8. A vida muda e o texto de dano flutua sobre a célula. Se a vida chegou a 0,
   `Die()`; senão a reação de acerto anima.
9. `DamageDealtEvent` (áudio, métricas), depois os estados do **atacante**
   reagem (roubo de vida: cura `fração × dano total × pilhas`) e os do **alvo**
   reagem (espinhos: devolve o dano fixo, mais a fração do recebido, ao atacante
   vivo). O dano devolvido por espinhos é do tipo `Thorns`, então não dispara
   espinhos nem roubo de vida de volta.

> [!note]
> Os espinhos reagem a todo golpe direto recebido, inclusive o que mata o
> portador. O dano de espinhos também é afetado pelo Dano causado do portador e
> pelo Dano recebido do atacante. Colisões não disparam espinhos.

**Exemplos com os valores atuais:** uma skill de 1 contra um Goblin blindado
(defesa 1) causa `1 − 1 = 0`; com o alvo Exposto, `0 + 1 = 1`. Empurrar o
mesmo Goblin contra uma rocha causa a colisão de 1, que ignora a armadura. Um
jogador Fraquejado causa `1 − 1 = 0` com uma skill de 1. A Martelada do Rei
Goblin (3) contra o jogador sem estados causa 3.

**Cura** (`CombatResolver.Heal` / `HealFraction`): recupera até a vida máxima
e levanta `HealedEvent`. Cura em personagem morto não faz nada.

### Bônus condicionais dos estados (passivas)

Balanceamento G3 (2026-10-06), com o fixo recebido de 2026-10-19. Um estado pode
mudar o dano de um golpe conforme a situação, sem sorteios e sem mexer no resto:
o `CombatResolver` chama, antes do cálculo, `StateEffect.ModifyOutgoingDamage`
(estados do atacante) e `ModifyIncomingDamage` (estados do alvo), que preenchem
um `DamageAdjustment` (fixo e percentual de quem causa, fixo e percentual de quem
recebe). O gancho é **puro** (sem sorteio, sem mudar o jogo), então a IA, a
previsão e o bot do simulador usam a mesma conta sem crítico:
`CombatResolver.PredictDamage` (também usada pela previsão das colisões e do
terreno).

**Quais tipos de dano.** Cada efeito tem uma máscara (`EDamageKindMask`). As
usadas hoje: `Skill` (os talentos de +1 de dano), `Collision` (Estilhaçar) e
`AnyAction` (ataque básico, skill, consumível e colisão: Exposto, Fraquejado e os
efeitos de abate). Dano periódico, espinhos e terreno só mudam se o efeito pedir,
e dano exato (`Pure`) nunca muda.

**Efeitos disponíveis** (`Settings/States`, lista de efeitos de um estado):

| Efeito | O que faz |
|---|---|
| `ConditionalDamageEffect` | quando a **condição** vale: no dano causado, `+ fixo` na base e `× (1 + percentual)`; no recebido, `+ fixo` depois da defesa e `× (1 + percentual)`; por contagem e por pilha |
| `OnKillEffect` | ao matar com os tipos escolhidos: cura fixa ou fração da vida máxima, reduz as recargas em andamento (aplicado no fim do turno, então inclui a skill usada no golpe), dá estados e/ou devolve o andar do turno (`grantExtraWalk`, `ExtraWalkGrantedEvent`) |
| `ApplyStateOnHitEffect` | ao acertar com os tipos escolhidos, aplica estados ao alvo vivo (pilhas multiplicadas pelas do estado: o posto) |

**Condições** de `ConditionalDamageEffect` ("oponente" = o alvo no dano causado e
o atacante no dano recebido; as condições sobre o oponente não valem quando não há
atacante, como no dano periódico):

| Condição | Vale quando |
|---|---|
| `Always` | sempre (Exposto, Fraquejado, Estilhaçar) |
| `PerAdjacentEnemy` | uma vez por inimigo do portador adjacente a ele, até o teto (`maxCount`, padrão 3) |
| `NoAdjacentEnemy` | nenhum inimigo do portador está adjacente a ele |
| `OpponentHasNegativeState` | o oponente tem algum estado nocivo (debuff) |
| `OpponentHasState` | o oponente tem algum dos estados listados (Quebradiço: Congelado ou Gelado; Predador: Envenenado) |
| `HolderMovedThisTurn` | o portador andou **neste turno, antes do golpe** |
| `HolderDidNotMoveThisTurn` | parado: o portador **não andou neste turno** até o golpe (Foco Arcano) |
| `OpponentIsolated` | nenhum aliado do oponente está adjacente a ele (Assassino) |
| `OpponentAtFullHp` | o oponente está com a vida cheia (golpe de abertura) |

Hoje os assets usam só `Always`, `OpponentHasState`, `HolderDidNotMoveThisTurn` e
`OpponentIsolated`. As outras condições continuam no código desde o G3, sem
nenhum estado que as use (os talentos que as usavam saíram em 2026-10-19).

- **Adjacência:** distância 1 pela métrica `CombatSettings.adjacencyMetric`
  (Chebyshev: os 8 vizinhos), contando só personagens vivos
  (`GridRules.CountAdjacentOpponents` e `HasAdjacentAlly`).
- **Andou:** `Character.MovedThisTurn` / `MovedLastTurn`. O andar
  (`GridController.MoveEntity`) e a troca do Passo Trocado marcam o movimento;
  ser empurrado ou puxado, a investida e a troca do Passo das Sombras não contam,
  e desfazer o andar (`GridController.UndoMove`) desmarca. A bandeira volta a
  falso quando o turno do personagem começa (`Character.BeginTurn`) e entra no
  salvamento (`EntitySnapshot.MovedLastTurn`), então carregar repete o mesmo dano.
  Para o dano **recebido** essas condições dependem do momento do golpe, então
  evite usá-las em redução de dano.

Configuração: `CombatSettings` (ver [valores](#configurações)). Uma `CombatSettings`
ausente cai nos padrões do código.

## Skills

Skills são **dados** (`SkillDefinition`, `Create > GridBattle > Skills > Skill
Definition`), nunca código dentro de controllers. Jogador e inimigos usam o mesmo
caminho: `Character.TryUseSkill(grid, skill, célula)`; num inimigo, uma skill
anunciada é **preparada** em vez de usada (ver
[Golpes anunciados](#golpes-anunciados-telegrafia)).

Uma skill tem:

| Campo | Significado |
|---|---|
| Tipo | ofensiva, defensiva ou utilitária (`ESkillType`) |
| Dano | dano fixo a cada afetado, antes da escala do atacante, do bônus de skills, do crítico e da defesa |
| Forma da área | `Circle`, `Cross`, `Linear`, `Cone`, `Arc` ou `Perpendicular` |
| Tamanho da área | de 1 a 9; ímpar em todas as formas, exceto `Linear`, que aceita tamanho par (`SkillDefinition.NeedsOddSize`) |
| Área de seleção | onde a skill pode ser mirada (`ESkillSelectionShape`, como no Dofus): `Circle` (todas as casas no alcance, um losango pela métrica de alcance de skill), `Line` (só a linha e a coluna do lançador, em cruz), `Diagonal`, `Star` (linha e diagonal) ou `Square`; a distância é contada ao longo da forma |
| Alcance mínimo e máximo | `minRange` (0 = a própria casa também) e `range`; o máximo soma o atributo Alcance de skills quando `rangeModifiable` está ligado; `range` 0 = centrada no próprio lançador; ou ilimitado (sem máximo; a forma e o mínimo continuam valendo) |
| Célula alvo | qualquer casa, casa vazia ou casa com personagem vivo (`ESkillTargetCell`) |
| Afeta | inimigos, aliados, aliados e o próprio, só o próprio, todos, ou todos menos quem lança (`ESkillTargetFilter`; `EveryoneButCaster` é o dos golpes de inimigos) |
| Ignora linha de visão | a mira não precisa ver a célula (ver [Linha de visão](#linha-de-visão)) |
| Exige alvo | só pode ser usada se atingir ao menos um personagem (desligado em todas) |
| Recarga | turnos do dono entre usos (0 = sem recarga) |
| Usa o andar | `usesWalk` (seção *Turn cost*, só para o jogador; a IA dos inimigos ignora): a skill é **canalizada**, gasta também o andar do turno, então só pode ser usada antes de andar e o lançador não anda depois (ver [Uso](#uso)) |
| Efeitos | resultados que não são dano, aplicados **depois do dano, na ordem da lista** (`SkillEffect`): aplicar estados, curar, teleportar, empurrar/puxar (`Displace`), trocar de lugar (`Swap`), investida (`DashToTarget`) e criar terreno (`CreateTerrain`) |
| Visual | `ActionVisualDefinition` tocado antes do dano |
| Telegrafia | `telegraphed`, `windupTurns`, `canMoveWhileWindingUp`, `cancelledByDisplacement`, `interruptibleByStates`, `telegraphFollowsCaster` (só para inimigos; ver [Golpes anunciados](#golpes-anunciados-telegrafia)) |
| Melhorias | `upgrades` (as variantes que a skill pode virar) e `upgradeOf` (a skill original de uma variante); ver [Melhorias de skill](#melhorias-de-skill-variantes) |

Efeitos de skill disponíveis:

| Efeito | O que faz | Usado por |
|---|---|---|
| `ApplyStatesSkillEffect` | aplica estados aos afetados ou ao lançador (no lançador, sem o bônus de duração dos talentos) | várias (Envenenado, Congelado, Atordoado...); Exausto no lançador do Meteoro |
| `HealSkillEffect` | cura o lançador ou os afetados | Mordida Vampírica (cura 1 o morcego) |
| `TeleportSkillEffect` | leva o lançador à célula alvo livre | nenhum asset atual |
| `DisplaceSkillEffect` | empurra ou puxa (ver [Empurrar e puxar](#empurrar-e-puxar-deslocamento)) | Golpe, Rajada de Vento, Gancho e outras |
| `SwapSkillEffect` | troca de lugar com o personagem da célula alvo | Passo das Sombras e variantes |
| `DashToTargetSkillEffect` | o lançador avança em linha reta até a célula alvo | Investida com Escudo e variantes, Investida Sombria |
| `CreateTerrainSkillEffect` | cria terreno temporário (ver [Terreno](#terreno)) | Bola de Fogo, Muralha de Gelo, Linha Venenosa e outras |

Área de seleção (pedido do usuário, 2026-10-12, no estilo do Dofus):

```
Círculo         Linha (cruz)    Diagonal        Estrela         Quadrado
. . X . .       . . X . .       X . . . X       X . X . X       X X X X X
. X X X .       . . X . .       . X . X .       . X X X .       X X X X X
X X @ X X       X X @ X X       . . @ . .       X X @ X X       X X @ X X
. X X X .       . . X . .       . X . X .       . X X X .       X X X X X
. . X . .       . . X . .       X . . . X       X . X . X       X X X X X
```
(alcance 2; `@` = quem usa; com alcance mínimo 1, o `@` sai; com mínimo 2, saem também as casas vizinhas)

A seleção de cada skill está na tabela de [Skills](#skills-62). Em resumo: o
Guerreiro mira em linha com alcance fixo (o Arremesso de Escudo, 2-4, aceita o
atributo Alcance de skills; o Pisão é na própria casa); o Mago mira em círculo
(Rajada de Vento em linha; a Nova de Gelo é na própria casa); o Ladino mira em
linha, estrela ou círculo (a Bomba de Fumaça é na própria casa); os inimigos
corpo a corpo miram a casa vizinha em linha (o Rasante do Morcego-olho, na
diagonal; o Tremor do Golem de pedra, a Martelada do Rei Goblin, o Choque
Estático, o Grito Sônico e a Rajada de Espinhos são na própria casa) e os de
longe, em linha ou círculo. O destaque de mira, a IA dos inimigos e o bot do simulador
seguem a mesma regra; a janela de detalhes mostra, por exemplo, "Alcance: 1-3, em
linha".

### Valores efetivos e modificadores (`EffectiveSkill`)

Balanceamento G3 (2026-10-06). Os números de uma skill para um lançador (dano,
tamanho da área, alcance, recarga, distância de empurrões e puxões, duração dos
estados e efeitos) **vêm sempre do `EffectiveSkill`**
(`EffectiveSkill.Resolve(lançador, skill)`): a skill do asset mais o que os
estados do lançador mudam (`SkillModifierEffect`). Sem modificadores, todo número
é exatamente o do asset. O asset nunca guarda estado de runtime.

> [!note]
> **Nenhum talento atual usa `SkillModifierEffect`.** Os talentos de classe que
> mudavam números de skills saíram na migração de 2026-10-19; o que muda uma
> skill agora é a [melhoria](#melhorias-de-skill-variantes), que troca a skill por
> uma variante. O mecanismo continua no código e vale também para as variantes
> de uma skill listada.

Um `SkillModifierEffect` vale para as skills listadas (e as variantes delas; lista
vazia = todas as skills do portador) e cada valor é **por pilha**:

| Campo | Efeito |
|---|---|
| `damageFlat`, `damagePercent` | `(dano + fixo) × (1 + percentual)`, antes da escala do lançador; só skills que já causam dano |
| `areaSteps` | cada passo soma 2 ao tamanho (um anel a mais; uma linha fica 2 células mais longa), no máximo 9 |
| `range` | soma ao alcance (skills centradas no lançador e de alcance ilimitado não mudam) |
| `cooldownReduction` | tira da recarga; a recarga mínima de `SkillSettings` continua valendo junto com a Redução de cooldown do atributo |
| `displacement` | soma à distância de todo empurrão ou puxão da skill (inclusive os anexados) |
| `stateDuration` | soma aos turnos de todo estado temporário que a skill aplica (os permanentes não mudam) |
| `attachedEffects` | `SkillEffect`s acrescentados à lista de efeitos da skill; `repeatAttachedPerStack` repete a lista uma vez por pilha |

**Quem lê o `EffectiveSkill`:** `SkillTargeting` (alcance, células de mira,
área, afetados, `CanUse`, `IsValidTarget`), `SkillDefinition.Execute` e
`GetDamageFor`, `SkillCooldowns.Trigger`, os efeitos de skill (via
`SkillContext.Effective`), o golpe anunciado (as células são calculadas com ele),
a previsão, a IA dos inimigos e o `BattleBot`.

### Áreas

`SkillArea` é pura e estática. A **direção** `d` é a direção cardinal do
lançador à célula alvo, pelo eixo dominante (empate vai na horizontal; alvo na
própria célula usa "para cima"). Seja `r = (tamanho − 1) / 2`. As células fora do
grid e as de terreno que **bloqueia áreas de skill** (hoje só a Muralha de Gelo)
são descartadas; a célula bloqueada sai da área, mas as de trás continuam nela.

| Forma | Células atingidas | Exemplos |
|---|---|---|
| `Circle` | células a distância euclidiana ≤ `r + 0,5` do alvo | tamanho 1 = só o alvo; 3 = quadrado 3×3; 5 = 5×5 sem os cantos |
| `Cross` | o alvo e um braço de comprimento `r` em cada eixo | tamanho 1 = só o alvo; 3 = um "+" de 5 células |
| `Linear` | `tamanho` células a partir do alvo, afastando-se do lançador | tamanho 3 = o alvo e as 2 seguintes; aceita tamanho par |
| `Cone` | linhas `k = 0..r` a partir do alvo, cada uma com `2k + 1` células de largura | tamanho 3 = 4 células; 5 = 9 células |
| `Arc` | uma fileira de `tamanho` células centrada no alvo, perpendicular a `d`, com cada célula puxada de volta ao lançador | um "V" que envolve o lançador |
| `Perpendicular` | `tamanho` células centradas no alvo, perpendiculares a `d` | tamanho 3 = o alvo e as duas células ao lado |

O resultado não tem duplicatas e sai ordenado por linha e depois coluna, uma
ordem canônica que mantém determinísticos os acertos (e os sorteios de crítico).

### Linha de visão

Desde 2026-10-12 (pedido do usuário), uma skill precisa **ver** a célula onde é
mirada (`SkillTargeting.HasLineOfSight`, regra em `Rules/LineOfSight`):

- a linha vai do centro da célula do lançador ao centro da célula alvo; toda
  célula que ela cruza no meio do caminho bloqueia a visão quando tem
  **terreno que bloqueia a visão** (`TerrainDefinition.blocksLineOfSight`; só
  vale para terreno que bloqueia movimento: a Rocha e a Muralha de Gelo) ou, com
  `CombatSettings.charactersBlockLineOfSight` ligado, um **personagem vivo**
  (inimigo ou aliado);
- as células das pontas nunca bloqueiam: dá para mirar a própria pedra ou o
  inimigo da frente;
- encostar só no canto de uma célula não a cruza, então um vizinho na
  diagonal é sempre visível. A regra é simétrica: A vê B se e só se B vê A;
- não precisam de visão: skills com `ignoresLineOfSight` (Tempestade Elétrica,
  Meteoro e as variantes deles, Bola de Fogo Arqueada, Chuva de fogo e Morteiro)
  e skills centradas no lançador. `CombatSettings.skillsNeedLineOfSight` desliga
  a regra toda;
- a visão vale para a **mira**, não para a área: uma área continua atingindo
  as células dela (o terreno só tira células da área com "Bloqueia área de
  skill");
- num golpe anunciado, a visão vale **ao preparar**; o disparo atinge as células
  marcadas sem conferir a visão de novo;
- os destaques de alcance, a IA dos inimigos, a previsão e o bot do simulador
  usam a mesma regra.

### Uso

Uma skill pode ser usada (`SkillTargeting.CanUse`) quando o lançador está vivo,
nenhum estado impede skills, a skill está pronta (sem recarga) **e** a célula
alvo é válida: dentro do grid, **na área de seleção** (forma, alcance mínimo e
máximo; regras puras em `Rules/SkillSelection`), com o conteúdo que a skill pede
(casa vazia ou ocupada), **em linha de visão** (ver acima), com ao menos um
afetado (só se a skill exige: `requiresAffectedTarget`, desligado em todas) e
aceita pelos efeitos (um teleporte precisa de uma célula livre). Um uso
inválido não faz nada e **não gasta o agir do turno**.

**Skills canalizadas** (`SkillDefinition.usesWalk`, decisão de 2026-10-20: as
magias fortes do Mago têm um contra). Para o jogador, uma skill que usa o andar
só pode ser selecionada, mirada e usada **antes de andar**
(`PlayerCharacterController.CanUseSkillNow`: a vez dele, o agir livre, a skill
pronta e, se ela usa o andar, o andar livre; desfazer o andar devolve a
chance). Usá-la gasta o agir **e** o andar, e o andar gasto não pode ser
desfeito; se o personagem recusar o uso, o andar é devolvido. Um abate que
devolve o andar (Ímpeto) continua devolvendo. Hoje são canalizadas:

| Skill | Contra |
|---|---|
| Meteoro, Meteoro Ardente, Meteoro Veloz | usa o andar e deixa o Mago **Exausto** por 2 turnos (não anda no turno seguinte; `ApplyStatesSkillEffect` no lançador) |
| Tempestade Elétrica, Tempestade Rápida, Tempestade Ampla | usa o andar, sem Exausto |

Como a regra é "ainda não andou", dá para usar a Tempestade no turno em que o
Mago está Exausto ou enraizado (o andar não foi gasto); o Meteoro não repete
por causa da recarga (4 ou 5). Os inimigos não usam `usesWalk`.

`SkillDefinition.Execute`: o visual da skill (`ActionVisualDefinition`) ou, sem
ele, a animação de ataque das skills ofensivas (antes do dano, para a reação do
alvo cair no impacto); o dano a cada afetado (tipo `Skill`, na ordem das
células); os efeitos, na ordem da lista; e `SkillUsedEvent` (a área pisca).
Depois, `Character.TryUseSkill` inicia a recarga.

O dano da skill usa `dano efetivo × multiplicador de dano do lançador` (escala
por profundidade, hoje 1), arredondado (`EffectiveSkill.Damage`).

### Cooldown

- Conta em **turnos do dono** da skill (do jogador e dos inimigos). "Recarga N"
  significa **N turnos do dono entre usos**.
- Ao usar: `recarga efetiva − Redução de cooldown`, nunca abaixo do mínimo
  (`SkillSettings.minimumCooldown`, 1) para skills que têm recarga. Skills com
  recarga 0 nunca esperam. Um efeito de abate pode reduzir as recargas em
  andamento (`SkillCooldowns.QueueReduction`): a redução entra no fim do turno,
  antes de as recargas caírem 1.
- No fim de cada turno do dono (`Character.EndTurn`), toda recarga em andamento
  diminui em 1, **exceto a das skills usadas naquele turno**. Por isso uma skill
  de recarga 1 volta a ficar disponível depois de exatamente um turno no meio.
- **Golpes anunciados:** a recarga começa **quando o golpe dispara** (ou é
  cancelado, como se tivesse sido usado), não quando é preparado. Como o inimigo
  dispara no começo do turno e prepara no fim dele, um golpe de recarga 0 é
  preparado de novo no mesmo turno em que dispara (atinge a cada turno); com
  recarga N ≥ 1 ele dispara, espera e só prepara de novo depois, então atinge a
  cada **N + 2** turnos (recarga 1: a cada 3 turnos). Por isso todo inimigo tem
  também um golpe de recarga 0, preparado nos turnos de espera do especial
  (ver [Uso pelos inimigos](#uso-pelos-inimigos)).
- As recargas são **zeradas a cada batalha** (`RunSettings`) e entram na captura
  e na restauração da batalha.
- Os assets são compartilhados e não guardam estado: as recargas vivem em
  `SkillCooldowns`, por personagem.

### Slots

O personagem carrega no máximo **6 skills** (`SkillSettings.maxSkillSlots`), uma
por botão da barra de skills. A classe começa com **uma skill** (decisão de
2026-10-20), que ocupa 1 dos 6 espaços (`startingSkillsUseSlots`), e os talentos
liberam **até 5** skills a mais (`TalentOfferSettings.maxSkillTalents`; cada classe
tem exatamente 5 talentos que liberam skill). Uma melhoria troca a skill pela variante no mesmo espaço. A lista
de skills de uma run vive em `RunState` (`SkillIds`); `Character.SetSkills` a
aplica ao personagem da batalha.

### Entrada do jogador

Barra de skills (6 botões) → `PlayerCharacterController.SelectSkill`. Tocar o
botão seleciona a skill (as células onde ela pode ser mirada ganham highlight
`SkillRange`), tocar de novo cancela, e tocar outra troca. Com uma skill
selecionada (`GameplayInputSettings.confirmTargetedActions`, ligado):

1. um toque numa célula válida **mira** (`AimedCell`, `ActionAimChangedEvent`):
   a área ganha o highlight `SkillArea` e a previsão aparece;
2. um toque **na mesma célula**, ou o botão **Confirmar**, usa a skill, gasta o
   agir do turno e limpa a seleção; um toque **em outra célula válida** move a
   mira; numa célula inválida o toque é rejeitado (pulso na célula) e a mira
   continua;
3. o botão **Cancelar** (ou tocar de novo no botão da skill) desfaz a seleção.

Depois de agir, as skills ficam inativas até o próximo turno; depois de andar,
as skills canalizadas também (a barra pergunta `CanUseSkillNow`). A seleção e a
mira caem sozinhas ao mudar a vez, e quando a skill deixa de poder ser usada ali
(recarga, silêncio, alvo inválido, andar gasto numa skill canalizada). Com a
confirmação desligada, o primeiro toque numa célula válida já usa a skill.

### Uso pelos inimigos

A ação de IA `UseSkillsAction` percorre as skills do config, na ordem. Para cada
skill pronta:

- **sem empurrão ou puxão:** com `coverEscapeCells` ligado no asset `UseSkills`
  (desde 2026-10-20), a mira cobre **as casas para onde o jogador pode fugir**:
  a zona é a casa do jogador mais as casas para onde ele pode andar no próximo
  turno (`GridRules.CanWalkTo` até o andar dele). Cada célula de mira válida vale a
  soma das casas da zona que a área atinge (a do jogador vale `playerCellWeight`
  1,5, as outras `escapeCellWeight` 1; casa já marcada por outro golpe vale
  `alreadyCoveredFactor` 0,25 disso), menos `allyHitPenalty` 1 por outro inimigo
  atingido. Empate: a que atinge o jogador, depois a mais perto dele. Abaixo de
  `minCoverValue` 1, ou com a opção desligada, vale a mira antiga
  (`SkillTargeting.TryFindAimCell`: a casa do jogador se for válida; senão a célula
  válida mais próxima do inimigo cuja área ainda o atinge). Sem sorteio: o
  jogador ainda pode fugir, mas não basta andar uma casa para qualquer lado;
- **com o efeito `Displace`:** a mira é a célula de melhor pontuação
  (`DisplacementScoring`) entre as que afetam o jogador, e a skill só é usada se a
  pontuação chegar ao mínimo (hoje −1000, ou seja, sempre que houver mira); ver
  [Empurrar e puxar](#empurrar-e-puxar-deslocamento).

A primeira skill que funciona usa o agir do turno. Como **todas as skills de
inimigos são anunciadas**, "usar" significa **preparar** o golpe. Curar,
proteger, enfraquecer e invocar continuam sendo ações de IA imediatas (ver
[IA dos inimigos](#ia-dos-inimigos)).

**Golpe básico e especial** (decisão de 2026-10-20). Todo inimigo tem ao menos
um golpe **sem recarga** (o básico) e um **com recarga** (o especial), com o
especial primeiro na lista: quando ele está pronto e alcança o jogador, o
inimigo o prepara; senão prepara o básico. Antes, um inimigo com um só golpe de
recarga 1 ou 2 preparava, disparava e só voltava a preparar dois ou três turnos
depois, e a marca sumia por um turno. Agora a marca aparece em todo turno em que
algum golpe alcança o jogador. Com recarga 2, o especial volta a cada 4 turnos
e o básico cobre os turnos entre eles; com recarga 1, a cada 3. O Rei Goblin
tem três golpes (Martelada, Onda de Choque e o básico Golpe Real). A lista de
cada inimigo está em [Inimigos](#inimigos) e [Skills](#skills-62).

Ainda pode haver turno sem golpe anunciado:

- quando nenhum golpe do inimigo alcança o jogador, mesmo depois de andar (por
  exemplo, o Totem do raio, que nunca anda, com o jogador fora da linha e longe
  dele);
- quando um inimigo de apoio usa o agir numa ação imediata (curar, proteger,
  invocar, enfraquecer): nesse turno ele não prepara golpe;
- no primeiro turno da batalha, que continua sem golpe preparado (decisão de
  2026-10-19).

> [!important]
> Criar uma skill nova = criar um asset de `SkillDefinition` (ou uma subclasse,
> se a regra for especial) + referenciar no config do personagem, num talento ou
> na lista de variantes de outra skill. **Zero mudanças em controllers.**

### Golpes anunciados (telegrafia)

Decisão de 2026-10-19 ([[inimigos#Golpes anunciados]]): **todo golpe de inimigo
é anunciado**. Uma skill com `telegraphed` ligado, num inimigo, não dispara na
hora: `Enemy.TryUseSkill` faz as mesmas checagens de um uso (vivo, sem estado que
impeça skills, recarga pronta, alvo válido, linha de visão), **fixa a área** nas
células que ela cobre agora (calculadas com o `EffectiveSkill`) e guarda um
`SkillTelegraph` (quem lança, a skill, a célula mirada, as células, os turnos até
o disparo e se o jogador estava entre os afetados). Preparar usa o agir. Um
inimigo só prepara um golpe por vez.

| Campo da skill | Valor em todas as skills de inimigos | O que faz |
|---|---|---|
| `telegraphed` | ligado | liga a preparação |
| `windupTurns` | 1 | turnos do inimigo até disparar (1 = no próximo turno dele) |
| `canMoveWhileWindingUp` | desligado | se o inimigo pode andar nos turnos de espera (só importa com `windupTurns` > 1) |
| `cancelledByDisplacement` | desligado | se empurrar, puxar ou trocar de lugar com quem prepara cancela o golpe |
| `interruptibleByStates` | ligado | se um estado que impede skills (Atordoado, Congelado, Cego) cancela o golpe |
| `telegraphFollowsCaster` | ligado | se a área acompanha quem prepara quando ele é deslocado |

**Ciclo** (`EnemyController.Act`): no começo do turno do inimigo,
`Enemy.AdvanceTelegraph` desconta 1; se ainda falta, o inimigo continua
preparando (usa o agir e, sem `canMoveWhileWindingUp`, o andar). Quando chega a
hora, o golpe **dispara nas células marcadas** (`SkillDefinition.ExecuteOnCells`)
contra **quem estiver nelas agora**, com o dano, os estados, os empurrões e o
terreno normais da skill; o visual toca em todas as células marcadas, mesmo
vazias. A recarga começa e sai `TelegraphResolvedEvent` (com quem foi atingido).
Disparar não gasta nada (`CombatSettings.firingUsesAction` desligado; ligado,
volta a regra de 2026-10-18 em que disparar usava o agir). Depois o comportamento do
inimigo roda normalmente: ele anda e prepara o próximo golpe. A batalha começa
sem nenhum golpe preparado (`Enemy.Initialize` zera o golpe): cada inimigo
prepara o primeiro no seu turno, depois da primeira vez do jogador.

**Quem é atingido:** os golpes de inimigos afetam **todos menos quem lança**
(`EveryoneButCaster`), então acertam outros inimigos que estiverem nas células:
o jogador pode empurrar um inimigo para dentro da área de outro, ou virar o golpe
contra os aliados de quem o prepara. Mortes assim dão XP ao jogador como qualquer
morte de inimigo. No simulador (dano por run de golpes de inimigos em inimigos):
2,4 (Guerreiro), 1,8 (Mago) e 6,0 (Ladino); mortes 0,6, 0,5 e 2,1.

**Cancelamento:**

- **morte** de quem prepara: o golpe some (sem recarga);
- **estado que impede skills** (Atordoado, Congelado, Cego), com
  `interruptibleByStates`: cancela na hora em que o estado chega
  (`OnStatesChangedForTelegraph`); a recarga começa como se tivesse sido usado;
- **deslocamento** (empurrão, puxão ou troca de lugar), com
  `cancelledByDisplacement` (desligado em todas): cancela.

**A área acompanha quem é deslocado** (`telegraphFollowsCaster`, ligado): quando
um inimigo que prepara é empurrado, puxado ou trocado de lugar
(`CharacterDisplacedEvent`, `CharactersSwappedEvent`), as células marcadas se
movem com ele, mantendo a forma e a direção (`SkillTelegraph.Shift`). Células
que saem do grid nunca são atingidas. Assim, empurrar o atacante muda para onde
o golpe vai.

**Como o jogador vê** (refeito em 2026-10-20, porque destaques sobrepostos e
números demais poluíam o tabuleiro): as células marcadas ficam com **listras
diagonais** laranja (`ForecastSprites.GetStrikeHatch`, `telegraphHighlightColor`),
vermelhas onde há um personagem que o golpe afeta (`telegraphHitColor`), o tempo
todo e em todas as vezes, por cima dos outros destaques; a listra não tem moldura,
então não se confunde com a moldura do andar ou da mira na mesma célula. Vários
golpes na mesma célula dão uma marca só, e o dano somado pisca na barra de vida de
quem seria atingido (ver [Previsão do resultado](#previsão-do-resultado)). As
células não mostram números (`ForecastSettings.showOrderOnStrikes` desligado). Para
saber de quem é cada área, o jogador **toca no inimigo**: as marcas dos outros golpes
ficam fracas (`ForecastSettings.dimmedStrikeAlpha`, 0,3), as do golpe dele ficam
fortes e o ícone do golpe aparece sobre ele. O número da ordem fica sempre no canto
da célula do inimigo.
Toque longo numa célula marcada abre a seção "Golpe preparado" da janela de
detalhes: a skill, quem lança (com o número da ordem), o dano previsto contra o
jogador e em quantos turnos dispara. A dica de tutorial `FirstTelegraph` aparece
na primeira vez que um golpe é preparado.

**Salvamento e determinismo:** o golpe preparado (skill, célula mirada, células,
turnos restantes, se mirava o jogador) entra no salvamento da batalha
(`CaptureTelegraph` / `RestoreTelegraph`). A mira não tem sorteio.

**Fora da telegrafia:** curar, proteger, enfraquecer e invocar continuam
imediatos (decisão de 2026-10-19). O design ([[inimigos]]) sugeria anunciá-los
também; isso **não está implementado**. A IA mira a célula atual do jogador, sem
sorteio e sem prever a fuga, como o design pede.

### Melhorias de skill (variantes)

Decisão de 2026-10-19 ([[mapa_e_nos]], [[classes_e_skills]]). Cada skill do
jogador tem **duas variantes** (`SkillDefinition.upgrades`; a variante aponta a
original em `upgradeOf`). Uma melhoria troca a skill pela variante escolhida
**para o resto da run**, no mesmo espaço da barra (`RunManager.ChooseSkillUpgrade`
troca o id em `RunState.Player.SkillIds`). Cada skill melhora **uma vez**: uma
variante não tem variantes (`CanBeUpgraded`), então a escolha é 1 entre 2.

- **De onde vem:** do **acampamento** (o nó de cura, opção Treinar) e da vitória
  numa **batalha de elite** (ver [XP, nível e talentos](#xp-nível-e-talentos) e
  [[mapa_e_nos]]). A janela (`SkillUpgradeController`, `ModalsSettings.skillUpgradeView`)
  lista as skills que ainda podem melhorar e as duas variantes de cada uma, cada
  variante com as linhas da ficha da skill que ela muda ("Dano: 2 > 3", "(novo)",
  "removido"; números do asset, sem o dono); a escolha é definitiva; recusar volta à escolha do acampamento ou, depois do elite, abre mão da
  recompensa (`DeclineSkillUpgrade`).
- **Mesma skill:** `Root` e `IsSameSkillAs` tratam a variante como a skill
  original: a oferta de talentos não oferece de novo uma skill já liberada (nem a
  variante dela), e os modificadores de skill listados para a original valem
  para as variantes.
- As variantes atuais estão em [Skills](#skills-62).

## Empurrar e puxar (deslocamento)

Decisão de balanceamento de 2026-10-06 (G4): skills e inimigos podem **empurrar
e puxar** personagens. O efeito `Displace` (`DisplaceSkillEffect`) entra na
lista de efeitos de uma skill, depois do dano, e tem dois campos: o **modo** e a
**distância** em células. As regras são puras e vivem em `Displacement`
(`Gameplay/Rules`); `DisplacementResolver` as aplica (e prevê, para a IA, a
previsão e o simulador).

| Modo | Para onde vai o personagem |
|---|---|
| `AwayFromCaster` | para longe do lançador |
| `TowardCaster` | em direção ao lançador (puxar); para ao ficar ao lado dele e nunca cai em cima |
| `AwayFromAreaCenter` | para longe do centro da área (a célula mirada); quem está no centro vai para longe do lançador |

**Direção.** É a de 8 vizinhos mais próxima do vetor da célula de referência até
o alvo (ângulo arredondado a 45°). Para células vizinhas é simplesmente o sinal
de cada componente; de longe, um alvo quase na mesma linha anda reto, não em
diagonal.

**Movimento.** Célula a célula em linha reta. Para antes de entrar em: fora do
grid (borda), terreno que bloqueia movimento, ou célula ocupada por outro
personagem. Uma diagonal entre dois obstáculos também para (a mesma regra de
andar: basta um dos lados livre). Passar por células não tem efeito. Um
personagem pode ficar parado (distância percorrida 0) e ainda assim colidir.
Sem sorteios.

**Colisão.** Parar contra obstáculo, personagem ou (se `edgeCountsAsCollision`)
a borda causa dano `Collision` ao deslocado: `collisionDamage` (**1**) mais
`collisionDamagePerUnspentCell` (**0**) por cada célula que não percorreu. O
personagem atingido leva `collisionHitCharacterDamage` (**1**), fixo. O lançador
nunca leva dano do próprio deslocamento (se algo é empurrado contra ele, só o
deslocado leva). O dano passa pelo `CombatResolver` (escudos, mortes, eventos,
sons), é atribuído ao lançador (então abates dão XP normalmente), **ignora a
defesa** (`collisionIgnoresDefense`: a colisão passa pela armadura), não é
crítico e não dispara espinhos nem roubo de vida. Estados que mudam o dano da
ação (Exposto no alvo, Fraquejado e Estilhaçar em quem empurrou) valem para a
colisão.

**Terreno.** A célula onde o personagem para reage **na hora** (entrada
forçada, `TerrainEffects.ApplyForcedEntry`): dano e estados do perigo ou do
bônus, qualquer que seja o gatilho do terreno (o gatilho "ao entrar" já foi
aplicado pelo próprio movimento). Os gatilhos normais de início e fim de turno
continuam valendo depois. `TerrainTriggeredEvent.IsForced` marca esses casos.

**Golpe anunciado de quem é deslocado.** A área acompanha o inimigo (ver
[Golpes anunciados](#golpes-anunciados-telegrafia)).

**Imóveis.** `CharacterConfig.canBeDisplaced = false` (o Rei Goblin): empurrões,
puxões e trocas não o movem e não causam dano a ele por isso, mas ele continua
bloqueando e leva o dano de quem for lançado contra ele.

**Vários alvos.** Numa área, os alvos são movidos do mais longe ao mais perto da
referência (do centro da área, ou do lançador), e nos puxões do mais perto ao
mais longe, com desempate por linha e coluna, para atrapalharem-se o mínimo.
`Displacement.Plan` prevê o conjunto na mesma ordem.

**Visual.** Lógica imediata: a posição muda na hora. O personagem desliza até a
célula (`GridMovementSettings.slideSecondsPerCell`, sem pulo), e a reação de
acerto (flash e recuo) das colisões espera o fim do deslizamento
(`collisionBumpDelay`). Tudo isso é pulado no simulador (`SimMode`). O fluxo
de turnos espera pelas animações como sempre.

**IA.** `UseSkillsAction` pontua cada uso possível de uma skill de deslocamento
(`DisplacementScoring`, pesos em `DisplacementAiWeights` no asset `UseSkills`):
dano de colisão e de terreno no jogador, dano nos aliados do inimigo (custo),
abate (+50), aliados que alcançam o jogador depois menos antes (+5 cada; hoje
sempre 0, porque a conta usa o ataque básico), +1 por casa que o jogador termina
mais perto do lançador (−1 por casa mais longe) e os estados da célula de
destino (+4 por estado de um perigo, −5 por estado de um bônus). A mira é a de maior pontuação entre as que afetam o jogador; com o
mínimo de −1000, a skill é usada sempre que há uma mira assim.

| Valor (`CombatSettings`) | Atual |
|---|---|
| Dano de colisão | 1 |
| Extra por célula não percorrida | 0 |
| Dano ao personagem atingido | 1 |
| Colisão ignora defesa | sim |
| A borda do grid conta como colisão | sim |

No simulador, o Guerreiro deslocou 61 inimigos por run, com 43 colisões e 38
mortes por colisão; o Mago 20, 7 e 6; o Ladino 5, 2 e 0.

### Trocar de lugar

- **`SwapSkillEffect`** (Passo das Sombras e variantes): o lançador troca de
  célula com o personagem vivo e deslocável da célula alvo (a skill exige casa
  ocupada). Se o dano da skill matou o alvo, o lançador ocupa a célula livre. O
  dano só atinge inimigos, mas a troca vale com qualquer personagem. A troca usa
  `GridController.SwapCharacters` e **não é um andar**.
- **Passo Trocado** (talento de regra, `SwapStepEffect`): enquanto o andar do
  turno não foi usado, tocar num personagem vizinho (8 vizinhos, vivo e
  deslocável) troca de lugar com ele. Usa o andar, conta como ter andado
  (`RegisterMovement`) e **não pode ser desfeito**. As células dos personagens com
  quem dá para trocar ganham o highlight `Walk`.
- Na troca, as duas células reagem como ao entrar (`EntityEnteredCellEvent`) e
  o golpe preparado de um inimigo trocado acompanha o inimigo.

### Investida

`DashToTargetSkillEffect` (Investida com Escudo e variantes, Investida Sombria do
Cavaleiro esqueleto): o lançador avança em linha reta (uma das 8 direções, rumo
à célula mirada) até a distância de Chebyshev dela e para na célula mirada se
estiver livre, ou na última livre antes dela: ao lado do alvo, antes de um
obstáculo ou de outro personagem no caminho, ou numa diagonal entre duas células
bloqueadas. **Não é um andar** (`MovedThisTurn` não muda); a célula final reage
como ao entrar. Fica antes do `Displace` na lista de efeitos, para o empurrão
partir da célula nova. Como o dano vem antes dos efeitos, o alvo leva o dano, o
lançador avança e então empurra.

### Talentos que mudam o deslocamento

| Talento | Efeito (`Settings/States/Talents`) | Regra |
|---|---|---|
| Dominó (Guerreiro) | `DominoEffect`, distância 1 | quando quem o jogador empurra ou puxa bate em outro personagem, esse também é empurrado 1 casa na mesma direção, com a própria colisão; a corrente vai só um passo (o segundo não empurra um terceiro) |
| Rastro de Chamas (Mago) | `DisplacementTrailEffect`, Fogo por 2 rodadas | quem o jogador empurra ou puxa deixa Fogo temporário na célula de onde saiu e nas que atravessou (não na final), só em chão comum e só com terreno que não bloqueia |
| Gelo Escorregadio (Mago) | `SlipperyPushEffect`, Congelado ou Gelado, deslize 6 | um empurrão (não um puxão) do jogador num alvo Congelado ou Gelado vai até max(distância, 6) casas: desliza até bater em algo |
| Estilhaçar (Guerreiro) | `ConditionalDamageEffect`, colisão +1 | as colisões que o jogador causa dão 1 de dano a mais |

As regras de distância passam por `Displacement.GetDistance`, então a previsão e
o bot veem a distância do Gelo Escorregadio. **Pendência:** o empurrão em cadeia
do Dominó ainda **não entra na previsão** (`DisplacementResolver.Predict` não o
calcula), e o Fogo do Rastro de Chamas também não aparece nela.

## Consumíveis em batalha

Um consumível (`ConsumableDefinition`, `Create > GridBattle > Consumables >
Consumable Definition`) é um item de uso único, obtido no nó de consumível do
mapa, que só pode ser usado **em batalha** e **não gasta o andar nem o agir do
turno** (no máximo um por turno). Tudo é dado:

| Campo | Significado |
|---|---|
| Mira | `Self` (usado na hora, sem alvo) ou `Cell` (precisa de uma célula alvo) |
| Alcance, alcance ilimitado | mira `Cell`: distância (Manhattan, como as skills) da célula alvo; 0 = centrado em quem usa |
| Ignora linha de visão | mira `Cell`: desligado = precisa ver a célula alvo, como as skills (obstáculos e personagens no caminho bloqueiam); ligado = algo que cai do alto |
| Forma e tamanho da área, afeta | a área em torno da célula alvo e quem ela atinge (as mesmas formas das skills) |
| Efeitos | curar quem usa, causar dano aos afetados, aplicar estados (a quem usa ou aos afetados), empurrar ou puxar os afetados (`DisplaceConsumableEffect`), trocar de lugar com o personagem da célula alvo (`SwapConsumableEffect`), criar terreno temporário (`CreateTerrainConsumableEffect`) |
| Visual | `ActionVisualDefinition` (a Bomba tem) |
| Peso de sorteio | chance relativa de sair no nó de consumível |
| Aparece no glossário | desligado |

- **Inventário** (`ConsumableInventory`): 3 espaços (`ConsumableSettings.slots`,
  no máximo 6), guardados em `RunState.Player.ConsumableIds`. Com o inventário
  cheio, a política padrão é perguntar ao jogador qual item descartar ou recusar
  o novo (`AskPlayer`; as outras são descartar o novo ou o mais antigo).
- **Regras de uso** (`ConsumableRules`): o item existe, o jogador está vivo, é a
  vez dele, a batalha não foi decidida, o limite de usos do turno (1) não foi
  atingido, a célula alvo é válida, **há linha de visão até ela** (desde
  2026-10-20, pela mesma regra das skills: `ConsumableRules.HasLineOfSight`; itens
  sem alvo, centrados em quem usa ou com `ignoresLineOfSight` não precisam) e **ao
  menos um efeito mudaria algo** (a cura recusa vida cheia; o dano exige um
  inimigo na área; o empurrão, alguém que possa ser empurrado; a troca, um
  personagem que possa ser movido; o terreno, ao menos uma célula onde ele caiba).
  Um item nunca é gasto à toa.
- **Uso** (`ConsumableExecutor`): tira o item do inventário, conta o uso no turno
  (o contador zera a cada turno global), aplica os efeitos e levanta
  `ConsumableUsedEvent`. **Nunca encerra a vez sozinho**, a não ser que depois
  dele nada mais seja possível.
- **Dano de consumível** (`EDamageKind.Consumable`) passa pelo crítico e pela
  defesa do alvo, mas não escala com a profundidade.
- **Entrada:** barra de itens (um botão por espaço). Item sem alvo é usado ao
  tocar o botão; item com alvo entra em modo de mira (highlight `SkillRange` nas
  células válidas) e segue o mesmo **mirar e confirmar** das skills (o primeiro
  toque mira e mostra a área e a previsão do dano; o segundo, ou Confirmar,
  usa). Tocar o mesmo botão cancela. Mirar um item cancela a skill selecionada, e
  vice-versa.
- **Previsão** (`ActionForecast.ForConsumable`): dano, empurrões e puxões (com
  colisões e terreno), troca de lugar e quem um estado que impede skills
  interromperia (Frasco Congelante), pelas mesmas funções das skills. O terreno
  criado não entra na previsão (como nas skills).
- **Efeitos de tabuleiro** (`BoardConsumableEffects.cs`): `DisplaceConsumableEffect`
  usa o `DisplacementResolver` com quem usa o item no papel de lançador (colisões
  atribuídas a ele; talentos de regra valem); `SwapConsumableEffect` usa
  `GridController.SwapCharacters` (o golpe preparado acompanha quem é trocado);
  `CreateTerrainConsumableEffect` segue as regras do `CreateTerrainSkillEffect`.
- **Bot do simulador:** usa itens de dano e de estado nos inimigos como antes e,
  desde 2026-10-20, os de empurrão quando a previsão dá dano ou abate sem ferir o
  jogador. A troca, a Pedra e o Frasco de Fogo não são usados pelo bot.
- **Obtenção:** o nó de consumível sorteia do pool, pelo peso, usando o fluxo
  `Consumables` da seed; com `choiceCount` 3, o jogador escolhe 1 entre 3 opções.

Itens atuais: ver [Valores atuais](#consumíveis).

Itens de tabuleiro implementados em 2026-10-20: Granada de Impacto, Pergaminho de
Troca, Pedra, Frasco de Fogo e Frasco Congelante. O Óleo do design ([[consumiveis]])
não foi implementado.

## Ações do jogador e entrada

O `PlayerCharacterController` escuta `CellTapEvent` (clique ou toque curto numa
célula) e **roteia** a ação:

```
tap em célula
├── entrada bloqueada (TurnBlockers)?  → ignora
├── não é a vez do jogador?            → pulso de rejeição (na vez dos inimigos, também acelera as animações)
├── (qualquer toque apaga o alcance de um inimigo inspecionado)
├── consumível selecionado?            → TapWithConsumable: mira / confirma (grátis)
├── skill selecionada?                 → TapWithSkill: mira / confirma (usa o agir; a canalizada, também o andar)
├── a célula do próprio jogador?       → encerra o turno
├── célula sem conteúdo                → ANDAR (ainda pode andar e GridRules.CanWalkTo)
│     └── válido → GridController.MoveEntity
└── célula com conteúdo
      ├── vizinho e Passo Trocado com o andar livre → TROCAR DE LUGAR (usa o andar)
      ├── inimigo vivo (sem ataque básico) → mostra ou esconde até onde ele pode andar (highlight Danger)
      └── senão                          → ataque básico, se o personagem tiver (hoje ninguém tem) ou rejeição
depois de cada escolha (andar, agir, item, troca): nada mais é possível (HasAnythingLeft falso) → encerra o turno
sozinho (PlayerActionEvent); senão a vez continua
```

- **Andar e agir.** O controller guarda o que o jogador já usou no turno
  (`CanMove`, `CanAct`, `CanUseConsumable`, `CanUndoMove`, `HasStartedTurn`) e
  levanta `PlayerTurnStateChangedEvent` a cada mudança. `EndTurn()` (botão
  **Encerrar**) e o toque no próprio personagem encerram a vez; o turno acaba
  sozinho quando, depois de uma escolha, `HasAnythingLeft` é falso (andar,
  agir e consumível usados ou impossíveis: nenhuma célula para andar, nenhuma
  skill que `CanUseSkillNow` aceite com célula para mirar, nenhum item
  utilizável). Uma skill canalizada pronta também conta enquanto o andar pode
  ser desfeito, para que andar primeiro não encerre a vez antes de o jogador
  poder voltar atrás. `CanWalkNow` diz se o jogador pode andar para algum lugar
  agora (falso com o andar gasto, Exausto, enraizado ou cercado). Os highlights
  de andar são refeitos depois de cada escolha.
- **Ímpeto** (talento, `OnKillEffect.grantExtraWalk`): matar um inimigo depois
  de ter andado devolve o andar do turno (`ExtraWalkGrantedEvent`; vale para
  mortes por skill, consumível ou colisão). O primeiro andar deixa de poder ser
  desfeito. O segundo segue a regra normal de desfazer: como a morte costuma vir
  de uma skill (o agir já foi usado), em geral ele também não pode ser desfeito,
  mas depois de uma morte por consumível pode.
- **Desfazer o andar** (`UndoMove()`, botão **Desfazer**): enquanto o agir não foi
  usado, nenhum consumível foi usado depois do andar, a célula de destino não tem
  terreno que reage ao entrar (`ETerrainTrigger.OnEnter`) e a célula de onde saiu
  continua livre. O personagem volta à célula de onde saiu pelo
  `GridController.UndoMove` (sem `EntityEnteredCellEvent`, então o terreno não
  reage) e pode andar de novo. A troca de lugar não pode ser desfeita.
- **HUD** (`UI/Hud/TurnActionsView`): entre o grid e a barra de itens, um pé
  (andar), uma espada (agir) e uma poção (o consumível do turno) que apagam quando
  usados (o pé também apaga quando o jogador não tem para onde andar, como
  Exausto: `CanWalkNow`), o botão **Desfazer** enquanto dá para desfazer e **Encerrar** durante
  toda a vez do jogador. **Enquanto uma ação está mirada**, os botões
  **Cancelar** (`cancel-action-btn`) e **Confirmar** (`confirm-action-btn`)
  ocupam o lugar de Desfazer e Encerrar.
- **Animação:** a lógica é imediata, mas o visual de uma skill de quem acabou de
  andar espera o fim do pulo (`GridController.GetMovementRemaining`); a reação do
  alvo e o número de dano esperam esse atraso também.
- **Salvamento:** continua no início do turno. Carregar no meio de um turno repete
  o turno desde o começo (com os mesmos sorteios).
- Ação inválida (fora de alcance, célula ocupada ou bloqueada ao andar, alvo
  inválido, andar ou agir já usados) mostra o pulso de rejeição na célula e **não
  gasta nada**.
- `PlayerCharacterController.TryUseSkill` e `TryUseConsumable` usam skills e
  itens direto numa célula, sem seleção nem confirmação (atalhos de UI e testes);
  `ConfirmAim()` e `CancelAction()` são os botões Confirmar e Cancelar.

### Entrada bloqueada

`GameplayInput.IsBlocked` vale enquanto algo segura o fluxo do turno
(`TurnBlockers`): o toque nas células, os botões de skill e de item e o toque
longo são ignorados. Quem segura:

| Bloqueio | Quando |
|---|---|
| "XP delivery" | orbs de XP em voo que vão subir de nível, ou que são do último inimigo da batalha |
| "Talent choice" | a escolha de talento está aberta |
| "Entity details" | a janela de detalhes está aberta |
| "Pause menu" | o menu de pausa está aberto |
| "Tutorial tip" | uma dica de tutorial está aberta |

Depois que a batalha é decidida (`ETurnOwner.None`) ninguém age. Fora da vez do
jogador, ou com a entrada bloqueada, as barras de skill e de item aparecem
inativas e os toques nelas não fazem nada.

### Detalhes de uma entidade

**Toque longo** em dispositivos de toque (o toque precisa ficar na célula por
`GameplayInputSettings.longPressSeconds`, 0,45 s) ou **clique direito** do mouse
abre a janela de detalhes da entidade na célula, do terreno dela e dos golpes
anunciados que vão atingi-la (`CellLongPressEvent`). O fim de um toque longo é
descartado (não anda nem age) e deslizar para fora da célula cancela o toque.
Toque longo num botão das barras abre a ficha da skill ou do item
(`ActionDetailsModel`: dano, área, alcance, linha de visão, recarga, empurrão,
os estados que a skill aplica no lançador na linha "Em você"
(`details.action.on_caster`, por exemplo "Em você: Exausto"), numa skill
anunciada a linha "Preparada" e, numa skill canalizada, a nota "Usa o andar do
turno: só antes de andar, e não anda depois." (`details.uses_walk`)).

A janela mostra: título e retrato; vida; para inimigos, o **papel**, o texto do
comportamento, a linha **Ordem** ("N de M", a ordem de ação) e a seção
**Preparando** com o golpe que prepara ("Skill (em N turno)"); os **atributos** (andar e defesa sempre; alcance de
ataque e dano básico só para quem tem ataque básico; os demais só quando
diferentes de zero); as **skills** com a recarga restante ou "pronta"; os
**estados ativos** com o nome, as pilhas e os turnos restantes ou "permanente";
o **terreno** da célula; e, numa célula marcada, a seção **Golpe preparado**
(skill, quem lança com o número da ordem, dano previsto contra o jogador, em
quantos turnos dispara). Uma célula vazia só com marcas tem o título "Área
marcada". Enquanto está aberta, o jogo não aceita ações (`TurnBlockers`). Ela só
fecha pelo **X**; abrir ou fechar não consome o turno nem faz nada no jogo, e um
novo grid a fecha.

No toque longo num **inimigo**, na vez do jogador, o grid também marca em laranja
(highlight `Danger`) as células para onde ele pode andar a partir de onde está
(`GridRules.GetWalkCells`; nenhuma se um estado o impede de andar). Até
2026-10-20 eram as células que os golpes prontos dele cobririam de qualquer casa
para onde pudesse andar, o que acendia quase o tabuleiro todo. Como a janela cobre boa parte do grid, a marcação
continua depois de fechá-la e some no próximo toque no grid. Sem ataque básico,
um **toque curto** num inimigo faz o mesmo sem abrir a janela (tocar de novo
esconde).

## Previsão do resultado

Lote C1 de 2026-10-19 (`Gameplay/Forecast`). Antes de confirmar, o jogador vê
o que a ação faria e o que os golpes anunciados fariam depois dela, no estilo do
Into the Breach. As contas são **puras** (sem sorteio, sem crítico, sem mudar
nada) e usam as mesmas regras do jogo; o bot do simulador lê as mesmas classes.

**`ActionForecast`** (a ação mirada, skill ou item):

- a área (`SkillTargeting`) e, para cada personagem, o dano previsto por
  `CombatResolver.PredictDamage`: o dano da skill, as colisões e o dano de
  terreno dos empurrões e puxões (antes dos escudos);
- quem morre (`Killed`: dano ≥ vida + escudo);
- os deslocamentos (`Moves`, de `DisplacementResolver.Predict`, com o Gelo
  Escorregadio e o resto das regras de `Displacement`; quem morre com o dano não
  é deslocado) e a **posição final** de cada um (`Finals`: o destino da
  investida, a troca de lugar, os empurrados);
- quem seria **interrompido** (`Interrupted`): sobreviventes que recebem um estado
  que impede skills (exceto os imunes a ele);
- os efeitos são tratados na ordem da lista, e só estes entram: investida, troca
  de lugar, empurrão ou puxão e estados que impedem skills. Num item, só o efeito
  de dano (`ForConsumable`).

**Não entram na previsão:** o empurrão em cadeia do Dominó, o terreno que a skill
ou o Rastro de Chamas criaria e os efeitos das passivas que não mudam o dano
(estados aplicados ao acertar, como Lâminas Envenenadas, e os efeitos de abate).

**`ThreatForecast.Compute(grid, ação)`** (os golpes anunciados):

- a ordem de ação (`ActingOrder`) e os personagens nas posições depois da ação;
  inimigos que a ação mata saem;
- somem os golpes de quem morre e, se a skill é interrompível, de quem é
  interrompido; quem é deslocado leva o golpe junto (`telegraphFollowsCaster`) ou
  o perde (`cancelledByDisplacement`);
- o resultado: as células marcadas (`MarkedCells`), as que têm um personagem que
  o golpe afeta (`HitCells`), o dano de cada golpe em cada personagem
  (`SkillTelegraph.PredictDamage`) e quem morreria somando o dano da ação;
  `FiresNext` marca os golpes que disparam no próximo turno do dono.

**`ForecastView`** (adicionada pelo `GridController`; refeita no máximo uma vez por
quadro quando algo muda; nada no simulador) desenha:

- a marca de cada golpe anunciado nas células (`Cell.Warned`), vermelha onde há
  um personagem que seria atingido (`Cell.WarnHit`);
- o número da ordem de ação sobre cada inimigo (`EnemyIconsView.SetActingOrder`) e
  o número de quem lança na célula mirada de cada golpe (cinza se ele só dispara
  depois do próximo turno); vários golpes na mesma célula ficam lado a lado, em
  ordem de ação, a `strikeOrderSpacing` (9 px) um do outro (antes eram 6 px fixos);
- o dano previsto piscando na barra de vida, com o número "-N" (vermelho se
  mata);
- com uma ação mirada: o dano e as mortes dela, setas dos empurrões e puxões
  (`ForecastSprites`, 10×10 desenhadas em código), "fantasmas" onde os
  personagens terminariam e os golpes como ficariam depois da ação (movidos com
  quem foi deslocado, sumidos com quem morreu ou foi interrompido).

A `ForecastView` substituiu a antiga marcação de golpes do `GridController`.
Ajustes visuais em `Settings/Combat/ForecastSettings.asset` (ver
[Configurações](#configurações)).

## Sistema de turnos

`TurnManager` (`GridBattle.Managers`, no GameObject do Grid, com o
`BattleController`) conduz o **turno global**. Cada turno global:

```
GlobalTurnStartedEvent(n)        ← zera o contador de consumíveis do turno; a batalha é capturada para o salvamento
  jogador: BeginTurn            ← efeitos de início de turno dos estados (veneno, queimadura, regeneração)
           EntityTurnStartedEvent
  espera as animações e os TurnBlockers
  TurnChangedEvent(Player)       ← o jogador pode agir (salva a batalha)
  ... a vez do jogador termina (PlayerActionEvent): nada mais possível, Encerrar ou toque em si mesmo ...
  TurnChangedEvent(Enemies)
  jogador: EntityTurnEndedEvent  ← gatilhos de fim de turno do terreno
           Character.EndTurn     ← recargas e durações diminuem
  espera o movimento do jogador e os TurnBlockers (orbs que sobem de nível, escolha de talento)
  para cada inimigo, na ordem de ação (ActingOrder):
      BeginTurn → Act (dispara o golpe preparado, anda, prepara o próximo) → EndTurn
      espera as animações dele, a pausa enemyStagger e os TurnBlockers
  espera as animações e os TurnBlockers
  TickTemporaryTerrain           ← o terreno temporário perde uma rodada
  → próximo turno global
```

- **Cada vez do jogador encerrada = 1 rodada completa dos inimigos.** Uma ação
  inválida não levanta o evento, então não há turno inimigo. A primeira escolha
  do turno fecha a janela em que o XP que ainda chega entra no salvamento do
  turno (`RunManager`).
- **A vez é explícita** (`ETurnOwner`: `Player`, `Enemies`, `None`;
  `TurnChangedEvent`): o jogador não age nem vê highlights fora da vez dele (as
  marcas dos golpes anunciados aparecem sempre).
- **Início e fim do turno de cada entidade** (`EntityTurnStartedEvent`,
  `EntityTurnEndedEvent`, chamados também para inimigos): os estados rodam os
  efeitos de início e de fim, as durações diminuem, o terreno dispara os gatilhos
  de início e de fim de turno, as recargas das skills e a memória da IA do
  inimigo diminuem. Uma entidade que morre no meio pula o resto do turno.
- **Rodada:** o terreno temporário perde uma rodada depois do último inimigo e
  antes da vez do jogador (e do salvamento dela).
- **Movimento:** a ocupação das células e o `CurrentGridPos` mudam na hora; só o
  visual anima (`GridMovementSettings`: estilo `Hop` ou `Flip`, pulso na célula de
  chegada). O andar segue um caminho de células ortogonais (`GridRules.FindWalkPath`):
  no estilo `Hop` o personagem pula de célula em célula, e cada célula cruzada levanta
  `EntityPassedCellEvent` (gancho para efeitos ao passar por uma célula; hoje nada
  reage, e o terreno continua reagindo só à célula de chegada).
- **Acelerar:** um toque durante a vez dos inimigos aplica `speedUpMultiplier`
  (2x) às animações e ao intervalo até a vez voltar ao jogador (sem
  `Time.timeScale`).
- **Nova batalha** (`GridInitializedEvent`) limpa os bloqueios, reinicia o
  `BattleController` e volta para a vez do jogador, abandonando qualquer vez
  inimiga pendente. Numa batalha restaurada o turno global continua do número
  salvo.
- **Salvamento:** a batalha é capturada no início de cada turno global, antes dos
  efeitos de início de turno, e gravada quando a vez passa ao jogador. A retomada
  repete o início do turno (ver [[GDD]] 8.1).

### Ordem dos inimigos

Decisão de 2026-10-19 (`Gameplay/Turns/ActingOrder`), no lugar da ordem por
distância ao jogador: a ordem é **fixa durante a batalha**, para o jogador poder
planejar. É o lugar de cada inimigo na batalha (`Enemy.ActOrder`): primeiro os
da lista gerada, na ordem da lista, depois os invocados, na ordem em que
chegaram; empate por linha (`y`) e depois coluna (`x`). O `ActOrder` entra no
salvamento (`EntitySnapshot.ActOrder`).

- O `TurnManager` faz os inimigos nessa ordem; a lista é tirada no início da
  rodada, então um inimigo invocado não age na rodada em que aparece.
- O número aparece sobre cada inimigo (1 = o primeiro entre os vivos), na célula
  mirada de cada golpe anunciado, na janela de detalhes (linha "Ordem": "N de M") e no nome
  de quem lança um golpe; o bot do simulador e a previsão usam a mesma ordem.
- **Um inimigo de cada vez:** cada inimigo dispara, anda e prepara, as animações
  dele terminam e só então, depois de `enemyStagger` (0,08 s), o próximo começa.
  No modo de simulação não há esperas.

### TurnBlockers

`TurnBlockers` (estático) reúne o que precisa terminar antes de o fluxo
continuar. Quem precisa segurar o jogo chama `Acquire(motivo)` e descarta o
`IDisposable` retornado para liberar. O `TurnManager` e o `BattleController`
esperam `WaitAsync` em cada passo. Os bloqueios que existem estão na tabela da
seção [Entrada bloqueada](#entrada-bloqueada). Uma nova batalha limpa todos.

### Fim da batalha

O `BattleController` decide o resultado quando um personagem morre:

- **o jogador morre:** `BattleDecidedEvent(false)` e `BattleEndedEvent(false)` na
  hora;
- **o último inimigo morre:** `BattleDecidedEvent(true)` na hora (a vez passa a
  `None`: ninguém age) e `BattleEndedEvent(true)` só depois de terminar as
  animações e os `TurnBlockers` (os orbs de XP do último abate, a subida de nível
  e a escolha de talento que ele causar). O último abate é resolvido por inteiro
  antes de a run seguir.

## IA dos inimigos

A IA é **dado, não código de controller**. O `EnemyController` (componente do
`Enemy.prefab`) resolve o golpe preparado e delega o resto do turno ao
`EnemyBehavior` do config:

```
EnemyController.Act()
  → guard: inimigo morto não age; sem jogador, não age
  → com golpe preparado: Enemy.AdvanceTelegraph
      ainda falta turno → usa o agir (e o andar, se a skill não deixa andar)
      disparou          → não usa nada (CombatSettings.firingUsesAction desligado)
  → config.Behavior.TakeTurn(EnemyTurnContext { Self, Grid, Target, Memory }, andar usado, agir usado)
      → um andar e um agir: percorre a lista em ordem, pulando o tipo já usado
        (EnemyAction.IsMovement: as ações de aproximação são andar; as outras
        são agir), e repete a passada enquanto algo funcionar (até 2)
```

- `EnemyBehavior` (SO, `Create > GridBattle > AI > Enemy Behavior`): lista
  ordenada de `EnemyAction`. Pode ser compartilhado por vários inimigos.
  Sem nenhuma ação que funcione, o inimigo pula a vez. `EnemyConfig` sem
  `behavior` = inimigo que não age. Preparar um golpe que não deixa andar
  também encerra o andar do turno.
- `EnemyAction` (SO abstrato, `TryExecute(in EnemyTurnContext)`). Ações são
  **assets sem estado de runtime**: o estado de cada inimigo (recargas e
  contadores) vive na `EnemyMemory`, guardada pelo id da ação.
- Os inimigos são **aliados entre si** (`GetAllies`, em ordem de linha e coluna),
  mas os golpes deles atingem uns aos outros (`EveryoneButCaster`).
- As habilidades com recarga (`EnemyAbilityAction`) ficam bloqueadas quando um
  estado impede skills.

Ações disponíveis (`Create > GridBattle > AI > Actions`):

| Ação | Assets | Comportamento |
|---|---|---|
| `ApproachForSkillAction` (andar) | `ApproachMelee` (distância preferida 1), `ApproachMid` (2), `ApproachRanged` (3) | entre a própria célula e as células para onde pode andar, prefere, nesta ordem: célula **sem perigo** (não marcada por golpe de outro inimigo, `avoidMarkedCells`, e sem terreno de perigo que o afete, `avoidHazards`); célula **de onde uma skill pronta alcança o jogador** (`TryFindAimCell`); distância ao jogador (métrica de ataque) mais perto da preferida (ou simplesmente a menor, se de nenhum lugar alcança); ficar parado; menor linha e coluna. Só usa o andar se o inimigo se mover. Consulta as posições com `ProbePosition` |
| `UseSkillsAction` (agir) | `UseSkills` | prepara a primeira skill pronta que alcança o jogador, mirando onde cobre mais casas para onde ele pode fugir (ver [Skills](#uso-pelos-inimigos)) |
| `ApplyStateToTargetAction` (agir) | `ApplyWeakened` | com o jogador no alcance (3), a habilidade pronta (recarga 2) e o jogador sem o estado, anima o ataque e aplica Fraquejado (1 turno), na hora |
| `HealAllyAction` (agir) | `HealAlly` | cura 1 de vida o aliado mais ferido (vida abaixo de 99%) no alcance 3, sem contar a si mesmo; recarga 2; só usa o agir se curou algo |
| `BuffAllyAction` (agir) | `BuffAllyShield` | dá Escudo (2 turnos) ao aliado que ainda não o tem, no alcance 3, sem contar a si mesmo, escolhendo o mais próximo do jogador; recarga 2 |
| `SummonAction` (agir) | `SummonRat` | invoca um Rato na célula livre adjacente (raio 1) mais próxima do jogador, com a escala do invocador, se houver menos de 2 vivos e no máximo 4 por batalha; recarga 3; o invocado não age na rodada em que aparece e **não dá XP** |
| `BasicAttackAction`, `ChaseTargetAction`, `KeepDistanceAction` | `BasicAttack`, `ChaseTarget`, `KeepDistance`, `KeepDistanceBackline` | ataque básico, passo guloso e manter distância; continuam no projeto, mas **nenhum comportamento os usa** |

Comportamentos atuais (`Settings/AI`):

| Comportamento | Ações, em ordem | Usado por |
|---|---|---|
| `ChaseAndAttack` | `ApproachMelee` → `UseSkills` | Goblin, Rato, Slime, Morcego-olho, Goblin lanceiro, Morcego vampiro, Besouro aríete, Rato espinhoso, Goblin blindado, Slime regenerante, Golem de pedra, Ogro brutamontes, Cavaleiro esqueleto, Rei Goblin, Guarda Goblin |
| `Ranged` | `ApproachRanged` → `UseSkills` | Crânio de fogo, Goblin bombardeiro, Goblin artilheiro |
| `Skirmisher` | `ApproachMid` → `UseSkills` | Aranha tecelã, Goblin arpoador, Xamã do vento, Bruxa do pântano, Golem de lava |
| `Controller` | `ApplyWeakened` → `ApproachRanged` → `UseSkills` | Olho maldito |
| `Support` | `HealAlly` → `BuffAllyShield` → `ApproachRanged` → `UseSkills` | Xamã goblin |
| `Summoner` | `SummonRat` → `ApproachMid` → `UseSkills` | Rainha dos ratos |
| `Stationary` | `UseSkills` | Totem do raio |

Como a ação de apoio (enfraquecer, curar, proteger, invocar) vem antes de
`UseSkills` e usa o agir, um suporte que cura no turno não prepara golpe nesse
turno.

### Cooldown das habilidades

A `EnemyMemory` conta as recargas das ações de IA em **turnos do próprio
inimigo**, com a mesma regra das skills: usada no turno N, a habilidade volta no
turno N + recarga + 1 (recarga 0 = todo turno). O controller desconta ao fim de
cada turno do inimigo, pulando o que começou naquele turno. Recargas e
contadores (inclusive o total de invocações) entram na captura e na restauração
da batalha. As recargas das **skills** dos inimigos ficam em `SkillCooldowns`,
como as do jogador (ver [Cooldown](#cooldown)).

### Papéis

Cada inimigo tem um **papel** (`EnemyRoleDefinition`: ícone, nome, descrição,
fator de ameaça e se conta como linha de frente): corpo a corpo, enxame,
atirador, suporte, invocador e controlador (Mecânica 4). O papel aparece sobre o
inimigo, na janela de detalhes, no glossário e na prévia do nó, e entra na
composição das batalhas; **quem decide como o inimigo age é o `behavior`**, não
o papel.

| Papel | Linha de frente | Fator de ameaça |
|---|---|---|
| Corpo a corpo (`Melee`) | sim | 1 |
| Enxame (`Swarm`) | sim | 1 |
| Atirador (`Ranged`) | não | 1 |
| Suporte (`Support`) | não | 1 |
| Invocador (`Summoner`) | não | 1 |
| Controlador (`Controller`) | não | 1 |

> [!tip]
> A IA escolhe por um turno: não planeja rotas por pathfinding e mira a célula
> atual do jogador. Comportamentos novos entram como novas subclasses de
> `EnemyAction` combinadas em outros `EnemyBehavior`, sem tocar no
> `EnemyController`.

## Terreno

Cada célula pode ter um `TerrainDefinition` (`Create > GridBattle > Terrain >
Terrain Definition`), que é só dado:

| Campo | Significado |
|---|---|
| Tipo | `Obstacle` (célula bloqueada), `Hazard` (perigo) ou `Bonus` |
| Bloqueia movimento | ninguém anda para dentro, ocupa, nasce nem é teleportado para a célula; sempre ligado nos obstáculos |
| Bloqueia área de skill | as áreas de skill pulam a célula, então ninguém sobre ela é atingido (ligado só na Muralha de Gelo) |
| Bloqueia linha de visão | skills não podem ser miradas através da célula; só vale para terreno que bloqueia movimento (Rocha e Muralha de Gelo) |
| Gatilho | quando o efeito se aplica: ao entrar, no início ou no fim do turno de quem está sobre ela (`ETerrainTrigger`) |
| Dano | dano do tipo `Terrain` à entidade sobre a célula |
| Estados | estados aplicados à entidade, sem fonte |
| Afeta jogador, afeta inimigos | os dois ligados: o terreno afeta jogador e inimigos igualmente |
| Visual | sprite sobreposto, cor do sprite e tinta do piso (o tipo é legível antes de entrar) |

- **Obstáculos:** quem anda contorna (`HasWalkPath`; personagens também bloqueiam o caminho), e o gerador garante que
  nenhum conjunto de obstáculos gerados isole células do grid.
- **Efeitos** (`TerrainEffects`, ligados ao `GridController`): a cada
  `EntityTurnStartedEvent`, `EntityTurnEndedEvent` e `EntityEnteredCellEvent`, se a
  célula sob a entidade tem o gatilho correspondente e afeta o lado dela, aplica
  o dano (primeiro) e depois os estados. Levanta `TerrainTriggeredEvent`, pulsa a
  célula e toca o som de perigo ou de bônus. Jogador e inimigos usam o mesmo
  caminho. Hoje todo terreno usa o gatilho **fim do turno**.
- **Entrada forçada:** quem é empurrado ou puxado e para sobre perigo ou bônus
  recebe o efeito na hora, além do gatilho normal (veja
  [Empurrar e puxar](#empurrar-e-puxar-deslocamento)).
- **Terreno temporário** (2026-10-19): `GridController.SetTerrain(pos, terreno,
  rodadas)` põe um terreno por N **rodadas** (`Cell.TerrainTurnsLeft`; 0 =
  permanente). A contagem cai no fim de cada rodada, depois do último inimigo e
  antes da vez do jogador (`TickTemporaryTerrain`), e a célula volta a ser chão ao
  chegar a 0. Entra no salvamento (`TerrainCellSpec.TurnsLeft`). Um terreno de 2
  rodadas criado na vez do jogador dura a vez dos inimigos, a próxima vez do
  jogador e a vez seguinte dos inimigos.
- **Skills que criam terreno** (`CreateTerrainSkillEffect`): terreno, onde
  (`TargetCell`: a célula mirada; `AreaCells`: todas as células da área;
  `CasterCell`: a do lançador), rodadas (0 = o resto da batalha) e
  `replaceTerrain` (desligado: só em chão comum). Terreno que bloqueia movimento
  nunca vai para uma célula ocupada. Hoje: Fogo (Bola de Fogo, Bola de Fogo
  Explosiva, Bola de Fogo Arqueada, Meteoro Ardente, Erupção), Pântano Venenoso
  (Linha Venenosa e variantes, Maldição do Pântano) e **Muralha de Gelo** (Muralha
  de Gelo e variantes), todos por 2 rodadas, exceto a Muralha Duradoura (4).
  O Rastro de Chamas cria Fogo pelo mesmo caminho.
- **Muralha de Gelo** (`Settings/Terrain/IceWall`): obstáculo que bloqueia
  movimento, área de skill e linha de visão; só nasce de skills (não está nos
  pools do gerador). A skill põe a muralha nas células **vazias** da área (uma
  linha perpendicular de 3, ou 5 na Muralha Longa); os inimigos que estavam nas
  células da área levam 1 de dano e ficam Gelados, e a muralha não aparece embaixo
  deles. Ela não protege as células de trás: só tira da área de um golpe a
  própria célula, e impede mirar através dela.
- **Geração** (`TerrainGenerator` e `TerrainGenerationSettings`): determinística
  pela seed (um fluxo derivado da seed e do nó) e pela profundidade de
  balanceamento. Faixas (andar a partir do qual vale: obstáculos / perigos /
  bônus): 1: 0 / 0 / 0; 4: 0-2 / 0-1 / 0-1; 10: 1-2 / 0-2 / 0-1; 16: 1-3 / 1-3 /
  0-2; 22: 2-4 / 2-3 / 1-2. Cada tipo sorteia do seu pool (pesos iguais: Rocha;
  Fogo e Pântano Venenoso; Santuário de Cura e Pedra Protetora). **Segurança:** as
  células a distância (Chebyshev) 1 do ponto de partida do jogador nunca recebem
  terreno, e os obstáculos não podem dividir o grid (preenchimento por vizinhança
  ortogonal); depois de 20 tentativas sem um layout inteiro, a batalha fica sem
  obstáculos. Os inimigos não começam sobre células bloqueadas nem de perigo.
- **Interface:** a prévia do nó mostra o terreno (mini-grid colorido por tipo e
  legenda), e a janela de detalhes mostra o terreno da célula, mesmo vazia.

Terrenos atuais (`Settings/Terrain`; todos com gatilho no fim do turno de quem
está sobre ele):

| Terreno | Tipo | Efeito |
|---|---|---|
| Rocha | obstáculo | bloqueia a célula e a visão |
| Fogo | perigo | 1 de dano |
| Pântano Venenoso | perigo | aplica Envenenado por 2 turnos |
| Santuário de Cura | bônus | aplica Regeneração por 3 turnos |
| Pedra Protetora | bônus | aplica Escudo por 2 turnos |
| Muralha de Gelo | obstáculo (só de skills) | bloqueia a célula, a visão e a área de skill |

## Morte e fim da batalha

A lógica é genérica em `Character` (`Die()`, `protected virtual`):

1. HP chega a 0 em `ApplyHit` → `IsDead` → `Die()`.
2. `Die()`:
   - inimigo: o golpe preparado some (`ETelegraphChange.Cancelled`, sem recarga);
   - desocupa a célula;
   - levanta `CharacterDiedEvent(this)`;
   - inimigo: efeito curto de flash e fade (~150 ms) e então `Destroy`; jogador:
     `Destroy` imediato.
3. Reações assinam `CharacterDiedEvent`:
   - **`BattleController`:** decide a vitória ou a derrota (ver [Fim da
     batalha](#fim-da-batalha));
   - **`XpRewardSystem`:** se morreu um inimigo, concede o XP dele (ver [XP e
     nível](#xp-nível-e-talentos)); qualquer morte de inimigo conta, inclusive por
     veneno, espinhos, terreno, colisão ou golpe de outro inimigo;
   - **jogador morto** → `RunManager` encerra a run (derrota), apaga o
     salvamento da run e a tela de fim de run mostra o resumo da build, o nível e
     a profundidade alcançados, de onde se volta ao menu;
   - **último inimigo morto** → `RunManager` guarda o estado do jogador (vida ao
     menos 1, nível, XP, estados permanentes), conta a batalha vencida e abre o
     mapa, a recompensa de elite (ver [XP, nível e talentos](#xp-nível-e-talentos))
     ou termina a run em vitória, se foi o chefe.
- `ReceiveDamage` é **idempotente pós-morte**: personagem morto não toma dano
  adicional, não usa skill (`TryUseSkill` guarda), não age no turno e não é
  curado.
- **Desistir** (menu de pausa) encerra a run pelo `RunManager` e, por padrão,
  conta como derrota no registro (`MetaSettings.giveUpCountsAsDefeat`).

## XP, nível e talentos

O `PlayerCharacter` acumula XP ao longo da run; nível, XP e o bônus de vida
máxima persistem entre as batalhas (`RunState.Player`).

### Fluxo

A regra (quanto XP, para quem, em quantos pacotes) e a apresentação (orbs
voando até a barra) são componentes separados, cada um no seu GameObject.
Nenhum deles vive no Grid.

```
inimigo morre (Character.Die)
  → desocupa a célula
  → CharacterDiedEvent
      → XpRewardSystem (GameObject próprio)          ← regra
          → lê Enemy.XpReward (a parte do XP da batalha que o gerador deu a ele; 0 em invocados)
          → resolve o PlayerCharacter atual
          → XpPacket.Split: N = teto(XP / xpPerPacket) pacotes, no máximo maxPackets (8),
            cada um com o progresso da barra após creditá-lo
          → se o XP vai subir de nível, ou é o do último inimigo vivo:
            TurnBlockers.Acquire("XP delivery")     ← segura o fluxo do turno
          → EventBus.Raise(XpRewardDroppedEvent { Origin, Packets, Collect })
              → XpOrbsVfx (no GameObject GameScreenLayer)  ← apresentação
                  → MarkPresented(): assume a entrega
                  → quando o golpe chega, todos os pacotes viram orbs (UI Toolkit)
                    juntos na posição do inimigo e se espalham numa mini explosão
                    (LitMotion), param um instante e voam juntos em arco até o
                    ponto da barra (XpBarView.TryGetTrack)
                  → ao chegar: remove o orb e chama Collect(pacote)
          → se ninguém assumiu a entrega: Collect de todos na hora
  Collect(pacote) → PlayerCharacter.GainXp(pacote.Amount)
      → se cruzou o limiar: nível sobe e a vida máxima ganha 1 por nível (sem curar)
      → PlayerXpChangedEvent { Level, CurrentXp, XpToNextLevel }
          → GameScreenController.OnXpChanged → XpBarView.SetState
      → PlayerLeveledUpEvent (um por nível ganho)
          → TalentService: abre a escolha de talento (jogo pausado)
  quando o último pacote chega: o bloqueio "XP delivery" é liberado
```

- **O XP só entra na conta quando os orbs chegam à barra**: a barra cresce orb
  a orb. A tela de escolha de talento só abre quando o orb leva a barra ao
  limiar (o evento de nível vem depois do `PlayerXpChangedEvent`, então a barra
  já mostra o nível novo). Entre o impacto do golpe e o XP efetivo há no máximo
  ~0,81 s, qualquer que seja a quantidade de XP (decisão de 2026-10-20: antes,
  muito XP em nível alto virava uma fila de orbs que segurava o jogo).
- **Explosão** (estilo *Vampire Survivors*, `XpOrbsVfx`): todos os orbs de um
  abate aparecem juntos no ponto da morte, escondidos até o impacto
  (`GetPendingImpactDelay`); espalham-se em direções distribuídas por igual a
  partir de um ângulo sorteado (`UnityEngine.Random`, só visual), com raio de
  14 ± 6 px, em 0,22 s (`OutCubic`); param 0,08 s; e voam juntos até a barra em
  0,45 s, cada um com até 0,06 s de atraso. As posições são arredondadas para
  pixels inteiros do painel.
- Divisão dos pacotes (`XpPacket.Split`): `N = ceil(XP / xpPerPacket)` (5, no
  `XpRewardSystem` da cena), limitado a `maxPackets` (8, padrão do código): acima
  do limite os pacotes ficam maiores. O resto da divisão é distribuído 1 a 1 nos
  primeiros pacotes (12 XP → 3 pacotes de 4; 15 XP → 3 pacotes de 5; 100 XP → 8
  pacotes, quatro de 13 e quatro de 12). O jogador nunca perde XP no
  arredondamento, e nenhum pacote fica com 0.
- Os orbs são elementos da **UI** (UI Toolkit, posicionados em absoluto na raiz do
  painel da GameScreen), convertidos do espaço do mundo por
  `RuntimePanelUtils.CameraTransformWorldToPanel`. Esperam o impacto visual do
  golpe que matou (`GridController.GetPendingImpactDelay`).
- Ajustes visuais ficam no asset `XpOrbsVfxSettings` e podem ser alterados em
  Play Mode: sprite, tamanho, arco, a explosão (`burstRadius` 14,
  `burstRadiusVariance` 6, `burstAngleJitter` 0,5, `burstDuration` 0,22,
  `burstEase` `OutCubic`), a pausa (`holdDuration` 0,08), o voo
  (`flightDuration` 0,45, `flightEase`) e a variação de partida
  (`launchJitter` 0,06). O antigo `staggerDelay` saiu.
- Som: continua um som de XP por abate, na hora da morte (`AudioEventHooks`), e
  não um por orb.
- `XpBarView` é o único lugar que conhece os elementos e as medidas da barra
  (`xp-bar-progress`, `xp-bar-detail-2`, `current-level`, 110 px / 112 px).
- Gameplay não depende da UI: sem apresentação disponível, o `XpRewardSystem`
  credita o XP na hora. A comunicação **jogo → UI é sempre via EventBus**. Um
  `PlayerCharacter` recém-criado emite `PlayerXpChangedEvent`, então a barra
  começa cada batalha com o estado da run.
- O inimigo morto já desocupou a célula antes de o XP ser concedido, e jogador
  morto não ganha XP.

### Curva e nível máximo

Configurados **globalmente** em `ProgressionSettings` (`Settings/Progression`),
iguais para todas as classes:

- `baseXpToLevelUp` = **150**: XP para ir do nível 1 ao 2.
- `xpToLevelUpGrowthPerLevel` = **100**: acréscimo a cada nível atingido.
- `maxLevel` = **12**.
- `carryOverLeftoverXp` = ligado: a sobra de XP passa para o limiar seguinte
  (com subidas de nível em cadeia quando um abate valioso fecha mais de uma conta).
- `maxHpPerLevel` = **1**: cada nível acima do 1 soma 1 de vida máxima
  (`GetMaxHpBonus(nível) = (nível − 1) × 1`), **sem curar** (a vida atual não
  muda).
- `levelUpHealFraction` = **0**: subir de nível não cura (decisão de 2026-10-19;
  o campo continua para quem quiser a cura de volta).

Fórmula: `XpToNextLevel(nível) = base + (nível − 1) × acréscimo`.

| Nível atual | XP para o próximo |
|---|---|
| 1 | 150 |
| 2 | 250 |
| 3 | 350 |
| 4 | 450 |
| 11 | 1150 |

São 7150 XP do nível 1 ao 12. No nível máximo o XP não gera mais níveis (e
portanto mais talentos) e a barra fica cheia. No simulador de XP de 2026-10-19,
com as três dificuldades antigas, o nível final médio foi 9,5, 9,9 e 10,6 nas
rotas fácil, normal e difícil (cerca de 10 níveis por run, como pede o [[GDD]]);
com as rotas Normal e Elite de 2026-10-20 ainda falta medir o nível final.

### XP por inimigo

O XP vem da batalha, não de cada inimigo (`BattleGenerationSettings.xpSource =
ThreatBudget`). O gerador calcula o XP total pelo orçamento de XP da profundidade
`p` e da dificuldade:

`XP = (xpBaseBudget 6 + 0,52 × (p − 1)) × multiplicador de orçamento do tipo
de batalha × xpPerThreat 6,9 × (1 + 0,07 × (p − 1)) × multiplicador de XP do
tipo de batalha × fator do tamanho da run`, arredondado,

e o divide entre os inimigos na proporção da ameaça de cada um (as sobras do
arredondamento vão para as maiores frações). Desde 2026-10-20 as batalhas são
só **normal** ou **de elite** (`EBattleDifficulty`). Multiplicadores (orçamento /
força / XP): normal 1 / 1 / 1, elite 1,35 / 1 / 2, então a batalha de elite dá
cerca de 2,7 vezes o XP da normal no mesmo andar. No simulador de XP (`GridBattle
> Talents > Simulate XP Progression`), o XP médio por batalha vai de 41 (andar 1)
a 420 (andar 29) na normal e de 170 (andar 4, a primeira elite) a 1134 (andar 29)
na elite. O elite não muda o total: só fica com uma parte maior, pela ameaça.
Invocados não dão XP. As fontes antigas continuam como opção: `EnemyReward`
(`xpReward` do config × o crescimento por profundidade) e `EnemyThreat` (ameaça
de cada inimigo × 6,9). O XP **não aparece no mapa** (decisão de 2026-10-20): só
o ícone de elite marca a batalha que dá mais.

**Profundidade de balanceamento.** Os valores por profundidade dos assets de
balanceamento (orçamento, escala, pool de inimigos, XP, faixas de terreno) são
escritos para um mapa de `balanceFloorCount` andares (30). Num mapa de
`floorCount` andares, o andar `d` é lido como `1 + (d − 1) × (30 − 1) / (floorCount − 1)`
(a pool e as faixas de terreno usam o andar inteiro mais próximo), e o XP de cada
batalha é multiplicado por `(30 − 1) / (floorCount − 1)`. Hoje `floorCount` também
é 30, então os fatores são 1.

### Talentos oferecidos ao subir de nível

Cada nível ganho abre uma oferta (`TalentService`, `TalentSession`); vários níveis
de uma vez geram ofertas em sequência, uma por nível. O talento escolhido vale já
para o restante do turno.

- **Pausa:** enquanto a oferta está aberta o jogo fica pausado (`TurnBlockers`,
  "Talent choice"; a música continua) e só a tela de escolha responde. As ofertas
  pendentes e as opções sorteadas entram no salvamento da run: reabrir o app
  mostra a mesma oferta. Uma oferta causada pelo último abate do chefe final é
  pulada (a run está acabando).
- **Pool e filtros:** a pool é a da classe (`talentPool`) mais a compartilhada
  (`TalentOfferSettings.sharedPool`). Entra na oferta o talento que a classe
  pode pegar, com nível mínimo atingido (o nível que acabou de ser alcançado),
  pré-requisitos cumpridos, posto abaixo do máximo, não banido, e, se libera skill,
  com espaço nos 6 slots, abaixo do limite de 3 talentos de skill e sem a skill já
  liberada (uma variante conta como a mesma skill).
- **Opções:** 3 por oferta (`optionsPerOffer`), até 5 com o talento Mais Opções.
- **Sorteio:** ponderado por sinergia, sem repetição:
  `peso = peso base × (1 + 0,5 × tags em comum com os talentos já escolhidos)`;
  o talento que só chega à classe pelo pool compartilhado tem o peso multiplicado
  por `sharedPoolWeightMultiplier` (0,5). Determinístico: depende só da seed da
  run, do nível e do reroll.
- **Ferramentas do jogador** por run: **2 rerolls**, **1 banimento** e **1 pulo**
  (mais os de talentos). Rerrolar sorteia de novo, preferindo talentos que não
  estão na tela. Banir tira o talento da run e sorteia outro para a mesma
  posição. Pular perde o talento daquele nível. Cada uso mostra um anúncio
  recompensado (ver [[GDD]] 7).
- **Efeito imediato:** o talento sobe 1 posto, seus estados vão para o jogador
  (permanentes, com o id do talento como fonte e uma pilha por posto), a skill que ele
  libera entra na barra, e um bônus de vida máxima recupera a mesma vida
  (`maxHpGainHealsSameAmount`; só a Vitalidade dá vida máxima).
- **Nó de talento:** funciona como um nível a mais (a oferta usa o nível do jogador
  mais 1, `talentNodeLevelBonus`), não concede XP e **custa 2 de vida máxima**
  para o resto da run (`RunSettings.talentNodeCost = MaxHp`,
  `talentNodeCostMaxHp` 2; nunca deixa a vida máxima abaixo de 1). O custo
  entra em `RunState.Player.MaxHpBonus` e a vida atual desce junto se ficar
  acima do novo máximo.

### Talentos de regra

Decisão de 2026-10-19 ([[talentos_e_oferta]]): os talentos que só somavam números
saíram (48 talentos) e entraram talentos que **mudam regras**. Os efeitos ficam
em `States/Effects/RuleTalentEffects.cs` e nas passivas:

| Talento | O que muda | Onde |
|---|---|---|
| Dominó, Rastro de Chamas, Gelo Escorregadio | empurrões e puxões | [Talentos que mudam o deslocamento](#talentos-que-mudam-o-deslocamento) |
| Passo Trocado | o andar pode trocar de lugar com um vizinho | [Trocar de lugar](#trocar-de-lugar) |
| Contragolpe | `CounterstrikeEffect`: quando um golpe anunciado que mirava o jogador (o jogador estava na área ao preparar) dispara sem atingi-lo, quem lançou fica Exposto por 2 turnos | `TelegraphResolvedEvent` |
| Ímpeto | matar depois de andar devolve o andar do turno | [Ações do jogador](#ações-do-jogador-e-entrada) |
| Colheita de Almas, Sede de Sangue, Banquete das Sombras | ao matar: recargas −1, ou cura 1 | `OnKillEffect` |
| Lâminas Envenenadas | as skills envenenam (Envenenado, 2 turnos) quem atingem | `ApplyStateOnHitEffect` |

Os talentos atuais são **provisórios** (ver [Valores atuais](#talentos)). O Ímã
do design (as skills que empurram podem puxar, e o contrário) **ainda não está
implementado**.

### Acampamento e elites

Mudanças de 2026-10-19 que vivem fora da batalha, mas mexem nas skills
([[mapa_e_nos]]):

- **Acampamento** (o nó de cura, `RunManager.ResolveHeal`): com
  `campOffersSkillUpgrade` ligado e alguma skill que ainda pode melhorar, o nó
  abre a escolha (`ERunPhase.Camp`, `CampEnteredEvent`): **Descansar** cura 40% da
  vida máxima (`RunSettings.healFraction` 0,4; `CampRest`) ou **Treinar** abre a
  melhoria de uma skill (`CampTrain`, `SkillUpgradeOfferEvent` com origem Camp).
  Sem skill para melhorar, o nó só cura os mesmos 40%.
- **Batalha normal ou de elite** (decisão de 2026-10-20, no lugar de fácil,
  normal e difícil): `EBattleDifficulty { Normal = 1, Elite = 2 }` (os números
  antigos ficaram: um save com Difícil carrega como Elite, e
  `MapRules.NormalizeDifficulties` transforma Fácil, e Difícil sem elite, em
  Normal ao retomar a run). O mapa sorteia normal 75 e elite 25
  (`MapGenerationSettings.normalWeight`/`eliteWeight`), mas um nó só vira elite
  se o gerador tem uma elite disponível na profundidade de balanceamento dele
  (`BattleGenerationSettings.HasEliteAt`); os andares 1 a 3 não têm elite (o
  Ogro brutamontes entra no 4). Numa escolha em que todas as batalhas são do
  mesmo tipo, uma delas vira o outro tipo quando dá.
- **Elite** (`BattleGenerator`): toda batalha de elite tem um inimigo único. O
  gerador sorteia, pelo peso, um `EnemyConfig` com `isUnique` do pool
  (`EnemyPoolEntry.IsEliteAt`, a mesma regra do mapa); ele ocupa uma vaga, gasta
  a sua ameaça do orçamento (×1,35), reduz em 1 o mínimo de inimigos e conta
  como linha de frente se o papel contar. Inimigos únicos nunca são sorteados
  como comuns. O spec fica com `IsElite`, e `MapRules.IsEliteBattle(nó)` é a
  única fonte do ícone, do título, da recompensa e do fluxo.
- **Recompensa de elite** (`RunManager.BeginEliteReward`/`ContinueEliteReward`):
  vencer uma batalha de elite dá, nesta ordem, mais XP (o dobro do multiplicador
  e o orçamento maior, acima), `RunSettings.eliteConsumableOffers` (1) ofertas de
  consumível (a mesma do nó de consumível: escolher 1 entre
  `ConsumableSettings.choiceCount`; inventário cheio segue a política de
  sempre; a janela tem o título "Recompensa da elite",
  `consumable_offer.elite_title`) e uma melhoria de skill (origem Elite, com
  `eliteGrantsSkillUpgrade` ligado e alguma skill que ainda melhora); recusar a
  oferta ou a melhoria abre mão só dela. O nó termina depois da última etapa, com
  a variação de vida da batalha. Cada etapa é salva (`RunState.EliteRewardOffer`,
  `EliteOffersLeft`), então recarregar retoma a recompensa. A oferta da elite
  sorteia do fluxo Consumables, como o nó de consumível.

## Highlights visuais

Os highlights são recalculados **por evento** (`PlayerCharacterController`): tudo
que pode mudá-los (movimento, morte, invocação, estados, seleção de skill ou
item, mira, recargas, início de turno global, batalha decidida) marca-os como
sujos, e eles são refeitos no máximo uma vez por quadro, e só enquanto há algo
mudado. Só aparecem na vez do jogador, exceto as marcas dos golpes anunciados.

| Highlight | Condição | Cor |
|---|---|---|
| **Walk** (azul) | célula livre em alcance de movimento; com Passo Trocado, também os vizinhos com quem dá para trocar | `walkHighlightColor` (prefab Cell) |
| **Attack** (vermelho) | alvo do ataque básico em alcance; hoje nunca aparece | `attackHighlightColor` (prefab Cell) |
| **SkillRange** (roxo) | com uma skill ou um item com alvo selecionado: as células onde ele pode ser mirado | `skillRangeHighlightColor` |
| **SkillArea** (rosa) | a área da ação mirada, antes de confirmar | `skillAreaHighlightColor` |
| **Danger** (laranja) | depois de um toque (ou toque longo) num inimigo: as células para onde ele pode andar (até o próximo toque) | `dangerHighlightColor` |
| **Marca de golpe** (amarela pulsante; vermelha onde atinge alguém) | as células de cada golpe anunciado, o tempo todo, por cima dos outros | `telegraphHighlightColor`, `telegraphHitColor` |

- Com uma skill ou item com alvo selecionado, só o `SkillRange` (e a `SkillArea`
  da mira) aparece.
- Depois de andar somem os `Walk`.
- Highlights que aparecem juntos entram em fade escalonado (visual).
- Quando uma skill ou uma bomba é usada, as células da área piscam
  (`skillAreaFlashColor`).
- Cores e alpha ficam no prefab `Cell.prefab` (alpha > 0 obrigatório); as cores
  novas usam o padrão do código.
- A marca de golpe e os números da previsão são desenhados pela `ForecastView`
  (ver [Previsão do resultado](#previsão-do-resultado)). É a única exceção à regra
  de que inimigos não pintam o grid.

## Pontos de extensão

| Extensão | Onde plugar |
|---|---|
| **Skill nova (jogador)** | asset `SkillDefinition` (ou subclasse para regra especial) + talento que a libera (`TalentDefinition.unlockedSkill`) ou skill inicial no `CharacterConfig.skills`; crie também as duas variantes (`upgrades`, com `upgradeOf` apontando a original) |
| **Golpe de inimigo** | `SkillDefinition` com `telegraphed` ligado, `Affects = EveryoneButCaster` e os campos de telegrafia; na lista `skills` do `EnemyConfig`, que precisa ter um golpe de recarga 0 (o básico) e um de recarga maior que 0 (o especial, primeiro na lista) |
| **Skill canalizada (jogador)** | `usesWalk` ligado na skill; o contra de não andar no turno seguinte é um `ApplyStatesSkillEffect` no lançador com Exausto por 2 |
| **Efeito novo de skill** | subclasse de `SkillEffect` (ela aparece no seletor do Inspector); para entrar na previsão, trate o efeito em `ActionForecast` |
| **Skill que empurra, puxa, troca, avança ou cria terreno** | efeitos `Displace`, `Swap`, `DashToTarget` (antes do `Displace`) e `CreateTerrain` na lista de efeitos da skill; valores da colisão em `CombatSettings`; personagem imóvel: `canBeDisplaced` desligado no config |
| **Novo inimigo** | asset `EnemyConfig` (sprite, animator, atributos, `role`, `behavior`, `threat`, skills, `initialStates`, `prefab = Enemy.prefab`) + entrada no pool de `BattleGenerationSettings`, sem prefab novo; elite: `isUnique` |
| **Novo papel de inimigo** | asset `EnemyRoleDefinition` (ícone, nome, fator de ameaça, linha de frente) |
| **Novo personagem jogável** | asset `PlayerCharacterConfig` (`prefab = PlayerCharacter.prefab`, `battlesToUnlock`, `talentPool`, 1 skill inicial) + entrada em `GameStateManager.playableCharacters` |
| **Comportamento de inimigo novo** | subclasse de `EnemyAction` (de `EnemyAbilityAction` se tiver recarga; `IsMovement` para andar) + asset; combinar ações num `EnemyBehavior` e referenciar no `EnemyConfig` |
| **Estado novo** | asset `StateDefinition` com efeitos; efeito novo = subclasse de `StateEffect` |
| **Talento de regra** | estado com um efeito de `RuleTalentEffects.cs` (ou um efeito novo) concedido por um talento |
| **Talento que modifica uma skill** | estado com `SkillModifierEffect` concedido por um talento com `maxRank` (nenhum hoje; a melhoria por variante é o caminho atual) |
| **Passiva condicional** | estado com `ConditionalDamageEffect` (condição, tipos de dano, fixo e percentual), `OnKillEffect` ou `ApplyStateOnHitEffect`; condição nova = valor em `EDamageCondition` e um caso em `ConditionalDamageEffect.Evaluate` |
| **Talento novo** | asset `TalentDefinition` (classes, nível, pré-requisitos, tags de sinergia, estados, skill) + entrada na `talentPool` da classe ou no `sharedPool` do `TalentOfferSettings` |
| **Consumível novo** | asset `ConsumableDefinition` + entrada no pool do `ConsumableSettings`; efeito novo = subclasse de `ConsumableEffect` |
| **Terreno novo** | asset `TerrainDefinition` + entrada no pool do `TerrainGenerationSettings` (ou só como terreno criado por skill) |
| **Quantidade de terreno por profundidade** | faixas do `TerrainGenerationSettings` |
| **Mapa e batalhas** | `MapGenerationSettings` (andares, andares de referência do balanceamento, nós, tipos, pesos de normal e elite), `BattleGenerationSettings` (pool por profundidade, orçamento de ameaça, escala, XP, limites, perfis normal e elite, chefe) e `RunSettings` (cura, acampamento, custo do nó de talento, recompensa de elite: ofertas de consumível e melhoria, estados entre batalhas) |
| **Fórmulas de dano** | `CombatSettings` (defesa fixa ou percentual, dano mínimo, arredondamento, teto do crítico, o que ignora defesa, colisão, se disparar um golpe usa o agir, linha de visão, métricas de distância) |
| **Hook do jogador na run** (talentos que mexem na vida máxima ou em estados) | `IRunPlayerModifier` registrado em `RunPlayerHooks` |
| **Segurar o fluxo do turno** (janela, escolha, animação longa) | `TurnBlockers.Acquire(motivo)` e descartar o handle ao terminar; a entrada do jogo fica bloqueada junto |
| **Facções/alianças** | `GridRules.AreAllies` e o filtro `Affects` das skills |
| **Distância em diagonais** | trocar a métrica em `CombatSettings` (Chebyshev) |
| **Previsão** | `ActionForecast`, `ThreatForecast`; aparência em `ForecastSettings` |
| **Reações visuais** (dano, ataque, morte) | `CharacterView` e `GridEffectsAnimator` |
| **Animações compartilhadas** | `AnimatorOverrideController` no `animatorController` do config (sobre um controller base comum) |
| **Ritmo e animação dos turnos** | `TurnManager.enemyStagger` e `GridMovementSettings`; animações que devam segurar o turno entram no esquema de espera do `GridController.WaitForMovementsAsync` |
| **Curva de XP, nível máximo e vida por nível** | `ProgressionSettings` |
| **Oferta de talentos** (opções, rerolls, banimentos, pesos) | `TalentOfferSettings` |
| **Efeitos de nível** | assinar `PlayerLeveledUpEvent` |
| **XP por fonte extra** (itens, eventos) | chamar `PlayerCharacter.GainXp`; para entrega animada, emitir `XpRewardDroppedEvent` com os pacotes |
| **Ajustar o VFX de XP** | asset `XpOrbsVfxSettings` (explosão, pausa, voo); XP por orb em `XpRewardSystem.xpPerPacket` e o limite de orbs por abate em `XpRewardSystem.maxPackets` |
| **Dicas de tutorial** | asset `TutorialTipDefinition` + gatilho em `TutorialService.Notify` |
| **Áudio** | clipes no `AudioLibrary` (um cue por `ESfx`, uma faixa por `EMusicContext`) |
| **Status/DoTs** (veneno etc.) | estados com `PeriodicDamageEffect`, que rodam nos turnos do portador |

## Valores atuais dos assets

Espelho de `Assets/Application/Settings/` depois da migração de 2026-10-19 e do
ajuste pelo simulador (`d481779`). **Todos os valores de conteúdo são
provisórios** e o balanceamento está pausado por decisão do Luiz; a lista das
escolhas para as perguntas em aberto está em [[valores_padrao_em_aberto]]. Os
nomes são os da tabela `Content` em pt-BR.

> [!note] Números pequenos
> Jogadores com 14 a 18 de vida e andar 2; inimigos comuns com 2 a 5 de vida,
> elites 7 a 8 e o chefe 22; golpes de 1 a 3; colisão 1. Ninguém tem ataque
> básico, crítico ou defesa base (os campos `basicAttackDamage` e
> `attackDistance` continuam nos assets, sem uso).

### Jogadores

`PlayerCharacterConfig` em `Characters/Players`. Andar 2 para todos; sem ataque
básico; crítico 0, multiplicador de crítico 2, defesa 0, penetração 0 e
modificadores de skill 0.

| Personagem | Classe | Vida | Skills iniciais | Liberação (batalhas vencidas) | Talentos da classe |
|---|---|---|---|---|---|
| Knight | Guerreiro (Warrior) | 18 | Golpe | 0 (desde o início) | Treino de Investida, Escudo Pesado, Força Bruta, Muralha, Passo Pesado, Dominó, Estilhaçar, Sede de Sangue |
| Mage | Mago (Mage) | 14 | Rajada de Vento | 15 | Toque Gélido, Piromancia, Conjurador de Tempestades, Grimório, Criomancia, Gelo Escorregadio, Rastro de Chamas, Foco Arcano, Quebradiço, Colheita de Almas |
| Rogue | Ladino (Rogue) | 15 | Adaga Envenenada | 30 | Gancho de Caça, Caminho das Sombras, Arsenal Oculto, Mestre das Sombras, Mestre do Veneno, Passo Trocado, Lâminas Envenenadas, Predador, Assassino, Banquete das Sombras |

Cada classe começa só com a skill **sem recarga** (Golpe, Rajada de Vento, Adaga
Envenenada), para sempre ter o que fazer. As que eram iniciais vêm de talentos de
nível 2: Investida com Escudo (Treino de Investida), Golpe de Escudo (Escudo
Pesado), Nova de Gelo (Toque Gélido), Bola de Fogo (Piromancia), Gancho (Gancho de
Caça) e Passo das Sombras (Caminho das Sombras). O Mago tem duas skills canalizadas
(Meteoro e Tempestade Elétrica, com as variantes), liberadas por talento. A vida máxima cresce 1 por nível (até
+11 no nível 12) e cai 2 a cada nó de talento.

### Inimigos

`EnemyConfig` em `Characters/Enemies`. Todos sem ataque básico, crítico 0 e
defesa base 0; cada um tem um golpe especial (com recarga, primeiro na lista) e
um básico (sem recarga), todos anunciados; o Rei Goblin tem dois especiais
(ver [Skills](#skills-62)). A coluna Skills está na ordem da lista (a ordem em
que a IA tenta). "Andar mín." é o andar (de 30) a partir do qual
entra no pool; peso 1, exceto onde indicado. A ameaça é a escrita no config
(`threat`) e decide o orçamento e a parte do XP. Os inimigos novos têm arte
própria (PixelLab). Nove ainda reaproveitam um sprite do `Characters.png` com
`tint` (provisórios): Goblin blindado, Guarda Goblin, Goblin arpoador, Rei Goblin,
Xamã goblin, Olho maldito, Rainha dos ratos, Slime regenerante e Rato espinhoso.

**Inimigos comuns**

| Inimigo | Papel | Comportamento | Vida | Andar | Skill | Ameaça | Andar mín. | Observação |
|---|---|---|---|---|---|---|---|---|
| Goblin | corpo a corpo | `ChaseAndAttack` | 3 | 2 | Investida, Porretada | 1 | 1 | |
| Rato | enxame | `ChaseAndAttack` | 2 | 3 | Mordida Pestilenta, Mordida | 0,7 | 1 | também é o invocado da Rainha dos ratos |
| Slime | corpo a corpo | `ChaseAndAttack` | 4 | 1 | Cuspe Ácido, Respingo Ácido | 1,2 | 1 | |
| Crânio de fogo | atirador | `Ranged` | 2 | 2 | Explosão de Chamas, Seta Flamejante | 1,2 | 1 | |
| Morcego-olho | enxame | `ChaseAndAttack` | 2 | 3 | Grito Sônico, Rasante | 0,8 | 1 | o Rasante só na diagonal |
| Goblin lanceiro | corpo a corpo | `ChaseAndAttack` | 3 | 2 | Dardo, Estocada | 1,3 | 3 | |
| Aranha tecelã | controlador | `Skirmisher` | 2 | 3 | Teia, Mordida | 1,4 | 4 | |
| Morcego vampiro | enxame | `ChaseAndAttack` | 2 | 3 | Grito Sônico, Mordida Vampírica | 1,2 | 5 | |
| Goblin bombardeiro | atirador | `Ranged` | 3 | 1 | Chuva de fogo, Estilingada | 1,8 | 6 | |
| Besouro aríete | controlador | `ChaseAndAttack` | 4 | 2 | Investida, Chifrada | 1,5 | 6 | |
| Xamã goblin | suporte | `Support` | 3 | 2 | Arco Elétrico, Faísca | 1,8 | 7 | peso 0,8; cura 1 e dá Escudo aos aliados |
| Rato espinhoso | corpo a corpo | `ChaseAndAttack` | 3 | 2 | Rajada de Espinhos, Mordida | 1,3 | 7 | Armadura de Espinhos (permanente) |
| Goblin arpoador | controlador | `Skirmisher` | 3 | 2 | Gancho, Estilingada | 1,5 | 8 | |
| Totem do raio | atirador | `Stationary` | 3 | 0 | Descarga, Choque Estático | 1,5 | 9 | nunca anda |
| Goblin blindado | corpo a corpo | `ChaseAndAttack` | 4 | 2 | Investida, Empurrão de Escudo | 1,6 | 10 | Blindado (permanente) |
| Slime regenerante | corpo a corpo | `ChaseAndAttack` | 4 | 1 | Cuspe Ácido, Respingo Ácido | 1,6 | 10 | Regeneração (permanente) |
| Xamã do vento | controlador | `Skirmisher` | 3 | 2 | Tornado, Lufada | 1,6 | 11 | |
| Goblin artilheiro | atirador | `Ranged` | 2 | 1 | Morteiro, Estilingada | 1,6 | 12 | |
| Olho maldito | controlador | `Controller` | 3 | 2 | Olhar Ofuscante, Olhar Maldito | 1,6 | 13 | enfraquece (Fraquejado) de longe |
| Bruxa do pântano | controlador | `Skirmisher` | 3 | 2 | Maldição do Pântano, Bola de Lama | 1,8 | 14 | |
| Golem de pedra | corpo a corpo | `ChaseAndAttack` | 5 | 1 | Tremor, Soco de Pedra | 2,5 | 15 | Blindado (permanente) |
| Rainha dos ratos | invocador | `Summoner` | 4 | 1 | Mordida Pestilenta, Guincho | 2 | 16 | peso 0,8; invoca Ratos |

**Elites** (`isUnique`: só aparecem como o elite das batalhas de elite)

| Inimigo | Papel | Comportamento | Vida | Andar | Skills | Ameaça | Andar mín. | Observação |
|---|---|---|---|---|---|---|---|---|
| Ogro brutamontes | corpo a corpo | `ChaseAndAttack` | 7 | 1 | Pancada esmagadora, Soco de Ogro | 3,5 | 4 | |
| Cavaleiro esqueleto | corpo a corpo | `ChaseAndAttack` | 7 | 2 | Investida Sombria, Varredura Óssea | 4 | 10 | |
| Golem de lava | atirador | `Skirmisher` | 8 | 1 | Erupção, Punho de Magma | 4,5 | 18 | Armadura de Espinhos (permanente); imune a Queimando |

**Chefe final e escolta (provisórios)**

| Inimigo | Papel | Vida | Andar | Skills | Ameaça | Observação |
|---|---|---|---|---|---|---|
| Rei Goblin | corpo a corpo, chefe | 22 | 1 | Martelada, Onda de Choque, Golpe Real | 10 | `ChaseAndAttack`; nó final; **imóvel**; imune a Atordoado (não a Congelado nem a Cego) |
| Guarda Goblin | corpo a corpo | 5 | 2 | Investida, Empurrão de Escudo | 2 | Blindado; só na escolta do chefe |

O encontro do chefe (`bossEncounter`) é fixo: Rei Goblin, Guarda Goblin, Xamã
goblin, Goblin artilheiro e Goblin lanceiro. Os golpes do rei atingem também a
escolta (`EveryoneButCaster`).

**Geração das batalhas** (`BattleGenerationSettings`): grid 6×6, jogador em
(2, 2), inimigos a pelo menos 2 casas dele; orçamento de ameaça por andar 1: 2,
5: 3,5, 10: 4,5, 15: 5,5, 20: 6,5, 25: 7,5 e 30: 8,5 (linear entre os pontos;
tolerância 0,3), multiplicado pelo tipo de batalha (normal 1, elite 1,35); de 2 a 6 inimigos, ocupação máxima de 40% do grid, ao menos um de linha de
frente, no máximo 1 suporte e 1 invocador; composição: a partir do andar 8, ao
menos 2 papéis diferentes; do 15, 2 papéis e pesos por papel (corpo a corpo
×0,7, enxame ×0,8, atirador ×1,6, suporte ×1,5, controlador ×1,4, invocador
×1,4); do 22, 3 papéis (×0,6, ×0,8, ×2, ×1,6, ×1,6, ×1,6). Sem escala de vida
nem de dano por profundidade. Detalhes em [[balanceamento_e_geracao]].

**Ainda não implementado** do design de 2026-10-19 ([[balanceamento_e_geracao]],
[[mapa_e_nos]]): a elite com papéis combinados e terreno de perigo garantido, e
um custo de complexidade calculado para cada inimigo. Hoje a batalha de elite só
muda o orçamento (×1,35), o XP (×2) e traz o inimigo único, e a ameaça de cada
inimigo é escrita à mão no config.

O encontro do editor (`DefaultEncounter`) coloca Morcego-olho, Crânio de fogo,
Goblin, Rato e Slime no grid de uma vez; as runs não usam isso.

### Skills (6.2)

`SkillDefinition` em `Skills/<Classe>` (variantes em `Skills/<Classe>/Upgrades`)
e `Skills/Enemies`. Formato da seleção: forma e alcance mínimo-máximo; "fixo" =
o atributo Alcance de skills não aumenta o alcance; "na própria casa" = centrada
no lançador. Todas as skills do jogador afetam só inimigos e não são anunciadas.
"Rodadas" = terreno temporário.

**Guerreiro**

| Skill | Dano | Área | Seleção | Recarga | Efeitos | Como se obtém |
|---|---|---|---|---|---|---|
| Golpe | 1 | 1 célula | linha 1, fixo | 0 | empurra 1 | inicial |
| Investida com Escudo | 1 | 1 célula | linha 1-3, fixo | 2 | avança até o alvo e empurra 1 | talento Treino de Investida |
| Golpe de Escudo | 2 | 1 célula | linha 1, fixo | 3 | empurra 2; Atordoado 1 turno | talento Escudo Pesado |
| Abalo Sísmico | 1 | linha de 3 | linha 1, fixo | 3 | empurra 1 | talento Força Bruta |
| Pisão | 1 | 3×3 ao redor | na própria casa | 3 | empurra 1 para longe do centro | talento Passo Pesado |
| Arremesso de Escudo | 1 | 1 célula | linha 2-4 | 2 | empurra 1 | talento Muralha |

**Mago**

| Skill | Dano | Área | Seleção | Recarga | Efeitos | Como se obtém |
|---|---|---|---|---|---|---|
| Rajada de Vento | 1 | 1 célula | linha 1-3 | 0 | empurra 1 | inicial |
| Nova de Gelo | 1 | 3×3 ao redor | na própria casa | 3 | Congelado 1 turno | talento Toque Gélido |
| Bola de Fogo | 2 | 1 célula | círculo 2-3 | 3 | Fogo na célula alvo por 2 rodadas | talento Piromancia |
| Muralha de Gelo | 1 | perpendicular de 3 | círculo 1-3 | 3 | Gelado 2 turnos; Muralha de Gelo nas células vazias da área por 2 rodadas | talento Criomancia |
| Tempestade Elétrica | 1 | cruz de 5 | círculo 2-4, ignora visão | 4 | Atordoado 1 turno; **canalizada** (usa o andar) | talento Conjurador de Tempestades |
| Meteoro | 2 | 3×3 | círculo 2-4, ignora visão | 5 | empurra 1 para longe do centro; **canalizada** (usa o andar) e Exausto 2 turnos no Mago | talento Grimório |

**Ladino**

| Skill | Dano | Área | Seleção | Recarga | Efeitos | Como se obtém |
|---|---|---|---|---|---|---|
| Adaga Envenenada | 1 | 1 célula | linha 1-3 | 0 | Envenenado 2 turnos | inicial |
| Gancho | 1 | 1 célula | linha 2-3 | 2 | puxa 3 | talento Gancho de Caça |
| Passo das Sombras | 1 | 1 célula | círculo 1-3, casa ocupada | 3 | troca de lugar com o personagem do alvo | talento Caminho das Sombras |
| Adaga de Arremesso | 1 | 1 célula | estrela 2-4 | 1 | Exposto 2 turnos | talento Arsenal Oculto |
| Bomba de Fumaça | 1 | 3×3 ao redor | na própria casa | 4 | Cego 1 turno | talento Mestre das Sombras |
| Linha Venenosa | 1 | linha de 4 | linha 1, fixo | 3 | Envenenado 2 turnos; Pântano Venenoso nas células da área por 2 rodadas | talento Mestre do Veneno |

**Variantes** (a melhoria troca a skill por uma das duas; o que não está escrito
é igual à original, inclusive a canalização e o Exausto do Meteoro e da Tempestade):

| Skill | Variante 1 | Variante 2 |
|---|---|---|
| Golpe | Golpe Amplo: área perpendicular de 3 | Golpe Pesado: dano 2, empurra 2, recarga 1 |
| Investida com Escudo | Investida Longa: linha 1-5 | Investida Esmagadora: empurra 2 |
| Golpe de Escudo | Golpe Retumbante: empurra 3 | Varredura de Escudo: área perpendicular de 3 |
| Abalo Sísmico | Fenda Longa: linha de 5 | Abalo Profundo: dano 2 |
| Pisão | Pisão Trovejante: também Atordoado 1 turno, recarga 4 | Pisão Pesado: empurra 2 |
| Arremesso de Escudo | Arremesso Atordoante: também Atordoado 1 turno, recarga 3 | Arremesso Pesado: empurra 2 |
| Rajada de Vento | Rajada Forte: empurra 2 | Ventania: área perpendicular de 3 |
| Nova de Gelo | Prisão de Gelo: 1 célula, círculo 1-3, recarga 2 | Congelamento Profundo: dano 2 |
| Bola de Fogo | Bola de Fogo Explosiva: dano 1, cruz de 5, Fogo em todas as células da área | Bola de Fogo Arqueada: círculo 2-4, ignora visão |
| Muralha de Gelo | Muralha Longa: perpendicular de 5 | Muralha Duradoura: 4 rodadas |
| Tempestade Elétrica | Tempestade Ampla: área 3×3 | Tempestade Rápida: recarga 3 |
| Meteoro | Meteoro Ardente: também Fogo na célula alvo por 2 rodadas | Meteoro Veloz: recarga 4 |
| Adaga Envenenada | Adaga Perfurante: linha de 2 | Adaga Debilitante: Fraquejado 1 turno em vez do veneno |
| Gancho | Gancho Longo: linha 2-5, puxa 4 | Gancho Atordoante: também Atordoado 1 turno, recarga 3 |
| Passo das Sombras | Passo Venenoso: também Envenenado 2 turnos | Passo Longo: círculo 1-5 |
| Adaga de Arremesso | Leque de Facas: área perpendicular de 3 | Adaga Rápida: recarga 0 |
| Bomba de Fumaça | Fumaça Arremessada: círculo 1-3 | Fumaça Densa: Cego 2 turnos |
| Linha Venenosa | Linha Venenosa Longa: linha de 6 | Veneno Concentrado: dano 2 |

**Inimigos** (todas anunciadas: preparo de 1 turno, o inimigo não anda enquanto
espera, não canceladas por deslocamento, interrompíveis por estados, a área
acompanha quem prepara; afetam todos menos quem lança; alcance fixo). As 17
marcadas com * entraram em 2026-10-20 (golpe básico e especial em todo inimigo):

| Skill | Inimigo | Dano | Área | Seleção | Recarga | Efeitos |
|---|---|---|---|---|---|---|
| Porretada | Goblin | 1 | fila de 3 na frente | linha 1 | 0 | |
| Mordida | Rato, Rato espinhoso | 1 | linha de 2 | linha 1 | 0 | |
| Respingo Ácido | Slime, Slime regenerante | 1 | perpendicular de 3 | linha 1 | 0 | |
| Seta Flamejante | Crânio de fogo | 1 | cruz de 5 | linha 2-3 | 0 | Queimando 2 turnos |
| Rasante | Morcego-olho | 1 | linha de 2 | diagonal 1 | 0 | |
| Estocada | Goblin lanceiro | 1 | linha de 2 | linha 1 | 0 | |
| Teia | Aranha tecelã | 1 | 1 célula | linha 2-4 | 1 | Enredado 1 turno |
| Mordida Vampírica | Morcego vampiro | 1 | linha de 2 | linha 1 | 0 | cura 1 o morcego |
| Chuva de fogo | Goblin bombardeiro | 1 | 3×3 | círculo 2-4, ignora visão | 2 | Queimando 1 turno |
| Chifrada | Besouro aríete | 1 | linha de 2 | linha 1 | 0 | empurra 2 |
| Faísca | Xamã goblin | 1 | cruz de 5 | círculo 2-3 | 0 | |
| Gancho | Goblin arpoador | 1 | 1 célula | linha 2-4 | 1 | puxa 3 |
| Descarga | Totem do raio | 1 | linha de 5 | linha 1 | 1 | |
| Empurrão de Escudo | Goblin blindado, Guarda Goblin | 1 | linha de 2 | linha 1 | 0 | empurra 1 |
| Lufada | Xamã do vento | 1 | linha de 3 | linha 1 | 0 | empurra 1 |
| Morteiro | Goblin artilheiro | 1 | cruz de 5 | círculo 2-5, ignora visão | 1 | |
| Olhar Maldito | Olho maldito | 1 | linha de 2 | linha 2-4 | 0 | Exposto 2 turnos |
| Maldição do Pântano | Bruxa do pântano | 1 | cruz de 5 | círculo 2-4 | 2 | Pântano Venenoso nas células da área por 2 rodadas |
| Tremor | Golem de pedra | 2 | 3×3 ao redor | na própria casa | 1 | |
| Guincho | Rainha dos ratos | 1 | cruz de 5 | círculo 1-2 | 0 | empurra 1 |
| Pancada esmagadora | Ogro brutamontes | 3 | linha de 3 | linha 1 | 1 | |
| Varredura Óssea | Cavaleiro esqueleto | 2 | perpendicular de 3 | linha 1 | 0 | |
| Investida Sombria | Cavaleiro esqueleto | 2 | 1 célula | linha 2-4 | 2 | avança até o alvo e empurra 1 |
| Erupção | Golem de lava | 2 | cruz de 5 | círculo 1-3 | 1 | Fogo nas células da área por 2 rodadas |
| Martelada | Rei Goblin | 3 | 3×3 ao redor | na própria casa | 2 | |
| Onda de Choque | Rei Goblin | 2 | linha de 4 | linha 1 | 1 | empurra 2 |
| Estilingada* | Goblin artilheiro, Goblin bombardeiro, Goblin arpoador | 1 | linha de 2 | linha 2-4 | 0 | |
| Golpe Real* | Rei Goblin | 2 | fila de 3 na frente | linha 1 | 0 | |
| Punho de Magma* | Golem de lava | 1 | fila de 3 na frente | linha 1 | 0 | Queimando 2 turnos |
| Choque Estático* | Totem do raio | 1 | 3×3 ao redor | na própria casa | 0 | |
| Soco de Ogro* | Ogro brutamontes | 2 | fila de 3 na frente | linha 1 | 0 | empurra 1 |
| Soco de Pedra* | Golem de pedra | 1 | fila de 3 na frente | linha 1 | 0 | empurra 1 |
| Bola de Lama* | Bruxa do pântano | 1 | cruz de 5 | círculo 2-3 | 0 | |
| Investida* | Goblin, Goblin blindado, Guarda Goblin, Besouro aríete | 1 | 1 célula | linha 2-3 | 2 | avança até o alvo e empurra 1 |
| Dardo* | Goblin lanceiro | 1 | 1 célula | linha 2-4 | 2 | empurra 1 |
| Arco Elétrico* | Xamã goblin | 1 | cruz de 5 | círculo 2-3 | 2 | |
| Grito Sônico* | Morcego-olho, Morcego vampiro | 1 | 3×3 ao redor | na própria casa | 2 | empurra 1 para longe |
| Explosão de Chamas* | Crânio de fogo | 1 | cruz de 5 | círculo 2-3 | 2 | Queimando 2 turnos |
| Olhar Ofuscante* | Olho maldito | 1 | 1 célula | linha 2-4 | 2 | Cego 1 turno |
| Mordida Pestilenta* | Rato, Rainha dos ratos | 1 | 1 célula | linha 1 | 2 | Envenenado 2 turnos |
| Rajada de Espinhos* | Rato espinhoso | 1 | 3×3 ao redor | na própria casa | 2 | |
| Cuspe Ácido* | Slime, Slime regenerante | 1 | 1 célula | círculo 2-3 | 2 | Exposto 2 turnos |
| Tornado* | Xamã do vento | 1 | cruz de 5 | círculo 2-4 | 2 | empurra 1 para longe do centro |

Com o ciclo de [Cooldown](#cooldown), as skills de recarga 0 atingem a cada
turno, as de recarga 1 a cada 3 turnos e as de recarga 2 a cada 4 turnos. Como
cada inimigo tenta o especial primeiro e o básico depois, ele prepara o especial
quando pronto e o básico nos outros turnos.

Mudança nos textos (2026-10-20): Descarga e Tremor não dizem mais "Descansa um
turno depois", e os comportamentos do Goblin artilheiro, do Totem do raio, do
Golem de pedra e do Morcego-olho passaram a citar o golpe básico (ou o Grito
Sônico).

### Consumíveis

`ConsumableDefinition` em `Consumables` (8 itens no pool de `ConsumableSettings`).
Inventário de 3 espaços, 1 uso por turno, só em batalha; o nó de consumível
oferece 3 para escolher 1. Todo item com alvo precisa de linha de visão.

| Consumível | Mira | Efeito | Peso de sorteio |
|---|---|---|---|
| Poção de Cura | sem alvo | recupera 4 de vida (recusa vida cheia) | 3 |
| Bomba | com alvo (até 3 células, Manhattan; área 3×3; precisa de linha de visão) | 2 de dano a cada inimigo na área | 2 |
| Tônico de Regeneração | sem alvo | aplica Regeneração (1 de vida por turno) por 3 turnos | 1 |
| Granada de Impacto | com alvo (até 3; área 3×3; linha de visão) | empurra 1 casa, para longe do centro, cada inimigo na área (colisões causam dano) | 1,5 |
| Pergaminho de Troca | com alvo (até 4; 1 célula ocupada; linha de visão) | quem usa troca de lugar com o personagem da célula (o golpe preparado vai junto) | 1 |
| Pedra | com alvo (até 3; célula vazia; linha de visão) | Rocha na célula por 3 rodadas (bloqueia o andar e a visão) | 1 |
| Frasco de Fogo | com alvo (até 3; cruz de 5; linha de visão) | Fogo nas células da área por 2 rodadas | 1 |
| Frasco Congelante | com alvo (até 3; 1 célula; linha de visão) | Congelado 1 turno no inimigo (cancela o golpe preparado) | 1 |

### Talentos

`TalentDefinition` em `Talents/<Classe>` e `Talents/Shared` (todos provisórios:
`isPlaceholder`). Nenhum tem pré-requisito. Os talentos que liberam skill não
concedem estado; os demais concedem um estado permanente com o mesmo nome
(`Settings/States/Talents`), uma pilha por posto.

**Guerreiro**

| Talento | Nível mínimo | Postos | Peso | Efeito |
|---|---|---|---|---|
| Sede de Sangue | 2 | 1 | 1 | Matar um inimigo cura 1 PV. |
| Dominó | 2 | 1 | 1 | Quando um inimigo que você empurra ou puxa bate em outro personagem, esse também é empurrado 1 casa na mesma direção. |
| Estilhaçar | 2 | 1 | 1 | As colisões que você causa dão 1 de dano a mais. |
| Força Bruta | 3 | 1 | 1,5 | Desbloqueia Abalo Sísmico. |
| Passo Pesado | 3 | 1 | 1,5 | Desbloqueia Pisão. |
| Muralha | 3 | 1 | 1,5 | Desbloqueia Arremesso de Escudo. |

**Mago**

| Talento | Nível mínimo | Postos | Peso | Efeito |
|---|---|---|---|---|
| Foco Arcano | 2 | 1 | 1 | Se você não andou neste turno, suas skills causam 1 de dano a mais. |
| Quebradiço | 2 | 1 | 1 | Suas skills causam 1 de dano a mais em inimigos congelados ou gelados. |
| Rastro de Chamas | 2 | 1 | 1 | Inimigos que você empurra ou puxa deixam fogo nas casas por onde saem, por 2 rodadas. |
| Gelo Escorregadio | 2 | 1 | 1 | Inimigos congelados ou gelados que você empurra deslizam até bater em algo. |
| Colheita de Almas | 2 | 1 | 1 | Matar um inimigo reduz em 1 a recarga das suas skills. |
| Conjurador de Tempestades | 3 | 1 | 1,5 | Desbloqueia Tempestade Elétrica. |
| Criomancia | 3 | 1 | 1,5 | Desbloqueia Muralha de Gelo. |
| Grimório | 3 | 1 | 1,5 | Desbloqueia Meteoro. |

**Ladino**

| Talento | Nível mínimo | Postos | Peso | Efeito |
|---|---|---|---|---|
| Assassino | 2 | 1 | 1 | Suas skills causam 1 de dano a mais em inimigos sem aliados ao lado. |
| Lâminas Envenenadas | 2 | 1 | 1 | Suas skills envenenam cada inimigo que atingem (Envenenado, 2 turnos). |
| Predador | 2 | 1 | 1 | Suas skills causam 1 de dano a mais em inimigos envenenados. |
| Banquete das Sombras | 2 | 1 | 1 | Matar um inimigo cura 1 PV. |
| Passo Trocado | 2 | 1 | 1 | O seu andar também pode trocar de lugar com um personagem adjacente. |
| Arsenal Oculto | 3 | 1 | 1,5 | Desbloqueia Adaga de Arremesso. |
| Mestre das Sombras | 3 | 1 | 1,5 | Desbloqueia Bomba de Fumaça. |
| Mestre do Veneno | 3 | 1 | 1,5 | Desbloqueia Linha Venenosa. |

**Compartilhados** (`TalentOfferSettings.sharedPool`, todas as classes; peso × 0,5
na oferta):

| Talento | Nível mínimo | Postos | Peso | Efeito |
|---|---|---|---|---|
| Contragolpe | 2 | 1 | 1 | Quando um golpe mirado em você erra, o inimigo que golpeou fica Exposto até o fim do próximo turno dele. |
| Ímpeto | 2 | 1 | 1 | Matar um inimigo dá a você um andar a mais neste turno. |
| Vitalidade | 2 | 2 | 1 | +2 de vida máxima por posto. |
| Longo Alcance | 3 | 1 | 1 | +1 de alcance para as suas skills cujo alcance pode crescer. |
| Banidor | 3 | 2 | 1 | +1 banimento das ofertas de talento por posto. |
| Mais Opções | 4 | 2 | 1 | +1 opção de talento em cada oferta por posto. |

As tags de sinergia (`Talents/Tags`) são Ataque, Arcano, Brigão, Crítico,
Defesa, Elemental, Guardião, Habilidades, Recuperação, Sombra, Utilidade e
Veneno. Os estados de talento que sobraram de antes (Veneno letal, Roubo de Vida)
não são concedidos por nenhum talento.

### Configurações

| Asset | Valores principais |
|---|---|
| `CombatSettings` | defesa **fixa**; teto do modo percentual 80%; **dano mínimo 0**; arredondamento ao mais próximo; teto de crítico 100%; dano periódico, espinhos e terreno **ignoram defesa**; colisão: dano 1 + 0 por célula não percorrida, 1 ao personagem atingido, ignora defesa, a borda conta; **disparar um golpe anunciado não usa o agir**; skills precisam de linha de visão e personagens a bloqueiam; métrica de andar e de ataque **euclidiana**; de skill **Manhattan**; adjacência **Chebyshev** (8 vizinhos) |
| `ForecastSettings` | número da ordem sobre os inimigos e nas células miradas ligados; deslocamento do número no golpe (−11, 16) px; números de golpes na mesma célula a 9 px um do outro (`strikeOrderSpacing`); fonte 1,5; cada piscada da barra de vida dura 0,6 s; fantasmas com alpha 0,45; setas deslocadas 10 px do centro da célula; número de dano a (11, −8) px |
| `SkillSettings` | 6 slots; as skills iniciais usam slots; recarga mínima 1 |
| `ConsumableSettings` | 3 espaços; inventário cheio: perguntar ao jogador; 1 uso por turno; só em batalha; o nó oferece 3 itens |
| `ProgressionSettings` | base 150, +100 por nível, nível máximo 12, sobra de XP guardada, sem cura ao subir de nível, +1 de vida máxima por nível |
| `TalentOfferSettings` | 3 opções (até 5); 2 rerolls, 1 banimento e 1 pulo por run; peso por sinergia +50% por tag em comum; peso dos talentos só do pool compartilhado ×0,5; até 3 talentos de skill; nó de talento oferece o nível +1; ganho de vida máxima cura o mesmo; a oferta evita repetir as opções na tela |
| `RunSettings` | nó de cura e descanso do acampamento: 40% da vida máxima; acampamento oferece melhoria de skill; elite dá 1 oferta de consumível (`eliteConsumableOffers`) e depois a melhoria (`eliteGrantsSkillUpgrade`); nó de talento custa 2 de vida máxima; estados temporários removidos entre batalhas; recargas zeradas entre batalhas; salvamento automático |
| `BattleGenerationSettings` | ver [Inimigos](#inimigos) e [XP por inimigo](#xp-por-inimigo); perfis normal (1/1/1) e elite (orçamento 1,35, força 1, XP 2); toda batalha de elite tem um inimigo único |
| `MapGenerationSettings` | 30 andares (balanceamento escrito para 30); batalhas sorteadas com pesos normal 75 e elite 25, elite só onde há uma elite na profundidade |
| `GridMovementSettings` | estilo `Hop`; avanço do ataque de 5 px em 0,15 s; flash de acerto vermelho; morte de 0,15 s; deslize de empurrões e puxões 0,06 s por célula (`OutQuad`), reação de colisão 0,02 s depois |
| `GameplayInputSettings` | toque longo de 0,45 s; mirar e confirmar ligado (padrão do código) |
| `TurnManager` (cena) | um inimigo de cada vez, na ordem fixa, 0,08 s de pausa entre eles, aceleração 2x |
| `XpRewardSystem` (cena) | 5 XP por orb, no máximo 8 orbs por abate (`maxPackets`, padrão do código) |
| `XpOrbsVfxSettings` | explosão de 14 ± 6 px em 0,22 s (`OutCubic`), pausa 0,08 s, voo 0,45 s, variação de partida 0,06 s |
