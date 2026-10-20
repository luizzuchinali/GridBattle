using System;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay;
using GridBattle.Managers.Audio;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// What is left of the player's turn, between the grid and the item bar: a foot (the walk), a sword (the action)
    /// and a potion (the turn's consumable), dimmed once used (the foot also while the player cannot walk at all this
    /// turn); Undo while the walk can be taken back; End turn during
    /// the whole player's turn. While a skill or an item is aimed (its forecast on the board), Cancel and Confirm take
    /// the place of Undo and End turn. The only place that knows these elements in the UXML; the buttons call the
    /// player's controller and everything redraws from <see cref="PlayerTurnStateChangedEvent"/> and
    /// <see cref="ActionAimChangedEvent"/>.
    /// </summary>
    public sealed class TurnActionsView : IDisposable
    {
        private const string UsedClass = "turn-icon--used";

        private readonly VisualElement _root;
        private readonly VisualElement _moveIcon;
        private readonly VisualElement _actIcon;
        private readonly VisualElement _itemIcon;
        private readonly Button _undoButton;
        private readonly Button _endButton;
        private readonly Button _cancelButton;
        private readonly Button _confirmButton;
        private bool _positioned;

        public TurnActionsView(VisualElement root)
        {
            _root = root.Q<VisualElement>("turn-actions");
            _moveIcon = root.Q<VisualElement>("turn-move-icon");
            _actIcon = root.Q<VisualElement>("turn-act-icon");
            _itemIcon = root.Q<VisualElement>("turn-item-icon");
            _undoButton = root.Q<Button>("undo-move-btn");
            _endButton = root.Q<Button>("end-turn-btn");
            _cancelButton = root.Q<Button>("cancel-action-btn");
            _confirmButton = root.Q<Button>("confirm-action-btn");
            _undoButton.OnClick(OnUndoClicked);
            _endButton.OnClick(OnEndClicked);
            _cancelButton.OnClick(OnCancelClicked);
            _confirmButton.OnClick(OnConfirmClicked);

            EventBus.Subscribe<PlayerTurnStateChangedEvent>(OnTurnStateChanged);
            EventBus.Subscribe<ActionAimChangedEvent>(OnAimChanged);
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            Refresh();

            // The bar is never drawn before it sits under the grid: hidden (layout kept) until a position is applied,
            // and repositioned at once, without animation, whenever the grid, the panel or the screen changes.
            _root.style.visibility = Visibility.Hidden;
            _root.style.transitionDuration = new StyleList<TimeValue>(new System.Collections.Generic.List<TimeValue> { new(0f) });
            _root.RegisterCallback<AttachToPanelEvent>(_ => UpdatePosition());
            _root.RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());
            if (_root.parent != null)
                _root.parent.RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());
            _root.schedule.Execute(UpdatePosition).Every(250);
            UpdatePosition();
        }

        /// <summary>A new board: the old position no longer holds, the bar hides until the new one is applied.</summary>
        private void OnGridInitialized(GridInitializedEvent e)
        {
            _positioned = false;
            if (_root != null)
                _root.style.visibility = Visibility.Hidden;
            UpdatePosition();
        }

        /// <summary>Top of the bar, in its parent's coordinates (pixels at the reference resolution), or -1.</summary>
        public float Top => _root != null && _root.style.top.keyword == StyleKeyword.Undefined ? _root.style.top.value.value : -1f;

        /// <summary>
        /// Puts the top of the bar <see cref="HudSettings.TurnBarGap"/> pixels below the bottom edge of the grid's last
        /// row of cells (world → panel through the main camera), on whole screen pixels.
        /// </summary>
        public void UpdatePosition()
        {
            if (_root?.panel == null || _root.parent == null) return;

            // The panel must have laid out its elements, or the parent's position is not final yet.
            var parentBound = _root.parent.worldBound;
            if (float.IsNaN(parentBound.y) || parentBound.height <= 0f) return;

            var grid = GridController.Instance;
            var camera = Camera.main;
            if (grid == null || camera == null || !grid.TryGetBottomEdge(out var edge)) return;

            // The edge is snapped to the screen's pixels (the panel is scaled by the pixel ratio, so a panel unit is one
            // reference pixel), which is where the pixel perfect camera draws it; the gap is whole reference pixels.
            var screen = camera.WorldToScreenPoint(edge);
            var snapped = new Vector2(Mathf.Round(screen.x), Screen.height - Mathf.Round(screen.y));
            var panelPoint = RuntimePanelUtils.ScreenToPanel(_root.panel, snapped);
            var top = panelPoint.y + HudSettings.Current.TurnBarGap - parentBound.y;
            if (float.IsNaN(top)) return;

            if (!_positioned || _root.style.top.keyword != StyleKeyword.Undefined ||
                Mathf.Abs(_root.style.top.value.value - top) >= 0.01f)
            {
                _root.style.top = top;
                _root.style.bottom = StyleKeyword.Auto;
            }

            if (_positioned) return;

            // Shown only from the next frame on, once the new top has been laid out.
            _positioned = true;
            _root.schedule.Execute(() =>
            {
                if (_positioned)
                    _root.style.visibility = StyleKeyword.Null;
            });
        }

        public Button UndoButton => _undoButton;
        public Button EndTurnButton => _endButton;
        public Button CancelButton => _cancelButton;
        public Button ConfirmButton => _confirmButton;

        public void Dispose()
        {
            EventBus.Unsubscribe<PlayerTurnStateChangedEvent>(OnTurnStateChanged);
            EventBus.Unsubscribe<ActionAimChangedEvent>(OnAimChanged);
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
        }

        /// <summary>Applies the current language.</summary>
        public void RefreshTexts()
        {
            _undoButton.text = HudText.Get("hud.undo_move");
            _endButton.text = HudText.Get("hud.end_turn");
            _cancelButton.text = HudText.Get("hud.cancel_action");
            _confirmButton.text = HudText.Get("hud.confirm_action");
        }

        private void OnTurnStateChanged(PlayerTurnStateChangedEvent e) => Refresh();

        private void OnAimChanged(ActionAimChangedEvent e) => Refresh();

        private void Refresh()
        {
            if (_root == null) return;

            RefreshTexts();

            // Read from the controller: the HUD may be built in the middle of a turn. The foot is dimmed whenever the
            // player cannot walk this turn, not only once the walk is used: kept in place by a state (Exhausted, rooted)
            // or with no cell to go to.
            var controller = FindController();
            var canMove = controller != null && controller.CanWalkNow;
            var canAct = controller != null && controller.CanAct;
            _moveIcon.EnableInClassList(UsedClass, !canMove);
            _actIcon.EnableInClassList(UsedClass, !canAct);
            _itemIcon.EnableInClassList(UsedClass, controller == null || !controller.CanUseConsumable);
            var aiming = controller != null && controller.IsMyTurn && controller.AimedCell.HasValue;
            _undoButton.SetDisplayed(!aiming && controller != null && controller.CanUndoMove);
            _endButton.SetDisplayed(!aiming && controller != null && controller.IsMyTurn);
            _cancelButton.SetDisplayed(aiming);
            _confirmButton.SetDisplayed(aiming);
        }

        private static PlayerCharacterController FindController()
        {
            var player = PlayerCharacter.Active;
            return player != null && player.TryGetComponent(out PlayerCharacterController controller) ? controller : null;
        }

        private void OnUndoClicked()
        {
            var controller = FindController();
            if (controller != null && controller.UndoMove())
                AudioManager.Play(ESfx.ButtonTap);
        }

        private void OnCancelClicked()
        {
            var controller = FindController();
            if (controller == null) return;

            controller.CancelAction();
            AudioManager.Play(ESfx.ButtonTap);
        }

        private void OnConfirmClicked()
        {
            var controller = FindController();
            if (controller != null && controller.ConfirmAim())
                AudioManager.Play(ESfx.ButtonTap);
        }

        private void OnEndClicked()
        {
            var controller = FindController();
            if (controller != null && controller.EndTurn())
                AudioManager.Play(ESfx.ButtonTap);
        }
    }
}
