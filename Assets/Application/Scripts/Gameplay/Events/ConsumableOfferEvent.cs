using System.Collections.Generic;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when a consumable node needs a decision from the player: several options to
    /// choose from, and/or the inventory is full under <see cref="EFullInventoryPolicy.AskPlayer"/>. The UI
    /// answers with <c>RunManager.ChooseConsumableOffer(index)</c>, <c>ReplaceConsumable(slot)</c> or
    /// <c>DeclineConsumableOffer()</c>.
    /// </summary>
    public class ConsumableOfferEvent
    {
        public MapNodeState Node { get; }
        public IReadOnlyList<ConsumableDefinition> Options { get; }

        /// <summary>
        /// No free slot: taking an item means discarding one the player carries (the UI asks which slot, then calls
        /// <c>ReplaceConsumable</c>).
        /// </summary>
        public bool InventoryFull { get; }

        public ConsumableOfferEvent(MapNodeState node, IReadOnlyList<ConsumableDefinition> options, bool inventoryFull)
        {
            Node = node;
            Options = options;
            InventoryFull = inventoryFull;
        }
    }
}
