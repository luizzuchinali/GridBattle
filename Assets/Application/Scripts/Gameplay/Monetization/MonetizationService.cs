using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Simulation;
using UnityEngine;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>What using a talent tool (reroll, ban, skip) costs right now (GDD 7, user decision 2026-10-12).</summary>
    public enum ETalentToolAdMode
    {
        /// <summary>Nothing: the remove ads purchase was made, the platform has no ads (PC) or the ad gate is off.</summary>
        Free,

        /// <summary>A rewarded ad, which is loaded: the use happens when it is watched to the end.</summary>
        Ad,

        /// <summary>A rewarded ad is needed but none is loaded, and the settings let the tool work without it.</summary>
        FreeWithoutAd,

        /// <summary>A rewarded ad is needed but none is loaded: the tools wait for one.</summary>
        Unavailable
    }

    /// <summary>
    /// Monetization of the game (GDD 7). It bootstraps itself (no scene setup), like the audio manager, and picks
    /// the ad provider and the store for the platform (<see cref="MonetizationSettings"/>): LevelPlay and Unity
    /// IAP on Android and iOS, the game's own test ads and store in the Editor (or the SDKs), nothing on PC.
    /// <list type="bullet">
    /// <item>Forced ads: every pause of the run (node completed, run ended) is counted in the profile; when
    /// <see cref="ForcedAdRules"/> says an ad is due, it becomes pending and the UI flow shows it at the next safe
    /// screen (<see cref="TryTakeForcedAd"/>, <see cref="ShowForcedAdAsync"/>). Never in a battle.</item>
    /// <item>Rewarded ad: every use of a talent tool (reroll, ban, skip) shows one first and happens only when
    /// it is watched to the end (<see cref="PayForTalentToolAsync"/>); the use counts against the run's limit as
    /// always. Free after the purchase and on platforms without ads.</item>
    /// <item>Remove ads: <see cref="BuyRemoveAds"/> and <see cref="RestorePurchases"/>; the purchase is kept in
    /// the profile and checked with the store at startup.</item>
    /// </list>
    /// The rules never depend on it: nothing here touches the run's random streams, and the balance simulation
    /// (<see cref="SimMode"/>) turns all of it off.
    /// </summary>
    public sealed class MonetizationService : MonoBehaviour, IRemoveAdsStoreListener
    {
        private const string ObjectName = "MonetizationService";

        private static MonetizationService _instance;

        private MonetizationSettings _settings;
        private IAdsProvider _ads;
        private IRemoveAdsStore _store;
        private bool _pendingForcedAd;
        private float? _lastForcedAdTime;
        private int _offersThisSession;
        private bool _showingAd;
        private EAdKind _showingKind;
        private bool _audioWasPaused;
        private ERemoveAdsMessage _storeMessage;

        // ------------------------------------------------------------------------------------ state

        /// <summary>This platform shows ads (false on PC, in an Editor set to Off and during the simulation).</summary>
        public static bool AdsSupported =>
            _instance != null && !SimMode.IsActive && !(_instance._ads is NoAdsProvider);

        /// <summary>This platform sells the remove ads purchase.</summary>
        public static bool StoreSupported =>
            _instance != null && !SimMode.IsActive && !(_instance._store is NoRemoveAdsStore);

        /// <summary>The remove ads purchase was made.</summary>
        public static bool AdsRemoved => State.AdsRemoved;

        /// <summary>The remove ads window can be offered: there is a store and the purchase was not made.</summary>
        public static bool CanOfferRemoveAds => StoreSupported && !AdsRemoved;

        /// <summary>The ads are the game's own test ads (the UI draws them).</summary>
        public static bool AdsAreSimulated => _instance != null && _instance._ads.IsSimulated;

        /// <summary>An ad is on screen.</summary>
        public static bool IsShowingAd => _instance != null && _instance._showingAd;

        /// <summary>
        /// Where the remove ads purchase stands. The profile decides whether it is owned: a store that still says
        /// owned while the profile does not (the Editor's testing menu cleared it) sells it again.
        /// </summary>
        public static ERemoveAdsStatus RemoveAdsStatus
        {
            get
            {
                if (AdsRemoved) return ERemoveAdsStatus.Owned;
                if (!StoreSupported) return ERemoveAdsStatus.Unavailable;

                var status = _instance._store.Status;
                return status == ERemoveAdsStatus.Owned ? ERemoveAdsStatus.Ready : status;
            }
        }

        /// <summary>The price of the purchase as the store formats it (null until known).</summary>
        public static string RemoveAdsPrice => StoreSupported ? _instance._store.Price : null;

        /// <summary>The outcome of the last purchase or restore (None after <see cref="ClearStoreMessage"/>).</summary>
        public static ERemoveAdsMessage StoreMessage => _instance != null ? _instance._storeMessage : ERemoveAdsMessage.None;

        /// <summary>A forced ad is due and waits for a safe screen.</summary>
        public static bool HasPendingForcedAd => _instance != null && _instance._pendingForcedAd && !SimMode.IsActive;

        /// <summary>The player's monetization counters (the profile's).</summary>
        public static MonetizationProfileState State => ProfileService.State.Monetization ??= new MonetizationProfileState();

        /// <summary>What using a talent tool costs now.</summary>
        public static ETalentToolAdMode TalentToolAdMode
        {
            get
            {
                var instance = _instance;
                if (instance == null || SimMode.IsActive || !instance._settings.TalentToolsNeedAd ||
                    AdsRemoved || instance._ads is NoAdsProvider)
                    return ETalentToolAdMode.Free;

                if (!instance._showingAd && instance._ads.IsReady(EAdKind.Rewarded))
                    return ETalentToolAdMode.Ad;

                return instance._settings.AllowToolsWithoutAd
                    ? ETalentToolAdMode.FreeWithoutAd
                    : ETalentToolAdMode.Unavailable;
            }
        }

        // ------------------------------------------------------------------------------------ forced ads

        /// <summary>
        /// Takes the pending forced ad: true when it can show now (call <see cref="ShowForcedAdAsync"/>). False when
        /// none is pending or no ad is loaded; then the ad stays due and waits for the next pause.
        /// </summary>
        public static bool TryTakeForcedAd()
        {
            var instance = _instance;
            if (instance == null || !instance._pendingForcedAd || SimMode.IsActive) return false;

            instance._pendingForcedAd = false;
            return !AdsRemoved && !instance._showingAd && instance._ads.IsReady(EAdKind.Interstitial);
        }

        /// <summary>Shows a forced ad (after <see cref="TryTakeForcedAd"/>). True when it was shown.</summary>
        public static async Awaitable<bool> ShowForcedAdAsync()
        {
            var instance = _instance;
            if (instance == null) return false;

            var outcome = await instance.ShowAd(EAdKind.Interstitial);
            if (!outcome.Shown) return false;

            var state = State;
            ForcedAdRules.RegisterForcedAd(state);
            instance._lastForcedAdTime = Time.realtimeSinceStartup;
            ProfileService.Save();
            MetricsRecorder.Record("ad_shown", new { kind = "interstitial", forcedAdsShown = state.ForcedAdsShown });
            return true;
        }

        /// <summary>
        /// Whether the remove ads window should open now, after a forced ad (every Nth ad, a few times per
        /// session). Counts the offer when it returns true.
        /// </summary>
        public static bool TakeRemoveAdsOffer()
        {
            var instance = _instance;
            if (instance == null || !CanOfferRemoveAds) return false;
            if (!ForcedAdRules.ShouldOfferRemoveAds(instance._settings, State, instance._offersThisSession)) return false;

            instance._offersThisSession++;
            return true;
        }

        // ------------------------------------------------------------------------------------ rewarded ad

        /// <summary>
        /// Pays for one use of a talent tool: true when the use may happen now. Free and FreeWithoutAd
        /// (<see cref="TalentToolAdMode"/>) answer true at once; Ad shows a rewarded ad and answers true only when it
        /// was watched to the end; Unavailable answers false. The caller then uses the tool through TalentService,
        /// which counts the use.
        /// </summary>
        public static async Awaitable<bool> PayForTalentToolAsync()
        {
            switch (TalentToolAdMode)
            {
                case ETalentToolAdMode.Free:
                case ETalentToolAdMode.FreeWithoutAd:
                    return true;
                case ETalentToolAdMode.Ad:
                    var outcome = await _instance.ShowAd(EAdKind.Rewarded);
                    if (outcome.Shown)
                        MetricsRecorder.Record("ad_shown", new { kind = "rewarded", rewarded = outcome.Rewarded });
                    return outcome.Rewarded;
                default:
                    return false;
            }
        }

        /// <summary>Ends the test ad on screen (Editor): watched to the end, or closed early (no reward).</summary>
        public static void FinishSimulatedAd(bool watchedToEnd)
        {
            if (_instance != null && _instance._ads is SimulatedAdsProvider simulated && _instance._showingAd)
                simulated.Finish(watchedToEnd, _instance._showingKind);
        }

        // ------------------------------------------------------------------------------------ remove ads

        /// <summary>Opens the store's purchase dialog for the remove ads purchase.</summary>
        public static void BuyRemoveAds()
        {
            if (!CanOfferRemoveAds) return;

            ClearStoreMessage();
            _instance._store.Buy();
        }

        /// <summary>Asks the store for the purchases of the player's account.</summary>
        public static void RestorePurchases()
        {
            if (!StoreSupported) return;

            ClearStoreMessage();
            _instance._store.Restore();
        }

        public static void ClearStoreMessage()
        {
            if (_instance == null || _instance._storeMessage == ERemoveAdsMessage.None) return;

            _instance._storeMessage = ERemoveAdsMessage.None;
            EventBus.Raise(new MonetizationChangedEvent());
        }

        /// <summary>
        /// For tests and the Editor menu: sets the purchase flag of the profile directly (no store) and saves.
        /// </summary>
        public static void SetAdsRemovedForTesting(bool removed)
        {
            State.AdsRemoved = removed;
            ProfileService.Save();
            EventBus.Raise(new MonetizationChangedEvent());
        }

        /// <summary>
        /// The profile was erased (development reset): this session's ad state goes too (pending ad, time of the
        /// last ad, offers shown, the last store message).
        /// </summary>
        public static void OnProfileReset()
        {
            if (_instance != null)
            {
                _instance._pendingForcedAd = false;
                _instance._lastForcedAdTime = null;
                _instance._offersThisSession = 0;
                _instance._storeMessage = ERemoveAdsMessage.None;
            }

            EventBus.Raise(new MonetizationChangedEvent());
        }

        /// <summary>For tests and the Editor menu: forgets the forced ad counters (a new player).</summary>
        public static void ResetAdCountersForTesting()
        {
            var state = State;
            state.PausesCounted = 0;
            state.PausesSinceForcedAd = 0;
            state.ForcedAdsShown = 0;
            ProfileService.Save();
            if (_instance == null) return;

            _instance._pendingForcedAd = false;
            _instance._lastForcedAdTime = null;
            _instance._offersThisSession = 0;
        }

        // ------------------------------------------------------------------------------------ lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var go = new GameObject(ObjectName);
            DontDestroyOnLoad(go);
            go.AddComponent<MonetizationService>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _settings = MonetizationSettings.Current;
            CreateProviders();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NodeCompletedEvent>(OnNodeCompleted);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NodeCompletedEvent>(OnNodeCompleted);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        private void Start()
        {
            _ads.Initialize();
            _store.Initialize(this);
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            _ads?.Dispose();
            _store?.Dispose();
            if (_showingAd)
                PauseAudio(false);
            _instance = null;
        }

        private void CreateProviders()
        {
            switch (ResolveMode())
            {
                case EEditorMonetizationMode.Simulated:
                    _ads = new SimulatedAdsProvider(this, () => _settings.SimulatedAdSeconds);
                    _store = new SimulatedRemoveAdsStore(this, _settings);
                    break;
                case EEditorMonetizationMode.Sdk:
                    var levelPlay = new LevelPlayAdsProvider(this, _settings);
                    levelPlay.ReadinessChanged += () => EventBus.Raise(new MonetizationChangedEvent());
                    _ads = levelPlay;
                    _store = new UnityIapRemoveAdsStore(_settings);
                    break;
                default:
                    _ads = new NoAdsProvider();
                    _store = new NoRemoveAdsStore();
                    break;
            }
        }

        /// <summary>The providers for this platform: the Editor's choice, the SDKs on a mobile platform that is on, else none.</summary>
        private EEditorMonetizationMode ResolveMode()
        {
#if UNITY_EDITOR
            return _settings.EditorMode;
#elif UNITY_ANDROID
            return _settings.EnabledOnAndroid ? EEditorMonetizationMode.Sdk : EEditorMonetizationMode.Off;
#elif UNITY_IOS
            return _settings.EnabledOnIos ? EEditorMonetizationMode.Sdk : EEditorMonetizationMode.Off;
#else
            return EEditorMonetizationMode.Off;
#endif
        }

        // ------------------------------------------------------------------------------------ pauses of the run

        private void OnNodeCompleted(NodeCompletedEvent e)
        {
            var battle = e.Node != null && (e.Node.Type == EMapNodeType.Battle || e.Node.Type == EMapNodeType.Boss);
            CountPause(battle ? EAdMoment.AfterBattle : EAdMoment.AfterNonCombatNode);
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            CountPause(EAdMoment.RunEnd);
        }

        private void CountPause(EAdMoment moment)
        {
            if (SimMode.IsActive) return;

            var state = State;
            ForcedAdRules.CountPause(state);
            if (!(_ads is NoAdsProvider) && ForcedAdRules.IsMomentEnabled(_settings, moment) &&
                ForcedAdRules.IsDue(_settings, state, SecondsSinceLastForcedAd))
                _pendingForcedAd = true;

            ProfileService.Save();
        }

        private float? SecondsSinceLastForcedAd =>
            _lastForcedAdTime.HasValue ? Time.realtimeSinceStartup - _lastForcedAdTime.Value : null;

        // ------------------------------------------------------------------------------------ showing ads

        private Awaitable<AdOutcome> ShowAd(EAdKind kind)
        {
            var source = new AwaitableCompletionSource<AdOutcome>();
            if (_showingAd || !_ads.IsReady(kind))
            {
                source.SetResult(default);
                return source.Awaitable;
            }

            _showingAd = true;
            _showingKind = kind;
            PauseAudio(true);
            EventBus.Raise(new AdShowStartedEvent(kind, _ads.IsSimulated,
                _ads.IsSimulated ? _settings.SimulatedAdSeconds : 0f));

            _ads.Show(kind, (shown, rewarded) =>
            {
                _showingAd = false;
                PauseAudio(false);
                EventBus.Raise(new AdShowFinishedEvent(kind, shown, rewarded));
                EventBus.Raise(new MonetizationChangedEvent());
                source.SetResult(new AdOutcome(shown, rewarded));
            });
            return source.Awaitable;
        }

        private void PauseAudio(bool pause)
        {
            if (!_settings.PauseAudioDuringAds) return;

            if (pause)
            {
                _audioWasPaused = AudioListener.pause;
                AudioListener.pause = true;
            }
            else
            {
                AudioListener.pause = _audioWasPaused;
            }
        }

        // ------------------------------------------------------------------------------------ store listener

        void IRemoveAdsStoreListener.OnStoreChanged()
        {
            EventBus.Raise(new MonetizationChangedEvent());
        }

        bool IRemoveAdsStoreListener.OnEntitled()
        {
            var state = State;
            if (!state.AdsRemoved)
            {
                state.AdsRemoved = true;
                if (!ProfileService.Save())
                {
                    state.AdsRemoved = false;
                    return false;
                }

                _pendingForcedAd = false;
                MetricsRecorder.Record("remove_ads_granted");
            }

            EventBus.Raise(new MonetizationChangedEvent());
            return true;
        }

        void IRemoveAdsStoreListener.OnStoreMessage(ERemoveAdsMessage message)
        {
            _storeMessage = message;
            EventBus.Raise(new MonetizationChangedEvent());
        }

        private readonly struct AdOutcome
        {
            public AdOutcome(bool shown, bool rewarded)
            {
                Shown = shown;
                Rewarded = rewarded;
            }

            public bool Shown { get; }
            public bool Rewarded { get; }
        }
    }
}
