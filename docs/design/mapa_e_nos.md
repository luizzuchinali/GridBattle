---
tags:
  - design
  - mapa
status: em definição (implementado, com valores iniciais configuráveis)
updated: 2026-10-05
---
# Mapa, Nós e Batalhas

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 2 (seção 2.3). Os consumíveis estão em [[consumiveis]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 2: Mapa, Nós e Batalhas
- **Descrição:** A run é um mapa de nós, como em Slay the Spire. O jogador escolhe o próximo nó entre os disponíveis; ao escolher um nó de batalha, entra no combate no grid. Os inimigos ficam mais fortes conforme o jogador avança no mapa, e a run termina com o chefe final. ❓ Formato exato do mapa (andares, ramificações e número de caminhos). Valores iniciais configuráveis: 30 andares (o último só com o chefe; escolha do usuário em 2026-10-06), 4 colunas, 2 a 4 nós por andar e até 2 caminhos por nó, sem cruzamentos. O balanceamento se ajusta ao número de andares (ver [[balanceamento_e_geracao]] e [[valores_padrao_em_aberto]]).
- **Run finita:** a run tem um número fixo de nós e termina na vitória (chefe final derrotado) ou na morte. Exemplo, não é valor final: um mapa de cerca de 30 nós (a pool de talentos por classe está em [[talentos_e_oferta]]). ❓ Número de nós e duração da run. Valor inicial: cerca de 31 nós (ver [[valores_padrao_em_aberto]]).
- **Profundidade:** quão longe o jogador está no mapa (a posição do nó, de 1 até o chefe final). Define a escala dos inimigos e é o eixo principal de balanceamento da dificuldade.
- **Dificuldade das batalhas no mesmo andar:** ❓ Como as batalhas do mesmo andar variam de dificuldade (sugestão: cada nó de batalha tem uma faixa de dificuldade, por exemplo fácil, normal e difícil, que muda o número e a força dos inimigos e o XP concedido; elites e chefes seriam casos especiais). Valores iniciais configuráveis: fácil 35%, normal 40% e difícil 25% dos nós de batalha (quando um nó leva a duas ou mais batalhas, elas têm dificuldades diferentes), com orçamento de ameaça ×0,75, ×1 e ×1,35 (ver [[valores_padrao_em_aberto]]).
- **Tipos de nó (exemplos):**
  - **Batalha:** combate no grid. Os inimigos derrotados concedem XP, que pode levar a um novo nível e à escolha de um talento.
  - **Cura:** recupera a vida do jogador.
  - **Talento:** oferece a escolha de um talento sem precisar lutar, mas **tem um custo**. ❓ Qual custo (por exemplo, vida, um consumível ou uma penalidade temporária). Valor inicial configurável: 15% da vida máxima, sem nunca matar (ver [[valores_padrao_em_aberto]]).
  - **Consumível:** concede um item de uso único.
  - ❓ Outros tipos (por exemplo, elite, evento, chefe) e a distribuição deles no mapa. Valores iniciais: o chefe final fecha o mapa; a profundidade 1 só tem batalhas e, depois, 55% são batalha, 15% cura, 10% talento e 20% consumível (ver [[valores_padrao_em_aberto]]).
- **Sem luta, sem talento:** o jogador só ganha talento ganhando XP em batalhas ou pelo nó de talento, que tem custo. Os nós de cura e de consumível não exigem limite por andar: quem evita as batalhas chega ao chefe sem talentos e perde.
- **Vida entre batalhas:** a vida do jogador persiste entre os nós. Ela só é recuperada por nós de cura (e por efeitos que a build conceda). Não há cura automática ao vencer uma batalha.
- **Fim da batalha:** a batalha termina quando todos os inimigos são eliminados. Não há limite de turnos: um jogador que enrola indefinidamente simplesmente não vence. Se builds de muita cura tornarem isso um problema, avaliar um debuff que cresce com o tempo na batalha. ❓ A rever só se acontecer na prática.
- **Derrota:** a morte em qualquer batalha encerra a run.
- **Direções para a variedade das batalhas:** para que as batalhas não sejam sempre "correr atrás do jogador e bater", a variedade vem de três mecânicas: papéis dos inimigos (Mecânica 4, ver [[inimigos]]), personalidade do grid (Mecânica 5, ver [[grid_e_terreno]]) e inimigos que cobram a build (Mecânica 6, ver [[inimigos]]).
- **Prévia do nó:** antes de escolher o nó, o jogador vê o tipo do nó e, nas batalhas, o XP total que ela concede, os papéis dos inimigos e o terreno do grid. Não vê a lista exata de inimigos.
- **Entrada do jogador:** Escolha de nó no mapa; dentro da batalha, as ações da Mecânica 1.
- **Feedback visual/sonoro:** Mapa com os tipos de nó e o caminho percorrido, indicação da profundidade, do nível e da barra de XP na HUD e transição entre mapa e batalha.
- **Interação com outras mecânicas:** Os inimigos derrotados concedem XP, que leva a níveis e à escolha de talentos. A dificuldade crescente por profundidade, contra o nível que o jogador conseguiu juntar, é o que testa a build, e a vida persistente faz o mapa pedir decisões de rota.
- **Nível e XP:** ver [[xp_e_niveis]] (regras) e [[talentos_e_oferta]] (escolha de talentos).
- **Geração das batalhas:** ver [[balanceamento_e_geracao]].
