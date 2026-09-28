using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GameScreenView : View
    {
        protected override UIScreen? Screen => UIScreen.Game;

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            var menuButton = root.Q<Button>("menu-button");
            menuButton.RegisterCallback<PointerUpEvent>(_ => { EventBus.Raise(new MenuOpenedEvent()); });
        }
    }
}
