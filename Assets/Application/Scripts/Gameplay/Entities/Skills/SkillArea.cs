using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Area-of-effect shapes of skills (GDD 6 / classes_e_skills). Pure and
    /// static: given a shape, a size, the caster's cell and the target cell it
    /// returns the cells hit.
    /// <para>
    /// Conventions: grid y grows downward (row 0 is the top row). Let
    /// <c>r = (size - 1) / 2</c> (size is odd, 1 to 9). The direction <c>d</c> is
    /// the cardinal direction from the caster to the target along the dominant
    /// axis (ties go horizontal; caster == target gives <c>(0, -1)</c>, i.e. up),
    /// and <c>p</c> is <c>d</c> rotated by 90 degrees (the shapes using it are
    /// symmetric, so the rotation sense does not matter).
    /// </para>
    /// <list type="bullet">
    /// <item><b>Circle</b>: cells whose Euclidean distance to the target is at most
    /// <c>r + 0.5</c> (size 1 = only the target, size 3 = 3x3 square, size 5 =
    /// 5x5 without the corners).</item>
    /// <item><b>Cross</b>: the target plus arms of length <c>r</c> along both axes
    /// (size 1 = only the target, size 3 = a plus of 5 cells).</item>
    /// <item><b>Linear</b>: <c>size</c> cells starting at the target and
    /// continuing along <c>d</c> (away from the caster).</item>
    /// <item><b>Perpendicular</b>: <c>size</c> cells centered on the target along
    /// <c>p</c>.</item>
    /// <item><b>Cone</b>: rows <c>k = 0..r</c> at <c>target + d*k</c>, each
    /// <c>2k + 1</c> cells wide and centered along <c>p</c> (size 3 = 4 cells,
    /// size 5 = 9 cells).</item>
    /// <item><b>Arc</b>: a row of <c>size</c> cells centered on the target along
    /// <c>p</c>, where the cell at lateral offset <c>i</c> is pulled back toward
    /// the caster by <c>min(|i|, r)</c> cells: a "V" bending around the caster.</item>
    /// </list>
    /// Cells outside the grid are dropped. The result has no duplicates and is
    /// sorted by row (y) then column (x), a canonical order that keeps hit order
    /// (and therefore random draws) deterministic.
    /// </summary>
    public static class SkillArea
    {
        /// <summary>Direction used when the caster targets its own cell: up.</summary>
        public static readonly Vector2Int DefaultDirection = new(0, -1);

        /// <summary>
        /// Cells hit by a skill of <paramref name="shape"/> and <paramref name="size"/>
        /// aimed at <paramref name="targetPos"/> by a caster at <paramref name="casterPos"/>.
        /// </summary>
        /// <param name="isInsideGrid">Filter for valid grid cells; null keeps every cell.</param>
        public static List<Vector2Int> GetCells(ESkillAreaShape shape, int size, Vector2Int casterPos,
            Vector2Int targetPos, Func<Vector2Int, bool> isInsideGrid = null)
        {
            size = Mathf.Max(1, size);
            var radius = (size - 1) / 2;
            var direction = GetDirection(casterPos, targetPos);
            var lateral = new Vector2Int(-direction.y, direction.x);
            var cells = new HashSet<Vector2Int>();

            switch (shape)
            {
                case ESkillAreaShape.Circle:
                    var maxDistance = radius + 0.5f;
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        for (var dy = -radius; dy <= radius; dy++)
                        {
                            if (Mathf.Sqrt(dx * dx + dy * dy) <= maxDistance)
                                cells.Add(targetPos + new Vector2Int(dx, dy));
                        }
                    }

                    break;

                case ESkillAreaShape.Cross:
                    cells.Add(targetPos);
                    for (var i = 1; i <= radius; i++)
                    {
                        cells.Add(targetPos + new Vector2Int(i, 0));
                        cells.Add(targetPos + new Vector2Int(-i, 0));
                        cells.Add(targetPos + new Vector2Int(0, i));
                        cells.Add(targetPos + new Vector2Int(0, -i));
                    }

                    break;

                case ESkillAreaShape.Linear:
                    for (var k = 0; k < size; k++)
                        cells.Add(targetPos + direction * k);
                    break;

                case ESkillAreaShape.Perpendicular:
                    for (var i = -radius; i <= radius; i++)
                        cells.Add(targetPos + lateral * i);
                    break;

                case ESkillAreaShape.Cone:
                    for (var k = 0; k <= radius; k++)
                    {
                        for (var i = -k; i <= k; i++)
                            cells.Add(targetPos + direction * k + lateral * i);
                    }

                    break;

                case ESkillAreaShape.Arc:
                    for (var i = -radius; i <= radius; i++)
                    {
                        var pullBack = Mathf.Min(Mathf.Abs(i), radius);
                        cells.Add(targetPos + lateral * i - direction * pullBack);
                    }

                    break;
            }

            var result = new List<Vector2Int>(cells.Count);
            foreach (var cell in cells)
            {
                if (isInsideGrid == null || isInsideGrid(cell))
                    result.Add(cell);
            }

            result.Sort(CompareRowThenColumn);
            return result;
        }

        /// <summary>
        /// Cardinal direction from the caster to the target along the dominant axis
        /// (ties go horizontal). <see cref="DefaultDirection"/> when both are the same cell.
        /// </summary>
        public static Vector2Int GetDirection(Vector2Int casterPos, Vector2Int targetPos)
        {
            var delta = targetPos - casterPos;
            if (delta == Vector2Int.zero) return DefaultDirection;

            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(delta.y));
        }

        private static int CompareRowThenColumn(Vector2Int a, Vector2Int b)
        {
            var byRow = a.y.CompareTo(b.y);
            return byRow != 0 ? byRow : a.x.CompareTo(b.x);
        }
    }
}
