---
tags:
  - plano
  - grid
  - refatoracao
created: 2026-07-12
aliases:
  - Plano de Refatoração Grid/Cell/GridController
---

# Refactor Plan — Grid / Cell / GridController

> Directive plan for an AI coding agent. Godot 4.7, GDScript. Execute phases in order.
> After each phase: open the project in Godot and confirm it runs with no errors.
> Do NOT change gameplay behavior — this is a structural refactor only.

Ver também: [[racional_refatoracao]] (explicação em português), [[analise_melhoria]].

## Files in scope
- `_application/grid.gd` (class `Grid`, extends `Node2D`, `@tool`)
- `_application/cell.gd` (class `Cell`, extends `Node2D`, `@tool`)
- `_application/cell_visual.gd` (class `CellVisual`) — leave as is
- `_application/grid_controller.gd` (class `GridController`, extends `Node`)
- `_application/character/character.gd`, `health.gd`, `enemy_character.gd`, `player_character.gd`

## Target architecture
- **Cell** = self-contained view/animation unit for ONE cell. Internal to Grid. External code must NOT touch `grid.grid[pos]` directly.
- **Grid** = the single public façade + owner of ALL grid topology/query logic. Position-based API. Delegates rendering/animation to its cells.
- **GridController** = gameplay orchestration only. Expresses intent via Grid's API. Holds NO grid-topology math (no `grid_size` loops, no border math, no step/distance math).

---

## Phase 1 — Move grid topology/query helpers into `Grid`
Add these bounds-safe query methods to `grid.gd`:

```gdscript
func is_within_bounds(pos: Vector2i) -> bool
func get_all_positions() -> Array[Vector2i]
func get_border_positions() -> Array[Vector2i]          # x==0 or x==max_x or y==0 or y==max_y
func get_content_positions() -> Array[Vector2i]         # positions whose cell has_content()
func get_empty_positions() -> Array[Vector2i]
func get_adjacent_positions(pos: Vector2i) -> Array[Vector2i]  # 4-neighbourhood, in-bounds only
func distance(a: Vector2i, b: Vector2i) -> int          # Manhattan
func step_towards(from: Vector2i, to: Vector2i) -> Vector2i    # move `_step_towards` here verbatim
```
- `is_adjacent(a, b)` already exists in Grid — keep it.
- Compute `max_x`/`max_y` from `grid_size` once via a small helper to avoid repeated `int(grid.grid_size.x)`.

## Phase 2 — Consolidate Grid's content API vocabulary
Rename Grid's public content methods to a single consistent vocabulary, dropping the redundant `_position`/`_node` verbosity. Each stays a bounds-checked delegator to the matching `Cell` method:

| Current Grid method              | New Grid method            | Delegates to Cell |
|----------------------------------|----------------------------|-------------------|
| `attach_node_to_position(p, n)`  | `set_content(p, n)`        | `set_content`     |
| `detach_node_from_position(p)`   | `clear_content(p)`         | `clear_content`   |
| `get_position_content(p)`        | `get_content(p)`           | `get_content`     |
| `position_has_content(p)`        | `has_content(p)`           | `has_content`     |
| `move_position_content(a, b)`    | `move_content(a, b)`       | (compose)         |
| `shake_position(p)`              | `shake(p)`                 | `shake`           |

- Names may match Cell's because they live in different scopes (`grid.set_content(p, n)` vs `cell.set_content(n)`); this is delegation, not duplication.
- Rename `set_enabled_cells_outline(...)` → `set_outlined_positions(positions: Array[Vector2i])`.
- Update ALL call sites in `grid_controller.gd`.

## Phase 3 — Add reusable behavior primitives to `Grid`
Move cascade-move + border-fill out of the controller into Grid, parameterized (not player-specific):

```gdscript
## Moves every occupied cell (except `exclude`) one step towards `target`,
## nearest-first, skipping cells whose destination is occupied.
func move_all_content_towards(target: Vector2i, exclude: Vector2i) -> void
```
- Body = current `_move_positions_towards_player`, using Phase-1 helpers (`get_content_positions`, `distance`, `step_towards`, `move_content`) and the occupancy-reservation dictionary. Keep nearest-first sort and reservation logic exactly.

```gdscript
## Fills every empty border cell with a node produced by `factory`.
## `factory` is a Callable returning a Node; `exclude` is skipped.
func fill_empty_border(factory: Callable, exclude: Vector2i) -> void
```
- Body = current `_spawn_enemies_at_empty_border_positions`, but Grid must NOT know about enemies — the controller passes a factory closure that instantiates a random enemy.

## Phase 4 — Slim down `GridController`
- Delete `_step_towards`, `_move_positions_towards_player`, `_spawn_enemies_at_empty_border_positions` (now in Grid).
- `_move_player_character` becomes:
  ```gdscript
  await grid.move_content(_current_player_character_position, new_position)
  _current_player_character_position = new_position
  await grid.move_all_content_towards(_current_player_character_position, _current_player_character_position)
  await grid.fill_empty_border(_make_random_enemy, _current_player_character_position)
  ```
- Replace the adjacent-cell array in `_process` with `grid.get_adjacent_positions(_current_player_character_position)`.
- Replace `populate_grid_with_enemies` internals to use `grid.get_all_positions()` / helpers instead of raw `grid_size` loops.
- Add `func _make_random_enemy() -> EnemyCharacter` returning an instantiated random enemy from `enemy_character_scenes`.

## Phase 5 — Fix leaky abstraction (Character/Health)
- In `_on_cell_tapped`, replace `enemy_character.get_node("Health") as Health` with `enemy_character.health` (already exposed by `Character` via `@onready var health := $Health`).
- Confirm `character.gd` uses `$Health` so `character.health` is valid.

## Phase 6 — Verification
- Open project in Godot; ensure no parse/type errors in Output.
- Run the game (F5). Manually verify:
  1. Enemies populate on start (player cell empty).
  2. Moving the player makes all content step toward the player, no gaps mid-grid.
  3. Empty border cells refill with enemies each move.
  4. Attacking an adjacent enemy damages it (damage tween plays) and removes it at 0 HP.
  5. Shake plays when tapping a non-adjacent cell.

## Constraints
- Preserve all existing `await`/animation timing semantics.
- Keep `@tool` behavior in Cell/Grid intact (editor_hint owner assignment).
- No new external dependencies.
- Do not rename `class_name` identifiers without updating every reference.