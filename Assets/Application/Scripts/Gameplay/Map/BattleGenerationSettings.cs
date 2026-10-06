using System;
using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Roles;
using GridBattle.Gameplay.Run;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Map
{
    /// <summary>An enemy the battle generator can draw, with the depths it appears at and its relative weight.</summary>
    [Serializable]
    public sealed class EnemyPoolEntry
    {
        [SerializeField]
        private EnemyConfig enemy;

        [SerializeField]
        [Min(1)]
        [Tooltip("First depth this enemy can appear at.")]
        private int minDepth = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Last depth this enemy can appear at (0 = no limit).")]
        private int maxDepth;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance among the enemies available at the depth (0 = never drawn).")]
        private float weight = 1f;

        public EnemyPoolEntry()
        {
        }

        public EnemyPoolEntry(EnemyConfig enemy, int minDepth = 1, int maxDepth = 0, float weight = 1f)
        {
            this.enemy = enemy;
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            this.weight = weight;
        }

        public EnemyConfig Enemy => enemy;
        public int MinDepth => minDepth;
        public int MaxDepth => maxDepth;
        public float Weight => weight;

        /// <summary>Whether the enemy can be drawn at <paramref name="depth"/>.</summary>
        public bool IsAvailableAt(int depth) =>
            enemy != null && weight > 0f && depth >= minDepth && (maxDepth <= 0 || depth <= maxDepth);
    }

    /// <summary>Where the XP of a generated battle comes from.</summary>
    public enum EBattleXpSource
    {
        /// <summary>Each enemy grants the XP Reward of its config, grown by depth.</summary>
        EnemyReward,

        /// <summary>Each enemy grants XP in proportion to its own threat.</summary>
        EnemyThreat,

        /// <summary>
        /// The battle grants XP in proportion to the threat budget of its depth and difficulty, split among the
        /// enemies by their threat: a harder difficulty always grants more, whatever enemies were drawn.
        /// </summary>
        ThreatBudget
    }

    /// <summary>How a battle difficulty changes the generated battle.</summary>
    [Serializable]
    public sealed class BattleDifficultyProfile
    {
        [SerializeField]
        private EBattleDifficulty difficulty;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Multiplier of the threat budget: more (or fewer) enemies.")]
        private float budgetMultiplier = 1f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Extra multiplier of the enemies' HP and damage on top of the depth scaling (1 = none).")]
        private float strengthMultiplier = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Multiplier of the XP the battle grants (1 = none).")]
        private float xpMultiplier = 1f;

        public BattleDifficultyProfile()
        {
        }

        public BattleDifficultyProfile(EBattleDifficulty difficulty, float budgetMultiplier,
            float strengthMultiplier = 1f, float xpMultiplier = 1f)
        {
            this.difficulty = difficulty;
            this.budgetMultiplier = budgetMultiplier;
            this.strengthMultiplier = strengthMultiplier;
            this.xpMultiplier = xpMultiplier;
        }

        public EBattleDifficulty Difficulty => difficulty;
        public float BudgetMultiplier => budgetMultiplier;
        public float StrengthMultiplier => strengthMultiplier;
        public float XpMultiplier => xpMultiplier;
    }

    /// <summary>Upper limit of the enemies of one role in a battle.</summary>
    [Serializable]
    public sealed class RoleLimit
    {
        [SerializeField]
        private EnemyRoleDefinition role;

        [SerializeField]
        [Min(0)]
        private int max = 2;

        public RoleLimit()
        {
        }

        public RoleLimit(EnemyRoleDefinition role, int max)
        {
            this.role = role;
            this.max = max;
        }

        public EnemyRoleDefinition Role => role;
        public int Max => max;
    }

    /// <summary>One point of the budget curve: the threat budget (Normal difficulty) at a depth.</summary>
    [Serializable]
    public sealed class BudgetPoint
    {
        [SerializeField]
        [Min(1f)]
        [Tooltip("Balance depth of the point.")]
        private float depth = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Threat budget of a Normal battle at this depth.")]
        private float budget = 6f;

        public BudgetPoint()
        {
        }

        public BudgetPoint(float depth, float budget)
        {
            this.depth = depth;
            this.budget = budget;
        }

        public float Depth => depth;
        public float Budget => budget;
    }

    /// <summary>Multiplier of the draw weight of one enemy role (see <see cref="CompositionBand"/>).</summary>
    [Serializable]
    public sealed class RoleWeight
    {
        [SerializeField]
        private EnemyRoleDefinition role;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Multiplier of the pool weight of every enemy with this role (1 = unchanged, 0 = never drawn).")]
        private float multiplier = 1f;

        public RoleWeight()
        {
        }

        public RoleWeight(EnemyRoleDefinition role, float multiplier)
        {
            this.role = role;
            this.multiplier = multiplier;
        }

        public EnemyRoleDefinition Role => role;
        public float Multiplier => multiplier;
    }

    /// <summary>
    /// Composition rules that apply from a depth on, until the next band starts (late-game tactical depth): how
    /// many different roles a battle tries to hold and which roles are favored. Without a band the generator draws
    /// from the pool weights alone.
    /// </summary>
    [Serializable]
    public sealed class CompositionBand
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("First depth this band applies to (balance depth; the band with the highest Min Depth not above the battle's depth is used).")]
        private int minDepth = 1;

        [SerializeField]
        [Min(1)]
        [Tooltip("The generator tries to draw at least this many different enemy roles in a battle (a battle that cannot reach it within the budget and limits is kept as is). 1 = no rule.")]
        private int minDistinctRoles = 1;

        [SerializeField]
        [Tooltip("Multipliers of the pool weights by role in this band (roles not listed keep multiplier 1).")]
        private List<RoleWeight> roleWeights = new();

        public CompositionBand()
        {
        }

        public CompositionBand(int minDepth, int minDistinctRoles, List<RoleWeight> roleWeights = null)
        {
            this.minDepth = minDepth;
            this.minDistinctRoles = minDistinctRoles;
            this.roleWeights = roleWeights ?? new List<RoleWeight>();
        }

        public int MinDepth => minDepth;
        public int MinDistinctRoles => Mathf.Max(1, minDistinctRoles);
        public IReadOnlyList<RoleWeight> RoleWeights => roleWeights;

        /// <summary>Weight multiplier of a role in this band (1 when it is not listed).</summary>
        public float GetRoleWeight([CanBeNull] EnemyRoleDefinition role)
        {
            if (role == null) return 1f;

            foreach (var entry in roleWeights)
            {
                if (entry != null && entry.Role == role)
                    return entry.Multiplier;
            }

            return 1f;
        }
    }

    /// <summary>
    /// Configuration of the battle generator (balanceamento_e_geracao.md, "Geração das batalhas"): the enemy
    /// pool by depth, the threat formula, the budget per depth and difficulty, enemy scaling, XP, limits,
    /// composition rules, placement and the final boss. Everything the document leaves open is a field here.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleGenerationSettings", menuName = "GridBattle/Map/Battle Generation Settings",
        order = 1)]
    public sealed class BattleGenerationSettings : ScriptableObject, IGameSettings
    {
        [Header("Grid")]
        [SerializeField]
        [Min(3)]
        private int gridWidth = 6;

        [SerializeField]
        [Min(3)]
        private int gridHeight = 6;

        [SerializeField]
        [Tooltip("Cell where the player starts every battle.")]
        private Vector2Int playerSpawn = new(2, 2);

        [Header("Enemy pool")]
        [SerializeField]
        [Tooltip("Enemies the generator can draw, with the depths they appear at (min/max depth) and their weight. Bosses are not drawn from here.")]
        private List<EnemyPoolEntry> pool = new();

        [Header("Threat formula (balanceamento_e_geracao.md, suggestion)")]
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("threat = damage x HP / divisor x (1 + movementCoefficient x (movement - 1)) x (1 + rangeCoefficient x (range - 1)) x role factor")]
        private float threatDivisor = 50f;

        [SerializeField]
        [Min(0f)]
        private float movementCoefficient = 0.25f;

        [SerializeField]
        [Min(0f)]
        private float rangeCoefficient = 0.5f;

        [SerializeField]
        [Tooltip("The threat of an enemy counts its HP and damage after the depth scaling (the budget then measures real strength). Off = the base stats.")]
        private bool useScaledThreat = true;

        [Header("Budget")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Open question (balanceamento_e_geracao, expected strength by depth): threat budget of depth 1 (Normal).")]
        private float baseBudget = 6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Threat budget added for every depth after the first.")]
        private float budgetPerDepth = 0.52f;

        [SerializeField]
        [Tooltip("Optional budget curve: points of (depth, Normal budget), linear between them and constant outside the first and last. " +
                 "When it has points it replaces Base Budget and Budget Per Depth, so the budget can grow slowly early and fast late.")]
        private List<BudgetPoint> budgetPoints = new();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("A battle is valid when its total threat is within this fraction of the budget (and it may overshoot the budget by it). The generator retries with another draw when it is not.")]
        private float budgetTolerance = 0.3f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of an enemy's threat may go beyond the remaining budget when it is added: 0 = the total never exceeds the budget, 0.5 = the total ends as close to the budget as possible (default), 1 = enemies are added until the tolerance is reached.")]
        private float overshootAllowance = 0.5f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Draws tried per battle before the closest one is kept.")]
        private int maxAttempts = 12;

        [SerializeField]
        [Tooltip("Open question (mapa_e_nos, difficulty): effect of each difficulty. Default: Easy 0.75 / Normal 1 / Hard 1.35 of the budget.")]
        private List<BattleDifficultyProfile> difficulties = new()
        {
            new BattleDifficultyProfile(EBattleDifficulty.Easy, 0.75f),
            new BattleDifficultyProfile(EBattleDifficulty.Normal, 1f),
            new BattleDifficultyProfile(EBattleDifficulty.Hard, 1.35f),
        };

        [Header("Enemy scaling by depth")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Open question (balanceamento_e_geracao, enemy scaling): HP gained per depth after the first (0.021 = +2.1%).")]
        private float hpPerDepth = 0.021f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Damage gained per depth after the first (0.0103 = +1.03%).")]
        private float damagePerDepth = 0.0103f;

        [SerializeField]
        [Tooltip("Off = linear growth (1 + rate x (depth - 1)); on = compound growth ((1 + rate) ^ (depth - 1)).")]
        private bool compoundScaling;

        [Header("XP")]
        [SerializeField]
        [Tooltip("Where the XP of a battle comes from. Threat Budget (default) makes Easy < Normal < Hard on the same depth: " +
                 "the battle grants Budget x XP Per Threat x (1 + XP Per Depth x (depth - 1)) x the difficulty's XP multiplier, split among the enemies by threat. " +
                 "Enemy Reward uses the XP Reward of each enemy config (grown by XP Per Depth); Enemy Threat, the threat of each enemy x XP Per Threat. " +
                 "GridBattle > Talents > Simulate XP Progression shows the levels each path reaches.")]
        private EBattleXpSource xpSource = EBattleXpSource.ThreatBudget;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Threat budget of depth 1 used only to size the XP of Threat Budget (0 = use Base Budget and Budget Per Depth). " +
                 "It lets the enemies' strength change without changing how fast the player levels up.")]
        private float xpBaseBudget;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Budget added per depth in the XP budget (only when XP Base Budget is above 0).")]
        private float xpBudgetPerDepth;

        [SerializeField]
        [Min(0f)]
        [Tooltip("XP grows by this fraction per depth after the first (Threat Budget and Enemy Reward), so that it keeps up with the XP curve of Progression Settings.")]
        private float xpPerDepth = 0.03f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("XP per point of threat (Threat Budget: of the budget; Enemy Threat: of each enemy).")]
        private float xpPerThreat = 5f;

        [Header("Limits")]
        [SerializeField]
        [Min(1)]
        private int minEnemies = 1;

        [SerializeField]
        [Min(1)]
        private int maxEnemies = 8;

        [SerializeField]
        [Range(0.05f, 1f)]
        [Tooltip("Open question (balanceamento_e_geracao, grid capacity): the enemies never take more than this fraction of the grid's cells.")]
        private float maxOccupancy = 0.4f;

        [Header("Composition")]
        [SerializeField]
        [Tooltip("Open question (balanceamento_e_geracao, composition rules): every battle has at least one enemy whose role counts as frontline (never a battle of only ranged or support enemies).")]
        private bool requireFrontline = true;

        [SerializeField]
        [Tooltip("Optional maximum of enemies per role in one battle (e.g. at most 1 controller).")]
        private List<RoleLimit> roleLimits = new();

        [SerializeField]
        [Tooltip("Late-game composition by depth: minimum of different roles per battle and role weight multipliers. " +
                 "The band with the highest Min Depth not above the battle's balance depth applies; none = pool weights only.")]
        private List<CompositionBand> compositionBands = new();

        [Header("Placement")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Enemies start at least this far (Manhattan distance) from the player's spawn.")]
        private int minDistanceFromPlayer = 2;

        [SerializeField]
        [Range(0.1f, 1f)]
        [Tooltip("Enemies that are not frontline (and the boss) start among this fraction of the free cells that are farthest from the player.")]
        private float farCellFraction = 0.5f;

        [SerializeField]
        [Tooltip("Enemies may start on hazard cells.")]
        private bool allowHazardCells;

        [Header("Final boss")]
        [SerializeField]
        [Tooltip("Enemies of the final node: the boss first, then its escort. They are scaled for the final depth. PLACEHOLDER content until the real boss exists.")]
        private List<EnemyConfig> bossEncounter = new();

        public Vector2Int GridSize => new(Mathf.Max(3, gridWidth), Mathf.Max(3, gridHeight));
        public Vector2Int PlayerSpawn => playerSpawn;
        public IReadOnlyList<EnemyPoolEntry> Pool => pool;
        public float ThreatDivisor => Mathf.Max(0.01f, threatDivisor);
        public float MovementCoefficient => movementCoefficient;
        public float RangeCoefficient => rangeCoefficient;
        public bool UseScaledThreat => useScaledThreat;
        public float BaseBudget => baseBudget;
        public float BudgetPerDepth => budgetPerDepth;
        public IReadOnlyList<BudgetPoint> BudgetPoints => budgetPoints;
        public float BudgetTolerance => budgetTolerance;
        public float OvershootAllowance => overshootAllowance;
        public int MaxAttempts => Mathf.Max(1, maxAttempts);
        public IReadOnlyList<BattleDifficultyProfile> Difficulties => difficulties;
        public float HpPerDepth => hpPerDepth;
        public float DamagePerDepth => damagePerDepth;
        public bool CompoundScaling => compoundScaling;
        public EBattleXpSource XpSource => xpSource;
        public float XpPerDepth => xpPerDepth;
        public float XpPerThreat => xpPerThreat;
        public int MinEnemies => Mathf.Max(1, minEnemies);
        public int MaxEnemies => Mathf.Max(MinEnemies, maxEnemies);
        public float MaxOccupancy => maxOccupancy;
        public bool RequireFrontline => requireFrontline;
        public IReadOnlyList<RoleLimit> RoleLimits => roleLimits;
        public IReadOnlyList<CompositionBand> CompositionBands => compositionBands;
        public float XpBaseBudget => xpBaseBudget;
        public float XpBudgetPerDepth => xpBudgetPerDepth;
        public int MinDistanceFromPlayer => minDistanceFromPlayer;
        public float FarCellFraction => farCellFraction;
        public bool AllowHazardCells => allowHazardCells;
        public IReadOnlyList<EnemyConfig> BossEncounter => bossEncounter;

        /// <summary>The profile of a difficulty (a neutral one if the list has none for it).</summary>
        public BattleDifficultyProfile GetProfile(EBattleDifficulty difficulty)
        {
            foreach (var profile in difficulties)
            {
                if (profile != null && profile.Difficulty == difficulty)
                    return profile;
            }

            return new BattleDifficultyProfile(difficulty, 1f);
        }

        /// <summary>
        /// Threat budget of a depth and difficulty (the depth-1 budget plus the per-depth increment, times the
        /// difficulty). Depths here are balance depths (<see cref="MapGenerationSettings.GetBalanceDepth"/>).
        /// </summary>
        public float GetBudget(float depth, EBattleDifficulty difficulty) =>
            GetBaseBudgetAt(depth) * GetProfile(difficulty).BudgetMultiplier;

        /// <summary>
        /// Normal-difficulty budget of a depth: the budget curve when it has points, else the base budget plus the
        /// per-depth increment.
        /// </summary>
        public float GetBaseBudgetAt(float depth)
        {
            if (budgetPoints.Count == 0)
                return baseBudget + budgetPerDepth * Mathf.Max(0f, depth - 1f);

            BudgetPoint lower = null;
            BudgetPoint upper = null;
            foreach (var point in budgetPoints)
            {
                if (point == null) continue;
                if (point.Depth <= depth && (lower == null || point.Depth > lower.Depth)) lower = point;
                if (point.Depth >= depth && (upper == null || point.Depth < upper.Depth)) upper = point;
            }

            if (lower == null) return upper != null ? upper.Budget : baseBudget;
            if (upper == null || upper.Depth <= lower.Depth) return lower.Budget;

            var t = (depth - lower.Depth) / (upper.Depth - lower.Depth);
            return Mathf.Lerp(lower.Budget, upper.Budget, t);
        }

        /// <summary>
        /// Threat budget the XP of a depth and difficulty is sized from: the battle budget unless
        /// <c>XP Base Budget</c> sets its own curve.
        /// </summary>
        public float GetXpBudget(float depth, EBattleDifficulty difficulty)
        {
            if (xpBaseBudget <= 0f) return GetBudget(depth, difficulty);

            return (xpBaseBudget + xpBudgetPerDepth * Mathf.Max(0f, depth - 1f)) *
                   GetProfile(difficulty).BudgetMultiplier;
        }

        /// <summary>The composition band that applies to <paramref name="depth"/>, or null when none does.</summary>
        [CanBeNull]
        public CompositionBand GetCompositionBand(float depth)
        {
            var whole = Mathf.RoundToInt(depth);
            CompositionBand best = null;
            foreach (var band in compositionBands)
            {
                if (band == null || band.MinDepth > whole) continue;
                if (best == null || band.MinDepth >= best.MinDepth)
                    best = band;
            }

            return best;
        }

        /// <summary>Factor of the XP of a depth (1 + XP Per Depth x (depth - 1)).</summary>
        public float GetXpDepthFactor(float depth) => 1f + xpPerDepth * Mathf.Max(0f, depth - 1f);

        /// <summary>HP multiplier of the enemies of a depth (before the difficulty's strength multiplier).</summary>
        public float GetHpMultiplier(float depth) => Grow(hpPerDepth, depth);

        /// <summary>Damage multiplier of the enemies of a depth (before the difficulty's strength multiplier).</summary>
        public float GetDamageMultiplier(float depth) => Grow(damagePerDepth, depth);

        /// <summary>The limit set for a role, or -1 when it has none.</summary>
        public int GetRoleLimit([CanBeNull] EnemyRoleDefinition role)
        {
            if (role == null) return -1;

            foreach (var limit in roleLimits)
            {
                if (limit != null && limit.Role == role)
                    return limit.Max;
            }

            return -1;
        }

        private float Grow(float rate, float depth)
        {
            var steps = Mathf.Max(0f, depth - 1f);
            return compoundScaling ? Mathf.Pow(1f + rate, steps) : 1f + rate * steps;
        }
    }
}
