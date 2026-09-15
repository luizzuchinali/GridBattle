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

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
        }

        public virtual void Show()
        {
            _container.ToggleInClassList("display-none");
            _container.ToggleInClassList("translate-right");
        }

        public virtual void Hide()
        {
            _container.ToggleInClassList("display-none");
            _container.ToggleInClassList("translate-right");
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
            OnUIReload(panelRenderer, rootElement);
        }

        protected abstract void OnUIReload(PanelRenderer panelRenderer, VisualElement root);
    }
}