using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Stats;
using GridBattle.Gameplay.Talents;
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
    /// <summary>The two sections of the glossary.</summary>
    public enum EGlossaryTab
    {
        Classes,
        Enemies
    }

    /// <summary>
    /// The in-game glossary (GDD 2.7, meta_progressao_e_perfil): two tabs. <b>Classes</b> lists the unlocked classes
    /// (and the locked ones, shown locked with their unlock progress); a class opens its talent pool, where the
    /// talents the player has chosen show icon, name and description and the others show "?". <b>Enemies</b> lists
    /// the enemies faced (and, when <see cref="MetaSettings.ShowUnknownEnemies"/> is on, the unknown ones as "?");
    /// an enemy opens its role, behavior, base attributes, skills and initial states. The data comes from
    /// <see cref="GlossaryService"/>; the detail replaces the list in the same window (a panel swap with a Back
    /// button). Opened by the flow on <c>GlossaryRequestedEvent</c>; closes with its X button.
    /// </summary>
    [Preserve]
    public sealed class GlossaryController : ViewController
    {
        private const string ActiveTabClass = "tab-button--active";

        /// <summary>Attributes an enemy detail always lists (base values).</summary>
        private static readonly EAttribute[] BasicAttributes =
        {
            EAttribute.MaxHp, EAttribute.WalkRange, EAttribute.AttackRange, EAttribute.BasicDamage, EAttribute.Defense,
        };

        /// <summary>Attributes an enemy detail lists only when their base value is not zero.</summary>
        private static readonly EAttribute[] OptionalAttributes =
        {
            EAttribute.CritChance, EAttribute.DefensePenetration, EAttribute.SkillDamageBonus, EAttribute.SkillRange,
            EAttribute.CooldownReduction,
        };

        private EGlossaryTab _tab;
        private PlayerCharacterConfig _classDetail;
        private EnemyConfig _enemyDetail;
        private float _listScroll;

        private Label _title;
        private VisualElement _tabs;
        private Button _classesTab;
        private Button _enemiesTab;
        private Button _backButton;
        private ScrollView _scroll;

        /// <summary>The section showing (or the one the detail belongs to).</summary>
        public EGlossaryTab Tab => _tab;

        /// <summary>A class or an enemy is open in the detail view.</summary>
        public bool IsDetail => _classDetail != null || _enemyDetail != null;

        /// <summary>The class open in the detail view (null when none).</summary>
        public PlayerCharacterConfig ClassDetail => _classDetail;

        /// <summary>The enemy open in the detail view (null when none).</summary>
        public EnemyConfig EnemyDetail => _enemyDetail;

        /// <summary>The content of the list or of the detail (for tests).</summary>
        public VisualElement Content => _scroll != null ? _scroll.contentContainer : null;

        public Button ClassesTabButton => _classesTab;
        public Button EnemiesTabButton => _enemiesTab;
        public Button BackButton => _backButton;

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
            _title = root.Q<Label>("glossary-title");
            _tabs = root.Q<VisualElement>("glossary-tabs");
            _classesTab = root.Q<Button>("glossary-tab-classes");
            _enemiesTab = root.Q<Button>("glossary-tab-enemies");
            _backButton = root.Q<Button>("glossary-back-button");
            _scroll = root.Q<ScrollView>("glossary-scroll");
            _scroll.HideScrollerWhenContentFits();

            _classesTab.OnClick(() => SelectTab(EGlossaryTab.Classes));
            _enemiesTab.OnClick(() => SelectTab(EGlossaryTab.Enemies));
            _backButton.OnClick(OnBackClicked);
            root.Q<Button>("glossary-close-button").OnClick(OnCloseClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _tab = EGlossaryTab.Classes;
            _classDetail = null;
            _enemyDetail = null;
            _listScroll = 0f;
            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        /// <summary>Back leaves the detail first, then closes the window.</summary>
        protected override bool OnBack()
        {
            if (IsDetail)
                ShowList();
            else
                OnCloseClicked();
            return true;
        }

        // ------------------------------------------------------------------------------------ navigation

        /// <summary>Shows the list of a section.</summary>
        public void SelectTab(EGlossaryTab tab)
        {
            AudioManager.Play(ESfx.ButtonTap);
            _tab = tab;
            _classDetail = null;
            _enemyDetail = null;
            _listScroll = 0f;
            Render();
        }

        /// <summary>Opens the detail of an unlocked class.</summary>
        public void OpenClass(PlayerCharacterConfig config)
        {
            if (config == null) return;

            AudioManager.Play(ESfx.ButtonTap);
            _listScroll = _scroll.scrollOffset.y;
            _tab = EGlossaryTab.Classes;
            _classDetail = config;
            _enemyDetail = null;
            Render();
        }

        /// <summary>Opens the detail of a discovered enemy.</summary>
        public void OpenEnemy(EnemyConfig config)
        {
            if (config == null) return;

            AudioManager.Play(ESfx.ButtonTap);
            _listScroll = _scroll.scrollOffset.y;
            _tab = EGlossaryTab.Enemies;
            _enemyDetail = config;
            _classDetail = null;
            Render();
        }

        /// <summary>Leaves the detail and shows the list again where it was.</summary>
        public void ShowList()
        {
            AudioManager.Play(ESfx.ButtonTap);
            _classDetail = null;
            _enemyDetail = null;
            Render();

            var offset = _listScroll;
            _scroll.schedule.Execute(() => _scroll.scrollOffset = new Vector2(0f, offset)).StartingIn(0);
        }

        private void OnBackClicked()
        {
            ShowList();
        }

        private void OnCloseClicked()
        {
            AudioManager.Play(ESfx.WindowClose);
            Close();
        }

        // ------------------------------------------------------------------------------------ rendering

        private void Render()
        {
            if (_title == null) return;

            _title.text = HudText.Get("glossary.title");
            _classesTab.text = HudText.Get("glossary.tab.classes");
            _enemiesTab.text = HudText.Get("glossary.tab.enemies");
            _backButton.text = HudText.Get("glossary.back");

            var detail = IsDetail;
            _tabs.SetDisplayed(!detail);
            _backButton.SetDisplayed(detail);
            _classesTab.EnableInClassList(ActiveTabClass, _tab == EGlossaryTab.Classes);
            _enemiesTab.EnableInClassList(ActiveTabClass, _tab == EGlossaryTab.Enemies);

            var content = _scroll.contentContainer;
            content.Clear();
            _scroll.scrollOffset = Vector2.zero;

            if (_classDetail != null)
                BuildClassDetail(content, _classDetail);
            else if (_enemyDetail != null)
                BuildEnemyDetail(content, _enemyDetail);
            else if (_tab == EGlossaryTab.Classes)
                BuildClassList(content);
            else
                BuildEnemyList(content);
        }

        // ------------------------------------------------------------------------------------ classes

        private void BuildClassList(VisualElement content)
        {
            var unlocked = GlossaryService.GetUnlockedClasses();
            foreach (var config in unlocked)
                content.Add(CreateClassRow(config, true));

            var locked = new List<PlayerCharacterConfig>();
            var database = GameDatabase.Instance;
            if (database != null)
            {
                foreach (var config in database.GetAll<PlayerCharacterConfig>())
                {
                    if (!unlocked.Contains(config))
                        locked.Add(config);
                }
            }

            locked.Sort((a, b) => a.BattlesToUnlock.CompareTo(b.BattlesToUnlock));
            foreach (var config in locked)
                content.Add(CreateClassRow(config, false));
        }

        private VisualElement CreateClassRow(PlayerCharacterConfig config, bool unlocked)
        {
            var row = new Button { name = "glossary-class-" + config.name, text = string.Empty };
            row.AddToClassList("list-row");
            row.EnableInClassList("list-row--locked", !unlocked);

            var icon = new Image
            {
                sprite = unlocked ? config.Sprite : ScreensSettings.Current.LockIcon,
                pickingMode = PickingMode.Ignore,
            };
            icon.AddToClassList("list-row__icon");
            row.Add(icon);

            string sub;
            if (unlocked)
            {
                var pool = TalentService.GetPool(config);
                sub = HudText.Format("glossary.class_progress", CountDiscovered(config, pool), pool.Count);
            }
            else
            {
                var (won, required) = ProfileService.GetUnlockProgress(config);
                sub = HudText.Format("glossary.locked_progress", won, required);
            }

            row.Add(CreateTexts(config.GetDisplayName(), sub));
            row.OnClick(() =>
            {
                if (unlocked)
                {
                    OpenClass(config);
                    return;
                }

                AudioManager.Play(ESfx.ButtonTap);
                Wobble(row);
            });
            return row;
        }

        private void BuildClassDetail(VisualElement content, PlayerCharacterConfig config)
        {
            content.Add(CreateHeader(config.Sprite, config.GetDisplayName(), null));

            var description = config.GetDescription();
            if (!string.IsNullOrEmpty(description))
                SummaryRows.AddText(content, description);

            var pool = TalentService.GetPool(config);
            SummaryRows.AddSection(content,
                HudText.Format("glossary.talents", CountDiscovered(config, pool), pool.Count));

            foreach (var talent in pool)
                content.Add(CreateTalentRow(config, talent));
        }

        private static int CountDiscovered(PlayerCharacterConfig config, List<TalentDefinition> pool)
        {
            var count = 0;
            foreach (var talent in pool)
            {
                if (GlossaryService.GetTalentState(config, talent.Id) == EGlossaryEntryState.Discovered)
                    count++;
            }

            return count;
        }

        /// <summary>A talent of a class pool: revealed (icon, name, description) or "?".</summary>
        private static VisualElement CreateTalentRow(PlayerCharacterConfig config, TalentDefinition talent)
        {
            var row = new VisualElement { name = "glossary-talent-" + talent.name, pickingMode = PickingMode.Ignore };
            row.AddToClassList("list-row");

            if (GlossaryService.GetTalentState(config, talent.Id) != EGlossaryEntryState.Discovered)
            {
                row.AddToClassList("glossary-row--unknown");
                row.Add(CreateUnknownMark());
                return row;
            }

            var icon = new Image { sprite = talent.Icon, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("list-row__icon");
            row.Add(icon);

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("list-row__texts");

            texts.Add(CreateLabel(talent.GetDisplayName(), "list-row__name"));

            var description = talent.GetDescription();
            if (!string.IsNullOrEmpty(description))
                texts.Add(CreateLabel(description, "list-row__desc"));

            if (talent.MaxRank > 1)
            {
                texts.Add(CreateLabel(
                    HudText.Format("glossary.max_rank", RomanNumerals.ToRoman(talent.MaxRank)), "list-row__desc"));
            }

            if (talent.IsSkillTalent && talent.UnlockedSkill != null)
            {
                texts.Add(CreateLabel(
                    HudText.Format("talent.new_skill", talent.UnlockedSkill.GetDisplayName()), "list-row__desc"));
            }

            row.Add(texts);
            return row;
        }

        // ------------------------------------------------------------------------------------ enemies

        private void BuildEnemyList(VisualElement content)
        {
            var enemies = GlossaryService.GetListedEnemies();
            if (enemies.Count == 0)
            {
                var empty = new Label(HudText.Get("glossary.empty_enemies")) { pickingMode = PickingMode.Ignore };
                empty.AddToClassList("glossary-empty");
                content.Add(empty);
                return;
            }

            for (var i = 0; i < enemies.Count; i++)
                content.Add(CreateEnemyRow(enemies[i], i));
        }

        private VisualElement CreateEnemyRow(EnemyConfig enemy, int index)
        {
            if (GlossaryService.GetEnemyState(enemy) != EGlossaryEntryState.Discovered)
            {
                var unknown = new VisualElement { name = "glossary-enemy-" + index, pickingMode = PickingMode.Ignore };
                unknown.AddToClassList("list-row");
                unknown.AddToClassList("glossary-row--unknown");
                unknown.Add(CreateUnknownMark());
                return unknown;
            }

            var row = new Button { name = "glossary-enemy-" + index, text = string.Empty };
            row.AddToClassList("list-row");

            var icon = new Image { sprite = enemy.Sprite, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("list-row__icon");
            row.Add(icon);

            var sub = enemy.Role != null ? enemy.Role.GetDisplayName() : string.Empty;
            row.Add(CreateTexts(enemy.GetDisplayName(), sub));
            row.OnClick(() => OpenEnemy(enemy));
            return row;
        }

        private void BuildEnemyDetail(VisualElement content, EnemyConfig enemy)
        {
            var sub = enemy.IsBoss ? HudText.Get("glossary.boss") : null;
            content.Add(CreateHeader(enemy.Sprite, enemy.GetDisplayName(), sub));

            if (enemy.Role != null)
            {
                SummaryRows.AddSection(content, HudText.Get("details.role"));
                SummaryRows.AddRow(content, enemy.Role.GetDisplayName(), string.Empty, enemy.Role.Icon);
            }

            var behavior = enemy.GetBehaviorDescription();
            if (!string.IsNullOrEmpty(behavior))
            {
                SummaryRows.AddSection(content, HudText.Get("details.behavior"));
                SummaryRows.AddText(content, behavior);
            }

            // Base values: the ones the enemy has before the depth scaling of the battle.
            SummaryRows.AddSection(content, HudText.Get("details.attributes"));
            foreach (var attribute in BasicAttributes)
                AddAttributeRow(content, enemy, attribute);

            foreach (var attribute in OptionalAttributes)
            {
                if (Mathf.Abs(enemy.GetBaseAttribute(attribute)) <= 0.0001f) continue;

                AddAttributeRow(content, enemy, attribute);
                if (attribute == EAttribute.CritChance)
                    AddAttributeRow(content, enemy, EAttribute.CritMultiplier);
            }

            SummaryRows.AddText(content, HudText.Get("glossary.base_values"));

            SummaryRows.AddSection(content, HudText.Get("details.skills"));
            var hasSkill = false;
            foreach (var skill in enemy.Skills)
            {
                if (skill == null) continue;

                hasSkill = true;
                SummaryRows.AddRow(content, skill.GetDisplayName(), string.Empty, skill.Icon);
                var description = skill.GetDescription();
                if (!string.IsNullOrEmpty(description))
                    SummaryRows.AddText(content, description);
            }

            if (!hasSkill)
                SummaryRows.AddText(content, HudText.Get("details.none"));

            var hasState = false;
            foreach (var grant in enemy.InitialStates)
            {
                if (!grant.IsValid) continue;

                if (!hasState)
                {
                    SummaryRows.AddSection(content, HudText.Get("glossary.initial_states"));
                    hasState = true;
                }

                var state = grant.State;
                var duration = grant.IsPermanent
                    ? HudText.Get("details.state.permanent")
                    : HudText.Format(grant.Duration == 1 ? "details.state.turn" : "details.state.turns", grant.Duration);
                SummaryRows.AddRow(content, state.GetDisplayName(), duration, state.Icon);
                var description = state.GetDescription();
                if (!string.IsNullOrEmpty(description))
                    SummaryRows.AddText(content, description);
            }
        }

        private static void AddAttributeRow(VisualElement content, EnemyConfig enemy, EAttribute attribute)
        {
            SummaryRows.AddRow(content, HudText.Get(EntityDetailsModel.GetAttributeKey(attribute)),
                EntityDetailsModel.FormatAttribute(attribute, enemy.GetBaseAttribute(attribute)));
        }

        // ------------------------------------------------------------------------------------ building blocks

        private static VisualElement CreateHeader(Sprite sprite, string name, string sub)
        {
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.AddToClassList("glossary-header");

            var image = new Image { sprite = sprite, pickingMode = PickingMode.Ignore };
            image.AddToClassList("glossary-header__image");
            header.Add(image);

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("glossary-header__texts");
            texts.Add(CreateLabel(name, "glossary-header__name"));
            if (!string.IsNullOrEmpty(sub))
                texts.Add(CreateLabel(sub, "glossary-header__sub"));
            header.Add(texts);
            return header;
        }

        private static VisualElement CreateTexts(string name, string sub)
        {
            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("list-row__texts");
            texts.Add(CreateLabel(name, "list-row__name"));
            if (!string.IsNullOrEmpty(sub))
                texts.Add(CreateLabel(sub, "list-row__desc"));
            return texts;
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        private static Label CreateUnknownMark()
        {
            var mark = new Label("?") { pickingMode = PickingMode.Ignore };
            mark.AddToClassList("list-row__unknown");
            return mark;
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
            }).Every(40).Until(() => index >= offsets.Length);
        }
    }
}
