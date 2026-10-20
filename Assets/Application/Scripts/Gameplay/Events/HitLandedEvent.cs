using GridBattle.Gameplay.Combat;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when the hit reaction of a damaged character starts on screen: the moment the attack lunge, the skill
    /// visual or the end of a push slide reaches it (at once for damage over time, terrain and thorns). The hit itself
    /// was resolved earlier (<see cref="DamageDealtEvent"/>); this is the presentation's impact point, used by the
    /// game feel (shake, hit stop, particles) and by the impact sounds. Only in Play Mode and never in the
    /// simulation.
    /// </summary>
    public class HitLandedEvent
    {
        public HitLandedEvent(HitResult hit, Vector3 position, Vector3 direction)
        {
            Hit = hit;
            Position = position;
            Direction = direction;
        }

        /// <summary>The resolved hit (<see cref="HitResult.Killed"/> when the reaction is the death).</summary>
        public HitResult Hit { get; }

        /// <summary>World position of the character when the hit lands.</summary>
        public Vector3 Position { get; }

        /// <summary>Direction the hit came from, pointing away from the attacker (world space, normalized).</summary>
        public Vector3 Direction { get; }
    }
}
