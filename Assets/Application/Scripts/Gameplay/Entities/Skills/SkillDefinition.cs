using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Base class for skills. Each concrete skill is a subclass (with its own
    /// [CreateAssetMenu]) referenced in the CharacterConfig skill list.
    /// </summary>
    public abstract class SkillDefinition : ScriptableObject
    {
        [SerializeField]
        private string skillName;

        [SerializeField]
        [TextArea]
        private string description;

        public string SkillName => skillName;
        public string Description => description;

        /// <summary>
        /// Whether the caster can use this skill on the target, given the grid
        /// rules.
        /// </summary>
        public abstract bool CanUse(Character caster, GridController grid, Vector2Int targetPos);

        /// <summary>
        /// Executes the skill effect. Returning true means the turn was
        /// consumed.
        /// </summary>
        public abstract bool Execute(Character caster, GridController grid, Vector2Int targetPos);
    }
}
