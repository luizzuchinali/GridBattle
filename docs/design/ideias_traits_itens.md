---
tags:
  - design
  - ideias
  - trait
  - item
created: 2026-07-12
aliases:
  - Ideias de Traits e Itens
---

# Ideias de Traits e Itens — Mecânicas Emergentes

Este documento descreve ideias de traits e itens que **mudam o funcionamento
do jogo** em vez de apenas ajustar números. Cada ideia inclui a mecânica, o
impacto no gameplay e notas de implementação (o que precisaria ser criado ou
alterado no código). É um documento de design, não um plano de execução.

Ver também: [[talentos]], [[plano_progressao]].

## Índice

1. [Dano periódico (veneno, sangramento, queimadura)](#1-dano-periódico)
2. [Escudos e absorção de dano](#2-escudos-e-absorção-de-dano)
3. [Modificadores de grid (terreno, empurrão, atração)](#3-modificadores-de-grid)
4. [Economia de recursos (fúria, mana, combo)](#4-economia-de-recursos)
5. [Transformação de conteúdo (converter inimigos)](#5-transformação-de-conteúdo)
6. [Itens de utilidade (visão, teleporte, swap)](#6-itens-de-utilidade)
7. [Reatividade (gatilhos por evento)](#7-reatividade)
8. [Itens passivos (equipáveis)](#8-itens-passivos-equipáveis)

---

## 1. Dano periódico

Hoje todo dano é instantâneo (`health.take_damage`). Dano periódico abre espaço
para venenos, queimaduras e sangramento — efeitos que tickam a cada ação do
jogador ou a cada intervalo de tempo.

### Status effects no `Health`

**Implementação:** adicionar um sistema de status effects no `Health` (ou como
componente separado). Cada effect tem: tipo, dano por tick, duração (em ticks),
e é processado quando `on_player_action` dispara (tick por turno) ou por timer.

```gdscript
# Exemplo: StatusEffect como Resource
class_name StatusEffect
extends Resource
enum Type { POISON, BURN, BLEED }
@export var type: Type
@export var damage_per_tick: int = 1
@export var ticks_remaining: int = 3
```

### Traits sugeridos

| Classe | Trait | Efeito | Nota |
|---|---|---|---|
| Ladino | **Lâmina Venenosa** | Ataques aplicam veneno (1 dano/turno, 3 turnos) | Modifica `EnemyCharacter.on_attacked` para aplicar status |
| Mago | **Ignição** | Bola de Fogo e Nova de Gelo aplicam queimadura (2 dano/turno, 2 turnos) | Item de área aplica status em sobreviventes |
| Guerreiro | **Sangramento** | Críticos aplicam sangramento (1 dano/turno, 2 turnos) | Gateado por `roll_damage().is_critical` |

### Itens sugeridos

| Item | Classe | Efeito |
|---|---|---|
| Frasco de Veneno | Ladino | Aplica veneno (3 dano/turno, 4 turnos) em área CIRCLE size 1 |
| Tocha Acesa | Guerreiro | Aplica queimadura (2 dano/turno, 3 turnos) em linha |
| Ampulheta Tóxica | Mago | Aplica veneno em todos os inimigos adjacentes ao jogador |

### Por que muda o jogo
Permite matar inimigos sem gastar turnos extras — aplica veneno e foge. Cria
decisão de "matou agora ou espera o tick?" e funciona como soft-CC.

---

## 2. Escudos e absorção de dano

Hoje o jogador só tem HP. Um escudo temporário adiciona uma camada de defesa
ativa e abre espaço para builds defensivas.

### Shield no `Health`

**Implementação:** adicionar `shield: int` ao `Health`. `take_damage` desconta
primeiro do escudo, depois do HP. Sinal `shield_changed`.

```gdscript
var shield: int = 0
func take_damage(amount: int, ...) -> void:
    var absorbed: int = mini(amount, shield)
    shield -= absorbed
    amount -= absorbed
    # ... resto igual
```

### Traits sugeridos

| Classe | Trait | Efeito |
|---|---|---|
| Guerreiro | **Pele de Ferro** | Ao matar um inimigo, ganha escudo = ATK do jogador (cap 10) |
| Mago | **Barreira Mística** | Ao usar um item, ganha escudo = 5 por 3 turnos |
| Ladino | **Sombra Protetora** | Após esquiva (não tomar dano de counter), ganha escudo 3 |

### Itens sugeridos

| Item | Classe | Efeito |
|---|---|---|
| Talismã de Pedra | Guerreiro | Ganha escudo 15 (consumível, sem seleção de alvo) |
| Runa de Proteção | Mago | Ganha escudo 8 e cura 5 |
| Pó de Invisibilidade | Ladino | Ganha escudo 6 e +20% de chance de drop por 3 turnos |

### Por que muda o jogo
Cria window de agressão segura — jogador pode atacar sem tomar counter. Permite
builds tank no Guerreiro e sobrevivência no Mago frágil.

---

## 3. Modificadores de grid

O grid é a arena. Traits que mudam o comportamento do grid (direção de
repopulação, tipo de conteúdo, empurrão) criam controle tático.

### Empurrão (knockback)

**Implementação:** adicionar `grid.push_content(from, direction, steps)` que
move conteúdo de uma célula N passos numa direção, se as células intermediárias
estiverem vazias. Pode ser integrado a `on_attacked` ou a itens.

### Traits sugeridos

| Classe | Trait | Efeito |
|---|---|---|
| Guerreiro | **Impacto** | Ataques empurram o inimigo 1 célula na direção do ataque |
| Mago | **Repulsão Arcana** | Críticos empurram o inimigo 2 células |
| Ladino | **Golpe Sentinela** | Ataques não causam counter se o inimigo foi empurrado |

### Itens sugeridos

| Item | Classe | Efeito |
|---|---|---|
| Onda de Choque | Guerreiro | Empurra todo conteúdo adjacente 1 célula para longe do jogador |
| Vento Cortante | Mago | Empurra uma linha inteira de conteúdo 2 células |
| Pit Trap | Ladino | Coloca uma armadilha de empurrão numa célula vazia (empurra quem pisar) |

### Por que muda o jogo
Empurrar inimigos para muros (dano de colisão?), para armadilhas, ou para longe
para ganhar tempo. Adiciona posicionamento tático ao combate.

### Repopulação direcionada

| Trait | Efeito | Nota |
|---|---|---|
| **Cacador** (Ladino) | Bordas vazias têm +30% de chance de gerar baús | Modifica `_make_random_content` com flag |
| **Necromante** (Mago) | Bordas vazias têm 20% de chance de gerar esqueletos aliados | Novo tipo de entidade |
| **Atrito** (Guerreiro) | Repopulação desliza 2 passos em vez de 1 | Modifica `_repopulate_grid` |

---

## 4. Economia de recursos

Hoje o jogador tem slots de item (6) sem custo de uso. Adicionar um recurso
que acumula e gasta adiciona gestão de longo prazo.

### Sistema de combo/fúria

**Implementação:** adicionar `combo: int` ao `PlayerCharacter`. Cada ataque
bem-sucedido +1; falhar/parar decrementa. Certos traits/items escalam com combo.

### Traits sugeridos

| Classe | Trait | Efeito |
|---|---|---|
| Ladino | **Combo Mortal** | +1 ATK por combo (cap 5); reset ao tomar dano |
| Guerreiro | **Fúria Acumulada** | A cada 3 ataques sem tomar dano, próximo ataque é crítico garantido |
| Mago | **Fluxo Contínuo** | A cada 2 itens usados, próximo item tem 0 cooldown de seleção |

### Itens sugeridos

| Item | Classe | Efeito |
|---|---|---|
| Tônico de Fúria | Guerreiro | Zera combo mas cura 15 de vida |
| Catalisador Arcano | Mago | Dobra o combo atual e converte em escudo |
| Marca do Caçador | Ladino | Próximos 3 ataques têm +50% de chance de crítico |

### Por que muda o jogo
Recompensa jogadores que mantêm ofensiva sem tomar dano. Cria risco/recompensa:
continuar atacando para manter combo vs recuar para curar.

---

## 5. Transformação de conteúdo

Hoje conteúdo é criado/destruído. Transformar conteúdo (inimigo → aliado,
muro → passagem, baú → inimigo) adiciona manipulação tática do tabuleiro.

### Traits sugeridos

| Classe | Trait | Efeito |
|---|---|---|
| Mago | **Polimorfia** | Inimigos com HP ≤ 2 viram muros (bloqueia movimento mas não ataca) |
| Ladino | **Suborno** | Baús têm 30% de chance de dar ouro extra (XP) e virar armadilha |
| Guerreiro | **Intimidação** | Inimigos adjacentes ao jogador têm 20% de chance de fugir (mover para longe) |

### Itens sugeridos

| Item | Classe | Efeito |
|---|---|---|
| Caixa Mágica | Mago | Transforma um inimigo em baú (muda a entidade da célula) |
| Selo da Conversão | Ladino | Transforma um muro em armadilha (dano para inimigos adjacentes) |
| Bandeira de Guerra | Guerreiro | Transforma todos os baús visíveis em inimigos (mais XP, mais risco) |

### Por que muda o jogo
Permite ao jogador manipular o tabuleiro — converter ameaças em oportunidades
ou bloqueios. Polimorfia + Bola de Fogo = clear estratégico.

---

## 6. Itens de utilidade

Itens que não causam dano mas mudam o estado do jogo.

### Itens sugeridos

| Item | Classe | Efeito | Implementação |
|---|---|---|---|
| Teleporte | Mago | Move o jogador para qualquer célula vazia | Novo `ItemBase` com `get_target_positions` retornando vazias |
| Troca | Ladino | Troca o jogador de posição com um inimigo | Swap de conteúdo entre duas células |
| Visão | Mago | Revela todos os mímicos disfarçados por 3 turnos | Flag global no GridController |
| Baú Portátil | Ladino | Cria um baú numa célula vazia adjacente | `set_content` com `ChestEntity` |
| Muro Temporário | Guerreiro | Cria um muro numa célula vazia (desaparece em 3 turnos) | WallEntity com TTL |

### Por que muda o jogo
Teleporte permite escapar de encurralamento. Troca posiciona o jogador atrás de
linhas inimigas. Muro temporário bloqueia contra-ataque de arqueiros.

---

## 7. Reatividade

Traits que reagem a eventos do jogo (morte, dano, level-up, repopulação).

### Hooks de evento

**Implementação:** o `GridController` já emite sinais indiretos (`_on_enemy_died`,
`_on_player_died`, `notify_player_acted`). Traits reativos podem se inscrever nesses
eventos via um sistema de hooks.

### Traits sugeridos

| Classe | Trait | Gatilho | Efeito |
|---|---|---|---|
| Guerreiro | **Vingança** | Ao tomar dano | Próximo ataque +50% de dano |
| Ladino | **Carniceiro** | Ao matar inimigo | +1 de XP extra e cura 2 HP |
| Mago | **Eco Arcano** | Ao usar item | 15% de chance de não consumir o item |
| Todos | **Último Suspiro** | Ao chegar a 1 HP | Cura 10 HP e ganha escudo 10 (1x por run) |
| Todos | **Adrenalina** | Ao matar 3 inimigos em 1 turno | Próximo ataque é crítico garantido |

### Por que muda o jogo
Cria momentos de virada e recompensa estilos de jogo específicos. Vingança
transforma tomar dano em oportunidade. Eco Arcano permite builds de spam de item.

### Implementação sugerida: TraitEffect

```gdscript
# Em vez de só StatModifier, um TraitEffect reage a eventos:
class_name TraitEffect
extends Resource
enum Trigger { ON_KILL, ON_DAMAGE_TAKEN, ON_ITEM_USE, ON_LEVEL_UP, ON_CRIT }
@export var trigger: Trigger
@export var action: Callable # ou um EffectResource polimórfico
```

`TraitDefinition` ganharia `effects: Array[TraitEffect]` além de `modifiers`.

---

## 8. Itens passivos (equipáveis)

Hoje todos os itens são consumíveis de uso único. Itens equipáveis (passivos
permanentes até trocar) abrem builds de longo prazo.

### Sistema de equipamento

**Implementação:** adicionar um slot de equipamento ao `PlayerCharacter`.
Equipar substitui o item passivo anterior. Efeitos passivos aplicam modificadores
contínuos ou hooks reativos.

### Itens equipáveis sugeridos

| Item | Classe | Efeito passivo |
|---|---|---|
| Anel do Sanguinário | Guerreiro | Cura 1 HP por inimigo morto |
| Amuleto Arcano | Mago | +15% de chance de crítico mas -2 ATK |
| Capa das Sombras | Ladino | Counter-ataques têm 20% de chance de errar |
| Bracelete de Espinhos | Todos | Reflete 1 de dano ao ser atacado |
| Bota do Vento | Todos | +1 célula de alcance em itens LINEAR |

### Por que muda o jogo
Adiciona decisão de longo prazo: qual passivo combina com os traits já
escolhidos? Cria sinergia entre traits, itens de consumo e equipamento.

---

## Priorização sugerida

Se fosse implementar por etapas, a ordem recomendada por impacto vs esforço:

1. **Dano periódico** (seção 1) — alto impacto, médio esforço. Muda combate
   sem precisar de UI nova.
2. **Escudos** (seção 2) — alto impacto, baixo esforço. Só adiciona `shield` ao
   `Health` e ajusta `take_damage`.
3. **Reatividade** (seção 7) — médio impacto, médio esforço. Precisa de sistema
   de hooks mas não de UI.
4. **Empurrão** (seção 3) — médio impacto, baixo esforço. Um método no `Grid`.
5. **Economia de recursos** (seção 4) — alto impacto, alto esforço. Precisa de
   UI de combo e tracking de estado.
6. **Itens passivos** (seção 8) — alto impacto, alto esforço. Precisa de slot de
   equipamento e UI.
7. **Transformação de conteúdo** (seção 5) — médio impacto, alto esforço.
8. **Itens de utilidade** (seção 6) — cada um é independente e pode ser
   implementado isoladamente.