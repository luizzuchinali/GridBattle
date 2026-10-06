using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the player's controller when the consumable being aimed changes: an
    /// item that needs a target cell was selected (the grid shows its range) or the
    /// selection was cleared (cancel, item used, turn change, a skill was selected).
    /// The item bar highlights the selected button from this. Items used immediately
    /// (no target) never become selected.
    /// </summary>
    public class ConsumableSelectionChangedEvent
    {
        public Character Owner { get; }

        /// <summary>Inventory slot of the selected item, or -1 when the selection was cleared.</summary>
        public int Slot { get; }

        /// <summary>The selected item, or null when the selection was cleared.</summary>
        public ConsumableDefinition Consumable { get; }

        public bool HasSelection => Consumable != null;

        public ConsumableSelectionChangedEvent(Character owner, int slot, ConsumableDefinition consumable)
        {
            Owner = owner;
            Slot = slot;
            Consumable = consumable;
        }
    }
}
