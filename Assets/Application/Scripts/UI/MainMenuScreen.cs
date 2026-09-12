using System;
using GridBattle.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle
{
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuScreen : MonoBehaviour
    {
        private PanelRenderer _panelRenderer;
        private VisualElement _container;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            EventBus.Subscribe<StartScreenTapEvent>(OnStartScreenTap);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
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
            _container = rootElement.Q<VisualElement>("container");
            _container.RegisterCallback<PointerUpEvent>(_ => { Debug.Log("Tapped!"); });
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            _container.RemoveFromClassList("display-none");
            _container.ToggleInClassList("translate-right");
        }
    }
}