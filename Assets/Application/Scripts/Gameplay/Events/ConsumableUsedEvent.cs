using System.Collections.Generic;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="ConsumableExecutor"/> when a consumable is used, after
    /// it left the inventory and its effects were applied. Using an item does not
    /// consume the turn's action, so no <c>PlayerActionEvent</c> follows. Hook for
    /// item VFX and statistics (the sound and the cell flash are played by the
    /// executor).
    /// </summary>
    public class ConsumableUsedEvent
    {
        public Character User { get; }
        public ConsumableDefinition Consumable { get; }

        /// <summary>Cell the item was aimed at (the user's own cell for items without a target).</summary>
        public Vector2Int TargetPos { get; }

        /// <summary>Cells of the area of effect (inside the grid), sorted by row then column.</summary>
        public IReadOnlyList<Vector2Int> AreaCells { get; }

        /// <summary>Characters the item reached (resolved before the effects).</summary>
        public IReadOnlyList<Character> Affected { get; }

        public ConsumableUsedEvent(Character user, ConsumableDefinition consumable, Vector2Int targetPos,
            IReadOnlyList<Vector2Int> areaCells, IReadOnlyList<Character> affected)
        {
            User = user;
            Consumable = consumable;
            TargetPos = targetPos;
            AreaCells = areaCells;
            Affected = affected;
        }
    }
}
