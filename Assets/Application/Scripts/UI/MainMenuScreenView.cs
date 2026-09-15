using GridBattle.UI.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class MainMenuScreenView : View
    {
        private VisualElement _container;

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            _container = root.Q<VisualElement>("container");
            _container.RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent()); });
        }
    }
}