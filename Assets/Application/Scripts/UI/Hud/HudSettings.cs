using GridBattle.Data;
using UnityEngine;
using ZS.UI.Navigation;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// Settings of the battle HUD and the entity details window (GDD 4.1 / 4.2). Open questions of the
    /// interface document are fields here, with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "HudSettings", menuName = "GridBattle/Settings/HUD Settings", order = 0)]
    public sealed class HudSettings : ScriptableObject, IGameSettings
    {
        [Header("Views")]
        [SerializeField]
        [Tooltip("Modal opened by a long tap (or right click) on a cell: details of the entity and the terrain there.")]
        private ViewDefinition entityDetailsView;

        [Header("Entity details")]
        [SerializeField]
        [Tooltip("List every attribute in the details window. Off = only the relevant ones: walk, attack, " +
                 "damage and defense always, the others when they are not zero.")]
        private bool showAllAttributes;

        [Header("HUD")]
        [SerializeField]
        [Tooltip("Open question (interface 4.2, depth): the depth next to the XP bar is hidden while the " +
                 "run depth is 0 or lower (no run, or the start).")]
        private bool hideDepthWhenZero = true;

        public ViewDefinition EntityDetailsView => entityDetailsView;
        public bool ShowAllAttributes => showAllAttributes;
        public bool HideDepthWhenZero => hideDepthWhenZero;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static HudSettings Current => GameSettings.Get<HudSettings>();
    }
}
