using GridBattle.Gameplay.Monetization;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// An ad starts (GDD 7). The real SDK covers the game with its own screen; a simulated ad (Editor) has none, so
    /// the UI shows its test screen while <see cref="Simulated"/> is true.
    /// </summary>
    public class AdShowStartedEvent
    {
        public EAdKind Kind { get; }

        /// <summary>A test ad of the game itself (no SDK): the UI draws it.</summary>
        public bool Simulated { get; }

        /// <summary>How long a simulated ad lasts, in real seconds (0 for a real ad).</summary>
        public float Seconds { get; }

        public AdShowStartedEvent(EAdKind kind, bool simulated, float seconds)
        {
            Kind = kind;
            Simulated = simulated;
            Seconds = seconds;
        }
    }

    /// <summary>The ad of <see cref="AdShowStartedEvent"/> is over.</summary>
    public class AdShowFinishedEvent
    {
        public EAdKind Kind { get; }

        /// <summary>The ad was shown (false = it failed to show).</summary>
        public bool Shown { get; }

        /// <summary>A rewarded ad was watched to the end: the reward is given.</summary>
        public bool Rewarded { get; }

        public AdShowFinishedEvent(EAdKind kind, bool shown, bool rewarded)
        {
            Kind = kind;
            Shown = shown;
            Rewarded = rewarded;
        }
    }

    /// <summary>
    /// Something the monetization UI shows changed: the remove ads purchase (owned, price, purchase or restore in
    /// progress or finished) or whether a rewarded ad is ready.
    /// </summary>
    public class MonetizationChangedEvent
    {
    }
}
