using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by TurnManager at the start of an entity's own turn, after its states ran their turn-start effects.
    /// </summary>
    public class EntityTurnStartedEvent
    {
        public Character Character { get; }
        public int GlobalTurn { get; }

        public EntityTurnStartedEvent(Character character, int globalTurn)
        {
            Character = character;
            GlobalTurn = globalTurn;
        }
    }
}
