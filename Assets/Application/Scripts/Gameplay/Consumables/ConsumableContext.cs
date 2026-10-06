using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>
    /// Everything a <see cref="ConsumableEffect"/> needs about one use: who used the
    /// item, where it was aimed, the cells of its area (inside the grid) and the
    /// characters its target filter lets it reach. Resolved before any effect runs,
    /// so deaths during the use do not change it. For items that target the user
    /// the area is the user's own cell and the user is the only affected character.
    /// </summary>
    public readonly struct ConsumableContext
    {
        public Character User { get; }
        public GridController Grid { get; }
        public ConsumableDefinition Consumable { get; }
        public Vector2Int TargetPos { get; }

        /// <summary>Cells of the area of effect that are inside the grid, sorted by row then column.</summary>
        public IReadOnlyList<Vector2Int> AreaCells { get; }

        /// <summary>Living characters inside the area that pass the item's target filter, in cell order.</summary>
        public IReadOnlyList<Character> Affected { get; }

        public ConsumableContext(Character user, GridController grid, ConsumableDefinition consumable,
            Vector2Int targetPos, IReadOnlyList<Vector2Int> areaCells, IReadOnlyList<Character> affected)
        {
            User = user;
            Grid = grid;
            Consumable = consumable;
            TargetPos = targetPos;
            AreaCells = areaCells;
            Affected = affected;
        }
    }
}
