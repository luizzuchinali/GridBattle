using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Summons an enemy next to the summoner (summoner role): when fewer than
    /// <see cref="MaxAlive"/> of its minions are alive and the ability is off
    /// cooldown, it spawns <see cref="Minion"/> on the free cell next to it that is
    /// closest to the target (ties: lowest row, then lowest column), with the
    /// summoner's attribute scaling. The minion does not act on the turn it
    /// appears. Starts the cooldown and consumes the turn. A summoner can create at most
    /// <see cref="MaxTotalSummons"/> minions per battle (0 = no limit).
    /// </summary>
    [CreateAssetMenu(fileName = "Summon", menuName = "GridBattle/AI/Actions/Summon", order = 7)]
    public class SummonAction : EnemyAbilityAction
    {
        private const float Epsilon = 0.0001f;

        [Header("Minion")]
        [SerializeField]
        [Tooltip("Enemy that is summoned.")]
        private EnemyConfig minion;

        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum minions of this type alive at once. Enemies restored from a save do not remember their summoner, so they count for every summoner of the same minion type.")]
        private int maxAlive = 2;

        [SerializeField]
        [Min(0)]
        [Tooltip("Most minions this summoner creates in one battle, however many die (0 = no limit). Keeps a fight from going on forever while the summoner stays out of reach; the count is saved with the enemy's memory.")]
        private int maxTotalSummons = 6;

        [SerializeField]
        [Min(1)]
        [Tooltip("How far from the summoner the minion may appear (in cells, diagonals included). 1 = adjacent cells.")]
        private int summonRadius = 1;

        [Header("Reward")]
        [SerializeField]
        [Tooltip("Whether summoned enemies grant XP when killed. Open question (xp_e_niveis.md): off by default, otherwise the XP of the battle could be inflated.")]
        private bool summonedGrantXp;

        public EnemyConfig Minion => minion;
        public int MaxAlive => maxAlive;
        public int MaxTotalSummons => maxTotalSummons;
        public bool SummonedGrantXp => summonedGrantXp;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            if (minion == null) return false;
            if (!IsAvailable(context)) return false;

            var self = context.Self;
            var grid = context.Grid;
            if (CountAliveMinions(context) >= maxAlive) return false;
            if (maxTotalSummons > 0 && context.Memory.Get(TotalKey) >= maxTotalSummons) return false;

            var cell = FindSummonCell(context);
            if (cell == null) return false;

            var xpReward = summonedGrantXp ? minion.XpReward : 0;
            var summoned = grid.SpawnEnemy(minion, cell.Value, self.Scaling, xpReward);
            if (summoned == null) return false;

            summoned.MarkSummoned(self);
            context.Memory.Set(TotalKey, context.Memory.Get(TotalKey) + 1);
            StartCooldown(context);
            EventBus.Raise(new EnemySummonedEvent(self, summoned));
            return true;
        }

        /// <summary>Memory key of the number of minions this summoner has created so far.</summary>
        private string TotalKey => EnemyMemory.Key(this, "total");

        private int CountAliveMinions(in EnemyTurnContext context)
        {
            var alive = 0;
            foreach (var ally in context.GetAllies())
            {
                if (!ally.IsSummoned || ally.Config != minion) continue;
                // A minion whose summoner is unknown counts for every summoner.
                if (ally.Summoner == null || ally.Summoner == context.Self)
                    alive++;
            }

            return alive;
        }

        private Vector2Int? FindSummonCell(in EnemyTurnContext context)
        {
            var grid = context.Grid;
            var origin = context.Self.CurrentGridPos;
            var targetPos = context.Target.CurrentGridPos;
            var metric = CombatResolver.Settings.AttackMetric;

            Vector2Int? best = null;
            var bestDistance = float.MaxValue;
            for (var y = -summonRadius; y <= summonRadius; y++)
            {
                for (var x = -summonRadius; x <= summonRadius; x++)
                {
                    if (x == 0 && y == 0) continue;

                    var candidate = origin + new Vector2Int(x, y);
                    if (!grid.IsValidPosition(candidate) || !grid.IsWalkable(candidate) || !grid.IsFreePosition(candidate)) continue;

                    // Visited by row, then column: only a strictly closer cell replaces the choice.
                    var distance = GridDistance.Measure(metric, candidate, targetPos);
                    if (best != null && distance >= bestDistance - Epsilon) continue;

                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
