---
tags:
  - plano
  - entidades
  - grid
created: 2026-07-12
aliases:
  - Plano de Entidades
---

# Plano: Tipos de células (Baú, Armadilha, Muro, Armadilha Direcional)

## Decisão de arquitetura

Os "tipos de célula" serão **entidades de conteúdo** (`Node2D` dentro da célula),
não subtipos de `Cell`. Motivos:
- `Grid`/`Cell` continuam sem conhecer regras de jogo (premissa 2 do AGENTS.md).
- Todo conteúdo já se move em direção ao jogador — baús/muros/armadilhas
  entram naturalmente nesse fluxo (`move_all_content_towards`, `fill_empty_border`).
- Reuso do componente `Health` para muro/armadilha direcional.

Nova classe base `GridEntity` (extends Node2D) com contrato declarativo que o
`GridController` consulta em vez de fazer `if content is X` para cada tipo.

Ver também: [[plano_refatoracao]], [[plano_progressao]].

## Fase 1 — Base: GridEntity

`_application/entity/grid_entity.gd`:
- `class_name GridEntity extends Node2D` (base de TODO conteúdo do grid,
  inclusive Character — hierarquia unificada).
- `Character` passa a `extends GridEntity`; `EnemyCharacter` implementa
  `is_attackable()=true` e `on_attacked(ctx)` com o combate atual (dano no
  inimigo + contra-dano no jogador saem do controller).
- API consultada pelo controller (defaults na base; subtipos sobrescrevem):
    - `func blocks_movement() -> bool` — default true; baú/armadilha: false
    - `func is_attackable() -> bool` — default false; inimigo/muro/armadilha direcional: true
    - `func on_player_enter(ctx) -> void` — baú/armadilha (jogador pisou)
    - `func on_attacked(ctx) -> void` — clique adjacente do jogador
    - `func on_player_action(ctx) -> void` — hook por ação do jogador (armadilha direcional gira)
- `ctx` = GridController (dá acesso a grid, player, loot, posição).
- Base declara `health: Health` opcional (null para quem não tem vida);
  entidades com vida têm nó `$Health` na cena (padrão do repo).
- `PlayerCharacter`: `is_attackable()=false`; nunca recebe `on_player_enter`.

## Fase 2 — Entidades

1. `wall_entity.gd` + `scenes/wall_entity.tscn`
    - `blocks_movement()=true`, `is_attackable()=true`, `$Health` (ex.: 10 hp).
    - `on_attacked`: recebe dano do ataque do jogador; ao morrer é removido do grid.
2. `chest_entity.gd` + `scenes/chest_entity.tscn`
    - `blocks_movement()=false`; `on_player_enter`: gera item aleatório com 100%
      de chance (via `ItemDropRoller.roll(possible_items, 1.0)`), entrega ao
      `ItemSlotContainer`, remove o baú e o jogador ocupa a célula.
3. `trap_entity.gd` + `scenes/trap_entity.tscn`
    - `blocks_movement()=false`; `on_player_enter`: `player.health.take_damage(trap_damage)`,
      armadilha é consumida (removida) e o jogador ocupa a célula.
4. `directional_trap_entity.gd` + `scenes/directional_trap_entity.tscn`
    - Estado: `facing: Vector2i` (N/E/S/W), sprite/seta rotaciona conforme facing.
    - `on_player_action`: gira 90° (sentido horário).
    - `is_attackable()=true`; `on_attacked`: explode — dano em linha na direção
      `facing` a partir da própria posição (`grid.get_line_positions`), atinge
      qualquer conteúdo com Health (inclusive o jogador), depois é removida.

## Fase 3 — Dano genérico (itens + explosão)

- Criar interface de dano única: qualquer conteúdo com `health` recebe dano.
    - `DamageAreaEffectItem.apply`: trocar `content is Character` por checagem
      genérica (`Character` ou `GridEntity` com `$Health`). Muro destruído por
      item de área é removido na mesma varredura de limpeza dos inimigos mortos.
    - Extrair helper no controller (ou classe estática `DamageResolver`) usado
      por item, ataque do jogador e explosão da armadilha direcional — sem duplicação.

## Fase 4 — GridController: dispatch e ações

- `_on_cell_tapped` (célula adjacente com conteúdo) vira dispatch uniforme:
    - `GridEntity.is_attackable()` → `on_attacked(self)` (inimigo combate,
      muro toma dano, armadilha direcional explode).
    - `blocks_movement()==false` → `on_player_enter(self)` e depois
      `_move_player_character` (baú/armadilha).
- Contador de "ação do jogador": após cada ação válida (mover/atacar/usar item
  confirmado), notificar todas as `GridEntity` do grid via `on_player_action`.
    - Sinal novo `player_acted` no GridController; `ItemUtilizer` confirmação
      também conta como ação.
- Spawn: `_make_random_enemy` vira `_make_random_content` com pesos exportados:
    - `@export enemy_weight/wall_weight/chest_weight/trap_weight/directional_trap_weight`
    - Usado em `populate_grid_with_enemies` (renomear p/ `populate_grid`) e
      `fill_empty_border` (mesma factory).
- Loot de inimigos: `item_drop_chance` reduzido (ex.: 0.5 → 0.08) no `run.tscn`.

## Fase 5 — Cenas e run.tscn

- Sprites: reutilizar `third/Freebies_Full_Icons.png` (AtlasTexture) para
  baú/armadilha/muro; seta da armadilha direcional pode ser `draw` simples.
- `run.tscn`: exportar as novas `PackedScene`s no GridController + pesos.

## Fase 6 — Validação manual (checklist)

1. Grid inicial contém mistura de inimigos + novas entidades (pesos).
2. Muro: bloqueia movimento; clique adjacente causa dano; some ao zerar; item
   de área também o destrói; borda repopula.
3. Baú: andar sobre ele dá 1 item (sempre) e o jogador ocupa a célula.
4. Armadilha: andar sobre ela dá dano no jogador e ela some.
5. Armadilha direcional: gira a cada ação; ao ser atacada explode em linha na
   direção atual, danificando inimigos/jogador no caminho.
6. Drop de monstros visivelmente raro.
7. Game over/loot/preview continuam funcionando.

## Ordem de execução

1. GridEntity base
2. Wall (mais simples, valida dispatch de ataque)
3. Chest + Trap (valida on_player_enter)
4. DirectionalTrap (usa player_acted + explosão)
5. Ajuste DamageAreaEffectItem + drop chance
6. run.tscn (spawns/pesos) + validação