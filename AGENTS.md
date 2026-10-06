# AGENTS.md — GridBattle

Guia para agentes (e pessoas) trabalhando neste repositório: estrutura,
padrões, fluxos principais e histórico de refatorações.

> **Mantenha este arquivo atualizado.** Toda mudança de estrutura (pastas,
> namespaces, novos sistemas, novos tipos de asset, convenções) deve ser
> refletida aqui na mesma alteração, incluindo uma entrada em
> [Histórico de refatorações](#histórico-de-refatorações).

## Visão geral

RPG tático por turnos em grid 2D (roguelite mobile, retrato). O jogador escolhe
uma classe e percorre um **mapa de nós** (batalha, cura, talento, consumível e
chefe final). As batalhas acontecem num grid com terreno. Ao subir de nível, o
jogador escolhe **talentos**, que concedem estados e liberam skills.

| Item | Valor |
|---|---|
| Engine | Unity **6000.6.0f1** |
| Render | URP 2D + Pixel Perfect Camera (PPU **100**, `GameConfigManager.Ppu`), referência 216×384 |
| UI | UI Toolkit (UXML/USS) via `PanelRenderer`; navegação pelo framework `ZS.UI` (`Assets/ZS/UI`) |
| Textos | Unity Localization 1.5 (tabelas `UI` e `Content`; idiomas `en`, `es`, `pt-BR`) |
| Salvamento | JSON (Newtonsoft) em `Application.persistentDataPath`: `run.json`, `profile.json`, `metrics.jsonl` |
| Tweens | LitMotion (`LMotion`) |
| Input | Input System (`InputSystemUIInputModule`) + `IPointer*Handler` nas células |
| Texto no mundo | TextMeshPro (dano flutuante das células) |
| Assemblies | Jogo: `Assembly-CSharp`/`-Editor` (sem `.asmdef`). Framework: `ZS.UI`, `ZS.UI.Editor`, `ZS.UI.Tests`, `ZS.UI.Samples` |
| Testes | EditMode do framework (`ZS.UI.Tests`); verificações do jogo por scripts no Editor (ver [Verificação](#verificação)) e `GridBattle > Talents > Run Checks` |
| Cena | `Assets/Scenes/SampleScene.unity` (única) |

Documentação de design (Obsidian, em português) em `docs/`: o [[GDD]] e os
subdocumentos de `docs/design/`. A referência do combate implementado é
`docs/design/sistema_combate.md`, que deve ficar em sincronia com o código.
O guia de uso dos sistemas pelo editor é `docs/projeto/guia_sistemas.md`. O plano e os
valores padrão das perguntas em aberto ficam em `docs/planos/`. **Não use nada
de `docs/propostas/`** (são rascunhos não aprovados).

## Estrutura de pastas

```
Assets/
├── Application/                    ← tudo que é do jogo
│   ├── Art/
│   │   ├── Animations/{Enemies,PlayerCharacters}/<Nome>/   AnimatorController + clips por personagem
│   │   └── Sprites/
│   │       ├── {Enemies,PlayerCharacters}/, Characters.png, Skills.png, UI.png
│   │       └── Placeholders/<Sistema>/                     ícones provisórios gerados (PlaceholderIcons)
│   ├── Localization/                                       LocalizationSettings, Locales (en, es, pt-BR), Tables (UI, Content)
│   ├── Prefabs/
│   │   ├── Cell.prefab                                     célula (destaques, terreno, barra de vida, texto de dano)
│   │   └── Entities/
│   │       ├── Character.prefab                            template base (SpriteRenderer + Animator + CharacterView)
│   │       ├── Enemy.prefab                                variant: + Enemy + EnemyController + EnemyIconsView (papel/estados)
│   │       └── PlayerCharacter.prefab                      variant: + PlayerCharacter + PlayerCharacterController
│   ├── Settings/                   ← dados de jogo (ScriptableObjects), editáveis pelo Inspector
│   │   ├── Resources/                                      GameDatabase.asset e GameSettings.asset (gerados pelo editor)
│   │   ├── Characters/{Players,Enemies}/                   PlayerCharacterConfig / EnemyConfig (inclui inimigos provisórios e o chefe)
│   │   ├── AI/                                             EnemyBehavior (ChaseAndAttack, Ranged, Support, Summoner, Controller)
│   │   ├── AI/Actions/, AI/Roles/                          EnemyAction e EnemyRoleDefinition
│   │   ├── Skills/<Classe>/, Skills/Enemies/, Skills/SkillSettings   SkillDefinition (tabela 6.2; Enemies = skills dos inimigos e do chefe)
│   │   ├── States/, States/Talents/                        StateDefinition (States/: estados de combate, inclusive Queimando, Veneno letal, Congelado, Atordoado, Exposto; States/Talents/: um estado por talento, genérico ou de classe)
│   │   ├── Talents/{Shared,Warrior,Mage,Rogue,Tags}/       TalentDefinition (talentos de classe com modificadores de skill e passivas, G3), SynergyTagDefinition, TalentOfferSettings (peso dos genéricos)
│   │   ├── Terrain/                                        TerrainDefinition + TerrainGenerationSettings
│   │   ├── Consumables/                                    ConsumableDefinition + ConsumableSettings
│   │   ├── Tutorial/                                       TutorialTipDefinition (uma por gatilho)
│   │   ├── Combat/, Progression/, Run/, Map/               CombatSettings, ProgressionSettings, RunSettings, Map/{Map,Battle}GenerationSettings
│   │   ├── Simulation/                                     BalanceSimulationSettings (simulador de balanceamento)
│   │   ├── Meta/, Audio/, Input/, Hud/, Screens/, Modals/  MetaSettings, AudioLibrary, GameplayInputSettings, HudSettings, ScreensSettings, ModalsSettings (views dos modais)
│   │   ├── Encounters/DefaultEncounter.asset               inimigos da batalha de depuração (grid montado no editor)
│   │   ├── Vfx/XpOrbsVfxSettings.asset, Movement/GridMovementSettings.asset
│   ├── UI/                         ← UI Toolkit organizada por feature
│   │   ├── Theme/                                          GridBattle.tss, Tokens.uss, Base.uss, Components.uss
│   │   ├── Settings/                                       PanelSettings, WorldPanelSettings, PanelTextSettings
│   │   ├── Layers/*.asset                                  UILayerDefinition (uma por painel)
│   │   ├── Screens/{Start,MainMenu,Game,Map,RunEnd,Victory}/   <Tela>.uxml + .uss + <Tela>View.asset (ViewDefinition)
│   │   ├── Overlays/{Menu,EntityDetails,ConsumableOffer,Confirm,TalentChoice,Glossary,Options,TutorialTip}/
│   │   ├── Background/, Transitions/, Animations/
│   └── Scripts/                    ← ver tabela abaixo
├── ZS/UI/                          ← framework reutilizável de navegação (ver Assets/ZS/UI/README.md)
├── AddressableAssetsData/                                  gerado pelo Unity Localization (tabelas são Addressables)
├── Scenes/SampleScene.unity
├── Resources/Fonts & Materials/                            fontes TMP e a fonte pixel da UI (ThaleahFat)
├── Settings/                                               URP, Build Profiles, Volume
docs/                                                       vault Obsidian (GDD, design, planos, projeto)
```

### Scripts (`Assets/Application/Scripts`)

O namespace segue a pasta: `GridBattle.<Pasta>[.<Subpasta>]`.

| Pasta | Namespace | Conteúdo |
|---|---|---|
| `/` | `GridBattle` | `EventBus` (pub/sub estático) |
| `Core/` | `GridBattle.Core` | `Loc` (textos localizados), `SaveSystem` (JSON atômico) |
| `Core/Randomness/` | `…Core.Randomness` | `Rng` (SplitMix64), `RunRandom` (fluxos `ERandomStream` + `Derive`), `GameRandom` (fluxos da run ativa) |
| `Data/` | `GridBattle.Data` | `GameDefinition` (id = GUID), `DisplayableDefinition` (nome/descrição localizados + ícone), `GameDatabase`, `GameSettings` + `IGameSettings`, `SubclassPicker` |
| `Editor/` | `GridBattle.Editor[.…]` | `GameDatabaseBuilder`, `GridControllerEditor`, `Drawers/SubclassPickerDrawer`, `Localization/LocalizationTableTool`, `Tools/PlaceholderIcons`, `Talents/{TalentChecks,XpProgressionSimulator}`, `Simulation/BalanceSimulation` (menu e Play Mode do simulador) |
| `Managers/` | `GridBattle.Managers` | `GameStateManager` (classes jogáveis), `RunManager` (a run), `TurnManager` (turnos), `GameConfigManager` |
| `Managers/Audio/` | `…Managers.Audio` | `AudioManager` (autoinicializado), `AudioLibrary`, `ESfx`, `EMusicContext`, `AudioEventHooks` |
| `Gameplay/` | `GridBattle.Gameplay` | `GridController`, `Cell`, `BattleController` (vitória/derrota), `CellContentHealthBar`, `EncounterConfig`, `ETurnOwner`, `GameplayInput(Settings)` |
| `Gameplay/Stats/` | `…Gameplay.Stats` | `EAttribute`, `AttributeModifier`, `CharacterStats` |
| `Gameplay/States/` (+`Effects/`) | `…Gameplay.States[.Effects]` | `StateDefinition`, `StateEffect` (ganchos: atributos, turnos, dano causado e recebido, bônus de dano condicional, modificadores de skill) e efeitos (`SkillModifierEffect`, `ConditionalDamageEffect`, `OnKillEffect`, `ApplyStateOnHitEffect`, ...), `StateGrant`, `StateInstance`, `StateContainer` |
| `Gameplay/Combat/` | `…Gameplay.Combat` | `CombatSettings`, `DamageCalculator`, `CombatResolver` (`Adjust`, `PredictDamage`), `DamageContext` (+`DamageAdjustment`, `EDamageKindMask`), `HitResult`, `DisplacementResolver` (aplica e prevê empurrões e puxões) |
| `Gameplay/Turns/` | `…Gameplay.Turns` | `TurnBlockers` |
| `Gameplay/Rules/` | `…Gameplay.Rules` | `GridRules`, `GridDistance`, `Displacement` (regras puras de empurrar e puxar: direção, caminho, colisão) |
| `Gameplay/Movement/` | `…Gameplay.Movement` | `GridMovementAnimator`, `GridEffectsAnimator`, `GridMovementSettings` |
| `Gameplay/Entities/` | `…Gameplay.Entities` | `GridEntity` → `Character` → `PlayerCharacter` / `Enemy`; `CharacterView`, `EnemyIconsView`, `CharacterFactory`, `CharacterScaling`, `ECharacter` |
| `Gameplay/Entities/Configs/` | `…Gameplay.Entities` ⚠️ | `CharacterConfig`, `EnemyConfig`, `PlayerCharacterConfig` (**exceção**: namespace sem `Configs`) |
| `Gameplay/Entities/Skills/` | `…Entities.Skills` | `SkillDefinition`, `EffectiveSkill` (+`SkillModifiers`: os números de uma skill para um lançador), `SkillEffect` (+`DisplaceSkillEffect`), `SkillArea`, `SkillTargeting`, `SkillCooldowns`, `SkillSettings` |
| `Gameplay/Entities/Roles/` | `…Entities.Roles` | `EnemyRoleDefinition` |
| `Gameplay/Entities/Interfaces/` | `…Entities.Interfaces` | `IDamageReceiver`, `IAttacker`, `IWalker` |
| `Gameplay/Controllers/` | `…Gameplay.Controllers` | `CharacterControllerBase<T>`, `PlayerCharacterController` (toque, skills, itens, destaques), `EnemyController` (IA + memória) |
| `Gameplay/AI/` (+`Actions/`) | `…Gameplay.AI[.Actions]` | `EnemyBehavior`, `EnemyAction`, `EnemyMemory`, `EnemyTurnContext`; ações (`UseSkills`, `BasicAttack`, `ChaseTarget`, `KeepDistance`, `ApplyStateToTarget`, `HealAlly`, `BuffAlly`, `Summon`); `DisplacementScoring` (valor de um empurrão/puxão para a IA, pesos em `UseSkills`) |
| `Gameplay/Terrain/` | `…Gameplay.Terrain` | `TerrainDefinition`, `TerrainGenerator`, `TerrainEffects`, `TerrainGenerationSettings` |
| `Gameplay/Consumables/` | `…Gameplay.Consumables` | `ConsumableDefinition`, `ConsumableEffect`, `ConsumableInventory`, `ConsumableRules`, `ConsumableExecutor`, `ConsumableGrant`, `ConsumableSettings` |
| `Gameplay/Progression/` | `…Gameplay.Progression` | `ProgressionSettings` (curva de XP, nível máximo), `XpRewardSystem`, `XpPacket` |
| `Gameplay/Talents/` | `…Gameplay.Talents` | `TalentDefinition`, `SynergyTagDefinition`, `TalentOfferSettings` (inclui `sharedPoolWeightMultiplier`), `TalentOfferGenerator`, `TalentRules`, `TalentSession`, `TalentService`, `TalentApplier`, `TalentRunModifier`, `TalentBootstrap` |
| `Gameplay/Map/` | `…Gameplay.Map` | `MapGenerator`, `MapValidator`, `MapRules`, `BattleGenerator` (+`ThreatCalculator`), `MapGenerationSettings`, `BattleGenerationSettings` |
| `Gameplay/Run/` | `…Gameplay.Run` | `RunState` (modelo salvo), `RunSettings`, `RunSummary`, `PlayerRunStateApplier`, `RunPlayerHooks` (+`IRunPlayerModifier`), `StateSnapshots` |
| `Gameplay/Simulation/` | `…Gameplay.Simulation` | simulador de balanceamento: `SimMode` (modo sem animações, sons, UI nem salvamento), `SimulationRunner`, bots (`BattleBot`, `MapPolicy`, `TalentPolicy`), `SimulationCollector`, `SimulationReport`, `SimulationOptions`, `BalanceSimulationSettings` |
| `Gameplay/Meta/` | `…Gameplay.Meta` | `ProfileService`/`ProfileState`, `GlossaryService`, `TutorialService` + `TutorialTipDefinition`, `MetricsRecorder`, `MetaSettings`, `MetaBootstrap` |
| `Gameplay/Events/` | `…Gameplay.Events` | eventos de gameplay |
| `UI/` | `GridBattle.UI` | `XpBarView`, `PixelPerfectPanelScale`, `UiExtensions` (`OnClick`, `SimulateClick`) |
| `UI/Flow/` | `…UI.Flow` | `GameFlowController` (eventos → navegação) |
| `UI/Screens/` | `…UI.Screens` | controladores das telas (Start, MainMenu, Game, Map, RunEnd, Victory), `ScreensSettings` |
| `UI/Hud/` | `…UI.Hud` | `SkillBarView`, `ItemBarView`, `DepthView`, `EntityDetailsModel`, `HudText`, `HudSettings` |
| `UI/Map/` | `…UI.Map` | `MapGraphView`, `MapLinesElement`, `MapHudView`, `NodePreviewView` |
| `UI/Overlays/` | `…UI.Overlays` | controladores dos modais (menu/pausa, detalhes, oferta de consumível, confirmação, talento, glossário, opções, dicas), `ModalsSettings` |
| `UI/Background/`, `UI/Vfx/`, `UI/Events/` | `…UI.*` | fundo, orbs de XP, eventos de UI |

### Objetos da cena

| GameObject | Componentes |
|---|---|
| `Main Camera` | `PixelPerfectCamera`, `PixelPerfectPanelScale`, `Physics2DRaycaster` |
| `GameStateManager` | `GameStateManager` (singleton, `DontDestroyOnLoad`), `RunManager` |
| `Grid` | `GridController` (`[ExecuteAlways]`), `TurnManager`, `BattleController`; células `Cell_x_y` são filhas |
| `XpRewardSystem` | `XpRewardSystem` |
| `UIManager` | `UIRoot`, `GameFlowController`; filhos (cada um `PanelRenderer` + `UILayer`): `BackgroundLayer` (999 → -1), `StartScreenLayer` (0), `MainMenuLayer` (1, + Animator), `GameScreenLayer` (2, + `XpOrbsVfx`), `ScreensLayer` (3, telas `Instantiate`: mapa, fim de run, vitória), `Overlays/MenuOverlayLayer` (5, camada dos modais), `TransitionLayer` (999) |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` |

Serviços sem objeto na cena (autoinicializados por
`[RuntimeInitializeOnLoadMethod]`): `AudioManager`, `MetaBootstrap` (perfil,
dicas, métricas) e `TalentBootstrap` (talentos).

## Fluxos principais

```
Telas (GameFlowController + ZS.UI)
  Start → MainMenu (classes bloqueadas com progresso, Continuar, Glossário, Opções)
  escolha de classe → RunManager.StartNewRun → MapOpenedEvent → tela do Mapa
  nó escolhido (SelectNode = prévia; EnterNode = confirma)
     batalha/chefe → BattleStartedEvent → tela Game (HUD)  → vitória → mapa | chefe → Vitória
     cura / consumível / talento → resolvidos no mapa (modais) → mapa
  derrota ou desistência → RunEndedEvent → tela de fim de run → MainMenu
  pedidos de tela durante uma transição viram "tela alvo" e são aplicados depois

Run (RunManager)
  StartNewRun: semente (RunSettings.debugSeed ou aleatória) → MapGenerator (mapa + BattleSpec de cada
    batalha: inimigos, terreno, XP) → RunState → salva run.json
  geração: cada batalha lê os assets de balanceamento na profundidade de balanceamento do nó
    (MapGenerationSettings.GetBalanceDepth: andares de referência esticados para o tamanho do mapa);
    XP da batalha = orçamento de ameaça (profundidade × dificuldade) × XP por ponto × crescimento por
    profundidade × fator do tamanho da run, dividido entre os inimigos pela ameaça (fácil < normal < difícil)
  vida, nível, XP, skills, consumíveis e talentos persistem entre nós (PlayerRunState)
  batalha: GridController.InitializeBattle(spec, classe, PlayerRunState) → terreno → jogador → inimigos
  salvamento: depois de cada nó e no início de cada vez do jogador (antes dos efeitos de início de turno;
    carregar repete o turno com os mesmos sorteios). ContinueRun retoma mapa, nó aberto ou batalha.

Turno (TurnManager)
  turno global: o jogador age primeiro, depois cada inimigo (mais perto do jogador primeiro; desempate y, x)
  turno da entidade: início (EntityTurnStartedEvent; estados: dano/cura periódicos; terreno OnTurnStart)
                     → ação → fim (EntityTurnEndedEvent; terreno OnTurnEnd, cooldowns, memória da IA;
                     depois a duração dos estados cai 1)
  ação do jogador: tocar célula (andar/atacar) ou usar skill → PlayerActionEvent;
    consumível NÃO consome a ação (máx. 1 por turno)
  cada passo espera animações (GridController.WaitForMovementsAsync) e TurnBlockers
    (orbs de XP que sobem de nível, escolha de talento, pausa, detalhes, dicas)
  último inimigo ou jogador morre → BattleDecidedEvent (vez = None, ninguém age)
    → após animações/orbs/talentos → BattleEndedEvent(vitória)

Dano (CombatResolver)
  estados de atacante e alvo opinam (Adjust: bônus condicionais, puros) → (base + fixo) × (1 + bônus de skill)
  × (1 + dano causado) × (1 + bônus condicional) → crítico (fluxo Combat) → defesa (fixa ou %, penetração)
  × (1 + dano recebido) × (1 + redução condicional) → mínimo → escudo absorve → vida; depois roubo de vida
  (atacante) e espinhos (alvo). Previsão sem sorteio: CombatResolver.PredictDamage (IA e bot)

XP e talentos
  inimigo morre → XpRewardSystem → orbs (XpOrbsVfx) → PlayerCharacter.GainXp (curva global)
  cada nível → cura (ProgressionSettings.levelUpHealFraction, padrão vida cheia) → PlayerLeveledUpEvent
    → TalentSession: oferta (TalentOfferGenerator, Derive(Talents, nível, reroll)) → pausa (TurnBlockers)
    → TalentOfferOpenedEvent → modal de talento → Choose/Reroll/Ban/Skip
  talento: estados permanentes com SourceId = id do talento (+pilhas = rank), skill liberada vai para a barra;
    reaplicado a cada batalha por IRunPlayerModifier

Morte de inimigo
  Character.Die → desocupa a célula → CharacterDiedEvent → flash/fade → Destroy
  (XP, métricas, glossário, estatísticas da run reagem ao evento)
```

## Padrões e convenções

### Dados em ScriptableObjects (data-driven)

- **Tudo que diferencia conteúdo e regra é dado**, em assets sob
  `Assets/Application/Settings/`. Código só implementa comportamentos
  reutilizáveis. Os ❓ dos documentos de design viraram campos com valor
  neutro (lista em `docs/planos/valores_padrao_em_aberto.md`).
- **Conteúdo** (personagens, skills, estados, talentos, tags, consumíveis,
  terrenos, papéis, ações de IA, dicas) deriva de `GameDefinition` (id
  estável = GUID do asset, usado nos salvamentos) ou de
  `DisplayableDefinition` (nome/descrição `LocalizedString` + ícone). O
  `GameDatabase` (em `Settings/Resources`) é reconstruído sozinho quando
  assets mudam (menu `GridBattle > Rebuild Game Database`); resolva ids com
  `GameDatabase.Instance.Get<T>(id)`.
- **Configurações de sistema** implementam `IGameSettings` (um asset por
  sistema) e são lidas com `GameSettings.Get<T>()` (ou `T.Current`). Também
  são registradas sozinhas no `GameSettings`; sem asset, valem os padrões do
  código (com aviso).
- **Efeitos compostos no Inspector**: listas `[SerializeReference,
  SubclassPicker]` (efeitos de estado, de skill e de consumível). Para um
  efeito novo, crie a subclasse `[Serializable]`; ela aparece no dropdown.
- Todo ScriptableObject criável tem `[CreateAssetMenu]` em `Create >
  GridBattle/...`; use `[Header]`, `[Tooltip]`, `[Min]`/`[Range]`.
- **Não crie um prefab por personagem.** Personagens usam os templates; o
  config é aplicado no spawn por `CharacterFactory` (com `CharacterScaling`).
- Assets (ações de IA, skills, efeitos) **nunca guardam estado de runtime**:
  cooldowns ficam em `SkillCooldowns`/`EnemyMemory`, estados em
  `StateContainer`.

### Regras de jogo

- **Atributos** (`EAttribute`) = base do config (× escala da profundidade) +
  modificadores dos estados ativos: `(base + Σ fixo) × (1 + Σ %)`. Leia
  sempre por `Character.Stats`, nunca direto do config.
- **Estados**: a duração conta turnos de quem carrega e cai no fim do turno
  dele (GDD 3.1). Reaplicação segue a `EStackPolicy` do estado; instâncias de
  origens diferentes (`SourceId`, ex.: talento) ficam separadas.
- **Dano e cura** passam sempre por `CombatResolver` (crítico, defesa,
  escudo, roubo de vida, espinhos, eventos). `Character.ReceiveDamage` é dano
  puro. Previsões (IA, bot) usam `CombatResolver.PredictDamage`, nunca
  `DamageCalculator` direto, para valer as passivas condicionais.
- **Números de skill** (dano, área, alcance, cooldown, distância de empurrão,
  duração de estados, efeitos) só pelo `EffectiveSkill.Resolve(lançador, skill)`:
  alvo, execução, cooldown, destaques, IA e bot passam por ele, e o asset da skill
  nunca é lido direto para esses números. Modificadores vêm de
  `SkillModifierEffect` nos estados do lançador (talentos de classe).
- **Passivas condicionais** (`ConditionalDamageEffect`, `OnKillEffect`,
  `ApplyStateOnHitEffect`): os ganchos `ModifyOutgoingDamage` /
  `ModifyIncomingDamage` são **puros** (sem sorteio, sem mudar o jogo), porque a
  previsão os chama também. "Andou no turno anterior" é
  `Character.MovedLastTurn` (entra no salvamento); adjacência pela métrica
  `CombatSettings.adjacencyMetric` (`GridRules.CountAdjacentOpponents`).
- **Distâncias** só por `GridDistance` com as métricas de `CombatSettings`
  (movimento/ataque euclidianos; alcance de skill Manhattan).
- **Empurrar e puxar** (deslocamento forçado) só por `Displacement` (regras
  puras, usadas também para prever) e `DisplacementResolver` (aplica):
  direção de 8 vizinhos, parada em obstáculo/borda/personagem, dano de colisão
  `EDamageKind.Collision` (valores em `CombatSettings`) atribuído ao lançador,
  terreno da célula final reage na hora (`GridController.ApplyForcedTerrain`).
  Personagem imóvel: `CharacterConfig.canBeDisplaced = false` (o chefe).
- **Determinismo**: todo sorteio de lógica usa os fluxos da run
  (`GameRandom.Stream(ERandomStream.X)` ou `RunRandom.Derive(...)` para
  sorteios por chave). `UnityEngine.Random` só para efeitos visuais.
- **Profundidade de balanceamento**: as profundidades dos assets de
  balanceamento (pool de inimigos, orçamento, escala, XP, faixas de terreno)
  valem para um mapa de `MapGenerationSettings.balanceFloorCount` andares (30)
  e são esticadas para o `floorCount` real. Mudar o número de andares não
  exige reescrever o balanceamento. As faixas de tipo de nó do mapa não são
  esticadas.
- **Lógica imediata, visual atrasado.** Estado muda na hora; animações só
  acompanham. Para segurar o fluxo de turno, use `TurnBlockers.Acquire(motivo)`
  (dispose para liberar). Entrada do jogador é ignorada enquanto houver
  bloqueio (`GameplayInput.IsBlocked`).

### Responsabilidades

- **Controllers são roteadores**: decidem a ação (input ou IA) e executam via
  `Character`/`GridController`/serviços. Regras ficam em `GridRules`,
  `SkillTargeting`, `ConsumableRules`, `TalentRules`, `MapRules` (estáticas).
- **Gameplay não depende de UI.** A UI reage a eventos e chama APIs
  (`RunManager`, `TalentService`, `ProfileService`, `PlayerCharacterController`).
  Quando a apresentação controla tempo (orbs de XP), o gameplay emite um evento
  que ela pode assumir e mantém um fallback sem UI.
- **Modo de simulação**: o simulador joga runs inteiras com as regras reais.
  Todo efeito visual, espera por tempo, som, dica, modal, métrica ou
  salvamento novo deve ser ignorado quando `SimMode.IsActive` (veja os
  existentes em `Cell`, `GridMovementAnimator`, `GridEffectsAnimator`,
  `TurnManager`, `XpOrbsVfx`, `AudioManager`, `RunManager`,
  `GameFlowController`). Com o modo desligado, o jogo não muda.
- Cada parte da UI que conhece nomes/medidas do UXML fica encapsulada numa
  classe (ex.: `XpBarView`, `SkillBarView`); outros sistemas usam essa API.
- Criação de personagens passa sempre por `CharacterFactory` /
  `GridController.SpawnEnemy`.
- Cada módulo toca os próprios sons (`AudioManager.Play(ESfx.X)`); ganchos
  genéricos (dano, morte, XP, movimento) já estão em `AudioEventHooks`.

### Comunicação por eventos

- `EventBus.Subscribe/Unsubscribe/Raise<T>` (estático). Eventos são classes
  simples com sufixo `Event`, em `Gameplay/Events` ou `UI/Events`.
- Toda inscrição tem o par de desinscrição. **O Play Mode está com domain
  reload desligado**: estado estático persiste entre sessões; zere estáticos
  com `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.

### Textos (Unity Localization)

- **Nenhum texto exibido ao jogador fica no código ou em assets como string
  solta** (GDD 8.1). Conteúdo usa `LocalizedString` (tabela `Content`, chaves
  `<tipo>.<id>.name/.desc`); interface usa a tabela `UI` (`Loc.Ui(chave)` /
  `HudText`). Atualize a UI em `Loc.LocaleChanged`.
- Em scripts de editor, crie entradas com `LocalizationTableTool.SetEntry`
  e `SetDisplayTexts` (en, es, pt-BR).
- ⚠️ A fonte pixel `ThaleahFat` só tem ASCII: acentos/ç/ñ/¿/¡ saem na fonte
  reserva até ela ser trocada (pendência de arte).

### UI — navegação (`ZS.UI`)

- **Somente `PanelRenderer`.** `UIDocument` está depreciado e não é suportado
  pelo `ZS.UI`; o `UILayer` exige um `PanelRenderer` no mesmo GameObject.
- **Poucas camadas, por função** (fundo, telas, modais, transição): cada
  `PanelSettings` distinto é um painel e cada `PanelRenderer` é um renderer na
  ordenação. Views escondidas (`display: none`) não custam layout nem desenho.
  **Telas novas usam `Instantiate` na `ScreensLayer`; modais novos usam
  `Instantiate` na camada de modais (`ModalLayer`).** As telas antigas
  (Start, MainMenu, Game, Menu, Background, Transition) são `LayerSource`
  (mantêm `sortingOrder` relativo ao mundo e caminhos de Animator).
- **Toda tela, modal ou página é um `ViewDefinition` (asset) + um
  `ViewController` (C# puro, `[Preserve]`)**, ao lado do seu UXML. Não crie
  MonoBehaviour por tela. Modais com resultado: `ModalController<T>`.
- Navegue só pelo `Navigator`; o fluxo do jogo fica no `GameFlowController`
  (eventos → navegação). **`ZS.UI` não pode referenciar nada do GridBattle.**
- Controladores: queries e callbacks em `OnBind` (repetido a cada reload de
  UI); inscrições externas em `OnCreate`/`OnDestroy`. Botões com
  `UiExtensions.OnClick` (e `SimulateClick` nos testes), não `Button.clicked`.
- Modais que pausam o jogo (talento, pausa, detalhes, dicas) seguram um
  `TurnBlockers` enquanto abertos. A raiz de todo modal usa `.overlay-screen`
  (absoluta, cobre o painel), para que modais empilhados não dividam a tela.
  As views dos modais novos ficam referenciadas em `ModalsSettings`/`HudSettings`
  (o `GameFlowController` as abre a partir dos eventos).

### UI — UXML e USS

- **Sem estilos inline no UXML**: estilo vai para USS. Atributos de conteúdo
  (`text`, `source`, `icon-image`, `picking-mode`) continuam no UXML.
- **Tokens** (`Theme/Tokens.uss`): cores, espaçamentos, tamanhos de fonte,
  durações, offsets e sprites de skin (`--skin-*`). Use `var(--token)`.
- **Componentes** (`Theme/Components.uss`): classes reutilizáveis
  (`.slot-button` e modificadores `--selected/--disabled/--empty/--round`,
  `.portrait-button`, `.panel-frame`, `.window-frame`, `.text-button`,
  `.hud-tile`, `.stat-bar`, `.mini-slot`, `.overlay-screen`, ...). Regras que
  sobrescrevem o tema de botões do Unity usam escopo `#container`.
- USS de tela só com o layout específico; `#id` só para ajustes únicos.
- Nomenclatura: kebab-case; modificadores com `--`; classes do framework com
  prefixo `zs-`.
- **Contratos de nome** (usados por código): `container`, `menu-button`,
  `xp-bar-progress`, `xp-bar-detail-2`, `current-level`, `tap-to-play-label`,
  `*-btn`, `overlay-background`, `skill-bar`, `item-bar`, `hud-depth*`,
  `close-button`, `details-*`, `map-*`, `node-<id>`, `preview-*`,
  `confirm-button`, `summary-*`, `offer-*`, `confirm-*`, `continue-btn`,
  `glossary-btn`, `options-btn`, `locked-info`; e os caminhos do Animator do
  menu (`#container/#character-selection/#<x>-btn/#<x>-image`). Não mude nome
  nem hierarquia sem atualizar o código/clip.
- Não use `<Template>`/`<Instance>` onde há seletor `>` ou caminho de Animator.
- UXML referencia USS por `project://database/...?guid=`; mova assets de UI
  pelo `AssetDatabase` (ou com o `.meta` junto).
- **Pixel perfect**: tamanhos inteiros (botões 32×32, ícones 16×16 ou 8×8
  nativos); nunca escala fracionária de sprite (no mundo, `EnemyIconsView` só
  amplia por inteiros). Tudo precisa caber em 216×384.

### Código C#

- **Toda documentação em `.cs` é em inglês** (XML docs, comentários,
  `[Tooltip]`, `[Header]`, mensagens de log). Documentos em `docs/` e
  mensagens de commit ficam em português.
- Campos serializados: `[SerializeField] private` em `camelCase`, expostos por
  propriedades somente leitura. Campos privados não serializados: `_camelCase`.
  Constantes e `static readonly`: `PascalCase`.
- Enums com prefixo `E` (`ECharacter`, `ECellHighlightType`).
- **Não use `?.`, `??` ou `??=` com `UnityEngine.Object`**: eles ignoram o
  null do Unity (objeto destruído). Use `!= null`, `TryGetComponent`.
- Animações e tweens com LitMotion (com `.AddTo(gameObject)` quando o alvo
  pode ser destruído).
- Ao mover/renomear scripts ou assets, **mova o `.meta` junto** (o GUID é a
  referência usada por cenas, prefabs, assets e saves).

## Como estender

| Quero... | Faça |
|---|---|
| Novo inimigo | `Create > GridBattle > Characters > Enemy Config`: sprite/animator (ou `tint` sobre arte existente), atributos, `prefab = Enemy.prefab`, `behavior`, `role`, `behaviorDescription`, `xpReward`, estados iniciais. Textos na tabela `Content`. Para aparecer nas batalhas: entrada no pool de `BattleGenerationSettings` (profundidade mín./máx., peso). |
| Novo comportamento de IA | Subclasse de `EnemyAction` (ou `EnemyAbilityAction`, que já tem cooldown na `EnemyMemory`) + asset; combine num `EnemyBehavior`. |
| Novo papel de inimigo | `Create > GridBattle > AI > Enemy Role` (ícone 8×8, fator de ameaça, conta como linha de frente). |
| Nova skill | `Create > GridBattle > Skills > Skill Definition`: tipo, dano, forma/tamanho de área, alcance, cooldown, alvos, efeitos. Inicial de classe: lista `skills` do `PlayerCharacterConfig`; liberada por talento: `unlockedSkill` do talento; de inimigo: `skills` do `EnemyConfig`. |
| Skill que empurra ou puxa | Skill normal com o efeito `Displace` na lista de efeitos: modo (longe do lançador, em direção ao lançador ou longe do centro da área) e distância. Ele roda depois do dano; colisão e terreno seguem `CombatSettings`. Para inimigos, `UseSkills` só a usa quando o resultado previsto vale a pena (`DisplacementAiWeights` no asset `UseSkills`). |
| Personagem imóvel (chefe) | Desmarque `canBeDisplaced` no `CharacterConfig`: empurrões e puxões não o movem, mas ele ainda bloqueia e leva o dano de quem é lançado contra ele. |
| Novo estado | `Create > GridBattle > States > State Definition` + efeitos no dropdown (atributos, dano/cura periódicos, espinhos, roubo de vida, escudo, restrições, modificadores da run). |
| Novo talento | `Create > GridBattle > Talents > Talent` (classes permitidas, nível, pré-requisitos, tags, `maxRank`, estados, skill) e coloque no `talentPool` da classe ou no pool compartilhado de `TalentOfferSettings`. O peso dos que só vêm do pool compartilhado é multiplicado por `sharedPoolWeightMultiplier` (0,5). |
| Talento que modifica uma skill da classe | Estado com `Skill Modifier Effect` (skills, dano fixo/%, passos de área, alcance, redução de cooldown, empurrão, duração de estados, efeitos anexados; valores por pilha, `repeatAttachedPerStack`) + talento com `maxRank` (posto = pilha) e a skill (ou o talento que a libera) nos pré-requisitos. Ver `docs/projeto/guia_sistemas.md`. |
| Passiva condicional de classe | Estado com `Conditional Damage Effect` (causado ou recebido; tipos de dano; condição: por inimigo adjacente, sem adjacente, alvo com estado nocivo ou com um estado, andou no turno anterior, alvo isolado, vida cheia), `On Kill Effect` ou `Apply State On Hit Effect`; talento como acima. Condição nova: `EDamageCondition` + `ConditionalDamageEffect.Evaluate`. |
| Novo consumível | `Create > GridBattle > Consumables > Consumable Definition` (alvo, área, efeitos, peso) e adicione ao pool de `ConsumableSettings`. |
| Novo terreno | `Create > GridBattle > Terrain > Terrain Definition` (tipo, bloqueio, gatilho, dano/estados, visual) e adicione ao pool de `TerrainGenerationSettings`. |
| Nova dica de tutorial | `TutorialTipDefinition` com o `ETutorialTrigger` + `TutorialService.Notify(gatilho)` no ponto do jogo. |
| Novo personagem jogável | `Player Character Config` (`prefab = PlayerCharacter.prefab`, `characterClass`, `battlesToUnlock`, `talentPool`, skills iniciais); adicione em `GameStateManager.playableCharacters` e um botão no menu. |
| Ajustar mapa / batalhas | `Settings/Map/MapGenerationSettings` (andares, andares de referência do balanceamento, nós, tipos, dificuldades) e `BattleGenerationSettings` (pool, ameaça, orçamento, escala, XP, limites, chefe). |
| Ajustar XP e níveis | `Settings/Progression/ProgressionSettings` (curva e nível máximo); XP das batalhas em `BattleGenerationSettings` (fonte `ThreatBudget`, XP por ponto de ameaça, crescimento por profundidade, multiplicador por dificuldade). Confira com `GridBattle > Talents > Simulate XP Progression`. |
| Medir o balanceamento | `GridBattle > Simulation > Run Balance Simulation` (opções no asset `Settings/Simulation/BalanceSimulationSettings`): runs completas por classe com bots, relatório em `persistentDataPath/Simulation/<data>_<rótulo>/` (`summary.md`, `runs.csv`, `battles.csv`). |
| Ajustar combate | `Settings/Combat/CombatSettings` (defesa, dano mínimo, métricas, colisão de empurrões e puxões). |
| Ajustar run | `Settings/Run/RunSettings` (cura, custo do nó de talento, estados entre batalhas, seed de depuração, `autoEnterFirstNode`). |
| Adicionar som | Arraste clipes na entrada do `ESfx`/`EMusicContext` em `Settings/Audio/AudioLibrary`. |
| Novo texto | Entrada nas tabelas `UI`/`Content` (`Window > Asset Management > Localization Tables`) nos 3 idiomas. |
| Nova tela/modal | UXML + USS em `UI/Screens|Overlays/<Nome>/`; `ViewController` em `Scripts/UI/...`; `ViewDefinition` (Instantiate na `ScreensLayer` ou na camada de modais); navegue a partir do `GameFlowController`. |
| Nova página interna | `<zsnav:PageHost name="...">` no UXML da tela; `Context.GetPageNavigator("...").Push(definição)`. |
| Novo token/estilo | Valor em `Theme/Tokens.uss`; classe reutilizável em `Theme/Components.uss`. |
| Sistema que altera o jogador da run | Implemente `IRunPlayerModifier` e registre em `RunPlayerHooks` (como `TalentRunModifier`). |

## Cuidados

- **Lógica imediata, visual atrasado.** Regras e IA nunca dependem da posição
  visual. Quem precisa esperar animações usa
  `GridController.WaitForMovementsAsync` e/ou `TurnBlockers`.
- **Ordem dos inimigos** é determinística (distância ao jogador, y, x); mudar
  isso é mudança de mecânica.
- **Determinismo dos saves**: a batalha é salva no início da vez do jogador e
  a carga repete o turno. Toda decisão sorteada precisa vir dos fluxos da run
  (ofertas de talento usam `Derive(Talents, nível, reroll)`).
- **Testes em Play Mode gravam no perfil real** (`run.json`,
  `profile.json`, `metrics.jsonl` em `persistentDataPath`, no Windows
  `%USERPROFILE%/AppData/LocalLow/Zuchinali Softworks/GridBattle`). Esses
  arquivos podem ser do jogo de verdade de quem desenvolve: antes do teste,
  guarde uma cópia dos que existem; depois, restaure as cópias e apague só
  os que o teste criou. O simulador não grava nenhum deles.
- **O Editor é compartilhado**: antes de entrar em Play Mode por script,
  confira se ele já está em Play Mode sem um teste seu rodando (alguém pode
  estar jogando) e não o interrompa sem avisar.
- **`GridController` é `[ExecuteAlways]`** e refaz o grid de depuração no
  `Awake` em edit mode (`debugPlayerConfig` + `DefaultEncounter` +
  `debugTerrain`). Por isso a cena guarda células e personagens “assados” e o
  diff da cena fica grande após um **Reset grid**. O checkbox do componente
  está desmarcado na cena e não tem efeito.
- Entrar em Play Mode altera os atlas dinâmicos do TMP
  (`ThaleahFat_TTF SDF.asset`, `LiberationSans SDF - Fallback.asset`).
  Reverta antes de commitar se não for intencional.
- Conteúdo marcado como **provisório** (inimigos de exemplo, chefe,
  talentos, ícones gerados) existe para os sistemas funcionarem: substitua
  pelos definitivos sem mudar código.
- Mudanças de gameplay devem ser deliberadas: refatorações precisam manter o
  jogo idêntico (ver Verificação).

## Verificação

Testes do framework: `unity command run_tests --mode EditMode --filter ZS.UI.Tests --filter_type assembly`.
Checagens dos talentos (inclui o resolvedor de skills e as passivas de classe do G3): menu `GridBattle > Talents > Run Checks`; simulação de
XP: `GridBattle > Talents > Simulate XP Progression`; simulação de runs
completas: `GridBattle > Simulation > Run Balance Simulation` (ou
`BalanceSimulation.Run/RunBatches` por script). Ela não grava `run.json`,
`metrics.jsonl` nem o perfil real (usa um perfil temporário).
O Unity Editor costuma estar aberto e acessível pela CLI `unity` (pacote
`com.unity.pipeline`):

- `unity command recompile` / `recompile_status` / `get_console_logs` para
  compilar e checar erros.
- `unity command run_script --file <arquivo fora de Assets> --entry Classe.Metodo`
  para rodar C# no Editor (migrações de assets, testes em Play Mode).
- **Partida determinística de regressão**: script de Play Mode que inicia a
  batalha de depuração (`GridController.InitializeGrid(Knight)`, sem passar
  pela run), joga por política fixa e registra estado por turno, com a ordem
  dos inimigos fixada só durante o teste; compare com a execução de
  referência. Agende passos por `Time.time` (o loop do Play Mode trava com o
  Editor sem foco).
- **Prova de regressão com conteúdo novo desligado** (usada no G3): antes de
  rodar o simulador, volte os pools de talentos de cada classe e o peso dos
  genéricos ao que eram, **só em memória** (reflexão, sem `SetDirty`, e restaure
  depois); com as mesmas seeds ele tem de reproduzir exatamente o resultado de
  referência (vitórias, andar, nível, ações). Mostra que o código novo não
  muda o jogo base.
- Fluxos de UI: scripts que clicam com `SimulateClick` e tiram screenshots
  (`ScreenCapture.CaptureScreenshot`) para conferir 216×384.
- Para provar que UI/USS/UXML não mudaram: script que grava a árvore visual de
  cada painel (`worldBound` + `resolvedStyle`) por estado e compara.

## Histórico de refatorações

Registre aqui toda mudança estrutural (mais recente primeiro).

### 2026-10-06 — Balanceamento G3: identidade de classe
Mudança de mecânica pedida (decisão do usuário: "modificadores + passivas únicas"; `docs/planos/plano_balanceamento.md`).
- **Valores efetivos de skill** (`Gameplay/Entities/Skills/EffectiveSkill`): todo
  número de uma skill para um lançador (dano, área, alcance, cooldown, empurrão,
  duração de estados, efeitos) passa por `EffectiveSkill.Resolve`, que soma os
  `SkillModifierEffect` dos estados do lançador. `SkillTargeting` (todas as
  funções têm sobrecarga com o `EffectiveSkill` para os laços),
  `SkillDefinition.Execute/GetDamageFor`, `SkillCooldowns.Trigger`,
  `DisplaceSkillEffect`/`ApplyStatesSkillEffect` (via `SkillContext.Effective`),
  `UseSkills`/`DisplacementScoring` e `BattleBot` leem dele. Sem modificadores os
  números são os do asset (partida determinística idêntica à referência).
- **Ganhos condicionais de dano:** `StateEffect.ModifyOutgoingDamage/ModifyIncomingDamage`
  (puros) alimentam um `DamageAdjustment` que `CombatResolver.DealDamage` passa ao
  `DamageCalculator` (continua puro; sobrecarga nova, a antiga igual a ajuste
  neutro). `CombatResolver.PredictDamage` prevê sem sorteio (bot, IA, colisões,
  terreno). Efeitos: `ConditionalDamageEffect` (condições: por inimigo adjacente,
  sem adjacente, alvo com estado nocivo ou com estado da lista, andou/não andou no
  turno anterior, alvo isolado, vida cheia; máscara `EDamageKindMask`),
  `OnKillEffect` (cura, cooldowns, estados) e `ApplyStateOnHitEffect`.
- **Movimento no turno:** `Character.MovedThisTurn/MovedLastTurn`
  (`GridController.MoveEntity` marca; `BeginTurn` gira) e
  `EntitySnapshot.MovedLastTurn` no salvamento; `SkillCooldowns.QueueReduction`
  (redução de cooldown no fim do turno); `GridRules.CountAdjacentOpponents/HasAdjacentAlly`
  com `CombatSettings.adjacencyMetric` (Chebyshev).
- **Conteúdo provisório** (`Settings/Talents`, `Settings/States`): 43 talentos de
  classe (Guerreiro 12, Mago 13, Ladino 18, além dos que liberam skills) com 43
  estados, 5 estados novos (Queimando, Veneno letal, Congelado, Atordoado,
  Exposto) e 2 tags (Brigão, Elemental). `TalentOfferSettings.sharedPoolWeightMultiplier`
  (0,5) dá menos peso aos genéricos. O Ladino, o mais fraco, ganhou o veneno que
  cresce com a vida do inimigo (`Venom`: 1 + 5% da vida máxima por pilha e −6% do
  dano do envenenado), cura ao matar e redução de dano contra inimigos com estado
  nocivo.
- **Simulador:** relatório com a parcela de talentos de classe × genéricos;
  `BattleBotOptions.stateValueWeight` (dano ao longo do tempo das skills conta na
  comparação com o ataque básico). `TalentChecks` ganhou verificações do
  resolvedor, dos modificadores por posto, do cooldown mínimo, da fórmula, das
  passivas e dos pools de classe.

### 2026-10-06 — Balanceamento (`docs/planos/plano_balanceamento.md`)
Mudança de mecânica pedida (o jogo estava desbalanceado).
- **G1, simulador** (`Gameplay/Simulation`, `Editor/Simulation`): um bot joga
  runs completas pelo `RunManager` com as regras reais, sem animações nem
  salvamento (`SimMode`), com seed. Relatório por classe: vitória,
  profundidade da morte, vida perdida, nível, XP por nó e dificuldade,
  inversões de XP, fontes de dano, talentos, skills, terreno. Guardas
  `SimMode.IsActive` nos pontos visuais e temporizados; `DamageCalculator`
  aceita `rng` nulo (previsão sem crítico).
- **G2, XP coerente e tamanho da run:**
  - `MapGenerationSettings`: `floorCount` 30 (pedido do usuário) e
    `balanceFloorCount` (30) com `GetBalanceDepth` e `RunLengthXpFactor`. Os
    geradores de batalha e de terreno leem os assets na profundidade de
    balanceamento;
  - valores de balanceamento convertidos de 11 para 30 andares (orçamento
    +0,52 por andar, vida +2,1% e dano +1,03% por andar, provisórios
    entrando nos andares 7/10/13/16, faixas de terreno 4 e 13);
  - `BattleGenerationSettings.xpSource` (`EBattleXpSource`, substitui
    `xpFromThreat`): `ThreatBudget` (padrão) dá XP = orçamento × 8,5 ×
    (1 + 0,07 × (profundidade − 1)), dividido pela ameaça dos inimigos. Não
    há mais inversão fácil > normal > difícil no mesmo andar.
- **Cura ao subir de nível** (pedido do usuário): `ProgressionSettings.levelUpHealFraction`
  (padrão 1, vida cheia), aplicada em `PlayerCharacter.GainXp` antes de cada
  `PlayerLeveledUpEvent`. A partida determinística de regressão desliga essa
  cura durante o teste para continuar comparável com a referência.

### 2026-10-06 — Balanceamento G4: empurrar e puxar
Mudança de mecânica pedida (deslocamento de personagens; `docs/planos/plano_balanceamento.md`).
- **Regras** (`Gameplay/Rules/Displacement`, puras): direção de 8 vizinhos
  (ângulo arredondado a 45°; para células vizinhas é o sinal de cada
  componente), caminho célula a célula, parada em borda, obstáculo (diagonal
  entre dois obstáculos também para, como andar), personagem ou, ao puxar, ao
  ficar ao lado do lançador (nunca em cima). Alvos de uma área são movidos do
  mais longe ao mais perto da referência (puxar: o contrário; empate por y, x).
  `Plan`/`DisplacementResolver.Predict` preveem sem alterar nada.
- **Aplicação** (`Gameplay/Combat/DisplacementResolver`): move na hora
  (`GridController.DisplaceEntity`: deslize, sem pulo), dano de colisão pelo
  `CombatResolver` (`EDamageKind.Collision`: sem crítico, espinhos nem roubo de
  vida; atribuído ao lançador, então XP e mortes seguem o normal), o outro
  personagem também leva dano (nunca o lançador), terreno da célula final
  reage na hora (`TerrainEffects.ApplyForcedEntry`; gatilho OnEnter já foi
  aplicado pelo movimento) e `CharacterDisplacedEvent`. Sem sorteios.
- **Dados:** `CombatSettings` (seção "Displacement": dano 6, +2 por célula não
  percorrida, 6 ao personagem atingido, ignora defesa, borda conta);
  `CharacterConfig.canBeDisplaced`; `GridMovementSettings` (deslize, atraso do
  impacto); efeito `DisplaceSkillEffect`.
- **Conteúdo provisório:** Golpe de Escudo (Guerreiro, empurra 2), Rajada de
  Vento (Mago, área 3x3, empurra 1 do centro) e Gancho (Ladino, puxa 3), cada
  uma liberada por um talento da classe; inimigo Goblin Arpoador (puxa) a
  partir do andar 8; o chefe ganhou a Onda de Choque (empurra 2) e é imóvel.
- **IA:** `UseSkills` pontua skills de deslocamento (`DisplacementScoring`:
  dano de colisão e de terreno, aliados que alcançam o jogador antes e
  depois, aproximação, abates) e só as usa acima de um valor mínimo.
- **Simulador:** `BattleBot` valoriza empurrões e puxões com a mesma previsão
  (colisão conta como dano e abate; bônus por tirar um inimigo do alcance,
  trazer um atirador, mandar para perigo ou tirar de célula de bônus);
  relatório com seção "Pushing and pulling"; `TalentPolicyOptions.preferredTalents`.

### 2026-10-05 — Implementação do GDD (`docs/planos/plano_implementacao_gdd.md`)
Mudança de mecânica pedida: implementação de tudo que está no GDD e em
`docs/design/` (sem `docs/propostas/`, sem monetização), com toda configuração
no editor. Valores das perguntas em aberto em
`docs/planos/valores_padrao_em_aberto.md`; guia de uso em `docs/projeto/`.
- **Base:**
  - `Data` (`GameDefinition` com id = GUID, `GameDatabase`, `GameSettings` +
    `IGameSettings`, `SubclassPicker`);
  - `Core` (`Loc`, `SaveSystem`, `Randomness`: `Rng`, `RunRandom`,
    `GameRandom`);
  - Unity Localization (en, es, pt-BR) e Newtonsoft JSON.
- **Combate:**
  - atributos (`Stats`), estados com efeitos e duração por turno do portador
    (`States`);
  - `CombatResolver`/`DamageCalculator`: crítico, defesa, penetração, escudo,
    roubo de vida, espinhos;
  - `GridDistance` com métricas configuráveis.
- **Turnos:**
  - turno global com início e fim do turno de cada entidade;
  - `TurnBlockers`;
  - `BattleController` (vitória/derrota, `BattleDecidedEvent`, vez `None`).
- **Skills** (`Entities/Skills`): áreas, alcance, cooldowns, mira do jogador,
  uso pela IA; as 9 skills da tabela 6.2.
- **Inimigos:**
  - papéis (`Entities/Roles`) e novas ações de IA (manter distância, aplicar
    estado, curar, fortalecer, invocar);
  - `EnemyMemory`, ícones de papel/estado e inimigos provisórios;
  - o FireSkull passou a ser atirador.
- **Terreno** (`Gameplay/Terrain`): obstáculos, perigo, bônus; gerador com
  conectividade; caminho em volta de obstáculos.
- **Consumíveis** (`Gameplay/Consumables`): inventário, uso sem consumir a
  ação (1 por turno), mira.
- **Run, mapa e salvamento:**
  - `Managers/RunManager`, `Gameplay/Map` (mapa de nós, geração de batalhas
    por orçamento de ameaça), `Gameplay/Run`;
  - salvamento automático (`run.json`) e retomada exata.
- **XP e talentos:** curva de XP saiu das classes para `ProgressionSettings`
  (pedido do usuário); `Gameplay/Talents` (ofertas determinísticas com
  sinergia, reroll/banir/pular, nó de talento, `IRunPlayerModifier`).
- **Meta** (`Gameplay/Meta`): perfil (`profile.json`), liberação de classes
  (0/15/30 batalhas), glossário, recordes, dicas de tutorial, métricas locais.
- **Áudio** (`Managers/Audio`): `AudioManager` autoinicializado e
  `AudioLibrary` (sem clipes).
- **UI:**
  - HUD (barras de skills e itens, profundidade, detalhes por toque
    longo/clique direito);
  - telas de mapa com prévia, fim de run e vitória (`ScreensLayer`);
  - modais de oferta de consumível, confirmação, talento, glossário, opções,
    pausa e dicas;
  - menu principal com classes bloqueadas e "Continuar";
  - `GameFlowController` reescrito;
  - destaques do grid por evento.
- **Pendências:**
  - fonte pixel sem acentos (arte);
  - conteúdo provisório;
  - balanceamento dos valores neutros;
  - clipes de áudio;
  - monetização.

### 2026-10-01 — Game feel, Fases 2–4
- `TurnManager`: ordem determinística, pacing `Staggered` (padrão, também no
  asset da cena), speed-up por toque (`GridController.AnimationSpeed`).
- `Cell`: highlights com fade escalonado (só visual).
- `GridEffectsAnimator` + `GridController.PlayAttackAnimation/PlayHitAnimation/
  PlayDeathAnimation`; `WaitForMovementsAsync` cobre movimento e efeitos.
  `Character.Die` adia o Destroy de inimigos (`PlaysDeathEffect`).
- Hit stop não implementado (fora do escopo); sem áudio no projeto, nenhum gancho
  de som foi criado.

### 2026-10-01 — Game feel do movimento, Fase 1 (`docs/planos/plano_game_feel_movimento.md`)
- `GridMovementSettings` ganhou `EMovementStyle` (Hop/Flip) e parâmetros de
  hop (ease, squash na aterrissagem), flip (fechar/abrir, overshoot,
  `scaleSteps` para quantizar o scale) e pulso de chegada. `GridMovementAnimator`
  despacha por estilo, expõe `SpeedMultiplier` e `onArrived`; ao completar/ser
  interrompido sempre normaliza posição e `localScale`.
- `Cell`: `PlayArrivalPulse` (chamado por `GridController.Move` ao chegar) e
  `PlayRejectFeedback` (o `PlayerCharacterController` chama em toques
  rejeitados); o toque válido não faz mais squash.
- Pendente: avaliar Hop × Flip jogando; Fases 2–4 do plano.

### 2026-09-30 — Animação de movimento e turnos com espera (mudança de mecânica pedida)
- `GridMovementAnimator` + `GridMovementSettings` (`Gameplay/Movement`): ao
  trocar de célula, o entity dá pequenos pulos (LitMotion) até o centro da
  célula nova. Integrado em `GridController.Move`; a lógica continua imediata.
- `TurnManager` passou a controlar a vez (`ETurnOwner`, `TurnChangedEvent`):
  após a ação do jogador, espera o movimento dele, executa os inimigos
  (aguardando os movimentos; `EEnemyTurnPacing` Sequential/Simultaneous) e
  devolve a vez. `GridController` emite `GridInitializedEvent` ao iniciar uma
  run (reseta a vez).
- `PlayerCharacterController` só aceita toques/skills e só mostra highlights na
  vez do jogador.
- Verificado: mesma sequência lógica de partida (posições, HP, mortes, XP) do
  código anterior; pulos, bloqueio de input e highlights confirmados em Play Mode.

### 2026-09-30 — `ZS.UI` somente com PanelRenderer
- Removido o suporte a `UIDocument` (depreciado): `UILayer` agora exige
  `PanelRenderer` (`[RequireComponent]`) e expõe `UILayer.PanelRenderer`.
- Cena de exemplo refeita com `PanelRenderer`.

### 2026-09-30 — Framework de navegação `ZS.UI` e reorganização de USS/UXML
- **Novo framework `Assets/ZS/UI`** (asmdefs `ZS.UI`, `ZS.UI.Editor`,
  `ZS.UI.Tests`, `ZS.UI.Samples`; namespace `ZS.*`, sem dependência do jogo):
  `UIRoot`, `UILayer` (somente PanelRenderer), `UILayerDefinition`,
  `ViewDefinition`, `ViewController`, `ModalController<T>`, `Navigator` (pilha,
  modais, voltar, política de concorrência), `PageHost`/`PageNavigator`,
  transições (`CoverTransition`, `UssClassTransition`), `SafeArea` (movido do
  jogo), inspector com validação, `Window > ZS > UI Navigation Debugger`,
  14 testes EditMode e cena de exemplo.
- **GridBattle migrado:** removidos `View`, `UIScreen`, `NavigationController`,
  `ScreenTransitionView` e as views de tela; criados controladores em
  `Scripts/UI/{Screens,Overlays,Background}` e `GameFlowController`
  (`UI/Flow`). GameObjects de UI renomeados para `*Layer`. `XpOrbsVfx` obtém o
  `GameScreenController` pelo `UIRoot`.
- **USS/UXML:** pastas por feature (`UI/Theme`, `Screens/*`, `Overlays/Menu`,
  `Background`, `Transitions`, `Settings`, `Layers`); tema e PanelSettings
  saíram de `Assets/UI Toolkit`; tokens (`Tokens.uss`) e componentes
  (`Components.uss`); estilos inline removidos; regras duplicadas
  (`#xp-area > Button`/`#spell-area > Button`) unificadas em `.slot-button`;
  regras mortas removidas; caminhos de schema corrigidos.
- Removido `Scripts/AssemblyInfo.cs` (prefixo `gb`): o jogo não tem mais
  elementos UXML próprios.
- Verificado: árvore visual e estilos resolvidos idênticos em todos os estados,
  mesma linha do tempo de transições, partida A/B, XP e Animator do menu
  idênticos.

### 2026-09-30 — `TurnManager` movido para Managers
- `Gameplay/TurnManager.cs` → `Managers/TurnManager.cs`, namespace
  `GridBattle.Gameplay` → `GridBattle.Managers` (GUID preservado; o
  componente continua no GameObject `Grid`).
- Criado este `AGENTS.md`.

### 2026-09-30 — Documentação dos `.cs` em inglês
- XML docs, comentários, tooltips, headers e logs de todos os scripts
  traduzidos para inglês.

### 2026-09-30 — Personagens e IA data-driven, XP desacoplado do grid (`47f7064`)
- **Personagens por config:** `CharacterConfig` ganhou visual (`sprite`,
  `animatorController`) e virou abstrato; `EnemyConfig` ganhou `prefab` e
  `behavior`; `PlayerCharacterConfig` ganhou `prefab` e
  `GetXpToNextLevel(level)`. `[CreateAssetMenu]`, tooltips e `[Min]` em todos.
- **Templates no lugar de 8 prefabs:** removidos `Prefabs/Entities/Enemies/*`
  e `PlayerCharacters/*`; criados `Character.prefab` + variants `Enemy` e
  `PlayerCharacter`. Novos `CharacterView` (visual) e `CharacterFactory`
  (spawn, linkado ao prefab em edit mode).
- **IA em ScriptableObjects:** `EnemyBehavior` + `EnemyAction`
  (`UseSkills`, `BasicAttack`, `ChaseTarget`); `ChaseAndAttack` reproduz a IA
  antiga. O passo guloso foi para `GridRules.FindStepToward`.
- **Spawn configurável:** `EncounterConfig` (substitui `enemyEntityPrefabs`);
  `GridController.debugPlayerConfig` (substitui `debugEntityPrefab`);
  `GameStateManager.playableCharacters` (substitui o dicionário de prefabs).
- **Controllers:** movidos para `Gameplay/Controllers`
  (`GridBattle.Gameplay.Controllers`); base vazia `CharacterController`
  (conflitava com `UnityEngine.CharacterController`) substituída por
  `CharacterControllerBase<T>`.
- **XP:** `XpVfxController` (no Grid) dividido em `XpRewardSystem` (regra,
  GameObject próprio), `XpOrbsVfx` + `XpOrbsVfxSettings` (apresentação, no
  `GameScreenView`), `XpBarView` (barra) e `XpPacket`; novo
  `XpRewardDroppedEvent`.
- Outros: `IDamageReceiver.OnHpChanged` virou `event`; `SkillDefinition`
  abstrata; `CellControllerEditor` → `GridControllerEditor`; `View.Root`.
- **Bugs corrigidos:** XP deixava de ser creditado a partir da 2ª run (cache
  de player destruído com `??=`); a barra de XP não zerava em nova run.
  Fallback: sem UI disponível, o XP é creditado na hora.
- Verificado com partida A/B (código antigo × novo, mesma ordem de inimigos):
  resultado idêntico.
