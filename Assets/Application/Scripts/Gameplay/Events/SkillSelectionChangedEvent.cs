using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the player's controller when the skill being aimed changes: a
    /// skill was selected (the grid shows its range) or the selection was cleared
    /// (cancel, skill used, turn change). The skill bar highlights the selected
    /// button from this.
    /// </summary>
    public class SkillSelectionChangedEvent
    {
        public Character Owner { get; }

        /// <summary>The selected skill, or null when the selection was cleared.</summary>
        public SkillDefinition Skill { get; }

        public bool HasSelection => Skill != null;

        public SkillSelectionChangedEvent(Character owner, SkillDefinition skill)
        {
            Owner = owner;
            Skill = skill;
        }
    }
}
