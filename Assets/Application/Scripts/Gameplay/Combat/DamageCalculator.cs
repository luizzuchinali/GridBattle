using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Combat
{
    /// <summary>
    /// Damage formula, in one place and free of side effects:
    /// (base + conditional flat) × (1 + skill bonus, skills only) × (1 + damage dealt) × (1 + conditional bonus) →
    /// critical → defense (flat or percentage, minus penetration) × (1 + damage taken) × (1 + conditional
    /// reduction) → rounding → minimum damage. The conditional parts come from the states of both sides through a
    /// <see cref="DamageAdjustment"/> (see <see cref="CombatResolver"/>); without it the formula is the plain one.
    /// The critical roll uses <paramref name="rng"/> only when the chance is above zero, so the random stream is
    /// untouched by characters that cannot crit.
    /// </summary>
    public static class DamageCalculator
    {
        /// <param name="rng">
        /// Stream of the critical roll. Null never crits and draws nothing: used to predict a hit (AI, simulation
        /// bots) without touching the run's random streams.
        /// </param>
        public static int Compute(int baseDamage, EDamageKind kind, [CanBeNull] CharacterStats attacker,
            CharacterStats target, CombatSettings settings, [CanBeNull] Rng rng, out bool isCrit) =>
            Compute(baseDamage, kind, attacker, target, settings, rng, default, out isCrit);

        /// <summary>
        /// The formula with the conditional bonuses of the states (<paramref name="adjustment"/>, see
        /// <see cref="CombatResolver.Adjust"/>). A default adjustment gives exactly the plain result.
        /// </summary>
        public static int Compute(int baseDamage, EDamageKind kind, [CanBeNull] CharacterStats attacker,
            CharacterStats target, CombatSettings settings, [CanBeNull] Rng rng, in DamageAdjustment adjustment,
            out bool isCrit)
        {
            isCrit = false;
            if (baseDamage <= 0) return 0;
            if (kind == EDamageKind.Pure) return baseDamage;

            float damage = baseDamage;
            if (adjustment.OutgoingFlat != 0f)
                damage = Mathf.Max(0f, damage + adjustment.OutgoingFlat);

            if (attacker != null)
            {
                if (kind == EDamageKind.Skill)
                    damage *= 1f + attacker.Get(EAttribute.SkillDamageBonus);

                damage *= Mathf.Max(0f, 1f + attacker.Get(EAttribute.DamageDealt));
                if (adjustment.OutgoingPercent != 0f)
                    damage *= Mathf.Max(0f, 1f + adjustment.OutgoingPercent);

                if (CanCrit(kind))
                {
                    var chance = Mathf.Min(attacker.Get(EAttribute.CritChance), settings.CritChanceCap);
                    if (chance > 0f && rng != null && rng.Chance(chance))
                    {
                        isCrit = true;
                        damage *= Mathf.Max(1f, attacker.Get(EAttribute.CritMultiplier));
                    }
                }
            }

            if (!IgnoresDefense(kind, settings))
            {
                var penetration = attacker != null ? Mathf.Clamp01(attacker.Get(EAttribute.DefensePenetration)) : 0f;
                var defense = target.Get(EAttribute.Defense) * (1f - penetration);
                damage = settings.DefenseMode == EDefenseMode.Flat
                    ? damage - defense
                    : damage * (1f - Mathf.Min(defense / 100f, settings.MaxPercentReduction));
            }

            damage *= Mathf.Max(0f, 1f + target.Get(EAttribute.DamageTaken));
            if (adjustment.IncomingPercent != 0f)
                damage *= Mathf.Max(0f, 1f + adjustment.IncomingPercent);

            return Mathf.Max(settings.MinimumDamage, settings.RoundValue(damage));
        }

        private static bool CanCrit(EDamageKind kind) =>
            kind == EDamageKind.BasicAttack || kind == EDamageKind.Skill || kind == EDamageKind.Consumable;

        private static bool IgnoresDefense(EDamageKind kind, CombatSettings settings) => kind switch
        {
            EDamageKind.Periodic => settings.PeriodicIgnoresDefense,
            EDamageKind.Thorns => settings.ThornsIgnoreDefense,
            EDamageKind.Terrain => settings.TerrainIgnoresDefense,
            EDamageKind.Collision => settings.CollisionIgnoresDefense,
            _ => false,
        };
    }
}
