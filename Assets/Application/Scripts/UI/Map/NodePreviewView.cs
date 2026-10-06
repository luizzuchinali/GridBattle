using GridBattle.Data;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using GridBattle.Managers;
using GridBattle.UI.Hud;
using GridBattle.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI;

namespace GridBattle.UI.Map
{
    /// <summary>
    /// Preview panel of the node picked on the map (mapa_e_nos): the type, the difficulty and total XP of a
    /// battle, the enemies' roles with a count (never the exact enemies) and the terrain (a mini grid colored by
    /// terrain kind and a legend), plus the confirm button. The only place that knows these elements in the UXML.
    /// </summary>
    public sealed class NodePreviewView
    {
        private readonly VisualElement _hint;
        private readonly VisualElement _content;
        private readonly Image _icon;
        private readonly Label _title;
        private readonly Label _xp;
        private readonly Label _text;
        private readonly VisualElement _battle;
        private readonly Label _rolesCaption;
        private readonly VisualElement _roles;
        private readonly Label _terrainCaption;
        private readonly VisualElement _grid;
        private readonly VisualElement _terrainList;
        private readonly Label _hintLabel;

        private MapNodeState _node;

        public NodePreviewView(VisualElement root)
        {
            _hint = root.Q<VisualElement>("preview-hint");
            _hintLabel = root.Q<Label>("preview-hint-label");
            _content = root.Q<VisualElement>("preview-content");
            _icon = root.Q<Image>("preview-icon");
            _title = root.Q<Label>("preview-title");
            _xp = root.Q<Label>("preview-xp");
            _text = root.Q<Label>("preview-text");
            _battle = root.Q<VisualElement>("preview-battle");
            _rolesCaption = root.Q<Label>("preview-roles-caption");
            _roles = root.Q<VisualElement>("preview-roles");
            _terrainCaption = root.Q<Label>("preview-terrain-caption");
            _grid = root.Q<VisualElement>("preview-grid");
            _terrainList = root.Q<VisualElement>("preview-terrain-list");
            ConfirmButton = root.Q<Button>("confirm-button");
        }

        /// <summary>The confirm button (enters the node).</summary>
        public Button ConfirmButton { get; }

        /// <summary>The node being previewed, or null.</summary>
        public MapNodeState Node => _node;

        /// <summary>Title text of the preview ("Battle - Hard").</summary>
        public string TitleText => _title != null ? _title.text : string.Empty;

        /// <summary>Total XP text of the preview ("XP 45"), empty for nodes without a battle.</summary>
        public string XpText => _xp != null ? _xp.text : string.Empty;

        /// <summary>Number of role chips shown.</summary>
        public int RoleChipCount => _roles != null ? _roles.childCount : 0;

        /// <summary>Number of cells of the mini grid.</summary>
        public int GridCellCount => _grid != null ? _grid.Query(className: "mini-grid__cell").ToList().Count : 0;

        /// <summary>Number of terrain types in the legend.</summary>
        public int TerrainLegendCount => _terrainList != null ? _terrainList.childCount : 0;

        /// <summary>Shows the node, or the hint when null.</summary>
        public void Show(MapNodeState node)
        {
            _node = node;
            if (_content == null) return;

            var has = node != null;
            _hint.SetDisplayed(!has);
            _content.SetDisplayed(has);
            ConfirmButton.SetDisplayed(has);
            ConfirmButton.SetEnabled(has);
            Render();
        }

        /// <summary>Clears the preview (shows the hint).</summary>
        public void Clear() => Show(null);

        /// <summary>Applies the current language to the texts (and the texts that depend on the run).</summary>
        public void RefreshTexts()
        {
            Render();
        }

        private void Render()
        {
            if (_hintLabel == null) return;

            _hintLabel.text = HudText.Get("map.preview.hint");
            ConfirmButton.text = HudText.Get("map.confirm");
            _rolesCaption.text = HudText.Get("map.preview.enemies");
            _terrainCaption.text = HudText.Get("map.preview.terrain");

            var node = _node;
            if (node == null) return;

            var settings = ScreensSettings.Current;
            _icon.sprite = settings.GetNodeIcon(node);
            _title.text = GetTitle(node);
            _title.style.color = node.Type == EMapNodeType.Battle
                ? settings.GetDifficultyColor(node.Difficulty)
                : new Color(1f, 0.8f, 0f, 1f);

            var isBattle = node.Battle != null && (node.Type == EMapNodeType.Battle || node.Type == EMapNodeType.Boss);
            _battle.SetDisplayed(isBattle);
            _text.SetDisplayed(!isBattle);
            _xp.SetDisplayed(isBattle);
            _xp.text = isBattle ? HudText.Format("map.preview.xp", node.Battle.TotalXp) : string.Empty;
            _text.text = GetDescription(node);

            if (isBattle)
                BuildBattle(node.Battle, settings);
        }

