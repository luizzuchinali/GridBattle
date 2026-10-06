---
tags:
  - design
  - balanceamento
status: em definição (implementado, com valores iniciais configuráveis)
updated: 2026-10-05
---
# Balanceamento e Geração de Batalhas

> Subdocumento do [[GDD]]. Resumo e decisões principais em 2.9 e na Mecânica 2. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## 2.9 Balanceamento e Fórmulas

Esta seção reúne as regras numéricas do jogo, que hoje estão espalhadas pelas mecânicas. Os valores são provisórios, e as fórmulas marcadas como sugestão não foram decididas.

- **Princípio:** a dificuldade vem da profundidade no mapa; o poder do jogador vem do nível (XP). A geração é determinística por seed (Mecânica 2), então o balanceamento pode ser testado com simulações do mapa antes de implementar.
- **Curva de XP:** a regra e a curva implementada estão em [[xp_e_niveis]]. Aqui ficam os valores e a fórmula final.
- **XP das batalhas:** O XP de cada batalha vem do orçamento de ameaça da profundidade e da dificuldade: orçamento × 8,5 × (1 + 0,07 × (profundidade − 1)), dividido entre os inimigos pela ameaça de cada um. Assim uma batalha fácil sempre dá menos XP que uma normal, e uma normal menos que uma difícil, no mesmo andar (fonte `ThreatBudget` em `BattleGenerationSettings`). As fontes antigas (XP base de cada inimigo escalado pela profundidade, ou ameaça de cada inimigo) continuam disponíveis como opção.
- **Tamanho da run:** os valores por profundidade (orçamento, escala, pool, XP, faixas de terreno) são escritos para um mapa de referência de 30 andares (`balanceFloorCount`) e esticados para o número real de andares. Com outro tamanho de mapa, o primeiro andar e o chefe têm a mesma força, e o XP de cada batalha muda para a run render mais ou menos os mesmos níveis.
- **Escala dos inimigos:** ❓ Como vida e dano dos inimigos crescem com a profundidade. Material de apoio: a proposta em `docs/propostas` testou +5% de vida e +4% de dano por nível, mas no modelo antigo de ondas. Valor inicial configurável: +2,1% de vida e +1,03% de dano por andar num mapa de 30 andares (os +6% e +3% pensados para 11 andares, convertidos), em crescimento linear (ver [[valores_padrao_em_aberto]]).
- **Ameaça de um inimigo (sugestão, não decidida):** `dano × vida / 50 × (1 + 0,25 × (movimento − 1)) × (1 + 0,5 × (alcance − 1)) × fator do papel`, usada para montar batalhas por orçamento de ameaça (Mecânica 2). Elites contariam como vários inimigos. Esta fórmula já é usada pelo gerador de batalhas, com fator de papel 1 para todos os papéis.
- **Dano e defesa:** o dano básico e o das skills são valores fixos. ❓ Fórmula da defesa (valor fixo ou porcentagem) e se o jogo prefere números fixos ou porcentagens em geral (decisão em aberto, ver 3.1). Valor inicial configurável: defesa fixa (`dano − defesa`) e dano mínimo 1; o modo percentual existe, com teto de 80% (ver [[valores_padrao_em_aberto]]).
- **Vida e cura:** ❓ Quanto o nó de cura recupera, quanto dano o jogador deve sofrer em média por batalha e como a cura da build entra nessa conta. Valor inicial do nó de cura: 30% da vida máxima (ver [[valores_padrao_em_aberto]]).
- **Faixa de níveis:** a diferença entre quem pega só batalhas fáceis e só difíceis é a alavanca de risco e recompensa (ver [[xp_e_niveis]]). ❓ Valores alvo.
- **Ferramentas:** simulação do mapa e métricas de balanceamento (seção 9). O protótipo de ondas em `docs/design/prototipo_ondas.py` serve de ponto de partida, mas está desatualizado para o modelo de nós.

## Geração das batalhas (parte da Mecânica 2)
- **Geração das batalhas:** as batalhas e o mapa não são escritos à mão; um algoritmo os formula de acordo com a profundidade. Entradas: a *seed* da run, a profundidade (e a faixa de dificuldade do nó) e a pool de inimigos do jogo. Saída: quais inimigos entram e em que quantidade, além do XP da batalha. A quantidade é definida pela força de cada inimigo em relação à força esperada para aquela profundidade (inimigos mais fortes entram em profundidades maiores, e os mais fracos aparecem em maior número). A mesma *seed* gera sempre o mesmo mapa e as mesmas batalhas, o que torna a run totalmente reproduzível (para debug). *Seeds* diferentes geram runs diferentes, para que o jogador não sinta repetição entre runs. A mesma *seed* governa todos os aspectos aleatórios da run (mapa, batalhas, ofertas de talento, rerolls e demais sorteios): com a mesma *seed* e as mesmas ações do jogador, a run se repete por completo.
- **Em aberto (geração):** ❓ função da força esperada por profundidade (e o quanto ela assume do nível do jogador), ❓ força de cada inimigo, ❓ regras da pool por profundidade (profundidade mínima/máxima de cada inimigo), ❓ regras de elites e chefes e ❓ limite de inimigos pela capacidade do grid. Valores iniciais configuráveis: orçamento de ameaça de 6 + 1,5 por profundidade, com fácil ×0,75, normal ×1 e difícil ×1,35; os cinco inimigos básicos desde a profundidade 1 e os provisórios entre as profundidades 3 e 6; chefe final provisório; de 1 a 8 inimigos, ocupando no máximo 40% do grid (ver [[valores_padrao_em_aberto]]).
- **Composição das batalhas:** o algoritmo de geração (Mecânica 2) monta as batalhas combinando papéis, e não só inimigos soltos. ❓ Regras de composição (por exemplo, nunca uma batalha só de atiradores, limite de controladores por batalha). Valor inicial: pelo menos um inimigo de linha de frente (corpo a corpo ou enxame).
- **Seed:** a *seed* da run existe apenas para debug e reprodução de cenários e governa todos os sorteios da run, incluindo as ofertas de talento, o mapa e as batalhas; não é exposta ao jogador (ver [[talentos_e_oferta]]).

## Profundidade e dificuldade
- **Spawn por profundidade:** A tabela de spawn filtra inimigos por profundidade mínima/máxima. Conforme o jogador avança no mapa, inimigos mais fortes aparecem.
- **Curva de dificuldade:** sobe com a profundidade ao longo do mapa, até o chefe final. ❓ Valores e ritmo a definir.
- **Inimigos mais fortes com o avanço no mapa:** o avanço no mapa introduz inimigos mais fortes (ver [[mapa_e_nos]] para a definição de profundidade).
