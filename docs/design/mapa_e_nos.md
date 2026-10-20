---
tags:
  - design
  - mapa
status: em definição (implementado, com valores iniciais configuráveis; acampamento, custo do nó de talento, consumível entre 3 e elite implementados em 2026-10-19; batalhas só normal ou de elite e mapa sem XP em 2026-10-20)
updated: 2026-10-20
---
# Mapa, Nós e Batalhas

> Subdocumento do [[GDD]]. Resumo e decisões principais na Mecânica 2 (seção 2.3). Os consumíveis estão em [[consumiveis]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 2: Mapa, Nós e Batalhas
- **Descrição:** A run é um mapa de nós, como em Slay the Spire. O jogador escolhe o próximo nó entre os disponíveis; ao escolher um nó de batalha, entra no combate no grid. As batalhas ficam mais complexas conforme o jogador avança no mapa (desde 2026-10-19, sem inimigos com mais vida ou dano), e a run termina com o chefe final. ❓ Formato exato do mapa (andares, ramificações e número de caminhos). Valores iniciais configuráveis: 30 andares (o último só com o chefe; escolha do usuário em 2026-10-06), 4 colunas, 2 a 4 nós por andar e até 2 caminhos por nó, sem cruzamentos. O balanceamento se ajusta ao número de andares (ver [[balanceamento_e_geracao]] e [[valores_padrao_em_aberto]]).
- **Run finita:** a run tem um número fixo de nós e termina na vitória (chefe final derrotado) ou na morte. Exemplo, não é valor final: um mapa de cerca de 30 nós (a pool de talentos por classe está em [[talentos_e_oferta]]). ❓ Número de nós e duração da run. Valor inicial: cerca de 31 nós (ver [[valores_padrao_em_aberto]]).
- **Profundidade:** quão longe o jogador está no mapa (a posição do nó, de 1 até o chefe final). Define quais inimigos, papéis e terrenos aparecem e o orçamento da batalha, e é o eixo principal de balanceamento da dificuldade (a escala de vida e dano por profundidade foi desligada em 2026-10-19).
- **Batalhas do mesmo andar (decisão de 2026-10-20, implementada; substitui fácil, normal e difícil):** há só dois tipos de batalha, **normal** e **de elite** (com ao menos um inimigo único). A elite dá mais XP, um consumível e uma melhoria de skill. Valores iniciais configuráveis: 75% normal e 25% elite dos nós de batalha, só onde há uma elite disponível na profundidade (os andares 1 a 3 não têm); quando um ponto de escolha leva a duas ou mais batalhas do mesmo tipo, uma delas vira o outro tipo, se possível. Orçamento de ameaça ×1 na normal e ×1,35 na elite (ver [Batalha normal e de elite](#batalha-normal-e-de-elite-2026-10-20)).
- **Tipos de nó (exemplos):**
  - **Batalha:** combate no grid. Os inimigos derrotados concedem XP, que pode levar a um novo nível e à escolha de um talento.
  - **Cura:** virou **acampamento** (curar ou melhorar uma skill) com a decisão de 2026-10-19, já implementada; ver [Nós e dificuldade](#nós-e-dificuldade-2026-10-19).
  - **Talento:** oferece a escolha de um talento sem precisar lutar, mas **tem um custo**. Valor implementado (provisório): −2 de vida máxima pelo resto da run (ver [Nós e dificuldade](#nós-e-dificuldade-2026-10-19) e [[valores_padrao_em_aberto]]).
  - **Consumível:** concede um item de uso único. Com a decisão de 2026-10-19 (implementada), oferece 3 itens e o jogador escolhe 1.
  - ❓ Outros tipos (por exemplo, evento, chefe de meio de run) e a distribuição deles no mapa. Valores iniciais: o chefe final fecha o mapa; a profundidade 1 só tem batalhas e, depois, 55% são batalha, 15% acampamento, 10% talento e 20% consumível; a elite é um dos dois tipos de batalha, não um tipo de nó próprio (ver [[valores_padrao_em_aberto]]).
- **Sem luta, sem talento:** o jogador só ganha talento ganhando XP em batalhas ou pelo nó de talento, que tem custo. Os nós de cura e de consumível não exigem limite por andar: quem evita as batalhas chega ao chefe sem talentos e perde.
- **Vida entre batalhas:** a vida do jogador persiste entre os nós. Ela só é recuperada no acampamento, em consumíveis e por efeitos que a build conceda. Não há cura automática ao vencer uma batalha nem ao subir de nível.
- **Fim da batalha:** a batalha termina quando todos os inimigos são eliminados. Não há limite de turnos: um jogador que enrola indefinidamente simplesmente não vence. Se builds de muita cura tornarem isso um problema, avaliar um debuff que cresce com o tempo na batalha. ❓ A rever só se acontecer na prática.
- **Derrota:** a morte em qualquer batalha encerra a run.
- **Direções para a variedade das batalhas:** para que as batalhas não sejam sempre "correr atrás do jogador e bater", a variedade vem de três mecânicas: papéis dos inimigos (Mecânica 4, ver [[inimigos]]), personalidade do grid (Mecânica 5, ver [[grid_e_terreno]]) e inimigos que cobram a build (Mecânica 6, ver [[inimigos]]).
- **Prévia do nó:** antes de escolher o nó, o jogador vê o tipo do nó e, nas batalhas, os papéis dos inimigos e o terreno do grid; na elite, o título "Batalha de elite" e a recompensa em palavras, sem números ("Recompensa: mais XP, um consumível e uma melhoria de skill"). Não vê a lista exata de inimigos nem a quantidade de XP (decisão de 2026-10-20).
- **Entrada do jogador:** Escolha de nó no mapa; dentro da batalha, as ações da Mecânica 1.
- **Feedback visual/sonoro:** Mapa com os tipos de nó (a batalha de elite com ícone próprio) e o caminho percorrido, sem o XP dos nós, indicação da profundidade, do nível e da barra de XP na HUD e transição entre mapa e batalha.
- **Interação com outras mecânicas:** Os inimigos derrotados concedem XP, que leva a níveis e à escolha de talentos. A dificuldade crescente por profundidade, contra o nível que o jogador conseguiu juntar, é o que testa a build, e a vida persistente faz o mapa pedir decisões de rota.
- **Nível e XP:** ver [[xp_e_niveis]] (regras) e [[talentos_e_oferta]] (escolha de talentos).
- **Geração das batalhas:** ver [[balanceamento_e_geracao]].

## Nós e dificuldade (2026-10-19)
> Decisões do Luiz em 2026-10-19, **implementadas em 2026-10-19 em parte**, com valores provisórios: subir de nível não cura, acampamento, custo do nó de talento, consumível entre 3 e elite na batalha difícil. Em 2026-10-20 as três dificuldades viraram duas (normal e elite; ver [Batalha normal e de elite](#batalha-normal-e-de-elite-2026-10-20)), e a tabela de dificuldades abaixo ficou só como histórico. Falta: a elite com papéis combinados e terreno de perigo. O problema que motivou as decisões: os nós de cura, talento e consumível não traziam escolha (a cura por nível apagava a vida perdida e os custos), e as três dificuldades quase não mudavam a batalha: a difícil tinha 35% mais dos mesmos inimigos (`BattleGenerationSettings`: orçamento ×1,35, força ×1) e dava o dobro de XP.

- **Primeiro passo:** subir de nível não cura (ver [[xp_e_niveis]]). Sem isso, nenhuma mudança nos nós funciona. Implementado.
- **Acampamento (substitui o nó de cura):** o jogador escolhe curar **ou** melhorar uma skill (variante; ver [[classes_e_skills#Nova direção das skills (2026-10-19)]]). Quem está bem de vida melhora; quem está mal cura. Implementado: descansar cura 40% da vida máxima, arredondado para o inteiro mais próximo (`RunSettings.healFraction`; o valor inicial do design era 30%); treinar abre a janela de melhoria, e recusar a melhoria volta à escolha do acampamento. Sem skill para melhorar, o acampamento só cura. O mapa garante um acampamento antes do chefe (`MapGenerationSettings.guaranteeHealBeforeBoss`).
- **Nó de talento:** escolher 1 de 3 talentos, pagando um custo que dura, porque o custo de 15% da vida só funcionava enquanto o nível curava. Opções discutidas: −2 de vida máxima; uma maldição (estado negativo por 2 batalhas); a próxima batalha vira elite. Valor implementado (a sugestão, provisória): −2 de vida máxima pelo resto da run (`RunSettings.talentNodeCost = MaxHp`, `talentNodeCostMaxHp = 2`).
- **Nó de consumível:** oferece 3 itens e o jogador escolhe 1 (implementado: `ConsumableSettings.choiceCount = 3`). Os itens de tabuleiro, em [[consumiveis]], ainda não foram implementados.
- **Dificuldade por complexidade (histórico; substituída em 2026-10-20 por normal e elite):** fácil, normal e difícil mudam o que acontece na batalha, não a força dos inimigos (a força já não muda: `strengthMultiplier` 1 nas três):

| Dificuldade | Composição | Recompensa |
|---|---|---|
| Fácil | poucos inimigos, 1 ou 2 papéis, golpes simples (um alvo ou linha curta) | XP menor |
| Normal | mais papéis juntos, algum golpe de área | XP |
| Difícil (elite) | um inimigo único com golpes grandes e papéis que se combinam (suporte protegendo, controlador prendendo), com terreno de perigo | mais XP (valor inicial ×1,5) e uma melhoria de skill no fim |

- **Estado da implementação da tabela (até 2026-10-19):** a difícil tinha um inimigo único (elite) quando havia um disponível na profundidade, e vencê-la dava uma melhoria de skill. A fácil e a normal só diferiam no orçamento (×0,75 e ×1). Em 2026-10-20 a fácil saiu e a difícil virou a batalha de elite (abaixo).
- **Regras de geração:** [[balanceamento_e_geracao#Dificuldade por complexidade (2026-10-19)]].
- **Prévia do nó:** mostra também a recompensa extra da elite (implementado; desde 2026-10-20 sem números).
- **Sem objetivos extras** nas batalhas por enquanto (decisão de 2026-10-19): a batalha termina quando todos os inimigos morrem.
- ❓ Se o jogador vê mais andares do mapa à frente, para planejar a rota (onde estão o próximo acampamento e a próxima elite).

## Batalha normal e de elite (2026-10-20)
> Pedido do Luiz em 2026-10-20, **implementado** (valores provisórios): "não existem mais batalhas fácil, normal e difícil: só batalha normal e batalha com ao menos um inimigo elite. A elite dá mais XP e itens consumíveis." E: "o mapa não mostra a quantidade de XP; só o ícone de que a batalha é mais difícil".

- **Dois tipos de batalha:** normal e de elite (`EBattleDifficulty { Normal = 1, Elite = 2 }`). Os números antigos ficaram: num save antigo, a difícil carrega como elite e a fácil vira normal ao retomar a run (`MapRules.NormalizeDifficulties`; uma difícil sem elite também vira normal).
- **Onde aparece a elite:** o mapa sorteia normal com peso 75 e elite com peso 25 (`MapGenerationSettings.normalWeight` e `eliteWeight`), mas um nó só vira elite se o gerador de batalhas tem um inimigo único disponível na profundidade de balanceamento do nó (`BattleGenerationSettings.HasEliteAt`). Hoje: Ogro brutamontes a partir do andar 4, Cavaleiro esqueleto a partir do 10 e Golem de lava a partir do 18; os andares 1 a 3 só têm batalhas normais. Num ponto de escolha em que todas as batalhas são do mesmo tipo, uma delas vira o outro tipo quando possível.
- **A batalha de elite:** sempre tem um inimigo único, com o resto do orçamento como escolta (orçamento ×1,35, força ×1, XP ×2 do perfil Elite de `BattleGenerationSettings`). A normal usa o perfil Normal (×1, ×1, ×1).
- **Recompensa da elite**, nesta ordem, depois da vitória:
  1. **mais XP**, durante a batalha (cerca de 2,7 vezes o XP de uma normal no mesmo andar; ver [[xp_e_niveis]]);
  2. **consumível:** `RunSettings.eliteConsumableOffers` ofertas (valor inicial 1), cada uma igual à do nó de consumível (escolher 1 entre 3; inventário cheio segue a política de sempre), com o título "Recompensa da elite";
  3. **melhoria de skill:** como antes (`RunSettings.eliteGrantsSkillUpgrade`, ligado), quando alguma skill ainda pode melhorar.
  Recusar uma etapa abre mão só dela. O nó termina depois da última, e cada etapa fica no salvamento da run (retomar no meio da recompensa funciona).
- **Mapa sem XP:** o nó de batalha mostra só o ícone (normal ou elite). A prévia mostra o título ("Batalha" ou "Batalha de elite"), os papéis dos inimigos, o terreno e, na elite, a linha de recompensa sem números: "Recompensa: mais XP, um consumível e uma melhoria de skill." (ou "Recompensa: mais XP e um consumível.", com a melhoria desligada).
- **Não implementado:** a composição própria da elite (papéis combinados, terreno de perigo garantido). O texto da prévia cita um consumível mesmo com `eliteConsumableOffers` em 0.
