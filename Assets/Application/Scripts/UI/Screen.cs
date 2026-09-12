using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Screen : MonoBehaviour
    {
        public PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
        }

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