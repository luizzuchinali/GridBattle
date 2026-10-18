using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;

namespace GridBattle.Gameplay.Events
{
    /// <summary>What happened to a telegraphed skill (<see cref="SkillTelegraphChangedEvent"/>).</summary>
    public enum ETelegraphChange
    {
        /// <summary>The enemy prepared the skill: its cells are marked.</summary>
        Prepared,

        /// <summary>One turn of waiting passed (the turns left went down).</summary>
        Waited,

        /// <summary>The skill fired on the marked cells (raised before the damage).</summary>
        Fired,

        /// <summary>The preparation was cancelled (death, a state that prevents skills, a push or pull).</summary>
        Cancelled,

        /// <summary>The preparation was restored from a battle save.</summary>
        Restored
    }

    /// <summary>
    /// Raised by <see cref="Enemy"/> whenever its telegraphed skill (<see cref="SkillTelegraph"/>) is prepared,
    /// waits, fires, is cancelled or is restored: the cell marks, the icons over the enemy and the tutorial react.
    /// </summary>
    public class SkillTelegraphChangedEvent
    {
        public SkillTelegraphChangedEvent(Enemy caster, SkillTelegraph telegraph, ETelegraphChange change)
        {
            Caster = caster;
            Telegraph = telegraph;
            Change = change;
        }

        public Enemy Caster { get; }
        public SkillTelegraph Telegraph { get; }
        public ETelegraphChange Change { get; }
    }
}
