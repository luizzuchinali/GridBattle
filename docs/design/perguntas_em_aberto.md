---
tags:
  - design
  - indice
updated: 2026-10-03
---
# Perguntas em aberto

> Índice das perguntas em aberto (❓) do [[GDD]] e dos subdocumentos, agrupadas por documento e seção. Cada pergunta vive no documento de origem (o link leva até a seção); quando for decidida, a decisão entra lá e a pergunta sai deste índice. Este índice foi gerado em 2026-10-03 e precisa ser regenerado quando as perguntas mudarem.


Total: 84 perguntas.


## [[GDD]] (19)


### [[GDD#2.6 Economia|2.6 Economia]]

- [ ] … O custo do nó de talento ainda é — a definir (ver Mecânica 2).

### [[GDD#3.4 Mundo / Cenário|3.4 Mundo / Cenário]]

- [ ] **Estrutura de fases/mundo:** Se isso muda no futuro (por exemplo, variação de cor do grid por trecho do mapa, ver 5.1).

### [[GDD#5.1 Direção de Arte|5.1 Direção de Arte]]

- [ ] **Paleta de cores:** Número de cores e a paleta final.
- [ ] **Referências:** A definir.
- [ ] Arte do mapa de nós (ícones dos tipos de nó e do caminho), do glossário e das telas de fim de run, e se haverá variação visual do grid conforme a profundidade.

### [[GDD#5.2 Áudio|5.2 Áudio]]

- [ ] **Trilha sonora:** Número de faixas de batalha (para não repetir sempre a mesma) e se a música continua durante a pausa da escolha de talento.
- [ ] **Controles de volume:** Se há botão rápido de silenciar e como o áudio se comporta durante os anúncios (ver seção 7).

### [[GDD#7. Monetização (se aplicável)|7. Monetização (se aplicável)]]

- [ ] **Modelo:** Se o modelo é o mesmo no PC e no mobile.
- [ ] **Anúncios forçados:** Quais outros momentos de transição.
- [ ] **Rampa de frequência:** Valores de X, duração da fase inicial e se a contagem é por nós, por batalhas vencidas ou por tempo (em avaliação: contar por batalhas e impor um tempo mínimo entre anúncios, porque os nós de cura e de consumível são instantâneos).
- [ ] **Anúncio recompensado (opcional):** Limite de recargas e se ele pode ser pedido durante a pausa da escolha de talento no meio da batalha (é uma ação iniciada pelo jogador, mas ocorre em batalha).
- [ ] **Remoção de anúncios:** Preço e se qualquer compra também remove os anúncios forçados.
- [ ] **Ética de monetização:** Definir um teto de recargas igual para quem assiste a anúncios e para quem comprou a remoção, para que o dinheiro não compre poder.

### [[GDD#8.1 Especificações|8.1 Especificações]]

- [ ] **Resolução alvo:** Como a escala se comporta em telas de proporção diferente de 9:16 e no PC (por exemplo, escala inteira com bordas).
- [ ] **Orientação:** Janela e proporção no PC (sugestão: janela em retrato ou com a interface centralizada).
- [ ] **Estado da run:** Detalhes do formato do salvamento e o que acontece se um anúncio estiver pendente ao fechar.
- [ ] **Idiomas:** Idioma padrão na primeira abertura (sugestão: o idioma do sistema, com inglês como padrão quando não houver tradução), se a troca de idioma vale na hora ou só ao reabrir, e quem faz as traduções.

### [[GDD#8.2 Requisitos mínimos|8.2 Requisitos mínimos]]

- [ ] Requisitos mínimos de PC (sistema operacional, memória e placa de vídeo) e a lista de aparelhos de teste.

### [[GDD#9. Métricas de Sucesso|9. Métricas de Sucesso]]

- [ ] Como os dados são coletados e armazenados (ferramenta de análise), o consentimento de privacidade necessário e se os dados são só locais (para teste interno) ou enviados para um servidor.

## [[mapa_e_nos]] (6)


### [[mapa_e_nos#Mecânica 2: Mapa, Nós e Batalhas|Mecânica 2: Mapa, Nós e Batalhas]]

- [ ] **Descrição:** Formato exato do mapa (andares, ramificações e número de caminhos).
- [ ] **Run finita:** Número de nós e duração da run.
- [ ] **Dificuldade das batalhas no mesmo andar:** Como as batalhas do mesmo andar variam de dificuldade (sugestão: cada nó de batalha tem uma faixa de dificuldade, por exemplo fácil, normal e difícil, que muda o número e a força dos inimigos e o XP concedido; elites e chefes seriam casos especiais).
- [ ] **Talento:** Qual custo (por exemplo, vida, um consumível ou uma penalidade temporária).
- [ ] Outros tipos (por exemplo, elite, evento, chefe) e a distribuição deles no mapa.
- [ ] **Fim da batalha:** A rever só se acontecer na prática.

## [[consumiveis]] (4)


### [[consumiveis#2.8 Consumíveis|2.8 Consumíveis]]

