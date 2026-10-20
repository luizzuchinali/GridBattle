using System.Collections.Generic;
using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Feel
{
    /// <summary>
    /// How hits, kills and pushes feel (juice): screen shake, hit stop, the white flash and squash of the character that
    /// is hit, the pixel particles and the pop of the damage numbers. Everything happens at the visual impact
    /// (<see cref="Events.HitLandedEvent"/>), after the rules already resolved the hit: visual only, nothing here
    /// changes the game. Sizes are in pixels at the reference resolution, so everything stays pixel perfect.
    /// </summary>
    [CreateAssetMenu(fileName = "GameFeelSettings", menuName = "GridBattle/Settings/Game Feel Settings", order = 0)]
    public sealed class GameFeelSettings : ScriptableObject, IGameSettings
    {
        [Header("Screen shake")]
        [SerializeField]
        [Tooltip("Master switch of the screen shake (the player can also turn it off in the Options).")]
        private bool shakeEnabled = true;

        [SerializeField]
        [Min(0)]
        [Tooltip("Largest camera offset, in pixels, reached at full trauma (trauma 1).")]
        private int shakeMaxPixels = 4;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Trauma lost per second (the shake fades out as it drops).")]
        private float shakeDecay = 3f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("New camera offsets per second while shaking (lower reads as chunkier, retro shakes).")]
        private float shakeFrequency = 30f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Trauma of a direct hit (attack, skill, item) on an enemy.")]
        private float shakeHit = 0.12f;

        [SerializeField]
        [Range(0f, 0.2f)]
        [Tooltip("Extra trauma per point of damage of a direct hit.")]
        private float shakePerDamage = 0.03f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Trauma of damage over time, terrain and thorns.")]
        private float shakeIndirect = 0.04f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Extra trauma of a critical hit.")]
        private float shakeCrit = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Trauma of a push or pull collision.")]
        private float shakeCollision = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Extra trauma when the hit kills.")]
        private float shakeKill = 0.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Extra trauma when the player is the one hit.")]
        private float shakePlayerHurt = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Trauma when the player levels up.")]
        private float shakeLevelUp = 0.15f;

        [Header("Hit stop (the game freezes for an instant)")]
        [SerializeField]
        private bool hitStopEnabled = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Freeze of a killing blow, in real seconds.")]
        private float hitStopKill = 0.07f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Freeze of a critical hit, in real seconds.")]
        private float hitStopCrit = 0.05f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Freeze of a push or pull collision, in real seconds.")]
        private float hitStopCollision = 0.05f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Freeze when the player is hit, in real seconds.")]
        private float hitStopPlayerHurt = 0.04f;

        [SerializeField]
        [Min(0)]
        [Tooltip("A direct hit of at least this damage also freezes (0 = only kills, crits, collisions and the player).")]
        private int hitStopHeavyDamage = 4;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Freeze of a heavy direct hit, in real seconds.")]
        private float hitStopHeavy = 0.03f;

        [Header("Flash and squash of the character hit")]
        [SerializeField]
        [Tooltip("Material that paints a sprite in one solid color (the white silhouette of the flash). Empty = no flash.")]
        private Material flashMaterial;

        [SerializeField]
        private Color flashColor = Color.white;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds the white silhouette stays over the character hit (it freezes with the hit stop).")]
        private float flashDuration = 0.06f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("Squash of the character hit: wider and shorter by this fraction, back to normal over the reaction. " +
                 "Rounded to whole sprite pixels.")]
        private float hitSquash = 0.15f;

        [SerializeField]
        [Range(1f, 2f)]
        [Tooltip("Scale a dying enemy pops to before it shrinks and fades. Rounded to whole sprite pixels.")]
        private float deathPopScale = 1.25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Part of the death animation spent popping (the rest shrinks and fades).")]
        private float deathPopPart = 0.3f;

        [Header("Pixel particles")]
        [SerializeField]
        private bool particlesEnabled = true;

        [SerializeField]
        [Min(1)]
        [Tooltip("Size of one particle, in pixels.")]
        private int particlePixels = 1;

        [SerializeField]
        [Tooltip("Sorting order of the particles (characters draw at 99).")]
        private int particleSortingOrder = 150;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Downward acceleration of the particles, in pixels per second squared.")]
        private float particleGravity = 420f;

        [SerializeField]
        private ParticleBurst deathBurst = new()
        {
            count = 16,
            minSpeed = 50f,
            maxSpeed = 120f,
            upwardBias = 50f,
            minLifetime = 0.25f,
            maxLifetime = 0.5f,
            colors = new List<Color>
            {
                Color.white,
                new(0.75f, 0.8f, 0.86f),
                new(0.89f, 0.23f, 0.27f),
                new(0.97f, 0.46f, 0.13f),
            },
        };

        [SerializeField]
        private ParticleBurst collisionBurst = new()
        {
            count = 8,
            minSpeed = 25f,
            maxSpeed = 60f,
            upwardBias = 20f,
            minLifetime = 0.2f,
            maxLifetime = 0.35f,
            colors = new List<Color>
            {
                new(0.92f, 0.83f, 0.67f),
                new(0.75f, 0.8f, 0.86f),
                new(0.55f, 0.61f, 0.71f),
            },
        };

        [SerializeField]
        private ParticleBurst critBurst = new()
        {
            count = 8,
            minSpeed = 60f,
            maxSpeed = 110f,
            upwardBias = 30f,
            minLifetime = 0.15f,
            maxLifetime = 0.3f,
            colors = new List<Color> { Color.white, new(1f, 0.91f, 0.38f), new(1f, 0.68f, 0.2f) },
        };

        [SerializeField]
        private ParticleBurst levelUpBurst = new()
        {
            count = 20,
            minSpeed = 30f,
            maxSpeed = 90f,
            upwardBias = 80f,
            minLifetime = 0.4f,
            maxLifetime = 0.7f,
            colors = new List<Color> { Color.white, new(1f, 0.91f, 0.38f), new(0.17f, 0.91f, 0.96f) },
        };

        [Header("Damage numbers")]
        [SerializeField]
        [Min(1f)]
        [Tooltip("Scale the number pops to when it appears, before settling at its size.")]
        private float numberPopScale = 1.6f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds of the pop.")]
        private float numberPopDuration = 0.12f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Size of a critical hit's number, relative to a normal one.")]
        private float critNumberScale = 1.4f;

        [SerializeField]
        private Color critNumberColor = new(1f, 0.91f, 0.38f);

        [SerializeField]
        private Color healNumberColor = new(0.39f, 0.78f, 0.3f);

        public static GameFeelSettings Current => GameSettings.Get<GameFeelSettings>();

        public bool ShakeEnabled => shakeEnabled;
        public int ShakeMaxPixels => shakeMaxPixels;
        public float ShakeDecay => shakeDecay;
        public float ShakeFrequency => shakeFrequency;
        public float ShakeHit => shakeHit;
        public float ShakePerDamage => shakePerDamage;
        public float ShakeIndirect => shakeIndirect;
        public float ShakeCrit => shakeCrit;
        public float ShakeCollision => shakeCollision;
        public float ShakeKill => shakeKill;
        public float ShakePlayerHurt => shakePlayerHurt;
        public float ShakeLevelUp => shakeLevelUp;

        public bool HitStopEnabled => hitStopEnabled;
        public float HitStopKill => hitStopKill;
        public float HitStopCrit => hitStopCrit;
        public float HitStopCollision => hitStopCollision;
        public float HitStopPlayerHurt => hitStopPlayerHurt;
        public int HitStopHeavyDamage => hitStopHeavyDamage;
        public float HitStopHeavy => hitStopHeavy;

        public Material FlashMaterial => flashMaterial;
        public Color FlashColor => flashColor;
        public float FlashDuration => flashDuration;
        public float HitSquash => hitSquash;
        public float DeathPopScale => deathPopScale;
        public float DeathPopPart => deathPopPart;

        public bool ParticlesEnabled => particlesEnabled;
        public int ParticlePixels => particlePixels;
        public int ParticleSortingOrder => particleSortingOrder;
        public float ParticleGravity => particleGravity;
        public ParticleBurst DeathBurst => deathBurst;
        public ParticleBurst CollisionBurst => collisionBurst;
        public ParticleBurst CritBurst => critBurst;
        public ParticleBurst LevelUpBurst => levelUpBurst;

        public float NumberPopScale => numberPopScale;
        public float NumberPopDuration => numberPopDuration;
        public float CritNumberScale => critNumberScale;
        public Color CritNumberColor => critNumberColor;
        public Color HealNumberColor => healNumberColor;
    }

    /// <summary>One burst of pixel particles: how many, how fast, how long and which colors (picked at random).</summary>
    [System.Serializable]
    public sealed class ParticleBurst
    {
        [Min(0)]
        public int count = 8;

        [Min(0f)]
        [Tooltip("Pixels per second.")]
        public float minSpeed = 40f;

        [Min(0f)]
        [Tooltip("Pixels per second.")]
        public float maxSpeed = 90f;

        [Tooltip("Extra upward speed, in pixels per second (the particles jump before gravity pulls them).")]
        public float upwardBias = 30f;

        [Min(0.01f)]
        public float minLifetime = 0.2f;

        [Min(0.01f)]
        public float maxLifetime = 0.4f;

        public List<Color> colors = new() { Color.white };
    }
}
