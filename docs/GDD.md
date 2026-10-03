---
tags:
  - design
  - gdd
created: 2026-07-12
updated: 2026-10-03
---
## Grid Battle (TEMP)

| Campo                        | Valor                   |
| ---------------------------- | ----------------------- |
| **Título**                   | Grid Battle             |
| **Gênero**                   | RPG, Survive, TurnBased |
| **Plataforma(s)**            | Mobile e PC             |
| **Engine**                   | Unity                   |
| **Classificação indicativa** | Livre                   |
| **Autor(es)**                | Zuchinali Softworks     |

---

## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid. A #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A run é um mapa de nós: o jogador escolhe o caminho, enfrenta batalhas no grid e ganha XP ao vencê-las; a cada nível alcançado, escolhe um trait entre as ofertas da classe. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo.

### 1.3 Pillars de Design

[3 a 5 princípios que guiam todas as decisões de design. Ex.: "Decisões sobre sorte", "Cada partida é única".]

1. Cada ação do jogador importa
2. Cada partida deve passar o sentimento de ser única 
3. O rng no jogo deve ser um fator, porém um fator controlável pelo jogador

### 1.4 Unique Selling Points (USPs)

- **Combate em grid por turnos onde cada ação do jogador gera uma reação.** Mover, atacar ou usar uma skill consome o turno, e todos os inimigos respondem em seguida.
- **Build montada durante a batalha.** O talento é escolhido no momento em que a barra de XP cruza o limiar de nível, com o jogo pausado, inclusive no meio da luta (Mecânica 3). O sorteio é controlável pelo jogador: reroll, banir e pular.
- **Risco e recompensa no mapa.** Batalhas mais difíceis rendem mais XP, e a vida persiste entre as batalhas, então o jogador decide quanto arriscar para ficar mais forte (Mecânica 2).
- **Run curta e de uma mão.** Run finita, em retrato, pensada para sessões curtas no celular, com salvamento automático a cada ação (ver 8.1).

---

## 2. Jogabilidade

### 2.1 Core Loop

Run finita em um mapa de nós, no estilo de Slay the Spire: o jogador escolhe o caminho, enfrenta batalhas no grid e termina a run derrotando o chefe final ou morrendo. O foco é a construção de build ao longo da run.

```
Iniciar a run (classe + seed) → mapa de nós
  → Escolher o próximo nó
      • Batalha → combate em grid por turnos (mover / atacar / usar skill)
          → cada inimigo derrotado soma o seu XP na hora (o total da batalha é mostrado na prévia do nó)
          → se o XP alcançar o próximo nível → subir de nível na hora (inclusive no meio da batalha) → o jogo oferece 3 talentos da classe
          → escolher 1 (ou usar reroll / banir / pular)
          → a build evolui e muda a forma de jogar com a classe
      • Nó sem luta (cura, talento com custo, consumível…) → efeito do nó
  → A vida do jogador persiste entre os nós
  → Repetir até o nó do chefe final
  → Chefe final derrotado → vitória
  → Morte em qualquer batalha → fim da run (resumo da build + nível e profundidade alcançados) → nova run (outra classe ou outro caminho)
```

### 2.2 Objetivo do Jogador

O objetivo é montar builds diferentes com as árvores de talento de cada classe e levá-las até o fim do mapa, derrotando o chefe final.

- **Curto prazo:** vencer a batalha atual e escolher o próximo talento ou o próximo nó.
- **Médio prazo:** formar uma build coerente, com sinergia entre os talentos oferecidos, e decidir a rota no mapa (quando lutar, quando curar).
- **Longo prazo:** vencer a run com cada classe e cada build, explorando caminhos diferentes das árvores.
- **Métrica de sucesso:** vitória, nível do jogador e profundidade alcançados, registrados por classe. ❓ Detalhes do registro (recordes, dificuldades adicionais).

### 2.3 Mecânicas Principais

#### Mecânica 1: Combate em Grid por Turnos
- **Descrição:** O combate acontece num grid de células, com no máximo uma entidade por célula. Cada ação consumida pelo jogador (mover, atacar ou usar skill) é seguida por uma rodada de ações de todos os inimigos. Ações inválidas não consomem o turno. Jogador e inimigos seguem as mesmas regras de alcance e ocupação.
- **Entrada do jogador:** Toque em uma célula (andar ou atacar, conforme o conteúdo e o alcance).
- **Feedback visual/sonoro:** Células destacadas para andar e para atacar, animações de movimento e de ataque, texto de dano flutuante.
- **Interação com outras mecânicas:** Cada inimigo derrotado concede o seu XP na hora, que leva ao nível e à escolha de talentos. Skills e talentos alteram alcance, dano e comportamento das ações.
- **Sem telegrafia (decisão de design):** os inimigos não anunciam onde vão atacar. O jogo se mantém como está: o impacto vem principalmente da build do jogador, e não de esquivar de golpes. A variedade entre batalhas vem de outras fontes (papéis dos inimigos, personalidade do grid e inimigos que cobram a build; ver Mecânicas 4, 5 e 6).

