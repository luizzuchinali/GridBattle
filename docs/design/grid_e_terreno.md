---
tags:
  - design
  - grid
status: em definição (não implementado)
updated: 2026-10-03
---
# Grid e Terreno

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 5 (seção 2.3). Os inimigos estão em [[inimigos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 5: Personalidade do Grid
- **Descrição:** O grid de cada batalha pode ter elementos de terreno que mudam o posicionamento e o valor das skills. Em vez de um tabuleiro vazio, cada batalha tem seu formato. Por exemplo, obstáculos criam corredores onde skills de área ficam muito fortes, e células de perigo obrigam o jogador a decidir onde vale a pena ficar.
- **Elementos de terreno (exemplos, a refinar):**
  - **Obstáculo:** célula bloqueada, que ninguém ocupa nem atravessa.
  - **Célula de perigo:** quem estiver nela sofre dano ou recebe um estado negativo (por exemplo, fogo, veneno ou gelo).
  - **Célula de bônus:** quem estiver nela recebe um estado benéfico.
  - ❓ Lista final de elementos, se bloqueiam alcance e área de skills, quando o efeito é aplicado, e se afetam jogador e inimigos da mesma forma (a direção inicial é que sim, coerente com a regra de que jogador e inimigos seguem as mesmas regras).
- **Geração:** o terreno de cada batalha é gerado pela *seed* e pela profundidade, junto com a composição dos inimigos. ❓ Regras de geração (quantidade e posições, garantir que o grid continue jogável e que o jogador e os inimigos tenham espaço para entrar).
- **Entrada do jogador:** Toque nas células (andar ou atacar) como na Mecânica 1; o terreno limita ou altera as opções.
- **Feedback visual/sonoro:** Visual distinto para cada tipo de célula e indicação clara do efeito antes de entrar nela.
- **Interação com outras mecânicas:** Células de perigo e de bônus aplicam estados (ver 3.1). O terreno reduz as células livres e interage com o limite de inimigos pela capacidade do grid (Mecânica 2). A prévia do nó mostra o terreno da batalha.
