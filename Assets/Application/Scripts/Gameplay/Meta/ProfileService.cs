using System;
using System.Collections.Generic;
using System.IO;
using GridBattle.Core;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// The player profile (GDD 8.1): class unlocking by battles won (3.2), per-class
    /// results (2.2), glossary discoveries (2.7), tutorial tips seen (interface 4.4) and
    /// player options (interface 4.3). Persisted in <c>profile.json</c>, separate from the
    /// run save. The profile loads on first use (and at startup, see
    /// <see cref="MetaBootstrap"/>) and is saved on every change; every change raises
    /// <see cref="ProfileChangedEvent"/>.
    /// <para>
    /// Run end, talents chosen/offered and the depth reached are reported by the run and
    /// talent systems through this API. Battles won and enemies faced are reported
    /// automatically (see <see cref="MetaBootstrap"/>).
    /// </para>
    /// </summary>
    public static class ProfileService
    {
        public const string DefaultFileName = "profile.json";

        private static ProfileState _state;
        private static string _fileName = DefaultFileName;

        /// <summary>The profile data. Read-only for callers: change it through this service.</summary>
        public static ProfileState State
        {
            get
            {
                EnsureLoaded();
                return _state;
            }
        }

        /// <summary>File the profile is saved to (persistent data path).</summary>
        public static string FileName => _fileName;

        // ---------------------------------------------------------------- persistence

        /// <summary>Loads the profile from disk (or starts a new one) and reconciles the class unlocks.</summary>
        public static void Load()
        {
            if (SaveSystem.TryLoad(_fileName, out ProfileState loaded))
            {
                _state = loaded;
                Migrate(_state);
            }
            else
            {
                if (SaveSystem.Exists(_fileName))
                    BackUpUnreadableFile();

                _state = CreateNew();
            }

            Normalize(_state);
            if (ReconcileUnlocks().Count > 0 || !SaveSystem.Exists(_fileName))
                Save();

            EventBus.Raise(new ProfileChangedEvent(EProfileChange.Loaded));
        }

        /// <summary>Writes the profile to disk. Every change through this service already does it.</summary>
        public static void Save()
        {
            if (_state == null) return;

            SaveSystem.Save(_fileName, _state);
        }

        /// <summary>
        /// Starts again with an empty profile (default options) and saves it. Does not touch the
        /// current locale until <see cref="ApplyLocale"/> runs.
        /// </summary>
        public static void ResetProfile()
        {
            _state = CreateNew();
            ReconcileUnlocks();
            Save();
            EventBus.Raise(new ProfileChangedEvent(EProfileChange.Loaded));
        }

        /// <summary>
        /// Tests only: uses another file name (and drops the in-memory profile, which loads again
        /// on the next access). Pass null to go back to <see cref="DefaultFileName"/>.
        /// </summary>
        public static void UseFile(string fileName)
        {
            _fileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName;
            _state = null;
        }

        /// <summary>Forgets the in-memory profile (it loads again on the next access).</summary>
        internal static void Unload()
        {
            _state = null;
            _fileName = DefaultFileName;
        }

        // ---------------------------------------------------------------- class unlocking (GDD 3.2)

        /// <summary>Whether the player can pick this class.</summary>
        public static bool IsClassUnlocked(PlayerCharacterConfig config)
        {
            if (config == null) return false;
            if (config.BattlesToUnlock <= 0) return true;

            var state = State;
            return state.TotalBattlesWon >= config.BattlesToUnlock || state.UnlockedClassIds.Contains(config.Id);
        }

        /// <summary>
        /// Progress toward unlocking a class, e.g. "7/15 battles" (interface 4.3).
        /// <c>won</c> is capped at <c>required</c>; a class available from the start is (0, 0).
        /// </summary>
        public static (int won, int required) GetUnlockProgress(PlayerCharacterConfig config)
        {
            if (config == null) return (0, 0);

            var required = Mathf.Max(0, config.BattlesToUnlock);
            return (Mathf.Min(State.TotalBattlesWon, required), required);
        }

        /// <summary>Battles won across all runs that count toward unlocking classes.</summary>
        public static int TotalBattlesWon => State.TotalBattlesWon;

        /// <summary>
        /// Counts a battle won. By default it counts right away, even if the run ends in defeat
        /// later; with <see cref="MetaSettings.CountBattlesFromLostRuns"/> off it is held back until
        /// <see cref="RegisterRunEnded"/> reports a victory.
        /// </summary>
        public static void RegisterBattleWon()
        {
            var state = State;
            var newlyUnlocked = new List<PlayerCharacterConfig>();
            if (MetaSettings.Current.CountBattlesFromLostRuns)
            {
                state.TotalBattlesWon++;
                newlyUnlocked = ReconcileUnlocks();
            }
            else
            {
                state.PendingBattlesWon++;
            }

            Commit(EProfileChange.BattlesWon);
            RaiseUnlocked(newlyUnlocked);
        }

        /// <summary>
        /// Registers the end of a run in the class record (runs played, victories, best level and
        /// depth; GDD 2.2). Also settles the battles held back by
        /// <see cref="MetaSettings.CountBattlesFromLostRuns"/>.
        /// </summary>
        /// <param name="level">Player level reached.</param>
        /// <param name="depth">Map depth reached (1 = first node).</param>
        public static void RegisterRunEnded(PlayerCharacterConfig cls, bool victory, int level, int depth)
        {
            var state = State;
            var newlyUnlocked = SettlePendingBattles(victory);

            if (cls != null && !string.IsNullOrEmpty(cls.Id))
            {
                var record = GetOrAddRecord(state, cls.Id);
                record.RunsPlayed++;
                if (victory)
                    record.Victories++;
                record.BestLevel = Mathf.Max(record.BestLevel, level);
                record.BestDepth = Mathf.Max(record.BestDepth, depth);

                MetricsRecorder.Record("run_ended", new
                {
                    @class = new { id = cls.Id, name = cls.name },
                    victory,
                    level,
                    depth,
                    totalBattlesWon = state.TotalBattlesWon,
                });
            }
            else
            {
                Debug.LogWarning("RegisterRunEnded called without a class with an id; the class record was not updated.");
            }

            Commit(EProfileChange.RunEnded);
            RaiseUnlocked(newlyUnlocked);
        }

        /// <summary>
        /// The player gave up the run (pause menu). Registered as a defeat or ignored, depending on
        /// <see cref="MetaSettings.GiveUpCountsAsDefeat"/> (interface 4.3 open question).
        /// </summary>
        public static void RegisterRunGivenUp(PlayerCharacterConfig cls, int level, int depth)
        {
            if (MetaSettings.Current.GiveUpCountsAsDefeat)
            {
                RegisterRunEnded(cls, false, level, depth);
                return;
            }

            var newlyUnlocked = SettlePendingBattles(false);
            Commit(EProfileChange.RunEnded);
            RaiseUnlocked(newlyUnlocked);
        }

        /// <summary>Results of a class (an empty record if the player never finished a run with it).</summary>
        public static ClassRecord GetClassRecord(PlayerCharacterConfig cls)
        {
            if (cls != null && State.Classes.TryGetValue(cls.Id ?? string.Empty, out var record))
                return record;

            return new ClassRecord();
        }

        /// <summary>
        /// Classes unlocked since the last call, to announce on the end-of-run screen (interface 4.3).
        /// Clears the list.
        /// </summary>
        public static List<PlayerCharacterConfig> ConsumeNewlyUnlockedClasses()
        {
            var state = State;
            var result = new List<PlayerCharacterConfig>();
            if (state.UnannouncedUnlockedClassIds.Count == 0) return result;

            var database = GameDatabase.Instance;
            if (database != null)
            {
                foreach (var id in state.UnannouncedUnlockedClassIds)
                {
                    var config = database.Get<PlayerCharacterConfig>(id);
                    if (config != null)
                        result.Add(config);
                }
            }

            state.UnannouncedUnlockedClassIds.Clear();
            Commit(EProfileChange.ClassesAnnounced);
            return result;
        }

        /// <summary>Whether there are unlocked classes the player was not told about yet.</summary>
        public static bool HasNewlyUnlockedClasses => State.UnannouncedUnlockedClassIds.Count > 0;

        // ---------------------------------------------------------------- glossary (GDD 2.7)

        /// <summary>A talent was chosen: the glossary reveals it for this class.</summary>
        public static void RegisterTalentChosen(PlayerCharacterConfig cls, string talentId)
        {
            if (cls == null || string.IsNullOrEmpty(talentId)) return;

            if (AddToClassList(State.Glossary.ChosenTalents, cls.Id, talentId))
                Commit(EProfileChange.TalentDiscovered);
        }

        /// <summary>
        /// A talent was offered. Only recorded when <see cref="MetaSettings.DiscoverOfferedTalents"/>
        /// is on (otherwise only chosen talents count as discovered).
        /// </summary>
        public static void RegisterTalentOffered(PlayerCharacterConfig cls, string talentId)
        {
            if (cls == null || string.IsNullOrEmpty(talentId)) return;
            if (!MetaSettings.Current.DiscoverOfferedTalents) return;

            if (AddToClassList(State.Glossary.OfferedTalents, cls.Id, talentId))
                Commit(EProfileChange.TalentDiscovered);
        }

        /// <summary>The player has chosen this talent at least once with this class.</summary>
        public static bool HasChosenTalent(PlayerCharacterConfig cls, string talentId)
        {
            return cls != null && !string.IsNullOrEmpty(talentId) &&
                   ClassListContains(State.Glossary.ChosenTalents, cls.Id, talentId);
        }

        /// <summary>
        /// Whether the glossary reveals the talent (chosen, or offered when
        /// <see cref="MetaSettings.DiscoverOfferedTalents"/> is on). Otherwise it shows "?".
        /// </summary>
        public static bool IsTalentDiscovered(PlayerCharacterConfig cls, string talentId)
        {
            if (HasChosenTalent(cls, talentId)) return true;

            return MetaSettings.Current.DiscoverOfferedTalents && cls != null && !string.IsNullOrEmpty(talentId) &&
                   ClassListContains(State.Glossary.OfferedTalents, cls.Id, talentId);
        }

        /// <summary>Talent ids chosen at least once with the class, in discovery order.</summary>
        public static IReadOnlyList<string> GetChosenTalents(PlayerCharacterConfig cls)
        {
            if (cls != null && State.Glossary.ChosenTalents.TryGetValue(cls.Id ?? string.Empty, out var list))
                return list;

            return Array.Empty<string>();
        }

        /// <summary>An enemy type is on the board: the glossary reveals it. Also for summoned enemies.</summary>
        public static void RegisterEnemyFaced(EnemyConfig enemy)
        {
            if (enemy == null) return;
            if (string.IsNullOrEmpty(enemy.Id))
            {
                Debug.LogWarning($"Enemy config '{enemy.name}' has no id; it was not added to the glossary.");
                return;
            }

            var faced = State.Glossary.EnemiesFaced;
            if (faced.Contains(enemy.Id)) return;

            faced.Add(enemy.Id);
            Commit(EProfileChange.EnemyFaced);
        }

        /// <summary>Same as <see cref="RegisterEnemyFaced"/> for several enemies, saving once (a battle starts).</summary>
        public static void RegisterEnemiesFaced(IEnumerable<EnemyConfig> enemies)
        {
            var added = false;
            var faced = State.Glossary.EnemiesFaced;
            foreach (var enemy in enemies)
            {
                if (enemy == null || string.IsNullOrEmpty(enemy.Id) || faced.Contains(enemy.Id)) continue;

                faced.Add(enemy.Id);
                added = true;
            }

            if (added)
                Commit(EProfileChange.EnemyFaced);
        }

        public static bool HasFacedEnemy(string enemyId)
        {
            return !string.IsNullOrEmpty(enemyId) && State.Glossary.EnemiesFaced.Contains(enemyId);
        }

        public static bool HasFacedEnemy(EnemyConfig enemy) => enemy != null && HasFacedEnemy(enemy.Id);

        /// <summary>Enemy ids faced at least once, in discovery order.</summary>
        public static IReadOnlyList<string> EnemiesFaced => State.Glossary.EnemiesFaced;

        // ---------------------------------------------------------------- tutorial tips

        /// <summary>Tip ids already shown.</summary>
        public static bool HasSeenTip(string tipId) => !string.IsNullOrEmpty(tipId) && State.TipsSeen.Contains(tipId);

        internal static void MarkTipSeen(string tipId)
        {
            if (string.IsNullOrEmpty(tipId) || State.TipsSeen.Contains(tipId)) return;

            State.TipsSeen.Add(tipId);
            Commit(EProfileChange.TipSeen);
        }

        /// <summary>Makes every tip show again (e.g. a "replay tips" button in the options).</summary>
        public static void ResetTipsSeen()
        {
            if (State.TipsSeen.Count == 0) return;

            State.TipsSeen.Clear();
            Commit(EProfileChange.TipSeen);
        }

        // ---------------------------------------------------------------- options (interface 4.3)

        /// <summary>Locale code chosen by the player ("en", "es", "pt-BR"), or null for automatic.</summary>
        public static string LocaleCode => State.Options.Locale;

        public static float MusicVolume => State.Options.MusicVolume;
        public static float SfxVolume => State.Options.SfxVolume;

        /// <summary>
        /// Whether tutorial tips are shown. Always true when
        /// <see cref="MetaSettings.AllowDisablingTips"/> is off.
        /// </summary>
        public static bool TipsEnabled => !MetaSettings.Current.AllowDisablingTips || State.Options.TipsEnabled;

        /// <summary>
        /// Chooses the language and applies it right away (Unity Localization). Null or empty means
        /// automatic: the system language, with the project locale (English) as fallback.
        /// </summary>
        public static void SetLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode)) localeCode = null;
            var options = State.Options;
            if (options.Locale == localeCode) return;

            options.Locale = localeCode;
            CommitOptions();
            ApplyLocale();
        }

        /// <param name="persist">
        /// False while a slider is being dragged (applies and announces the value without writing
        /// the file); call again with true, or <see cref="Save"/>, when the drag ends.
        /// </param>
        public static void SetMusicVolume(float volume, bool persist = true)
        {
            volume = Mathf.Clamp01(volume);
            var options = State.Options;
            var changed = !Mathf.Approximately(options.MusicVolume, volume);
            options.MusicVolume = volume;
            CommitVolume(changed, persist);
        }

        /// <inheritdoc cref="SetMusicVolume"/>
        public static void SetSfxVolume(float volume, bool persist = true)
        {
            volume = Mathf.Clamp01(volume);
            var options = State.Options;
            var changed = !Mathf.Approximately(options.SfxVolume, volume);
            options.SfxVolume = volume;
            CommitVolume(changed, persist);
        }

        /// <summary>Ignored (tips stay on) when <see cref="MetaSettings.AllowDisablingTips"/> is off.</summary>
        public static void SetTipsEnabled(bool enabled)
        {
            if (!enabled && !MetaSettings.Current.AllowDisablingTips) return;

            var options = State.Options;
            if (options.TipsEnabled == enabled) return;

            options.TipsEnabled = enabled;
            CommitOptions();
        }

        /// <summary>
        /// Applies the chosen locale to Unity Localization once it is initialized (a null choice
        /// applies the automatic one). Does nothing outside Play Mode so the editor's own locale
        /// is left alone.
        /// </summary>
        public static void ApplyLocale()
        {
            if (!Application.isPlaying) return;

            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
                ApplyLocaleNow();
            else
                initialization.Completed += _ => ApplyLocaleNow();
        }

        private static void ApplyLocaleNow()
        {
            if (!Application.isPlaying) return;

            var available = LocalizationSettings.AvailableLocales;
            Locale locale = null;
            var code = State.Options.Locale;
            if (!string.IsNullOrEmpty(code))
                locale = available.GetLocale(new LocaleIdentifier(code));

            if (locale == null)
                locale = SelectAutomaticLocale(available);

            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }

        /// <summary>System language through the startup selectors configured in Localization Settings, English as fallback.</summary>
        private static Locale SelectAutomaticLocale(ILocalesProvider available)
        {
            foreach (var selector in LocalizationSettings.StartupLocaleSelectors)
            {
                var locale = selector.GetStartupLocale(available);
                if (locale != null) return locale;
            }

            return LocalizationSettings.ProjectLocale;
        }

        // ---------------------------------------------------------------- internals

        private static void EnsureLoaded()
        {
            if (_state == null)
                Load();
        }

        private static ProfileState CreateNew()
        {
            var settings = MetaSettings.Current;
            return new ProfileState
            {
                Options =
                {
                    MusicVolume = settings.DefaultMusicVolume,
                    SfxVolume = settings.DefaultSfxVolume,
                    TipsEnabled = settings.TipsEnabledByDefault,
                },
            };
        }

        /// <summary>One step per saved version; the file is upgraded in memory and saved on the next change.</summary>
        private static void Migrate(ProfileState state)
        {
            if (state.Version > ProfileState.CurrentVersion)
            {
                Debug.LogWarning($"Profile version {state.Version} is newer than this build ({ProfileState.CurrentVersion}); " +
                                 "unknown data may be lost on the next save.");
                return;
            }

            // Future migrations: if (state.Version < 2) { ...; state.Version = 2; }
            state.Version = ProfileState.CurrentVersion;
        }

        /// <summary>Replaces collections that a hand-edited or older file left null.</summary>
        private static void Normalize(ProfileState state)
        {
            state.Classes ??= new Dictionary<string, ClassRecord>();
            state.UnlockedClassIds ??= new List<string>();
            state.UnannouncedUnlockedClassIds ??= new List<string>();
            state.TipsSeen ??= new List<string>();
            state.Options ??= new OptionsState();
            state.Glossary ??= new GlossaryState();
            state.Glossary.ChosenTalents ??= new Dictionary<string, List<string>>();
            state.Glossary.OfferedTalents ??= new Dictionary<string, List<string>>();
            state.Glossary.EnemiesFaced ??= new List<string>();
            state.Options.MusicVolume = Mathf.Clamp01(state.Options.MusicVolume);
            state.Options.SfxVolume = Mathf.Clamp01(state.Options.SfxVolume);
        }

        private static void BackUpUnreadableFile()
        {
            try
            {
                var path = SaveSystem.GetPath(_fileName);
                File.Copy(path, path + ".corrupt", true);
                Debug.LogWarning($"Profile '{_fileName}' could not be read; a copy was kept as '{_fileName}.corrupt'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not back up the unreadable profile '{_fileName}': {exception}");
            }
        }

        /// <summary>
        /// Marks as unlocked every class whose requirement is met. Classes available from the start
        /// are added silently; the others are queued for the end-of-run announcement. Returns the
        /// classes unlocked just now.
        /// </summary>
        private static List<PlayerCharacterConfig> ReconcileUnlocks()
        {
            var newlyUnlocked = new List<PlayerCharacterConfig>();
            var database = GameDatabase.Instance;
            if (database == null) return newlyUnlocked;

            var state = _state;
            foreach (var config in database.GetAll<PlayerCharacterConfig>())
            {
                if (string.IsNullOrEmpty(config.Id) || state.UnlockedClassIds.Contains(config.Id)) continue;
                if (config.BattlesToUnlock > 0 && state.TotalBattlesWon < config.BattlesToUnlock) continue;

                state.UnlockedClassIds.Add(config.Id);
                if (config.BattlesToUnlock <= 0) continue;

                state.UnannouncedUnlockedClassIds.Add(config.Id);
                newlyUnlocked.Add(config);
            }

            return newlyUnlocked;
        }

        /// <summary>Battles held back by CountBattlesFromLostRuns = false are counted if the run was won, or dropped.</summary>
        private static List<PlayerCharacterConfig> SettlePendingBattles(bool victory)
        {
            var state = State;
            var pending = state.PendingBattlesWon;
            state.PendingBattlesWon = 0;
            if (pending <= 0) return new List<PlayerCharacterConfig>();

            if (!victory && !MetaSettings.Current.CountBattlesFromLostRuns)
                return new List<PlayerCharacterConfig>();

            state.TotalBattlesWon += pending;
            return ReconcileUnlocks();
        }

        private static ClassRecord GetOrAddRecord(ProfileState state, string classId)
        {
            if (!state.Classes.TryGetValue(classId, out var record))
            {
                record = new ClassRecord();
                state.Classes[classId] = record;
            }

            return record;
        }

        private static bool AddToClassList(Dictionary<string, List<string>> lists, string classId, string value)
        {
            if (!lists.TryGetValue(classId, out var list))
            {
                list = new List<string>();
                lists[classId] = list;
            }

            if (list.Contains(value)) return false;

            list.Add(value);
            return true;
        }

        private static bool ClassListContains(Dictionary<string, List<string>> lists, string classId, string value)
        {
            return lists.TryGetValue(classId ?? string.Empty, out var list) && list.Contains(value);
        }

        private static void Commit(EProfileChange change)
        {
            Save();
            EventBus.Raise(new ProfileChangedEvent(change));
        }

        private static void CommitOptions(bool persist = true)
        {
            if (persist)
                Save();

            var options = State.Options;
            EventBus.Raise(new OptionsChangedEvent(options.Locale, options.MusicVolume, options.SfxVolume, TipsEnabled));
            EventBus.Raise(new ProfileChangedEvent(EProfileChange.Options));
        }

        private static void CommitVolume(bool changed, bool persist)
        {
            if (changed)
                CommitOptions(persist);
            else if (persist)
                Save();
        }

        private static void RaiseUnlocked(List<PlayerCharacterConfig> classes)
        {
            foreach (var config in classes)
                EventBus.Raise(new ClassUnlockedEvent(config));
        }
    }
}
