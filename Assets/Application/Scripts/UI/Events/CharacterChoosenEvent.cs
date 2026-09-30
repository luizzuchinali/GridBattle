using GridBattle.Gameplay.Entities;

namespace GridBattle.UI.Events
{
    public class CharacterChoosenEvent
    {
        public ECharacter Character { get; private set; }

        public CharacterChoosenEvent(ECharacter character)
        {
            Character = character;
        }
    }
}