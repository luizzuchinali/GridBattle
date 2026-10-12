using GridBattle.Gameplay.Meta;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>A natural pause of the run where a forced ad may show (GDD 7).</summary>
    public enum EAdMoment
    {
        /// <summary>A battle was won: the map comes back.</summary>
        AfterBattle,

        /// <summary>A node without a fight (heal, talent, consumable) was left: the map comes back.</summary>
        AfterNonCombatNode,

        /// <summary>The run ended (defeat, victory or giving up): the end screen.</summary>
        RunEnd
    }

    /// <summary>
    /// Rules of the forced ads (GDD 7), pure: every pause counts one step; the first ad waits for
    /// <see cref="MonetizationSettings.GraceNodes"/> steps in total, the first
    /// <see cref="MonetizationSettings.EarlyAdCount"/> ads come every <see cref="MonetizationSettings.EarlyIntervalNodes"/>
    /// steps and the next ones every <see cref="MonetizationSettings.NormalIntervalNodes"/>, never closer than
    /// <see cref="MonetizationSettings.MinSecondsBetweenAds"/>. An ad that is due but cannot show (no ad loaded, a
    /// moment that is turned off, too soon) stays due for the next pause.
    /// </summary>
    public static class ForcedAdRules
    {
        /// <summary>Whether ads of <paramref name="moment"/> are on.</summary>
        public static bool IsMomentEnabled(MonetizationSettings settings, EAdMoment moment)
        {
            return moment switch
            {
                EAdMoment.AfterBattle => settings.AfterBattle,
                EAdMoment.AfterNonCombatNode => settings.AfterNonCombatNode,
                _ => settings.AtRunEnd,
            };
        }

        /// <summary>Steps between one forced ad and the next, after <paramref name="adsShown"/> ads.</summary>
        public static int GetInterval(MonetizationSettings settings, int adsShown)
        {
            return adsShown < settings.EarlyAdCount ? settings.EarlyIntervalNodes : settings.NormalIntervalNodes;
        }

        /// <summary>
        /// Whether a forced ad is due now, from the player's counters and the seconds since the last forced ad
        /// (null = none this session).
        /// </summary>
        public static bool IsDue(MonetizationSettings settings, MonetizationProfileState state, float? secondsSinceLastAd)
        {
            if (state == null || state.AdsRemoved) return false;
            if (state.PausesCounted < settings.GraceNodes) return false;
            if (state.PausesSinceForcedAd < GetInterval(settings, state.ForcedAdsShown)) return false;

            return !secondsSinceLastAd.HasValue || secondsSinceLastAd.Value >= settings.MinSecondsBetweenAds;
        }

        /// <summary>Counts one pause (a node completed or a run ended).</summary>
        public static void CountPause(MonetizationProfileState state)
        {
            state.PausesCounted++;
            state.PausesSinceForcedAd++;
        }

        /// <summary>A forced ad was shown: the interval starts again.</summary>
        public static void RegisterForcedAd(MonetizationProfileState state)
        {
            state.ForcedAdsShown++;
            state.PausesSinceForcedAd = 0;
        }

        /// <summary>Whether the remove ads window should open after the forced ad just shown.</summary>
        public static bool ShouldOfferRemoveAds(MonetizationSettings settings, MonetizationProfileState state,
            int offersThisSession)
        {
            return settings.OfferEveryNthAd > 0 && state.ForcedAdsShown > 0 &&
                   state.ForcedAdsShown % settings.OfferEveryNthAd == 0 &&
                   offersThisSession < settings.MaxOffersPerSession;
        }
    }
}
