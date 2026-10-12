---
tags:
  - projeto
  - resumo
created: 2026-10-12
---
# Resumo das mudanças (2026-10-05 a 2026-10-12)

Um resumo curto do que foi feito no jogo, para entender as mudanças sem ler
os planos inteiros. Detalhes técnicos: [[guia_sistemas]] (como usar cada
sistema pelo editor), [[simulador_balanceamento]], [[plano_implementacao_gdd]],
[[plano_balanceamento]] e `AGENTS.md`.

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
