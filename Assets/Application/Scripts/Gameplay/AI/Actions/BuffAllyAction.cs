using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Applies a state (usually a buff such as Shield) to an ally within range that
    /// does not have it yet (support role). Among the candidates it picks the one
    /// closest to the target, since it is the one that will fight first. Starts the
    /// cooldown and consumes the turn.
    /// </summary>
    [CreateAssetMenu(fileName = "BuffAlly", menuName = "GridBattle/AI/Actions/Buff Ally", order = 6)]
    public class BuffAllyAction : EnemyAbilityAction
    {
        private const float Epsilon = 0.0001f;

        [Header("Effect")]
        [SerializeField]
        [Tooltip("State applied to the ally, with its duration (in turns of the ally) and stacks.")]
        private StateGrant grant;

        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum distance to the ally (attack range metric of CombatSettings).")]
        private int range = 3;

        [SerializeField]
        [Tooltip("Whether the buffer can also buff itself.")]
        private bool includeSelf;

        public StateGrant Grant => grant;
        public int Range => range;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            if (!grant.IsValid) return false;
            if (!IsAvailable(context)) return false;

            var self = context.Self;
            var targetPos = context.Target.CurrentGridPos;
            var metric = CombatResolver.Settings.AttackMetric;

            Enemy best = null;
            var bestDistance = float.MaxValue;
            foreach (var ally in context.GetAllies(includeSelf))
            {
                if (!GridRules.IsInAttackRange(self.CurrentGridPos, ally.CurrentGridPos, range)) continue;
                if (ally.States.Has(grant.State)) continue;

                // Allies come ordered by row, then column: only a strictly closer
                // ally replaces the current choice.
                var distance = GridDistance.Measure(metric, ally.CurrentGridPos, targetPos);
                if (best != null && distance >= bestDistance - Epsilon) continue;

                best = ally;
                bestDistance = distance;
            }

            if (best == null) return false;

            best.States.Apply(grant);
            StartCooldown(context);
            return true;
        }
    }
}
