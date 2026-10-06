using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Combat
{
    /// <summary>
    /// Applies damage and healing between characters: the conditional bonuses of both sides' states
    /// (<see cref="Adjust"/>), the formula (<see cref="DamageCalculator"/>), shields, HP, then the reactions of
    /// both sides' states (life steal, thorns). Every hit raises <see cref="DamageDealtEvent"/>. Player and
    /// enemies go through the same path. <see cref="PredictDamage"/> answers "how much would this hit do" with the
    /// same rules and no random draw (AI, simulation bot).
    /// </summary>
    public static class CombatResolver
    {
        public static CombatSettings Settings => GameSettings.Get<CombatSettings>();

        public static HitResult BasicAttack(Character attacker, Character target) =>
            DealDamage(attacker, target, attacker.Stats.GetInt(EAttribute.BasicDamage), EDamageKind.BasicAttack);

        /// <summary>
        /// Deals <paramref name="baseDamage"/> of <paramref name="kind"/> to
        /// <paramref name="target"/>. Returns the outcome (zero damage if the target
        /// is already dead).
        /// </summary>
        public static HitResult DealDamage([CanBeNull] Character attacker, Character target, int baseDamage,
            EDamageKind kind)
        {
            var hit = new HitResult { Attacker = attacker, Target = target, Kind = kind };
            if (target == null || target.IsDead) return hit;

            var attackerStats = attacker != null ? attacker.Stats : null;
            var adjustment = Adjust(attacker, target, kind);
            hit.Damage = DamageCalculator.Compute(baseDamage, kind, attackerStats, target.Stats, Settings,
                GameRandom.Stream(ERandomStream.Combat), adjustment, out hit.IsCrit);

            target.ApplyHit(ref hit);
            EventBus.Raise(new DamageDealtEvent(hit));

            if (attacker != null)
                attacker.States.NotifyDamageDealt(hit);
            target.States.NotifyDamageReceived(hit);
            return hit;
        }

        /// <summary>
        /// The damage a hit of <paramref name="baseDamage"/> would do right now (no critical, no random draw,
        /// shields not counted): the same conditional bonuses and formula as <see cref="DealDamage"/>.
        /// </summary>
        public static int PredictDamage([CanBeNull] Character attacker, Character target, int baseDamage,
            EDamageKind kind)
        {
            return DamageCalculator.Compute(baseDamage, kind, attacker != null ? attacker.Stats : null, target.Stats,
                Settings, null, Adjust(attacker, target, kind), out _);
        }

        /// <summary>
        /// What the states of both sides add to a hit before its damage is computed (see
        /// <see cref="StateEffect.ModifyOutgoingDamage"/> and <see cref="StateEffect.ModifyIncomingDamage"/>):
        /// the attacker's outgoing bonuses and the target's incoming ones. Exact damage (<see cref="EDamageKind.Pure"/>)
        /// is never adjusted. Pure: no random draws.
        /// </summary>
        public static DamageAdjustment Adjust([CanBeNull] Character attacker, Character target, EDamageKind kind)
        {
            var adjustment = new DamageAdjustment();
            if (kind == EDamageKind.Pure || target == null) return adjustment;

            var context = new DamageContext(attacker, target, kind);
            if (attacker != null)
                attacker.States.CollectOutgoingDamage(context, ref adjustment);
            target.States.CollectIncomingDamage(context, ref adjustment);
            return adjustment;
        }

        /// <summary>Heals up to max HP. Returns the HP actually recovered.</summary>
        public static int Heal(Character target, int amount)
        {
            if (target == null || target.IsDead || amount <= 0) return 0;

            var healed = target.RestoreHp(amount);
            if (healed > 0)
                EventBus.Raise(new HealedEvent(target, healed));
            return healed;
        }

        /// <summary>Heals a fraction of max HP.</summary>
        public static int HealFraction(Character target, float fraction) =>
            target == null ? 0 : Heal(target, Mathf.RoundToInt(target.MaxHp * fraction));
    }
}
