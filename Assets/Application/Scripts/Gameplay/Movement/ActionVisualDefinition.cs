using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>What the caster does when an action starts.</summary>
    public enum ECasterGesture
    {
        None,

        /// <summary>The attack lunge toward the target (the one of basic attacks).</summary>
        Lunge,

        /// <summary>The caster flashes in <see cref="ActionVisualDefinition.GlowColor"/> (spells, items).</summary>
        Glow
    }

    /// <summary>In which order the cells of the area play their effect (this is what draws the shape).</summary>
    public enum EVisualCellOrder
    {
        /// <summary>Every cell at once.</summary>
        Simultaneous,

        /// <summary>A wave from the aimed cell outward.</summary>
        RippleFromTarget,

        /// <summary>From the cell nearest to the caster to the farthest (lines, cones).</summary>
        AwayFromCaster,

        /// <summary>From one side to the other, across the caster-to-target direction (sweeps).</summary>
        Sweep
    }

    /// <summary>Which way directional art (drawn facing right) turns, in 90° steps.</summary>
    public enum EVisualFacing
    {
        /// <summary>It does not turn (round art).</summary>
        None,

        /// <summary>Toward the caster-to-target direction (slashes, thrusts).</summary>
        CasterToTarget,

        /// <summary>
        /// Away from the aimed cell, the way a push from the center of the area moves each character (wind, blasts).
        /// The aimed cell itself uses the caster-to-target direction.
        /// </summary>
        AwayFromTarget
    }

    /// <summary>
    /// How a skill or an item looks when used, built from small reusable pieces instead of an animation of the
    /// whole action: a caster gesture, an optional projectile from the caster to the aimed cell, and a short effect
    /// played on every cell of the area in an order that draws the shape. Every effect fits a square box of the
    /// cell's width, so the same art serves rows and columns of the rectangular cells; directional art is drawn
    /// facing right and turned in 90° steps. The hit reaction and the damage number of each character wait for
    /// the impact on its cell. Only visual: rules never read it, and the balance simulation skips it.
    /// </summary>
    [CreateAssetMenu(fileName = "ActionVisual", menuName = "GridBattle/Vfx/Action Visual")]
    public sealed class ActionVisualDefinition : ScriptableObject
    {
        [Header("Caster")]
        [SerializeField]
        [Tooltip("What the caster does first: the attack lunge, a flash in the glow color or nothing.")]
        private ECasterGesture casterGesture = ECasterGesture.Lunge;

        [SerializeField]
        private Color glowColor = Color.white;

        [Header("Projectile (optional)")]
        [SerializeField]
        [Tooltip("Frames of something flying from the caster to the aimed cell (empty = no projectile). Drawn facing right.")]
        private List<Sprite> projectileFrames = new();

        [SerializeField]
        [Min(1f)]
        [Tooltip("Cells per second the projectile travels.")]
        private float projectileCellsPerSecond = 10f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Height of the projectile's arc, in cells (0 = straight; a thrown bomb uses about 0.6).")]
        private float projectileArc;

        [SerializeField]
        [Tooltip("The projectile turns (in 90° steps) to face its direction. Off for round projectiles.")]
        private bool projectileFacesDirection;

        [Header("Effect on each cell of the area")]
        [SerializeField]
        [Tooltip("Frames played on every cell of the area. Keep them within a square of the cell's width.")]
        private List<Sprite> cellFrames = new();

        [SerializeField]
        [Min(0.01f)]
        private float frameSeconds = 0.06f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Frame at which the hit lands: the hit reaction and the damage number of the character on the cell wait for it.")]
        private int impactFrame = 1;

        [SerializeField]
        private EVisualCellOrder order = EVisualCellOrder.Simultaneous;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds between one step of the order and the next.")]
        private float stepSeconds = 0.05f;

        [SerializeField]
        [FormerlySerializedAs("cellFacesDirection")]
        [Tooltip("Which way the cell effect (drawn facing right) turns: not at all, toward the caster-to-target " +
                 "direction (slashes) or away from the aimed cell (pushes from the center). The hit reactions follow it.")]
        private EVisualFacing cellFacing;

        [SerializeField]
        [Min(0)]
        [Tooltip("The cell effect falls from this many pixels above before it plays (lightning from the sky). 0 = no fall.")]
        private int dropPixels;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds the last frame stays, fading out (clouds of smoke or poison). 0 = it disappears at once.")]
        private float lingerSeconds;

        [SerializeField]
        [Tooltip("Color multiplied over the cell frames and the projectile.")]
        private Color tint = Color.white;

        public ECasterGesture CasterGesture => casterGesture;
        public Color GlowColor => glowColor;
        public IReadOnlyList<Sprite> ProjectileFrames => projectileFrames;
        public float ProjectileCellsPerSecond => Mathf.Max(1f, projectileCellsPerSecond);
        public float ProjectileArc => projectileArc;
        public bool ProjectileFacesDirection => projectileFacesDirection;
        public IReadOnlyList<Sprite> CellFrames => cellFrames;
        public float FrameSeconds => Mathf.Max(0.01f, frameSeconds);
        public int ImpactFrame => Mathf.Clamp(impactFrame, 0, Mathf.Max(0, cellFrames.Count - 1));
        public EVisualCellOrder Order => order;
        public float StepSeconds => stepSeconds;
        public EVisualFacing CellFacing => cellFacing;
        public int DropPixels => dropPixels;
        public float LingerSeconds => lingerSeconds;
        public Color Tint => tint;

        /// <summary>Whether a projectile flies before the cell effects.</summary>
        public bool HasProjectile => projectileFrames.Count > 0 && projectileFrames[0] != null;
    }
}
