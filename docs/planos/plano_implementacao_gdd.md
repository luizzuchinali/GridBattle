---
tags:
  - plano
  - implementacao
created: 2026-10-05
status: em execução
---
# Plano de implementação do GDD

Implementação do [[GDD]] e dos documentos de `docs/design/`. **Não usa nada de
`docs/propostas/`.** Monetização (anúncios e compra) fica fora por enquanto.

Regras gerais:

- **Tudo configurável pelo editor.** Valores, regras e conteúdo ficam em
  ScriptableObjects (`Create > GridBattle/...`). Cada ❓ dos documentos vira um
  campo com um valor inicial neutro; os valores escolhidos ficam em
  [[valores_padrao_em_aberto]].
- **Padrões do projeto** (ver `AGENTS.md`):
  - docs dos `.cs` em inglês;
  - controllers roteiam e as regras ficam em `GridRules` e nas classes de regra;
  - comunicação por `EventBus`;
  - UI pelo `ZS.UI`;
  - "lógica imediata, visual atrasado";
  - não usar `?.`/`??` com `UnityEngine.Object`.
- **Determinismo:** todo sorteio de lógica usa `RunRandom` (fluxos com
  semente). `UnityEngine.Random` só para efeitos visuais.
- **Textos:** Unity Localization, com tabelas `UI` e `Content` e os idiomas
  `en`, `es` e `pt-BR`.

## Arquitetura

### Base (`Scripts/Core`, `Scripts/Data`)

| Tipo | Papel |
|---|---|
| `GameDefinition` (SO abstrato) | Base de todo conteúdo salvo por referência. `Id` estável (GUID do asset, preenchido no editor). |
| `GameDatabase` (SO, `Resources`) | Registro de todas as `GameDefinition`; `Get<T>(id)`. Reconstruído automaticamente no editor. |
| `GameSettings` (SO, `Resources`) | Raiz das configurações: aponta para os assets de cada sistema. |
| `Rng` / `RunRandom` | PRNG próprio com estado serializável e fluxos nomeados (`ERandomStream`). |
| `SaveSystem` | JSON em `persistentDataPath`, com escrita atômica. Salvamentos separados de run e de perfil. |
| `Loc` | Acesso aos textos localizados e aviso de troca de idioma. |

### Combate

| Tipo | Papel |
|---|---|
| `EAttribute`, `CharacterStats` | Atributos base do config somados aos modificadores dos estados ativos. |
| `StateDefinition` + `StateEffect` (`[SerializeReference]`) | Efeitos configuráveis: atributos, dano e cura periódicos, espinhos, roubo de vida, escudo, restrições e modificadores da run. |
| `StateContainer` | Estados de um personagem. A duração diminui no fim do turno da entidade portadora (regra do GDD). A política de reaplicação é por estado. |
| `CombatResolver` / `DamageCalculator` | Crítico, defesa (fixa ou percentual, configurável), penetração, dano mínimo, escudo, roubo de vida e espinhos. |
| `TurnManager` | Turno global, turno de cada entidade (`EntityTurnStarted/Ended`), espera por animações e por `TurnBlockers` (orbs de XP, escolha de talento, pausa). |
| `BattleController` | Vitória (todos os inimigos eliminados) e derrota (morte do jogador). |

### Sistemas de jogo

| Sistema | Peças principais |
|---|---|
| Skills | `SkillDefinition` com dados: tipo, dano, forma e tamanho de área, alcance (Manhattan), cooldown em ações e efeitos. Modo de mira no `PlayerCharacterController`. Barra com 6 slots. |
| Terreno | `TerrainDefinition` (obstáculo, perigo, bônus): bloqueio e estados aplicados. Visual na `Cell`. Geração por semente e profundidade, com garantia de conectividade. |
| Papéis de inimigos | `EnemyRoleDefinition` (ícone, nome e fator de ameaça) e novas `EnemyAction`: manter distância, aplicar estado, curar ou fortalecer aliado e invocar. Memória de cooldown por inimigo. |
| Consumíveis | `ConsumableDefinition` (efeitos e mira opcional) e inventário com limite. Não consome a ação; no máximo um por turno. |
| Run e mapa | `RunState` serializável, `RunManager`, `MapGenerator` (andares, nós e conexões), `BattleGenerator` (orçamento de ameaça por profundidade, faixa de dificuldade, pool por profundidade, escala de atributos), `BossEncounter` e nós de cura, talento (com custo) e consumível. |
| XP e talentos | `TalentDefinition` (classes, nível, pré-requisitos, tags de sinergia, ranks, estados, skill). Oferta ponderada por sinergia, com reroll, banimento e pular. A pausa da escolha começa quando o orb leva a barra ao limiar. Nível máximo. |
| Perfil e meta | `ProfileState`: batalhas vencidas, liberação de classes, glossário (talentos escolhidos e inimigos enfrentados), recordes por classe, dicas vistas e opções. |
| Interface | HUD (XP e profundidade, menu à direita, barra de itens, barra de skills), janela de detalhes (toque longo ou clique direito), mapa com prévia, escolha de talento, glossário, pausa, opções, fim de run, vitória e dicas. |
| Áudio | `AudioLibrary` (SFX por evento e música por contexto) e volumes nas opções. Sem clipes por enquanto. |

