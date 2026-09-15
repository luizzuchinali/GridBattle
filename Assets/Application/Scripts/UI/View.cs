using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class View : MonoBehaviour
    {
        public PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
        }

        public abstract void Show();

        public abstract void Hide();

        protected virtual void OnEnable()
        {
            PanelRenderer.RegisterUIReloadCallback(ReloadUICallback);
        }

        protected virtual void OnDisable()
        {
            PanelRenderer.UnregisterUIReloadCallback(ReloadUICallback);
        }

        protected abstract void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version);
    }
}