using LitMotion;
using UnityEngine;

namespace GridBattle.UI.Vfx
{
    /// <summary>
    /// Visual settings for XP orbs. Being an asset, it can be tweaked in Play Mode
    /// without losing the values on exit.
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

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Flight duration of each orb, in seconds.")]
        private float flightDuration = 0.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Interval between one orb's launch and the next, in seconds.")]
        private float staggerDelay = 0.08f;

        [SerializeField]
        [Tooltip("Flight arc height, in panel px.")]
        private float arcHeight = 60f;

        [SerializeField]
        private Ease flightEase = Ease.InQuad;

        public Sprite OrbSprite => orbSprite;
        public float OrbSize => orbSize;
        public float FlightDuration => flightDuration;
        public float StaggerDelay => staggerDelay;
        public float ArcHeight => arcHeight;
        public Ease FlightEase => flightEase;
    }
}
