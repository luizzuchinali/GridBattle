using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GridBattle.Core;
using GridBattle.Data;
using GridBattle.Gameplay.AI;
using GridBattle.Gameplay.AI.Actions;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Roles;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Monetization;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Talents;
using GridBattle.Gameplay.Terrain;
using GridBattle.UI.Overlays;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using ZS.UI.Navigation;
using Object = UnityEngine.Object;

namespace GridBattle.Editor.Tools
{
    /// <summary>
    /// Checks the game content for the mistakes that are easy to make when creating it in the editor and only show up
    /// while playing: texts missing in a language, text keys used by the code that do not exist, empty references
    /// (a skill, an effect, a pool entry), talents that are never offered, enemies that never appear, broken screen
    /// definitions, missing icons and assets missing from the <see cref="GameDatabase"/>. Read only: it changes
    /// nothing. Menu <c>GridBattle/Validate Content</c>; scripts and agents call <see cref="Run"/>.
    /// <para>
    /// Errors break something in the game; warnings are content that is probably unfinished; notes are things worth
    /// knowing (placeholder content, an enemy only used as a summon).
    /// </para>
    /// </summary>
    public static class ContentValidator
    {
        private enum ESeverity
        {
            Error,
            Warning,
            Note
        }

        private sealed class Issue
        {
            public ESeverity Severity;
            public string Message;
            public Object Context;
        }

        private static readonly string[] Locales = { "en", "es", "pt-BR" };

        /// <summary>A text key: lowercase words separated by dots (e.g. <c>menu.new_game</c>).</summary>
        private static readonly Regex KeyPattern = new(@"""([a-z][a-z0-9_]*(?:\.[a-z0-9_]+)+)""", RegexOptions.Compiled);

        [MenuItem("GridBattle/Validate Content", priority = 0)]
        private static void RunFromMenu()
        {
            var report = Run(true);
            Debug.Log(report);
        }

        /// <summary>
        /// Runs every check and returns the report (counts first, then every issue). With <paramref name="log"/>
        /// each issue is also written to the Console with its asset as context (click to select it).
        /// </summary>
        public static string Run(bool log = false)
        {
            var issues = new List<Issue>();
            var database = GameDatabase.Instance;
            var checkedAssets = database != null ? database.Definitions.Count : 0;
            if (database == null)
            {
                Add(issues, ESeverity.Error, "There is no GameDatabase (GridBattle > Rebuild Game Database).", null);
            }
            else
            {
                CheckDatabase(issues, database);
                CheckTexts(issues, database);
                CheckIcons(issues, database);
                CheckCharacters(issues, database);
                CheckEffects(issues, database);
                CheckTalents(issues, database);
                CheckEnemyAppearance(issues, database);
                CheckSettingsPools(issues);
                CheckSkillSelection(issues, database);
            }

            CheckMonetization(issues);

            var checkedViews = CheckViews(issues);
            var checkedKeys = CheckCodeTextKeys(issues);

            if (log)
            {
                foreach (var issue in issues)
                {
                    var text = "[Validate Content] " + issue.Message;
                    if (issue.Severity == ESeverity.Error) Debug.LogError(text, issue.Context);
                    else if (issue.Severity == ESeverity.Warning) Debug.LogWarning(text, issue.Context);
                    else Debug.Log(text, issue.Context);
                }
            }

            return BuildReport(issues, $"Checked {checkedAssets} content assets, {checkedViews} views and {checkedKeys} text keys used in the code.");
        }

        // ------------------------------------------------------------------------------------ checks

        /// <summary>Every content asset of the project is in the database, with a unique id.</summary>
        private static void CheckDatabase(List<Issue> issues, GameDatabase database)
        {
            var listed = new HashSet<GameDefinition>(database.Definitions.Where(d => d != null));
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(GameDefinition), new[] { "Assets" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && !listed.Contains(asset))
                    Add(issues, ESeverity.Error, $"{Describe(asset)} is not in the GameDatabase (GridBattle > Rebuild Game Database).", asset);
            }

