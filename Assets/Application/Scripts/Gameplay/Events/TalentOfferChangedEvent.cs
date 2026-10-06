using GridBattle.Gameplay.Talents;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the talent module when the open offer changes without closing: a reroll (all options are new),
    /// a ban (one option was replaced) or another offer queued behind it (only <c>QueuedAfter</c> changed). The
    /// choice screen redraws from <see cref="Offer"/> (the same object as <c>TalentService.CurrentOffer</c> from now on).
    /// </summary>
    public class TalentOfferChangedEvent
    {
        public TalentOffer Offer { get; }
        public ETalentOfferChange Change { get; }

        /// <summary>For a ban, the position of the option that was banned (and replaced); -1 for a reroll.</summary>
        public int BannedIndex { get; }

        public TalentOfferChangedEvent(TalentOffer offer, ETalentOfferChange change, int bannedIndex = -1)
        {
            Offer = offer;
            Change = change;
            BannedIndex = bannedIndex;
        }
    }
}
