using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Roles;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Map
{
    /// <summary>
    /// Pure queries over a <see cref="MapState"/> (which nodes the player can enter, the path so far...). The
    /// run manager and the map UI use them; nothing here changes the map.
    /// </summary>
    public static class MapRules
    {
        /// <summary>The node with the id, or null.</summary>
        [CanBeNull]
        public static MapNodeState GetNode(MapState map, int id)
        {
            if (map == null || id < 0 || id >= map.Nodes.Count) return null;

            // Ids are assigned in order, so the list index is the id; fall back to a search for edited saves.
            var node = map.Nodes[id];
            if (node.Id == id) return node;

            foreach (var candidate in map.Nodes)
            {
                if (candidate.Id == id)
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Nodes the player can enter now: the first floor before the first node, the nodes the current node
        /// leads to afterwards. Empty when the current node is the boss.
        /// </summary>
        public static List<MapNodeState> GetAvailableNodes(MapState map)
        {
            var result = new List<MapNodeState>();
            if (map == null) return result;

            if (map.CurrentNodeId < 0)
            {
                foreach (var node in map.Nodes)
                {
                    if (node.Floor == 0)
                        result.Add(node);
                }

                return result;
            }

            var current = GetNode(map, map.CurrentNodeId);
            if (current == null) return result;

            foreach (var id in current.Next)
            {
                var next = GetNode(map, id);
                if (next != null)
                    result.Add(next);
            }

            return result;
        }

        /// <summary>Whether the node can be entered from the current position.</summary>
        public static bool IsAvailable(MapState map, int nodeId)
        {
            foreach (var node in GetAvailableNodes(map))
            {
                if (node.Id == nodeId)
                    return true;
            }

            return false;
        }

        /// <summary>Whether the player already went through the node.</summary>
        public static bool IsVisited(MapState map, int nodeId) => map != null && map.VisitedNodeIds.Contains(nodeId);

        /// <summary>
        /// Whether the node is a battle with an elite: its icon and title on the map, and its reward when won
        /// (consumable offers and a skill upgrade, see <see cref="RunSettings"/>).
        /// </summary>
        public static bool IsEliteBattle([CanBeNull] MapNodeState node) =>
            node != null && node.Type == EMapNodeType.Battle && node.Battle != null && node.Battle.IsElite;

        /// <summary>
        /// Fixes the kinds of battle of a map saved before battles became only normal or elite (2026-10-20): an old
        /// Easy battle (or any unknown value) becomes Normal, and an old Hard battle without an elite (it now loads as
        /// Elite) becomes Normal too, so the kind always matches what the battle holds. Returns the nodes changed.
        /// </summary>
        public static int NormalizeDifficulties([CanBeNull] MapState map)
        {
            if (map == null) return 0;

            var changed = 0;
            foreach (var node in map.Nodes)
            {
                var difficulty = node.Difficulty == EBattleDifficulty.Elite && IsEliteBattle(node)
                    ? EBattleDifficulty.Elite
                    : EBattleDifficulty.Normal;
                if (node.Difficulty == difficulty) continue;

                node.Difficulty = difficulty;
                changed++;
            }

            return changed;
        }

        /// <summary>The nodes of a floor, ordered by column.</summary>
        public static List<MapNodeState> GetFloor(MapState map, int floor)
        {
            var result = new List<MapNodeState>();
            if (map == null) return result;

            foreach (var node in map.Nodes)
            {
                if (node.Floor == floor)
                    result.Add(node);
            }

            result.Sort((a, b) => a.Column.CompareTo(b.Column));
            return result;
        }

        // ------------------------------------------------------------------- node preview (mapa_e_nos.md)

        /// <summary>
        /// What the node preview shows of the enemies (type, roles and terrain are visible; the exact enemies and the
        /// XP are not): how many enemies of each role the battle has, roles in the order they first appear.
        /// Enemies without a role are not listed.
        /// </summary>
        public static List<(EnemyRoleDefinition Role, int Count)> GetRoleCounts([CanBeNull] BattleSpec spec)
        {
            var result = new List<(EnemyRoleDefinition Role, int Count)>();
            var database = GameDatabase.Instance;
            if (spec == null || database == null) return result;

            foreach (var enemy in spec.Enemies)
            {
                var config = database.Get<EnemyConfig>(enemy.EnemyId);
                var role = config != null ? config.Role : null;
                if (role == null) continue;

                var index = result.FindIndex(entry => entry.Role == role);
                if (index < 0)
                    result.Add((role, 1));
                else
                    result[index] = (role, result[index].Count + 1);
            }

            return result;
        }

        /// <summary>The terrain of a battle for the node preview: how many cells of each terrain type.</summary>
        public static List<(TerrainDefinition Terrain, int Count)> GetTerrainCounts([CanBeNull] BattleSpec spec)
        {
            var result = new List<(TerrainDefinition Terrain, int Count)>();
            var database = GameDatabase.Instance;
            if (spec == null || database == null) return result;

            foreach (var cell in spec.Terrain)
            {
                var terrain = database.Get<TerrainDefinition>(cell.TerrainId);
                if (terrain == null) continue;

                var index = result.FindIndex(entry => entry.Terrain == terrain);
                if (index < 0)
                    result.Add((terrain, 1));
                else
                    result[index] = (terrain, result[index].Count + 1);
            }

            return result;
        }

        /// <summary>The final boss node (last floor), or null.</summary>
        [CanBeNull]
        public static MapNodeState GetBossNode(MapState map)
        {
            if (map == null) return null;

            foreach (var node in map.Nodes)
            {
                if (node.Type == EMapNodeType.Boss)
                    return node;
            }

            return null;
        }
    }
}
