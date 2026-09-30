---
tags:
  - design
  - combate
  - sistemas
created: 2026-09-29
updated: 2026-09-29
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
> `EnemyController`) são apenas **roteadores**: leem input, perguntam ao
> character/grid se a ação é válida e executam via métodos do character e do
> `GridController`. Atributos, classe e skills pertencem aos characters e aos
> objetos de configuração.

## Grid e células

- O `GridController` é dono do estado: matriz `Cell[,]`, `gridSize`, spawn do
  player e dos inimigos em `InitializeGrid`.
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
> No editor, `InitializeGrid` instancia os entities **linkados ao prefab**
> (`PrefabUtility.InstantiatePrefab`), então clones na cena refletem mudanças
> no prefab (configs, atributos) automaticamente. Em runtime, usa
> `Instantiate` normal.

> [!note]
> Distância no grid usa `Vector2Int.Distance` (euclidiana). Com
> `walkDistance = 1` apenas os 4 vizinhos ortogonais são alcançáveis — as
> diagonais (≈ 1,41) ficam fora de alcance. Trocar para Chebyshev (diagonais
> alcançáveis) é uma mudança de 1 função em `GridRules`.

## Configuração de personagens

Cada character tem um **objeto de configuração** (ScriptableObject) que dita
seus atributos, conteúdo e comportamentos disponíveis. Os attributes do
`Character` **não são mais campos serializados no prefab** — tudo vem do
config.

```
CharacterConfig (SO)
├── maxHp
├── walkDistance
├── attackDistance
├── basicAttackDamage
└── skills: List<SkillDefinition>

PlayerCharacterConfig : CharacterConfig (SO)
├── characterClass: ECharacter        ← exclusivo do player
├── baseXpToLevelUp
└── xpToLevelUpGrowthPerLevel

EnemyConfig : CharacterConfig (SO)
└── xpReward                          ← exclusivo do inimigo
```

- `Character` (base) referencia um `CharacterConfig` (campo `config`):
  `MaxHp`, `WalkDistance`, `AttackDistance`, `BasicAttackDamage` e `Skills`
  são lidos dele. `current` (HP) é estado de runtime, inicializado com
  `config.MaxHp` no `Awake`. O ataque básico usa
  `Character.Attack(target)` → `target.ReceiveDamage(BasicAttackDamage)`.
- **`PlayerCharacter`** tem informações extras que `Enemy` não tem:
  - `PlayerConfig` (acesso tipado ao `PlayerCharacterConfig`);
  - `Class` (`ECharacter`: Warrior/Mage/Rogue) — **somente PlayerCharacters
    possuem classe**.
- **`Enemy`** herda apenas o `Character` base — sem classe; o comportamento
  por tipo de inimigo vem do config (atributos + skills).
- Assets vivos em `Assets/Application/Settings/Characters/`:
  - `Players/Knight.asset` (Warrior), `Players/Mage.asset`,
    `Players/Rogue.asset` — `PlayerCharacterConfig`;
  - `Enemies/Goblin.asset`, `Rat.asset`, `Slime.asset`, `FireSkull.asset`,
    `EyeBat.asset` — `EnemyConfig`.

> [!note] Balanceamento inicial
> Para dar vantagem ao jogador no início da run, os **players têm ataque
> básico 15** e os **inimigos 5–7**. HP e mobilidade variam por personagem
> conforme a tabela abaixo. Os valores moram no
> `CharacterConfigSetup.cs` (ferramenta de editor) e são aplicados aos
> assets — **alterar sempre em pares** (script ↔ documento). Ajustes devem
> acompanhar o design de progressão ([[plano_progressao]]).

### Valores atuais das settings

Espelho de `Assets/Application/Settings/Characters/` (aplicados por
`CharacterConfigSetup.cs`). `attackDistance = 1` para todos.

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
- Ferramenta de setup: `Assets/Application/Scripts/Editor/CharacterConfigSetup.cs`
  (cria os assets e atribui aos prefabs).

> [!note]
> `ECharacter` vive em `GridBattle.Gameplay.Entities` (domínio de gameplay).
> A UI (`MainMenuScreenView`, `CharacterChoosenEvent`) apenas referencia —
> o enum não pertence à camada de UI.

## Skills

Skills são **objetos de dados** (`SkillDefinition`, ScriptableObject), nunca
código dentro de controllers. Cada skill concreta é uma subclasse de
`SkillDefinition` com `CanUse`/`Execute`:

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
- `EnemyController.TrySpecialAction()` — itera as skills do config do inimigo
  e tenta usá-las contra a posição do player.

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

`TurnManager` (MonoBehaviour na cena, no GameObject do Grid) assina
`PlayerActionEvent`:

```
PlayerActionEvent
  → para cada EnemyController da cena (ordem arbitrária):
      → enemy.Act()
```

