using System;
using System.Collections.Generic;
using System.Text;
using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Progression;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Talents
{
    /// <summary>
    /// Edit-mode simulation of the XP economy (xp_e_niveis.md): generates maps with the real generators and
    /// follows paths that always prefer Easy, Normal or Hard battles, to see how many levels each choice
    /// reaches with the XP of the battles and the XP curve of <see cref="ProgressionSettings"/>. It is how the XP
    /// defaults of <see cref="BattleGenerationSettings"/> were tuned; run it again after changing the curve, the
    /// XP settings, the map length or the battle generator.
    /// </summary>
    public static class XpProgressionSimulator
    {
        /// <summary>The difficulty a simulated player prefers when the floor offers a choice.</summary>
        public enum EPathPolicy
        {
            Easy,
            Normal,
            Hard
        }

        /// <summary>Result of one policy over many maps.</summary>
        public sealed class PolicyResult
        {
            public EPathPolicy Policy;
            public int Maps;
            public double MeanBattles;
            public double MeanXpBeforeBoss;
            public double MeanLevelBeforeBoss;

            /// <summary>
            /// Level before the boss if every battle of the same paths had the preferred difficulty (the XP of the
            /// battle is replaced by the mean XP of that difficulty at the same depth): what a map that always
            /// offered the choice would give.
            /// </summary>
            public double MeanIdealLevelBeforeBoss;

            public double MeanLevelAfterBoss;
            public int MinLevelAfterBoss = int.MaxValue;
            public int MaxLevelAfterBoss;
            public double PreferredShare;
        }

        /// <summary>Mean XP of the battles of one depth and difficulty.</summary>
        public sealed class DepthXp
        {
            public int Depth;
            public readonly double[] Mean = new double[3];
            public readonly int[] Samples = new int[3];
        }

        [MenuItem("GridBattle/Talents/Simulate XP Progression")]
        private static void RunFromMenu()
        {
            Debug.Log(Run(300));
        }

        /// <summary>Runs the full report with the registered settings (or with another XP per depth).</summary>
        /// <param name="maps">Maps generated (seeds 1..maps).</param>
        /// <param name="xpPerDepthOverride">A value to try instead of the asset's; negative = use the asset.</param>
        public static string Run(int maps, float xpPerDepthOverride = -1f)
        {
            var progression = ProgressionSettings.Current;
            var mapSettings = GameSettings.Get<MapGenerationSettings>();
            var battleSettings = GameSettings.Get<BattleGenerationSettings>();
            var terrainSettings = GameSettings.Get<TerrainGenerationSettings>();

            var ownsClone = false;
            if (xpPerDepthOverride >= 0f)
            {
                battleSettings = UnityEngine.Object.Instantiate(battleSettings);
                SetXpPerDepth(battleSettings, xpPerDepthOverride);
                ownsClone = true;
            }

            try
            {
                var generated = new List<MapState>(maps);
                for (var seed = 1; seed <= maps; seed++)
                {
                    var random = new RunRandom((ulong)seed * 7919UL + 17UL);
                    generated.Add(MapGenerator.Generate(random, mapSettings, battleSettings, terrainSettings));
                }

                var builder = new StringBuilder();
                builder.AppendLine(
                    $"XP progression simulation: {maps} maps, {mapSettings.FloorCount} floors, xpPerDepth = {battleSettings.XpPerDepth:0.###}, " +
                    $"curve {progression.BaseXpToLevelUp} + {progression.XpToLevelUpGrowthPerLevel}/level, max level {progression.MaxLevel}");

                AppendDepthTable(builder, generated, progression);
                foreach (EPathPolicy policy in Enum.GetValues(typeof(EPathPolicy)))
                {
                    var result = SimulatePolicy(generated, policy, progression);
                    builder.AppendLine(
                        $"all-{policy}: battles {result.MeanBattles:0.0}, XP before boss {result.MeanXpBeforeBoss:0}, " +
                        $"level before boss {result.MeanLevelBeforeBoss:0.0} ({(result.MeanLevelBeforeBoss - 1) / Math.Max(1.0, result.MeanBattles):0.00} levels per battle), final level {result.MeanLevelAfterBoss:0.0} " +
                        $"(min {result.MinLevelAfterBoss}, max {result.MaxLevelAfterBoss}), battles at the preferred difficulty {result.PreferredShare:P0}, " +
                        $"level before boss if every battle had it {result.MeanIdealLevelBeforeBoss:0.0}");
                }

                return builder.ToString();
            }
            finally
            {
                if (ownsClone)
                    UnityEngine.Object.DestroyImmediate(battleSettings);
            }
        }

        /// <summary>Mean XP of the generated battles by depth and difficulty (boss excluded).</summary>
        public static List<DepthXp> MeasureDepthXp(List<MapState> maps)
        {
            var sums = new Dictionary<int, double[]>();
            var counts = new Dictionary<int, int[]>();
            foreach (var map in maps)
            {
                foreach (var node in map.Nodes)
                {
                    if (node.Type != EMapNodeType.Battle || node.Battle == null) continue;

                    if (!sums.ContainsKey(node.Depth))
                    {
                        sums[node.Depth] = new double[3];
                        counts[node.Depth] = new int[3];
                    }

                    sums[node.Depth][(int)node.Difficulty] += node.Battle.TotalXp;
                    counts[node.Depth][(int)node.Difficulty]++;
                }
            }

            var result = new List<DepthXp>();
            var depths = new List<int>(sums.Keys);
            depths.Sort();
            foreach (var depth in depths)
            {
                var row = new DepthXp { Depth = depth };
                for (var d = 0; d < 3; d++)
                {
                    row.Samples[d] = counts[depth][d];
                    row.Mean[d] = counts[depth][d] > 0 ? sums[depth][d] / counts[depth][d] : 0.0;
                }

                result.Add(row);
            }

            return result;
        }

        private static void AppendDepthTable(StringBuilder builder, List<MapState> maps, ProgressionSettings progression)
        {
            builder.AppendLine("depth | XP to level (if level = depth) | mean XP Easy / Normal / Hard | Hard / XP to level");
            foreach (var row in MeasureDepthXp(maps))
            {
                var need = progression.GetXpToNextLevel(row.Depth);
                builder.AppendLine(
                    $"{row.Depth,5} | {need,27} | {row.Mean[0],6:0} / {row.Mean[1],6:0} / {row.Mean[2],6:0} | {row.Mean[2] / need,6:0.00}");
            }
        }

        /// <summary>
        /// Follows a path through every map that always takes the battle of the preferred difficulty (then the
        /// next closest one) and adds up the XP; non-battle nodes grant nothing. Levels follow the curve and the cap.
        /// </summary>
        public static PolicyResult SimulatePolicy(List<MapState> maps, EPathPolicy policy, ProgressionSettings progression)
        {
            var result = new PolicyResult { Policy = policy, Maps = maps.Count };
            double battlesTotal = 0, xpTotal = 0, levelBefore = 0, levelAfter = 0, preferred = 0, battlesWithChoice = 0;
            double idealLevel = 0;
            var meanByDepth = new Dictionary<int, DepthXp>();
            foreach (var row in MeasureDepthXp(maps))
                meanByDepth[row.Depth] = row;

            foreach (var map in maps)
            {
                var xp = 0;
                var idealXp = 0.0;
                var battles = 0;
                var current = (MapNodeState)null;
                var candidates = MapRules.GetAvailableNodes(new MapState { Nodes = map.Nodes, CurrentNodeId = -1 });

                while (candidates.Count > 0)
                {
                    var next = Choose(candidates, policy);
                    if (next.Type == EMapNodeType.Boss)
                    {
                        current = next;
                        break;
                    }

                    if (next.Type == EMapNodeType.Battle && next.Battle != null)
                    {
                        xp += next.Battle.TotalXp;
                        if (meanByDepth.TryGetValue(next.Depth, out var depthRow))
                            idealXp += depthRow.Mean[(int)PreferredDifficulty(policy)];
                        battles++;
                        battlesWithChoice++;
                        if (next.Difficulty == PreferredDifficulty(policy)) preferred++;
                    }

                    candidates = new List<MapNodeState>();
                    foreach (var id in next.Next)
                        candidates.Add(map.Nodes[id]);
                }

                var xpAfterBoss = xp + (current != null && current.Battle != null ? current.Battle.TotalXp : 0);
                var before = GetLevel(xp, progression);
                var after = GetLevel(xpAfterBoss, progression);

                battlesTotal += battles;
                xpTotal += xp;
                levelBefore += before;
                levelAfter += after;
                idealLevel += GetLevel((int)Math.Round(idealXp), progression);
                result.MinLevelAfterBoss = Math.Min(result.MinLevelAfterBoss, after);
                result.MaxLevelAfterBoss = Math.Max(result.MaxLevelAfterBoss, after);
            }

            result.MeanBattles = battlesTotal / maps.Count;
            result.MeanXpBeforeBoss = xpTotal / maps.Count;
            result.MeanLevelBeforeBoss = levelBefore / maps.Count;
            result.MeanIdealLevelBeforeBoss = idealLevel / maps.Count;
            result.MeanLevelAfterBoss = levelAfter / maps.Count;
            result.PreferredShare = battlesWithChoice > 0 ? preferred / battlesWithChoice : 0;
            return result;
        }

        /// <summary>Level reached with <paramref name="totalXp"/> from level 1 (leftover XP carries over, capped at the max level).</summary>
        public static int GetLevel(int totalXp, ProgressionSettings progression)
        {
            var level = 1;
            var remaining = totalXp;
            while (level < progression.MaxLevel)
            {
                var need = progression.GetXpToNextLevel(level);
                if (remaining < need) break;
                remaining = progression.CarryOverLeftoverXp ? remaining - need : 0;
                level++;
            }

            return level;
        }

        private static EBattleDifficulty PreferredDifficulty(EPathPolicy policy) => policy switch
        {
            EPathPolicy.Easy => EBattleDifficulty.Easy,
            EPathPolicy.Hard => EBattleDifficulty.Hard,
            _ => EBattleDifficulty.Normal,
        };

        /// <summary>The battle node closest to the preferred difficulty (ties: first), else any node.</summary>
        private static MapNodeState Choose(List<MapNodeState> candidates, EPathPolicy policy)
        {
            var preferred = (int)PreferredDifficulty(policy);
            MapNodeState best = null;
            var bestScore = int.MaxValue;
            foreach (var node in candidates)
            {
                if (node.Type == EMapNodeType.Boss) return node;

                var score = node.Type == EMapNodeType.Battle
                    ? Math.Abs((int)node.Difficulty - preferred)
                    : 100;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            return best;
        }

        private static void SetXpPerDepth(BattleGenerationSettings settings, float value)
        {
            var field = typeof(BattleGenerationSettings).GetField("xpPerDepth",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(settings, value);
        }
    }
}
