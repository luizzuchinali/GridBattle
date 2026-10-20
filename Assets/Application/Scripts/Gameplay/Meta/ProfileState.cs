using System;
using System.Collections.Generic;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Everything the game remembers about the player between runs (GDD 8.1), saved
    /// in <c>profile.json</c> and kept apart from the run save. Plain data
    /// (Newtonsoft JSON): content is referenced by
    /// <see cref="Data.GameDefinition.Id"/>. Read it through
    /// <see cref="ProfileService"/> and change it only through that service so every
    /// change is saved and announced.
    /// </summary>
    [Serializable]
    public sealed class ProfileState
    {
        public const int CurrentVersion = 1;

        /// <summary>Save format version, used by <see cref="ProfileService"/> to migrate old files.</summary>
        public int Version = CurrentVersion;

        /// <summary>Battles won across all runs (GDD 3.2). Only battles count, not the other node types.</summary>
        public int TotalBattlesWon;

        /// <summary>
        /// Battles won in the run in progress that are not counted yet. Only used when
        /// <see cref="MetaSettings.CountBattlesFromLostRuns"/> is off: they are added to
        /// <see cref="TotalBattlesWon"/> if the run ends in victory and dropped otherwise.
        /// </summary>
        public int PendingBattlesWon;

        /// <summary>Records by class (key = PlayerCharacterConfig id).</summary>
        public Dictionary<string, ClassRecord> Classes = new();

        /// <summary>
        /// Classes that were unlocked at some point (PlayerCharacterConfig ids). Sticky: a class
        /// stays unlocked even if its requirement is raised later.
        /// </summary>
        public List<string> UnlockedClassIds = new();

        /// <summary>Unlocked classes the player was not told about yet (for the end-of-run screen).</summary>
        public List<string> UnannouncedUnlockedClassIds = new();

        public GlossaryState Glossary = new();

        /// <summary>Tutorial tips already shown (TutorialTipDefinition ids).</summary>
        public List<string> TipsSeen = new();

        public OptionsState Options = new();

        /// <summary>Forced ads and the remove ads purchase (GDD 7).</summary>
        public MonetizationProfileState Monetization = new();
    }

    /// <summary>Results of the player with one class (GDD 2.2).</summary>
    [Serializable]
    public sealed class ClassRecord
    {
        public int RunsPlayed;
        public int Victories;

        /// <summary>Highest level reached in any run with this class.</summary>
        public int BestLevel;

        /// <summary>Deepest map position reached in any run with this class (1 = first node).</summary>
        public int BestDepth;
    }

    /// <summary>What the player has discovered (GDD 2.7), persistent between runs.</summary>
    [Serializable]
    public sealed class GlossaryState
    {
        /// <summary>Talents chosen at least once, by class (key = PlayerCharacterConfig id, value = TalentDefinition ids).</summary>
        public Dictionary<string, List<string>> ChosenTalents = new();

        /// <summary>
        /// Talents that were offered at least once, by class. Only filled when
        /// <see cref="MetaSettings.DiscoverOfferedTalents"/> is on.
        /// </summary>
        public Dictionary<string, List<string>> OfferedTalents = new();

        /// <summary>Enemy types faced at least once (EnemyConfig ids).</summary>
        public List<string> EnemiesFaced = new();
    }

    /// <summary>Player options (interface 4.3).</summary>
    [Serializable]
    public sealed class OptionsState
    {
        /// <summary>Locale code ("en", "es", "pt-BR"). Null = automatic (system language, English fallback).</summary>
        public string Locale;

        /// <summary>Music volume, 0..1.</summary>
        public float MusicVolume = 1f;

        /// <summary>Sound effects volume, 0..1.</summary>
        public float SfxVolume = 1f;

        /// <summary>Whether contextual tutorial tips are shown.</summary>
        public bool TipsEnabled = true;

        /// <summary>Whether hits shake the screen (game feel; some players prefer it still).</summary>
        public bool ScreenShake = true;
    }

    /// <summary>
    /// What monetization remembers about the player (GDD 7): the remove ads purchase (a local copy of the store
    /// entitlement, checked again with the store at startup) and the counters of the forced ad frequency ramp.
    /// </summary>
    [Serializable]
    public sealed class MonetizationProfileState
    {
        /// <summary>The remove ads purchase was made (no forced ads, and the talent tools need no ad).</summary>
        public bool AdsRemoved;

        /// <summary>Pauses of the run counted in total (nodes completed and runs ended), for the grace period.</summary>
        public int PausesCounted;

        /// <summary>Pauses counted since the last forced ad.</summary>
        public int PausesSinceForcedAd;

        /// <summary>Forced ads shown in total (the first ones use the early, lower frequency).</summary>
        public int ForcedAdsShown;
    }
}
