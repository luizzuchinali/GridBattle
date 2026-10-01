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
| UI | UI Toolkit (UXML/USS) via `PanelRenderer` |
| Tweens | LitMotion (`LMotion`) |
| Input | Input System (`InputSystemUIInputModule`) + `IPointerClickHandler` nas células |
| Texto no mundo | TextMeshPro (dano flutuante das células) |
| Assemblies | `Assembly-CSharp` e `Assembly-CSharp-Editor` (sem `.asmdef`) |
| Testes | não há suíte automatizada (ver [Verificação](#verificação)) |
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
│   ├── UI/                                                 UXML das telas, USS/, Overlays/, Animations/
│   └── Scripts/                    ← ver tabela abaixo
├── Scenes/SampleScene.unity
├── Resources/Fonts & Materials/                            fontes TMP
├── Settings/                                               URP, Build Profiles, Volume
├── TextMesh Pro/ · UI Toolkit/                             assets de pacotes
docs/                                                       vault Obsidian (design, análises)
```

### Scripts (`Assets/Application/Scripts`)

O namespace segue a pasta: `GridBattle.<Pasta>[.<Subpasta>]`.

| Pasta | Namespace | Conteúdo |
|---|---|---|
| `/` | `GridBattle` | `EventBus` (pub/sub estático), `AssemblyInfo` (prefixo UXML `gb`) |
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
| `UI/` | `GridBattle.UI` | `View` (base) e views de tela, `XpBarView`, `SafeArea`, `PixelPerfectPanelScale`, `UIScreen` |
| `UI/Controllers/` | `GridBattle.UI.Controllers` | `NavigationController` (fluxo e visibilidade de telas) |
| `UI/Events/` | `GridBattle.UI.Events` | eventos de UI |
| `UI/Vfx/` | `GridBattle.UI.Vfx` | `XpOrbsVfx`, `XpOrbsVfxSettings` |

### Objetos da cena

| GameObject | Componentes |
|---|---|
| `Main Camera` | `PixelPerfectCamera`, `PixelPerfectPanelScale`, `Physics2DRaycaster` |
| `GameStateManager` | `GameStateManager` (singleton, `DontDestroyOnLoad`) |
| `Grid` | `GridController` (`[ExecuteAlways]`), `TurnManager`; células `Cell_x_y` são filhas |
| `XpRewardSystem` | `XpRewardSystem` |
| `UIManager` | `NavigationController`; filhos: `Background`, `StartScreenView`, `MainMenuScreenView`, `GameScreenView` (+ `XpOrbsVfx`), `ScreenTransitionView`, `Overlays/MenuScreenOverlay` |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` |

## Fluxos principais

```
Escolha de classe (MainMenuScreenView)
  → CharacterChoosenEvent
      → NavigationController: transição para UIScreen.Game
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
    → player: NavigationController volta ao menu
    → inimigo: XpRewardSystem → XpRewardDroppedEvent
        → XpOrbsVfx anima orbs até a barra → Collect(pacote)
            → PlayerCharacter.GainXp → PlayerXpChangedEvent → GameScreenView/XpBarView
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

### UI

- Views herdam de `View` (exige `PanelRenderer`) e montam a UI em
  `OnUIReload(panelRenderer, root)`. A visibilidade das telas é do
  `NavigationController` (classe `display-none` no elemento `container`);
  overlays controlam a própria.
- Elementos customizados de UXML usam o prefixo `gb` (`AssemblyInfo.cs`).

### Código C#

- **Toda documentação em `.cs` é em inglês** (XML docs, comentários,
  `[Tooltip]`, `[Header]`, mensagens de log). Documentos em `docs/` e
  mensagens de commit ficam em português.
- Campos serializados: `[SerializeField] private` em `camelCase`, expostos por
  propriedades somente leitura. Campos privados não serializados: `_camelCase`.
  Constantes e `static readonly`: `PascalCase`.
- Enums com prefixo `E` (`ECharacter`, `ECellHighlightType`); exceção
  histórica: `UIScreen`.
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
| Nova tela | Subclasse de `View` com `Screen` retornando um novo valor de `UIScreen`; transições no `NavigationController`. |

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

Não há testes automatizados. O Unity Editor costuma estar aberto e acessível
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

## Histórico de refatorações

Registre aqui toda mudança estrutural (mais recente primeiro).

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
