using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the player's controller when the selected skill or item is aimed at a cell (waiting for the
    /// confirmation) or the aim is dropped (<see cref="Cell"/> null). The forecast view previews the aimed action and
    /// the HUD shows the Confirm button.
    /// </summary>
    public class ActionAimChangedEvent
    {
        public Character Owner { get; }

        /// <summary>The aimed cell, or null when nothing is aimed.</summary>
        public Vector2Int? Cell { get; }

        public ActionAimChangedEvent(Character owner, Vector2Int? cell)
        {
            Owner = owner;
            Cell = cell;
        }
    }
}
