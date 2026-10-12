using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Monetization;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Screen of a test ad (Editor, GDD 7): opened by the flow on <see cref="AdShowStartedEvent"/> when the ad is
    /// simulated (the argument), it covers everything with the ad's kind and a countdown and closes on
    /// <see cref="AdShowFinishedEvent"/>. Close ends the ad early: a rewarded ad then gives no reward. On a device
    /// the LevelPlay SDK shows the real ad and this screen never opens.
    /// </summary>
    [Preserve]
    public sealed class SimulatedAdController : ModalController<bool>
    {
        private VisualElement _root;
        private Label _title;
        private Label _kind;
        private Label _countdown;
        private Label _note;
        private Button _closeButton;
        private EAdKind _adKind;
        private float _endTime;
        private bool _finished;
        private IVisualElementScheduledItem _tick;

        /// <summary>The "Close" button (ends the ad early).</summary>
        public Button CloseButton => _closeButton;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
            EventBus.Subscribe<AdShowFinishedEvent>(OnAdFinished);
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
            EventBus.Unsubscribe<AdShowFinishedEvent>(OnAdFinished);
            StopTick();
        }

        protected override void OnBind(VisualElement root)
        {
            _root = root;
            _title = root.Q<Label>("simulated-ad-title");
            _kind = root.Q<Label>("simulated-ad-kind");
            _countdown = root.Q<Label>("simulated-ad-countdown");
            _note = root.Q<Label>("simulated-ad-note");
            _closeButton = root.Q<Button>("simulated-ad-close-button");
            _closeButton.OnClick(() => MonetizationService.FinishSimulatedAd(false));

            // The ad covers the game: nothing behind it reacts.
            root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Render();
        }

        protected override void OnEnter(object args)
        {
            _finished = false;
            var started = args as AdShowStartedEvent;
            _adKind = started != null ? started.Kind : EAdKind.Interstitial;
            _endTime = Time.realtimeSinceStartup + (started != null ? started.Seconds : 0f);
            StopTick();
            _tick = _root?.schedule.Execute(Render).Every(100);
            Render();

            // The ad may have ended before the screen opened.
            if (!MonetizationService.IsShowingAd)
                Finish();
        }

        protected override void OnExit()
        {
            StopTick();
        }

        /// <summary>The back action closes the ad early, like the button.</summary>
        protected override bool OnBack()
        {
            MonetizationService.FinishSimulatedAd(false);
            return true;
        }

        private void OnAdFinished(AdShowFinishedEvent e)
        {
            Finish();
        }

        private void Finish()
        {
            if (_finished) return;

            _finished = true;
            StopTick();
            Close(true);
        }

        private void StopTick()
        {
            _tick?.Pause();
            _tick = null;
        }

        private void Render()
        {
            if (_title == null) return;

            _title.text = HudText.Get("ads.simulated.title");
            _kind.text = _adKind == EAdKind.Rewarded
                ? HudText.Get("ads.simulated.rewarded")
                : HudText.Get("ads.simulated.interstitial");
            _countdown.text = HudText.Format("ads.simulated.countdown",
                Mathf.Max(0, Mathf.CeilToInt(_endTime - Time.realtimeSinceStartup)));
            _note.text = HudText.Get("ads.simulated.note");
            _closeButton.text = HudText.Get("ads.simulated.close");
        }
    }
}
