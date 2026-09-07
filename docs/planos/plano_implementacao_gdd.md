---
tags:
  - plano
  - skill
  - gdd
  - implementacao
created: 2026-07-12
aliases:
  - Plano de Implementação do GDD
status: planejado
---

# Plano: Implementação do GDD — Skills, Lacunas e Melhorias

> Plano diretivo para agentes de IA. Godot 4.7, GDScript. Executar fases em
> ordem. Após cada fase: abrir o projeto no Godot e confirmar que roda sem
> erros. Respeitar as premissas do `AGENTS.md` (Grid é fachada agnóstica,
> GridController só orquestra, mutações visuais são awaitable, tipagem
> estática obrigatória).

Ver também: [[GDD]], [[plano_progressao]], [[plano_refatoracao]], [[plano_entidades]], [[talentos]], [[ideias_traits_itens]], [[analise_melhoria]].

---

## 0. Resumo executivo — Análise GDD vs Código

### 0.1 O que já está implementado

| Sistema do GDD | Status | Arquivos |
|---|---|---|
| Grid / Cell / GridController | ✅ Completo | `grid.gd`, `cell.gd`, `grid_controller.gd` |
| Combate baseado em turnos (mover/atacar) | ✅ Completo | `grid_controller.gd`, `enemy_character.gd` |
| Repopulação direcional do grid | ✅ Completo | `grid.gd` (`shift_all_content`, `fill_empty_border`) |
| Health (dano, cura, morte, counter) | ✅ Completo | `health.gd` |
| XP / Níveis / Level curve | ✅ Completo | `experience.gd`, `level_curve.gd` |
| Stats (attack, crit, max_health, drop chance) | ✅ Completo | `stats.gd`, `stat_modifier.gd` |
| Classes (Guerreiro, Mago, Ladino) | ✅ Completo | `class_definition.gd`, `resource/class/*.tres` |
| Traits (modificadores + desbloqueio de itens) | ✅ Completo | `trait_definition.gd`, `trait_sheet.gd`, `resource/trait/*.tres` |
| Escolha obrigatória de trait no level-up | ✅ Completo | `trait_choice_overlay.gd` |
| Itens consumíveis (cura) | ✅ Completo | `health_regen_item.gd` |
| Itens ativos (dano em área, 6 shapes) | ✅ Completo | `area_effect_item_base.gd`, `damage_area_effect_item.gd` |
| Sistema de targeting (seleção de alvo) | ✅ Completo | `item_utilizer.gd` |
| Drops (pool base + trait-gated, filtro por nível) | ✅ Completo | `item_drop_roller.gd`, `grid_controller.gd` |
| Entidades (baú, muro, armadilha, altar, armadilha direcional) | ✅ Completo | `entity/*.gd` |
| Inimigos (slime, arqueiro, tanque, elite, mímico) | ✅ Completo | `character/*.gd` |
| Spawn por nível (SpawnEntry com min/max_level) | ✅ Completo | `spawn_entry.gd` |
| HUD (nível, XP, traits, slots de item) | ✅ Completo | `ui/progression_hud.gd`, `ui/item_slot*.gd` |
| Seleção de classe | ✅ Completo | `class_select_overlay.gd` |
| Game Over | ✅ Completo | `game_over_dialog.gd` |

### 0.2 O que falta (lacunas do GDD)

| Lacuna | Severidade | Fase |
|---|---|---|
| **Sistema de Skills** (ações reutilizáveis com cooldown) | 🔴 Crítica | Fase 1 |
| **UI de Skills** (botões na HUD, cooldown visual) | 🔴 Crítica | Fase 2 |
| **Traits que desbloqueiam skills** (não só itens) | 🔴 Crítica | Fase 3 |
| **Skills DEFENSIVAS** (escudo/buff no Health) | 🟡 Média | Fase 4 |
| **Skills UTILITY** (teleporte, troca) | 🟡 Média | Fase 4 |
| **Preencher seções template do GDD** (core loop, economia, etc.) | 🟢 Baixa | Fase 6 |