## Fases

1. Localization e base: definições, banco, configurações, RNG, atributos, estados, combate, turnos e bloqueios.
2. Skills, inimigos e papéis.
3. Terreno e consumíveis.
4. Run, mapa, geração de batalhas e salvamento.
5. XP, talentos e nó de talento.
6. Perfil, meta-progressão, glossário (dados) e dicas.
7. Interface.
8. Áudio, conteúdo, textos e verificação de ponta a ponta.

Ao fim, `AGENTS.md`, `docs/design/sistema_combate.md` e o status dos
documentos de design são atualizados.

## Estado atual (2026-10-05, ~01h)

Nada foi commitado.

### Concluído e verificado
- **Unity Localization 1.5.13** com os idiomas `en`, `es` e `pt-BR` e as
  tabelas `UI` e `Content` (`Assets/Application/Localization`), mais o
  Newtonsoft JSON no `manifest.json`.
- **Base de dados** (`Scripts/Data`):
  - `GameDefinition`, `DisplayableDefinition`, `GameDatabase` e `GameSettings`
    (estes dois em `Settings/Resources`);
  - `GameDatabaseBuilder` (editor) e `SubclassPicker`.
- **Núcleo** (`Scripts/Core`): `Loc`, `SaveSystem` e `Randomness`
  (`Rng`, `RunRandom`, `GameRandom`).
- **Atributos, estados e combate:**
  - `Gameplay/Stats`, `Gameplay/States` (efeitos e `StateContainer`),
    `Gameplay/Combat` (`CombatSettings`, `DamageCalculator`, `CombatResolver`);
  - `Rules/GridDistance`;
  - `Character` e `CharacterConfig` com os novos atributos e estados iniciais;
  - asset `Settings/Combat/CombatSettings.asset`.
- **Turnos:**
  - `TurnManager` reescrito: turno global, turno de cada entidade e
    `TurnBlockers`;
  - `BattleController` adicionado ao GameObject `Grid` na cena;
  - `XpRewardSystem` segura o turno quando o XP vai subir de nível ou é o da
    última batalha.
- **Modelo de salvamento** da run: `Gameplay/Run/RunState.cs`.
- **Ferramentas de editor:** `LocalizationTableTool` (textos) e
  `PlaceholderIcons` (ícones provisórios).
- **Estados criados** (`Settings/States`): Fraquejado, Armadura de Espinhos,
  Roubo de Vida, Regeneração, Escudo e Envenenado.
- **Verificação:** com os dados antigos, a partida determinística ficou
  idêntica à de antes. Para isso, o alcance do FireSkull foi forçado a 1 só
  durante o teste.

### Em andamento (agentes Sonnet), conferir ao retomar
- **Skills:**
  - `Gameplay/Entities/Skills/**`: `SkillDefinition` com dados, áreas,
    alcance, cooldowns e `SkillSettings`;
  - mira no `PlayerCharacterController`;
  - destaques na `Cell`;
  - as 9 skills da tabela, com o Golpe no Knight.
- **Inimigos:**
  - papéis (`Gameplay/Entities/Roles`);
  - novas `EnemyAction` (KeepDistance, ApplyStateToTarget, HealAlly,
    BuffAlly, Summon), memória de IA e comportamentos por papel;
  - ícones sobre o inimigo, inimigos provisórios e `GridController.SpawnEnemy`.
- **Meta:**
  - `Gameplay/Meta/**`: perfil (`profile.json`), liberação de classes
    (`PlayerCharacterConfig.battlesToUnlock` = 0/15/30), glossário, recordes,
    opções, dicas (`TutorialTipDefinition`) e métricas locais.
- **Áudio:**
  - `Managers/Audio/**`: `AudioLibrary` (sem clipes), `AudioManager`
    autoinicializado e ganchos de eventos;
  - `EntityMoveStartedEvent` no `GridMovementAnimator`.

