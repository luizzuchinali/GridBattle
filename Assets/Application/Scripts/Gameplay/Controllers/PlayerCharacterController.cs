using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
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
    /// <para>
    /// A turn has a walk, an action (basic attack or skill) and one consumable, in any order. After each choice the
    /// turn ends by itself when none of the three is still possible (<see cref="HasAnythingLeft"/>: used already, or
    /// nothing to do it with: no cell to walk to, no target and no ready skill with a cell to aim at, no usable item);
    /// otherwise it waits for <see cref="EndTurn"/> (the End turn button, always shown on the player's turn) or a tap
    /// on the player's own character. The walk can be taken back
    /// (<see cref="UndoMove"/>) while the action was not used, no item was used after it and its cell did not
    /// react (terrain that triggers on entering). Every change raises <see cref="PlayerTurnStateChangedEvent"/>.
    /// </para>
    /// <para>
    /// A long press on an enemy also marks on the grid the cells it can hit on its next turn
    /// (<see cref="GridRules.GetReachCells"/>) until the next tap.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : CharacterControllerBase<PlayerCharacter>
    {
        private static readonly Dictionary<Vector2Int, ECellHighlightType> NoHighlights = new();

        private readonly List<Action> _unsubscribers = new();
        private bool _isMyTurn = true;
        private bool _highlightsDirty = true;

        // This turn: what was used, where the walk started and whether it can be undone.
        private bool _moved;
        private bool _acted;
        private bool _undoable;
        private Vector2Int _moveStart;

        /// <summary>The enemy whose reach is shown (long press), or null.</summary>
        private Enemy _inspected;

        /// <summary>The player can walk now (their turn, no walk yet).</summary>
        public bool CanMove => _isMyTurn && !_moved;

        /// <summary>The player can attack or use a skill now (their turn, no action yet).</summary>
        public bool CanAct => _isMyTurn && !_acted;

        /// <summary>The player can still use a consumable this turn (their turn and the turn's limit not reached).</summary>
        public bool CanUseConsumable =>
            _isMyTurn && Owner.ConsumableUsesThisTurn < ConsumableSettings.Current.MaxUsesPerTurn;

        /// <summary>The walk of this turn can be taken back (see <see cref="UndoMove"/>).</summary>
        public bool CanUndoMove => _isMyTurn && _moved && _undoable && !_acted;

        /// <summary>The player walked or acted this turn.</summary>
        public bool HasStartedTurn => _moved || _acted;

        /// <summary>It is the player's turn (until something ends it).</summary>
        public bool IsMyTurn => _isMyTurn;

        /// <summary>
        /// Whether the player can still do any of the three things of the turn now: walk (not walked and a cell to
        /// walk to), act (not acted and an attack target, or a ready skill with a cell to aim at) or use a consumable
        /// (the turn's limit not reached and an item with a valid target). When none is left after a choice, the turn
        /// ends by itself.
        /// </summary>
        public bool HasAnythingLeft => _isMyTurn && (CanWalkSomewhere() || CanActSomehow() || CanUseAnyItem());

        private bool CanWalkSomewhere()
        {
            if (!CanMove) return false;

            var range = Owner.WalkDistance;
            var origin = Owner.CurrentGridPos;
            for (var y = -range; y <= range; y++)
            {
                for (var x = -range; x <= range; x++)
                {
                    if ((x != 0 || y != 0) && GridRules.CanWalkTo(Grid, Owner, origin + new Vector2Int(x, y)))
                        return true;
                }
            }

            return false;
        }

        private bool CanActSomehow()
        {
            if (!CanAct) return false;

            if (GridRules.GetHighlightInfos(Grid, Owner, false, true).Count > 0) return true;

            foreach (var skill in Owner.Skills)
            {
                if (skill != null && SkillTargeting.CanSelect(Owner, skill) &&
                    SkillTargeting.GetTargetableCells(Grid, Owner, skill).Count > 0)
                    return true;
            }

            return false;
        }

        private bool CanUseAnyItem()
        {
            if (!CanUseConsumable) return false;

            var inventory = Owner.Inventory;
            for (var slot = 0; slot < inventory.Slots; slot++)
            {
                var item = inventory.Get(slot);
                if (item != null && ConsumableRules.CanSelect(Grid, Owner, item, _isMyTurn) &&
                    ConsumableRules.GetTargetableCells(Grid, Owner, item).Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>The enemy whose reach the grid shows (long press), or null.</summary>
        public Enemy InspectedEnemy => _inspected != null && !_inspected.IsDead ? _inspected : null;

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
            EventBus.Subscribe<CellLongPressEvent>(OnCellLongPress);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);

            // Everything that can change which cells are walkable, attackable or aimable.
            WatchHighlights<EntityEnteredCellEvent>();
            WatchHighlights<EntityMoveUndoneEvent>();
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
            EventBus.Unsubscribe<CellLongPressEvent>(OnCellLongPress);
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
        /// aimed skill's range, else the reach of the enemy inspected by a long press, else where the player can
        /// walk or attack (what is left of the turn). Pure query (no selection is changed, nothing is applied);
        /// tests compare it with what the cells show.
        /// </summary>
        public Dictionary<Vector2Int, ECellHighlightType> ComputeHighlights()
        {
            if (SelectedConsumable != null)
                return ConsumableRules.GetHighlightInfos(Grid, Owner, SelectedConsumable);

            if (SelectedSkill != null)
                return GridRules.GetSkillHighlightInfos(Grid, Owner, SelectedSkill);

            var inspected = InspectedEnemy;
            if (inspected != null)
            {
                var danger = new Dictionary<Vector2Int, ECellHighlightType>();
                foreach (var cell in GridRules.GetReachCells(Grid, inspected))
                    danger[cell] = ECellHighlightType.Danger;
                return danger;
            }

            return GridRules.GetHighlightInfos(Grid, Owner, CanMove, CanAct);
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
            _moved = false;
            _acted = false;
            _undoable = false;
            _inspected = null;
            _highlightsDirty = true;
            if (!_isMyTurn)
                Grid.HighlightCells(NoHighlights);
            RaiseTurnState();
        }

        /// <summary>A long press on an enemy shows the cells it can hit on its next turn, until the next tap.</summary>
        private void OnCellLongPress(CellLongPressEvent e)
        {
            if (e.Cell == null || e.Cell.GetContent() is not Enemy enemy || enemy.IsDead) return;

            _inspected = enemy;
            _highlightsDirty = true;
        }

        // ------------------------------------------------------------------------------------ the turn

        /// <summary>
        /// Ends the player's turn now (the End turn button, or a tap on the player's own character). Returns false
        /// outside the player's turn or while a window blocks the input.
        /// </summary>
        public bool EndTurn()
        {
            if (!_isMyTurn || GameplayInput.IsBlocked) return false;

            FinishTurn();
            return true;
        }

        /// <summary>
        /// Takes back the walk of this turn (see <see cref="CanUndoMove"/>): the player hops back to where the turn
        /// started and can walk again. Nothing was drawn by the walk, so the turn is exactly as before it. Returns
        /// false when the walk cannot be undone.
        /// </summary>
        public bool UndoMove()
        {
            if (!CanUndoMove || GameplayInput.IsBlocked || !Grid.IsFreePosition(_moveStart)) return false;

            CancelSkillSelection();
            CancelConsumableSelection();
            Grid.UndoMove(Owner, _moveStart);
            _moved = false;
            _undoable = false;
            _highlightsDirty = true;
            RaiseTurnState();
            return true;
        }

        private void OnMoved(Vector2Int from, Vector2Int to)
        {
            _moved = true;
            _moveStart = from;

            // Terrain that reacts on entering (damage, states) already did: that walk cannot be taken back.
            var terrain = Grid.GetTerrain(to);
            _undoable = terrain == null || terrain.Trigger != ETerrainTrigger.OnEnter;
            AfterTurnChoice();
        }

        private void OnActed()
        {
            _acted = true;
            _undoable = false;
            AfterTurnChoice();
        }

        /// <summary>A walk, an action or an item was used: the turn ends by itself when nothing else can be done.</summary>
        private void AfterTurnChoice()
        {
            _highlightsDirty = true;
            RaiseTurnState();
            if (_isMyTurn && !HasAnythingLeft)
                FinishTurn();
        }

        private void FinishTurn()
        {
            CancelSkillSelection();
            CancelConsumableSelection();
            EventBus.Raise<PlayerActionEvent>();
        }

        private void RaiseTurnState()
        {
            EventBus.Raise(new PlayerTurnStateChangedEvent(_isMyTurn, CanMove, CanAct, CanUseConsumable, CanUndoMove,
                HasStartedTurn));
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

            if (!CanAct || !OwnsSkill(skill) || !SkillTargeting.CanSelect(Owner, skill)) return false;

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

            // Using an item replaces any aiming in progress; the walk before it can no longer be taken back.
            CancelConsumableSelection();
            CancelSkillSelection();
            _undoable = false;
            AfterTurnChoice();
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

            // Any tap puts away the reach of an inspected enemy.
            if (_inspected != null)
            {
                _inspected = null;
                _highlightsDirty = true;
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

            // Tapping the player's own character ends the turn.
            if (@event.Cell.GetContent() == Owner)
            {
                FinishTurn();
                return;
            }

            if (!@event.Cell.HasContent)
            {
                if (!CanMove || !GridRules.CanWalkTo(Grid, Owner, @event.Cell.GridPosition))
                {
                    @event.Cell.PlayRejectFeedback();
                    return;
                }

                var from = Owner.CurrentGridPos;
                Grid.MoveEntity(Owner, @event.Cell.GridPosition);
                OnMoved(from, @event.Cell.GridPosition);
            }
            else
            {
                if (!CanAct || !GridRules.IsAttackTarget(Grid, Owner, @event.Cell.GridPosition))
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

                OnActed();
            }
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
            if (!CanAct || !Owner.TryUseSkill(Grid, SelectedSkill, cell.GridPosition))
            {
                cell.PlayRejectFeedback();
                return;
            }

            CancelSkillSelection();
            OnActed();
        }

        /// <summary>
        /// Uses a skill directly on a target cell, without going through the selection
        /// (UI shortcuts, tests). Validation and effect live in SkillDefinition /
        /// Character, not here. Returns true if the skill was used (the action is
        /// consumed).
        /// </summary>
        public bool TryUseSkill(SkillDefinition skill, Vector2Int targetPos)
        {
            if (!CanAct) return false;
            if (!Owner.TryUseSkill(Grid, skill, targetPos)) return false;

            CancelSkillSelection();
            OnActed();
            return true;
        }
    }
}