#### Mecânica 2: Mapa, Nós e Batalhas
- **Descrição:** A run é um mapa de nós, como em Slay the Spire. O jogador escolhe o próximo nó entre os disponíveis; ao escolher um nó de batalha, entra no combate no grid. Os inimigos ficam mais fortes conforme o jogador avança no mapa, e a run termina com o chefe final. ❓ Formato exato do mapa (andares, ramificações e número de caminhos).
- **Run finita:** a run tem um número fixo de nós e termina na vitória (chefe final derrotado) ou na morte. Exemplo, não é valor final: um mapa de cerca de 30 nós, com uma pool de cerca de 50 talentos por classe. A pool pode ser maior que o número de escolhas de uma run, então cada run usa só uma parte dela. ❓ Número de nós e duração da run.
- **Profundidade e nível (conceitos separados):**
  - **Profundidade:** quão longe o jogador está no mapa (a posição do nó, de 1 até o chefe final). Define a escala dos inimigos e é o eixo principal de balanceamento da dificuldade.
  - **Nível:** o nível do jogador, obtido com XP. Cada nível alcançado concede a escolha de um talento (Mecânica 3). Varia conforme o caminho escolhido: dois jogadores na mesma profundidade podem ter níveis diferentes.
  - **XP por inimigo derrotado:** cada inimigo tem um valor de XP, que é somado ao jogador na hora em que ele é derrotado (comportamento já implementado). O XP total possível da batalha é a soma dos valores dos inimigos que a compõem, conhecido ao gerar a batalha e mostrado na prévia do nó. Batalhas com mais inimigos ou inimigos mais fortes rendem mais XP, então as batalhas mais difíceis rendem mais. Não há farm de abates porque a batalha tem um número fixo de inimigos e termina quando todos são eliminados. ❓ Inimigos invocados durante a batalha (por exemplo, por um Invocador) devem dar XP? (sugestão: não, ou o XP total da batalha poderia ser inflado). Sugestão: o valor de XP de cada inimigo sai da sua força na fórmula de ameaça (a mesma usada na geração das batalhas), em vez de ser escrito à mão.
  - **Nem toda batalha concede talento:** o jogador pode escolher batalhas mais fáceis, que rendem pouco XP, ou mais difíceis, que rendem mais. A decisão de risco e recompensa é do jogador: o esperado é que ele tenda a escolher batalhas difíceis para ficar mais forte, ao custo de mais dano e de menos vida para as batalhas seguintes.
  - **Nível máximo fixo:** a run tem um nível máximo definido no design, independente do número de batalhas do mapa. Exemplo, não é valor final: nível máximo 30. Nota: o exemplo de cerca de 30 nós por mapa inclui nós sem luta, então com 30 níveis seria preciso que muitas batalhas rendessem mais de um nível, ou que o mapa tivesse mais nós; revisar os dois exemplos em conjunto. O XP além do teto não gera mais níveis. Isso permite que algumas batalhas (por exemplo, as mais difíceis e as elites) concedam mais XP do que o necessário para um nível, levando a mais de um nível de uma vez, e que o mapa tenha mais batalhas do que níveis. Quem pegar as batalhas mais difíceis chega ao nível máximo; quem escolher batalhas mais fáceis chega ao fim com menos níveis (por exemplo, perto de 15 a 25). A diferença entre o máximo e o mínimo é a alavanca de balanceamento da escolha de risco e recompensa: pequena demais e não vale arriscar, grande demais e quem pega só as fáceis não consegue vencer. ❓ Valor do nível máximo, número de batalhas do mapa em relação a ele, quanto XP a batalha difícil e a elite concedem em relação ao custo do nível, e se o nó de talento conta como nível (hoje ele concede só o talento, sem XP).
  - ❓ Como as batalhas do mesmo andar variam de dificuldade (sugestão: cada nó de batalha tem uma faixa de dificuldade, por exemplo fácil, normal e difícil, que muda o número e a força dos inimigos e o XP concedido; elites e chefes seriam casos especiais).
  - **Custo por nível crescente:** o XP necessário para cada nível cresce conforme o nível sobe. A curva de custo e o XP das batalhas, ao longo do mapa, definem quanto XP total o jogador pode juntar e, com isso, até onde cada caminho chega. Para que a batalha difícil renda cerca de um nível, a curva pode acompanhar o XP de uma batalha difícil na profundidade correspondente: como os inimigos ficam mais fortes (e valem mais XP) com a profundidade, a curva pode ser derivada do gerador de batalhas. Exemplo, não é valor final: o nível 1 exige 100 de XP; uma batalha fácil concede 50, uma média 75 e uma difícil 100 nesse ponto. Com esse exemplo, em 30 batalhas, só difíceis rendem cerca de um nível por batalha, só médias cerca de 75% disso e só fáceis cerca de 50%, supondo que o XP que sobra ao subir de nível é guardado. Elites e batalhas especiais podem render mais de 100%. ❓ A fórmula da curva de custo, e se a sobra de XP é guardada.
  - **Subida de nível na hora:** o talento é escolhido no momento em que a barra de XP cruza o limiar do nível, mesmo no meio da batalha. Nesse momento o jogo é pausado e o jogador escolhe o talento; a partida só continua depois da escolha. Se a barra cruzar mais de um limiar (um abate que leve a mais de um nível), as escolhas vêm em sequência, uma por nível, e o jogo só retoma depois da última. O talento escolhido vale já para o restante do turno. Como a lógica é imediata e o visual atrasado, a tela de escolha deve aparecer quando a barra de XP visual cruzar o limiar, depois da animação do abate, e não antes. ❓ Se a escolha pode ser adiada para o fim da batalha (hoje não: o jogo pausa e exige a escolha) e como funciona o reroll, o banimento e o pular durante a pausa.
  - **Sem XP em nós sem combate:** cura e consumível não concedem XP. O nó de talento concede o talento sem XP e tem custo.
- **Tipos de nó (exemplos):**
  - **Batalha:** combate no grid. Os inimigos derrotados concedem XP, que pode levar a um novo nível e à escolha de um talento.
  - **Cura:** recupera a vida do jogador.
  - **Talento:** oferece a escolha de um talento sem precisar lutar, mas **tem um custo**. ❓ Qual custo (por exemplo, vida, um consumível ou uma penalidade temporária).
  - **Consumível:** concede um item de uso único.
  - ❓ Outros tipos (por exemplo, elite, evento, chefe) e a distribuição deles no mapa.
- **Sem luta, sem talento:** o jogador só ganha talento ganhando XP em batalhas ou pelo nó de talento, que tem custo. Os nós de cura e de consumível não exigem limite por andar: quem evita as batalhas chega ao chefe sem talentos e perde.
- **Vida entre batalhas:** a vida do jogador persiste entre os nós. Ela só é recuperada por nós de cura (e por efeitos que a build conceda). Não há cura automática ao vencer uma batalha.
- **Fim da batalha:** a batalha termina quando todos os inimigos são eliminados. Não há limite de turnos: um jogador que enrola indefinidamente simplesmente não vence. Se builds de muita cura tornarem isso um problema, avaliar um debuff que cresce com o tempo na batalha. ❓ A rever só se acontecer na prática.
- **Derrota:** a morte em qualquer batalha encerra a run.
- **Direções para a variedade das batalhas:** para que as batalhas não sejam sempre "correr atrás do jogador e bater", a variedade vem de três mecânicas: papéis dos inimigos (Mecânica 4), personalidade do grid (Mecânica 5) e inimigos que cobram a build (Mecânica 6).
- **Prévia do nó:** antes de escolher o nó, o jogador vê o tipo do nó e, nas batalhas, o XP total que ela concede, os papéis dos inimigos e o terreno do grid. Não vê a lista exata de inimigos.
- **Geração das batalhas:** as batalhas e o mapa não são escritos à mão; um algoritmo os formula de acordo com a profundidade. Entradas: a *seed* da run, a profundidade (e a faixa de dificuldade do nó) e a pool de inimigos do jogo. Saída: quais inimigos entram e em que quantidade, além do XP da batalha. A quantidade é definida pela força de cada inimigo em relação à força esperada para aquela profundidade (inimigos mais fortes entram em profundidades maiores, e os mais fracos aparecem em maior número). A mesma *seed* gera sempre o mesmo mapa e as mesmas batalhas, o que torna a run totalmente reproduzível (para debug). *Seeds* diferentes geram runs diferentes, para que o jogador não sinta repetição entre runs. A mesma *seed* governa todos os aspectos aleatórios da run (mapa, batalhas, ofertas de talento, rerolls e demais sorteios): com a mesma *seed* e as mesmas ações do jogador, a run se repete por completo.
- **Em aberto (geração):** ❓ função da força esperada por profundidade (e o quanto ela assume do nível do jogador), ❓ força de cada inimigo, ❓ regras da pool por profundidade (profundidade mínima/máxima de cada inimigo), ❓ regras de elites e chefes e ❓ limite de inimigos pela capacidade do grid.
- **Entrada do jogador:** Escolha de nó no mapa; dentro da batalha, as ações da Mecânica 1.
- **Feedback visual/sonoro:** Mapa com os tipos de nó e o caminho percorrido, indicação da profundidade, do nível e da barra de XP na HUD e transição entre mapa e batalha.
- **Interação com outras mecânicas:** Os inimigos derrotados concedem XP, que leva a níveis e à escolha de talentos. A dificuldade crescente por profundidade, contra o nível que o jogador conseguiu juntar, é o que testa a build, e a vida persistente faz o mapa pedir decisões de rota.