**Terminados e verificados pelos agentes** (falta minha revisão):

- **Skills:**
  - Entregue:
    - 9 skills em `Settings/Skills/<Classe>/`, com o Golpe (`Strike`) no Knight;
    - `SkillSettings`;
    - eventos `SkillUsed`, `SkillSelectionChanged`, `SkillCooldownsChanged` e `SkillListChanged`;
    - cooldown decrementado em `Character.EndTurn()`;
    - `Character.SetSkills`.
  - Pendente:
    - barra de skills na UI;
    - prévia da área antes do toque;
    - skills centradas no próprio personagem exigem tocar na própria célula.
  - Conferir: o A/B de andar/atacar divergiu por causa da mudança do FireSkull
    (papel e comportamento de atirador). Refazer com o comportamento antigo
    forçado durante o teste.
- **Meta:**
  - Entregue:
    - `ProfileService`, `GlossaryService`, `TutorialService`, `MetricsRecorder`
      e `MetaBootstrap` (autoinicializado);
    - 9 dicas em `Settings/Tutorial`;
    - `MetaSettings`;
    - `battlesToUnlock` com 0, 15 e 30.
  - Falta ligar:
    - `RegisterRunEnded`, `RegisterTalentChosen`/`Offered`, `RegisterRunGivenUp`,
      as outras dicas (`TutorialService.Notify`) e `MetricsRecorder.Record` (run,
      talentos e UI);
    - travar a escolha de classe por `IsClassUnlocked`, com o progresso "7/15";
    - `OptionsChangedEvent` → volumes do áudio.

- **Inimigos:**
  - Papéis (`Settings/AI/Roles`). Novas ações em `Settings/AI/Actions`:
    KeepDistance, ApplyWeakened, HealAlly, BuffAllyShield e SummonRat.
    Comportamentos Ranged, Support, Summoner e Controller.
  - `EnemyMemory`, `GridController.SpawnEnemy`, `CharacterFactory.Spawn` com
    escala e `EnemySummonedEvent`.
  - Ícones de papel e estado sobre o inimigo (`EnemyIconsView`).
  - Estado novo `Armored`.
  - Inimigos provisórios: GoblinShaman, RatQueen, HexingEye, ArmoredGoblin,
    RegeneratingSlime e ThornyRat.
  - Pendente:
    - conferir os ícones visualmente;
    - colocar `KeepDistance` antes de `BasicAttack` em `Ranged.asset`, para o
      atirador recuar (hoje ele só se aproxima);
    - conciliar `SpawnEnemy`/`SummonAction` com as células bloqueadas pelo
      terreno.

- **Áudio:**
  - Entregue: `Managers/Audio` com `ESfx` (31 efeitos), `EMusicContext`,
    `AudioLibrary` (sem clipes), `AudioManager.Play`, `SetMusicContext`,
    `PauseMusic` e `SetTalentPause`; ganchos automáticos de combate, XP e
    música de batalha.
  - Os outros módulos devem chamar `AudioManager.Play` para:
    - skill: SkillCast;
    - terreno: HazardCell e BonusCell;
    - talentos: TalentOfferOpen, TalentChosen, Reroll, Ban e Skip;
    - mapa: NodeSelect, NodeConfirm e a música Map/Boss (Boss antes do
      `GridInitializedEvent`);
    - consumíveis: ConsumableGained e ConsumableUse;
    - UI: ButtonTap, WindowOpen, WindowClose e ScreenTransition;
    - fim de run: RunVictory, RunDefeat e ClassUnlocked.

**Integração feita por mim:**
- Volumes do perfil aplicados ao áudio, na inicialização e no
  `OptionsChangedEvent`.
- `Ranged.asset` passou para UseSkills → KeepDistance → BasicAttack →
  ChaseTarget, para o atirador recuar.
- Partida determinística refeita com tudo integrado: **idêntica** à de antes,
  com o FireSkull antigo (alcance 1 e ChaseAndAttack) forçado só durante o
  teste.
- `profile.json` e `metrics.jsonl` de teste apagados. **Os testes em Play Mode
  gravam no perfil real**: apague os dois arquivos em
  `%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle` depois dos
  testes.

**Leva B (2026-10-05, manhã): concluída, revisada e integrada.**
- **Terreno** (`Gameplay/Terrain`, `Settings/Terrain`):
  - obstáculo, fogo, pântano venenoso, santuário e pedra de proteção;
  - gerador determinístico com conectividade;
  - caminho em volta dos obstáculos;
  - `GridController.Size`, `IsWalkable` e `ApplyTerrain`.
