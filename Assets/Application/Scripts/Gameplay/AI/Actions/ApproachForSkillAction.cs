using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// The walk of an enemy that only uses skills (2026-10-19 direction): it walks to the cell from which one of its
    /// ready skills reaches the player, so the next action can prepare the strike. Among its own cell and the cells
    /// it can walk to it prefers, in order: cells that are not dangerous (marked by a prepared strike of another
    /// enemy, or hazard terrain that affects it), cells from which a ready skill reaches the player, then the
    /// distance to the player closest to <see cref="PreferredDistance"/> (or simply the closest when no skill
    /// reaches from anywhere), then staying put, then the lowest row and column. The walk is used only when the
    /// enemy moves. Pure in its choice: positions are probed with <see cref="GridController.ProbePosition"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "ApproachForSkill", menuName = "GridBattle/AI/Actions/Approach For Skill", order = 4)]
    public class ApproachForSkillAction : EnemyAction
    {
        private const float Epsilon = 0.0001f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Distance to the player (attack range metric) the enemy prefers among the cells from which it can " +
                 "strike: 1 = as close as possible (melee), higher for enemies that strike from afar.")]
        private float preferredDistance = 1f;

        [SerializeField]
        [Tooltip("Avoid cells marked by the prepared strikes of other enemies (they hit everyone but their caster).")]
        private bool avoidMarkedCells = true;

        [SerializeField]
        [Tooltip("Avoid hazard terrain that affects the enemy.")]
        private bool avoidHazards = true;

        public float PreferredDistance => preferredDistance;

        public override bool IsMovement => true;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            var self = context.Self;
            var grid = context.Grid;
            var target = context.Target;
            if (self == null || target == null || target.IsDead || self.WalkDistance <= 0) return false;

            var marked = avoidMarkedCells ? GetMarkedCells(self) : null;
            var start = self.CurrentGridPos;
            var candidates = new List<Vector2Int> { start };
            var walk = self.WalkDistance;
            for (var y = -walk; y <= walk; y++)
            {
                for (var x = -walk; x <= walk; x++)
                {
                    var cell = start + new Vector2Int(x, y);
                    if (cell != start && GridRules.CanWalkTo(grid, self, cell))
                        candidates.Add(cell);
                }
            }

            var best = start;
            var bestScore = Score(context, start, marked);
            for (var i = 1; i < candidates.Count; i++)
            {
                var score = Score(context, candidates[i], marked);
                if (!IsBetter(score, bestScore)) continue;

                best = candidates[i];
                bestScore = score;
            }

            if (best == start) return false;

            grid.MoveEntity(self, best);
            return true;
        }

        /// <summary>Lower is better: (dangerous, cannot strike, distance error).</summary>
        private (int Danger, int NoStrike, float Error) Score(in EnemyTurnContext context, Vector2Int cell,
            HashSet<Vector2Int> marked)
        {
            var self = context.Self;
            var danger = (marked != null && marked.Contains(cell)) || IsHazard(context.Grid, self, cell) ? 1 : 0;
            var canStrike = CanStrikeFrom(context, cell);
            var distance = GridDistance.Measure(CombatResolver.Settings.AttackMetric, cell,
                context.Target.CurrentGridPos);
            var error = canStrike ? Mathf.Abs(distance - preferredDistance) : distance;
            return (danger, canStrike ? 0 : 1, error);
        }

        private static bool IsBetter((int Danger, int NoStrike, float Error) score,
            (int Danger, int NoStrike, float Error) best)
        {
            if (score.Danger != best.Danger) return score.Danger < best.Danger;
            if (score.NoStrike != best.NoStrike) return score.NoStrike < best.NoStrike;
            return score.Error < best.Error - Epsilon;
        }

        private bool IsHazard(GridController grid, Character self, Vector2Int cell)
        {
            if (!avoidHazards) return false;

            var terrain = grid.GetTerrain(cell);
            return terrain != null && terrain.Kind == ETerrainKind.Hazard && terrain.AffectsCharacter(self);
        }

        /// <summary>Whether a ready skill of the enemy reaches the player from <paramref name="cell"/>.</summary>
        private static bool CanStrikeFrom(in EnemyTurnContext context, Vector2Int cell)
        {
            var self = context.Self;
            using (context.Grid.ProbePosition(self, cell))
            {
                foreach (var skill in self.Skills)
                {
                    if (skill == null) continue;
                    if (SkillTargeting.TryFindAimCell(context.Grid, self, skill, context.Target, out _))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Cells marked by the prepared strikes of the other enemies.</summary>
        private static HashSet<Vector2Int> GetMarkedCells(Character self)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var telegraph in SkillTelegraph.GetAll())
            {
                if (telegraph.Caster == self) continue;
                foreach (var cell in telegraph.Cells)
                    cells.Add(cell);
            }

            return cells;
        }
    }
}
