using UnityEngine;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// An action an enemy can attempt on its turn. Actions are reusable assets
    /// shared across behaviors and must NOT hold runtime state: the same asset is
    /// shared by every enemy that uses it.
    /// </summary>
    public abstract class EnemyAction : ScriptableObject
    {
        /// <summary>
        /// Tries to execute the action. Returning true consumes the enemy's turn.
        /// </summary>
        public abstract bool TryExecute(in EnemyTurnContext context);
    }
}
