using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <c>RunManager.EnterNode</c> when the player commits to a node, before it is resolved (the battle
    /// board is built, the heal applied, the talent cost paid or the consumable drawn right after).
    /// </summary>
    public class NodeEnteredEvent
    {
        public MapNodeState Node { get; }

        public NodeEnteredEvent(MapNodeState node)
        {
            Node = node;
        }
    }
}
