using GridBattle.Data;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Combat
{
    public enum EDefenseMode
    {
        /// <summary>Damage − defense.</summary>
        Flat,

        /// <summary>Defense is a percentage of damage reduced (capped by Max Percent Reduction).</summary>
        Percent
    }

    public enum ERounding
    {
        Round,
        Floor,
        Ceil
    }

    /// <summary>
    /// Combat formulas (GDD 2.9 / 3.1). Several of these are open questions in the
    /// design documents; the defaults keep the current game unchanged.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "GridBattle/Settings/Combat Settings", order = 0)]
    public sealed class CombatSettings : ScriptableObject, IGameSettings
    {
        [Header("Defense")]
        [SerializeField]
        [Tooltip("How defense reduces damage (open question: fixed value or percentage).")]
        private EDefenseMode defenseMode = EDefenseMode.Flat;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Percent mode only: maximum fraction of damage that defense can remove.")]
        private float maxPercentReduction = 0.8f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Minimum damage of a hit whose base damage is above zero.")]
        private int minimumDamage = 1;

        [SerializeField]
        private ERounding rounding = ERounding.Round;

        [Header("Critical")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Upper limit of the critical chance.")]
        private float critChanceCap = 1f;

        [Header("Special damage")]
        [SerializeField]
        [Tooltip("Damage over time ignores defense.")]
        private bool periodicIgnoresDefense = true;

        [SerializeField]
        [Tooltip("Thorns damage ignores defense.")]
        private bool thornsIgnoreDefense = true;

        [SerializeField]
        [Tooltip("Terrain (hazard cell) damage ignores defense.")]
        private bool terrainIgnoresDefense = true;

        [Header("Displacement (push and pull)")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Damage taken by a character that is pushed or pulled and stops against an obstacle, another character or (see below) the edge of the grid.")]
        private int collisionDamage = 6;

        [SerializeField]
        [Min(0)]
        [Tooltip("Extra collision damage for every cell the character did not travel because of the collision (push 2 stopped at once = 2 unspent cells).")]
        private int collisionDamagePerUnspentCell = 2;

        [SerializeField]
        [Min(0)]
        [Tooltip("Damage taken by the character that was hit by the displaced one (flat, whoever it is, but never the caster of the displacement).")]
        private int collisionHitCharacterDamage = 6;

        [SerializeField]
        [Tooltip("Collision damage ignores defense (like terrain damage).")]
        private bool collisionIgnoresDefense = true;

        [SerializeField]
        [Tooltip("Being pushed into the edge of the grid counts as a collision (damage as above). When off the character just stops.")]
        private bool edgeCountsAsCollision = true;

        [Header("Distances")]
        [SerializeField]
        [Tooltip("Metric of the walk range (the game currently uses Euclidean).")]
        private EDistanceMetric movementMetric = EDistanceMetric.Euclidean;

        [SerializeField]
        [Tooltip("Metric of the basic attack range (the game currently uses Euclidean).")]
        private EDistanceMetric attackMetric = EDistanceMetric.Euclidean;

        [SerializeField]
        [Tooltip("Metric of the skill target range (GDD: Manhattan).")]
        private EDistanceMetric skillRangeMetric = EDistanceMetric.Manhattan;

        [SerializeField]
        [Tooltip("Metric that decides who is adjacent to a character for the conditional damage effects of talents " +
                 "(bonus per adjacent enemy, no adjacent enemy, isolated target). Chebyshev = the 8 neighbors, " +
                 "Euclidean/Manhattan = the 4 orthogonal ones.")]
        private EDistanceMetric adjacencyMetric = EDistanceMetric.Chebyshev;

        public EDefenseMode DefenseMode => defenseMode;
        public float MaxPercentReduction => maxPercentReduction;
        public int MinimumDamage => minimumDamage;
        public ERounding Rounding => rounding;
        public float CritChanceCap => critChanceCap;
        public bool PeriodicIgnoresDefense => periodicIgnoresDefense;
        public bool ThornsIgnoreDefense => thornsIgnoreDefense;
        public bool TerrainIgnoresDefense => terrainIgnoresDefense;
        public int CollisionDamage => collisionDamage;
        public int CollisionDamagePerUnspentCell => collisionDamagePerUnspentCell;
        public int CollisionHitCharacterDamage => collisionHitCharacterDamage;
        public bool CollisionIgnoresDefense => collisionIgnoresDefense;
        public bool EdgeCountsAsCollision => edgeCountsAsCollision;
        public EDistanceMetric MovementMetric => movementMetric;
        public EDistanceMetric AttackMetric => attackMetric;
        public EDistanceMetric SkillRangeMetric => skillRangeMetric;
        public EDistanceMetric AdjacencyMetric => adjacencyMetric;

        /// <summary>Base collision damage of a displaced character that left <paramref name="unspentCells"/> cells untravelled.</summary>
        public int GetCollisionDamage(int unspentCells) =>
            collisionDamage + collisionDamagePerUnspentCell * Mathf.Max(0, unspentCells);

        public int RoundValue(float value) => rounding switch
        {
            ERounding.Floor => Mathf.FloorToInt(value),
            ERounding.Ceil => Mathf.CeilToInt(value),
            _ => Mathf.RoundToInt(value),
        };
    }
}
