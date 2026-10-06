using GridBattle.Core;
using GridBattle.Gameplay.Meta;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Options (interface 4.3, GDD 5.2 and 8.1): language (automatic, English, Spanish, Brazilian Portuguese), music
    /// and effects volume, and the tutorial tips (on/off when <see cref="MetaSettings.AllowDisablingTips"/> allows
    /// it, plus "show tips again"). Everything goes through <see cref="ProfileService"/>: the language applies at
    /// once and every open view redraws from <see cref="Loc.LocaleChanged"/>; a volume is applied while the
    /// slider is dragged and written to the profile when it is released (or when the window closes). Opened by
    /// the flow on <c>OptionsRequestedEvent</c>; closes with its X button or the back action.
    /// </summary>
    [Preserve]
    public sealed class OptionsController : ViewController
    {
        private const string ActiveClass = "option-choice--active";

        private static readonly string[] LanguageCodes = { null, "en", "es", "pt-BR" };
        private static readonly string[] LanguageNameKeys =
        {
            "options.language.auto", "options.language.en", "options.language.es", "options.language.pt",
        };

        private Label _title;
        private Label _languageLabel;
        private Label _musicLabel;
        private Label _sfxLabel;
        private Label _musicValue;
        private Label _sfxValue;
        private Label _tipsLabel;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Button _tipsToggle;
        private Button _tipsReset;
        private readonly Button[] _languageButtons = new Button[4];
        private bool _musicDirty;
        private bool _sfxDirty;

        public Slider MusicSlider => _musicSlider;
        public Slider SfxSlider => _sfxSlider;
        public Button TipsToggle => _tipsToggle;
        public Button TipsResetButton => _tipsReset;

        /// <summary>Button of a language: 0 = automatic, 1 = English, 2 = Spanish, 3 = Brazilian Portuguese.</summary>
        public Button GetLanguageButton(int index) => _languageButtons[index];

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
            CommitVolumes();
        }

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("options-title");
            _languageLabel = root.Q<Label>("options-language-label");
            _musicLabel = root.Q<Label>("options-music-label");
            _sfxLabel = root.Q<Label>("options-sfx-label");
            _musicValue = root.Q<Label>("options-music-value");
            _sfxValue = root.Q<Label>("options-sfx-value");
            _tipsLabel = root.Q<Label>("options-tips-label");
            root.Q<ScrollView>("options-scroll").HideScrollerWhenContentFits();
            _musicSlider = root.Q<Slider>("options-music-slider");
            _sfxSlider = root.Q<Slider>("options-sfx-slider");
            _tipsToggle = root.Q<Button>("options-tips-toggle");
            _tipsReset = root.Q<Button>("options-tips-reset");

            _languageButtons[0] = root.Q<Button>("options-lang-auto");
            _languageButtons[1] = root.Q<Button>("options-lang-en");
            _languageButtons[2] = root.Q<Button>("options-lang-es");
            _languageButtons[3] = root.Q<Button>("options-lang-pt");
            for (var i = 0; i < _languageButtons.Length; i++)
            {
                var code = LanguageCodes[i];
                _languageButtons[i].OnClick(() => OnLanguageClicked(code));
            }

            BindSlider(_musicSlider, true);
            BindSlider(_sfxSlider, false);

            _tipsToggle.OnClick(OnTipsToggleClicked);
            _tipsReset.OnClick(OnTipsResetClicked);
            root.Q<Button>("options-close-button").OnClick(OnCloseClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        protected override void OnExit()
        {
            CommitVolumes();
        }

        /// <summary>The back action closes the window.</summary>
        protected override bool OnBack()
        {
            OnCloseClicked();
            return true;
        }

        /// <summary>Writes the volumes changed by a drag to the profile (also called when a slider is released).</summary>
        public void CommitVolumes()
        {
            if (_musicDirty)
            {
                _musicDirty = false;
                ProfileService.SetMusicVolume(_musicSlider.value, true);
            }

            if (_sfxDirty)
            {
                _sfxDirty = false;
                ProfileService.SetSfxVolume(_sfxSlider.value, true);
            }
        }

        private void BindSlider(Slider slider, bool music)
        {
            slider.lowValue = 0f;
            slider.highValue = 1f;
            slider.RegisterValueChangedCallback(e => OnVolumeChanged(music, e.newValue));

            // Released: write the profile (the pointer capture also ends when the finger leaves the window).
            slider.RegisterCallback<PointerUpEvent>(_ => OnVolumeReleased(music), TrickleDown.TrickleDown);
            slider.RegisterCallback<PointerCaptureOutEvent>(_ => OnVolumeReleased(music));
        }

        private void OnVolumeChanged(bool music, float value)
        {
            if (music)
            {
                _musicDirty = true;
                ProfileService.SetMusicVolume(value, false);
                _musicValue.text = FormatPercent(value);
            }
            else
            {
                _sfxDirty = true;
                ProfileService.SetSfxVolume(value, false);
                _sfxValue.text = FormatPercent(value);
            }
        }

        private void OnVolumeReleased(bool music)
        {
            var dirty = music ? _musicDirty : _sfxDirty;
            CommitVolumes();

            // Let the player hear the new effects level.
            if (!music && dirty)
                AudioManager.Play(ESfx.ButtonTap);
        }

        private void OnLanguageClicked(string code)
        {
            AudioManager.Play(ESfx.ButtonTap);

            // Applies the locale; every open view redraws from Loc.LocaleChanged (including this one).
            ProfileService.SetLocale(code);
            Render();
        }

        private void OnTipsToggleClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            ProfileService.SetTipsEnabled(!ProfileService.TipsEnabled);
            Render();
        }

        private void OnTipsResetClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            ProfileService.ResetTipsSeen();

            // Tips requested earlier in this session may show again.
            TutorialService.ResetSession();
            Render();
        }

        private void OnCloseClicked()
        {
            AudioManager.Play(ESfx.WindowClose);
            CommitVolumes();
            Close();
        }

        private void Render()
        {
            if (_title == null) return;

            _title.text = HudText.Get("menu.options");
            _languageLabel.text = HudText.Get("options.language");
            _musicLabel.text = HudText.Get("options.music");
            _sfxLabel.text = HudText.Get("options.sfx");
            _tipsLabel.text = HudText.Get("options.tips");

            var current = ProfileService.LocaleCode;
            for (var i = 0; i < _languageButtons.Length; i++)
            {
                _languageButtons[i].text = HudText.Get(LanguageNameKeys[i]);
                _languageButtons[i].EnableInClassList(ActiveClass, LanguageCodes[i] == current);
            }

            _musicSlider.SetValueWithoutNotify(ProfileService.MusicVolume);
            _sfxSlider.SetValueWithoutNotify(ProfileService.SfxVolume);
            _musicValue.text = FormatPercent(ProfileService.MusicVolume);
            _sfxValue.text = FormatPercent(ProfileService.SfxVolume);

            var canDisable = MetaSettings.Current.AllowDisablingTips;
            _tipsToggle.SetDisplayed(canDisable);
            _tipsToggle.text = HudText.Get(ProfileService.TipsEnabled ? "options.tips_on" : "options.tips_off");
            _tipsToggle.EnableInClassList(ActiveClass, ProfileService.TipsEnabled);
            _tipsReset.text = HudText.Get("options.tips_reset");
            _tipsReset.SetEnabled(ProfileService.State.TipsSeen.Count > 0);
        }

        private static string FormatPercent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    }
}
