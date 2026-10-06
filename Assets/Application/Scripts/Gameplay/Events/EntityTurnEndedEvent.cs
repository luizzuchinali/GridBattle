using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by TurnManager at the end of an entity's own turn, before its state durations go down. Hooks for terrain, cooldowns and AI memory.
    /// </summary>
    public class EntityTurnEndedEvent
    {
        public Character Character { get; }
        public int GlobalTurn { get; }

        public EntityTurnEndedEvent(Character character, int globalTurn)
        {
            Character = character;
            GlobalTurn = globalTurn;
        }
    }
}
