using GridBattle.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class View : MonoBehaviour
    {
        protected PanelRenderer PanelRenderer;
        private VisualElement _rootElement;
        private VisualElement _container;

        /// <summary>
        /// Qual tela esta view representa. Telas têm a visibilidade controlada pelo
        /// NavigationController; overlays/views auxiliares retornam null (sempre visíveis).
        /// </summary>
        protected virtual UIScreen? Screen => null;

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
        }

        public virtual void Show()
        {
            _container?.EnableInClassList("display-none", false);
        }

        public virtual void Hide()
        {
            _container?.EnableInClassList("display-none", true);
        }

        /// <summary>
        /// Indica se esta view representa uma tela (visibilidade controlada pelo
        /// NavigationController). Overlays/views auxiliares retornam false e
        /// controlam a própria visibilidade.
        /// </summary>
        public bool IsScreenView => Screen != null;

        /// <summary>
        /// Indica se esta view pertence à tela informada. Views auxiliares
        /// (Screen == null) nunca pertencem a uma tela específica.
        /// </summary>
        public bool BelongsToScreen(UIScreen screen)
        {
            return Screen != null && Screen == screen;
        }

        protected virtual void OnEnable()
        {
            PanelRenderer.RegisterUIReloadCallback(ReloadUICallback);
        }

        protected virtual void OnDisable()
        {
            PanelRenderer.UnregisterUIReloadCallback(ReloadUICallback);
        }

        private void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            _rootElement = rootElement;
            _container = rootElement.Q<VisualElement>("container");

            RequestVisibilityApply();
            OnUIReload(panelRenderer, rootElement);
        }

        /// <summary>
        /// A visibilidade é responsabilidade do NavigationController. Após um reload
        /// da UI (quando o container volta a nascer visível), o controller decide
        /// novamente o estado de todas as views.
        /// </summary>
        private void RequestVisibilityApply()
        {
            var navigationController = FindAnyObjectByType<NavigationController>();
            navigationController?.ApplyScreenVisibility();
        }

        protected abstract void OnUIReload(PanelRenderer panelRenderer, VisualElement root);
    }
}