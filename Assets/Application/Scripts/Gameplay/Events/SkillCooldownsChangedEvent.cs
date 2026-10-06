using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised whenever a character's skill cooldowns change (a skill was used, a
    /// turn ended, cooldowns were reset or restored). The skill bar re-reads
    /// <c>Owner.Cooldowns</c> from this.
    /// </summary>
    public class SkillCooldownsChangedEvent
    {
        public Character Owner { get; }

        public SkillCooldownsChangedEvent(Character owner)
        {
            Owner = owner;
        }
    }
}
