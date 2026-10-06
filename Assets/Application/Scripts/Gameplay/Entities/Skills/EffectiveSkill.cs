using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// What the active states of a caster add up to for one skill (see <see cref="EffectiveSkill"/>): the sums that
    /// <c>SkillModifierEffect</c>s write through <see cref="States.StateEffect.CollectSkillModifiers"/>. Plain data.
    /// </summary>
    public struct SkillModifiers
    {
        /// <summary>Flat damage added to the skill's base damage.</summary>
        public float DamageFlat;

        /// <summary>Fraction added to the skill's damage (0.25 = +25%).</summary>
        public float DamagePercent;

        /// <summary>Steps the area grows (each step is +2 to the odd area size: one more ring around the center).</summary>
        public int AreaSteps;

        /// <summary>Added to the skill's range (self-centered and unlimited skills ignore it).</summary>
        public int Range;

        /// <summary>Subtracted from the skill's cooldown.</summary>
        public int CooldownReduction;

        /// <summary>Added to the distance of every push or pull of the skill.</summary>
        public int Displacement;

        /// <summary>Turns added to every temporary state the skill applies.</summary>
        public int StateDuration;

        /// <summary>Effects appended to the skill's own when it is used (null when there are none).</summary>
        public List<SkillEffect> Attached;

        /// <summary>Whether any modifier applies (the effective skill then equals the authored one).</summary>
        public readonly bool IsEmpty =>
            DamageFlat == 0f && DamagePercent == 0f && AreaSteps == 0 && Range == 0 && CooldownReduction == 0 &&
            Displacement == 0 && StateDuration == 0 && (Attached == null || Attached.Count == 0);
    }

    /// <summary>
    /// The numbers of a skill for one caster right now: the authored <see cref="SkillDefinition"/> plus everything
    /// the caster's states change about it (<c>SkillModifierEffect</c>: damage, area, range, cooldown, push and
    /// pull distance, state durations, attached effects). This is the single place that answers "how much damage,
    /// how big, how far, how long a cooldown, which effects"; targeting, execution, cooldowns, highlights, the enemy
    /// AI and the simulation bot all read it, so a modified skill behaves the same everywhere. Without modifiers
    /// every number is exactly the authored one. Resolve it once per decision and pass it down: it is a cheap value.
    /// </summary>
    public readonly struct EffectiveSkill
    {
        private readonly float _scaling;

        public EffectiveSkill(SkillDefinition skill, in SkillModifiers modifiers, float damageScaling)
        {
            Skill = skill;
            _scaling = damageScaling;

            var damage = skill.Damage > 0 ? (float)skill.Damage : 0f;
            if (damage > 0f && (modifiers.DamageFlat != 0f || modifiers.DamagePercent != 0f))
                damage = Mathf.Max(0f, (damage + modifiers.DamageFlat) * (1f + modifiers.DamagePercent));
            BaseDamage = damage;

            var area = Mathf.Clamp(skill.AreaSize + 2 * modifiers.AreaSteps, SkillDefinition.MinAreaSize,
                SkillDefinition.MaxAreaSize);
            AreaSize = area % 2 == 0 ? area - 1 : area;

            // Self-centered skills (range 0) and unlimited ones keep their kind: a range bonus makes no sense there.
            Range = skill.IsSelfCentered || skill.UnlimitedRange ? skill.Range : Mathf.Max(0, skill.Range + modifiers.Range);
            Cooldown = skill.Cooldown <= 0 ? 0 : skill.Cooldown - modifiers.CooldownReduction;
            DisplacementBonus = modifiers.Displacement;
            StateDurationBonus = modifiers.StateDuration;
            HasModifiers = !modifiers.IsEmpty;

            if (modifiers.Attached == null || modifiers.Attached.Count == 0)
            {
                Effects = skill.Effects;
            }
            else
            {
                var all = new List<SkillEffect>(skill.Effects.Count + modifiers.Attached.Count);
                all.AddRange(skill.Effects);
                all.AddRange(modifiers.Attached);
                Effects = all;
            }
        }

        /// <summary>The authored skill.</summary>
        public SkillDefinition Skill { get; }

        /// <summary>True when at least one state changes the skill.</summary>
        public bool HasModifiers { get; }

        /// <summary>Damage per affected character before the caster's scaling (authored damage with the modifiers; 0 for skills without damage).</summary>
        public float BaseDamage { get; }

        /// <summary>
        /// Damage dealt to each affected character, rounded: <see cref="BaseDamage"/> times the caster's damage
        /// scaling (the same value <see cref="SkillDefinition.GetDamageFor"/> returns).
        /// </summary>
        public int Damage => CombatResolver.Settings.RoundValue(BaseDamage * _scaling);

        /// <summary>Area size (odd, 1 to <see cref="SkillDefinition.MaxAreaSize"/>) with the modifiers.</summary>
        public int AreaSize { get; }

        /// <summary>Range of the skill with the modifiers, before the caster's Skill Range attribute.</summary>
        public int Range { get; }

        /// <summary>Cooldown of the skill with the modifiers, before the owner's Cooldown Reduction attribute and the minimum.</summary>
        public int Cooldown { get; }

        /// <summary>Cells added to every push or pull of the skill.</summary>
        public int DisplacementBonus { get; }

        /// <summary>Turns added to every temporary state the skill applies.</summary>
        public int StateDurationBonus { get; }

        /// <summary>The skill's own effects followed by the ones the modifiers attach.</summary>
        public IReadOnlyList<SkillEffect> Effects { get; }

        /// <summary>The authored shape (modifiers never change it).</summary>
        public ESkillAreaShape AreaShape => Skill.AreaShape;

        public bool UnlimitedRange => Skill.UnlimitedRange;

        /// <summary>Whether the skill is centered on the caster (authored range 0, not unlimited).</summary>
        public bool IsSelfCentered => Skill.IsSelfCentered;

        /// <summary>The skill's push or pull effect (the first one), or null.</summary>
        public DisplaceSkillEffect FindDisplacement()
        {
            foreach (var effect in Effects)
            {
                if (effect is DisplaceSkillEffect displace)
                    return displace;
            }

            return null;
        }

        /// <summary>Cells a push or pull of this skill moves its targets (the authored distance plus the bonus, at least 1).</summary>
        public int GetDisplacementDistance(DisplaceSkillEffect effect) =>
            Mathf.Max(1, effect.Distance + DisplacementBonus);

        /// <summary>
        /// The effective numbers of <paramref name="skill"/> for <paramref name="caster"/>: the authored skill with
        /// the modifiers of the caster's states. A null caster has no modifiers and no damage scaling.
        /// </summary>
        public static EffectiveSkill Resolve(Character caster, SkillDefinition skill)
        {
            var modifiers = new SkillModifiers();
            if (caster != null && skill != null)
                caster.States.CollectSkillModifiers(skill, ref modifiers);

            return new EffectiveSkill(skill, modifiers, caster != null ? caster.Scaling.DamageMultiplier : 1f);
        }
    }
}
