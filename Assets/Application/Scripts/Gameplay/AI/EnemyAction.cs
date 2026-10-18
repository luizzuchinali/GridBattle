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
        /// The action is a walk (chase, keep distance): an enemy uses one walk and one other action per turn
        /// (<see cref="EnemyBehavior.TakeTurn"/>).
        /// </summary>
        public virtual bool IsMovement => false;

        /// <summary>
        /// Tries to execute the action. Returning true uses the walk or the action of the enemy's turn (see
        /// <see cref="IsMovement"/>).
        /// </summary>
        public abstract bool TryExecute(in EnemyTurnContext context);
    }
}
