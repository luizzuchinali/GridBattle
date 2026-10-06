using System;
using GridBattle.Core;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Hud;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Pause menu (interface 4.3): Continue, Glossary, Options and Give up (only while a run is active, after a
    /// confirmation; the run manager then ends the run and the flow shows the run-end screen). Tapping outside the
    /// panel also closes it. While it is open the turn flow is paused (<see cref="TurnBlockers"/>) and released
    /// when it closes. The glossary and the options open above it through their request events.
    /// </summary>
    [Preserve]
    public sealed class MenuOverlayController : ViewController
    {
        private const string BlockerReason = "Pause menu";

        private IDisposable _blocker;
        private bool _askingGiveUp;
        private Label _title;
        private Button _continueButton;
        private Button _glossaryButton;
        private Button _optionsButton;
        private Button _giveUpButton;

        /// <summary>The Give up button (hidden when no run is active).</summary>
        public Button GiveUpButton => _giveUpButton;

        public Button ContinueButton => _continueButton;
        public Button GlossaryButton => _glossaryButton;
        public Button OptionsButton => _optionsButton;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
            ReleaseBlocker();
        }

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("menu-title");
            _continueButton = root.Q<Button>("menu-continue-button");
            _glossaryButton = root.Q<Button>("menu-glossary-button");
            _optionsButton = root.Q<Button>("menu-options-button");
            _giveUpButton = root.Q<Button>("menu-giveup-button");

            _continueButton.OnClick(OnContinueClicked);
            _glossaryButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new GlossaryRequestedEvent());
            });
            _optionsButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new OptionsRequestedEvent());
            });
            _giveUpButton.OnClick(OnGiveUpClicked);

            // Tapping outside the window closes the menu.
            var overlayBackground = root.Q<VisualElement>("overlay-background");
            overlayBackground.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.target == overlayBackground)
                    CloseMenu();
            });

            Render();
        }

        protected override void OnEnter(object args)
        {
            _askingGiveUp = false;
            if (_blocker == null)
                _blocker = TurnBlockers.Acquire(BlockerReason);

            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        protected override void OnExit()
        {
            // The game resumes as soon as the menu starts closing.
            ReleaseBlocker();
        }

        /// <summary>The back action closes the menu.</summary>
        protected override bool OnBack()
        {
            CloseMenu();
            return true;
        }

        private void OnContinueClicked()
        {
            CloseMenu();
        }

        private void CloseMenu()
        {
            if (_askingGiveUp) return;

            AudioManager.Play(ESfx.WindowClose);
            ReleaseBlocker();
            Close();
        }

        private async void OnGiveUpClicked()
        {
            var runs = RunManager.Instance;
            if (_askingGiveUp || runs == null || !runs.IsRunActive) return;

            AudioManager.Play(ESfx.ButtonTap);
            var settings = ModalsSettings.Current;
            if (settings.ConfirmGiveUp && settings.ConfirmView != null)
            {
                _askingGiveUp = true;
                bool confirmed;
                try
                {
                    var request = new ConfirmRequest(HudText.Get("menu.give_up.title"), HudText.Get("menu.give_up.message"));
                    confirmed = await Context.Navigator.ShowModal<bool>(settings.ConfirmView, request);
                }
                finally
                {
                    _askingGiveUp = false;
                }

                if (!confirmed) return;
            }

            // The run ends: the flow closes the modals and shows the run-end screen. The pause is released first.
            if (!runs.IsRunActive) return;

            ReleaseBlocker();
            Close();
            runs.GiveUp();
        }

        private void ReleaseBlocker()
        {
            _blocker?.Dispose();
            _blocker = null;
        }

        private void Render()
        {
            if (_title == null) return;

            var runs = RunManager.Instance;
            _title.text = HudText.Get("menu.pause.title");
            _continueButton.text = HudText.Get("menu.continue");
            _glossaryButton.text = HudText.Get("menu.glossary");
            _optionsButton.text = HudText.Get("menu.options");
            _giveUpButton.text = HudText.Get("menu.give_up");
            _giveUpButton.SetDisplayed(runs != null && runs.IsRunActive);
        }
    }
}
