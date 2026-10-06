using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Rules of the player profile and meta-progression (GDD 2.2, 2.7, 3.2, 8.1, 9 and
    /// interface 4.3/4.4). Several of these are open questions in the design
    /// documents; the defaults are the suggestions written there.
    /// </summary>
    [CreateAssetMenu(fileName = "MetaSettings", menuName = "GridBattle/Settings/Meta Settings", order = 5)]
    public sealed class MetaSettings : ScriptableObject, IGameSettings
    {
        [Header("Class unlocking (GDD 3.2)")]
        [SerializeField]
        [Tooltip("Open question: whether battles won in runs that end in defeat count toward unlocking classes. " +
                 "When off, battles are held back and only committed if the run ends in victory " +
                 "(ProfileService.RegisterRunEnded must be called when every run ends).")]
        private bool countBattlesFromLostRuns = true;

        [Header("Glossary (GDD 2.7)")]
        [SerializeField]
        [Tooltip("Open question: when on, talents that were only offered (not chosen) are also revealed in the glossary.")]
        private bool discoverOfferedTalents;

        [SerializeField]
        [Tooltip("Open question: when on, enemies not faced yet appear as '?' in the glossary; when off they are hidden.")]
        private bool showUnknownEnemies = true;

        [Header("Run records and give up (interface 4.3)")]
        [SerializeField]
        [Tooltip("Open question: giving up a run counts as a defeat in the per-class records (level, depth, runs played).")]
        private bool giveUpCountsAsDefeat = true;

        [Header("Tutorial tips (interface 4.4)")]
        [SerializeField]
        [Tooltip("Value of the 'tips enabled' option in a brand-new profile.")]
        private bool tipsEnabledByDefault = true;

        [SerializeField]
        [Tooltip("Open question: whether the player can turn tips off in the options. When off, tips are always enabled.")]
        private bool allowDisablingTips = true;

        [Header("Options defaults")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Music volume (0..1) of a brand-new profile.")]
        private float defaultMusicVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Sound effects volume (0..1) of a brand-new profile.")]
        private float defaultSfxVolume = 1f;

        [Header("Design metrics (GDD 9)")]
        [SerializeField]
        [Tooltip("Appends design metrics (battles, damage, deaths...) to metrics.jsonl in the persistent data path. " +
                 "Local only: how the data is collected and sent is an open question.")]
        private bool recordMetrics = true;

        [SerializeField]
        [Min(0)]
        [Tooltip("When metrics.jsonl exceeds this size (KB) it is moved to metrics.old.jsonl and a new file starts. 0 = no limit.")]
        private int metricsMaxFileKb = 2048;

        public bool CountBattlesFromLostRuns => countBattlesFromLostRuns;
        public bool DiscoverOfferedTalents => discoverOfferedTalents;
        public bool ShowUnknownEnemies => showUnknownEnemies;
        public bool GiveUpCountsAsDefeat => giveUpCountsAsDefeat;
        public bool TipsEnabledByDefault => tipsEnabledByDefault;
        public bool AllowDisablingTips => allowDisablingTips;
        public float DefaultMusicVolume => defaultMusicVolume;
        public float DefaultSfxVolume => defaultSfxVolume;
        public bool RecordMetrics => recordMetrics;
        public int MetricsMaxFileKb => metricsMaxFileKb;

        /// <summary>The registered settings asset (defaults if none is registered).</summary>
        public static MetaSettings Current => GameSettings.Get<MetaSettings>();
    }
}
