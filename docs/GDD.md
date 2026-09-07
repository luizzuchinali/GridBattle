---
tags:
  - design
  - gdd
created: 2026-07-12
---
## Grid Battle (TEMP)

| Campo                        | Valor               |
| ---------------------------- | ------------------- |
| **Título**                   | Grid Battle         |
| **Gênero**                   | RPG, Survive        |
| **Plataforma(s)**            | Mobile e PC         |
| **Engine**                   | Godot               |
| **Classificação indicativa** | Livre               |
| **Autor(es)**                | Zuchinali Softworks |

---
## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid, #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A cada nível que o jogador receber, irá receber um ponto de trait para escolher na arvore de #trait. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo e também influenciar nos #item possíveis de serem dropados. Os #item são consumíveis de uso único, podendo ser poções, buffs ou **itens ativos** (como granadas, bombas e arremessáveis) com efeito próprio de uso único.
### 1.3 Pillars de Design
[3 a 5 princípios que guiam todas as decisões de design. Ex.: "Decisões sobre sorte", "Cada partida é única".]

1. Cada ação do jogador importa
2. Cada partida deve passar o sentimento de ser única 
3. O rng no jogo deve ser um fator, porém um fator controlável pelo jogador

### 1.4 Unique Selling Points (USPs)
[O que diferencia este jogo de outros do mesmo gênero.]

- O sistema de combate em um grid por turno onde cada ação do jogador gera uma reação.
- ...

---

## 2. Jogabilidade

### 2.1 Core Loop
[Descreva o ciclo central de jogo. Ex.: Explorar → Encontrar inimigos → Combater → Coletar loot → Melhorar personagem → Repetir.]

```
[Diagrama ou descrição textual do loop]
```

### 2.2 Objetivo do Jogador
[Qual é a meta de curto, médio e longo prazo?]

- **Curto prazo:** ...
- **Médio prazo:** ...
- **Longo prazo:** ...

### 2.3 Mecânicas Principais
[Liste e descreva cada mecânica central.]

#### Mecânica 1: [Nome]
- **Descrição:** ...
- **Entrada do jogador:** [controle/toque]
- **Feedback visual/sonoro:** ...
- **Interação com outras mecânicas:** ...

#### Mecânica 2: [Nome]
- ...

### 2.4 Mecânicas Secundárias

#### Mecânica 3: Skills Ativas
- **Descrição:** Cada classe possui um conjunto de skills ativas adquiridas via traits. Skills são ações especiais com cooldown, área de efeito (shape), dano (se ofensivas) e outros parâmetros. Diferem de itens por serem reutilizáveis (cooldown) e vinculadas ao personagem.
- **Entrada do jogador:** Toque em botão de skill na HUD → seleção de alvo (similar ao sistema de itens).
- **Feedback visual/sonoro:** Animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** Skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo). Skills compartilham o mesmo sistema de seleção de alvo dos itens.

#### Mecânica 4: Itens Consumíveis vs Itens Ativos
- **Descrição:** Itens são divididos em duas categorias:
  1. **Consumíveis:** Poções de cura, buffs temporários, antídotos — uso imediato ou com seleção de alvo, efeito único.
  2. **Itens Ativos:** Itens de uso único com efeito próprio no grid (ex.: granada, bomba de fumaça, adaga de arremesso), usando a mesma mecânica de área/dano das skills, mas sem vínculo com a classe. Ao usar, o item é consumido.
- **Entrada do jogador:** Toque no slot de item → seleção de alvo (se aplicável).
- **Feedback visual/sonoro:** Mesmo sistema de prévia de área dos itens; animação de consumo ao usar.
- **Interação com outras mecânicas:** Traits podem desbloquear itens ativos específicos na pool de drop. Skills de classe podem ser mais poderosas que itens ativos, mas têm cooldown.
[Sistemas complementares que não são o foco, mas enriquecem a experiência.]

- ...

### 2.5 Progressão


### 2.5.1 Skills e Níveis

- **Skills de Classe:** São definidas pela `ClassDefinition` e desbloqueadas progressivamente via traits. Cada skill possui:
  - **Nome e descrição**
  - **Dano** (se ofensiva) — valor fixo
  - **Área de efeito** (shape: CIRCLE, CROSS, LINEAR, CONE, ARC, PERPENDICULAR)
  - **Tamanho da área** (ímpat, 1-9)
  - **Alcance** (distância Manhattan do jogador para selecionar alvo)
  - **Cooldown** (em ações do jogador — número de turnos que precisa esperar entre usos)
  - **Ícone**

- **Itens Ativos:** São `ItemBase` de uso único com efeito próprio no grid (ex.: granada, bomba de fumaça, adaga de arremesso). Não replicam uma skill de classe — são itens consumíveis independentes. Usam a mesma estrutura de `AreaEffectItemBase` e `DamageAreaEffectItem` já existente no código.

- **Sistema de cooldown:** Cada skill ativa tem um contador de cooldown que decrementa a cada ação do jogador (movimento ou ataque). Skills não podem ser usadas enquanto `cooldown_remaining > 0`.

