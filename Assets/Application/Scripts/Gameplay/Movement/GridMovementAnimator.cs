using System.Collections.Generic;
using System.Threading;
using GridBattle.Gameplay.Entities;
using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// Visual side of grid movement. The logical move (cell occupancy and
    /// CurrentGridPos) happens immediately; this only animates the entity from
    /// where it was to its new cell with small hops, and tells whether any
    /// movement is still playing.
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
        /// Animates <paramref name="entity"/>, already parented to its new cell,
        /// from <paramref name="fromWorld"/> to its current local position.
        /// </summary>
        public void Animate(GridEntity entity, Vector3 fromWorld, Vector2Int fromCell, Vector2Int toCell,
            GridMovementSettings settings)
        {
            Stop(entity);
            if (settings == null || !Application.isPlaying) return;

            var target = entity.transform;
            var end = target.localPosition;
            var start = target.parent != null ? target.parent.InverseTransformPoint(fromWorld) : fromWorld;
            if (start == end) return;

            var distance = Vector2Int.Distance(fromCell, toCell);
            var hops = Mathf.Max(1, Mathf.RoundToInt(distance * settings.HopsPerCell));
            var hopHeight = settings.HopHeight;

            target.localPosition = start;
            var handle = LMotion.Create(0f, 1f, hops * settings.HopDuration)
                .WithOnComplete(() => target.localPosition = end)
                .Bind(progress =>
                {
                    var arc = Mathf.Abs(Mathf.Sin(Mathf.PI * hops * progress)) * hopHeight;
                    target.localPosition = Vector3.Lerp(start, end, progress) + Vector3.up * arc;
                })
                .AddTo(entity.gameObject);

            _motions[entity] = handle;
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
