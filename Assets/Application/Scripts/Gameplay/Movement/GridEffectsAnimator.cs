using System.Collections.Generic;
using System.Threading;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// Visual combat effects (attack lunge, hit reaction, death). Like movement,
    /// gameplay is already resolved when these run; they only animate the entity
    /// and are tracked so the turn can wait for them.
    /// </summary>
    public sealed class GridEffectsAnimator
    {
        private readonly struct PendingHit
        {
            public readonly float Delay;
            public readonly Vector3 Direction;
            public readonly float Time;

            public PendingHit(float delay, Vector3 direction)
            {
                Delay = delay;
                Direction = direction;
                Time = UnityEngine.Time.time;
            }
        }

        private readonly Dictionary<GridEntity, MotionHandle> _lunges = new();
        private readonly Dictionary<GridEntity, MotionHandle> _reactions = new();
        private readonly Dictionary<GridEntity, PendingHit> _pendingHits = new();
        private readonly List<GridEntity> _finished = new();

        /// <summary>Divides every effect duration (1 = normal speed).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        public bool IsAnimating
        {
            get
            {
                Prune(_lunges);
                Prune(_reactions);
                return _lunges.Count > 0 || _reactions.Count > 0;
            }
        }

        /// <summary>
        /// The attacker advances toward <paramref name="direction"/> (world space)
        /// and returns. The target's next hit reaction is delayed to the impact
        /// point (half of the lunge).
        /// </summary>
        public void PlayAttack(GridEntity attacker, GridEntity target, Vector3 direction,
            GridMovementSettings settings)
        {
            if (settings == null || !Application.isPlaying || SimMode.IsActive || attacker == null) return;

            direction.z = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
            var duration = settings.LungeDuration / Speed;
            if (target != null)
                _pendingHits[target] = new PendingHit(duration * 0.5f, direction);

            Stop(_lunges, attacker);
            var transform = attacker.transform;
            var basePos = transform.localPosition;
            var worldOffset = direction * (settings.LungePixels / GameConfigManager.Ppu);
            var offset = transform.parent != null
                ? transform.parent.InverseTransformVector(worldOffset)
                : worldOffset;

            _lunges[attacker] = LMotion.Create(0f, 1f, duration)
                .WithOnComplete(() => transform.localPosition = basePos)
                .Bind(t =>
                {
                    var k = t < 0.5f
                        ? EaseUtility.Evaluate(t * 2f, Ease.OutQuad)
                        : 1f - EaseUtility.Evaluate((t - 0.5f) * 2f, Ease.InQuad);
                    transform.localPosition = basePos + offset * k;
                })
                .AddTo(attacker.gameObject);
        }

        /// <summary>
        /// Delays the entity's next hit or death reaction by <paramref name="delay"/> seconds, with the recoil
        /// pushed toward <paramref name="direction"/> (world space): the collision bump of a pushed or pulled
        /// character, which must wait for its slide to end. Call it before the damage that causes the reaction.
        /// </summary>
        public void QueueImpact(GridEntity entity, Vector3 direction, float delay)
        {
            if (!Application.isPlaying || SimMode.IsActive || entity == null) return;

            direction.z = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
            _pendingHits[entity] = new PendingHit(delay, direction);
        }

        /// <summary>Flash and small recoil, at the impact point of a pending attack.</summary>
        public void PlayHit(GridEntity entity, GridMovementSettings settings)
        {
            if (settings == null || !Application.isPlaying || SimMode.IsActive || entity == null) return;
            if (!entity.TryGetComponent(out SpriteRenderer sprite)) return;

            TakePending(entity, out var delay, out var direction);
            Stop(_reactions, entity);

            var transform = entity.transform;

            // A delayed reaction (the entity may still be sliding) reads its resting position when it starts.
            var basePos = transform.localPosition;
            var started = delay <= 0f;
            var baseColor = sprite.color;
            var worldOffset = direction * (settings.HitRecoilPixels / GameConfigManager.Ppu);
            var offset = transform.parent != null
                ? transform.parent.InverseTransformVector(worldOffset)
                : worldOffset;
            var flash = settings.HitFlashDuration / Speed;
            var total = delay + flash;

            _reactions[entity] = LMotion.Create(0f, 1f, total)
                .WithOnComplete(() =>
                {
                    if (started)
                        transform.localPosition = basePos;
                    sprite.color = baseColor;
                })
                .Bind(t =>
                {
                    var time = t * total - delay;
                    if (time < 0f) return;

                    if (!started)
                    {
                        basePos = transform.localPosition;
                        started = true;
                    }

                    var k = time / flash;
                    sprite.color = k < 0.7f ? settings.HitFlashColor : baseColor;
                    transform.localPosition = basePos + offset * (1f - Mathf.Abs(2f * k - 1f));
                })
                .AddTo(entity.gameObject);
        }

        /// <summary>
        /// Flash and fade, then destroys the entity. The entity is already logically
        /// dead and out of its cell.
        /// </summary>
        public void PlayDeath(GridEntity entity, GridMovementSettings settings)
        {
            if (entity == null) return;
            if (settings == null || !Application.isPlaying || SimMode.IsActive ||
                !entity.TryGetComponent(out SpriteRenderer sprite))
            {
                Object.Destroy(entity.gameObject);
                return;
            }

            TakePending(entity, out var delay, out _);
            Stop(_reactions, entity);

            var baseColor = sprite.color;
            var fade = settings.DeathDuration / Speed;
            var total = delay + fade;

            _reactions[entity] = LMotion.Create(0f, 1f, total)
                .WithOnComplete(() => Object.Destroy(entity.gameObject))
                .Bind(t =>
                {
                    var time = t * total - delay;
                    if (time < 0f) return;

                    var color = settings.HitFlashColor;
                    color.a = 1f - time / fade;
                    sprite.color = color;
                })
                .AddTo(entity.gameObject);
        }

        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            while (IsAnimating)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        private float Speed => Mathf.Max(0.01f, SpeedMultiplier);

        private void TakePending(GridEntity entity, out float delay, out Vector3 direction)
        {
            // A pending hit older than a second belongs to an attack that never landed.
            if (_pendingHits.Remove(entity, out var pending) && Time.time - pending.Time < 1f)
            {
                delay = pending.Delay;
                direction = pending.Direction;
            }
            else
            {
                delay = 0f;
                direction = Vector3.right;
            }
        }

        private static void Stop(Dictionary<GridEntity, MotionHandle> motions, GridEntity entity)
        {
            if (!motions.Remove(entity, out var handle)) return;
            if (handle.IsActive())
                handle.Complete();
        }

        private void Prune(Dictionary<GridEntity, MotionHandle> motions)
        {
            _finished.Clear();
            foreach (var pair in motions)
            {
                if (pair.Key == null || !pair.Value.IsActive())
                    _finished.Add(pair.Key);
            }

            foreach (var entity in _finished)
                motions.Remove(entity);
        }
    }
}
