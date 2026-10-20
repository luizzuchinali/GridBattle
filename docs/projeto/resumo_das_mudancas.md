---
tags:
  - projeto
  - resumo
created: 2026-10-12
---
# Resumo das mudanças (2026-10-05 a 2026-10-20)

Um resumo curto do que foi feito no jogo, para entender as mudanças sem ler
os planos inteiros. Detalhes técnicos: [[guia_sistemas]] (como usar cada
sistema pelo editor), [[simulador_balanceamento]], [[plano_implementacao_gdd]],
[[plano_balanceamento]] e `AGENTS.md`.

## Ajustes de 2026-10-20

Cinco pedidos seus, feitos sobre o novo molde (seção seguinte). O design está
no [[GDD]] e nos documentos de `docs/design/`; o uso pelo editor, em
[[guia_sistemas]].

### O que muda para quem joga

- **Contras do Mago.** O Meteoro (e as variantes Meteoro Ardente e Meteoro
  Veloz) é uma magia **canalizada**: gasta a ação e também o andar do turno,
  então só pode ser usado antes de andar, e o Mago fica **Exausto** (não anda)
  no turno seguinte. Muito dano em área, mas tem de ser lançado de um lugar
  seguro. A Tempestade Elétrica (e a Rápida e a Ampla) também é canalizada,
  sem o Exausto. Depois de andar, o botão dessas magias fica apagado; desfazer
  o andar o acende de novo. A ficha da skill mostra "Em você: Exausto" e a
  nota "Usa o andar do turno: só antes de andar, e não anda depois.".
- **Golpe anunciado em todo turno.** Antes, um inimigo com um só golpe de
  recarga 1 ou 2 marcava, disparava e ficava um ou dois turnos sem marca. Agora
  todo inimigo tem um golpe **básico** sem recarga e um **especial** com
  recarga: prepara o especial quando está pronto e o básico nos outros turnos.
  Entraram 17 golpes novos (por exemplo, Estilingada, Investida, Grito Sônico,
  Cuspe Ácido e Tornado), com ícones novos. A lista de cada inimigo está em
  [[inimigos]].
- **XP em explosão.** Quando um inimigo morre, sai um "bolo" de XP, como no
  *Vampire Survivors*: as bolinhas aparecem juntas, se espalham numa mini
  explosão e voam todas juntas para a barra, em cerca de 0,8 s, qualquer que
  seja o XP. Cada abate tem no máximo 8 bolinhas; com mais XP, cada uma vale
  mais. O jogo não fica mais parado esperando a fila de bolinhas em nível alto.
- **Batalha normal ou de elite.** Não há mais fácil, normal e difícil. A
  batalha de elite tem um inimigo único e dá mais XP (cerca de 2,7 vezes a
  normal no mesmo andar), uma oferta de consumível ("Recompensa da elite",
  escolher 1 de 3) e uma melhoria de skill. Os andares 1 a 3 só têm batalhas
  normais, porque a primeira elite (Ogro brutamontes) entra no andar 4.
- **Mapa sem XP.** O mapa não mostra mais o XP dos nós: só o ícone de elite
  marca a batalha mais difícil. A prévia mostra "Batalha" ou "Batalha de
  elite", os papéis, o terreno e, na elite, a recompensa em palavras.

### Valores padrão escolhidos

Todos provisórios (o balanceamento continua pausado).

| Item | Valor |
|---|---|
| Skills canalizadas | Meteoro, Tempestade Elétrica e as variantes (`usesWalk`) |
| Exausto | aplicado pelo Meteoro no próprio Mago por 2 turnos (o turno do lançamento e o seguinte, em que ele não anda) |
| Bônus de duração dos talentos | não vale mais para estados que a skill aplica no próprio lançador |
| Golpes dos inimigos | um básico com recarga 0 e um especial com recarga 2 nos golpes novos (os especiais antigos mantiveram a recarga) |
| Orbs de XP | no máximo 8 por abate; explosão de 14 ± 6 px em 0,22 s, pausa de 0,08 s, voo de 0,45 s |
| Tipos de batalha | normal 75, elite 25 (elite só onde há uma elite disponível) |
| Batalha de elite | orçamento ×1,35, XP ×2 (o antigo difícil) |
| Recompensa da elite | 1 oferta de consumível (`eliteConsumableOffers`), depois a melhoria de skill |