- [ ] **Armazenamento:** Número de espaços (a barra comporta 6 botões de 32 px na resolução de referência) e o que acontece quando o jogador recebe um consumível com os espaços cheios.
- [ ] **Uso:** Se o consumível precisa de alvo na batalha (por exemplo, a bomba de área) e como o alvo é escolhido.
- [ ] **Obtenção:** Se o jogador recebe um consumível sorteado pela seed ou escolhe entre opções, e se consumíveis também podem vir de outras fontes (por exemplo, como custo ou recompensa do nó de talento).
- [ ] **Interação com outras mecânicas:** Lista final, raridade e se os consumíveis aparecem no glossário do jogo (2.7).

## [[xp_e_niveis]] (5)


### [[xp_e_niveis#Regras de XP e nível|Regras de XP e nível]]

- [ ] **XP por inimigo derrotado:** Inimigos invocados durante a batalha (por exemplo, por um Invocador) devem dar XP? (sugestão: não, ou o XP total da batalha poderia ser inflado).
- [ ] **Nível máximo fixo:** Valor do nível máximo, número de batalhas do mapa em relação a ele, quanto XP a batalha difícil e a elite concedem em relação ao custo do nível, e se o nó de talento conta como nível (hoje ele concede só o talento, sem XP).
- [ ] **Custo por nível crescente:** A fórmula da curva de custo, e se a sobra de XP é guardada.
- [ ] **Subida de nível na hora:** Se a escolha pode ser adiada para o fim da batalha (hoje não: o jogo pausa e exige a escolha) e como funciona o reroll, o banimento e o pular durante a pausa.

### [[xp_e_niveis#Curva implementada hoje|Curva implementada hoje]]

- [ ] **Curva de XP implementada:** Ajustar a curva e o XP dos inimigos juntos para a regra de XP por batalha (Mecânica 2).

## [[talentos_e_oferta]] (6)


### [[talentos_e_oferta#Escolha de talentos ao subir de nível (Mecânica 3)|Escolha de talentos ao subir de nível (Mecânica 3)]]

- [ ] **Descrição:** a definir.
- [ ] **Configuração da escolha de talentos:** Quantidades base.
- [ ] **Ferramentas do jogador:** a definir.
- [ ] **Ferramentas do jogador:** O que acontece com o nível ao pular a oferta (sugestão: o nível é perdido, sem talento).

### [[talentos_e_oferta#Talentos (3.5)|Talentos (3.5)]]

- [ ] **Limite de talentos de skill:** Como a oferta se comporta depois do sexto (sugestão: talentos que liberam skills deixam de ser oferecidos).
- [ ] Lista final de talentos e seus valores, a pool compartilhada, se um talento pode ter mais de um nível (o mesmo talento escolhido de novo), e quantos talentos por classe.

## [[balanceamento_e_geracao]] (11)


### [[balanceamento_e_geracao#2.9 Balanceamento e Fórmulas|2.9 Balanceamento e Fórmulas]]

- [ ] **Escala dos inimigos:** Como vida e dano dos inimigos crescem com a profundidade.
- [ ] **Dano e defesa:** Fórmula da defesa (valor fixo ou porcentagem) e se o jogo prefere números fixos ou porcentagens em geral (decisão em aberto, ver 3.1).
- [ ] **Vida e cura:** Quanto o nó de cura recupera, quanto dano o jogador deve sofrer em média por batalha e como a cura da build entra nessa conta.
- [ ] **Faixa de níveis:** Valores alvo.

### [[balanceamento_e_geracao#Geração das batalhas (parte da Mecânica 2)|Geração das batalhas (parte da Mecânica 2)]]

- [ ] **Em aberto (geração):** função da força esperada por profundidade (e o quanto ela assume do nível do jogador),
- [ ] **Em aberto (geração):** força de cada inimigo,
- [ ] **Em aberto (geração):** regras da pool por profundidade (profundidade mínima/máxima de cada inimigo),
- [ ] **Em aberto (geração):** regras de elites e chefes e
- [ ] **Em aberto (geração):** limite de inimigos pela capacidade do grid.
- [ ] **Composição das batalhas:** Regras de composição (por exemplo, nunca uma batalha só de atiradores, limite de controladores por batalha).

### [[balanceamento_e_geracao#Profundidade e dificuldade|Profundidade e dificuldade]]

- [ ] **Curva de dificuldade:** Valores e ritmo a definir.

## [[estados_e_atributos]] (7)


### [[estados_e_atributos#3.1 Atributos e Estados|3.1 Atributos e Estados]]

- [ ] Quais atributos e estados também valem para os inimigos.

### [[estados_e_atributos#Atributos|Atributos]]

- [ ] … Defensivo | Defesa | Reduz o dano recebido. — Valor fixo ou porcentagem.
- [ ] Em turnos ou porcentagem, e mínimo.

### [[estados_e_atributos#Estados|Estados]]

- [ ] Valor fixo ou porcentagem da vida máxima.
- [ ] … temporária que absorve dano antes da vida real. — Como é obtido e se expira.
- [ ] … final de estados, seus nomes e valores são — a definir.
- [ ] Reaplicação e acúmulo do mesmo estado, remoção antecipada, e o que acontece com os estados ao mudar de batalha.

