using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager once the board of a battle node is ready (new battle, or restored from the save),
    /// right after <see cref="GridInitializedEvent"/>. The UI switches from the map to the battle HUD.
    /// </summary>
    public class BattleStartedEvent
    {
        public MapNodeState Node { get; }

        /// <summary>The generated battle (enemies at their start positions, terrain, total XP).</summary>
        public BattleSpec Spec { get; }

        /// <summary>The battle was rebuilt from a save in the middle of the fight.</summary>
        public bool Restored { get; }

        public BattleStartedEvent(MapNodeState node, BattleSpec spec, bool restored)
        {
            Node = node;
            Spec = spec;
            Restored = restored;
        }
    }
}