### O que ainda pode acontecer

- **Turno sem golpe anunciado:** quando nenhum golpe do inimigo alcança o
  jogador (o Totem do raio nunca anda), quando um inimigo de apoio cura,
  protege, invoca ou enfraquece (isso usa a ação dele), e no primeiro turno da
  batalha (decisão de 2026-10-19).
- **Tempestade com o Mago Exausto:** como a regra é "ainda não andou", dá para
  usar a Tempestade no turno em que o Mago está Exausto.
- **Bot do simulador e o Meteoro:** o bot ainda não pesa o custo de ficar sem
  andar no turno seguinte.
- **Saves antigos:** uma run salva antes da mudança continua; as batalhas
  fáceis viram normais e as difíceis sem elite também.

### Como foi verificado

No seu PC: o projeto compila sem erros; `GridBattle > Validate Content` deu 0
erros; as checagens de talentos passaram (4219 verificações); o script da
migração confirmou que todo inimigo tem um golpe anunciado sem recarga e um
com recarga.

## Novo molde do combate (2026-10-19)

O combate foi refeito no molde do *Into the Breach*, e a progressão no do
*Slay the Spire*. O design está no [[GDD]] e em `docs/design/` (a referência
do combate é `docs/design/sistema_combate.md`); o uso pelo editor está em
[[guia_sistemas]]. As seções mais abaixo descrevem o jogo **antes** dessa
mudança (por exemplo, o ataque básico e os talentos de número, que saíram).

### O que muda para quem joga

- **Sem ataque básico.** Toda ação é uma skill. Cada classe começa com 3
  skills e libera até mais 3 por talentos (6 espaços na barra). O jogador anda
  até 2 casas, age 1 vez e pode usar 1 item por turno, em qualquer ordem.
- **Golpes anunciados.** Na vez de cada inimigo, ele dispara o golpe que
  preparou no turno anterior, anda e prepara o próximo. As células do golpe
  ficam marcadas no tabuleiro um turno antes. O golpe acerta todo mundo que
  estiver nelas, menos quem lança: outros inimigos também. Se o jogador
  empurrar, puxar ou trocar de lugar quem prepara, a área vai junto, e dá para
  virar o golpe contra os outros inimigos. Matar quem prepara, ou atordoá-lo ou
  congelá-lo, cancela o golpe. Curar, proteger, invocar e enfraquecer
  continuam imediatos. No primeiro turno da batalha nenhum golpe está
  preparado.
- **Ordem fixa e numerada.** Os inimigos agem sempre na mesma ordem durante a
  batalha, e o número de cada um aparece sobre a cabeça dele e na área do
  golpe que ele preparou.
- **Mirar e confirmar com previsão.** O primeiro toque numa célula só mira e
  mostra o que aconteceria: dano, mortes, setas dos empurrões, onde cada um
  terminaria e onde os golpes anunciados cairiam depois da ação. O segundo
  toque (ou *Confirmar*) executa; *Cancelar* desfaz a mira.
- **Talentos que mudam o jogo.** Os 48 talentos de número (mais dano, mais
  área, menos recarga) saíram. Ficaram os que liberam skills, os de regra
  (Dominó, Rastro de Chamas, Gelo Escorregadio, Contragolpe, Passo Trocado,
  Ímpeto) e as passivas de classe.
- **Melhoria de skill.** Cada skill melhora uma vez: escolher 1 entre 2
  variantes (por exemplo, a Bola de Fogo vira Explosiva ou Arqueada). A
  melhoria vem do acampamento ou de vencer uma elite.
- **Acampamento.** O antigo nó de cura deixa escolher entre descansar (cura
  40% da vida máxima) ou treinar (melhorar uma skill).
- **Elite.** Toda batalha difícil tem uma elite (hoje Ogro brutamontes,
  Cavaleiro esqueleto ou Golem de lava); vencê-la dá uma melhoria de skill.
- **Vida.** Subir de nível dá +1 de vida máxima e não cura mais. O nó de
  talento custa 2 de vida máxima pelo resto da run.
