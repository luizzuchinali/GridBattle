using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when the player may upgrade one skill (training at a camp, or the reward of an
    /// elite battle). The UI shows <see cref="Skills"/> (the skills that can still be upgraded) and their
    /// <see cref="SkillDefinition.Upgrades"/>, and answers with <c>RunManager.ChooseSkillUpgrade(skill, variant)</c>
    /// or <c>RunManager.DeclineSkillUpgrade()</c>.
    /// </summary>
    public class SkillUpgradeOfferEvent
    {
        public MapNodeState Node { get; }
        public ESkillUpgradeSource Source { get; }

        /// <summary>The player's skills that can still be upgraded, in skill bar order.</summary>
        public IReadOnlyList<SkillDefinition> Skills { get; }

        /// <summary>The run was loaded from the save while the choice was open.</summary>
        public bool Resumed { get; }

        public SkillUpgradeOfferEvent(MapNodeState node, ESkillUpgradeSource source, IReadOnlyList<SkillDefinition> skills,
            bool resumed)
        {
            Node = node;
            Source = source;
            Skills = skills;
            Resumed = resumed;
        }
    }
}
