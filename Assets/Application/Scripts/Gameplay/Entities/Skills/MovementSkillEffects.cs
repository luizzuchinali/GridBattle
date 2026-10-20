using System;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// The caster trades cells with the character on the target cell (Shadow Step). The other character must be
    /// alive and displaceable (<see cref="Character.CanBeDisplaced"/>); when it died from the skill's damage, the
    /// caster moves into the freed cell instead. A swap is not a walk (see <see cref="GridController.SwapCharacters"/>);
    /// a prepared enemy strike follows its caster when it is swapped.
    /// </summary>
    [Serializable]
    public sealed class SwapSkillEffect : SkillEffect
    {
        public override bool CanUse(in SkillContext context)
        {
            var other = GetOther(context);
            return other != null && other.CanBeDisplaced;
        }

        public override void Apply(in SkillContext context)
        {
            var caster = context.Caster;
            if (caster == null || caster.IsDead) return;

            var grid = context.Grid;
            var target = context.TargetPos;
            var other = GetOther(context);
            if (other != null)
            {
                if (other.CanBeDisplaced)
                    grid.SwapCharacters(caster, other, caster);
                return;
            }

            // The target died from the damage: the caster takes its cell.
            if (grid.IsValidPosition(target) && grid.IsFreePosition(target))
                grid.DisplaceEntity(caster, target);
        }

        private static Character GetOther(in SkillContext context)
        {
            var grid = context.Grid;
            if (!grid.IsValidPosition(context.TargetPos)) return null;

            var other = grid.GetContent(context.TargetPos) as Character;
            return other != null && !other.IsDead && other != context.Caster ? other : null;
        }
    }

    /// <summary>
    /// The caster dashes in a straight line (one of the 8 directions, toward the aimed cell): it stops on the aimed
    /// cell when it is free, or on the last free cell before it (next to the target when one stands there, before
    /// an obstacle or another character on the way). A dash is not a walk (<see cref="Character.MovedThisTurn"/>
    /// does not change) and the cell it ends on reacts as when walking in. Put it before a Displace effect so the
    /// push starts from the new cell.
    /// </summary>
    [Serializable]
    public sealed class DashToTargetSkillEffect : SkillEffect
    {
        public override void Apply(in SkillContext context)
        {
            var caster = context.Caster;
            if (caster == null || caster.IsDead) return;

            var destination = GetDestination(context.Grid, caster.CurrentGridPos, context.TargetPos);
            if (destination != caster.CurrentGridPos)
                context.Grid.DisplaceEntity(caster, destination);
        }

        /// <summary>
        /// Where a dash from <paramref name="from"/> toward <paramref name="target"/> ends (pure; used to predict
        /// it too). The direction is the 8-neighbour direction closest to the target and the dash covers at most the
        /// Chebyshev distance to it.
        /// </summary>
        public static Vector2Int GetDestination(GridController grid, Vector2Int from, Vector2Int target)
        {
            var direction = Displacement.SnapDirection(target - from);
            if (direction == Vector2Int.zero) return from;

            var steps = Mathf.Max(Mathf.Abs(target.x - from.x), Mathf.Abs(target.y - from.y));
            var diagonal = direction.x != 0 && direction.y != 0;
            var position = from;
            for (var i = 0; i < steps; i++)
            {
                var next = position + direction;
                if (!grid.IsValidPosition(next) || !grid.IsFreePosition(next)) break;
                if (diagonal &&
                    !grid.IsWalkable(position + new Vector2Int(direction.x, 0)) &&
                    !grid.IsWalkable(position + new Vector2Int(0, direction.y)))
                    break;

                position = next;
            }

            return position;
        }

        /// <summary>The skill's dash effect, or null when it has none.</summary>
        public static DashToTargetSkillEffect Find(SkillDefinition skill)
        {
            if (skill == null) return null;

            foreach (var effect in skill.Effects)
            {
                if (effect is DashToTargetSkillEffect dash)
                    return dash;
            }

            return null;
        }
    }

    /// <summary>Which cells a <see cref="CreateTerrainSkillEffect"/> covers.</summary>
    public enum ETerrainPlacement
    {
        /// <summary>Only the aimed cell.</summary>
        TargetCell,

        /// <summary>Every cell of the skill's area.</summary>
        AreaCells,

        /// <summary>The caster's own cell (after any movement of the skill).</summary>
        CasterCell
    }

    /// <summary>
    /// Leaves terrain on the grid for a number of rounds (Fireball leaves Fire, Ice Wall raises walls, the Swamp
    /// Witch leaves a poison bog). By default only plain floor gets it; terrain that blocks movement is never put
    /// on an occupied cell. The terrain disappears when its rounds run out (<see cref="GridController.TickTemporaryTerrain"/>).
    /// </summary>
    [Serializable]
    public sealed class CreateTerrainSkillEffect : SkillEffect
    {
        [SerializeField]
        [Tooltip("Terrain left on the cells.")]
        private TerrainDefinition terrain;

        [SerializeField]
        [Tooltip("The aimed cell, every cell of the area, or the caster's cell.")]
        private ETerrainPlacement cells = ETerrainPlacement.TargetCell;

        [SerializeField]
        [Min(0)]
        [Tooltip("Rounds the terrain lasts (it disappears before the player's turn once they run out). 0 = the rest of the battle.")]
        private int turns = 2;

        [SerializeField]
        [Tooltip("Also replace terrain that is already there (off: only plain floor gets the new terrain).")]
        private bool replaceTerrain;

        public TerrainDefinition Terrain => terrain;
        public ETerrainPlacement Cells => cells;
        public int Turns => Mathf.Max(0, turns);
        public bool ReplaceTerrain => replaceTerrain;

        public override void Apply(in SkillContext context)
        {
            if (terrain == null) return;

            switch (cells)
            {
                case ETerrainPlacement.AreaCells:
                    foreach (var cell in context.AreaCells)
                        TryPlace(context.Grid, cell);
                    break;
                case ETerrainPlacement.CasterCell:
                    if (context.Caster != null)
                        TryPlace(context.Grid, context.Caster.CurrentGridPos);
                    break;
                default:
                    TryPlace(context.Grid, context.TargetPos);
                    break;
            }
        }

        /// <summary>Whether the terrain would go on <paramref name="cell"/> now (same rules as <see cref="Apply"/>).</summary>
        public bool CanPlace(GridController grid, Vector2Int cell)
        {
            if (terrain == null || !grid.IsValidPosition(cell)) return false;
            if (!replaceTerrain && grid.GetTerrain(cell) != null) return false;
            if (terrain.BlocksMovement && grid.GetContent(cell) != null) return false;
            return true;
        }

        private void TryPlace(GridController grid, Vector2Int cell)
        {
            if (CanPlace(grid, cell))
                grid.SetTerrain(cell, terrain, Turns);
        }
    }
}