- **Conteúdo novo:** 10 inimigos com arte própria (27 no total), skills novas
  das três classes com as variantes de melhoria, ícones novos e a Muralha de
  Gelo, um terreno criado por skill (ver [[arte]]).

### Valores padrão escolhidos

Todos provisórios: o balanceamento continua pausado (decisão sua de
2026-10-06).

| Item | Valor |
|---|---|
| Ataque básico | desligado em todos (jogador e inimigos) |
| Skills | 3 iniciais por classe, até 3 por talentos (`maxSkillTalents` = 3) |
| Andar do jogador | 2 casas |
| Golpes dos inimigos | anunciados 1 turno antes, acertam todos menos quem lança, a área acompanha quem é deslocado, atordoar ou congelar cancela |
| Disparar um golpe | não gasta o agir do inimigo (`firingUsesAction` desligado) |
| Subir de nível | +1 de vida máxima, sem cura |
| Curva de XP | 150 para o nível 2, +100 por nível, nível máximo 12 (cerca de 10 níveis por run; no simulador de XP, nível final médio 9,5 / 9,9 / 10,6 nas rotas fácil / normal / difícil) |
| Nó de talento | custa 2 de vida máxima |
| Acampamento | descansar (cura 40%) ou treinar (melhoria) |
| Elite | dá uma melhoria ao vencer |
| Melhorias | uma por skill, 1 entre 2 variantes |
| Armadura e dano mínimo | o dano mínimo é 0: armadura −1 pode zerar um golpe de 1 |
| Colisão | 1 de dano a quem é lançado, 0 por casa não percorrida, 1 a quem é atingido; ignora a armadura |
| Vida das classes | Guerreiro 18, Mago 14, Ladino 15 |
| Slime | 4 de vida |
| Orçamento de ameaça do andar 1 | 2 (era 2,5) |
| Chefe (provisório) | Rei Goblin, 22 de vida, imóvel, com escolta de Guarda Goblin, Xamã goblin, Goblin artilheiro e Goblin lanceiro |
| Números da ordem na mesma célula | 9 px entre eles (antes 6 px fixos: "1 3" parecia "13") |

### O que o simulador mostrou

Depois do ajuste provisório, 20 runs por classe (seeds 1000 a 1019):

- **Vitórias:** Guerreiro 60%, Mago 20%, Ladino 55%. 73% das derrotas
  acontecem antes do andar 10. O chefe foi vencido em todas as runs que
  chegaram até ele.
- **Dano por batalha** (em % da vida máxima): andares 1 a 4, 21%; 5 a 9,
  16%; 10 a 29, de 10 a 15%; chefe, 27%.
- **Empurrões por run** (inimigos deslocados / colisões / mortes por
  colisão): Guerreiro 61 / 43 / 38, Mago 20 / 7 / 6, Ladino 5 / 2 / 0.
- **Golpes de inimigos em inimigos** (dano / mortes por run): Guerreiro
  2,4 / 0,6, Mago 1,8 / 0,5, Ladino 6,0 / 2,1.
- **O Mago é o mais fraco:** leva o mesmo dano por batalha que os outros e
  quase não mata por colisão; o bot usa pouco o controle (Nova de Gelo 1,8 vez
  por run).

O bot do simulador agora lê a mesma previsão que o jogador vê: evita os golpes
anunciados, valoriza desviar golpes para outros inimigos e conta que ainda
pode andar depois de agir. Detalhes em [[simulador_balanceamento]].

### O que o teste de interface mostrou

Teste no Play Mode do seu PC (o perfil real foi guardado e restaurado): uma
run de Guerreiro, a primeira batalha, 5 turnos com capturas de tela.
Funcionou como o design pede:
- a ordem dos inimigos aparece numerada sobre cada um, com um selo que mostra
  a ordem, o ícone do golpe e o dano;
- as células atingidas têm contorno vermelho, e o total previsto aparece sobre
  o jogador ("-2");
- ao mirar, a previsão mostra o dano no alvo ("-1"), a seta do empurrão, o
  fantasma onde o empurrado termina e o golpe dele acompanhando-o (o dano
  previsto no jogador cai de -3 para -2); quem morreria perde o selo do golpe;
- o toque longo num inimigo mostra "Ordem 1 de 3" e "Preparando: <golpe> (em
  1 turno)";
