using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.Map
{
    /// <summary>
    /// Generates the map of a run (GDD Mechanic 2, mapa_e_nos.md): a finite graph of nodes in floors, as in Slay
    /// the Spire. The first floors hold battles, the last floor holds only the final boss, every node is
    /// reachable from the first floor and leads to the boss, and paths never cross. Every battle node gets its
    /// <see cref="BattleSpec"/> (enemies, terrain, XP) at generation time, so the preview and the battle are
    /// the same. Pure and deterministic: everything comes from the <see cref="RunRandom"/> (derived streams keyed
    /// by node id, so the result never depends on how many other draws the run has made).
    /// </summary>
    public static class MapGenerator
    {
        // Sub-streams of the Map stream: a change in the number of draws of one step never moves the others.
        private const long ShapeKey = 0;
        private const long TypeKey = 1;
        private const long DifficultyKey = 2;

        /// <summary>Generates the whole map of a run, battles included.</summary>
        public static MapState Generate(RunRandom random, MapGenerationSettings map, BattleGenerationSettings battle,
            TerrainGenerationSettings terrain)
        {
            var state = new MapState { FloorCount = map.FloorCount, Lanes = map.Lanes };
            var floors = BuildShape(random.Derive(ERandomStream.Map, ShapeKey), map, state);
            AssignTypes(random.Derive(ERandomStream.Map, TypeKey), map, state, floors);
            AssignDifficulties(random.Derive(ERandomStream.Map, DifficultyKey), map, state);
            GenerateBattles(random, map, battle, terrain, state);
            return state;
        }

        // ------------------------------------------------------------------- shape

        /// <summary>
        /// Creates the nodes (ids by floor, then column) and their edges. Returns the node ids of every floor.
        /// </summary>
        private static List<List<int>> BuildShape(Rng rng, MapGenerationSettings map, MapState state)
        {
            var floorCount = map.FloorCount;
            var maxEdges = map.MaxEdgesPerNode;
            var floors = new List<List<int>>(floorCount);

            var previousCount = 0;
            for (var floor = 0; floor < floorCount; floor++)
            {
                int[] columns;
                if (floor == floorCount - 1)
                {
                    columns = new[] { (map.Lanes - 1) / 2 };
                }
                else
                {
                    var count = rng.Range(map.MinNodesPerFloor, map.MaxNodesPerFloor + 1);

                    // A floor can only be as wide as the paths of the floor before it can reach.
                    if (floor > 0)
                        count = Mathf.Min(count, previousCount * maxEdges);
                    columns = DrawColumns(rng, map.Lanes, count);
                }

                var ids = new List<int>(columns.Length);
                foreach (var column in columns)
                {
                    var node = new MapNodeState
                    {
                        Id = state.Nodes.Count,
                        Floor = floor,
                        Column = column,
                        Type = floor == floorCount - 1 ? EMapNodeType.Boss : EMapNodeType.Battle,
                        Difficulty = EBattleDifficulty.Normal,
                    };
                    state.Nodes.Add(node);
                    ids.Add(node.Id);
                }

                floors.Add(ids);
                previousCount = columns.Length;
            }

            for (var floor = 0; floor + 1 < floorCount; floor++)
                ConnectFloors(rng, state, floors[floor], floors[floor + 1], maxEdges, map.MergeChance);

            return floors;
        }

        /// <summary><paramref name="count"/> distinct lanes, in increasing order.</summary>
        private static int[] DrawColumns(Rng rng, int lanes, int count)
        {
            count = Mathf.Clamp(count, 1, lanes);
            var all = new List<int>(lanes);
            for (var i = 0; i < lanes; i++)
                all.Add(i);

            rng.Shuffle(all);
            var chosen = all.GetRange(0, count);
            chosen.Sort();
            return chosen.ToArray();
        }

        /// <summary>
        /// Connects two floors without crossings: every node of the upper floor gets an interval of children
        /// (at most <paramref name="maxEdges"/>), and consecutive intervals share or follow each other, so every
        /// lower node has a parent and the edges form a monotone staircase over the sorted columns.
        /// </summary>
        private static void ConnectFloors(Rng rng, MapState state, List<int> upper, List<int> lower, int maxEdges,
            float mergeChance)
        {
            var parents = upper.Count;
            var children = lower.Count;
            var low = 0;
            for (var i = 0; i < parents; i++)
            {
                var remainingAfter = parents - 1 - i;
                int high;
                if (i == parents - 1)
                {
                    high = children - 1;
                }
                else
                {
                    // High end: the last parent must still be able to reach the last child, and no parent has more
                    // than maxEdges children.
                    var maxHigh = Mathf.Min(low + maxEdges - 1, children - 1);
                    var minHigh = Mathf.Max(low, children - 1 - remainingAfter * maxEdges);
                    minHigh = Mathf.Min(minHigh, maxHigh);
                    high = rng.Range(minHigh, maxHigh + 1);
                }

                var node = state.Nodes[upper[i]];
                for (var child = low; child <= high; child++)
                    node.Next.Add(lower[child]);

                // The next parent starts at the same child (a shared child) or at the following one.
                if (i < parents - 1)
                {
                    var nextRemaining = parents - 1 - (i + 1);
                    var shareAllowed = high + maxEdges + nextRemaining * maxEdges >= children;
                    var share = high >= children - 1 || (shareAllowed && rng.Chance(mergeChance));
                    low = share ? high : Mathf.Min(high + 1, children - 1);
                }
            }
        }

        // ------------------------------------------------------------------- types and difficulties

        private static void AssignTypes(Rng rng, MapGenerationSettings map, MapState state, List<List<int>> floors)
        {
            var parentsOf = new Dictionary<int, List<int>>();
            foreach (var node in state.Nodes)
            {
                foreach (var next in node.Next)
                {
                    if (!parentsOf.TryGetValue(next, out var list))
                    {
                        list = new List<int>();
                        parentsOf[next] = list;
                    }

                    list.Add(node.Id);
                }
            }

            for (var floor = 0; floor < floors.Count - 1; floor++)
            {
                var weights = map.GetTypeWeights(floor + 1);
                foreach (var id in floors[floor])
                {
                    var node = state.Nodes[id];
                    node.Type = DrawType(rng, map, state, weights, parentsOf, id);
                }
            }

            if (map.GuaranteeHealBeforeBoss && floors.Count >= 2)
            {
                var floor = floors[floors.Count - 2];
                var hasHeal = false;
                foreach (var id in floor)
                    hasHeal |= state.Nodes[id].Type == EMapNodeType.Heal;

                if (!hasHeal)
                    state.Nodes[floor[rng.Range(0, floor.Count)]].Type = EMapNodeType.Heal;
            }
        }

        private static EMapNodeType DrawType(Rng rng, MapGenerationSettings map, MapState state, float[] weights,
            Dictionary<int, List<int>> parentsOf, int nodeId)
        {
            var weightList = new List<float>(weights);
            var type = EMapNodeType.Battle;
            for (var attempt = 0; attempt < map.TypeRerollAttempts; attempt++)
            {
                var index = rng.WeightedIndex(weightList);
                type = index < 0 ? EMapNodeType.Battle : MapGenerationSettings.GetDrawnType(index);
                if (!map.AvoidSameNonBattleTypeInARow || type == EMapNodeType.Battle ||
                    !ParentHasType(state, parentsOf, nodeId, type))
                    return type;
            }

            // Every draw repeated a non-battle type: a battle breaks the streak.
            return ParentHasType(state, parentsOf, nodeId, type) && weights[0] > 0f ? EMapNodeType.Battle : type;
        }

        private static bool ParentHasType(MapState state, Dictionary<int, List<int>> parentsOf, int nodeId,
            EMapNodeType type)
        {
            if (!parentsOf.TryGetValue(nodeId, out var parents)) return false;

            foreach (var parent in parents)
            {
                if (state.Nodes[parent].Type == type)
                    return true;
            }

            return false;
        }

        private static void AssignDifficulties(Rng rng, MapGenerationSettings map, MapState state)
        {
            var weights = new List<float>
            {
                map.GetDifficultyWeight(EBattleDifficulty.Easy),
                map.GetDifficultyWeight(EBattleDifficulty.Normal),
                map.GetDifficultyWeight(EBattleDifficulty.Hard),
            };

            foreach (var node in state.Nodes)
            {
                if (node.Type != EMapNodeType.Battle) continue;

                var index = rng.WeightedIndex(weights);
                node.Difficulty = index < 0 ? EBattleDifficulty.Normal : (EBattleDifficulty)index;
            }

            if (map.DiversifyBattleChoices)
                DiversifyChoices(rng, weights, state);
        }

        /// <summary>
        /// Every choice point of the map (the first floor, and each node with two or more next nodes) whose
        /// battles all share one difficulty gets one of them redrawn among the other difficulties, so the
        /// player can trade safety for XP (GDD Mechanic 2). Choice points are visited in id order (deterministic).
        /// </summary>
        private static void DiversifyChoices(Rng rng, List<float> weights, MapState state)
        {
            var firstFloor = new List<MapNodeState>();
            foreach (var node in state.Nodes)
            {
                if (node.Floor == 0)
                    firstFloor.Add(node);
            }

            Diversify(rng, weights, firstFloor);

            foreach (var node in state.Nodes)
            {
                if (node.Next.Count < 2) continue;

                var children = new List<MapNodeState>();
                foreach (var id in node.Next)
                {
                    var child = MapRules.GetNode(state, id);
                    if (child != null)
                        children.Add(child);
                }

                Diversify(rng, weights, children);
            }
        }

        private static void Diversify(Rng rng, List<float> weights, List<MapNodeState> choices)
        {
            MapNodeState last = null;
            var count = 0;
            foreach (var choice in choices)
            {
                if (choice.Type != EMapNodeType.Battle) continue;

                if (last != null && choice.Difficulty != last.Difficulty) return;
                last = choice;
                count++;
            }

            if (count < 2) return;

            var others = new List<float>(weights);
            others[(int)last.Difficulty] = 0f;
            var index = rng.WeightedIndex(others);
            if (index >= 0)
                last.Difficulty = (EBattleDifficulty)index;
        }

        // ------------------------------------------------------------------- battles

        /// <summary>
        /// Generates terrain and enemies of every battle node. The balance assets are read at the node's balance
        /// depth (<see cref="MapGenerationSettings.GetBalanceDepth"/>), so the difficulty curve fits the map length.
        /// </summary>
        private static void GenerateBattles(RunRandom random, MapGenerationSettings map,
            BattleGenerationSettings battle, TerrainGenerationSettings terrain, MapState state)
        {
            var size = battle.GridSize;
            var player = battle.PlayerSpawn;
            foreach (var node in state.Nodes)
            {
                if (node.Type != EMapNodeType.Battle && node.Type != EMapNodeType.Boss) continue;

                var balanceDepth = map.GetBalanceDepth(node.Depth);
                var terrainRng = random.Derive(ERandomStream.Terrain, node.Id);
                var cells = TerrainGenerator.Generate(terrainRng, Mathf.RoundToInt(balanceDepth), size.x, size.y,
                    player, terrain);
                var battleRng = random.Derive(ERandomStream.Battle, node.Id);
                node.Battle = BattleGenerator.Generate(battleRng, node.Depth, node.Difficulty,
                    node.Type == EMapNodeType.Boss, battle, cells, balanceDepth, map.RunLengthXpFactor);
            }
        }
    }
}
