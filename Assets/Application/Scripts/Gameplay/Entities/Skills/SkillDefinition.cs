using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>Broad category of a skill (GDD 6): what it is mostly meant to do.</summary>
    public enum ESkillType
    {
        /// <summary>Deals damage.</summary>
        Offensive,

        /// <summary>Shield, healing or buffs.</summary>
        Defensive,

        /// <summary>Teleport, swap and other non-combat effects.</summary>
        Utility
    }

    /// <summary>
    /// Geometric shape of a skill's area of effect. Semantics (direction, size) are
    /// documented on <see cref="SkillArea"/>.
    /// </summary>
    public enum ESkillAreaShape
    {
        /// <summary>Round blob around the target (size 3 = 3x3 square).</summary>
        Circle,

        /// <summary>The target plus an arm along each axis.</summary>
        Cross,

        /// <summary>A line starting at the target and going away from the caster.</summary>
        Linear,

        /// <summary>A triangle widening away from the caster, starting at the target.</summary>
        Cone,

        /// <summary>A "V" around the target that bends back toward the caster.</summary>
        Arc,

        /// <summary>A line through the target, crossing the caster-to-target direction.</summary>
        Perpendicular
    }

    /// <summary>Which characters inside the area a skill's damage and effects reach.</summary>
    public enum ESkillTargetFilter
    {
        /// <summary>Characters of the opposite side (player vs enemies).</summary>
        Enemies,

        /// <summary>Characters of the caster's side, except the caster.</summary>
        Allies,

        /// <summary>Characters of the caster's side, including the caster.</summary>
        AlliesAndSelf,

        /// <summary>Only the caster (when it is inside the area).</summary>
        Self,

        /// <summary>Everybody inside the area, friend or foe.</summary>
        Everyone
    }

    /// <summary>
    /// An active skill (GDD Mechanic 7 / section 6): a reusable action with a
    /// cooldown, a range to pick the target cell and an area of effect around it.
    /// Fully data-driven: damage, area, range, cooldown and who is affected are
    /// Inspector fields, and non-damage results (states, healing, teleport) are
    /// composed from <see cref="SkillEffect"/>s. Players and enemies share the same
    /// path (<see cref="Character.TryUseSkill"/>). Special skills can subclass and
    /// override <see cref="CanUse"/> / <see cref="Execute"/>. Assets are shared and
    /// never hold runtime state (cooldowns live in <see cref="SkillCooldowns"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Skill", menuName = "GridBattle/Skills/Skill Definition", order = 0)]
    public class SkillDefinition : DisplayableDefinition
    {
        public const int MinAreaSize = 1;
        public const int MaxAreaSize = 9;

        [Header("Classification")]
        [SerializeField]
        private ESkillType type = ESkillType.Offensive;

        [Header("Damage")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Fixed damage dealt to every affected character, before the caster's damage scaling, " +
                 "skill damage bonus, critical and the target's defense.")]
        private int damage;

        [Header("Area of effect")]
        [SerializeField]
        private ESkillAreaShape areaShape = ESkillAreaShape.Circle;

        [SerializeField]
        [Range(MinAreaSize, MaxAreaSize)]
        [Tooltip("Size of the area (odd number, 1 to 9). 1 = only the target cell.")]
        private int areaSize = 1;

        [Header("Targeting")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum distance (skill range metric, Manhattan by default) from the caster to the target cell, " +
                 "plus the caster's Skill Range attribute. 0 = self-centered (the target is the caster's own cell).")]
        private int range = 1;

        [SerializeField]
        [Tooltip("The target cell can be anywhere on the grid (ignores Range).")]
        private bool unlimitedRange;

        [SerializeField]
        private ESkillTargetFilter affects = ESkillTargetFilter.Enemies;

        [SerializeField]
        [Tooltip("The skill can only be used if at least one character would be affected " +
                 "(an invalid action does not consume the turn). Turn off for skills that work on empty cells " +
                 "(e.g. a teleport).")]
        private bool requiresAffectedTarget = true;

        [Header("Cooldown")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Player actions between uses (enemy turns for enemies). 0 = no cooldown.")]
        private int cooldown = 1;

        [Header("Effects")]
        [SerializeReference]
        [SubclassPicker]
        [Tooltip("Non-damage results: states, healing, teleport, push and pull. Applied after the damage. " +
                 "Talents can attach more (see SkillModifierEffect).")]
        private List<SkillEffect> effects = new();

        public ESkillType Type => type;
        public int Damage => damage;
        public ESkillAreaShape AreaShape => areaShape;
        public int AreaSize => areaSize;
        public int Range => range;
        public bool UnlimitedRange => unlimitedRange;
        public ESkillTargetFilter Affects => affects;
        public bool RequiresAffectedTarget => requiresAffectedTarget;
        public int Cooldown => cooldown;
        public IReadOnlyList<SkillEffect> Effects => effects;

        /// <summary>True for skills centered on the caster (range 0, not unlimited).</summary>
        public bool IsSelfCentered => !unlimitedRange && range == 0;

        /// <summary>
        /// Whether the caster can use this skill on the target cell right now:
        /// alive, not silenced, cooldown ready, target in range and inside the grid,
        /// and (when required) at least one character affected. See
        /// <see cref="SkillTargeting.CanUse"/>.
        /// </summary>
        public virtual bool CanUse(Character caster, GridController grid, Vector2Int targetPos)
        {
            return SkillTargeting.CanUse(grid, caster, this, targetPos);
        }

        /// <summary>
        /// Executes the skill: attack animation (offensive skills), damage to every
        /// affected character, then the effects. Returning true means the action was
        /// consumed (the caller triggers the cooldown). Raises
        /// <see cref="SkillUsedEvent"/>. Game state changes immediately; animations
        /// only follow.
        /// </summary>
        public virtual bool Execute(Character caster, GridController grid, Vector2Int targetPos)
        {
            // The numbers (damage, area, effects) are the caster's effective ones: authored + talent modifiers.
            var context = SkillTargeting.CreateContext(grid, caster, EffectiveSkill.Resolve(caster, this), targetPos);

            // Before the damage, so the target's hit reaction lands on the impact.
            if (type == ESkillType.Offensive && targetPos != caster.CurrentGridPos)
                grid.PlayAttackAnimation(caster, targetPos);

            DealDamage(context);

            foreach (var effect in context.Effective.Effects)
            {
                if (effect != null)
                    effect.Apply(context);
            }

            EventBus.Raise(new SkillUsedEvent(caster, this, targetPos, context.AreaCells, context.Affected));
            return true;
        }

        /// <summary>
        /// Damage dealt to one affected character before the damage formula: the authored damage with the caster's
        /// talent modifiers (<see cref="EffectiveSkill"/>) and its spawn scaling.
        /// </summary>
        public int GetDamageFor(Character caster) => EffectiveSkill.Resolve(caster, this).Damage;

        private void DealDamage(in SkillContext context)
        {
            if (damage <= 0) return;

            var amount = context.Effective.Damage;
            foreach (var target in context.Affected)
                CombatResolver.DealDamage(context.Caster, target, amount, EDamageKind.Skill);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            areaSize = Mathf.Clamp(areaSize, MinAreaSize, MaxAreaSize);
            if (areaSize % 2 == 0)
                areaSize++;
        }
#endif
    }
}