### 0.3 A divergência central: Skills vs Itens

O GDD define **skills** como ações reutilizáveis com cooldown, vinculadas à
classe, desbloqueadas via traits. Hoje, os efeitos descritos como skills no GDD
(Bola de Fogo, Nova de Gelo, Adaga de Arremesso, etc.) existem apenas como
**itens consumíveis** (`DamageAreaEffectItem`), desbloqueados por traits via
`unlocked_items`.

| Aspecto | Skill (GDD) | Item Ativo (atual) |
|---|---|---|
| Reutilizável? | ✅ Sim (cooldown) | ❌ Não (consumido ao usar) |
| Vinculado à classe? | ✅ Sim | ❌ Não (qualquer classe pode dropar) |
| Desbloqueado por trait? | ✅ Sim | ✅ Sim (via `unlocked_items`) |
| Tem cooldown? | ✅ Sim (turnos) | ❌ Não |
| Tem área/shape? | ✅ Sim | ✅ Sim (mesmas 6 formas) |
| Tem dano? | ✅ Sim | ✅ Sim |
| UI | Botão de skill na HUD | Slot de item na HUD |

**Conclusão:** A infraestrutura de área/shape/dano já existe em
`AreaEffectItemBase` e `DamageAreaEffectItem`. O sistema de skills deve **reaproveitar**
essa infraestrutura, não duplicá-la. A diferença fundamental é: skills são
reutilizáveis (com cooldown) e vinculadas ao personagem; itens são consumíveis.

---

## 1. Fase 1 — Sistema de Skills (núcleo)

### 1.1 `SkillDefinition` (`_application/progression/skill_definition.gd`)

Resource que define uma skill reutilizável. Reaproveita os mesmos conceitos de
`AreaEffectItemBase` (shape, size, range, damage) mas adiciona tipo e cooldown.

```gdscript
class_name SkillDefinition
extends Resource

enum SkillType { OFFENSIVE, DEFENSIVE, UTILITY }

@export var id: StringName
@export var display_name: String
@export_multiline var description: String
@export var icon: Texture2D
@export var skill_type: SkillType = SkillType.OFFENSIVE

## Área de efeito (mesmas formas de AreaEffectItemBase).
enum AreaShape { CIRCLE, CROSS, LINEAR, PERPENDICULAR, ARC, CONE }
@export var shape: AreaShape = AreaShape.CIRCLE
@export_range(1, 9, 2) var size: int = 3

## Distância Manhattan máxima para seleção de alvo. -1 = sem limite.
@export var max_range: int = -1

## Dano fixo (se ofensiva).
@export var damage: int = 0

## Número de ações do jogador entre usos (movimento ou ataque contam).
@export var cooldown: int = 1

## Se true, permite selecionar também o eixo perpendicular nas formas de linha.
@export var selection_is_cross_shaped: bool = false

## Nível mínimo do jogador para a skill estar disponível (se desbloqueada por trait).
@export var min_level: int = 1
```

> **Nota de design:** `SkillDefinition.AreaShape` é uma cópia intencional de
> `AreaEffectItemBase.AreaShape`. Num futuro refactor (Fase 5), ambas podem ser
> unificadas num `enum` compartilhado. Por ora, duplicar mantém as duas sistemas
> desacoplados e evita quebrar `.tres` existentes.

### 1.2 `SkillComponent` (`_application/character/skill_component.gd`)

Componente-nó no `PlayerCharacter` (como `Stats`, `Experience`, `TraitSheet`).
Mantém o repertório de skills disponíveis e o estado de cooldown de cada uma.

