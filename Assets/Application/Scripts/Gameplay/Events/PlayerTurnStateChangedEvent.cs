namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the player's controller whenever what the player may still do this turn changes (a walk, an action,
    /// a consumable, an undone walk, a new turn).
    /// </summary>
    public class PlayerTurnStateChangedEvent
    {
        /// <summary>It is the player's turn and nothing ended it yet.</summary>
        public bool IsPlayerTurn { get; }

        /// <summary>The player can still walk this turn.</summary>
        public bool CanMove { get; }

        /// <summary>The player can still attack or use a skill this turn.</summary>
        public bool CanAct { get; }

        /// <summary>The player can still use a consumable this turn.</summary>
        public bool CanUseConsumable { get; }

        /// <summary>The walk of this turn can be taken back.</summary>
        public bool CanUndoMove { get; }

        /// <summary>The player already walked or acted this turn (the End turn button shows from then on).</summary>
        public bool Started { get; }

        public PlayerTurnStateChangedEvent(bool isPlayerTurn, bool canMove, bool canAct, bool canUseConsumable,
            bool canUndoMove, bool started)
        {
            IsPlayerTurn = isPlayerTurn;
            CanMove = canMove;
            CanAct = canAct;
            CanUseConsumable = canUseConsumable;
            CanUndoMove = canUndoMove;
            Started = started;
        }
    }
}
