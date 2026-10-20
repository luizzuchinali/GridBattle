using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace GridBattle.Gameplay.Feel
{
    /// <summary>
    /// The juice of the battle (<see cref="GameFeelSettings"/>): screen shake, hit stop and pixel particles at the
    /// impact of every hit (<see cref="HitLandedEvent"/>) and when the player levels up. Added to the grid by
    /// <see cref="GridController"/> in Play Mode. Visual only: the rules already resolved everything, and nothing
    /// happens in the simulation.
    /// </summary>
    public sealed class GameFeel : MonoBehaviour
    {
        private struct Particle
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
        }

        private static Sprite _pixel;

        private readonly List<Particle> _particles = new();
        private readonly Stack<SpriteRenderer> _pool = new();
        private Transform _particleRoot;

        private Camera _shakenCamera;
        private Vector3 _shakeRestPosition;
        private Vector2Int _shakeOffset;
        private float _trauma;
        private float _nextShakeStep;

        private bool _stopping;
        private float _stopUntil;
        private float _timeScaleBeforeStop = 1f;

        private static GameFeelSettings Settings => GameFeelSettings.Current;
        private static float PixelSize => 1f / GameConfigManager.Ppu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _pixel = null;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            EventBus.Subscribe<HitLandedEvent>(OnHitLanded);
            EventBus.Subscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HitLandedEvent>(OnHitLanded);
            EventBus.Unsubscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            EndHitStop();
            _trauma = 0f;
            _shakeOffset = Vector2Int.zero;
            ClearParticles();
        }

        /// <summary>Adds screen shake (0..1 trauma; the offset grows with its square and fades by itself).</summary>
        public void Shake(float trauma)
        {
            var settings = Settings;
            if (trauma <= 0f || !settings.ShakeEnabled || !ProfileService.ScreenShakeEnabled || SimMode.IsActive) return;

            _trauma = Mathf.Clamp01(_trauma + trauma);
        }

        /// <summary>Freezes the game for <paramref name="seconds"/> of real time (overlapping stops extend it).</summary>
        public void HitStop(float seconds)
        {
            if (seconds <= 0f || !Settings.HitStopEnabled || SimMode.IsActive) return;

            if (!_stopping)
            {
                _timeScaleBeforeStop = Time.timeScale;
                _stopping = true;
                _stopUntil = Time.unscaledTime;
            }

            _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + seconds);
            Time.timeScale = 0f;
        }

        /// <summary>Throws a burst of pixel particles from <paramref name="position"/> (world).</summary>
        public void Burst(ParticleBurst burst, Vector3 position, Vector3 direction)
        {
            var settings = Settings;
            if (burst == null || burst.count <= 0 || !settings.ParticlesEnabled || SimMode.IsActive) return;

            direction.z = 0f;
            var hasDirection = direction.sqrMagnitude > 0.0001f;
            for (var i = 0; i < burst.count; i++)
            {
                // Spread all around, leaning away from the attacker when there is one.
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (hasDirection)
                    dir = (dir + (Vector2)direction.normalized * 0.8f).normalized;

                var speed = Random.Range(burst.minSpeed, burst.maxSpeed);
                var velocity = dir * speed + Vector2.up * burst.upwardBias;
                var color = burst.colors != null && burst.colors.Count > 0
                    ? burst.colors[Random.Range(0, burst.colors.Count)]
                    : Color.white;
                Spawn(position, velocity * PixelSize, Random.Range(burst.minLifetime, burst.maxLifetime), color);
            }
        }

        private void OnHitLanded(HitLandedEvent e)
        {
            if (SimMode.IsActive) return;

            var settings = Settings;
            var hit = e.Hit;
            var direct = hit.IsDirect;
            var collision = hit.Kind == EDamageKind.Collision;
            var onPlayer = hit.Target is PlayerCharacter;

            var trauma = collision
                ? settings.ShakeCollision
                : direct
                    ? settings.ShakeHit + settings.ShakePerDamage * Mathf.Max(0, hit.Damage)
                    : settings.ShakeIndirect;
            if (hit.IsCrit) trauma += settings.ShakeCrit;
            if (hit.Killed) trauma += settings.ShakeKill;
            if (onPlayer) trauma += settings.ShakePlayerHurt;
            Shake(trauma);

            var stop = 0f;
            if (hit.Killed) stop = Mathf.Max(stop, settings.HitStopKill);
            if (hit.IsCrit) stop = Mathf.Max(stop, settings.HitStopCrit);
            if (collision) stop = Mathf.Max(stop, settings.HitStopCollision);
            if (onPlayer && (direct || collision)) stop = Mathf.Max(stop, settings.HitStopPlayerHurt);
            if (direct && settings.HitStopHeavyDamage > 0 && hit.Damage >= settings.HitStopHeavyDamage)
                stop = Mathf.Max(stop, settings.HitStopHeavy);
            HitStop(stop);

            if (hit.Killed)
                Burst(settings.DeathBurst, e.Position, e.Direction);
            if (collision)
                Burst(settings.CollisionBurst, e.Position, -e.Direction);
            if (hit.IsCrit)
                Burst(settings.CritBurst, e.Position, e.Direction);
        }

        private void OnPlayerLeveledUp(PlayerLeveledUpEvent e)
        {
            if (SimMode.IsActive || e.Player == null) return;

            var settings = Settings;
            Shake(settings.ShakeLevelUp);
            var position = e.Player.TryGetComponent(out SpriteRenderer sprite)
                ? sprite.bounds.center
                : e.Player.transform.position;
            Burst(settings.LevelUpBurst, position, Vector3.zero);
        }

        private void Update()
        {
            if (_stopping && Time.unscaledTime >= _stopUntil)
                EndHitStop();

            UpdateShake(Time.unscaledDeltaTime);
            UpdateParticles(Time.deltaTime);
        }

        private void EndHitStop()
        {
            if (!_stopping) return;

            _stopping = false;

            // Someone else changed the time scale during the stop: leave theirs.
            if (Time.timeScale == 0f)
                Time.timeScale = _timeScaleBeforeStop;
        }

        private void UpdateShake(float deltaTime)
        {
            if (_trauma <= 0f)
            {
                _shakeOffset = Vector2Int.zero;
                return;
            }

            var settings = Settings;
            _trauma = Mathf.Max(0f, _trauma - settings.ShakeDecay * deltaTime);
            if (Time.unscaledTime < _nextShakeStep) return;

            _nextShakeStep = Time.unscaledTime + 1f / settings.ShakeFrequency;
            var amount = settings.ShakeMaxPixels * _trauma * _trauma;
            _shakeOffset = new Vector2Int(ShakeStep(amount), ShakeStep(amount));
        }

        /// <summary>
        /// A random whole-pixel step of up to <paramref name="amount"/> pixels. Rounded away from zero, so even a
        /// light hit moves the picture by one pixel while the trauma lasts.
        /// </summary>
        private static int ShakeStep(float amount)
        {
            var step = Random.Range(-1f, 1f) * amount;
            return (int)(Mathf.Sign(step) * Mathf.Ceil(Mathf.Abs(step)));
        }

        /// <summary>
        /// The shake moves the camera only while it renders, so every system that reads the camera (the HUD placed
        /// under the grid, the XP orbs) still sees it at rest. Whole pixels keep the picture pixel perfect.
        /// </summary>
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_shakeOffset == Vector2Int.zero || camera != Camera.main) return;

            _shakenCamera = camera;
            _shakeRestPosition = camera.transform.position;
            camera.transform.position = _shakeRestPosition + new Vector3(_shakeOffset.x, _shakeOffset.y, 0f) * PixelSize;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _shakenCamera || _shakenCamera == null) return;

            camera.transform.position = _shakeRestPosition;
            _shakenCamera = null;
        }

        private void Spawn(Vector3 position, Vector2 velocity, float life, Color color)
        {
            if (_particleRoot == null)
            {
                _particleRoot = new GameObject("FeelParticles").transform;
                _particleRoot.SetParent(transform, false);
            }

            var sprite = _pool.Count > 0 ? _pool.Pop() : CreateParticle();
            sprite.color = color;
            sprite.sortingOrder = Settings.ParticleSortingOrder;
            sprite.transform.localScale = Vector3.one * Settings.ParticlePixels;
            sprite.gameObject.SetActive(true);

            var particle = new Particle
            {
                Renderer = sprite,
                Position = position,
                Velocity = velocity,
                Life = life,
            };
            Place(particle, position.z);
            _particles.Add(particle);
        }

        private SpriteRenderer CreateParticle()
        {
            if (_pixel == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), GameConfigManager.Ppu);
                _pixel.name = "FeelPixel";
            }

            var go = new GameObject("Particle");
            go.transform.SetParent(_particleRoot, false);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = _pixel;
            return sprite;
        }

        private void UpdateParticles(float deltaTime)
        {
            if (_particles.Count == 0) return;

            var gravity = Settings.ParticleGravity * PixelSize;
            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                particle.Life -= deltaTime;
                if (particle.Life <= 0f || particle.Renderer == null)
                {
                    Release(particle.Renderer);
                    _particles.RemoveAt(i);
                    continue;
                }

                particle.Velocity.y -= gravity * deltaTime;
                particle.Position += particle.Velocity * deltaTime;
                Place(particle, particle.Renderer.transform.position.z);
                _particles[i] = particle;
            }
        }

        /// <summary>Snaps the particle to the pixel grid, so it moves in whole pixels like the art.</summary>
        private static void Place(Particle particle, float z)
        {
            var size = PixelSize;
            particle.Renderer.transform.position = new Vector3(
                Mathf.Round(particle.Position.x / size) * size,
                Mathf.Round(particle.Position.y / size) * size,
                z);
        }

        private void Release(SpriteRenderer sprite)
        {
            if (sprite == null) return;

            sprite.gameObject.SetActive(false);
            _pool.Push(sprite);
        }

        private void ClearParticles()
        {
            foreach (var particle in _particles)
                Release(particle.Renderer);
            _particles.Clear();
        }
    }
}
