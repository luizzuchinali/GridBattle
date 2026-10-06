using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Talents;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// The talent decisions of the balance simulation: answers the open offer through <see cref="TalentService"/>
    /// like the choice screen does (choose, and optionally reroll, ban or skip when no option combines with the
    /// build). One call is one step: it returns after a reroll or a ban so the next call sees the new offer.
    /// Random picks use the bot's own <see cref="Rng"/>.
    /// </summary>
    public sealed class TalentPolicy
    {
        private readonly TalentPolicyOptions _options;
        private readonly Rng _rng;
        private int _rerollsOnThisOffer;

        public TalentPolicy(TalentPolicyOptions options, Rng rng)
        {
            _options = options ?? new TalentPolicyOptions();
            _rng = rng ?? new Rng(1UL);
        }

        /// <summary>Takes one step on the open offer. Returns false when there is no offer or nothing could be done.</summary>
        public bool Answer()
        {
            var offer = TalentService.CurrentOffer;
            if (offer == null) return false;

            if (!AnyOptionMatchesBuild(offer))
            {
                if (_options.RerollWithoutSynergy && offer.CanReroll && _rerollsOnThisOffer < _options.MaxRerollsPerOffer)
                {
                    _rerollsOnThisOffer++;
                    if (TalentService.Reroll()) return true;
                }

                if (_options.BanWithoutSynergy && offer.CanBan && offer.Options.Count > 1 && TalentService.Ban(0))
                    return true;

                if (_options.SkipWithoutSynergy && offer.CanSkip && TalentService.Skip())
                {
                    _rerollsOnThisOffer = 0;
                    return true;
                }
            }

            var index = Pick(offer);
            _rerollsOnThisOffer = 0;
            return index >= 0 && TalentService.Choose(index);
        }

        private int Pick(TalentOffer offer)
        {
            var count = offer.Options.Count;
            if (count == 0) return -1;

            // Talents the experiment wants in the build win over the policy.
            var preferred = _options.PreferredTalents;
            for (var p = 0; p < preferred.Count; p++)
            {
                for (var i = 0; i < count; i++)
                {
                    if (offer.Options[i].Talent.name == preferred[p]) return i;
                }
            }

            switch (_options.Policy)
            {
                case ETalentPolicy.HighestSynergy:
                    var best = 0;
                    for (var i = 1; i < count; i++)
                    {
                        if (offer.Options[i].SharedTags.Count > offer.Options[best].SharedTags.Count)
                            best = i;
                    }

                    return best;
                case ETalentPolicy.Random:
                    return _rng.Range(0, count);
                default:
                    return 0;
            }
        }

        private static bool AnyOptionMatchesBuild(TalentOffer offer)
        {
            foreach (var option in offer.Options)
            {
                if (option.MatchesBuild) return true;
            }

            return false;
        }
    }
}
