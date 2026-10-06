using GridBattle.Data;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// An action an enemy can attempt on its turn. Actions are reusable assets
    /// shared across behaviors and must NOT hold runtime state: the same asset is
    /// shared by every enemy that uses it. Per-enemy state (cooldowns, counters)
    /// lives in the enemy's <see cref="EnemyMemory"/>, keyed by this action's
    /// stable <see cref="GameDefinition.Id"/>.
    /// </summary>
    public abstract class EnemyAction : GameDefinition
    {
        /// <summary>
        /// Tries to execute the action. Returning true consumes the enemy's turn.
        /// </summary>
        public abstract bool TryExecute(in EnemyTurnContext context);
    }
}
