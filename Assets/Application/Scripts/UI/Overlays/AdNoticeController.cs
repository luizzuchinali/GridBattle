using GridBattle.Core;
using GridBattle.Gameplay.Monetization;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Notice before a forced ad (GDD 7): "Ad break, starting in 3...". The argument is the number of seconds; it
    /// closes by itself (result true) when the countdown ends, in real time, and cannot be closed by the player.
    /// The hint about the remove ads purchase shows only while the purchase can be made.
    /// </summary>
    [Preserve]
    public sealed class AdNoticeController : ModalController<bool>
    {
        private VisualElement _root;
        private Label _title;
        private Label _countdown;
        private Label _hint;
        private float _endTime;
        private IVisualElementScheduledItem _tick;

        /// <summary>Whole seconds left on the countdown.</summary>
        public int SecondsLeft => Mathf.Max(0, Mathf.CeilToInt(_endTime - Time.realtimeSinceStartup));

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
            StopTick();
        }

        protected override void OnBind(VisualElement root)
        {
            _root = root;
            _title = root.Q<Label>("ad-notice-title");
            _countdown = root.Q<Label>("ad-notice-countdown");
            _hint = root.Q<Label>("ad-notice-hint");

            // The dim background swallows every pointer event: nothing behind the window reacts.
            root.Q<VisualElement>("overlay-background").RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Render();
        }

        protected override void OnEnter(object args)
        {
            var seconds = args is int value ? value : 3;
            _endTime = Time.realtimeSinceStartup + Mathf.Max(0, seconds);
            StopTick();
            _tick = _root?.schedule.Execute(Tick).Every(100);
            Render();
        }

        protected override void OnExit()
        {
            StopTick();
        }

        /// <summary>The back action does nothing: the notice ends by itself.</summary>
        protected override bool OnBack() => true;

        private void Tick()
        {
            if (SecondsLeft > 0)
            {
                Render();
                return;
            }

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

            _title.text = HudText.Get("ads.notice.title");
            _countdown.text = HudText.Format("ads.notice.countdown", Mathf.Max(1, SecondsLeft));
            _hint.text = HudText.Get("ads.notice.hint");
            _hint.SetDisplayed(MonetizationService.CanOfferRemoveAds);
        }
    }
}
