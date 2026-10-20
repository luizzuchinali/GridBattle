---
tags:
  - design
  - balanceamento
status: em definição (implementado, com valores iniciais configuráveis; números pequenos e fim da escala por profundidade implementados em 2026-10-19, dificuldade por complexidade em parte; batalhas só normal ou de elite em 2026-10-20)
updated: 2026-10-20
---
# Balanceamento e Geração de Batalhas

> Subdocumento do [[GDD]]. Resumo e decisões principais em 2.9 e na Mecânica 2. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## 2.9 Balanceamento e Fórmulas

Esta seção reúne as regras numéricas do jogo, que hoje estão espalhadas pelas mecânicas. Os valores são provisórios, e as fórmulas marcadas como sugestão não foram decididas.

- **Princípio:** a dificuldade vem da profundidade no mapa; o poder do jogador vem do nível (XP). Desde 2026-10-19, a dificuldade vem da **complexidade do combate**, não de números (ver [Dificuldade por complexidade](#dificuldade-por-complexidade-2026-10-19)). A geração é determinística por seed (Mecânica 2), então o balanceamento pode ser testado com simulações do mapa antes de implementar.
- **Curva de XP:** a regra e a curva implementada estão em [[xp_e_niveis]]. Aqui ficam os valores e a fórmula final.
- **XP das batalhas:** O XP de cada batalha vem de um orçamento de XP da profundidade e do tipo de batalha (normal ou elite, desde 2026-10-20), separado do orçamento de ameaça da batalha: (6 + 0,52 × (profundidade − 1)) × multiplicador de orçamento × 6,9 × (1 + 0,07 × (profundidade − 1)) × multiplicador de XP, dividido entre os inimigos pela ameaça de cada um (fórmula completa em [[xp_e_niveis#Curva implementada hoje]]). Assim uma batalha de elite sempre dá mais XP que uma normal no mesmo andar, cerca de 2,7 vezes (fonte `ThreatBudget` em `BattleGenerationSettings`). As fontes antigas (XP base de cada inimigo escalado pela profundidade, ou ameaça de cada inimigo) continuam disponíveis como opção.
- **Tamanho da run:** os valores por profundidade (orçamento, escala, pool, XP, faixas de terreno) são escritos para um mapa de referência de 30 andares (`balanceFloorCount`) e esticados para o número real de andares. Com outro tamanho de mapa, o primeiro andar e o chefe têm a mesma força, e o XP de cada batalha muda para a run render mais ou menos os mesmos níveis.
- **Escala dos inimigos:** decidido e implementado em 2026-10-19: vida e dano dos inimigos **não crescem** com a profundidade (`BattleGenerationSettings.hpPerDepth` e `damagePerDepth` em 0; ver [Dificuldade por complexidade](#dificuldade-por-complexidade-2026-10-19) e [[valores_padrao_em_aberto]]).
- **Ameaça de um inimigo (sugestão, não decidida):** `dano × vida / 50 × (1 + 0,25 × (movimento − 1)) × (1 + 0,5 × (alcance − 1)) × fator do papel`, usada para montar batalhas por orçamento de ameaça (Mecânica 2). Elites contariam como vários inimigos. Desde 2026-10-19 todos os inimigos têm a ameaça escrita à mão (`EnemyConfig.threat`: de 0,7 no Rato a 4,5 no Golem de lava e 10 no chefe), e a fórmula só vale para um inimigo com ameaça 0.
- **Dano e defesa:** o dano das skills é um valor fixo (não há mais ataque básico). ❓ Fórmula da defesa (valor fixo ou porcentagem) e se o jogo prefere números fixos ou porcentagens em geral (decisão em aberto, ver 3.1). Valor inicial configurável: defesa fixa (`dano − defesa`) e dano mínimo 0 desde 2026-10-19, para o Blindado (+1 de defesa) poder zerar um golpe de 1; o modo percentual existe, com teto de 80% (ver [[valores_padrao_em_aberto]]).
- **Vida e cura:** ❓ Quanto o nó de cura recupera, quanto dano o jogador deve sofrer em média por batalha e como a cura da build entra nessa conta. Valor inicial do acampamento: 40% da vida máxima (ver [[valores_padrao_em_aberto]]).
- **Faixa de níveis:** a diferença entre quem pega só batalhas normais e quem pega todas as elites que pode é a alavanca de risco e recompensa (ver [[xp_e_niveis]]). ❓ Valores alvo.
- **Ferramentas:** simulação do mapa e métricas de balanceamento (seção 9). O protótipo de ondas em `docs/design/prototipo_ondas.py` serve de ponto de partida, mas está desatualizado para o modelo de nós.

## Geração das batalhas (parte da Mecânica 2)
- **Geração das batalhas:** as batalhas e o mapa não são escritos à mão; um algoritmo os formula de acordo com a profundidade. Entradas: a *seed* da run, a profundidade (e o tipo de batalha do nó: normal ou elite) e a pool de inimigos do jogo. Saída: quais inimigos entram e em que quantidade, além do XP da batalha. A quantidade é definida pela força de cada inimigo em relação à força esperada para aquela profundidade (inimigos mais fortes entram em profundidades maiores, e os mais fracos aparecem em maior número). A mesma *seed* gera sempre o mesmo mapa e as mesmas batalhas, o que torna a run totalmente reproduzível (para debug). *Seeds* diferentes geram runs diferentes, para que o jogador não sinta repetição entre runs. A mesma *seed* governa todos os aspectos aleatórios da run (mapa, batalhas, ofertas de talento, rerolls e demais sorteios): com a mesma *seed* e as mesmas ações do jogador, a run se repete por completo.
- **Em aberto (geração):** ❓ função da força esperada por profundidade (e o quanto ela assume do nível do jogador), ❓ força de cada inimigo, ❓ regras da pool por profundidade (profundidade mínima/máxima de cada inimigo), ❓ regras de elites e chefes e ❓ limite de inimigos pela capacidade do grid. Valores iniciais configuráveis: curva de orçamento de ameaça 2 no andar 1, 3,5 no 5, 4,5 no 10, 5,5 no 15, 6,5 no 20, 7,5 no 25 e 8,5 no 30, com normal ×1 e elite ×1,35 (até 2026-10-19 havia também a fácil, ×0,75); Goblin, Rato, Slime, Crânio de fogo e Morcego-olho desde a profundidade 1 e os outros comuns entrando entre as profundidades 3 e 16; os inimigos únicos (elites) a partir das profundidades 4, 10 e 18, um em cada batalha de elite (só há nó de elite onde algum está disponível); chefe final provisório; de 2 a 6 inimigos, ocupando no máximo 40% do grid (ver [[valores_padrao_em_aberto]]).
- **Composição das batalhas:** o algoritmo de geração (Mecânica 2) monta as batalhas combinando papéis, e não só inimigos soltos. ❓ Regras de composição (por exemplo, nunca uma batalha só de atiradores, limite de controladores por batalha). Valor inicial: pelo menos um inimigo de linha de frente (corpo a corpo ou enxame).
- **Seed:** a *seed* da run existe apenas para debug e reprodução de cenários e governa todos os sorteios da run, incluindo as ofertas de talento, o mapa e as batalhas; não é exposta ao jogador (ver [[talentos_e_oferta]]).

## Profundidade e dificuldade
- **Spawn por profundidade:** A tabela de spawn filtra inimigos por profundidade mínima/máxima. Conforme o jogador avança no mapa, inimigos mais fortes aparecem.
- **Curva de dificuldade:** sobe com a profundidade ao longo do mapa, até o chefe final. ❓ Valores e ritmo a definir.
- **Inimigos mais fortes com o avanço no mapa:** o avanço no mapa introduz inimigos mais fortes (ver [[mapa_e_nos]] para a definição de profundidade). Desde 2026-10-19, "mais fortes" quer dizer com golpes mais complexos, não com mais vida ou dano.

## Dificuldade por complexidade (2026-10-19)
> Decisão do Luiz em 2026-10-19, **implementada em parte em 2026-10-19**: a dificuldade não deve vir de números, mas da complexidade do combate. Substitui a escala de vida e dano por profundidade e o orçamento de ameaça baseado em vida × dano. Implementado: sem escala de números, inimigos novos com golpes de área, em linha, que empurram, puxam ou deixam terreno, a ameaça de cada inimigo escrita à mão como custo de complexidade e a elite na batalha de elite. Em 2026-10-20 as batalhas passaram a ser só normal ou de elite (ver [Batalha normal e de elite](#batalha-normal-e-de-elite-2026-10-20)). Falta: pontuar a complexidade a partir do golpe, o teto de inimigos por tipo de batalha e as regras de composição da elite (ver [[mapa_e_nos#Batalha normal e de elite (2026-10-20)]]).

- **Sem escala de números:** a vida e o dano dos inimigos comuns não crescem com a profundidade. Um Goblin do andar 25 é igual ao do andar 1. Inimigos únicos e chefes têm mais vida por serem quem são, não pelo andar. Implementado (`hpPerDepth` e `damagePerDepth` em 0).
- **O que cresce com a profundidade e com o tipo de batalha do nó:**
  - **Golpes:** áreas maiores, golpes em linha que atravessam o grid, golpes que empurram ou puxam, golpes que deixam terreno.
  - **Simultaneidade:** mais inimigos com golpe preparado no mesmo turno, cobrindo mais células, o que deixa menos lugares seguros.
  - **Combinação de papéis:** suporte que protege o atacante, controlador que prende o jogador dentro de uma área marcada, invocador que enche o grid.
  - **Terreno:** mais obstáculos que limitam a fuga e perigos perto das áreas marcadas.
  - **Inimigos novos:** cada faixa de andares traz inimigos com golpes novos (ver [[inimigos#Muitos inimigos e inimigos únicos]]).
- **Orçamento de complexidade (sugestão, a validar):** o gerador continua montando a batalha por orçamento, mas o custo de cada inimigo mede a complexidade que ele traz (o golpe, o papel, a mobilidade), e não vida × dano. O orçamento cresce com a profundidade e na batalha de elite. Implementado de forma provisória: o custo é o campo `EnemyConfig.threat`, escrito à mão em cada inimigo (por exemplo, Rato 0,7, Goblin 1, Goblin bombardeiro 1,8, Ogro brutamontes 3,5, Golem de lava 4,5). ❓ Como pontuar a complexidade de um inimigo e de um golpe de forma sistemática.
- **Teto de inimigos:** a dificuldade não pode vir só de "mais inimigos"; o grid 6×6 e a regra de ocupação continuam limitando. ❓ Número máximo por batalha em cada tipo de batalha. Valor atual: o mesmo na normal e na elite, de 2 a 6 inimigos e no máximo 40% do grid ocupado (`minEnemies`, `maxEnemies`, `maxOccupancy`).
- **XP:** continua vindo do orçamento da batalha, então batalhas mais complexas rendem mais. Elite: o design de 2026-10-19 sugeria cerca de ×1,5 o XP da normal; hoje a batalha de elite dá cerca de 2,7 vezes (orçamento ×1,35 e XP ×2), mais um consumível e uma melhoria de skill (pedido de 2026-10-20: a elite dá mais XP e consumíveis).

## Batalha normal e de elite (2026-10-20)
> Pedido do Luiz em 2026-10-20, **implementado**: não há mais batalhas fácil, normal e difícil, só normal e de elite. Regras do mapa e da recompensa em [[mapa_e_nos#Batalha normal e de elite (2026-10-20)]].

- **Perfis** (`BattleGenerationSettings.difficulties`): normal com orçamento ×1, força ×1 e XP ×1; elite com orçamento ×1,35, força ×1 e XP ×2. O perfil da fácil (×0,75, ×1, ×0,65) e a opção `hardBattlesHaveElite` saíram: toda batalha de elite tem um inimigo único.
- **Onde há elite:** um nó só vira elite se `BattleGenerationSettings.HasEliteAt` encontra no pool um inimigo único, não chefe, disponível na profundidade de balanceamento do nó (a mesma regra com que o gerador sorteia a elite). Com o pool atual, os andares 1 a 3 só têm batalhas normais.
- **Pesos do mapa:** normal 75 e elite 25 (`MapGenerationSettings`); antes eram fácil 35, normal 40 e difícil 25.
- **XP no simulador de XP:** média por batalha de 41 (andar 1) a 420 (andar 29) na normal e de 170 (andar 4) a 1134 (andar 29) na elite.

## Números pequenos (2026-10-19)
> Decisão do Luiz em 2026-10-19, **implementada em 2026-10-19** (valores provisórios; o ajuste pelo simulador mudou a vida das classes e do Slime). Com números pequenos, cada ponto de dano conta: um empurrão contra a rocha muda quem morre no turno, e cada golpe recebido pesa. Antes, os inimigos causavam de 3 a 7 contra 80 a 120 de vida do jogador, e nenhum golpe isolado importava.

| Valor | Antes de 2026-10-19 | Valor inicial do design | Implementado (provisório) |
|---|---|---|---|
| Vida do jogador | 80 a 120 | 10 a 20 (Guerreiro 16, Ladino 12, Mago 10) | Guerreiro 18, Ladino 15, Mago 14 |
| Vida dos inimigos comuns | 20 a 40 | 2 a 5 | 2 a 5 |
| Vida de inimigos únicos (elite) | não existiam | 6 a 8 | 7 a 8 (Ogro brutamontes 7, Cavaleiro esqueleto 7, Golem de lava 8) |
| Vida do chefe | 120 | ❓ (sugestão: 15 a 20) | 22 (Rei Goblin) |
| Dano das skills | 4 a 15 | 1 a 3 | 1 a 2 nas skills das classes |
| Dano dos golpes dos inimigos | 3 a 10 | 1 a 3 | 1 a 3 |
| Colisão (empurrado contra algo) | 6, +2 por casa não percorrida | 1, e 1 em quem é atingido | 1, +0 por casa não percorrida, e 1 em quem é atingido |
| Terreno de perigo (Fogo) | 4 | 1 | 1 |
| Cura do acampamento | 30% da vida | 30% da vida máxima, arredondado para cima | 40% da vida máxima, arredondado para o inteiro mais próximo |

- **Porcentagens:** com números pequenos, bônus percentuais (+15% de dano) somem no arredondamento. Talentos e estados passam a usar valores inteiros (+1) ou regras (implementado nos talentos e estados em uso). ❓ O que acontece com crítico, defesa percentual e roubo de vida (sugestão: saem, ou viram efeitos inteiros). Estado atual: os três continuam no código, sem uso: a chance de crítico é 0 em todos os personagens, a defesa está no modo fixo e o estado Roubo de Vida (15%) não é aplicado por nada.