#### Mecânica 3: XP, Nível e Escolha de Talentos (build adaptativa)
- **Descrição:** O jogador ganha XP a cada inimigo derrotado. A cada nível alcançado, no momento em que ele ocorre (inclusive no meio da batalha), o jogo oferece 3 talentos sorteados entre os que a classe escolhida pode pegar naquele momento (pré-requisitos cumpridos). O jogador escolhe 1. A build nasce da adaptação ao que aparece, e não de um plano fechado desde o início. O sorteio é ponderado por sinergia: talentos ligados aos já escolhidos têm mais chance de aparecer, para que as builds tendam a se formar sem serem garantidas. Detalhes do sorteio (por exemplo, evitar repetição excessiva ou proteção contra azar) ❓ a definir.
- **Configuração da escolha de talentos:** o número de opções oferecidas (base: 3), de rerolls e de banimentos são configurações da run, não características de classe. Estados ativos podem alterá-las (por exemplo, um estado que concede mais rerolls). ❓ Quantidades base.
- **Recuperação de vida:** subir de nível não recupera vida. A vida persiste entre as batalhas e se recupera nos nós de cura do mapa (ver Mecânica 2).
- **Ferramentas do jogador:** o RNG deve ser controlável pelo jogador (pilar 3). Para isso, o jogador pode **rerrolar** a oferta, **banir** um talento da pool e **pular** a oferta. Custo de uso ❓ a definir. Possibilidade em avaliação: recarregar essas ferramentas assistindo a anúncio recompensado, de forma gratuita para quem comprar a remoção de anúncios (ver seção 7).
- **Fora do escopo desta mecânica:** escolha livre periódica na árvore inteira (descartada). A *seed* da run existe apenas para debug e reprodução de cenários e governa todos os sorteios da run, incluindo as ofertas de talento, o mapa e as batalhas (ver Mecânica 2); não é exposta ao jogador.
- **Entrada do jogador:** Tela de escolha que aparece no momento em que o jogador alcança um nível (inclusive no meio da batalha, com o jogo pausado) e no nó de talento, com as 3 opções e as ferramentas. Toque para escolher.
- **Feedback visual/sonoro:** Destaque nos talentos que combinam com a build atual; efeito visual de aquisição.
- **Interação com outras mecânicas:** Talentos concedem estados (que alteram atributos, comportamentos e regras do jogo) e liberam skills ativas. As batalhas, cada vez mais difíceis, testam a build.

#### Mecânica 4: Papéis dos Inimigos
- **Descrição:** Cada inimigo tem um papel que define como ele se comporta, em vez de todos correrem atrás do jogador e atacarem. Hoje todos os inimigos (Goblin, Rat, Slime, FireSkull e EyeBat) seguem o mesmo comportamento de perseguir e atacar, e só diferem em números. Com papéis distintos, o jogador passa a decidir quem enfrentar primeiro e como se posicionar, e o ataque básico no inimigo mais próximo deixa de ser sempre a melhor jogada. Os inimigos seguem as mesmas regras de turno, alcance e ocupação do jogador (Mecânica 1) e não telegrafam ataques.
- **Papéis (exemplos, a refinar):**
  - **Corpo a corpo:** persegue o jogador e ataca (o comportamento atual).
  - **Enxame:** frágil, rápido e em quantidade.
  - **Atirador:** ataca à distância e tenta manter a distância do jogador.
  - **Suporte:** cura ou fortalece aliados, o que o torna um alvo prioritário.
  - **Invocador:** cria novos inimigos ao longo da batalha.
  - **Controlador:** aplica estados negativos ao jogador (por exemplo, Fraquejado ou impedir o movimento).
  - ❓ Lista final de papéis e de inimigos de cada papel.
- **Composição das batalhas:** o algoritmo de geração (Mecânica 2) monta as batalhas combinando papéis, e não só inimigos soltos. ❓ Regras de composição (por exemplo, nunca uma batalha só de atiradores, limite de controladores por batalha).
- **Entrada do jogador:** Nenhuma direta; o jogador reage ao comportamento dos inimigos pelas ações da Mecânica 1.
- **Feedback visual/sonoro:** Ícone ou marca visual do papel sobre o inimigo; a prévia do nó mostra os papéis presentes.
- **Interação com outras mecânicas:** Controladores e suportes aplicam estados (ver 3.1). Alcance, área e controle do jogador passam a ter valor para alcançar atiradores e suportes que ficam atrás. A prévia do nó revela os papéis para o jogador planejar a rota.

#### Mecânica 5: Personalidade do Grid
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

#### Mecânica 6: Inimigos que Cobram a Build
- **Descrição:** Alguns inimigos exigem uma resposta específica da build e não podem ser resolvidos só com o ataque básico. Cada um testa uma parte da build, e uma build de uma coisa só será testada em algum momento, o que incentiva o jogador a diversificar os talentos. Um jogador experiente sabe quais inimigos podem aparecer e escolhe os talentos para lidar com eles em um momento futuro da run; assim, o conhecimento do jogo vira vantagem estratégica.
- **Exemplos (a refinar):**
  - **Blindado:** defesa alta; pede penetração de defesa ou dano que ignore defesa (por exemplo, dano ao longo do tempo).
  - **Regenerante:** recupera vida; pede dano concentrado (burst).
  - **Enxame:** muitos inimigos frágeis; pede skills de área.
  - **Rápido:** alcance de movimento alto; pede controle (por exemplo, impedir o movimento).
  - **Espinhoso:** devolve parte do dano recebido; pede skills à distância ou dano ao longo do tempo.
  - ❓ Lista final, frequência com que aparecem e se isso se liga a elites e chefes.