```gdscript
class_name SkillComponent
extends Node

signal skill_unlocked(skill: SkillDefinition)
signal cooldowns_changed

## Skills atualmente no repertório do personagem (desbloqueadas por traits).
var _skills: Array[SkillDefinition] = []
## Cooldown restante (em ações) por skill id. 0 = pronto.
var _cooldowns: Dictionary[StringName, int] = {}

func get_skills() -> Array[SkillDefinition]
func has_skill(skill: SkillDefinition) -> bool
func unlock(skill: SkillDefinition) -> void         # adiciona ao repertório
func can_use(skill: SkillDefinition) -> bool         # tem a skill E cooldown == 0
func get_cooldown_remaining(skill: SkillDefinition) -> int
func start_cooldown(skill: SkillDefinition) -> void  # seta cooldown = skill.cooldown
func tick_cooldowns() -> void                         # decrementa todos em 1 (chamado em notify_player_acted)
func reset_all_cooldowns() -> void
```

### 1.3 `SkillCaster` (`_application/item/skill_caster.gd`)

Orquestra o uso de uma skill: valida cooldown, inicia seleção de alvo (reusando
o fluxo do `ItemUtilizer`), aplica o efeito e dispara o cooldown. Estruturalmente
espelha `ItemUtilizer` — ambos compartilham o mesmo padrão de targeting.

```gdscript
class_name SkillCaster
extends Node

## Inicia o uso de `skill`. Se a skill tem seleção de alvo, entra em modo de
## targeting (igual ao ItemUtilizer). Se não, aplica imediatamente.
func cast_skill(skill: SkillDefinition) -> void
```

**Implementação do `apply`:** A skill causa dano usando a mesma lógica de
`DamageAreaEffectItem.apply` — iterar posições afetadas, aplicar dano em
entidades com `get_health()`, remover mortos, repovoar. **Extrair essa lógica
para um helper compartilhado** (ver Fase 5 — Melhoria A).

### 1.4 Cooldown ticking

O `GridController.notify_player_acted()` já é chamado após toda ação do jogador
(mover, atacar, usar item). Conectar o `SkillComponent.tick_cooldowns()` a esse
evento:

```gdscript
# Em GridController._ready(), após inicializar o player:
player_character.skill_component # acessível via @onready

# Em notify_player_acted():
func notify_player_acted() -> void:
    for position in grid.get_content_positions():
        var content: GridEntity = grid.get_content(position) as GridEntity
        content.on_player_action(self)
    player_character.skill_component.tick_cooldowns()  # NOVO
```

### 1.5 Reaproveitamento da lógica de área

`AreaEffectItemBase` já implementa `get_target_positions`, `get_affected_positions`
e `get_positions_in_application_order`. Para evitar duplicação, **extrair essas
funções para um helper estático** ou fazer `SkillDefinition` delegar a um
`AreaEffectItemBase` interno (composição). A abordagem recomendada é a primeira:

```gdscript
# _application/util/area_calculator.gd
class_name AreaCalculator
extends RefCounted

static func get_target_positions(grid: Grid, player_position: Vector2i,
    shape: int, size: int, max_range: int, selection_is_cross_shaped: bool
) -> Array[Vector2i]

static func get_affected_positions(grid: Grid, player_position: Vector2i,
    target_position: Vector2i, shape: int, size: int
) -> Array[Vector2i]

static func get_positions_in_application_order(target_position: Vector2i,
    positions: Array[Vector2i]
) -> Array[Vector2i]
```

`AreaEffectItemBase` e `SkillDefinition`/`SkillCaster` chamam esses métodos
estáticos. Isso elimina a duplicação e centraliza a lógica de shapes.

### Validação da Fase 1

- Headless: skill desbloqueada aparece no `SkillComponent`; `can_use` é true
  inicialmente; após `start_cooldown`, `can_use` é false; após N `tick_cooldowns`,
  `can_use` volta a true.
- Skill ofensiva causa dano nas posições afetadas corretas (mesmo shape/size do
  item equivalente).
- Cooldown decrementa apenas com `notify_player_acted` (mover/atacar/usar item).

---

## 2. Fase 2 — UI de Skills

### 2.1 `SkillBar` (`_application/ui/skill_bar.gd`)

Barra de botões de skill na HUD, espelhando `ItemSlotContainer`. Cada botão
mostra ícone da skill + indicador de cooldown (overlay escurecido ou número).

