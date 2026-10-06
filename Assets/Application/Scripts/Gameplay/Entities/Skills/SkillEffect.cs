using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Everything a <see cref="SkillEffect"/> needs about one skill use: who cast
    /// it, where it was aimed, the cells of the area (inside the grid) and the
    /// characters the skill's target filter lets it reach. Resolved before any
    /// damage, so deaths during the use do not change it.
    /// </summary>
    public readonly struct SkillContext
    {
        public Character Caster { get; }
        public GridController Grid { get; }
        public SkillDefinition Skill { get; }

        /// <summary>The skill as the caster uses it now: authored numbers plus the modifiers of its states.</summary>
        public EffectiveSkill Effective { get; }

        public Vector2Int TargetPos { get; }

        /// <summary>Cells of the area of effect that are inside the grid, sorted by row then column.</summary>
        public IReadOnlyList<Vector2Int> AreaCells { get; }

        /// <summary>Living characters inside the area that pass the skill's target filter, in cell order.</summary>
        public IReadOnlyList<Character> Affected { get; }

        public SkillContext(Character caster, GridController grid, SkillDefinition skill, in EffectiveSkill effective,
            Vector2Int targetPos, IReadOnlyList<Vector2Int> areaCells, IReadOnlyList<Character> affected)
        {
            Caster = caster;
            Grid = grid;
            Skill = skill;
            Effective = effective;
            TargetPos = targetPos;
            AreaCells = areaCells;
            Affected = affected;
        }
    }

    /// <summary>Who receives the result of a <see cref="SkillEffect"/>.</summary>
    public enum ESkillEffectRecipient
    {
        /// <summary>Every character the skill affects (area and target filter).</summary>
        AffectedTargets,

        /// <summary>The character that cast the skill.</summary>
        Caster
    }

    /// <summary>
    /// A non-damage result of a skill. Effects are plain serializable classes
    /// picked in the Inspector ([SerializeReference]) and shared by every use of
    /// the skill, so they must not hold runtime state.
    /// </summary>
    [Serializable]
    public abstract class SkillEffect
    {
        /// <summary>
        /// Whether the effect allows the skill to be aimed at the target cell (e.g.
        /// a teleport needs a free cell). Checked together with the skill's own
        /// rules; default: always.
        /// </summary>
        public virtual bool CanUse(in SkillContext context) => true;

        /// <summary>Applies the effect. Called after the skill's damage.</summary>
        public abstract void Apply(in SkillContext context);

        /// <summary>Collects the effect's recipients.</summary>
        protected static IEnumerable<Character> GetRecipients(in SkillContext context, ESkillEffectRecipient recipient)
        {
            if (recipient == ESkillEffectRecipient.Caster)
                return new[] { context.Caster };

            return context.Affected;
        }
    }

    /// <summary>Applies states (buffs/debuffs) to the affected targets or to the caster.</summary>
    [Serializable]
    public sealed class ApplyStatesSkillEffect : SkillEffect
    {
        [SerializeField]
        private ESkillEffectRecipient recipient = ESkillEffectRecipient.AffectedTargets;

        [SerializeField]
        private List<StateGrant> states = new();

        public ESkillEffectRecipient Recipient => recipient;
        public IReadOnlyList<StateGrant> States => states;

        public override void Apply(in SkillContext context)
        {
            // Talents can lengthen the temporary states a skill applies (EffectiveSkill.StateDurationBonus).
            var bonus = context.Effective.StateDurationBonus;
            foreach (var character in GetRecipients(context, recipient))
            {
                if (character == null || character.IsDead) continue;

                foreach (var grant in states)
                {
                    if (!grant.IsValid) continue;

                    var duration = grant.IsPermanent ? grant.Duration : Mathf.Max(1, grant.Duration + bonus);
                    character.States.Apply(grant.State, duration, grant.Stacks);
                }
            }
        }
    }

    /// <summary>Heals the affected targets or the caster: a flat amount and/or a fraction of max HP.</summary>
    [Serializable]
    public sealed class HealSkillEffect : SkillEffect
    {
        [SerializeField]
        private ESkillEffectRecipient recipient = ESkillEffectRecipient.AffectedTargets;

        [SerializeField]
        [Min(0)]
        private int flatHeal;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the recipient's max HP added to the healing.")]
        private float maxHpFraction;

        public ESkillEffectRecipient Recipient => recipient;
        public int FlatHeal => flatHeal;
        public float MaxHpFraction => maxHpFraction;

        public override void Apply(in SkillContext context)
        {
            foreach (var character in GetRecipients(context, recipient))
            {
                if (character == null || character.IsDead) continue;

                var amount = flatHeal + CombatResolver.Settings.RoundValue(maxHpFraction * character.MaxHp);
                CombatResolver.Heal(character, amount);
            }
        }
    }

    /// <summary>
    /// The caster moves to the target cell, which must be free, walkable (not blocked
    /// by terrain) and different from the caster's own cell. Uses the regular grid
    /// movement.
    /// </summary>
    [Serializable]
    public sealed class TeleportSkillEffect : SkillEffect
    {
        public override bool CanUse(in SkillContext context)
        {
            var grid = context.Grid;
            var target = context.TargetPos;
            return grid.IsValidPosition(target)
                   && grid.IsWalkable(target)
                   && target != context.Caster.CurrentGridPos
                   && grid.IsFreePosition(target);
        }

        public override void Apply(in SkillContext context)
        {
            if (context.Caster.IsDead || !CanUse(context)) return;

            context.Grid.MoveEntity(context.Caster, context.TargetPos);
        }
    }
}