        /// <summary>Localized title of a node: its type and, for battles, the difficulty.</summary>
        public static string GetTitle(MapNodeState node)
        {
            switch (node.Type)
            {
                case EMapNodeType.Boss: return HudText.Get("map.node.boss");
                case EMapNodeType.Heal: return HudText.Get("map.node.heal");
                case EMapNodeType.Talent: return HudText.Get("map.node.talent");
                case EMapNodeType.Consumable: return HudText.Get("map.node.consumable");
                default:
                    return HudText.Format("map.node.battle_difficulty", HudText.Get("map.node.battle"),
                        HudText.Get("map.difficulty." + node.Difficulty.ToString().ToLowerInvariant()));
            }
        }

        private static string GetDescription(MapNodeState node)
        {
            var runs = RunManager.Instance;
            var maxHp = runs != null ? runs.PlayerMaxHp : 0;
            var hp = runs != null ? runs.PlayerHp : 0;
            var settings = RunSettings.Current;

            switch (node.Type)
            {
                case EMapNodeType.Heal:
                    return HudText.Format("map.node.heal.desc", settings.GetHealAmount(maxHp));
                case EMapNodeType.Talent:
                    return HudText.Format("map.node.talent.desc", settings.GetTalentNodeCost(hp, maxHp));
                case EMapNodeType.Consumable:
                    return HudText.Get("map.node.consumable.desc");
                default:
                    return string.Empty;
            }
        }

        private void BuildBattle(BattleSpec spec, ScreensSettings settings)
        {
            // Roles: icon and count only, never the exact enemies.
            _roles.Clear();
            foreach (var (role, count) in MapRules.GetRoleCounts(spec))
            {
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("role-chip");
                if (role.Icon != null)
                {
                    var icon = new Image { sprite = role.Icon, pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("role-chip__icon");
                    chip.Add(icon);
                }
                else
                {
                    var name = new Label(role.GetDisplayName()) { pickingMode = PickingMode.Ignore };
                    name.AddToClassList("role-chip__count");
                    chip.Add(name);
                }

                var amount = new Label("x" + count) { pickingMode = PickingMode.Ignore };
                amount.AddToClassList("role-chip__count");
                chip.Add(amount);
                _roles.Add(chip);
            }

            // Terrain: mini grid (the player's start cell is marked) and a legend with counts.
            var width = Mathf.Max(1, spec.Width);
            var height = Mathf.Max(1, spec.Height);
            var cells = new VisualElement[width * height];
            _grid.Clear();
            for (var y = 0; y < height; y++)
            {
                // One element per row (not a wrapping row): the grid keeps its shape at any scale.
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("mini-grid__row");
                _grid.Add(row);

                for (var x = 0; x < width; x++)
                {
                    var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                    cell.AddToClassList("mini-grid__cell");
                    cell.style.backgroundColor = settings.PreviewEmptyColor;
                    cells[y * width + x] = cell;
                    row.Add(cell);
                }
            }

            var database = GameDatabase.Instance;
            foreach (var spot in spec.Terrain)
            {
                if (spot.X < 0 || spot.X >= width || spot.Y < 0 || spot.Y >= height) continue;

                var terrain = database != null
                    ? database.Get<TerrainDefinition>(spot.TerrainId)
                    : null;
                if (terrain != null)
                    cells[spot.Y * width + spot.X].style.backgroundColor = settings.GetTerrainColor(terrain.Kind);
            }

            if (spec.PlayerX >= 0 && spec.PlayerX < width && spec.PlayerY >= 0 && spec.PlayerY < height)
                cells[spec.PlayerY * width + spec.PlayerX].style.backgroundColor = settings.PreviewPlayerColor;

            _terrainList.Clear();
            var counts = MapRules.GetTerrainCounts(spec);
            if (counts.Count == 0)
            {
                var none = new Label(HudText.Get("map.preview.no_terrain")) { pickingMode = PickingMode.Ignore };
                none.AddToClassList("preview-terrain-row__name");
                _terrainList.Add(none);
            }

            foreach (var (terrain, count) in counts)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("preview-terrain-row");

                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("preview-terrain-row__swatch");
                swatch.style.backgroundColor = settings.GetTerrainColor(terrain.Kind);
                row.Add(swatch);

                var name = new Label(HudText.Format("map.preview.terrain_count", terrain.GetDisplayName(), count))
                {
                    pickingMode = PickingMode.Ignore
                };
                name.AddToClassList("preview-terrain-row__name");
                row.Add(name);
                _terrainList.Add(row);
            }
        }
    }
}
