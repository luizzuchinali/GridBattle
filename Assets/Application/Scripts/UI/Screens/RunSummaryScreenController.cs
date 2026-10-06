using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Shared logic of the end-of-run screens (interface 4.3): the title, the class and level, the run summary
    /// (depth reached, battles won, enemies defeated), the build (talents with icons and skills), the progress
    /// toward the classes still locked and the classes unlocked by this run. The run summary arrives as the
    /// navigation argument (<see cref="RunSummary"/>). The classes unlocked are read (and so announced) here,
    /// once, with <c>ProfileService.ConsumeNewlyUnlockedClasses</c>.
    /// </summary>
    public abstract class RunSummaryScreenController : ViewController
    {
        private RunSummary _summary;
        private List<PlayerCharacterConfig> _unlocked = new();
        private Label _title;
        private Label _subtitle;
        private Image _portrait;
        private Label _className;
        private Label _classLevel;
        private ScrollView _scroll;
        private Button _menuButton;

        /// <summary>The summary shown (null until the screen is entered).</summary>
        public RunSummary Summary => _summary;

        /// <summary>The classes this run unlocked (announced on this screen).</summary>
        public IReadOnlyList<PlayerCharacterConfig> NewlyUnlocked => _unlocked;

        /// <summary>The "Main menu" button (null until the UI loads).</summary>
        public Button MenuButton => _menuButton;

        /// <summary>Key (UI table) of the title for <paramref name="summary"/>.</summary>
        protected abstract string GetTitleKey(RunSummary summary);

        /// <summary>Key (UI table) of the line under the title.</summary>
        protected abstract string GetSubtitleKey(RunSummary summary);

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
            _title = root.Q<Label>("summary-title");
            _subtitle = root.Q<Label>("summary-subtitle");
            _portrait = root.Q<Image>("summary-portrait");
            _className = root.Q<Label>("summary-class");
            _classLevel = root.Q<Label>("summary-level");
            _scroll = root.Q<ScrollView>("summary-scroll");
            _menuButton = root.Q<Button>("summary-menu-button");

            _menuButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new MainMenuRequestedEvent());
            });

            Render();
        }

        protected override void OnEnter(object args)
        {
            _summary = args as RunSummary;

            // Announce the unlocks once: the profile forgets them as soon as they are read.
            _unlocked = ProfileService.ConsumeNewlyUnlockedClasses();
            if (_unlocked.Count > 0)
                AudioManager.Play(ESfx.ClassUnlocked);

            Render();
        }

        /// <summary>The back action does nothing here: the only way out is the main menu button.</summary>
        protected override bool OnBack()
        {
            return true;
        }

        private void Render()
        {
            if (_title == null) return;

            var summary = _summary;
            _title.text = summary != null ? HudText.Get(GetTitleKey(summary)) : string.Empty;
            _subtitle.text = summary != null ? HudText.Get(GetSubtitleKey(summary)) : string.Empty;
            _menuButton.text = HudText.Get("run_end.menu");

            var playerClass = summary != null ? summary.PlayerClass : null;
            _portrait.sprite = playerClass != null ? playerClass.Sprite : null;
            _portrait.style.display = playerClass != null && playerClass.Sprite != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _className.text = playerClass != null ? playerClass.GetDisplayName() : string.Empty;
            _classLevel.text = summary != null ? HudText.Format("run_end.class_level", summary.Level) : string.Empty;

            var content = _scroll.contentContainer;
            content.Clear();
            if (summary != null)
            {
                BuildUnlocks(content);
                BuildRun(content, summary);
                BuildBuild(content, summary);
                BuildClassProgress(content);
            }

            _scroll.scrollOffset = Vector2.zero;
        }

        private void BuildUnlocks(VisualElement content)
        {
            foreach (var unlocked in _unlocked)
            {
                var banner = new Label(HudText.Format("run_end.unlocked", unlocked.GetDisplayName()));
                banner.AddToClassList("summary-banner");
                content.Add(banner);
            }
        }

        private static void BuildRun(VisualElement content, RunSummary summary)
        {
            SummaryRows.AddSection(content, HudText.Get("run_end.section.run"));
            SummaryRows.AddRow(content, HudText.Get("run_end.depth"),
                HudText.Format("run_end.depth_value", summary.Depth, summary.FloorCount));
            SummaryRows.AddRow(content, HudText.Get("run_end.battles"), summary.BattlesWon.ToString());
            SummaryRows.AddRow(content, HudText.Get("run_end.enemies"), summary.EnemiesKilled.ToString());
            SummaryRows.AddRow(content, HudText.Get("run_end.damage_dealt"), summary.DamageDealt.ToString());
            SummaryRows.AddRow(content, HudText.Get("run_end.damage_taken"), summary.DamageTaken.ToString());
        }

        private static void BuildBuild(VisualElement content, RunSummary summary)
        {
            var database = GameDatabase.Instance;

            SummaryRows.AddSection(content, HudText.Get("run_end.section.talents"));
            if (summary.Talents.Count == 0)
                SummaryRows.AddText(content, HudText.Get("run_end.none"));

            foreach (var talent in summary.Talents)
            {
                var definition = database != null ? database.Get<DisplayableDefinition>(talent.TalentId) : null;
                var name = definition != null ? definition.GetDisplayName() : talent.TalentId;
                var rank = talent.Rank > 1 ? HudText.Format("run_end.rank", talent.Rank) : string.Empty;
                SummaryRows.AddRow(content, name, rank, definition != null ? definition.Icon : null);
            }

            SummaryRows.AddSection(content, HudText.Get("run_end.section.skills"));
            if (summary.SkillIds.Count == 0)
                SummaryRows.AddText(content, HudText.Get("run_end.none"));

            foreach (var skillId in summary.SkillIds)
            {
                var skill = database != null ? database.Get<SkillDefinition>(skillId) : null;
                if (skill == null) continue;

                SummaryRows.AddRow(content, skill.GetDisplayName(), string.Empty, skill.Icon);
            }
        }

        /// <summary>Progress toward the classes that are still locked ("Mage: 7/15 battles").</summary>
        private void BuildClassProgress(VisualElement content)
        {
            var manager = GameStateManager.Instance;
            if (manager == null) return;

            var started = false;
            foreach (var config in manager.PlayableCharacters)
            {
                if (config == null || ProfileService.IsClassUnlocked(config)) continue;

                if (!started)
                {
                    SummaryRows.AddSection(content, HudText.Get("run_end.section.classes"));
                    started = true;
                }

                var (won, required) = ProfileService.GetUnlockProgress(config);
                SummaryRows.AddText(content,
                    HudText.Format("menu.unlock_progress", config.GetDisplayName(), won, required));
            }
        }
    }
}
