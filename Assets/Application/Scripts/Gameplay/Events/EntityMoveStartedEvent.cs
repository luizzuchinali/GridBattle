using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by GridMovementAnimator when an entity changes cell, right when its
    /// move animation starts (also when there is no animation configured). The
    /// logical move already happened; this is for presentation (step sound, dust...).
    /// </summary>
    public class EntityMoveStartedEvent
    {
        public GridEntity Entity { get; }
        public Vector2Int FromCell { get; }
        public Vector2Int ToCell { get; }

        public EntityMoveStartedEvent(GridEntity entity, Vector2Int fromCell, Vector2Int toCell)
        {
            Entity = entity;
            FromCell = fromCell;
            ToCell = toCell;
        }
    }
}
