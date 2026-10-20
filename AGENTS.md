# AGENTS.md — GridBattle

Guia para agentes (e pessoas) trabalhando neste repositório: estrutura,
padrões, fluxos principais e histórico de refatorações.

> **Mantenha este arquivo atualizado.** Toda mudança de estrutura (pastas,
> namespaces, novos sistemas, novos tipos de asset, convenções) deve ser
> refletida aqui na mesma alteração, incluindo uma entrada em
> [Histórico de refatorações](#histórico-de-refatorações).

## Visão geral

RPG tático por turnos em grid 2D (roguelite mobile, retrato). O jogador escolhe
uma classe e percorre um **mapa de nós** (batalha normal ou de elite,
acampamento, talento, consumível e chefe final). As batalhas acontecem num
grid com terreno, no molde do *Into the Breach* (decisão de 2026-10-19): não há
ataque básico, toda ação é uma skill, todo golpe de inimigo é anunciado um turno
antes e a previsão mostra o resultado de uma ação antes de confirmar. A
progressão segue o *Slay the Spire*: ao subir de nível, o jogador escolhe
**talentos**, que mudam regras e liberam skills; no acampamento ou depois de
vencer uma elite, ele melhora uma skill (1 de 2 variantes).

| Item | Valor |
|---|---|
| Engine | Unity **6000.6.0f1** |
| Render | URP 2D + Pixel Perfect Camera (PPU **100**, `GameConfigManager.Ppu`), referência 216×384 |
| UI | UI Toolkit (UXML/USS) via `PanelRenderer`; navegação pelo framework `ZS.UI` (`Assets/ZS/UI`) |
| Textos | Unity Localization 1.5 (tabelas `UI` e `Content`; idiomas `en`, `es`, `pt-BR`) |
| Salvamento | JSON (Newtonsoft) em `Application.persistentDataPath`: `run.json`, `profile.json`, `metrics.jsonl` |
| Tweens | LitMotion (`LMotion`) |
| Monetização | Unity LevelPlay 9.5 (*Ads Mediation*, `com.unity.services.levelplay`: intersticial + recompensado) e Unity IAP 5.4 (`com.unity.purchasing`: compra não consumível `remove_ads`); Mobile Dependency Resolver para as dependências nativas. Só Android e iOS; no Editor, anúncios e loja de teste |
| Input | Input System (`InputSystemUIInputModule`) + `IPointer*Handler` nas células |
| Texto no mundo | TextMeshPro (dano flutuante das células) |
| Assemblies | Jogo: `Assembly-CSharp`/`-Editor` (sem `.asmdef`). Framework: `ZS.UI`, `ZS.UI.Editor`, `ZS.UI.Tests`, `ZS.UI.Samples` |
| Testes | EditMode do framework (`ZS.UI.Tests`); verificações do jogo por scripts no Editor (ver [Verificação](#verificação)) e `GridBattle > Talents > Run Checks` |
| Cena | `Assets/Scenes/SampleScene.unity` (única) |

Documentação de design (Obsidian, em português) em `docs/`: o [[GDD]] e os
subdocumentos de `docs/design/`. A referência do combate implementado é
`docs/design/sistema_combate.md`, que deve ficar em sincronia com o código.
O guia de uso dos sistemas pelo editor é `docs/projeto/guia_sistemas.md`
(o simulador de balanceamento tem o seu: `docs/projeto/simulador_balanceamento.md`;
a monetização também: `docs/projeto/monetizacao.md`, com o passo a passo dos painéis e dos builds em
`docs/projeto/habilitar_anuncios_e_compras.md`;
o resumo das mudanças está em `docs/projeto/resumo_das_mudancas.md`; o guia de estilo da arte e como gerar ícones
novos no PixelLab, em `docs/projeto/arte.md`). O plano e os
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
│   │       ├── Icons/{Skills,Talents,States,Roles,Consumables,Map,UI}/   ícones finais (PixelLab, ENDESGA 32; ver docs/projeto/arte.md)
│   │       ├── Terrain/                                    sprites de terreno 24×24 (sobre a célula e como ícone; inclui IceWall)
│   │       ├── UI/                                         slots redondos da barra de itens (cores do UI.png)
│   │       └── Placeholders/Vfx/                           visuais de skills ainda provisórios (VfxPlaceholders)
│   ├── Localization/                                       LocalizationSettings, Locales (en, es, pt-BR), Tables (UI, Content)
│   ├── Prefabs/
│   │   ├── Cell.prefab                                     célula (destaques, terreno, barra de vida, texto de dano)
│   │   └── Entities/
│   │       ├── Character.prefab                            template base (SpriteRenderer + Animator + CharacterView)
│   │       ├── Enemy.prefab                                variant: + Enemy + EnemyController + EnemyIconsView (ordem de ação no canto da célula, papel, estados; golpe só ao tocar)
│   │       └── PlayerCharacter.prefab                      variant: + PlayerCharacter + PlayerCharacterController
│   ├── Settings/                   ← dados de jogo (ScriptableObjects), editáveis pelo Inspector
│   │   ├── Resources/                                      GameDatabase.asset e GameSettings.asset (gerados pelo editor)
│   │   ├── Characters/{Players,Enemies}/                   PlayerCharacterConfig / EnemyConfig (27 inimigos provisórios, inclusive as elites `isUnique` e o chefe)
│   │   ├── AI/                                             EnemyBehavior (ChaseAndAttack, Ranged, Skirmisher, Stationary, Support, Summoner, Controller)
│   │   ├── AI/Actions/, AI/Roles/                          EnemyAction (ApproachMelee/Mid/Ranged = ApproachForSkillAction) e EnemyRoleDefinition
│   │   ├── Skills/<Classe>/, Skills/<Classe>/Upgrades/     SkillDefinition: 6 por classe (1 inicial + 5 liberadas por talento) e 2 variantes de melhoria de cada uma (Meteoro e Tempestade Elétrica do Mago são canalizadas: `usesWalk`)
│   │   ├── Skills/Enemies/, Skills/SkillSettings           43 golpes anunciados dos inimigos e do chefe (cada inimigo: um especial com recarga e um básico sem recarga); SkillSettings (6 espaços de skill)
│   │   ├── States/, States/Talents/                        StateDefinition (States/: estados de combate, inclusive Blindado, Exposto, Fraquejado, Congelado, Cego, Atordoado, Enredado, Exausto; States/Talents/: um estado por talento)
│   │   ├── Talents/{Shared,Warrior,Mage,Rogue,Tags}/       TalentDefinition (talentos de regra, passivas e os que liberam skills), SynergyTagDefinition, TalentOfferSettings (peso dos genéricos)
│   │   ├── Terrain/                                        TerrainDefinition (inclui IceWall, a Muralha de Gelo criada por skill) + TerrainGenerationSettings
│   │   ├── Consumables/                                    ConsumableDefinition + ConsumableSettings
│   │   ├── Tutorial/                                       TutorialTipDefinition (uma por gatilho)
│   │   ├── Combat/, Progression/, Run/, Map/               CombatSettings, ForecastSettings (visual da previsão), ProgressionSettings, RunSettings, Map/{Map,Battle}GenerationSettings
│   │   ├── Simulation/                                     BalanceSimulationSettings (simulador de balanceamento)
│   │   ├── Meta/, Audio/, Input/, Hud/, Screens/, Modals/  MetaSettings, AudioLibrary, GameplayInputSettings, HudSettings, ScreensSettings, ModalsSettings (views dos modais)
│   │   ├── Encounters/DefaultEncounter.asset               inimigos da batalha de depuração (grid montado no editor)
│   │   ├── Vfx/XpOrbsVfxSettings.asset, Movement/GridMovementSettings.asset
│   │   ├── Vfx/ActionVisuals/                              ActionVisualDefinition (visual de cada skill e da Bomba; provisórios gerados)
│   │   ├── Monetization/MonetizationSettings.asset         anúncios, compra, rampa de frequência, privacidade, modo do Editor
│   ├── UI/                         ← UI Toolkit organizada por feature
│   │   ├── Theme/                                          GridBattle.tss, Tokens.uss, Base.uss, Components.uss
│   │   ├── Settings/                                       PanelSettings, WorldPanelSettings, PanelTextSettings
│   │   ├── Layers/*.asset                                  UILayerDefinition (uma por painel)
│   │   ├── Screens/{Start,MainMenu,Game,Map,RunEnd,Victory}/   <Tela>.uxml + .uss + <Tela>View.asset (ViewDefinition)
│   │   ├── Overlays/{Menu,EntityDetails,ConsumableOffer,Confirm,TalentChoice,Glossary,Options,TutorialTip,CharacterDetails,RemoveAds,AdNotice,SimulatedAd,SkillUpgrade}/
│   │   ├── Background/, Transitions/, Animations/
│   └── Scripts/                    ← ver tabela abaixo
├── ZS/UI/                          ← framework reutilizável de navegação (ver Assets/ZS/UI/README.md)
├── LevelPlay/Editor/, LevelPlay/Resources/                 gerados pelo pacote Ads Mediation (dependências nativas; LevelPlayMediationSettings: permissão AD_ID ligada)
├── MobileDependencyResolver/                               resolvedor das dependências nativas (importado pelo LevelPlay)
├── Plugins/Android/                                        mainTemplate.gradle e gradleTemplate.properties (criados pelo resolvedor)
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
| `Editor/` | `GridBattle.Editor[.…]` | `GameDatabaseBuilder`, `GridControllerEditor`, `Drawers/SubclassPickerDrawer`, `Localization/LocalizationTableTool`, `Tools/PlaceholderIcons`, `Tools/VfxPlaceholders` (`GridBattle > Vfx > Generate Placeholder Visuals`), `Tools/MonetizationMenu` (`GridBattle > Monetization`), `Tools/ContentValidator` (`GridBattle > Validate Content`), `Talents/{TalentChecks,XpProgressionSimulator}`, `Simulation/BalanceSimulation` (menu e Play Mode do simulador) |
| `Managers/` | `GridBattle.Managers` | `GameStateManager` (classes jogáveis), `RunManager` (a run; acampamento, melhorias de skill, recompensa da elite), `TurnManager` (turnos; inimigos na ordem de `ActingOrder`), `GameConfigManager`, `GameReset` (reset total, só em build de desenvolvimento) |
| `Managers/Audio/` | `…Managers.Audio` | `AudioManager` (autoinicializado), `AudioLibrary`, `ESfx`, `EMusicContext`, `AudioEventHooks` |
| `Gameplay/` | `GridBattle.Gameplay` | `GridController` (andar por caminho: `WalkEntity`; inclui terreno temporário: `SetTerrain(célula, terreno, rodadas)` e `TickTemporaryTerrain`; troca de lugar: `SwapCharacters`), `Cell` (marcas dos golpes, dano previsto), `BattleController` (vitória/derrota), `CellContentHealthBar`, `EncounterConfig`, `ETurnOwner`, `GameplayInput(Settings)` (`confirmTargetedActions`: mirar e confirmar) |
| `Gameplay/Stats/` | `…Gameplay.Stats` | `EAttribute`, `AttributeModifier`, `CharacterStats` |
| `Gameplay/States/` (+`Effects/`) | `…Gameplay.States[.Effects]` | `StateDefinition`, `StateEffect` (ganchos: atributos, turnos, dano causado e recebido, bônus de dano condicional, modificadores de skill) e efeitos (`SkillModifierEffect`, `ConditionalDamageEffect`, `OnKillEffect` (+`grantExtraWalk`), `ApplyStateOnHitEffect`, ...); talentos de regra em `RuleTalentEffects.cs` (`DominoEffect`, `DisplacementTrailEffect`, `SlipperyPushEffect`, `CounterstrikeEffect`, `SwapStepEffect`); `StateGrant`, `StateInstance`, `StateContainer` (`FindEffect<T>`/`HasEffect<T>`: consulta dos talentos de regra) |
| `Gameplay/Combat/` | `…Gameplay.Combat` | `CombatSettings`, `DamageCalculator`, `CombatResolver` (`Adjust`, `PredictDamage`), `DamageContext` (+`DamageAdjustment`, `EDamageKindMask`), `HitResult`, `DisplacementResolver` (aplica e prevê empurrões e puxões; Dominó e rastro dos talentos de regra) |
| `Gameplay/Turns/` | `…Gameplay.Turns` | `TurnBlockers`, `ActingOrder` (ordem fixa dos inimigos na batalha: `Enemy.ActOrder`, empate por linha e coluna) |
| `Gameplay/Forecast/` | `…Gameplay.Forecast` | previsão (estilo *Into the Breach*, pura): `ActionForecast` (o que a ação mirada faria: área, dano, mortes, deslocamentos, posições finais, interrompidos), `ThreatForecast` (+`TelegraphForecast`: o que os golpes anunciados fariam, depois da ação), `ForecastView` (desenha no tabuleiro; adicionada pelo `GridController` em Play Mode), `ForecastSettings`, `ForecastSprites` (feitos em código: setas 10×10, listras da marca dos golpes, selos) |
| `Gameplay/Rules/` | `…Gameplay.Rules` | `GridRules` (inclui `GetWalkCells`: para onde um personagem pode andar, mostrado ao tocar num inimigo; `GetReachCells`/`GetSkillReachCells`: onde um inimigo pode golpear no próximo turno, sem uso no jogo hoje), `GridDistance`, `Displacement` (regras puras de empurrar e puxar: direção, caminho, colisão, `GetDistance` com o Gelo Escorregadio), `LineOfSight` (linha de visão das skills), `SkillSelection` (área de seleção das skills: forma, alcance mínimo e máximo) |
| `Gameplay/Movement/` | `…Gameplay.Movement` | `GridMovementAnimator`, `GridEffectsAnimator`, `GridMovementSettings`, `ActionVisualDefinition` (+`ECasterGesture`, `EVisualCellOrder`, `EVisualFacing`: visual de skill/item) e `ActionVisualPlayer` (toca o visual) |
| `Gameplay/Entities/` | `…Gameplay.Entities` | `GridEntity` → `Character` → `PlayerCharacter` / `Enemy` (golpe preparado `Telegraph`, `ActOrder`); `CharacterView`, `EnemyIconsView` (+ordem de ação, `SetFocused`: ícone do golpe do inimigo tocado), `CharacterFactory`, `CharacterScaling`, `ECharacter` |
| `Gameplay/Entities/Configs/` | `…Gameplay.Entities` ⚠️ | `CharacterConfig`, `EnemyConfig`, `PlayerCharacterConfig` (**exceção**: namespace sem `Configs`) |
| `Gameplay/Entities/Skills/` | `…Entities.Skills` | `SkillDefinition` (+telegrafia, variantes `upgrades`/`upgradeOf`, skill canalizada `usesWalk`), `SkillTelegraph` (golpe anunciado em preparo, salvo com a batalha), `EffectiveSkill` (+`SkillModifiers`: os números de uma skill para um lançador), `SkillEffect` (+`DisplaceSkillEffect`; em `MovementSkillEffects.cs`: `SwapSkillEffect`, `DashToTargetSkillEffect`, `CreateTerrainSkillEffect` + `ETerrainPlacement`), `SkillArea`, `SkillTargeting`, `SkillCooldowns`, `SkillSettings` |
| `Gameplay/Entities/Roles/` | `…Entities.Roles` | `EnemyRoleDefinition` |
| `Gameplay/Entities/Interfaces/` | `…Entities.Interfaces` | `IDamageReceiver`, `IAttacker`, `IWalker` |
| `Gameplay/Controllers/` | `…Gameplay.Controllers` | `CharacterControllerBase<T>`, `PlayerCharacterController` (toque, skills, itens, destaques; turno com andar e agir, `UndoMove`, `EndTurn`; mirar e confirmar: `AimedCell`, `ConfirmAim`, `CancelAction`; skills canalizadas: `CanUseSkillNow`, `CanWalkNow`; troca de lugar do Passo Trocado; andar do inimigo no toque), `EnemyController` (dispara o golpe preparado e roda a IA; memória) |
| `Gameplay/AI/` (+`Actions/`) | `…Gameplay.AI[.Actions]` | `EnemyBehavior` (um andar e um agir por turno), `EnemyAction` (`IsMovement`), `EnemyMemory`, `EnemyTurnContext`; ações (`UseSkills` (mira onde a área cobre mais casas para onde o jogador pode fugir: `coverEscapeCells`), `ApproachForSkillAction` (anda até onde uma skill pronta alcança o jogador, evitando células marcadas por golpes e terreno perigoso), `ApplyStateToTarget`, `HealAlly`, `BuffAlly`, `Summon`; `BasicAttack`, `ChaseTarget` e `KeepDistance` existem, mas nenhum comportamento usa); `DisplacementScoring` (valor de um empurrão/puxão para a IA, pesos em `UseSkills`) |
| `Gameplay/Terrain/` | `…Gameplay.Terrain` | `TerrainDefinition`, `TerrainGenerator`, `TerrainEffects`, `TerrainGenerationSettings` |
| `Gameplay/Consumables/` | `…Gameplay.Consumables` | `ConsumableDefinition` (`ignoresLineOfSight`), `ConsumableEffect` (curar, dano, estados), `BoardConsumableEffects.cs` (`DisplaceConsumableEffect`, `SwapConsumableEffect`, `CreateTerrainConsumableEffect`), `ConsumableInventory`, `ConsumableRules` (`HasLineOfSight`), `ConsumableExecutor`, `ConsumableGrant`, `ConsumableSettings` |
| `Gameplay/Progression/` | `…Gameplay.Progression` | `ProgressionSettings` (curva de XP, nível máximo, vida máxima por nível), `XpRewardSystem` (`xpPerPacket`, `maxPackets`: no máximo 8 orbs por abate), `XpPacket` |
| `Gameplay/Talents/` | `…Gameplay.Talents` | `TalentDefinition`, `SynergyTagDefinition`, `TalentOfferSettings` (inclui `sharedPoolWeightMultiplier`), `TalentOfferGenerator`, `TalentRules`, `TalentSession`, `TalentService`, `TalentApplier`, `TalentRunModifier`, `TalentBootstrap` |
| `Gameplay/Map/` | `…Gameplay.Map` | `MapGenerator` (nó de elite só onde há elite: `BattleGenerationSettings.HasEliteAt`), `MapValidator`, `MapRules` (`IsEliteBattle`, `NormalizeDifficulties`), `BattleGenerator` (+`ThreatCalculator`; a elite das batalhas de elite), `MapGenerationSettings`, `BattleGenerationSettings` |
| `Gameplay/Run/` | `…Gameplay.Run` | `RunState` (modelo salvo; `EBattleDifficulty` Normal/Elite; fases `Camp` e `SkillUpgrade`, `ESkillUpgradeSource`, `PlayerRunState.MaxHpBonus`, recompensa da elite em andamento: `EliteRewardOffer`, `EliteOffersLeft`), `RunSettings` (acampamento, custo do nó de talento, recompensa da elite), `RunSummary`, `PlayerRunStateApplier`, `RunPlayerHooks` (+`IRunPlayerModifier`), `StateSnapshots` |
| `Gameplay/Monetization/` | `…Gameplay.Monetization` | monetização (GDD 7): `MonetizationService` (autoinicializado; anúncio forçado pendente, anúncio antes de cada uso das ferramentas de talento, compra), `ForcedAdRules` (rampa de frequência, pura), `MonetizationSettings`, provedores de anúncio (`LevelPlayAdsProvider`, `SimulatedAdsProvider`, `NoAdsProvider` em `AdProviders.cs`) e de loja (`UnityIapRemoveAdsStore`, `SimulatedRemoveAdsStore`, `NoRemoveAdsStore` em `RemoveAdsStores.cs`) |
| `Gameplay/Simulation/` | `…Gameplay.Simulation` | simulador de balanceamento: `SimMode` (modo sem animações, sons, UI nem salvamento), `SimulationRunner`, bots (`BattleBot`, que lê a previsão como o jogador e só usa uma skill canalizada antes de andar; `MapPolicy`, com as políticas `EMapPolicy` Normal/Elite/Mixed, que responde ao acampamento, à melhoria e às ofertas de consumível, inclusive a da elite; `TalentPolicy`), `SimulationCollector`, `SimulationReport`, `SimulationOptions`, `BalanceSimulationSettings` |
| `Gameplay/Meta/` | `…Gameplay.Meta` | `ProfileService`/`ProfileState`, `GlossaryService`, `TutorialService` + `TutorialTipDefinition`, `MetricsRecorder`, `MetaSettings`, `MetaBootstrap` |
| `Gameplay/Events/` | `…Gameplay.Events` | eventos de gameplay (os do novo molde: `ActionAimChangedEvent`, `TelegraphResolvedEvent`, `CharactersSwappedEvent`, `ExtraWalkGrantedEvent`, `CampEnteredEvent`, `SkillUpgradeOfferEvent`, `SkillUpgradedEvent`) |
| `UI/` | `GridBattle.UI` | `XpBarView`, `PixelPerfectPanelScale`, `UiExtensions` (`OnClick`, `SimulateClick`), `LongPressManipulator` (toque longo / clique direito em elementos de UI) |
| `UI/Flow/` | `…UI.Flow` | `GameFlowController` (eventos → navegação) |
| `UI/Screens/` | `…UI.Screens` | controladores das telas (Start, MainMenu, Game, Map, RunEnd, Victory), `ScreensSettings` |
| `UI/Hud/` | `…UI.Hud` | `SkillBarView`, `ItemBarView` (toque longo nos botões abre os detalhes), `DepthView`, `TurnActionsView` (pé, espada e poção do que resta no turno, Desfazer e Encerrar turno; Cancelar e Confirmar com uma ação mirada), `EntityDetailsModel` (+`EntityDetailsRequest`: célula, skill ou item; ordem de ação e golpe preparado do inimigo), `ActionDetailsModel` (ficha de skill e item; linha "Em você" com os estados no lançador e nota da skill canalizada), `HudText`, `HudSettings` |
| `UI/Map/` | `…UI.Map` | `MapGraphView` (nós `map-node--normal`/`--elite`, sem XP), `MapLinesElement`, `MapHudView`, `NodePreviewView` (título, papéis, terreno e, na elite, a linha `preview-reward`) |
| `UI/Overlays/` | `…UI.Overlays` | controladores dos modais (menu/pausa, detalhes, oferta de consumível, confirmação, talento, glossário, opções, dicas, detalhes do personagem, remover anúncios, aviso de anúncio, anúncio de teste, acampamento e melhoria de skill em `SkillUpgradeController`; a oferta de consumível tem o título da recompensa da elite), `ModalsSettings` |
| `UI/Background/`, `UI/Vfx/`, `UI/Events/` | `…UI.*` | fundo, orbs de XP (`XpOrbsVfx`: explosão e voo juntos; `XpOrbsVfxSettings`), eventos de UI |

### Objetos da cena

| GameObject | Componentes |
|---|---|
| `Main Camera` | `PixelPerfectCamera`, `PixelPerfectPanelScale`, `Physics2DRaycaster` |
| `GameStateManager` | `GameStateManager` (singleton, `DontDestroyOnLoad`), `RunManager` |
| `Grid` | `GridController` (`[ExecuteAlways]`), `TurnManager`, `BattleController`; em Play Mode o `GridController` adiciona a `ForecastView`; células `Cell_x_y` são filhas |
| `XpRewardSystem` | `XpRewardSystem` |
| `UIManager` | `UIRoot`, `GameFlowController`; filhos (cada um `PanelRenderer` + `UILayer`): `BackgroundLayer` (999 → -1), `StartScreenLayer` (0), `MainMenuLayer` (1, + Animator), `GameScreenLayer` (2, + `XpOrbsVfx`), `ScreensLayer` (3, telas `Instantiate`: mapa, fim de run, vitória), `Overlays/MenuOverlayLayer` (5, camada dos modais), `TransitionLayer` (999) |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` |

Serviços sem objeto na cena (autoinicializados por
`[RuntimeInitializeOnLoadMethod]`): `AudioManager`, `MetaBootstrap` (perfil,
dicas, métricas), `TalentBootstrap` (talentos) e `MonetizationService`
(anúncios e compra).

## Fluxos principais

```
Telas (GameFlowController + ZS.UI)
  Start → MainMenu: menu inicial (Novo jogo, Continuar só com RunManager.HasContinuableRun, Glossário, Opções)
     Novo jogo → seleção de personagem (mesma view; classes bloqueadas com progresso, Voltar)
     card → CharacterDetailsRequestedEvent → modal de detalhes → Jogar → CharacterChoosenEvent
     (com run em andamento, confirmação) → RunManager.StartNewRun → MapOpenedEvent → tela do Mapa
  nó escolhido (SelectNode = prévia; EnterNode = confirma)
     batalha/chefe → BattleStartedEvent → tela Game (HUD)  → vitória → mapa | elite → mapa + consumível + melhoria | chefe → Vitória
     acampamento / consumível / talento → resolvidos no mapa (modais) → mapa
  derrota ou desistência → RunEndedEvent → tela de fim de run → MainMenu
  pedidos de tela durante uma transição viram "tela alvo" e são aplicados depois

Run (RunManager)
  StartNewRun: semente (RunSettings.debugSeed ou aleatória) → MapGenerator (mapa + BattleSpec de cada
    batalha: inimigos, terreno, XP) → RunState → salva run.json
  geração: cada batalha lê os assets de balanceamento na profundidade de balanceamento do nó
    (MapGenerationSettings.GetBalanceDepth: andares de referência esticados para o tamanho do mapa);
    XP da batalha = orçamento de ameaça (profundidade × dificuldade) × XP por ponto × crescimento por
    profundidade × fator do tamanho da run, dividido entre os inimigos pela ameaça (fácil < normal < difícil);
    a ameaça de um inimigo é EnemyConfig.threat quando definida. Batalhas só normal ou de elite
    (EBattleDifficulty; pesos MapGenerationSettings.normalWeight 75 / eliteWeight 25): um nó só vira elite se
    BattleGenerationSettings.HasEliteAt(profundidade de balanceamento) (os andares 1 a 3 não têm); batalha de
    elite = um inimigo isUnique do pool na profundidade, com escolta (perfil Elite: orçamento ×1,35, XP ×2).
    Saves antigos: MapRules.NormalizeDifficulties (fácil, e difícil sem elite, viram normal) no ContinueRun
  vida, vida máxima extra (MaxHpBonus), nível, XP, skills (já melhoradas), consumíveis e talentos persistem
    entre nós (PlayerRunState)
  nós fora da batalha: acampamento (nó de cura) → CampEnteredEvent → descansar (CampRest: cura
    RunSettings.healFraction) ou treinar (CampTrain → melhoria); sem skill para melhorar, só cura.
    Talento → custa vida máxima pelo resto da run (ETalentNodeCost.MaxHp, talentNodeCostMaxHp) →
    TalentNodeEnteredEvent. Consumível → escolher 1 entre ConsumableSettings.choiceCount
  elite vencida → BeginEliteReward: RunSettings.eliteConsumableOffers (1) ofertas de consumível (a do nó de
    consumível, ConsumableOfferEvent com o título da elite) → melhoria de skill (eliteGrantsSkillUpgrade) →
    CompleteNode com a vida da batalha; cada etapa salva (RunState.EliteRewardOffer, EliteOffersLeft)
  melhoria de skill (treino no acampamento ou elite vencida, RunSettings.eliteGrantsSkillUpgrade) →
    SkillUpgradeOfferEvent → modal SkillUpgrade (skill → 1 de 2 variantes, com o que muda) →
    ChooseSkillUpgrade troca o id em PlayerRunState.SkillIds → SkillUpgradedEvent; recusar volta à
    escolha do acampamento ou, na elite, abre mão da recompensa. Cada skill melhora uma vez
  batalha: GridController.InitializeBattle(spec, classe, PlayerRunState) → terreno → jogador → inimigos
  salvamento: depois de cada nó e no início de cada vez do jogador (antes dos efeitos de início de turno;
    carregar repete o turno com os mesmos sorteios). ContinueRun retoma mapa, nó aberto ou batalha.

Turno (TurnManager)
  turno global: o jogador age primeiro, depois cada inimigo, um de cada vez, na ordem fixa da batalha
    (ActingOrder: a lista gerada, depois os invocados; o número fica sobre cada um): o inimigo age, as
    animações dele terminam e só então o próximo começa. Fim da rodada: o terreno temporário perde uma
    rodada (GridController.TickTemporaryTerrain), antes do salvamento da vez do jogador
  turno da entidade: início (EntityTurnStartedEvent; estados: dano/cura periódicos; terreno OnTurnStart)
                     → ação → fim (EntityTurnEndedEvent; terreno OnTurnEnd, cooldowns, memória da IA;
                     depois a duração dos estados cai 1)
  vez do jogador: anda 1 vez e age 1 vez (uma skill; não há ataque básico), em qualquer ordem, e pode usar
    1 consumível. Skill canalizada (usesWalk: Meteoro e Tempestade Elétrica) gasta o agir e o andar, só antes
    de andar (PlayerCharacterController.CanUseSkillNow); o Meteoro deixa o Mago Exausto no turno seguinte. Skill ou item: o 1º toque numa célula válida só mira (AimedCell, ActionAimChangedEvent; a
    previsão mostra dano, mortes, empurrões e onde os golpes anunciados cairiam depois), o 2º toque na
    mesma célula ou Confirmar usa (ConfirmAim), Cancelar desfaz a mira (CancelAction). Tocar num inimigo
    mostra até onde ele pode andar (GridRules.GetWalkCells). A vez acaba sozinha só quando nada mais é possível
    (HasAnythingLeft falso depois de uma escolha; com item utilizável espera), ou com Encerrar turno /
    tocando em si mesmo → PlayerActionEvent; desfazer o andar enquanto não agiu
    (PlayerCharacterController.UndoMove); PlayerTurnStateChangedEvent a cada mudança
  vez do inimigo (EnemyController): dispara o golpe preparado, se houver (Enemy.AdvanceTelegraph: atinge
    quem estiver nas células marcadas agora, todos menos quem lança; TelegraphResolvedEvent), depois anda
    (ApproachForSkillAction) e prepara o próximo golpe (UseSkills → Enemy.TryUseSkill marca as células; tenta
    na ordem da lista: o especial com recarga primeiro, o básico sem recarga quando o especial não está pronto;
    mira onde a área cobre mais casas para onde o jogador pode andar, UseSkillsAction.coverEscapeCells).
    Disparar não gasta o agir (CombatSettings.firingUsesAction desligado). Curar, proteger, invocar e
    enfraquecer são imediatos. A batalha começa sem golpe preparado: cada inimigo prepara o primeiro no
    seu turno, depois da primeira vez do jogador
  golpe preparado: empurrar, puxar ou trocar de lugar quem prepara leva as células junto
    (SkillDefinition.telegraphFollowsCaster); a morte dele ou um estado que impede skills o cancela
  cada passo espera animações (GridController.WaitForMovementsAsync) e TurnBlockers
    (orbs de XP que sobem de nível, escolha de talento, pausa, detalhes, dicas)
  último inimigo ou jogador morre → BattleDecidedEvent (vez = None, ninguém age)
    → após animações/orbs/talentos → BattleEndedEvent(vitória)

Dano (CombatResolver)
  estados de atacante e alvo opinam (Adjust: bônus condicionais, puros) → (base + fixo) × (1 + bônus de skill)
  × (1 + dano causado) × (1 + bônus condicional) → crítico (fluxo Combat) → defesa (fixa ou %, penetração;
  Blindado = +1 de defesa) × (1 + dano recebido) + dano recebido fixo (Exposto +1) × (1 + redução
  condicional) → mínimo (CombatSettings.minimumDamage 0: a armadura pode zerar um golpe de 1) → escudo
  absorve → vida; depois roubo de vida (atacante) e espinhos (alvo)
  colisão de empurrão ou puxão: 1 a quem é lançado (+0 por casa não percorrida) e 1 a quem é atingido,
  sem defesa (CombatSettings.collisionIgnoresDefense)
  previsão sem sorteio: CombatResolver.PredictDamage (previsão do jogador, IA e bot)

XP e talentos
  inimigo morre → XpRewardSystem (no máximo maxPackets = 8 pacotes; acima disso, pacotes maiores) → orbs
    (XpOrbsVfx: todos saem juntos numa mini explosão no ponto da morte, param e voam juntos, ~0,8 s no total)
    → PlayerCharacter.GainXp a cada orb que chega (curva global:
    ProgressionSettings, 150 para o nível 2 e +100 a cada nível, nível máximo 12)
  cada nível → +1 de vida máxima sem cura (maxHpPerLevel; levelUpHealFraction 0) → PlayerLeveledUpEvent
    → TalentSession: oferta (TalentOfferGenerator, Derive(Talents, nível, reroll)) → pausa (TurnBlockers)
    → TalentOfferOpenedEvent → modal de talento → Choose/Reroll/Ban/Skip
  talento: estados permanentes com SourceId = id do talento (+pilhas = rank), skill liberada vai para a barra
    (até TalentOfferSettings.maxSkillTalents = 5); reaplicado a cada batalha por IRunPlayerModifier.
    Talentos de regra (Dominó, Rastro de Chamas, Gelo Escorregadio, Contragolpe, Passo Trocado, Ímpeto)
    são efeitos de estado lidos onde a regra vale (StateContainer.FindEffect<T>)

Monetização (MonetizationService, GDD 7)
  NodeCompletedEvent / RunEndedEvent → conta uma pausa no perfil → ForcedAdRules.IsDue (carência, fase
    inicial, frequência normal, tempo mínimo) → anúncio forçado pendente
  GameFlowController (mapa, fim de run ou menu parados, sem modal) → aviso (AdNotice) → intersticial
    → a cada N anúncios, janela de remover anúncios
  janela de talento: trocar/banir (ao tocar a carta)/pular → PayForTalentToolAsync → recompensado
    assistido até o fim → TalentService.Reroll/Ban/Skip (conta o uso; grátis depois da compra, sem anúncios
    ou sem anúncio disponível)
  compra: pedido pendente → perfil marca AdsRemoved e salva → só então ConfirmPurchase

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
  sempre por `Character.Stats`, nunca direto do config. A vida máxima base do
  jogador soma os níveis (`ProgressionSettings.GetMaxHpBonus`) e o bônus da run
  (`PlayerRunState.MaxHpBonus`, negativo depois de um nó de talento): quando um
  deles muda, chame `Character.RefreshBaseAttributes` (a vida não é curada).
- **Estados**: a duração conta turnos de quem carrega e cai no fim do turno
  dele (GDD 3.1). O bônus de duração dos talentos (`StateDurationBonus`) não vale
  para estados que uma skill aplica no próprio lançador (Exausto do Meteoro). Reaplicação segue a `EStackPolicy` do estado; instâncias de
  origens diferentes (`SourceId`, ex.: talento) ficam separadas.
- **Dano e cura** passam sempre por `CombatResolver` (crítico, defesa,
  escudo, roubo de vida, espinhos, eventos). `Character.ReceiveDamage` é dano
  puro. Previsões (do jogador, da IA e do bot) usam `CombatResolver.PredictDamage`,
  nunca `DamageCalculator` direto, para valer as passivas condicionais. Com
  `CombatSettings.minimumDamage` 0, defesa e redução podem zerar um golpe.
- **Números de skill** (dano, área, alcance, cooldown, distância de empurrão,
  duração de estados, efeitos) só pelo `EffectiveSkill.Resolve(lançador, skill)`:
  alvo, execução, cooldown, destaques, IA e bot passam por ele, e o asset da skill
  nunca é lido direto para esses números. Modificadores vêm de
  `SkillModifierEffect` nos estados do lançador (talentos de classe).
- **Passivas condicionais** (`ConditionalDamageEffect`, `OnKillEffect`,
  `ApplyStateOnHitEffect`): os ganchos `ModifyOutgoingDamage` /
  `ModifyIncomingDamage` são **puros** (sem sorteio, sem mudar o jogo), porque a
  previsão os chama também. "Andou neste turno" é
  `Character.MovedThisTurn` (antes do golpe; desfazer o andar desmarca); adjacência pela métrica
  `CombatSettings.adjacencyMetric` (`GridRules.CountAdjacentOpponents`).
- **Sem ataque básico.** `CharacterConfig.hasBasicAttack` está desligado em
  todos os personagens (o padrão do código é ligado): agir é sempre usar uma
  skill, e `attackDistance`/`basicAttackDamage` não são usados. Ações e destaques
  que dependem do ataque básico consultam `Character.HasBasicAttack`.
- **Turno: andar e agir.** O turno tem um andar e um agir (uma skill), em
  qualquer ordem, mais um consumível. Skills e itens com alvo são mirados e
  depois confirmados (`GameplayInputSettings.confirmTargetedActions`;
  `PlayerCharacterController.AimedCell`, `ConfirmAim`, `CancelAction`). A vez do jogador termina com
  `PlayerActionEvent` (quando nada mais é possível, no Encerrar turno ou no toque em si
  mesmo); o que ainda resta fica no `PlayerCharacterController` (`CanMove`,
  `CanAct`, `CanUseConsumable`, `CanUndoMove`) e sai em
  `PlayerTurnStateChangedEvent`. **Skill canalizada** (`SkillDefinition.usesWalk`, só
  jogador): gasta também o andar, só antes de andar e sem desfazer; UI e bot
  perguntam `PlayerCharacterController.CanUseSkillNow` (e `CanWalkNow` para "pode andar para
  algum lugar"); `HasAnythingLeft` conta uma canalizada pronta enquanto o andar pode
  ser desfeito. O contra "não anda no próximo turno" é dado: `ApplyStatesSkillEffect`
  no lançador com Exausto por 2. Inimigos: `EnemyAction.IsMovement` separa andar
  de agir, os inimigos agem na ordem fixa de `Turns/ActingOrder` (nunca ordene
  por conta própria) e o `TurnManager` só começa o próximo inimigo depois das
  animações do anterior. Desfazer o andar usa `GridController.UndoMove` (sem
  `EntityEnteredCellEvent`); só vale sem gatilho `OnEnter` na célula. O ataque ou
  o visual de skill de quem acabou de andar espera o fim do pulo
  (`GridController.GetMovementRemaining`). Recargas contam turnos do dono.
  "Andou" nos bônus condicionais é `Character.MovedThisTurn`.
- **Golpes anunciados (telegrafia).** Todo golpe de inimigo é uma skill com
  `telegraphed` (hoje `windupTurns` 1) e alvo `EveryoneButCaster`: `Enemy.TryUseSkill`
  prepara um `SkillTelegraph` (células fixadas na hora, salvas com a batalha) e,
  no turno seguinte do inimigo, `Enemy.AdvanceTelegraph` dispara nas células
  marcadas contra quem estiver nelas, inimigos inclusive (`TelegraphResolvedEvent`).
  Empurrar, puxar ou trocar quem prepara desloca as células junto
  (`SkillTelegraph.Shift`, `telegraphFollowsCaster`); `cancelledByDisplacement`
  e `interruptibleByStates` cancelam. A IA mira onde a área cobre mais casas para
  onde o jogador pode andar no próximo turno (`UseSkillsAction.coverEscapeCells` e
  pesos no asset `UseSkills`), não só a casa dele. O jogador nunca telegrafa. **Todo inimigo
  precisa de um golpe com recarga 0 (básico) e um com recarga > 0 (especial,
  primeiro na lista `skills`)**: a recarga começa no disparo, e sem o básico a
  marca some nos turnos de espera. Lacunas que continuam: turno em que nenhum
  golpe alcança o jogador (o Totem nunca anda), ação de apoio imediata (curar,
  proteger, invocar, enfraquecer usa o agir) e o primeiro turno da batalha.
- **Previsão** (`Gameplay/Forecast`): `ActionForecast` e `ThreatForecast` são
  puras e usam as mesmas regras da execução (`EffectiveSkill`,
  `SkillTargeting`, `CombatResolver.PredictDamage`,
  `DisplacementResolver.Predict`, `DashToTargetSkillEffect.GetDestination`,
  `SkillTelegraph.PredictDamage`), sem sorteio (sem crítico) e antes dos
  escudos. Toda regra nova que muda dano, posição ou quem é atingido tem de
  valer nas duas (de preferência pela mesma função), senão a previsão mente.
  Pendência: o empurrão em cadeia do Dominó ainda não entra na previsão
  (`DisplacementResolver.Predict` não o calcula). O bot do simulador lê as
  mesmas classes. `ForecastView` só desenha.
- **Skills que movem e criam terreno** (`MovementSkillEffects.cs`): troca de
  lugar (`SwapSkillEffect`) e investida (`DashToTargetSkillEffect`, antes de um
  `Displace`) não são andar (`Character.MovedThisTurn` não muda);
  `CreateTerrainSkillEffect` põe terreno temporário por `GridController.SetTerrain(célula,
  terreno, rodadas)` (só em piso livre por padrão; terreno que bloqueia nunca
  numa célula ocupada), que perde uma rodada no fim de cada rodada dos inimigos
  e some em 0 (`TickTemporaryTerrain`; rodadas salvas em `TerrainCellSpec.TurnsLeft`).
- **Talentos de regra** (`States/Effects/RuleTalentEffects.cs`, `OnKillEffect.grantExtraWalk`):
  efeitos de estado sem gancho de dano, consultados no ponto onde a regra vale
  com `States.FindEffect<T>()` (Dominó e rastro no `DisplacementResolver`,
  Gelo Escorregadio em `Displacement.GetDistance`, Contragolpe no
  `PlayerCharacter`, Passo Trocado no `PlayerCharacterController`).
- **Melhorias de skill**: uma variante é um `SkillDefinition` inteiro com
  `upgradeOf` apontando para o original, listado em `upgrades` do original (2
  por skill; a variante não melhora de novo). Compare skills por
  `SkillDefinition.IsSameSkillAs`/`Root` (pré-requisitos, ofertas): um
  `SkillModifierEffect` do original vale também para as variantes.
- **Áreas**: as formas centradas no alvo têm tamanho ímpar; `Linear` começa no
  alvo e pode ter tamanho par (`SkillDefinition.NeedsOddSize`).
- **Andar** só por `GridRules.CanWalkTo`: a casa final livre, dentro do alcance e alcançável por um caminho de casas
  ortogonais livres (`HasWalkPath`): terreno que bloqueia **e personagens** (jogador e inimigos) são obstáculos; não
  se passa na diagonal entre dois deles. Destaques, IA, bot e o toque no inimigo usam a mesma regra. O andar é
  executado por `GridController.WalkEntity`, que segue esse caminho (`GridRules.FindWalkPath`) célula a célula:
  `EntityPassedCellEvent` em cada célula cruzada, `EntityEnteredCellEvent` no destino. Efeito novo "ao passar por
  uma célula" escuta o `EntityPassedCellEvent` (e, se muda dano ou posição, entra também na previsão).
  `MoveEntity` é o movimento direto (Teleporte).
- **Distâncias** só por `GridDistance` com as métricas de `CombatSettings`
  (movimento/ataque euclidianos; alcance de skill Manhattan).
- **Empurrar e puxar** (deslocamento forçado) só por `Displacement` (regras
  puras, usadas também para prever) e `DisplacementResolver` (aplica):
  direção de 8 vizinhos, parada em obstáculo/borda/personagem, dano de colisão
  `EDamageKind.Collision` (valores em `CombatSettings`) atribuído ao lançador,
  terreno da célula final reage na hora (`GridController.ApplyForcedTerrain`).
  Personagem imóvel: `CharacterConfig.canBeDisplaced = false` (o chefe).
  Talentos de regra entram aqui: Dominó empurra quem foi atingido, o rastro
  deixa terreno e o Gelo Escorregadio aumenta a distância (este último também na
  previsão, por `Displacement.GetDistance`).
- **Itens com alvo** (`ConsumableRules`) seguem a mesma linha de visão das skills (`HasLineOfSight`; `ignoresLineOfSight`
  no item para algo que cai do alto) e só são usáveis quando um efeito mudaria algo (`ConsumableEffect.IsUseful`).
- **Mira de skills**: célula na **área de seleção** (`Rules/SkillSelection`, estilo Dofus: forma
  `ESkillSelectionShape` Círculo/Linha/Diagonal/Estrela/Quadrado, `minRange` e `range` contados ao longo
  da forma, `rangeModifiable` para o atributo Alcance de skills), com o conteúdo pedido
  (`ESkillTargetCell`: qualquer, vazia, ocupada) e em **linha de visão**
  (`SkillTargeting.HasLineOfSight` → `Rules/LineOfSight`: centro a centro;
  terreno com `blocksLineOfSight` e personagens vivos no caminho bloqueiam,
  as pontas não; canto não conta; simétrica). Skills com `ignoresLineOfSight`
  e centradas no lançador não precisam; `CombatSettings.skillsNeedLineOfSight`
  e `charactersBlockLineOfSight` configuram. `requiresAffectedTarget` está
  desligado em todas: a skill mira qualquer célula válida (destaque em todo o
  alcance).
- **Jogador e grid atuais**: `PlayerCharacter.Active` e
  `GridController.Instance` (registrados pelos próprios objetos; fora do Play
  Mode caem numa busca na cena). Não use `FindAnyObjectByType` para eles. As
  buscas de inimigos (`FindObjectsByType<Enemy>`) continuam onde estão: a ordem
  delas pode influenciar a IA e o bot.
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
- **Visual de skills e itens** (`ActionVisualDefinition`, campo `visual` da
  skill e do consumível): gesto de quem usa, projétil opcional e efeito em cada
  célula da área numa ordem que desenha a forma. `SkillDefinition.Execute` e
  `ConsumableExecutor` chamam `GridController.PlayActionVisual` **antes** do
  dano; cada personagem atingido tem a reação enfileirada para o impacto na sua
  célula (`QueueImpactAnimation`). O número de dano (`Cell`), o efeito de morte
  e os orbs de XP leem esse atraso (`GridController.GetPendingImpactDelay`).
  Arte num quadrado da largura da célula (serve para linhas e colunas),
  desenhada virada para a direita e girada só em passos de 90°. Nenhuma regra
  lê o visual.

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
  `GameFlowController`, `ActionVisualPlayer`, `MonetizationService`, `ForecastView`, `EnemyIconsView`). Com o modo desligado, o jogo não muda.
  Com `SimulationOptions.showBoard` (`SimMode.ShowBoard`) a única tela que o
  fluxo abre é a de batalha, para quem estiver olhando ver o tabuleiro.
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
- O UI Toolkit **não tem pseudo-classes estruturais** (`:first-child`, `:last-child`, `:nth-child`, `:not`):
  a regra inteira é descartada, com um aviso no console. Para tratar o primeiro ou o último item, ponha uma
  classe pelo código.
- Nomenclatura: kebab-case; modificadores com `--`; classes do framework com
  prefixo `zs-`.
- **Contratos de nome** (usados por código): `container`, `menu-button`,
  `xp-bar-progress`, `xp-bar-detail-2`, `current-level`, `tap-to-play-label`,
  `*-btn`, `overlay-background`, `skill-bar`, `item-bar`, `hud-depth*`,
  `close-button`, `details-*`, `map-*`, `node-<id>`, `preview-*` (inclui `preview-reward`, a recompensa da elite),
  `confirm-button`, `summary-*`, `offer-*`, `confirm-*`, `continue-btn`,
  `new-game-btn`, `back-btn`, `select-class-label`, `menu-buttons`,
  `glossary-btn`, `options-btn`, `locked-info`, `character-*`, `remove-ads-*`,
  `ad-notice-*`, `simulated-ad-*`, `talent-tools-hint`, `options-ads-label`,
  `options-remove-ads-button`, `summary-remove-ads-button`, `options-dev-label`, `options-reset-button`, `turn-actions`, `turn-move-icon`, `turn-act-icon`, `turn-item-icon`,
  `undo-move-btn`, `end-turn-btn`, `cancel-action-btn`, `confirm-action-btn`,
  `upgrade-title`, `upgrade-subtitle`, `upgrade-scroll`, `upgrade-back-button`,
  `upgrade-decline-button`; e os caminhos do Animator do
  menu (`#container/#character-selection/#<x>-btn/#<x>-image`). Não mude nome
  nem hierarquia sem atualizar o código/clip.
- Não use `<Template>`/`<Instance>` onde há seletor `>` ou caminho de Animator.
- UXML referencia USS por `project://database/...?guid=`; mova assets de UI
  pelo `AssetDatabase` (ou com o `.meta` junto).
- **Pixel perfect**: tamanhos inteiros (botões 32×32, ícones 16×16 ou 8×8
  nativos); nunca escala fracionária de sprite (no mundo, `EnemyIconsView` só
  amplia por inteiros). Tudo precisa caber em 216×384.

### Arte (padrão visual)

Vale para **toda arte nova** (ícones, terreno, inimigos, interface), feita por qualquer thread. O guia completo,
com os lotes, as escolhas e o passo a passo, é `docs/projeto/arte.md`; leia antes de gerar arte.

- **Ferramenta:** PixelLab (MCP). Ícones e sprites com `create_image_pro` (16×16 dá 64 opções por 10 gerações;
  uma lista numerada vira uma opção por item, na ordem). Animações com `animate_image`. Escolha olhando as opções
  ampliadas, nunca às cegas.
- **Paleta:** ENDESGA 32 em todos os ícones e terrenos (passe `palette` + `palette_name`; a lista de cores está no
  guia). Personagens e inimigos usam o estilo do `Characters.png` como imagem de referência (`style_image`). Peças
  de interface (molduras, slots, botões) usam as cores do `UI.png`, não a paleta dos ícones.
- **Traço:** contorno escuro de 1 px (`#181425`), silhueta forte e legível no tamanho real, objeto centralizado,
  fundo **transparente** (confira; o modelo às vezes desenha um quadrado de fundo).
- **Tamanhos nativos, sem escala fracionária:** ícones de interface 16×16; insígnias sobre inimigos 8×8 (desenhe à
  mão na paleta, o PixelLab não gera menos de 16); terreno 24×24 sem piso por baixo; personagens em quadros 24×30
  com pés na borda de baixo (pivô embaixo no centro); slots 32×32. Nunca redimensione arte em pixel: refaça no
  tamanho certo.
- **Linguagem visual:** cor por tema (fogo laranja, gelo ciano, vento branco, raio amarelo, veneno verde, Venom
  roxo, fumaça cinza, aço para golpes e escudos, vermelho para vida); sinais fixos de modificador (`+` amarelo =
  mais dano, anel = área maior, ampulheta = recarga menor, seta = alcance ou empurrão); ícone de skill mostra a
  ação, ícone de talento mostra a skill com o modificador; o estado concedido por um talento usa o ícone do
  talento; papéis de inimigo com cores diferentes entre si; nós do mapa em escala de perigo.
- **Inimigos novos têm arte própria** (não recolorir outro com `tint`): sprite parado de frente + animação parada
  de 8 quadros, no estilo dos existentes. Passo a passo e a armadilha do recorte em tamanhos não quadrados no guia.
- **Organização:** `Art/Sprites/Icons/<Categoria>/<NomeDoAsset>.png` (mesmo nome do asset que usa o ícone),
  `Art/Sprites/Terrain/`, `Art/Sprites/Enemies/<Nome>.png` + `Art/Animations/Enemies/<Nome>/`, `Art/Sprites/UI/`.
  Nada novo em `Placeholders/`. Para trocar arte, mova pelo `AssetDatabase` ou substitua o PNG no mesmo caminho
  (o GUID é a referência). Importação: Sprite, Point, sem compressão, 100 PPU, sem mipmaps.
- **Registro:** cada lote gerado entra na tabela "Origem de cada asset" do guia (lote, pedido, id da galeria e a
  opção escolhida), para dar para refazer no mesmo estilo.
- **Conferência:** veja a arte no jogo (captura em Play Mode, guardando e restaurando o perfil real) e rode
  `GridBattle > Validate Content`.

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
| Novo inimigo | `Create > GridBattle > Characters > Enemy Config`: sprite/animator com arte própria (ver "Arte (padrão visual)"; `tint` sobre arte existente só para provisórios), atributos, `hasBasicAttack` desligado, skills de golpe anunciado (linha abaixo): um **especial** com recarga > 0 primeiro e um **básico** com recarga 0 depois, `prefab = Enemy.prefab`, `behavior` (um `Approach*` + `UseSkills`), `role`, `behaviorDescription`, `xpReward`, `threat` (custo de complexidade para o gerador; 0 = calculado), estados iniciais. Textos na tabela `Content`. Para aparecer nas batalhas: entrada no pool de `BattleGenerationSettings` (profundidade mín./máx., peso). |
| Inimigo com golpe anunciado | Skill em `Skills/Enemies/` com `Telegraphed`, `Windup Turns` (1 = dispara no próximo turno dele), `Can Move While Winding Up`, `Cancelled By Displacement`, `Interruptible By States`, `Telegraph Follows Caster` e `affects = EveryoneButCaster` (acerta outros inimigos). A previsão e o bot passam a ler o golpe sozinhos. Todo inimigo tem ao menos um golpe de recarga 0 (básico) e um de recarga > 0 (especial, antes na lista), para haver golpe anunciado em todo turno. |
| Elite | Marque `isUnique` no `EnemyConfig` e ponha no pool de `BattleGenerationSettings`: nunca é sorteado como inimigo comum; cada batalha de elite leva uma elite disponível na profundidade, e o mapa só cria nós de elite onde há uma (`HasEliteAt`). Vencê-la dá mais XP (perfil Elite), `RunSettings.eliteConsumableOffers` ofertas de consumível e uma melhoria de skill (`eliteGrantsSkillUpgrade`). |
| Novo comportamento de IA | Subclasse de `EnemyAction` (ou `EnemyAbilityAction`, que já tem cooldown na `EnemyMemory`) + asset; combine num `EnemyBehavior`. Ação que só anda: `IsMovement => true` (ela usa o andar do turno, e as outras, o agir). Andar até de onde uma skill alcança o jogador: `ApproachForSkillAction` (`preferredDistance`: 1, 2 e 3 em `ApproachMelee`, `ApproachMid` e `ApproachRanged`). |
| Novo papel de inimigo | `Create > GridBattle > AI > Enemy Role` (ícone 8×8, fator de ameaça, conta como linha de frente). |
| Nova skill | `Create > GridBattle > Skills > Skill Definition`: tipo, dano, forma/tamanho de área (ímpar; `Linear` aceita par), área de seleção (forma, alcance mínimo e máximo, alcance modificável, célula alvo), cooldown, alvos, efeitos, melhorias (`upgrades`). Inicial de classe: lista `skills` do `PlayerCharacterConfig` (1 por classe, sem recarga); liberada por talento: `unlockedSkill` do talento; de inimigo: `skills` do `EnemyConfig`. |
| Melhoria de skill (variante) | Copie a skill para `Skills/<Classe>/Upgrades/`, mude o que melhora, ponha o original em `upgradeOf` e a variante na lista `upgrades` do original (2 por skill), com nome e descrição nos 3 idiomas. A janela de melhoria mostra sozinha as linhas da ficha que mudam. |
| Skill que troca de lugar, investe ou cria terreno | Efeitos `SwapSkillEffect` (troca com o personagem do alvo), `DashToTargetSkillEffect` (avança em linha reta; ponha antes de um `Displace`) e `CreateTerrainSkillEffect` (terreno, células `TargetCell`/`AreaCells`/`CasterCell`, rodadas, `replaceTerrain`). Terreno novo para isso: um `TerrainDefinition` comum (como `IceWall`), sem entrar no pool do gerador. |
| Skill que empurra ou puxa | Skill normal com o efeito `Displace` na lista de efeitos: modo (longe do lançador, em direção ao lançador ou longe do centro da área) e distância. Ele roda depois do dano; colisão e terreno seguem `CombatSettings`. Para inimigos, `UseSkills` só a usa quando o resultado previsto vale a pena (`DisplacementAiWeights` no asset `UseSkills`). |
| Personagem imóvel (chefe) | Desmarque `canBeDisplaced` no `CharacterConfig`: empurrões e puxões não o movem, mas ele ainda bloqueia e leva o dano de quem é lançado contra ele. Imunidades: `immuneStates` (estados ignorados, `StateContainer.Apply` devolve nulo) e `maxHpPercentDamageMultiplier` (parte do dano periódico que depende da vida máxima). |
| Novo estado | `Create > GridBattle > States > State Definition` + efeitos no dropdown (atributos, dano/cura periódicos, espinhos, roubo de vida, escudo, restrições, modificadores da run). |
| Novo talento | `Create > GridBattle > Talents > Talent` (classes permitidas, nível, pré-requisitos, tags, `maxRank`, estados, skill) e coloque no `talentPool` da classe ou no pool compartilhado de `TalentOfferSettings`. O peso dos que só vêm do pool compartilhado é multiplicado por `sharedPoolWeightMultiplier` (0,5). |
| Talento de regra | Estado com um efeito de `RuleTalentEffects.cs` (`DominoEffect`, `DisplacementTrailEffect`, `SlipperyPushEffect`, `CounterstrikeEffect`, `SwapStepEffect`) ou `On Kill Effect` com `grantExtraWalk` (Ímpeto); talento como acima. Regra nova: subclasse de `StateEffect` consultada com `States.FindEffect<T>()` no ponto onde a regra vale e, se ela muda dano ou posição, também na previsão (`Gameplay/Forecast`). |
| Talento que modifica uma skill da classe | Nenhum talento atual usa (os talentos de número saíram no novo molde). Estado com `Skill Modifier Effect` (skills, dano fixo/%, passos de área, alcance, redução de cooldown, empurrão, duração de estados, efeitos anexados; valores por pilha, `repeatAttachedPerStack`) + talento com `maxRank` (posto = pilha) e a skill (ou o talento que a libera) nos pré-requisitos. Ver `docs/projeto/guia_sistemas.md`. |
| Passiva condicional de classe | Estado com `Conditional Damage Effect` (causado ou recebido; tipos de dano; condição: por inimigo adjacente, sem adjacente, alvo com estado nocivo ou com um estado, andou neste turno, alvo isolado, vida cheia), `On Kill Effect` ou `Apply State On Hit Effect`; talento como acima. Condição nova: `EDamageCondition` + `ConditionalDamageEffect.Evaluate`. |
| Skill canalizada (gasta também o andar) | Marque `Uses Walk` na skill (só jogador). Para "não anda no próximo turno", um `Apply States` com destinatário `Caster` e o estado `Exhausted` por 2 (como no Meteoro). A ficha mostra a nota e a linha "Em você" sozinha. |
| Skill que não precisa de linha de visão | Marque `Ignores Line Of Sight` na skill (algo lançado por cima, um raio do céu). |
| Terreno que bloqueia (ou não) a visão | `Blocks Line Of Sight` no `TerrainDefinition` (só vale para terreno que bloqueia movimento; desligue para obstáculos baixos). |
| Conferir conteúdo novo | `GridBattle > Validate Content` (textos nos 3 idiomas, chaves de texto do código, referências vazias, talentos fora dos pools, inimigos que não aparecem, telas, ícones, banco de conteúdo). |
| Novo consumível | `Create > GridBattle > Consumables > Consumable Definition` (alvo, área, linha de visão, efeitos, peso) e adicione ao pool de `ConsumableSettings`. Efeitos: curar, dano, estados, empurrar/puxar, trocar de lugar, criar terreno. Um efeito novo que muda dano ou posição entra também em `ActionForecast.ForConsumable`. |
| Visual de skill ou item | `Create > GridBattle > Vfx > Action Visual` (gesto, projétil, quadros por célula, ordem, quadro do impacto, direção, queda, permanência) e ligue no campo `Visual` da skill/consumível. Provisórios: `GridBattle > Vfx > Generate Placeholder Visuals` (redesenha os PNGs e reescreve os visuais provisórios; só liga onde o campo está vazio). Ver `docs/projeto/guia_sistemas.md`. |
| Novo terreno | `Create > GridBattle > Terrain > Terrain Definition` (tipo, bloqueio, gatilho, dano/estados, visual) e adicione ao pool de `TerrainGenerationSettings`. |
| Nova dica de tutorial | `TutorialTipDefinition` com o `ETutorialTrigger` + `TutorialService.Notify(gatilho)` no ponto do jogo. |
| Novo personagem jogável | `Player Character Config` (`prefab = PlayerCharacter.prefab`, `characterClass`, `battlesToUnlock`, `talentPool`, a skill inicial e um talento que libera cada uma das outras); adicione em `GameStateManager.playableCharacters` e um botão no menu. |
| Ajustar mapa / batalhas | `Settings/Map/MapGenerationSettings` (andares, andares de referência do balanceamento, nós, tipos, pesos de batalha normal e de elite) e `BattleGenerationSettings` (pool, ameaça, orçamento, escala, XP, limites, perfis Normal e Elite, chefe). |
| Ajustar XP e níveis | `Settings/Progression/ProgressionSettings` (curva, nível máximo, vida máxima por nível `maxHpPerLevel`, cura ao subir `levelUpHealFraction`); XP das batalhas em `BattleGenerationSettings` (fonte `ThreatBudget`, XP por ponto de ameaça, crescimento por profundidade, multiplicador dos perfis Normal e Elite); orbs em `XpRewardSystem` (`xpPerPacket`, `maxPackets`) e `Settings/Vfx/XpOrbsVfxSettings` (explosão, pausa, voo). Confira com `GridBattle > Talents > Simulate XP Progression`. |
| Medir o balanceamento | `GridBattle > Simulation > Run Balance Simulation` (opções no asset `Settings/Simulation/BalanceSimulationSettings`): runs completas por classe com bots, relatório em `persistentDataPath/Simulation/<data>_<rótulo>/` (`summary.md`, `runs.csv`, `battles.csv`). Guia completo: `docs/projeto/simulador_balanceamento.md`. |
| Ajustar combate | `Settings/Combat/CombatSettings` (defesa, dano mínimo, métricas, colisão de empurrões e puxões, se disparar um golpe anunciado gasta o agir: `firingUsesAction`). |
| Ajustar a previsão | `Settings/Combat/ForecastSettings` (só visual: números da ordem, listras da marca dos golpes, opacidade dos golpes dos outros ao tocar num inimigo, dano piscando, setas, fantasmas). Posição dos selos do inimigo no `EnemyIconsView` do `Enemy.prefab`. Mirar e confirmar: `GameplayInputSettings.confirmTargetedActions`. |
| Ajustar run | `Settings/Run/RunSettings` (cura do acampamento, treino no acampamento `campOffersSkillUpgrade`, custo do nó de talento `talentNodeCost`/`talentNodeCostMaxHp`, recompensa da elite `eliteConsumableOffers` e `eliteGrantsSkillUpgrade`, estados entre batalhas, seed de depuração, `autoEnterFirstNode`). |
| Ajustar a monetização | `Settings/Monetization/MonetizationSettings` (chaves e blocos do LevelPlay, produto da loja, momentos e rampa dos anúncios forçados, aviso, oferta, privacidade, modo do Editor); recargas por run em `TalentOfferSettings`. Testes sem loja: `GridBattle > Monetization > Testing`. Ver `docs/projeto/monetizacao.md`. |
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
- **Ordem dos inimigos** é fixa na batalha e mostrada ao jogador
  (`Turns/ActingOrder`: `Enemy.ActOrder`, empate por linha e coluna); mudar
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
- **Resetar o jogo** (*Opções > Desenvolvimento*, `GameReset`) no Editor apaga o perfil, o save e as
  métricas **reais** de quem desenvolve. Em testes automáticos, guarde e restaure os arquivos como acima.
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
  talentos, ícones e visuais de skills gerados) existe para os sistemas
  funcionarem: substitua pelos definitivos sem mudar código.
- Mudanças de gameplay devem ser deliberadas: refatorações precisam manter o
  jogo idêntico (ver Verificação).

## Verificação

Testes do framework: `unity command run_tests --mode EditMode --filter ZS.UI.Tests --filter_type assembly`.
Conteúdo: `GridBattle > Validate Content` (ou `ContentValidator.Run()` por script) deve dar 0 erros.
Checagens dos talentos (reescritas para o conteúdo do novo molde em 2026-10-19): menu `GridBattle > Talents > Run Checks`; simulação de
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
  Editor sem foco). As referências antigas (`trace4.txt`) são do molde
  anterior a 2026-10-19, que tinha ataque básico: depois do novo molde, só
  vale comparar com uma referência gravada nele.
- **Simulador como medida do novo molde** (usado em 2026-10-19): 20 runs por
  classe com as seeds 1000–1019 e um rótulo próprio; o relatório tem os golpes
  de inimigos em inimigos ("Enemy hits on enemies (dmg)", "Kills by enemies")
  na seção de empurrões. O bot lê a mesma previsão que o jogador
  (`ActionForecast`/`ThreatForecast`).
- **Teste de interface do turno** (usado em 2026-10-19): script de Play Mode
  que começa uma run, joga alguns turnos da primeira batalha com capturas e
  confere a ordem numerada, o selo do golpe sobre cada inimigo, as células
  marcadas com o total previsto, a previsão ao mirar (dano, seta, fantasma,
  golpe que acompanha o empurrado) e que a vida do jogador cai exatamente o
  previsto.
- **Prova de regressão de refatoração com o simulador** (usada em 2026-10-12):
  rode lotes antes e depois com as mesmas seeds e compare `runs.csv` e
  `battles.csv` sem as colunas `ms` e `frames`: têm de ser idênticos.
- **Prova de regressão com conteúdo novo desligado** (usada no G3): antes de
  rodar o simulador, volte os pools de talentos de cada classe e o peso dos
  genéricos ao que eram, **só em memória** (reflexão, sem `SetDirty`, e restaure
  depois); com as mesmas seeds ele tem de reproduzir exatamente o resultado de
  referência (vitórias, andar, nível, ações). Mostra que o código novo não
  muda o jogo base.
- Fluxos de UI: scripts que clicam com `SimulateClick` e tiram screenshots
  (`ScreenCapture.CaptureScreenshot`) para conferir 216×384.
- Monetização: no Editor (`Editor Mode = Simulated`) o anúncio é a tela de teste e a compra é simulada. Um
  script de Play Mode pode levar o perfil até um anúncio devido (contadores de `MonetizationService.State`)
  e disparar `NodeCompletedEvent`. Só `MonetizationService` escuta esse evento.
- Para provar que UI/USS/UXML não mudaram: script que grava a árvore visual de
  cada painel (`worldBound` + `resolvedStyle`) por estado e compara.

## Histórico de refatorações

Registre aqui toda mudança estrutural (mais recente primeiro).

### 2026-10-20 — Linha de visão nos itens e consumíveis de tabuleiro
Pedido do Luiz: "A bomba deve ser seu uso obstruído pela linha de visão. Também sinto falta de mais itens consumíveis."
Mudança de mecânica pedida.
- **Linha de visão:** `ConsumableRules.HasLineOfSight` (mesma regra das skills: `CombatSettings.skillsNeedLineOfSight`,
  `LineOfSight.IsClear`) em `IsValidTarget`; campo novo `ConsumableDefinition.ignoresLineOfSight` (desligado em todos).
  Vale para a Bomba e todos os itens com alvo; mira, destaques, previsão e bot passam por `IsValidTarget`. A ficha do
  item mostra a linha "Visão" e o empurrão.
- **Efeitos novos** (`Gameplay/Consumables/BoardConsumableEffects.cs`): `DisplaceConsumableEffect`
  (`DisplacementResolver`, quem usa é o lançador), `SwapConsumableEffect` (`GridController.SwapCharacters`) e
  `CreateTerrainConsumableEffect` (regras do `CreateTerrainSkillEffect`). `ActionForecast.ForConsumable` prevê dano,
  empurrões, troca e interrupção por estado (Congelado).
- **Itens novos** (`Settings/Consumables/`, no pool de `ConsumableSettings`, textos nos 3 idiomas, ícones do lote R):
  Granada de Impacto (área 3×3, empurra 1 para longe do centro), Pergaminho de Troca (troca com um personagem a até 4),
  Pedra (Rocha por 3 rodadas numa casa vazia), Frasco de Fogo (Fogo numa cruz de 5 por 2 rodadas) e Frasco Congelante
  (Congelado 1 turno). Valores provisórios. O texto da Bomba cita a linha de visão.
- **Bot do simulador:** usa itens de empurrão quando a previsão dá dano ou abate sem ferir o jogador; troca, Pedra e
  Frasco de Fogo ele não usa.

### 2026-10-20 — Cada classe começa com uma skill
Pedido do Luiz: "Os personagens devem iniciar com somente uma única habilidade. As demais devem ser obtidas conforme a
run como as outras." Mudança de mecânica; só dados (nenhum código de jogo mudou).
- **Skill inicial** (`Settings/Characters/Players/`): a lista `skills` de cada classe ficou só com a skill sem recarga
  (Knight: Golpe; Mage: Rajada de Vento; Rogue: Adaga Envenenada).
- **Talentos novos** (nível 2, `maxRank` 1, peso 1,5, ícone da skill, tags de sinergia dos talentos de skill da classe),
  no `talentPool` de cada classe: Guerreiro `ChargeDrill` (Treino de Investida → Investida com Escudo) e `HeavyShield`
  (Escudo Pesado → Golpe de Escudo); Mago `FrostTouch` (Toque Gélido → Nova de Gelo) e `Pyromancy` (Piromancia → Bola
  de Fogo); Ladino `HuntingHook` (Gancho de Caça → Gancho) e `ShadowPath` (Caminho das Sombras → Passo das Sombras).
  Textos nos 3 idiomas.
- `TalentOfferSettings.maxSkillTalents` 3 → 5 (a barra continua com 6 espaços).
- `TalentChecks`: a skill de recarga longa do Mago agora é procurada também nas skills liberadas por talento.
- **Verificado** (no PC do Luiz): `Validate Content` sem erros (depois de reconstruir o `GameDatabase`); `TalentChecks`
  ALL PASSED (4343).
- **Pendência:** o simulador não foi rodado; o começo da run deve ficar mais difícil.

### 2026-10-20 — Andar por um caminho de células; personagens bloqueiam o caminho
Pedidos do Luiz: o jogador andava passando por um inimigo (de um lado dele para o outro), e o andar deve seguir um
caminho de células até o destino, para depois haver efeitos ao passar por uma célula. Mudança de mecânica pedida.
- **Regra** (`GridRules.HasWalkPath`, `CanWalkTo`): o caminho até a casa final só passa por casas livres; personagens
  (jogador e inimigos) bloqueiam como terreno, e a diagonal entre dois obstáculos fecha. Antes só o terreno bloqueava
  o caminho (e só era conferido com terreno bloqueando no grid); os personagens só ocupavam a casa final.
- Vale para tudo que usa `CanWalkTo`: destaques do jogador, andar dos inimigos (`ApproachForSkillAction`,
  `FindStepToward`), bot do simulador e o destaque do inimigo tocado (`GetWalkCells`).
- **Caminho** (`GridRules.FindWalkPath`: o mais curto por casas ortogonais livres, empate pela ordem baixo, esquerda,
  direita, cima; `HasWalkPath` passou a usá-lo). O andar do jogador e dos inimigos (`PlayerCharacterController`,
  `ApproachForSkillAction`, `KeepDistanceAction`, `ChaseTargetAction`) vai por `GridController.WalkEntity`, que segue
  o caminho: evento novo `EntityPassedCellEvent` (célula e posição no caminho) em cada célula cruzada, antes do
  `EntityEnteredCellEvent` do destino; nada reage a ele ainda (o terreno continua só na chegada). O visual pula de
  célula em célula (`GridMovementAnimator.AnimatePath`; o estilo `Flip` continua direto), e desfazer o andar volta
  pelo caminho. O Teleporte continua com `MoveEntity` (direto).

### 2026-10-20 — Toque no inimigo mostra só até onde ele anda
Pedido do Luiz: o destaque do inimigo tocado acendia casas demais. Só visual; nenhuma regra mudou.
- **Destaque** (`PlayerCharacterController.ComputeHighlights`): tocar num inimigo (ou o toque longo) marca só as células
  para onde ele pode andar a partir de onde está (`GridRules.GetWalkCells`: `CanWalkTo` dentro de `WalkDistance`;
  nenhuma com um estado que impede andar), com a moldura laranja do `Danger`. Antes eram todas as células que os
  golpes prontos dele cobririam de qualquer casa para onde pudesse andar, o que acendia quase o tabuleiro todo num
  inimigo voador (pendência da entrada "Leitura do tabuleiro", resolvida).
- O resto do toque não mudou: as marcas dos outros golpes ficam fracas e o ícone do golpe dele aparece.
- `GridRules.GetReachCells`/`GetSkillReachCells` continuam no código, sem uso no jogo.

### 2026-10-20 — Combate mais exigente: mira nas casas de fuga, golpes maiores e força com o andar
Pedido do Luiz ("o jogo está bem fácil, basicamente não importa muito a estratégia"). Commits `29164bf` (IA e
áreas), o da escala e do orçamento e o dos textos e documentos. Valores em teste: são uma proposta para ele jogar.
Detalhes e números em `docs/design/balanceamento_e_geracao.md` ("Combate mais exigente").
- **Decisão:** o Luiz não escolheu entre as opções propostas ("Menos fuga", "Só números", "Reforços"); segui a
  recomendada, "Menos fuga". Ela traz de volta a escala de vida e dano por profundidade, que a decisão de
  2026-10-19 tinha zerado; o Luiz confirmou depois que a escala fica ("Manter"). Os valores seguem em teste.
- **IA** (`UseSkillsAction`, seção "Aim"): `coverEscapeCells` (código desligado por padrão, ligado no asset
  `UseSkills`) faz a skill sem empurrão mirar a célula cuja área cobre mais da zona do jogador (a casa dele com
  `playerCellWeight` 1,5 e as casas para onde ele pode andar com `escapeCellWeight` 1; casa já marcada por outro
  golpe × `alreadyCoveredFactor` 0,25; −`allyHitPenalty` 1 por outro inimigo atingido; mínimo `minCoverValue` 1,
  senão a mira antiga de `SkillTargeting.TryFindAimCell`). Empate: atinge o jogador, depois mais perto dele.
- **Golpes básicos maiores** (`Skills/Enemies/`): os 16 golpes de uma casa ganharam área: `Perpendicular` 3
  (ClubStrike, MagmaFist, OgrePunch, StoneFist, KingStrike), `Linear` 2 (Bite, VampireBite, Ram, ShieldPush,
  DiveBite, Sling, HexGaze) e `Cross` 3 (FlameBolt, MudBolt, Spark, Screech); descrições nos 3 idiomas.
- **Escala e orçamento** (`BattleGenerationSettings`): `hpPerDepth` 0,025 e `damagePerDepth` 0,035; curva de
  orçamento 2 / 3,5 / 5 / 6,5 / 7,5 / 8,5 / 9,5 nos andares 1 / 5 / 10 / 15 / 20 / 25 / 30 (antes 4,5 a 8,5 do
  andar 10 em diante; o começo não mudou).
- **Resultado no simulador** (seeds 1000–1019, 20 runs por classe; `melhorias-mago-elite` → `menos-fuga-3`):
  vitórias Guerreiro 55% → 20%, Mago 15% → 0%, Ladino 50% → 20%; dano por batalha em % da vida máxima nos andares
  11–15 de 9% para 24% e nos 26–30 de 19% para 41%. Só a IA e as áreas (`menos-fuga-1`) quase não mudaram o
  resultado; um inimigo a mais já no andar 1 (`menos-fuga-2`) fez o bot morrer quase sempre antes do andar 6.
- **Pendências:** o Mago ficou sem vitórias no simulador; as descrições dos golpes citam o dano base, não o
  escalado pelo andar; se o começo continuar fácil para quem joga, o próximo ajuste é o orçamento dos andares 1 a 9.

### 2026-10-20 — Leitura do tabuleiro: golpes listrados e ordem no canto
Pedido do Luiz: os destaques dos golpes anunciados se sobrepunham e a ordem sobre cada inimigo era informação demais
em pouco espaço ("deve mostrar as informações, mas de forma limpa"). Só visual; nenhuma regra mudou.
- **Marca dos golpes** (`Cell`, `ForecastSprites.GetStrikeHatch`): listras diagonais no formato do destaque, sem
  moldura (laranja; vermelhas onde alguém seria atingido), no lugar da cópia pulsante da moldura, que se misturava
  com a moldura do andar. Cores e pulso novos no `Cell` (`telegraphHighlightColor`, `telegraphHitColor`,
  `telegraphPulse` 0,25); listras em `ForecastSettings` (`strikeStripePeriod` 6, `strikeStripeWidth` 2).
- **Sem números nas células:** `ForecastSettings.showOrderOnStrikes` desligado (o "123" de três golpes na mesma
  célula sumiu).
- **Inimigo tocado:** `ForecastView` acompanha `PlayerCharacterController.InspectedEnemy`; as marcas dos outros golpes
  ficam fracas (`Cell.WarnDimmed`, `ForecastSettings.dimmedStrikeAlpha` 0,3) e o ícone do golpe aparece sobre ele
  (`EnemyIconsView.SetFocused`).
- **Selos do inimigo** (`EnemyIconsView`, valores gravados no `Enemy.prefab`): a ordem fica num selo escuro no canto
  de cima à esquerda da própria célula (`actingOrderOffset`), o papel no canto de cima à direita e os estados abaixo
  dele; o ícone do golpe só no inimigo tocado, com os turnos só quando passam de 1. Nada passa por cima da barra de
  vida do vizinho. Selos em `ForecastSprites.GetPlate`.
- **Verificado** (no PC do Luiz): compila sem erros; teste em Play Mode com 6 a 7 inimigos (perfil real restaurado):
  marcas listradas, nenhum número nas células, ordem no canto, tocar num inimigo esmaece os outros golpes (12 células
  marcadas, 11 esmaecidas ao tocar num inimigo de golpe de 1 célula) e mostra o ícone dele.
- **Pendências:** tocar num inimigo voador ainda acendia o alcance dele no tabuleiro quase todo (resolvida na entrada
  "Toque no inimigo mostra só até onde ele anda").

### 2026-10-20 — Contras do Mago, golpe todo turno, XP em explosão e batalhas de elite
Pedido do Luiz (melhorias depois do novo molde). Implementação nos commits `1288849` (código, ícones e ajustes) e
`353aa2f` (conteúdo: golpes, estado Exausto, magias canalizadas). Base: `183e92e`.
- **Decisões do Luiz:**
  - o Mago paga as magias fortes: o Meteoro e a Tempestade Elétrica (e as variantes) gastam o andar do turno, e o
    Meteoro ainda deixa o Mago Exausto (não anda) por 2 turnos;
  - todo inimigo golpeia em todo turno: um golpe básico (recarga 0) e um especial (recarga maior que 0), o
    especial primeiro na lista;
  - o XP sai do inimigo numa explosão de orbs que voam juntos para a barra;
  - batalhas só normais ou de elite (a fácil e a difícil saíram); a elite dá mais XP, uma oferta de consumível e a
    melhoria de skill;
  - o mapa não mostra o XP das batalhas.
- **Magias canalizadas** (`SkillDefinition.usesWalk`, seção "Turn cost (player)"; inimigos ignoram): usar a skill
  gasta o andar (`PlayerCharacterController.CanUseSkillNow`/`CanWalkNow`; a skill pode vir antes de andar, ou
  depois de desfazer o andar, e `HasAnythingLeft` conta uma canalizada pronta enquanto o andar pode ser desfeito).
  Estado novo `States/Exhausted` (Exausto: `BehaviorRestrictionEffect` que impede andar, nocivo,
  `RefreshDuration`), aplicado ao lançador pelo Meteoro e variantes; o bônus de duração de estados dos talentos não
  vale mais para estados aplicados ao próprio lançador (`ApplyStatesSkillEffect`). Ficha da skill: linha "Em você"
  (`details.action.on_caster`) e a nota `details.uses_walk`. O bot só usa canalizadas da célula atual, antes de
  andar.
- **Golpe básico e especial** (`Skills/Enemies/`, agora 43 golpes): 17 golpes básicos novos, todos anunciados; a
  lista `skills` de cada inimigo tem o especial primeiro e o básico depois (`UseSkills` tenta na ordem). Textos de
  inimigos e golpes corrigidos nos 3 idiomas (migração por script no Editor).
- **XP em explosão** (`XpRewardSystem.maxPackets` 8, `XpOrbsVfxSettings`): os orbs se espalham em volta do inimigo
  (`burstRadius`, `burstRadiusVariance`, `burstAngleJitter`, `burstDuration`, `burstEase`), esperam `holdDuration`
  e voam juntos (`launchJitter`, `flightDuration`), em cerca de 0,8 s; `staggerDelay` saiu.
- **Batalhas normal e de elite** (`EBattleDifficulty { Normal = 1, Elite = 2 }`):
  - mapa com pesos `MapGenerationSettings.normalWeight` 75 e `eliteWeight` 25 (o antigo `hardWeight`); nó de elite
    só onde há elite disponível (`BattleGenerationSettings.HasEliteAt`, `EnemyPoolEntry.IsEliteAt`);
    `MapRules.IsEliteBattle`;
  - `BattleGenerationSettings.difficulties` com os perfis Normal (1/1/1) e Elite (orçamento ×1,35, força ×1,
    XP ×2); saves antigos passam por `MapRules.NormalizeDifficulties` (todo nó vira normal, menos os de elite que
    têm elite disponível);
  - recompensa da elite (`RunManager.BeginEliteReward`/`ContinueEliteReward`): `RunSettings.eliteConsumableOffers`
    (1) ofertas de consumível (`OpenConsumableOffer`, título `consumable_offer.elite_title`) e depois a melhoria de
    skill; o progresso fica salvo em `RunState.EliteRewardOffer`/`EliteOffersLeft`;
  - XP medido pela ferramenta de XP: normal de 41 (profundidade 1) a 420 (29); elite de 170 (4) a 1134 (29), cerca
    de 2,7 vezes a normal.
- **Mapa:** os nós não mostram XP; a prévia diz "Batalha" ou "Batalha de elite" e mostra a recompensa
  (`preview-reward`); `ScreensSettings.battleEliteIcon`, `eliteColor` e `GetBattleColor`; classes
  `map-node--normal` e `map-node--elite`.
- **Simulador:** `EMapPolicy { Normal = 1, Elite = 2, Mixed = 3 }`; o `SimulationRunner` responde à oferta de
  consumível da elite; o relatório tem colunas Normal e Elite.
- **Arte:** lote Q do PixelLab (ícones dos 17 golpes novos e do Exausto), registrado em `docs/projeto/arte.md`.
- **Verificado** (no PC do Luiz): compila sem erros; `Validate Content` sem erros; `TalentChecks` ALL PASSED (4219);
  a checagem da migração passou.
- **Resultado no simulador** (rótulo `melhorias-mago-elite`, seeds 1000–1019, 20 runs por classe, sem balanceamento):
  vitórias Guerreiro 55%, Mago 15%, Ladino 50% (antes 60/20/55%); 94% das derrotas antes do andar 10, a maioria
  na primeira elite (Ogro Brutamontes: 16 de 36 golpes finais); nenhuma inversão de XP entre normal e elite. Teste
  em Play Mode (run de Mago, perfil real restaurado): Meteoro só antes de andar, desfazer o andar o libera de novo,
  Exausto apaga o pé no turno seguinte, um golpe anunciado por inimigo vivo a cada turno depois do primeiro, mapa sem
  números nos nós e com o ícone de elite.
- **Pendências:**
  - ainda há turnos sem golpe: o Totem e os inimigos de suporte quando curam, protegem ou invocam, e o primeiro turno
    da batalha;
  - o bot não pesa o Exausto ao escolher o Meteoro;
  - a Tempestade pode ser usada com o Mago Exausto (gasta um andar que ele não tinha);
  - a prévia da elite cita o consumível mesmo com `eliteConsumableOffers` 0;
  - um save antigo parado no meio da recompensa da elite retoma só a melhoria;
  - os ícones `BattleEasy`/`BattleHard` e chaves de UI antigas ficaram sem uso;
  - a elite dá cerca de 2,7 vezes o XP da normal (o design sugeria ×1,5) e os níveis finais por rota não foram
    medidos de novo.

### 2026-10-19 — Novo molde de combate (Into the Breach + Slay the Spire)
Pedido do Luiz: mudar a direção do combate para o molde do *Into the Breach* (golpes anunciados, previsão,
empurrões como arma) e a progressão para o do *Slay the Spire* (talentos que mudam regras, melhorias de skill).
Design escrito antes em `docs/GDD.md` e `docs/design/` (commit `d578655`); implementação nos commits `9996383`
(lote A), `63ff2dc` (lote B), `e99c87b`/`57a22c3` (lote C1), `650886b`/`37ff643` (arte), `1f0ec15` (conteúdo),
`792d38c` (bot e áreas pares), `d481779` (ajuste provisório) e `05353f2` (detalhes da previsão). Base: `8b0df7e`.
- **Decisões do Luiz:**
  - sem ataque básico (`hasBasicAttack` desligado em todos os personagens): toda ação é uma skill;
  - cada classe começa com 3 skills e libera até 3 por talentos (`TalentOfferSettings.maxSkillTalents` 3; 6
    espaços em `SkillSettings`); o andar do jogador é de 2 casas (`walkDistance`);
  - golpes de inimigo anunciados um turno antes, que acompanham quem é empurrado e atingem todos menos quem lança;
  - ordem fixa e numerada dos inimigos; mirar e confirmar com previsão do resultado;
  - talentos de número removidos (48 assets de talento saíram); talentos de regra no lugar;
  - uma melhoria por skill (1 de 2 variantes), no acampamento ou ao vencer uma elite.
- **Turno** (`TurnManager`, `EnemyController`, `Turns/ActingOrder`): o inimigo dispara o golpe preparado
  (`Enemy.AdvanceTelegraph`), anda e prepara o próximo; disparar não gasta o agir (`CombatSettings.firingUsesAction`
  desligado). A batalha começa sem golpe preparado. O terreno temporário perde uma rodada antes da vez do jogador
  (`GridController.TickTemporaryTerrain`).
- **Golpes anunciados** (`SkillDefinition`: `telegraphed`, `windupTurns`, `telegraphFollowsCaster`,
  `cancelledByDisplacement`, `interruptibleByStates`; `SkillTelegraph.Shift`): as 26 skills de
  `Skills/Enemies/` são anunciadas com `windupTurns` 1 e alvo `EveryoneButCaster`. `TelegraphResolvedEvent`.
- **Previsão** (pasta nova `Gameplay/Forecast`): `ActionForecast` (dano, mortes, deslocamentos, interrompidos),
  `ThreatForecast`/`TelegraphForecast`, `ForecastView`, `ForecastSprites`, `ForecastSettings`
  (`Settings/Combat/ForecastSettings.asset`; `strikeOrderSpacing` 9). Mirar e confirmar:
  `GameplayInputSettings.confirmTargetedActions` (ligado), `PlayerCharacterController.AimedCell`/`ConfirmAim`/
  `CancelAction`, `ActionAimChangedEvent`, botões `cancel-action-btn` e `confirm-action-btn` no `GameScreen.uxml`.
  `EnemyIconsView` mostra a ordem e os turnos do golpe; os detalhes do inimigo mostram "Ordem" e o golpe preparado.
- **Skills que movem e criam terreno** (`MovementSkillEffects.cs`): `SwapSkillEffect`, `DashToTargetSkillEffect`,
  `CreateTerrainSkillEffect` (+`ETerrainPlacement`); terreno temporário em `GridController.SetTerrain` e
  `TerrainCellSpec.TurnsLeft`; terreno novo `IceWall`. `CharactersSwappedEvent`.
- **Talentos de regra** (`States/Effects/RuleTalentEffects.cs`): `DominoEffect`, `DisplacementTrailEffect`,
  `SlipperyPushEffect`, `CounterstrikeEffect`, `SwapStepEffect`, e `OnKillEffect.grantExtraWalk`
  (`ExtraWalkGrantedEvent`); consultados com `StateContainer.FindEffect<T>`.
- **IA:** `ApproachForSkillAction` (assets `ApproachMelee`/`ApproachMid`/`ApproachRanged`) anda até de onde uma
  skill pronta alcança o jogador; behaviors novos `Skirmisher` e `Stationary`.
- **Run** (`RunManager`, `RunState`, `RunSettings`): acampamento com descansar (`healFraction` 0,4) ou treinar
  (`campOffersSkillUpgrade`; `CampEnteredEvent`); melhoria de skill (`SkillDefinition.upgrades`/`upgradeOf`,
  `SkillUpgradeOfferEvent`, `SkillUpgradedEvent`, modal `UI/Overlays/SkillUpgrade` com `SkillUpgradeController`);
  elite nas batalhas difíceis (`BattleGenerationSettings.hardBattlesHaveElite`, `EnemyConfig.isUnique`: Golem de
  Lava, Ogro Brutamontes e Cavaleiro Esqueleto) com melhoria ao vencer (`eliteGrantsSkillUpgrade`); nó de talento
  custa 2 de vida máxima (`ETalentNodeCost.MaxHp`, `talentNodeCostMaxHp`, `PlayerRunState.MaxHpBonus`).
- **Progressão** (`ProgressionSettings`): +1 de vida máxima por nível sem cura (`maxHpPerLevel` 1,
  `levelUpHealFraction` 0); curva 150 + 100 por nível, nível máximo 12.
- **Combate** (`CombatSettings`): `minimumDamage` 0 (a armadura pode zerar um golpe de 1); colisão 1, 0 por casa
  não percorrida, 1 a quem é atingido, ignorando a defesa.
- **Áreas:** `Linear` aceita tamanho par (`SkillDefinition.NeedsOddSize`).
- **Conteúdo** (migração por script no Editor): 18 skills de classe com 36 variantes em `Skills/<Classe>/Upgrades/`,
  26 golpes de inimigo, 27 inimigos (10 com arte nova do PixelLab), estados novos Cego e Congelado (Blindado, Exposto,
  Fraquejado e outros ajustados), ícones novos em `Art/Sprites/Icons/` e `Art/Sprites/Terrain/IceWall.png`, textos nos 3 idiomas.
- **Valores provisórios** (ajuste pelo simulador; o balanceamento continua pausado): vida das classes Guerreiro 18,
  Mago 14, Ladino 15; Slime 4; orçamento de ameaça do andar 1 = 2; chefe Rei Goblin com 22 de vida, imóvel, com
  escolta Guarda, Xamã, Artilharia e Lanceiro Goblin.
- **Simulador:** o `BattleBot` lê `ActionForecast`/`ThreatForecast` (`BattleBotOptions.redirectedStrikeWeight` 1,
  `dodgeAfterActingFactor` 0,5); `MapPolicy` responde ao acampamento e à melhoria; o relatório ganhou os golpes de
  inimigos em inimigos.
- **Verificado:** validador sem erros; simulador com 20 runs por classe (seeds 1000–1019); teste de interface em
  Play Mode (run de Guerreiro, 5 turnos com capturas, perfil real restaurado): ordem numerada, selo do golpe,
  células marcadas com o total previsto, previsão ao mirar, vida do jogador caindo exatamente o previsto, nenhum golpe
  preparado no primeiro turno. As checagens de talentos foram reescritas para o conteúdo novo.
- **Resultado no simulador** (rótulo `novo-molde-inicio-chefe`, sem balanceamento): vitórias Guerreiro 60%, Mago
  20%, Ladino 55%; 73% das derrotas antes do andar 10; chefe vencido em todas as vezes que foi alcançado. O Mago é o
  mais fraco (quase não mata por colisão; o bot usa pouco o controle).
- **Pendências:** o empurrão em cadeia do Dominó não entra na previsão (`DisplacementResolver.Predict`);
  balanceamento (números provisórios); `BasicAttack`, `ChaseTarget` e `KeepDistance` ficaram sem uso.

### 2026-10-19 — Ícones das skills, inimigos de teste e padrão de arte
Pedidos do Luiz.
- **Ícones das skills** (`Art/Sprites/Icons/Skills/`, PixelLab): os 15 placeholders de `Placeholders/Skills` movidos
  (GUIDs mantidos) e redesenhados, mais os das habilidades novas; `Placeholders/` ficou só com `Vfx`.
- **Inimigos de teste com arte própria** (design em `docs/design/inimigos.md`, "Inimigos novos de teste"): Goblin
  Bombardeiro (Chuva de Fogo, telegrafada), Ogro Brutamontes (Pancada Esmagadora, telegrafada, sem empurrão para a
  IA usar sempre) e Aranha Tecelã (Teia, aplica o estado novo Enredado). Sprites e animações em
  `Art/Sprites/Enemies/` e `Art/Animations/Enemies/`, configs, skills, estado, textos nos 3 idiomas e entradas no
  pool de `BattleGenerationSettings` (andares 6, 12 e 4). Só dados e arte; nenhum código mudou.
- **Padrão de arte** na seção "Arte (padrão visual)" deste arquivo; detalhes em `docs/projeto/arte.md`.
- **Verificado:** validador sem erros; teste em Play Mode com os três inimigos (Teia aplicou Enredado, a Chuva de Fogo
  marcou 9 células e disparou no turno seguinte).

### 2026-10-18 — Arte final dos ícones, terreno e slots (PixelLab)
Pedido do Luiz: trocar todos os placeholders em uso, menos os das skills, com coerência visual. Guia em
`docs/projeto/arte.md`. Só arte e referências; nenhuma regra mudou.
- **Pastas novas** em `Art/Sprites/`: `Icons/{Talents,States,Roles,Consumables,Map,UI}`, `Terrain` e `UI`. Os PNGs
  foram movidos pelo `AssetDatabase` (GUIDs mantidos) e reescritos; `Tokens.uss` atualizou os caminhos dos slots e
  dos ícones do turno. `Placeholders/` ficou só com `Skills` e `Vfx`.
- **Arte:** ícones 16×16 e terreno 24×24 gerados com `create_image_pro` do PixelLab na paleta ENDESGA 32 (lotes e
  escolhas no guia); insígnias 8×8 (papéis e estados sobre os inimigos) desenhadas à mão na mesma paleta; slots
  redondos redesenhados com as camadas e cores dos slots quadrados do `UI.png`. O terreno passou de 20×20 para
  24×24.
- **Dados:** os 43 estados de talento de classe (`Settings/States/Talents`) passaram a usar o ícone do talento, como os
  compartilhados; as cópias em `Placeholders/States` foram apagadas.
- **Verificado:** validador sem erros; capturas em Play Mode do mapa (nós e prévia com papéis), da HUD (ícones do
  turno, slots), da escolha de talento e dos cinco terrenos; perfil real restaurado depois.

### 2026-10-18 — Andar e agir no mesmo turno
Mudança de mecânica pedida (o Luiz quer saber se o combate fica divertido); docs em
`docs/design/sistema_combate.md`, GDD Mecânica 1 e `docs/design/interface.md`.
- **Regra nova e única:** o turno tem um andar e um agir (ataque básico ou skill), em qualquer ordem, mais um
  consumível. A primeira versão tinha a opção `CombatSettings.actionEconomy` com a regra antiga (`SingleAction`);
  com ela, o simulador reproduziu a linha de base (runs.csv e battles.csv idênticos, 90 runs, mesmas seeds). Depois
  o Luiz pediu para remover o modo antigo: a opção e os caminhos de código dele saíram.
- **Jogador** (`PlayerCharacterController`):
  - o turno acaba sozinho só quando andar, agir e usar consumível estão usados ou impossíveis (`HasAnythingLeft`,
    pedido do Luiz: sem itens, andar + agir encerra; com item utilizável, espera), com `EndTurn()` (botão Encerrar
    turno, sempre visível na vez do jogador) ou tocando em si mesmo (encerra a qualquer momento, inclusive para
    esperar);
  - `UndoMove()` volta o andar enquanto não agiu, sem item usado depois e sem terreno `OnEnter` na célula
    (`GridController.UndoMove`, `EntityMoveUndoneEvent`);
  - `PlayerTurnStateChangedEvent` (andar, agir, consumível, desfazer) a cada mudança;
  - `PlayerActionEvent` passou a significar "a vez do jogador acabou".
- **Destaques:** `GridRules.GetHighlightInfos(grid, personagem, podeAndar, podeAgir)`. Toque longo num inimigo marca
  `ECellHighlightType.Danger` nas células de `GridRules.GetReachCells` (andar + ataque básico) até o próximo toque.
- **HUD:** `UI/Hud/TurnActionsView`, entre o grid e a barra de itens:
  - pé, espada e poção (`Art/Sprites/Placeholders/UI/TurnMove|TurnAct|TurnItem.png`, tokens `--skin-turn-*`);
  - Desfazer e Encerrar turno no `GameScreen.uxml`;
  - a barra de skills fica inativa depois de agir.
- **Inimigos:**
  - `EnemyAction.IsMovement` (ChaseTarget e KeepDistance);
  - o `EnemyBehavior` faz até 2 passadas na lista, um andar e um agir;
  - o `TurnManager` faz um inimigo de cada vez, esperando as animações dele (`EEnemyTurnPacing` removido; sobrou a
    pausa `enemyStagger`).
- **Animação:**
  - `GridMovementAnimator.GetRemaining`;
  - o avanço do ataque (`GridEffectsAnimator.PlayAttack`, atraso inicial) e os visuais de skill
    (`ActionVisualPlayer`) de quem acabou de andar esperam o pulo terminar.
- **Talentos:**
  - Bate e Corre e Foco Arcano usam `EDamageCondition.HolderMovedThisTurn`/`HolderDidNotMoveThisTurn` (renomeados,
    mesmos valores nos assets) e leem `Character.MovedThisTurn`;
  - textos atualizados nos 3 idiomas, além da dica da primeira batalha e de Recuperação Rápida.
- **Salvamento:** continua no início do turno; a primeira escolha fecha a janela de XP do salvamento (`RunManager`).
- **Simulador:** o `BattleBot` escolhe pares (andar, agir) com `GridController.ProbePosition` (consulta sem mover nada).
- **Verificado:**
  - `TalentChecks` 7778/7778, `ZS.UI` 14/14, validador sem erros, console limpo;
  - teste de Play Mode do fluxo do turno 18/18 (inclui a vez que espera com item utilizável);
  - inimigos sequenciais em Play Mode (15 turnos de inimigo, nenhum começou com animação em andamento);
  - capturas da HUD.
- **Resultado no simulador** (sem balanceamento): vitórias de 0–3% e profundidade média da morte de 12–14 (antes:
  0/20/13% e 20–24), porque os inimigos também andam e atacam.

### 2026-10-12 — Área de seleção das skills (estilo Dofus)
Pedido do usuário: além da área de efeito, a área onde a skill pode ser mirada, configurável como no Dofus.
- `Rules/SkillSelection` (puro): `ESkillSelectionShape` (Circle, Line, Diagonal, Star, Square), distância ao
  longo da forma, alcance mínimo e máximo; `ESkillTargetCell` (Any, Empty, Occupied).
- `SkillDefinition`: `selectionShape`, `minRange`, `rangeModifiable` e `targetCell` (padrões = o
  comportamento anterior); `EffectiveSkill` os expõe; `SkillTargeting.IsTargetInRange` e
  `IsTargetCellAccepted` os aplicam (mira do jogador, destaques, IA, bot). Ficha da skill: "Alcance: 1-3, em
  linha". `ContentValidator` acusa mínimo maior que o máximo.
- Configuração (mudança de mecânica pedida; ver `docs/design/sistema_combate.md`): Guerreiro em linha com
  alcance fixo, Mago em círculo 1-3 (Nova 0-2), Ladino em linha/estrela, Ganchos em linha 2-4.
- Verificado: com os padrões, partida determinística e simulador idênticos; com a configuração, partida
  idêntica (não usa skills), `TalentChecks` 7777/7777 e teste de Play Mode das formas (14 verificações).
  O simulador mudou (Guerreiro 13% → 0% e 40% → 17%; Mago e Ladino quase iguais). **Sem balanceamento:**
  decisão do usuário, as skills e os números ainda vão mudar.

### 2026-10-12 — Monetização (GDD 7)
Pedido do usuário: anúncios e compra de remoção de anúncios como no GDD. Decisões do usuário:
- Android e iOS;
- cobrança pela Google Play e pela App Store (Unity IAP, não D2C);
- privacidade só com a estrutura pronta;
- as melhorias aprovadas: aviso antes do anúncio, oferta depois do anúncio e botão no menu principal.

Guia em `docs/projeto/monetizacao.md`.
- **Pacotes:**
  - `com.unity.services.levelplay` 9.5.1 e `com.unity.purchasing` 5.4.4;
  - o Mobile Dependency Resolver importado pelo LevelPlay (`Assets/MobileDependencyResolver`,
    `Assets/LevelPlay`, templates de Gradle em `Assets/Plugins/Android`, `ProjectSettings/GvhProjectSettings.xml`);
  - permissão AD_ID ligada em `LevelPlayMediationSettings`.
- **Lógica** (`Gameplay/Monetization`):
  - `MonetizationService` se autoinicializa e escolhe os provedores pela plataforma: SDKs no
    Android/iOS, teste ou SDK no Editor (`EEditorMonetizationMode`), nada no PC;
  - conta pausas (`NodeCompletedEvent`, `RunEndedEvent`) no `ProfileState.Monetization`;
  - `ForcedAdRules` decide o anúncio devido;
  - pausa o `AudioListener` durante anúncios.
- **Ferramentas de talento com anúncio** (decisão do usuário, no lugar da recarga feita antes):
  - `MonetizationService.PayForTalentToolAsync` mostra o recompensado antes de cada uso de trocar, banir ou
    pular;
  - a regra de talento não mudou: o `TalentService` conta o uso como sempre, e o bot do simulador o chama
    direto, sem anúncio;
  - `MonetizationSettings.talentToolsNeedAd` e `allowToolsWithoutAd`.
- **Compra:**
  - grava o perfil antes de `ConfirmPurchase`;
  - `SaveSystem.Save` e `ProfileService.Save` passaram a devolver se gravaram.
- **UI:**
  - modais `RemoveAds`, `AdNotice` e `SimulatedAd` (`ModalsSettings`);
  - na janela de talento, uma linha avisando que cada uso mostra um anúncio (`talent-tools-hint`);
  - "Remover anúncios" no menu principal, nas Opções e no fim de run (`RemoveAdsRequestedEvent`);
  - o `GameFlowController` mostra o anúncio forçado pendente em tela parada (mapa, fim de run, menu).
- **Editor:**
  - `Tools/MonetizationMenu` (marcar ou desmarcar a compra, zerar os contadores);
  - o `ContentValidator` confere as views e avisa (nota) quando faltam as chaves do LevelPlay.
- **Reset de desenvolvimento** (pedido do usuário): `Managers/GameReset` (só com `Debug.isDebugBuild`:
  Editor e Development Build) encerra a run pelo caminho do "Desistir", apaga o save, o perfil, as métricas
  (`MetricsRecorder.DeleteFiles`) e o estado de anúncios da sessão (`MonetizationService.OnProfileReset`),
  aplica as opções padrão e emite `GameResetEvent` (o `GameFlowController` vai ao menu principal, sem a
  tela de fim de run). Botão em *Opções > Desenvolvimento*, com confirmação. Teste de Play Mode: 15 de 15.
- **Menu principal:** todos os espaços entre os botões com `--spacing-md` (o "Continuar" tinha 2 px a mais
  em cima).
- **USS:** removida de `Components.uss` a regra `.slot-bar > .slot-button:first-child`. O UI Toolkit não
  suporta `:first-child`, então ela nunca valeu (só gerava o aviso "Unknown pseudo class"). A tela não muda:
  o primeiro botão das barras continua com a margem de 2 px.
- **Verificado:**
  - teste de Play Mode com 31 de 31 verificações (anúncio forçado com aviso, tempo mínimo, oferta,
    compra, restauração, botões, trocar e banir com anúncio, anúncio fechado antes, pular grátis depois da
    compra);
  - partida determinística idêntica a `trace4.txt`;
  - simulador idêntico (180 runs);
  - `TalentChecks` 7777/7777, `ZS.UI` 14/14, validador sem erros.

### 2026-10-12 — Protótipo de visuais de skills e itens
Pedido do usuário (recomendação para células retangulares: peças pequenas por
célula em vez de uma animação da ação inteira). Só visual.
- `Gameplay/Movement/ActionVisualDefinition` (asset `Create > GridBattle >
  Vfx > Action Visual`) e `ActionVisualPlayer` (criado pelo `GridController`;
  `IsAnimatingMovement` e `AnimationSpeed` o incluem; ignorado em `SimMode`).
- `SkillDefinition.visual` (seção *Look*) e `ConsumableDefinition.visual`:
  com visual, a skill o toca no lugar da investida; sem ele, nada muda.
- Impacto sincronizado: `GridEffectsAnimator.PeekPendingDelay` /
  `GridController.GetPendingImpactDelay`; `Cell` atrasa a barra de vida e o
  número de dano; `XpOrbsVfx` esconde os orbs até o impacto
  (`XpRewardDroppedEvent.Source`, o inimigo que morreu).
- Arte provisória: `Editor/Tools/VfxPlaceholders` desenha quadros 24×24 em
  `Art/Sprites/Placeholders/Vfx/` e cria `Settings/Vfx/ActionVisuals/<Nome>Visual.asset`
  para as 14 skills e a Bomba.
- Verificado: partida determinística idêntica a `trace4.txt`; simulador
  (180 runs, seeds 1000–1029, duas políticas) idêntico ao lote anterior;
  validador com 0 problemas; capturas em Play Mode de cada visual.

### 2026-10-12 — Menu, talentos, linha de visão e manutenção
Pedidos do usuário; resumo para leitura em `docs/projeto/resumo_das_mudancas.md`.
- **Menu principal:** menu inicial (Novo jogo, Continuar só com
  `RunManager.HasContinuableRun`, Glossário, Opções) e seleção de personagem na
  mesma view (o Animator dos cards exige `#container/#character-selection`);
  card → `CharacterDetailsRequestedEvent` → modal `CharacterDetails`
  (`CharacterDetailsController`, `ModalsSettings.characterDetailsView`) →
  Jogar → `CharacterChoosenEvent`. Descrições provisórias das classes.
  `RunManager.HasContinuableRun` valida o save (com cache por data do arquivo).
- **Carta de talento:** sem o selo "combina com a build"
  (`ModalsSettings.showBuildMatchOnCards`, desligado).
- **Linha de visão e mira livre** (mudança de mecânica pedida): `LineOfSight`,
  `TerrainDefinition.blocksLineOfSight`, `CombatSettings.skillsNeedLineOfSight`
  / `charactersBlockLineOfSight`, `SkillDefinition.ignoresLineOfSight`
  (Tempestade Elétrica; a Bola de Fogo foi desligada pelo usuário);
  `requiresAffectedTarget` desligado em todas.
- **Detalhes de skills e itens:** toque longo ou clique direito num botão das
  barras (`UI/LongPressManipulator`, mesma duração das células) abre a janela de
  detalhes com `EntityDetailsRequest.ForSkill/ForConsumable`; a ficha vem de
  `UI/Hud/ActionDetailsModel` (números efetivos do dono).
  `SkillCooldowns.GetCooldownAfterUse` é a recarga que um uso aplicaria (a
  mesma conta do `Trigger`). O toque rápido que encerra um toque longo é
  ignorado (`LongPressManipulator.ConsumeFired`).
- **Manutenção (sem mudar o jogo; simulador idêntico com as mesmas seeds,
  partida determinística idêntica, `TalentChecks` 7777/7777, `ZS.UI` 14/14):**
  - `PlayerCharacter.Active` e `GridController.Instance` no lugar de 19 buscas
    na cena;
  - `ContentValidator` (`GridBattle > Validate Content`);
  - `SummaryRows.AddBaseAttributes/AddSkills` compartilhados pelo glossário e
    pelos detalhes do personagem;
  - `MapLinesElement.Clear` → `ClearConnections` (escondia o
    `VisualElement.Clear`);
  - dica de tutorial da vida corrigida (a vida volta toda ao subir de nível).
- **Simulador:** `SimulationOptions.showBoard` (mostra o tabuleiro e a HUD
  enquanto roda; resultados idênticos); um erro na preparação encerra a
  simulação com aviso em vez de travar em "starting".
- **Docs:** `docs/projeto/simulador_balanceamento.md` (guia do simulador) e
  `docs/projeto/resumo_das_mudancas.md` (resumo das mudanças).

### 2026-10-06 — Balanceamento G5/G6 (interrompido; balanceamento pausado)
O agente parou no meio do ajuste e o usuário pausou o balanceamento (o jogo
ainda vai mudar). Compila; partida determinística e `TalentChecks` não foram
rodadas de novo depois destas mudanças. Detalhes em `docs/planos/plano_balanceamento.md`.
- `BattleGenerationSettings`: curva de orçamento opcional (`budgetPoints`,
  linear entre pontos andar → orçamento), orçamento de XP separado
  (`xpBaseBudget`, `xpBudgetPerDepth`) e faixas de composição
  (`compositionBands`: mínimo de papéis diferentes e peso por papel a partir
  de um andar), usadas pelo `BattleGenerator`.
- `CharacterConfig`: `immuneStates` e `maxHpPercentDamageMultiplier` (lido por
  `PeriodicDamageEffect`); o chefe usa os dois.
- `SummonAction`: `maxTotalSummons` (invocados por batalha, contado na memória
  do inimigo e salvo) além de `maxAlive`.
- Assets: Escudo com `IgnoreIfPresent`, limites de papel, terreno em 5
  faixas, valores numéricos de partida (listados no plano). Relatório do
  simulador com "Death spread and the final boss" e "Battles by floor band"
  (papéis, terreno, invocações e táticas por faixa de andares).
- Não construído: ferramenta de metas e ajuste automático com janela para uso
  humano (decisão registrada no plano).

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
