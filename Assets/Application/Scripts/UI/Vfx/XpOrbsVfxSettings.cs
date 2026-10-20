using LitMotion;
using UnityEngine;

namespace GridBattle.UI.Vfx
{
    /// <summary>
    /// Visual settings for XP orbs. Being an asset, it can be tweaked in Play Mode
    /// without losing the values on exit. A kill's orbs burst out of the enemy
    /// together, hold for a moment and fly to the bar together, so the whole
    /// delivery takes about BurstDuration + HoldDuration + LaunchJitter +
    /// FlightDuration, whatever the amount of XP.
    /// </summary>
    [CreateAssetMenu(fileName = "XpOrbsVfxSettings", menuName = "GridBattle/VFX/Xp Orbs Settings", order = 0)]
    public class XpOrbsVfxSettings : ScriptableObject
    {
        [SerializeField]
        private Sprite orbSprite;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Orb size, in panel px.")]
        private float orbSize = 8f;

        [Header("Burst")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Distance the orbs scatter from the death point, in panel px.")]
        private float burstRadius = 14f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Random variation (+/-) of each orb's burst distance, in panel px.")]
        private float burstRadiusVariance = 6f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Random variation of each orb's burst direction, as a fraction of the even angle between orbs " +
                 "(0 = evenly spaced around the death point).")]
        private float burstAngleJitter = 0.5f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Duration of the burst, in seconds.")]
        private float burstDuration = 0.22f;

        [SerializeField]
        private Ease burstEase = Ease.OutCubic;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pause between the burst and the flight to the bar, in seconds.")]
        private float holdDuration = 0.08f;

        [Header("Flight")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum random delay of each orb's departure, in seconds, so they leave together but not in lockstep.")]
        private float launchJitter = 0.06f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Flight duration of each orb, in seconds.")]
        private float flightDuration = 0.45f;

        [SerializeField]
        [Tooltip("Flight arc height, in panel px.")]
        private float arcHeight = 60f;

        [SerializeField]
        private Ease flightEase = Ease.InQuad;

        public Sprite OrbSprite => orbSprite;
        public float OrbSize => orbSize;
        public float BurstRadius => burstRadius;
        public float BurstRadiusVariance => burstRadiusVariance;
        public float BurstAngleJitter => burstAngleJitter;
        public float BurstDuration => burstDuration;
        public Ease BurstEase => burstEase;
        public float HoldDuration => holdDuration;
        public float LaunchJitter => launchJitter;
        public float FlightDuration => flightDuration;
        public float ArcHeight => arcHeight;
        public Ease FlightEase => flightEase;
    }
}
