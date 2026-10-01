---
tags:
  - design
  - combate
  - sistemas
created: 2026-09-29
updated: 2026-09-30
aliases:
  - Sistema de Combate
  - Turnos
---

# Sistema de Combate

Documento de design do comportamento do combate em grid do GridBattle:
configuração de personagens, movimento, ataque, skills, morte, sistema de
turnos e os pontos de extensão. Reflete o comportamento atualmente
implementado.

Ver também: [[talentos]], [[ideias_traits_itens]], [[plano_progressao]].

## Índice

1. [Visão geral](#visão-geral)
2. [Grid e células](#grid-e-células)
3. [Configuração de personagens](#configuração-de-personagens)
4. [Skills](#skills)
5. [Regras centrais (GridRules)](#regras-centrais-gridrules)
6. [Ações do jogador](#ações-do-jogador)
7. [Sistema de turnos](#sistema-de-turnos)
8. [IA dos inimigos](#ia-dos-inimigos)
9. [Morte e fim de run](#morte-e-fim-de-run)
10. [XP e level up](#xp-e-level-up)
11. [Highlights visuais](#highlights-visuais)
12. [Pontos de extensão](#pontos-de-extensão)

---

## Visão geral

O combate acontece em um **grid 2D de células** (padrão 6×6, definido em
`GridController`). Cada célula pode conter **no máximo um entity**
(`GridEntity` → `Character` → `PlayerCharacter` ou `Enemy`).

O jogo é **por turnos**: o jogador executa **uma ação**, e em seguida **todos
os inimigos executam uma ação**. Uma "ação" é um dos seguintes:

| Ação | Estado |
|---|---|
| Movimento (andar) | ✅ implementado |
| Ataque | ✅ implementado |
| Uso de skill | ⚙️ infraestrutura pronta, skills concretas 🔜 |

> [!important]
> Jogador e inimigos seguem **exatamente as mesmas regras** — todas as
> checagens de alcance/ocupação passam pelas regras centrais
> (`GridRules`). Nenhuma checagem é duplicada nos controllers.

> [!important]
> **Lógica não vive em controllers.** Os controllers (`PlayerCharacterController`,
> `EnemyController`, base `CharacterControllerBase<T>`) são apenas
> **roteadores**: leem input (ou a IA configurada), perguntam ao
> character/grid se a ação é válida e executam via métodos do character e do
> `GridController`. Atributos, classe, skills, visual e IA pertencem aos
> objetos de configuração (ScriptableObjects).

## Grid e células

- O `GridController` é dono do estado: matriz `Cell[,]`, `gridSize` e o spawn
  em `InitializeGrid(playerConfig)`:
  - player na célula (2, 2), a partir de um `PlayerCharacterConfig`;
  - inimigos do `EncounterConfig` (asset), na ordem da lista, cada um na
    primeira célula livre da varredura do grid;
  - no editor, o grid é iniciado com o `debugPlayerConfig` (botão
    **Reset grid** no inspector refaz o grid).
- A criação de personagens é centralizada em `CharacterFactory.Spawn(config)`.
- Cada `Cell` tem:
  - `GridPosition` (`Vector2Int`) — coordenada lógica;
  - `HasContent` / `GetContent()` / `SetContent()` / `RemoveContent()` — o
    entity que a ocupa;
  - highlight visual (walk/attack) controlado via `Highlighted` +
    `HighlightType`.
- O `GridController` expõe consultas e execuções de estado:
  - `IsValidPosition(pos)` — dentro dos limites;
  - `IsFreePosition(pos)` — sem conteúdo;
  - `GetContent(pos)` — entity ocupante (ou null);
  - `MoveEntity(entity, targetPos)` — **execução centralizada de movimento**
    (move o conteúdo da célula + atualiza `CurrentGridPos`).

> [!note]
> No editor (fora do Play Mode), o `CharacterFactory` instancia os entities
> **linkados ao prefab-template** (`PrefabUtility.InstantiatePrefab`) e
> registra o config aplicado como override, então os clones na cena refletem
> mudanças no template. Em runtime, usa `Instantiate` normal.

> [!note]
> Distância no grid usa `Vector2Int.Distance` (euclidiana). Com
> `walkDistance = 1` apenas os 4 vizinhos ortogonais são alcançáveis — as
> diagonais (≈ 1,41) ficam fora de alcance. Trocar para Chebyshev (diagonais
> alcançáveis) é uma mudança de 1 função em `GridRules`.

## Configuração de personagens

Cada character é definido por um **objeto de configuração** (ScriptableObject)
que dita **tudo** o que o diferencia: visual, atributos, skills e, no caso de
inimigos, a IA e a recompensa. Não existe mais um prefab por personagem:
todos compartilham um **prefab-template** e recebem o config no spawn
(`CharacterFactory`).

```
CharacterConfig (SO, abstrato)
├── sprite, animatorController        ← visual (aplicado pelo CharacterView)
├── maxHp, walkDistance, attackDistance, basicAttackDamage
└── skills: List<SkillDefinition>

PlayerCharacterConfig : CharacterConfig    (Create > GridBattle > Characters)
├── prefab: PlayerCharacter           ← template compartilhado
├── characterClass: ECharacter        ← exclusivo do player
├── baseXpToLevelUp
├── xpToLevelUpGrowthPerLevel
└── GetXpToNextLevel(level)           ← curva de XP

EnemyConfig : CharacterConfig              (Create > GridBattle > Characters)
├── prefab: Enemy                     ← template compartilhado
├── behavior: EnemyBehavior           ← IA (ver "IA dos inimigos")
└── xpReward                          ← exclusivo do inimigo
```

Prefabs-template em `Assets/Application/Prefabs/Entities/`:

```
Character.prefab          SpriteRenderer + Animator + CharacterView (base)
├── Enemy.prefab          (variant) + Enemy + EnemyController
└── PlayerCharacter.prefab (variant) + PlayerCharacter + PlayerCharacterController
```

- `Character.Initialize(config)` aplica o config: atributos, HP cheio, nome
  do GameObject e visual (`CharacterView.Apply` define sprite e
  `RuntimeAnimatorController`). `Awake` continua inicializando o HP para
  characters colocados à mão na cena com um config atribuído.
- O ataque básico usa `Character.Attack(target)` →
  `target.ReceiveDamage(BasicAttackDamage)`. `OnHpChanged` é um `event`.
- **`PlayerCharacter`** tem informações extras que `Enemy` não tem:
  - `PlayerConfig` (acesso tipado ao `PlayerCharacterConfig`);
  - `Class` (`ECharacter`: Warrior/Mage/Rogue) — **somente PlayerCharacters
    possuem classe**.
- **`Enemy`** expõe `EnemyConfig` (acesso tipado); sem classe.
- A classe escolhida no menu é resolvida pelo `GameStateManager`, que tem a
  lista `playableCharacters` e procura o config cujo `CharacterClass` bate.
- Assets vivos em `Assets/Application/Settings/`:
  - `Characters/Players/Knight.asset` (Warrior), `Mage.asset`, `Rogue.asset`
    — `PlayerCharacterConfig`;
  - `Characters/Enemies/Goblin.asset`, `Rat.asset`, `Slime.asset`,
    `FireSkull.asset`, `EyeBat.asset` — `EnemyConfig`;
  - `AI/ChaseAndAttack.asset` + `AI/Actions/*.asset` — IA padrão dos inimigos;
  - `Encounters/DefaultEncounter.asset` — inimigos do grid;
  - `Vfx/XpOrbsVfxSettings.asset` — ajustes visuais dos orbs de XP.

### Criando um personagem novo

- **Inimigo:** `Create > GridBattle > Characters > Enemy Config`; preencher
  sprite, animator, atributos, `prefab = Enemy.prefab`, `behavior` (ex.:
  `ChaseAndAttack`) e `xpReward`; adicionar o asset a um `EncounterConfig`.
  Nenhum prefab novo.
- **Personagem jogável:** `Create > GridBattle > Characters > Player Character
  Config`; `prefab = PlayerCharacter.prefab`; adicionar à lista
  `playableCharacters` do `GameStateManager`.
- Só crie um prefab próprio (variant do template) se o personagem precisar de
  componentes extras.

> [!note] Balanceamento inicial
> Para dar vantagem ao jogador no início da run, os **players têm ataque
> básico 15** e os **inimigos 5–7**. HP e mobilidade variam por personagem
> conforme a tabela abaixo. Os valores moram nos próprios assets de config —
> ao alterar, atualize também este documento. Ajustes devem acompanhar o
> design de progressão ([[plano_progressao]]).

### Valores atuais das settings

Espelho de `Assets/Application/Settings/Characters/`. `attackDistance = 1`
para todos.

#### Players — `PlayerCharacterConfig`

| Personagem | Classe | HP | Ataque básico | Alcance de andar |
|---|---|---|---|---|
| Knight | Warrior | 120 | 15 | 1 |
| Mage | Mage | 80 | 15 | 1 |
| Rogue | Rogue | 90 | 15 | 1 |

#### Inimigos — `EnemyConfig`

| Inimigo | HP | Ataque básico | Alcance de andar | XP |
|---|---|---|---|---|
| Goblin | 30 | 5 | 1 | 10 |
| Rat | 20 | 5 | 2 | 8 |
| Slime | 40 | 5 | 1 | 12 |
| FireSkull | 25 | 7 | 1 | 15 |
| EyeBat | 25 | 5 | 2 | 8 |

Diretrizes de leitura da tabela:

- **Slime** — tanque fraco (mais HP, passo curto).
- **Rat / EyeBat** — rápidos e frágeis (`walkDistance = 2`, perseguem mais
  agressivamente, morrem fácil).
- **FireSkull** — o mais perigoso dos comuns (ataque maior).
- **Mage** — a classe mais frágil do jogador (HP menor); compensará com
  skills de área quando implementadas (ver [[talentos]]).

> [!note]
> `ECharacter` vive em `GridBattle.Gameplay.Entities` (domínio de gameplay).
> A UI (`MainMenuScreenView`, `CharacterChoosenEvent`) apenas referencia —
> o enum não pertence à camada de UI.

## Skills

Skills são **objetos de dados** (`SkillDefinition`, ScriptableObject
abstrato), nunca código dentro de controllers. Cada skill concreta é uma
subclasse de `SkillDefinition` (com seu `[CreateAssetMenu]`) que implementa
`CanUse`/`Execute`:

- `SkillDefinition.CanUse(caster, grid, targetPos)` — valida usando as
  regras do grid (deve combinar com `GridRules`).
- `SkillDefinition.Execute(caster, grid, targetPos)` — aplica o efeito;
  retornar `true` significa "turno consumido".

A execução é **do character**, não do controller:

- `Character.TryUseSkill(grid, skill, targetPos)` — ponto único: checa
  `IsDead`, chama `skill.CanUse` e `skill.Execute`. Player e inimigos usam o
  mesmo caminho.
- `PlayerCharacter.Skills` / `Enemy.Skills` vêm do config — a lista de skills
  disponíveis é **data**, editável por asset.
- `PlayerCharacterController.TryUseSkill(skill, targetPos)` — rota para a UI
  futura: delega ao character e, se consumiu, levanta `PlayerActionEvent`.
- Inimigos usam skills pela ação de IA `UseSkillsAction`, que itera as skills
  do config do inimigo e tenta usá-las contra a posição do player.

> [!important]
> Criar uma skill nova = criar uma subclasse de `SkillDefinition` + um asset
> `.asset` dela + referenciar no config do character desejado. **Zero
> mudanças em controllers.**

## Regras centrais (GridRules)

`Gameplay/Rules/GridRules.cs` é o **único lugar** com cálculos entre
characters e grid. É uma classe estática com funções puras:

| Função | Responsabilidade |
|---|---|
| `IsInWalkRange(from, to, walkDistance)` | alcance de movimento |
| `IsInAttackRange(from, to, attackDistance)` | alcance de ataque |
| `CanWalkTo(grid, character, target)` | posição válida **e** livre **e** em alcance |
| `IsAttackTarget(grid, attacker, target)` | alvo válido: um `IDamageReceiver` que **não** é o próprio atacante, em alcance de ataque |
| `GetHighlightInfos(grid, character)` | dicionário de highlights (Walk/Attack) para a área do character |
| `FindStepToward(grid, character, target)` | passo guloso de um turno: a célula alcançável mais perto do alvo (ou null se nenhuma aproxima) |

Regras de negócio derivadas:

- **Um character nunca anda para uma célula ocupada** (`IsFreePosition`).
- **Um character nunca ataca a si mesmo** nem aliados da mesma natureza —
  hoje o atacante é excluído por identidade (`content != attacker`); quando
  houver times/facções, a checagem de facção entra aqui.
- A área avaliada para highlights e IA é o quadrado de raio
  `max(WalkDistance, AttackDistance)` (valores vindos do config).

## Ações do jogador

O `PlayerCharacterController` escuta `CellTapEvent` (clique/tap em uma célula)
e **roteia** a ação:

```
tap em célula
├── sem conteúdo  → ANDAR  (GridRules.CanWalkTo)
│     └── válido → GridController.MoveEntity + consome o turno
└── com conteúdo  → ATACAR (GridRules.IsAttackTarget)
      └── alvo é IDamageReceiver → PlayerCharacter.Attack + consome o turno
```

- Ação inválida (fora de alcance, célula ocupada ao tentar andar, alvo do
  próprio time) **não consome o turno**.
- Skills: `PlayerCharacterController.TryUseSkill` valida via
  `PlayerCharacter.TryUseSkill` (que consulta a `SkillDefinition`); se
  consumiu o turno, levanta `PlayerActionEvent`.
- Toda ação consumida levanta `PlayerActionEvent`, que dispara o turno dos
  inimigos (ver abaixo).

## Sistema de turnos

`TurnManager` (`GridBattle.Managers`, MonoBehaviour no GameObject do Grid) assina
`PlayerActionEvent`:

```
PlayerActionEvent
  → para cada EnemyController da cena (ordem arbitrária):
      → enemy.Act()
```

- **Cada ação do jogador = 1 rodada completa dos inimigos.**
- Inimigos agem de forma **síncrona e sequencial** (sem delays); animações e
  pacing visual podem ser adicionados depois sem mudar as regras.

> [!warning] Ordem dos inimigos não é determinística
> `FindObjectsByType` sem ordenação devolve os inimigos numa ordem que depende
> dos IDs alocados pelo Unity — ela **muda de uma run para outra**. Quando dois
> inimigos disputam a mesma célula, o resultado do turno varia. Se o design
> pedir turnos reproduzíveis, ordenar explicitamente no `TurnManager` (ex.:
> ordem de spawn ou posição no grid).
- Ação inválida do jogador não levanta o evento → não há turno inimigo.
- Movimento e ataque do jogador **e** uso de skills consomem o turno.

## IA dos inimigos

A IA é **dado, não código de controller**. O `EnemyController` (componente
do template `Enemy.prefab`) só resolve o alvo e delega o turno ao
`EnemyBehavior` do config:

```
EnemyController.Act()
  → guard: inimigo morto não age; sem player, não age
  → config.Behavior.TakeTurn(EnemyTurnContext { Self, Grid, Target })
      → tenta cada EnemyAction em ordem; a primeira que retornar true
        consome o turno
```

- `EnemyBehavior` (SO, `Create > GridBattle > AI > Enemy Behavior`) — lista
  ordenada de `EnemyAction`. Pode ser compartilhado por vários inimigos.
- `EnemyAction` (SO abstrato) — `TryExecute(in EnemyTurnContext)`. Ações são
  **assets sem estado de runtime** (o mesmo asset serve a todos os inimigos).
- Ações disponíveis (`Create > GridBattle > AI > Actions`):

| Ação | Asset | Comportamento |
|---|---|---|
| `UseSkillsAction` | `UseSkills` | tenta as skills do config contra a posição do player (`Enemy.TryUseSkill`) |
| `BasicAttackAction` | `BasicAttack` | se `GridRules.IsAttackTarget(grid, enemy, playerPos)`, ataca o player |
| `ChaseTargetAction` | `ChaseTarget` | anda o passo de `GridRules.FindStepToward`; sem passo que aproxime, não consome o turno |

- Comportamento padrão de todos os inimigos atuais: `ChaseAndAttack` =
  `UseSkills` → `BasicAttack` → `ChaseTarget` (a mesma prioridade da IA
  original).
- `EnemyConfig` sem `behavior` = inimigo que não age.

> [!tip]
> A IA de passo é **gananciosa por um turno** (one-step greedy): ela não
> planeja rotas (pathfinding A*). Comportamentos novos (fugir, manter
> distância, curar aliados, patrulhar) entram como novas subclasses de
> `EnemyAction` combinadas em outros `EnemyBehavior` — sem tocar no
> `EnemyController`.

## Morte e fim de run

A lógica é genérica em `Character` (`Die()`, `protected virtual`):

1. HP chega a 0 em `ReceiveDamage` → `IsDead` → `Die()`.
2. `Die()`:
   - desocupa a célula (`GetComponentInParent<Cell>().RemoveContent()`);
   - levanta `CharacterDiedEvent(this)`;
   - `Destroy(gameObject)`.
3. Reações assinam `CharacterDiedEvent`:
   - **Player morre** → `GameFlowController` encerra a run: `Navigator.Replace`
     de volta para o **menu de escolha de personagem**
     (`MainMenuScreenView`). Nova run recria o grid do zero via
     `InitializeGrid`.
   - **Inimigo morre** → apenas desocupa a célula; nenhum evento de navegação.
- `ReceiveDamage` é **idempotente pós-morte**: personagem morto não toma dano
  adicional (guard `if (IsDead) return;`), não usa skill (`TryUseSkill`
  também guarda) e não age no turno.

## XP e level up

O `PlayerCharacter` acumula XP ao longo da run; o estado (nível, XP) é de
runtime e reseta a cada nova run (o character é recriado pelo
`InitializeGrid`).

### Fluxo

A regra (quanto XP, para quem, em quantos pacotes) e a apresentação (orbs
voando até a barra) são componentes separados, cada um no seu GameObject —
nenhum deles vive no Grid.

```
inimigo morre (Character.Die)
  → desocupa a célula
  → CharacterDiedEvent
      → XpRewardSystem (GameObject próprio)          ← regra
          → lê EnemyConfig.XpReward do inimigo morto
          → resolve o PlayerCharacter atual
          → XpPacket.Split: N = teto(XpReward / xpPerPacket) pacotes,
            cada um com o progresso da barra após creditá-lo
          → EventBus.Raise(XpRewardDroppedEvent { Origin, Packets, Collect })
              → XpOrbsVfx (no GameObject GameScreenLayer)  ← apresentação
                  → MarkPresented(): assume a entrega
                  → cada pacote vira um orb (UI Toolkit) que voa em arco
                    (LitMotion) da posição do inimigo até o ponto da barra
                    (XpBarView.TryGetTrack), com stagger
                  → ao chegar: remove o orb e chama Collect(pacote)
          → se ninguém assumiu a entrega: Collect de todos na hora
  Collect(pacote) → PlayerCharacter.GainXp(pacote.Amount)
      → EventBus.Raise(PlayerXpChangedEvent { Level, CurrentXp, XpToNextLevel })
          → GameScreenController.OnXpChanged → XpBarView.SetState
              → largura do "xp-bar-progress" = progresso * 110px
              → texto do label "current-level" = Level
```

- **O XP só entra na conta quando os orbs chegam à barra**: a barra cresce
  orb a orb, não de uma vez na morte. Há um delay de ~0,6s (mais stagger)
  entre a morte e o XP efetivo.
- Divisão dos pacotes (`XpPacket.Split`): `N = ceil(XpReward / xpPerPacket)`
  (padrão 5, no `XpRewardSystem`); o resto da divisão é distribuído 1 a 1 nos
  primeiros pacotes (ex.: 12 XP → 3 pacotes de 4; 15 XP → 3 pacotes de 5). O
  jogador nunca perde XP no arredondamento.
- Os orbs são elementos da **UI** (UI Toolkit, absolute-positioned no root do
  painel da GameScreen), convertidos de world-space via
  `RuntimePanelUtils.CameraTransformWorldToPanel` — não são sprites no mundo.
- Ajustes visuais (sprite, tamanho, duração, stagger, arco, easing) ficam no
  asset `XpOrbsVfxSettings` e podem ser alterados em Play Mode.
- `XpBarView` é o único lugar que conhece os elementos e as medidas da barra
  (`xp-bar-progress`, `xp-bar-detail-2`, `current-level`, 110px/112px).
- Gameplay não depende da UI: sem apresentação disponível (painel não
  carregado, sem câmera, sem settings), o `XpRewardSystem` credita o XP
  imediatamente em vez de perdê-lo.
- A comunicação **jogo → UI é sempre via EventBus** (`PlayerXpChangedEvent`,
  `XpRewardDroppedEvent`). Um `PlayerCharacter` recém-criado emite
  `PlayerXpChangedEvent` no `Initialize`, então a barra começa cada run
  zerada. `GameScreenController.OnBind` restaura o estado ao recarregar a UI.
- O inimigo morto já desocupou a célula antes do XP ser concedido (a ordem
  do `Die()` garante isso).
- Player morto não ganha XP (guard `IsDead` no `GainXp`).

### Limiar de XP

Configurado em `PlayerCharacterConfig`:

- `baseXpToLevelUp` = **50** — XP para ir do nível 1 ao 2.
- `xpToLevelUpGrowthPerLevel` = **25** — acréscimo no limiar a cada nível
  atingido.

Fórmula (`PlayerCharacterConfig.GetXpToNextLevel`):
`XpToNextLevel(level) = baseXpToLevelUp + (level − 1) × growth`.

| Nível atual | XP para o próximo |
|---|---|
| 1 | 50 |
| 2 | 75 |
| 3 | 100 |
| 4 | 125 |

XP excedente é **carregado** para o próximo limiar (não descartado), com
level ups em cadeia quando um inimigo valioso fecha a conta.

### Recompensa de XP por inimigo

Configurado em `EnemyConfig.XpReward`:

| Inimigo | XP |
|---|---|
| Goblin | 10 |
| Rat | 8 |
| Slime | 12 |
| FireSkull | 15 |
| EyeBat | 8 |

## Highlights visuais

Sempre que o `PlayerCharacterController.Update` roda, o grid é re-pintado via
`GridRules.GetHighlightInfos`:

| Highlight | Condição | Cor |
|---|---|---|
| **Walk** (azul) | célula livre, em alcance de movimento | `walkHighlightColor` (prefab Cell) |
| **Attack** (vermelho) | célula com `IDamageReceiver` (não-player) em alcance de ataque | `attackHighlightColor` (prefab Cell) |

- Cores e alpha ficam no prefab `Cell.prefab` (alpha > 0 obrigatório).
- Inimigos não pintam highlights (decisão de design: o campo tático é
  apresentado do ponto de vista do jogador).

## Pontos de extensão

| Extensão | Onde plugar |
|---|---|
| **Skill nova (player ou inimigo)** | subclasse de `SkillDefinition` + asset `.asset` + referência no `CharacterConfig`/`PlayerCharacterConfig` — sem tocar em controllers |
| **Novo inimigo** | asset `EnemyConfig` (sprite, animator, atributos, `behavior`, `xpReward`, `prefab = Enemy.prefab`) + entrada num `EncounterConfig` — sem prefab novo |
| **Novo personagem jogável** | asset `PlayerCharacterConfig` (`prefab = PlayerCharacter.prefab`) + entrada em `GameStateManager.playableCharacters` |
| **Comportamento de inimigo novo** | subclasse de `EnemyAction` + asset; combinar ações num `EnemyBehavior` e referenciar no `EnemyConfig` |
| **Encontros / tabelas de spawn** | novos assets `EncounterConfig`; critérios de escolha (nível etc.) entram em quem decide qual encontro passar ao `GridController` |
| **Componentes extras num personagem** | prefab variant de `Enemy.prefab`/`PlayerCharacter.prefab` referenciado no `prefab` do config |
| **Reações visuais** (dano, ataque, morte) | `CharacterView` |
| **Animações compartilhadas** | `AnimatorOverrideController` no `animatorController` do config (sobre um controller base comum) |
| **Atributos de classe** (stats por Warrior/Mage/Rogue) | valores diferentes por `PlayerCharacterConfig` asset; efeitos de classe como skills/triggers futuros |
| **Skills do jogador na UI** | botões chamam `PlayerCharacterController.TryUseSkill(skill, targetPos)` |
| **Efeitos de level up** | assinar `PlayerXpChangedEvent` (ex.: curar, escolher trait — ver [[plano_progressao]]) |
| **XP por fonte extra** (itens, eventos) | chamar `PlayerCharacter.GainXp`; para entrega animada, emitir `XpRewardDroppedEvent` com os pacotes |
| **Facções/alianças** | checagem de facção dentro de `GridRules.IsAttackTarget` |
| **Distância em diagonais** | trocar a métrica em `GridRules.IsInWalkRange`/`IsInAttackRange` |
| **Pacing/animar turnos** | substituir a iteração síncrona do `TurnManager` por coroutine, sem mudar `EnemyController` |
| **Ajustar o VFX de XP** | asset `XpOrbsVfxSettings` (sprite, tamanho, duração do voo, stagger, altura do arco, easing); XP por orb em `XpRewardSystem.xpPerPacket` |
| **Status/DoTs** (veneno etc.) | tickar a cada `PlayerActionEvent` (ver [[ideias_traits_itens]]) |
