using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI.Map
{
    /// <summary>
    /// Draws the connections between map nodes (behind the node buttons). Each connection is one straight stroke
    /// from node to node. Direct lines never share segments (the generator guarantees connections don't cross),
    /// so every drawn path is a real connection — elbows sharing a horizontal "bus" made unrelated nodes look
    /// connected. A single stroke per connection keeps semi-transparent colors even (no overlapping pieces).
    /// </summary>
    public sealed class MapLinesElement : VisualElement
    {
        private readonly List<Connection> _connections = new();

        public MapLinesElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>Number of connections drawn.</summary>
        public int ConnectionCount => _connections.Count;

        /// <summary>Removes every connection.</summary>
        public void Clear()
        {
            _connections.Clear();
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Adds a connection from the top of the lower node to the bottom of the upper one. Connections with a
        /// higher <paramref name="priority"/> are drawn later (on top).
        /// </summary>
        public void Add(Vector2 from, Vector2 to, Color color, float width, int priority)
        {
            _connections.Add(new Connection(from, to, color, width, priority));
            _connections.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineCap = LineCap.Butt;

            foreach (var connection in _connections)
            {
                painter.lineWidth = Mathf.Max(1f, Mathf.Round(connection.Width));
                painter.strokeColor = connection.Color;
                painter.BeginPath();
                painter.MoveTo(connection.From);
                painter.LineTo(connection.To);
                painter.Stroke();
            }
        }

        private readonly struct Connection
        {
            public Connection(Vector2 from, Vector2 to, Color color, float width, int priority)
            {
                From = from;
                To = to;
                Color = color;
                Width = width;
                Priority = priority;
            }

            public Vector2 From { get; }
            public Vector2 To { get; }
            public Color Color { get; }
            public float Width { get; }
            public int Priority { get; }
        }
    }
}
