using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// AI for an enemy type: a list of actions in priority order. On its turn, the
    /// first action that consumes the turn ends the enemy's turn. The same behavior
    /// can be shared by several EnemyConfigs.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBehavior", menuName = "GridBattle/AI/Enemy Behavior", order = 0)]
    public class EnemyBehavior : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Actions in priority order.")]
        private List<EnemyAction> actions = new();

        public IReadOnlyList<EnemyAction> Actions => actions;

        /// <summary>
        /// Runs the enemy's turn. Returns false if no action consumed the turn
        /// (the enemy skips its turn).
        /// </summary>
        public bool TakeTurn(in EnemyTurnContext context)
        {
            foreach (var action in actions)
            {
                if (action != null && action.TryExecute(context))
                    return true;
            }

            return false;
        }
    }
}
