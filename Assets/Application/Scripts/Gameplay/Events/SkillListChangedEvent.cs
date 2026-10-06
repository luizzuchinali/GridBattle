using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a character's runtime skill list is replaced (a new player
    /// character was initialized with its class skills, or the run unlocked
    /// skills through <c>Character.SetSkills</c>). The skill bar rebuilds its
    /// buttons from <c>Owner.Skills</c>.
    /// </summary>
    public class SkillListChangedEvent
    {
        public Character Owner { get; }

        public SkillListChangedEvent(Character owner)
        {
            Owner = owner;
        }
    }
}
