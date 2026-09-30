using GridBattle.Gameplay.Entities;
using GridBattle.UI.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class MainMenuScreenView : View
    {
        protected override UIScreen? Screen => UIScreen.MainMenu;

        private VisualElement _container;

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            _container = root.Q<VisualElement>("container");
            _container.Q<Button>("knight-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior));
            });
            _container.Q<Button>("mage-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Mage));
            });
            _container.Q<Button>("rogue-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Rogue));
            });
        }
    }
}