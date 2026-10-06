using GridBattle.Gameplay;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// The item bar (GDD 4.2), above the skill bar: one circular button per inventory slot with the
    /// consumable's icon, a disabled look when it cannot be used now
    /// (<see cref="ConsumableRules.GetUseBlock"/>) and a selected highlight while an item that needs a
    /// target is being aimed. A tap goes to <c>PlayerCharacterController.SelectConsumable</c>.
    /// </summary>
    public sealed class ItemBarView : SlotBarView
    {
        private GridController _grid;

        public ItemBarView(VisualElement container) : base(container, true)
        {
            Watch<ConsumableInventoryChangedEvent>(_ => Refresh());
            Watch<ConsumableSelectionChangedEvent>(e => RefreshIfPlayer(e.Owner));
            Watch<ConsumableUsedEvent>(e => RefreshIfPlayer(e.User));
            Watch<GlobalTurnStartedEvent>(_ => Refresh());
            Watch<BattleDecidedEvent>(_ => Refresh());
            Refresh();
        }

        protected override int GetSlotCount()
        {
            return Player != null ? Player.Inventory.Slots : ConsumableSettings.Current.Slots;
        }

        protected override void RefreshSlot(int index, HudSlot slot)
        {
            var item = Player != null ? Player.Inventory.Get(index) : null;
            if (item == null)
            {
                slot.Set(null, string.Empty, false, false, true);
                return;
            }

            if (_grid == null)
                _grid = UnityEngine.Object.FindAnyObjectByType<GridController>();

            var selected = Controller != null && Controller.SelectedConsumableSlot == index;
            var disabled = IsInactive ||
                           ConsumableRules.GetUseBlock(_grid, Player, item, IsPlayerTurn) != EConsumableUseBlock.None;
            slot.Set(item.Icon, string.Empty, selected, disabled && !selected, false);
        }

        protected override void OnSlotTapped(int index)
        {
            if (Controller != null)
                Controller.SelectConsumable(index);
        }

        private void RefreshIfPlayer(Character character)
        {
            if (character is PlayerCharacter)
                Refresh();
        }
    }
}
