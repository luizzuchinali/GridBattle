using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <c>RunManager.SelectNode</c>: the player picked a node on the map to preview (type and, for
    /// battles, XP, enemy roles and terrain). Nothing is committed until <c>EnterNode</c>.
    /// </summary>
    public class NodeSelectedEvent
    {
        public MapNodeState Node { get; }

        public NodeSelectedEvent(MapNodeState node)
        {
            Node = node;
        }
    }
}
