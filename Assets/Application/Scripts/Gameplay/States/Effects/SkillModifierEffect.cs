using System;
using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.States.Effects
{
    /// <summary>
    /// Changes the skills of the holder while the state is active (class talents: "Fireball +3 damage", "Shield
    /// Charge cooldown -1", "Gust pushes 1 cell farther", "Poisoned Dagger poisons"). Every value is per stack, so
    /// a talent with ranks grants one stack per rank. Several modifiers add up. The numbers are never read from
    /// here: <see cref="EffectiveSkill"/> resolves them for the skill and the caster, and everything that targets,
    /// executes, highlights, predicts or schedules a skill goes through it.
    /// <para>
    /// A modifier applies to the listed skills, or to every skill of the holder when the list is empty. Damage
    /// modifiers only affect skills that deal damage (a utility skill never becomes an attack); the range of
    /// self-centered and unlimited skills stays as it is; the cooldown never goes below the minimum of
    /// <see cref="SkillSettings"/>.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class SkillModifierEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("Skills the modifier applies to. Empty = every skill of the holder.")]
        private List<SkillDefinition> skills = new();

        [Header("Per stack")]
        [SerializeField]
        [Tooltip("Flat damage added to the skill's base damage (before the caster's scaling and the damage formula).")]
        private int damageFlat;

        [SerializeField]
        [Tooltip("Fraction added to the skill's damage (0.25 = +25%).")]
        private float damagePercent;

        [SerializeField]
        [Tooltip("Area growth: each step adds 2 to the odd area size (one more ring around the center; a line gets 2 cells longer). Capped at the maximum area size.")]
        private int areaSteps;

        [SerializeField]
        [Tooltip("Added to the skill's range (ignored by self-centered and unlimited skills).")]
        private int range;

        [SerializeField]
        [Tooltip("Subtracted from the skill's cooldown (never below the minimum cooldown of the Skill Settings).")]
        private int cooldownReduction;

        [SerializeField]
        [Tooltip("Cells added to every push or pull of the skill (also to the ones attached below).")]
        private int displacement;

        [SerializeField]
        [Tooltip("Turns added to every temporary state the skill applies (permanent states are not affected).")]
        private int stateDuration;

        [Header("Attached effects")]
        [SerializeReference]
        [SubclassPicker]
        [Tooltip("Effects appended to the skill's own when it is used (e.g. apply a state to the targets, push, heal).")]
        private List<SkillEffect> attachedEffects = new();

        [SerializeField]
        [Tooltip("Appends the attached effects once for every stack instead of once (rank 2 = twice; with a state that adds stacks, rank 2 = two stacks).")]
        private bool repeatAttachedPerStack;

        public IReadOnlyList<SkillDefinition> Skills => skills;
        public int DamageFlat => damageFlat;
        public float DamagePercent => damagePercent;
        public int AreaSteps => areaSteps;
        public int Range => range;
        public int CooldownReduction => cooldownReduction;
        public int Displacement => displacement;
        public int StateDuration => stateDuration;
        public IReadOnlyList<SkillEffect> AttachedEffects => attachedEffects;
        public bool RepeatAttachedPerStack => repeatAttachedPerStack;

        /// <summary>Whether the modifier applies to the skill (the list is empty or contains it).</summary>
        public bool AppliesTo(SkillDefinition skill)
        {
            return skill != null && (skills.Count == 0 || skills.Contains(skill));
        }

        public override void CollectSkillModifiers(Character holder, StateInstance state, SkillDefinition skill,
            ref SkillModifiers modifiers)
        {
            if (!AppliesTo(skill)) return;

            var stacks = Mathf.Max(1, state.Stacks);
            modifiers.DamageFlat += damageFlat * stacks;
            modifiers.DamagePercent += damagePercent * stacks;
            modifiers.AreaSteps += areaSteps * stacks;
            modifiers.Range += range * stacks;
            modifiers.CooldownReduction += cooldownReduction * stacks;
            modifiers.Displacement += displacement * stacks;
            modifiers.StateDuration += stateDuration * stacks;

            if (attachedEffects.Count == 0) return;

            modifiers.Attached ??= new List<SkillEffect>();
            var copies = repeatAttachedPerStack ? stacks : 1;
            for (var i = 0; i < copies; i++)
            {
                foreach (var effect in attachedEffects)
                {
                    if (effect != null)
                        modifiers.Attached.Add(effect);
                }
            }
        }
    }
}