- **Como são definidos:** pelos atributos e estados do inimigo (ver 3.1), o que permite criar essas respostas obrigatórias combinando os mesmos blocos do sistema de estados. ❓ Quais atributos e estados valem para os inimigos.
- **Entrada do jogador:** Nenhuma direta; a escolha acontece na tela de talentos (Mecânica 3) e na rota do mapa (Mecânica 2).
- **Feedback visual/sonoro:** Indicação visível das características do inimigo (por exemplo, ícone de blindado ou de regenerante), para que o jogador leia o que ele exige.
- **Interação com outras mecânicas:** A prévia do nó mostra os papéis dos inimigos, o que informa a escolha de rota. A pool de inimigos possíveis é parte do conhecimento do jogador para escolher talentos (Mecânica 3). O Glossário do jogo (2.7) registra os inimigos já enfrentados. ❓ Se o sorteio de talentos deve considerar os inimigos que o jogador já viu na run.

### 2.4 Mecânicas Secundárias

> **Será revisto no futuro.** A ideia inicial é deixar o jogo base funcionando antes de implementar mais coisas acima desta camada. O que segue abaixo é a direção atual, sujeita a revisão.

#### Mecânica 7: Skills Ativas
- **Descrição:** Cada classe possui um conjunto de skills ativas adquiridas via traits. Skills são ações especiais com cooldown, área de efeito (shape), dano (se ofensivas) e outros parâmetros. São reutilizáveis (cooldown) e vinculadas ao personagem.
- **Entrada do jogador:** Toque em botão de skill na HUD → seleção de alvo.
- **Feedback visual/sonoro:** Animação própria da skill, efeito de área no grid, partículas, som de ativação.
- **Interação com outras mecânicas:** Skills podem ter sinergia com traits (ex.: um trait que reduz cooldown de skills de fogo).

### 2.5 Progressão

> **Será revisto no futuro.** As regras abaixo descrevem a direção desejada da progressão; os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.

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

- **Spawn por profundidade:** A tabela de spawn filtra inimigos por profundidade mínima/máxima. Conforme o jogador avança no mapa, inimigos mais fortes aparecem.
- **Traits por nível:** Traits têm nível requerido e pré-requisitos. Ao alcançar um nível, o jogador vê 3 traits sorteados entre os disponíveis para a classe, com peso maior para os que têm sinergia com a build atual (ver Mecânica 3 em 2.3).

- **Sistema de progressão:** XP por inimigo derrotado e nível do jogador (a profundidade no mapa é um eixo separado), árvore de traits e skills por classe
- **Curva de dificuldade:** sobe com a profundidade ao longo do mapa, até o chefe final. ❓ Valores e ritmo a definir.
- **Unlocks:** Traits desbloqueiam skills ativas (que entram no repertório do personagem). O avanço no mapa introduz inimigos mais fortes.

### 2.6 Economia
Não há moeda por enquanto. Os recursos da run são: vida (persiste entre os nós e se recupera em nós de cura), XP (por inimigo derrotado, leva a níveis e talentos), consumíveis (nó de consumível) e as ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3). O custo do nó de talento ainda é ❓ a definir (ver Mecânica 2). Itens ativos e equipáveis estão fora do escopo (ver 11.2).

### 2.7 Glossário do Jogo

> Não confundir com o glossário de termos da seção 13 deste documento. Este é um recurso dentro do jogo.

- **Descrição:** o jogo tem um glossário que o jogador preenche conforme joga. Ele registra o que o jogador já descobriu, e o que ainda não descobriu aparece escondido. Também serve de referência para o jogador experiente planejar a build (ver Mecânica 6 em 2.3).
- **Seção de classes:** mostra as classes já liberadas. Em cada classe aparecem os talentos que o jogador já escolheu em algum momento do jogo. Os talentos que nunca foram escolhidos aparecem como "?".
- **Seção de inimigos:** mostra todos os inimigos que o jogador já enfrentou. De cada um é possível ver os atributos, as skills e como o tipo de inimigo age (seu papel e comportamento).
- **Persistência:** o glossário vale entre runs. O que foi descoberto fica registrado no perfil do jogador, e não só na run atual.
- ❓ O que conta como descoberto para um talento (apenas escolhido, ou também apenas oferecido), se os inimigos ainda não enfrentados aparecem como "?" na lista, se as informações de cada inimigo são reveladas por etapas (por exemplo, mais detalhes depois de enfrentá-lo mais vezes), quais valores de atributo são exibidos (base ou escalados pela profundidade) e onde o glossário fica no menu (ver 4.3).

---


## 3. Personagens e Mundo

### 3.1 Atributos e Estados

Duas camadas descrevem e alteram um personagem. Elas formam o vocabulário que talentos, skills e inimigos usam para modificá-lo:

- **Atributos:** os valores base que configuram o personagem no início da run. Cada classe define os seus (ver 3.2). O valor em jogo é o valor base somado aos estados ativos.
- **Estados:** têm um nome e concedem um ou mais efeitos ao personagem, que incrementam ou reduzem atributos ou modificam comportamentos do jogo. A duração (permanente ou X turnos) não é fixa do estado: depende da situação do jogo ou do talento que o aplica.

**Talentos (traits)** concedem estados, e é por meio deles que modificam o personagem e o jogo. Skills e inimigos também aplicam estados.

As configurações da run (opções de talento, rerolls e banimentos) não são atributos de classe; ver Mecânica 3 em 2.3. Estados ativos podem alterá-las.

❓ Quais atributos e estados também valem para os inimigos.

#### Atributos

| Categoria | Atributo | Descrição | Situação |
|---|---|---|---|
| Base | Vida máxima | Quantidade máxima de vida. | Existe |
| Base | Alcance de movimento | Distância que o personagem anda por ação. | Existe |
| Base | Alcance de ataque | Distância do ataque básico. | Existe |
| Ofensivo | Dano básico | Dano do ataque básico. | Existe |
| Ofensivo | Chance de crítico | Probabilidade de um ataque causar dano extra. | Novo |
| Ofensivo | Multiplicador de crítico | Quanto o crítico multiplica o dano. | Novo |
| Ofensivo | Penetração de defesa | Ignora parte da defesa do alvo. | Novo |
| Defensivo | Defesa | Reduz o dano recebido. ❓ Valor fixo ou porcentagem. | Novo |
| Skills | Bônus de dano de skills | Aumenta o dano das skills ofensivas. | Novo |
| Skills | Alcance de skills | Aumenta a distância máxima para escolher o alvo. | Novo |
| Skills | Redução de cooldown | Skills voltam a ficar disponíveis mais cedo. ❓ Em turnos ou porcentagem, e mínimo. | Novo |

