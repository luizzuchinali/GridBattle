---
tags:
  - design
  - gdd
created: 2026-07-12
updated: 2026-09-27
---
## Grid Battle (TEMP)

| Campo                        | Valor               |
| ---------------------------- | ------------------- |
| **Título**                   | Grid Battle         |
| **Gênero**                   | RPG, Survive        |
| **Plataforma(s)**            | Mobile e PC         |
| **Engine**                   | Unity               |
| **Classificação indicativa** | Livre               |
| **Autor(es)**                | Zuchinali Softworks |

---

## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid. A #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A cada nível que o jogador receber, irá receber um ponto de trait para escolher na árvore de #trait. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo e também influenciar nos #item possíveis de serem dropados. Os #item são consumíveis de uso único, podendo ser poções, buffs, antídotos e etc., sempre com efeito imediato ou temporário sobre o personagem.

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

> **Será revisto no futuro.** A ideia inicial é deixar o jogo base funcionando antes de implementar mais coisas acima desta camada. O que segue abaixo é a direção atual, sujeita a revisão.

#### Mecânica 3: Skills Ativas
- **Descrição:** Cada classe possui um conjunto de skills ativas adquiridas via traits. Skills são ações especiais com cooldown, área de efeito (shape), dano (se ofensivas) e outros parâmetros. Diferem de itens por serem reutilizáveis (cooldown) e vinculadas ao personagem.
- **Entrada do jogador:** Toque em botão de skill na HUD → seleção de alvo.
- **Feedback visual/sonoro:** Animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** Skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo). Skills compartilham o mesmo sistema de seleção de alvo dos itens.

#### Mecânica 4: Itens Consumíveis
- **Descrição:** Itens são exclusivamente consumíveis de uso único: poções de cura, buffs temporários, antídotos e afins — uso imediato ou com seleção de alvo, efeito único sobre o personagem. Itens de efeito ativo no grid (arremessáveis, granadas, bombas) não fazem mais parte do design; efeitos desse tipo pertencem somente às skills.
- **Entrada do jogador:** Toque no slot de item → uso imediato ou seleção de alvo (se aplicável).
- **Feedback visual/sonoro:** Indicador de efeito no personagem (cura, buff, status removido); animação de consumo ao usar.
- **Interação com outras mecânicas:** Traits influenciam quais itens podem ser dropados. Itens complementam a sobrevivência sem substituir as skills de combate.

### 2.5 Progressão

> **Será revisto no futuro.** As regras abaixo descrevem a direção desejada da progressão; os detalhes finais (valores, parâmetros e formato das skills/itens) serão revisados quando o jogo base estiver funcionando.

### 2.5.1 Skills e Níveis

- **Skills de Classe:** São definidas pela classe do personagem e desbloqueadas progressivamente via traits. Cada skill possui:
  - **Nome e descrição**
  - **Tipo** (ofensiva, defensiva ou utilitária)
  - **Dano** (se ofensiva) — valor fixo
  - **Área de efeito** (shape: CIRCLE, CROSS, LINEAR, CONE, ARC, PERPENDICULAR)
  - **Tamanho da área** (ímpar, 1-9)
  - **Alcance** (distância Manhattan do jogador para selecionar alvo)
  - **Cooldown** (em ações do jogador — número de turnos que precisa esperar entre usos)
  - **Ícone**

- **Sistema de cooldown:** Cada skill ativa tem um contador de cooldown que decrementa a cada ação do jogador (movimento ou ataque). Skills não podem ser usadas enquanto o cooldown não zera.

### 2.5.2 Progressão de Conteúdo

- **Spawn por nível:** A tabela de spawn filtra inimigos por nível mínimo/máximo. Conforme o jogador sobe de nível, inimigos mais fortes aparecem.
- **Itens por nível:** Itens têm nível mínimo; entram na pool de drop apenas quando o jogador atinge o nível mínimo.
- **Traits por nível:** Traits têm nível requerido; o jogador ganha um ponto de trait e, após recebê-lo, entra em uma tela de escolha dos traits. Essa tela mostra todos os traits em ordem de requerimentos etc. NÃO DEVE MOSTRAR DE MANEIRA ALEATÓRIA OS TRAITS NO LEVEL UP.

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

> Skills de classe e slots de skill fazem parte do escopo "Será revisto no futuro".

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

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando. Skills e itens detalhados abaixo representam a direção inicial e serão reavaliados antes da implementação completa.

### 6.1 Definição de Skill

**Skill** é uma ação ativa do personagem, vinculada à classe ou a traits adquiridos. Toda skill tem:

- **Tipo:** Ofensiva (causa dano), Defensiva (escudo/cura/buff), Utilitária (teleporte, troca etc.)
- **Área de efeito:** Define a forma geométrica do efeito no grid (CIRCLE, CROSS, LINEAR, PERPENDICULAR, ARC, CONE).
- **Tamanho da área:** Ímpar, de 1 a 9.
- **Alcance:** Distância Manhattan máxima para seleção do alvo.
- **Dano:** Valor fixo (se ofensiva).
- **Cooldown:** Número de ações do jogador entre usos.

Skills são **reutilizáveis** — após o cooldown, podem ser usadas novamente. Não são consumidas.

### 6.2 Definição de Item

**Item** é um recurso consumível de uso único: poções, buffs, antídotos e similares — efeito imediato (cura, buff temporário, remoção de status) sobre o personagem. Não existem itens ativos de combate no grid; efeitos desse tipo pertencem exclusivamente às skills.

Itens entram na pool de drop a partir de uma lista base da run, somada aos itens desbloqueados por traits.

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

---
## 7. Monetização (se aplicável)

- **Modelo:** [Premium, F2P, Ad-supported, IAP]
- **Itens/Conteúdo pago:** ...
- **Ética de monetização:** [princípios para evitar pay-to-win, etc.]

---
## 8. Plataforma Técnica

### 8.1 Especificações
- **Engine:** Unity
- **Renderização:** [URP, Built-in...]
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

- Itens ativos (arremessáveis, granadas, bombas) — removidos do design; itens são somente consumíveis.
- Skills e itens detalhados — serão revistos no futuro, após o jogo base funcionar.
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
| Item  | Consumível de uso único (poções, buffs, antídotos etc.) com efeito imediato ou temporário sobre o personagem. Não existem itens ativos de combate. | #item |
