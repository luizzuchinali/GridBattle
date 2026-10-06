namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by TurnManager when a global turn starts (the player plays first, then every enemy).
    /// </summary>
    public class GlobalTurnStartedEvent
    {
        public int GlobalTurn { get; }

        public GlobalTurnStartedEvent(int globalTurn)
        {
            GlobalTurn = globalTurn;
        }
    }
}
