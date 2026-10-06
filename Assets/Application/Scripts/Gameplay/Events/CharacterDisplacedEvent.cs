using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="Combat.DisplacementResolver"/> once for every character a push or pull was applied to
    /// (also when it could not travel at all because something stood right behind it), after the move, the
    /// collision damage and the terrain reaction were resolved. Immovable characters raise nothing. Hook for
    /// presentation, metrics and the balance simulation.
    /// </summary>
    public class CharacterDisplacedEvent
    {
        public CharacterDisplacedEvent(Character target, [CanBeNull] Character caster, Vector2Int from, Vector2Int to,
            int cellsMoved, EDisplacementBlock block, bool collided, [CanBeNull] Character blockedBy,
            int collisionDamage, int hitCharacterDamage, [CanBeNull] TerrainDefinition forcedTerrain)
        {
            Target = target;
            Caster = caster;
            From = from;
            To = to;
            CellsMoved = cellsMoved;
            Block = block;
            Collided = collided;
            BlockedBy = blockedBy;
            CollisionDamage = collisionDamage;
            HitCharacterDamage = hitCharacterDamage;
            ForcedTerrain = forcedTerrain;
        }

        /// <summary>The character that was pushed or pulled.</summary>
        public Character Target { get; }

        /// <summary>Who caused the displacement (null if unknown).</summary>
        [CanBeNull]
        public Character Caster { get; }

        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public int CellsMoved { get; }

        /// <summary>What stopped the character before the whole distance (none when it travelled all of it).</summary>
        public EDisplacementBlock Block { get; }

        /// <summary>The stop was a collision (damage dealt; a stop at the grid edge only when the settings say so).</summary>
        public bool Collided { get; }

        /// <summary>The character that was hit (null unless <see cref="Block"/> is <see cref="EDisplacementBlock.Character"/>).</summary>
        [CanBeNull]
        public Character BlockedBy { get; }

        /// <summary>Collision damage dealt to <see cref="Target"/> (before shields; 0 without a collision).</summary>
        public int CollisionDamage { get; }

        /// <summary>Collision damage dealt to <see cref="BlockedBy"/> (before shields; 0 for none, or when it was the caster).</summary>
        public int HitCharacterDamage { get; }

        /// <summary>The terrain that applied its effect at once on the cell where the character stopped, or null.</summary>
        [CanBeNull]
        public TerrainDefinition ForcedTerrain { get; }
    }
}