- a vida do jogador caiu exatamente o previsto;
- no primeiro turno nenhum golpe estava preparado, como manda o design.

### O que ficou pendente

- **Balanceamento:** continua pausado; os números acima são um primeiro
  ajuste pelo simulador. O Mago precisa de atenção quando o balanceamento
  voltar.
- **Previsão do Dominó:** o empurrão em cadeia do talento Dominó ainda não
  aparece na previsão (o jogo aplica, a tela não mostra antes).
- **Visuais das skills:** continuam provisórios.
- **Arte provisória:** 9 inimigos ainda usam a arte de outro inimigo com outra
  cor, inclusive o chefe (lista em [[arte]]).
- **Nada foi enviado ao repositório remoto** (decisão sua): as mudanças estão
  em commits locais.

## Em uma frase

O jogo deixou de ser uma batalha de demonstração e virou uma **run
completa**: menu, escolha de classe, mapa de 30 andares com batalhas, cura,
talentos e itens, chefe final, salvamento, glossário, opções e três idiomas.
Tudo é configurável por assets no Inspector.

## 1. O que o jogo tem agora

| Sistema | O que faz | Onde mexer (em `Assets/Application/Settings/`) |
|---|---|---|
| Classes | Guerreiro, Mago e Ladino, cada um com uma skill inicial e os próprios talentos. Mago e Ladino se liberam com batalhas vencidas (15 e 30) | `Characters/Players/` |
| Atributos e estados | Vida, dano, defesa, crítico, alcance... e estados com duração (veneno, escudo, regeneração, atordoado, queimando...) | configs dos personagens, `States/` |
| Combate | Fórmula de dano com crítico, defesa, escudo, roubo de vida e espinhos | `Combat/CombatSettings` |
| Skills | 14 skills com área, alcance, recarga e efeitos; os talentos podem melhorá-las | `Skills/` |
| Empurrar e puxar | Skills que deslocam personagens; bater em obstáculo, borda ou outro personagem causa dano; parar num terreno ativa o efeito na hora | `Combat/CombatSettings`, efeito `Displace` nas skills |
| Inimigos e IA | 6 papéis (corpo a corpo, enxame, atirador, suporte, invocador, controlador), comportamentos montados com ações (atacar, perseguir, manter distância, curar, invocar, puxar...) e um chefe | `Characters/Enemies/`, `AI/` |
| Terreno | Obstáculos, células de perigo (fogo, pântano) e de bônus (santuário, pedra de proteção) | `Terrain/` |
| Consumíveis | Poção, bomba, tônico; usar um item não gasta a ação (1 por turno) | `Consumables/` |
| Mapa e run | Mapa de nós com 30 andares (batalha fácil/normal/difícil, cura, talento, item, chefe); batalhas geradas por um "orçamento de ameaça" que cresce com o andar | `Map/`, `Run/` |
| XP e talentos | Curva de XP única para todas as classes; a cada nível, escolha entre 3 talentos (com reroll, banir e pular) | `Progression/`, `Talents/` |
| Perfil e meta | Classes liberadas, glossário, recordes, opções, dicas de tutorial, métricas locais | `Meta/`, `Tutorial/` |
| Interface | HUD com skills, itens e profundidade; detalhes por toque longo; telas de mapa, fim de run e vitória; modais de talento, item, confirmação, glossário, opções, pausa e dicas | `Hud/`, `Screens/`, `Modals/`, `Assets/Application/UI/` |
| Textos | Inglês, espanhol e português, pela Unity Localization | `Assets/Application/Localization/` |
| Áudio | Sistema pronto (efeitos e músicas por contexto), ainda sem clipes | `Audio/AudioLibrary` |
| Salvamento | A run é salva no início de cada vez do jogador e pode ser continuada pelo menu | arquivos `run.json`, `profile.json` |

**Regra geral:** conteúdo e números moram em assets. Para criar um inimigo,
skill, estado, talento, item ou terreno, basta criar o asset pelo menu
`Create > GridBattle/...` e preenchê-lo; o código não muda. O passo a passo
está em [[guia_sistemas]].

## 2. Mudanças pedidas por você durante o trabalho

