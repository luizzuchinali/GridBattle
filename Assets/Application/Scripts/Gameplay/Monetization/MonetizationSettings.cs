using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>What plays the ads and sells the purchase when the game runs in the Unity Editor.</summary>
    public enum EEditorMonetizationMode
    {
        /// <summary>The game's own test ads and store: a short countdown screen and a purchase that always works.</summary>
        Simulated,

        /// <summary>The real SDKs: LevelPlay mock ads (build target Android or iOS) and the Unity IAP fake store.</summary>
        Sdk,

        /// <summary>No ads and no store, like a PC build: the talent tools are free.</summary>
        Off
    }

    /// <summary>A privacy answer to pass to the ad SDK before it starts (not set = the SDK is not told anything).</summary>
    public enum EConsentValue
    {
        NotSet,
        Yes,
        No
    }

    /// <summary>
    /// Monetization (GDD 7): forced ads at natural pauses of the run with a frequency ramp, a rewarded ad before
    /// every use of a talent offer tool (reroll, ban, skip; the use counts against the run's limit as always), and
    /// the purchase that removes the forced ads and the ads of the tools. Ads come from Unity LevelPlay and the
    /// purchase from Unity IAP (Google Play / App Store), only on Android and iOS; elsewhere there are no ads and
    /// the tools are free. The open questions of GDD 7 are fields here with a neutral default
    /// (<c>docs/planos/valores_padrao_em_aberto.md</c>).
    /// </summary>
    [CreateAssetMenu(fileName = "MonetizationSettings", menuName = "GridBattle/Settings/Monetization Settings", order = 20)]
    public sealed class MonetizationSettings : ScriptableObject, IGameSettings
    {
        [Header("Platforms")]
        [SerializeField]
        [Tooltip("Ads and the purchase on Android builds.")]
        private bool enabledOnAndroid = true;

        [SerializeField]
        [Tooltip("Ads and the purchase on iOS builds.")]
        private bool enabledOnIos = true;

        [SerializeField]
        [Tooltip("What the Editor uses. Simulated: the game's own test ads and store (no SDK). Sdk: LevelPlay mock " +
                 "ads (needs the Android or iOS build target) and the Unity IAP fake store. Off: like a PC build.")]
        private EEditorMonetizationMode editorMode = EEditorMonetizationMode.Simulated;

        [Header("LevelPlay (ads)")]
        [SerializeField]
        [Tooltip("App Key of the Android app in the LevelPlay dashboard.")]
        private string androidAppKey;

        [SerializeField]
        [Tooltip("App Key of the iOS app in the LevelPlay dashboard.")]
        private string iosAppKey;

        [SerializeField]
        [Tooltip("Ad unit id of the forced (interstitial) ad on Android.")]
        private string androidInterstitialAdUnitId;

        [SerializeField]
        [Tooltip("Ad unit id of the rewarded ad on Android.")]
        private string androidRewardedAdUnitId;

        [SerializeField]
        [Tooltip("Ad unit id of the forced (interstitial) ad on iOS.")]
        private string iosInterstitialAdUnitId;

        [SerializeField]
        [Tooltip("Ad unit id of the rewarded ad on iOS.")]
        private string iosRewardedAdUnitId;

        [SerializeField]
        [Tooltip("Development builds only: opens the LevelPlay Test Suite after the SDK starts (checks every ad " +
                 "network on a device). Never in a release build.")]
        private bool launchTestSuite;

        [SerializeField]
        [Tooltip("Development builds only: verbose logs of the ad network adapters.")]
        private bool adaptersDebug;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Seconds before loading an ad again after a load failed (doubles on each failure, up to 8x).")]
        private float loadRetrySeconds = 30f;

        [Header("Privacy (passed to LevelPlay before it starts)")]
        [SerializeField]
        [Tooltip("GDPR / LGPD consent. Not set = nothing is passed. A consent screen of your own should set this " +
                 "per player; this field is the default.")]
        private EConsentValue gdprConsent = EConsentValue.NotSet;

        [SerializeField]
        [Tooltip("CCPA: Yes = the player opted out of the sale of personal data. Not set = nothing is passed.")]
        private EConsentValue ccpaDoNotSell = EConsentValue.NotSet;

        [SerializeField]
        [Tooltip("The app is directed at children (COPPA). Not set = nothing is passed.")]
        private EConsentValue childDirected = EConsentValue.NotSet;

        [Header("Unity IAP (remove ads purchase)")]
        [SerializeField]
        [Tooltip("Product id of the non-consumable purchase that removes the forced ads (the same in both stores " +
                 "unless overridden below).")]
        private string removeAdsProductId = "remove_ads";

        [SerializeField]
        [Tooltip("Product id in Google Play when it differs from the one above (empty = the same).")]
        private string googlePlayProductId;

        [SerializeField]
        [Tooltip("Product id in the App Store when it differs from the one above (empty = the same).")]
        private string appleProductId;

        [Header("Forced ads (GDD 7): when")]
        [SerializeField]
        [Tooltip("After a battle is won, back on the map.")]
        private bool afterBattle = true;

        [SerializeField]
        [Tooltip("After a node without a fight (heal, talent, consumable), back on the map.")]
        private bool afterNonCombatNode = true;

        [SerializeField]
        [Tooltip("At the end of a run (defeat, victory or giving up), on the end screen.")]
        private bool atRunEnd = true;

        [Header("Forced ads (GDD 7): frequency ramp")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Nodes the player completes (in total, across runs) before the first forced ad: time to like the game.")]
        private int graceNodes = 10;

        [SerializeField]
        [Min(0)]
        [Tooltip("How many forced ads use the low early frequency.")]
        private int earlyAdCount = 3;

        [SerializeField]
        [Min(1)]
        [Tooltip("Nodes between forced ads in the early phase.")]
        private int earlyIntervalNodes = 6;

        [SerializeField]
        [Min(1)]
        [Tooltip("Nodes between forced ads after the early phase (normal frequency).")]
        private int normalIntervalNodes = 4;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Minimum seconds between two forced ads (heal and consumable nodes are instant). An ad that is due " +
                 "waits for the next pause after this time.")]
        private float minSecondsBetweenAds = 180f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Seconds of the notice (\"Ad break in 3...\") before a forced ad. 0 = no notice.")]
        private int noticeSeconds = 3;

        [Header("Remove ads offer after a forced ad")]
        [SerializeField]
        [Min(0)]
        [Tooltip("After every Nth forced ad, the remove ads window opens. 0 = never.")]
        private int offerEveryNthAd = 3;

        [SerializeField]
        [Min(0)]
        [Tooltip("At most this many of those offers per session (app launch).")]
        private int maxOffersPerSession = 1;

        [Header("Rewarded ad (talent tools)")]
        [SerializeField]
        [Tooltip("Every use of reroll, ban or skip shows a rewarded ad first and happens only when it is watched to " +
                 "the end (user decision 2026-10-12), also in the talent choice of a battle (the player asks for " +
                 "it). Off = the tools are free for everyone.")]
        private bool talentToolsNeedAd = true;

        [SerializeField]
        [Tooltip("When no rewarded ad can be shown (no connection, no fill), the tools work without it. Off = they " +
                 "wait for an ad (the window says the ad is unavailable).")]
        private bool allowToolsWithoutAd = true;

        [Header("Ad playback")]
        [SerializeField]
        [Tooltip("The game's sound is paused while an ad is on screen.")]
        private bool pauseAudioDuringAds = true;

        [SerializeField]
        [Min(0.5f)]
        [Tooltip("Seconds a simulated (Editor) ad stays on screen.")]
        private float simulatedAdSeconds = 2.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds a simulated (Editor) purchase or restore waits before answering.")]
        private float simulatedStoreSeconds = 0.6f;

        [SerializeField]
        [Tooltip("Price the simulated (Editor) store shows. Test only: real prices come from the stores.")]
        private string simulatedPrice = "$1.99";

        [SerializeField]
        [Tooltip("The simulated (Editor) store account already owns the purchase: Restore purchases finds it (tests " +
                 "a new device or a reinstall).")]
        private bool simulatedAccountOwnsPurchase;

        public bool EnabledOnAndroid => enabledOnAndroid;
        public bool EnabledOnIos => enabledOnIos;
        public EEditorMonetizationMode EditorMode => editorMode;
        public string AndroidAppKey => androidAppKey;
        public string IosAppKey => iosAppKey;
        public string AndroidInterstitialAdUnitId => androidInterstitialAdUnitId;
        public string AndroidRewardedAdUnitId => androidRewardedAdUnitId;
        public string IosInterstitialAdUnitId => iosInterstitialAdUnitId;
        public string IosRewardedAdUnitId => iosRewardedAdUnitId;
        public bool LaunchTestSuite => launchTestSuite;
        public bool AdaptersDebug => adaptersDebug;
        public float LoadRetrySeconds => Mathf.Max(1f, loadRetrySeconds);
        public EConsentValue GdprConsent => gdprConsent;
        public EConsentValue CcpaDoNotSell => ccpaDoNotSell;
        public EConsentValue ChildDirected => childDirected;
        public string RemoveAdsProductId => removeAdsProductId;
        public string GooglePlayProductId => googlePlayProductId;
        public string AppleProductId => appleProductId;
        public bool AfterBattle => afterBattle;
        public bool AfterNonCombatNode => afterNonCombatNode;
        public bool AtRunEnd => atRunEnd;
        public int GraceNodes => graceNodes;
        public int EarlyAdCount => earlyAdCount;
        public int EarlyIntervalNodes => Mathf.Max(1, earlyIntervalNodes);
        public int NormalIntervalNodes => Mathf.Max(1, normalIntervalNodes);
        public float MinSecondsBetweenAds => minSecondsBetweenAds;
        public int NoticeSeconds => noticeSeconds;
        public int OfferEveryNthAd => offerEveryNthAd;
        public int MaxOffersPerSession => maxOffersPerSession;
        public bool TalentToolsNeedAd => talentToolsNeedAd;
        public bool AllowToolsWithoutAd => allowToolsWithoutAd;
        public bool PauseAudioDuringAds => pauseAudioDuringAds;
        public float SimulatedAdSeconds => Mathf.Max(0.5f, simulatedAdSeconds);
        public float SimulatedStoreSeconds => simulatedStoreSeconds;
        public string SimulatedPrice => simulatedPrice;
        public bool SimulatedAccountOwnsPurchase => simulatedAccountOwnsPurchase;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static MonetizationSettings Current => GameSettings.Get<MonetizationSettings>();
    }
}