- **Cada ação do jogador = 1 rodada completa dos inimigos.**
- Inimigos agem de forma **síncrona e sequencial** (sem delays); animações e
  pacing visual podem ser adicionados depois sem mudar as regras.
- Ação inválida do jogador não levanta o evento → não há turno inimigo.
- Movimento e ataque do jogador **e** uso de skills consomem o turno.

## IA dos inimigos

`EnemyController` (componente dos prefabs de inimigo) executa a ação do
turno nesta ordem de prioridade:

1. **Guard:** inimigo morto não age.
2. **Ação especial** — `TrySpecialAction()` tenta as skills do config do
   inimigo via `Enemy.TryUseSkill`. Subclasses podem sobrescrever para
   comportamentos manuais exclusivos do tipo. Retornar `true` significa
   "ação consumida".
3. **Atacar** — se `GridRules.IsAttackTarget(grid, enemy, playerPos)`,
   ataca o `PlayerCharacter` (`IDamageReceiver`).
4. **Andar em direção ao player** — `FindBestStep` varre toda a área de
   `WalkDistance`, filtra por `GridRules.CanWalkTo` e escolhe a célula
   candidata **com menor distância até o player**. Sem caminho que aproxime,
   fica parado (passa o turno).

> [!tip]
> A IA de passo é **gananciosa por um turno** (one-step greedy): ela não
> planeja rotas (pathfinding A*). Se o design pedir contornar obstáculos,
> planejar rotas multi-turno ou coordenar grupos, a mudança fica dentro de
> `FindBestStep`/`GridRules` sem afetar os controllers.

## Morte e fim de run

A lógica é genérica em `Character` (`Die()`, `protected virtual`):

1. HP chega a 0 em `ReceiveDamage` → `IsDead` → `Die()`.
2. `Die()`:
   - desocupa a célula (`GetComponentInParent<Cell>().RemoveContent()`);
   - levanta `CharacterDiedEvent(this)`;
   - `Destroy(gameObject)`.
3. Reações assinam `CharacterDiedEvent`:
   - **Player morre** → `NavigationController` encerra a run: transição de
     tela de volta para o **menu de escolha de personagem**
     (`UIScreen.MainMenu`). Nova run recria o grid do zero via
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

```
inimigo morre (Character.Die)
  → CharacterDiedEvent
      → PlayerCharacter.OnCharacterDied
          → lê EnemyConfig.XpReward do inimigo morto
          → soma em CurrentXp (level ups em cadeia, se acumular)
          → EventBus.Raise(PlayerXpChangedEvent { Level, CurrentXp, XpToNextLevel })
              → GameScreenView.OnXpChanged
                  → progresso % = CurrentXp / XpToNextLevel
                  → largura do "xp-bar-progress" = % * 110px
```

- A comunicação **jogo → UI é sempre via EventBus** (`PlayerXpChangedEvent`);
  a UI nunca lê estado de gameplay em tempo real — só ao recarregar a tela
  (`GameScreenView.OnUIReload` restaura o estado atual do `PlayerCharacter`
  para redrawing da barra).
- O inimigo morto já desocupou a célula antes do XP ser concedido (a ordem
  do `Die()` garante isso).
- Player morto não ganha XP (guard `IsDead`).

### Limiar de XP

Configurado em `PlayerCharacterConfig`:

- `baseXpToLevelUp` = **50** — XP para ir do nível 1 ao 2.
- `xpToLevelUpGrowthPerLevel` = **25** — acréscimo no limiar a cada nível
  atingido.

Fórmula: `XpToNextLevel(level) = baseXpToLevelUp + (level − 1) × growth`.

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
| **Novo tipo de inimigo** | novo prefab `Enemy` + `CharacterConfig` próprio; comportamento exclusivo via skills no config ou subclasse de `EnemyController` |
| **Atributos de classe** (stats por Warrior/Mage/Rogue) | valores diferentes por `PlayerCharacterConfig` asset; efeitos de classe como skills/triggers futuros |
| **Skills do jogador na UI** | botões chamam `PlayerCharacterController.TryUseSkill(skill, targetPos)` |
| **Efeitos de level up** | assinar `PlayerXpChangedEvent` (ex.: curar, escolher trait — ver [[plano_progressao]]) |
| **XP por fonte extra** (itens, eventos) | chamar o mesmo caminho do `PlayerCharacter` e emitir `PlayerXpChangedEvent` |
| **Facções/alianças** | checagem de facção dentro de `GridRules.IsAttackTarget` |
| **Distância em diagonais** | trocar a métrica em `GridRules.IsInWalkRange`/`IsInAttackRange` |
| **Pacing/animar turnos** | substituir a iteração síncrona do `TurnManager` por coroutine, sem mudar `EnemyController` |
| **Status/DoTs** (veneno etc.) | tickar a cada `PlayerActionEvent` (ver [[ideias_traits_itens]]) |
