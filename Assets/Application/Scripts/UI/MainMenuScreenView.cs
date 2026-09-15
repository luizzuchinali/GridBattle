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
            _container.Q<Button>("character-1").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
            _container.Q<Button>("character-2").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Mage)); });
            _container.Q<Button>("character-3").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Rogue)); });
            _container.Q<Button>("character-4").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
            _container.Q<Button>("character-5").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
            _container.Q<Button>("character-6").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
            _container.Q<Button>("character-7").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
        }
    }
}