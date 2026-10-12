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
  personagens. A Bola de Fogo e a Tempestade Elétrica ignoram isso; skills
  centradas no personagem não precisam. Tudo configurável (cada skill, cada
  terreno e `CombatSettings`).
- **Mira livre:** toda skill pode ser mirada em qualquer célula do alcance
  que ela vê, e o destaque cobre todo o alcance, não só os inimigos.
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
- **Conteúdo provisório:** inimigos de exemplo, chefe, talentos e ícones
  existem para os sistemas funcionarem; substitua pelos definitivos sem
  mudar código.
- **Áudio:** falta colocar os clipes.
- **Balanceamento:** pausado; os números atuais são ponto de partida.
- **Anúncios e compras:** fora do escopo por enquanto.

## 8. Cuidados ao testar no Editor

- Testes automáticos e o simulador usam o Play Mode: não jogue no Editor
  enquanto eles rodam.
- Testes em Play Mode podem gravar `run.json`, `profile.json` e
  `metrics.jsonl` (seu jogo de verdade). A regra do projeto é guardar uma
  cópia antes e restaurar depois.
- Os arquivos ficam em
  `%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle`. Para
  recomeçar do zero, apague os três.
