# ZS.UI — navegação de telas sobre UI Toolkit

Sistema reutilizável de telas, modais e páginas para UI Toolkit (Unity 6).
Independente de projeto: namespace `ZS.*`, assemblies próprios e nenhuma
dependência externa. Ainda não é um pacote, mas a pasta já tem a estrutura de
um (`Runtime/`, `Editor/`, `Tests/`, `Samples/`).

```
ZS/UI/
├── Runtime/       ZS.UI.asmdef
│   ├── Core/          SafeArea, VisualElementExtensions (SetDisplayed)
│   ├── Navigation/    UIRoot, UILayer, UILayerDefinition, ViewDefinition, ViewController,
│   │                  ModalController<T>, Navigator, PageNavigator, PageHost, NavigationOptions
│   ├── Transitions/   ViewTransition, TransitionScope, CoverTransition, UssClassTransition
│   └── Styles/        ZSTransitions.uss (fade/slide/backdrop opcionais)
├── Editor/        ZS.UI.Editor.asmdef — inspector do ViewDefinition, picker de controlador, debugger
├── Tests/Editor/  ZS.UI.Tests.asmdef — testes EditMode do Navigator
└── Samples/Navigation/  cena de exemplo (pilha, modal com resultado, páginas, fade)
```

## Conceitos

| Conceito | O que é |
|---|---|
| **Camada** (`UILayer` + `UILayerDefinition`) | Um painel (PanelRenderer ou UIDocument) com seu `sortingOrder`. A definição (asset) é a identidade usada pelas views; o componente liga essa identidade a um painel da cena. |
| **View** (`ViewDefinition`) | Asset com UXML, estilos extras, tipo do controlador, camada, modo de montagem, transição e opções de modal. Serve para tela, modal ou página. |
| **Controlador** (`ViewController`) | Classe C# pura (não MonoBehaviour) criada pelo sistema. Ciclo de vida: `OnCreate` → `OnBind(root)` (de novo a cada reload de UI) → `OnEnter(args)` → `OnEntered` → `OnExit` → `OnExited` → `OnDestroy`; `OnBack()` para tratar o voltar. Use `[Preserve]` nas subclasses. |
| **Navigator** | Pilha de telas (`Push`, `Replace`, `Pop`, `PopTo`), modais (`ShowModal`, `ShowModal<T>`, `CloseModal`) e voltar (`HandleBack`). Operações retornam `Awaitable`. |
| **Páginas** (`PageHost` + `PageNavigator`) | Navegação dentro de uma view (abas, wizard): `<zsnav:PageHost name="x"/>` no UXML + `Context.GetPageNavigator("x")`. |
| **Transição** (`ViewTransition`) | Asset que anima a troca e decide quando ela acontece (`ShowTo`, `HideFrom`, `Swap`). Incluídas: `CoverTransition` (cobertura que entra, troca e sai) e `UssClassTransition` (fade/slide via classes USS). |
| **UIRoot** | Ponto de entrada na cena: lista camadas, views estáticas, tela inicial e política de concorrência (`Ignore`/`Queue` para pedidos durante uma transição). |

### Modos de montagem

- **Instantiate** (padrão): o UXML é clonado na camada (ou no PageHost) a cada
  instância. Permite várias instâncias, modais dinâmicos e páginas.
- **LayerSource**: a view é o próprio conteúdo do painel da camada (o UXML do
  PanelRenderer/UIDocument). Uma instância por camada, criada com o UIRoot.
  Mantém a hierarquia exata (útil para Animator sobre UI Toolkit) e permite
  preview em edit mode. A camada aponta para a view em `UILayer.sourceView`.

Mostrar/esconder usa `display` inline no(s) elemento(s) raiz da view; o UXML
de uma view deve ter **um elemento raiz**.

## Plugando num projeto

1. Crie os assets de camada: `Create > ZS > UI > Layer Definition` (ex.:
   `Screens`, `Modals`).
2. Na cena: um GameObject com `UIRoot`; filhos com `UIDocument` (ou
   `PanelRenderer`) + `UILayer` apontando para cada definição; liste-os em
   `UIRoot.layers` (o `sortingOrder` do painel define a ordem de desenho).
3. Para cada tela/modal/página: UXML + subclasse de `ViewController` +
   `Create > ZS > UI > View Definition` (escolha o controlador no dropdown,
   a camada e a transição).
4. Defina `UIRoot.initialScreen` e navegue:

```csharp
[Preserve]
public sealed class HomeController : ViewController
{
    protected override void OnBind(VisualElement root)
    {
        root.Q<Button>("play").clicked += () => Context.Navigator.Push(gameDefinition, levelId);
        root.Q<Button>("quit").clicked += async () =>
        {
            if (await Context.Navigator.ShowModal<bool>(confirmDefinition, "Quit?"))
                Application.Quit();
        };
    }
}
```

- **Dependências:** atribua um `IServiceProvider` em `UIRoot.Services` e use
  `Context.GetService<T>()` nos controladores (qualquer container de DI serve).
- **Voltar:** chame `UIRoot.HandleBack()` a partir do seu input (Esc/Android);
  ele fecha o modal do topo, depois volta páginas, depois a tela; retorna
  `false` na raiz.
- **Fluxo do jogo:** mantenha a lógica de "quando ir para qual tela" fora do
  framework (ex.: um MonoBehaviour que traduz eventos do jogo em chamadas ao
  `Navigator`).

## Estendendo

- **Nova transição:** subclasse de `ViewTransition` (+ `[CreateAssetMenu]`);
  use `scope.CancellationToken` nos `Awaitable`s; o que não for feito até o fim
  de `Run` é completado pelo navegador.
- **Modal com resultado:** herde de `ModalController<T>` e chame `Close(valor)`.
- **Inspeção em Play Mode:** `Window > ZS > UI Navigation Debugger`.
- **Testes:** `ZS.UI.Tests` usa um host falso (`FakeHost`), sem cena. No
  assembly de testes (friend via `InternalsVisibleTo`), overrides de ciclo de
  vida são `protected internal override`; em projetos, `protected override`.

## Limitações conhecidas (v1)

- `UIDocument` não tem callback de reload: o `UILayer` verifica a troca do
  `rootVisualElement` a cada frame.
- Páginas sempre usam `Instantiate`; o estado visual de uma página é refeito
  (re-clonado) após um reload de UI, o estado do controlador é mantido.
- Hooks de ciclo de vida são síncronos; carregamentos assíncronos devem ser
  iniciados em `OnEnter` pelo próprio controlador.
