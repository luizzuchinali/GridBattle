using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    public class CharacterDiedEvent
    {
        public Character Character { get; }

        public CharacterDiedEvent(Character character)
        {
            Character = character;
        }
    }
}