            foreach (var group in database.Definitions.Where(d => d != null).GroupBy(d => d.Id))
            {
                if (group.Count() > 1)
                    Add(issues, ESeverity.Error, $"Id {group.Key} is shared by {string.Join(", ", group.Select(d => d.name))}.", group.First());
            }
        }

        /// <summary>Names (required) and descriptions (when set) exist in every language.</summary>
        private static void CheckTexts(List<Issue> issues, GameDatabase database)
        {
            foreach (var definition in database.GetAll<DisplayableDefinition>())
            {
                CheckLocalized(issues, definition, definition.DisplayName, "name", true);
                CheckLocalized(issues, definition, definition.Description, "description", false);
                if (definition is EnemyConfig enemy)
                    CheckLocalized(issues, enemy, enemy.BehaviorDescription, "behavior description", false);

                var needsDescription = definition is SkillDefinition || definition is TalentDefinition ||
                                       definition is ConsumableDefinition || definition is PlayerCharacterConfig;
                if (needsDescription && definition.Description.IsEmpty)
                    Add(issues, ESeverity.Warning, $"{Describe(definition)} has no description (players see it in the game).", definition);
            }
        }

        private static void CheckLocalized(List<Issue> issues, Object owner, LocalizedString text, string what, bool required)
        {
            if (text == null || text.IsEmpty)
            {
                if (required)
                    Add(issues, ESeverity.Error, $"{Describe(owner)} has no {what} text.", owner);
                return;
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(text.TableReference);
            var entry = collection != null ? collection.SharedData.GetEntryFromReference(text.TableEntryReference) : null;
            if (entry == null)
            {
                Add(issues, ESeverity.Error, $"{Describe(owner)}: the {what} points to a text that does not exist.", owner);
                return;
            }

            var missing = MissingLocales(collection, entry.Id);
            if (missing.Count > 0)
                Add(issues, ESeverity.Error, $"{Describe(owner)}: the {what} ('{entry.Key}') is empty in {string.Join(", ", missing)}.", owner);
        }

        private static List<string> MissingLocales(StringTableCollection collection, long entryId)
        {
            var missing = new List<string>();
            foreach (var code in Locales)
            {
                var table = collection.GetTable(code) as StringTable;
                var value = table != null ? table.GetEntry(entryId) : null;
                if (value == null || string.IsNullOrWhiteSpace(value.Value))
                    missing.Add(code);
            }

            return missing;
        }

        /// <summary>Content that the game draws with an icon has one.</summary>
        private static void CheckIcons(List<Issue> issues, GameDatabase database)
        {
            foreach (var definition in database.GetAll<DisplayableDefinition>())
            {
                var needsIcon = definition is SkillDefinition || definition is TalentDefinition ||
                                definition is ConsumableDefinition || definition is EnemyRoleDefinition ||
                                (definition is StateDefinition state && state.ShowInDetails);
                if (needsIcon && definition.Icon == null)
                    Add(issues, ESeverity.Warning, $"{Describe(definition)} has no icon.", definition);
            }
        }

        /// <summary>Characters are complete enough to be spawned.</summary>
        private static void CheckCharacters(List<Issue> issues, GameDatabase database)
        {
            foreach (var config in database.GetAll<CharacterConfig>())
            {
                if (config.Sprite == null)
                    Add(issues, ESeverity.Warning, $"{Describe(config)} has no sprite.", config);
                if (config.Skills.Any(s => s == null))
                    Add(issues, ESeverity.Error, $"{Describe(config)} has an empty entry in its skills.", config);
                if (config.InitialStates.Any(g => !g.IsValid))
                    Add(issues, ESeverity.Warning, $"{Describe(config)} has an initial state without a state.", config);

                switch (config)
                {
                    case PlayerCharacterConfig player:
                        if (player.Prefab == null)
                            Add(issues, ESeverity.Error, $"{Describe(player)} has no prefab (PlayerCharacter.prefab).", player);
                        if (player.Skills.Count(s => s != null) == 0)
                            Add(issues, ESeverity.Warning, $"{Describe(player)} starts with no skill (every class starts with one).", player);
                        if (player.TalentPool.Any(t => t == null))
                            Add(issues, ESeverity.Error, $"{Describe(player)} has an empty entry in its talent pool.", player);
                        break;
                    case EnemyConfig enemy:
                        if (enemy.Prefab == null)
                            Add(issues, ESeverity.Error, $"{Describe(enemy)} has no prefab (Enemy.prefab).", enemy);
                        if (enemy.Behavior == null)
                            Add(issues, ESeverity.Error, $"{Describe(enemy)} has no behavior (it would never act).", enemy);
                        else if (enemy.Behavior.Actions.Any(a => a == null))
                            Add(issues, ESeverity.Error, $"{Describe(enemy.Behavior)} has an empty action.", enemy.Behavior);
                        if (enemy.Role == null)
                            Add(issues, ESeverity.Warning, $"{Describe(enemy)} has no role (no role icon, threat factor 1).", enemy);
                        break;
                }
            }
        }

        /// <summary>No effect list holds an empty entry (an effect whose class was renamed or deleted shows up as one).</summary>
        private static void CheckEffects(List<Issue> issues, GameDatabase database)
        {
            foreach (var skill in database.GetAll<SkillDefinition>())
            {
                if (skill.Effects.Any(e => e == null))
                    Add(issues, ESeverity.Error, $"{Describe(skill)} has an empty effect.", skill);
            }

            foreach (var state in database.GetAll<StateDefinition>())
            {
                if (state.Effects.Any(e => e == null))
                    Add(issues, ESeverity.Error, $"{Describe(state)} has an empty effect.", state);
            }

            foreach (var consumable in database.GetAll<ConsumableDefinition>())
            {
                if (consumable.Effects.Any(e => e == null))
                    Add(issues, ESeverity.Error, $"{Describe(consumable)} has an empty effect.", consumable);
            }
        }

        /// <summary>Talents are offered somewhere and their references are set.</summary>
        private static void CheckTalents(List<Issue> issues, GameDatabase database)
        {
            var offers = TalentOfferSettings.Current;
            var pooled = new HashSet<TalentDefinition>();
            if (offers != null)
                pooled.UnionWith(offers.SharedPool.Where(t => t != null));
            foreach (var player in database.GetAll<PlayerCharacterConfig>())
                pooled.UnionWith(player.TalentPool.Where(t => t != null));

            foreach (var talent in database.GetAll<TalentDefinition>())
            {
                if (!pooled.Contains(talent))
                    Add(issues, ESeverity.Warning, $"{Describe(talent)} is in no talent pool (class or shared): it is never offered.", talent);
                if (talent.Prerequisites.Any(p => p == null))
                    Add(issues, ESeverity.Error, $"{Describe(talent)} has an empty prerequisite.", talent);
                if (talent.States.Any(g => !g.IsValid))
                    Add(issues, ESeverity.Error, $"{Describe(talent)} grants a state entry without a state.", talent);
                if (!talent.IsSkillTalent && !talent.States.Any(g => g.IsValid))
                    Add(issues, ESeverity.Warning, $"{Describe(talent)} grants nothing (no state and no skill).", talent);
                foreach (var prerequisite in talent.Prerequisites.Where(p => p != null))
                {
                    if (!pooled.Contains(prerequisite))
                        Add(issues, ESeverity.Warning, $"{Describe(talent)} needs {prerequisite.name}, which is never offered.", talent);
                }
            }
        }

        /// <summary>Enemies appear in battles: in the generator's pool, the boss encounter or as a summon.</summary>
        private static void CheckEnemyAppearance(List<Issue> issues, GameDatabase database)
        {
            var battle = GameSettings.Get<BattleGenerationSettings>();
            var drawn = new HashSet<EnemyConfig>();
            if (battle != null)
            {
                foreach (var entry in battle.Pool)
                {
                    if (entry == null || entry.Enemy == null)
                    {
                        Add(issues, ESeverity.Error, "Battle Generation Settings has an enemy pool entry without an enemy.", battle);
                        continue;
                    }

                    drawn.Add(entry.Enemy);
                }

                foreach (var boss in battle.BossEncounter)
                {
                    if (boss == null)
                        Add(issues, ESeverity.Error, "Battle Generation Settings has an empty boss encounter entry.", battle);
                    else
                        drawn.Add(boss);
                }
            }

            var summoned = new HashSet<EnemyConfig>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(SummonAction)))
            {
                var action = AssetDatabase.LoadAssetAtPath<SummonAction>(AssetDatabase.GUIDToAssetPath(guid));
                if (action == null) continue;
                if (action.Minion == null)
                    Add(issues, ESeverity.Error, $"{Describe(action)} summons nothing.", action);
                else
                    summoned.Add(action.Minion);
            }

