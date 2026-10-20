using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when the player enters a camp (a heal node that also lets the player train): the
    /// UI asks whether to rest (<c>RunManager.CampRest()</c>, heals <see cref="HealAmount"/>) or to train
    /// (<c>RunManager.CampTrain()</c>, opens the skill upgrade choice).
    /// </summary>
    public class CampEnteredEvent
    {
        public MapNodeState Node { get; }

        /// <summary>HP resting would recover (already limited by the maximum).</summary>
        public int HealAmount { get; }

        /// <summary>The run was loaded from the save while the camp was open.</summary>
        public bool Resumed { get; }

        public CampEnteredEvent(MapNodeState node, int healAmount, bool resumed)
        {
            Node = node;
            HealAmount = healAmount;
            Resumed = resumed;
        }
    }
}
