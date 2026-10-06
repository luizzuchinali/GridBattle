using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Managers.Audio;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>
    /// Uses a carried consumable: validates it (<see cref="ConsumableRules"/>), takes it out of the
    /// inventory, counts it against the turn's limit, applies its effects and raises
    /// <see cref="ConsumableUsedEvent"/>. Per GDD 2.8 using an item does <b>not</b> consume the
    /// turn's action: <c>PlayerActionEvent</c> is never raised here, so the player can still move,
    /// attack or use a skill afterwards. Game state changes immediately; the sound and the cell flash
    /// only follow.
    /// </summary>
    public static class ConsumableExecutor
    {
        /// <summary>
        /// Uses the item in <paramref name="slot"/> of the player's inventory, aimed at
        /// <paramref name="targetPos"/> (ignored by items that target the user). Returns false, changing
        /// nothing, when the slot is empty, it is not the player's turn, no battle is running, the turn's
        /// limit was reached or the target is invalid.
        /// </summary>
        public static bool TryUse(GridController grid, PlayerCharacter user, int slot, Vector2Int targetPos,
            bool isPlayerTurn)
        {
            if (grid == null || user == null) return false;

            var inventory = user.Inventory;
            var consumable = inventory.Get(slot);
            if (consumable == null) return false;

            if (!consumable.NeedsTarget)
                targetPos = user.CurrentGridPos;

            if (!ConsumableRules.CanUse(grid, user, consumable, targetPos, isPlayerTurn)) return false;

            // Resolved before anything changes, so deaths during the use do not alter it.
            var context = ConsumableRules.CreateContext(grid, user, consumable, targetPos);

            user.RegisterConsumableUse();
            inventory.RemoveAt(slot);
            consumable.Execute(context);

            EventBus.Raise(new ConsumableUsedEvent(user, consumable, context.TargetPos, context.AreaCells,
                context.Affected));
            AudioManager.Play(ESfx.ConsumableUse);

            if (consumable.NeedsTarget)
                FlashCells(context.AreaCells);

            return true;
        }

        /// <summary>Visual only: the cells of an area item flash like a skill's area does.</summary>
        private static void FlashCells(IReadOnlyList<Vector2Int> areaCells)
        {
            foreach (var cell in Object.FindObjectsByType<Cell>(FindObjectsInactive.Exclude))
            {
                foreach (var position in areaCells)
                {
                    if (position != cell.GridPosition) continue;

                    cell.PlaySkillFlash();
                    break;
                }
            }
        }
    }
}