```gdscript
class_name SkillBar
extends Container

func setup(player: PlayerCharacter) -> void          # conecta ao SkillComponent
func _refresh() -> void                                # reconstrói botões a partir de skill_component.get_skills()
```

- Cada `SkillButton` mostra o ícone da skill e, se em cooldown, um overlay
  escurecido proporcional ao cooldown restante / cooldown total.
- Botão desabilitado (ou visualmente escurecido) quando `can_use == false`.
- Ao pressionar: chama `skill_caster.cast_skill(skill)`.
- Sinal `cooldowns_changed` do `SkillComponent` dispara `_refresh()`.

### 2.2 Layout na HUD

A HUD atual (`ProgressionHud`) fica no topo. A `SkillBar` deve ir na parte
inferior da tela (orientação retrato), acima ou ao lado dos slots de item.
Posicionar via `run.tscn` como filho do layout existente.

### 2.3 Feedback visual/sonoro

- Ao castar: `Haptics` (reusar `explosion()` para ofensivas, criar
  `Haptics.skill_cast()` se desejar diferenciação).
- Animação de área no grid (já existe via `set_highlighted_cell_colors` no
  targeting, e `grid.shake()` nas células afetadas).
- Indicador de cooldown atualiza a cada `cooldowns_changed`.

### Validação da Fase 2

- Skills desbloqueadas aparecem como botões na SkillBar.
- Botão mostra cooldown visualmente (escurecido + número).
- Pressionar botão de skill ofensiva inicia targeting (se aplicável) ou aplica
  imediatamente.
- Cooldown visual atualiza após cada ação do jogador.

---

## 3. Fase 3 — Traits que desbloqueiam Skills

### 3.1 Estender `TraitDefinition`

Adicionar `unlocked_skills` paralelo a `unlocked_items`:

```gdscript
# Em trait_definition.gd, adicionar:
@export var unlocked_skills: Array[SkillDefinition] = []
```

### 3.2 Estender `TraitSheet`

- `get_unlocked_skills() -> Array[SkillDefinition]` — união dos
  `unlocked_skills` de todos os traits adquiridos.
- Em `unlock()`: além de aplicar modifiers e registrar itens, registrar skills
  no `SkillComponent` do jogador.

### 3.3 Conexão no GridController

No `_ready()`, após `player_character.initialize_class(chosen_class)`:

```gdscript
player_character.trait_sheet.trait_unlocked.connect(_on_trait_unlocked)

func _on_trait_unlocked(trait: TraitDefinition, _rank: int) -> void:
    for skill in trait.unlocked_skills:
        player_character.skill_component.unlock(skill)
```

### 3.4 Migrar traits de "item-gated" para "skill-gated"

Os traits do GDD que desbloqueiam efeitos de combate (Bola de Fogo, Nova de
Gelo, etc.) devem desbloquear **skills**, não apenas itens consumíveis. A
tabela do GDD (seção 6.3) mapeia cada skill ao trait que a concede:

| Classe | Skill | Trait | Antes (item) | Depois (skill) |
|---|---|---|---|---|
| Guerreiro | Golpe | Inicial | — | Skill inicial da classe |
| Guerreiro | Investida com Escudo | Muralha | `shield_bash.tres` (item) | `shield_bash` skill + item |
| Mago | Bola de Fogo | Grimório | `fireball.tres` (item) | `fireball` skill + item |
| Mago | Nova de Gelo | Criomancia | `frost_nova.tres` (item) | `frost_nova` skill + item |
| Mago | Tempestade Elétrica | Conjurador de Tempestades | `thunderstorm.tres` (item) | `thunderstorm` skill + item |
| Ladino | Adaga de Arremesso | Arsenal Oculto | `throwing_dagger.tres` (item) | `throwing_dagger` skill + item |
| Ladino | Bomba de Fumaça | Mestre das Sombras | `smoke_bomb.tres` (item) | `smoke_bomb` skill + item |
| Ladino | Adaga Envenenada | Mãos Ligeiras | `poison_dagger.tres` (item) | `poison_dagger` skill + item |
| Ladino | Linha Venenosa | Mestre do Veneno | `poison_line.tres` (item) | `poison_line` skill + item |

