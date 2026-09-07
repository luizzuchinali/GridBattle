---
tags:
  - plano
  - progressao
  - classe
  - trait
created: 2026-07-12
aliases:
  - Plano de Progressão
status: implementado
---

# Plano: XP, Níveis, Classes e Árvore de Traits

> **STATUS: IMPLEMENTADO** (com escopo reduzido acordado): Fase 1 completa;
> Fase 2 com **Guerreiro, Mago e Ladino**; Fase 3 completa; Fase 4 com
> Slime, Arqueiro, Tanque (2×HP, sem gimmick de passo), Elite, Mímico e
> **Altar** (sem terrenos). Valores **fixos**. Código em
> `_application/progression/` + novas cenas em `character/scenes/` e
> `entity/scenes/`; dados em `progression/resource/`. Validação:
> `_application/util/_headless_progression_test.gd`.

Plano diretivo para implementar progressão do jogador em `grid_battle`. Segue as
premissas de `AGENTS.md`: `Grid` continua agnóstico a regras de jogo (XP/nível é
domínio do `GridController` e componentes), componentes tipados em cena, dados em
`Resource`s (aproveitando o addon `resources_spreadsheet_view` para balancear).

Ver também: [[talentos]], [[ideias_traits_itens]], [[plano_entidades]].

---

## 0. Decisão de design: valores fixos vs escaláveis

**Recomendação: valores fixos.** Cada tipo de inimigo/entidade tem HP, dano e XP
fixos; o jogador tem stats base fixos por classe. Todo ganho de poder vem de
**traits** (ataque, HP, crítico, pool de itens). A "escalada" de dificuldade vem
da **composição** do board (inimigos mais fortes começam a aparecer em níveis
maiores), não de inflação numérica.

Motivos:

1. **Legibilidade tática.** O jogo é de planejamento (padrão direcional de
   repopulação). Com valores fixos o jogador aprende o board: "slime morre em
   2 hits; com o trait +ataque, em 1". Escalar HP de inimigos quebraria essa
   leitura constantemente.
2. **Balanceamento simples.** Sem fórmulas de scaling, o balance vive em tabelas
   de `Resource` editáveis pelo `resources_spreadsheet_view` (já instalado).
3. **Traits ficam significativos.** Se tudo escala junto, +5 de ataque vira
   ruído; com valores fixos, cada trait cruza thresholds reais de hits-to-kill.
4. **Fácil de evoluir.** Se um dia quiser scaling, dá para adicionar um
   multiplicador por nível num único lugar (`Stats`), sem retrabalho.

---

## 1. Fase 1 — XP e nível (núcleo)

### 1.1 Componente `Experience` (`_application/character/experience.gd`)

Componente-nó no molde de `Health` (adicionado à cena `player_character.tscn`
como `$Experience`; acesso tipado `player_character.experience`).

```gdscript
class_name Experience
extends Node

signal xp_gained(amount: int, current_xp: int, xp_to_next: int)
signal leveled_up(new_level: int)

@export var level_curve: LevelCurve   # Resource com a tabela de XP

var level: int = 1
var current_xp: int = 0

func add_xp(amount: int) -> void   # acumula, resolve múltiplos level-ups, emite sinais
func xp_to_next_level() -> int
```

### 1.2 `LevelCurve` (`_application/progression/resource/level_curve.gd`)

`Resource` com `@export var xp_per_level: Array[int]` (tabela explícita, valores
fixos — sem fórmula). Último valor repetido para níveis além da tabela (ou
`max_level` quando a tabela acabar).

### 1.3 XP por inimigo

- `EnemyCharacter`: `@export var xp_reward: int = 5` (fixo, por cena de inimigo).
- Ponto de concessão: `GridController._on_enemy_died(enemy)` já existe e recebe
  o inimigo → `player_character.experience.add_xp(enemy.xp_reward)`.
- Futuro: `WallEntity`/outros destrutíveis podem exportar `xp_reward` também
  (mover o export para `GridEntity` com default 0 se fizer sentido).

### 1.4 UI e feedback

- Barra de XP + label de nível no topo (layout retrato; ao lado da vida se houver).
- Level-up: texto "LEVEL UP!" (reusar padrão do `DamageText`), `Haptics`
  nova entrada `level_up()` (vibração longa/forte, celebratória).
- Sinal `leveled_up` também concede ponto de trait (fase 3).

### Validação da fase

Teste headless `_headless_xp_test.gd`: matar inimigo → XP correto; acumular até
level-up → `leveled_up` emitido, sobra de XP preservada, múltiplos level-ups em
um `add_xp` grande.

---

## 2. Fase 2 — Stats e classes

