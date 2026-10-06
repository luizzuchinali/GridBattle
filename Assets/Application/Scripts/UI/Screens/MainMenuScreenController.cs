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
    /// Main menu and class selection (interface 4.3): each portrait button raises CharacterChoosenEvent. Classes
    /// that are not unlocked yet look locked, show their progress ("Mage: 7/15 battles") and cannot be picked.
    /// "Continue" resumes the saved run, "Glossary" and "Options" raise requests for their modals.
    /// </summary>
    [Preserve]
    public sealed class MainMenuScreenController : ViewController
    {
        private const string LockedClass = "portrait-button--locked";
        private const string LockClass = "portrait-lock";
        private const int WobbleStepMs = 45;

        private readonly List<ClassSlot> _slots = new();
        private Label _titleLabel;
        private VisualElement _lockedInfo;
        private Button _continueButton;
        private Button _glossaryButton;
        private Button _optionsButton;

        /// <summary>The "Continue" button (null until the UI loads).</summary>
        public Button ContinueButton => _continueButton;

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
            _lockedInfo = root.Q<VisualElement>("locked-info");

            BindClass(root, "knight-btn", ECharacter.Warrior);
            BindClass(root, "mage-btn", ECharacter.Mage);
            BindClass(root, "rogue-btn", ECharacter.Rogue);

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

            Refresh();
        }

        protected override void OnEnter(object args)
        {
            // The menu is shown again after every run: the save, the unlocks and the progress may have changed.
            Refresh();
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

            button.RegisterCallback<ClickEvent>(_ =>
            {
                if (slot.Locked)
                {
                    AudioManager.Play(ESfx.ButtonTap);
                    Wobble(button);
                    return;
                }

                EventBus.Raise(new CharacterChoosenEvent(character));
            });
        }

        /// <summary>Applies the save, the profile and the language to every element of the menu.</summary>
        private void Refresh()
        {
            if (_titleLabel == null) return;

            _titleLabel.text = HudText.Get("menu.select_class");
            _continueButton.text = HudText.Get("menu.continue");
            _glossaryButton.text = HudText.Get("menu.glossary");
            _optionsButton.text = HudText.Get("menu.options");

            var runs = RunManager.Instance;
            _continueButton.SetDisplayed(runs != null && runs.HasSavedRun);

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

            _lockedInfo.SetDisplayed(_lockedInfo.childCount > 0);
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

        /// <summary>Small shake of a locked class that was tapped.</summary>
        private static void Wobble(VisualElement element)
        {
            var offsets = new[] { -2f, 2f, -1f, 1f, 0f };
            var index = 0;
            element.schedule.Execute(() =>
            {
                element.style.translate = new Translate(offsets[index], 0);
                index++;
            }).Every(WobbleStepMs).Until(() => index >= offsets.Length);
        }

        private sealed class ClassSlot
        {
            public ECharacter Character;
            public Button Button;
            public bool Locked;
        }
    }
}
