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
            _container.ToggleInClassList("display-none");
        }

        public virtual void Hide()
        {
            _container.ToggleInClassList("display-none");
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

            ApplyPersistedVisibility();
            OnUIReload(panelRenderer, rootElement);
        }

        private void ApplyPersistedVisibility()
        {
            if (Screen == null || _container == null)
                return;

            var navigationController = FindAnyObjectByType<NavigationController>();
            if (navigationController == null)
                return;

            var shouldShow = navigationController.CurrentScreen == Screen;
            _container.EnableInClassList("display-none", !shouldShow);
        }

        protected abstract void OnUIReload(PanelRenderer panelRenderer, VisualElement root);
    }
}