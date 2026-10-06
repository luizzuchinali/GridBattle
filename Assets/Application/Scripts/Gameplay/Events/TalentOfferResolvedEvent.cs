using GridBattle.Gameplay.Talents;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the talent module after an offer was resolved (a talent chosen, the offer skipped, or no
    /// talent could be offered). The talent is already in effect when it is raised. If more offers are pending
    /// the next <see cref="TalentOfferOpenedEvent"/> follows right after; otherwise the game resumes (the talent
    /// node, if it was one, is completed).
    /// </summary>
    public class TalentOfferResolvedEvent
    {
        /// <summary>The offer as it was when it ended.</summary>
        public TalentOffer Offer { get; }

        public ETalentOfferResult Result { get; }

        /// <summary>The talent taken (null unless <see cref="Result"/> is Chosen).</summary>
        [CanBeNull]
        public TalentDefinition Chosen { get; }

        /// <summary>Rank of the talent after taking it (0 when none was taken).</summary>
        public int Rank { get; }

        /// <summary>More offers wait to open.</summary>
        public bool HasMoreOffers { get; }

        public TalentOfferResolvedEvent(TalentOffer offer, ETalentOfferResult result, [CanBeNull] TalentDefinition chosen,
            int rank, bool hasMoreOffers)
        {
            Offer = offer;
            Result = result;
            Chosen = chosen;
            Rank = rank;
            HasMoreOffers = hasMoreOffers;
        }
    }
}
