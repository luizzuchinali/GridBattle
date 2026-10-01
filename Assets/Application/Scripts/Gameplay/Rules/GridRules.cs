using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    public static class GridRules
    {
        public static bool IsInWalkRange(Vector2Int from, Vector2Int to, int walkDistance)
        {
            return Vector2Int.Distance(to, from) <= walkDistance;
        }

        public static bool IsInAttackRange(Vector2Int from, Vector2Int to, int attackDistance)
        {
            return Vector2Int.Distance(to, from) <= attackDistance;
        }

        public static bool CanWalkTo(GridController grid, Character character, Vector2Int targetPos)
        {
            if (!grid.IsValidPosition(targetPos)) return false;
            if (!grid.IsFreePosition(targetPos)) return false;
            return IsInWalkRange(character.CurrentGridPos, targetPos, character.WalkDistance);
        }

        public static bool IsAttackTarget(GridController grid, Character attacker, Vector2Int targetPos)
        {
            if (!grid.IsValidPosition(targetPos)) return false;
            if (!IsInAttackRange(attacker.CurrentGridPos, targetPos, attacker.AttackDistance)) return false;

            var content = grid.GetContent(targetPos);
            return content is IDamageReceiver && content != attacker;
        }

        /// <summary>
        /// Greedy one-turn step: among the reachable cells
        /// (<see cref="CanWalkTo"/>), the one closest to
        /// <paramref name="targetPos"/>. Returns null if none brings the character
        /// closer to the target. Does not plan multi-turn routes.
        /// </summary>
        public static Vector2Int? FindStepToward(GridController grid, Character character, Vector2Int targetPos)
        {
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