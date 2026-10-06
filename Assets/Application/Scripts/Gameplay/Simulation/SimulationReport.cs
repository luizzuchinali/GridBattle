using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Run;
using UnityEngine;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// Turns the records of a batch into <c>summary.md</c> (the tables the designer reads), <c>runs.csv</c> and
    /// <c>battles.csv</c> (for spreadsheets), written to
    /// <c>persistentDataPath/&lt;OutputFolder&gt;/&lt;timestamp&gt;_&lt;label&gt;/</c>. Pure aggregation over
    /// <see cref="RunRecord"/> and <see cref="BattleRecord"/>: nothing here touches the game.
    /// </summary>
    public static class SimulationReport
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly EBattleDifficulty[] Difficulties =
            { EBattleDifficulty.Easy, EBattleDifficulty.Normal, EBattleDifficulty.Hard };

        /// <summary>Builds the summary, fills <c>SummaryText</c> and <c>OutputFolder</c> and writes the files.</summary>
        public static void Write(SimulationBatchResult batch)
        {
            batch.SummaryText = BuildSummary(batch);

            try
            {
                var options = batch.Options;
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", Invariant);
                var label = Sanitize(options.Label);
                var folder = Path.Combine(Application.persistentDataPath, options.OutputFolder, $"{stamp}_{label}");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "summary.md"), batch.SummaryText);
                if (options.WriteCsv)
                {
                    File.WriteAllText(Path.Combine(folder, "runs.csv"), BuildRunsCsv(batch));
                    File.WriteAllText(Path.Combine(folder, "battles.csv"), BuildBattlesCsv(batch));
                }

                batch.OutputFolder = folder;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Balance simulation: could not write the report: {exception.Message}");
            }
        }

        /// <summary>
        /// One table comparing the batches of a session (win rate, depth, level, aborts per class), written next to
        /// the batch folders as <c>&lt;timestamp&gt;_comparison.md</c>. Returns the text ("" for a single batch).
        /// </summary>
        public static string WriteComparison(IReadOnlyList<SimulationBatchResult> results)
        {
            if (results == null || results.Count < 2) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("# Balance simulation: comparison of batches");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var batch in results)
            {
                foreach (var cls in batch.Runs.Select(r => r.ClassName).Distinct())
                {
                    var set = batch.Runs.Where(r => r.ClassName == cls).ToList();
                    var wins = set.Count(r => r.Result == ERunResult.Victory);
                    var battles = batch.Battles.Where(b => b.ClassName == cls && !b.Aborted).ToList();
                    rows.Add(new[]
                    {
                        batch.Options.Label,
                        batch.Options.DescribePolicies(),
                        cls,
                        set.Count.ToString(Invariant),
                        $"{Percent(wins, set.Count)} ({WilsonInterval(wins, set.Count)})",
                        Avg(set.Select(r => (double)r.EndDepth)),
                        Avg(set.Select(r => (double)r.FinalLevel)),
                        Avg(battles.Select(b => PercentOfMax(b.DamageTaken, b.MaxHp)), "0") + "%",
                        Avg(battles.Select(b => (double)b.Turns)),
                        set.Count(r => r.Result == ERunResult.Aborted).ToString(Invariant),
                    });
                }
            }

            Table(sb, new[]
            {
                "Batch", "Policies", "Class", "Runs", "Win rate (95% CI)", "Avg depth", "Avg level", "Damage % max HP per battle",
                "Player actions per battle", "Aborted",
            }, rows);

            var text = sb.ToString();
            try
            {
                var options = results[0].Options;
                var folder = Path.Combine(Application.persistentDataPath, options.OutputFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"{DateTime.Now.ToString("yyyyMMdd_HHmmss", Invariant)}_comparison.md"), text);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Balance simulation: could not write the comparison: {exception.Message}");
            }

            return text;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "batch";

            var builder = new StringBuilder();
            foreach (var c in value)
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return builder.ToString();
        }

        // ------------------------------------------------------------------------------------ summary

        /// <summary>The markdown summary of a batch.</summary>
        public static string BuildSummary(SimulationBatchResult batch)
        {
            var options = batch.Options;
            var runs = batch.Runs;
            var battles = batch.Battles;
            var classes = runs.Select(r => r.ClassName).Distinct().ToList();
            var roles = BuildRoleMap();
            var sb = new StringBuilder();

            sb.AppendLine($"# Balance simulation: {options.Label}");
            sb.AppendLine();
            sb.AppendLine($"- Status: {batch.Status}");
            sb.AppendLine($"- Policies: {options.DescribePolicies()}");
            sb.AppendLine($"- Runs per class: {options.RunsPerClass}, seeds {options.StartSeed}..{options.StartSeed + options.RunsPerClass - 1} " +
                          $"(same maps for every class), bot seed {options.BotSeed}");
            sb.AppendLine($"- Limits: {options.MaxTurnsPerBattle} turns per battle, {options.StuckFrames} stuck frames, {options.MaxSecondsPerRun.ToString("0", Invariant)} s per run");
            sb.AppendLine();

            AppendSpeed(sb, batch);
            AppendOverview(sb, runs, classes);
            AppendDeathHistogram(sb, runs, classes);
            AppendDeathSpread(sb, runs, battles, classes);
            AppendFloorBands(sb, battles);
            AppendLevelByDepth(sb, battles, classes);

            foreach (var cls in classes)
                AppendClassDetail(sb, cls, runs, battles);

            AppendXpAnalysis(sb, runs, battles);
            AppendEnemies(sb, battles, runs, roles, classes);
            AppendTalents(sb, runs, classes);
            AppendUsage(sb, battles, runs, classes);
            AppendDisplacement(sb, battles, runs, classes);
            AppendAborted(sb, runs, battles);
            return sb.ToString();
        }

        private static void AppendSpeed(StringBuilder sb, SimulationBatchResult batch)
        {
            var seconds = batch.TotalMilliseconds / 1000.0;
            var runs = batch.Runs.Count;
            sb.AppendLine("## Speed and health of the simulation");
            sb.AppendLine();
            sb.AppendLine($"- {runs} runs, {batch.Battles.Count} battles in {seconds.ToString("0.0", Invariant)} s " +
                          $"({(runs > 0 ? (seconds / runs).ToString("0.00", Invariant) : "-")} s per run, " +
                          $"{(seconds > 0 ? (runs / seconds).ToString("0.0", Invariant) : "-")} runs per second)");
            if (runs > 0)
            {
                var frames = batch.Runs.Select(r => (double)r.Frames).Average();
                var maxRun = batch.Runs.Max(r => r.Milliseconds);
                sb.AppendLine($"- Frames per run: {frames.ToString("0", Invariant)} on average; slowest run {maxRun.ToString("0", Invariant)} ms");
            }

            sb.AppendLine($"- Aborted runs: {batch.Runs.Count(r => r.Result == ERunResult.Aborted)}; exceptions: {batch.Exceptions}; console errors: {batch.ConsoleErrors}");
            sb.AppendLine();
        }

        private static void AppendOverview(StringBuilder sb, List<RunRecord> runs, List<string> classes)
        {
            sb.AppendLine("## Results by class");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var cls in classes)
            {
                var set = runs.Where(r => r.ClassName == cls).ToList();
                var wins = set.Count(r => r.Result == ERunResult.Victory);
                var finished = set.Count(r => r.Result != ERunResult.Aborted);
                rows.Add(new[]
                {
                    cls,
                    set.Count.ToString(Invariant),
                    $"{Percent(wins, set.Count)} ({WilsonInterval(wins, set.Count)})",
                    set.Count(r => r.Result == ERunResult.Defeat).ToString(Invariant),
                    set.Count(r => r.Result == ERunResult.Aborted).ToString(Invariant),
                    Avg(set.Select(r => (double)r.EndDepth)),
                    Avg(set.Select(r => (double)r.FinalLevel)),
                    Avg(set.Select(r => (double)r.BattlesWon)),
                    Avg(set.Select(r => (double)r.Turns)),
                    Avg(set.Select(r => (double)r.TalentsPicked)),
                    Avg(set.Select(r => (double)r.DistinctTalents)),
                    Avg(set.Select(r => (double)r.SkillsUsed)),
                    Avg(set.Select(r => (double)r.ConsumablesUsed)),
                    finished == 0 ? "-" : Avg(set.Select(r => r.Milliseconds), "0"),
                });
            }

            Table(sb, new[]
            {
                "Class", "Runs", "Win rate (95% CI)", "Defeats", "Aborted", "Avg depth", "Avg level", "Battles won",
                "Player actions", "Talents", "Distinct", "Skill uses", "Item uses", "ms/run",
            }, rows);

            var all = runs.Where(r => r.Result != ERunResult.Aborted).ToList();
            if (all.Count > 0)
            {
                sb.AppendLine($"Nodes per run (average): heal {Avg(runs.Select(r => (double)r.HealNodes))}, " +
                              $"talent {Avg(runs.Select(r => (double)r.TalentNodes))}, " +
                              $"consumable {Avg(runs.Select(r => (double)r.ConsumableNodes))}; " +
                              $"rerolls {Avg(runs.Select(r => (double)r.Rerolls))}, bans {Avg(runs.Select(r => (double)r.Bans))}, " +
                              $"skips {Avg(runs.Select(r => (double)r.Skips))}.");
                sb.AppendLine();
            }
        }

        private static void AppendDeathHistogram(StringBuilder sb, List<RunRecord> runs, List<string> classes)
        {
            var floors = runs.Count == 0 ? 0 : runs.Max(r => r.Floors);
            sb.AppendLine($"## Where runs end (depth of the death; the map has {floors} floors, the last one is the boss)");
            sb.AppendLine();
            var maxDepth = Math.Max(1, runs.Count == 0 ? 1 : runs.Max(r => r.EndDepth));
            var header = new List<string> { "Depth" };
            foreach (var cls in classes)
            {
                header.Add($"{cls} defeats");
                header.Add($"{cls} %");
            }

            var rows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                foreach (var cls in classes)
                {
                    var set = runs.Where(r => r.ClassName == cls).ToList();
                    var count = set.Count(r => r.Result == ERunResult.Defeat && r.EndDepth == depth);
                    row.Add(count.ToString(Invariant));
                    row.Add(Percent(count, set.Count));
                }

                rows.Add(row.ToArray());
            }

            var victoryRow = new List<string> { "Victory" };
            foreach (var cls in classes)
            {
                var set = runs.Where(r => r.ClassName == cls).ToList();
                var count = set.Count(r => r.Result == ERunResult.Victory);
                victoryRow.Add(count.ToString(Invariant));
                victoryRow.Add(Percent(count, set.Count));
            }

            rows.Add(victoryRow.ToArray());
            var abortedRow = new List<string> { "Aborted" };
            foreach (var cls in classes)
            {
                var set = runs.Where(r => r.ClassName == cls).ToList();
                var count = set.Count(r => r.Result == ERunResult.Aborted);
                abortedRow.Add(count.ToString(Invariant));
                abortedRow.Add(Percent(count, set.Count));
            }

            rows.Add(abortedRow.ToArray());
            Table(sb, header.ToArray(), rows);
        }

        /// <summary>Floor bands of the tactical-depth report: first and last depth (the boss floor is its own band).</summary>
        private static readonly (string Label, int From, int To)[] FloorBands =
        {
            ("1-4", 1, 4), ("5-9", 5, 9), ("10-14", 10, 14), ("15-19", 15, 19), ("20-24", 20, 24), ("25-29", 25, 29),
        };

        /// <summary>
        /// How the lost runs spread over the map (deaths before depth 10, 10-19, 20-29, in the boss fight) and how
        /// the boss fight goes for the runs that reach it (win rate when reached, level on entering it).
        /// </summary>
        private static void AppendDeathSpread(StringBuilder sb, List<RunRecord> runs, List<BattleRecord> battles,
            List<string> classes)
        {
            sb.AppendLine("## Death spread and the final boss");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var cls in classes.Concat(new[] { "All" }))
            {
                var set = runs.Where(r => cls == "All" || r.ClassName == cls).ToList();
                var defeats = set.Where(r => r.Result == ERunResult.Defeat).ToList();
                var boss = battles.Where(b => (cls == "All" || b.ClassName == cls) && b.NodeType == EMapNodeType.Boss &&
                                              !b.Aborted).ToList();
                var bossWins = boss.Count(b => b.Victory);
                var floors = set.Count == 0 ? 30 : set.Max(r => r.Floors);
                var bossDefeats = defeats.Count(r => r.EndDepth >= floors);
                var before10 = defeats.Count(r => r.EndDepth < 10);
                var before20 = defeats.Count(r => r.EndDepth >= 10 && r.EndDepth < 20);
                var before30 = defeats.Count(r => r.EndDepth >= 20 && r.EndDepth < floors);
                rows.Add(new[]
                {
                    cls,
                    defeats.Count.ToString(Invariant),
                    Percent(before10, defeats.Count),
                    Percent(before20, defeats.Count),
                    Percent(before30, defeats.Count),
                    Percent(bossDefeats, defeats.Count),
                    Percent(boss.Count, set.Count),
                    Percent(bossWins, boss.Count),
                    Avg(boss.Select(b => (double)b.LevelStart)),
                });
            }

            Table(sb, new[]
            {
                "Class", "Defeats", "Deaths before depth 10", "Deaths 10-19", "Deaths 20-29", "Deaths in the boss fight",
                "Runs reaching the boss", "Boss win rate when reached", "Level entering the boss",
            }, rows);
        }

        /// <summary>
        /// Tactical depth by floor band (all classes): enemies, distinct roles, terrain, summons and how much the
        /// player had to use skills and displacement. Regular battle nodes only; the boss is its own row.
        /// </summary>
        private static void AppendFloorBands(StringBuilder sb, List<BattleRecord> battles)
        {
            sb.AppendLine("## Battles by floor band (all classes): composition, terrain and tactics");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var band in FloorBands)
            {
                var set = battles.Where(b => b.NodeType == EMapNodeType.Battle && !b.Aborted &&
                                             b.Depth >= band.From && b.Depth <= band.To).ToList();
                rows.Add(BandRow(band.Label, set));
            }

            var bossSet = battles.Where(b => b.NodeType == EMapNodeType.Boss && !b.Aborted).ToList();
            rows.Add(BandRow("Boss", bossSet));
            Table(sb, new[]
            {
                "Floors", "Battles", "Enemies", "Distinct roles", "2+ roles", "With terrain", "With hazards",
                "Obstacles", "Summoned", "Skill uses", "Displacements", "Player actions", "Damage % max HP",
            }, rows);
        }

        private static string[] BandRow(string label, List<BattleRecord> set)
        {
            return new[]
            {
                label,
                set.Count.ToString(Invariant),
                Avg(set.Select(b => (double)b.EnemyNames.Count)),
                Avg(set.Select(b => (double)b.DistinctRoles)),
                Percent(set.Count(b => b.DistinctRoles >= 2), set.Count),
                Percent(set.Count(b => b.HasTerrain), set.Count),
                Percent(set.Count(b => b.HazardCells > 0), set.Count),
                Avg(set.Select(b => (double)b.ObstacleCells)),
                Avg(set.Select(b => (double)b.Summoned), "0.00"),
                Avg(set.Select(b => (double)b.SkillsUsed)),
                Avg(set.Select(b => (double)(b.EnemiesDisplaced + b.PlayerDisplaced)), "0.00"),
                Avg(set.Select(b => (double)b.Turns)),
                Avg(set.Select(b => PercentOfMax(b.DamageTaken, b.MaxHp)), "0") + "%",
            };
        }

        private static void AppendLevelByDepth(StringBuilder sb, List<BattleRecord> battles, List<string> classes)
        {
            sb.AppendLine("## Player level when entering the battle of each depth (average)");
            sb.AppendLine();
            var maxDepth = battles.Count == 0 ? 1 : battles.Max(b => b.Depth);
            var header = new List<string> { "Depth" };
            header.AddRange(classes);
            var rows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                foreach (var cls in classes)
                {
                    var set = battles.Where(b => b.ClassName == cls && b.Depth == depth && !b.Aborted).ToList();
                    row.Add(set.Count == 0 ? "-" : $"{Avg(set.Select(b => (double)b.LevelStart))} (n={set.Count})");
                }

                rows.Add(row.ToArray());
            }

            Table(sb, header.ToArray(), rows);
        }

        private static void AppendClassDetail(StringBuilder sb, string cls, List<RunRecord> runs, List<BattleRecord> battles)
        {
            var set = battles.Where(b => b.ClassName == cls).ToList();
            var maxDepth = set.Count == 0 ? 1 : set.Max(b => b.Depth);

            sb.AppendLine($"## {cls}: battles by depth");
            sb.AppendLine();
            var rows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var atDepth = set.Where(b => b.Depth == depth && !b.Aborted).ToList();
                if (atDepth.Count == 0) continue;

                var won = atDepth.Count(b => b.Victory);
                rows.Add(new[]
                {
                    depth.ToString(Invariant),
                    atDepth.Count.ToString(Invariant),
                    Percent(won, atDepth.Count),
                    Avg(atDepth.Select(b => (double)b.Turns)),
                    Avg(atDepth.Select(b => (double)b.DamageTaken)),
                    Avg(atDepth.Select(b => PercentOfMax(b.DamageTaken, b.MaxHp)), "0") + "%",
                    Avg(atDepth.Select(b => (double)b.HpLostNet)),
                    Avg(atDepth.Select(b => (double)b.EnemyNames.Count)),
                    Avg(atDepth.Select(b => (double)b.PlannedXp)),
                    Avg(atDepth.Select(b => (double)b.GainedXp)),
                    Avg(atDepth.Select(b => (double)b.XpCredited)),
                    Avg(atDepth.Select(b => (double)b.SkillsUsed)),
                });
            }

            Table(sb, new[]
            {
                "Depth", "Battles", "Won", "Player actions", "Damage taken", "Damage % max HP", "HP lost (net)",
                "Enemies", "Planned XP", "Killed XP", "Credited XP", "Skill uses",
            }, rows);

            sb.AppendLine($"### {cls}: damage taken as % of max HP by depth and difficulty (average; n)");
            sb.AppendLine();
            var diffRows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                var any = false;
                foreach (var difficulty in Difficulties)
                {
                    var cell = set.Where(b => b.Depth == depth && b.Difficulty == difficulty && !b.Aborted &&
                                              b.NodeType == EMapNodeType.Battle).ToList();
                    any |= cell.Count > 0;
                    row.Add(cell.Count == 0
                        ? "-"
                        : $"{Avg(cell.Select(b => PercentOfMax(b.DamageTaken, b.MaxHp)), "0")}% (n={cell.Count})");
                }

                var boss = set.Where(b => b.Depth == depth && b.NodeType == EMapNodeType.Boss && !b.Aborted).ToList();
                any |= boss.Count > 0;
                row.Add(boss.Count == 0 ? "-" : $"{Avg(boss.Select(b => PercentOfMax(b.DamageTaken, b.MaxHp)), "0")}% (n={boss.Count})");
                if (any)
                    diffRows.Add(row.ToArray());
            }

            Table(sb, new[] { "Depth", "Easy", "Normal", "Hard", "Boss" }, diffRows);

            var turnRows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                var any = false;
                foreach (var difficulty in Difficulties)
                {
                    var cell = set.Where(b => b.Depth == depth && b.Difficulty == difficulty && !b.Aborted &&
                                              b.NodeType == EMapNodeType.Battle).ToList();
                    any |= cell.Count > 0;
                    row.Add(cell.Count == 0 ? "-" : Avg(cell.Select(b => (double)b.Turns)));
                }

                if (any)
                    turnRows.Add(row.ToArray());
            }

            sb.AppendLine($"### {cls}: player actions per battle by depth and difficulty (average)");
            sb.AppendLine();
            Table(sb, new[] { "Depth", "Easy", "Normal", "Hard" }, turnRows);
        }

        private static void AppendXpAnalysis(StringBuilder sb, List<RunRecord> runs, List<BattleRecord> battles)
        {
            sb.AppendLine("## XP per battle node");
            sb.AppendLine();

            // One map per seed (the classes play the same maps).
            var maps = runs.GroupBy(r => r.Seed).Select(g => g.First()).ToList();
            var nodes = maps.SelectMany(m => m.MapBattles.Select(n => (Map: m, Node: n)))
                .Where(x => x.Node.Type == EMapNodeType.Battle).ToList();
            var maxDepth = nodes.Count == 0 ? 1 : nodes.Max(x => x.Node.Depth);

            sb.AppendLine($"### Planned XP (generated maps, {maps.Count} maps): min / avg / max by depth and difficulty");
            sb.AppendLine();
            var rows = new List<string[]>();
            for (var depth = 1; depth <= maxDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                var any = false;
                foreach (var difficulty in Difficulties)
                {
                    var cell = nodes.Where(x => x.Node.Depth == depth && x.Node.Difficulty == difficulty)
                        .Select(x => (double)x.Node.TotalXp).ToList();
                    any |= cell.Count > 0;
                    row.Add(MinAvgMax(cell));
                }

                if (any)
                    rows.Add(row.ToArray());
            }

            Table(sb, new[] { "Depth", "Easy", "Normal", "Hard" }, rows);

            sb.AppendLine("### Does a lower difficulty give more XP than a higher one on the same floor?");
            sb.AppendLine();
            sb.AppendLine("Every pair of battle nodes of the same floor of the same map is compared (Easy vs Normal, Normal vs Hard, " +
                          "Easy vs Hard). \"Inverted\" means the lower difficulty gave MORE XP; \"equal\" the same.");
            sb.AppendLine();
            var pairRows = new List<string[]>();
            var comparisons = new[]
            {
                (EBattleDifficulty.Easy, EBattleDifficulty.Normal),
                (EBattleDifficulty.Normal, EBattleDifficulty.Hard),
                (EBattleDifficulty.Easy, EBattleDifficulty.Hard),
            };
            foreach (var (lower, higher) in comparisons)
            {
                var pairs = 0;
                var inverted = 0;
                var equal = 0;
                var floors = 0;
                var floorsInverted = 0;
                foreach (var map in maps)
                {
                    for (var depth = 1; depth <= maxDepth; depth++)
                    {
                        var low = map.MapBattles.Where(n => n.Type == EMapNodeType.Battle && n.Depth == depth && n.Difficulty == lower).ToList();
                        var high = map.MapBattles.Where(n => n.Type == EMapNodeType.Battle && n.Depth == depth && n.Difficulty == higher).ToList();
                        if (low.Count == 0 || high.Count == 0) continue;

                        floors++;
                        var floorInverted = false;
                        foreach (var a in low)
                        {
                            foreach (var b in high)
                            {
                                pairs++;
                                if (a.TotalXp > b.TotalXp)
                                {
                                    inverted++;
                                    floorInverted = true;
                                }
                                else if (a.TotalXp == b.TotalXp)
                                {
                                    equal++;
                                }
                            }
                        }

                        if (floorInverted) floorsInverted++;
                    }
                }

                pairRows.Add(new[]
                {
                    $"{lower} vs {higher}",
                    pairs.ToString(Invariant),
                    $"{inverted} ({Percent(inverted, pairs)})",
                    $"{equal} ({Percent(equal, pairs)})",
                    floors.ToString(Invariant),
                    $"{floorsInverted} ({Percent(floorsInverted, floors)})",
                });
            }

            Table(sb, new[] { "Comparison", "Pairs", "Inverted pairs", "Equal pairs", "Floors with both", "Floors with an inversion" }, pairRows);

            sb.AppendLine("### XP gained in the battles played: min / avg / max by depth and difficulty");
            sb.AppendLine();
            var playedRows = new List<string[]>();
            var played = battles.Where(b => !b.Aborted && b.NodeType == EMapNodeType.Battle).ToList();
            var playedDepth = played.Count == 0 ? 1 : played.Max(b => b.Depth);
            for (var depth = 1; depth <= playedDepth; depth++)
            {
                var row = new List<string> { depth.ToString(Invariant) };
                var any = false;
                foreach (var difficulty in Difficulties)
                {
                    var cell = played.Where(b => b.Depth == depth && b.Difficulty == difficulty && b.Victory)
                        .Select(b => (double)b.GainedXp).ToList();
                    any |= cell.Count > 0;
                    row.Add(MinAvgMax(cell));
                }

                if (any)
                    playedRows.Add(row.ToArray());
            }

            Table(sb, new[] { "Depth", "Easy", "Normal", "Hard" }, playedRows);
        }

        private static void AppendEnemies(StringBuilder sb, List<BattleRecord> battles, List<RunRecord> runs,
            Dictionary<string, string> roles, List<string> classes)
        {
            var total = battles.Sum(b => (double)b.DamageTaken);
            var names = battles.SelectMany(b => b.EnemyNames).Concat(battles.SelectMany(b => b.Kills.Keys))
                .Concat(battles.SelectMany(b => b.DamageByEnemy.Keys)).Distinct()
                .Where(n => roles.ContainsKey(n)).OrderBy(n => n).ToList();

            sb.AppendLine("## Enemies (all classes): damage taken, kills and deaths caused");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var name in names)
            {
                var appearances = battles.Count(b => b.EnemyNames.Contains(name));
                var spawned = battles.Sum(b => b.EnemyNames.Count(n => n == name));
                var damage = battles.Sum(b => b.DamageByEnemy.TryGetValue(name, out var d) ? d : 0);
                var kills = battles.Sum(b => b.Kills.TryGetValue(name, out var k) ? k : 0);
                var deaths = runs.Count(r => r.Result == ERunResult.Defeat && r.KilledBy == name);
                rows.Add(new[]
                {
                    name,
                    roles[name],
                    appearances.ToString(Invariant),
                    spawned.ToString(Invariant),
                    kills.ToString(Invariant),
                    damage.ToString(Invariant),
                    appearances == 0 ? "-" : (damage / (double)appearances).ToString("0.0", Invariant),
                    spawned == 0 ? "-" : (damage / (double)spawned).ToString("0.0", Invariant),
                    total <= 0 ? "-" : (damage * 100.0 / total).ToString("0.0", Invariant) + "%",
                    deaths.ToString(Invariant),
                });
            }

            Table(sb, new[]
            {
                "Enemy", "Role", "Battles in", "Spawned", "Killed", "HP damage to player", "Per battle in", "Per enemy",
                "Share of damage", "Run-ending hits",
            }, rows);

            sb.AppendLine("### Damage taken by role and by kind of damage");
            sb.AppendLine();
            var byRole = new SortedDictionary<string, long>();
            foreach (var battle in battles)
            {
                foreach (var pair in battle.DamageByEnemy)
                {
                    var role = roles.TryGetValue(pair.Key, out var r) ? r : pair.Key;
                    byRole.TryGetValue(role, out var current);
                    byRole[role] = current + pair.Value;
                }
            }

            Table(sb, new[] { "Role / source", "HP damage", "Share" },
                byRole.Select(p => new[] { p.Key, p.Value.ToString(Invariant), total <= 0 ? "-" : (p.Value * 100.0 / total).ToString("0.0", Invariant) + "%" }));

            var byKind = new SortedDictionary<string, long>();
            foreach (var battle in battles)
            {
                foreach (var pair in battle.DamageByKind)
                {
                    byKind.TryGetValue(pair.Key, out var current);
                    byKind[pair.Key] = current + pair.Value;
                }
            }

            Table(sb, new[] { "Kind", "HP damage", "Share" },
                byKind.Select(p => new[] { p.Key, p.Value.ToString(Invariant), total <= 0 ? "-" : (p.Value * 100.0 / total).ToString("0.0", Invariant) + "%" }));

            sb.AppendLine("### Who ended the runs (lost runs, by class)");
            sb.AppendLine();
            var killers = runs.Where(r => r.Result == ERunResult.Defeat).Select(r => r.KilledBy).Distinct().OrderBy(k => k).ToList();
            var killerRows = new List<string[]>();
            foreach (var killer in killers)
            {
                var row = new List<string> { string.IsNullOrEmpty(killer) ? "?" : killer };
                foreach (var cls in classes)
                    row.Add(runs.Count(r => r.ClassName == cls && r.Result == ERunResult.Defeat && r.KilledBy == killer).ToString(Invariant));
                killerRows.Add(row.ToArray());
            }

            var killerHeader = new List<string> { "Last hit by" };
            killerHeader.AddRange(classes);
            Table(sb, killerHeader.ToArray(), killerRows);
        }

        private static void AppendTalents(StringBuilder sb, List<RunRecord> runs, List<string> classes)
        {
            sb.AppendLine("## Talents: offered, taken and pick rate");
            sb.AppendLine();
            foreach (var cls in classes)
            {
                var set = runs.Where(r => r.ClassName == cls).ToList();
                var offered = new Dictionary<string, int>();
                var taken = new Dictionary<string, int>();
                foreach (var run in set)
                {
                    foreach (var pair in run.TalentsOffered) offered[pair.Key] = offered.GetValueOrDefault(pair.Key) + pair.Value;
                    foreach (var pair in run.TalentsTaken) taken[pair.Key] = taken.GetValueOrDefault(pair.Key) + pair.Value;
                }

                sb.AppendLine($"### {cls}: {Avg(set.Select(r => (double)r.TalentsPicked))} talents per run, " +
                              $"{Avg(set.Select(r => (double)r.DistinctTalents))} distinct, " +
                              $"{offered.Count} distinct talents offered, {taken.Count} taken at least once");
                sb.AppendLine();
                var classPicks = set.Sum(r => r.ClassTalentPicks);
                var sharedPicks = set.Sum(r => r.SharedTalentPicks);
                sb.AppendLine($"Class talents: {Avg(set.Select(r => (double)r.ClassTalentPicks))} picks per run " +
                              $"({Percent(classPicks, classPicks + sharedPicks)} of all picks); " +
                              $"generic (shared pool only): {Avg(set.Select(r => (double)r.SharedTalentPicks))} per run.");
                sb.AppendLine();
                var rows = offered.Keys.Concat(taken.Keys).Distinct()
                    .OrderByDescending(n => taken.GetValueOrDefault(n)).ThenBy(n => n)
                    .Select(n => new[]
                    {
                        n,
                        offered.GetValueOrDefault(n).ToString(Invariant),
                        taken.GetValueOrDefault(n).ToString(Invariant),
                        offered.GetValueOrDefault(n) == 0 ? "-" : Percent(taken.GetValueOrDefault(n), offered.GetValueOrDefault(n)),
                        set.Count == 0 ? "-" : (taken.GetValueOrDefault(n) / (double)set.Count).ToString("0.00", Invariant),
                    });
                Table(sb, new[] { "Talent", "Offered", "Taken", "Pick rate", "Per run" }, rows);
            }
        }

        private static void AppendUsage(StringBuilder sb, List<BattleRecord> battles, List<RunRecord> runs, List<string> classes)
        {
            sb.AppendLine("## Skills, items and terrain");
            sb.AppendLine();
            foreach (var cls in classes)
            {
                var set = battles.Where(b => b.ClassName == cls).ToList();
                var runCount = Math.Max(1, runs.Count(r => r.ClassName == cls));
                var skills = new Dictionary<string, int>();
                foreach (var battle in set)
                    foreach (var pair in battle.SkillUses) skills[pair.Key] = skills.GetValueOrDefault(pair.Key) + pair.Value;

                sb.AppendLine($"### {cls}: skill uses");
                sb.AppendLine();
                Table(sb, new[] { "Skill", "Uses", "Per run", "Per battle" },
                    skills.OrderByDescending(p => p.Value).Select(p => new[]
                    {
                        p.Key,
                        p.Value.ToString(Invariant),
                        (p.Value / (double)runCount).ToString("0.0", Invariant),
                        set.Count == 0 ? "-" : (p.Value / (double)set.Count).ToString("0.00", Invariant),
                    }));
            }

            var items = new Dictionary<string, int>();
            var terrain = new Dictionary<string, int>();
            var terrainDamage = 0;
            foreach (var battle in battles)
            {
                foreach (var pair in battle.ItemUses) items[pair.Key] = items.GetValueOrDefault(pair.Key) + pair.Value;
                foreach (var pair in battle.TerrainBy) terrain[pair.Key] = terrain.GetValueOrDefault(pair.Key) + pair.Value;
                terrainDamage += battle.TerrainDamage;
            }

            sb.AppendLine("### Consumables used (all classes)");
            sb.AppendLine();
            Table(sb, new[] { "Item", "Uses", "Per run" },
                items.OrderByDescending(p => p.Value).Select(p => new[]
                {
                    p.Key, p.Value.ToString(Invariant), (p.Value / (double)Math.Max(1, runs.Count)).ToString("0.00", Invariant),
                }));

            sb.AppendLine($"### Terrain triggers (all classes; damage dealt to the player by terrain: {terrainDamage})");
            sb.AppendLine();
            Table(sb, new[] { "Terrain / who", "Triggers", "Per battle" },
                terrain.OrderBy(p => p.Key).Select(p => new[]
                {
                    p.Key, p.Value.ToString(Invariant), battles.Count == 0 ? "-" : (p.Value / (double)battles.Count).ToString("0.00", Invariant),
                }));
        }

        /// <summary>Pushes and pulls (G4): per class, how often characters were moved, collided and hit terrain at once.</summary>
        private static void AppendDisplacement(StringBuilder sb, List<BattleRecord> battles, List<RunRecord> runs,
            List<string> classes)
        {
            sb.AppendLine("## Pushing and pulling (displacement)");
            sb.AppendLine();
            var rows = new List<string[]>();
            foreach (var cls in classes)
            {
                var set = battles.Where(b => b.ClassName == cls).ToList();
                var runCount = Math.Max(1, runs.Count(r => r.ClassName == cls));
                rows.Add(new[]
                {
                    cls,
                    PerRun(set.Sum(b => b.EnemiesDisplaced), runCount),
                    PerRun(set.Sum(b => b.PlayerDisplaced), runCount),
                    PerRun(set.Sum(b => b.Collisions), runCount),
                    PerRun(set.Sum(b => b.CollisionDamageToEnemies), runCount),
                    PerRun(set.Sum(b => b.CollisionDamageToPlayer), runCount),
                    PerRun(set.Sum(b => b.CollisionKills), runCount),
                    PerRun(set.Sum(b => b.ForcedTerrainEnemies), runCount),
                    PerRun(set.Sum(b => b.ForcedTerrainPlayer), runCount),
                });
            }

            Table(sb, new[]
            {
                "Class", "Enemies displaced", "Player displaced", "Collisions", "Collision dmg to enemies",
                "Collision dmg to player", "Collision kills", "Forced terrain (enemies)", "Forced terrain (player)",
            }, rows);
            sb.AppendLine("(all numbers per run)");
            sb.AppendLine();
        }

        private static string PerRun(int total, int runs) => (total / (double)runs).ToString("0.00", Invariant);

        private static void AppendAborted(StringBuilder sb, List<RunRecord> runs, List<BattleRecord> battles)
        {
            var aborted = runs.Where(r => r.Result == ERunResult.Aborted).ToList();
            sb.AppendLine($"## Aborted runs ({aborted.Count})");
            sb.AppendLine();
            if (aborted.Count == 0)
            {
                sb.AppendLine("None.");
                sb.AppendLine();
                return;
            }

            var rows = aborted.Take(40).Select(r =>
            {
                var battle = battles.LastOrDefault(b => b.ClassName == r.ClassName && b.RunIndex == r.RunIndex && b.Aborted);
                return new[]
                {
                    r.ClassName,
                    r.RunIndex.ToString(Invariant),
                    r.Seed.ToString(Invariant),
                    r.AbortReason,
                    r.EndDepth.ToString(Invariant),
                    battle != null ? battle.EnemySummary : "-",
                    battle != null ? battle.GlobalTurns.ToString(Invariant) : "-",
                };
            });
            Table(sb, new[] { "Class", "Run", "Seed", "Reason", "Depth", "Enemies", "Global turn" }, rows);
        }

        // ------------------------------------------------------------------------------------ csv

        private static string BuildRunsCsv(SimulationBatchResult batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine("batch,class,run,seed,map_policy,talent_policy,result,abort_reason,end_depth,floors,final_level,battles_won," +
                          "battles_played,player_actions,damage_dealt,damage_taken,talents_picked,distinct_talents,skill_uses,item_uses," +
                          "terrain_triggers,heal_nodes,talent_nodes,consumable_nodes,rerolls,bans,skips,killed_by,ms,frames,class_talent_picks,shared_talent_picks");
            foreach (var r in batch.Runs)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(batch.Options.Label), Csv(r.ClassName), r.RunIndex.ToString(Invariant), r.Seed.ToString(Invariant),
                    batch.Options.Map.Policy.ToString(), batch.Options.Talents.Policy.ToString(), r.Result.ToString(),
                    Csv(r.AbortReason), r.EndDepth.ToString(Invariant), r.Floors.ToString(Invariant), r.FinalLevel.ToString(Invariant),
                    r.BattlesWon.ToString(Invariant), r.BattlesPlayed.ToString(Invariant), r.Turns.ToString(Invariant),
                    r.DamageDealt.ToString(Invariant), r.DamageTaken.ToString(Invariant), r.TalentsPicked.ToString(Invariant),
                    r.DistinctTalents.ToString(Invariant), r.SkillsUsed.ToString(Invariant), r.ConsumablesUsed.ToString(Invariant),
                    r.TerrainTriggers.ToString(Invariant), r.HealNodes.ToString(Invariant), r.TalentNodes.ToString(Invariant),
                    r.ConsumableNodes.ToString(Invariant), r.Rerolls.ToString(Invariant), r.Bans.ToString(Invariant),
                    r.Skips.ToString(Invariant), Csv(r.KilledBy), r.Milliseconds.ToString("0", Invariant), r.Frames.ToString(Invariant),
                    r.ClassTalentPicks.ToString(Invariant), r.SharedTalentPicks.ToString(Invariant),
                }));
            }

            return sb.ToString();
        }

        private static string BuildBattlesCsv(SimulationBatchResult batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine("batch,class,run,seed,node,depth,type,difficulty,enemies,enemy_count,planned_xp,gained_xp,victory,aborted," +
                          "abort_reason,player_actions,global_turns,hp_start,hp_end,max_hp,hp_lost_net,damage_taken,healing,damage_dealt," +
                          "level_start,level_end,skill_uses,item_uses,terrain_triggers,terrain_damage,distinct_roles,obstacles,hazards,bonus,summoned,ms");
            foreach (var b in batch.Battles)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(batch.Options.Label), Csv(b.ClassName), b.RunIndex.ToString(Invariant), b.Seed.ToString(Invariant),
                    b.NodeId.ToString(Invariant), b.Depth.ToString(Invariant), b.NodeType.ToString(), b.Difficulty.ToString(),
                    Csv(b.EnemySummary), b.EnemyNames.Count.ToString(Invariant), b.PlannedXp.ToString(Invariant),
                    b.GainedXp.ToString(Invariant), b.Victory ? "1" : "0", b.Aborted ? "1" : "0", Csv(b.AbortReason),
                    b.Turns.ToString(Invariant), b.GlobalTurns.ToString(Invariant), b.HpStart.ToString(Invariant),
                    b.HpEnd.ToString(Invariant), b.MaxHp.ToString(Invariant), b.HpLostNet.ToString(Invariant),
                    b.DamageTaken.ToString(Invariant), b.HealingReceived.ToString(Invariant), b.DamageDealt.ToString(Invariant),
                    b.LevelStart.ToString(Invariant), b.LevelEnd.ToString(Invariant), b.SkillsUsed.ToString(Invariant),
                    b.ConsumablesUsed.ToString(Invariant), b.TerrainTriggers.ToString(Invariant), b.TerrainDamage.ToString(Invariant),
                    b.DistinctRoles.ToString(Invariant), b.ObstacleCells.ToString(Invariant), b.HazardCells.ToString(Invariant),
                    b.BonusCells.ToString(Invariant), b.Summoned.ToString(Invariant),
                    b.Milliseconds.ToString("0", Invariant),
                }));
            }

            return sb.ToString();
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        // ------------------------------------------------------------------------------------ helpers

        private static Dictionary<string, string> BuildRoleMap()
        {
            var map = new Dictionary<string, string>();
            var database = GameDatabase.Instance;
            if (database == null) return map;

            foreach (var enemy in database.GetAll<EnemyConfig>())
                map[enemy.name] = enemy.Role != null ? enemy.Role.name : "-";
            return map;
        }

        private static void Table(StringBuilder sb, string[] header, IEnumerable<string[]> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0)
            {
                sb.AppendLine("(no data)");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| " + string.Join(" | ", header) + " |");
            sb.AppendLine("|" + string.Concat(header.Select(_ => " --- |")));
            foreach (var row in list)
                sb.AppendLine("| " + string.Join(" | ", row) + " |");
            sb.AppendLine();
        }

        private static string Avg(IEnumerable<double> values, string format = "0.0")
        {
            var list = values.ToList();
            return list.Count == 0 ? "-" : list.Average().ToString(format, Invariant);
        }

        private static string MinAvgMax(List<double> values)
        {
            if (values.Count == 0) return "-";

            return $"{values.Min().ToString("0", Invariant)} / {values.Average().ToString("0.0", Invariant)} / " +
                   $"{values.Max().ToString("0", Invariant)} (n={values.Count})";
        }

        private static string Percent(int part, int whole) =>
            whole == 0 ? "-" : (part * 100.0 / whole).ToString("0.0", Invariant) + "%";

        private static double PercentOfMax(int value, int max) => max <= 0 ? 0 : value * 100.0 / max;

        /// <summary>95% Wilson score interval of a proportion, as "low-high%".</summary>
        private static string WilsonInterval(int successes, int total)
        {
            if (total == 0) return "-";

            const double z = 1.96;
            var p = successes / (double)total;
            var denominator = 1 + z * z / total;
            var center = (p + z * z / (2 * total)) / denominator;
            var margin = z * Math.Sqrt(p * (1 - p) / total + z * z / (4.0 * total * total)) / denominator;
            return $"{Math.Max(0, center - margin) * 100:0}-{Math.Min(1, center + margin) * 100:0}%";
        }
    }
}
