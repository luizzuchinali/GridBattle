namespace GridBattle.Gameplay.Stats
{
    /// <summary>
    /// Character attributes (GDD 3.1). The value in play is the base value from
    /// the character config combined with the modifiers of its active states.
    /// Fractions are stored as 0..1 (0.1 = 10%).
    /// </summary>
    public enum EAttribute
    {
        MaxHp,
        WalkRange,
        AttackRange,
        BasicDamage,

        /// <summary>Chance (0..1) of a hit being critical.</summary>
        CritChance,

        /// <summary>Damage multiplier of a critical hit.</summary>
        CritMultiplier,

        /// <summary>Fraction (0..1) of the target's defense ignored.</summary>
        DefensePenetration,

        /// <summary>Reduces damage taken (flat or percentage, see CombatSettings).</summary>
        Defense,

        /// <summary>Extra damage (fraction) of offensive skills.</summary>
        SkillDamageBonus,

        /// <summary>Extra target selection range of skills.</summary>
        SkillRange,

        /// <summary>Player actions removed from skill cooldowns.</summary>
        CooldownReduction,

        /// <summary>Multiplier offset (fraction) of all damage dealt. -0.5 = deals 50% less.</summary>
        DamageDealt,

        /// <summary>Multiplier offset (fraction) of all damage taken. 0.2 = takes 20% more.</summary>
        DamageTaken,
    }

    public static class AttributeInfo
    {
        public static readonly int Count = System.Enum.GetValues(typeof(EAttribute)).Length;

        /// <summary>Attributes whose value is a whole number in play.</summary>
        public static bool IsInteger(EAttribute attribute) => attribute switch
        {
            EAttribute.MaxHp => true,
            EAttribute.WalkRange => true,
            EAttribute.AttackRange => true,
            EAttribute.BasicDamage => true,
            EAttribute.Defense => true,
            EAttribute.SkillRange => true,
            EAttribute.CooldownReduction => true,
            _ => false,
        };

        /// <summary>Attributes that cannot go below zero.</summary>
        public static bool IsNonNegative(EAttribute attribute) =>
            attribute != EAttribute.DamageDealt && attribute != EAttribute.DamageTaken;
    }
}
