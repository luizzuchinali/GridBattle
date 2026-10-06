---
tags:
  - design
  - inimigos
status: em definição (papéis, comportamentos e inimigos provisórios implementados)
updated: 2026-10-05
---
# Inimigos

> Subdocumento do [[GDD]]. Ações, comportamento e características dos inimigos: Mecânicas 4 e 6 (seção 2.3) e 3.3. O terreno do grid está em [[grid_e_terreno]], e a variedade das batalhas como um todo em [[mapa_e_nos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 4: Papéis dos Inimigos
- **Descrição:** Cada inimigo tem um papel que define como ele se comporta, em vez de todos correrem atrás do jogador e atacarem. Antes dos papéis, todos os inimigos (Goblin, Rat, Slime, FireSkull e EyeBat) seguiam o mesmo comportamento de perseguir e atacar e só diferiam em números; hoje cada inimigo tem um papel e um comportamento configurável (ver 3.3). Com papéis distintos, o jogador passa a decidir quem enfrentar primeiro e como se posicionar, e o ataque básico no inimigo mais próximo deixa de ser sempre a melhor jogada. Os inimigos seguem as mesmas regras de turno, alcance e ocupação do jogador (Mecânica 1) e não telegrafam ataques.
- **Papéis (exemplos, a refinar):**
  - **Corpo a corpo:** persegue o jogador e ataca (o comportamento padrão).
  - **Enxame:** frágil, rápido e em quantidade.
  - **Atirador:** ataca à distância e tenta manter a distância do jogador.
  - **Suporte:** cura ou fortalece aliados, o que o torna um alvo prioritário.
  - **Invocador:** cria novos inimigos ao longo da batalha.
  - **Controlador:** aplica estados negativos ao jogador (por exemplo, Fraquejado ou impedir o movimento).
  - ❓ Lista final de papéis e de inimigos de cada papel. Valores iniciais: seis papéis (corpo a corpo, enxame, atirador, suporte, invocador e controlador). Goblin e Slime são corpo a corpo, Rat e EyeBat são enxame e FireSkull é atirador; Xamã Goblin (suporte), Rainha dos Ratos (invocador) e Olho Maldito (controlador) são provisórios, com arte existente recolorida (ver [[valores_padrao_em_aberto]]).
- **Composição das batalhas:** definida pelo algoritmo de geração (ver [[balanceamento_e_geracao]]); os papéis entram na composição.
- **Entrada do jogador:** Nenhuma direta; o jogador reage ao comportamento dos inimigos pelas ações da Mecânica 1.
- **Feedback visual/sonoro:** Ícone ou marca visual do papel sobre o inimigo; a prévia do nó mostra os papéis presentes.
- **Interação com outras mecânicas:** Controladores e suportes aplicam estados (ver 3.1). Alcance, área e controle do jogador passam a ter valor para alcançar atiradores e suportes que ficam atrás. A prévia do nó revela os papéis para o jogador planejar a rota.

## Mecânica 6: Inimigos que Cobram a Build
- **Descrição:** Alguns inimigos exigem uma resposta específica da build e não podem ser resolvidos só com o ataque básico. Cada um testa uma parte da build, e uma build de uma coisa só será testada em algum momento, o que incentiva o jogador a diversificar os talentos. Um jogador experiente sabe quais inimigos podem aparecer e escolhe os talentos para lidar com eles em um momento futuro da run; assim, o conhecimento do jogo vira vantagem estratégica.
- **Exemplos (a refinar):**
  - **Blindado:** defesa alta; pede penetração de defesa ou dano que ignore defesa (por exemplo, dano ao longo do tempo).
  - **Regenerante:** recupera vida; pede dano concentrado (burst).
  - **Enxame:** muitos inimigos frágeis; pede skills de área.
  - **Rápido:** alcance de movimento alto; pede controle (por exemplo, impedir o movimento).
  - **Espinhoso:** devolve parte do dano recebido; pede skills à distância ou dano ao longo do tempo.
  - ❓ Lista final, frequência com que aparecem e se isso se liga a elites e chefes. Valores iniciais: Goblin Blindado (defesa), Slime Regenerante (regeneração) e Rato Espinhoso (espinhos), todos provisórios (ver [[valores_padrao_em_aberto]]).
- **Como são definidos:** pelos atributos e estados do inimigo (ver 3.1), o que permite criar essas respostas obrigatórias combinando os mesmos blocos do sistema de estados. ❓ Quais atributos e estados valem para os inimigos. Valor inicial: todos, com o mesmo modelo (ver [[valores_padrao_em_aberto]]).
- **Entrada do jogador:** Nenhuma direta; a escolha acontece na tela de talentos (Mecânica 3) e na rota do mapa (Mecânica 2).
- **Feedback visual/sonoro:** Indicação visível das características do inimigo (por exemplo, ícone de blindado ou de regenerante), para que o jogador leia o que ele exige.
- **Interação com outras mecânicas:** A prévia do nó mostra os papéis dos inimigos, o que informa a escolha de rota. A pool de inimigos possíveis é parte do conhecimento do jogador para escolher talentos (Mecânica 3). O Glossário do jogo (2.7) registra os inimigos já enfrentados. ❓ Se o sorteio de talentos deve considerar os inimigos que o jogador já viu na run.

## 3.3 NPCs / Inimigos
Papéis e comportamentos dos inimigos: ver Mecânica 4. Inimigos que exigem resposta da build: ver Mecânica 6 em 2.3.

Inimigos implementados hoje (um asset de configuração por inimigo). Valores provisórios.

| Nome | Vida máxima | Alcance de movimento | Alcance de ataque | Dano básico | XP |
|---|---|---|---|---|---|
| Goblin | 30 | 1 | 1 | 5 | 10 |
| Rat (Rato) | 20 | 2 | 1 | 5 | 8 |
| Slime | 40 | 1 | 1 | 5 | 12 |
| FireSkull (Crânio de fogo) | 25 | 1 | 2 | 7 | 15 |
| EyeBat (Morcego-olho) | 25 | 2 | 1 | 5 | 8 |
| GoblinShaman (Xamã Goblin), provisório | 24 | 1 | 1 | 3 | 14 |
| RatQueen (Rainha dos Ratos), provisório | 30 | 1 | 1 | 4 | 16 |
| HexingEye (Olho Maldito), provisório | 22 | 1 | 1 | 3 | 14 |
| ArmoredGoblin (Goblin Blindado), provisório | 30 | 1 | 1 | 5 | 14 |
| RegeneratingSlime (Slime Regenerante), provisório | 35 | 1 | 1 | 4 | 14 |
| ThornyRat (Rato Espinhoso), provisório | 28 | 1 | 1 | 4 | 12 |
| GoblinKing (Rei Goblin), chefe final provisório | 120 | 1 | 1 | 10 | 50 |
| GoblinGuard (Guarda Goblin), escolta do chefe, provisório | 36 | 1 | 1 | 5 | 20 |
| GoblinHookman (Goblin Arpoador), puxa o jogador, provisório (acréscimo de balanceamento) | 26 | 1 | 1 | 4 | 14 |

- **Comportamento atual:** cada inimigo tem um `behavior` configurável (ações em ordem de prioridade; Mecânica 4). Corpo a corpo e enxame: se estiverem no alcance de ataque do jogador, atacam; senão, dão um passo em direção a ele. Atirador (FireSkull): mantém a distância de 2 e ataca de longe. Suporte (Xamã Goblin): cura os aliados feridos, protege-os com Escudo e fica a 3 de distância do jogador. Invocador (Rainha dos Ratos): invoca até 2 Ratos e fica a 3 de distância. Controlador (Olho Maldito): aplica Fraquejado de longe e depois se aproxima para atacar. Detalhes em [[sistema_combate]].
- **Skills dos inimigos:** cada tipo de inimigo terá skills exclusivas dele, separadas das skills das classes (ver [[classes_e_skills]] para as skills das classes). Hoje só duas têm skill (`SkillDefinition`): o **Goblin Arpoador** (provisório, papel controlador, a partir do andar 8 de 30) lança o **Gancho** (dano 3, alcance 4, cooldown 3, puxa o jogador até 3 células em direção a ele, para trazê-lo ao grupo, a perigos ou a obstáculos) e o **Rei Goblin** tem a **Onda de Choque** (dano 6 ao redor, empurra 2 células, cooldown 3) e é **imóvel** (nada o empurra nem o puxa). A IA só usa uma skill de deslocamento quando o resultado previsto compensa: o Arpoador puxa sempre que está pronto e o chefe só empurra quando o jogador está contra a borda ou um obstáculo (ver [[sistema_combate]], "Empurrar e puxar"). A ação de usar skills já existe na IA dos inimigos. As habilidades dos inimigos atuais (curar, proteger, enfraquecer e invocar) são ações de IA com cooldown, pela mesma regra das skills ("N ações entre usos"), e aparecem na janela de detalhes como o texto do comportamento (ver [[valores_padrao_em_aberto]]). ❓ Lista de skills de cada tipo de inimigo e como elas aparecem na janela de detalhes da entidade.
- **Encontro atual:** o encontro de teste do editor coloca os cinco inimigos básicos no grid de uma vez; nas runs, as batalhas são geradas por profundidade (ver [[balanceamento_e_geracao]]).
- **Chefes e elites:** há um chefe final provisório (o Rei Goblin, com dois Guardas Goblin de escolta); elites ainda não existem. ❓ Lista de chefes e elites (ver Mecânica 2) e se algum inimigo é o chefe final. Valor inicial: Rei Goblin com 2 Guardas Goblin, provisório (ver [[valores_padrao_em_aberto]]).
- ❓ Papel de cada inimigo (Mecânica 4) e quais são inimigos que cobram a build (Mecânica 6). Valores iniciais: ver as notas das Mecânicas 4 e 6 e [[valores_padrao_em_aberto]].

Cada inimigo tem um valor de XP, somado ao XP da batalha (ver Mecânica 2 em 2.3); o valor da tabela é escalado pela profundidade da batalha (ver [[xp_e_niveis]]).
