using System.Collections.Generic;
using System.Threading;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Feel;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// Visual combat effects (attack lunge, hit reaction, death). Like movement,
    /// gameplay is already resolved when these run; they only animate the entity
    /// and are tracked so the turn can wait for them. When a hit or death reaction
    /// starts on screen (the impact), the character flashes white
    /// (<see cref="SpriteFlash"/>) and <see cref="HitLandedEvent"/> is raised for the
    /// game feel and the impact sounds.
    /// </summary>
    public sealed class GridEffectsAnimator
    {
        /// <summary>Part of the attack lunge spent winding up (stepping back) before it strikes.</summary>
        private const float WindUpEnd = 0.2f;

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
        /// and returns, after <paramref name="startDelay"/> seconds (the end of its own walk, so it never lunges
        /// mid-hop). The target's next hit reaction is delayed to the impact point (half of the lunge).
        /// </summary>
        public void PlayAttack(GridEntity attacker, GridEntity target, Vector3 direction,
            GridMovementSettings settings, float startDelay = 0f)
        {
            if (settings == null || !Application.isPlaying || SimMode.IsActive || attacker == null) return;

            direction.z = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
            startDelay = Mathf.Max(0f, startDelay);
            var duration = settings.LungeDuration / Speed;
            if (target != null)
                _pendingHits[target] = new PendingHit(startDelay + duration * 0.5f, direction);

            Stop(_lunges, attacker);
            var transform = attacker.transform;
            var worldOffset = direction * (settings.LungePixels / GameConfigManager.Ppu);
            var offset = transform.parent != null
                ? transform.parent.InverseTransformVector(worldOffset)
                : worldOffset;

            // The resting position is read when the lunge starts: a walk may still be moving the attacker until then.
            var basePos = transform.localPosition;
            var started = startDelay <= 0f;
            var total = startDelay + duration;
            var windUp = settings.LungeWindUp;
            _lunges[attacker] = LMotion.Create(0f, 1f, total)
                .WithOnComplete(() => transform.localPosition = basePos)
                .Bind(t =>
                {
                    var time = t * total - startDelay;
                    if (time < 0f) return;

                    if (!started)
                    {
                        started = true;
                        basePos = transform.localPosition;
                    }

                    // Wind up (a small step back), strike (fast, the impact at half) and return.
                    var u = Mathf.Clamp01(time / duration);
                    float k;
                    if (u < WindUpEnd)
                        k = -windUp * EaseUtility.Evaluate(u / WindUpEnd, Ease.OutQuad);
                    else if (u < 0.5f)
                        k = Mathf.LerpUnclamped(-windUp, 1f,
                            EaseUtility.Evaluate((u - WindUpEnd) / (0.5f - WindUpEnd), Ease.InQuad));
                    else
                        k = 1f - EaseUtility.Evaluate((u - 0.5f) * 2f, Ease.OutQuad);
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

        /// <summary>
        /// Flash, squash and small recoil, at the impact point of a pending attack. <paramref name="hit"/> is the hit
        /// that caused it, announced by <see cref="HitLandedEvent"/> when the reaction starts.
        /// </summary>
        public void PlayHit(GridEntity entity, GridMovementSettings settings, HitResult hit)
        {
            if (!Application.isPlaying || SimMode.IsActive || entity == null) return;
            if (settings == null || !entity.TryGetComponent(out SpriteRenderer sprite))
            {
                RaiseLanded(entity, hit, Vector3.right);
                return;
            }

            TakePending(entity, out var delay, out var direction);
            Stop(_reactions, entity);

            var transform = entity.transform;
            var feel = GameFeelSettings.Current;

            // A delayed reaction (the entity may still be sliding) reads its resting position when it starts.
            var basePos = transform.localPosition;
            var baseScale = transform.localScale;
            var started = false;
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
                    if (!started) return;

                    transform.localPosition = basePos;
                    transform.localScale = baseScale;
                    sprite.color = baseColor;
                })
                .Bind(t =>
                {
                    var time = t * total - delay;
                    if (time < 0f) return;

                    if (!started)
                    {
                        basePos = transform.localPosition;
                        baseScale = transform.localScale;
                        started = true;
                        SpriteFlash.Play(entity, feel.FlashColor, feel.FlashDuration / Speed);
                        RaiseLanded(entity, hit, direction);
                    }

                    var k = Mathf.Clamp01(time / flash);
                    sprite.color = k < 0.7f ? settings.HitFlashColor : baseColor;
                    transform.localPosition = basePos + offset * (1f - Mathf.Abs(2f * k - 1f));

                    // Squash: wider and shorter at the impact, back to normal at the end.
                    var squash = feel.HitSquash * (1f - k);
                    transform.localScale = new Vector3(
                        baseScale.x * QuantizeScale(1f + squash, sprite, true),
                        baseScale.y * QuantizeScale(1f - squash, sprite, false),
                        baseScale.z);
                })
                .AddTo(entity.gameObject);
        }

        /// <summary>
        /// Flash, pop, then shrink and fade, then destroys the entity. The entity is already logically
        /// dead and out of its cell. <paramref name="hit"/> is the killing hit, announced by
        /// <see cref="HitLandedEvent"/> when the reaction starts.
        /// </summary>
        public void PlayDeath(GridEntity entity, GridMovementSettings settings, HitResult hit)
        {
            if (entity == null) return;
            if (settings == null || !Application.isPlaying || SimMode.IsActive ||
                !entity.TryGetComponent(out SpriteRenderer sprite))
            {
                RaiseLanded(entity, hit, Vector3.right);
                Object.Destroy(entity.gameObject);
                return;
            }

            TakePending(entity, out var delay, out var direction);
            Stop(_reactions, entity);

            var feel = GameFeelSettings.Current;
            var transform = entity.transform;
            var baseScale = transform.localScale;
            var fade = settings.DeathDuration / Speed;
            var total = delay + fade;
            var landed = false;
            var pop = Mathf.Clamp(feel.DeathPopPart, 0.01f, 0.99f);

            _reactions[entity] = LMotion.Create(0f, 1f, total)
                .WithOnComplete(() => Object.Destroy(entity.gameObject))
                .Bind(t =>
                {
                    var time = t * total - delay;
                    if (time < 0f) return;

                    if (!landed)
                    {
                        landed = true;
                        baseScale = transform.localScale;
                        SpriteFlash.Play(entity, feel.FlashColor, feel.FlashDuration / Speed);
                        RaiseLanded(entity, hit, direction);
                    }

                    // Pops up, then shrinks and fades away.
                    var u = Mathf.Clamp01(time / fade);
                    float scale;
                    float alpha;
                    if (u < pop)
                    {
                        scale = Mathf.Lerp(1f, feel.DeathPopScale, EaseUtility.Evaluate(u / pop, Ease.OutQuad));
                        alpha = 1f;
                    }
                    else
                    {
                        var v = (u - pop) / (1f - pop);
                        scale = Mathf.Lerp(feel.DeathPopScale, 0.5f, EaseUtility.Evaluate(v, Ease.InQuad));
                        alpha = 1f - v;
                    }

                    var color = settings.HitFlashColor;
                    color.a = alpha;
                    sprite.color = color;
                    transform.localScale = new Vector3(
                        baseScale.x * QuantizeScale(scale, sprite, true),
                        baseScale.y * QuantizeScale(scale, sprite, false),
                        baseScale.z);
                })
                .AddTo(entity.gameObject);
        }

        /// <summary>Raises <see cref="HitLandedEvent"/> for a character's hit (other entities have no hits).</summary>
        private static void RaiseLanded(GridEntity entity, HitResult hit, Vector3 direction)
        {
            if (!Application.isPlaying || SimMode.IsActive || entity == null || hit.Target == null) return;

            var position = entity.TryGetComponent(out SpriteRenderer sprite) && sprite.sprite != null
                ? sprite.bounds.center
                : entity.transform.position;
            EventBus.Raise(new HitLandedEvent(hit, position, direction));
        }

        /// <summary>
        /// <paramref name="scale"/> rounded so the sprite spans a whole number of its own pixels (width or height),
        /// which keeps the pixel art columns and rows regular while it squashes or pops.
        /// </summary>
        private static float QuantizeScale(float scale, SpriteRenderer sprite, bool horizontal)
        {
            if (sprite.sprite == null) return scale;

            var pixels = horizontal ? sprite.sprite.rect.width : sprite.sprite.rect.height;
            if (pixels <= 0f) return scale;

            return Mathf.Max(1f, Mathf.Round(scale * pixels)) / pixels;
        }

        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            while (IsAnimating)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        private float Speed => Mathf.Max(0.01f, SpeedMultiplier);

        /// <summary>
        /// Seconds the entity's next hit reaction will wait (0 when none is queued), without consuming it: the cell
        /// shows the damage number at the same moment.
        /// </summary>
        public float PeekPendingDelay(GridEntity entity)
        {
            return entity != null && _pendingHits.TryGetValue(entity, out var pending) && Time.time - pending.Time < 1f
                ? pending.Delay
                : 0f;
        }

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