### 2.1 Componente `Stats` (`_application/character/stats.gd`)

Hoje `attack_damage` é hardcoded no `PlayerCharacter` e `max_health` no `Health`.
Centralizar leitura de stats do jogador num componente:

```gdscript
class_name Stats
extends Node

var base_attack: int
var base_max_health: int
var base_crit_chance: float      # 0.0–1.0
var base_crit_multiplier: float  # ex.: 2.0

func get_attack() -> int          # base + soma de modificadores de traits
func get_max_health() -> int
func get_crit_chance() -> float
func get_crit_multiplier() -> float
func roll_damage() -> DamageResult  # aplica chance de crítico; retorna {amount, is_crit}
```

- Modificadores são registrados/removidos pelos traits (fase 3):
  `add_modifier(StatModifier)` / lista interna somada nos getters.
- `EnemyCharacter.on_attacked` passa a usar `player.stats.roll_damage()` em vez
  de `player_character.attack_damage`. Crítico: número de dano em cor/escala
  diferente no `DamageText` + `Haptics.attack_hit` mais forte.
- Inimigos NÃO precisam de `Stats` (valores fixos exportados na cena bastam) —
  só o jogador tem crescimento.

### 2.2 `ClassDefinition` (`_application/progression/resource/class_definition.gd`)

```gdscript
class_name ClassDefinition
extends Resource

@export var id: StringName
@export var display_name: String
@export var icon: Texture2D
@export var base_attack: int
@export var base_max_health: int
@export var base_crit_chance: float
@export var base_crit_multiplier: float
@export var trait_tree: Array[TraitDefinition]  # árvore da classe (fase 3)
@export var sprite_frames: SpriteFrames          # visual do jogador por classe (opcional)
```

Classes iniciais sugeridas (3 arquétipos claros):

| Classe | Perfil | Base |
|---|---|---|
| **Guerreiro** | tanque, troca dano na porrada | ATK médio, HP alto, crit baixo |
| **Ladino** | crítico e burst, frágil | ATK médio, HP baixo, crit alto |
| **Caçador de tesouros** | economia/itens | ATK baixo, HP médio, +drop de itens |

### 2.3 Seleção de classe

- Cena simples `class_select.tscn` antes da `run.tscn` (ou dialog inicial na
  própria run, reaproveitando o padrão do `GameOverDialog` + `set_input_locked`).
- Escolha vive num autoload leve `RunConfig` (só a `ClassDefinition` escolhida)
  ou é passada à run na troca de cena. `GridController._ready` aplica a classe:
  instancia o player e inicializa `Stats`/`Health` a partir do resource.

### Validação da fase

Player nasce com stats da classe; ataque usa `roll_damage`; crítico ocorre na
frequência esperada (teste headless com seed fixa / N amostras).

---

## 3. Fase 3 — Árvore de traits

### 3.1 `TraitDefinition` (`_application/progression/resource/trait_definition.gd`)

```gdscript
class_name TraitDefinition
extends Resource

@export var id: StringName
@export var display_name: String
@export var description: String
@export var icon: Texture2D
@export var max_ranks: int = 1
@export var required_level: int = 1
@export var prerequisites: Array[TraitDefinition]  # arestas da árvore
@export var modifiers: Array[StatModifier]          # aplicados por rank
@export var unlocked_items: Array[ItemBase]         # itens que entram na pool de drop ao adquirir
```

Traits têm **dois efeitos possíveis** (um trait pode ter ambos):

1. **Modificadores de stats** (`modifiers`) — buffs/debuffs numéricos.
2. **Desbloqueio de itens** (`unlocked_items`) — a partir da aquisição do trait,
   esses itens passam a poder dropar de baús e monstros. É assim que a "maneira
   de jogar" da classe molda a economia da run: os traits escolhidos definem
   quais itens existem para o jogador.

### 3.2 `StatModifier` (`_application/progression/resource/stat_modifier.gd`)

```gdscript
class_name StatModifier
extends Resource

@export var stat: Stat            # enum: ATTACK, MAX_HEALTH, CRIT_CHANCE, CRIT_MULTIPLIER, ITEM_DROP_CHANCE, ...
@export var amount: float         # pode ser NEGATIVO → debuff (trade-off)
```

Debuffs são só modificadores negativos no mesmo trait — ex.: "Fúria: +4 ATK,
−10 HP máx". Nenhum sistema extra necessário.

### 3.3 Componente `TraitSheet` (`_application/progression/trait_sheet.gd`)

Estado dos traits do jogador na run:

