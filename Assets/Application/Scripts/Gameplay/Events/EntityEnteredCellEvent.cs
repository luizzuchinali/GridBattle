using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by GridController.Move right after an entity logically changes cell
    /// (occupancy and CurrentGridPos already updated; the hop animation only
    /// follows). Hook for terrain effects that trigger on entering a cell.
    /// </summary>
    public class EntityEnteredCellEvent
    {
        public GridEntity Entity { get; }
        public Vector2Int FromCell { get; }
        public Vector2Int ToCell { get; }

        public EntityEnteredCellEvent(GridEntity entity, Vector2Int fromCell, Vector2Int toCell)
        {
            Entity = entity;
            FromCell = fromCell;
            ToCell = toCell;
        }
    }
}
