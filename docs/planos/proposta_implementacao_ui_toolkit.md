---
tags:
  - plano
  - implementacao
  - ui-toolkit
  - navegacao
created: 2026-09-14
updated: 2026-09-14
aliases:
  - Proposta de Implementação UI Toolkit GridBattle
---

# Proposta de Implementação — UI Toolkit GridBattle

> Proposta derivada de `comparacao_arquiteturas_ui_toolkit.md`. Combina a arquitetura geral de `unity-6-6-ui-toolkit-architecture.md` com as garantias de navegação de `plano_navegacao_stack_ui_toolkit.md`, sem migrar a cena para um host único. Esta entrega é de planejamento: nenhum código, UXML, USS ou cena é alterado aqui.

## 1. Decisão arquitetural

**Painéis distribuídos com `NavigationService` centralizado.**

- Manter um `PanelRenderer` por view (`StartScreen`, `MainMenuScreen`, `GameScreen` e futuras), todos compartilhando `Assets/UI Toolkit/PanelSettings.asset` (resolução `216×384`).
- As raízes dos renderizadores permanecem irmãs no mesmo painel de runtime; a ordenação entre elas é controlada por `PanelRenderer.sortingOrder`, derivado e restaurado pelo serviço — nunca fixado no cenário.
- A pilha lógica (páginas e modais) é mantida pelo serviço, referenciando as views existentes; `Push`/`Pop` atualizam estado, interação e `sortingOrder`, não criam renderers.
- Migração da classe `Screen` é mínima: ela continua exigindo seu `PanelRenderer` e o callback de reload.

### 1.1 Por que não o host único

A cena já possui três renderizadores no mesmo painel; o host único exigiria reescrever `Screen`, reestruturar a cena e centralizar o rebuild, sem benefício enquanto existir uma instância por tipo. Suas garantias (conclusão após transição, rollback, `Busy`, foco modal, bloqueio de entrada) são incorporadas ao serviço e à apresentação distribuída.

### 1.2 Quando reavaliar

- Criação dinâmica de entradas ou múltiplas instâncias do mesmo tipo.
- Necessidade de camadas visuais explícitas que espelhem as pilhas.
- Nesses casos, migrar para o modelo de host único de `plano_navegacao_stack_ui_toolkit.md` (seções 5–7 permanecem válidas).

## 2. Arquitetura

```text
UIManager (GameObject "UI")
├── NavigationFlowController   (traduz EventBus → comandos; decide QUANDO)
├── NavigationService          (pilhas, validação tipada, concorrência, confirmação)
├── DistributedPresentation    (ordena raízes, aplica classes USS, bloqueia, foca)
│
├── Screens/                   (filhos do UIManager)
│   ├── StartScreen (PanelRenderer + StartScreen.cs)
│   ├── MainMenuScreen (PanelRenderer + MainMenuScreen.cs)
│   └── GameScreen (PanelRenderer + GameScreen.cs)
└── Modals/                    (futuro: PanelRenderer de Modal / Overlay)
```

Os três componentes são `MonoBehaviour`s distintos no mesmo GameObject, e os objetos com os `PanelRenderer`s ficam como filhos dele — mesma organização recomendada em `unity-6-6-ui-toolkit-architecture.md` (seção 13), com o orquestrador dividido em componentes em vez de monolítico.

Responsabilidades:

| Componente | Responsabilidade |
|---|---|
| `NavigationService` | Duas pilhas LIFO (páginas e modais), validação por tipo, `Busy`, confirmação somente após a apresentação, rollback |
| `DistributedPresentation` | Deriva/restaura `sortingOrder`, aplica classes USS de estado, bloqueia raízes cobertas, gerencia foco |
| `NavigationFlowController` | Assina `StartScreenTapEvent` e `CharacterChoosenEvent`; traduz intenções em comandos; publica notificações só após confirmação |
| `Screen` (base) | Sem alteração estrutural: renderer próprio, reload callback |
| Controllers de tela | Apenas comportamento local; não alteram visibilidade de outras telas |
| `IUITransition` / `UssTransition` | Executa animações por classes USS; trocável por LitMotion no futuro |

## 3. API do serviço

Mesmas operações e pré-condições do plano de navegação (tipos como chave, validação antes de mutar):

```csharp
public interface INavigationService
{
    bool IsBusy { get; }
    Awaitable<NavigationResult> PushAsync<TPage>(CancellationToken ct = default) where TPage : Screen;
    Awaitable<NavigationResult> PopAsync<TPage>(CancellationToken ct = default) where TPage : Screen;
    Awaitable<NavigationResult> ReplaceAsync<TPage>(CancellationToken ct = default) where TPage : Screen;
    Awaitable<NavigationResult> ShowModalAsync<TModal>(CancellationToken ct = default) where TModal : Screen;
    Awaitable<NavigationResult> CloseModalAsync<TModal>(CancellationToken ct = default) where TModal : Screen;
    Awaitable<NavigationResult> BackAsync<TExpectedEntry>(CancellationToken ct = default) where TExpectedEntry : Screen;
}
```

