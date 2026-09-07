---
tags:
  - analise
  - codigo
  - melhoria
created: 2026-07-12
aliases:
  - Análise de Melhorias
---

# Análise de Melhorias — `grid_battle`

Análise do código-fonte do jogo `grid_battle` (Godot 4.7, GDScript) com foco em arquitetura,
qualidade de código, performance e boas práticas Godot.

Ver também: [[plano_refatoracao]], [[racional_refatoracao]].

---

## 1. Architecture & SRP

### 1.1 `Grid.grid` (dicionário) é público

**Arquivo:** `_application/grid.gd:16`
**Problema:** `var grid: Dictionary[Vector2i, Cell] = {}` é um campo público, permitindo que
qualquer código externo acesse células diretamente (`grid.grid[pos]`). O `AGENTS.md` proíbe
explicitamente isso ("Código externo **não deve** acessar `grid.grid[pos]` diretamente"), mas
não há impedimento técnico.
**Sugestão:** Tornar `_grid` privado e criar um accessor `get_cell(pos) -> Cell` para uso
exclusivo de outros sistemas.

### 1.2 `Character` usa `get_node_or_null` para o sprite

**Arquivo:** `_application/character/character.gd:30`
**Problema:** `var sprite: CanvasItem = get_node_or_null("AnimatedSprite2D") as CanvasItem`
acopla o código ao nome exato do nó na cena. Se o nó for renomeado, o dano tween simplesmente
não funciona (silêncio).
**Sugestão:** Usar `@onready var sprite: AnimatedSprite2D = $AnimatedSprite2D`.
> ✅ **CORRIGIDO:** `character.gd` agora usa `@onready var sprite: AnimatedSprite2D = $AnimatedSprite2D`.

### 1.3 `EnemyCharacter` acessa `player_character` diretamente via controller

**Arquivo:** `_application/character/enemy_character.gd:16-23`
**Problema:** `grid_controller.player_character.attack_damage` e
`grid_controller.player_character.health.take_damage(...)` acoplam o inimigo a ambos
`GridController` e `PlayerCharacter`. Se houver múltiplos personagens ou a API mudar, quebra.
**Sugestão:** `on_attacked` deveria receber os parâmetros que precisa (dano, etc.) em vez de
navegar pelo controller.

### 1.4 `GridEntity.get_health()` retorna `null` — `Character` não sobrescreve

**Arquivo:** `_application/entity/grid_entity.gd:30-31`, `_application/character/character.gd:22-23`
**Problema:** `Character` tem `get_health()` mas **não sobrescreve** `GridEntity.get_health()`.
Quando código de dano em área faz `(content as GridEntity).get_health()`, o método chamado é o
da classe base (`GridEntity`), que retorna `null`. Isso é um **bug**: dano em área nunca afeta
personagens.
**Sugestão:** Adicionar `func get_health() -> Health: return health` em `Character` como
override, ou melhor, usar composição (um nó `Health` acessível por interface).
> ✅ **CORRIGIDO:** `Character` agora tem `func get_health() -> Health: return health` como override.

### 1.5 `WallEntity` duplica lógica de health bar de `Character`

**Arquivo:** `_application/entity/wall_entity.gd:4-13`
**Problema:** O padrão `@onready var health: Health = $Health` + conexão de sinais é copiado
de `Character` para `WallEntity`. Se a lógica mudar (ex.: animação de dano), ambos precisam
ser atualizados.
**Sugestão:** Extrair um componente/mixin compartilhado, ou fazer `Character` ser usado como
base para entidades com vida (em vez de `GridEntity`).

---

## 2. Bugs Comportamentais

### 2.1 `GridController.populate_grid` não usa `await`

**Arquivo:** `_application/grid_controller.gd:65`
**Problema:** `Grid.set_content` é async, mas é chamado sem `await`. As animações flip-in
nunca ocorrem durante a população inicial.
**Sugestão:** Adicionar `await` à chamada.
> ℹ️ **INTENCIONAL:** `set_content` marca ocupação lógica síncronamente antes da animação. A população inicial é fire-and-forget por design (sequenciar 35 flip-ins seria lento). O estado do jogo está correto; apenas as animações rodam em paralelo.