> **Decisão de design:** Manter AMBOS — o trait desbloqueia a skill (reutilizável,
> com cooldown) E o item consumível (na pool de drop). Isso alinha com o GDD:
> "Itens ativos podem ser dropados por inimigos ou encontrados em baús,
> permitindo ao jogador ter acesso temporário a efeitos poderosos mesmo sem ter
> o trait correspondente." O item é o "acesso temporário"; a skill é o "acesso
> permanente com cooldown".

### 3.5 Skills iniciais por classe

O GDD diz "Cada classe começa com 1-2 skills básicas". Adicionar a
`ClassDefinition`:

```gdscript
# Em class_definition.gd, adicionar:
@export var starting_skills: Array[SkillDefinition] = []
```

- **Guerreiro:** `Golpe` (OFENSIVA, CIRCLE size 1, dano 5, alcance 1, cooldown 1)
- **Mago:** `Bola de Fogo` (OFENSIVA, CIRCLE size 3, dano 10, alcance -1, cooldown 3)
  — ou uma skill básica mais fraca se preferir progressão.
- **Ladino:** `Adaga de Arremesso` (OFENSIVA, CIRCLE size 1, dano 6, alcance 3,
  cooldown 1)

> **Nota:** O GDD lista Bola de Fogo como desbloqueada via trait Grimório para o
> Mago, mas também diz que classes começam com 1-2 skills. Decisão: o Mago começa
> com uma skill básica (ex.: "Raio Arcano", dano 4, CIRCLE 1, alcance 2, cd 1) e
> Bola de Fogo vem via Grimório. Alternativamente, Bola de Fogo é a skill inicial
> e Grimório apenas adiciona o item consumível. Ajustar conforme balanceamento.

No `GridController._ready()`, após `initialize_class`:

```gdscript
for skill in chosen_class.starting_skills:
    player_character.skill_component.unlock(skill)
```

### Validação da Fase 3

- Adquirir um trait com `unlocked_skills` adiciona a skill à SkillBar.
- Skills iniciais da classe aparecem na SkillBar desde o início.
- Traits existentes continuam funcionando (itens ainda dropam).
- Trait que desbloqueia skill E item: skill aparece na barra, item continua na
  pool de drop.

---

## 4. Fase 4 — Skills Defensivas e Utility

### 4.1 Escudo no `Health` (DEFENSIVE)

O GDD menciona skills defensivas (escudo/cura/buff). A cura já existe
(`HealthRegenItem`). Escudo exige estender `Health`:

```gdscript
# Em health.gd, adicionar:
signal shield_changed(current_shield: int)

var shield: int = 0:
    set(value):
        shield = maxi(value, 0)
        shield_changed.emit(shield)

# Modificar take_damage para descontar do escudo primeiro:
func take_damage(amount: int, is_critical: bool = false) -> void:
    if amount <= 0 or not is_alive():
        return
    var absorbed: int = mini(amount, shield)
    shield -= absorbed
    amount -= absorbed
    if amount <= 0:
        shield_changed.emit(shield)
        return
    # ... resto igual (dano em HP)
```

- UI: indicador de escudo ao lado da barra de vida (overlay azul/cinza).
- Skills defensivas de exemplo:
  - **Escudo Arcano** (Mago, trait Barreira Arcana): ganha escudo 8, cooldown 3.
  - **Muralha de Ferro** (Guerreiro, trait Muralha rank 2): ganha escudo = ATK,
    cooldown 4.

### 4.2 Skills Utility

Skills que não causam dano nem curam, mas manipulam o estado do jogo. Requerem
`SkillType.UTILITY` e lógica de `apply` customizada (não usa o dano em área).

Exemplos do GDD e `ideias_traits_itens`:
- **Teleporte** (Mago): move o jogador para qualquer célula vazia dentro do
  alcance. `get_target_positions` retorna células vazias; `apply` move o
  jogador.
- **Troca** (Ladino): troca o jogador de posição com um inimigo. `apply` faz
  swap de conteúdo entre duas células.

