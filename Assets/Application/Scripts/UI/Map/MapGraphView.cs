using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Run;
using GridBattle.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI.Map
{
    /// <summary>
    /// The node map inside the scroll view (mapa_e_nos, interface 4.1): floors from the bottom (floor 0) to the
    /// top (the boss), lanes as columns, one button per node with an icon per type, the connections drawn behind
    /// them, the path walked marked and the nodes that can be chosen highlighted. The only place that knows the
    /// map elements in the UXML.
    /// </summary>
    public sealed class MapGraphView
    {
        private const string NodeClass = "map-node";

        private readonly ScrollView _scroll;
        private readonly VisualElement _canvas;
        private readonly Dictionary<int, Button> _buttons = new();
        private MapLinesElement _lines;
        private float _focusY;
        private int _canvasHeight;
        private bool _focusPending;

        public MapGraphView(VisualElement root)
        {
            _scroll = root.Q<ScrollView>("map-scroll");
            _canvas = root.Q<VisualElement>("map-canvas");
            if (_scroll != null)
                _scroll.RegisterCallback<GeometryChangedEvent>(_ => ApplyFocus());
        }

        /// <summary>The player tapped a node that can be chosen now.</summary>
        public event Action<int> NodeClicked;

        /// <summary>Id of the node shown as selected (the one being previewed), or -1.</summary>
        public int SelectedId { get; private set; } = -1;

        /// <summary>Number of node buttons drawn.</summary>
        public int NodeCount => _buttons.Count;

        /// <summary>Number of connections drawn.</summary>
        public int ConnectionCount => _lines != null ? _lines.ConnectionCount : 0;

        /// <summary>Vertical scroll position of the map.</summary>
        public float ScrollY => _scroll != null ? _scroll.scrollOffset.y : 0f;

        /// <summary>The scroll view of the map (null until the UI loads).</summary>
        public ScrollView Scroll => _scroll;

        /// <summary>The button of a node, or null.</summary>
        public Button GetButton(int nodeId) => _buttons.TryGetValue(nodeId, out var button) ? button : null;

        /// <summary>
        /// Builds the whole map for <paramref name="map"/>. <paramref name="availableNodes"/> are the nodes that can
        /// be chosen now (empty outside the map phase). <paramref name="selectedId"/> is the node being
        /// previewed (-1 for none). When <paramref name="focus"/> is true the scroll goes to the floor with the
        /// nodes to choose (or the current floor) once the layout is known.
        /// </summary>
        public void Rebuild(MapState map, IEnumerable<MapNodeState> availableNodes, int selectedId, bool focus)
        {
            if (_canvas == null) return;

            var settings = ScreensSettings.Current;
            _canvas.Clear();
            _buttons.Clear();
            SelectedId = selectedId;

            var floors = Mathf.Max(1, map.FloorCount);
            var lanes = Mathf.Max(1, map.Lanes);
            var nodeSize = settings.NodeSize;
            var laneWidth = settings.MapWidth / lanes;
            var left = (settings.MapWidth - laneWidth * lanes) / 2;
            _canvasHeight = settings.MapPadding * 2 + nodeSize + (floors - 1) * settings.FloorSpacing;

            _canvas.style.width = settings.MapWidth;
            _canvas.style.height = _canvasHeight;

            _lines = new MapLinesElement();
            _lines.AddToClassList("map-lines");
            _canvas.Add(_lines);

            Vector2 Center(MapNodeState node) => new(
                left + node.Column * laneWidth + laneWidth / 2,
                _canvasHeight - settings.MapPadding - nodeSize / 2 - node.Floor * settings.FloorSpacing);

            var available = new HashSet<int>();
            foreach (var node in availableNodes)
                available.Add(node.Id);

            var visited = new HashSet<int>(map.VisitedNodeIds);
            var half = nodeSize / 2;

            // Connections: dim ones first, then the choices from the current node, then the path walked.
            foreach (var node in map.Nodes)
            {
                foreach (var nextId in node.Next)
                {
                    var next = MapRules.GetNode(map, nextId);
                    if (next == null) continue;

                    var priority = 0;
                    var color = settings.DimColor;
                    if (visited.Contains(node.Id) && visited.Contains(nextId))
                    {
                        priority = 2;
                        color = settings.PathColor;
                    }
                    else if (node.Id == map.CurrentNodeId && available.Contains(nextId))
                    {
                        priority = 1;
                        color = settings.AvailableColor;
                    }

                    var from = Center(node);
                    var to = Center(next);
                    _lines.Add(new Vector2(from.x, from.y - half), new Vector2(to.x, to.y + half), color,
                        settings.LineWidth, priority);
                }
            }

            foreach (var node in map.Nodes)
            {
                var button = CreateNodeButton(node, settings, Center(node), half,
                    visited.Contains(node.Id), node.Id == map.CurrentNodeId, available.Contains(node.Id));
                _canvas.Add(button);
                _buttons[node.Id] = button;
            }

            SetSelected(selectedId);

            if (focus)
            {
                var focusFloor = FocusFloor(map, available);
                _focusY = _canvasHeight - settings.MapPadding - nodeSize / 2 - focusFloor * settings.FloorSpacing;
                _focusPending = true;
                ApplyFocus();
            }
        }

        /// <summary>Marks the node being previewed (-1 clears it).</summary>
        public void SetSelected(int nodeId)
        {
            SelectedId = nodeId;
            foreach (var pair in _buttons)
                pair.Value.EnableInClassList("map-node--selected", pair.Key == nodeId);
        }

        private Button CreateNodeButton(MapNodeState node, ScreensSettings settings, Vector2 center, int half,
            bool visited, bool current, bool available)
        {
            var button = new Button { name = "node-" + node.Id, text = string.Empty };
            button.AddToClassList(NodeClass);
            button.AddToClassList("map-node--" + node.Type.ToString().ToLowerInvariant());
            if (node.Type == EMapNodeType.Battle)
                button.AddToClassList("map-node--" + node.Difficulty.ToString().ToLowerInvariant());
            button.EnableInClassList("map-node--visited", visited);
            button.EnableInClassList("map-node--current", current);
            button.EnableInClassList("map-node--available", available);

            var style = button.style;
            style.left = center.x - half;
            style.top = center.y - half;
            style.width = settings.NodeSize;
            style.height = settings.NodeSize;

            var icon = new Image { sprite = settings.GetNodeIcon(node), pickingMode = PickingMode.Ignore };
            icon.AddToClassList("map-node__icon");
            button.Add(icon);

            if (settings.ShowNodeXp && node.Battle != null)
            {
                var xp = new Label(node.Battle.TotalXp.ToString()) { pickingMode = PickingMode.Ignore };
                xp.AddToClassList("map-node__xp");
                xp.style.color = settings.GetDifficultyColor(node.Difficulty);
                button.Add(xp);
            }

            if (available)
            {
                var id = node.Id;
                button.OnClick(() => NodeClicked?.Invoke(id));
            }
            else
            {
                button.pickingMode = PickingMode.Ignore;
            }

            return button;
        }

        /// <summary>The floor to bring into view: the one with the nodes to choose, else the current one.</summary>
        private static int FocusFloor(MapState map, HashSet<int> available)
        {
            foreach (var id in available)
            {
                var node = MapRules.GetNode(map, id);
                if (node != null)
                    return node.Floor;
            }

            var current = MapRules.GetNode(map, map.CurrentNodeId);
            return current != null ? current.Floor : 0;
        }

        /// <summary>Scrolls so the focus floor sits at the configured height of the viewport (once the layout is known).</summary>
        private void ApplyFocus()
        {
            if (!_focusPending || _scroll == null) return;

            var viewport = _scroll.contentViewport.layout.height;
            if (float.IsNaN(viewport) || viewport <= 0f) return;

            _focusPending = false;
            var target = _focusY - viewport * ScreensSettings.Current.ScrollFocus;
            var max = Mathf.Max(0f, _canvasHeight - viewport);
            _scroll.scrollOffset = new Vector2(0f, Mathf.Round(Mathf.Clamp(target, 0f, max)));
        }
    }
}
