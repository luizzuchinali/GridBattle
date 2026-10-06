using GridBattle.Gameplay.Talents;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the talent module when a talent offer opens (a level was reached, or the player entered a
    /// talent node). The game is paused (<c>TurnBlockers</c>, "Talent choice") until every pending offer is
    /// resolved. The choice screen listens to it, shows <see cref="Offer"/> and answers through
    /// <see cref="TalentService.Choose"/>, <see cref="TalentService.Reroll"/>, <see cref="TalentService.Ban"/> and
    /// <see cref="TalentService.Skip"/>. A kill that gives several levels raises it once per level, in sequence
    /// (the next one right after <see cref="TalentOfferResolvedEvent"/>).
    /// </summary>
    public class TalentOfferOpenedEvent
    {
        public TalentOffer Offer { get; }

        public TalentOfferOpenedEvent(TalentOffer offer)
        {
            Offer = offer;
        }
    }
}
