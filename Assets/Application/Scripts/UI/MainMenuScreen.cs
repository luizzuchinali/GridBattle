using GridBattle.Events;
using UnityEngine.UIElements;

namespace GridBattle
{
    public class MainMenuScreen : Screen
    {
        private VisualElement _container;

        protected override void Awake()
        {
            base.Awake();

            EventBus.Subscribe<StartScreenTapEvent>(OnStartScreenTap);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
        }

        protected override void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            _container = rootElement.Q<VisualElement>("container");
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            _container.RemoveFromClassList("display-none");
            _container.ToggleInClassList("translate-right");
        }
    }
}