- **Consumíveis** (`Gameplay/Consumables`, `Settings/Consumables`):
  - poção, bomba e tônico;
  - inventário sobre a lista da run;
  - não consomem a ação, um por turno.

**Revisão (correções minhas):**
- O cooldown das habilidades dos inimigos segue a regra das skills ("N ações
  entre usos"). Os valores dos assets foram ajustados para manter o ritmo.
- Os ícones sobre os inimigos são 8×8 nativos, com escala só inteira (pixel
  perfect).
- `ETurnOwner.None` e `BattleDecidedEvent`: ninguém age depois que a batalha
  está decidida, enquanto ainda rolam animações, orbs e escolha de talento.
- Os estados vindos do terreno no fim do turno contam o turno corrente (regra
  do GDD). Pântano e santuário passaram a durar 3 turnos.
- A partida determinística refeita após cada leva continua **idêntica**.

**Leva C:**
- **HUD: concluído e revisado.**
  - Layout: profundidade à esquerda, XP no centro, menu à direita, grid, barra
    de itens (redonda) e barra de skills com cooldown.
  - Janela de detalhes por toque longo (0,45 s) ou clique direito: atributos,
    skills, estados com duração e terreno. Fecha só no X e pausa o fluxo de
    turno.
  - Destaques recalculados por evento: 18 vezes em 3471 frames no teste.
  - Corrigido: "dano crítico" aparecia sem chance de crítico.
  - **Pendência de arte:** a fonte `ThaleahFat` tem só ASCII (106
    caracteres), sem acentos, ç, ñ, ¿ e ¡. Em pt-BR e es esses caracteres saem
    na fonte reserva. É preciso uma fonte pixel com Latin-1 (decisão do
    usuário).
**Leva D (2026-10-05, tarde): concluída e revisada.**
- **Talentos** (`Gameplay/Talents`, `Settings/Talents`,
  `Settings/Progression`):
  - curva de XP global (`ProgressionSettings`, 50/+25) e nível máximo 30;
  - ofertas determinísticas com sinergia, reroll, banir e pular;
  - pausa durante a escolha, nó de talento e `IRunPlayerModifier`;
  - 23 talentos provisórios;
  - XP por profundidade ajustado (0,6) para ~1 nível por batalha difícil.
- **Telas** (`UI/Screens/{Map,RunEnd,Victory}`,
  `UI/Overlays/{ConsumableOffer,Confirm}`, `ScreensLayer`):
  - fluxo completo no `GameFlowController`;
  - menu principal com classes bloqueadas e "Continuar".
- **Correções e acréscimos meus:**
  - removido o último acesso à curva de XP pelo config de classe;
  - `MapGenerationSettings.diversifyBattleChoices` (escolha real de
    dificuldade).

**Leva E: concluída e revisada.**
- Modais: escolha de talento, glossário, opções, pausa e dicas
  (`ModalsSettings`).
- Correções minhas:
  - linhas do mapa como traços diretos (antes um nó parecia ligado a nós de
    outros caminhos);
  - o nó de talento oferece os talentos do nível + 1 (no nível 1 não havia
    opções);
  - dica "primeira skill" ligada à barra de skills;
  - botão de menu do HUD com `OnClick`;
  - janela de detalhes com raiz absoluta.
- Decisão do usuário: toda classe começa com uma skill (Mago: Nova de Gelo;
  Ladino: Adaga Envenenada); Criomancia e Mãos Ligeiras viraram reforço
  (+15% de dano de skill); Golpe reajustado pelo usuário (dano 10,
  perpendicular 3).

**Leva F:**
- `AGENTS.md` reescrito;
- documentos de design atualizados ao estado implementado;
- guia em `docs/projeto/guia_sistemas.md`;
- verificação de ponta a ponta em andamento.

- **Run, mapa, geração de batalhas e salvamento:** concluído e revisado (sem
  correções). Partida determinística idêntica.
  - Entregue:
    - `Managers/RunManager.cs`;
    - `Gameplay/Map` (MapGenerator, BattleGenerator/ThreatCalculator,
      validador, regras);
    - `Gameplay/Run` (`RunSettings`, `RunPlayerHooks`, que é o ponto de
      extensão dos talentos, `RunSummary`, aplicador do estado do jogador);
    - `GridController.InitializeBattle`/`ClearBoard`/`CaptureBattle`/
      `RestoreBattle`;
    - chefe provisório GoblinKing com dois GoblinGuard;
    - `RunManager` no GameObject `GameStateManager` da cena.
  - Salvamento: no início de cada vez do jogador, antes dos efeitos de início
    de turno. A retomada repete o turno. A oferta de talento no meio da
    batalha deve ser determinística: `Derive(Talents, nível, reroll)`.
  - Valores neutros: 11 andares, cerca de 31 nós, orçamento 6 + 1,5/prof., +6%
    de vida e +3% de dano por profundidade. Com a curva de XP atual, a run
    chega por volta do nível 5: **ajustar curva e XP junto com os talentos**.
  - Mudança fora da posse do agente: `Cell.ShowFloatingText` agora usa
    `.AddTo(...)` no tween do texto de dano (evita erros ao limpar o
    tabuleiro).
  - `GameFlowController` ainda volta ao menu quando o jogador morre. Nenhuma
    tela de mapa existe ainda (para depurar sem UI: `RunSettings.autoEnterFirstNode`).
  - **Ao retomar:**
    1. Revisar o código da run.
    2. Rodar a partida determinística de novo.
    3. Seguir para a leva D.
  - Depois: leva D (talentos, curva de XP global e nível máximo), telas
    (mapa, fim de run, vitória), modais (talento, glossário, opções, pausa,
    dicas, menu principal com classes bloqueadas), textos e verificação final.
- Testes em Play Mode gravam `run.json`, `profile.json` e `metrics.jsonl` em
  `%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle`. Apague os
  três depois dos testes.

### Próximas levas
- **B:**
  - **Terreno:** `TerrainDefinition`, visual na `Cell`, `GridRules`
    (obstáculos), efeitos por gatilho e gerador com conectividade.
  - **Consumíveis:** definição, inventário, uso sem consumir a ação (um por
    turno) e mira.
- **C:**
  - **Geração:** `MapGenerator` (andares, nós e conexões) e `BattleGenerator`
    (orçamento de ameaça por profundidade e dificuldade, pool por
    profundidade, escala de atributos, posições, chefe).
  - **Run:** `RunManager` (início com seed, nós, cura, nó de talento com
    custo, nó de consumível, vitória e derrota), `GridController`
    (`InitializeBattle`, `Capture`/`Restore`) e salvamento automático no
    início de cada vez do jogador.
- **D:**
  - **Talentos:** `TalentDefinition`, ofertas ponderadas por sinergia
    (reroll, banir, pular), nível máximo, escolha com pausa ao subir de nível
    e talentos que liberam as skills.
  - **Curva de XP fora dos personagens (pedido do usuário):**
    - `baseXpToLevelUp` e `xpToLevelUpGrowthPerLevel` **não devem ficar em
      `PlayerCharacterConfig`**;
    - movê-los para uma configuração global de progressão (asset
      `IGameSettings`, junto com o nível máximo), mantendo os valores atuais
      50 e +25;
    - remover os campos dos configs de classe;
    - atualizar `PlayerCharacter.XpToNextLevel` e o `xp_e_niveis.md`.
- **E, interface:**
  - **HUD:** barra de skills, barra de itens, profundidade, menu à direita e
    detalhes da entidade por toque longo.
  - **Telas:** mapa com prévia, fim de run e vitória.
  - **Modais e menus:** escolha de talento, glossário, opções, pausa
    (continuar, glossário, opções, desistir), dicas e menu principal com as
    classes bloqueadas.
- **F:**
  - textos restantes e conteúdo;
  - verificação de ponta a ponta;
  - `AGENTS.md`, `sistema_combate.md` e [[valores_padrao_em_aberto]].
  - **Guia dos sistemas (pedido do usuário, depois de tudo implementado):** um
    `.md` em `docs/projeto/` que explica a estrutura criada (pastas, sistemas,
    assets e fluxos) e como usar cada coisa pelo editor. Por exemplo: criar
    inimigo, skill, estado, talento, consumível, terreno ou dica; ajustar mapa,
    batalhas, XP e combate; adicionar textos e sons; salvamentos e testes.

### Infraestrutura dos agentes
- Briefing e scripts ficam no scratchpad da sessão (`scratchpad/gdd`):
  `AGENT_BRIEFING.md`, `tools/compile.sh`, `tools/runplay.sh` e
  `tools/unity.sh`. Os scripts usam um lock compartilhado do Editor.
- O `runplay.sh` muda temporariamente duas EditorPrefs durante os testes
  (`kAutoRefreshMode=2` e `ScriptCompilationDuringPlay=1`) e restaura no fim.
  Se uma sessão for interrompida no meio de um teste, conferir essas
  preferências em `Preferences > Asset Pipeline / General` e apagar
  `tools/unity.lock`, caso tenha ficado.