- **Sem anúncios nem compras** por enquanto.
- **Perguntas em aberto do design (❓)** viraram campos no Inspector com um
  valor neutro, listados em [[valores_padrao_em_aberto]].
- **Curva de XP fora das classes:** a mesma curva para todas, num asset só.
- **Toda classe começa com uma skill:** Guerreiro com Golpe (dano 10, linha
  de 3, valor ajustado por você), Mago com Nova de Gelo, Ladino com Adaga
  Envenenada. Os talentos que liberavam essas duas viraram talentos de
  reforço (+15% de dano de skill).
- **Mapa com 30 andares.** O balanceamento se ajusta sozinho se o número de
  andares mudar.
- **Cada nível recupera toda a vida.**
- **Correção nas linhas do mapa:** as ligações entre nós ficaram ambíguas e
  passaram a ser linhas retas, uma por ligação.
- **Nó de talento:** oferece os talentos do nível seguinte (antes, no nível
  1, não havia o que oferecer).

## 3. Mudanças de 2026-10-12

- **Menu principal novo:** a tela inicial leva a um menu com **Novo jogo**,
  **Continuar** (só aparece se existe uma run em andamento que pode ser
  retomada), **Glossário** e **Opções**.
- **Seleção de personagem:** aparece só depois de Novo jogo. Tocar num
  personagem abre uma janela com retrato, descrição, atributos e skill
  inicial, com **Voltar** e **Jogar**. Com uma run em andamento, Jogar pede
  confirmação. As descrições das classes são provisórias.
- **Tela de talento:** cada carta mostra só o próprio talento (ícone, nome,
  nível, descrição, skill que libera), sem o aviso "combina com a build"
  (dá para religar em `ModalsSettings`).
- **Linha de visão:** as skills não podem ser miradas através de pedras nem de
  personagens. A Tempestade Elétrica ignora isso (a Bola de Fogo também
  ignorava; você a deixou precisando de visão); skills centradas no
  personagem não precisam. Tudo configurável (cada skill, cada
  terreno e `CombatSettings`).
- **Detalhes de skills e itens:** como nas células, tocar e segurar (ou
  clicar com o botão direito) num botão de skill ou de item mostra a ficha:
  descrição, dano, alcance, área, quem é afetado, recarga (com os talentos
  aplicados), linha de visão, empurrão e se está pronta. O toque rápido
  continua selecionando.
- **Mira livre:** toda skill pode ser mirada em qualquer célula do alcance
  que ela vê, e o destaque cobre todo o alcance, não só os inimigos.
- **Visual das skills e itens (protótipo):** cada skill, e também a Bomba,
  mostra o que faz no tabuleiro:
  - um gesto de quem usa (investida ou brilho);
  - um projétil, quando existe (bola de fogo, adaga, bomba, gancho);
  - um efeito curto em cada célula da área, numa ordem que desenha a forma
    (onda a partir do centro, linha que se afasta, corte que varre).

  O número de dano, a reação, a morte e os orbs de XP esperam o golpe chegar
  na célula. A arte é provisória, desenhada por código. Ela cabe num quadrado
  da largura da célula, então serve para linhas e colunas sem desenhar duas
  vezes. É só aparência: o simulador e a partida de referência deram
  resultados idênticos.
- **Manutenção sem mudar o jogo:**
  - um ponto único para "o jogador atual" e "o grid", no lugar de 19
    buscas espalhadas pela cena;
  - o **validador de conteúdo** (`GridBattle > Validate Content`);
  - blocos de interface compartilhados entre o glossário e a janela do
    personagem;
  - uma dica de tutorial que ainda dizia que a vida só voltava em nós de cura
    foi corrigida.
- **Simulador:** mostra o tabuleiro e a HUD enquanto roda (antes ficava a
  tela "toque para jogar" por cima), e não trava mais em "starting" se algo
  der errado no começo.

### Área de seleção das skills (2026-10-12)

Pedido seu: além da área de efeito, cada skill tem uma **área de seleção** (onde pode ser mirada), como no
Dofus:
- **forma:** círculo, linha (cruz), diagonal, estrela ou quadrado;
- **alcance mínimo e máximo**;
- **alcance modificável ou fixo**;
- **casa alvo:** qualquer, vazia ou com personagem.

