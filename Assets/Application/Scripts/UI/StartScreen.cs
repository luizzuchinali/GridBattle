using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle
{
    [RequireComponent(typeof(PanelRenderer))]
    public class StartScreen : MonoBehaviour
    {
        private PanelRenderer _panelRenderer;

        private VisualElement _tapToPlayElement;
        private VisualElement _container;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            _panelRenderer.RegisterUIReloadCallback(ReloadCallback);
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(ReloadCallback);
        }

        private void ReloadCallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            _tapToPlayElement = rootElement.Q<VisualElement>("tap-to-play-label");
            _tapToPlayElement.RegisterCallback<TransitionEndEvent, VisualElement>((_, target) =>
            {
                target.ToggleInClassList("fade-out");
            }, _tapToPlayElement);
            _tapToPlayElement.schedule.Execute(() => _tapToPlayElement.AddToClassList("fade-out")).StartingIn(100);
            
            _container = rootElement.Q<VisualElement>("container");
            _container.RegisterCallback<PointerUpEvent>(_ =>
            {
                Debug.Log("Tapped!");
            });
        }
    }
}