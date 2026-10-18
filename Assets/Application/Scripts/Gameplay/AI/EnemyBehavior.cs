using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// AI for an enemy type: a list of actions in priority order. A turn has a walk and an action, like the player's:
    /// the list is read again after each success, the first walk that works (<see cref="EnemyAction.IsMovement"/>)
    /// uses the walk of the turn and the first other action that works uses its action, so a melee enemy chases and
    /// then hits, and a ranged one shoots and then keeps its distance (the order of the list decides which comes
    /// first). The same behavior can be shared by several EnemyConfigs.
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
        /// (the enemy skips its turn). <paramref name="walkUsed"/> and <paramref name="actionUsed"/> say what the
        /// turn already spent (a telegraphed skill that fired or is still being prepared uses the action, and
        /// waiting in place uses the walk too).
        /// </summary>
        /// <remarks>
        /// One walk and one action: passes over the list, in order, skipping the kind already used, until both are
        /// used or a pass does nothing (a walk can bring the target into reach of an action earlier in the list).
        /// Preparing a telegraphed skill that keeps the enemy in place also ends the walk: it may walk before
        /// preparing, never after.
        /// </remarks>
        public bool TakeTurn(in EnemyTurnContext context, bool walkUsed = false, bool actionUsed = false)
        {
            var moved = walkUsed;
            var acted = actionUsed;
            var any = false;
            for (var pass = 0; pass < 2 && !(moved && acted); pass++)
            {
                var progress = false;
                foreach (var action in actions)
                {
                    if (action == null || context.Self.IsDead) continue;
                    if (action.IsMovement ? moved : acted) continue;
                    if (!action.TryExecute(context)) continue;

                    if (action.IsMovement)
                        moved = true;
                    else
                        acted = true;
                    if (context.Self.Telegraph is { Skill: { CanMoveWhileWindingUp: false } })
                        moved = true;
                    progress = true;
                    any = true;
                    if (moved && acted) break;
                }

                if (!progress) break;
            }

            return any;
        }
    }
}