#### Estados

Um estado tem um **nome** e concede um ou mais efeitos. Ao ser aplicado, recebe uma **duração**: permanente ou X turnos. Quem define a duração é a situação do jogo ou o talento que o aplica, então o mesmo estado pode ser temporário num caso e permanente em outro. Exemplo: o estado **Fraquejado** deixa o personagem com 50% menos dano.

| Estado                          | O que faz                                                                           |
| ------------------------------- | ----------------------------------------------------------------------------------- |
| Fraquejado                      | Reduz o dano do personagem em 50%.                                                  |
| Armadura de Espinhos            | Ao ser atacado, devolve X de dano ao atacante.                                      |
| Roubo de vida (nome provisório) | Ao causar dano, recupera uma porcentagem dele como vida.                            |
| Regeneração (nome provisório)   | A cada turno do jogador, recupera vida. ❓ Valor fixo ou porcentagem da vida máxima. |
| Escudo (nome provisório)        | Vida temporária que absorve dano antes da vida real. ❓ Como é obtido e se expira.   |
| Envenenado (nome provisório)    | Causa dano ao longo dos turnos.                                                     |

A lista final de estados, seus nomes e valores são ❓ a definir.

**Turno global e turno das entidades.** Existe o **turno global**. Em cada turno global o jogador joga primeiro e depois jogam os inimigos. O turno global termina depois que a última entidade do grid faz sua ação. Cada entidade (jogador ou inimigo) tem o seu próprio turno dentro do turno global.

**Duração.** A duração de um estado é contada em turnos da **entidade que está com o estado**, seja um buff ou um debuff, e não importa quem o aplicou. Um estado de duração 2 dura 2 turnos da entidade portadora: a duração diminui ao fim de cada turno dela e o estado termina quando chega a zero. O turno da portadora conta como o primeiro se ela ainda não terminou o turno dela no turno global atual; caso contrário, o primeiro é o próximo turno dela.

Exemplo com um buff de 2 turnos que o jogador aplica em si mesmo:

```
Turno global 1: Jogador usa o buff (1º turno do buff, ao fim da jogada: duração 2 → 1)
                Inimigos jogam (buff ativo)
Turno global 2: Jogador joga (2º turno do buff, ao fim da jogada: duração 1 → 0, o buff termina)
                Inimigos jogam (sem buff)
Turno global 3: Jogador joga (sem buff)
```

Exemplo com Fraquejado de 2 turnos aplicado pelo jogador em um inimigo (o turno do inimigo ainda não aconteceu no turno global 1, então conta como o primeiro):

```
Turno global 1: Jogador aplica Fraquejado (duração 2) em um inimigo
                O inimigo joga, com Fraquejado (1º turno, duração 2 → 1)
Turno global 2: Jogador joga
                O inimigo joga, com Fraquejado (2º turno, duração 1 → 0, termina)
Turno global 3: Jogador joga
                O inimigo joga, sem Fraquejado
```

Exemplo inverso: um inimigo aplica um debuff de 2 turnos no jogador durante a jogada dele no turno global 1. O turno do jogador nesse turno global já terminou, então a contagem começa no turno 2: o debuff afeta as jogadas do jogador nos turnos globais 2 e 3.

❓ Reaplicação e acúmulo do mesmo estado, remoção antecipada, e o que acontece com os estados ao mudar de batalha.

Fora do conjunto por enquanto: esquiva e tamanho da área das skills.

### 3.2 Personagem do Jogador
Valores atuais das três classes, como estão implementados no jogo (um asset de configuração por classe). Todos os valores são provisórios e serão balanceados.

| Classe | Vida máxima | Alcance de movimento | Alcance de ataque | Dano básico |
|---|---|---|---|---|
| Guerreiro (Knight) | 120 | 1 | 1 | 15 |
| Mago (Mage) | 80 | 1 | 1 | 15 |
| Ladino (Rogue) | 90 | 1 | 1 | 15 |

- Hoje as classes diferem só na vida máxima. Movimento, alcance e dano básico são iguais, e nenhuma tem skill configurada (a lista de skills de cada classe em 6.2 é a direção de design, ainda não implementada).
- **Curva de XP implementada:** o XP para subir do nível 1 ao 2 é 50, e cada nível seguinte soma 25 (50, 75, 100, 125...). O valor é configurado por classe e hoje é igual nas três. Isso já segue a regra de custo por nível crescente (Mecânica 2), mas os inimigos hoje dão um XP fixo (3.3), sem escalar com a profundidade. ❓ Ajustar a curva e o XP dos inimigos juntos para a regra de XP por batalha (Mecânica 2).
- ❓ Papel e identidade de cada classe e a motivação do personagem. Nada disso está definido ainda, só os números acima. Sugestão a validar: o Guerreiro com mais vida (já é assim), o Mago com skills de área e o Ladino com mais mobilidade ou alcance.
- **Classes disponíveis:** apenas uma classe está aberta desde o início; as outras duas são liberadas jogando.
  - **Guerreiro (Knight):** disponível desde o início.
  - **Mago (Mage):** liberado ao vencer 15 batalhas.
  - **Ladino (Rogue):** liberado ao vencer 30 batalhas.
  - Só contam batalhas vencidas (nós de cura, talento e consumível não entram na contagem).
  - A contagem é acumulada entre runs (soma de todas as batalhas vencidas em todas as runs) e fica guardada no perfil do jogador (8.1).
  - ❓ Se batalhas vencidas em runs que terminam em derrota contam (sugestão: sim, para o progresso nunca ser perdido) e como avisar o jogador da nova classe liberada (sugestão: tela de fim de run).

> Skills de classe e slots de skill fazem parte do escopo "Será revisto no futuro".

- **Skills de classe:** Cada classe começa com 1-2 skills básicas (ex.: Guerreiro começa com "Golpe" — dano em área frontal). Novas skills são desbloqueadas via traits.
- **Slots de skill:** O personagem pode carregar até N skills ativas por vez, selecionadas na tela de classe antes da run (ou durante, via traits).

### 3.3 NPCs / Inimigos
Papéis e comportamentos dos inimigos: ver Mecânica 4. Inimigos que exigem resposta da build: ver Mecânica 6 em 2.3.

Inimigos implementados hoje (um asset de configuração por inimigo). Valores provisórios.

