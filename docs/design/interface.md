---
tags:
  - design
  - interface
status: em definição (parte implementada)
updated: 2026-10-03
---
# Interface: Controles, HUD, Menus e Tutorial

> Subdocumento do [[GDD]]. Resumo e decisões principais na seção 4. O glossário do jogo (2.7) está em [[meta_progressao_e_perfil]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

Interface em UI Toolkit, em orientação retrato no mobile (ver 8.1). Os controles são os mesmos em mobile e PC: toque no mobile, clique do mouse no PC. Não há controle de teclado.

## 4.1 Esquema de Controles
| Ação | Entrada |
|------|--------|
| Andar ou atacar | Toque/clique em uma célula do grid. Anda ou ataca conforme o conteúdo da célula e o alcance. Ação inválida não consome o turno. |
| Usar skill | Toque no botão da skill na barra de skills (parte inferior da tela); as células de alcance são destacadas, e um toque na célula alvo usa a skill. Tocar de novo no botão da skill cancela. |
| Usar consumível | Toque no botão do item na barra de itens, acima da barra de skills. Não consome a ação do turno, e só um consumível pode ser usado por turno (ver 2.8). ❓ Se o item precisa de alvo. |
| Escolher nó no mapa | Toque em um nó disponível mostra a prévia (tipo, XP total, papéis dos inimigos e terreno); um botão de confirmar entra no nó. Tocar em outro nó troca a prévia. |
| Escolher talento | Toque em um dos 3 talentos oferecidos. Botões de reroll, banir e pular na mesma tela. |
| Ver detalhes de uma entidade | Toque longo (*long tap*) em uma célula com uma entidade (o jogador ou um inimigo); no PC, clique com o botão direito do mouse. Mostra os detalhes dela: atributos, skills e os estados ativos com a duração restante; no caso de inimigos, também o papel e o comportamento. Os detalhes abrem em uma janela própria, que só fecha quando o jogador clica no X da janela (soltar o dedo ou o botão não fecha). Enquanto a janela está aberta, o jogo não aceita ações. Abrir ou fechar a janela não consome o turno nem faz nenhuma ação no jogo. O toque curto no mobile e o clique esquerdo no PC continuam sendo andar ou atacar. |
| Abrir menu | Botão de menu na HUD. |
| Abrir glossário | Botão no menu principal, no mapa e no menu de pausa (ver 2.7). |

- As entradas de jogo são aceitas apenas na vez do jogador, e ignoradas durante as animações e o turno dos inimigos.
- Durante a pausa da escolha de talento, só a tela de escolha responde ao toque.
- A janela de detalhes de uma entidade não consome o turno e não ataca nem anda. ❓ Se o jogador também vê os talentos já escolhidos (por exemplo, nos detalhes do próprio personagem), e, no mobile, o tempo de espera que distingue um toque curto de um longo para não abrir os detalhes sem querer.
- ❓ Como o banimento é feito (botão em cada talento ou modo de banir).

## 4.2 HUD
**Em batalha (retrato, de cima para baixo):**
- **Topo:** a barra de XP com o nível do jogador (já implementada, com os orbs de XP vindo dos inimigos derrotados). À direita da barra de XP fica o botão de menu/pausa, no lugar do botão de traits que existe hoje.
- **Meio:** o grid.
- **Parte inferior, barra de skills:** 6 botões quadrados de 32×32 (na resolução de referência) com as skills liberadas no momento, e o cooldown de cada uma. Em uma run, o jogador só pode liberar 6 talentos que liberam skills (ver 3.5).
- **Acima da barra de skills, barra de itens:** botões circulares de 32×32 com os consumíveis que o jogador tem e pode usar (ver 2.8).
- **Sobre as entidades no grid:** barra de vida e texto de dano flutuante; nos inimigos, também o ícone de papel.
- **Estados:** os estados ativos de cada entidade, com a duração restante, ficam nos detalhes da entidade (toque longo na célula, ver 4.1). ❓ Se o jogador também tem ícones de estado fixos na HUD para ver de relance.
- ❓ Onde ficam a vida do jogador em número e a profundidade (sugestão: a vida como barra sobre o personagem, como nas outras entidades, e a profundidade perto da barra de XP).

**No mapa:**
- Vida do jogador, nível e barra de XP e profundidade; acesso ao glossário e ao menu. Consumíveis só são usados em batalha. ❓ Se o mapa mostra os consumíveis que o jogador tem (só para consulta).

❓ Como a HUD fica no PC.

## 4.3 Menus
- **Tela inicial:** toque para começar (já implementada).
- **Menu principal e seleção de classe:** as classes bloqueadas mostram o progresso de liberação (por exemplo, "7/15 batalhas", ver 3.2). Acesso ao glossário e às opções.
- **Mapa:** nós, caminho percorrido, prévia do nó e botão de confirmar.
- **Batalha:** grid e HUD.
- **Detalhes da entidade:** janela aberta por toque longo (mobile) ou clique direito (PC) em uma entidade, fechada pelo X da janela (ver 4.1).
- **Escolha de talento:** tela sobre a batalha ou o mapa, com o jogo pausado (ver Mecânica 3).
- **Glossário:** seções de classes e de inimigos (ver [[meta_progressao_e_perfil]]). ❓ Se é acessível durante a pausa de escolha de talento.
- **Pausa (overlay de menu, já existe):** continuar, glossário, opções e desistir da run. ❓ O que acontece com a run ao desistir (conta como derrota no registro de nível e profundidade).
- **Opções:** idioma (inglês, espanhol e português do Brasil; ver 8.1), volume da música e dos efeitos (ver 5.2) e ❓ vibração, entre outros.
- **Fim de run (derrota):** resumo da build, nível e profundidade alcançados, progresso de liberação de classes e anúncio (ver seção 7).
- **Vitória:** resumo da run e o que foi desbloqueado.
- **Remoção de anúncios:** tela de compra (ver seção 7).

## 4.4 Tutorial e primeira experiência

- **Primeira partida:** só o Guerreiro está aberto no início (3.2), o que concentra o aprendizado em uma classe.
- **O que o jogador precisa aprender:** mover e atacar, o XP e a subida de nível, a escolha de talento com o jogo pausado, a escolha de nós e a prévia, a vida que persiste entre batalhas, o toque longo para ver detalhes de uma entidade, e as ferramentas de oferta (reroll, banir, pular).
- **Como ensinar (sugestão):** dicas contextuais curtas na primeira run, mostradas na primeira vez em que cada situação aparece (primeira batalha, primeira subida de nível, primeiro mapa, primeiro nó de cura, primeiro toque longo). Cada dica aparece uma vez, e o perfil do jogador guarda quais já foram vistas (8.1). Todas são traduzidas (8.1).
- **Referência:** o glossário do jogo (2.7) funciona como consulta depois da primeira run.
- ❓ Se haverá um tutorial guiado separado, se a primeira run tem um mapa mais curto ou batalhas mais fáceis, e se as dicas podem ser desativadas nas Opções.
