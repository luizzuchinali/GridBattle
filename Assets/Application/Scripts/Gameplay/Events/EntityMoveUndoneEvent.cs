using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <c>GridController.UndoMove</c> when a walk of the current turn is taken back (occupancy, position and
    /// the movement flag already restored). Terrain does not react to it.
    /// </summary>
    public class EntityMoveUndoneEvent
    {
        public Character Character { get; }
        public Vector2Int FromCell { get; }
        public Vector2Int ToCell { get; }

        public EntityMoveUndoneEvent(Character character, Vector2Int fromCell, Vector2Int toCell)
        {
            Character = character;
            FromCell = fromCell;
            ToCell = toCell;
        }
    }
}
