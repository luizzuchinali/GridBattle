using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a state is applied to a character (or reapplied, see <see cref="Reapplied"/>).
    /// </summary>
    public class StateAppliedEvent
    {
        public Character Character { get; }
        public StateInstance State { get; }
        public bool Reapplied { get; }

        public StateAppliedEvent(Character character, StateInstance state, bool reapplied)
        {
            Character = character;
            State = state;
            Reapplied = reapplied;
        }
    }
}