Implementação: `SkillCaster` verifica `skill.skill_type`:
- `OFFENSIVE`: usa a lógica de dano em área (compartilhada com itens).
- `DEFENSIVE`: aplica cura/escudo diretamente no jogador (sem targeting, ou
  targeting = célula do próprio jogador).
- `UTILITY`: delega para um handler específico da skill (pode ser um método
  polimórfico ou um `SkillEffect` Resource, no padrão de `ItemBase.apply`).

> **Recomendação:** Para o MVP, implementar apenas OFFENSIVE + DEFENSIVE (cura/
> escudo). Utility (teleporte/troca) é fase posterior — exige lógica de
> targeting diferente e pode introduzir bugs no sistema de repopulação.

### Validação da Fase 4

- Skill defensiva (escudo) adiciona escudo ao `Health`; dano subsequente é
  absorvido pelo escudo antes do HP.
- Escudo aparece visualmente na HUD.
- (Opcional) Skill utility de teleporte move o jogador corretamente e
  repopulação funciona após.

---

## 5. Melhorias em Sistemas Existentes

> Identificadas durante a análise do código. Não são bloqueadoras para o GDD,
> mas melhoram manutenibilidade e corrigem bugs. Podem ser feitas em paralelo
> ou antes das Fases 1–4.

### Melhoria A — Extrair `DamageResolver` compartilhado (alta prioridade)

**Problema:** A lógica de "aplicar dano em posições afetadas, remover mortos,
repopular" está duplicada em:
- `damage_area_effect_item.gd` → `apply()`
- `directional_trap_entity.gd` → `on_attacked()` (explosão em linha)
- (Futuro) `skill_caster.gd` → `apply()`

**Solução:** Criar helper estático:

```gdscript
# _application/util/damage_resolver.gd
class_name DamageResolver
extends RefCounted

## Aplica `damage` em todas as entidades com Health nas posições afetadas,
## na ordem de aplicação (anel + horário). Remove mortos e repovoa o grid.
static func resolve_area_damage(
    grid_controller: GridController,
    positions_in_order: Array[Vector2i],
    damage: int
) -> void
```

Refatorar `DamageAreaEffectItem.apply` e `DirectionalTrapEntity.on_attacked`
para usar `DamageResolver`. `SkillCaster` usa o mesmo helper.

### Melhoria B — `grid_size` como `Vector2i` (baixa prioridade)

**Problema:** `grid_size` é `Vector2`, forçando `int(grid_size.x)` em vários
lugares. AGENTS.md diz que posições são `Vector2i`.

**Solução:** Mudar para `@export var grid_size: Vector2i = Vector2i(6, 6)`.
Atualizar todos os usos de `grid_size.x`/`grid_size.y` (já são int agora).

### Melhoria C — `Cell.set_content` tipar como `Node2D` (baixa prioridade)

**Problema:** `set_content(node: Node)` aceita qualquer Node, mas todo conteúdo
é `Node2D`. AGENTS.md: "conteúdo de célula é um `Node2D`".

**Solução:** Mudar assinatura para `func set_content(node: Node2D)` em `Cell` e
`Grid`.

### Melhoria D — `grid.grid` privado (média prioridade)

**Problema:** `var grid: Dictionary[Vector2i, Cell] = {}` é público. AGENTS.md
proíbe acesso direto mas não há impedimento técnico.

**Solução:** Renomear para `_grid` e adicionar `func get_cell(pos: Vector2i)
-> Cell` para uso interno do próprio Grid (não expor a clientes).

### Melhoria E — `&&`/`||` → `and`/`or` (trivial)

**Problema:** `grid.gd` usa `&&`/`||` (C-style) em vez de `and`/`or`.

**Solução:** Substituir em `is_adjacent()`.

### Melhoria F — Posição inicial do jogador como `@export` (baixa prioridade)

**Problema:** `_initial_player_character_coordinate = Vector2i(2, 1)` é
hardcoded e depende do grid ser 6×6.

