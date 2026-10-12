using GridBattle.Gameplay.Entities;

namespace GridBattle.UI.Events
{
    /// <summary>
    /// A character card of the class selection was tapped: the flow opens the character details window, where the
    /// player reads about the class and may start a new run with it.
    /// </summary>
    public sealed class CharacterDetailsRequestedEvent
    {
        public CharacterDetailsRequestedEvent(ECharacter character)
        {
            Character = character;
        }

        public ECharacter Character { get; }
    }
}