- Resultados: `Ok`, `Busy`, `AtRoot`, `TypeMismatch`, `NotRegistered`, `AlreadyActive`, `ModalBlocking`.
- Uma operação por vez; sem fila. Modais abertos bloqueiam operações de página; `BackAsync` fecha o modal superior primeiro.
- Páginas cobertas permanecem com seu renderer ativo (raiz visível ou classe `covered`), preservando estado; o serviço decide retenção, não o controller.

## 4. Ordenação e bloqueio (diferença-chave do host único)

Como a ordem é dada por `sortingOrder` e não pela hierarquia, `DistributedPresentation` mantém faixas fixas:

```text
Screens:    sortingOrder base (ex.: 10) + índice na pilha de páginas
Modais:     faixa alta (ex.: 100+) + índice na pilha de modais
```

- Ao `Push`, a nova página recebe o próximo valor de screen; a anterior fica coberta (classe `--covered`, sem interação via bloqueio explícito, não apenas `picking-mode`).
- Ao `Pop`, o serviço restaura o `sortingOrder` anterior e revela a página conservada.
- `transitionBlocker` visual é substituído por bloqueio direto das raízes cobertas; a exclusão mútua de operações já é garantida pelo serviço (`Busy`).
- Foco: guardar o elemento focado antes de cobrir; restaurar após revelar, com fallback válido e preservando o estado original `enabled` dos controles.

## 5. Transições

Regras do plano de navegação aplicadas às raízes irmãs:

- Classes USS de estado por raiz: `.screen--prepared`, `--visible`, `--exit-*`, `--covered` (em `Assets/Application/UI/USS/Navigation.uss`); `display: none` só após a animação de saída; sem `z-index` nem estilo inline.
- `UssTransition` com presets `None`, `Fade`, `Slide` (usando geometria relativa, não o `216px` fixo de `Base.uss`).
- Conclusão via `TransitionEndEvent` filtrado por alvo e propriedade (ignorar eventos propagados, como o do `tap-to-play-label`), com timeout de segurança.
- Cancelamento/rebuild: o callback `ReloadUICallback` de cada tela reata somente sua própria raiz; falha em uma transição restaura classes, ordem e foco anteriores e libera o bloqueio em `finally`.

## 6. Migração do fluxo atual

- `StartScreen`: publica `StartScreenTapEvent`, deixa de alternar classes de outras telas.
- `MainMenuScreen`: botões de personagem emitem a intenção de seleção; clique genérico no contêiner deixa de navegar; para de reagir a `CharacterChoosenEvent` trocando visibilidade própria/global.
- `NavigationFlowController`: `StartScreenTapEvent → ReplaceAsync<MainMenuScreen>()`; `CharacterChoosenEvent → PushAsync<GameScreen>()`.
- Fluxo alvo: `StartScreen → MainMenuScreen → GameScreen → MainMenuScreen` (replace inicial sem histórico; push cria entrada; pop conserva o menu).
- Cena: nenhum objeto novo é obrigatório; a ordem fixa atual (`m_SortingOrder` 0/1/2) passa a ser derivada pelo serviço.

## 7. Roteiro

| Marco | Conteúdo | Critério |
|---|---|---|
| 1. Núcleo | `NavigationEntry`, `NavigationResult`, catálogo tipado, `NavigationService`, `NavigationFlowController` em `Assets/Application/Scripts/UI/Navigation/` | Testes EditMode com apresentação falsa passam |
| 2. Apresentação | `DistributedPresentation` (sortingOrder, bloqueio, foco) sobre os renderers existentes | `Push`/`Pop` confirmam após transição |
| 3. Transições | `IUITransition`, `UssTransition`, `Navigation.uss` | `None`/`Fade`/`Slide` terminam no mesmo estado |
| 4. Fluxo | Migração dos três controllers e do EventBus | Fluxo alvo completo no Editor |
| 5. Modais (opcional) | Primeiro modal real com backdrop-raiz e foco confinado | `BackAsync` modal-first validado |

## 8. Validação

### EditMode (apresentação falsa)

- Push/Pop/Replace, raiz, tipo não registrado, categoria errada, tipo incompatível, duplicado, `Busy` concorrente, cancelamento/rollback, confirmação única, limpeza do bloqueio em exceções.

### PlayMode (Editor)

- Fluxo `StartScreen → MainMenuScreen → GameScreen → MainMenuScreen` com estado do menu preservado.
- Páginas cobertas sem ponteiro/submit/teclado; foco restaurado ao fechar modal.
- `TransitionEndEvent` de filho não conclui a transição da raiz.
- Rebuild/desativação durante transição sem callbacks duplicados nem operação pendente.

## 9. Limites

Persistência, Addressables, múltiplas instâncias por tipo e regras de gameplay permanecem fora do escopo. A recomendação (painéis distribuídos) deve ser reavaliada conforme os gatilhos da seção 1.2.