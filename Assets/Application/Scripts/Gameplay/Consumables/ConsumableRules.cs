using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>Why a consumable cannot be used right now (<see cref="ConsumableRules.GetUseBlock"/>).</summary>
    public enum EConsumableUseBlock
    {
        /// <summary>Nothing blocks it.</summary>
        None,

        /// <summary>There is no item (empty slot).</summary>
        NoItem,

        /// <summary>The user is dead.</summary>
        UserDead,

        /// <summary>It is not the player's turn.</summary>
        NotPlayerTurn,

        /// <summary>No battle is running (consumables are battle-only, GDD 2.8).</summary>
        NotInBattle,

        /// <summary>The turn's consumable limit was already reached (GDD 2.8: one per turn).</summary>
        UsesPerTurnReached
    }

    /// <summary>
    /// Rules for using consumables (GDD 2.8): when one can be used, where it can be
    /// aimed, its area and who it reaches. Pure static functions over the grid and
    /// the characters, shared by the player's input, the highlights and
    /// <see cref="ConsumableExecutor"/>. Area shapes and the range metric are the
    /// skills' (<see cref="SkillArea"/>, <c>CombatSettings.SkillRangeMetric</c>).
    /// </summary>
    public static class ConsumableRules
    {
        /// <summary>
        /// Whether a battle is running on the grid: it has not been decided yet (victory or
        /// defeat). Without a grid there is no battle.
        /// </summary>
        public static bool IsBattleActive(GridController grid)
        {
            if (grid == null) return false;

            return !(grid.TryGetComponent(out BattleController battle) && battle.IsOver);
        }

        /// <summary>
        /// Why the item cannot be used (or selected) right now, wherever it is aimed: no item, dead
        /// user, not the player's turn, no battle, or the per-turn limit reached.
        /// </summary>
        public static EConsumableUseBlock GetUseBlock(GridController grid, PlayerCharacter user,
            ConsumableDefinition consumable, bool isPlayerTurn, ConsumableSettings settings = null)
        {
            if (settings == null)
                settings = ConsumableSettings.Current;

            if (consumable == null) return EConsumableUseBlock.NoItem;
            if (user == null || user.IsDead) return EConsumableUseBlock.UserDead;
            if (!isPlayerTurn) return EConsumableUseBlock.NotPlayerTurn;
            if (settings.OnlyInBattle && !IsBattleActive(grid)) return EConsumableUseBlock.NotInBattle;
            if (user.ConsumableUsesThisTurn >= settings.MaxUsesPerTurn) return EConsumableUseBlock.UsesPerTurnReached;

            return EConsumableUseBlock.None;
        }

        /// <summary>
        /// Whether the item can be selected at all right now (see <see cref="GetUseBlock"/>); what lets
        /// an item button be pressed.
        /// </summary>
        public static bool CanSelect(GridController grid, PlayerCharacter user, ConsumableDefinition consumable,
            bool isPlayerTurn, ConsumableSettings settings = null)
        {
            return GetUseBlock(grid, user, consumable, isPlayerTurn, settings) == EConsumableUseBlock.None;
        }

        /// <summary>
        /// Whether the target cell is close enough to the user. Items that target the user and
        /// self-centered ones (range 0) only accept the user's own cell; unlimited ones accept any cell.
        /// Does not check the grid bounds.
        /// </summary>
        public static bool IsTargetInRange(Character user, ConsumableDefinition consumable, Vector2Int targetPos)
        {
            if (!consumable.NeedsTarget) return targetPos == user.CurrentGridPos;
            if (consumable.UnlimitedRange) return true;
            if (consumable.IsSelfCentered) return targetPos == user.CurrentGridPos;

            return GridDistance.IsWithin(CombatResolver.Settings.SkillRangeMetric, user.CurrentGridPos, targetPos,
                consumable.Range);
        }

        /// <summary>
        /// Cells of the item's area (inside the grid) when aimed at <paramref name="targetPos"/>. Items that
        /// target the user cover only the user's cell.
        /// </summary>
        public static List<Vector2Int> GetAreaCells(GridController grid, Character user,
            ConsumableDefinition consumable, Vector2Int targetPos)
        {
            if (!consumable.NeedsTarget)
                return new List<Vector2Int> { user.CurrentGridPos };

            return SkillArea.GetCells(consumable.AreaShape, consumable.AreaSize, user.CurrentGridPos, targetPos,
                grid.IsValidPosition);
        }

        /// <summary>
        /// Living characters the item reaches: for items that target the user, only the user; for Cell items
        /// the characters inside the area that pass the target filter, in cell order (row, then column).
        /// </summary>
        public static List<Character> GetAffectedCharacters(GridController grid, Character user,
            ConsumableDefinition consumable, IReadOnlyList<Vector2Int> areaCells)
        {
            var affected = new List<Character>();
            if (!consumable.NeedsTarget)
            {
                if (!user.IsDead)
                    affected.Add(user);
                return affected;
            }

            foreach (var cell in areaCells)
            {
                if (grid.GetContent(cell) is Character character && !character.IsDead &&
                    SkillTargeting.IsAffected(user, character, consumable.Affects))
                    affected.Add(character);
            }

            return affected;
        }

        /// <summary>Resolves the area and the affected characters of a use, before any effect.</summary>
        public static ConsumableContext CreateContext(GridController grid, Character user,
            ConsumableDefinition consumable, Vector2Int targetPos)
        {
            if (!consumable.NeedsTarget)
                targetPos = user.CurrentGridPos;

            var cells = GetAreaCells(grid, user, consumable, targetPos);
            var affected = GetAffectedCharacters(grid, user, consumable, cells);
            return new ConsumableContext(user, grid, consumable, targetPos, cells, affected);
        }

        /// <summary>
        /// Whether the target cell is a valid aim for the item: inside the grid, in range and at least one
        /// effect would change something (e.g. a damage item needs a character in the area). Ignores the turn
        /// and the per-turn limit (see <see cref="GetUseBlock"/>).
        /// </summary>
        public static bool IsValidTarget(GridController grid, Character user, ConsumableDefinition consumable,
            Vector2Int targetPos)
        {
            if (consumable.NeedsTarget && !grid.IsValidPosition(targetPos)) return false;
            if (!IsTargetInRange(user, consumable, targetPos)) return false;

            return consumable.IsUseful(CreateContext(grid, user, consumable, targetPos));
        }

        /// <summary>
        /// Full use check: <see cref="GetUseBlock"/> is clear and the target is valid. Items that target the
        /// user ignore <paramref name="targetPos"/>.
        /// </summary>
        public static bool CanUse(GridController grid, PlayerCharacter user, ConsumableDefinition consumable,
            Vector2Int targetPos, bool isPlayerTurn, ConsumableSettings settings = null)
        {
            return CanSelect(grid, user, consumable, isPlayerTurn, settings) &&
                   IsValidTarget(grid, user, consumable, targetPos);
        }

        /// <summary>
        /// Every cell the item can currently be aimed at (valid targets, see <see cref="IsValidTarget"/>),
        /// in row-then-column order. For items that target the user, just the user's cell.
        /// </summary>
        public static List<Vector2Int> GetTargetableCells(GridController grid, Character user,
            ConsumableDefinition consumable)
        {
            var cells = new List<Vector2Int>();
            if (!consumable.NeedsTarget)
            {
                if (IsValidTarget(grid, user, consumable, user.CurrentGridPos))
                    cells.Add(user.CurrentGridPos);
                return cells;
            }

            var size = SkillTargeting.MeasureGrid(grid);
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsValidTarget(grid, user, consumable, cell))
                        cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>
        /// Highlights for an item being aimed: every cell it can be used on, marked as
        /// <see cref="ECellHighlightType.SkillRange"/>.
        /// </summary>
        public static Dictionary<Vector2Int, ECellHighlightType> GetHighlightInfos(GridController grid,
            Character user, ConsumableDefinition consumable)
        {
            var highlightInfos = new Dictionary<Vector2Int, ECellHighlightType>();
            foreach (var cell in GetTargetableCells(grid, user, consumable))
                highlightInfos[cell] = ECellHighlightType.SkillRange;

            return highlightInfos;
        }
    }
}
