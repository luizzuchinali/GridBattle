using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Controllers
{
    /// <summary>
    /// Routes the player's input. With no skill selected a tap walks or attacks;
    /// with a skill selected (<see cref="SelectSkill"/>, from the skill bar) the
    /// grid shows the cells the skill can be aimed at and a tap on one of them uses
    /// it. Consumables (item bar, GDD 2.8) work the same way: an item without a target is
    /// used on selection, one with a target highlights its range and a tap on a cell uses
    /// it; using one never consumes the turn's action. Rules live in <see cref="GridRules"/>,
    /// <see cref="SkillTargeting"/> and <see cref="ConsumableRules"/>; effects in
    /// <see cref="Character"/>, <see cref="SkillDefinition"/> and <see cref="ConsumableDefinition"/>.
    /// The cell highlights are event-driven: everything that can change them marks them dirty and
    /// they are recomputed at most once per frame, and only while dirty.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : CharacterControllerBase<PlayerCharacter>
    {
        private static readonly Dictionary<Vector2Int, ECellHighlightType> NoHighlights = new();

        private readonly List<Action> _unsubscribers = new();
        private bool _isMyTurn = true;
        private bool _highlightsDirty = true;

        /// <summary>The skill being aimed, or null when no skill is selected.</summary>
        public SkillDefinition SelectedSkill { get; private set; }

        /// <summary>Inventory slot of the consumable being aimed, or -1 when none is selected.</summary>
        public int SelectedConsumableSlot { get; private set; } = -1;

        /// <summary>The consumable being aimed, or null when none is selected.</summary>
        public ConsumableDefinition SelectedConsumable { get; private set; }

        /// <summary>
        /// How many times the highlights were recomputed (diagnostics: stays still while nothing changes).
        /// </summary>
        public int HighlightRefreshCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);

            // Everything that can change which cells are walkable, attackable or aimable.
            WatchHighlights<EntityEnteredCellEvent>();
            WatchHighlights<CharacterDiedEvent>();
            WatchHighlights<EnemySummonedEvent>();
            WatchHighlights<StateAppliedEvent>();
            WatchHighlights<StateRemovedEvent>();
            WatchHighlights<SkillSelectionChangedEvent>();
            WatchHighlights<SkillCooldownsChangedEvent>();
            WatchHighlights<SkillListChangedEvent>();
            WatchHighlights<ConsumableSelectionChangedEvent>();
            WatchHighlights<ConsumableInventoryChangedEvent>();
            WatchHighlights<ConsumableUsedEvent>();
            WatchHighlights<GlobalTurnStartedEvent>();
            WatchHighlights<GridInitializedEvent>();
            WatchHighlights<BattleDecidedEvent>();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
            EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
            foreach (var unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();

            if (SelectedSkill != null)
                CancelSkillSelection();
            if (SelectedConsumable != null)
                CancelConsumableSelection();
        }

        /// <summary>Marks the highlights dirty whenever a <typeparamref name="TEvent"/> is raised.</summary>
        private void WatchHighlights<TEvent>()
        {
            Action<TEvent> handler = _ => _highlightsDirty = true;
            EventBus.Subscribe(handler);
            _unsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void Update()
        {
            if (!_isMyTurn || !_highlightsDirty) return;

            RefreshHighlights();
        }

        /// <summary>
        /// Recomputes the highlights now (cancelling a selection that stopped being valid) and applies
        /// them to the grid. Runs only when something marked them dirty, at most once per frame.
        /// </summary>
        private void RefreshHighlights()
        {
            HighlightRefreshCount++;

            if (SelectedConsumable != null && !IsConsumableSelectionValid())
                CancelConsumableSelection();
            if (SelectedSkill != null && !SkillTargeting.CanSelect(Owner, SelectedSkill))
                CancelSkillSelection();

            Grid.HighlightCells(ComputeHighlights());

            // The cancellations above raise events that mark the flag again: the result is already up to date.
            _highlightsDirty = false;
        }

        /// <summary>The item can stop being selectable while aimed (replaced or removed from its slot, no battle).</summary>
        private bool IsConsumableSelectionValid()
        {
            return Owner.Inventory.Get(SelectedConsumableSlot) == SelectedConsumable &&
                   ConsumableRules.CanSelect(Grid, Owner, SelectedConsumable, _isMyTurn);
        }

        /// <summary>
        /// The highlights for the current state, computed from scratch: the aimed item's range, else the
        /// aimed skill's range, else where the player can walk or attack. Pure query (no selection is
        /// changed, nothing is applied); tests compare it with what the cells show.
        /// </summary>
        public Dictionary<Vector2Int, ECellHighlightType> ComputeHighlights()
        {
            if (SelectedConsumable != null)
                return ConsumableRules.GetHighlightInfos(Grid, Owner, SelectedConsumable);

            if (SelectedSkill != null)
                return GridRules.GetSkillHighlightInfos(Grid, Owner, SelectedSkill);

            return GridRules.GetHighlightInfos(Grid, Owner);
        }

        /// <summary>
        /// The player only acts, and only sees highlights, on its own turn. Any turn
        /// change drops the skill and consumable selections.
        /// </summary>
        private void OnTurnChanged(TurnChangedEvent e)
        {
            CancelSkillSelection();
            CancelConsumableSelection();

            _isMyTurn = e.IsPlayerTurn;
            _highlightsDirty = true;
            if (!_isMyTurn)
                Grid.HighlightCells(NoHighlights);
        }

        /// <summary>
        /// Selects <paramref name="skill"/> for aiming (called by the skill bar). The
        /// grid then highlights the cells it can be used on. Selecting the skill that
        /// is already selected cancels it (GDD 4.1), and selecting another one switches.
        /// Ignored (returns false) when it is not the player's turn, the skill is not
        /// one of the character's, it is on cooldown or the player is silenced.
        /// Returns true if the selection changed.
        /// </summary>
        public bool SelectSkill(SkillDefinition skill)
        {
            if (!_isMyTurn || GameplayInput.IsBlocked || skill == null) return false;

            if (SelectedSkill == skill)
            {
                CancelSkillSelection();
                return true;
            }

            if (!OwnsSkill(skill) || !SkillTargeting.CanSelect(Owner, skill)) return false;

            // Aiming a skill replaces aiming an item.
            CancelConsumableSelection();
            SelectedSkill = skill;
            EventBus.Raise(new SkillSelectionChangedEvent(Owner, skill));
            return true;
        }

        /// <summary>Clears the skill selection (back to walking and attacking).</summary>
        public void CancelSkillSelection()
        {
            if (SelectedSkill == null) return;

            SelectedSkill = null;
            EventBus.Raise(new SkillSelectionChangedEvent(Owner, null));
        }

        /// <summary>
        /// Selects the consumable in <paramref name="slot"/> (called by the item bar). An item without a
        /// target is used right away; one with a target enters aiming mode: the grid highlights the cells it
        /// can be aimed at and a tap on one uses it. Selecting the slot that is already selected cancels it,
        /// and selecting another one switches. Aiming an item cancels a selected skill. Using an item does not
        /// consume the turn's action (GDD 2.8). Ignored (returns false) when the slot is empty, it is not the
        /// player's turn or the turn's consumable limit was reached. Returns true if the selection changed or
        /// the item was used.
        /// </summary>
        public bool SelectConsumable(int slot)
        {
            if (!_isMyTurn || GameplayInput.IsBlocked) return false;

            if (SelectedConsumable != null && SelectedConsumableSlot == slot)
            {
                CancelConsumableSelection();
                return true;
            }

            var consumable = Owner.Inventory.Get(slot);
            if (consumable == null || !ConsumableRules.CanSelect(Grid, Owner, consumable, _isMyTurn)) return false;

            if (!consumable.NeedsTarget)
                return TryUseConsumable(slot, Owner.CurrentGridPos);

            CancelSkillSelection();
            CancelConsumableSelection();
            SelectedConsumableSlot = slot;
            SelectedConsumable = consumable;
            EventBus.Raise(new ConsumableSelectionChangedEvent(Owner, slot, consumable));
            return true;
        }

        /// <summary>Clears the consumable selection (back to walking and attacking).</summary>
        public void CancelConsumableSelection()
        {
            if (SelectedConsumable == null) return;

            SelectedConsumableSlot = -1;
            SelectedConsumable = null;
            EventBus.Raise(new ConsumableSelectionChangedEvent(Owner, -1, null));
        }

        /// <summary>
        /// Uses the consumable in <paramref name="slot"/> directly on a target cell, without going through
        /// the selection (UI shortcuts, tests). Validation and effects live in
        /// <see cref="ConsumableRules"/>/<see cref="ConsumableExecutor"/>, not here. Returns true if the item
        /// was used; the turn's action is not consumed, so no <see cref="PlayerActionEvent"/> is raised.
        /// </summary>
        public bool TryUseConsumable(int slot, Vector2Int targetPos)
        {
            if (!ConsumableExecutor.TryUse(Grid, Owner, slot, targetPos, _isMyTurn)) return false;

            // Using an item replaces any aiming in progress.
            CancelConsumableSelection();
            CancelSkillSelection();
            return true;
        }

        private bool OwnsSkill(SkillDefinition skill)
        {
            foreach (var owned in Owner.Skills)
            {
                if (owned == skill) return true;
            }

            return false;
        }

        private void OnCellTap(CellTapEvent @event)
        {
            // A window is open (entity details, talent choice, pause...): the grid behind it does nothing.
            if (GameplayInput.IsBlocked) return;

            if (!_isMyTurn)
            {
                @event.Cell.PlayRejectFeedback();
                return;
            }

            if (SelectedConsumable != null)
            {
                TapWithConsumable(@event.Cell);
                return;
            }

            if (SelectedSkill != null)
            {
                TapWithSkill(@event.Cell);
                return;
            }

            if (!@event.Cell.HasContent)
            {
                if (!GridRules.CanWalkTo(Grid, Owner, @event.Cell.GridPosition))
                {
                    @event.Cell.PlayRejectFeedback();
                    return;
                }

                Grid.MoveEntity(Owner, @event.Cell.GridPosition);
            }
            else
            {
                if (!GridRules.IsAttackTarget(Grid, Owner, @event.Cell.GridPosition))
                {
                    @event.Cell.PlayRejectFeedback();
                    return;
                }

                var target = @event.Cell.GetContent();
                if (target is IDamageReceiver receiver)
                {
                    Grid.PlayAttackAnimation(Owner, @event.Cell.GridPosition);
                    Owner.Attack(receiver);
                }
            }

            EventBus.Raise<PlayerActionEvent>();
        }

        /// <summary>
        /// A tap while a consumable is selected: a valid cell uses the item (clearing the selection, the
        /// action is not consumed); an invalid one is rejected and the selection stays so the player can try
        /// another cell.
        /// </summary>
        private void TapWithConsumable(Cell cell)
        {
            if (!TryUseConsumable(SelectedConsumableSlot, cell.GridPosition))
                cell.PlayRejectFeedback();
        }

        /// <summary>
        /// A tap while a skill is selected: a valid cell uses the skill (consuming the
        /// action and clearing the selection); an invalid one is rejected and the
        /// selection stays so the player can try another cell.
        /// </summary>
        private void TapWithSkill(Cell cell)
        {
            // TryUseSkill validates (range, area, cooldown, restrictions) before doing anything.
            if (!Owner.TryUseSkill(Grid, SelectedSkill, cell.GridPosition))
            {
                cell.PlayRejectFeedback();
                return;
            }

            CancelSkillSelection();
            EventBus.Raise<PlayerActionEvent>();
        }

        /// <summary>
        /// Uses a skill directly on a target cell, without going through the selection
        /// (UI shortcuts, tests). Validation and effect live in SkillDefinition /
        /// Character, not here. Returns true if the skill was used (the action is
        /// consumed).
        /// </summary>
        public bool TryUseSkill(SkillDefinition skill, Vector2Int targetPos)
        {
            if (!_isMyTurn) return false;
            if (!Owner.TryUseSkill(Grid, skill, targetPos)) return false;

            CancelSkillSelection();
            EventBus.Raise<PlayerActionEvent>();
            return true;
        }
    }
}
