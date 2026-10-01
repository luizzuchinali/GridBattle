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
        /// Which screen this view represents. Screens have their visibility controlled
        /// by NavigationController; overlays/auxiliary views return null (always visible).
        /// </summary>
        protected virtual UIScreen? Screen => null;

        /// <summary>
        /// Root of this view's panel (null until the UI loads).
        /// </summary>
        protected VisualElement Root => _rootElement;

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
        /// Whether this view represents a screen (visibility controlled by
        /// NavigationController). Overlays/auxiliary views return false and
        /// control their own visibility.
        /// </summary>
        public bool IsScreenView => Screen != null;

        /// <summary>
        /// Whether this view belongs to the given screen. Auxiliary views
        /// (Screen == null) never belong to a specific screen.
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
        /// Visibility is NavigationController's responsibility. After a UI reload
        /// (when the container is recreated visible), the controller decides the
        /// state of all views again.
        /// </summary>
        private void RequestVisibilityApply()
        {
            var navigationController = FindAnyObjectByType<NavigationController>();
            navigationController?.ApplyScreenVisibility();
        }

        protected abstract void OnUIReload(PanelRenderer panelRenderer, VisualElement root);
    }
}