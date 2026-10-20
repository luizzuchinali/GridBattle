---
tags:
  - design
  - grid
status: em definição (implementado, com valores iniciais configuráveis; números pequenos e terreno criado por skills implementados em 2026-10-19)
updated: 2026-10-19
---
# Grid e Terreno

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 5 (seção 2.3). Os inimigos estão em [[inimigos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 5: Personalidade do Grid
- **Descrição:** O grid de cada batalha pode ter elementos de terreno que mudam o posicionamento e o valor das skills. Em vez de um tabuleiro vazio, cada batalha tem seu formato. Por exemplo, obstáculos criam corredores onde skills de área ficam muito fortes, e células de perigo obrigam o jogador a decidir onde vale a pena ficar.
- **Elementos de terreno (exemplos, a refinar):**
  - **Obstáculo:** célula bloqueada, que ninguém ocupa nem atravessa.
  - **Célula de perigo:** quem estiver nela sofre dano ou recebe um estado negativo (por exemplo, fogo, veneno ou gelo).
  - **Célula de bônus:** quem estiver nela recebe um estado benéfico.
  - ❓ Lista final de elementos, se bloqueiam alcance e área de skills, quando o efeito é aplicado, e se afetam jogador e inimigos da mesma forma (a direção inicial é que sim, coerente com a regra de que jogador e inimigos seguem as mesmas regras). Valores iniciais configuráveis: Rocha (obstáculo), Fogo (1 de dano), Pântano Venenoso (Envenenado por 2 turnos), Santuário de Cura (Regeneração) e Pedra Protetora (Escudo); a Muralha de Gelo, criada só por skill, é um obstáculo temporário; o terreno gerado não bloqueia área de skill (a Muralha de Gelo bloqueia); a Rocha bloqueia a linha de visão das skills (decisão de 2026-10-12: obstáculos e personagens no caminho impedem mirar através deles, menos as skills que ignoram linha de visão); o efeito se aplica ao fim do turno de quem está na célula; afeta jogador e inimigos igualmente (ver [[valores_padrao_em_aberto]]).
- **Geração:** o terreno de cada batalha é gerado pela *seed* e pela profundidade, junto com a composição dos inimigos. ❓ Regras de geração (quantidade e posições, garantir que o grid continue jogável e que o jogador e os inimigos tenham espaço para entrar). Valores iniciais configuráveis (faixas no mapa de referência de 30 andares): andares 1 a 3 sem terreno; a partir do 4, 0 a 2 obstáculos, 0 a 1 célula de perigo e 0 a 1 de bônus; a partir do 10, 1 a 2, 0 a 2 e 0 a 1; a partir do 16, 1 a 3, 1 a 3 e 0 a 2; a partir do 22, 2 a 4, 2 a 3 e 1 a 2; as células ao redor do ponto de partida do jogador ficam livres e os obstáculos nunca dividem o grid (ver [[valores_padrao_em_aberto]]).
- **Entrada do jogador:** Toque nas células (andar ou atacar) como na Mecânica 1; o terreno limita ou altera as opções.
- **Caminho do andar (decisão de 2026-10-20, implementada):** o personagem anda célula a célula pelo caminho mais curto de casas livres, só na horizontal e na vertical. Obstáculos e personagens (jogador e inimigos) bloqueiam o caminho; a diagonal entre dois deles também. Hoje o terreno só reage na célula de chegada; efeitos "ao passar por uma célula" podem usar o evento de cada célula cruzada (ver [[sistema_combate]]).
- **Feedback visual/sonoro:** Visual distinto para cada tipo de célula e indicação clara do efeito antes de entrar nela.
- **Interação com outras mecânicas:** Células de perigo e de bônus aplicam estados (ver 3.1). O terreno reduz as células livres e interage com o limite de inimigos pela capacidade do grid (Mecânica 2). A prévia do nó mostra o terreno da batalha.
- **Direção de 2026-10-19:** com números pequenos, o Fogo causa 1 (implementado). O terreno vira um dos eixos da dificuldade por complexidade: mais obstáculos que limitam a fuga dos golpes anunciados e perigos perto das áreas marcadas (ver [[balanceamento_e_geracao#Dificuldade por complexidade (2026-10-19)]]); implementado só pelas faixas de profundidade acima, sem colocar perigos perto das áreas marcadas. Skills, talentos e consumíveis que criam ou tiram terreno passam a ser comuns (ver [[classes_e_skills#Nova direção das skills (2026-10-19)]]). Implementado: terreno temporário, que some depois de algumas rodadas, criado pela Bola de Fogo (Fogo), Muralha de Gelo (Muralha de Gelo), Linha Venenosa (Pântano Venenoso), pelo talento Rastro de Chamas (Fogo) e pelos golpes de inimigos Erupção (Fogo) e Maldição do Pântano (Pântano Venenoso), todos por 2 rodadas nos valores atuais (a variante Muralha Duradoura, 4). Não há skills nem consumíveis que tirem terreno.
- **Implementação:** terreno, geração, efeitos e valores atuais em [[sistema_combate]].
