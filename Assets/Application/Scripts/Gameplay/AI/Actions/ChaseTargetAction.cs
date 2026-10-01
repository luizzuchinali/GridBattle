using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Moves one step toward the target (GridRules.FindStepToward). Does not consume
    /// the turn if no reachable cell brings the enemy closer to the target.
    /// </summary>
    [CreateAssetMenu(fileName = "ChaseTarget", menuName = "GridBattle/AI/Actions/Chase Target", order = 2)]
    public class ChaseTargetAction : EnemyAction
    {
        public override bool TryExecute(in EnemyTurnContext context)
        {
            var step = GridRules.FindStepToward(context.Grid, context.Self, context.Target.CurrentGridPos);
            if (step == null) return false;

            context.Grid.MoveEntity(context.Self, step.Value);
            return true;
        }
    }
}
