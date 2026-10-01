namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by TurnManager whenever the turn passes to the player or to the
    /// enemies. The player can only act (and only sees cell highlights) on its turn.
    /// </summary>
    public class TurnChangedEvent
    {
        public ETurnOwner Owner { get; }

        public bool IsPlayerTurn => Owner == ETurnOwner.Player;

        public TurnChangedEvent(ETurnOwner owner)
        {
            Owner = owner;
        }
    }
}
