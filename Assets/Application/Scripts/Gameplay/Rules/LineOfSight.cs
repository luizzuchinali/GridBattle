using System;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    /// <summary>
    /// Line of sight between two cells, used to aim skills (<see cref="Entities.Skills.SkillTargeting"/>). The line
    /// goes from the center of one cell to the center of the other; every cell it crosses in between blocks it when
    /// its terrain blocks sight (<see cref="Terrain.TerrainDefinition.BlocksLineOfSight"/>) or, when
    /// <see cref="CombatSettings.CharactersBlockLineOfSight"/> is on, when a living character stands on it. The two
    /// end cells never block, and a line that only touches the corner of a cell does not cross it (so a diagonal
    /// neighbor is always visible). Pure and symmetric: A sees B exactly when B sees A.
    /// </summary>
    public static class LineOfSight
    {
        /// <summary>How far inside a cell the line has to pass to cross it (in cells): corners do not count.</summary>
        private const float CornerTolerance = 0.001f;

        /// <summary>Whether nothing blocks the line between the centers of <paramref name="from"/> and <paramref name="to"/>.</summary>
        public static bool IsClear(GridController grid, Vector2Int from, Vector2Int to)
        {
            if (from == to) return true;

            var charactersBlock = CombatResolver.Settings.CharactersBlockLineOfSight;
            var minX = Math.Min(from.x, to.x);
            var maxX = Math.Max(from.x, to.x);
            var minY = Math.Min(from.y, to.y);
            var maxY = Math.Max(from.y, to.y);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (cell == from || cell == to || !grid.IsValidPosition(cell)) continue;
                    if (!Crosses(from, to, cell)) continue;
                    if (BlocksSight(grid, cell, charactersBlock)) return false;
                }
            }

            return true;
        }

        /// <summary>Whether the cell's terrain blocks sight, or a living character on it does (when they block).</summary>
        public static bool BlocksSight(GridController grid, Vector2Int cell, bool charactersBlock)
        {
            var terrain = grid.GetTerrain(cell);
            if (terrain != null && terrain.BlocksLineOfSight) return true;

            return charactersBlock && grid.GetContent(cell) is Character character && !character.IsDead;
        }

        /// <summary>
        /// Whether the segment between the centers of <paramref name="from"/> and <paramref name="to"/> passes through
        /// the inside of <paramref name="cell"/> (its square shrunk by <see cref="CornerTolerance"/>), clipping the
        /// segment against the square (Liang–Barsky).
        /// </summary>
        private static bool Crosses(Vector2Int from, Vector2Int to, Vector2Int cell)
        {
            float x0 = from.x, y0 = from.y;
            float dx = to.x - from.x, dy = to.y - from.y;
            var half = 0.5f - CornerTolerance;
            float tMin = 0f, tMax = 1f;
            return Clip(-dx, x0 - (cell.x - half), ref tMin, ref tMax) &&
                   Clip(dx, cell.x + half - x0, ref tMin, ref tMax) &&
                   Clip(-dy, y0 - (cell.y - half), ref tMin, ref tMax) &&
                   Clip(dy, cell.y + half - y0, ref tMin, ref tMax) &&
                   tMin < tMax;
        }

        private static bool Clip(float p, float q, ref float tMin, ref float tMax)
        {
            if (Mathf.Approximately(p, 0f)) return q > 0f;

            var t = q / p;
            if (p < 0f)
            {
                if (t > tMax) return false;
                if (t > tMin) tMin = t;
            }
            else
            {
                if (t < tMin) return false;
                if (t < tMax) tMax = t;
            }

            return true;
        }
    }
}
