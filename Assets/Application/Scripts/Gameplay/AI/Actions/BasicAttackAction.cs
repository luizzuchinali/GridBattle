using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Basic attack on the target, if it is in range (GridRules.IsAttackTarget).
    /// </summary>
    [CreateAssetMenu(fileName = "BasicAttack", menuName = "GridBattle/AI/Actions/Basic Attack", order = 1)]
    public class BasicAttackAction : EnemyAction
    {
        public override bool TryExecute(in EnemyTurnContext context)
        {
            if (!GridRules.IsAttackTarget(context.Grid, context.Self, context.Target.CurrentGridPos))
                return false;

            if (context.Target is IDamageReceiver receiver)
            {
                context.Grid.PlayAttackAnimation(context.Self, context.Target.CurrentGridPos);
                context.Self.Attack(receiver);
            }
            return true;
        }
    }
}
