using System.Collections.Generic;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Monetization;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Simulation;
using GridBattle.Gameplay.Talents;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Hud;
using GridBattle.UI.Overlays;
using UnityEngine;
using ZS.UI.Navigation;

namespace GridBattle.UI.Flow
{
    /// <summary>
    /// Game-specific screen flow: translates game/UI events into navigation on the UIRoot. Start → main menu →
    /// map → battle (HUD) → map ... → end of run (defeat, given up or victory) → main menu, plus the modals the
    /// flow opens (pause menu, consumable offer, new-run confirmation, talent choice, glossary, options, the
    /// contextual tutorial tips and the monetization windows of GDD 7: remove ads, the notice before a forced ad
    /// and the screen of a test ad).
    /// <para>
    /// Forced ads: when <see cref="MonetizationService"/> has one pending, it shows on the next safe screen (map,
    /// end of run, main menu; never the battle) once nothing else is going on: no transition, no modal, no tip or
    /// offer waiting. The notice comes first, and now and then the remove ads window after the ad.
    /// </para>
    /// <para>
    /// The run manager has no UI and decides when the run moves on; this class only follows its events. Screen
    /// changes are requests for a <i>target screen</i>: if a transition is running the target waits for it and is
    /// applied afterwards, so a request that arrives mid-transition (e.g. a very fast battle) is never lost and
    /// intermediate screens are skipped instead of queued.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIRoot))]
    public class GameFlowController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField]
        private ViewDefinition mainMenuScreen;

        [SerializeField]
        [Tooltip("The battle HUD (the grid world is behind it).")]
        private ViewDefinition gameScreen;

        [SerializeField]
        [Tooltip("Node map of the run.")]
        private ViewDefinition mapScreen;

        [SerializeField]
        [Tooltip("End of run: defeat or given up.")]
        private ViewDefinition runEndScreen;

        [SerializeField]
        private ViewDefinition victoryScreen;

        [Header("Modals")]
        [SerializeField]
        private ViewDefinition menuOverlay;

        [SerializeField]
        [Tooltip("Consumable node offer (choose an item, discard one when the inventory is full).")]
        private ViewDefinition consumableOfferOverlay;

        [SerializeField]
        [Tooltip("Yes/no confirmation (starting a new run discards the saved one).")]
        private ViewDefinition confirmOverlay;

        private UIRoot _uiRoot;
        private Navigator _hookedNavigator;

        private ViewDefinition _targetScreen;
        private object _targetArgs;
        private bool _hasTarget;
        private bool _confirmingNewRun;
        private bool _showingCharacterDetails;

        private readonly List<PendingModal> _pendingModals = new();

        /// <summary>Tutorial tips waiting for a moment when they can show (one at a time, never under a modal).</summary>
        private readonly Queue<TutorialTipDefinition> _tipQueue = new();

        /// <summary>A talent offer opened while the navigator was busy: its window opens as soon as it is idle.</summary>
        private bool _talentOfferPending;

        /// <summary>A forced ad is going on (notice, ad, remove ads offer): tips and waiting modals hold.</summary>
        private bool _adSequenceRunning;

        private Navigator Navigator => _uiRoot != null ? _uiRoot.Navigator : null;

        /// <summary>The screen the flow is heading to (or showing), for tests and debugging.</summary>
        public ViewDefinition TargetScreen => _targetScreen;

        private void Awake()
        {
            _uiRoot = GetComponent<UIRoot>();
        }

        private void Start()
        {
            // The navigator exists once the UIRoot has woken up.
            _hookedNavigator = Navigator;
            if (_hookedNavigator != null)
                _hookedNavigator.Changed += OnNavigationChanged;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Subscribe<CharacterDetailsRequestedEvent>(OnCharacterDetailsRequested);
            EventBus.Subscribe<ContinueRunRequestedEvent>(OnContinueRequested);
            EventBus.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            EventBus.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Subscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Subscribe<ConsumableOfferEvent>(OnConsumableOffer);
            EventBus.Subscribe<CampEnteredEvent>(OnCampEntered);
            EventBus.Subscribe<SkillUpgradeOfferEvent>(OnSkillUpgradeOffer);
            EventBus.Subscribe<MenuOpenedEvent>(OnMenuOpened);
            EventBus.Subscribe<TalentOfferOpenedEvent>(OnTalentOfferOpened);
            EventBus.Subscribe<GlossaryRequestedEvent>(OnGlossaryRequested);
            EventBus.Subscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventBus.Subscribe<TutorialTipRequestedEvent>(OnTutorialTipRequested);
            EventBus.Subscribe<RemoveAdsRequestedEvent>(OnRemoveAdsRequested);
            EventBus.Subscribe<AdShowStartedEvent>(OnAdShowStarted);
            EventBus.Subscribe<GameResetEvent>(OnGameReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Unsubscribe<CharacterDetailsRequestedEvent>(OnCharacterDetailsRequested);
            EventBus.Unsubscribe<ContinueRunRequestedEvent>(OnContinueRequested);
            EventBus.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            EventBus.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Unsubscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Unsubscribe<ConsumableOfferEvent>(OnConsumableOffer);
            EventBus.Unsubscribe<CampEnteredEvent>(OnCampEntered);
            EventBus.Unsubscribe<SkillUpgradeOfferEvent>(OnSkillUpgradeOffer);
            EventBus.Unsubscribe<MenuOpenedEvent>(OnMenuOpened);
            EventBus.Unsubscribe<TalentOfferOpenedEvent>(OnTalentOfferOpened);
            EventBus.Unsubscribe<GlossaryRequestedEvent>(OnGlossaryRequested);
            EventBus.Unsubscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventBus.Unsubscribe<TutorialTipRequestedEvent>(OnTutorialTipRequested);
            EventBus.Unsubscribe<RemoveAdsRequestedEvent>(OnRemoveAdsRequested);
            EventBus.Unsubscribe<AdShowStartedEvent>(OnAdShowStarted);
            EventBus.Unsubscribe<GameResetEvent>(OnGameReset);
        }

        private void LateUpdate()
        {
            // Tips wait a frame: the events raised in the same call (a level up opens the talent window right
            // after its tip is requested) register first, so a tip never opens under the window it explains.
            PumpTips();
            PumpForcedAd();
        }

        private void OnDestroy()
        {
            if (_hookedNavigator != null)
                _hookedNavigator.Changed -= OnNavigationChanged;
            _hookedNavigator = null;
        }

        // ------------------------------------------------------------------------------------ events

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            ShowScreen(mainMenuScreen);
        }

        private void OnMainMenuRequested(MainMenuRequestedEvent e)
        {
            ShowScreen(mainMenuScreen);
        }

        private async void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            var manager = GameStateManager.Instance;
            var runs = RunManager.Instance;
            if (manager == null || runs == null || _confirmingNewRun) return;

            // A locked class cannot be picked (the menu already shows it locked).
            var config = manager.FindPlayableCharacter(e.Character);
            if (config == null || !ProfileService.IsClassUnlocked(config)) return;

            if (runs.HasContinuableRun && confirmOverlay != null && Navigator != null)
            {
                _confirmingNewRun = true;
                var request = new ConfirmRequest(HudText.Get("menu.new_run.title"), HudText.Get("menu.new_run.message"));
                bool confirmed;
                try
                {
                    confirmed = await Navigator.ShowModal<bool>(confirmOverlay, request);
                }
                finally
                {
                    _confirmingNewRun = false;
                }

                if (!confirmed) return;
                runs.AbandonSavedRun();
            }

            // The run starts right away: the map (MapOpenedEvent) follows and the screen change waits for any
            // running transition.
            manager.StartRun(e.Character);
        }

        /// <summary>
        /// A character card was tapped: the details window opens; Play there starts a new run with the class (the
        /// same path as <see cref="CharacterChoosenEvent"/>, with the confirmation over a run in progress).
        /// </summary>
        private async void OnCharacterDetailsRequested(CharacterDetailsRequestedEvent e)
        {
            var view = ModalsSettings.Current.CharacterDetailsView;
            var manager = GameStateManager.Instance;
            if (view == null || Navigator == null || manager == null || _showingCharacterDetails) return;

            var config = manager.FindPlayableCharacter(e.Character);
            if (config == null) return;

            _showingCharacterDetails = true;
            bool play;
            try
            {
                play = await Navigator.ShowModal<bool>(view, config);
            }
            finally
            {
                _showingCharacterDetails = false;
            }

            if (play)
                EventBus.Raise(new CharacterChoosenEvent(e.Character));
        }

        private void OnContinueRequested(ContinueRunRequestedEvent e)
        {
            var runs = RunManager.Instance;
            if (runs == null || !runs.HasContinuableRun) return;

            runs.ContinueRun();
        }

        private void OnRunStarted(RunStartedEvent e)
        {
            // A resumed run may stop on a talent node or a consumable offer: no MapOpenedEvent follows, but the map
            // is the screen behind those modals. A saved battle announces itself with BattleStartedEvent.
            if (e.Run != null && e.Run.Phase != ERunPhase.Battle && e.Run.Phase != ERunPhase.Finished)
                ShowScreen(mapScreen);
        }

        private void OnMapOpened(MapOpenedEvent e)
        {
            ShowScreen(mapScreen);
        }

        private void OnBattleStarted(BattleStartedEvent e)
        {
            ShowScreen(gameScreen);
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            // The development reset ends the run on its way to the main menu: no end of run screen.
            if (GameReset.IsResetting) return;

            ShowScreen(e.Victory ? victoryScreen : runEndScreen, e.Summary);
        }

        /// <summary>The development reset erased everything: nothing waiting is shown and the main menu opens.</summary>
        private void OnGameReset(GameResetEvent e)
        {
            _pendingModals.Clear();
            DropWaitingModals();
            ShowScreen(mainMenuScreen);
        }

        /// <summary>
        /// A consumable offer: from a consumable node the map is already showing; the reward of an elite battle comes
        /// right after the battle, so the map comes first (the battle screen goes away) and the window opens over it.
        /// </summary>
        private void OnConsumableOffer(ConsumableOfferEvent e)
        {
            if (MapRules.IsEliteBattle(e.Node))
                ShowScreen(mapScreen);
            ShowModalWhenOnMap(consumableOfferOverlay, e);
        }

        private void OnCampEntered(CampEnteredEvent e)
        {
            ShowModalWhenOnMap(ModalsSettings.Current.SkillUpgradeView, e);
        }

        /// <summary>
        /// A skill upgrade to choose: from a camp the map is already showing; after an elite battle the map comes
        /// first (the battle screen goes away) and the window opens over it.
        /// </summary>
        private void OnSkillUpgradeOffer(SkillUpgradeOfferEvent e)
        {
            if (SimMode.IsActive) return;

            if (e.Source == ESkillUpgradeSource.Elite && !e.Resumed)
                ShowScreen(mapScreen);
            ShowModalWhenOnMap(ModalsSettings.Current.SkillUpgradeView, e);
        }

        private void OnMenuOpened(MenuOpenedEvent e)
        {
            // The game is paused by the talent choice: only that window answers.
            if (TalentService.HasOffer) return;

            Navigator.ShowModal(menuOverlay);
        }

        /// <summary>A talent offer opened (level up or talent node): its window opens as soon as the navigator is idle.</summary>
        private void OnTalentOfferOpened(TalentOfferOpenedEvent e)
        {
            if (ModalsSettings.Current.TalentChoiceView == null || Navigator == null || SimMode.IsActive) return;

            // An open window shows the next offer of a kill that gave several levels by itself.
            if (IsModalOpen<TalentChoiceController>()) return;

            _talentOfferPending = true;
            FlushTalentOffer();
        }

        private void OnGlossaryRequested(GlossaryRequestedEvent e)
        {
            var settings = ModalsSettings.Current;
            var view = settings.GlossaryView;
            if (view == null || Navigator == null || IsModalOpen(view)) return;

            // Open question (interface 4.3): not reachable during the talent pause unless the setting says so.
            if (TalentService.HasOffer && !settings.AllowGlossaryDuringTalentChoice) return;

            _ = Navigator.ShowModal(view);
        }

        private void OnOptionsRequested(OptionsRequestedEvent e)
        {
            var view = ModalsSettings.Current.OptionsView;
            if (view == null || Navigator == null || IsModalOpen(view)) return;

            _ = Navigator.ShowModal(view);
        }

        private void OnTutorialTipRequested(TutorialTipRequestedEvent e)
        {
            if (e.Tip == null || _tipQueue.Contains(e.Tip)) return;

            _tipQueue.Enqueue(e.Tip);
        }

        private void OnRemoveAdsRequested(RemoveAdsRequestedEvent e)
        {
            var view = ModalsSettings.Current.RemoveAdsView;
            if (view == null || Navigator == null || IsModalOpen(view)) return;

            _ = Navigator.ShowModal<bool>(view);
        }

        /// <summary>A test ad (Editor) draws nothing by itself: its screen covers everything until it ends.</summary>
        private void OnAdShowStarted(AdShowStartedEvent e)
        {
            var view = ModalsSettings.Current.SimulatedAdView;
            if (!e.Simulated || view == null || Navigator == null) return;

            _ = Navigator.ShowModal<bool>(view, e);
        }

        // ------------------------------------------------------------------------------------ navigation

        /// <summary>Makes <paramref name="screen"/> the target screen and navigates there as soon as possible.</summary>
        private void ShowScreen(ViewDefinition screen, object args = null)
        {
            // The simulation shows no screen, except the battle screen when it was asked to show the board.
            if (screen == null || (SimMode.IsActive && !(SimMode.ShowBoard && screen == gameScreen))) return;

            _targetScreen = screen;
            _targetArgs = args;
            _hasTarget = true;
            Reconcile();
        }

        private void OnNavigationChanged(ViewController previous, ViewController current)
        {
            Reconcile();
        }

        /// <summary>Moves toward the target screen when no transition is running, then opens waiting modals.</summary>
        private void Reconcile()
        {
            var navigator = Navigator;
            if (navigator == null || navigator.IsTransitioning) return;

            if (_hasTarget)
            {
                var current = navigator.Current;
                if (current != null && current.Definition == _targetScreen)
                {
                    // Already there: screens refresh themselves from their own events.
                    _hasTarget = false;
                }
                else
                {
                    var screen = _targetScreen;
                    var args = _targetArgs;
                    _hasTarget = false;

                    // The end states and the menu leave nothing open above them.
                    if (screen != mapScreen && screen != gameScreen)
                    {
                        CloseModals(navigator);
                        DropWaitingModals();
                    }

                    AudioManager.Play(ESfx.ScreenTransition);
                    _ = navigator.Replace(screen, args);
                    return;
                }
            }

            FlushPendingModals();
            FlushTalentOffer();
        }

        /// <summary>Opens a modal over the map: now if the map is showing, otherwise as soon as it is.</summary>
        private void ShowModalWhenOnMap(ViewDefinition definition, object args)
        {
            if (definition == null || SimMode.IsActive) return;

            _pendingModals.Add(new PendingModal(definition, args));
            FlushPendingModals();
        }

        private void FlushPendingModals()
        {
            var navigator = Navigator;
            if (_pendingModals.Count == 0 || navigator == null || navigator.IsTransitioning || _hasTarget ||
                _adSequenceRunning) return;

            var current = navigator.Current;
            if (current == null || current.Definition != mapScreen) return;

            var pending = _pendingModals.ToArray();
            _pendingModals.Clear();
            foreach (var modal in pending)
                _ = navigator.ShowModal(modal.Definition, modal.Args);
        }

        /// <summary>Opens the window of a talent offer that came in while the navigator was busy.</summary>
        private void FlushTalentOffer()
        {
            if (!_talentOfferPending) return;

            var navigator = Navigator;
            var view = ModalsSettings.Current.TalentChoiceView;
            if (navigator == null || view == null || navigator.IsTransitioning || _hasTarget) return;

            _talentOfferPending = false;

            // The offer may have been answered meanwhile (a test or a script), or a window may already show it.
            if (!TalentService.HasOffer || IsModalOpen<TalentChoiceController>()) return;

            _ = navigator.ShowModal(view, TalentService.CurrentOffer);
        }

        /// <summary>
        /// Shows the next tutorial tip when the screen allows it: the map or the battle, no transition running and
        /// no other modal open (the talent choice excepted when the settings allow tips over it).
        /// </summary>
        private void PumpTips()
        {
            if (_tipQueue.Count == 0 || _adSequenceRunning || MonetizationService.IsShowingAd) return;

            var navigator = Navigator;
            var view = ModalsSettings.Current.TutorialTipView;
            if (navigator == null || view == null || navigator.IsTransitioning || _hasTarget) return;

            var current = navigator.Current;
            if (current == null || (current.Definition != mapScreen && current.Definition != gameScreen)) return;

            var overTalents = ModalsSettings.Current.ShowTipsOverTalentChoice;
            foreach (var modal in navigator.Modals)
            {
                if (modal is TalentChoiceController && overTalents) continue;
                return;
            }

            // Tips already seen (or turned off in the options meanwhile) are dropped.
            while (_tipQueue.Count > 0)
            {
                var tip = _tipQueue.Dequeue();
                if (!ProfileService.TipsEnabled || TutorialService.HasSeen(tip)) continue;

                _ = navigator.ShowModal(view, tip);
                return;
            }
        }

        /// <summary>
        /// Shows the pending forced ad (GDD 7) when the screen allows it: the map, an end of run screen or the main
        /// menu (never the battle), no transition running, no modal open and nothing else waiting to open.
        /// </summary>
        private void PumpForcedAd()
        {
            if (_adSequenceRunning || !MonetizationService.HasPendingForcedAd) return;

            var navigator = Navigator;
            if (navigator == null || navigator.IsTransitioning || _hasTarget) return;

            var current = navigator.Current;
            if (current == null || !IsAdSafeScreen(current.Definition)) return;
            if (navigator.Modals.Count > 0 || _pendingModals.Count > 0 || _talentOfferPending || _tipQueue.Count > 0)
                return;

            RunForcedAd();
        }

        private bool IsAdSafeScreen(ViewDefinition screen) =>
            screen == mapScreen || screen == runEndScreen || screen == victoryScreen || screen == mainMenuScreen;

        /// <summary>The notice, the ad and, every few ads, the remove ads window.</summary>
        private async void RunForcedAd()
        {
            if (!MonetizationService.TryTakeForcedAd()) return;

            _adSequenceRunning = true;
            try
            {
                var modals = ModalsSettings.Current;
                var seconds = MonetizationSettings.Current.NoticeSeconds;
                if (modals.AdNoticeView != null && seconds > 0)
                    await Navigator.ShowModal<bool>(modals.AdNoticeView, seconds);

                var shown = await MonetizationService.ShowForcedAdAsync();
                if (shown && modals.RemoveAdsView != null && MonetizationService.TakeRemoveAdsOffer())
                    await Navigator.ShowModal<bool>(modals.RemoveAdsView);
            }
            finally
            {
                _adSequenceRunning = false;
                Reconcile();
            }
        }

        /// <summary>The run left the map and the battle: nothing queued for them is shown any more.</summary>
        private void DropWaitingModals()
        {
            _talentOfferPending = false;
            if (_tipQueue.Count == 0) return;

            // Tips not shown can be requested again later in this session.
            _tipQueue.Clear();
            TutorialService.ResetSession();
        }

        private bool IsModalOpen<T>() where T : ViewController
        {
            foreach (var modal in Navigator.Modals)
            {
                if (modal is T) return true;
            }

            return false;
        }

        private bool IsModalOpen(ViewDefinition definition)
        {
            foreach (var modal in Navigator.Modals)
            {
                if (modal.Definition == definition) return true;
            }

            return false;
        }

        private static void CloseModals(Navigator navigator)
        {
            foreach (var modal in navigator.Modals)
                navigator.CloseModal(modal);
        }

        private readonly struct PendingModal
        {
            public PendingModal(ViewDefinition definition, object args)
            {
                Definition = definition;
                Args = args;
            }

            public ViewDefinition Definition { get; }
            public object Args { get; }
        }
    }
}
