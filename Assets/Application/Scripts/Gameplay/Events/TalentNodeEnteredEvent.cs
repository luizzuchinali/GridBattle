using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when the player enters a talent node, after the HP cost was paid. The talent
    /// module opens the offer (<see cref="ETalentOfferSource.TalentNode"/>) and calls
    /// <c>RunManager.CompleteTalentNode()</c> when the player has chosen (or skipped).
    /// </summary>
    public class TalentNodeEnteredEvent
    {
        public MapNodeState Node { get; }

        /// <summary>HP the node cost (already deducted from the run's HP; 0 when free or when the player had 1 HP).</summary>
        public int HpCost { get; }

        /// <summary>The run was loaded from the save while this node was open (the cost was already paid).</summary>
        public bool Resumed { get; }

        public TalentNodeEnteredEvent(MapNodeState node, int hpCost, bool resumed)
        {
            Node = node;
            HpCost = hpCost;
            Resumed = resumed;
        }
    }
}
