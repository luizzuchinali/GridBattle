using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GridBattle.Core;
using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Progression;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.States.Effects;
using GridBattle.Gameplay.Stats;
using GridBattle.Gameplay.Talents;
using GridBattle.Gameplay.Turns;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace GridBattle.Editor.Talents
{
    /// <summary>
    /// Edit-mode checks of the talent module (the game has no test assembly, see AGENTS.md): content sanity,
    /// offer determinism, the filters (level, prerequisites, ranks, bans, skill talent limit, free skill slots),
    /// the synergy weighting (statistical), the session flow (open, choose, reroll, ban, skip, several levels,
    /// the talent node and its resume), the XP curve and the level ups (Max HP per level without healing).
    /// The mechanisms that no current talent uses any more (skill modifiers, the cooldown minimum, some damage
    /// conditions, the reroll modifier) are checked with throwaway fixtures built in memory and destroyed at the
    /// end; nothing is saved and no asset is dirtied. The class identity of the current design is checked on the
    /// data: rule talents of the class, the talents that unlock skills (up to the settings' limit) and the two
    /// upgrade variants of every class skill. Balance numbers (XP curve, Max HP, bonuses) are read from the assets
    /// and checked through their formula. Run from the menu (GridBattle > Talents > Run Checks) or from a script:
    /// <c>GridBattle.Editor.Talents.TalentChecks.RunAll()</c> returns the report. Refuses to run in Play Mode (it
    /// uses throwaway runs and the static turn blockers).
    /// </summary>
    public static class TalentChecks
    {
        private static int _passed;
        private static readonly List<string> Failures = new();
        private static readonly StringBuilder Log = new();
        private static readonly List<Object> FixtureObjects = new();

        [MenuItem("GridBattle/Talents/Run Checks")]
        private static void RunFromMenu()
        {
            var report = RunAll();
            if (report.Contains("FAILED"))
                Debug.LogError(report);
            else
                Debug.Log(report);
        }

        /// <summary>Runs every check and returns a report ("ALL PASSED" or the failures).</summary>
        public static string RunAll()
        {
            if (Application.isPlaying)
                return "FAILED: the talent checks only run in Edit Mode.";

            _passed = 0;
            Failures.Clear();
            Log.Clear();
            TurnBlockers.Clear();

            try
            {
                Run(nameof(CheckContent), CheckContent);
                Run(nameof(CheckDescriptions), CheckDescriptions);
                Run(nameof(CheckXpCurve), CheckXpCurve);
                Run(nameof(CheckLevelUpMaxHp), CheckLevelUpMaxHp);
                Run(nameof(CheckDeterminism), CheckDeterminism);
                Run(nameof(CheckFilters), CheckFilters);
                Run(nameof(CheckSynergyWeights), CheckSynergyWeights);
                Run(nameof(CheckRules), CheckRules);
                Run(nameof(CheckSessionFlow), CheckSessionFlow);
                Run(nameof(CheckToolsAndCounts), CheckToolsAndCounts);
                Run(nameof(CheckTalentNode), CheckTalentNode);
                Run(nameof(CheckPoolExhaustion), CheckPoolExhaustion);
                Run(nameof(CheckSkillResolver), CheckSkillResolver);
                Run(nameof(CheckSkillModifiers), CheckSkillModifiers);
                Run(nameof(CheckCooldownMinimum), CheckCooldownMinimum);
                Run(nameof(CheckDamageAdjustment), CheckDamageAdjustment);
                Run(nameof(CheckConditionalPassives), CheckConditionalPassives);
                Run(nameof(CheckHitReactions), CheckHitReactions);
                Run(nameof(CheckClassIdentity), CheckClassIdentity);
            }
            finally
            {
                TurnBlockers.Clear();
                DestroyFixtures();
            }

            var summary = Failures.Count == 0
                ? $"ALL PASSED ({_passed} assertions)"
                : $"FAILED: {Failures.Count} of {_passed + Failures.Count} assertions\n - " + string.Join("\n - ", Failures);
            return summary + (Log.Length > 0 ? "\n" + Log : string.Empty);
        }

        private static void Run(string name, Action check)
        {
            try
            {
                check();
            }
            catch (Exception exception)
            {
                Failures.Add($"{name} threw {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
                _passed++;
            else
                Failures.Add(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message) =>
            Assert(EqualityComparer<T>.Default.Equals(expected, actual), $"{message} (expected {expected}, got {actual})");

        // ------------------------------------------------------------------ helpers

        private static PlayerCharacterConfig GetClass(ECharacter character)
        {
            foreach (var config in GameDatabase.Instance.GetAll<PlayerCharacterConfig>())
            {
                if (config.CharacterClass == character)
                    return config;
            }

            throw new InvalidOperationException("No class asset for " + character);
        }

        private static TalentDefinition GetTalent(string assetName)
        {
            foreach (var talent in GameDatabase.Instance.GetAll<TalentDefinition>())
            {
                if (talent.name == assetName)
                    return talent;
            }

            throw new InvalidOperationException("No talent asset " + assetName);
        }

        private static SkillDefinition GetSkill(string assetName)
        {
            foreach (var skill in GameDatabase.Instance.GetAll<SkillDefinition>())
            {
                if (skill.name == assetName)
                    return skill;
            }

            throw new InvalidOperationException("No skill asset " + assetName);
        }

        private static StateDefinition LoadState(string assetName)
        {
            var state = AssetDatabase.LoadAssetAtPath<StateDefinition>($"Assets/Application/Settings/States/{assetName}.asset");
            if (state == null)
                throw new InvalidOperationException("No state asset " + assetName);
            return state;
        }

        private static RunState NewRun(PlayerCharacterConfig playerClass, ulong seed, int level = 2)
        {
            var run = new RunState { Random = new RunRandom(seed), ClassId = playerClass.Id, Phase = ERunPhase.Map };
            run.Player.Level = level;
            foreach (var skill in playerClass.Skills)
                run.Player.SkillIds.Add(skill.Id);
            return run;
        }

        private static TalentSession NewSession(RunState run, PlayerCharacterConfig playerClass) =>
            new(run, playerClass, false) { LivePlayerProvider = () => null };

        private static string Ids(IEnumerable<string> ids) =>
            string.Join(",", ids.Select(id =>
            {
                var talent = TalentRules.Resolve(id);
                return talent != null ? talent.name : id;
            }));

        private static Character NewHolder(PlayerCharacterConfig config)
        {
            var holderObject = new GameObject("check_holder_" + config.name) { hideFlags = HideFlags.HideAndDontSave };
            var holder = holderObject.AddComponent<Character>();
            holder.Initialize(config);
            return holder;
        }

        private static void Dispose(params Character[] characters)
        {
            foreach (var character in characters)
            {
                if (character != null)
                    Object.DestroyImmediate(character.gameObject);
            }
        }

        /// <summary>The first effect of type <typeparamref name="T"/> in the talent's states.</summary>
        private static T EffectOf<T>(TalentDefinition talent) where T : StateEffect
        {
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                foreach (var effect in grant.State.Effects)
                {
                    if (effect is T typed)
                        return typed;
                }
            }

            throw new InvalidOperationException($"{talent.name} has no {typeof(T).Name}");
        }

        /// <summary>The stacks the talent's first state has at the rank (one per rank, within the state's maximum).</summary>
        private static int StacksAt(TalentDefinition talent, int rank) => TalentRules.GetStacks(talent.States[0], rank);

        /// <summary>
        /// Max HP with a talent at a rank, from the attribute modifiers of its states: <c>(base + flat) x (1 + percent)</c>,
        /// the formula of <see cref="CharacterStats"/>.
        /// </summary>
        private static int ExpectedMaxHp(int baseMaxHp, TalentDefinition talent, int rank)
        {
            var modifiers = new List<AttributeModifier>();
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                var instance = new StateInstance(grant.State, StateInstance.Permanent, TalentRules.GetStacks(grant, rank), talent.Id);
                foreach (var effect in grant.State.Effects)
                {
                    if (effect != null)
                        effect.CollectModifiers(instance, modifiers);
                }
            }

            var flat = modifiers.Where(m => m.Attribute == EAttribute.MaxHp && m.Type == EModifierType.Flat).Sum(m => m.Value);
            var percent = modifiers.Where(m => m.Attribute == EAttribute.MaxHp && m.Type == EModifierType.Percent).Sum(m => m.Value);
            return Mathf.Max(1, Mathf.RoundToInt((baseMaxHp + flat) * (1f + percent)));
        }

        /// <summary>A run modifier of the talent at a rank, read from its states' effects.</summary>
        private static int ExpectedRunModifier(TalentDefinition talent, int rank, ERunModifier modifier)
        {
            var total = 0;
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                var instance = new StateInstance(grant.State, StateInstance.Permanent, TalentRules.GetStacks(grant, rank), talent.Id);
                foreach (var effect in grant.State.Effects)
                {
                    if (effect != null)
                        total += effect.GetRunModifier(modifier, instance);
                }
            }

            return total;
        }

        /// <summary>
        /// The damage the formula gives with the conditional effect counted <paramref name="count"/> times
        /// (0 = condition not met): what <see cref="CombatResolver.PredictDamage"/> must return when the effect is
        /// the only state that changes the hit.
        /// </summary>
        private static int ExpectedDamage(Character attacker, Character target, int baseDamage, EDamageKind kind,
            ConditionalDamageEffect effect, int count)
        {
            var adjustment = new DamageAdjustment();
            if (count > 0 && effect.AppliesToKinds.Contains(kind))
            {
                if (effect.Direction == EDamageDirection.Dealt)
                    adjustment.AddOutgoing(effect.Percent * count, effect.Flat * count);
                else
                    adjustment.AddIncoming(effect.Percent * count, effect.Flat * count);
            }

            return DamageCalculator.Compute(baseDamage, kind, attacker.Stats, target.Stats, CombatResolver.Settings, null,
                adjustment, out _);
        }

        // ------------------------------------------------------------------ fixtures (memory only)

        private static T NewFixture<T>(string name) where T : ScriptableObject
        {
            var fixture = ScriptableObject.CreateInstance<T>();
            fixture.name = "check_" + name;
            fixture.hideFlags = HideFlags.HideAndDontSave;
            FixtureObjects.Add(fixture);
            return fixture;
        }

        private static void DestroyFixtures()
        {
            foreach (var fixture in FixtureObjects)
            {
                if (fixture != null)
                    Object.DestroyImmediate(fixture);
            }

            FixtureObjects.Clear();
        }

        /// <summary>Sets a field (private ones included, declared on the type or on a base type) by reflection.</summary>
        private static void SetField(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;

                field.SetValue(target, value);
                return;
            }

            throw new InvalidOperationException($"{target.GetType().Name} has no field {name}");
        }

        /// <summary>A state that adds one stack per application (one per talent rank).</summary>
        private static StateDefinition NewStateFixture(string name, int maxStacks, params StateEffect[] effects)
        {
            var state = NewFixture<StateDefinition>(name);
            SetField(state, "id", "talent-checks-state-" + name);
            SetField(state, "stackPolicy", EStackPolicy.AddStacks);
            SetField(state, "maxStacks", maxStacks);
            SetField(state, "effects", new List<StateEffect>(effects));
            return state;
        }

        /// <summary>A talent that is not in the game database (so it never resolves from a run's saved ids).</summary>
        private static TalentDefinition NewTalentFixture(string name, int requiredLevel, int maxRank,
            IEnumerable<TalentDefinition> prerequisites, params StateDefinition[] states)
        {
            var talent = NewFixture<TalentDefinition>(name);
            SetField(talent, "id", "talent-checks-talent-" + name);
            SetField(talent, "requiredLevel", requiredLevel);
            SetField(talent, "maxRank", maxRank);
            SetField(talent, "prerequisites",
                prerequisites != null ? prerequisites.ToList() : new List<TalentDefinition>());
            SetField(talent, "states", states.Select(state => new StateGrant(state, 1, 1, true)).ToList());
            return talent;
        }

        private static ConditionalDamageEffect NewConditional(EDamageCondition condition, EDamageKindMask kinds, float flat)
        {
            var effect = new ConditionalDamageEffect();
            SetField(effect, "direction", EDamageDirection.Dealt);
            SetField(effect, "appliesTo", kinds);
            SetField(effect, "condition", condition);
            SetField(effect, "percent", 0f);
            SetField(effect, "flat", flat);
            return effect;
        }

        private static TalentDefinition NewConditionalTalent(string name, EDamageCondition condition, EDamageKindMask kinds,
            float flat, int maxRank)
        {
            var state = NewStateFixture(name, maxRank, NewConditional(condition, kinds, flat));
            return NewTalentFixture(name, 2, maxRank, null, state);
        }

        // ------------------------------------------------------------------ content

        private static void CheckContent()
        {
            var database = GameDatabase.Instance;
            var settings = TalentOfferSettings.Current;
            var progression = ProgressionSettings.Current;
            var talents = database.GetAll<TalentDefinition>();
            Assert(talents.Count > 0, "the game has talents");

            foreach (var talent in talents)
            {
                Assert(!string.IsNullOrEmpty(talent.Id), $"{talent.name} has no id");
                Assert(talent.Icon != null, $"{talent.name} has no icon");
                Assert(talent.DisplayName != null && !talent.DisplayName.IsEmpty, $"{talent.name} has no localized name");
                Assert(talent.Description != null && !talent.Description.IsEmpty, $"{talent.name} has no localized description");
                Assert(talent.SynergyTags.Count > 0, $"{talent.name} has no synergy tag");
                Assert(talent.States.Count > 0 || talent.IsSkillTalent, $"{talent.name} grants nothing");

                foreach (var grant in talent.States)
                {
                    Assert(grant.State != null, $"{talent.name} has an empty state grant");
                    if (grant.State != null)
                    {
                        Assert(grant.State.MaxStacks >= talent.MaxRank * grant.Stacks,
                            $"{talent.name}: state {grant.State.name} allows {grant.State.MaxStacks} stacks, the talent needs {talent.MaxRank * grant.Stacks}");
                        Assert(grant.State.StackPolicy == EStackPolicy.AddStacks || talent.MaxRank == 1,
                            $"{talent.name}: state {grant.State.name} does not stack");
                    }
                }

                foreach (var prerequisite in talent.Prerequisites)
                    Assert(prerequisite != null && prerequisite != talent, $"{talent.name} has an invalid prerequisite");
            }

            foreach (var playerClass in database.GetAll<PlayerCharacterConfig>())
            {
                var pool = TalentRules.GetPool(playerClass, settings);
                var rankSlots = pool.Sum(t => t.MaxRank);
                var skillTalents = pool.Count(t => t.IsSkillTalent);
                Log.AppendLine($"pool {playerClass.name}: {pool.Count} talents, {rankSlots} rank slots, {skillTalents} skill talents");
                Assert(rankSlots >= progression.MaxLevel + 3,
                    $"{playerClass.name}: {rankSlots} rank slots cannot fill {progression.MaxLevel - 1} level ups and the talent nodes");

                // Every talent of a class's own pool is allowed for that class.
                foreach (var talent in playerClass.TalentPool)
                    Assert(talent != null && talent.IsAllowedFor(playerClass), $"{(talent != null ? talent.name : "null")} is in {playerClass.name}'s pool but not allowed for it");
            }

            // The shared pool (the generic talents): it exists, every entry is valid and class-agnostic. Its size is
            // content, not a rule (the 2026-10-19 design removed the number talents, see talentos_e_oferta.md).
            Log.AppendLine($"shared pool: {settings.SharedPool.Count} talents");
            Assert(settings.SharedPool.Count > 0, "the shared pool has talents");
            foreach (var talent in settings.SharedPool)
            {
                Assert(talent != null, "the shared pool has an empty entry");
                if (talent != null)
                    Assert(talent.AllowedClasses.Count == 0, $"{talent.name} is in the shared pool but limited to some classes");
            }
        }

        // ------------------------------------------------------------------ descriptions

        /// <summary>Whether the effect has numbers the player needs to read in the talent's description.</summary>
        private static bool HasPlayerFacingNumbers(StateEffect effect) => effect switch
        {
            ConditionalDamageEffect conditional => conditional.Flat != 0f || conditional.Percent != 0f,
            OnKillEffect onKill => onKill.HealFlat > 0 || onKill.HealMaxHpFraction > 0f || onKill.CooldownReduction > 0,
            SkillModifierEffect modifier => modifier.DamageFlat != 0 || modifier.DamagePercent != 0f || modifier.AreaSteps != 0 ||
                                            modifier.Range != 0 || modifier.CooldownReduction != 0 || modifier.Displacement != 0 ||
                                            modifier.StateDuration != 0,
            AttributeModifierEffect _ => true,
            RunModifierEffect _ => true,
            // Rule talents (RuleTalentEffects.cs), on-hit states (the state's own details carry its numbers) and
            // anything else describe a rule, not numbers.
            _ => false,
        };

        private static string GetEntry(StringTableCollection collection, string code, string key)
        {
            var table = collection.GetTable(code) as StringTable;
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null ? entry.Value : null;
        }

        /// <summary>
        /// Every talent has its name and description in the three languages, and the description states the numbers
        /// when the talent's effects have numbers the player must know (rule talents are left out).
        /// </summary>
        private static void CheckDescriptions()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("Content");
            Assert(collection != null, "the Content string table exists");
            if (collection == null) return;

            foreach (var talent in GameDatabase.Instance.GetAll<TalentDefinition>())
            {
                var nameKey = talent.DisplayName.TableEntryReference.Key;
                var descriptionKey = talent.Description.TableEntryReference.Key;
                var needsNumbers = talent.States.Any(g => g.IsValid && g.State.Effects.Any(e => e != null && HasPlayerFacingNumbers(e)));
                foreach (var code in new[] { "en", "es", "pt-BR" })
                {
                    Assert(!string.IsNullOrEmpty(GetEntry(collection, code, nameKey)), $"{talent.name} name in {code}");
                    var description = GetEntry(collection, code, descriptionKey);
                    Assert(!string.IsNullOrEmpty(description), $"{talent.name} description in {code}");
                    if (needsNumbers)
                        Assert(!string.IsNullOrEmpty(description) && description.Any(char.IsDigit),
                            $"{talent.name}: the {code} description states its numbers ({description})");
                }
            }
        }

        // ------------------------------------------------------------------ XP curve

        /// <summary>The curve follows <c>base + (level - 1) x growth</c> with the asset's values; the rules hold.</summary>
        private static void CheckXpCurve()
        {
            var progression = ProgressionSettings.Current;
            var baseXp = progression.BaseXpToLevelUp;
            var growth = progression.XpToLevelUpGrowthPerLevel;
            Log.AppendLine($"XP curve: {baseXp} + {growth} per level, max level {progression.MaxLevel}, leftover XP carried over: {progression.CarryOverLeftoverXp}");

            Assert(baseXp > 0, "the first level costs XP");
            AssertEqual(baseXp, progression.GetXpToNextLevel(1), "XP from level 1 is the base");
            AssertEqual(baseXp + growth, progression.GetXpToNextLevel(2), "XP from level 2 is base + growth");
            AssertEqual(baseXp + (progression.MaxLevel - 1) * growth, progression.GetXpToNextLevel(progression.MaxLevel),
                "XP at the max level follows the formula");

            for (var level = 1; level < progression.MaxLevel; level++)
                Assert(progression.GetXpToNextLevel(level + 1) >= progression.GetXpToNextLevel(level),
                    $"the XP to the next level never goes down (level {level})");

            var total = 0;
            for (var level = 2; level <= progression.MaxLevel; level++)
            {
                total += progression.GetXpToNextLevel(level - 1);
                AssertEqual(total, progression.GetTotalXpToReach(level), $"total XP to reach level {level}");
                if (!progression.CarryOverLeftoverXp) continue;

                // With the leftover kept, the total XP decides the level.
                AssertEqual(level - 1, XpProgressionSimulator.GetLevel(total - 1, progression), $"{total - 1} XP is still level {level - 1}");
                AssertEqual(level, XpProgressionSimulator.GetLevel(total, progression), $"{total} XP is level {level}");
            }

            if (progression.CarryOverLeftoverXp)
                AssertEqual(progression.MaxLevel, XpProgressionSimulator.GetLevel(10_000_000, progression), "the level cap holds");
            AssertEqual(progression.GetTotalXpToReach(progression.MaxLevel), progression.GetTotalXpToReach(progression.MaxLevel + 5),
                "no XP is counted above the cap");

            // Leftover XP: one level's cost plus the next level's cost reaches level 3 only when the leftover is kept.
            if (progression.MaxLevel >= 3)
            {
                var twoLevels = progression.GetXpToNextLevel(1) + progression.GetXpToNextLevel(2);
                AssertEqual(progression.CarryOverLeftoverXp ? 3 : 2, XpProgressionSimulator.GetLevel(twoLevels, progression),
                    "leftover XP carries over to the next level as the setting says");
            }
        }

        /// <summary>
        /// A real level up (<see cref="PlayerCharacter.GainXp"/>): the leftover XP stays, the Max HP grows by the
        /// setting's amount per level, the HP heals only the setting's fraction, and the cap holds. Also the run's
        /// Max HP outside battles counts the levels.
        /// </summary>
        private static void CheckLevelUpMaxHp()
        {
            var progression = ProgressionSettings.Current;
            var knight = GetClass(ECharacter.Warrior);
            Log.AppendLine($"level up: +{progression.MaxHpPerLevel} max HP per level, heals {progression.LevelUpHealFraction:P0} of max HP");

            for (var level = 1; level <= progression.MaxLevel; level++)
                AssertEqual((level - 1) * progression.MaxHpPerLevel, progression.GetMaxHpBonus(level), $"Max HP bonus at level {level}");

            var run = NewRun(knight, 1UL, 1);
            AssertEqual(knight.MaxHp, RunPlayerHooks.GetMaxHp(run.Player, knight), "the run's Max HP at level 1 is the class's");
            run.Player.Level = 4;
            run.Player.MaxHpBonus = -2;
            AssertEqual(knight.MaxHp + progression.GetMaxHpBonus(4) - 2, RunPlayerHooks.GetMaxHp(run.Player, knight),
                "the run's Max HP counts the levels and the run's lasting bonus");

            if (progression.MaxLevel < 2) return;

            var holderObject = new GameObject("check_player") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var player = holderObject.AddComponent<PlayerCharacter>();
                player.Initialize(knight);
                player.SetProgress(1, 0);
                AssertEqual(knight.MaxHp, player.MaxHp, "a level 1 player has the class's Max HP");

                var hpBefore = Mathf.Max(1, player.MaxHp - 3);
                player.SetHp(hpBefore);
                var leftover = Mathf.Min(1, progression.GetXpToNextLevel(2) - 1);
                player.GainXp(progression.GetXpToNextLevel(1) + leftover);
                AssertEqual(2, player.Level, "one level's XP gives one level");
                AssertEqual(progression.CarryOverLeftoverXp ? leftover : 0, player.CurrentXp, "the leftover XP stays as the setting says");
                AssertEqual(knight.MaxHp + progression.GetMaxHpBonus(2), player.MaxHp, "a level up adds the Max HP per level");
                var heal = Mathf.RoundToInt(player.MaxHp * progression.LevelUpHealFraction);
                AssertEqual(Mathf.Min(player.MaxHp, hpBefore + heal), player.Current,
                    "a level up heals only the setting's fraction (none when it is 0)");

                // Without the leftover a single gain gives one level at most, so the cap is only reached with it.
                if (progression.CarryOverLeftoverXp)
                {
                    player.GainXp(10_000_000);
                    AssertEqual(progression.MaxLevel, player.Level, "the level cap holds for the player");
                    Assert(player.CurrentXp <= player.XpToNextLevel, "at the cap the XP bar stays at most full");
                    AssertEqual(knight.MaxHp + progression.GetMaxHpBonus(progression.MaxLevel), player.MaxHp, "Max HP at the cap");
                }
            }
            finally
            {
                Object.DestroyImmediate(holderObject);
            }
        }

        // ------------------------------------------------------------------ determinism

        private static void CheckDeterminism()
        {
            var settings = TalentOfferSettings.Current;
            var knight = GetClass(ECharacter.Warrior);

            var run = NewRun(knight, 12345UL, 5);
            var a = TalentOfferGenerator.Generate(run.Player, knight, 5, 3, settings, run.Random, 5, 0);
            var b = TalentOfferGenerator.Generate(run.Player, knight, 5, 3, settings, new RunRandom(12345UL), 5, 0);
            AssertEqual(3, a.Count, "an offer has 3 options");
            AssertEqual(Ids(a), Ids(b), "same seed, key and reroll index give the same offer");

            // Drawing other things from the persistent Talents stream never moves a keyed draw.
            run.Random.Get(ERandomStream.Talents).NextULong();
            var c = TalentOfferGenerator.Generate(run.Player, knight, 5, 3, settings, run.Random, 5, 0);
            AssertEqual(Ids(a), Ids(c), "a keyed draw does not depend on the persistent stream");

            var differentReroll = 0;
            var differentKey = 0;
            for (var seed = 1UL; seed <= 40UL; seed++)
            {
                var random = new RunRandom(seed);
                var player = NewRun(knight, seed, 6).Player;
                var first = Ids(TalentOfferGenerator.Generate(player, knight, 6, 3, settings, random, 6, 0));
                var rerolled = Ids(TalentOfferGenerator.Generate(player, knight, 6, 3, settings, random, 6, 1));
                var otherLevel = Ids(TalentOfferGenerator.Generate(player, knight, 6, 3, settings, random, 7, 0));
                if (first != rerolled) differentReroll++;
                if (first != otherLevel) differentKey++;
            }

            Assert(differentReroll >= 36, $"the reroll index must change the offer (changed {differentReroll}/40)");
            Assert(differentKey >= 36, $"the offer key must change the offer (changed {differentKey}/40)");

            // Different keys for a node and a level never collide.
            Assert(TalentOfferGenerator.GetNodeKey(0) != TalentOfferGenerator.GetLevelKey(30), "node and level keys are apart");

            // The draw has no repeated talent.
            for (var seed = 1UL; seed <= 40UL; seed++)
            {
                var player = NewRun(knight, seed, 10).Player;
                var offer = TalentOfferGenerator.Generate(player, knight, 10, 5, settings, new RunRandom(seed), 10, 0);
                AssertEqual(offer.Count, offer.Distinct().Count(), "an offer draws without replacement");
            }
        }

        // ------------------------------------------------------------------ filters

        private static void CheckFilters()
        {
            var settings = TalentOfferSettings.Current;
            var knight = GetClass(ECharacter.Warrior);
            var mage = GetClass(ECharacter.Mage);
            var rogue = GetClass(ECharacter.Rogue);

            // Class: a Warrior never sees the Mage's talents, and the shared ones reach everyone.
            var run = NewRun(knight, 1UL, 30);
            var warriorOptions = TalentOfferGenerator.GetCandidates(run.Player, knight, 30, settings).Select(c => c.Talent).ToList();
            Assert(warriorOptions.Contains(GetTalent("Muralha")), "the Warrior can be offered Bulwark");
            Assert(!warriorOptions.Contains(GetTalent("Grimorio")), "the Warrior is not offered Mage talents");
            Assert(warriorOptions.Contains(GetTalent("Vitality")), "shared talents reach the Warrior");

            // Level and prerequisite (no current talent has a prerequisite: two fixtures in a copy of the settings).
            var entry = NewTalentFixture("Entry", 2, 1, null);
            var gated = NewTalentFixture("Gated", 4, 1, new[] { entry });
            var withFixtures = Object.Instantiate(settings);
            withFixtures.hideFlags = HideFlags.HideAndDontSave;
            FixtureObjects.Add(withFixtures);
            SetField(withFixtures, "sharedPool", new List<TalentDefinition>(settings.SharedPool) { entry, gated });

            var lowLevel = NewRun(knight, 1UL, 3);
            TalentRules.AddRank(lowLevel.Player, entry);
            Assert(!TalentOfferGenerator.GetCandidates(lowLevel.Player, knight, 3, withFixtures).Any(c => c.Talent == gated),
                "a level below the required level does not offer the talent");
            Assert(TalentOfferGenerator.GetCandidates(lowLevel.Player, knight, 4, withFixtures).Any(c => c.Talent == gated),
                "the required level with the prerequisite offers the talent");
            var noPrerequisite = NewRun(knight, 1UL, 10);
            Assert(TalentOfferGenerator.GetCandidates(noPrerequisite.Player, knight, 10, withFixtures).Any(c => c.Talent == entry),
                "a talent without prerequisites is offered");
            Assert(!TalentOfferGenerator.GetCandidates(noPrerequisite.Player, knight, 10, withFixtures).Any(c => c.Talent == gated),
                "a talent is not offered before its prerequisite");

            // Ranks: a talent at max rank leaves the pool, below it stays (Vitality has ranks).
            var vitality = GetTalent("Vitality");
            Assert(vitality.MaxRank > 1, "Vitality has more than one rank (needed by the rank checks)");
            var ranked = NewRun(knight, 1UL, 10);
            for (var i = 0; i < vitality.MaxRank - 1; i++)
                TalentRules.AddRank(ranked.Player, vitality);
            Assert(TalentOfferGenerator.GetCandidates(ranked.Player, knight, 10, settings).Any(c => c.Talent == vitality),
                "a talent below its max rank can be offered again");
            TalentRules.AddRank(ranked.Player, vitality);
            AssertEqual(vitality.MaxRank, TalentRules.GetRank(ranked.Player, vitality), "rank reached the maximum");
            TalentRules.AddRank(ranked.Player, vitality);
            AssertEqual(vitality.MaxRank, TalentRules.GetRank(ranked.Player, vitality), "rank never goes above the maximum");
            Assert(!TalentOfferGenerator.GetCandidates(ranked.Player, knight, 10, settings).Any(c => c.Talent == vitality),
                "a talent at its max rank leaves the pool");

            // Bans.
            var banned = NewRun(knight, 1UL, 10);
            banned.Player.BannedTalentIds.Add(vitality.Id);
            Assert(!TalentOfferGenerator.GetCandidates(banned.Player, knight, 10, settings).Any(c => c.Talent == vitality),
                "a banned talent is not offered");
            for (var seed = 1UL; seed <= 30UL; seed++)
            {
                var offer = TalentOfferGenerator.Generate(banned.Player, knight, 10, 3, settings, new RunRandom(seed), 10, 0);
                Assert(!offer.Contains(vitality.Id), "a banned talent never shows up in a draw");
            }

            // Skill talent limit: with a limit of 1, a second skill talent is not offered.
            var limited = Object.Instantiate(settings);
            limited.hideFlags = HideFlags.HideAndDontSave;
            FixtureObjects.Add(limited);
            SetField(limited, "maxSkillTalents", 1);
            var mageRun = NewRun(mage, 1UL, 30);
            var grimoire = GetTalent("Grimorio");
            Assert(TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, limited).Any(c => c.Talent == grimoire),
                "skill talents are offered below the limit");
            GrantSkillTalent(mageRun.Player, grimoire);
            var after = TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, limited);
            Assert(!after.Any(c => c.Talent.IsSkillTalent), "once the skill talent limit is reached no skill talent is offered");
            var withDefault = TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, settings);
            if (settings.MaxSkillTalents == 0 || settings.MaxSkillTalents > 1)
                Assert(withDefault.Any(c => c.Talent.IsSkillTalent),
                    $"with the settings' limit ({settings.MaxSkillTalents}) the other skill talents stay available");
            Assert(!withDefault.Any(c => c.Talent == grimoire), "a talent already taken (max rank 1) is not offered again");

            // The settings' limit itself: after that many skill talents, none is offered.
            if (settings.MaxSkillTalents > 0)
            {
                var full = NewRun(mage, 2UL, 30);
                var mageSkillTalents = mage.TalentPool.Where(t => t != null && t.IsSkillTalent).ToList();
                foreach (var talent in mageSkillTalents.Take(settings.MaxSkillTalents))
                    GrantSkillTalent(full.Player, talent);
                if (mageSkillTalents.Count >= settings.MaxSkillTalents)
                    Assert(!TalentOfferGenerator.GetCandidates(full.Player, mage, 30, settings).Any(c => c.Talent.IsSkillTalent),
                        $"after {settings.MaxSkillTalents} skill talents no skill talent is offered");
            }

            // Free skill slots: a Rogue whose slots are all taken is offered no skill talent.
            var slots = SkillSettings.Current.MaxSkillSlots;
            var rogueUnlocks = rogue.TalentPool.Where(t => t != null && t.IsSkillTalent).Select(t => t.UnlockedSkill).ToList();
            var ownedForSlots = GameDatabase.Instance.GetAll<SkillDefinition>()
                .Where(s => s.UpgradeOf == null && !rogueUnlocks.Any(s.IsSameSkillAs) && !rogue.Skills.Any(s.IsSameSkillAs))
                .Take(slots).ToList();
            var fullBar = NewRun(rogue, 1UL, 30);
            fullBar.Player.SkillIds.Clear();
            foreach (var skill in ownedForSlots)
                fullBar.Player.SkillIds.Add(skill.Id);
            AssertEqual(slots, ownedForSlots.Count, "enough non-Rogue skills exist to fill the bar");
            AssertEqual(0, TalentRules.GetFreeSkillSlots(fullBar.Player, rogue), "the skill bar is full");
            Assert(!TalentOfferGenerator.GetCandidates(fullBar.Player, rogue, 30, settings).Any(c => c.Talent.IsSkillTalent),
                "no skill talent is offered without a free skill slot");
            Assert(TalentOfferGenerator.GetCandidates(fullBar.Player, rogue, 30, settings).Count > 0,
                "other talents are still offered when the skill bar is full");

            // A skill the player already has (or an upgrade variant of it) is not unlocked twice.
            var arsenal = GetTalent("ArsenalOculto");
            var owner = NewRun(rogue, 1UL, 30);
            owner.Player.SkillIds.Add(arsenal.UnlockedSkill.Id);
            Assert(!TalentOfferGenerator.GetCandidates(owner.Player, rogue, 30, settings).Any(c => c.Talent == arsenal),
                "a talent that unlocks a skill the player already has is not offered");
            var variant = arsenal.UnlockedSkill.Upgrades.FirstOrDefault(u => u != null);
            if (variant != null)
            {
                var variantOwner = NewRun(rogue, 1UL, 30);
                variantOwner.Player.SkillIds.Add(variant.Id);
                Assert(!TalentOfferGenerator.GetCandidates(variantOwner.Player, rogue, 30, settings).Any(c => c.Talent == arsenal),
                    "a talent whose skill the player has as an upgrade variant is not offered");
            }

            // Start-of-run skill slots follow SkillSettings.StartingSkillsUseSlots.
            var warriorSlots = NewRun(knight, 1UL, 2);
            AssertEqual(SkillSettings.Current.StartingSkillsUseSlots ? knight.Skills.Count : 0, TalentRules.GetUsedSkillSlots(warriorSlots.Player, knight),
                "starting skills use slots as the skill settings say");
        }

        private static void GrantSkillTalent(PlayerRunState player, TalentDefinition talent)
        {
            TalentRules.AddRank(player, talent);
            player.SkillIds.Add(talent.UnlockedSkill.Id);
        }

        // ------------------------------------------------------------------ synergy

        /// <summary>
        /// The draw weight is <c>base x shared multiplier (generic talents only) x (1 + bonus x shared tags)</c>, and many
        /// single draws follow the weights. Uses a Warrior who took Shatter and Bulwark.
        /// </summary>
        private static void CheckSynergyWeights()
        {
            var settings = TalentOfferSettings.Current;
            var knight = GetClass(ECharacter.Warrior);

            var run = NewRun(knight, 1UL, 10);
            var taken = new[] { GetTalent("Shatter"), GetTalent("Muralha") };
            foreach (var talent in taken)
                TalentRules.AddRank(run.Player, talent);

            var takenTags = new Dictionary<SynergyTagDefinition, int>();
            foreach (var talent in taken)
            {
                foreach (var tag in talent.SynergyTags)
                {
                    if (tag == null) continue;
                    takenTags.TryGetValue(tag, out var count);
                    takenTags[tag] = count + 1;
                }
            }

            int Synergy(TalentDefinition talent)
            {
                var seen = new List<SynergyTagDefinition>();
                var synergy = 0;
                foreach (var tag in talent.SynergyTags)
                {
                    if (tag == null || seen.Contains(tag)) continue;
                    seen.Add(tag);
                    if (takenTags.TryGetValue(tag, out var occurrences))
                        synergy += settings.SynergyCounting == ESynergyCounting.TagOccurrences ? occurrences : 1;
                }

                return synergy;
            }

            bool IsGeneric(TalentDefinition talent) => settings.SharedPool.Contains(talent) && !knight.TalentPool.Contains(talent);

            var tags = TalentRules.GetTakenTagCounts(run.Player);
            var candidates = TalentOfferGenerator.GetCandidates(run.Player, knight, 10, settings);
            var bonus = settings.SynergyBonusPerSharedTag;
            foreach (var candidate in candidates)
            {
                var talent = candidate.Talent;
                var expected = talent.BaseWeight * (IsGeneric(talent) ? settings.SharedPoolWeightMultiplier : 1f) * (1f + bonus * Synergy(talent));
                Assert(Mathf.Approximately(expected, candidate.Weight),
                    $"{talent.name}: weight {candidate.Weight}, the formula gives {expected}");
                AssertEqual(talent.SynergyTags.Where(t => t != null).Distinct().Count(t => takenTags.ContainsKey(t)),
                    TalentRules.GetSharedTags(talent, tags).Count, $"{talent.name}: shared tags listed for the choice screen");
            }

            var plain = candidates.Where(c => Synergy(c.Talent) == 0).Select(c => c.Talent).FirstOrDefault();
            var paired = candidates.Where(c => TalentRules.GetSharedTags(c.Talent, tags).Count >= 2).Select(c => c.Talent).FirstOrDefault();
            Assert(plain != null, "a candidate shares no tag with the build (needed by the draw check)");
            Assert(paired != null, "a candidate shares two tags with the build (needed by the draw check)");

            // Statistical: the first pick of many single-option draws follows weight / total.
            var total = candidates.Sum(c => c.Weight);
            var counts = new Dictionary<string, int>();
            const int Draws = 30000;
            for (var key = 0; key < Draws; key++)
            {
                var pick = TalentOfferGenerator.Generate(run.Player, knight, 10, 1, settings, run.Random, 10_000 + key, 0)[0];
                var name = TalentRules.Resolve(pick).name;
                counts.TryGetValue(name, out var current);
                counts[name] = current + 1;
            }

            var worst = 0.0;
            foreach (var candidate in candidates)
            {
                counts.TryGetValue(candidate.Talent.name, out var observed);
                var expectedShare = candidate.Weight / total;
                var share = observed / (double)Draws;
                worst = Math.Max(worst, Math.Abs(share - expectedShare));
            }

            Log.AppendLine($"synergy draw: {Draws} single draws, largest gap between observed and expected share {worst:P2}");
            Assert(worst < 0.012, $"observed draw shares differ from the weights by {worst:P2}");

            if (plain != null && paired != null && bonus > 0f)
            {
                // Per unit of base weight, a talent with two shared tags shows up about (1 + 2 x bonus) times as often.
                double PerBase(TalentDefinition talent) =>
                    counts.GetValueOrDefault(talent.name) / (double)Draws /
                    (talent.BaseWeight * (IsGeneric(talent) ? settings.SharedPoolWeightMultiplier : 1f));
                Assert(PerBase(paired) > PerBase(plain) * (1f + 2f * bonus) * 0.85f,
                    $"{paired.name} (two shared tags) is drawn more often than {plain.name} (none), as the weights say");
            }
        }

        // ------------------------------------------------------------------ rules over the run data

        private static void CheckRules()
        {
            var knight = GetClass(ECharacter.Warrior);
            var run = NewRun(knight, 1UL, 10);
            AssertEqual(knight.MaxHp, TalentRules.GetMaxHp(run.Player, knight.MaxHp), "no talents, no Max HP bonus");

            // Vitality: the Max HP of its states, per rank (the numbers come from the asset).
            var vitality = GetTalent("Vitality");
            var rankOne = ExpectedMaxHp(knight.MaxHp, vitality, 1);
            Assert(rankOne > knight.MaxHp, "Vitality adds Max HP");
            TalentRules.AddRank(run.Player, vitality);
            AssertEqual(rankOne, TalentRules.GetMaxHp(run.Player, knight.MaxHp), "Vitality rank 1 Max HP");
            if (vitality.MaxRank > 1)
            {
                TalentRules.AddRank(run.Player, vitality);
                var rankTwo = ExpectedMaxHp(knight.MaxHp, vitality, 2);
                Assert(rankTwo > rankOne, "Vitality rank 2 adds more than rank 1");
                AssertEqual(rankTwo, TalentRules.GetMaxHp(run.Player, knight.MaxHp), "Vitality rank 2 Max HP");
                AssertEqual(rankTwo, new TalentRunModifier().ModifyMaxHp(run.Player, knight, knight.MaxHp), "the run hook reports the same Max HP");
            }

            AssertEqual(0, TalentRules.GetRunModifier(run.Player, ERunModifier.TalentOptions), "no option modifier yet");
            var wider = GetTalent("WiderChoice");
            var banisher = GetTalent("Banisher");
            TalentRules.AddRank(run.Player, wider);
            TalentRules.AddRank(run.Player, banisher);
            TalentRules.AddRank(run.Player, banisher);
            var options = ExpectedRunModifier(wider, 1, ERunModifier.TalentOptions);
            var bans = ExpectedRunModifier(banisher, TalentRules.GetRank(run.Player, banisher), ERunModifier.Bans);
            Assert(options > 0, "Wider Choice adds options");
            Assert(bans > 0, "Banisher adds bans");
            if (banisher.MaxRank > 1)
                AssertEqual(2 * ExpectedRunModifier(banisher, 1, ERunModifier.Bans), bans, "Banisher rank 2 gives twice rank 1");
            AssertEqual(options, TalentRules.GetRunModifier(run.Player, ERunModifier.TalentOptions), "Wider Choice: options modifier");
            AssertEqual(bans, TalentRules.GetRunModifier(run.Player, ERunModifier.Bans), "Banisher: bans modifier");
            AssertEqual(0, TalentRules.GetRunModifier(run.Player, ERunModifier.Rerolls), "no reroll modifier");
            AssertEqual(0, TalentRules.GetRunModifier(run.Player, ERunModifier.Skips), "no skip modifier");

            var settings = TalentOfferSettings.Current;
            var session = NewSession(run, knight);
            AssertEqual(settings.GetOptionCount(options), session.OptionCount, "options per offer = base + modifier (capped)");
            AssertEqual(settings.BaseRerolls, session.RerollsLeft, "rerolls = base");
            AssertEqual(settings.BaseBans + bans, session.BansLeft, "bans = base + modifier");
            AssertEqual(settings.BaseSkips, session.SkipsLeft, "skips = base");
            AssertEqual(settings.MaxOptionsPerOffer, settings.GetOptionCount(99), "the option count is capped");
            session.Dispose();

            // No current talent changes rerolls or skips: the run modifier effect itself, on a fixture state.
            var rerollEffect = new RunModifierEffect();
            SetField(rerollEffect, "modifier", ERunModifier.Rerolls);
            SetField(rerollEffect, "amount", 1);
            var skipEffect = new RunModifierEffect();
            SetField(skipEffect, "modifier", ERunModifier.Skips);
            SetField(skipEffect, "amount", 1);
            var toolState = NewStateFixture("Tools", 3, rerollEffect, skipEffect);
            var holder = NewHolder(knight);
            holder.States.Apply(toolState, StateInstance.Permanent, 2, "talent-checks-source");
            AssertEqual(2, holder.States.GetRunModifier(ERunModifier.Rerolls), "a reroll modifier counts once per stack");
            AssertEqual(2, holder.States.GetRunModifier(ERunModifier.Skips), "a skip modifier counts once per stack");
            AssertEqual(0, holder.States.GetRunModifier(ERunModifier.Bans), "a modifier only changes its own tool");
            Dispose(holder);

            // A talent state of a run is recognized as the talent module's (not carried between battles).
            var instance = new StateInstance(vitality.States[0].State, StateInstance.Permanent, 2, vitality.Id);
            Assert(new TalentRunModifier().OwnsState(run.Player, instance), "talent states are owned by the talent module");
            var foreign = new StateInstance(vitality.States[0].State, StateInstance.Permanent, 1, null);
            Assert(!new TalentRunModifier().OwnsState(run.Player, foreign), "states without a talent source are not");
        }

        // ------------------------------------------------------------------ session flow

        private static void CheckSessionFlow()
        {
            var knight = GetClass(ECharacter.Warrior);
            var settings = TalentOfferSettings.Current;
            var opened = new List<TalentOfferOpenedEvent>();
            var resolved = new List<TalentOfferResolvedEvent>();
            var acquired = new List<TalentAcquiredEvent>();
            Action<TalentOfferOpenedEvent> onOpened = opened.Add;
            Action<TalentOfferResolvedEvent> onResolved = resolved.Add;
            Action<TalentAcquiredEvent> onAcquired = acquired.Add;
            EventBus.Subscribe(onOpened);
            EventBus.Subscribe(onResolved);
            EventBus.Subscribe(onAcquired);

            try
            {
                // One level: the offer opens, the game is paused, choosing resumes it.
                var run = NewRun(knight, 777UL, 2);
                var session = NewSession(run, knight);
                session.EnqueueLevelUp(2);
                Assert(session.HasOffer, "a level up opens an offer");
                Assert(TurnBlockers.IsBlocked, "an open offer pauses the game");
                Assert(TurnBlockers.Reasons.Contains("Talent choice"), "the pause is the 'Talent choice' blocker");
                AssertEqual(1, opened.Count, "TalentOfferOpenedEvent raised once");
                AssertEqual(settings.OptionsPerOffer, session.CurrentOffer.Options.Count, "the offer shows the settings' options");
                AssertEqual(ETalentOfferSource.LevelUp, session.CurrentOffer.Source, "level up source");
                AssertEqual(2, session.CurrentOffer.Level, "offer level");
                Assert(!session.Choose(7), "an invalid index chooses nothing");
                Assert(session.HasOffer, "the offer stays open after an invalid choice");

                var chosen = session.CurrentOffer.Options[1].Talent;
                Assert(session.Choose(1), "choosing works");
                AssertEqual(1, TalentRules.GetRank(run.Player, chosen), "the talent is in the run at rank 1");
                AssertEqual(1, acquired.Count, "TalentAcquiredEvent raised");
                AssertEqual(1, resolved.Count, "TalentOfferResolvedEvent raised");
                AssertEqual(ETalentOfferResult.Chosen, resolved[0].Result, "resolved as chosen");
                Assert(resolved[0].Chosen == chosen, "the event carries the chosen talent");
                Assert(!session.HasOffer, "no offer left");
                Assert(!TurnBlockers.IsBlocked, "the game resumes after the last offer");
                AssertEqual(0, run.PendingOffers.Count, "no pending offer left in the run");

                // Several levels at once: offers open in sequence, the pause holds until the last.
                opened.Clear();
                resolved.Clear();
                session.EnqueueLevelUp(3);
                session.EnqueueLevelUp(4);
                AssertEqual(2, run.PendingOffers.Count, "two levels queue two offers");
                AssertEqual(1, opened.Count, "only the first one is open");
                AssertEqual(1, session.CurrentOffer.QueuedAfter, "the open offer knows one more waits");
                AssertEqual(3, session.CurrentOffer.Level, "the first offer is for the first level gained");
                session.Choose(0);
                AssertEqual(2, opened.Count, "the next offer opens right after the first is resolved");
                Assert(TurnBlockers.IsBlocked, "the pause holds between offers");
                AssertEqual(4, session.CurrentOffer.Level, "the second offer is for the second level");
                Assert(resolved[0].HasMoreOffers, "the resolution tells another offer follows");
                session.Choose(0);
                Assert(!TurnBlockers.IsBlocked, "the game resumes after the last of the sequence");
                AssertEqual(3, run.Player.Talents.Sum(t => t.Rank), "three ranks in the build after three choices");

                session.Dispose();
            }
            finally
            {
                EventBus.Unsubscribe(onOpened);
                EventBus.Unsubscribe(onResolved);
                EventBus.Unsubscribe(onAcquired);
                TurnBlockers.Clear();
            }

            // The same state gives the same offer (a reload replays the turn).
            var seedRun = NewRun(knight, 4242UL, 2);
            var cloneRun = SaveSystem.Clone(seedRun);
            var sessionA = NewSession(seedRun, knight);
            var sessionB = NewSession(cloneRun, knight);
            sessionA.EnqueueLevelUp(2);
            sessionB.EnqueueLevelUp(2);
            AssertEqual(Ids(sessionA.CurrentOffer.Options.Select(o => o.Talent.Id)),
                Ids(sessionB.CurrentOffer.Options.Select(o => o.Talent.Id)), "a replayed level up re-derives the same offer");
            sessionA.Reroll();
            sessionB.Reroll();
            AssertEqual(Ids(sessionA.CurrentOffer.Options.Select(o => o.Talent.Id)),
                Ids(sessionB.CurrentOffer.Options.Select(o => o.Talent.Id)), "a replayed reroll re-derives the same offer");
            sessionA.Dispose();
            sessionB.Dispose();
            TurnBlockers.Clear();

            // A talent that unlocks a skill adds it to the run.
            var rogue = GetClass(ECharacter.Rogue);
            var rogueRun = NewRun(rogue, 99UL, 2);
            var rogueSession = NewSession(rogueRun, rogue);
            var arsenal = GetTalent("ArsenalOculto");
            rogueSession.GrantTalent(arsenal);
            Assert(rogueRun.Player.SkillIds.Contains(arsenal.UnlockedSkill.Id), "a skill talent unlocks its skill in the run");
            var startingSlots = SkillSettings.Current.StartingSkillsUseSlots ? rogue.Skills.Count : 0;
            AssertEqual(startingSlots + 1, TalentRules.GetUsedSkillSlots(rogueRun.Player, rogue), "the skill takes a slot");
            rogueSession.Dispose();

            // A Max HP talent heals the same amount when the settings say so (outside battle, in the run's HP). The HP
            // starts at 1, well below the maximum, so the heal is never cut by the maximum.
            var vitality = GetTalent("Vitality");
            var gain = ExpectedMaxHp(knight.MaxHp, vitality, 1) - knight.MaxHp;
            var warriorRun = NewRun(knight, 5UL, 2);
            warriorRun.Player.Hp = 1;
            var warriorSession = NewSession(warriorRun, knight);
            warriorSession.GrantTalent(vitality);
            AssertEqual(settings.MaxHpGainHealsSameAmount ? 1 + gain : 1, warriorRun.Player.Hp, "Max HP gain heals the same amount (setting)");
            warriorSession.Dispose();
        }

        // ------------------------------------------------------------------ reroll, ban, skip

        private static void CheckToolsAndCounts()
        {
            var knight = GetClass(ECharacter.Warrior);
            var settings = TalentOfferSettings.Current;
            var changed = new List<TalentOfferChangedEvent>();
            Action<TalentOfferChangedEvent> onChanged = changed.Add;
            EventBus.Subscribe(onChanged);

            try
            {
                var run = NewRun(knight, 31UL, 6);
                var session = NewSession(run, knight);
                session.EnqueueLevelUp(6);
                AssertEqual(settings.BaseRerolls, session.CurrentOffer.RerollsLeft, "the offer shows the rerolls left");

                // Reroll: counts go down, the options are new, the index is saved.
                var before = session.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
                Assert(session.Reroll(), "reroll works");
                AssertEqual(1, run.Player.RerollsUsed, "one reroll used");
                AssertEqual(1, run.PendingOffers[0].RerollIndex, "the offer remembers its reroll index");
                AssertEqual(settings.BaseRerolls - 1, session.RerollsLeft, "rerolls left go down");
                var after = session.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
                Assert(!after.Intersect(before).Any(), "a reroll shows new talents when the pool allows");
                AssertEqual(1, changed.Count, "TalentOfferChangedEvent raised");
                AssertEqual(ETalentOfferChange.Rerolled, changed[0].Change, "reroll change kind");

                for (var i = 1; i < settings.BaseRerolls; i++)
                    Assert(session.Reroll(), "rerolls up to the base amount work");
                AssertEqual(0, session.RerollsLeft, "no rerolls left");
                Assert(!session.Reroll(), "a reroll without any left does nothing");
                AssertEqual(settings.BaseRerolls, run.Player.RerollsUsed, "rerolls used stays at the base amount");

                // Ban: the talent leaves the pool for good and is replaced.
                var optionsBeforeBan = session.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
                var banned = optionsBeforeBan[0];
                Assert(session.Ban(0), "ban works");
                Assert(run.Player.BannedTalentIds.Contains(banned), "the banned talent is in the run's ban list");
                AssertEqual(1, run.Player.BansUsed, "one ban used");
                var optionsAfterBan = session.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
                AssertEqual(optionsBeforeBan.Count, optionsAfterBan.Count, "a ban is replaced by a new draw");
                Assert(!optionsAfterBan.Contains(banned), "the banned talent left the offer");
                for (var i = 1; i < optionsBeforeBan.Count; i++)
                    AssertEqual(optionsBeforeBan[i], optionsAfterBan[i], $"the other options stay ({i})");
                Assert(!optionsBeforeBan.Contains(optionsAfterBan[0]), "the replacement is a talent that was not on screen");
                AssertEqual(ETalentOfferChange.Banned, changed[changed.Count - 1].Change, "ban change kind");
                AssertEqual(0, changed[changed.Count - 1].BannedIndex, "the event tells which option was banned");
                AssertEqual(settings.BaseBans - 1, session.BansLeft, "bans left go down");
                Assert(!session.Ban(0), "a ban without any left does nothing");

                // The banned talent stays out of every later offer.
                session.Choose(0);
                for (var level = 7; level < 14; level++)
                {
                    session.EnqueueLevelUp(level);
                    Assert(!session.CurrentOffer.Options.Any(o => o.Talent.Id == banned), "a banned talent never comes back");
                    session.Choose(0);
                }

                // Skip: no talent, the level's choice is lost, the count goes down.
                var ranksBeforeSkip = run.Player.Talents.Sum(t => t.Rank);
                session.EnqueueLevelUp(14);
                var resolvedSkip = new List<TalentOfferResolvedEvent>();
                Action<TalentOfferResolvedEvent> onResolved = resolvedSkip.Add;
                EventBus.Subscribe(onResolved);
                Assert(session.Skip(), "skip works");
                EventBus.Unsubscribe(onResolved);
                AssertEqual(ranksBeforeSkip, run.Player.Talents.Sum(t => t.Rank), "skipping takes no talent");
                AssertEqual(1, run.Player.SkipsUsed, "one skip used");
                AssertEqual(ETalentOfferResult.Skipped, resolvedSkip[0].Result, "resolved as skipped");
                Assert(!TurnBlockers.IsBlocked, "the game resumes after a skip");
                session.EnqueueLevelUp(15);
                Assert(!session.Skip(), "a skip without any left does nothing");
                Assert(session.HasOffer, "the offer stays open when the skip is refused");
                session.Choose(0);

                session.Dispose();
            }
            finally
            {
                EventBus.Unsubscribe(onChanged);
                TurnBlockers.Clear();
            }

            // Talents that add tools raise the counts at once (Banisher: bans; Wider Choice: options).
            var toolsRun = NewRun(knight, 8UL, 6);
            var toolsSession = NewSession(toolsRun, knight);
            var baseBans = toolsSession.BansLeft;
            var banisher = GetTalent("Banisher");
            toolsSession.GrantTalent(banisher);
            AssertEqual(baseBans + ExpectedRunModifier(banisher, 1, ERunModifier.Bans), toolsSession.BansLeft, "Banisher adds bans right away");
            var baseOptions = toolsSession.OptionCount;
            var wider = GetTalent("WiderChoice");
            toolsSession.GrantTalent(wider);
            var widerOptions = settings.GetOptionCount(ExpectedRunModifier(wider, 1, ERunModifier.TalentOptions));
            Assert(widerOptions > baseOptions, "Wider Choice adds an option (below the cap)");
            AssertEqual(widerOptions, toolsSession.OptionCount, "Wider Choice adds options right away");
            toolsSession.EnqueueLevelUp(7);
            AssertEqual(widerOptions, toolsSession.CurrentOffer.Options.Count, "the next offer shows the extra option");
            toolsSession.Dispose();
            TurnBlockers.Clear();
        }

        // ------------------------------------------------------------------ talent node

        private static void CheckTalentNode()
        {
            var knight = GetClass(ECharacter.Warrior);
            var run = NewRun(knight, 2024UL, 5);
            run.Phase = ERunPhase.TalentNode;
            run.Map.CurrentNodeId = 3;

            var completed = 0;
            var session = NewSession(run, knight);
            session.NodeOfferCompleted += () => completed++;
            session.OpenNodeOffer();
            Assert(session.HasOffer, "the talent node opens an offer");
            AssertEqual(ETalentOfferSource.TalentNode, session.CurrentOffer.Source, "talent node source");
            AssertEqual(1, run.PendingOffers.Count, "the node's offer is pending in the run");
            var options = session.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
            AssertEqual(5 + TalentOfferSettings.Current.TalentNodeLevelBonus, session.CurrentOffer.Level, "a node offer uses the player's level plus the node bonus");

            // A resumed run (cloned save) opens the same offer, not a new one.
            var saved = SaveSystem.Clone(run);
            session.Dispose();
            TurnBlockers.Clear();
            var resumed = NewSession(saved, knight);
            var resumedCompleted = 0;
            resumed.NodeOfferCompleted += () => resumedCompleted++;
            resumed.OpenNodeOffer();
            AssertEqual(Ids(options), Ids(resumed.CurrentOffer.Options.Select(o => o.Talent.Id)), "a resumed talent node keeps its options");
            AssertEqual(1, saved.PendingOffers.Count, "resuming does not add a second offer");

            // A reroll is part of the saved offer.
            resumed.Reroll();
            var rerolled = resumed.CurrentOffer.Options.Select(o => o.Talent.Id).ToList();
            var saved2 = SaveSystem.Clone(saved);
            resumed.Dispose();
            TurnBlockers.Clear();
            var resumed2 = NewSession(saved2, knight);
            resumed2.OpenNodeOffer();
            AssertEqual(Ids(rerolled), Ids(resumed2.CurrentOffer.Options.Select(o => o.Talent.Id)), "the rerolled node offer survives a save");
            AssertEqual(1, resumed2.CurrentOffer.RerollIndex, "the reroll index survives a save");

            // Choosing completes the node.
            var resumed2Completed = 0;
            resumed2.NodeOfferCompleted += () => resumed2Completed++;
            resumed2.Choose(0);
            AssertEqual(1, resumed2Completed, "choosing at the talent node completes the node");
            Assert(!TurnBlockers.IsBlocked, "the pause is released at the node");
            AssertEqual(0, saved2.PendingOffers.Count, "no pending offer after the node");
            AssertEqual(0, completed + resumedCompleted, "the earlier sessions never completed the node");
            resumed2.Dispose();

            // Skipping the node's offer also completes it.
            var skipRun = NewRun(knight, 3UL, 5);
            skipRun.Phase = ERunPhase.TalentNode;
            var skipSession = NewSession(skipRun, knight);
            var skipCompleted = 0;
            skipSession.NodeOfferCompleted += () => skipCompleted++;
            skipSession.OpenNodeOffer();
            skipSession.Skip();
            AssertEqual(1, skipCompleted, "skipping at the talent node completes the node");
            skipSession.Dispose();
            TurnBlockers.Clear();
        }

        // ------------------------------------------------------------------ the pool lasts a whole run

        private static void CheckPoolExhaustion()
        {
            var progression = ProgressionSettings.Current;
            foreach (var character in new[] { ECharacter.Warrior, ECharacter.Mage, ECharacter.Rogue })
            {
                var playerClass = GetClass(character);
                var run = NewRun(playerClass, 321UL + (ulong)character, 2);
                var session = NewSession(run, playerClass);
                var emptyAt = -1;
                for (var level = 2; level <= progression.MaxLevel; level++)
                {
                    run.Player.Level = level;
                    session.EnqueueLevelUp(level);
                    if (!session.HasOffer)
                    {
                        emptyAt = level;
                        break;
                    }

                    // Follow the synergy: always take the first option.
                    session.Choose(0);
                }

                Log.AppendLine($"{playerClass.name}: {run.Player.Talents.Count} different talents / {run.Player.Talents.Sum(t => t.Rank)} ranks taken " +
                               $"over levels 2..{progression.MaxLevel}, offers ran dry at level {(emptyAt < 0 ? "never" : emptyAt.ToString())}, skill talents {TalentRules.CountSkillTalents(run.Player)}");
                AssertEqual(-1, emptyAt, $"{playerClass.name}: the pool must last up to the max level");
                session.Dispose();
                TurnBlockers.Clear();
            }
        }

        // ------------------------------------------------------------------ skills: resolver and modifiers

        /// <summary>With no modifier the resolver returns exactly the authored numbers of every skill.</summary>
        private static void CheckSkillResolver()
        {
            var settings = CombatResolver.Settings;
            var holder = NewHolder(GetClass(ECharacter.Warrior));
            var count = 0;
            foreach (var skill in GameDatabase.Instance.GetAll<SkillDefinition>())
            {
                count++;
                var effective = EffectiveSkill.Resolve(holder, skill);
                Assert(!effective.HasModifiers, $"{skill.name}: no modifier without talents");
                AssertEqual(skill.AreaSize, effective.AreaSize, $"{skill.name}: area size");
                Assert(skill.AreaSize % 2 == 1 || !SkillDefinition.NeedsOddSize(skill.AreaShape),
                    $"{skill.name}: only line areas may have an even size");
                AssertEqual(skill.Range, effective.Range, $"{skill.name}: range");
                AssertEqual(skill.Cooldown, effective.Cooldown, $"{skill.name}: cooldown");
                AssertEqual(skill.Effects.Count, effective.Effects.Count, $"{skill.name}: effects");
                AssertEqual(0, effective.DisplacementBonus, $"{skill.name}: displacement bonus");
                AssertEqual(settings.RoundValue(skill.Damage * holder.Scaling.DamageMultiplier), effective.Damage,
                    $"{skill.name}: damage");
                AssertEqual(effective.Damage, skill.GetDamageFor(holder), $"{skill.name}: GetDamageFor goes through the resolver");
                AssertEqual(skill.Range + (skill.RangeModifiable ? holder.Stats.GetInt(EAttribute.SkillRange) : 0),
                    SkillTargeting.GetMaxRange(holder, skill), $"{skill.name}: max range");
            }

            Assert(count > 0, "the game has skills");
            Dispose(holder);
        }

        /// <summary>
        /// The skill modifier effect (no current talent uses it; a fixture talent with ranks): one stack per rank,
        /// the numbers grow per rank, only the listed skills and their upgrade variants change, line areas may stay
        /// even and the other shapes stay odd.
        /// </summary>
        private static void CheckSkillModifiers()
        {
            var skills = GameDatabase.Instance.GetAll<SkillDefinition>();
            var bash = GetSkill("ShieldBash");
            var line = skills.FirstOrDefault(s => s.UpgradeOf == null && s.AreaShape == ESkillAreaShape.Linear && s.AreaSize % 2 == 0);
            var attached = skills.SelectMany(s => s.Effects).FirstOrDefault(e => e != null);
            Assert(attached != null, "some skill has an effect to attach");

            var modifier = new SkillModifierEffect();
            SetField(modifier, "skills", line != null ? new List<SkillDefinition> { bash, line } : new List<SkillDefinition> { bash });
            SetField(modifier, "damageFlat", 1);
            SetField(modifier, "damagePercent", 0.5f);
            SetField(modifier, "areaSteps", 1);
            SetField(modifier, "range", 1);
            SetField(modifier, "cooldownReduction", 1);
            SetField(modifier, "displacement", 1);
            SetField(modifier, "stateDuration", 1);
            SetField(modifier, "attachedEffects", attached != null ? new List<SkillEffect> { attached } : new List<SkillEffect>());
            SetField(modifier, "repeatAttachedPerStack", true);
            const int MaxRank = 2;
            var talent = NewTalentFixture("Modifier", 2, MaxRank, null, NewStateFixture("Modifier", MaxRank, modifier));

            var playerClass = GetClass(ECharacter.Warrior);
            for (var rank = 1; rank <= MaxRank; rank++)
            {
                var holder = NewHolder(playerClass);
                TalentApplier.ApplyStates(holder, talent, rank);
                var instance = holder.States.Find(talent.States[0].State, talent.Id);
                Assert(instance != null && instance.Stacks == rank, $"modifier rank {rank}: the state has {rank} stack(s)");

                foreach (var skill in skills)
                {
                    var applies = modifier.AppliesTo(skill);
                    var label = $"modifier r{rank} on {skill.name}";
                    AssertEqual(skill == bash || skill == line || (skill.UpgradeOf != null && (skill.UpgradeOf == bash || skill.UpgradeOf == line)),
                        applies, $"{label}: applies to the listed skills and their variants only");

                    var effective = EffectiveSkill.Resolve(holder, skill);
                    AssertEqual(applies, effective.HasModifiers, $"{label}: modified only when it applies");

                    var flat = applies ? modifier.DamageFlat * rank : 0;
                    var percent = applies ? modifier.DamagePercent * rank : 0f;
                    var steps = applies ? modifier.AreaSteps * rank : 0;
                    var expectedDamage = skill.Damage > 0 && applies
                        ? Mathf.Max(0f, (skill.Damage + flat) * (1f + percent))
                        : skill.Damage;
                    Assert(Mathf.Approximately(expectedDamage, effective.BaseDamage), $"{label}: base damage {expectedDamage}, got {effective.BaseDamage}");

                    var expectedArea = Mathf.Clamp(skill.AreaSize + 2 * steps, SkillDefinition.MinAreaSize, SkillDefinition.MaxAreaSize);
                    if (expectedArea % 2 == 0 && SkillDefinition.NeedsOddSize(skill.AreaShape))
                        expectedArea--;
                    AssertEqual(expectedArea, effective.AreaSize, $"{label}: area size");
                    Assert(effective.AreaSize % 2 == 1 || !SkillDefinition.NeedsOddSize(skill.AreaShape), $"{label}: the area stays odd unless it is a line");

                    var expectedRange = skill.IsSelfCentered || skill.UnlimitedRange || !applies
                        ? skill.Range
                        : Mathf.Max(0, skill.Range + modifier.Range * rank);
                    AssertEqual(expectedRange, effective.Range, $"{label}: range");
                    AssertEqual(skill.Cooldown <= 0 ? 0 : skill.Cooldown - (applies ? modifier.CooldownReduction * rank : 0), effective.Cooldown, $"{label}: cooldown");
                    AssertEqual(applies ? modifier.Displacement * rank : 0, effective.DisplacementBonus, $"{label}: displacement bonus");
                    AssertEqual(applies ? modifier.StateDuration * rank : 0, effective.StateDurationBonus, $"{label}: state duration bonus");
                    AssertEqual(skill.Effects.Count + (applies && attached != null ? rank : 0), effective.Effects.Count, $"{label}: attached effects");
                }

                // A real use: the push distance of Shield Bash grows with the modifier (the same number the bot and the AI read).
                var effectiveBash = EffectiveSkill.Resolve(holder, bash);
                var displace = effectiveBash.FindDisplacement();
                Assert(displace != null, "Shield Bash has its displacement effect");
                if (displace != null)
                    AssertEqual(displace.Distance + modifier.Displacement * rank, effectiveBash.GetDisplacementDistance(displace),
                        $"modifier rank {rank} pushes {modifier.Displacement * rank} cell(s) farther");

                Dispose(holder);
            }
        }

        /// <summary>The cooldown never goes below the minimum, whatever the modifiers and the attribute remove (fixtures).</summary>
        private static void CheckCooldownMinimum()
        {
            var minimum = SkillSettings.Current.MinimumCooldown;
            var knight = GetClass(ECharacter.Warrior);
            var charge = GetSkill("ShieldCharge");
            Assert(charge.Cooldown > 0, "Shield Charge has a cooldown (needed by the check)");

            var holder = NewHolder(knight);
            holder.Cooldowns.Trigger(charge);
            AssertEqual(Mathf.Max(minimum, charge.Cooldown), holder.Cooldowns.GetRemaining(charge), "plain cooldown");
            holder.Cooldowns.Reset();

            var modifier = new SkillModifierEffect();
            SetField(modifier, "skills", new List<SkillDefinition> { charge });
            SetField(modifier, "cooldownReduction", 1);
            var faster = NewTalentFixture("FasterCharge", 2, 2, null, NewStateFixture("FasterCharge", 2, modifier));
            TalentApplier.ApplyStates(holder, faster, 1);
            holder.Cooldowns.Trigger(charge);
            AssertEqual(Mathf.Max(0, Mathf.Max(minimum, charge.Cooldown - 1)), holder.Cooldowns.GetRemaining(charge), "a modifier reduces the cooldown");
            holder.Cooldowns.Reset();

            // The modifier at rank 2 plus 2 of the Cooldown Reduction attribute remove more than the whole cooldown.
            TalentApplier.ApplyStates(holder, faster, 2);
            var attribute = new AttributeModifierEffect();
            SetField(attribute, "modifiers", new List<AttributeModifier> { new(EAttribute.CooldownReduction, EModifierType.Flat, 2f) });
            var quick = NewTalentFixture("QuickRecovery", 2, 1, null, NewStateFixture("QuickRecovery", 1, attribute));
            TalentApplier.ApplyStates(holder, quick, 1);
            AssertEqual(2, holder.Stats.GetInt(EAttribute.CooldownReduction), "the attribute fixture gives 2 cooldown reduction");
            holder.Cooldowns.Trigger(charge);
            AssertEqual(minimum, holder.Cooldowns.GetRemaining(charge), "modifier and attribute together never go below the minimum cooldown");
            Dispose(holder);

            // The queued on-kill reduction is applied at the end of the turn.
            var other = NewHolder(knight);
            other.Cooldowns.Trigger(charge);
            var before = other.Cooldowns.GetRemaining(charge);
            other.Cooldowns.QueueReduction(1);
            AssertEqual(before, other.Cooldowns.GetRemaining(charge), "a queued reduction waits for the end of the turn");
            other.Cooldowns.Tick();
            AssertEqual(Mathf.Max(0, before - 1), other.Cooldowns.GetRemaining(charge), "the queued reduction is applied at the end of the turn");
            Dispose(other);
        }

        // ------------------------------------------------------------------ damage

        /// <summary>The damage formula: a neutral adjustment changes nothing, bonuses and reductions follow the documented order.</summary>
        private static void CheckDamageAdjustment()
        {
            var settings = CombatResolver.Settings;
            var knight = NewHolder(GetClass(ECharacter.Warrior));
            var mage = NewHolder(GetClass(ECharacter.Mage));
            var attacker = knight.Stats;
            var target = mage.Stats;

            foreach (EDamageKind kind in Enum.GetValues(typeof(EDamageKind)))
            {
                var plain = DamageCalculator.Compute(20, kind, attacker, target, settings, null, out _);
                var neutral = DamageCalculator.Compute(20, kind, attacker, target, settings, null, default, out _);
                AssertEqual(plain, neutral, $"{kind}: a neutral adjustment is the plain formula");
            }

            var plainSkill = DamageCalculator.Compute(20, EDamageKind.Skill, attacker, target, settings, null, out _);
            var bonus = new DamageAdjustment();
            bonus.AddOutgoing(0.5f);
            AssertEqual(30, DamageCalculator.Compute(20, EDamageKind.BasicAttack, attacker, target, settings, null, bonus, out _),
                "+50% outgoing");
            var flat = new DamageAdjustment();
            flat.AddOutgoing(0f, 4f);
            AssertEqual(24, DamageCalculator.Compute(20, EDamageKind.BasicAttack, attacker, target, settings, null, flat, out _),
                "+4 flat outgoing");
            var reduction = new DamageAdjustment();
            reduction.AddIncoming(-0.25f);
            AssertEqual(15, DamageCalculator.Compute(20, EDamageKind.BasicAttack, attacker, target, settings, null, reduction, out _),
                "-25% incoming");
            AssertEqual(20, DamageCalculator.Compute(20, EDamageKind.Pure, attacker, target, settings, null, bonus, out _),
                "exact damage is never adjusted");
            Assert(bonus.IsNeutral == false && default(DamageAdjustment).IsNeutral, "IsNeutral");

            // Without any state the resolver's adjustment is neutral and the prediction equals the formula.
            Assert(CombatResolver.Adjust(knight, mage, EDamageKind.Skill).IsNeutral, "no states, no adjustment");
            AssertEqual(plainSkill, CombatResolver.PredictDamage(knight, mage, 20, EDamageKind.Skill), "prediction equals the formula without passives");
            Dispose(knight, mage);
        }

        /// <summary>
        /// The damage conditions that need no grid, through the current talents (Predator, Brittle, Arcane Focus,
        /// Shatter, Assassin) and fixtures for the conditions no current talent uses. Expected values come from the
        /// formula with each talent's own numbers.
        /// </summary>
        private static void CheckConditionalPassives()
        {
            var rogueConfig = GetClass(ECharacter.Rogue);
            var mageConfig = GetClass(ECharacter.Mage);
            var victim = NewHolder(mageConfig);
            var weakened = LoadState("Weakened");
            var shield = LoadState("Shield");
            const int Base = 20;

            // Predator: skills deal more to targets with its listed states (Poisoned). 21 for 20 with +1 flat is right.
            var predatorTalent = GetTalent("Predator");
            var predator = EffectOf<ConditionalDamageEffect>(predatorTalent);
            var rogue = NewHolder(rogueConfig);
            TalentApplier.ApplyStates(rogue, predatorTalent, predatorTalent.MaxRank);
            var predatorStacks = StacksAt(predatorTalent, predatorTalent.MaxRank);
            AssertEqual(ExpectedDamage(rogue, victim, Base, EDamageKind.Skill, predator, 0),
                CombatResolver.PredictDamage(rogue, victim, Base, EDamageKind.Skill), "Predator: nothing against a target without its states");
            Assert(predator.States.Count > 0, "Predator lists the states it looks for");
            victim.States.Apply(predator.States[0], 3);
            var predatorDamage = CombatResolver.PredictDamage(rogue, victim, Base, EDamageKind.Skill);
            AssertEqual(ExpectedDamage(rogue, victim, Base, EDamageKind.Skill, predator, predatorStacks), predatorDamage,
                "Predator: the bonus against a target with its state");
            Assert(predatorDamage > ExpectedDamage(rogue, victim, Base, EDamageKind.Skill, predator, 0), "Predator gives a bonus");
            foreach (var kind in new[] { EDamageKind.BasicAttack, EDamageKind.Periodic, EDamageKind.Collision })
            {
                if (predator.AppliesToKinds.Contains(kind)) continue;
                AssertEqual(ExpectedDamage(rogue, victim, Base, kind, predator, 0), CombatResolver.PredictDamage(rogue, victim, Base, kind),
                    $"Predator does not boost {kind} damage");
            }

            victim.States.Clear();
            Dispose(rogue);

            // Brittle: only the states it lists count (a buff and another debuff do not).
            var brittleTalent = GetTalent("Brittle");
            var brittle = EffectOf<ConditionalDamageEffect>(brittleTalent);
            var brittleHolder = NewHolder(mageConfig);
            TalentApplier.ApplyStates(brittleHolder, brittleTalent, brittleTalent.MaxRank);
            var brittleStacks = StacksAt(brittleTalent, brittleTalent.MaxRank);
            int PredictBrittle() => CombatResolver.PredictDamage(brittleHolder, victim, Base, EDamageKind.Skill);
            var brittlePlain = ExpectedDamage(brittleHolder, victim, Base, EDamageKind.Skill, brittle, 0);
            victim.States.Apply(shield, 3);
            AssertEqual(brittlePlain, PredictBrittle(), "Brittle: a beneficial state does not count");
            if (!brittle.States.Contains(weakened))
            {
                victim.States.Apply(weakened, 3);
                AssertEqual(brittlePlain, PredictBrittle(), "Brittle: a harmful state it does not list does not count");
            }

            Assert(brittle.States.Count > 0, "Brittle lists the states it looks for");
            victim.States.Apply(brittle.States[0], 3);
            AssertEqual(ExpectedDamage(brittleHolder, victim, Base, EDamageKind.Skill, brittle, brittleStacks), PredictBrittle(),
                "Brittle: a listed state counts");
            victim.States.Clear();
            Dispose(brittleHolder);

            // Any harmful state (no current talent; fixture): a buff does not count, a debuff does.
            var harmTalent = NewConditionalTalent("HarmfulState", EDamageCondition.OpponentHasNegativeState, EDamageKindMask.Skill, 1f, 1);
            var harmHolder = NewHolder(mageConfig);
            TalentApplier.ApplyStates(harmHolder, harmTalent, 1);
            var harm = EffectOf<ConditionalDamageEffect>(harmTalent);
            victim.States.Apply(shield, 3);
            AssertEqual(ExpectedDamage(harmHolder, victim, Base, EDamageKind.Skill, harm, 0),
                CombatResolver.PredictDamage(harmHolder, victim, Base, EDamageKind.Skill), "harmful state: a beneficial state does not count");
            victim.States.Apply(weakened, 3);
            AssertEqual(ExpectedDamage(harmHolder, victim, Base, EDamageKind.Skill, harm, 1),
                CombatResolver.PredictDamage(harmHolder, victim, Base, EDamageKind.Skill), "harmful state: a debuff counts");
            victim.States.Clear();
            Dispose(harmHolder);

            // Moved this turn (no current talent; fixture with 2 ranks): the walk of the current turn counts, last turn's does not.
            var runnerTalent = NewConditionalTalent("HitAndRun", EDamageCondition.HolderMovedThisTurn, EDamageKindMask.Skill, 1f, 2);
            var runnerEffect = EffectOf<ConditionalDamageEffect>(runnerTalent);
            var runner = NewHolder(rogueConfig);
            TalentApplier.ApplyStates(runner, runnerTalent, 2);
            int PredictRunner() => CombatResolver.PredictDamage(runner, victim, Base, EDamageKind.Skill);
            runner.BeginTurn();
            var runnerPlain = ExpectedDamage(runner, victim, Base, EDamageKind.Skill, runnerEffect, 0);
            var runnerBonus = ExpectedDamage(runner, victim, Base, EDamageKind.Skill, runnerEffect, 2);
            Assert(runnerBonus > runnerPlain, "the moved-this-turn fixture gives a bonus");
            AssertEqual(runnerPlain, PredictRunner(), "moved this turn: no bonus before walking");
            runner.RegisterMovement();
            AssertEqual(runnerBonus, PredictRunner(), "moved this turn, rank 2: bonus after walking this turn");
            runner.BeginTurn();
            AssertEqual(runnerPlain, PredictRunner(), "moved this turn: last turn's walk does not count");
            runner.RegisterMovement();
            runner.SetMovedThisTurn(false);
            AssertEqual(runnerPlain, PredictRunner(), "moved this turn: an undone walk does not count");
            runner.RegisterMovement();
            runner.RestoreMoved(true);
            runner.BeginTurn();
            Assert(runner.MovedLastTurn && !runner.MovedThisTurn, "a restored 'moved' flag becomes last turn's flag when the turn starts");
            Dispose(runner);

            // Arcane Focus: while the holder has not walked this turn, only on the kinds it lists.
            var focusTalent = GetTalent("ArcaneFocus");
            var focusEffect = EffectOf<ConditionalDamageEffect>(focusTalent);
            var focusStacks = StacksAt(focusTalent, focusTalent.MaxRank);
            var focus = NewHolder(mageConfig);
            TalentApplier.ApplyStates(focus, focusTalent, focusTalent.MaxRank);
            focus.BeginTurn();
            var focusBonus = ExpectedDamage(focus, victim, Base, EDamageKind.Skill, focusEffect, focusStacks);
            var focusPlain = ExpectedDamage(focus, victim, Base, EDamageKind.Skill, focusEffect, 0);
            Assert(focusBonus > focusPlain, "Arcane Focus gives a bonus");
            AssertEqual(focusBonus, CombatResolver.PredictDamage(focus, victim, Base, EDamageKind.Skill),
                "Arcane Focus: bonus on skills while it has not walked this turn");
            if (!focusEffect.AppliesToKinds.Contains(EDamageKind.BasicAttack))
                AssertEqual(ExpectedDamage(focus, victim, Base, EDamageKind.BasicAttack, focusEffect, 0),
                    CombatResolver.PredictDamage(focus, victim, Base, EDamageKind.BasicAttack), "Arcane Focus: kinds it does not list are not boosted");
            focus.RegisterMovement();
            AssertEqual(focusPlain, CombatResolver.PredictDamage(focus, victim, Base, EDamageKind.Skill), "Arcane Focus: no bonus after walking this turn");
            focus.BeginTurn();
            AssertEqual(focusBonus, CombatResolver.PredictDamage(focus, victim, Base, EDamageKind.Skill), "Arcane Focus: last turn's walk does not matter");
            Dispose(focus);

            // Shatter: collision damage only.
            var shatterTalent = GetTalent("Shatter");
            var shatter = EffectOf<ConditionalDamageEffect>(shatterTalent);
            var warrior = NewHolder(GetClass(ECharacter.Warrior));
            TalentApplier.ApplyStates(warrior, shatterTalent, shatterTalent.MaxRank);
            var shatterStacks = StacksAt(shatterTalent, shatterTalent.MaxRank);
            Assert(shatter.AppliesToKinds.Contains(EDamageKind.Collision), "Shatter applies to collisions");
            var collisionBonus = ExpectedDamage(warrior, victim, Base, EDamageKind.Collision, shatter, shatterStacks);
            Assert(collisionBonus > ExpectedDamage(warrior, victim, Base, EDamageKind.Collision, shatter, 0), "Shatter gives a bonus");
            AssertEqual(collisionBonus, CombatResolver.PredictDamage(warrior, victim, Base, EDamageKind.Collision), "Shatter: collisions deal more damage");
            if (!shatter.AppliesToKinds.Contains(EDamageKind.Skill))
                AssertEqual(ExpectedDamage(warrior, victim, Base, EDamageKind.Skill, shatter, 0),
                    CombatResolver.PredictDamage(warrior, victim, Base, EDamageKind.Skill), "Shatter does not touch skills");
            Dispose(warrior);

            // Without a grid nobody stands next to anyone: the per-adjacent bonus is 0, the no-adjacent and the
            // isolated-target bonuses apply (fixtures for the first two, Assassin for the last).
            var perAdjacent = NewConditionalTalent("PerAdjacent", EDamageCondition.PerAdjacentEnemy, EDamageKindMask.Skill, 1f, 1);
            var noAdjacent = NewConditionalTalent("NoAdjacent", EDamageCondition.NoAdjacentEnemy, EDamageKindMask.Skill, 1f, 1);
            var lonely = NewHolder(GetClass(ECharacter.Warrior));
            TalentApplier.ApplyStates(lonely, perAdjacent, 1);
            AssertEqual(ExpectedDamage(lonely, victim, Base, EDamageKind.Skill, EffectOf<ConditionalDamageEffect>(perAdjacent), 0),
                CombatResolver.PredictDamage(lonely, victim, Base, EDamageKind.Skill), "no grid, no adjacent enemies, no per-adjacent bonus");
            Dispose(lonely);
            var alone = NewHolder(GetClass(ECharacter.Warrior));
            TalentApplier.ApplyStates(alone, noAdjacent, 1);
            AssertEqual(ExpectedDamage(alone, victim, Base, EDamageKind.Skill, EffectOf<ConditionalDamageEffect>(noAdjacent), 1),
                CombatResolver.PredictDamage(alone, victim, Base, EDamageKind.Skill), "no grid, no adjacent enemy: the no-adjacent bonus applies");
            Dispose(alone);

            var assassinTalent = GetTalent("Assassin");
            var assassin = EffectOf<ConditionalDamageEffect>(assassinTalent);
            var killer = NewHolder(rogueConfig);
            TalentApplier.ApplyStates(killer, assassinTalent, assassinTalent.MaxRank);
            AssertEqual(ExpectedDamage(killer, victim, Base, EDamageKind.Skill, assassin, StacksAt(assassinTalent, assassinTalent.MaxRank)),
                CombatResolver.PredictDamage(killer, victim, Base, EDamageKind.Skill), "Assassin: a target with no ally next to it takes the bonus");
            Dispose(killer, victim);
        }

        /// <summary>On-hit and on-kill effects: Envenomed Blades, Shadow Feast, Bloodlust, Soul Harvest, Momentum, and a stacking fixture.</summary>
        private static void CheckHitReactions()
        {
            var rogue = NewHolder(GetClass(ECharacter.Rogue));
            var mageConfig = GetClass(ECharacter.Mage);
            var mage = NewHolder(mageConfig);
            var victim = NewHolder(GetClass(ECharacter.Warrior));

            // Envenomed Blades: hits of the kinds it lists apply its state; other kinds do not.
            var bladesTalent = GetTalent("EnvenomedBlades");
            var blades = EffectOf<ApplyStateOnHitEffect>(bladesTalent);
            Assert(blades.States.Count > 0 && blades.States[0].IsValid, "Envenomed Blades applies a state");
            TalentApplier.ApplyStates(rogue, bladesTalent, bladesTalent.MaxRank);
            var poison = blades.States[0];
            var otherKind = blades.AppliesToKinds.Contains(EDamageKind.BasicAttack) ? EDamageKind.Periodic : EDamageKind.BasicAttack;
            rogue.States.NotifyDamageDealt(new HitResult { Attacker = rogue, Target = victim, Kind = otherKind, Damage = 1, HpDamage = 1 });
            Assert(victim.States.Find(poison.State) == null, $"Envenomed Blades does not react to {otherKind} hits");
            var skillHit = new HitResult { Attacker = rogue, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1 };
            if (blades.AppliesToKinds.Contains(EDamageKind.Skill))
            {
                rogue.States.NotifyDamageDealt(skillHit);
                var applied = victim.States.Find(poison.State);
                Assert(applied != null, "Envenomed Blades: a skill hit applies the state");
                if (applied != null)
                {
                    AssertEqual(Mathf.Min(poison.State.MaxStacks, poison.Stacks * StacksAt(bladesTalent, bladesTalent.MaxRank)), applied.Stacks,
                        "Envenomed Blades: the state's stacks");
                    AssertEqual(poison.Duration, applied.Remaining, "Envenomed Blades: the state's duration");
                }
            }

            victim.States.Clear();

            // Stacks per rank (fixture with 2 ranks applying Venom, which adds stacks).
            var venom = LoadState("Venom");
            var onHit = new ApplyStateOnHitEffect();
            SetField(onHit, "appliesTo", EDamageKindMask.Skill);
            SetField(onHit, "states", new List<StateGrant> { new(venom, 2, 1) });
            var coated = NewTalentFixture("CoatedSkills", 2, 2, null, NewStateFixture("CoatedSkills", 2, onHit));
            var coater = NewHolder(GetClass(ECharacter.Rogue));
            TalentApplier.ApplyStates(coater, coated, 2);
            var coatHit = new HitResult { Attacker = coater, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1 };
            coater.States.NotifyDamageDealt(coatHit);
            var venomInstance = victim.States.Find(venom);
            AssertEqual(Mathf.Min(venom.MaxStacks, 2), venomInstance != null ? venomInstance.Stacks : 0, "on-hit fixture rank 2: 2 venom stacks per hit");
            if (venom.StackPolicy == EStackPolicy.AddStacks)
            {
                coater.States.NotifyDamageDealt(coatHit);
                AssertEqual(Mathf.Min(venom.MaxStacks, 4), venomInstance != null ? venomInstance.Stacks : 0, "venom stacks add up (up to the maximum)");
            }

            victim.States.Clear();
            Dispose(coater);

            // Shadow Feast and Bloodlust: killing heals, a plain hit does not, a kind they do not list does not.
            foreach (var (talentName, holder) in new[] { ("ShadowFeast", rogue), ("Bloodlust", NewHolder(GetClass(ECharacter.Warrior))) })
            {
                var talent = GetTalent(talentName);
                var onKill = EffectOf<OnKillEffect>(talent);
                TalentApplier.ApplyStates(holder, talent, talent.MaxRank);
                var stacks = StacksAt(talent, talent.MaxRank);
                var heal = (onKill.HealFlat + CombatResolver.Settings.RoundValue(onKill.HealMaxHpFraction * holder.MaxHp)) * stacks;
                Assert(heal > 0, $"{talentName} heals on a kill");
                var start = Mathf.Max(1, holder.MaxHp / 2);
                holder.SetHp(start);
                holder.States.NotifyDamageDealt(new HitResult { Attacker = holder, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1 });
                AssertEqual(start, holder.Current, $"{talentName}: no kill, no heal");
                holder.States.NotifyDamageDealt(new HitResult { Attacker = holder, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1, Killed = true });
                AssertEqual(Mathf.Min(holder.MaxHp, start + heal), holder.Current, $"{talentName} heals {heal} HP on a kill");
                if (!onKill.AppliesToKinds.Contains(EDamageKind.Thorns))
                {
                    holder.SetHp(start);
                    holder.States.NotifyDamageDealt(new HitResult { Attacker = holder, Target = victim, Kind = EDamageKind.Thorns, Damage = 1, HpDamage = 1, Killed = true });
                    AssertEqual(start, holder.Current, $"{talentName}: a kill by thorns does not count");
                }

                if (holder != rogue)
                    Dispose(holder);
            }

            // Soul Harvest: a kill shortens the running cooldowns at the end of the turn.
            var harvestTalent = GetTalent("SoulHarvest");
            var harvest = EffectOf<OnKillEffect>(harvestTalent);
            Assert(harvest.CooldownReduction > 0, "Soul Harvest shortens cooldowns");
            var slowSkill = mageConfig.Skills.FirstOrDefault(skill => skill != null && skill.Cooldown > 1);
            Assert(slowSkill != null, "the Mage has a skill with a cooldown above 1 (needed by the check)");
            if (slowSkill != null)
            {
                TalentApplier.ApplyStates(mage, harvestTalent, harvestTalent.MaxRank);
                mage.Cooldowns.Trigger(slowSkill);
                var running = mage.Cooldowns.GetRemaining(slowSkill);
                mage.States.NotifyDamageDealt(new HitResult { Attacker = mage, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1, Killed = true });
                mage.Cooldowns.Tick();
                var reduction = harvest.CooldownReduction * StacksAt(harvestTalent, harvestTalent.MaxRank);
                AssertEqual(Mathf.Max(0, running - reduction), mage.Cooldowns.GetRemaining(slowSkill),
                    $"Soul Harvest: the used skill's cooldown is {reduction} shorter");
            }

            // Momentum (rule talent): a kill grants an extra walk, a plain hit does not.
            var momentumTalent = GetTalent("Momentum");
            Assert(EffectOf<OnKillEffect>(momentumTalent).GrantExtraWalk, "Momentum grants an extra walk");
            var walks = new List<ExtraWalkGrantedEvent>();
            Action<ExtraWalkGrantedEvent> onWalk = walks.Add;
            EventBus.Subscribe(onWalk);
            try
            {
                var runner = NewHolder(GetClass(ECharacter.Warrior));
                TalentApplier.ApplyStates(runner, momentumTalent, momentumTalent.MaxRank);
                runner.States.NotifyDamageDealt(new HitResult { Attacker = runner, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1 });
                AssertEqual(0, walks.Count, "Momentum: no kill, no extra walk");
                runner.States.NotifyDamageDealt(new HitResult { Attacker = runner, Target = victim, Kind = EDamageKind.Skill, Damage = 1, HpDamage = 1, Killed = true });
                AssertEqual(1, walks.Count, "Momentum: a kill grants one extra walk");
                Assert(walks.Count == 1 && walks[0].Character == runner, "the extra walk goes to the killer");
                Dispose(runner);
            }
            finally
            {
                EventBus.Unsubscribe(onWalk);
            }

            Dispose(rogue, mage, victim);
        }

        // ------------------------------------------------------------------ class identity

        private static bool IsRuleEffect(StateEffect effect) =>
            effect is DominoEffect || effect is DisplacementTrailEffect || effect is SlipperyPushEffect ||
            effect is CounterstrikeEffect || effect is SwapStepEffect || (effect is OnKillEffect onKill && onKill.GrantExtraWalk);

        private static bool IsRuleTalent(TalentDefinition talent) =>
            talent.States.Any(g => g.IsValid && g.State.Effects.Any(e => e != null && IsRuleEffect(e)));

        /// <summary>
        /// The class identity of the current design (2026-10-19): each class has its own rule talents, talents that
        /// unlock new skills (up to the settings' limit, and the bar fits them), and every class skill has two upgrade
        /// variants. Also the pools' prerequisites and levels, random play, and the class share of the offers.
        /// </summary>
        private static void CheckClassIdentity()
        {
            var settings = TalentOfferSettings.Current;
            var skillSettings = SkillSettings.Current;
            var progression = ProgressionSettings.Current;

            foreach (var character in new[] { ECharacter.Warrior, ECharacter.Mage, ECharacter.Rogue })
            {
                var playerClass = GetClass(character);
                var classTalents = playerClass.TalentPool.Where(t => t != null).ToList();
                var shared = TalentRules.GetPool(playerClass, settings).Where(t => TalentRules.IsSharedOnly(t, playerClass, settings)).ToList();
                var rules = classTalents.Where(IsRuleTalent).ToList();
                var skillTalents = classTalents.Where(t => t.IsSkillTalent).ToList();
                var passives = classTalents.Count(t => t.States.Any(g => g.IsValid && g.State.Effects.Any(e =>
                    e is ConditionalDamageEffect || (e is OnKillEffect k && !k.GrantExtraWalk) || e is ApplyStateOnHitEffect)));
                Log.AppendLine($"{playerClass.name}: {classTalents.Count} class talents ({classTalents.Sum(t => t.MaxRank)} ranks; {rules.Count} rule talents, " +
                               $"{skillTalents.Count} skill talents, {passives} passives), {shared.Count} generic talents ({shared.Sum(t => t.MaxRank)} ranks), " +
                               $"levels {(classTalents.Count > 0 ? classTalents.Min(t => t.RequiredLevel) : 0)}..{(classTalents.Count > 0 ? classTalents.Max(t => t.RequiredLevel) : 0)}");

                Assert(classTalents.Count > 0, $"{playerClass.name} has its own talents");
                Assert(rules.Count > 0, $"{playerClass.name} has its own rule talent");
                Assert(shared.Count > 0 && shared.All(t => t.AllowedClasses.Count == 0), "generic talents are class-agnostic");

                // Skill talents: new skills only (originals, not a starting skill, not twice), enough of them to reach
                // the limit, and the bar fits the starting skills plus the limit.
                Assert(skillTalents.Count > 0, $"{playerClass.name} has talents that unlock skills");
                if (settings.MaxSkillTalents > 0)
                {
                    Assert(skillTalents.Count >= settings.MaxSkillTalents,
                        $"{playerClass.name}: {skillTalents.Count} skill talents cannot reach the limit of {settings.MaxSkillTalents}");
                    if (skillSettings.StartingSkillsUseSlots)
                        Assert(playerClass.Skills.Count + settings.MaxSkillTalents <= skillSettings.MaxSkillSlots,
                            $"{playerClass.name}: {playerClass.Skills.Count} starting skills and {settings.MaxSkillTalents} unlocked ones do not fit {skillSettings.MaxSkillSlots} slots");
                }

                foreach (var talent in skillTalents)
                {
                    var unlocked = talent.UnlockedSkill;
                    Assert(unlocked.UpgradeOf == null, $"{talent.name} unlocks an original skill, not an upgrade variant");
                    Assert(!playerClass.Skills.Any(s => s != null && s.IsSameSkillAs(unlocked)), $"{talent.name} unlocks a skill the class does not start with");
                    AssertEqual(1, skillTalents.Count(t => t.UnlockedSkill.IsSameSkillAs(unlocked)), $"{talent.name}: no other talent of the class unlocks {unlocked.name}");
                }

                // Every class skill (starting and unlocked) has two upgrade variants that point back to it and cannot be upgraded again.
                var classSkills = playerClass.Skills.Where(s => s != null).Concat(skillTalents.Select(t => t.UnlockedSkill)).Distinct().ToList();
                foreach (var skill in classSkills)
                {
                    Assert(skill.CanBeUpgraded, $"{playerClass.name}: {skill.name} can be upgraded");
                    var variants = skill.Upgrades.Where(u => u != null).ToList();
                    AssertEqual(2, variants.Count, $"{playerClass.name}: {skill.name} has two upgrade variants");
                    AssertEqual(variants.Count, variants.Distinct().Count(), $"{skill.name}: the variants are different assets");
                    foreach (var variant in variants)
                    {
                        Assert(variant != skill, $"{skill.name}: a variant is not the skill itself");
                        Assert(variant.UpgradeOf == skill, $"{variant.name} points back to {skill.name}");
                        Assert(!variant.CanBeUpgraded && variant.Upgrades.Count(u => u != null) == 0, $"{variant.name} cannot be upgraded again");
                        Assert(variant.IsSameSkillAs(skill) && variant.Root == skill, $"{variant.name} counts as {skill.name}");
                    }
                }

                foreach (var talent in classTalents)
                {
                    Assert(!TalentRules.IsSharedOnly(talent, playerClass, settings), $"{talent.name} is a class talent");
                    Assert(talent.IsAllowedFor(playerClass), $"{talent.name} is allowed for {playerClass.name}");
                    Assert(!talent.Prerequisites.Contains(talent), $"{talent.name} is not its own prerequisite");
                    Assert(talent.RequiredLevel <= progression.MaxLevel, $"{talent.name} can be reached before the max level");
                    foreach (var prerequisite in talent.Prerequisites)
                        Assert(classTalents.Contains(prerequisite) || settings.SharedPool.Contains(prerequisite),
                            $"{talent.name}: prerequisite {(prerequisite != null ? prerequisite.name : "null")} is in the class's pool");
                }

                // Prerequisites are respected: random play never takes a talent before its prerequisites or its level.
                for (var seed = 0; seed < 12; seed++)
                {
                    var run = NewRun(playerClass, 900UL + (ulong)seed, 2);
                    var session = NewSession(run, playerClass);
                    var picker = new System.Random(seed);
                    var broken = 0;
                    for (var level = 2; level <= progression.MaxLevel; level++)
                    {
                        run.Player.Level = level;
                        session.EnqueueLevelUp(level);
                        if (!session.HasOffer) break;

                        var options = session.CurrentOffer.Options;
                        var index = picker.Next(options.Count);
                        var chosen = options[index].Talent;
                        if (level < chosen.RequiredLevel || !TalentRules.ArePrerequisitesMet(run.Player, chosen) ||
                            TalentRules.GetRank(run.Player, chosen) >= chosen.MaxRank)
                            broken++;
                        session.Choose(index);
                    }

                    AssertEqual(0, broken, $"{playerClass.name}, seed {seed}: random picks respect level, prerequisites and rank");
                    if (settings.MaxSkillTalents > 0)
                        Assert(TalentRules.CountSkillTalents(run.Player) <= settings.MaxSkillTalents,
                            $"{playerClass.name}, seed {seed}: random play never goes above the skill talent limit");
                    Assert(TalentRules.GetUsedSkillSlots(run.Player, playerClass) <= skillSettings.MaxSkillSlots,
                        $"{playerClass.name}, seed {seed}: random play never goes above the skill slots");
                    session.Dispose();
                    TurnBlockers.Clear();
                }

                // The class share of the offers: with the shared pool multiplier the class's own talents lead.
                var shareRun = NewRun(playerClass, 77UL, progression.MaxLevel);
                var candidates = TalentOfferGenerator.GetCandidates(shareRun.Player, playerClass, progression.MaxLevel, settings);
                var classWeight = candidates.Where(c => !TalentRules.IsSharedOnly(c.Talent, playerClass, settings)).Sum(c => c.Weight);
                var totalWeight = candidates.Sum(c => c.Weight);
                var classShare = totalWeight > 0f ? classWeight / totalWeight : 0f;
                Log.AppendLine($"{playerClass.name}: class talents are {classShare:P0} of the offer weight at level {progression.MaxLevel} with an empty build");
                Assert(classShare >= 0.5f, $"{playerClass.name}: class talents are only {classShare:P0} of the offer weight");
            }

            // The multiplier is exactly what the setting says, only for generic talents (empty build: no synergy).
            var knight = GetClass(ECharacter.Warrior);
            var emptyRun = NewRun(knight, 5UL, 10);
            var all = TalentOfferGenerator.GetCandidates(emptyRun.Player, knight, 10, settings);
            var vitality = GetTalent("Vitality");
            var bulwark = GetTalent("Muralha");
            Assert(Mathf.Approximately(vitality.BaseWeight * settings.SharedPoolWeightMultiplier, all.First(c => c.Talent == vitality).Weight),
                "generic talent weight = base x shared multiplier");
            Assert(Mathf.Approximately(bulwark.BaseWeight, all.First(c => c.Talent == bulwark).Weight), "class talent weight is untouched (no tags taken)");
        }
    }
}
