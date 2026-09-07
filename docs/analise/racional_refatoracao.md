---
tags:
  - analise
  - arquitetura
  - refatoracao
created: 2026-07-12
aliases:
  - Rationale da Refatoração
---

# Melhorias propostas — explicação

Este documento explica **o porquê** de cada melhoria, para você avaliar se concorda antes de aplicar. O plano técnico (para a IA executar) está em [[plano_refatoracao]].

## O problema que você apontou

Você notou, corretamente, que **`Grid` e `Cell` têm funções repetidas**. Hoje o `Grid` é quase só uma camada de "wrappers" que repassam a chamada para o `Cell` correspondente, com nomes mais longos:

| `Grid` (fachada)                 | O que faz de verdade         |
|----------------------------------|------------------------------|
| `attach_node_to_position(p, n)`  | chama `cell.set_content(n)`  |
| `detach_node_from_position(p)`   | chama `cell.clear_content()` |
| `get_position_content(p)`        | chama `cell.get_content()`   |
| `position_has_content(p)`        | chama `cell.has_content()`   |
| `shake_position(p)`              | chama `cell.shake()`         |

E depois tudo isso é chamado no `GridController`.

## "Não faria sentido ter só o `Grid`?"

Quase. A conclusão a que cheguei é um meio-termo, e explico o motivo:

- **Vale a pena manter o `Cell`** como classe separada. Ele é uma unidade coesa: cuida do desenho, das animações (flip, shake, clique) e do conteúdo de **uma única célula**. Fundir isso no `Grid` deixaria o `Grid` gigante e misturaria "desenhar uma célula" com "gerenciar o tabuleiro".
- **Vale a pena manter o `Grid` como fachada.** O `GridController` **não deve** mexer célula por célula (`grid[pos].set_content(...)`) — isso o acoplaria à estrutura interna do grid. Ter o `Grid` como porta de entrada única é uma boa prática (Lei de Deméter).

Ou seja: a "repetição" que você viu **não é o problema real** — ela é uma delegação legítima da fachada. **O problema de verdade está em outro lugar.**

## O problema real: o `GridController` sabe demais

O `GridController` deveria falar sobre **regras do jogo**, mas hoje ele está cheio de **matemática de topologia do grid**:

- Faz loops manuais sobre `grid.grid_size` em 3 lugares diferentes (popular, mover, spawnar).
- Calcula o que é "borda" do grid na mão.
- Tem `_step_towards` (geometria de deslocamento) e cálculo de distância de Manhattan.
- Controla o dicionário de posições ocupadas para evitar colisões.

Nada disso é "regra de jogo" — é **conhecimento de como o grid é feito**. Isso deveria morar no `Grid`.

## O que proponho

### 1. Mover a "inteligência de grid" para dentro do `Grid`
Criar no `Grid` métodos de consulta reutilizáveis: `get_all_positions()`, `get_border_positions()`, `get_content_positions()`, `get_adjacent_positions(pos)`, `is_within_bounds(pos)`, `distance(a, b)` e `step_towards(from, to)`.

**Benefício:** o `GridController` deixa de duplicar loops e cálculos; qualquer código futuro (outra IA de inimigo, novos modos) reaproveita esses métodos.

### 2. Padronizar o vocabulário da fachada
Renomear os métodos do `Grid` para um vocabulário curto e consistente: `set_content`, `clear_content`, `get_content`, `has_content`, `move_content`, `shake`.

**Benefício:** some a verbosidade (`attach_node_to_position` → `set_content`). Não há conflito com o `Cell` porque os nomes vivem em escopos diferentes (`grid.set_content(pos, n)` vs `cell.set_content(n)`).

### 3. Transformar os comportamentos em primitivas reutilizáveis
- `grid.move_all_content_towards(target, exclude)` — o "todos avançam um passo em direção ao jogador, em cascata, sem buracos".
- `grid.fill_empty_border(factory, exclude)` — "preencha as bordas vazias" recebendo uma *função fábrica* que cria o inimigo.

**Ponto importante:** o `Grid` **não deve conhecer inimigos**. Por isso ele recebe uma função que sabe criar o inimigo, mantendo o `Grid` genérico e reutilizável.

### 4. Corrigir um vazamento de abstração
Hoje o controller faz `enemy_character.get_node("Health")` — dependendo do nome do nó como string. Como `Character` já expõe `health` de forma tipada, passa a ser `enemy_character.health`.

**Benefício:** mais seguro (erro de tipo em vez de erro em runtime) e desacoplado da árvore de nós.

## Resultado final (como fica cada arquivo)

- **`Cell`**: continua igual — dono do visual e das animações de uma célula.
- **`Grid`**: vira a única API pública + dono de toda a lógica de topologia e dos comportamentos reutilizáveis.
- **`GridController`**: fica enxuto, só orquestrando regras de jogo:
  ```
  mover jogador → grid.move_all_content_towards(...) → grid.fill_empty_border(...)
  ```

## O que **não** vou fazer
- Não vou fundir `Cell` dentro do `Grid` (perderia coesão).
- Não vou mudar nenhum comportamento do jogo — é refatoração estrutural. O timing das animações e as regras permanecem idênticos.

## Sua decisão
Se concordar, aplico seguindo [[plano_refatoracao]]. Se preferir uma variação (ex.: fundir mesmo `Cell` no `Grid`, ou manter os nomes atuais), me diga e ajusto o plano.