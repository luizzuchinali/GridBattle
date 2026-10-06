using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Combat
{
    public enum EDamageKind
    {
        BasicAttack,
        Skill,
        Consumable,
        Periodic,
        Thorns,
        Terrain,

        /// <summary>Exact damage: no critical, no defense, no modifiers.</summary>
        Pure,

        /// <summary>Damage from being pushed or pulled into an obstacle, the grid edge or another character (no critical, no thorns, no life steal).</summary>
        Collision
    }

    /// <summary>Outcome of one hit, shared with state effects, events, audio and metrics.</summary>
    public struct HitResult
    {
        /// <summary>Who dealt the damage (null for damage over time, terrain...).</summary>
        public Character Attacker;

        public Character Target;
        public EDamageKind Kind;

        /// <summary>Damage after critical, defense and modifiers (before shields).</summary>
        public int Damage;

        /// <summary>Part of <see cref="Damage"/> absorbed by shields.</summary>
        public int Absorbed;

        /// <summary>Part of <see cref="Damage"/> removed from HP.</summary>
        public int HpDamage;

        public bool IsCrit;
        public bool Killed;

        /// <summary>Attacks, skills and consumables: what triggers thorns and life steal.</summary>
        public bool IsDirect =>
            Kind == EDamageKind.BasicAttack || Kind == EDamageKind.Skill || Kind == EDamageKind.Consumable;
    }
}
