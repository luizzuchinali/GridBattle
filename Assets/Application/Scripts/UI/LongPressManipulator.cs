using System;
using GridBattle.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    /// <summary>
    /// Long press on a UI element, the same gesture as a long tap on a cell: a touch or the left button held for
    /// <see cref="GameplayInputSettings.LongPressSeconds"/>, or a right click (at once), calls the action. Moving
    /// the pointer more than a few pixels while holding cancels it. Releasing after a long press still produces a
    /// click on buttons: the click handler should ignore it by checking <see cref="ConsumeFired"/>.
    /// </summary>
    public sealed class LongPressManipulator : PointerManipulator
    {
        /// <summary>Panel pixels the pointer may move while held before the press stops counting.</summary>
        private const float MoveTolerance = 6f;

        private readonly Action _action;
        private IVisualElementScheduledItem _timer;
        private Vector2 _startPosition;
        private int _pointerId = -1;
        private bool _fired;

        public LongPressManipulator(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Whether a long press fired since the last press started, clearing the flag: a click handler returns early
        /// when it is true, so the release that ends a long press does not also tap the element.
        /// </summary>
        public bool ConsumeFired()
        {
            var fired = _fired;
            _fired = false;
            return fired;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // Trickle down: buttons stop the propagation of their own pointer events.
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            CancelTimer();
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            _fired = false;
            if (e.button == (int)MouseButton.RightMouse)
            {
                _action?.Invoke();
                return;
            }

            if (e.button != (int)MouseButton.LeftMouse) return;

            CancelTimer();
            _pointerId = e.pointerId;
            _startPosition = e.position;
            var delayMs = (long)(GameplayInputSettings.Current.LongPressSeconds * 1000f);
            _timer = target.schedule.Execute(Fire).StartingIn(delayMs);
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (_timer == null || e.pointerId != _pointerId) return;

            if (((Vector2)e.position - _startPosition).sqrMagnitude > MoveTolerance * MoveTolerance)
                CancelTimer();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (e.pointerId == _pointerId)
                CancelTimer();
        }

        private void OnPointerCancel(PointerCancelEvent e)
        {
            if (e.pointerId == _pointerId)
                CancelTimer();
        }

        private void OnPointerLeave(PointerLeaveEvent e)
        {
            if (e.pointerId == _pointerId)
                CancelTimer();
        }

        private void Fire()
        {
            _timer = null;
            _pointerId = -1;
            _fired = true;
            _action?.Invoke();
        }

        private void CancelTimer()
        {
            _timer?.Pause();
            _timer = null;
            _pointerId = -1;
        }
    }
}
