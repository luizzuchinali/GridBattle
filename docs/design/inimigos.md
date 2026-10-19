---
tags:
  - design
  - inimigos
status: em definição (papéis, comportamentos e inimigos provisórios implementados)
updated: 2026-10-18
---
# Inimigos

> Subdocumento do [[GDD]]. Ações, comportamento e características dos inimigos: Mecânicas 4 e 6 (seção 2.3) e 3.3. O terreno do grid está em [[grid_e_terreno]], e a variedade das batalhas como um todo em [[mapa_e_nos]]. Referências de seção (por exemplo, "3.2" ou "Mecânica 2") apontam para o GDD. As perguntas em aberto deste documento estão listadas em [[perguntas_em_aberto]].

## Mecânica 4: Papéis dos Inimigos
- **Descrição:** Cada inimigo tem um papel que define como ele se comporta, em vez de todos correrem atrás do jogador e atacarem. Antes dos papéis, todos os inimigos (Goblin, Rat, Slime, FireSkull e EyeBat) seguiam o mesmo comportamento de perseguir e atacar e só diferiam em números; hoje cada inimigo tem um papel e um comportamento configurável (ver 3.3). Com papéis distintos, o jogador passa a decidir quem enfrentar primeiro e como se posicionar, e o ataque básico no inimigo mais próximo deixa de ser sempre a melhor jogada. Os inimigos seguem as mesmas regras de turno, alcance e ocupação do jogador (Mecânica 1): em cada turno andam e agem, e agem um de cada vez, cada um terminando antes do próximo. Por padrão os ataques são imediatos (o toque longo num inimigo só mostra até onde ele pode chegar); uma habilidade pode, opcionalmente, ser telegrafada (ver [Habilidades telegrafadas](#habilidades-telegrafadas)).
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

## Habilidades telegrafadas
> Decisão de 2026-10-18 (pedido do usuário), **só design, ainda não implementada**. Substitui a decisão anterior de "sem telegrafia" do [[GDD]] (Mecânica 1).

- **Descrição:** uma habilidade de inimigo pode ser **telegrafada**: o inimigo a prepara num turno, as células que ela vai atingir ficam marcadas no grid, e ela só é disparada no turno seguinte dele, nas células marcadas. O jogador tem a própria vez inteira para reagir: sair da área, empurrar outro inimigo para dentro dela, ficar atrás de um obstáculo, matar quem prepara ou simplesmente aceitar o golpe para fazer outra coisa. Com a regra de andar e agir no mesmo turno (Mecânica 1), desviar não custa o ataque do jogador, então a decisão é *para onde* andar, não *se* vai atacar.
- **É uma possibilidade, não uma regra:** telegrafar é uma opção de **cada habilidade**, desligada por padrão. Um inimigo pode ter só habilidades imediatas, só telegrafadas ou uma mistura. Nenhum inimigo é obrigado a ter uma. O uso esperado é concentrado em **chefes e elites** (golpes grandes que pedem leitura do campo), com casos pontuais em inimigos comuns.
- **Por que:** dá profundidade de posicionamento sem tirar o foco da build. O golpe telegrafado é evitável, então pode ser mais forte (mais dano, área maior ou um estado pesado) do que uma habilidade imediata do mesmo inimigo; quem lê o campo é recompensado, e quem não lê leva um golpe que dói. Também dá identidade aos chefes ("quando ele levanta o martelo, saia de perto").

### Como funciona
1. **Preparar (turno N do inimigo):** a IA decide usar a habilidade como hoje (mesma mira e mesmas regras de alcance e linha de visão). Em vez de disparar, ela **prepara**: a área é calculada e **fica presa às células** (não segue o jogador). Preparar usa o agir do turno. O inimigo pode andar antes de preparar, no mesmo turno, mas não depois.
2. **Esperar:** a habilidade fica preparada por `turnos de preparação` turnos do inimigo (padrão 1: dispara no próximo turno dele). Enquanto isso, o inimigo **não anda** (valor inicial; configurável por habilidade, para golpes em que ele pode se mover).
3. **Disparar (turno N + preparação):** no início da ação do inimigo, antes de qualquer outra coisa, a habilidade atinge **quem estiver nas células marcadas naquele momento**, com o dano, os efeitos e o empurrão normais da skill (passa pelo `CombatResolver`, como todo dano). Disparar usa o agir desse turno; o andar continua livre.
4. **Recarga:** começa a contar quando a habilidade dispara ou é cancelada (mesma regra "N ações entre usos" das skills).

- **Quem é atingido:** segue o campo de alvos da skill. Uma skill que atinge "todos" também acerta os outros inimigos que estiverem na área, o que abre jogadas de isca (atrair o golpe do chefe para cima dos guardas). Valor inicial: os golpes grandes de chefe atingem todos; os de inimigos comuns, só o jogador.
- **Cancelamento:** a habilidade preparada é cancelada se quem prepara **morre** ou recebe um estado que **impede skills** (Atordoado, Congelado). Empurrar ou puxar quem prepara não move a área: ela fica nas células marcadas, para a leitura ser sempre a mesma. Opção por habilidade: `cancelada por deslocamento` (padrão desligado). O chefe é imóvel, então isso só importa para elites e comuns.
- **Várias ao mesmo tempo:** podem existir várias habilidades preparadas na mesma batalha, de inimigos diferentes. As áreas se sobrepõem e cada uma dispara no turno do seu dono, na ordem normal dos inimigos.
- **Determinismo e salvamento:** a habilidade preparada (quem, qual skill, células e turnos restantes) entra no salvamento da batalha. A escolha das células usa as mesmas regras sem sorteio da IA atual.

### Configuração por habilidade (no asset da skill do inimigo)
| Campo | Valor inicial | O que faz |
|---|---|---|
| `telegrafada` | desligado | liga a preparação |
| `turnos de preparação` | 1 | quantos turnos do inimigo até disparar |
| `anda enquanto prepara` | desligado | se o inimigo pode andar nos turnos de espera |
| `cancelada por deslocamento` | desligado | se empurrar ou puxar quem prepara cancela |
| `interrompível por estados` | ligado | se Atordoado/Congelado cancelam |

As habilidades que hoje são ações de IA (curar, proteger, enfraquecer, invocar) também poderiam ser telegrafadas (por exemplo, uma invocação grande anunciada um turno antes). ❓ Se a opção vale também para essas ações ou só para skills. Valor inicial: só skills.

### Como o jogador vê
- **No grid:** as células marcadas ficam com um destaque próprio de **aviso** (cor diferente do laranja do toque longo e do vermelho de ataque), visível o tempo todo, inclusive na vez dos inimigos. É a única exceção à regra de que inimigos não pintam o grid.
- **Sobre quem prepara:** o ícone da habilidade com o número de turnos até o disparo (1 = dispara no próximo turno dele).
- **Toque longo numa célula marcada:** a janela de detalhes mostra qual inimigo prepara, qual habilidade, o dano previsto contra o jogador e em quantos turnos dispara. Nos detalhes de quem prepara: "Preparando: <habilidade>".
- **Toque longo no inimigo:** a marcação de alcance (laranja) continua igual; a área preparada aparece junto, com a cor de aviso.
- **No disparo:** o visual da skill (`ActionVisualDefinition`) toca nas células marcadas, mesmo vazias, para o jogador ver que desviou.
- **Tutorial:** uma dica na primeira vez que uma habilidade telegrafada aparece ("Saia da área marcada antes do próximo turno dele").
- ❓ Se a prévia do nó indica que a batalha tem inimigos com habilidades telegrafadas.

### Exemplos (provisórios, só para orientar o conteúdo)
- **Rei Goblin, Martelada:** prepara em volta de si (3×3), dispara no próximo turno, dano alto, atinge todos (inclusive os Guardas). A Onda de Choque continua imediata.
- **Elite atirador, Chuva de fogo:** marca 3 a 5 células perto do jogador e deixa Queimando em quem ficar.
- **Goblin Arpoador elite, Arpão em linha:** marca uma linha inteira e puxa quem estiver nela.
- **Inimigo comum:** no máximo uma habilidade telegrafada simples, para ensinar a mecânica cedo.

### Interação com outras mecânicas
- **Turno (Mecânica 1):** desviar usa o andar; o agir fica livre. Desfazer o movimento continua valendo.
- **Empurrar e puxar:** jogar um inimigo dentro da área preparada de outro é uma jogada válida; o golpe o atinge se a skill acertar todos.
- **Terreno (Mecânica 5):** obstáculos limitam por onde fugir; linha de visão vale no momento de preparar, não no disparo.
- **Talentos e estados:** Atordoado e Congelado ganham valor como interrupção; alcance e mobilidade ganham valor como fuga.
- **IA:** ❓ Se a IA deve mirar pensando na fuga do jogador (por exemplo, cobrir a célula para onde ele iria) ou só a célula atual. Valor inicial: a mira de hoje (célula do jogador). ❓ Se a IA evita a própria área preparada e a dos aliados.
- **Simulador:** o bot precisa conhecer as células marcadas (evitar terminar nelas quando o golpe vale mais que a jogada) para o balanceamento medir a mecânica.
- **Balanceamento:** ❓ Quanto mais forte uma habilidade telegrafada deve ser que uma imediata. Valor inicial sugerido: cerca do dobro do dano, ou área maior com o mesmo dano.

## Muitos inimigos e inimigos únicos
> Direção do Luiz (2026-10-18): o jogo deve ter **muitos inimigos**, e a cada alguns nós aparecem **inimigos
> únicos, com habilidades**, para dar versatilidade às batalhas. Só design; a geração ainda não faz isso.

- **Elenco grande, introduzido aos poucos:** cada faixa de andares traz inimigos novos, para a run mudar de cara ao
  avançar. Valor inicial: três faixas (andares 1–10, 11–20 e 21–30), cada uma com 6 a 10 inimigos comuns e 3 a 5
  únicos, além dos que continuam das faixas anteriores. Meta de cerca de 40 inimigos no total.
- **Inimigo comum:** sem habilidade ou com uma habilidade simples e imediata. É o volume das batalhas.
- **Inimigo único:** arte própria, mais vida que um comum da mesma faixa, 1 ou 2 habilidades (pelo menos uma
  telegrafada) e um papel claro. É o que muda o plano da batalha: o jogador olha a prévia e decide a rota por ele.
- **Frequência:** valor inicial: um inimigo único em toda batalha difícil e em uma de cada três batalhas normais, a
  partir do andar 4; nunca o mesmo único duas vezes seguidas na run; nunca dois únicos na mesma batalha antes do andar
  15. ❓ Se, em vez disso, os únicos ganham um nó próprio no mapa (nó de elite).
- **Leitura:** a prévia do nó marca a batalha que tem um único (um selo na ficha do inimigo), e o glossário registra
  cada único na primeira vez em que aparece.
- **Como cada inimigo é feito:** um `EnemyConfig`, as skills dele (`SkillDefinition`, com a opção de telegrafar) e a
  arte gerada no PixelLab no estilo do jogo (sprite 24×30 de frente com animação parada; ícones 16×16 das
  habilidades). O teste abaixo valida esse caminho de ponta a ponta.
- ❓ Quantos únicos por faixa, se algum único vira chefe de meio de run, e se os únicos dão recompensa extra (mais XP
  ou um consumível). Valor inicial: só o XP maior, pela ameaça.

## Inimigos novos de teste (2026-10-18)
> Pedido do Luiz: testar inimigos novos com habilidades, com arte própria gerada no PixelLab (não são inimigos
> existentes recoloridos). **Teste**, valores provisórios e sem balanceamento. Cada um mostra um uso diferente das
> habilidades: uma telegrafada de área, uma telegrafada de linha e uma imediata de controle.

| Inimigo | Papel | Vida | Andar | Alcance do ataque | Dano básico | Defesa | XP | Aparece a partir do andar (de 30) |
|---|---|---|---|---|---|---|---|---|
| **Goblin Bombardeiro** | atirador | 24 | 1 | 2 | 4 | 0 | 15 | 6 |
| **Ogro Brutamontes** | corpo a corpo (robusto) | 60 | 1 | 1 | 7 | 1 | 24 | 12 |
| **Aranha Tecelã** | controlador | 20 | 2 | 1 | 3 | 0 | 12 | 4 |

- **Goblin Bombardeiro, Chuva de Fogo** (telegrafada): mira uma célula a 2–4 de distância (círculo), marca uma
  área 3×3 e dispara no próximo turno dele. Dano 8 e Queimando (1 pilha, 2 turnos) em quem estiver na área, inclusive
  outros inimigos (atinge todos menos ele). Recarga 3. Fica a 2 de distância do jogador, como o Crânio de Fogo.
  Ensina a sair da área e a usar a área contra os outros inimigos.
- **Ogro Brutamontes, Pancada Esmagadora** (telegrafada): mira uma célula vizinha em linha reta e marca uma linha de
  3 células a partir dela, para longe do ogro. Dispara no próximo turno dele: dano 14, atinge todos menos ele.
  Enquanto prepara, não anda. Recarga 3. É o golpe que dói muito, mas é fácil de evitar andando para o lado; pede
  leitura do campo e dano alto para derrubá-lo (vida 60 e defesa 1). Sem empurrão: a IA só usa skills de
  deslocamento quando o empurrão vale a pena, e com ele o ogro quase nunca usava o golpe.
- **Aranha Tecelã, Teia** (imediata): mira uma célula a 2–4 de distância, em linha reta. Dano 2 e o estado novo
  **Enredado** (não pode andar no próximo turno) no jogador. Recarga 3. Anda 2 e fica a 2 de distância. Faz
  pressão de controle cedo e valoriza matar quem controla primeiro.
- **Estado novo, Enredado:** −2 de alcance de movimento (o jogador não anda) por 1 turno de quem carrega. Prejudicial.
- ❓ Se esses inimigos ficam no jogo depois do teste, e se algum vira elite. Valor inicial: entram na geração das
  batalhas nos andares acima, com os mesmos pesos dos inimigos provisórios.

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
- **Chefes e elites:** há um chefe final provisório (o Rei Goblin, com dois Guardas Goblin de escolta); elites ainda não existem. Chefes e elites são os principais candidatos a [habilidades telegrafadas](#habilidades-telegrafadas), mas não os únicos. ❓ Lista de chefes e elites (ver Mecânica 2) e se algum inimigo é o chefe final. Valor inicial: Rei Goblin com 2 Guardas Goblin, provisório (ver [[valores_padrao_em_aberto]]).
- ❓ Papel de cada inimigo (Mecânica 4) e quais são inimigos que cobram a build (Mecânica 6). Valores iniciais: ver as notas das Mecânicas 4 e 6 e [[valores_padrao_em_aberto]].

Cada inimigo tem um valor de XP, somado ao XP da batalha (ver Mecânica 2 em 2.3); o valor da tabela é escalado pela profundidade da batalha (ver [[xp_e_niveis]]).
