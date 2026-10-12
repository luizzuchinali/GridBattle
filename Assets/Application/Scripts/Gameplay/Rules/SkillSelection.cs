using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    /// <summary>
    /// Shape of the cells a skill can be aimed at around its caster: the selection area, as in Dofus (the area of
    /// effect is a separate thing, drawn around the aimed cell). Distances are counted along the shape.
    /// </summary>
    public enum ESkillSelectionShape
    {
        /// <summary>Every cell within the range by the skill range metric of CombatSettings (Manhattan by default: a diamond).</summary>
        Circle,

        /// <summary>Only the caster's row and column (Dofus "cast in line"): a cross.</summary>
        Line,

        /// <summary>Only the four diagonals (Dofus "cast in diagonal"); one diagonal step counts 1.</summary>
        Diagonal,

        /// <summary>Rows, columns and diagonals together (line and diagonal).</summary>
        Star,

        /// <summary>Every cell of the square around the caster (a diagonal step counts 1).</summary>
        Square
    }

    /// <summary>What must be on the aimed cell (Dofus "free cell" / "needs a target").</summary>
    public enum ESkillTargetCell
    {
        /// <summary>Any cell.</summary>
        Any,

        /// <summary>A cell without a character (teleports, summons, traps).</summary>
        Empty,

        /// <summary>A cell with a living character on it.</summary>
        Occupied
    }

    /// <summary>
    /// Rules of a selection area, pure: whether a cell is on the shape and how far it is along it. Used by
    /// <c>SkillTargeting</c> (player aiming, highlights, the enemy AI and the simulation bot).
    /// </summary>
    public static class SkillSelection
    {
        /// <summary>
        /// Distance from <paramref name="from"/> to <paramref name="to"/> counted along <paramref name="shape"/>,
        /// or -1 when the cell is not on the shape. The caster's own cell is on every shape, at distance 0.
        /// </summary>
        public static float GetDistance(ESkillSelectionShape shape, EDistanceMetric circleMetric, Vector2Int from,
            Vector2Int to)
        {
            var dx = Mathf.Abs(to.x - from.x);
            var dy = Mathf.Abs(to.y - from.y);
            return shape switch
            {
                ESkillSelectionShape.Line => dx == 0 || dy == 0 ? dx + dy : -1f,
                ESkillSelectionShape.Diagonal => dx == dy ? dx : -1f,
                ESkillSelectionShape.Star => dx == 0 || dy == 0 || dx == dy ? Mathf.Max(dx, dy) : -1f,
                ESkillSelectionShape.Square => Mathf.Max(dx, dy),
                _ => GridDistance.Measure(circleMetric, from, to),
            };
        }

        /// <summary>
        /// Whether <paramref name="to"/> can be selected: on the shape and between <paramref name="minRange"/> and
        /// <paramref name="maxRange"/> (inclusive; a negative maximum means no limit).
        /// </summary>
        public static bool IsSelectable(ESkillSelectionShape shape, EDistanceMetric circleMetric, Vector2Int from,
            Vector2Int to, int minRange, int maxRange)
        {
            var distance = GetDistance(shape, circleMetric, from, to);
            if (distance < 0f || distance < minRange) return false;

            return maxRange < 0 || distance <= maxRange;
        }
    }
}