## [[inimigos]] (7)


### [[inimigos#Mecânica 4: Papéis dos Inimigos|Mecânica 4: Papéis dos Inimigos]]

- [ ] Lista final de papéis e de inimigos de cada papel.

### [[inimigos#Mecânica 6: Inimigos que Cobram a Build|Mecânica 6: Inimigos que Cobram a Build]]

- [ ] Lista final, frequência com que aparecem e se isso se liga a elites e chefes.
- [ ] **Como são definidos:** Quais atributos e estados valem para os inimigos.
- [ ] **Interação com outras mecânicas:** Se o sorteio de talentos deve considerar os inimigos que o jogador já viu na run.

### [[inimigos#3.3 NPCs / Inimigos|3.3 NPCs / Inimigos]]

- [ ] **Skills dos inimigos:** Lista de skills de cada tipo de inimigo e como elas aparecem na janela de detalhes da entidade.
- [ ] **Chefes e elites:** Lista de chefes e elites (ver Mecânica 2) e se algum inimigo é o chefe final.
- [ ] Papel de cada inimigo (Mecânica 4) e quais são inimigos que cobram a build (Mecânica 6).

## [[grid_e_terreno]] (2)


### [[grid_e_terreno#Mecânica 5: Personalidade do Grid|Mecânica 5: Personalidade do Grid]]

- [ ] Lista final de elementos, se bloqueiam alcance e área de skills, quando o efeito é aplicado, e se afetam jogador e inimigos da mesma forma (a direção inicial é que sim, coerente com a regra de que jogador e inimigos seguem as mesmas regras).
- [ ] **Geração:** Regras de geração (quantidade e posições, garantir que o grid continue jogável e que o jogador e os inimigos tenham espaço para entrar).

## [[classes_e_skills]] (2)


### [[classes_e_skills#3.2 Personagem do Jogador|3.2 Personagem do Jogador]]

- [ ] Papel e identidade de cada classe e a motivação do personagem.

### [[classes_e_skills#Skills|Skills]]

- [ ] **Slots de skill:** Se a skill inicial da classe ocupa um dos 6 espaços, e o que acontece quando os 6 já estão ocupados (sugestão: talentos que liberam skills deixam de ser oferecidos).

## [[meta_progressao_e_perfil]] (4)


### [[meta_progressao_e_perfil#Liberação das classes (3.2)|Liberação das classes (3.2)]]

- [ ] Se batalhas vencidas em runs que terminam em derrota contam (sugestão: sim, para o progresso nunca ser perdido) e como avisar o jogador da nova classe liberada (sugestão: tela de fim de run).

### [[meta_progressao_e_perfil#Glossário do jogo (2.7)|Glossário do jogo (2.7)]]

- [ ] O que conta como descoberto para um talento (apenas escolhido, ou também apenas oferecido), se os inimigos ainda não enfrentados aparecem como "?" na lista, se as informações de cada inimigo são reveladas por etapas (por exemplo, mais detalhes depois de enfrentá-lo mais vezes), quais valores de atributo são exibidos (base ou escalados pela profundidade) e onde o glossário fica no menu (ver 4.3).

### [[meta_progressao_e_perfil#Perfil do jogador (8.1)|Perfil do jogador (8.1)]]

- [ ] **Perfil do jogador:** Sincronização entre dispositivos.

### [[meta_progressao_e_perfil#Registro de resultados (2.2)|Registro de resultados (2.2)]]

- [ ] **Registro:** Detalhes do registro (recordes, dificuldades adicionais).

## [[interface]] (11)


### [[interface#4.1 Esquema de Controles|4.1 Esquema de Controles]]

- [ ] … consumível pode ser usado por turno (ver 2.8). — Se o item precisa de alvo.
- [ ] Se o jogador também vê os talentos já escolhidos (por exemplo, nos detalhes do próprio personagem), e, no mobile, o tempo de espera que distingue um toque curto de um longo para não abrir os detalhes sem querer.
- [ ] Como o banimento é feito (botão em cada talento ou modo de banir).

### [[interface#4.2 HUD|4.2 HUD]]

- [ ] **Estados:** Se o jogador também tem ícones de estado fixos na HUD para ver de relance.
- [ ] Onde ficam a vida do jogador em número e a profundidade (sugestão: a vida como barra sobre o personagem, como nas outras entidades, e a profundidade perto da barra de XP).
- [ ] Se o mapa mostra os consumíveis que o jogador tem (só para consulta).
- [ ] Como a HUD fica no PC.

### [[interface#4.3 Menus|4.3 Menus]]

- [ ] **Glossário:** Se é acessível durante a pausa de escolha de talento.
- [ ] **Pausa (overlay de menu, já existe):** O que acontece com a run ao desistir (conta como derrota no registro de nível e profundidade).
- [ ] **Opções:** vibração, entre outros.

### [[interface#4.4 Tutorial e primeira experiência|4.4 Tutorial e primeira experiência]]

- [ ] Se haverá um tutorial guiado separado, se a primeira run tem um mapa mais curto ou batalhas mais fáceis, e se as dicas podem ser desativadas nas Opções.