### 2.2 Ataque não aguarda animação de counter

**Arquivo:** `_application/grid_controller.gd:81`
**Problema:** `content.on_attacked(self, grid_position)` é async mas não é chamado com `await`.
A cascata de movimento/repovoamento pode ocorrer antes da animação de dano terminar.
**Sugestão:** Chamar com `await`.
> ✅ **CORRIGIDO:** `_on_cell_tapped` agora usa `await content.on_attacked(self, grid_position)`.

### 2.3 `run.tscn` serializa `highlight_color = null` e `highlight_enabled = null`

**Arquivo:** `_application/run.tscn:318-319`
**Problema:** O editor salvou valores `null` como override de export, mas os defaults no script
são não-null. Isso pode causar comportamento inesperado.
**Sugestão:** Abrir o inspector do `Grid` em `run.tscn` e resetar esses campos para o padrão.

---

## 3. Code Smells

### 3.1 `Grid._ready()` acionado por `@export_tool_button` — anti-pattern

**Arquivo:** `_application/grid.gd:14`
**Problema:** O botão de reset chama `_ready()` diretamente, que libera todos os filhos com
`.free()` e os recria. Qualquer referência externa às células antigas vira dangling. Além
disso, `grid_ready.emit()` é disparado de novo.
**Sugestão:** Extrair a lógica de setup para um método `_rebuild_grid()` separado.

### 3.2 Operadores `&&` e `||` em `grid.gd`

**Arquivo:** `_application/grid.gd:163`
**Problema:** `return (abs(delta.x) == 1 && delta.y == 0) || (abs(delta.y) == 1 && delta.x == 0)`
usa `&&`/`||` (C-style) em vez de `and`/`or` (GDScript), inconsistente com o resto do projeto.
**Sugestão:** Usar `and`/`or` por consistência.

### 3.3 `grid_size` é `Vector2` em vez de `Vector2i`

**Arquivo:** `_application/grid.gd:9`
**Problema:** Todas as posições no grid usam `Vector2i`, mas `grid_size` é `Vector2` (float).
Isso força casts em `int(grid_size.x)` em vários lugares.
**Sugestão:** Mudar para `@export var grid_size: Vector2i = Vector2i(6, 6)`.

### 3.4 Magic number: posição inicial do jogador é hardcoded

**Arquivo:** `_application/grid_controller.gd:27`
**Problema:** `Vector2i(2, 1)` é uma posição que depende do grid ser 6×6. Se o grid mudar,
essa posição pode ficar estranha.
**Sugestão:** Tornar `@export` ou derivar do `grid_size`.

### 3.5 `set_content` parâmetro `Node` deveria ser `Node2D`

**Arquivo:** `_application/cell.gd:103`
**Problema:** O parâmetro é `Node`, mas todo conteúdo passado é `Node2D`. O AGENTS.md diz que
"conteúdo de célula é um `Node2D`".
**Sugestão:** Mudar para `func set_content(node: Node2D) -> void`.

### 3.6 Dead code: `TopHContainer` em `run.tscn`

**Arquivo:** `_application/run.tscn:176-214`
**Problema:** Subárvore `TopHContainer/ItemSlotContainer` inteira com `visible = false` e sem
scripts. É UI morta.
**Sugestão:** Remover a subárvore.

### 3.7 `ItemSlot.temp_consumable` export paralelo a `item`

**Arquivo:** `_application/ui/item_slot.gd:4-14`
**Problema:** `temp_consumable` é um export separado que é copiado para `item` em `_ready()`.
Os dois podem ficar fora de sincronia depois do uso em runtime.
**Sugestão:** Usar `@export var item: ItemBase` diretamente.

---

## 4. Performance

### 4.1 `_process()` roda em células mesmo quando não destacadas

**Arquivo:** `_application/cell.gd:93`
**Problema:** `_process` é ativado/desativado globalmente. Se qualquer célula está destacada,
todas as células com highlight ativado rodam `_process`. Em grids grandes (ex.: 10×10 = 100
células), mesmo com apenas 4 destacadas, todas executam a cada frame.
**Sugestão:** Já está sendo usado `set_process`, o que é bom — não há problema real enquanto
poucas células estiverem ativas.