- `ranks: Dictionary[StringName, int]` — rank atual de cada trait adquirido.
- `is_eligible(trait, level) -> bool` (nível, pré-requisitos, rank < max).
- `get_eligible_traits(class, level) -> Array[TraitDefinition]` — filtra
  `class.trait_tree` pelos elegíveis no nível atual (usado pelo sorteio de
  opções no level-up).
- `unlock(trait)` → registra rank, aplica `modifiers` no `Stats`
  (MAX_HEALTH também cura o delta ganho, para o buff ser sentido na hora;
  ITEM_DROP_CHANCE modifica o `item_drop_chance` efetivo do `GridController`)
  e adiciona `unlocked_items` à pool de drop.
- `get_unlocked_items() -> Array[ItemBase]` — união dos `unlocked_items` de
  todos os traits adquiridos (sem duplicatas). Fonte única consultada pelo
  sistema de drop (ver 3.6).

> Não há mais pontos acumuláveis: a aquisição é orientada a **escolha
> obrigatória no level-up** (ver 3.4).

### 3.4 UI de traits — escolha no level-up e inspeção

- A cada `leveled_up`, o `GridController` sorteia 3 traits elegíveis da
  classe atual (`TraitSheet.get_eligible_traits`), mostra o overlay modal
  `TraitChoiceOverlay` e o jogador escolhe um obrigatoriamente. Múltiplos
  level-ups em sequência serializam numa fila (uma escolha por nível).
- O `ClassDefinition.trait_tree` agora funciona como a **pool sorteável** da
  classe (não é mais uma árvore navegável).
- Botão persistente na HUD abre o `TraitTreePanel` como **inspeção
  somente-leitura** (mostra ranks atuais/max de cada trait da classe).
- Enquanto o overlay de escolha ou a inspeção estão abertos:
  `grid_controller.set_input_locked(true)` (mecanismo já existe).

### 3.6 Pool de drop dinâmica

Hoje a pool é o export `possible_items` do `GridController`, sorteada pelo
`ItemDropRoller` em dois pontos: morte de inimigo (`_on_enemy_died`) e baú
(`ChestEntity`). Passa a ser composta em camadas:

```
pool efetiva = itens_base (possible_items, sempre disponíveis)
             + trait_sheet.get_unlocked_items()      # habilitados por traits
             , filtrada por min_level (fase 4.2)
```

- Um helper no `GridController` (`_get_effective_item_pool() -> Array[ItemBase]`)
  monta essa união e é usado nos dois pontos de drop — `ChestEntity` já recebe o
  controller em `on_player_enter` (hoje lê `grid_controller.possible_items`
  direto), então nenhum acoplamento novo.
- Itens exclusivos de trait ficam **fora** de `possible_items` e referenciados
  apenas no `TraitDefinition` correspondente.
- Sugestões de itens trait-gated por classe:
  - **Guerreiro:** Poção de fúria (próximo ataque ×2), Pedra de amolar (+ATK por N movimentos).
  - **Ladino:** Adaga de arremesso (dano à distância), Fumaça (próximo contra-ataque não acerta).
  - **Caçador:** Ímã de tesouro (converte inimigo em baú), Picareta (quebra muro à distância).

### 3.4 UI da árvore

> Substituído pela escolha obrigatória no level-up + inspeção somente-leitura
> (ver seção 3.4 acima, reescrita).

### 3.5 Exemplos de traits por classe (valores fixos, ilustrativos)

- **Guerreiro:** Pele de pedra (+8 HP máx), Contra-golpe (reflete 1 de dano),
  Fúria (+4 ATK / −10 HP máx), Alquimia de guerra (desbloqueia Poção de fúria e
  Pedra de amolar na pool de drop).
- **Ladino:** Lâmina afiada (+10% crit), Golpe duplo (crit multiplier 2.0→2.5),
  Vidro afiado (+15% crit / −20% HP máx), Arsenal oculto (desbloqueia Adaga de
  arremesso e Fumaça).
- **Caçador:** Faro de ouro (+8% drop), Mochila (+1 slot de item),
  Avareza (+15% drop / −2 ATK), Kit de escavação (desbloqueia Ímã de tesouro e
  Picareta).

> Traits condicionais (ex.: Execução: +100% dano em inimigos com ≤3 HP) pedem
> hooks além de `StatModifier`; deixar para uma sub-fase 3.7 com `TraitEffect`
> (Resource com métodos virtuais, mesmo padrão do `ItemBase`). Começar com
> modificadores planos + desbloqueio de itens.

### Validação da fase

Headless: level-up dá ponto; unlock respeita pré-requisitos/nível; modificadores
somam nos getters do `Stats`; debuff aplica; HP máximo ajusta o `Health` vivo;
item trait-gated não dropa antes do unlock e entra na pool efetiva depois
(inimigo E baú).