**Solução:** `@export var initial_player_position: Vector2i = Vector2i(2, 1)`.

### Melhoria G — `ItemSlot.temp_consumable` (trivial)

**Problema:** `temp_consumable` é um export paralelo a `item`, copiado em
`_ready()`. Podem dessincronizar.

**Solução:** Usar `@export var item: ItemBase` diretamente. Remover
`temp_consumable`.

### Melhoria H — `Grid._ready()` chamado por tool button (baixa prioridade)

**Problema:** `@export_tool_button("Reset Grid")` chama `_ready()` diretamente,
que faz `.free()` em filhos. Referências externas viram dangling.

**Solução:** Extrair setup para `_rebuild_grid()` e fazer o botão chamar isso.

### Melhoria I — `populate_grid` sem `await` (análise)

**Status:** Na verdade, `set_content` marca ocupação lógica **síncronamente**
antes da animação. As animações flip-in da população inicial rodam em paralelo
(fire-and-forget). O estado do jogo está correto; apenas as animações não são
sequenciais. **Isso é comportamento intencional** (popular o grid inicial de
forma sequencial seria lento). Manter como está, mas documentar a decisão.

### Melhoria J — Health `died` emitido repetidamente (média prioridade)

**Problema:** `take_damage` quando `current_health` já é 0 pode emitir `died`
de novo.

**Status:** Já tem guarda `if not is_alive(): return` no início de
`take_damage`. **Já corrigido** — confirmar e fechar o item na
`analise_melhoria.md`.

### Melhoria K — `Character.get_health()` override (análise)

**Status:** `Character` já tem `func get_health() -> Health: return health`.
**Já corrigido** — o bug descrito em `analise_melhoria.md` já foi resolvido.

### Melhoria L — Unificar targeting entre ItemUtilizer e SkillCaster (média prioridade)

**Problema:** `ItemUtilizer` e `SkillCaster` terão lógica de targeting
praticamente idêntica (destacar células, selecionar, confirmar, cancelar).

**Solução:** Extrair um `TargetSelector` reutilizável:

```gdscript
class_name TargetSelector
extends Node

## Entra em modo de seleção: destaca `target_positions`, espera o jogador
## escolher (toque para selecionar, toque de novo para confirmar, toque fora
## para cancelar). Retorna a posição confirmada ou NO_SELECTION se cancelado.
func select_target(target_positions: Array[Vector2i],
    get_affected: Callable  # (Vector2i) -> Array[Vector2i] para prévia de área
) -> Vector2i
```

`ItemUtilizer` e `SkillCaster` usam `TargetSelector` internamente. Isso elimina
a duplicação de ~60 linhas de lógica de highlight/seleção/confirm/cancel.

---

## 6. Fase 5 — Preencher Lacunas do GDD (design, não código)

As seguintes seções do GDD estão como template e precisam de preenchimento de
design. Não são bloqueadoras para implementação técnica, mas guiam decisões
futuras:

| Seção do GDD | Conteúdo necessário |
|---|---|
| 2.1 Core Loop | Descrever o ciclo: Mover/Atacar → Repovoar → Inimigos agem → Repetir |
| 2.2 Objetivos | Curto: sobreviver / Médio: subir nível e escolher traits / Longo: build completo |
| 2.6 Economia | Itens como recurso; XP como progressão; sem moeda (por ora) |
| 3.1 Personagem | Nome, motivação, habilidades iniciais por classe |
| 3.2 Inimigos | Tabela completa (slime, arqueiro, tanque, elite, mímico + futuros) |
| 3.3 Mundo | Ambientação e estética (a definir) |
| 4.1-4.3 Controles/HUD/Menus | Documentar o que já existe + planejar menu principal, pausa |
| 5. Arte e Áudio | Direção de arte (pixel art atual), SFX (Haptics existe), música (a definir) |
| 7. Monetização | Modelo (a definir — recomendação: premium) |
| 9. Acessibilidade | Checklist |
| 10. Métricas | KPIs |
| 11. Cronograma | Milestones |
| 12. Riscos | Tabela de riscos |

