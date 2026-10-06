using System;
using System.Collections.Generic;
using System.Threading;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using JetBrains.Annotations;
using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// Visual side of grid movement. The logical move (cell occupancy and
    /// CurrentGridPos) happens immediately; this only animates the entity from
    /// where it was to its new cell (hop or flip, see GridMovementSettings), and
    /// tells whether any movement is still playing.
    /// </summary>
    public sealed class GridMovementAnimator
    {
        private readonly Dictionary<GridEntity, MotionHandle> _motions = new();
        private readonly List<GridEntity> _finished = new();

        public bool IsAnimating
        {
            get
            {
                Prune();
                return _motions.Count > 0;
            }
        }

        /// <summary>
        /// Divides every animation duration (1 = normal speed). Reset it when the
        /// speed-up ends.
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// Animates <paramref name="entity"/>, already parented to its new cell,
        /// from <paramref name="fromWorld"/> to its current local position.
        /// <paramref name="onArrived"/> runs when the movement completes (also
        /// when it is interrupted by a new movement of the same entity).
        /// </summary>
        public void Animate(GridEntity entity, Vector3 fromWorld, Vector2Int fromCell, Vector2Int toCell,
            GridMovementSettings settings, Action onArrived = null)
        {
            Stop(entity);
            if (Application.isPlaying)
                EventBus.Raise(new EntityMoveStartedEvent(entity, fromCell, toCell));
            if (settings == null || !Application.isPlaying || SimMode.IsActive) return;

            var target = entity.transform;
            var end = target.localPosition;
            var start = target.parent != null ? target.parent.InverseTransformPoint(fromWorld) : fromWorld;
            if (start == end) return;

            var speed = Mathf.Max(0.01f, SpeedMultiplier);
            var handle = settings.Style == EMovementStyle.Flip
                ? CreateFlip(target, start, end, settings, speed, onArrived)
                : CreateHop(target, start, end, Vector2Int.Distance(fromCell, toCell), settings, speed, onArrived);

            _motions[entity] = handle.AddTo(entity.gameObject);
        }

        /// <summary>Seconds a slide over <paramref name="cells"/> cells lasts at normal speed (0 without settings).</summary>
        public static float GetSlideDuration([CanBeNull] GridMovementSettings settings, int cells)
        {
            return settings == null || cells <= 0 ? 0f : settings.SlideSecondsPerCell * cells;
        }

        /// <summary>
        /// Like <see cref="Animate"/> for a character that is pushed or pulled: it slides in a straight line to
        /// its new cell, without hopping or flipping, taking <see cref="GetSlideDuration"/> per cell crossed.
        /// Skipped (only the move event is raised) outside Play Mode and during the balance simulation.
        /// </summary>
        public void AnimateSlide(GridEntity entity, Vector3 fromWorld, Vector2Int fromCell, Vector2Int toCell,
            GridMovementSettings settings)
        {
            Stop(entity);
            if (Application.isPlaying)
                EventBus.Raise(new EntityMoveStartedEvent(entity, fromCell, toCell));
            if (settings == null || !Application.isPlaying || SimMode.IsActive) return;

            var target = entity.transform;
            var end = target.localPosition;
            var start = target.parent != null ? target.parent.InverseTransformPoint(fromWorld) : fromWorld;
            if (start == end) return;

            var cells = Mathf.Max(Mathf.Abs(toCell.x - fromCell.x), Mathf.Abs(toCell.y - fromCell.y));
            var total = GetSlideDuration(settings, cells) / Mathf.Max(0.01f, SpeedMultiplier);
            target.localPosition = start;
            var handle = LMotion.Create(0f, 1f, total)
                .WithEase(settings.SlideEase)
                .WithOnComplete(() => Normalize(target, end))
                .Bind(t => target.localPosition = Vector3.LerpUnclamped(start, end, t));
            _motions[entity] = handle.AddTo(entity.gameObject);
        }

        private static MotionHandle CreateHop(Transform target, Vector3 start, Vector3 end, float distance,
            GridMovementSettings settings, float speed, Action onArrived)
        {
            var hops = Mathf.Max(1, Mathf.RoundToInt(distance * settings.HopsPerCell));
            var hopHeight = settings.HopHeight;
            var hopsDuration = hops * settings.HopDuration / speed;
            var squashDuration = settings.LandingSquash > 0f ? settings.LandingSquashDuration / speed : 0f;
            var squashY = 1f - settings.LandingSquash / GameConfigManager.Ppu;
            var total = hopsDuration + squashDuration;

            target.localPosition = start;
            return LMotion.Create(0f, 1f, total)
                .WithOnComplete(() =>
                {
                    Normalize(target, end);
                    onArrived?.Invoke();
                })
                .Bind(t =>
                {
                    var time = t * total;
                    if (time < hopsDuration)
                    {
                        var progress = EaseUtility.Evaluate(time / hopsDuration, settings.HopEase);
                        var arc = Mathf.Abs(Mathf.Sin(Mathf.PI * hops * time / hopsDuration)) * hopHeight;
                        target.localPosition = Vector3.Lerp(start, end, progress) + Vector3.up * arc;
                        target.localScale = Vector3.one;
                    }
                    else
                    {
                        // Landing: flatten and recover (V shape) at the destination.
                        var k = (time - hopsDuration) / squashDuration;
                        target.localPosition = end;
                        target.localScale = new Vector3(1f, Mathf.Lerp(1f, squashY, 1f - Mathf.Abs(2f * k - 1f)), 1f);
                    }
                });
        }

        private static MotionHandle CreateFlip(Transform target, Vector3 start, Vector3 end,
            GridMovementSettings settings, float speed, Action onArrived)
        {
            var close = settings.FlipCloseDuration / speed;
            var open = settings.FlipOpenDuration / speed;
            var total = close + open;
            var overshoot = settings.FlipOvershoot;
            var steps = settings.ScaleSteps;

            target.localPosition = start;
            return LMotion.Create(0f, 1f, total)
                .WithOnComplete(() =>
                {
                    Normalize(target, end);
                    onArrived?.Invoke();
                })
                .Bind(t =>
                {
                    var time = t * total;
                    float scaleX;
                    if (time < close)
                    {
                        scaleX = 1f - EaseUtility.Evaluate(time / close, settings.FlipCloseEase);
                        target.localPosition = start;
                    }
                    else
                    {
                        // 0 -> 1 + overshoot -> 1: the first 70% opens, the rest settles.
                        var k = EaseUtility.Evaluate((time - close) / open, settings.FlipOpenEase);
                        scaleX = k < 0.7f
                            ? k / 0.7f * (1f + overshoot)
                            : Mathf.Lerp(1f + overshoot, 1f, (k - 0.7f) / 0.3f);
                        target.localPosition = end;
                    }

                    if (steps > 0)
                        scaleX = Mathf.Round(scaleX * steps) / steps;
                    target.localScale = new Vector3(scaleX, 1f, 1f);
                });
        }

        private static void Normalize(Transform target, Vector3 end)
        {
            target.localPosition = end;
            target.localScale = Vector3.one;
        }

        /// <summary>Completes once no entity is moving (immediately if none is).</summary>
        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            while (IsAnimating)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        private void Stop(GridEntity entity)
        {
            if (_motions.TryGetValue(entity, out var handle))
            {
                if (handle.IsActive())
                    handle.Complete();
                _motions.Remove(entity);
            }
        }

        private void Prune()
        {
            _finished.Clear();
            foreach (var pair in _motions)
            {
                if (pair.Key == null || !pair.Value.IsActive())
                    _finished.Add(pair.Key);
            }

            foreach (var entity in _finished)
                _motions.Remove(entity);
        }
    }
}
