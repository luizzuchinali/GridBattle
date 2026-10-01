using GridBattle.Gameplay.Entities;
using GridBattle.UI.Events;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Character selection: each portrait button raises CharacterChoosenEvent.
    /// </summary>
    [Preserve]
    public sealed class MainMenuScreenController : ViewController
    {
        protected override void OnBind(VisualElement root)
        {
            root.Q<Button>("knight-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Warrior));
            });
            root.Q<Button>("mage-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Mage));
            });
            root.Q<Button>("rogue-btn").RegisterCallback<ClickEvent>(_ =>
            {
                EventBus.Raise(new CharacterChoosenEvent(ECharacter.Rogue));
            });
        }
    }
}
