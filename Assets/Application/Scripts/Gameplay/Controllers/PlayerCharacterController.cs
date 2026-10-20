using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States.Effects;
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
    /// A skill that uses the walk too (<see cref="SkillDefinition.UsesWalk"/>, a channelled spell) can only be selected,
    /// aimed and used before walking (<see cref="CanUseSkillNow"/>; undoing the walk gives the chance back), and using
    /// it spends the walk for the rest of the turn: it cannot be undone. A kill that gives the walk back (Momentum)
    /// still does.
    /// </para>
    /// <para>
    /// A tap on an enemy marks on the grid only the cells it can walk to from where it stands
    /// (<see cref="GridRules.GetWalkCells"/>) until the next tap.
    /// </para>
    /// <para>
    /// Aim, then confirm (<see cref="GameplayInputSettings.ConfirmTargetedActions"/>): the first tap on a valid cell
    /// only aims the selected skill or item (<see cref="AimedCell"/>; its area is highlighted and the forecast shows
    /// the damage, kills, pushes and where the enemies' strikes would land); a second tap on the same cell, or
    /// <see cref="ConfirmAim"/> (the Confirm button), uses it. Another valid cell moves the aim.
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

        /// <summary>
        /// The walk of the turn is unused (their turn, no walk yet, no skill that uses the walk). A state may still keep
        /// the player in place: see <see cref="CanWalkNow"/>.
        /// </summary>
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
        /// The player can walk now and has somewhere to go: the walk is unused and a cell is in reach (or a swap step).
        /// Unlike <see cref="CanMove"/>, false while a state prevents walking (Exhausted, rooted: walk range 0) or no
        /// cell around is free.
        /// </summary>
        public bool CanWalkNow => CanWalkSomewhere();

        /// <summary>
        /// Whether the player may select, aim and use <paramref name="skill"/> now, wherever it is aimed: their turn,
        /// the action unused, the skill ready (cooldown, not silenced) and, for a skill that uses the walk too
        /// (<see cref="SkillDefinition.UsesWalk"/>), the walk unused. The skill bar and the simulation bot ask this.
        /// </summary>
        public bool CanUseSkillNow(SkillDefinition skill)
        {
            if (skill == null || !CanAct) return false;
            if (skill.UsesWalk && !CanMove) return false;

            return SkillTargeting.CanSelect(Owner, skill);
        }

        /// <summary>
        /// Whether the player can still do any of the three things of the turn now: walk (not walked and a cell to
        /// walk to), act (not acted and an attack target, or a ready skill with a cell to aim at; a skill that uses the
        /// walk only before walking) or use a consumable (the turn's limit not reached and an item with a valid
        /// target). When none is left after a choice, the turn ends by itself. A ready skill that uses the walk counts
        /// while the walk can still be undone, so walking first does not end the turn before the player can take it back.
        /// </summary>
        public bool HasAnythingLeft =>
            _isMyTurn && (CanWalkSomewhere() || CanActSomehow() || CanUseAnyItem() || CanChannelAfterUndo());

        private bool CanChannelAfterUndo()
        {
            if (!CanUndoMove) return false;

            foreach (var skill in Owner.Skills)
            {
                if (skill != null && skill.UsesWalk && SkillTargeting.CanSelect(Owner, skill))
                    return true;
            }

            return false;
        }

        private bool CanWalkSomewhere()
        {
            if (!CanMove || Grid == null) return false;
            if (GetSwapStepCells().Count > 0) return true;

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
                if (CanUseSkillNow(skill) && SkillTargeting.GetTargetableCells(Grid, Owner, skill).Count > 0)
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

        /// <summary>The enemy whose walk range the grid shows (tapped), or null.</summary>
        public Enemy InspectedEnemy => _inspected != null && !_inspected.IsDead ? _inspected : null;

        /// <summary>The skill being aimed, or null when no skill is selected.</summary>
        public SkillDefinition SelectedSkill { get; private set; }

        /// <summary>
        /// The cell the selected skill or item is aimed at, waiting for the confirmation, or null (nothing aimed, or
        /// actions are used on the first tap).
        /// </summary>
        public Vector2Int? AimedCell { get; private set; }

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
            EventBus.Subscribe<ExtraWalkGrantedEvent>(OnExtraWalkGranted);

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
            WatchHighlights<CharactersSwappedEvent>();
            WatchHighlights<CharacterDisplacedEvent>();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
            EventBus.Unsubscribe<CellLongPressEvent>(OnCellLongPress);
            EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Unsubscribe<ExtraWalkGrantedEvent>(OnExtraWalkGranted);
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
            if (SelectedSkill != null && !CanUseSkillNow(SelectedSkill))
                CancelSkillSelection();
            if (AimedCell.HasValue && !IsAimStillValid(AimedCell.Value))
                SetAim(null);

            Grid.HighlightCells(ComputeHighlights());

            // The cancellations above raise events that mark the flag again: the result is already up to date.
            _highlightsDirty = false;
        }

        private bool IsAimStillValid(Vector2Int target)
        {
            if (SelectedSkill != null)
                return CanUseSkillNow(SelectedSkill) && SkillTargeting.CanUse(Grid, Owner, SelectedSkill, target);
            if (SelectedConsumable != null) return ConsumableRules.CanUse(Grid, Owner, SelectedConsumable, target, _isMyTurn);
            return false;
        }

        /// <summary>The item can stop being selectable while aimed (replaced or removed from its slot, no battle).</summary>
        private bool IsConsumableSelectionValid()
        {
            return Owner.Inventory.Get(SelectedConsumableSlot) == SelectedConsumable &&
                   ConsumableRules.CanSelect(Grid, Owner, SelectedConsumable, _isMyTurn);
        }

        /// <summary>
        /// The highlights for the current state, computed from scratch: the aimed item's range, else the
        /// aimed skill's range, else the cells the tapped enemy can walk to, else where the player can
        /// walk or attack (what is left of the turn). Pure query (no selection is changed, nothing is applied);
        /// tests compare it with what the cells show.
        /// </summary>
        public Dictionary<Vector2Int, ECellHighlightType> ComputeHighlights()
        {
            if (SelectedConsumable != null)
                return WithAimedArea(ConsumableRules.GetHighlightInfos(Grid, Owner, SelectedConsumable));

            if (SelectedSkill != null)
                return WithAimedArea(GridRules.GetSkillHighlightInfos(Grid, Owner, SelectedSkill));

            var inspected = InspectedEnemy;
            if (inspected != null)
            {
                var walk = new Dictionary<Vector2Int, ECellHighlightType>();
                foreach (var cell in GridRules.GetWalkCells(Grid, inspected))
                    walk[cell] = ECellHighlightType.Danger;
                return walk;
            }

            var infos = GridRules.GetHighlightInfos(Grid, Owner, CanMove, CanAct);
            foreach (var cell in GetSwapStepCells())
                infos[cell] = ECellHighlightType.Walk;
            return infos;
        }

        /// <summary>The range highlights with the area of the aimed action on top (<see cref="ECellHighlightType.SkillArea"/>).</summary>
        private Dictionary<Vector2Int, ECellHighlightType> WithAimedArea(Dictionary<Vector2Int, ECellHighlightType> infos)
        {
            if (!AimedCell.HasValue) return infos;

            var target = AimedCell.Value;
            var area = SelectedSkill != null
                ? SkillTargeting.GetAreaCells(Grid, Owner, SelectedSkill, target)
                : ConsumableRules.GetAreaCells(Grid, Owner, SelectedConsumable, target);
            foreach (var cell in area)
                infos[cell] = ECellHighlightType.SkillArea;
            return infos;
        }

        /// <summary>
        /// Swap step (rule talent <see cref="SwapStepEffect"/>): while the walk is unused, the player can trade
        /// places with this adjacent character (8 neighbours) instead of walking.
        /// </summary>
        public bool CanSwapStepWith(Character other)
        {
            if (!CanMove || other == null || other == Owner || other.IsDead || !other.CanBeDisplaced) return false;
            if (Owner.WalkDistance <= 0 || !Owner.States.HasEffect<SwapStepEffect>()) return false;

            var delta = other.CurrentGridPos - Owner.CurrentGridPos;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) == 1;
        }

        /// <summary>The cells of the characters the player can swap step with now.</summary>
        private List<Vector2Int> GetSwapStepCells()
        {
            var cells = new List<Vector2Int>();
            if (!CanMove || !Owner.States.HasEffect<SwapStepEffect>()) return cells;

            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var cell = Owner.CurrentGridPos + new Vector2Int(x, y);
                    if ((x == 0 && y == 0) || !Grid.IsValidPosition(cell)) continue;
                    if (Grid.GetContent(cell) is Character other && CanSwapStepWith(other))
                        cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>
        /// Uses the walk to trade places with <paramref name="other"/> (see <see cref="CanSwapStepWith"/>). A swap
        /// counts as the turn's walk but cannot be undone. Returns false when it is not possible.
        /// </summary>
        public bool TrySwapStep(Character other)
        {
            if (!_isMyTurn || GameplayInput.IsBlocked || !CanSwapStepWith(other)) return false;

            CancelSkillSelection();
            CancelConsumableSelection();
            if (!Grid.SwapCharacters(Owner, other, Owner)) return false;

            Owner.RegisterMovement();
            _moved = true;
            _undoable = false;
            AfterTurnChoice();
            return true;
        }

        /// <summary>Momentum (a kill that gives the walk back): the player may walk again this turn.</summary>
        private void OnExtraWalkGranted(ExtraWalkGrantedEvent e)
        {
            if (e.Character != Owner || !_isMyTurn || !_moved) return;

            _moved = false;
            _undoable = false;
            _highlightsDirty = true;
            RaiseTurnState();
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

        /// <summary>A long press on an enemy shows the cells it can walk to, until the next tap.</summary>
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
        /// one of the character's or it cannot be used now (<see cref="CanUseSkillNow"/>: action used, cooldown,
        /// silenced, or a skill that uses the walk after walking). Returns true if the selection changed.
        /// </summary>
        public bool SelectSkill(SkillDefinition skill)
        {
            if (!_isMyTurn || GameplayInput.IsBlocked || skill == null) return false;

            if (SelectedSkill == skill)
            {
                CancelSkillSelection();
                return true;
            }

            if (!OwnsSkill(skill) || !CanUseSkillNow(skill)) return false;

            // Aiming a skill replaces aiming an item.
            CancelConsumableSelection();
            SetAim(null);
            SelectedSkill = skill;
            EventBus.Raise(new SkillSelectionChangedEvent(Owner, skill));
            return true;
        }

        /// <summary>Clears the skill selection (back to walking and attacking).</summary>
        public void CancelSkillSelection()
        {
            if (SelectedSkill == null) return;

            SetAim(null);
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
            SetAim(null);
            SelectedConsumableSlot = slot;
            SelectedConsumable = consumable;
            EventBus.Raise(new ConsumableSelectionChangedEvent(Owner, slot, consumable));
            return true;
        }

        /// <summary>Clears the consumable selection (back to walking and attacking).</summary>
        public void CancelConsumableSelection()
        {
            if (SelectedConsumable == null) return;

            SetAim(null);
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

            // Any tap puts away the walk range of an inspected enemy.
            var wasInspected = _inspected;
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
                Grid.WalkEntity(Owner, @event.Cell.GridPosition);
                OnMoved(from, @event.Cell.GridPosition);
            }
            else
            {
                // Swap step: a tap on an adjacent character trades places with it (it uses the walk).
                if (@event.Cell.GetContent() is Character swapWith && CanSwapStepWith(swapWith))
                {
                    TrySwapStep(swapWith);
                    return;
                }

                // Without a basic attack a tap on an enemy shows (or hides) where it can walk.
                if (!Owner.HasBasicAttack && @event.Cell.GetContent() is Enemy enemy && !enemy.IsDead)
                {
                    if (wasInspected != enemy)
                        _inspected = enemy;
                    _highlightsDirty = true;
                    return;
                }

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
            var target = cell.GridPosition;
            if (NeedsConfirmation(target))
            {
                if (ConsumableRules.CanUse(Grid, Owner, SelectedConsumable, target, _isMyTurn))
                    SetAim(target);
                else
                    cell.PlayRejectFeedback();
                return;
            }

            if (!TryUseConsumable(SelectedConsumableSlot, target))
                cell.PlayRejectFeedback();
        }

        /// <summary>
        /// Whether a tap on <paramref name="target"/> only aims the selected action (the first tap, or a tap on another
        /// cell) instead of using it.
        /// </summary>
        private bool NeedsConfirmation(Vector2Int target) =>
            GameplayInputSettings.Current.ConfirmTargetedActions && AimedCell != target;

        private void SetAim(Vector2Int? cell)
        {
            if (AimedCell == cell) return;

            AimedCell = cell;
            _highlightsDirty = true;
            EventBus.Raise(new ActionAimChangedEvent(Owner, cell));
        }

        /// <summary>
        /// Uses the aimed skill or item on <see cref="AimedCell"/> (the Confirm button; a second tap on the cell does the
        /// same). Returns false when nothing is aimed or the action cannot be used there any more.
        /// </summary>
        public bool ConfirmAim()
        {
            if (!AimedCell.HasValue || !_isMyTurn || GameplayInput.IsBlocked) return false;

            var target = AimedCell.Value;
            if (SelectedConsumable != null)
                return TryUseConsumable(SelectedConsumableSlot, target);

            return SelectedSkill != null && UseSkill(SelectedSkill, target);
        }

        /// <summary>Drops the aimed or selected skill or item (the Cancel button), back to walking and attacking.</summary>
        public void CancelAction()
        {
            CancelSkillSelection();
            CancelConsumableSelection();
        }

        /// <summary>
        /// A tap while a skill is selected: a valid cell uses the skill (consuming the
        /// action and clearing the selection); an invalid one is rejected and the
        /// selection stays so the player can try another cell.
        /// </summary>
        private void TapWithSkill(Cell cell)
        {
            var target = cell.GridPosition;
            if (NeedsConfirmation(target))
            {
                if (CanUseSkillNow(SelectedSkill) && SkillTargeting.CanUse(Grid, Owner, SelectedSkill, target))
                    SetAim(target);
                else
                    cell.PlayRejectFeedback();
                return;
            }

            if (!UseSkill(SelectedSkill, target))
                cell.PlayRejectFeedback();
        }

        /// <summary>
        /// Uses a skill directly on a target cell, without going through the selection
        /// (UI shortcuts, tests). Validation and effect live in SkillDefinition /
        /// Character, not here; the turn's rules (<see cref="CanUseSkillNow"/>) apply. Returns true if the skill was
        /// used (the action is consumed, and the walk too for a skill that uses it).
        /// </summary>
        public bool TryUseSkill(SkillDefinition skill, Vector2Int targetPos) => UseSkill(skill, targetPos);

        /// <summary>
        /// The one way the player uses a skill (tap, Confirm, <see cref="TryUseSkill"/>): checks the turn's rules,
        /// lets the character validate and execute it (range, area, cooldown, restrictions), then spends the action
        /// and, for a skill that uses the walk, the walk (it cannot be undone).
        /// </summary>
        private bool UseSkill(SkillDefinition skill, Vector2Int targetPos)
        {
            if (!CanUseSkillNow(skill)) return false;

            // The walk is spent before the skill resolves, so a kill during it that gives the walk back (Momentum,
            // ExtraWalkGrantedEvent) still does; nothing is spent when the character refuses the use.
            var spendsWalk = skill.UsesWalk;
            if (spendsWalk)
                _moved = true;

            if (!Owner.TryUseSkill(Grid, skill, targetPos))
            {
                if (spendsWalk)
                    _moved = false;
                return false;
            }

            CancelSkillSelection();
            OnActed();
            return true;
        }
    }
}