Configuração:
- **Guerreiro:** mira em cruz, como o Iop (Golpe e Golpe de Escudo em linha, alcance 1 fixo; Investida com
  Escudo em linha, 1-2).
- **Mago:** círculos de 1-3; a Nova de Gelo continua podendo ser centrada no Mago.
- **Ladino:** linhas (Adaga Envenenada, Linha Venenosa, Gancho de 2-4), estrela na Adaga de Arremesso e
  círculo na Bomba de Fumaça.

O destaque de mira, a IA e a janela de detalhes ("Alcance: 1-3, em linha") seguem a configuração. Tudo é
mudado no asset de cada skill.

### Monetização (2026-10-12)

Implementação da seção 7 do GDD, com as suas decisões: Android e iOS, cobrança pela Google Play e pela App
Store, privacidade só com a estrutura pronta, e as três melhorias (aviso antes do anúncio, oferta depois do
anúncio e botão no menu principal).

- **Anúncio forçado:**
  - **Quando:** ao voltar ao mapa depois de uma batalha ou de um nó sem luta, e na tela de fim de run.
  - **Nunca:** na batalha.
  - **Aviso:** "Pausa para anúncio — começa em 3..." antes de cada um.
  - **Frequência:**
    - nenhum nas 10 primeiras pausas;
    - os 3 primeiros a cada 6 pausas;
    - depois a cada 4;
    - pelo menos 3 minutos entre dois.
- **Anúncio recompensado** (sua decisão, no lugar da recarga): trocar, banir ou pular mostra antes um
  anúncio. O uso só acontece se ele for assistido até o fim e conta no limite da run como sempre. Sem
  anúncio disponível, a ferramenta funciona mesmo assim.
- **Remover anúncios:**
  - **Onde:** menu principal, Opções e tela de derrota, além de abrir sozinha a cada 3 anúncios (1 vez por
    sessão).
  - **O que dá:** os anúncios forçados acabam e as ferramentas deixam de mostrar anúncio, com o **mesmo
    limite** de quem assiste.
  - **Restaurar compras:** botão para trocar de aparelho ou reinstalar.
- **No PC** não há anúncios nem loja, e as ferramentas funcionam sem anúncio.
- **Resetar o jogo:** em build de desenvolvimento e no Editor, *Opções > Desenvolvimento > Resetar o jogo* apaga
  a run, o perfil (com a compra), os contadores de anúncios e as métricas, e volta ao menu.
- **No Editor**, anúncio e loja são de teste (tela preta "Anúncio de teste"). Os SDKs reais (LevelPlay e
  Unity IAP) já estão instalados e entram no celular.
- **Falta você:**
  - criar o app e os blocos de anúncio no LevelPlay e preencher as chaves;
  - criar o produto `remove_ads` nas lojas;
  - criar o perfil de build do iOS;
  - decidir a tela de consentimento e o ATT.

  O passo a passo está em [[habilitar_anuncios_e_compras]].

## 4. Balanceamento (pausado em 2026-10-06)

Problemas que você relatou: batalhas fáceis davam mais XP que normais,
talentos muito repetidos entre classes, lutas finais virando só checagem de
números. As etapas:

| Etapa | O que mudou no jogo | Resultado |
|---|---|---|
| G1. Simulador | Um bot joga runs inteiras sozinho e gera um relatório | Mostrou que nenhuma classe vencia e que a vida perdida entre lutas era o que mais matava |
| G2. XP coerente | O XP de uma batalha vem da dificuldade prevista para o andar | Uma batalha fácil nunca dá mais XP que uma normal, nem uma normal mais que uma difícil, no mesmo andar; o nível 2 chega na primeira batalha |
| Cura a cada nível | Pedido seu | O desgaste deixou de ser o problema; o jogo ficou fácil demais (~50% de vitória para o bot) |
| G4. Empurrar e puxar | Uma skill nova por classe (Golpe de Escudo, Rajada de Vento, Gancho), um inimigo que puxa, o chefe que empurra | Novas opções táticas; o chefe não pode ser empurrado |
| G3. Identidade de classe | 43 talentos de classe: melhoram as skills da própria classe ou dão passivas (por exemplo, bônus por inimigo ao lado, contra alvo envenenado, depois de se mover); talentos genéricos aparecem menos | 64% a 98% dos talentos escolhidos passaram a ser da própria classe; o Ladino deixou de ser muito mais fraco |
| G5/G6. Lutas finais e dificuldade (interrompido) | Mais papéis diferentes e mais terreno no fim da run, força dos inimigos por curva, limites para invocadores, escudo que não se renova, chefe imune a atordoamento | Vitória do bot entre 12% e 18% por classe, perto da meta de "roguelite difícil" |

