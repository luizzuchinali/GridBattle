using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Forecast
{
    /// <summary>
    /// Look of the battle forecast (Into the Breach style clarity): the order the enemies act in, the number on the
    /// area of each prepared strike, the damage a strike or the player's aimed action would deal (shown on the health
    /// bars) and the arrows and ghosts of the pushes, pulls and swaps an aimed action would cause. Visual only.
    /// Sizes and offsets are in pixels at the reference resolution, so everything stays pixel perfect.
    /// </summary>
    [CreateAssetMenu(fileName = "ForecastSettings", menuName = "GridBattle/Settings/Forecast Settings", order = 0)]
    public sealed class ForecastSettings : ScriptableObject, IGameSettings
    {
        [Header("Acting order")]
        [SerializeField]
        [Tooltip("Show over every enemy the place it acts in during the enemy phase (1 = first).")]
        private bool showActingOrder = true;

        [SerializeField]
        [Tooltip("Show the caster's number on the aimed cell of each prepared strike, so each area reads as whose.")]
        private bool showOrderOnStrikes = true;

        [SerializeField]
        [Tooltip("Offset of the strike number from the center of its aimed cell, in pixels.")]
        private Vector2 strikeOrderOffset = new(-11f, 16f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pixels between the numbers of several strikes aimed at the same cell (wide enough that 1 and 3 do not " +
                 "read as 13).")]
        private float strikeOrderSpacing = 9f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Font size of the order numbers (world text, like the damage numbers).")]
        private float orderFontSize = 1.5f;

        [SerializeField]
        private Color orderColor = Color.white;

        [SerializeField]
        [Tooltip("Color of the order number of a strike that fires in a later enemy phase (not the next one).")]
        private Color laterStrikeColor = new(0.75f, 0.75f, 0.75f, 1f);

        [Header("Predicted damage")]
        [SerializeField]
        [Tooltip("Show on the health bars the damage the prepared strikes (or the aimed action) would deal.")]
        private bool showPredictedDamage = true;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Seconds of one blink of the part of the health bar a hit would take away.")]
        private float blinkPeriod = 0.6f;

        [SerializeField]
        private Color damageTextColor = new(1f, 0.85f, 0.1f, 1f);

        [SerializeField]
        [Tooltip("Color of the predicted damage when it would kill.")]
        private Color lethalTextColor = new(1f, 0.25f, 0.2f, 1f);

        [SerializeField]
        [Tooltip("Offset of the predicted damage number from the cell's center, in pixels.")]
        private Vector2 damageTextOffset = new(11f, -8f);

        [Header("Aimed action")]
        [SerializeField]
        private Color arrowColor = new(0.4f, 0.9f, 1f, 1f);

        [SerializeField]
        [Tooltip("Pixels the push or pull arrow is moved from the character's cell center, toward the movement.")]
        private float arrowOffset = 10f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Opacity of the ghost drawn where a pushed, pulled, swapped or dashing character would end.")]
        private float ghostAlpha = 0.45f;

        public bool ShowActingOrder => showActingOrder;
        public bool ShowOrderOnStrikes => showOrderOnStrikes;
        public Vector2 StrikeOrderOffset => strikeOrderOffset;
        public float StrikeOrderSpacing => strikeOrderSpacing;
        public float OrderFontSize => orderFontSize;
        public Color OrderColor => orderColor;
        public Color LaterStrikeColor => laterStrikeColor;
        public bool ShowPredictedDamage => showPredictedDamage;
        public float BlinkPeriod => Mathf.Max(0.1f, blinkPeriod);
        public Color DamageTextColor => damageTextColor;
        public Color LethalTextColor => lethalTextColor;
        public Vector2 DamageTextOffset => damageTextOffset;
        public Color ArrowColor => arrowColor;
        public float ArrowOffset => arrowOffset;
        public float GhostAlpha => ghostAlpha;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static ForecastSettings Current => GameSettings.Get<ForecastSettings>();
    }
}
