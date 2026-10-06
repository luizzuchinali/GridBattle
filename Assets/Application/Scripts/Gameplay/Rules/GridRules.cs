using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    /// <summary>
    /// Rules between characters and the grid. Pure, static functions: the only
    /// place where ranges, valid targets and steps are computed. Distance metrics
    /// come from CombatSettings.
    /// </summary>
    public static class GridRules
    {
        private static readonly Vector2Int[] OrthogonalSteps =
        {
            new(0, -1), new(-1, 0), new(1, 0), new(0, 1)
        };

        public static bool IsInWalkRange(Vector2Int from, Vector2Int to, int walkDistance)
        {
            return GridDistance.IsWithin(CombatResolver.Settings.MovementMetric, from, to, walkDistance);
        }

        public static bool IsInAttackRange(Vector2Int from, Vector2Int to, int attackDistance)
        {
            return GridDistance.IsWithin(CombatResolver.Settings.AttackMetric, from, to, attackDistance);
        }

        /// <summary>
        /// Whether the character can move to <paramref name="targetPos"/> this turn:
        /// inside the grid, not blocked by terrain, free, within the walk range and,
        /// when terrain blocks cells, reachable without crossing a blocked cell
        /// (see <see cref="HasWalkPath"/>).
        /// </summary>
        public static bool CanWalkTo(GridController grid, Character character, Vector2Int targetPos)
        {
            if (!grid.IsValidPosition(targetPos)) return false;
            if (!grid.IsWalkable(targetPos)) return false;
            if (!grid.IsFreePosition(targetPos)) return false;
            if (!IsInWalkRange(character.CurrentGridPos, targetPos, character.WalkDistance)) return false;

            return !grid.HasBlockedCells ||
                   HasWalkPath(grid, character.CurrentGridPos, targetPos, character.WalkDistance);
        }

        /// <summary>
        /// Whether <paramref name="to"/> can be reached from <paramref name="from"/>
        /// without crossing a blocked cell: a chain of unblocked, 4-adjacent cells,
        /// all within the walk range of <paramref name="from"/>. Characters do not
        /// block the way (only the destination must be free, see
        /// <see cref="CanWalkTo"/>).
        /// </summary>
        public static bool HasWalkPath(GridController grid, Vector2Int from, Vector2Int to, int walkDistance)
        {
            if (from == to) return true;

            var visited = new HashSet<Vector2Int> { from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var step in OrthogonalSteps)
                {
                    var next = current + step;
                    if (!visited.Add(next)) continue;
                    if (!grid.IsWalkable(next)) continue;
                    if (!IsInWalkRange(from, next, walkDistance)) continue;
                    if (next == to) return true;

                    queue.Enqueue(next);
                }
            }

            return false;
        }

        public static bool IsAttackTarget(GridController grid, Character attacker, Vector2Int targetPos)
        {
            if (!grid.IsValidPosition(targetPos)) return false;
            if (!grid.IsWalkable(targetPos)) return false;
            if (!attacker.CanBasicAttack) return false;
            if (!IsInAttackRange(attacker.CurrentGridPos, targetPos, attacker.AttackDistance)) return false;

            var content = grid.GetContent(targetPos);
            return content is IDamageReceiver && content != attacker;
        }

        /// <summary>
        /// Greedy one-turn step: among the reachable cells
        /// (<see cref="CanWalkTo"/>), the one closest to
        /// <paramref name="targetPos"/>. Returns null if none brings the character
        /// closer to the target. Does not plan multi-turn routes, except that when
        /// terrain blocks cells the step follows the shortest route around them
        /// (<see cref="FindStepAroundTerrain"/>), so a character is not stuck behind
        /// an obstacle.
        /// </summary>
        public static Vector2Int? FindStepToward(GridController grid, Character character, Vector2Int targetPos)
        {
            if (grid.HasBlockedCells && TryFindStepAroundTerrain(grid, character, targetPos, out var detour))
                return detour;

            Vector2Int? bestStep = null;
            var bestDistance = Vector2Int.Distance(character.CurrentGridPos, targetPos);
            var range = character.WalkDistance;

            for (var x = -range; x <= range; x++)
            {
                for (var y = -range; y <= range; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var candidate = character.CurrentGridPos + new Vector2Int(x, y);
                    if (!CanWalkTo(grid, character, candidate)) continue;

                    var distance = Vector2Int.Distance(candidate, targetPos);
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    bestStep = candidate;
                }
            }

            return bestStep;
        }

        /// <summary>
        /// Step along the shortest route to the target that goes around blocked
        /// cells (4-adjacent flood fill from the target, ignoring characters): among
        /// the reachable cells (<see cref="CanWalkTo"/>), the one with the lowest
        /// route length, which must be shorter than the character's current one;
        /// ties go to the straight-line closest, then the first visited (row, then
        /// column). <paramref name="step"/> is null when no cell improves the route.
        /// Returns false when the target cannot be reached by any route (the caller
        /// falls back to the greedy step).
        /// </summary>
        private static bool TryFindStepAroundTerrain(GridController grid, Character character,
            Vector2Int targetPos, out Vector2Int? step)
        {
            step = null;
            var routeLength = ComputeRouteLengths(grid, targetPos);
            var start = character.CurrentGridPos;
            var startLength = routeLength[start.x, start.y];
            if (startLength < 0) return false;

            var bestLength = startLength;
            var bestDistance = float.MaxValue;
            var range = character.WalkDistance;
            for (var y = -range; y <= range; y++)
            {
                for (var x = -range; x <= range; x++)
                {
                    if (x == 0 && y == 0) continue;

                    var candidate = start + new Vector2Int(x, y);
                    if (!CanWalkTo(grid, character, candidate)) continue;

                    var length = routeLength[candidate.x, candidate.y];
                    if (length < 0 || length >= startLength) continue;

                    var distance = Vector2Int.Distance(candidate, targetPos);
                    var isBetter = step == null || length < bestLength ||
                                   (length == bestLength && distance < bestDistance);
                    if (!isBetter) continue;

                    bestLength = length;
                    bestDistance = distance;
                    step = candidate;
                }
            }

            return true;
        }

        /// <summary>
        /// Length of the shortest route from every cell to <paramref name="targetPos"/>
        /// through unblocked cells (4-adjacent moves); -1 for cells with no route.
        /// </summary>
        private static int[,] ComputeRouteLengths(GridController grid, Vector2Int targetPos)
        {
            var size = grid.Size;
            var lengths = new int[size.x, size.y];
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                    lengths[x, y] = -1;
            }

            if (!grid.IsValidPosition(targetPos)) return lengths;

            var queue = new Queue<Vector2Int>();
            lengths[targetPos.x, targetPos.y] = 0;
            queue.Enqueue(targetPos);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var step in OrthogonalSteps)
                {
                    var next = current + step;
                    if (!grid.IsWalkable(next) || lengths[next.x, next.y] >= 0) continue;

                    lengths[next.x, next.y] = lengths[current.x, current.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return lengths;
        }

        /// <summary>
        /// How many living characters of the other side are adjacent to <paramref name="character"/> (distance 1 by
        /// <see cref="CombatSettings.AdjacencyMetric"/>). Used by the conditional damage effects of talents.
        /// </summary>
        public static int CountAdjacentOpponents(GridController grid, Character character)
        {
            return CountAdjacent(grid, character, false);
        }

        /// <summary>
        /// Whether a living ally of <paramref name="character"/> (itself excluded) is adjacent to it: a character
        /// with none is "isolated".
        /// </summary>
        public static bool HasAdjacentAlly(GridController grid, Character character)
        {
            return CountAdjacent(grid, character, true) > 0;
        }

        private static int CountAdjacent(GridController grid, Character character, bool allies)
        {
            if (grid == null || character == null) return 0;

            var metric = CombatResolver.Settings.AdjacencyMetric;
            var origin = character.CurrentGridPos;
            var count = 0;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var cell = origin + new Vector2Int(dx, dy);
                    if (!grid.IsValidPosition(cell) || !GridDistance.IsWithin(metric, origin, cell, 1)) continue;
                    if (grid.GetContent(cell) is not Character other || other.IsDead) continue;
                    if (AreAllies(character, other) == allies)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Whether two characters fight on the same side (player vs enemies). A
        /// character is its own ally.
        /// </summary>
        public static bool AreAllies(Character a, Character b)
        {
            return (a is PlayerCharacter) == (b is PlayerCharacter);
        }

        /// <summary>
        /// Highlights for a skill being aimed: every cell it can be used on (see
        /// <see cref="SkillTargeting.GetTargetableCells"/>), marked as
        /// <see cref="ECellHighlightType.SkillRange"/>.
        /// </summary>
        public static Dictionary<Vector2Int, ECellHighlightType> GetSkillHighlightInfos(
            GridController grid, Character caster, SkillDefinition skill)
        {
            var highlightInfos = new Dictionary<Vector2Int, ECellHighlightType>();
            foreach (var cell in SkillTargeting.GetTargetableCells(grid, caster, skill))
                highlightInfos[cell] = ECellHighlightType.SkillRange;

            return highlightInfos;
        }

        public static Dictionary<Vector2Int, ECellHighlightType> GetHighlightInfos(
            GridController grid, Character character)
        {
            var highlightInfos = new Dictionary<Vector2Int, ECellHighlightType>();
            var currentPos = character.CurrentGridPos;
            var range = Mathf.Max(character.WalkDistance, character.AttackDistance);

            for (var x = -range; x <= range; x++)
            {
                for (var y = -range; y <= range; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var targetPos = currentPos + new Vector2Int(x, y);
                    if (!grid.IsValidPosition(targetPos)) continue;

                    if (IsAttackTarget(grid, character, targetPos))
                    {
                        highlightInfos.Add(targetPos, ECellHighlightType.Attack);
                        continue;
                    }

                    if (CanWalkTo(grid, character, targetPos))
                        highlightInfos.Add(targetPos, ECellHighlightType.Walk);
                }
            }

            return highlightInfos;
        }
    }
}