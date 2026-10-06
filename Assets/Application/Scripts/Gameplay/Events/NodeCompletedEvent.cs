using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when a node is resolved and visited (battle won, heal applied, talent node
    /// completed, consumable taken or declined), before <see cref="MapOpenedEvent"/>.
    /// </summary>
    public class NodeCompletedEvent
    {
        public MapNodeState Node { get; }

        /// <summary>Persistent HP change caused by the node (heal node: positive, talent node cost: negative, battle: HP after minus HP before).</summary>
        public int HpDelta { get; }

        public NodeCompletedEvent(MapNodeState node, int hpDelta)
        {
            Node = node;
            HpDelta = hpDelta;
        }
    }
}
