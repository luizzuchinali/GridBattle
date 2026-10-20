using GridBattle.Data;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using UnityEngine;
using UnityEngine.Serialization;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Settings of the run screens (map, node preview, main menu): node icons, colors and the layout of the map
    /// in reference pixels (216x384). Open questions of the interface documents are fields here, with neutral
    /// defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "ScreensSettings", menuName = "GridBattle/Settings/Screens Settings", order = 1)]
    public sealed class ScreensSettings : ScriptableObject, IGameSettings
    {
        [Header("Map layout (reference pixels)")]
        [SerializeField]
        [Min(96)]
        [Tooltip("Width of the node map. The lanes share it equally.")]
        private int mapWidth = 200;

        [SerializeField]
        [Min(32)]
        [Tooltip("Vertical distance between the centers of two floors. Keep it even and above the node size so the " +
                 "connection lines show between the floors.")]
        private int floorSpacing = 44;

        [SerializeField]
        [Min(16)]
        [Tooltip("Side of a node button. 32 matches the slot skin pixel for pixel.")]
        private int nodeSize = 32;

        [SerializeField]
        [Min(0)]
        [Tooltip("Free space above the boss and below the first floor.")]
        private int mapPadding = 8;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("When the map opens, the floor with the nodes to choose is placed at this height of the viewport " +
                 "(0 = top, 1 = bottom).")]
        private float scrollFocus = 0.6f;

        [Header("Map nodes")]
        [SerializeField]
        [Tooltip("A normal battle. Replace the PNGs to change the art.")]
        private Sprite battleNormalIcon;

        [SerializeField]
        [Tooltip("A battle with an elite (more XP; its reward: a consumable and a skill upgrade). It must read apart " +
                 "from the normal battle at a glance: the map shows no XP. Empty = the normal battle icon.")]
        private Sprite battleEliteIcon;

        [SerializeField]
        private Sprite healIcon;

        [SerializeField]
        private Sprite talentIcon;

        [SerializeField]
        private Sprite consumableIcon;

        [SerializeField]
        private Sprite bossIcon;

        [Header("Map lines")]
        [SerializeField]
        [Min(1)]
        private int lineWidth = 2;

        [SerializeField]
        [Tooltip("Connections the player walked.")]
        private Color pathColor = new(1f, 0.8f, 0f, 1f);

        [SerializeField]
        [Tooltip("Connections from the current node to the nodes that can be chosen now.")]
        private Color availableColor = new(0.91f, 0.87f, 0.79f, 1f);

        [SerializeField]
        [Tooltip("Every other connection.")]
        private Color dimColor = new(0.35f, 0.31f, 0.27f, 1f);

        [Header("Battle colors (node preview title)")]
        [SerializeField]
        private Color normalColor = new(0.92f, 0.75f, 0.27f, 1f);

        [SerializeField]
        [FormerlySerializedAs("hardColor")]
        private Color eliteColor = new(0.93f, 0.41f, 0.36f, 1f);

        [Header("Node preview")]
        [SerializeField]
        [Tooltip("Mini grid color of a cell without terrain.")]
        private Color previewEmptyColor = new(0.35f, 0.22f, 0.15f, 1f);

        [SerializeField]
        [Tooltip("Mini grid color of the cell the player starts on.")]
        private Color previewPlayerColor = new(1f, 0.8f, 0f, 1f);

        [SerializeField]
        private Color terrainObstacleColor = new(0.62f, 0.62f, 0.66f, 1f);

        [SerializeField]
        private Color terrainHazardColor = new(0.9f, 0.35f, 0.24f, 1f);

        [SerializeField]
        private Color terrainBonusColor = new(0.36f, 0.82f, 0.43f, 1f);

        [Header("HUD on the map")]
        [SerializeField]
        [Tooltip("Open question (interface 4.2): the map shows the consumables the player carries, read only " +
                 "(they can only be used in battle).")]
        private bool showConsumablesOnMap = true;

        [Header("Main menu")]
        [SerializeField]
        [Tooltip("Drawn over the portrait of a class that is not unlocked yet.")]
        private Sprite lockIcon;

        public int MapWidth => mapWidth;
        public int FloorSpacing => floorSpacing;
        public int NodeSize => nodeSize;
        public int MapPadding => mapPadding;
        public float ScrollFocus => scrollFocus;
        public int LineWidth => lineWidth;
        public Color PathColor => pathColor;
        public Color AvailableColor => availableColor;
        public Color DimColor => dimColor;
        public Color PreviewEmptyColor => previewEmptyColor;
        public Color PreviewPlayerColor => previewPlayerColor;
        public bool ShowConsumablesOnMap => showConsumablesOnMap;
        public Sprite LockIcon => lockIcon;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static ScreensSettings Current => GameSettings.Get<ScreensSettings>();

        /// <summary>Icon of a node on the map (a battle is normal or elite).</summary>
        public Sprite GetNodeIcon(MapNodeState node)
        {
            switch (node.Type)
            {
                case EMapNodeType.Heal: return healIcon;
                case EMapNodeType.Talent: return talentIcon;
                case EMapNodeType.Consumable: return consumableIcon;
                case EMapNodeType.Boss: return bossIcon;
                default:
                    return MapRules.IsEliteBattle(node) && battleEliteIcon != null ? battleEliteIcon : battleNormalIcon;
            }
        }

        /// <summary>Title color of a battle node in the preview (normal or elite).</summary>
        public Color GetBattleColor(MapNodeState node) => MapRules.IsEliteBattle(node) ? eliteColor : normalColor;

        public Color GetTerrainColor(ETerrainKind kind) => kind switch
        {
            ETerrainKind.Obstacle => terrainObstacleColor,
            ETerrainKind.Bonus => terrainBonusColor,
            _ => terrainHazardColor,
        };
    }
}
