using System;
using GridBattle.Core;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// A contextual tutorial tip (interface 4.4): a small window with the tip's name as the title, its description
    /// as the text and an OK button. Opened by the flow with a <see cref="TutorialTipDefinition"/> as argument; OK
    /// (or the back action) marks the tip as seen in the profile so it never shows again. While it is open the
    /// turn flow is paused (<see cref="TurnBlockers"/>), so the game does not move behind it.
    /// </summary>
    [Preserve]
    public sealed class TutorialTipController : ViewController
    {
        private const string BlockerReason = "Tutorial tip";

        private TutorialTipDefinition _tip;
        private IDisposable _blocker;
        private Label _title;
        private Label _text;
        private Image _icon;
        private Button _okButton;

        /// <summary>The tip on screen.</summary>
        public TutorialTipDefinition Tip => _tip;

        public Button OkButton => _okButton;

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
            _title = root.Q<Label>("tip-title");
            _text = root.Q<Label>("tip-text");
            _icon = root.Q<Image>("tip-icon");
            _okButton = root.Q<Button>("tip-ok-button");

            _okButton.OnClick(OnOkClicked);

            // The dim background swallows every pointer event: the tip is answered with its button.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _tip = args as TutorialTipDefinition;
            if (_blocker == null)
                _blocker = TurnBlockers.Acquire(BlockerReason);

            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        protected override void OnExit()
        {
            // The game resumes as soon as the window starts closing.
            ReleaseBlocker();
        }

        /// <summary>The back action answers the tip like the OK button.</summary>
        protected override bool OnBack()
        {
            OnOkClicked();
            return true;
        }

        private void OnOkClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            if (_tip != null)
                TutorialService.MarkSeen(_tip);

            ReleaseBlocker();
            Close();
        }

        private void ReleaseBlocker()
        {
            _blocker?.Dispose();
            _blocker = null;
        }

        private void Render()
        {
            if (_title == null) return;

            _title.text = _tip != null ? _tip.GetDisplayName() : string.Empty;
            _text.text = _tip != null ? _tip.GetDescription() : string.Empty;
            _okButton.text = HudText.Get("tip.ok");

            var icon = _tip != null ? _tip.Icon : null;
            _icon.sprite = icon;
            _icon.EnableInClassList("tip-icon--hidden", icon == null);
        }
    }
}
