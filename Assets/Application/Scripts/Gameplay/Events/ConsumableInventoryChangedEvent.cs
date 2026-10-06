using GridBattle.Gameplay.Consumables;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised whenever the carried consumables change (item added, used, replaced,
    /// discarded) and when a player character gets another inventory
    /// (<c>PlayerCharacter.SetInventory</c>, a new battle). The item bar redraws its
    /// buttons from <see cref="Inventory"/>.
    /// </summary>
    public class ConsumableInventoryChangedEvent
    {
        public ConsumableInventory Inventory { get; }

        public ConsumableInventoryChangedEvent(ConsumableInventory inventory)
        {
            Inventory = inventory;
        }
    }
}
