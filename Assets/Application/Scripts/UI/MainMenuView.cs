using GridBattle.UI.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class MainMenuView : View
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
            _container.RegisterCallback<ClickEvent, VisualElement>((e, target) =>
            {
                target.ToggleInClassList("display-none");
                target.ToggleInClassList("translate-right");
                EventBus.Raise(new CharacterChoosenEvent());
            }, _container);
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            _container.ToggleInClassList("display-none");
            _container.ToggleInClassList("translate-right");
        }
    }
}