            foreach (var enemy in database.GetAll<EnemyConfig>())
            {
                if (drawn.Contains(enemy)) continue;

                if (summoned.Contains(enemy))
                    Add(issues, ESeverity.Note, $"{Describe(enemy)} only appears as a summon.", enemy);
                else
                    Add(issues, ESeverity.Warning, $"{Describe(enemy)} never appears (not in the battle pool, the boss encounter or a summon).", enemy);
            }
        }

        /// <summary>The pools of the generators and of the consumable offers hold no empty entry.</summary>
        private static void CheckSettingsPools(List<Issue> issues)
        {
            var terrain = GameSettings.Get<TerrainGenerationSettings>();
            if (terrain != null)
            {
                foreach (ETerrainKind kind in Enum.GetValues(typeof(ETerrainKind)))
                {
                    var pool = terrain.GetPool(kind);
                    if (pool != null && pool.Any(e => e == null || e.Terrain == null))
                        Add(issues, ESeverity.Error, $"Terrain Generation Settings: the {kind} pool has an entry without a terrain.", terrain);
                }
            }

            var consumables = GameSettings.Get<ConsumableSettings>();
            if (consumables != null && consumables.Pool.Any(c => c == null))
                Add(issues, ESeverity.Error, "Consumable Settings: the pool has an empty entry.", consumables);
        }

        /// <summary>A skill whose minimum range passes its maximum cannot be aimed anywhere.</summary>
        private static void CheckSkillSelection(List<Issue> issues, GameDatabase database)
        {
            foreach (var skill in database.GetAll<SkillDefinition>())
            {
                if (skill == null || skill.IsSelfCentered || skill.UnlimitedRange) continue;
                if (skill.MinRange > skill.Range)
                    Add(issues, ESeverity.Error, $"{Describe(skill)}: the minimum range ({skill.MinRange}) is larger than the maximum ({skill.Range}).", skill);
            }
        }

        /// <summary>
        /// Monetization (GDD 7): the windows are set, and every mobile platform that is on has its LevelPlay App
        /// Key and ad units and a product id (a note while they are empty: the game then runs without ads there).
        /// </summary>
        private static void CheckMonetization(List<Issue> issues)
        {
            var modals = GameSettings.Get<ModalsSettings>();
            if (modals != null && (modals.RemoveAdsView == null || modals.AdNoticeView == null || modals.SimulatedAdView == null))
                Add(issues, ESeverity.Warning, "Modals Settings: a monetization view (remove ads, ad notice, simulated ad) is empty.", modals);

            var settings = GameSettings.Get<MonetizationSettings>();
            if (settings == null) return;

            if ((settings.EnabledOnAndroid || settings.EnabledOnIos) && string.IsNullOrEmpty(settings.RemoveAdsProductId))
                Add(issues, ESeverity.Warning, "Monetization Settings: the remove ads product id is empty (the purchase is off).", settings);

            if (settings.EnabledOnAndroid && (string.IsNullOrEmpty(settings.AndroidAppKey) ||
                                              string.IsNullOrEmpty(settings.AndroidInterstitialAdUnitId) ||
                                              string.IsNullOrEmpty(settings.AndroidRewardedAdUnitId)))
                Add(issues, ESeverity.Note, "Monetization Settings: the Android LevelPlay App Key or ad unit ids are empty (no ads on Android until they are filled).", settings);

            if (settings.EnabledOnIos && (string.IsNullOrEmpty(settings.IosAppKey) ||
                                          string.IsNullOrEmpty(settings.IosInterstitialAdUnitId) ||
                                          string.IsNullOrEmpty(settings.IosRewardedAdUnitId)))
                Add(issues, ESeverity.Note, "Monetization Settings: the iOS LevelPlay App Key or ad unit ids are empty (no ads on iOS until they are filled).", settings);
        }

        /// <summary>Every screen, modal and page definition has its UXML and a controller type that exists.</summary>
        private static int CheckViews(List<Issue> issues)
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ViewDefinition), new[] { "Assets/Application" }))
            {
                var view = AssetDatabase.LoadAssetAtPath<ViewDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (view == null) continue;

                count++;
                foreach (var problem in view.Validate())
                    Add(issues, ESeverity.Error, $"{Describe(view)}: {problem}", view);
            }

            return count;
        }

        /// <summary>
        /// Text keys written in the code (<c>HudText.Get/Format("...")</c>, <c>Loc.Ui("...")</c>, including the ones
        /// inside a conditional) exist in the UI table in every language. Keys built at runtime are not checked.
        /// </summary>
        private static int CheckCodeTextKeys(List<Issue> issues)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Loc.UiTable);
            if (collection == null)
            {
                Add(issues, ESeverity.Error, $"The '{Loc.UiTable}' string table does not exist.", null);
                return 0;
            }

            var keys = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles("Assets/Application/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                if (path.Replace('\\', '/').Contains("/Editor/")) continue;

                foreach (var line in File.ReadLines(path))
                {
                    if (!line.Contains("HudText.Get") && !line.Contains("HudText.Format") && !line.Contains("Loc.Ui(")) continue;

                    foreach (Match match in KeyPattern.Matches(line))
                        keys.TryAdd(match.Groups[1].Value, Path.GetFileName(path));
                }
            }

            foreach (var pair in keys.OrderBy(p => p.Key))
            {
                var entry = collection.SharedData.GetEntry(pair.Key);
                if (entry == null)
                {
                    Add(issues, ESeverity.Error, $"Text key '{pair.Key}' (used in {pair.Value}) is not in the UI table.", null);
                    continue;
                }

                var missing = MissingLocales(collection, entry.Id);
                if (missing.Count > 0)
                    Add(issues, ESeverity.Error, $"Text key '{pair.Key}' (used in {pair.Value}) is empty in {string.Join(", ", missing)}.", null);
            }

            return keys.Count;
        }

        // ------------------------------------------------------------------------------------ report

        private static void Add(List<Issue> issues, ESeverity severity, string message, Object context)
        {
            issues.Add(new Issue { Severity = severity, Message = message, Context = context });
        }

        private static string Describe(Object asset) =>
            asset == null ? "(none)" : $"{asset.GetType().Name} '{asset.name}'";

        private static string BuildReport(List<Issue> issues, string scope)
        {
            var sb = new StringBuilder();
            var errors = issues.Count(i => i.Severity == ESeverity.Error);
            var warnings = issues.Count(i => i.Severity == ESeverity.Warning);
            var notes = issues.Count(i => i.Severity == ESeverity.Note);
            sb.AppendLine($"Validate Content: {errors} errors, {warnings} warnings, {notes} notes. {scope}");
            foreach (var severity in new[] { ESeverity.Error, ESeverity.Warning, ESeverity.Note })
            {
                foreach (var issue in issues.Where(i => i.Severity == severity))
                    sb.AppendLine($"- {severity}: {issue.Message}");
            }

            return sb.ToString();
        }
    }
}
