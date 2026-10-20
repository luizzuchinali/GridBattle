using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>
    /// Pushes or pulls the affected characters (Impact Grenade), with the same rules as a skill's Displace effect
    /// (<see cref="DisplacementResolver"/>: collisions, terrain, the user's rule talents). The user counts as the
    /// caster: collision damage is attributed to it and "away from the caster" starts from its cell.
    /// </summary>
    [Serializable]
    public sealed class DisplaceConsumableEffect : ConsumableEffect
    {
        [SerializeField]
        [Tooltip("Away from the user, toward the user (a pull: it stops next to the user) or away from the center " +
                 "of the item's area (the aimed cell).")]
        private EDisplacementMode mode = EDisplacementMode.AwayFromAreaCenter;

        [SerializeField]
        [Min(1)]
        [Tooltip("Cells each target is moved (it stops earlier when something is in the way).")]
        private int distance = 1;

        public EDisplacementMode Mode => mode;
        public int Distance => Mathf.Max(1, distance);

        public override bool IsUseful(in ConsumableContext context)
        {
            foreach (var character in context.Affected)
            {
                if (character != null && !character.IsDead && character.CanBeDisplaced)
                    return true;
            }

            return false;
        }

        public override void Apply(in ConsumableContext context)
        {
            var targets = new List<Character>();
            foreach (var character in context.Affected)
            {
                if (character != null && !character.IsDead)
                    targets.Add(character);
            }

            DisplacementResolver.Apply(context.Grid, context.User, targets, mode, Distance, context.TargetPos);
        }
    }

    /// <summary>
    /// The user trades cells with the character on the aimed cell (Swap Scroll), like the Shadow Step skill
    /// (<see cref="GridController.SwapCharacters"/>): the other character must be alive and displaceable. A swap is
    /// not a walk, and a prepared enemy strike follows its caster.
    /// </summary>
    [Serializable]
    public sealed class SwapConsumableEffect : ConsumableEffect
    {
        public override bool IsUseful(in ConsumableContext context)
        {
            var other = GetOther(context);
            return other != null && other.CanBeDisplaced;
        }

        public override void Apply(in ConsumableContext context)
        {
            var user = context.User;
            if (user == null || user.IsDead) return;

            var other = GetOther(context);
            if (other != null && other.CanBeDisplaced)
                context.Grid.SwapCharacters(user, other, user);
        }

        /// <summary>The living character on the aimed cell other than the user, or null.</summary>
        public static Character GetOther(in ConsumableContext context)
        {
            var grid = context.Grid;
            if (grid == null || !grid.IsValidPosition(context.TargetPos)) return null;

            var other = grid.GetContent(context.TargetPos) as Character;
            return other != null && !other.IsDead && other != context.User ? other : null;
        }
    }

    /// <summary>
    /// Leaves terrain for a number of rounds (Stone raises a rock, Fire Flask sets the area on fire), with the same
    /// rules as <see cref="CreateTerrainSkillEffect"/>: by default only plain floor gets it, and terrain that blocks
    /// movement never goes on an occupied cell. The item is only usable when at least one cell would get it.
    /// </summary>
    [Serializable]
    public sealed class CreateTerrainConsumableEffect : ConsumableEffect
    {
        [SerializeField]
        [Tooltip("Terrain left on the cells.")]
        private TerrainDefinition terrain;

        [SerializeField]
        [Tooltip("The aimed cell, every cell of the area, or the user's cell.")]
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

        public override bool IsUseful(in ConsumableContext context)
        {
            foreach (var cell in GetCells(context))
            {
                if (CanPlace(context.Grid, cell))
                    return true;
            }

            return false;
        }

        public override void Apply(in ConsumableContext context)
        {
            foreach (var cell in GetCells(context))
            {
                if (CanPlace(context.Grid, cell))
                    context.Grid.SetTerrain(cell, terrain, Turns);
            }
        }

        /// <summary>Whether the terrain would go on <paramref name="cell"/> now.</summary>
        public bool CanPlace(GridController grid, Vector2Int cell)
        {
            if (terrain == null || grid == null || !grid.IsValidPosition(cell)) return false;
            if (!replaceTerrain && grid.GetTerrain(cell) != null) return false;
            if (terrain.BlocksMovement && grid.GetContent(cell) != null) return false;
            return true;
        }

        private IEnumerable<Vector2Int> GetCells(ConsumableContext context)
        {
            switch (cells)
            {
                case ETerrainPlacement.AreaCells:
                    foreach (var cell in context.AreaCells)
                        yield return cell;
                    break;
                case ETerrainPlacement.CasterCell:
                    if (context.User != null)
                        yield return context.User.CurrentGridPos;
                    break;
                default:
                    yield return context.TargetPos;
                    break;
            }
        }
    }
}
