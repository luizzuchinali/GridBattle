using System.Collections.Generic;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Map
{
    /// <summary>
    /// Checks the invariants of a generated map (used by the edit-mode tests and, in the editor, as a sanity
    /// check): boss last and alone, columns inside the lanes and distinct per floor, edges only to the next
    /// floor, at most the configured edges per node, no crossing edges, every node reachable from the first
    /// floor and leading to the boss, and a battle for every battle node.
    /// </summary>
    public static class MapValidator
    {
        /// <summary>Returns the broken invariants (empty = valid).</summary>
        public static List<string> Validate(MapState map, MapGenerationSettings settings)
        {
            var errors = new List<string>();
            if (map == null || map.Nodes.Count == 0)
            {
                errors.Add("The map has no nodes.");
                return errors;
            }

            var floorCount = settings.FloorCount;
            if (map.FloorCount != floorCount)
                errors.Add($"FloorCount {map.FloorCount} != settings {floorCount}.");

            var floors = new Dictionary<int, List<MapNodeState>>();
            for (var i = 0; i < map.Nodes.Count; i++)
            {
                var node = map.Nodes[i];
                if (node.Id != i)
                    errors.Add($"Node at index {i} has id {node.Id}.");
                if (node.Floor < 0 || node.Floor >= floorCount)
                {
                    errors.Add($"Node {node.Id} has floor {node.Floor} outside 0..{floorCount - 1}.");
                    continue;
                }

                if (node.Column < 0 || node.Column >= settings.Lanes)
                    errors.Add($"Node {node.Id} has column {node.Column} outside the {settings.Lanes} lanes.");

                if (!floors.TryGetValue(node.Floor, out var list))
                {
                    list = new List<MapNodeState>();
                    floors[node.Floor] = list;
                }

                list.Add(node);
            }

            for (var floor = 0; floor < floorCount; floor++)
            {
                if (!floors.TryGetValue(floor, out var nodes) || nodes.Count == 0)
                {
                    errors.Add($"Floor {floor} has no nodes.");
                    continue;
                }

                var columns = new HashSet<int>();
                foreach (var node in nodes)
                {
                    if (!columns.Add(node.Column))
                        errors.Add($"Floor {floor} has two nodes in column {node.Column}.");
                }

                if (floor == floorCount - 1)
                {
                    if (nodes.Count != 1 || nodes[0].Type != EMapNodeType.Boss)
                        errors.Add("The last floor must hold only the final boss.");
                }
                else
                {
                    if (nodes.Count < settings.MinNodesPerFloor || nodes.Count > settings.MaxNodesPerFloor)
                        errors.Add($"Floor {floor} has {nodes.Count} nodes (allowed {settings.MinNodesPerFloor}-{settings.MaxNodesPerFloor}).");
                    foreach (var node in nodes)
                    {
                        if (node.Type == EMapNodeType.Boss)
                            errors.Add($"Node {node.Id} is a boss before the last floor.");
                    }
                }
            }

            foreach (var node in map.Nodes)
            {
                ValidateEdges(map, settings, node, floorCount, errors);

                var isBattle = node.Type == EMapNodeType.Battle || node.Type == EMapNodeType.Boss;
                if (isBattle && node.Battle == null)
                    errors.Add($"Battle node {node.Id} has no BattleSpec.");
                if (!isBattle && node.Battle != null)
                    errors.Add($"Non-battle node {node.Id} has a BattleSpec.");
                if (isBattle && node.Battle != null && node.Battle.Enemies.Count == 0)
                    errors.Add($"Battle node {node.Id} has no enemies.");
            }

            ValidateNoCrossings(map, floors, floorCount, errors);
            ValidateReachability(map, floorCount, errors);
            return errors;
        }

        private static void ValidateEdges(MapState map, MapGenerationSettings settings, MapNodeState node,
            int floorCount, List<string> errors)
        {
            var isLast = node.Floor == floorCount - 1;
            if (isLast)
            {
                if (node.Next.Count != 0)
                    errors.Add($"The boss node {node.Id} must not lead anywhere.");
                return;
            }

            if (node.Next.Count == 0)
                errors.Add($"Node {node.Id} leads nowhere.");
            if (node.Next.Count > settings.MaxEdgesPerNode)
                errors.Add($"Node {node.Id} has {node.Next.Count} edges (max {settings.MaxEdgesPerNode}).");

            var seen = new HashSet<int>();
            foreach (var id in node.Next)
            {
                var target = MapRules.GetNode(map, id);
                if (target == null)
                {
                    errors.Add($"Node {node.Id} leads to missing node {id}.");
                    continue;
                }

                if (target.Floor != node.Floor + 1)
                    errors.Add($"Node {node.Id} (floor {node.Floor}) leads to node {id} on floor {target.Floor}.");
                if (!seen.Add(id))
                    errors.Add($"Node {node.Id} lists node {id} twice.");
            }
        }

        private static void ValidateNoCrossings(MapState map, Dictionary<int, List<MapNodeState>> floors,
            int floorCount, List<string> errors)
        {
            for (var floor = 0; floor + 1 < floorCount; floor++)
            {
                if (!floors.TryGetValue(floor, out var nodes)) continue;

                var edges = new List<(int from, int to)>();
                foreach (var node in nodes)
                {
                    foreach (var id in node.Next)
                    {
                        var target = MapRules.GetNode(map, id);
                        if (target != null)
                            edges.Add((node.Column, target.Column));
                    }
                }

                for (var a = 0; a < edges.Count; a++)
                {
                    for (var b = a + 1; b < edges.Count; b++)
                    {
                        var first = edges[a];
                        var second = edges[b];
                        if ((first.from < second.from && first.to > second.to) ||
                            (first.from > second.from && first.to < second.to))
                            errors.Add($"Edges cross between floors {floor} and {floor + 1} " +
                                       $"(columns {first.from}->{first.to} and {second.from}->{second.to}).");
                    }
                }
            }
        }

        private static void ValidateReachability(MapState map, int floorCount, List<string> errors)
        {
            var reachable = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (var node in map.Nodes)
            {
                if (node.Floor != 0) continue;

                reachable.Add(node.Id);
                queue.Enqueue(node.Id);
            }

            while (queue.Count > 0)
            {
                var node = MapRules.GetNode(map, queue.Dequeue());
                if (node == null) continue;

                foreach (var next in node.Next)
                {
                    if (reachable.Add(next))
                        queue.Enqueue(next);
                }
            }

            foreach (var node in map.Nodes)
            {
                if (!reachable.Contains(node.Id))
                    errors.Add($"Node {node.Id} is not reachable from the first floor.");
            }

            // Leads to the boss: walk the graph backwards from the boss.
            var boss = MapRules.GetBossNode(map);
            if (boss == null)
            {
                errors.Add("The map has no boss node.");
                return;
            }

            var parents = new Dictionary<int, List<int>>();
            foreach (var node in map.Nodes)
            {
                foreach (var next in node.Next)
                {
                    if (!parents.TryGetValue(next, out var list))
                    {
                        list = new List<int>();
                        parents[next] = list;
                    }

                    list.Add(node.Id);
                }
            }

            var reachesBoss = new HashSet<int> { boss.Id };
            queue.Enqueue(boss.Id);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!parents.TryGetValue(id, out var list)) continue;

                foreach (var parent in list)
                {
                    if (reachesBoss.Add(parent))
                        queue.Enqueue(parent);
                }
            }

            foreach (var node in map.Nodes)
            {
                if (!reachesBoss.Contains(node.Id))
                    errors.Add($"Node {node.Id} does not lead to the boss.");
            }
        }
    }
}
