using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    public enum EMovementStyle
    {
        Hop,
        Flip
    }

    /// <summary>
    /// How grid entities animate when they change cells: either small hops
    /// (<see cref="EMovementStyle.Hop"/>) or a horizontal flip that closes at
    /// the old cell and opens at the new one (<see cref="EMovementStyle.Flip"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "GridMovementSettings", menuName = "GridBattle/Grid Movement Settings", order = 20)]
    public class GridMovementSettings : ScriptableObject
    {
        [Header("Style")]
        [SerializeField]
        private EMovementStyle style = EMovementStyle.Flip;

        [Header("Hop")]
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Hops per cell of distance (the total is rounded, minimum 1).")]
        private float hopsPerCell = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Peak height of each hop, in world units (PPU 100: 0.04 = 4px).")]
        private float hopHeight = 0.04f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Duration of a single hop, in seconds.")]
        private float hopDuration = 0.12f;

        [SerializeField]
        private Ease hopEase = Ease.InOutSine;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Vertical flattening on landing, in pixels.")]
        private float landingSquash = 2f;

        [SerializeField]
        [Min(0.01f)]
        private float landingSquashDuration = 0.06f;

        [Header("Flip")]
        [SerializeField]
        [Min(0.01f)]
        private float flipCloseDuration = 0.07f;

        [SerializeField]
        [Min(0.01f)]
        private float flipOpenDuration = 0.09f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Extra horizontal scale reached while opening before settling to 1.")]
        private float flipOvershoot = 0.1f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Quantizes the flip's horizontal scale in this many steps, to keep pixel art columns regular (0 = continuous).")]
        private int scaleSteps = 4;

        [SerializeField]
        private Ease flipCloseEase = Ease.InQuad;

        [SerializeField]
        private Ease flipOpenEase = Ease.OutQuad;

        [Header("Arrival")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Size of the pulse played on the destination cell, in pixels.")]
        private float arrivalPulsePixels = 2f;

        [SerializeField]
        [Min(0.01f)]
        private float arrivalPulseDuration = 0.08f;

        [Header("Slide (pushed and pulled characters)")]
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds a pushed or pulled character takes to slide across one cell (no hop: it is thrown).")]
        private float slideSecondsPerCell = 0.06f;

        [SerializeField]
        private Ease slideEase = Ease.OutQuad;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Extra seconds between the end of the slide and the collision reaction (flash and bump) of the characters involved.")]
        private float collisionBumpDelay = 0.02f;

        [Header("Combat")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("How far the attacker advances toward the target, in pixels.")]
        private float lungePixels = 5f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Total duration of the attack lunge (out and back). Impact is at half.")]
        private float lungeDuration = 0.15f;

        [SerializeField]
        private Color hitFlashColor = new(1f, 0.3f, 0.3f, 1f);

        [SerializeField]
        [Min(0.01f)]
        private float hitFlashDuration = 0.1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Recoil away from the attacker on hit, in pixels.")]
        private float hitRecoilPixels = 2f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Fade-out duration before a dead enemy is destroyed.")]
        private float deathDuration = 0.15f;

        public float SlideSecondsPerCell => slideSecondsPerCell;
        public Ease SlideEase => slideEase;
        public float CollisionBumpDelay => collisionBumpDelay;
        public float LungePixels => lungePixels;
        public float LungeDuration => lungeDuration;
        public Color HitFlashColor => hitFlashColor;
        public float HitFlashDuration => hitFlashDuration;
        public float HitRecoilPixels => hitRecoilPixels;
        public float DeathDuration => deathDuration;
        public EMovementStyle Style => style;
        public float HopsPerCell => hopsPerCell;
        public float HopHeight => hopHeight;
        public float HopDuration => hopDuration;
        public Ease HopEase => hopEase;
        public float LandingSquash => landingSquash;
        public float LandingSquashDuration => landingSquashDuration;
        public float FlipCloseDuration => flipCloseDuration;
        public float FlipOpenDuration => flipOpenDuration;
        public float FlipOvershoot => flipOvershoot;
        public int ScaleSteps => scaleSteps;
        public Ease FlipCloseEase => flipCloseEase;
        public Ease FlipOpenEase => flipOpenEase;
        public float ArrivalPulsePixels => arrivalPulsePixels;
        public float ArrivalPulseDuration => arrivalPulseDuration;
    }
}
