using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Heals the most injured ally within range whose HP fraction is below the
    /// threshold (support role). The heal is a flat amount plus a fraction of the
    /// ally's max HP. Starts the cooldown and consumes the turn.
    /// </summary>
    [CreateAssetMenu(fileName = "HealAlly", menuName = "GridBattle/AI/Actions/Heal Ally", order = 5)]
    public class HealAllyAction : EnemyAbilityAction
    {
        private const float Epsilon = 0.0001f;

        [Header("Targeting")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum distance to the ally (attack range metric of CombatSettings).")]
        private int range = 3;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Only allies whose HP fraction is below this value are healed (1 = any injured ally).")]
        private float hpThreshold = 0.75f;

        [SerializeField]
        [Tooltip("Whether the healer can also heal itself.")]
        private bool includeSelf;

        [Header("Heal")]
        [SerializeField]
        [Min(0)]
        private int flatHeal = 6;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the ally's max HP added to the heal.")]
        private float maxHpFraction;

        public int Range => range;
        public float HpThreshold => hpThreshold;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            if (!IsAvailable(context)) return false;

            var self = context.Self;
            Enemy best = null;
            var bestFraction = float.MaxValue;
            foreach (var ally in context.GetAllies(includeSelf))
            {
                if (!GridRules.IsInAttackRange(self.CurrentGridPos, ally.CurrentGridPos, range)) continue;

                var fraction = (float)ally.Current / ally.MaxHp;
                if (fraction >= hpThreshold) continue;

                // Allies come ordered by row, then column: only a strictly lower
                // fraction replaces the current choice.
                if (best != null && fraction >= bestFraction - Epsilon) continue;

                best = ally;
                bestFraction = fraction;
            }

            if (best == null) return false;

            var amount = flatHeal + Mathf.RoundToInt(maxHpFraction * best.MaxHp);
            if (CombatResolver.Heal(best, amount) <= 0) return false;

            StartCooldown(context);
            return true;
        }
    }
}
