using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Definition of a character: everything that sets it apart from another
    /// (visuals, attributes and skills). Characters share the same prefab template
    /// and receive their config on spawn, so creating a new character means
    /// creating an asset, not a prefab.
    /// </summary>
    public abstract class CharacterConfig : ScriptableObject
    {
        [Header("Visual")]
        [SerializeField]
        [Tooltip("Initial sprite of the character (the Animator takes over from the first frame).")]
        private Sprite sprite;

        [SerializeField]
        [Tooltip("Character animations. Accepts an AnimatorController or AnimatorOverrideController.")]
        private RuntimeAnimatorController animatorController;

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

        [Header("Skills")]
        [SerializeField]
        private List<SkillDefinition> skills = new();

        public Sprite Sprite => sprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public int MaxHp => maxHp;
        public int WalkDistance => walkDistance;
        public int AttackDistance => attackDistance;
        public int BasicAttackDamage => basicAttackDamage;
        public IReadOnlyList<SkillDefinition> Skills => skills;
    }
}
