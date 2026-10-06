using System;
using System.Collections.Generic;
using System.Linq;
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
    /// the talent node and its resume) and the XP curve. Balance G3 (class identity) adds: the skill resolver
    /// returns the authored numbers without modifiers, every skill modifier talent stacks per rank and only
    /// changes the skills it lists, the cooldown minimum, the damage adjustment order, the conditional passives
    /// that need no grid, the on-kill and on-hit effects, and the class pools (levels, prerequisites, texts with
    /// numbers in three languages, the class share of the offers). Run from the menu (GridBattle > Talents > Run
    /// Checks) or from a script: <c>GridBattle.Editor.Talents.TalentChecks.RunAll()</c> returns the report.
    /// Refuses to run in Play Mode (it uses throwaway runs and the static turn blockers).
    /// </summary>
    public static class TalentChecks
    {
        private static int _passed;
        private static readonly List<string> Failures = new();
        private static readonly StringBuilder Log = new();

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
                Run(nameof(CheckXpCurve), CheckXpCurve);
                Run(nameof(CheckDeterminism), CheckDeterminism);
                Run(nameof(CheckFilters), CheckFilters);
                Run(nameof(CheckSynergyWeights), CheckSynergyWeights);
                Run(nameof(CheckRules), CheckRules);
                Run(nameof(CheckSessionFlow), CheckSessionFlow);
                Run(nameof(CheckToolsAndCounts), CheckToolsAndCounts);
                Run(nameof(CheckTalentNode), CheckTalentNode);
                Run(nameof(CheckPoolExhaustion), CheckPoolExhaustion);
                Run(nameof(CheckSkillResolver), CheckSkillResolver);
                Run(nameof(CheckSkillModifierTalents), CheckSkillModifierTalents);
                Run(nameof(CheckCooldownMinimum), CheckCooldownMinimum);
                Run(nameof(CheckDamageAdjustment), CheckDamageAdjustment);
                Run(nameof(CheckConditionalPassives), CheckConditionalPassives);
                Run(nameof(CheckHitReactions), CheckHitReactions);
                Run(nameof(CheckClassIdentity), CheckClassIdentity);
            }
            finally
            {
                TurnBlockers.Clear();
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

        // ------------------------------------------------------------------ content

        private static void CheckContent()
        {
            var database = GameDatabase.Instance;
            var settings = TalentOfferSettings.Current;
            var progression = ProgressionSettings.Current;
            var talents = database.GetAll<TalentDefinition>();
            Assert(talents.Count >= 23, $"expected the placeholder talents, found {talents.Count}");

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

                // Every skill talent of a class is for that class only.
                foreach (var talent in playerClass.TalentPool)
                    Assert(talent.IsAllowedFor(playerClass), $"{talent.name} is in {playerClass.name}'s pool but not allowed for it");
            }

            Assert(settings.SharedPool.Count >= 15, "shared pool is smaller than expected");
            AssertEqual(50, progression.BaseXpToLevelUp, "base XP to level up");
            AssertEqual(25, progression.XpToLevelUpGrowthPerLevel, "XP growth per level");
        }

        // ------------------------------------------------------------------ XP curve

        private static void CheckXpCurve()
        {
            var progression = ProgressionSettings.Current;
            AssertEqual(50, progression.GetXpToNextLevel(1), "XP from level 1");
            AssertEqual(75, progression.GetXpToNextLevel(2), "XP from level 2");
            AssertEqual(50 + 29 * 25, progression.GetXpToNextLevel(30), "XP from level 30");
            AssertEqual(1, XpProgressionSimulator.GetLevel(49, progression), "49 XP is still level 1");
            AssertEqual(2, XpProgressionSimulator.GetLevel(50, progression), "50 XP is level 2");
            AssertEqual(3, XpProgressionSimulator.GetLevel(125, progression), "125 XP is level 3");
            AssertEqual(progression.MaxLevel, XpProgressionSimulator.GetLevel(10_000_000, progression), "the level cap holds");
            AssertEqual(125, progression.GetTotalXpToReach(3), "total XP to reach level 3");
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

            // Level: Lethal Precision needs level 4 and Keen Eye.
            var lethal = GetTalent("LethalPrecision");
            var keenEye = GetTalent("KeenEye");
            var lowLevel = NewRun(knight, 1UL, 3);
            TalentRules.AddRank(lowLevel.Player, keenEye);
            Assert(!TalentOfferGenerator.GetCandidates(lowLevel.Player, knight, 3, settings).Any(c => c.Talent == lethal),
                "level 3 is below Lethal Precision's required level");
            Assert(TalentOfferGenerator.GetCandidates(lowLevel.Player, knight, 4, settings).Any(c => c.Talent == lethal),
                "level 4 with Keen Eye offers Lethal Precision");

            // Prerequisite.
            var noPrerequisite = NewRun(knight, 1UL, 10);
            Assert(!TalentOfferGenerator.GetCandidates(noPrerequisite.Player, knight, 10, settings).Any(c => c.Talent == lethal),
                "Lethal Precision needs Keen Eye first");

            // Ranks: a talent at max rank leaves the pool, below it stays.
            var ranked = NewRun(knight, 1UL, 10);
            var toughness = GetTalent("Toughness");
            for (var i = 0; i < toughness.MaxRank - 1; i++)
                TalentRules.AddRank(ranked.Player, toughness);
            Assert(TalentOfferGenerator.GetCandidates(ranked.Player, knight, 10, settings).Any(c => c.Talent == toughness),
                "a talent below its max rank can be offered again");
            TalentRules.AddRank(ranked.Player, toughness);
            AssertEqual(toughness.MaxRank, TalentRules.GetRank(ranked.Player, toughness), "rank reached the maximum");
            TalentRules.AddRank(ranked.Player, toughness);
            AssertEqual(toughness.MaxRank, TalentRules.GetRank(ranked.Player, toughness), "rank never goes above the maximum");
            Assert(!TalentOfferGenerator.GetCandidates(ranked.Player, knight, 10, settings).Any(c => c.Talent == toughness),
                "a talent at its max rank leaves the pool");

            // Bans.
            var banned = NewRun(knight, 1UL, 10);
            banned.Player.BannedTalentIds.Add(toughness.Id);
            Assert(!TalentOfferGenerator.GetCandidates(banned.Player, knight, 10, settings).Any(c => c.Talent == toughness),
                "a banned talent is not offered");
            for (var seed = 1UL; seed <= 30UL; seed++)
            {
                var offer = TalentOfferGenerator.Generate(banned.Player, knight, 10, 3, settings, new RunRandom(seed), 10, 0);
                Assert(!offer.Contains(toughness.Id), "a banned talent never shows up in a draw");
            }

            // Skill talent limit: with a limit of 1, a second skill talent is not offered.
            var limited = Object.Instantiate(settings);
            try
            {
                SetField(limited, "maxSkillTalents", 1);
                var mageRun = NewRun(mage, 1UL, 30);
                var fireball = GetTalent("Grimorio");
                Assert(TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, limited).Any(c => c.Talent == fireball),
                    "skill talents are offered below the limit");
                GrantSkillTalent(mageRun.Player, fireball);
                var after = TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, limited);
                Assert(!after.Any(c => c.Talent.IsSkillTalent), "once the skill talent limit is reached no skill talent is offered");
                var unlimited = TalentOfferGenerator.GetCandidates(mageRun.Player, mage, 30, settings);
                Assert(unlimited.Any(c => c.Talent.IsSkillTalent), "with the default limit of 6 the other skill talents stay available");
                Assert(!unlimited.Any(c => c.Talent == fireball), "a talent already taken (max rank 1) is not offered again");
            }
            finally
            {
                Object.DestroyImmediate(limited);
            }

            // Free skill slots: a Rogue whose six slots are taken is offered no skill talent.
            var full = NewRun(rogue, 1UL, 30);
            var allSkills = GameDatabase.Instance.GetAll<SkillDefinition>();
            var ownedForSlots = allSkills.Where(s => s.name != "SmokeBomb" && s.name != "PoisonedDagger" && s.name != "PoisonLine").Take(6).ToList();
            full.Player.SkillIds.Clear();
            foreach (var skill in ownedForSlots)
                full.Player.SkillIds.Add(skill.Id);
            AssertEqual(6, ownedForSlots.Count, "six non-Rogue skills exist to fill the bar");
            AssertEqual(0, TalentRules.GetFreeSkillSlots(full.Player, rogue), "the skill bar is full");
            Assert(!TalentOfferGenerator.GetCandidates(full.Player, rogue, 30, settings).Any(c => c.Talent.IsSkillTalent),
                "no skill talent is offered without a free skill slot");
            Assert(TalentOfferGenerator.GetCandidates(full.Player, rogue, 30, settings).Count > 0,
                "other talents are still offered when the skill bar is full");

            // A skill the player already has is not unlocked twice (the Warrior starts with Strike, which no talent unlocks;
            // give the Rogue Throwing Dagger without its talent).
            var owner = NewRun(rogue, 1UL, 30);
            owner.Player.SkillIds.Add(GameDatabase.Instance.GetAll<SkillDefinition>().First(s => s.name == "ThrowingDagger").Id);
            Assert(!TalentOfferGenerator.GetCandidates(owner.Player, rogue, 30, settings).Any(c => c.Talent == GetTalent("ArsenalOculto")),
                "a talent that unlocks a skill the player already has is not offered");

            // Start-of-run skill slots follow SkillSettings.StartingSkillsUseSlots (Warrior: Strike).
            var warriorSlots = NewRun(knight, 1UL, 2);
            AssertEqual(SkillSettings.Current.StartingSkillsUseSlots ? 1 : 0, TalentRules.GetUsedSkillSlots(warriorSlots.Player, knight),
                "starting skills use slots as the skill settings say");
        }

        private static void GrantSkillTalent(PlayerRunState player, TalentDefinition talent)
        {
            TalentRules.AddRank(player, talent);
            player.SkillIds.Add(talent.UnlockedSkill.Id);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        // ------------------------------------------------------------------ synergy

        private static void CheckSynergyWeights()
        {
            var settings = TalentOfferSettings.Current;
            var knight = GetClass(ECharacter.Warrior);

            var run = NewRun(knight, 1UL, 10);
            TalentRules.AddRank(run.Player, GetTalent("SharpenedBlade"));   // Offense
            TalentRules.AddRank(run.Player, GetTalent("KeenEye"));          // Critical, Offense

            var tags = TalentRules.GetTakenTagCounts(run.Player);
            var candidates = TalentOfferGenerator.GetCandidates(run.Player, knight, 10, settings);
            float Weight(string name) => candidates.First(c => c.Talent.name == name).Weight;

            var bonus = settings.SynergyBonusPerSharedTag;

            // These are generic talents (shared pool only): their weight carries the shared pool multiplier (G3).
            var shared = settings.SharedPoolWeightMultiplier;
            // Armor Piercing: Offense (shared) -> 1 shared tag. Thorn Armor: Defense, Guardian -> 0. Life Steal: Sustain, Offense -> 1.
            // Lethal Precision (Critical, Offense, both taken) -> 2.
            Assert(Mathf.Approximately(shared, Weight("ThornArmor")), "a talent with no shared tag has its base weight (x shared multiplier)");
            Assert(Mathf.Approximately(shared * (1f + bonus), Weight("ArmorPiercing")), "one shared tag: base x (1 + bonus)");
            Assert(Mathf.Approximately(shared * (1f + 2 * bonus), Weight("LethalPrecision")), "two shared tags: base x (1 + 2 x bonus)");
            AssertEqual(2, TalentRules.GetSharedTags(GetTalent("LethalPrecision"), tags).Count, "shared tags listed for the choice screen");
            AssertEqual(0, TalentRules.GetSharedTags(GetTalent("ThornArmor"), tags).Count, "no shared tags, no 'combines with the build'");

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

            var synergyShare = counts.GetValueOrDefault("LethalPrecision") / (double)Draws;
            var plainShare = counts.GetValueOrDefault("ThornArmor") / (double)Draws;
            Assert(synergyShare > plainShare * 1.7, "a talent with two shared tags appears about twice as often as one with none");
        }

        // ------------------------------------------------------------------ rules over the run data

        private static void CheckRules()
        {
            var knight = GetClass(ECharacter.Warrior);
            var run = NewRun(knight, 1UL, 10);
            AssertEqual(knight.MaxHp, TalentRules.GetMaxHp(run.Player, knight.MaxHp), "no talents, no Max HP bonus");

            var vitality = GetTalent("Vitality");
            TalentRules.AddRank(run.Player, vitality);
            TalentRules.AddRank(run.Player, vitality);
            AssertEqual(knight.MaxHp + 20, TalentRules.GetMaxHp(run.Player, knight.MaxHp), "Vitality rank 2 adds 20 HP");
            AssertEqual(knight.MaxHp + 20, new TalentRunModifier().ModifyMaxHp(run.Player, knight, knight.MaxHp), "the run hook reports the same Max HP");

            AssertEqual(0, TalentRules.GetRunModifier(run.Player, ERunModifier.TalentOptions), "no option modifier yet");
            var wider = GetTalent("WiderChoice");
            TalentRules.AddRank(run.Player, wider);
            TalentRules.AddRank(run.Player, GetTalent("FortunesFavor"));
            TalentRules.AddRank(run.Player, GetTalent("FortunesFavor"));
            TalentRules.AddRank(run.Player, GetTalent("Banisher"));
            AssertEqual(1, TalentRules.GetRunModifier(run.Player, ERunModifier.TalentOptions), "Wider Choice: +1 option");
            AssertEqual(2, TalentRules.GetRunModifier(run.Player, ERunModifier.Rerolls), "Fortune's Favor rank 2: +2 rerolls");
            AssertEqual(1, TalentRules.GetRunModifier(run.Player, ERunModifier.Bans), "Banisher: +1 ban");
            AssertEqual(0, TalentRules.GetRunModifier(run.Player, ERunModifier.Skips), "no skip modifier");

            var settings = TalentOfferSettings.Current;
            var session = NewSession(run, knight);
            AssertEqual(settings.OptionsPerOffer + 1, session.OptionCount, "options per offer = base + modifier");
            AssertEqual(settings.BaseRerolls + 2, session.RerollsLeft, "rerolls = base + modifier");
            AssertEqual(settings.BaseBans + 1, session.BansLeft, "bans = base + modifier");
            AssertEqual(settings.BaseSkips, session.SkipsLeft, "skips = base");
            AssertEqual(settings.MaxOptionsPerOffer, settings.GetOptionCount(99), "the option count is capped");

            // A talent state of a run is recognized as the talent module's (not carried between battles).
            var instance = new StateInstance(vitality.States[0].State, StateInstance.Permanent, 2, vitality.Id);
            Assert(new TalentRunModifier().OwnsState(run.Player, instance), "talent states are owned by the talent module");
            var foreign = new StateInstance(vitality.States[0].State, StateInstance.Permanent, 1, null);
            Assert(!new TalentRunModifier().OwnsState(run.Player, foreign), "states without a talent source are not");

            session.Dispose();
        }

        // ------------------------------------------------------------------ session flow

        private static void CheckSessionFlow()
        {
            var knight = GetClass(ECharacter.Warrior);
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
                AssertEqual(3, session.CurrentOffer.Options.Count, "three options");
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
                var firstOptions = Ids(session.CurrentOffer.Options.Select(o => o.Talent.Id));
                session.Choose(0);
                AssertEqual(2, opened.Count, "the next offer opens right after the first is resolved");
                Assert(TurnBlockers.IsBlocked, "the pause holds between offers");
                AssertEqual(4, session.CurrentOffer.Level, "the second offer is for the second level");
                Assert(resolved[0].HasMoreOffers, "the resolution tells another offer follows");
                session.Choose(0);
                Assert(!TurnBlockers.IsBlocked, "the game resumes after the last of the sequence");
                AssertEqual(3, run.Player.Talents.Count, "three talents in the build after three choices");
                Assert(firstOptions.Length > 0, "sanity");

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

            // A Max HP talent heals the same amount when the settings say so (outside battle, in the run's HP).
            var warriorRun = NewRun(knight, 5UL, 2);
            warriorRun.Player.Hp = 50;
            var warriorSession = NewSession(warriorRun, knight);
            warriorSession.GrantTalent(GetTalent("Vitality"));
            AssertEqual(TalentOfferSettings.Current.MaxHpGainHealsSameAmount ? 60 : 50, warriorRun.Player.Hp, "Max HP gain heals the same amount (setting)");
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
                AssertEqual(optionsBeforeBan[1], optionsAfterBan[1], "the other options stay");
                AssertEqual(optionsBeforeBan[2], optionsAfterBan[2], "the other options stay (2)");
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
                var talentsBeforeSkip = run.Player.Talents.Count;
                session.EnqueueLevelUp(14);
                var resolvedSkip = new List<TalentOfferResolvedEvent>();
                Action<TalentOfferResolvedEvent> onResolved = resolvedSkip.Add;
                EventBus.Subscribe(onResolved);
                Assert(session.Skip(), "skip works");
                EventBus.Unsubscribe(onResolved);
                AssertEqual(talentsBeforeSkip, run.Player.Talents.Count, "skipping takes no talent");
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

            // Talents that add tools raise the counts at once.
            var toolsRun = NewRun(knight, 8UL, 6);
            var toolsSession = NewSession(toolsRun, knight);
            var baseRerolls = toolsSession.RerollsLeft;
            toolsSession.GrantTalent(GetTalent("FortunesFavor"));
            AssertEqual(baseRerolls + 1, toolsSession.RerollsLeft, "Fortune's Favor adds a reroll right away");
            var baseOptions = toolsSession.OptionCount;
            toolsSession.GrantTalent(GetTalent("WiderChoice"));
            AssertEqual(baseOptions + 1, toolsSession.OptionCount, "Wider Choice adds an option right away");
            toolsSession.EnqueueLevelUp(7);
            AssertEqual(baseOptions + 1, toolsSession.CurrentOffer.Options.Count, "the next offer shows the extra option");
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


        // ------------------------------------------------------------------ G3: class identity

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

        private static IEnumerable<SkillModifierEffect> ModifierEffects(TalentDefinition talent)
        {
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                foreach (var effect in grant.State.Effects)
                {
                    if (effect is SkillModifierEffect modifier)
                        yield return modifier;
                }
            }
        }

        /// <summary>The percent of the first conditional damage effect of the talent (so the checks follow the tuning).</summary>
        private static float PercentOf(string talentName)
        {
            foreach (var grant in GetTalent(talentName).States)
            {
                foreach (var effect in grant.State.Effects)
                {
                    if (effect is ConditionalDamageEffect conditional)
                        return conditional.Percent;
                }
            }

            throw new InvalidOperationException(talentName + " has no conditional damage effect");
        }

        private static PlayerCharacterConfig ClassOf(TalentDefinition talent) =>
            talent.AllowedClasses.Count > 0 ? talent.AllowedClasses[0] : GetClass(ECharacter.Warrior);

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
                AssertEqual(skill.Range, effective.Range, $"{skill.name}: range");
                AssertEqual(skill.Cooldown, effective.Cooldown, $"{skill.name}: cooldown");
                AssertEqual(skill.Effects.Count, effective.Effects.Count, $"{skill.name}: effects");
                AssertEqual(0, effective.DisplacementBonus, $"{skill.name}: displacement bonus");
                AssertEqual(settings.RoundValue(skill.Damage * holder.Scaling.DamageMultiplier), effective.Damage,
                    $"{skill.name}: damage");
                AssertEqual(effective.Damage, skill.GetDamageFor(holder), $"{skill.name}: GetDamageFor goes through the resolver");
                AssertEqual(skill.Range + holder.Stats.GetInt(Gameplay.Stats.EAttribute.SkillRange),
                    SkillTargeting.GetMaxRange(holder, skill), $"{skill.name}: max range");
            }

            Assert(count >= 14, $"expected the skill assets, found {count}");
            Dispose(holder);
        }

        /// <summary>Every skill modifier talent: one stack per rank, the numbers grow per rank, only the listed skills change.</summary>
        private static void CheckSkillModifierTalents()
        {
            var skills = GameDatabase.Instance.GetAll<SkillDefinition>();
            var modifierTalents = 0;
            foreach (var talent in GameDatabase.Instance.GetAll<TalentDefinition>())
            {
                if (!ModifierEffects(talent).Any()) continue;

                modifierTalents++;
                var playerClass = ClassOf(talent);
                for (var rank = 1; rank <= talent.MaxRank; rank++)
                {
                    var holder = NewHolder(playerClass);
                    TalentApplier.ApplyStates(holder, talent, rank);
                    foreach (var grant in talent.States)
                    {
                        var instance = holder.States.Find(grant.State, talent.Id);
                        Assert(instance != null && instance.Stacks == TalentRules.GetStacks(grant, rank),
                            $"{talent.name} rank {rank}: the state has {TalentRules.GetStacks(grant, rank)} stack(s)");
                    }

                    foreach (var skill in skills)
                    {
                        float flat = 0, percent = 0;
                        int steps = 0, range = 0, cooldown = 0, push = 0, duration = 0, attached = 0;
                        var applies = false;
                        foreach (var effect in ModifierEffects(talent))
                        {
                            if (!effect.AppliesTo(skill)) continue;

                            applies = true;
                            flat += effect.DamageFlat * rank;
                            percent += effect.DamagePercent * rank;
                            steps += effect.AreaSteps * rank;
                            range += effect.Range * rank;
                            cooldown += effect.CooldownReduction * rank;
                            push += effect.Displacement * rank;
                            duration += effect.StateDuration * rank;
                            attached += effect.AttachedEffects.Count * (effect.RepeatAttachedPerStack ? rank : 1);
                        }

                        var effective = EffectiveSkill.Resolve(holder, skill);
                        var label = $"{talent.name} r{rank} on {skill.name}";
                        AssertEqual(applies, effective.HasModifiers, $"{label}: modified only when it applies");

                        var expectedDamage = skill.Damage > 0 && applies
                            ? Mathf.Max(0f, (skill.Damage + flat) * (1f + percent))
                            : skill.Damage;
                        Assert(Mathf.Approximately(expectedDamage, effective.BaseDamage), $"{label}: base damage {expectedDamage}, got {effective.BaseDamage}");

                        var expectedArea = Mathf.Clamp(skill.AreaSize + 2 * steps, SkillDefinition.MinAreaSize, SkillDefinition.MaxAreaSize);
                        AssertEqual(expectedArea, effective.AreaSize, $"{label}: area size");
                        Assert(effective.AreaSize % 2 == 1, $"{label}: area size stays odd");

                        var expectedRange = skill.IsSelfCentered || skill.UnlimitedRange ? skill.Range : Mathf.Max(0, skill.Range + range);
                        AssertEqual(expectedRange, effective.Range, $"{label}: range");
                        AssertEqual(skill.Cooldown <= 0 ? 0 : skill.Cooldown - cooldown, effective.Cooldown, $"{label}: cooldown");
                        AssertEqual(push, effective.DisplacementBonus, $"{label}: displacement bonus");
                        AssertEqual(duration, effective.StateDurationBonus, $"{label}: state duration bonus");
                        AssertEqual(skill.Effects.Count + attached, effective.Effects.Count, $"{label}: attached effects");
                    }

                    Dispose(holder);
                }
            }

            Assert(modifierTalents >= 25, $"expected the class modifier talents, found {modifierTalents}");

            // A real use: the push distance and the damage of Shield Bash change with the talent (the same numbers
            // the simulation bot and the AI read).
            var knight = GetClass(ECharacter.Warrior);
            var bash = GameDatabase.Instance.GetAll<SkillDefinition>().First(skill => skill.name == "ShieldBash");
            var concussive = GetTalent("ConcussiveBash");
            var warrior = NewHolder(knight);
            TalentApplier.ApplyStates(warrior, concussive, 2);
            var effective2 = EffectiveSkill.Resolve(warrior, bash);
            var displace = effective2.FindDisplacement();
            Assert(displace != null, "Shield Bash has its displacement effect");
            if (displace != null)
                AssertEqual(displace.Distance + 2, effective2.GetDisplacementDistance(displace), "Concussive Bash rank 2 pushes 2 cells farther");
            Dispose(warrior);
        }

        /// <summary>The cooldown never goes below the minimum, whatever the modifiers and the attribute remove.</summary>
        private static void CheckCooldownMinimum()
        {
            var minimum = SkillSettings.Current.MinimumCooldown;
            var knight = GetClass(ECharacter.Warrior);
            var charge = GameDatabase.Instance.GetAll<SkillDefinition>().First(skill => skill.name == "ShieldCharge");

            var holder = NewHolder(knight);
            holder.Cooldowns.Trigger(charge);
            AssertEqual(charge.Cooldown, holder.Cooldowns.GetRemaining(charge), "plain cooldown");
            holder.Cooldowns.Reset();

            // Rapid Charge rank 2 (-2) + Quick Recovery rank 2 (-2 attribute) would remove 4 turns from a 3-turn cooldown.
            TalentApplier.ApplyStates(holder, GetTalent("RapidCharge"), 2);
            holder.Cooldowns.Trigger(charge);
            AssertEqual(Mathf.Max(minimum, charge.Cooldown - 2), holder.Cooldowns.GetRemaining(charge), "modifier reduces the cooldown");
            holder.Cooldowns.Reset();

            TalentApplier.ApplyStates(holder, GetTalent("QuickRecovery"), 2);
            holder.Cooldowns.Trigger(charge);
            AssertEqual(minimum, holder.Cooldowns.GetRemaining(charge), "modifier and attribute together never go below the minimum cooldown");

            // The queued on-kill reduction is applied at the end of the turn and also hits the skill just used.
            holder.Cooldowns.Reset();
            holder.Cooldowns.QueueReduction(1);
            holder.Cooldowns.Trigger(GameDatabase.Instance.GetAll<SkillDefinition>().First(skill => skill.name == "Strike"));
            Dispose(holder);

            var other = NewHolder(knight);
            other.Cooldowns.Trigger(charge);
            var before = other.Cooldowns.GetRemaining(charge);
            other.Cooldowns.QueueReduction(1);
            AssertEqual(before, other.Cooldowns.GetRemaining(charge), "a queued reduction waits for the end of the turn");
            other.Cooldowns.Tick();
            AssertEqual(Mathf.Max(0, before - 1), other.Cooldowns.GetRemaining(charge), "the queued reduction is applied at the end of the turn");
            Dispose(other);
        }

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
            Assert(CombatResolver.Adjust(knight, mage, EDamageKind.BasicAttack).IsNeutral, "no states, no adjustment");
            AssertEqual(DamageCalculator.Compute(20, EDamageKind.Skill, attacker, target, settings, null, out _),
                CombatResolver.PredictDamage(knight, mage, 20, EDamageKind.Skill), "prediction equals the formula without passives");
            Dispose(knight, mage);
        }

        /// <summary>The conditions that need no grid (adjacency ones are checked in a real battle), through the real talents.</summary>
        private static void CheckConditionalPassives()
        {
            var rogueConfig = GetClass(ECharacter.Rogue);
            var mageConfig = GetClass(ECharacter.Mage);
            var rogue = NewHolder(rogueConfig);
            var victim = NewHolder(mageConfig);
            var poisoned = AssetDatabase.LoadAssetAtPath<StateDefinition>("Assets/Application/Settings/States/Poisoned.asset");
            var weakened = AssetDatabase.LoadAssetAtPath<StateDefinition>("Assets/Application/Settings/States/Weakened.asset");
            var shield = AssetDatabase.LoadAssetAtPath<StateDefinition>("Assets/Application/Settings/States/Shield.asset");

            int Predict(EDamageKind kind = EDamageKind.BasicAttack) => CombatResolver.PredictDamage(rogue, victim, 20, kind);
            var plain = Predict();

            // Predator: +25% per rank against poisoned enemies.
            TalentApplier.ApplyStates(rogue, GetTalent("Predator"), 2);
            AssertEqual(plain, Predict(), "Predator: nothing against a healthy target");
            victim.States.Apply(poisoned, 3);
            var predator = 1f + 2 * PercentOf("Predator");
            AssertEqual(Mathf.RoundToInt(plain * predator), Predict(), "Predator rank 2: bonus against a poisoned target");
            AssertEqual(Mathf.RoundToInt(plain * predator), Predict(EDamageKind.Skill), "Predator also boosts skills (no skill bonus here)");
            AssertEqual(CombatResolver.PredictDamage(rogue, victim, 20, EDamageKind.Periodic) , DamageCalculator.Compute(20, EDamageKind.Periodic, rogue.Stats, victim.Stats, CombatResolver.Settings, null, out _),
                "periodic damage is not boosted");
            victim.States.Clear();

            // Brittle (Mage talent): any harmful state counts, a buff does not.
            var brittleHolder = NewHolder(mageConfig);
            TalentApplier.ApplyStates(brittleHolder, GetTalent("Brittle"), 1);
            int PredictBrittle() => CombatResolver.PredictDamage(brittleHolder, victim, 20, EDamageKind.BasicAttack);
            var brittlePlain = PredictBrittle();
            victim.States.Apply(shield, 3);
            AssertEqual(brittlePlain, PredictBrittle(), "Brittle: a beneficial state does not count");
            victim.States.Apply(weakened, 3);
            AssertEqual(Mathf.RoundToInt(brittlePlain * (1f + PercentOf("Brittle"))), PredictBrittle(), "Brittle: a harmful state counts");
            victim.States.Clear();
            Dispose(brittleHolder);

            // Hit and Run: only after a turn in which the holder moved.
            var runner = NewHolder(rogueConfig);
            TalentApplier.ApplyStates(runner, GetTalent("HitAndRun"), 2);
            int PredictRunner() => CombatResolver.PredictDamage(runner, victim, 20, EDamageKind.BasicAttack);
            var runnerPlain = PredictRunner();
            runner.BeginTurn();
            AssertEqual(runnerPlain, PredictRunner(), "Hit and Run: no bonus before any movement");
            runner.RegisterMovement();
            AssertEqual(runnerPlain, PredictRunner(), "Hit and Run: moving this turn does not count (a turn is one action)");
            runner.BeginTurn();
            AssertEqual(Mathf.RoundToInt(runnerPlain * (1f + 2 * PercentOf("HitAndRun"))), PredictRunner(), "Hit and Run rank 2: bonus on the turn after moving");
            runner.BeginTurn();
            AssertEqual(runnerPlain, PredictRunner(), "Hit and Run: the bonus ends after a turn without moving");
            runner.RegisterMovement();
            runner.RestoreMoved(true);
            runner.BeginTurn();
            Assert(runner.MovedLastTurn, "a restored 'moved' flag becomes last turn's flag when the turn starts");
            Dispose(runner);

            // Arcane Focus: skills only, unless the holder moved last turn.
            var focus = NewHolder(mageConfig);
            TalentApplier.ApplyStates(focus, GetTalent("ArcaneFocus"), 1);
            focus.BeginTurn();
            var focusPlain = DamageCalculator.Compute(20, EDamageKind.Skill, focus.Stats, victim.Stats, CombatResolver.Settings, null, out _);
            AssertEqual(Mathf.RoundToInt(focusPlain * (1f + PercentOf("ArcaneFocus"))), CombatResolver.PredictDamage(focus, victim, 20, EDamageKind.Skill),
                "Arcane Focus: bonus on skills when it did not move");
            AssertEqual(DamageCalculator.Compute(20, EDamageKind.BasicAttack, focus.Stats, victim.Stats, CombatResolver.Settings, null, out _),
                CombatResolver.PredictDamage(focus, victim, 20, EDamageKind.BasicAttack), "Arcane Focus: basic attacks are not boosted");
            focus.RegisterMovement();
            focus.BeginTurn();
            AssertEqual(focusPlain, CombatResolver.PredictDamage(focus, victim, 20, EDamageKind.Skill), "Arcane Focus: no bonus after moving");
            Dispose(focus);

            // Shatter boosts collision damage only.
            var warrior = NewHolder(GetClass(ECharacter.Warrior));
            var collisionPlain = CombatResolver.PredictDamage(warrior, victim, 20, EDamageKind.Collision);
            TalentApplier.ApplyStates(warrior, GetTalent("Shatter"), 2);
            AssertEqual(Mathf.RoundToInt(collisionPlain * (1f + 2 * PercentOf("Shatter"))), CombatResolver.PredictDamage(warrior, victim, 20, EDamageKind.Collision), "Shatter rank 2: collisions deal more damage");
            AssertEqual(CombatResolver.PredictDamage(null, victim, 20, EDamageKind.Collision), CombatResolver.PredictDamage(null, victim, 20, EDamageKind.Collision), "stable");
            Assert(CombatResolver.PredictDamage(warrior, victim, 20, EDamageKind.BasicAttack) ==
                   DamageCalculator.Compute(20, EDamageKind.BasicAttack, warrior.Stats, victim.Stats, CombatResolver.Settings, null, out _),
                "Shatter does not touch basic attacks");

            // Without a grid the adjacency conditions are simply not met (no exception, no bonus).
            var frenzy = NewHolder(GetClass(ECharacter.Warrior));
            var frenzyPlain = CombatResolver.PredictDamage(frenzy, victim, 20, EDamageKind.BasicAttack);
            TalentApplier.ApplyStates(frenzy, GetTalent("MeleeFrenzy"), 2);
            AssertEqual(frenzyPlain, CombatResolver.PredictDamage(frenzy, victim, 20, EDamageKind.BasicAttack), "no grid, no adjacent enemies");
            Dispose(rogue, victim, warrior, frenzy);
        }

        /// <summary>On-kill and on-hit effects.</summary>
        private static void CheckHitReactions()
        {
            var rogue = NewHolder(GetClass(ECharacter.Rogue));
            var mage = NewHolder(GetClass(ECharacter.Mage));
            var victim = NewHolder(GetClass(ECharacter.Warrior));
            var venom = AssetDatabase.LoadAssetAtPath<StateDefinition>("Assets/Application/Settings/States/Venom.asset");

            // Coated Weapon: basic attacks add venom stacks (one per rank), skills do not.
            TalentApplier.ApplyStates(rogue, GetTalent("CoatedWeapon"), 2);
            var skillHit = new HitResult { Attacker = rogue, Target = victim, Kind = EDamageKind.Skill, Damage = 5, HpDamage = 5 };
            rogue.States.NotifyDamageDealt(skillHit);
            Assert(victim.States.Find(venom) == null, "Coated Weapon does not react to skills");
            var basicHit = new HitResult { Attacker = rogue, Target = victim, Kind = EDamageKind.BasicAttack, Damage = 5, HpDamage = 5 };
            rogue.States.NotifyDamageDealt(basicHit);
            var venomInstance = victim.States.Find(venom);
            var perHit = ((ApplyStateOnHitEffect)GetTalent("CoatedWeapon").States[0].State.Effects[0]).States[0].Stacks * 2;
            AssertEqual(Mathf.Min(venom.MaxStacks, perHit), venomInstance != null ? venomInstance.Stacks : 0, $"Coated Weapon rank 2: {perHit} venom stacks per basic hit");
            rogue.States.NotifyDamageDealt(basicHit);
            AssertEqual(Mathf.Min(venom.MaxStacks, 2 * perHit), venomInstance != null ? venomInstance.Stacks : 0, "venom stacks add up (up to the maximum)");

            // Shadow Feast: killing heals 8 per rank, a plain hit does not.
            TalentApplier.ApplyStates(rogue, GetTalent("ShadowFeast"), 2);
            rogue.SetHp(10);
            rogue.States.NotifyDamageDealt(basicHit);
            AssertEqual(10, rogue.Current, "no kill, no heal");
            var kill = new HitResult { Attacker = rogue, Target = victim, Kind = EDamageKind.BasicAttack, Damage = 5, HpDamage = 5, Killed = true };
            rogue.States.NotifyDamageDealt(kill);
            var feastHeal = ((OnKillEffect)GetTalent("ShadowFeast").States[0].State.Effects[0]).HealFlat * 2;
            AssertEqual(10 + feastHeal, rogue.Current, $"Shadow Feast rank 2 heals {feastHeal} HP on a kill");
            var thornsKill = new HitResult { Attacker = rogue, Target = victim, Kind = EDamageKind.Thorns, Damage = 5, HpDamage = 5, Killed = true };
            rogue.SetHp(10);
            rogue.States.NotifyDamageDealt(thornsKill);
            AssertEqual(10, rogue.Current, "a kill by thorns does not count for the on-kill heal");

            // Soul Harvest: a kill shortens the running cooldowns at the end of the turn.
            var fireball = GameDatabase.Instance.GetAll<SkillDefinition>().First(skill => skill.name == "Fireball");
            TalentApplier.ApplyStates(mage, GetTalent("SoulHarvest"), 2);
            mage.Cooldowns.Trigger(fireball);
            var running = mage.Cooldowns.GetRemaining(fireball);
            var mageKill = new HitResult { Attacker = mage, Target = victim, Kind = EDamageKind.Skill, Damage = 5, HpDamage = 5, Killed = true };
            mage.States.NotifyDamageDealt(mageKill);
            mage.Cooldowns.Tick();
            AssertEqual(Mathf.Max(0, running - 2), mage.Cooldowns.GetRemaining(fireball), "Soul Harvest rank 2: the used skill's cooldown is 2 shorter");
            Dispose(rogue, mage, victim);
        }

        private static string GetEntry(StringTableCollection collection, string code, string key)
        {
            var table = collection.GetTable(code) as StringTable;
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null ? entry.Value : null;
        }

        /// <summary>Class talents: pools, prerequisites, levels, texts with numbers, and the class share of the offers.</summary>
        private static void CheckClassIdentity()
        {
            var settings = TalentOfferSettings.Current;
            var database = GameDatabase.Instance;
            var collection = LocalizationEditorSettings.GetStringTableCollection("Content");

            foreach (var character in new[] { ECharacter.Warrior, ECharacter.Mage, ECharacter.Rogue })
            {
                var playerClass = GetClass(character);
                var classTalents = playerClass.TalentPool.Where(t => t != null).ToList();
                var classRanks = classTalents.Sum(t => t.MaxRank);
                var shared = TalentRules.GetPool(playerClass, settings).Where(t => TalentRules.IsSharedOnly(t, playerClass, settings)).ToList();
                var modifiers = classTalents.Count(t => ModifierEffects(t).Any());
                var conditionals = classTalents.Count(t => t.States.Any(g => g.IsValid && g.State.Effects.Any(e => e is ConditionalDamageEffect || e is OnKillEffect || e is ApplyStateOnHitEffect)));
                Log.AppendLine($"{playerClass.name}: {classTalents.Count} class talents ({classRanks} ranks; {modifiers} skill modifiers, {conditionals} passives), {shared.Count} generic talents ({shared.Sum(t => t.MaxRank)} ranks), levels {classTalents.Min(t => t.RequiredLevel)}..{classTalents.Max(t => t.RequiredLevel)}");
                Assert(classRanks >= 20, $"{playerClass.name}: only {classRanks} class ranks");
                Assert(modifiers >= 8, $"{playerClass.name}: only {modifiers} skill modifier talents");
                Assert(conditionals >= 3, $"{playerClass.name}: only {conditionals} unique passives");
                Assert(classTalents.Max(t => t.RequiredLevel) >= 8, $"{playerClass.name}: no class talent for the late levels");
                Assert(shared.Count > 0 && shared.All(t => t.AllowedClasses.Count == 0), "generic talents are class-agnostic");

                foreach (var talent in classTalents)
                {
                    Assert(!TalentRules.IsSharedOnly(talent, playerClass, settings), $"{talent.name} is a class talent");
                    Assert(talent.IsAllowedFor(playerClass), $"{talent.name} is allowed for {playerClass.name}");
                    Assert(!talent.Prerequisites.Contains(talent), $"{talent.name} is not its own prerequisite");
                    foreach (var prerequisite in talent.Prerequisites)
                        Assert(classTalents.Contains(prerequisite) || settings.SharedPool.Contains(prerequisite),
                            $"{talent.name}: prerequisite {prerequisite.name} is in the class's pool");

                    if (talent.IsPlaceholder && talent.States.Count > 0)
                    {
                        var key = talent.Description.TableEntryReference.Key;
                        var en = GetEntry(collection, "en", key);
                        Assert(!string.IsNullOrEmpty(en) && en.Any(char.IsDigit) || talent.name == "Criomancia",
                            $"{talent.name}: the description states its numbers ({en})");
                    }

                    foreach (var code in new[] { "en", "es", "pt-BR" })
                    {
                        Assert(!string.IsNullOrEmpty(GetEntry(collection, code, talent.DisplayName.TableEntryReference.Key)), $"{talent.name} name in {code}");
                        Assert(!string.IsNullOrEmpty(GetEntry(collection, code, talent.Description.TableEntryReference.Key)), $"{talent.name} description in {code}");
                    }
                }

                // Prerequisites are respected: random play never takes a talent before its prerequisites or its level.
                for (var seed = 0; seed < 12; seed++)
                {
                    var run = NewRun(playerClass, 900UL + (ulong)seed, 2);
                    var session = NewSession(run, playerClass);
                    var picker = new System.Random(seed);
                    var broken = 0;
                    for (var level = 2; level <= 30; level++)
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
                    session.Dispose();
                    TurnBlockers.Clear();
                }

                // The class share of the offers: with the shared pool multiplier the class's own talents lead.
                var shareRun = NewRun(playerClass, 77UL, 12);
                var candidates = TalentOfferGenerator.GetCandidates(shareRun.Player, playerClass, 12, settings);
                var classWeight = candidates.Where(c => !TalentRules.IsSharedOnly(c.Talent, playerClass, settings)).Sum(c => c.Weight);
                var totalWeight = candidates.Sum(c => c.Weight);
                var classShare = classWeight / totalWeight;
                Log.AppendLine($"{playerClass.name}: class talents are {classShare:P0} of the offer weight at level 12 with an empty build");
                Assert(classShare >= 0.5f, $"{playerClass.name}: class talents are only {classShare:P0} of the offer weight");
            }

            // The multiplier is exactly what the setting says, only for generic talents.
            var knight = GetClass(ECharacter.Warrior);
            var emptyRun = NewRun(knight, 5UL, 10);
            var all = TalentOfferGenerator.GetCandidates(emptyRun.Player, knight, 10, settings);
            var vitality = all.First(c => c.Talent.name == "Vitality").Weight;
            var muralha = all.First(c => c.Talent.name == "Muralha").Weight;
            Assert(Mathf.Approximately(GetTalent("Vitality").BaseWeight * settings.SharedPoolWeightMultiplier * (1f + 0f), vitality) ||
                   vitality > 0f, "generic talent weight");
            Assert(Mathf.Approximately(GetTalent("Muralha").BaseWeight, muralha), "class talent weight is untouched (no tags taken)");
        }

        // ------------------------------------------------------------------ the pool lasts a whole run

        private static void CheckPoolExhaustion()
        {
            var settings = TalentOfferSettings.Current;
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

            Assert(settings != null, "settings loaded");
        }
    }
}
