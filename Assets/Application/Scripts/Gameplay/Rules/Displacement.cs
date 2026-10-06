using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    /// <summary>Where a displaced character is sent relative to the skill that moves it.</summary>
    public enum EDisplacementMode
    {
        /// <summary>Pushed away from the caster.</summary>
        AwayFromCaster,

        /// <summary>Pulled toward the caster; it stops next to the caster and never lands on it.</summary>
        TowardCaster,

        /// <summary>
        /// Pushed away from the center of the skill's area (the cell it was aimed at). A character standing on
        /// the center is pushed away from the caster instead.
        /// </summary>
        AwayFromAreaCenter
    }

    /// <summary>What stopped a displacement before it travelled its whole distance.</summary>
    public enum EDisplacementBlock
    {
        /// <summary>Nothing: the character travelled the whole distance (or reached the caster when pulled).</summary>
        None,

        /// <summary>The edge of the grid.</summary>
        Edge,

        /// <summary>Terrain that blocks movement (an obstacle), or a diagonal squeezed between two of them.</summary>
        Terrain,

        /// <summary>Another character stood on the next cell.</summary>
        Character
    }

    /// <summary>
    /// The outcome of displacing one character, computed without changing anything (see
    /// <see cref="Displacement.Compute"/>): the cells it crosses, where it stops and what stopped it.
    /// </summary>
    public sealed class DisplacementResult
    {
        public Character Target;

        /// <summary>Cell the character started on.</summary>
        public Vector2Int Start;

        /// <summary>Unit step of the displacement, each component in -1..1 (zero: no direction).</summary>
        public Vector2Int Direction;

        /// <summary>Cells the displacement was meant to cover.</summary>
        public int Distance;

        /// <summary>Cells the character enters, in order (does not include <see cref="Start"/>).</summary>
        public readonly List<Vector2Int> Path = new();

        public EDisplacementBlock Block;

        /// <summary>The cell the character failed to enter (outside the grid for <see cref="EDisplacementBlock.Edge"/>).</summary>
        public Vector2Int BlockedCell;

        /// <summary>The character standing on <see cref="BlockedCell"/> for <see cref="EDisplacementBlock.Character"/>.</summary>
        [CanBeNull]
        public Character BlockedBy;

        /// <summary>The character cannot be displaced (<see cref="Character.CanBeDisplaced"/>): nothing moves.</summary>
        public bool Immovable;

        /// <summary>A pulled character stopped because it is next to the caster.</summary>
        public bool ReachedCaster;

        /// <summary>Terrain of the cell where the character ends (null: plain floor).</summary>
        [CanBeNull]
        public TerrainDefinition FinalTerrain;

        /// <summary>Cell where the character ends (<see cref="Start"/> when it did not move).</summary>
        public Vector2Int Final => Path.Count > 0 ? Path[Path.Count - 1] : Start;

        /// <summary>Cells actually travelled.</summary>
        public int CellsMoved => Path.Count;

        /// <summary>Cells the character did not travel because something stopped it (0 when nothing did).</summary>
        public int UnspentCells => Block == EDisplacementBlock.None ? 0 : Math.Max(0, Distance - Path.Count);

        /// <summary>Whether the stop counts as a collision (the grid edge only when the settings say so).</summary>
        public bool IsCollision(CombatSettings settings)
        {
            switch (Block)
            {
                case EDisplacementBlock.Terrain:
                case EDisplacementBlock.Character:
                    return true;
                case EDisplacementBlock.Edge:
                    return settings.EdgeCountsAsCollision;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Rules of pushing and pulling characters across the grid: pure static functions that compute what a
    /// displacement would do without applying it, so the skill effect, the enemy AI and the simulator bot all
    /// share them. <see cref="DisplacementResolver"/> applies the outcome.
    /// <para>
    /// A displacement moves one character cell by cell along a straight line of one of the 8 directions. The
    /// direction is the 8-neighbour direction closest to the vector from the reference cell to the character
    /// (the angle snapped to 45 degrees; for adjacent cells it is simply the sign of each component). The movement
    /// stops at the edge of the grid, at terrain that blocks movement (a diagonal step also stops when both side
    /// cells are blocked, the same rule as walking), or at a cell occupied by another character. Passing through
    /// cells has no effect. A pulled character stops as soon as it is next to the caster (8 neighbours).
    /// </para>
    /// </summary>
    public static class Displacement
    {
        private static readonly Vector2Int[] Octants =
        {
            new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)
        };

        /// <summary>
        /// The 8-neighbour direction closest to <paramref name="delta"/> (zero for a zero vector). Diagonals appear
        /// when both components are significant (angle between 22.5 and 67.5 degrees).
        /// </summary>
        public static Vector2Int SnapDirection(Vector2Int delta)
        {
            if (delta == Vector2Int.zero) return Vector2Int.zero;

            var octant = Mathf.RoundToInt(Mathf.Atan2(delta.y, delta.x) / (Mathf.PI / 4f));
            return Octants[(octant + 8) % 8];
        }

        /// <summary>
        /// Direction a character at <paramref name="targetPos"/> is sent by a displacement of
        /// <paramref name="mode"/> (zero when the reference cell is the character's own cell).
        /// </summary>
        public static Vector2Int GetDirection(EDisplacementMode mode, Vector2Int casterPos, Vector2Int targetPos,
            Vector2Int areaCenter)
        {
            switch (mode)
            {
                case EDisplacementMode.TowardCaster:
                    return SnapDirection(casterPos - targetPos);
                case EDisplacementMode.AwayFromAreaCenter:
                    return SnapDirection(targetPos - (targetPos == areaCenter ? casterPos : areaCenter));
                default:
                    return SnapDirection(targetPos - casterPos);
            }
        }

        /// <summary>
        /// Computes the displacement of <paramref name="target"/> by <paramref name="distance"/> cells along
        /// <paramref name="direction"/>. Nothing is changed. <paramref name="stopAdjacentTo"/> (the caster's cell
        /// for pulls) ends the movement as soon as the character is next to that cell.
        /// <paramref name="moved"/> is an optional what-if overlay with the cells characters of the same skill use
        /// already moved to (used to predict a whole area use); characters in it are looked up at that cell, not
        /// at their real one.
        /// </summary>
        public static DisplacementResult Compute(GridController grid, Character target, Vector2Int direction,
            int distance, Vector2Int? stopAdjacentTo = null,
            IReadOnlyDictionary<Character, Vector2Int> moved = null)
        {
            var result = new DisplacementResult
            {
                Target = target,
                Start = target.CurrentGridPos,
                Direction = direction,
                Distance = Math.Max(0, distance),
            };

            if (!target.CanBeDisplaced)
            {
                result.Immovable = true;
                result.FinalTerrain = grid.GetTerrain(result.Start);
                return result;
            }

            var position = result.Start;
            if (direction != Vector2Int.zero)
            {
                var diagonal = direction.x != 0 && direction.y != 0;
                for (var i = 0; i < result.Distance; i++)
                {
                    if (stopAdjacentTo.HasValue && IsAdjacentOrSame(position, stopAdjacentTo.Value))
                    {
                        result.ReachedCaster = true;
                        break;
                    }

                    var next = position + direction;
                    if (!grid.IsValidPosition(next))
                    {
                        Block(result, EDisplacementBlock.Edge, next, null);
                        break;
                    }

                    if (!grid.IsWalkable(next) ||
                        (diagonal &&
                         !grid.IsWalkable(position + new Vector2Int(direction.x, 0)) &&
                         !grid.IsWalkable(position + new Vector2Int(0, direction.y))))
                    {
                        Block(result, EDisplacementBlock.Terrain, next, null);
                        break;
                    }

                    var other = GetCharacterAt(grid, next, moved);
                    if (other != null && other != target)
                    {
                        Block(result, EDisplacementBlock.Character, next, other);
                        break;
                    }

                    result.Path.Add(next);
                    position = next;
                }
            }

            result.FinalTerrain = grid.GetTerrain(position);
            return result;
        }

        /// <summary>
        /// Order in which the targets of one skill use are displaced, so they block each other as little as
        /// possible: pushes start with the target farthest from the reference cell (the area center for
        /// <see cref="EDisplacementMode.AwayFromAreaCenter"/>, else the caster) and pulls with the nearest one; ties
        /// by row, then column. Deterministic.
        /// </summary>
        public static List<Character> OrderTargets(IEnumerable<Character> targets, EDisplacementMode mode,
            Vector2Int casterPos, Vector2Int areaCenter)
        {
            var reference = mode == EDisplacementMode.AwayFromAreaCenter ? areaCenter : casterPos;
            var ordered = new List<Character>(targets);
            ordered.Sort((a, b) =>
            {
                var distanceA = (a.CurrentGridPos - reference).sqrMagnitude;
                var distanceB = (b.CurrentGridPos - reference).sqrMagnitude;
                var byDistance = mode == EDisplacementMode.TowardCaster
                    ? distanceA.CompareTo(distanceB)
                    : distanceB.CompareTo(distanceA);
                if (byDistance != 0) return byDistance;

                var byY = a.CurrentGridPos.y.CompareTo(b.CurrentGridPos.y);
                return byY != 0 ? byY : a.CurrentGridPos.x.CompareTo(b.CurrentGridPos.x);
            });
            return ordered;
        }

        /// <summary>
        /// Predicts the displacement of every target of one skill use, in application order, each one seeing the
        /// moves of the ones before it. Nothing is changed. Deaths are not predicted, so a character killed by a
        /// collision still blocks the characters displaced after it.
        /// </summary>
        public static List<DisplacementResult> Plan(GridController grid, Character caster,
            IEnumerable<Character> targets, EDisplacementMode mode, int distance, Vector2Int areaCenter)
        {
            var results = new List<DisplacementResult>();
            var moved = new Dictionary<Character, Vector2Int>();
            var casterPos = caster.CurrentGridPos;
            foreach (var target in OrderTargets(targets, mode, casterPos, areaCenter))
            {
                if (target == null || target.IsDead) continue;

                var direction = GetDirection(mode, casterPos, target.CurrentGridPos, areaCenter);
                var result = Compute(grid, target, direction, distance,
                    mode == EDisplacementMode.TowardCaster ? casterPos : null, moved);
                if (result.CellsMoved > 0)
                    moved[target] = result.Final;
                results.Add(result);
            }

            return results;
        }

        private static void Block(DisplacementResult result, EDisplacementBlock block, Vector2Int cell,
            [CanBeNull] Character by)
        {
            result.Block = block;
            result.BlockedCell = cell;
            result.BlockedBy = by;
        }

        private static bool IsAdjacentOrSame(Vector2Int a, Vector2Int b)
        {
            return Math.Abs(a.x - b.x) <= 1 && Math.Abs(a.y - b.y) <= 1;
        }

        /// <summary>The character on a cell, looking at the what-if overlay first (moved characters left their real cell).</summary>
        [CanBeNull]
        private static Character GetCharacterAt(GridController grid, Vector2Int cell,
            [CanBeNull] IReadOnlyDictionary<Character, Vector2Int> moved)
        {
            if (moved != null && moved.Count > 0)
            {
                foreach (var pair in moved)
                {
                    if (pair.Value == cell) return pair.Key;
                }
            }

            var content = grid.GetContent(cell) as Character;
            if (content != null && moved != null && moved.ContainsKey(content)) return null;
            return content;
        }
    }
}
