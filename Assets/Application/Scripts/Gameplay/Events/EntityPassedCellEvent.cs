using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by GridController.WalkEntity for each cell a walking entity crosses on its way, in order, before the
    /// <see cref="EntityEnteredCellEvent"/> of the destination (the destination itself is not a passed cell). The
    /// logical move already happened; the hops only follow. Hook for effects that trigger when a character walks
    /// over a cell; nothing reacts to it yet (terrain still only reacts to the destination).
    /// </summary>
    public class EntityPassedCellEvent
    {
        public GridEntity Entity { get; }
        public Vector2Int Cell { get; }

        /// <summary>Position of the cell in the walk (0 = the first cell after the start).</summary>
        public int StepIndex { get; }

        public EntityPassedCellEvent(GridEntity entity, Vector2Int cell, int stepIndex)
        {
            Entity = entity;
            Cell = cell;
            StepIndex = stepIndex;
        }
    }
}
