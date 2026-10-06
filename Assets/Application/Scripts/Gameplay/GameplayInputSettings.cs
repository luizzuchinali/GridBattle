using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Input rules of the battle (GDD 4.1). Open questions of the interface document are
    /// fields here, with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayInputSettings", menuName = "GridBattle/Settings/Gameplay Input Settings", order = 0)]
    public sealed class GameplayInputSettings : ScriptableObject, IGameSettings
    {
        [Header("Entity details")]
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Open question (interface 4.1, 'tempo de espera'): seconds a touch must stay on a cell to open the " +
                 "entity details (long tap) instead of walking or attacking. Does not apply to the mouse: " +
                 "the right button opens the details right away.")]
        private float longPressSeconds = 0.45f;

        /// <summary>Seconds a touch has to be held on a cell to count as a long tap.</summary>
        public float LongPressSeconds => Mathf.Max(0.1f, longPressSeconds);

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static GameplayInputSettings Current => GameSettings.Get<GameplayInputSettings>();
    }
}
