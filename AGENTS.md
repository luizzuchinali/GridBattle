# AGENTS.md — GridBattle

Guia para agentes (e pessoas) trabalhando neste repositório: estrutura,
padrões, fluxos principais e histórico de refatorações.

> **Mantenha este arquivo atualizado.** Toda mudança de estrutura (pastas,
> namespaces, novos sistemas, novos tipos de asset, convenções) deve ser
> refletida aqui na mesma alteração, incluindo uma entrada em
> [Histórico de refatorações](#histórico-de-refatorações).

## Visão geral

Jogo tático por turnos em grid 2D (roguelite mobile). O jogador escolhe uma
classe, enfrenta inimigos num grid 6×6 e ganha XP/níveis.

| Item | Valor |
|---|---|
| Engine | Unity **6000.6.0f1** |
| Render | URP 2D + Pixel Perfect Camera (PPU **100**, `GameConfigManager.Ppu`) |
| UI | UI Toolkit (UXML/USS) via `PanelRenderer`; navegação pelo framework `ZS.UI` (`Assets/ZS/UI`) |
| Tweens | LitMotion (`LMotion`) |
| Input | Input System (`InputSystemUIInputModule`) + `IPointerClickHandler` nas células |
| Texto no mundo | TextMeshPro (dano flutuante das células) |
| Assemblies | Jogo: `Assembly-CSharp`/`-Editor` (sem `.asmdef`). Framework: `ZS.UI`, `ZS.UI.Editor`, `ZS.UI.Tests`, `ZS.UI.Samples` |
| Testes | EditMode do framework (`ZS.UI.Tests`); o jogo é verificado por scripts no Editor (ver [Verificação](#verificação)) |
| Cena | `Assets/Scenes/SampleScene.unity` (única) |

Documentação de design (Obsidian, em português) em `docs/`. A referência do
combate implementado é `docs/design/sistema_combate.md`; mantenha-a em
sincronia com o código.

## Estrutura de pastas

```
Assets/
├── Application/                    ← tudo que é do jogo
│   ├── Art/
│   │   ├── Animations/{Enemies,PlayerCharacters}/<Nome>/   AnimatorController + clips por personagem
│   │   └── Sprites/{Enemies,PlayerCharacters}/             sprites (+ Characters.png, Skills.png, UI.png)
│   ├── Prefabs/
│   │   ├── Cell.prefab                                     célula do grid
│   │   └── Entities/
│   │       ├── Character.prefab                            template base (SpriteRenderer + Animator + CharacterView)
│   │       ├── Enemy.prefab                                variant: + Enemy + EnemyController
│   │       └── PlayerCharacter.prefab                      variant: + PlayerCharacter + PlayerCharacterController
│   ├── Settings/                   ← dados de jogo (ScriptableObjects), editáveis pelo Inspector
│   │   ├── Characters/Enemies/*.asset                      EnemyConfig (Goblin, Rat, Slime, FireSkull, EyeBat)
│   │   ├── Characters/Players/*.asset                      PlayerCharacterConfig (Knight, Mage, Rogue)
│   │   ├── AI/ChaseAndAttack.asset                         EnemyBehavior padrão
│   │   ├── AI/Actions/*.asset                              EnemyActions (UseSkills, BasicAttack, ChaseTarget)
│   │   ├── Encounters/DefaultEncounter.asset               EncounterConfig usado pelo grid
│   │   └── Vfx/XpOrbsVfxSettings.asset                     ajustes visuais dos orbs de XP
│   ├── UI/                         ← UI Toolkit organizada por feature
│   │   ├── Theme/                                          GridBattle.tss (tema dos PanelSettings), Tokens.uss, Base.uss, Components.uss
│   │   ├── Settings/                                       PanelSettings, WorldPanelSettings, PanelTextSettings
│   │   ├── Layers/*.asset                                  UILayerDefinition (uma por painel)
│   │   ├── Screens/{Start,MainMenu,Game}/                  <Tela>.uxml + .uss + <Tela>View.asset (ViewDefinition)
│   │   ├── Overlays/Menu/                                  MenuOverlay.uxml/.uss + MenuOverlayView.asset
│   │   ├── Background/                                     Background.uxml/.uss + BackgroundView.asset
│   │   ├── Transitions/                                    ScreenTransition.uxml/.uss (cobertura) + ScreenCoverView/ScreenCoverTransition.asset
│   │   └── Animations/                                     Animator do menu (CharacterButtonsAnim)
│   └── Scripts/                    ← ver tabela abaixo
├── ZS/UI/                          ← framework reutilizável de navegação (ver Assets/ZS/UI/README.md)
│   ├── Runtime/                                            ZS.UI.asmdef: Core (SafeArea), Navigation, Transitions, Styles
│   ├── Editor/                                             ZS.UI.Editor.asmdef: inspector, picker de controlador, Navigation Debugger
│   ├── Tests/Editor/                                       ZS.UI.Tests.asmdef: testes EditMode
│   └── Samples/Navigation/                                 NavigationSample.unity (fora do build)
├── Scenes/SampleScene.unity
├── Resources/Fonts & Materials/                            fontes TMP
├── Settings/                                               URP, Build Profiles, Volume
├── TextMesh Pro/                                           assets de pacote
docs/                                                       vault Obsidian (design, análises)
```

### Scripts (`Assets/Application/Scripts`)

O namespace segue a pasta: `GridBattle.<Pasta>[.<Subpasta>]`.

| Pasta | Namespace | Conteúdo |
|---|---|---|
| `/` | `GridBattle` | `EventBus` (pub/sub estático) |
| `Editor/` | `GridBattle.Editor` | `GridControllerEditor` (botão **Reset grid**) |
| `Managers/` | `GridBattle.Managers` | `GameStateManager` (início de run, lista de personagens jogáveis), `TurnManager` (rodada dos inimigos), `GameConfigManager` (PPU, FPS mobile) |
| `Gameplay/` | `GridBattle.Gameplay` | `GridController` (tabuleiro + spawn), `Cell`, `CellContentHealthBar`, `EncounterConfig` |
| `Gameplay/Entities/` | `GridBattle.Gameplay.Entities` | `GridEntity` → `Character` → `PlayerCharacter` / `Enemy`; `CharacterView`, `CharacterFactory`, `ECharacter` |
| `Gameplay/Entities/Configs/` | `GridBattle.Gameplay.Entities` ⚠️ | `CharacterConfig` (abstrato), `EnemyConfig`, `PlayerCharacterConfig` — **exceção**: namespace não inclui `Configs` |
| `Gameplay/Entities/Interfaces/` | `…Entities.Interfaces` | `IDamageReceiver`, `IAttacker`, `IWalker` |
| `Gameplay/Entities/Skills/` | `…Entities.Skills` | `SkillDefinition` (abstrata) |
| `Gameplay/Controllers/` | `GridBattle.Gameplay.Controllers` | `CharacterControllerBase<T>`, `PlayerCharacterController` (input), `EnemyController` (IA) |
| `Gameplay/AI/` | `GridBattle.Gameplay.AI` | `EnemyBehavior`, `EnemyAction`, `EnemyTurnContext` |
| `Gameplay/AI/Actions/` | `…AI.Actions` | `UseSkillsAction`, `BasicAttackAction`, `ChaseTargetAction` |
| `Gameplay/Progression/` | `GridBattle.Gameplay.Progression` | `XpRewardSystem`, `XpPacket` |
| `Gameplay/Rules/` | `GridBattle.Gameplay.Rules` | `GridRules` (regras puras grid × character) |
| `Gameplay/Events/` | `GridBattle.Gameplay.Events` | eventos de gameplay |
| `UI/` | `GridBattle.UI` | `XpBarView`, `PixelPerfectPanelScale` |
| `UI/Flow/` | `GridBattle.UI.Flow` | `GameFlowController` (eventos do jogo → `Navigator`) |
| `UI/Screens/` | `GridBattle.UI.Screens` | `StartScreenController`, `MainMenuScreenController`, `GameScreenController` |
| `UI/Overlays/` | `GridBattle.UI.Overlays` | `MenuOverlayController` |
| `UI/Background/` | `GridBattle.UI.Background` | `BackgroundController` |
| `UI/Events/` | `GridBattle.UI.Events` | eventos de UI |
| `UI/Vfx/` | `GridBattle.UI.Vfx` | `XpOrbsVfx`, `XpOrbsVfxSettings` |

### Objetos da cena

| GameObject | Componentes |
|---|---|
| `Main Camera` | `PixelPerfectCamera`, `PixelPerfectPanelScale`, `Physics2DRaycaster` |
| `GameStateManager` | `GameStateManager` (singleton, `DontDestroyOnLoad`) |
| `Grid` | `GridController` (`[ExecuteAlways]`), `TurnManager`; células `Cell_x_y` são filhas |
| `XpRewardSystem` | `XpRewardSystem` |
| `UIManager` | `UIRoot`, `GameFlowController`; filhos (cada um `PanelRenderer` + `UILayer`, view LayerSource): `BackgroundLayer` (999 → -1), `StartScreenLayer` (0), `MainMenuLayer` (1, + Animator), `GameScreenLayer` (2, + `XpOrbsVfx`), `TransitionLayer` (999), `Overlays/MenuOverlayLayer` (5) |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` |

## Fluxos principais

```
Navegação de UI (ZS.UI)
  UIRoot.Awake → cria os controladores das views LayerSource → Replace(StartScreenView) instantâneo
  StartScreenTapEvent → GameFlowController → Navigator.Replace(MainMenuScreenView)
      → ScreenCoverTransition: cobre (0,3s + 0,2s) → troca → revela (0,3s)

Escolha de classe (MainMenuScreenController)
  → CharacterChoosenEvent
      → GameFlowController: Navigator.Replace(GameScreenView) (ignorado se já há transição)
      → BackgroundController: camada Background vai para sortingOrder -1
      → GameStateManager.StartRun(classe)
          → resolve PlayerCharacterConfig por CharacterClass
          → GridController.InitializeGrid(config)
              → recria células; CharacterFactory.Spawn(player) em (2,2)
              → CharacterFactory.Spawn(inimigo) para cada EnemyConfig do EncounterConfig

Turno
  Cell (clique) → CellTapEvent → PlayerCharacterController (andar/atacar via GridRules)
    → PlayerActionEvent → TurnManager → EnemyController.Act() de cada inimigo
        → EnemyConfig.Behavior.TakeTurn(EnemyTurnContext) → EnemyActions em ordem

Morte
  Character.Die → desocupa célula → CharacterDiedEvent → Destroy
    → player: GameFlowController → Navigator.Replace(MainMenuScreenView)
    → inimigo: XpRewardSystem → XpRewardDroppedEvent
        → XpOrbsVfx anima orbs até a barra → Collect(pacote)
            → PlayerCharacter.GainXp → PlayerXpChangedEvent → GameScreenController/XpBarView

Menu
  menu-button → MenuOpenedEvent → GameFlowController → Navigator.ShowModal(MenuOverlayView)
  toque fora do painel (overlay-background) → MenuOverlayController.Close()
```

## Padrões e convenções

### Dados em ScriptableObjects (data-driven)

- **O que diferencia personagens, IA, encontros e ajustes visuais é dado**, em
  assets sob `Assets/Application/Settings/`. Código só implementa
  comportamentos reutilizáveis.
- Todo ScriptableObject criável tem `[CreateAssetMenu]` no menu
  **`Create > GridBattle/...`** (`Characters`, `AI`, `AI/Actions`,
  `Encounter Config`, `VFX`). Use `[Header]`, `[Tooltip]` e `[Min]` para o
  Inspector.
- **Não crie um prefab por personagem.** Personagens usam os templates
  `Enemy.prefab`/`PlayerCharacter.prefab`; o config é aplicado no spawn por
  `CharacterFactory` (`Character.Initialize` + `CharacterView.Apply`). Prefab
  próprio só como variant do template, quando precisar de componentes extras.
- Assets de IA (`EnemyAction`) são compartilhados: **nunca guardam estado de
  runtime**.

### Responsabilidades

- **Controllers são roteadores**: decidem qual ação tomar (input ou IA) e a
  executam via `Character`/`GridController`. Lógica de regra não vive neles.
- **`GridRules` é o único lugar** com cálculos entre characters e grid
  (alcance, alvo válido, passo de perseguição, highlights). Funções puras e
  estáticas.
- **Gameplay não depende de UI.** A UI reage a eventos de gameplay. Quando a
  apresentação controla tempo (ex.: XP), o gameplay emite um evento que a
  apresentação pode assumir (`MarkPresented`) e mantém um fallback sem UI.
- Cada parte da UI que conhece nomes/medidas do UXML fica encapsulada numa
  classe (ex.: `XpBarView`); outros sistemas usam essa API, não `Q("...")`.
- Criação de personagens passa sempre por `CharacterFactory`.

### Comunicação por eventos

- `EventBus.Subscribe/Unsubscribe/Raise<T>` (estático). Eventos são classes
  simples com sufixo `Event`, em `Gameplay/Events` ou `UI/Events`.
- Toda inscrição tem o par de desinscrição (`Awake`/`OnDestroy` ou
  `OnEnable`/`OnDisable`). **O Play Mode está com domain reload desligado**
  (Enter Play Mode Options): estado estático persiste entre sessões.

### UI — navegação (`ZS.UI`)

- **Toda tela, modal ou página é um `ViewDefinition` (asset) + um
  `ViewController` (C# puro, `[Preserve]`)**, ao lado do seu UXML. Não crie
  MonoBehaviour por tela.
- Navegue só pelo `Navigator` (`UIRoot.Navigator` ou `Context.Navigator`):
  `Push`/`Replace`/`Pop`/`PopTo`, `ShowModal`/`CloseModal`, `HandleBack`.
  Páginas internas: `<zsnav:PageHost>` + `Context.GetPageNavigator(nome)`.
- Controladores: queries e callbacks de UI em `OnBind` (repetido a cada reload
  de UI); inscrições externas em `OnCreate`/`OnDestroy`. Projetos fazem
  override com `protected override`.
- O fluxo específico do jogo fica no `GameFlowController` (eventos →
  navegação). **`ZS.UI` não pode referenciar nada do GridBattle**; código
  específico de projeto não entra em `Assets/ZS`.
- No GridBattle cada view é `LayerSource` (o próprio UXML do painel), com uma
  camada por painel para manter os `sortingOrder` relativos ao mundo 2D e os
  caminhos do Animator.

### UI — UXML e USS

- **Sem estilos inline no UXML**: estilo vai para USS. Atributos de conteúdo
  (`text`, `source`, `icon-image`, `picking-mode`) continuam no UXML.
- **Tokens** (`Theme/Tokens.uss`): cores, espaçamentos, tamanhos de fonte,
  durações, offsets e sprites de skin (`--skin-*`). Use `var(--token)` e não
  repita URLs de sprites.
- **Componentes** (`Theme/Components.uss`): classes reutilizáveis
  (`.slot-button`, `.portrait-button`, `.portrait-image`, `.panel-frame`).
  Regras que sobrescrevem o tema de botões do Unity usam escopo `#container`
  para vencer `:hover`/`:active`/`:focus` do tema.
- USS de tela (`Screens/<Nome>/<Nome>.uss`) só com o layout específico da tela;
  `#id` só para ajustes únicos.
- Nomenclatura: kebab-case; modificadores com `--`
  (`.portrait-button--flush`); classes do framework com prefixo `zs-`.
- **Contratos de nome:** elementos usados por código (`container`,
  `menu-button`, `xp-bar-progress`, `xp-bar-detail-2`, `current-level`,
  `tap-to-play-label`, `*-btn`, `overlay-background`) e os caminhos do Animator
  do menu (`#container/#character-selection/#<x>-btn/#<x>-image`) não mudam de
  nome nem de hierarquia sem atualizar o código/clip.
- Não use `<Template>`/`<Instance>` onde há seletor `>` ou caminho de Animator
  (o `TemplateContainer` muda a hierarquia).
- UXML referencia USS por `project://database/...?guid=` (o GUID é a
  referência real); o TSS importa com caminhos relativos. Mova assets de UI
  pelo `AssetDatabase` (ou com o `.meta` junto).
- Elementos customizados: prefixo `zs` (`ZS.UI`, ex.: `zs:SafeArea`) e
  `zsnav` (`ZS.UI.Navigation`, ex.: `zsnav:PageHost`).

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
- Animações e tweens com LitMotion.
- Ao mover/renomear scripts ou assets, **mova o `.meta` junto** (o GUID é a
  referência usada por cenas, prefabs e assets).

## Como estender

| Quero... | Faça |
|---|---|
| Novo inimigo | `Create > GridBattle > Characters > Enemy Config`: sprite, animator, atributos, `prefab = Enemy.prefab`, `behavior`, `xpReward`. Adicione a um `EncounterConfig`. |
| Novo personagem jogável | `Player Character Config` com `prefab = PlayerCharacter.prefab` e `characterClass`; adicione em `GameStateManager.playableCharacters` (e um botão no menu). |
| Novo comportamento de IA | Subclasse de `EnemyAction` (+ `[CreateAssetMenu]`), crie o asset e combine num `EnemyBehavior`. |
| Nova skill | Subclasse de `SkillDefinition` (+ `[CreateAssetMenu]`), asset, e referencie em `CharacterConfig.skills`. |
| Novo encontro | Novo `EncounterConfig`; a ordem da lista é a ordem de spawn. |
| Reação visual de personagem | `CharacterView`. |
| Ajustar orbs de XP | `Settings/Vfx/XpOrbsVfxSettings.asset`; XP por orb em `XpRewardSystem.xpPerPacket`. |
| Nova tela/modal | UXML + USS em `UI/Screens/<Nome>/`; `ViewController` em `Scripts/UI/Screens`; `Create > ZS > UI > View Definition` (controlador, camada, modo, transição). Instantiate: basta a camada existir. LayerSource: GameObject com `PanelRenderer` (o UXML) + `UILayer` (`sourceView`) listado em `UIRoot.layers`. Navegue a partir do `GameFlowController`. |
| Nova página interna | `<zsnav:PageHost name="...">` no UXML da tela; `Context.GetPageNavigator("...").Push(definição)`. |
| Nova transição | Asset `CoverTransition`/`UssClassTransition`, ou subclasse de `ViewTransition`; referencie em `ViewDefinition.transition`. |
| Novo token/estilo | Valor em `Theme/Tokens.uss`; classe reutilizável em `Theme/Components.uss`. |

## Cuidados

- **Ordem dos inimigos não é determinística.** `TurnManager` usa
  `FindObjectsByType` sem ordenação; a ordem muda entre runs e altera o
  resultado quando dois inimigos disputam a mesma célula. Mudar isso é mudança
  de mecânica.
- **`GridController` é `[ExecuteAlways]`** e refaz o grid no `Awake` também
  em edit mode (com `debugPlayerConfig`). Por isso a cena guarda células e
  personagens “assados” e o diff da cena fica grande após um **Reset grid**.
  O checkbox do componente está desmarcado na cena e não tem efeito (só há
  `Awake`).
- Entrar em Play Mode altera os atlas dinâmicos do TMP
  (`ThaleahFat_TTF SDF.asset`, `LiberationSans SDF - Fallback.asset`).
  Reverta antes de commitar se não for intencional.
- Mudanças de gameplay devem ser deliberadas: refatorações precisam manter o
  jogo idêntico (ver Verificação).

## Verificação

Testes do framework: `unity command run_tests --mode EditMode --filter ZS.UI.Tests --filter_type assembly`.
O jogo não tem testes automatizados. O Unity Editor costuma estar aberto e acessível
pela CLI `unity` (pacote `com.unity.pipeline`):

- `unity command recompile` / `recompile_status` / `get_console_logs` para
  compilar e checar erros.
- `unity command run_script --file <arquivo fora de Assets> --entry Classe.Metodo`
  para rodar C# no Editor (migrações de assets, testes em Play Mode).
- Para provar que uma refatoração não mudou o jogo: script de Play Mode que
  joga uma partida determinística (escolhe a classe, ataca/anda por política
  fixa e registra estado por turno), com a ordem dos inimigos fixada só
  durante o teste; compare o resultado do código antigo (`git stash`) com o
  novo. Agende passos por `Time.time`: o loop do Play Mode trava com o Editor
  sem foco.
- Para provar que UI/USS/UXML não mudaram: script de Play Mode que percorre o
  fluxo (Start → MainMenu → Game → menu aberto/fechado → morte) e grava, por
  estado, a árvore visual de cada painel (`worldBound` + `resolvedStyle`) e a
  linha do tempo de visibilidade/`sortingOrder`; compare antes × depois
  ignorando as listas de classes.

## Histórico de refatorações

Registre aqui toda mudança estrutural (mais recente primeiro).

### 2026-09-30 — Framework de navegação `ZS.UI` e reorganização de USS/UXML
- **Novo framework `Assets/ZS/UI`** (asmdefs `ZS.UI`, `ZS.UI.Editor`,
  `ZS.UI.Tests`, `ZS.UI.Samples`; namespace `ZS.*`, sem dependência do jogo):
  `UIRoot`, `UILayer` (PanelRenderer/UIDocument), `UILayerDefinition`,
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
