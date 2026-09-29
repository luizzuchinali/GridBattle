using GridBattle.Gameplay.Entities;
using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class MainMenuScreenView : View
    {
        protected override UIScreen? Screen => UIScreen.MainMenu;

        private VisualElement _container;

        [SerializeField]
        public PlayerCharacter[] playerCharacterPrefabs;

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            _container = root.Q<VisualElement>("container");
            var character1 = _container.Q<Button>("character-1");
            character1.RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior)); });
            Instantiate(playerCharacterPrefabs[0], character1.);
            _container.Q<Button>("character-2").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Mage)); });
            _container.Q<Button>("character-3").RegisterCallback<ClickEvent>(_ => { EventBus.Raise(new CharacterChoosenEvent(ECharacter.Rogue)); });
        }
    }
}