using GridBattle.Gameplay.Entities.Skills;

namespace GridBattle.Gameplay.Events
{
    /// <summary>Raised by the run manager when the player turned a skill into one of its upgrades.</summary>
    public class SkillUpgradedEvent
    {
        public SkillDefinition Original { get; }
        public SkillDefinition Upgrade { get; }

        public SkillUpgradedEvent(SkillDefinition original, SkillDefinition upgrade)
        {
            Original = original;
            Upgrade = upgrade;
        }
    }
}