### 2.5.2 Progressão de Conteúdo

- **Spawn por nível:** A tabela de spawn (`SpawnEntry`) filtra inimigos por `min_level`/`max_level`. Conforme o jogador sobe de nível, inimigos mais fortes aparecem.
- **Itens por nível:** Itens têm `min_level`; entram na pool de drop apenas quando o jogador atinge o nível mínimo.
- **Traits por nível:** Traits têm `required_level`; O jogador ganha um ponto de trait podendo após receber o ponto, entrar em uma tela de escolha dos traits. Essa tela mostra todos os traits em ordem de requerimentos e etc. NÃO DEVE MOSTRAR DE MANEIRA ALEATÓRIA OS TRAITS NO LEVEL UP.
[Como o jogador evolui ao longo do jogo — níveis, XP, unlocks, dificuldade.]

- **Sistema de progressão:** XP e níveis, árvore de traits e skills por classe
- **Curva de dificuldade:** ...
- **Unlocks:** Traits desbloqueiam skills ativas (que entram no repertório do personagem) e itens (que entram na pool de drop). O nível do jogador desbloqueia inimigos mais fortes e itens de tier mais alto.

### 2.6 Economia
[Recursos, moedas, custos, recompensas.]

| Recurso | Origem | Uso |
|---------|--------|-----|
| [Moeda] | [Drop de inimigos] | [Comprar upgrades] |
| ... | ... | ... |

---


## 3. Personagens e Mundo

### 3.1 Personagem do Jogador
- **Nome:** ...
- **Descrição:** ...
- **Motivação:** ...
- **Habilidades iniciais:** ...

- **Skills de classe:** Cada classe começa com 1-2 skills básicas (ex.: Guerreiro começa com "Golpe" — dano em área frontal). Novas skills são desbloqueadas via traits.
- **Slots de skill:** O personagem pode carregar até N skills ativas por vez, selecionadas na tela de classe antes da run (ou durante, via traits).

### 3.2 NPCs / Inimigos
[Liste tipos de inimigos, bosses e NPCs relevantes.]

| Nome | Tipo | Comportamento | Dificuldade |
|------|------|---------------|-------------|
| ... | Inimigo comum | ... | Fácil |
| ... | Boss | ... | Difícil |

### 3.3 Mundo / Cenário
- **Ambientação:** ...
- **Estética:** ...
- **Estrutura de fases/mundo:** [linear, aberto, procedural, fases fixas...]

---

## 4. Controles e Interface

### 4.1 Esquema de Controles
| Ação | Entrada |
|------|--------|
| Mover | [Toque / WASD / Analógico] |
| Atacar | [Toque / Botão] |
| ... | ... |

### 4.2 HUD
[Quais elementos aparecem na tela durante o jogo — vida, pontos, minimapa, etc.]

- ...

### 4.3 Menus
[Liste telas de menu: Principal, Pausa, Opções, Inventário, Fim de partida...]

- ...

---

## 5. Arte e Áudio

### 5.1 Direção de Arte
- **Estilo visual:** [pixel art, 2D vetorial, 3D low-poly...]
- **Paleta de cores:** ...
- **Referências:** ...

### 5.2 Áudio
- **Trilha sonora:** [estilo, ritmo, momentos]
- **SFX:** [lista de efeitos necessários]
- **Locução/Voz:** [sim/não, idioma]


## 6. Skills e Itens

### 6.1 Definição de Skill

**Skill** é uma ação ativa do personagem, vinculada à classe ou a traits adquiridos. Toda skill tem:

- **Tipo:** `OFFENSIVE` (causa dano), `DEFENSIVE` (escudo/cura/buff), `UTILITY` (teleporte, troca, etc.)
- **Área de efeito:** Define a forma geométrica do efeito no grid (CIRCLE, CROSS, LINEAR, PERPENDICULAR, ARC, CONE).
- **Tamanho da área:** Ímpar, de 1 a 9.
- **Alcance:** Distância Manhattan máxima para seleção do alvo.
- **Dano:** Valor fixo (se ofensiva).
- **Cooldown:** Número de ações do jogador entre usos.

Skills são **reutilizáveis** — após o cooldown, podem ser usadas novamente. Não são consumidas.

### 6.2 Definição de Item

**Item** é um recurso consumível de uso único. Dividido em:

- **Consumível:** Poções, buffs, antídotos — efeito imediato (cura, buff temporário).
- **Item Ativo:** Item de uso único com efeito próprio no grid (ex.: granada, bomba de fumaça), com área, dano e alcance definidos por ele mesmo — não é uma skill de classe e é consumido ao usar.

Ambos entram na pool de drop via `possible_items` (base) + `trait_sheet.get_unlocked_items()` (desbloqueados por traits).

### 6.3 Tabela de Skills por Classe

