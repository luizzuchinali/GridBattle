using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Main menu (interface 4.3), in two states of the same view:
    /// <list type="bullet">
    /// <item><b>Title</b> (when the menu opens): New game, Continue (only with a run in progress that can resume,
    /// <see cref="RunManager.HasContinuableRun"/>), Glossary and Options.</item>
    /// <item><b>Class selection</b> (after New game): one card per class and Back. Tapping a card raises
    /// <see cref="CharacterDetailsRequestedEvent"/>; the details window shows the class and starts the run with
    /// Play. Classes that are not unlocked yet look locked and list their progress ("Mage: 7/15 battles").</item>
    /// </list>
    /// The two states share one view because the class cards are animated by the layer's Animator through fixed
    /// element paths (<c>#container/#character-selection/...</c>), which a page host would break.
    /// </summary>
    [Preserve]
    public sealed class MainMenuScreenController : ViewController
    {
        private const string LockedClass = "portrait-button--locked";
        private const string LockClass = "portrait-lock";

        private readonly List<ClassSlot> _slots = new();
        private Label _titleLabel;
        private VisualElement _characterSelection;
        private VisualElement _lockedInfo;
        private VisualElement _menuButtons;
        private Button _newGameButton;
        private Button _continueButton;
        private Button _glossaryButton;
        private Button _optionsButton;
        private Button _backButton;
        private bool _selectingClass;

        /// <summary>The "New game" button (null until the UI loads).</summary>
        public Button NewGameButton => _newGameButton;

        /// <summary>The "Continue" button (null until the UI loads).</summary>
        public Button ContinueButton => _continueButton;

        /// <summary>The "Back" button of the class selection (null until the UI loads).</summary>
        public Button BackButton => _backButton;

        /// <summary>Whether the class selection is showing (false: the title menu).</summary>
        public bool IsSelectingClass => _selectingClass;

        /// <summary>Whether the class of the button is shown locked.</summary>
        public bool IsClassLocked(ECharacter character)
        {
            foreach (var slot in _slots)
            {
                if (slot.Character == character)
                    return slot.Locked;
            }

            return false;
        }

        /// <summary>Shows the class selection (what New game does).</summary>
        public void ShowClassSelection()
        {
            _selectingClass = true;
            ApplyState();
        }

        /// <summary>Shows the title menu (what Back does).</summary>
        public void ShowTitle()
        {
            _selectingClass = false;
            ApplyState();
        }

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Refresh;
            EventBus.Subscribe<ProfileChangedEvent>(OnProfileChanged);
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Refresh;
            EventBus.Unsubscribe<ProfileChangedEvent>(OnProfileChanged);
        }

        protected override void OnBind(VisualElement root)
        {
            _slots.Clear();
            _titleLabel = root.Q<Label>("select-class-label");
            _characterSelection = root.Q<VisualElement>("character-selection");
            _lockedInfo = root.Q<VisualElement>("locked-info");
            _menuButtons = root.Q<VisualElement>("menu-buttons");

            BindClass(root, "knight-btn", ECharacter.Warrior);
            BindClass(root, "mage-btn", ECharacter.Mage);
            BindClass(root, "rogue-btn", ECharacter.Rogue);

            _newGameButton = root.Q<Button>("new-game-btn");
            _newGameButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                ShowClassSelection();
            });

            _continueButton = root.Q<Button>("continue-btn");
            _continueButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new ContinueRunRequestedEvent());
            });

            _glossaryButton = root.Q<Button>("glossary-btn");
            _glossaryButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new GlossaryRequestedEvent());
            });

            _optionsButton = root.Q<Button>("options-btn");
            _optionsButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new OptionsRequestedEvent());
            });

            _backButton = root.Q<Button>("back-btn");
            _backButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                ShowTitle();
            });

            Refresh();
        }

        protected override void OnEnter(object args)
        {
            // The menu always opens on the title. It is shown again after every run: the save, the unlocks and the
            // progress may have changed.
            _selectingClass = false;
            Refresh();
        }

        /// <summary>The back action leaves the class selection for the title menu.</summary>
        protected override bool OnBack()
        {
            if (!_selectingClass) return false;

            ShowTitle();
            return true;
        }

        private void OnProfileChanged(ProfileChangedEvent e)
        {
            Refresh();
        }

        private void BindClass(VisualElement root, string buttonName, ECharacter character)
        {
            var button = root.Q<Button>(buttonName);
            if (button == null) return;

            var slot = new ClassSlot { Character = character, Button = button };
            _slots.Add(slot);

            // Locked classes open their details too: the window shows how to unlock them (and has no Play button).
            button.RegisterCallback<ClickEvent>(_ =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new CharacterDetailsRequestedEvent(character));
            });
        }

        /// <summary>Applies the save, the profile and the language to every element of the menu.</summary>
        private void Refresh()
        {
            if (_titleLabel == null) return;

            _titleLabel.text = HudText.Get("menu.select_class");
            _newGameButton.text = HudText.Get("menu.new_game");
            _continueButton.text = HudText.Get("menu.continue");
            _glossaryButton.text = HudText.Get("menu.glossary");
            _optionsButton.text = HudText.Get("menu.options");
            _backButton.text = HudText.Get("menu.back");

            var runs = RunManager.Instance;
            _continueButton.SetDisplayed(runs != null && runs.HasContinuableRun);

            _lockedInfo.Clear();
            var manager = GameStateManager.Instance;
            foreach (var slot in _slots)
            {
                var config = manager != null ? manager.FindPlayableCharacter(slot.Character) : null;
                slot.Locked = config != null && !ProfileService.IsClassUnlocked(config);
                slot.Button.EnableInClassList(LockedClass, slot.Locked);
                SetLockIcon(slot);

                if (!slot.Locked) continue;

                var (won, required) = ProfileService.GetUnlockProgress(config);
                var line = new Label(HudText.Format("menu.unlock_progress", config.GetDisplayName(), won, required));
                line.AddToClassList("locked-info__line");
                _lockedInfo.Add(line);
            }

            ApplyState();
        }

        /// <summary>Shows the elements of the current state (title menu or class selection) and hides the others.</summary>
        private void ApplyState()
        {
            if (_titleLabel == null) return;

            _menuButtons.SetDisplayed(!_selectingClass);
            _titleLabel.SetDisplayed(_selectingClass);
            _characterSelection.SetDisplayed(_selectingClass);
            _backButton.SetDisplayed(_selectingClass);
            _lockedInfo.SetDisplayed(_selectingClass && _lockedInfo.childCount > 0);
        }

        private static void SetLockIcon(ClassSlot slot)
        {
            var existing = slot.Button.Q<Image>(className: LockClass);
            if (!slot.Locked)
            {
                existing?.RemoveFromHierarchy();
                return;
            }

            if (existing != null) return;

            var icon = new Image { sprite = ScreensSettings.Current.LockIcon, pickingMode = PickingMode.Ignore };
            icon.AddToClassList(LockClass);
            slot.Button.Add(icon);
        }

        private sealed class ClassSlot
        {
            public ECharacter Character;
            public Button Button;
            public bool Locked;
        }
    }
}
