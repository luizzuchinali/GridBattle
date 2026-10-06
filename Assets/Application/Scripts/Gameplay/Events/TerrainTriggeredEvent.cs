using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a terrain cell applied its effect (damage and/or states) to the
    /// character standing on it.
    /// </summary>
    public class TerrainTriggeredEvent
    {
        public Character Character { get; }
        public Vector2Int Position { get; }
        public TerrainDefinition Terrain { get; }
        public ETerrainTrigger Trigger { get; }

        /// <summary>Damage dealt by the cell (before shields; 0 if it only applies states).</summary>
        public int Damage { get; }

        /// <summary>The cell applied its effect at once because the character was pushed or pulled onto it.</summary>
        public bool IsForced { get; }

        public TerrainTriggeredEvent(Character character, Vector2Int position, TerrainDefinition terrain,
            ETerrainTrigger trigger, int damage, bool isForced = false)
        {
            IsForced = isForced;
            Character = character;
            Position = position;
            Terrain = terrain;
            Trigger = trigger;
            Damage = damage;
        }
    }
}