#### Guerreiro
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Golpe | OFENSIVA | 5 | CIRCLE | 1 | 1 | 1 | Inicial |
| Investida com Escudo | OFENSIVA | 12 | LINEAR | 3 | 2 | 3 | Muralha |

#### Mago
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Bola de Fogo | OFENSIVA | 10 | CIRCLE | 3 | — | 3 | Grimório |
| Nova de Gelo | OFENSIVA | 8 | CIRCLE | 3 | 2 | 2 | Criomancia |
| Tempestade Elétrica | OFENSIVA | 14 | CONE | 3 | 3 | 4 | Conjurador de Tempestades |

#### Ladino
| Skill | Tipo | Dano | Área | Tamanho | Alcance | Cooldown | Trait |
|---|---|---|---|---|---|---|---|
| Adaga de Arremesso | OFENSIVA | 6 | CIRCLE | 1 | 3 | 1 | Arsenal Oculto |
| Bomba de Fumaça | OFENSIVA | 5 | CROSS | 1 | 1 | 2 | Mestre das Sombras |
| Adaga Envenenada | OFENSIVA | 7 | LINEAR | 3 | 4 | 2 | Mãos Ligeiras |
| Linha Venenosa | OFENSIVA | 9 | LINEAR | 5 | 5 | 3 | Mestre do Veneno |

### 6.4 Itens Ativos

Efeitos de combate também existem como **itens ativos** (itens consumíveis com efeito próprio no grid). Exemplos já implementados:

- **Granada** (`grenade.tres`): Dano em área, uso único.
- **Bola de Fogo** (`fireball.tres`): 10 de dano em área CIRCLE size 3, uso único.
- **Bomba de Fumaça** (`smoke_bomb.tres`): 5 de dano em área, uso único.
- **Adaga de Arremesso** (`throwing_dagger.tres`): Dano à distância, uso único.

A diferença é que a skill de classe é reutilizável (cooldown) e vinculada ao personagem, enquanto o item ativo é consumido. Itens ativos podem ser dropados por inimigos ou encontrados em baús, permitindo ao jogador ter acesso temporário a efeitos poderosos mesmo sem ter o trait correspondente.

---
## 7. Monetização (se aplicável)

- **Modelo:** [Premium, F2P, Ad-supported, IAP]
- **Itens/Conteúdo pago:** ...
- **Ética de monetização:** [princípios para evitar pay-to-win, etc.]

---
## 8. Plataforma Técnica

### 8.1 Especificações
- **Engine:** ...
- **Renderização:** [OpenGL, Vulkan, GL Compatibility...]
- **Resolução alvo:** ...
- **Orientação:** [Retrato / Paisagem]
- **FPS alvo:** ...

### 8.2 Requisitos mínimos
- ...

### 8.3 Plataformas de publicação
- Steam
- App Store
- Google Play
---

## 9. Acessibilidade

[Liste recursos de acessibilidade planejados.]

- [ ] Tamanho de fonte ajustável
- [ ] Daltonismo (cores alternativas)
- [ ] Legendas
- [ ] Dificuldade ajustável
- [ ] Remapeamento de controles
- [ ] ...

---

## 10. Métricas de Sucesso

[Como medir se o jogo atingiu seus objetivos.]

- **KPIs de jogo:** [retenção D1/D7/D30, tempo de sessão, taxa de conclusão]
- **KPIs de negócio:** [vendas, ARPU, conversão]
- **Metas qualitativas:** [avaliações na loja, feedback de comunidade]

---

## 11. Cronograma e Escopo

### 11.1 Milestones
| Milestone | Data alvo | Entregáveis |
|-----------|-----------|-------------|
| Protótipo jogável | [DD/MM/AAAA] | [Core loop funcional] |
| Alpha | [DD/MM/AAAA] | [Conteúdo principal] |
| Beta | [DD/MM/AAAA] | [Balanceamento e polish] |
| Lançamento | [DD/MM/AAAA] | [Versão 1.0] |

### 11.2 Escopo (Out of Scope)
[O que NÃO será feito nesta versão. Ajuda a controlar feature creep.]

- ...

---

## 12. Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| [ex.: Engine não suporta X] | Média | Alto | [Plano B] |
| ... | ... | ... | ... |

---

## 13. Glossário

| Termo | Definição                                                                                                                         | Tags   |
| ----- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| Grid  | Tabuleiro de células onde ocorre o combate                                                                                        | #grid  |
| Trait | São mecânicas obtidas a cada nível ganho pelo jogador, eles alteram características, liberam skills, desbloqueiam itens e modificam mecânicas de jogo. | #trait |
| Skill | Ação ativa do personagem, vinculada à classe ou a traits. Possui área de efeito, dano, alcance e cooldown. Pode ser reutilizada. | #skill |
| Item  | Consumível de uso único. Divide-se em consumíveis (poções, buffs) e itens ativos (efeito próprio de combate, ex.: granadas). | #item  |
| Item Ativo | Item consumível de uso único com efeito próprio de combate no grid (ex.: granada, bomba de fumaça), independente das skills de classe. | #item |
