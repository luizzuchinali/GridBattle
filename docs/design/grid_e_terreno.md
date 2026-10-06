---
tags:
  - design
  - grid
status: em definição (implementado, com valores iniciais configuráveis)
updated: 2026-10-05
---
# Grid e Terreno

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 5 (seção 2.3). Os inimigos estão em [[inimigos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 5: Personalidade do Grid
- **Descrição:** O grid de cada batalha pode ter elementos de terreno que mudam o posicionamento e o valor das skills. Em vez de um tabuleiro vazio, cada batalha tem seu formato. Por exemplo, obstáculos criam corredores onde skills de área ficam muito fortes, e células de perigo obrigam o jogador a decidir onde vale a pena ficar.
- **Elementos de terreno (exemplos, a refinar):**
  - **Obstáculo:** célula bloqueada, que ninguém ocupa nem atravessa.
  - **Célula de perigo:** quem estiver nela sofre dano ou recebe um estado negativo (por exemplo, fogo, veneno ou gelo).
  - **Célula de bônus:** quem estiver nela recebe um estado benéfico.
  - ❓ Lista final de elementos, se bloqueiam alcance e área de skills, quando o efeito é aplicado, e se afetam jogador e inimigos da mesma forma (a direção inicial é que sim, coerente com a regra de que jogador e inimigos seguem as mesmas regras). Valores iniciais configuráveis: Rocha (obstáculo), Fogo (4 de dano), Pântano Venenoso (Envenenado), Santuário de Cura (Regeneração) e Pedra Protetora (Escudo); o terreno não bloqueia área de skill; o efeito se aplica ao fim do turno de quem está na célula; afeta jogador e inimigos igualmente (ver [[valores_padrao_em_aberto]]).
- **Geração:** o terreno de cada batalha é gerado pela *seed* e pela profundidade, junto com a composição dos inimigos. ❓ Regras de geração (quantidade e posições, garantir que o grid continue jogável e que o jogador e os inimigos tenham espaço para entrar). Valores iniciais configuráveis: profundidade 1 sem terreno; de 2 a 4, 0 a 2 obstáculos, 0 a 1 célula de perigo e 0 a 1 de bônus; de 5 em diante, 1 a 3, 1 a 2 e 0 a 1; as células ao redor do ponto de partida do jogador ficam livres e os obstáculos nunca dividem o grid (ver [[valores_padrao_em_aberto]]).
- **Entrada do jogador:** Toque nas células (andar ou atacar) como na Mecânica 1; o terreno limita ou altera as opções.
- **Feedback visual/sonoro:** Visual distinto para cada tipo de célula e indicação clara do efeito antes de entrar nela.
- **Interação com outras mecânicas:** Células de perigo e de bônus aplicam estados (ver 3.1). O terreno reduz as células livres e interage com o limite de inimigos pela capacidade do grid (Mecânica 2). A prévia do nó mostra o terreno da batalha.
- **Implementação:** terreno, geração, efeitos e valores atuais em [[sistema_combate]].
