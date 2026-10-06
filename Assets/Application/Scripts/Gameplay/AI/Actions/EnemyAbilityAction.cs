using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Base for the special abilities of enemies (applying a state, healing,
    /// buffing, summoning): they share a cooldown kept in the enemy's
    /// <see cref="EnemyMemory"/> and are blocked while the enemy is prevented from
    /// using skills. The asset itself stays stateless.
    /// </summary>
    public abstract class EnemyAbilityAction : EnemyAction
    {
        [Header("Cooldown")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Turns of the enemy between uses (same rule as skill cooldowns): used on turn N, usable again on turn N + cooldown + 1 (0 = every turn).")]
        private int cooldownTurns = 2;

        public int CooldownTurns => cooldownTurns;

        /// <summary>Whether the enemy may use the ability now (not restricted, cooldown over).</summary>
        protected bool IsAvailable(in EnemyTurnContext context)
        {
            if (context.Self.IsDead) return false;
            if (context.Self.States.IsRestricted(EBehaviorRestriction.PreventSkills)) return false;
            return context.Memory.IsReady(EnemyMemory.Key(this));
        }

        /// <summary>Starts this ability's cooldown on the enemy that used it.</summary>
        protected void StartCooldown(in EnemyTurnContext context)
        {
            context.Memory.StartCooldown(EnemyMemory.Key(this), cooldownTurns);
        }
    }
}
