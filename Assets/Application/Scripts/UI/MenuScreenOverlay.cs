using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class MenuScreenOverlay : View
    {
        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            PanelRenderer.sortingOrder = 5;

            var overlayBackground = root.Q<VisualElement>("overlay-background");
            overlayBackground.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.target == overlayBackground)
                {
                    Hide();
                }
            });

            Hide();
        }

        protected override void Awake()
        {
            base.Awake();

            EventBus.Subscribe<MenuOpenedEvent>(OnMenuOpened);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MenuOpenedEvent>(OnMenuOpened);
        }

        private void OnMenuOpened(MenuOpenedEvent e)
        {
            Show();
        }
    }
}
