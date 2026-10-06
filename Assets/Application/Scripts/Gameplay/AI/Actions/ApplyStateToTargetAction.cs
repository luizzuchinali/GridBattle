using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Applies a state (usually a debuff such as Weakened) to the player when it is
    /// within range and the ability is off cooldown (controller role). Plays the
    /// attack animation toward the target, starts the cooldown and consumes the
    /// turn.
    /// </summary>
    [CreateAssetMenu(fileName = "ApplyStateToTarget", menuName = "GridBattle/AI/Actions/Apply State To Target", order = 4)]
    public class ApplyStateToTargetAction : EnemyAbilityAction
    {
        [Header("Effect")]
        [SerializeField]
        [Tooltip("State applied to the target, with its duration (in turns of the target) and stacks.")]
        private StateGrant grant;

        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum distance to the target (attack range metric of CombatSettings).")]
        private int range = 3;

        [SerializeField]
        [Tooltip("Does nothing (the turn is not consumed) while the target already has the state, so the enemy does something else instead of wasting the ability.")]
        private bool skipIfTargetHasState = true;

        public StateGrant Grant => grant;
        public int Range => range;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            if (!grant.IsValid) return false;
            if (!IsAvailable(context)) return false;

            var target = context.Target;
            if (target.IsDead) return false;
            if (!GridRules.IsInAttackRange(context.Self.CurrentGridPos, target.CurrentGridPos, range)) return false;
            if (skipIfTargetHasState && target.States.Has(grant.State)) return false;

            context.Grid.PlayAttackAnimation(context.Self, target.CurrentGridPos);
            target.States.Apply(grant);
            StartCooldown(context);
            return true;
        }
    }
}
