---
tags:
  - design
  - gdd
created: 2026-07-12
updated: 2026-10-20
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

> **Documentos de detalhe** (em `docs/design/`): [[mapa_e_nos]], [[xp_e_niveis]], [[talentos_e_oferta]], [[consumiveis]], [[meta_progressao_e_perfil]], [[estados_e_atributos]], [[inimigos]], [[grid_e_terreno]], [[balanceamento_e_geracao]], [[classes_e_skills]], [[interface]] e [[sistema_combate]] (combate implementado). Perguntas em aberto: [[perguntas_em_aberto]]. Cada regra existe em um só lugar: o GDD guarda as decisões em resumo, e o detalhe fica no subdocumento.

> [!important] Nova direção do combate (decisões de 2026-10-19, implementadas em 2026-10-19 com valores provisórios)
> O jogo passa a misturar o combate de Into the Breach com a run de Slay the Spire, para que **cada ação do jogador importe fortemente** (pilar principal, 1.3). O que muda:
> - **Sem ataque básico:** agir é sempre usar uma skill, e toda skill causa dano e mexe no tabuleiro (Mecânica 1, [[classes_e_skills]]).
> - **Todo golpe de inimigo é anunciado** um turno antes, nas células que ele vai atingir (Mecânica 1, [[inimigos#Golpes anunciados]]).
> - **Números pequenos:** vida do jogador por volta de 10 a 20, inimigos de 2 a 8, dano de 1 a 3 (2.9). Implementado: Guerreiro 18, Ladino 15 e Mago 14 de vida; inimigos comuns de 2 a 5, únicos de 7 a 8 e o chefe com 22; dano de 1 a 3.
> - **A dificuldade vem da complexidade do combate, não de números** (2.9, [[balanceamento_e_geracao]]). Implementado em parte: os inimigos não escalam mais com a profundidade e, desde 2026-10-20, as batalhas são só normal ou de elite (ver 2.9 e Mecânica 2).
> - **Os níveis continuam dando talentos, mas não curam,** e a run tem de 8 a 12 níveis (Mecânica 3, [[xp_e_niveis]]). Implementado: cada nível dá +1 de vida máxima sem curar, com nível máximo 12.
> - **Talentos mudam regras, não números** (3.5, [[talentos_e_oferta]]).
> - **Nós sem luta com escolha:** acampamento (curar ou melhorar uma skill), talento com custo que dura e consumível escolhido entre 3 (Mecânica 2, [[mapa_e_nos]]).
>
> O combate implementado está em [[sistema_combate]]. Ainda não implementado: os consumíveis que mexem no tabuleiro (2.8), o talento Ímã (3.5) e a composição própria da elite (papéis combinados e terreno de perigo; 2.9 e Mecânica 2). A discussão que levou a essas decisões partiu da lista de problemas do Luiz: ações sem impacto, talentos que só aumentam números, nós sem escolha e dificuldades que não fazem diferença.

> [!note] Ajustes de 2026-10-20 (pedidos do Luiz, implementados)
> - **Magias canalizadas do Mago:** o Meteoro e a Tempestade Elétrica (e as variantes) gastam também o andar do turno, então só podem ser usados antes de andar; o Meteoro ainda deixa o Mago **Exausto** (não anda no turno seguinte). Muito dano em área, mas lançado de um lugar seguro (Mecânica 7, [[classes_e_skills]]).
> - **Golpe anunciado a cada turno:** todo inimigo tem um golpe básico sem recarga e um especial com recarga, para a marca não sumir no turno depois do disparo (Mecânica 1, [[inimigos]]).
> - **XP em explosão:** as bolinhas de XP de um abate saem juntas numa mini explosão e voam juntas para a barra, em menos de 1 s, qualquer que seja o XP (Mecânica 3, [[xp_e_niveis]]).
> - **Batalha normal ou de elite:** não há mais fácil, normal e difícil. A batalha de elite tem um inimigo único e dá mais XP, um consumível e uma melhoria de skill; o mapa não mostra o XP, só o ícone de elite (Mecânica 2, [[mapa_e_nos]]).

---

## 1. Visão Geral

### 1.1 Logline

Um RPG/Survivor em turnos onde cada ação do jogador importa para que ele consiga sobreviver.

### 1.2 Sinopse

O jogador poderá selecionar dentre X classes para controlar dentro de um sistema de #grid. A #classe determina as variações de quais ações o jogador poderá realizar e seus #trait's. A run é um mapa de nós: o jogador escolhe o caminho, enfrenta batalhas no grid e ganha XP ao vencê-las; a cada nível alcançado, escolhe um trait entre as ofertas da classe. Os #trait serão responsáveis por modificar características, modificar comportamentos do jogo, liberar #skill de uso ativo.

### 1.3 Pillars de Design

1. **Cada ação do jogador importa fortemente** (pilar principal, decisão de 2026-10-19). Toda ação muda a batalha: causa dano e mexe no tabuleiro (empurra, puxa, troca de lugar, cria ou tira terreno, aplica um estado). Os inimigos anunciam o que vão fazer, e o turno do jogador é desarmar esses golpes. Os números são pequenos para que cada ponto de dano conte. Quando duas regras disputam, vale a que dá mais peso à ação do jogador.
2. Cada partida deve passar o sentimento de ser única 
3. O rng no jogo deve ser um fator, porém um fator controlável pelo jogador

**Princípio de dificuldade (decisão de 2026-10-19):** a dificuldade vem da **complexidade do combate** (golpes mais difíceis de desarmar, mais papéis juntos, terreno mais traiçoeiro, inimigos únicos), e não de números maiores. Ver 2.9.

### 1.4 Unique Selling Points (USPs)

- **Combate de quebra-cabeça em grid, onde cada ação muda o tabuleiro.** Todo inimigo anuncia o golpe antes, marcando as células que vai atingir. Em cada turno o jogador anda, usa uma skill e pode usar um consumível para desarmar esses golpes: sair da área, empurrar o atacante, colocar outro inimigo na frente. Não há ataque básico: toda skill causa dano e mexe no tabuleiro (decisões de 2026-10-19).
- **Build montada durante a batalha.** O talento é escolhido no momento em que a barra de XP cruza o limiar de nível, com o jogo pausado, inclusive no meio da luta (Mecânica 3). O sorteio é controlável pelo jogador: reroll, banir e pular.
- **Risco e recompensa no mapa.** As batalhas de elite são mais complexas (com um inimigo único) e rendem mais XP, um consumível e uma melhoria de skill. A vida persiste entre as batalhas e não volta ao subir de nível, então o jogador decide quanto arriscar para ficar mais forte (Mecânica 2).
- **Run curta e de uma mão.** Run finita, em retrato, pensada para sessões curtas no celular, com salvamento automático a cada turno (ver 8.1).

---

## 2. Jogabilidade

### 2.1 Core Loop

Run finita em um mapa de nós, no estilo de Slay the Spire: o jogador escolhe o caminho, enfrenta batalhas no grid e termina a run derrotando o chefe final ou morrendo. O foco é a construção de build ao longo da run.

```
Iniciar a run (classe + seed) → mapa de nós
  → Escolher o próximo nó
      • Batalha → combate em grid por turnos (andar / usar skill / consumível, contra golpes anunciados)
          → cada inimigo derrotado soma o seu XP na hora (o mapa não mostra o XP; a batalha de elite dá mais)
          → batalha de elite vencida → consumível (1 de 3) e melhoria de skill
          → se o XP alcançar o próximo nível → subir de nível na hora (inclusive no meio da batalha) → o jogo oferece 3 talentos da classe
          → escolher 1 (ou usar reroll / banir / pular)
          → a build evolui e muda a forma de jogar com a classe
      • Nó sem luta (acampamento, talento com custo, consumível…) → escolha do nó
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
- **Métrica de sucesso:** vitória, nível do jogador e profundidade alcançados, registrados por classe (detalhes em [[meta_progressao_e_perfil]]).

### 2.3 Mecânicas Principais

#### Mecânica 1: Combate em Grid por Turnos
> Decisões de 2026-10-19, implementadas: sem ataque básico (`hasBasicAttack` desligado em todos os personagens), todo golpe de inimigo anunciado, números pequenos, ordem fixa dos inimigos e previsão do resultado antes de confirmar a ação. Regras em [[sistema_combate]].

- **Descrição:** O combate acontece num grid de células, com no máximo uma entidade por célula. **Andar e agir no mesmo turno** (decisão de 2026-10-18): em cada turno o jogador anda uma vez e age uma vez (usa uma skill), na ordem que quiser, e depois todos os inimigos fazem o mesmo. O turno só acaba sozinho quando o jogador não consegue fazer mais nada (andar, agir e usar consumível já usados ou impossíveis); senão, acaba no botão "Encerrar turno" (sempre visível na vez dele) ou tocando no próprio personagem. Enquanto não agiu, o jogador pode desfazer o movimento. Ações inválidas não gastam nada. Usar um consumível não gasta o andar nem o agir (no máximo um por turno; ver 2.8). Os inimigos agem um de cada vez, cada um terminando o seu turno antes do próximo. Jogador e inimigos seguem as mesmas regras de alcance e ocupação. **O andar segue um caminho** (decisão de 2026-10-20): o personagem vai célula a célula, só por casas livres em linha reta (sem diagonal), e outros personagens bloqueiam a passagem como os obstáculos; ninguém passa por cima ou pelo lado de um inimigo para chegar atrás dele. Cada célula cruzada é registrada, para efeitos futuros "ao passar por uma célula".
- **Sem ataque básico (decisão de 2026-10-19):** agir é sempre usar uma skill. Cada classe começa com 2 ou 3 skills, e toda skill faz duas coisas: causa dano **e** muda o tabuleiro (empurra, puxa, troca de lugar, cria ou tira terreno, aplica um estado). Pelo menos uma skill inicial não tem recarga, para o jogador sempre ter o que fazer. Detalhes: [[classes_e_skills#Nova direção das skills (2026-10-19)]].
- **Golpes anunciados (decisão de 2026-10-19, substitui a telegrafia opcional de 2026-10-18):** todo golpe de inimigo é anunciado, em todos os inimigos. No turno dele, o inimigo primeiro dispara o golpe que preparou no turno anterior (contra quem estiver nas células marcadas, inclusive outros inimigos), depois anda e prepara o próximo: as células que o novo golpe vai atingir ficam marcadas no grid. O turno do jogador vira um quebra-cabeça: sair da área, empurrar o atacante ou outro inimigo para dentro dela, ficar atrás de um obstáculo ou matar e atordoar quem prepara. Detalhes: [[inimigos#Golpes anunciados]]. Para a marca aparecer em todo turno, todo inimigo tem um golpe **básico sem recarga** e um **especial com recarga**, e prepara o especial quando ele está pronto (decisão de 2026-10-20). Ainda fica sem marca o turno em que nenhum golpe alcança o jogador, o turno em que um inimigo de apoio cura, protege, invoca ou enfraquece, e o primeiro turno da batalha.
- **Números pequenos (decisão de 2026-10-19):** vida do jogador por volta de 10 a 20, inimigos de 2 a 8, skills de 1 a 3 de dano, colisão 1. Com números pequenos, um empurrão contra uma rocha muda quem morre no turno, e cada golpe recebido pesa. Valores iniciais em [[balanceamento_e_geracao#Números pequenos (2026-10-19)]].
- **Ordem e previsão (implementadas em 2026-10-19):** os inimigos agem numa ordem fixa durante a batalha, mostrada por um número no canto da célula de cada um. Ao mirar uma skill ou um item, o grid mostra o resultado antes de confirmar: dano, mortes, empurrões, onde cada um termina e onde os golpes anunciados vão cair depois da ação. Detalhes em [[sistema_combate#Previsão do resultado]] e [[sistema_combate#Ordem dos inimigos]].
- **Entrada do jogador:** Toque em uma célula para andar; toque no botão de uma skill e depois na célula alvo para mirar (a previsão aparece), e de novo na mesma célula ou em "Confirmar" para agir ("Cancelar" desfaz a mira); toque no próprio personagem ou "Encerrar turno" para terminar a vez, "Desfazer" para voltar o movimento.
- **Feedback visual/sonoro:** Células destacadas para andar e para mirar a skill (refeitas depois de andar), um pé, uma espada e uma poção que mostram o que ainda resta no turno (andar, agir e o consumível), animações de movimento e de skill, texto de dano flutuante. As células de cada golpe anunciado ficam marcadas o tempo todo com listras de aviso (vermelhas onde alguém seria atingido), e cada inimigo mostra o número da sua ordem no canto da célula. Tocar num inimigo destaca só as células para onde ele pode andar, esmaece os golpes dos outros e mostra o ícone do golpe dele (leitura refeita em 2026-10-20 para limpar o tabuleiro).
- **Interação com outras mecânicas:** Cada inimigo derrotado concede o seu XP na hora, que leva ao nível e à escolha de talentos. Os talentos mudam as regras das skills e do tabuleiro (Mecânica 3).

#### Mecânica 2: Mapa, Nós e Batalhas
- **Descrição:** a run é um mapa de nós, no estilo de Slay the Spire. O jogador escolhe o próximo nó, e um nó de batalha leva ao combate no grid. A run é finita (exemplo, não final: cerca de 30 nós) e termina na vitória, ao derrotar o chefe final, ou na morte.
- **Profundidade e nível:** a profundidade (posição no mapa) define a complexidade das batalhas: quais inimigos, golpes, papéis e terrenos aparecem (decisão de 2026-10-19; antes ela escalava a vida e o dano dos inimigos). O nível é do jogador e vem do XP, então varia conforme o caminho escolhido.
- **Risco e recompensa:** cada inimigo derrotado soma o seu XP na hora. Desde 2026-10-20 só há dois tipos de batalha: **normal** e **de elite**. A batalha de elite é mais complexa (um inimigo único, não inimigos com mais vida) e dá mais XP (cerca de 2,7 vezes a normal no mesmo andar), um consumível (escolher 1 de 3) e uma melhoria de skill. Como subir de nível não cura mais, o XP extra custa vida de verdade (decisão de 2026-10-19). Implementado: um nó só vira elite onde há um inimigo único disponível na profundidade (o primeiro aparece no andar 4, então os andares 1 a 3 só têm batalhas normais); fora disso, 75% das batalhas são normais e 25% de elite. O nível tem um teto fixo, e quem foge das elites termina a run com menos níveis.
- **Tipos de nó:** batalha (normal ou de elite), acampamento (o antigo nó de cura: curar **ou** melhorar uma skill), talento (com um custo que dura) e consumível (escolher 1 de 3 itens). Implementados com os valores provisórios: o acampamento cura 40% da vida máxima ou dá uma melhoria, o nó de talento custa 2 de vida máxima pelo resto da run e o nó de consumível oferece 3 itens. Outros tipos (evento, chefe de meio de run) em aberto. Detalhes: [[mapa_e_nos#Nós e dificuldade (2026-10-19)]].
- **Vida:** persiste entre as batalhas e só é recuperada no acampamento, em consumíveis ou por efeitos da build. Subir de nível não cura (decisão de 2026-10-19).
- **Fim da batalha:** quando todos os inimigos são eliminados, sem limite de turnos. Sem objetivos extras por enquanto (decisão de 2026-10-19; ver seção 10).
- **Prévia do nó:** antes de escolher, o jogador vê o tipo do nó e, nas batalhas, os papéis dos inimigos, o terreno e, na elite, a recompensa em palavras ("Recompensa: mais XP, um consumível e uma melhoria de skill"). O mapa não mostra a quantidade de XP (decisão de 2026-10-20): só o ícone de elite marca a batalha mais difícil.
- **Geração:** as batalhas e o mapa são gerados por algoritmo a partir da seed, de forma reproduzível.
- **Detalhes:** [[mapa_e_nos]] (mapa e nós), [[xp_e_niveis]] (XP, nível e curva) e [[balanceamento_e_geracao]] (geração das batalhas).

#### Mecânica 3: XP, Nível e Escolha de Talentos (build adaptativa)
- **Descrição:** a cada nível alcançado, no momento em que a barra de XP cruza o limiar, o jogo é pausado e oferece 3 talentos sorteados entre os que a classe pode pegar. O jogador escolhe 1. O sorteio é ponderado por sinergia com a build.
- **Controle do RNG (pilar 3):** o jogador pode rerrolar a oferta, banir um talento e pular a oferta. Quantidades e custos em aberto. Valores iniciais configuráveis: 2 rerolls, 1 banimento e 1 pulo por run, sem custo além desse limite (ver [[valores_padrao_em_aberto]]).
- **Vida (decisão de 2026-10-19, substitui a cura total por nível de 2026-10-06):** subir de nível não cura. A vida persiste entre as batalhas e só volta no acampamento, em consumíveis e em talentos de cura. Com a cura por nível, a vida quase nunca pesava, o nó de cura e o custo do nó de talento sumiam no nível seguinte e a batalha difícil (mais XP, mais curas) era sempre a melhor escolha. Implementado: cada nível dá +1 de vida máxima, sem curar (valor padrão; `ProgressionSettings.maxHpPerLevel`).
- **Menos níveis, talentos maiores (decisão de 2026-10-19):** de 8 a 12 níveis por run, para cada talento valer mais. Implementado: curva de 150 de XP para o nível 2 e +100 a cada nível, nível máximo 12; com as três dificuldades antigas, o simulador de XP dava nível final médio de 9,5, 9,9 e 10,6 nas rotas fácil, normal e difícil (a medida com as rotas normal e elite de 2026-10-20 ainda falta). O talento continua chegando na hora em que a barra cruza o limiar, inclusive no meio do turno; com golpes anunciados, ele pode abrir uma saída que não existia.
- **Talentos mudam regras, não números (decisão de 2026-10-19):** ver 3.5.
- **Detalhes:** [[talentos_e_oferta]] (oferta, reroll, banir e pular) e [[xp_e_niveis]] (XP e nível).

#### Mecânica 4: Papéis dos Inimigos
- **Descrição:** cada inimigo tem um papel (corpo a corpo, enxame, atirador, suporte, invocador, controlador) que define como ele se comporta, em vez de todos perseguirem e atacarem. Seguem as mesmas regras de turno, alcance e ocupação do jogador; todo golpe é anunciado um turno antes (Mecânica 1, decisão de 2026-10-19), e a prévia do nó mostra os papéis. Detalhes: [[inimigos]].

#### Mecânica 5: Personalidade do Grid
- **Descrição:** o grid de cada batalha pode ter terreno (obstáculos, células de perigo e de bônus) que muda o posicionamento e o valor das skills. O terreno é gerado pela seed e pela profundidade e aparece na prévia do nó. Detalhes: [[grid_e_terreno]].

#### Mecânica 6: Inimigos que Cobram a Build
- **Descrição:** alguns inimigos exigem uma resposta específica da build (blindado, regenerante, enxame, rápido, espinhoso). Isso incentiva diversificar os talentos e faz o conhecimento do jogo, com o glossário, virar vantagem estratégica. Detalhes: [[inimigos]].

### 2.4 Mecânicas Secundárias

> **Será revisto no futuro.** A ideia inicial é deixar o jogo base funcionando antes de implementar mais coisas acima desta camada. O que segue abaixo é a direção atual, sujeita a revisão.

#### Mecânica 7: Skills Ativas
- **Descrição:** as skills são a única forma de agir (sem ataque básico, decisão de 2026-10-19). Cada classe começa com uma skill só, a sem recarga (decisão de 2026-10-20; antes eram 2 ou 3), e libera as outras por talentos durante a run, até 6 (mantido em 2026-10-19). Algumas skills fortes têm um contra (decisão de 2026-10-20): as **canalizadas** gastam o agir e também o andar do turno, só podem ser usadas antes de andar e o personagem não anda depois; o Meteoro do Mago ainda deixa o Mago Exausto (sem andar) no turno seguinte. Implementado: 1 skill inicial por classe, sem recarga (decisão de 2026-10-20), e até 5 liberadas por talento (`TalentOfferSettings.maxSkillTalents`); cada skill tem 2 variantes de melhoria e melhora uma vez. Toda skill causa dano e mexe no tabuleiro, tem cooldown e área de efeito e é reutilizável. As melhorias de skill (elite e acampamento) dão uma variante nova, escolhida entre duas, e não só um número maior. Uso: toque no botão da skill na barra inferior e depois na célula alvo. Detalhes: [[classes_e_skills]].

### 2.5 Progressão

> **Será revisto no futuro.** Os detalhes finais (valores, parâmetros e formato das skills) serão revisados quando o jogo base estiver funcionando.

- **Sistema de progressão:** XP por inimigo derrotado e nível do jogador (a profundidade no mapa é um eixo separado), árvore de talentos e skills por classe. A curva de dificuldade sobe com a profundidade até o chefe final.
- **Detalhes:** [[xp_e_niveis]] (XP e nível), [[talentos_e_oferta]] (talentos) e [[classes_e_skills]] (skills).

### 2.6 Economia
Não há moeda por enquanto (decisão de 2026-10-18: a loja com moedas foi adiada; ideia guardada: os nós de cura, consumível e talento virariam lojas, cada uma do seu tipo, pagas com moedas que só viriam das batalhas). Os recursos da run são: vida (persiste entre os nós, não volta ao subir de nível e se recupera no acampamento), XP (por inimigo derrotado, leva a níveis e talentos), consumíveis (nó de consumível, ver 2.8) e as ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3). O custo do nó de talento é um custo que dura (direção de 2026-10-19, porque o custo em vida só funcionava enquanto o nível curava; opções em [[mapa_e_nos#Nós e dificuldade (2026-10-19)]]). Valor implementado: −2 de vida máxima pelo resto da run (`RunSettings.talentNodeCostMaxHp`; ver [[valores_padrao_em_aberto]]). Itens ativos e equipáveis estão fora do escopo (ver seção 10).

### 2.7 Glossário do Jogo

> Não confundir com o glossário de termos da seção 11 deste documento. Este é um recurso dentro do jogo.

- **Descrição:** o jogo tem um glossário que o jogador preenche conforme joga, com as classes liberadas (os talentos já escolhidos aparecem, os demais como "?") e os inimigos já enfrentados (atributos, skills e como agem). Persiste entre runs, no perfil do jogador (8.1). Detalhes: [[meta_progressao_e_perfil]].

### 2.8 Consumíveis
- **Descrição:** item de uso único obtido no nó de consumível. Só pode ser usado em batalha, não gasta o andar nem o agir do turno e só um pode ser usado por turno, o que o torna muito valioso. Direção de 2026-10-19: o nó oferece 3 itens e o jogador escolhe 1, e os itens novos mexem no tabuleiro (empurrar, trocar de lugar, criar terreno). Implementado: a escolha de 1 entre 3 e os números pequenos dos itens atuais (Poção de Cura 4, Bomba 2). Desde 2026-10-20 há 5 itens de tabuleiro (Granada de Impacto, Pergaminho de Troca, Pedra, Frasco de Fogo e Frasco Congelante), e todo item arremessado precisa de linha de visão, como as skills. Detalhes: [[consumiveis]].

### 2.9 Balanceamento e Fórmulas
- **Princípio (decisão de 2026-10-19, substitui o anterior):** a dificuldade vem da **complexidade do combate**, não de números maiores. Avançar no mapa e escolher batalhas mais difíceis traz golpes mais difíceis de desarmar (áreas maiores, golpes em linha que atravessam o grid, golpes que empurram), mais papéis juntos, mais inimigos com golpes ao mesmo tempo, terreno mais traiçoeiro e inimigos únicos. A vida e o dano dos inimigos comuns **não crescem** com a profundidade (implementado: `hpPerDepth` e `damagePerDepth` em 0). O poder do jogador vem do nível (talentos que mudam regras) e das melhorias de skill. Ainda não implementado: a composição própria da elite (papéis combinados e terreno de perigo; hoje ela é um inimigo único com o orçamento ×1,35) e o orçamento calculado pela complexidade de cada golpe (hoje a ameaça de cada inimigo é escrita à mão em `EnemyConfig.threat`). A geração é determinística por seed, então o balanceamento pode ser simulado antes de implementar. Detalhes: [[balanceamento_e_geracao#Dificuldade por complexidade (2026-10-19)]].
- **Números pequenos (decisão de 2026-10-19, implementada):** vida do jogador por volta de 10 a 20, inimigos de 2 a 8, dano de 1 a 3, colisão e terreno 1. Valores atuais em [[balanceamento_e_geracao#Números pequenos (2026-10-19)]].
- **Estado implementado (2026-10-19, valores provisórios; o balanceamento continua pausado):** a curva de XP é global e configurável (`ProgressionSettings`): 150 de XP para o nível 2 e +100 a cada nível, nível máximo 12 e sobra de XP guardada. O XP de cada batalha vem de um orçamento de XP da profundidade e do tipo de batalha: (6 + 0,52 × (profundidade − 1)) × multiplicador de orçamento × 6,9 × (1 + 0,07 × (profundidade − 1)) × multiplicador de XP, dividido entre os inimigos pela ameaça de cada um. Normal: orçamento ×1 e XP ×1; elite: orçamento ×1,35 e XP ×2, então a batalha de elite sempre dá mais XP que a normal no mesmo andar (cerca de 2,7 vezes). Detalhes: [[balanceamento_e_geracao]], [[xp_e_niveis]] e [[valores_padrao_em_aberto]].

## 3. Personagens e Mundo

### 3.1 Atributos e Estados

Duas camadas descrevem e alteram um personagem:

- **Atributos:** valores base (vida máxima, alcance de movimento etc.), definidos por classe ou inimigo. O alcance de ataque e o dano básico saem do jogador junto com o ataque básico (decisão de 2026-10-19; implementado desligando o ataque básico em todos os personagens: os dois atributos continuam no código, sem uso). O valor em jogo é o base somado aos estados ativos.
- **Estados:** têm um nome e concedem efeitos (alteram atributos ou comportamentos), permanentes ou por X turnos. Talentos, skills e inimigos aplicam estados.
- **Duração:** conta em turnos da entidade que está com o estado, e não de quem o aplicou.
- **Detalhes:** tabela de atributos, catálogo de estados, regra de turno global e exemplos em [[estados_e_atributos]].

### 3.2 Personagem do Jogador

Três classes, implementadas com diferença de vida máxima e de skills (valores atuais, provisórios, na escala de números pequenos de 2026-10-19; ver [[classes_e_skills]]):

| Classe | Vida máxima | Liberação |
|---|---|---|
| Guerreiro (Knight) | 18 | disponível desde o início |
| Mago (Mage) | 14 | ao vencer 15 batalhas |
| Ladino (Rogue) | 15 | ao vencer 30 batalhas |

- **Liberação:** só contam batalhas vencidas, acumuladas entre runs e guardadas no perfil do jogador (8.1). Regras em [[meta_progressao_e_perfil]].
- **Skills:** o personagem carrega até 6 skills, uma por botão da barra de skills (ver [[classes_e_skills]]). Sem ataque básico (2026-10-19), cada classe começa com uma skill sem recarga que causa dano e mexe no tabuleiro (decisão de 2026-10-20): Golpe (Guerreiro), Rajada de Vento (Mago) e Adaga Envenenada (Ladino). Até 5 outras vêm de talentos, inclusive as que eram iniciais (Investida com Escudo, Golpe de Escudo, Nova de Gelo, Bola de Fogo, Gancho e Passo das Sombras).
- **Detalhes:** valores, curva de XP implementada e perguntas em aberto em [[classes_e_skills]].

### 3.3 NPCs / Inimigos

27 inimigos implementados, todos provisórios: 22 comuns na geração das batalhas (dos seis papéis, incluindo os que cobram a build: blindado, regenerante e espinhoso), 3 inimigos únicos que são as elites das batalhas de elite (Ogro brutamontes, Cavaleiro esqueleto e Golem de lava), o chefe final (Rei Goblin) e a escolta dele (Guarda Goblin). Cada um tem vida, movimento, um papel, um comportamento configurável e skills exclusivas: um golpe básico sem recarga e um especial com recarga (o chefe tem dois especiais), e todos os golpes são anunciados (decisões de 2026-10-19 e 2026-10-20, ver Mecânica 1). Tabela e detalhes: [[inimigos]].

### 3.4 Mundo / Cenário
- **Ambientação:** masmorra subterrânea. A run é uma descida por salas e corredores (os nós do mapa) até o chefe final, onde as batalhas acontecem em salas de grid. Os inimigos são criaturas da masmorra (goblins, ratos, slimes, crânio de fogo, morcegos, aranha, golems, esqueleto e as próximas).
- **Estética:** fantasia sombria leve, em pixel art (ver 5.1). O terreno das batalhas (obstáculos, células de perigo e de bônus, ver Mecânica 5) é o que dá personalidade a cada sala.
- **Estrutura de fases/mundo:** mapa de nós ramificado e gerado por seed, com batalhas no grid (ver Mecânica 2 em 2.3). Um único tema visual do início ao fim: a dificuldade e os inimigos mudam com a profundidade, mas não há regiões visualmente diferentes. ❓ Se isso muda no futuro (por exemplo, variação de cor do grid por trecho do mapa, ver 5.1).
- **Narrativa:** sem história explícita. O mundo é sugerido pela arte e pelos nomes, sem diálogos nem textos de história, o que também evita traduzir narrativa para os três idiomas (ver 8.1).

### 3.5 Talentos

- **Definição:** talentos (traits) são o que o jogador escolhe ao alcançar um nível. Concedem estados e podem liberar skills.
- **Pool:** cada classe tem uma pool maior que o número de escolhas da run (exemplo: cerca de 50 talentos). Implementado (provisório): 6 a 8 talentos por classe mais 6 compartilhados. Em uma run, só 5 talentos que liberam skills podem ser escolhidos (a skill inicial ocupa o outro espaço).
- **Oferta:** 3 talentos sorteados, com peso maior para os que combinam com a build.
- **Regras, não números (decisão de 2026-10-19):** os talentos que só aumentam números (+vida, +dano, +crítico, +defesa) saem da oferta ou viram raros. Os talentos mudam regras do combate e do tabuleiro, como as relíquias do Slay the Spire (por exemplo: o inimigo lançado contra outro empurra esse outro; quem você empurra deixa fogo no caminho). Aumentar números passa a ser papel das melhorias de skill. Com 8 a 12 níveis por run, cada escolha precisa mudar o plano. Implementado: os talentos de número das classes saíram e entraram talentos de regra (Dominó, Rastro de Chamas, Gelo Escorregadio, Passo Trocado, Contragolpe, Ímpeto); o Ímã não foi implementado, e dos genéricos de número ficou a Vitalidade (+2 de vida máxima por posto). Detalhes em [[talentos_e_oferta]].
- **Detalhes:** estrutura sugerida (ramos), pré-requisitos e perguntas em aberto em [[talentos_e_oferta]].

## 4. Controles e Interface

Interface em UI Toolkit, em retrato no mobile (ver 8.1). Os controles são os mesmos em mobile e PC (toque ou clique do mouse), sem controle de teclado.

- **Controles:** toque ou clique em uma célula para andar; toque no botão de skill e depois na célula alvo para mirar, com a previsão do resultado, e de novo na célula ou em "Confirmar" para usar a skill (sem ataque básico, decisão de 2026-10-19); toque no botão de item para usar um consumível; toque longo (mobile) ou clique direito (PC) em uma entidade abre a janela de detalhes. As entradas só são aceitas na vez do jogador.
- **HUD em batalha, de cima para baixo:** barra de XP com o botão de menu/pausa à direita; o grid; a barra de itens (botões circulares de 32×32); a barra de skills (6 botões quadrados de 32×32).
- **Telas:** inicial, seleção de classe, mapa, batalha, escolha de talento (com o jogo pausado), glossário, pausa, opções, fim de run, vitória e remoção de anúncios.
- **Tutorial:** dicas contextuais na primeira run, mostradas uma vez (sugestão).
- **Detalhes:** [[interface]].

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
  - **Batalha de elite no mapa:** o nó de elite tem ícone próprio, distinguível do nó de batalha normal; o mapa não mostra o XP (Mecânica 2, decisão de 2026-10-20).
- **Referências:** ❓ A definir.
- ❓ Arte do mapa de nós (ícones dos tipos de nó e do caminho), do glossário e das telas de fim de run, e se haverá variação visual do grid conforme a profundidade.

### 5.2 Áudio
Há a infraestrutura de áudio (efeitos por evento, música por contexto e volumes nas Opções), mas ainda nenhum clipe de áudio no projeto.

- **Trilha sonora:** chiptune/sintetizador atmosférico, coerente com a pixel art e com o tom de fantasia sombria leve. A trilha é dividida por contexto: uma faixa calma para o menu e o mapa, outra para as batalhas e outra para o chefe final. A música muda na transição entre mapa e batalha. ❓ Número de faixas de batalha (para não repetir sempre a mesma) e se a música continua durante a pausa da escolha de talento. Valores iniciais configuráveis: qualquer número de faixas de batalha, uma sorteada sem repetir em seguida, e a música continua durante a pausa (ver [[valores_padrao_em_aberto]]).
- **SFX (efeitos necessários):**
  - **Combate:** andar, skill (um som por skill ou por tipo), golpe anunciado (preparar e disparar), colisão de empurrão, dano recebido, morte de inimigo, morte do jogador.
  - **Progressão:** ganho de XP (orbs de XP), subida de nível e abertura da tela de escolha de talento, escolha de talento, reroll, banimento, pular.
  - **Estados e terreno:** aplicação e fim de estados (por exemplo, Fraquejado), células de perigo e de bônus.
  - **Mapa:** selecionar e confirmar um nó, cura, consumível.
  - **Interface:** toque em botões, abrir e fechar janelas (inclusive a de detalhes da entidade), transição de tela.
  - **Resultado:** vitória e derrota da run, classe liberada.
- **Locução/Voz:** sem voz. Os personagens se expressam por efeitos sonoros curtos, o que evita gravar e traduzir falas para os três idiomas (ver 8.1).
- **Controles de volume:** o jogador pode ajustar o volume da música e dos efeitos nas Opções (ver 4.3). ❓ Se há botão rápido de silenciar e como o áudio se comporta durante os anúncios (ver seção 7).

## 6. Skills

> **Será revisto no futuro.** A prioridade atual é o jogo base funcionando.

Uma skill de classe é uma ação ativa; com a decisão de 2026-10-19 (sem ataque básico), é a única forma de agir. Cada classe começa com 2 ou 3 e libera as outras por talentos. Os inimigos têm skills exclusivas de cada tipo, todas anunciadas (ver [[inimigos]]). Uma skill é reutilizável (cooldown) e tem tipo, área de efeito, tamanho, alcance, dano fixo e um efeito no tabuleiro. O personagem carrega no máximo 6. Definição completa e tabelas por classe (implementadas em 2026-10-19, valores provisórios): [[classes_e_skills]].

## 7. Monetização (se aplicável)

Definição mínima: anúncios e compra de remoção de anúncios.

- **Modelo:** anúncios mais uma compra para removê-los. ❓ Se o modelo é o mesmo no PC e no mobile.
- **Anúncios forçados:** aparecem de tempos em tempos no meio da run, em pausas naturais, como ao sair de uma batalha ou ao sair de um nó sem luta (por exemplo, depois do nó de talento). Nunca durante uma batalha em andamento, o que inclui a escolha de talento feita no meio da batalha ao subir de nível: o anúncio só aparece depois que a batalha termina. ❓ Quais outros momentos de transição.
- **Rampa de frequência:** no início (as primeiras X vezes) os anúncios aparecem com frequência baixa, para dar tempo de o jogador gostar do jogo e não ser atrapalhado. Depois de um tempo, passam à frequência normal, a cada X nós. ❓ Valores de X, duração da fase inicial e se a contagem é por nós, por batalhas vencidas ou por tempo (em avaliação: contar por batalhas e impor um tempo mínimo entre anúncios, porque os nós de cura e de consumível são instantâneos).
- **Anúncio recompensado:** cada uso das ferramentas de oferta de talento (reroll, banimento e pular; ver Mecânica 3 em 2.3) mostra antes um anúncio recompensado, e o uso só acontece (e conta no limite da run) se o anúncio for assistido até o fim. Vale também na escolha de talento no meio da batalha, porque o uso é iniciado pelo jogador. Decisão de 2026-10-12, no lugar da recarga das ferramentas.
- **Remoção de anúncios:** compra que remove os anúncios forçados e os anúncios das ferramentas de oferta de talento (ver Mecânica 3). ❓ Preço e se qualquer compra também remove os anúncios forçados.
- **Ética de monetização:** evitar pay-to-win. O limite de usos das ferramentas é o mesmo para quem assiste a anúncios e para quem comprou a remoção: o dinheiro só tira os anúncios.
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
- **Idiomas:** o jogo suporta vários idiomas. Os idiomas iniciais são inglês, espanhol e português do Brasil. Todo texto exibido ao jogador (interface, nomes e descrições de talentos, skills, estados e inimigos, glossário do jogo e mensagens) deve ser traduzível, e não fixo no código ou nos assets. ❓ Idioma padrão na primeira abertura (sugestão: o idioma do sistema, com inglês como padrão quando não houver tradução), se a troca de idioma vale na hora ou só ao reabrir, e quem faz as traduções. Valores iniciais: idioma do sistema na primeira abertura, com inglês como padrão, e a troca vale na hora (ver [[valores_padrao_em_aberto]]).
- **Perfil do jogador:** as classes liberadas, o contador de batalhas vencidas e o glossário persistem entre runs, em um salvamento de perfil separado do da run (detalhes em [[meta_progressao_e_perfil]]).

### 8.2 Requisitos mínimos
- **Android:** Android 8.0 (API 26) ou superior (configuração atual do projeto).
- **iOS:** iOS 15.0 ou superior (configuração atual do projeto).
- ❓ Requisitos mínimos de PC (sistema operacional, memória e placa de vídeo) e a lista de aparelhos de teste.

### 8.3 Plataformas de publicação
- Steam
- App Store
- Google Play
---

## 9. Métricas de Sucesso

Esta seção cobre apenas as métricas de design, usadas para balancear o jogo. Métricas de negócio (vendas, conversão, receita por usuário) e metas qualitativas (notas nas lojas, feedback da comunidade) não serão definidas neste documento.

- **Dificuldade e progressão:**
  - Em que profundidade o jogador morre (distribuição por classe).
  - Taxa de vitória por classe e por build.
  - Nível do jogador na morte e no chefe final.
- **Escolha de risco e recompensa:**
  - Proporção de batalhas fáceis, médias e difíceis escolhidas no mapa, e a vida do jogador ao entrar em cada tipo.
  - Se quem escolhe só as fáceis chega ao chefe final e vence (para avaliar a faixa de níveis, ver Mecânica 2).
  - Uso dos acampamentos (curar ou melhorar), dos nós de talento e de consumível.
- **Talentos e builds:**
  - Quais talentos são escolhidos e quais são ofertados e recusados.
  - Uso de reroll, banir e pular.
  - Combinações de talentos mais frequentes e as que mais vencem.
- **Inimigos:**
  - Quais inimigos mais causam dano e mais matam o jogador.
  - Tempo e turnos por batalha (para identificar batalhas que se arrastam).
- **Liberação de classes:** quantas runs o jogador leva para liberar o Mago e o Ladino (ver 3.2).

❓ Como os dados são coletados e armazenados (ferramenta de análise), o consentimento de privacidade necessário e se os dados são só locais (para teste interno) ou enviados para um servidor. Valor inicial: só locais, gravados em `metrics.jsonl` e ativados (ver [[valores_padrao_em_aberto]]).

---

## 10. Fora do Escopo

- Itens ativos e equipáveis — fora do escopo por enquanto; podem voltar em uma versão futura. Os consumíveis (nó de consumível no mapa) entram no escopo.
- Skills detalhadas — serão revistas no futuro, após o jogo base funcionar.
- Companheiros (aliados que lutam junto com o jogador) — descartados por agora (decisão de 2026-10-18).
- Loja com moedas — adiada (decisão de 2026-10-18). Os nós sem luta continuam sem moedas, mas passam a ter escolha (acampamento, talento com custo que dura e consumível entre 3; decisão de 2026-10-19, ver Mecânica 2). Ideia guardada: virariam lojas pagas com moedas vindas das batalhas.
- Imprevistos de batalha (eventos no meio da luta, como desabamento, fogo que se espalha e baú) — adiados (decisão de 2026-10-18). O ajuste de skills para ganharem do ataque básico e os inimigos que resistem a ele deixaram de fazer sentido com a remoção do ataque básico (2026-10-19).
- Objetivos extras nas batalhas (por exemplo, "mate o xamã antes de ele invocar" ou "termine em 4 turnos") — fora por enquanto (decisão de 2026-10-19). A batalha termina quando todos os inimigos morrem.
- Acessibilidade — nenhum recurso planejado no momento; será revista quando o jogo base estiver funcionando.
- Cronograma e riscos — não serão tratados neste documento.

---

## 11. Glossário

| Termo | Definição                                                                                                                         | Tags   |
| ----- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| Grid  | Tabuleiro de células onde ocorre o combate                                                                                        | #grid  |
| Trait | São mecânicas obtidas a cada nível alcançado pelo jogador, eles concedem estados que alteram características, comportamentos e mecânicas de jogo, e liberam skills. | #trait |
| Talento | Sinônimo de trait na linguagem do jogo (o jogador escolhe talentos ao alcançar um nível). | #trait |
| Skill | Ação ativa do personagem, vinculada à classe ou a traits, e a única forma de agir (sem ataque básico). Possui área de efeito, dano, alcance, cooldown e um efeito no tabuleiro. Pode ser reutilizada. | #skill |
| Golpe anunciado | Golpe de inimigo preparado num turno e disparado no turno seguinte dele, nas células marcadas no grid, contra quem estiver lá. Todos os golpes dos inimigos são anunciados. | #inimigo |
| Elite | Batalha de elite: mais complexa, com um inimigo único, e que dá mais XP, um consumível e uma melhoria de skill. É o outro tipo de batalha, além da normal. | #batalha |
| Skill canalizada | Skill que gasta o agir e também o andar do turno: só pode ser usada antes de andar, e o personagem não anda depois (Meteoro e Tempestade Elétrica do Mago). | #skill |
| Acampamento | Nó sem luta em que o jogador escolhe curar ou melhorar uma skill (substitui o nó de cura). | #nó |
| Atributo | Valor base que configura o personagem no início da run (vida máxima, alcance de movimento, defesa etc.). | #atributo |
| Estado | Condição com nome que concede efeitos ao personagem (incrementa atributos ou modifica comportamentos do jogo), permanente ou por X turnos conforme a situação ou o talento que o aplica. | #estado |
| Turno | Turno global: o jogador joga primeiro e depois jogam os inimigos; termina depois que a última entidade do grid faz sua ação. Cada entidade tem o seu turno dentro dele. | #turno |
| Run | Uma partida completa: da escolha da classe até a vitória (chefe final derrotado) ou a morte. | #run |
| Classe | Personagem jogável, com atributos e talentos próprios (Guerreiro, Mago e Ladino). | #classe |
| Seed | Número que define todos os sorteios da run (mapa, batalhas, ofertas de talento). A mesma seed e as mesmas ações repetem a run. | #seed |
| Consumível | Item de uso único, obtido no nó de consumível. | #consumível |
| Nó | Ponto do mapa da run. Pode ser uma batalha, um acampamento, um nó de talento (com custo), de consumível, entre outros. | #nó |
| Mapa | Estrutura de nós da run, no estilo de Slay the Spire. O jogador escolhe o caminho até o chefe final. | #mapa |
| Batalha | Combate em grid por turnos iniciado ao escolher um nó de batalha. Os inimigos derrotados nela concedem XP, que pode levar a um novo nível e à escolha de um talento. | #batalha |
| Profundidade | Quão longe o jogador está no mapa. Define a complexidade das batalhas (inimigos, golpes, papéis e terreno). | #profundidade |
| Nível | Nível do jogador, obtido com XP de batalhas. Cada nível alcançado concede a escolha de um talento. | #nível |
| XP | Valor de cada inimigo, somado ao jogador na hora em que ele é derrotado (as bolinhas de XP saem juntas do inimigo e voam juntas para a barra). O mapa não mostra o XP da batalha. Leva ao próximo nível. | #xp |