### 4.2 `set_highlighted_positions` itera todas as células

**Arquivo:** `_application/grid.gd:216-221`
**Problema:** `for cell_position in grid.keys()` percorre todas as células mesmo quando só
algumas precisam ser alteradas. Chamado a cada frame via `_process`.
**Sugestão:** Para grids pequenos (6×6) é irrelevante. Se crescer, otimizar iterando apenas
as posições de entrada.

---

## 5. Godot-Specific Pitfalls

### 5.1 `Grid._ready()` usa `.free()` em vez de `.queue_free()`

**Arquivo:** `_application/grid.gd:20`
**Problema:** `child.free()` é imediato e pode crashar se um child tiver tweens ou sinais
pendentes. `.queue_free()` é mais seguro.
**Sugestão:** Usar `child.queue_free()`.

### 5.2 Conexões de sinal com lambda não podem ser desconectadas

**Arquivo:** `_application/grid.gd:29`
**Problema:** `cell.cell_tapped.connect(func(): _on_cell_tapped(Vector2i(x, y)))` cria lambdas
que não podem ser passadas para `disconnect()`.
**Sugestão:** Usar `cell.cell_tapped.connect(_on_cell_tapped.bind(Vector2i(x, y)))` — bind
cria um Callable que pode ser desconectado mais tarde.

### 5.3 `damage_text` anexado a `current_scene` — pode ser nulo

**Arquivo:** `_application/character/character.gd:59`
**Problema:** `get_tree().current_scene.add_child(damage_text)` pode crashar se `current_scene`
for nulo durante reload/teardown.
**Sugestão:** Verificar null antes de adicionar, ou usar `get_tree().root.add_child(...)`.
> ✅ **CORRIGIDO:** `_spawn_damage_number` agora tem guarda `if not is_inside_tree() or get_tree().current_scene == null: return`.

### 5.4 `TouchScreenButton` requer `emulate_touch_from_mouse=true`

**Arquivo:** `_application/cell.gd:80`
**Problema:** `TouchScreenButton` só funciona com input touch. O projeto depende de
`emulate_touch_from_mouse=true` para funcionar com mouse. Se essa flag for removida, o jogo
para de responder.
**Sugestão:** Usar `Area2D` + `InputEventMouse`/`InputEventScreenTouch` para suporte a ambos.

---

## 6. Experiência do Projeto / Manutenibilidade

### 6.1 AGENTS.md e docs desatualizados

**Arquivo:** `AGENTS.md`, `docs/refactor_plan.md`
**Problema:** A documentação ainda referencia `CellVisual` como classe existente, mas ela foi
removida (agora `Cell` usa `Sprite2D` diretamente). Isso confunde novos agentes/desenvolvedores.
**Sugestão:** Atualizar `AGENTS.md` e `docs/` para refletir o estado atual do código.

### 6.2 Health.take_damage emite `died` repetidamente

**Arquivo:** `_application/character/health.gd:21-32`
**Problema:** Se `take_damage` é chamado quando `current_health` já é 0, o sinal `died` é
emitido de novo. Não há guarda de "já está morto".
**Sugestão:** Adicionar `if not is_alive: return` no início de `take_damage`.
> ✅ **CORRIGIDO:** `take_damage` agora tem `if not is_alive(): return` no início.

### 6.3 `cell_tapped` em `Grid` e em `Cell` têm o mesmo nome

**Arquivo:** `_application/grid.gd:6` e `_application/cell.gd:7`
**Problema:** Ambos os sinais se chamam `cell_tapped`. Isso não quebra nada, mas é confuso
durante debug.
**Sugestão:** Renomear um dos dois (ex.: `Cell.cell_tapped` → `Cell.tapped`, ou
`Grid.cell_tapped` → `Grid.cell_selected`).
> ✅ **CORRIGIDO:** `Cell` agora tem sinal `tapped` (renomeado de `cell_tapped`). `Grid` mantém `cell_tapped`.
