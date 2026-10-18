---
tags:
  - design
  - interface
status: em definição (implementado, com valores iniciais configuráveis)
updated: 2026-10-18
---
# Interface: Controles, HUD, Menus e Tutorial

> Subdocumento do [[GDD]]. Resumo e decisões principais na seção 4. O glossário do jogo (2.7) está em [[meta_progressao_e_perfil]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

Interface em UI Toolkit, em orientação retrato no mobile (ver 8.1). Os controles são os mesmos em mobile e PC: toque no mobile, clique do mouse no PC. Não há controle de teclado.

## 4.1 Esquema de Controles
| Ação | Entrada |
|------|--------|
| Andar ou atacar | Toque/clique em uma célula do grid. Anda ou ataca conforme o conteúdo da célula e o alcance. Em cada turno o jogador anda uma vez e age uma vez (ataque ou skill), na ordem que quiser (decisão de 2026-10-18). Ação inválida não gasta nada. |
| Encerrar o turno | Botão "Encerrar turno", sempre visível na vez do jogador, ou toque no próprio personagem (que também serve para passar a vez sem fazer nada). O turno também acaba sozinho quando o jogador não consegue fazer mais nada: andar, agir e usar consumível já usados ou impossíveis. Com um item utilizável, a vez espera. |
| Desfazer o movimento | Botão "Desfazer", enquanto o jogador ainda não agiu nem usou consumível depois de andar e a célula de destino não tem terreno que reage ao entrar. |
| Usar skill | Toque no botão da skill na barra de skills (parte inferior da tela); as células de alcance são destacadas, e um toque na célula alvo usa a skill. Tocar de novo no botão da skill cancela. |
| Usar consumível | Toque no botão do item na barra de itens, acima da barra de skills. Não gasta o andar nem o agir do turno, e só um consumível pode ser usado por turno (ver 2.8). ❓ Se o item precisa de alvo (valor inicial: depende do item; a bomba tem alvo, e a poção e o tônico não). |
| Escolher nó no mapa | Toque em um nó disponível mostra a prévia (tipo, XP total, papéis dos inimigos e terreno); um botão de confirmar entra no nó. Tocar em outro nó troca a prévia. |
| Ver detalhes de skill ou item | Toque e segure (ou clique com o botão direito) num botão da barra de skills ou de itens: abre a mesma janela de detalhes das células, com a ficha da skill ou do item (descrição, dano, alcance, área, alvos, recarga, linha de visão, empurrão e se está pronta). Fecha pelo X; o jogo fica pausado enquanto está aberta. |
| Mirar skill | Toque no botão da skill: o destaque mostra todas as células do alcance que ela vê (linha de visão), vazias ou não; toque numa delas para usar. |
| Escolher talento | Toque em um dos 3 talentos oferecidos. Botões de reroll, banir e pular na mesma tela. Cada carta mostra só o próprio talento: ícone, nome, nível do talento, descrição e a skill que ele libera (decisão de 2026-10-12: sem indicar se combina com a build; configurável em `ModalsSettings.showBuildMatchOnCards`). |
| Ver detalhes de uma entidade | Toque longo (*long tap*) em uma célula com uma entidade (o jogador ou um inimigo); no PC, clique com o botão direito do mouse. Mostra os detalhes dela: atributos, skills e os estados ativos com a duração restante; no caso de inimigos, também o papel e o comportamento. Os detalhes abrem em uma janela própria, que só fecha quando o jogador clica no X da janela (soltar o dedo ou o botão não fecha). Enquanto a janela está aberta, o jogo não aceita ações. Abrir ou fechar a janela não gasta nada do turno nem faz nenhuma ação no jogo. O toque curto no mobile e o clique esquerdo no PC continuam sendo andar ou atacar. No toque longo num inimigo, o grid também marca as células que ele alcança com o ataque básico no próximo turno (andando e atacando); a marcação continua depois de fechar a janela, até o próximo toque no grid. Toque longo numa célula marcada por uma habilidade telegrafada mostra quem a prepara, a habilidade, o dano previsto e em quantos turnos ela dispara (design, não implementado; ver [[inimigos#Habilidades telegrafadas]]). |
| Abrir menu | Botão de menu na HUD. |
| Abrir glossário | Botão no menu principal, no mapa e no menu de pausa (ver 2.7). |

- As entradas de jogo são aceitas apenas na vez do jogador, e ignoradas durante as animações e o turno dos inimigos.
- Durante a pausa da escolha de talento, só a tela de escolha responde ao toque.
- A janela de detalhes de uma entidade não gasta nada do turno e não ataca nem anda. ❓ Se o jogador também vê os talentos já escolhidos (por exemplo, nos detalhes do próprio personagem), e, no mobile, o tempo de espera que distingue um toque curto de um longo para não abrir os detalhes sem querer. Valor inicial do tempo de espera: 0,45 s (ver [[valores_padrao_em_aberto]]).
- ❓ Como o banimento é feito (botão em cada talento ou modo de banir). Valor inicial: modo de banir (toca em Banir e depois no talento; tocar de novo em Banir cancela), e a mesma posição recebe um novo sorteio (ver [[valores_padrao_em_aberto]]).

## 4.2 HUD
**Em batalha (retrato, de cima para baixo):**
- **Topo:** a barra de XP com o nível do jogador (já implementada, com os orbs de XP vindo dos inimigos derrotados). À direita da barra de XP fica o botão de menu/pausa (que substituiu o botão de traits).
- **Meio:** o grid.
- **Parte inferior, barra de skills:** 6 botões quadrados de 32×32 (na resolução de referência) com as skills liberadas no momento, e o cooldown de cada uma. Em uma run, o jogador só pode liberar 6 talentos que liberam skills (ver 3.5).
- **Acima da barra de skills, barra de itens:** botões circulares de 32×32 com os consumíveis que o jogador tem e pode usar (ver 2.8).
- **Entre o grid e a barra de itens:** o que ainda resta no turno, com um pé (andar), uma espada (agir) e uma poção (o consumível do turno), ícones de 16×16 que apagam quando usados. Também ficam ali os botões "Desfazer" (enquanto dá para voltar o movimento) e "Encerrar turno" (sempre na vez do jogador).
- **Sobre as entidades no grid:** barra de vida e texto de dano flutuante; nos inimigos, também o ícone de papel.
- **Habilidades telegrafadas (design de 2026-10-18, não implementado):** as células da área preparada ficam com um destaque de aviso, de cor própria, visível o tempo todo (inclusive na vez dos inimigos); sobre quem prepara aparece o ícone da habilidade com o número de turnos até o disparo. Ver [[inimigos#Habilidades telegrafadas]].
- **Estados:** os estados ativos de cada entidade, com a duração restante, ficam nos detalhes da entidade (toque longo na célula, ver 4.1). ❓ Se o jogador também tem ícones de estado fixos na HUD para ver de relance.
- ❓ Onde ficam a vida do jogador em número e a profundidade (sugestão: a vida como barra sobre o personagem, como nas outras entidades, e a profundidade perto da barra de XP). Valor inicial: a profundidade fica num bloco à esquerda da barra de XP (ver [[valores_padrao_em_aberto]]).

**No mapa:**
- Vida do jogador, nível e barra de XP e profundidade; acesso ao glossário e ao menu. Consumíveis só são usados em batalha. ❓ Se o mapa mostra os consumíveis que o jogador tem (só para consulta). Valor inicial: sim (`ScreensSettings.showConsumablesOnMap`).

❓ Como a HUD fica no PC.

## 4.3 Menus
- **Tela inicial:** toque para começar (já implementada).
- **Menu principal:** Novo jogo, Continuar (só aparece com uma run em andamento que pode ser retomada), Glossário e Opções.
- **Seleção de personagem:** aparece só depois de "Novo jogo", com um card por classe e "Voltar". As classes bloqueadas mostram o progresso de liberação (por exemplo, "7/15 batalhas", ver 3.2). Tocar num card abre os **detalhes do personagem**: retrato, descrição, atributos iniciais e skills iniciais, com "Voltar" e "Jogar" (classe bloqueada: mostra o progresso e não tem "Jogar"). "Jogar" com uma run em andamento pede confirmação, porque a run atual é descartada (decisão de 2026-10-12).
- **Mapa:** nós, caminho percorrido, prévia do nó e botão de confirmar.
- **Batalha:** grid e HUD.
- **Detalhes da entidade:** janela aberta por toque longo (mobile) ou clique direito (PC) em uma entidade, fechada pelo X da janela (ver 4.1).
- **Escolha de talento:** tela sobre a batalha ou o mapa, com o jogo pausado (ver Mecânica 3).
- **Glossário:** seções de classes e de inimigos (ver [[meta_progressao_e_perfil]]). ❓ Se é acessível durante a pausa de escolha de talento. Valor inicial: não (`ModalsSettings.allowGlossaryDuringTalentChoice`).
- **Pausa (overlay de menu, já existe):** continuar, glossário, opções e desistir da run. ❓ O que acontece com a run ao desistir (conta como derrota no registro de nível e profundidade). Valor inicial: conta como derrota (ver [[valores_padrao_em_aberto]]).
- **Opções:** idioma (inglês, espanhol e português do Brasil; ver 8.1), volume da música e dos efeitos (ver 5.2) e ❓ vibração, entre outros.
- **Fim de run (derrota):** resumo da build, nível e profundidade alcançados, progresso de liberação de classes e anúncio (ver seção 7).
- **Vitória:** resumo da run e o que foi desbloqueado.
- **Remoção de anúncios:** tela de compra (ver seção 7).

## 4.4 Tutorial e primeira experiência

- **Primeira partida:** só o Guerreiro está aberto no início (3.2), o que concentra o aprendizado em uma classe.
- **O que o jogador precisa aprender:** mover e atacar, o XP e a subida de nível, a escolha de talento com o jogo pausado, a escolha de nós e a prévia, a vida que persiste entre batalhas, o toque longo para ver detalhes de uma entidade, e as ferramentas de oferta (reroll, banir, pular).
- **Como ensinar (sugestão):** dicas contextuais curtas na primeira run, mostradas na primeira vez em que cada situação aparece (primeira batalha, primeira subida de nível, primeiro mapa, primeiro nó de cura, primeiro toque longo). Cada dica aparece uma vez, e o perfil do jogador guarda quais já foram vistas (8.1). Todas são traduzidas (8.1).
- **Implementado:** nove dicas, uma por situação: primeira batalha, primeiro mapa, primeira subida de nível, primeira batalha vencida, primeiro nó de cura, de talento e de consumível, primeiro toque longo e primeira skill.
- **Referência:** o glossário do jogo (2.7) funciona como consulta depois da primeira run.
- ❓ Se haverá um tutorial guiado separado, se a primeira run tem um mapa mais curto ou batalhas mais fáceis, e se as dicas podem ser desativadas nas Opções. Valor inicial: podem, e vêm ativadas por padrão (ver [[valores_padrao_em_aberto]]).
