using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Definition of a character: everything that sets it apart from another
    /// (visuals, attributes, skills and initial states). Characters share the
    /// same prefab template and receive their config on spawn, so creating a new
    /// character means creating an asset, not a prefab.
    /// </summary>
    public abstract class CharacterConfig : DisplayableDefinition
    {
        [Header("Visual")]
        [SerializeField]
        [Tooltip("Initial sprite of the character (the Animator takes over from the first frame).")]
        private Sprite sprite;

        [SerializeField]
        [Tooltip("Character animations. Accepts an AnimatorController or AnimatorOverrideController.")]
        private RuntimeAnimatorController animatorController;

        [SerializeField]
        [Tooltip("Sprite tint (white = original colors).")]
        private Color tint = Color.white;

        [Header("Attributes")]
        [SerializeField]
        [Min(1)]
        private int maxHp = 100;

        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum movement distance per turn (GridRules metric).")]
        private int walkDistance = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Maximum basic attack distance (GridRules metric).")]
        private int attackDistance = 1;

        [SerializeField]
        [Min(0)]
        private int basicAttackDamage = 10;

        [Header("Offense")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Chance (0..1) of a critical hit.")]
        private float critChance;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Damage multiplier of a critical hit.")]
        private float critMultiplier = 2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction (0..1) of the target's defense ignored.")]
        private float defensePenetration;

        [Header("Defense")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Reduces damage taken (flat or percentage, see CombatSettings).")]
        private int defense;

        [Header("Skill modifiers")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Extra damage (fraction) of offensive skills. 0.1 = +10%.")]
        private float skillDamageBonus;

        [SerializeField]
        [Min(0)]
        [Tooltip("Extra target selection range of skills.")]
        private int skillRange;

        [SerializeField]
        [Min(0)]
        [Tooltip("Player actions removed from skill cooldowns.")]
        private int cooldownReduction;

        [Header("Displacement")]
        [SerializeField]
        [Tooltip("Whether pushes and pulls can move this character. Off for heavy characters such as a boss: " +
                 "it stays where it is (it still blocks and is hurt by characters thrown against it).")]
        private bool canBeDisplaced = true;

        [Header("Resistances")]
        [SerializeField]
        [Tooltip("States this character ignores: applying them (by skills, terrain, AI or anything else) does nothing. " +
                 "For a boss that must not be stunned, for example.")]
        private List<StateDefinition> immuneStates = new();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Multiplier of the part of periodic damage that scales with this character's max HP (such as Venom's " +
                 "percentage per stack). 1 = no change; a boss uses less so that percentage poisons do not melt it.")]
        private float maxHpPercentDamageMultiplier = 1f;

        [Header("Skills")]
        [SerializeField]
        private List<SkillDefinition> skills = new();

        [Header("States")]
        [SerializeField]
        [Tooltip("States applied on spawn (e.g. a permanent Regeneration for a regenerating enemy).")]
        private List<StateGrant> initialStates = new();

        public Sprite Sprite => sprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public Color Tint => tint;
        public int MaxHp => maxHp;
        public int WalkDistance => walkDistance;
        public int AttackDistance => attackDistance;
        public int BasicAttackDamage => basicAttackDamage;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<StateGrant> InitialStates => initialStates;

        /// <summary>Whether pushes and pulls can move the character.</summary>
        public bool CanBeDisplaced => canBeDisplaced;

        /// <summary>States the character ignores.</summary>
        public IReadOnlyList<StateDefinition> ImmuneStates => immuneStates;

        /// <summary>Multiplier of the max-HP-percentage part of periodic damage taken (1 = unchanged).</summary>
        public float MaxHpPercentDamageMultiplier => maxHpPercentDamageMultiplier;

        /// <summary>Whether <paramref name="state"/> is in the character's immunity list.</summary>
        public bool IsImmuneTo(StateDefinition state)
        {
            if (state == null) return false;

            foreach (var immune in immuneStates)
            {
                if (immune == state)
                    return true;
            }

            return false;
        }

        /// <summary>Base value of an attribute, before states and spawn scaling.</summary>
        public float GetBaseAttribute(EAttribute attribute) => attribute switch
        {
            EAttribute.MaxHp => maxHp,
            EAttribute.WalkRange => walkDistance,
            EAttribute.AttackRange => attackDistance,
            EAttribute.BasicDamage => basicAttackDamage,
            EAttribute.CritChance => critChance,
            EAttribute.CritMultiplier => critMultiplier,
            EAttribute.DefensePenetration => defensePenetration,
            EAttribute.Defense => defense,
            EAttribute.SkillDamageBonus => skillDamageBonus,
            EAttribute.SkillRange => skillRange,
            EAttribute.CooldownReduction => cooldownReduction,
            _ => 0f,
        };
    }
}