Os valores atuais são um **ponto de partida**: o balanceamento está pausado
porque o jogo ainda vai mudar. O que falta e como retomar está em
[[plano_balanceamento]].

## 5. Ferramentas para você

| Ferramenta | Para quê | Onde |
|---|---|---|
| Simulador de balanceamento | Jogar runs sozinho e medir vitória, dano, XP, talentos | `GridBattle > Simulation > Run Balance Simulation`; ver [[simulador_balanceamento]] |
| Progressão de XP | Ver em segundos quantos níveis cada caminho do mapa rende | `GridBattle > Talents > Simulate XP Progression` |
| Validador de conteúdo | Achar textos faltando em algum idioma, referências vazias, talentos nunca oferecidos, inimigos que nunca aparecem, telas mal configuradas | `GridBattle > Validate Content` |
| Checagens dos talentos | Conferir se talentos, pools, textos e efeitos estão coerentes | `GridBattle > Talents > Run Checks` |
| Banco de conteúdo | Reconstruir a lista de todos os assets do jogo (acontece sozinho) | `GridBattle > Rebuild Game Database` |
| Batalha rápida | Testar uma batalha sem passar pelo mapa | objeto `Grid` na cena, botão **Reset grid** |
| Visuais provisórios | Redesenhar a arte provisória das skills e criar os visuais que faltam | `GridBattle > Vfx > Generate Placeholder Visuals`; ver [[guia_sistemas]] |
| Monetização | Configurar anúncios e a compra; testar sem loja (marcar a compra, zerar os contadores) | `GridBattle > Monetization`; ver [[monetizacao]] |
| Repetir uma run | Usar sempre a mesma seed | `Run/RunSettings > Debug Seed` |
| Navegação da UI ao vivo | Ver telas e modais abertos | `Window > ZS > UI Navigation Debugger` |

## 6. Como foi verificado

- O jogo compila sem erros e o Console fica limpo.
- Os 14 testes do framework de UI passam.
- As checagens dos talentos passam (7777 verificações no fim do G3).
- Uma **partida de referência** (a batalha de depuração jogada sempre do
  mesmo jeito) dá exatamente o mesmo resultado antes e depois de cada mudança
  que não devia alterar o jogo. Isso prova que refatorações e sistemas novos
  não mudaram o que já existia.
- Nas mudanças de manutenção, o **simulador** jogou os mesmos 180 runs antes e
  depois, com as mesmas seeds, e o resultado saiu idêntico batalha por batalha.
- Os fluxos novos de interface foram testados no jogo rodando, com capturas de
  tela.
- Você jogou e achou o jogo funcional.

## 7. Pendências

- **Fonte pixel com acentos:** a fonte atual só tem letras sem acento
  (decisão de arte).
- **Conteúdo provisório:** inimigos de exemplo, chefe, talentos, ícones e
  visuais das skills existem para os sistemas funcionarem; substitua pelos
  definitivos sem mudar código.
- **Áudio:** falta colocar os clipes.
- **Balanceamento:** pausado; os números atuais são ponto de partida.
- **Monetização:** implementada. Faltam:
  - as chaves do LevelPlay e o produto nas lojas;
  - o perfil de build do iOS;
  - a tela de consentimento e o ATT.

  Ver [[monetizacao]].

## 8. Cuidados ao testar no Editor

- Testes automáticos e o simulador usam o Play Mode: não jogue no Editor
  enquanto eles rodam.
- Testes em Play Mode podem gravar `run.json`, `profile.json` e
  `metrics.jsonl` (seu jogo de verdade). A regra do projeto é guardar uma
  cópia antes e restaurar depois.
- Os arquivos ficam em
  `%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle`. Para
  recomeçar do zero, apague os três.
