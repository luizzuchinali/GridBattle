using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a state ends or is removed from a character.
    /// </summary>
    public class StateRemovedEvent
    {
        public Character Character { get; }
        public StateInstance State { get; }

        public StateRemovedEvent(Character character, StateInstance state)
        {
            Character = character;
            State = state;
        }
    }
}