| Nome | Vida máxima | Alcance de movimento | Alcance de ataque | Dano básico | XP |
|---|---|---|---|---|---|
| Goblin | 30 | 1 | 1 | 5 | 10 |
| Rat (Rato) | 20 | 2 | 1 | 5 | 8 |
| Slime | 40 | 1 | 1 | 5 | 12 |
| FireSkull (Crânio de fogo) | 25 | 1 | 2 | 7 | 15 |
| EyeBat (Morcego-olho) | 25 | 2 | 1 | 5 | 8 |

- **Comportamento atual:** todos seguem o mesmo comportamento (Mecânica 4): se estiverem no alcance de ataque do jogador, atacam; senão, dão um passo em direção a ele. Nenhum tem skill configurada. Eles só diferem nos números da tabela.
- **Encontro atual:** o encontro de teste coloca os cinco inimigos no grid de uma vez.
- **Chefes e elites:** ainda não implementados. ❓ Lista de chefes e elites (ver Mecânica 2) e se algum inimigo é o chefe final.
- ❓ Papel de cada inimigo (Mecânica 4) e quais são inimigos que cobram a build (Mecânica 6).

Cada inimigo tem um valor de XP, somado ao XP da batalha (ver Mecânica 2 em 2.3).

### 3.4 Mundo / Cenário
- **Ambientação:** masmorra subterrânea. A run é uma descida por salas e corredores (os nós do mapa) até o chefe final, onde as batalhas acontecem em salas de grid. Os inimigos são criaturas da masmorra (goblin, rato, slime, crânio de fogo, morcego-olho e as próximas).
- **Estética:** fantasia sombria leve, em pixel art (ver 5.1). O terreno das batalhas (obstáculos, células de perigo e de bônus, ver Mecânica 5) é o que dá personalidade a cada sala.
- **Estrutura de fases/mundo:** mapa de nós ramificado e gerado por seed, com batalhas no grid (ver Mecânica 2 em 2.3). Um único tema visual do início ao fim: a dificuldade e os inimigos mudam com a profundidade, mas não há regiões visualmente diferentes. ❓ Se isso muda no futuro (por exemplo, variação de cor do grid por trecho do mapa, ver 5.1).
- **Narrativa:** sem história explícita. O mundo é sugerido pela arte e pelos nomes, sem diálogos nem textos de história, o que também evita traduzir narrativa para os três idiomas (ver 8.1).

---

## 4. Controles e Interface

Interface em UI Toolkit, em orientação retrato no mobile (ver 8.1). Os controles são os mesmos em mobile e PC: toque no mobile, clique do mouse no PC. Não há controle de teclado.

### 4.1 Esquema de Controles
| Ação | Entrada |
|------|--------|
| Andar ou atacar | Toque/clique em uma célula do grid. Anda ou ataca conforme o conteúdo da célula e o alcance. Ação inválida não consome o turno. |
| Usar skill | Toque no botão da skill na HUD (as células de alcance são destacadas) e toque na célula alvo. Tocar de novo no botão da skill cancela. |
| Escolher nó no mapa | Toque em um nó disponível mostra a prévia (tipo, XP total, papéis dos inimigos e terreno); um botão de confirmar entra no nó. Tocar em outro nó troca a prévia. |
| Escolher talento | Toque em um dos 3 talentos oferecidos. Botões de reroll, banir e pular na mesma tela. |
| Ver detalhes de uma entidade | Toque longo (*long tap*) em uma célula com uma entidade (o jogador ou um inimigo); no PC, clique com o botão direito do mouse. Mostra os detalhes dela: atributos, skills e os estados ativos com a duração restante; no caso de inimigos, também o papel e o comportamento. Os detalhes abrem em uma janela própria, que só fecha quando o jogador clica no X da janela (soltar o dedo ou o botão não fecha). Enquanto a janela está aberta, o jogo não aceita ações. Abrir ou fechar a janela não consome o turno nem faz nenhuma ação no jogo. O toque curto no mobile e o clique esquerdo no PC continuam sendo andar ou atacar. |
| Abrir menu | Botão de menu na HUD. |
| Abrir glossário | Botão no menu principal, no mapa e no menu de pausa (ver 2.7). |

- As entradas de jogo são aceitas apenas na vez do jogador, e ignoradas durante as animações e o turno dos inimigos.
- Durante a pausa da escolha de talento, só a tela de escolha responde ao toque.
- A janela de detalhes de uma entidade não consome o turno e não ataca nem anda. ❓ Se o jogador também vê os talentos já escolhidos (por exemplo, nos detalhes do próprio personagem), e, no mobile, o tempo de espera que distingue um toque curto de um longo para não abrir os detalhes sem querer.
- ❓ Como o banimento é feito (botão em cada talento ou modo de banir).

### 4.2 HUD
**Em batalha:**
- Vida do jogador (barra e número).
- Barra de XP com o nível do jogador (já implementada, com os orbs de XP vindo dos inimigos derrotados).
- Profundidade no mapa.
- Botões das skills ativas, com o cooldown de cada uma.
- Os estados ativos de cada entidade, com a duração restante, ficam nos detalhes da entidade (toque longo na célula, ver 4.1). ❓ Se o jogador também tem ícones de estado fixos na HUD para ver de relance.
- Barra de vida e ícone de papel sobre cada inimigo; texto de dano flutuante.
- Botão de menu.

**No mapa:**
- Vida do jogador, nível e barra de XP, consumíveis e profundidade.
- Acesso ao glossário e ao menu.

❓ Posição exata dos elementos no retrato (sugestão: HUD no alto, grid no centro, botões de skill embaixo, ao alcance do polegar) e como ficam no PC.

### 4.3 Menus
- **Tela inicial:** toque para começar (já implementada).
- **Menu principal e seleção de classe:** as classes bloqueadas mostram o progresso de liberação (por exemplo, "7/15 batalhas", ver 3.2). Acesso ao glossário e às opções.
- **Mapa:** nós, caminho percorrido, prévia do nó e botão de confirmar.
- **Batalha:** grid e HUD.
- **Detalhes da entidade:** janela aberta por toque longo (mobile) ou clique direito (PC) em uma entidade, fechada pelo X da janela (ver 4.1).
- **Escolha de talento:** tela sobre a batalha ou o mapa, com o jogo pausado (ver Mecânica 3).
- **Glossário:** seções de classes e de inimigos (ver 2.7). ❓ Se é acessível durante a pausa de escolha de talento.
- **Pausa (overlay de menu, já existe):** continuar, glossário, opções e desistir da run. ❓ O que acontece com a run ao desistir (conta como derrota no registro de nível e profundidade).
- **Opções:** idioma (inglês, espanhol e português do Brasil; ver 8.1), volume da música e dos efeitos (ver 5.2) e ❓ vibração, entre outros.
- **Fim de run (derrota):** resumo da build, nível e profundidade alcançados, progresso de liberação de classes e anúncio (ver seção 7).
- **Vitória:** resumo da run e o que foi desbloqueado.
- **Remoção de anúncios:** tela de compra (ver seção 7).