> **Recomendação:** Preencher em uma sessão de design separada, com input do
> designer. O foco deste plano é a implementação técnica (Skills + melhorias).

---

## 7. Ordem de Execução e Dependências

```
Melhorias A, E, G (quick wins, sem dependência)
    │
    ▼
Fase 1: SkillDefinition + SkillComponent + SkillCaster + AreaCalculator
    │   (depende de Melhoria A: DamageResolver)
    │
    ├──► Fase 2: SkillBar (UI)
    │       │
    │       └──► Fase 3: Traits unlock skills + starting skills
    │                   │
    │                   └──► Fase 4: DEFENSIVE (escudo no Health) + UTILITY
    │
    ▼
Melhorias B, C, D, F, H, L (refactor, paralelo, sem quebrar gameplay)
    │
    ▼
Fase 6: Preencher GDD (design)
```

### Prioridade recomendada

1. **Melhoria A** (DamageResolver) — prerequisite para Fase 1, elimina duplicação.
2. **Melhoria E + G** (quick wins triviais) — minutos de trabalho.
3. **Fase 1** (Skill system core) — a maior lacuna do GDD.
4. **Fase 2** (SkillBar UI) — torna skills utilizáveis.
5. **Fase 3** (Traits unlock skills) — conecta skills ao sistema de progressão.
6. **Melhoria L** (TargetSelector) — extrai targeting compartilhado.
7. **Fase 4** (Defensive/Utility) — expande o repertório de skills.
8. **Melhorias B, C, D, F, H** (refactor de baixo risco) — paralelo.
9. **Fase 6** (GDD design) — sessão de design.

---

## 8. Premissas Preservadas

- `Grid`/`Cell` não conhecem skills, cooldowns ou regras de jogo.
- `GridController` orquestra; skills são expressas via API do `Grid`.
- Dados de skills em `Resource`s (`.tres`), editáveis via
  `resources_spreadsheet_view`.
- Componentes acessados de forma tipada (`player.skill_component`).
- Mutações visuais são awaitable.
- `@tool` em `Grid`/`Cell` preservado.
- Não editar `addons/`, `third/` ou `.godot/`.

## 9. Arquivos novos e modificados

### Novos
- `_application/progression/skill_definition.gd`
- `_application/character/skill_component.gd`
- `_application/item/skill_caster.gd`
- `_application/ui/skill_bar.gd`
- `_application/ui/skill_button.gd`
- `_application/util/damage_resolver.gd`
- `_application/util/area_calculator.gd`
- `_application/util/target_selector.gd` (Melhoria L)
- `_application/progression/resource/skill/*.tres` (uma por skill do GDD)

### Modificados
- `_application/progression/trait_definition.gd` — adicionar `unlocked_skills`
- `_application/progression/class_definition.gd` — adicionar `starting_skills`
- `_application/progression/trait_sheet.gd` — adicionar `get_unlocked_skills()`
- `_application/character/player_character.gd` — adicionar `SkillComponent` nó
- `_application/character/health.gd` — adicionar `shield` (Fase 4)
- `_application/grid_controller.gd` — conectar skills, tick cooldowns
- `_application/item/resource/area_effect_item_base.gd` — delegar a `AreaCalculator`
- `_application/item/resource/damage_area_effect_item.gd` — usar `DamageResolver`
- `_application/entity/directional_trap_entity.gd` — usar `DamageResolver`
- `_application/item/item_utilizer.gd` — usar `TargetSelector` (Melhoria L)
- `_application/ui/progression_hud.gd` — integrar `SkillBar`
- `_application/run.tscn` — adicionar `SkillCaster`, `SkillBar`, `TargetSelector`
- `_application/progression/resource/trait/*.tres` — adicionar `unlocked_skills`
- `_application/progression/resource/class/*.tres` — adicionar `starting_skills`
- `_application/grid.gd` — Melhorias B, D, E, H
- `_application/cell.gd` — Melhoria C
- `_application/ui/item_slot.gd` — Melhoria G
