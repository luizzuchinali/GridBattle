using GridBattle.Core;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Meta;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using GridBattle.UI.Screens;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Character details (interface 4.3): opened from a character card of the class selection with the class's
    /// <see cref="PlayerCharacterConfig"/> as argument. Shows the portrait, name and unlock state, the class
    /// description, its base attributes and its starting skills. Answers <c>true</c> when the player taps Play (a new
    /// run with this class), <c>false</c> on Back or the back action. A locked class shows its unlock progress and
    /// has no Play button.
    /// </summary>
    [Preserve]
    public sealed class CharacterDetailsController : ModalController<bool>
    {
        private static readonly Color LockedTint = new(0.2f, 0.2f, 0.2f);

        private PlayerCharacterConfig _config;
        private VisualElement _header;
        private ScrollView _scroll;
        private Button _playButton;
        private Button _backButton;

        /// <summary>The class shown (null before the window opens).</summary>
        public PlayerCharacterConfig Config => _config;

        /// <summary>Whether the class shown is still locked (no Play button).</summary>
        public bool IsLocked => _config != null && !ProfileService.IsClassUnlocked(_config);

        public Button PlayButton => _playButton;
        public Button BackButton => _backButton;

        /// <summary>The scrollable sheet (for tests).</summary>
        public VisualElement Content => _scroll != null ? _scroll.contentContainer : null;

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
            _header = root.Q<VisualElement>("character-header");
            _scroll = root.Q<ScrollView>("character-scroll");
            _playButton = root.Q<Button>("character-play-button");
            _backButton = root.Q<Button>("character-back-button");

            _playButton.OnClick(() =>
            {
                if (!IsLocked) Answer(true);
            });
            _backButton.OnClick(() => Answer(false));

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _config = args as PlayerCharacterConfig;
            Render();
            if (_scroll != null)
                _scroll.scrollOffset = Vector2.zero;
            AudioManager.Play(ESfx.WindowOpen);
        }

        /// <summary>The back action answers "no".</summary>
        protected override bool OnBack()
        {
            Answer(false);
            return true;
        }

        private void Answer(bool play)
        {
            AudioManager.Play(play ? ESfx.ButtonTap : ESfx.WindowClose);
            Close(play);
        }

        private void Render()
        {
            if (_header == null) return;

            _header.Clear();
            var content = _scroll.contentContainer;
            content.Clear();
            _backButton.text = HudText.Get("menu.back");
            _playButton.text = HudText.Get("character.play");
            if (_config == null) return;

            var locked = IsLocked;
            BuildHeader(locked);

            var description = _config.GetDescription();
            if (!string.IsNullOrEmpty(description))
                SummaryRows.AddText(content, description);

            SummaryRows.AddSection(content, HudText.Get("details.attributes"));
            SummaryRows.AddBaseAttributes(content, _config);
            SummaryRows.AddText(content, HudText.Get("character.base_values"));

            SummaryRows.AddSection(content, HudText.Get("character.starting_skills"));
            SummaryRows.AddSkills(content, _config.Skills);

            _playButton.SetDisplayed(!locked);
        }

        private void BuildHeader(bool locked)
        {
            var image = new Image { sprite = _config.Sprite, pickingMode = PickingMode.Ignore };
            image.AddToClassList("character-header__image");
            if (locked)
                image.tintColor = LockedTint;
            _header.Add(image);

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("character-header__texts");

            var name = new Label(_config.GetDisplayName()) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("character-header__name");
            texts.Add(name);

            if (locked)
            {
                var (won, required) = ProfileService.GetUnlockProgress(_config);
                var status = new Label(HudText.Format("glossary.locked_progress", won, required))
                    { pickingMode = PickingMode.Ignore };
                status.AddToClassList("character-header__status");
                texts.Add(status);
            }

            _header.Add(texts);
        }
    }
}