---

## 5. Arte e Áudio

### 5.1 Direção de Arte
- **Estilo visual:** pixel art 2D, com câmera Pixel Perfect (PPU 100) e render URP 2D. Resolução de referência em retrato de 216×384. Os personagens e inimigos são sprites animados em tiras de quadros (hoje 36×30 px por quadro); skills e elementos de interface têm folhas de sprite próprias.
- **Tom:** fantasia sombria leve. Fundo escuro e criaturas estranhas (como o crânio de fogo e o morcego-olho), mas legível e sem ser pesado, coerente com a classificação indicativa livre.
- **Paleta de cores:** paleta limitada e fixa, com um número pequeno de cores compartilhado por todo o jogo, para manter a pixel art coesa e facilitar a criação de conteúdo novo. Na interface já existem o fundo escuro (rgb 27, 27, 27), o amarelo de destaque (#ffcc00) e o dourado do XP. ❓ Número de cores e a paleta final.
- **Legibilidade (prioridades):** a arte deve deixar claro, de relance:
  - **Papéis dos inimigos:** silhueta e cor distintas por papel (corpo a corpo, enxame, atirador, suporte, invocador, controlador, elite), além do ícone de papel sobre o inimigo (Mecânica 4).
  - **Terreno do grid:** obstáculos, células de perigo e células de bônus identificáveis antes de o jogador entrar nelas (Mecânica 5).
  - **Estados e dano:** ícones de estado e texto de dano legíveis no tamanho pequeno da tela.
  - **Dificuldade do nó no mapa:** os nós de batalha fácil, média e difícil, e o XP, distinguíveis visualmente no mapa (Mecânica 2).
- **Referências:** ❓ A definir.
- ❓ Arte do mapa de nós (ícones dos tipos de nó e do caminho), do glossário e das telas de fim de run, e se haverá variação visual do grid conforme a profundidade.

### 5.2 Áudio
Ainda não há nenhum áudio no projeto.

- **Trilha sonora:** chiptune/sintetizador atmosférico, coerente com a pixel art e com o tom de fantasia sombria leve. A trilha é dividida por contexto: uma faixa calma para o menu e o mapa, outra para as batalhas e outra para o chefe final. A música muda na transição entre mapa e batalha. ❓ Número de faixas de batalha (para não repetir sempre a mesma) e se a música continua durante a pausa da escolha de talento.
- **SFX (efeitos necessários):**
  - **Combate:** andar, ataque básico, skill (um som por skill ou por tipo), dano recebido, morte de inimigo, morte do jogador.
  - **Progressão:** ganho de XP (orbs de XP), subida de nível e abertura da tela de escolha de talento, escolha de talento, reroll, banimento, pular.
  - **Estados e terreno:** aplicação e fim de estados (por exemplo, Fraquejado), células de perigo e de bônus.
  - **Mapa:** selecionar e confirmar um nó, cura, consumível.
  - **Interface:** toque em botões, abrir e fechar janelas (inclusive a de detalhes da entidade), transição de tela.
  - **Resultado:** vitória e derrota da run, classe liberada.
- **Locução/Voz:** sem voz. Os personagens se expressam por efeitos sonoros curtos, o que evita gravar e traduzir falas para os três idiomas (ver 8.1).
- **Controles de volume:** o jogador pode ajustar o volume da música e dos efeitos nas Opções (ver 4.3). ❓ Se há botão rápido de silenciar e como o áudio se comporta durante os anúncios (ver seção 7).

## 6. Skills

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando. As skills detalhadas abaixo representam a direção inicial e serão reavaliados antes da implementação completa.

### 6.1 Definição de Skill

**Skill** é uma ação ativa do personagem, vinculada à classe ou a traits adquiridos. Toda skill tem:

- **Tipo:** Ofensiva (causa dano), Defensiva (escudo/cura/buff), Utilitária (teleporte, troca etc.)
- **Área de efeito:** Define a forma geométrica do efeito no grid (CIRCLE, CROSS, LINEAR, PERPENDICULAR, ARC, CONE).
- **Tamanho da área:** Ímpar, de 1 a 9.
- **Alcance:** Distância Manhattan máxima para seleção do alvo.
- **Dano:** Valor fixo (se ofensiva).
- **Cooldown:** Número de ações do jogador entre usos.

Skills são **reutilizáveis** — após o cooldown, podem ser usadas novamente. Não são consumidas.

### 6.2 Tabela de Skills por Classe

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

Definição mínima: anúncios e compra de remoção de anúncios.

- **Modelo:** anúncios mais uma compra para removê-los. ❓ Se o modelo é o mesmo no PC e no mobile.
- **Anúncios forçados:** aparecem de tempos em tempos no meio da run, em pausas naturais, como ao sair de uma batalha ou ao sair de um nó sem luta (por exemplo, depois do nó de talento). Nunca durante uma batalha em andamento, o que inclui a escolha de talento feita no meio da batalha ao subir de nível: o anúncio só aparece depois que a batalha termina. ❓ Quais outros momentos de transição.
- **Rampa de frequência:** no início (as primeiras X vezes) os anúncios aparecem com frequência baixa, para dar tempo de o jogador gostar do jogo e não ser atrapalhado. Depois de um tempo, passam à frequência normal, a cada X nós. ❓ Valores de X, duração da fase inicial e se a contagem é por nós, por batalhas vencidas ou por tempo (em avaliação: contar por batalhas e impor um tempo mínimo entre anúncios, porque os nós de cura e de consumível são instantâneos).
- **Anúncio recompensado (opcional):** recarrega as ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3 em 2.3). Só é exibido se o jogador pedir. ❓ Limite de recargas e se ele pode ser pedido durante a pausa da escolha de talento no meio da batalha (é uma ação iniciada pelo jogador, mas ocorre em batalha).
- **Remoção de anúncios:** compra que remove os anúncios forçados e recarrega as ferramentas de oferta de talento sem precisar assistir a anúncio (ver Mecânica 3). ❓ Preço e se qualquer compra também remove os anúncios forçados.
- **Ética de monetização:** evitar pay-to-win. ❓ Definir um teto de recargas igual para quem assiste a anúncios e para quem comprou a remoção, para que o dinheiro não compre poder.
- **Em avaliação, fora da definição mínima:** classes adicionais (além das três, que são liberadas jogando; ver 3.2), cosméticos e expansões de conteúdo.

---
## 8. Plataforma Técnica

