using GridBattle.Core;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// What a <see cref="ConfirmOverlayController"/> asks. The texts are already localized; empty button texts
    /// fall back to the default "Yes" and "No".
    /// </summary>
    public sealed class ConfirmRequest
    {
        public ConfirmRequest(string title, string message, string confirmText = null, string cancelText = null)
        {
            Title = title;
            Message = message;
            ConfirmText = confirmText;
            CancelText = cancelText;
        }

        public string Title { get; }
        public string Message { get; }
        public string ConfirmText { get; }
        public string CancelText { get; }
    }

    /// <summary>
    /// Yes/no confirmation modal: <c>var ok = await navigator.ShowModal&lt;bool&gt;(view, new ConfirmRequest(...))</c>.
    /// Only its two buttons (or the back action, which means "no") close it; the dim background swallows touches.
    /// </summary>
    [Preserve]
    public sealed class ConfirmOverlayController : ModalController<bool>
    {
        private ConfirmRequest _request;
        private Label _title;
        private Label _message;
        private Button _confirmButton;
        private Button _cancelButton;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
        }

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("confirm-title");
            _message = root.Q<Label>("confirm-message");
            _confirmButton = root.Q<Button>("confirm-yes-button");
            _cancelButton = root.Q<Button>("confirm-no-button");

            _confirmButton.OnClick(() => Answer(true));
            _cancelButton.OnClick(() => Answer(false));

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _request = args as ConfirmRequest;
            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        /// <summary>The back action answers "no".</summary>
        protected override bool OnBack()
        {
            Answer(false);
            return true;
        }

        private void Answer(bool confirmed)
        {
            AudioManager.Play(confirmed ? ESfx.ButtonTap : ESfx.WindowClose);
            Close(confirmed);
        }

        private void Render()
        {
            if (_title == null) return;

            _title.text = _request != null ? _request.Title : string.Empty;
            _message.text = _request != null ? _request.Message : string.Empty;

            var yes = _request != null ? _request.ConfirmText : null;
            var no = _request != null ? _request.CancelText : null;
            _confirmButton.text = string.IsNullOrEmpty(yes) ? HudText.Get("confirm.yes") : yes;
            _cancelButton.text = string.IsNullOrEmpty(no) ? HudText.Get("confirm.no") : no;
        }
    }
}
