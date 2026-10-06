using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Positions the enemy at a preferred distance from the target (ranged,
    /// support and summoner roles). Among the cells it can walk to plus its own
    /// cell, it picks the one whose distance to the target (attack range metric)
    /// is closest to <see cref="PreferredDistance"/>; with
    /// <see cref="PreferTargetInAttackRange"/>, cells from which the target is in
    /// the enemy's attack range win first. Ties prefer staying put, then the
    /// lowest row, then the lowest column. The turn is consumed only when the
    /// enemy actually moves, so later actions of the behavior still run when it
    /// is already well placed.
    /// </summary>
    [CreateAssetMenu(fileName = "KeepDistance", menuName = "GridBattle/AI/Actions/Keep Distance", order = 3)]
    public class KeepDistanceAction : EnemyAction
    {
        private const float Epsilon = 0.0001f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Distance to the target the enemy tries to hold (attack range metric of CombatSettings).")]
        private float preferredDistance = 2f;

        [SerializeField]
        [Tooltip("Cells from which the target is in the enemy's attack range are preferred over any other (ranged attackers). Turn off for enemies that only want to stay away (support, summoner).")]
        private bool preferTargetInAttackRange = true;

        public float PreferredDistance => preferredDistance;
        public bool PreferTargetInAttackRange => preferTargetInAttackRange;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            var self = context.Self;
            var targetPos = context.Target.CurrentGridPos;
            var range = self.WalkDistance;
            if (range <= 0) return false;

            var start = self.CurrentGridPos;
            var best = start;
            var bestScore = Score(start, targetPos, self.AttackDistance);

            for (var y = -range; y <= range; y++)
            {
                for (var x = -range; x <= range; x++)
                {
                    if (x == 0 && y == 0) continue;

                    var candidate = start + new Vector2Int(x, y);
                    if (!GridRules.CanWalkTo(context.Grid, self, candidate)) continue;

                    var score = Score(candidate, targetPos, self.AttackDistance);
                    // Candidates are visited by row, then column: only a strictly
                    // better score replaces the current best, so ties keep the
                    // own cell first and then the lowest (y, x).
                    if (!IsBetter(score, bestScore)) continue;

                    best = candidate;
                    bestScore = score;
                }
            }

            if (best == start) return false;

            context.Grid.MoveEntity(self, best);
            return true;
        }

        /// <summary>Lower is better: (not in attack range, distance error to the preferred distance).</summary>
        private (int OutOfRange, float Error) Score(Vector2Int cell, Vector2Int targetPos, int attackDistance)
        {
            var distance = GridDistance.Measure(CombatResolver.Settings.AttackMetric, cell, targetPos);
            var outOfRange = preferTargetInAttackRange && !GridRules.IsInAttackRange(cell, targetPos, attackDistance)
                ? 1
                : 0;
            return (outOfRange, Mathf.Abs(distance - preferredDistance));
        }

        private static bool IsBetter((int OutOfRange, float Error) score, (int OutOfRange, float Error) best)
        {
            if (score.OutOfRange != best.OutOfRange) return score.OutOfRange < best.OutOfRange;
            return score.Error < best.Error - Epsilon;
        }
    }
}