### 8.1 Especificações
- **Engine:** Unity
- **Renderização:** URP 2D, com câmera Pixel Perfect (PPU 100) e interface em UI Toolkit.
- **Resolução alvo:** resolução de referência de 216×384 em retrato, escalada para a tela do dispositivo (ver 5.1). ❓ Como a escala se comporta em telas de proporção diferente de 9:16 e no PC (por exemplo, escala inteira com bordas).
- **Orientação:** retrato no mobile. ❓ Janela e proporção no PC (sugestão: janela em retrato ou com a interface centralizada).
- **FPS alvo:** 60 FPS no mobile (já configurado no projeto).
- **Estado da run:** a run é salva automaticamente entre as ações do jogador, tanto no meio da batalha quanto na tela do mapa. Se o app for para segundo plano ou for fechado, o jogador retoma exatamente do ponto em que parou. O estado salvo inclui a posição dos sorteios da *seed* (por exemplo, críticos e ofertas de talento), para que recarregar não mude o resultado das ações. A escolha de talento pendente (a oferta de 3 talentos, quando o jogo está pausado por uma subida de nível) também é salva, com a mesma oferta, para que fechar e reabrir o app não permita sortear de novo. Não há salvamento manual. ❓ Detalhes do formato do salvamento e o que acontece se um anúncio estiver pendente ao fechar.
- **Idiomas:** o jogo suporta vários idiomas. Os idiomas iniciais são inglês, espanhol e português do Brasil. Todo texto exibido ao jogador (interface, nomes e descrições de talentos, skills, estados e inimigos, glossário do jogo e mensagens) deve ser traduzível, e não fixo no código ou nos assets. ❓ Idioma padrão na primeira abertura (sugestão: o idioma do sistema, com inglês como padrão quando não houver tradução), se a troca de idioma vale na hora ou só ao reabrir, e quem faz as traduções.
- **Perfil do jogador:** as classes liberadas, o contador de batalhas vencidas (3.2) e o glossário (2.7) persistem entre runs, em um salvamento de perfil separado do salvamento da run. ❓ Sincronização entre dispositivos.

### 8.2 Requisitos mínimos
- **Android:** Android 8.0 (API 26) ou superior (configuração atual do projeto).
- **iOS:** iOS 15.0 ou superior (configuração atual do projeto).
- ❓ Requisitos mínimos de PC (sistema operacional, memória e placa de vídeo) e a lista de aparelhos de teste.

### 8.3 Plataformas de publicação
- Steam
- App Store
- Google Play
---

## 9. Acessibilidade

Nenhum recurso de acessibilidade está planejado no momento. Esta seção será revista quando o jogo base estiver funcionando.

---

## 10. Métricas de Sucesso

Esta seção cobre apenas as métricas de design, usadas para balancear o jogo. Métricas de negócio (vendas, conversão, receita por usuário) e metas qualitativas (notas nas lojas, feedback da comunidade) não serão definidas neste documento.

- **Dificuldade e progressão:**
  - Em que profundidade o jogador morre (distribuição por classe).
  - Taxa de vitória por classe e por build.
  - Nível do jogador na morte e no chefe final.
- **Escolha de risco e recompensa:**
  - Proporção de batalhas fáceis, médias e difíceis escolhidas no mapa, e a vida do jogador ao entrar em cada tipo.
  - Se quem escolhe só as fáceis chega ao chefe final e vence (para avaliar a faixa de níveis, ver Mecânica 2).
  - Uso dos nós de cura, de talento e de consumível.
- **Talentos e builds:**
  - Quais talentos são escolhidos e quais são ofertados e recusados.
  - Uso de reroll, banir e pular.
  - Combinações de talentos mais frequentes e as que mais vencem.
- **Inimigos:**
  - Quais inimigos mais causam dano e mais matam o jogador.
  - Tempo e turnos por batalha (para identificar batalhas que se arrastam).
- **Liberação de classes:** quantas runs o jogador leva para liberar o Mago e o Ladino (ver 3.2).

❓ Como os dados são coletados e armazenados (ferramenta de análise), o consentimento de privacidade necessário e se os dados são só locais (para teste interno) ou enviados para um servidor.

---

## 11. Cronograma e Escopo

### 11.1 Milestones
Não será definido neste documento. O cronograma não faz parte do GDD.

### 11.2 Escopo (Out of Scope)
[O que NÃO será feito nesta versão. Ajuda a controlar feature creep.]

- Itens ativos e equipáveis — fora do escopo por enquanto; podem voltar em uma versão futura. Os consumíveis (nó de consumível no mapa) entram no escopo.
- Skills detalhadas — serão revistas no futuro, após o jogo base funcionar.
- ...

---

## 12. Riscos

Não será preenchido neste documento.

---

## 13. Glossário

| Termo | Definição                                                                                                                         | Tags   |
| ----- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| Grid  | Tabuleiro de células onde ocorre o combate                                                                                        | #grid  |
| Trait | São mecânicas obtidas a cada nível alcançado pelo jogador, eles concedem estados que alteram características, comportamentos e mecânicas de jogo, e liberam skills. | #trait |
| Talento | Sinônimo de trait na linguagem do jogo (o jogador escolhe talentos ao alcançar um nível). | #trait |
| Skill | Ação ativa do personagem, vinculada à classe ou a traits. Possui área de efeito, dano, alcance e cooldown. Pode ser reutilizada. | #skill |
| Atributo | Valor base que configura o personagem no início da run (vida máxima, dano básico, defesa etc.). | #atributo |
| Estado | Condição com nome que concede efeitos ao personagem (incrementa atributos ou modifica comportamentos do jogo), permanente ou por X turnos conforme a situação ou o talento que o aplica. | #estado |
| Turno | Turno global: o jogador joga primeiro e depois jogam os inimigos; termina depois que a última entidade do grid faz sua ação. Cada entidade tem o seu turno dentro dele. | #turno |
| Nó | Ponto do mapa da run. Pode ser uma batalha, um nó de cura, de talento (com custo), de consumível, entre outros. | #nó |
| Mapa | Estrutura de nós da run, no estilo de Slay the Spire. O jogador escolhe o caminho até o chefe final. | #mapa |
| Batalha | Combate em grid por turnos iniciado ao escolher um nó de batalha. Os inimigos derrotados nela concedem XP, que pode levar a um novo nível e à escolha de um talento. | #batalha |
| Profundidade | Quão longe o jogador está no mapa. Define a escala dos inimigos. | #profundidade |
| Nível | Nível do jogador, obtido com XP de batalhas. Cada nível alcançado concede a escolha de um talento. | #nível |
| XP | Valor de cada inimigo, somado ao jogador na hora em que ele é derrotado. O total possível da batalha é mostrado na prévia do nó. Leva ao próximo nível. | #xp |