---

## 4. Fase 4 — Progressão de conteúdo por nível

### 4.1 `SpawnTable` (`_application/progression/resource/spawn_table.gd`)

Substituir os exports `*_spawn_weight` do `GridController` por uma tabela:

```gdscript
class_name SpawnEntry
extends Resource
@export var scene: PackedScene
@export var weight: int
@export var min_level: int = 1
@export var max_level: int = -1   # -1 = sem teto (permite "aposentar" inimigos fracos)
```

- `GridController._make_random_content()` filtra entradas por
  `player_character.experience.level` antes do sorteio ponderado.
  A lógica de roleta já existe — só muda a fonte dos pesos.
- **`Grid` não muda**: continua recebendo a mesma `Callable` fábrica.

### 4.2 Pool de itens por nível

- `ItemBase`: `@export var min_level: int = 1`.
- O filtro por nível é aplicado sobre a **pool efetiva** (base + itens de traits,
  ver 3.6) em `_get_effective_item_pool()` — nível e trait são dois portões
  independentes do mesmo funil.

### 4.3 Novos inimigos (sugestões, valores fixos)

| Nível | Inimigo | Gimmick |
|---|---|---|
| 1 | Slime (atual) | baseline |
| 3 | Arqueiro | contra-ataca mesmo sem ser adjacente (fila/coluna) |
| 5 | Tanque | 2× HP, dá 1 passo a cada 2 repopulações (pesado) |
| 7 | Elite/Campeão | buffa inimigos adjacentes (+1 dano); XP alto |
| 9 | Mímico | aparenta baú; revela ao ser atacado/adjacente |
| 12 | Ninho/Spawner | a cada N movimentos, spawna slime adjacente |

### 4.4 Novas células/entidades (sugestões)

**Entidades (conteúdo, encaixam no sistema atual `GridEntity`):**

1. **Cristal de XP** — atacável com 1 HP; não contra-ataca; dá só XP. Cria
   decisão "rota de XP vs rota segura".
2. **Fonte de cura** — mover para ela cura N HP fixo e a consome.
3. **Bomba com pavio** — contador em movimentos; explode em área (dano no
   jogador também). Sinergia forte com o padrão direcional: dá para "empurrá-la"
   para longe/perto com seus movimentos.
4. **Altar** — troca fixa: sacrifica X HP → ganha Y XP (ou um item). Escolha
   opcional ao entrar.
5. **Portal (par)** — entrar teleporta ao portal gêmeo; reposiciona o jogador e
   redefine a direção do fluxo do board.
6. **Muro rachado** — variante do `WallEntity`: 2 hits, drop garantido.

**Terrenos (novo eixo: propriedade da `Cell`, coexiste com conteúdo):**

7. **Espinhos** — dano fixo ao entrar; persiste alguns turnos.
8. **Gelo** — ao entrar, o jogador desliza mais uma célula na mesma direção se
   estiver livre.
9. **Célula amaldiçoada** — debuff temporário (ex.: −crit por 3 movimentos).
10. **Santuário** — pisar remove debuffs / pequena cura por turno parado.

> Terrenos exigem estender `Cell` com um `terrain: TerrainDefinition` (visual de
> fundo + hook `on_entered(player)`), deslizando junto com o conteúdo **ou**
> fixo na célula (mais simples e mais legível — recomendado). É uma fase própria;
> não misturar com a 4.1–4.3.

### Validação da fase

Headless: com nível 1 só spawna tier 1; forçar nível N → tabela filtra certo;
pesos somam; item pool respeita `min_level`.

---

## 5. Ordem de execução e dependências

```
Fase 1 (XP/nível)  ──►  Fase 2 (Stats/classes)  ──►  Fase 3 (traits)
        │
        └─────────►  Fase 4.1–4.3 (spawn por nível)   [depende só da fase 1]
                              └──►  Fase 4.4 terrenos  [fase própria, por último]
```

- Fases 1 e 4.1 são o MVP jogável da progressão.
- Estrutura de pastas nova: `_application/progression/` (componentes) e
  `_application/progression/resource/` (Resources de dados), espelhando
  `_application/item/`.

## 6. Premissas preservadas

- `Grid`/`Cell` não conhecem XP, nível, classe ou traits (exceto terrenos na
  4.4, que são visuais/hooks genéricos na `Cell`).
- Todos os dados de balance em `Resource`s editáveis via spreadsheet view.
- Componentes acessados de forma tipada (`player.experience`, `player.stats`).
- Cada fase termina com teste headless próprio + validação manual no editor
  (checklist do `AGENTS.md`).