using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Tries the enemy's skills, in order, against the player: for each ready skill
    /// it aims at the player's cell, or at the nearest valid cell whose area still
    /// reaches the player (<see cref="SkillTargeting.TryFindAimCell"/>), and uses
    /// the first one that works. With <see cref="CoverEscapeCells"/> (on in the game's asset, decision of 2026-10-20)
    /// a strike is aimed instead where its area covers the most cells the player could stand on when it fires (its
    /// cell and the cells it can walk to), counting less the cells other strikes already cover: telegraphed strikes
    /// no longer miss to a single step aside. Range, area, cooldown and effects live in each
    /// <see cref="SkillDefinition"/>; cooldowns tick at the end of each of the
    /// enemy's turns. Skills that cannot reach the player (support skills) are
    /// skipped; they are handled by other actions.
    /// <para>
    /// Skills that push or pull (<see cref="DisplaceSkillEffect"/>) are only used when the predicted outcome is
    /// worth it (<see cref="DisplacementScoring"/>, weights in <see cref="DisplacementAiWeights"/>): the aim cell is the
    /// one with the best score among the cells that reach the player, and the skill is skipped when even that
    /// one scores below the minimum (an enemy does not push the player out of reach for nothing).
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "UseSkills", menuName = "GridBattle/AI/Actions/Use Skills", order = 0)]
    public class UseSkillsAction : EnemyAction
    {
        [Header("Aim")]
        [SerializeField]
        [Tooltip("Aim where the area covers the most cells the player could stand on when the strike fires (its cell " +
                 "and the cells it can walk to) instead of only the player's cell. Off = the old aim.")]
        private bool coverEscapeCells;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of covering the player's own cell.")]
        private float playerCellWeight = 1.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of covering a cell the player can walk to.")]
        private float escapeCellWeight = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Factor on the value of a cell another enemy's prepared strike already covers.")]
        private float alreadyCoveredFactor = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value lost for each other enemy the area would hit (strikes hit everyone but their caster).")]
        private float allyHitPenalty = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Lowest value worth preparing a strike for (below it the skill is skipped).")]
        private float minCoverValue = 1f;

        [Header("Pushing and pulling")]
        [SerializeField]
        private DisplacementAiWeights displacement = new();

        public DisplacementAiWeights DisplacementWeights => displacement;

        /// <summary>Whether strikes aim at the player's escape cells too (see the class summary).</summary>
        public bool CoverEscapeCells => coverEscapeCells;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            var self = context.Self;
            List<Character> allies = null;
            HashSet<Vector2Int> zone = null;
            HashSet<Vector2Int> covered = null;
            foreach (var skill in self.Skills)
            {
                if (skill == null) continue;

                Vector2Int aimCell;
                var effective = EffectiveSkill.Resolve(self, skill);
                if (effective.FindDisplacement() != null)
                {
                    allies ??= GetAllies(context);
                    if (!TryFindDisplacementAimCell(context, effective, allies, out aimCell)) continue;
                }
                else if (coverEscapeCells)
                {
                    zone ??= GetPlayerZone(context);
                    covered ??= GetCoveredCells(self);
                    if (!TryFindCoverAimCell(context, effective, zone, covered, out aimCell)) continue;
                }
                else if (!SkillTargeting.TryFindAimCell(context.Grid, self, skill, context.Target, out aimCell))
                {
                    continue;
                }

                if (self.TryUseSkill(context.Grid, skill, aimCell))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The valid cell that reaches the player with the best displacement score (the first one in row, then
        /// column order on ties), provided that score is at least the minimum.
        /// </summary>
        /// <summary>
        /// The aim cell whose area is worth the most over the player's zone (see the class summary): ties go to the
        /// aim that covers the player's cell, then to the one nearest to it, then to the lowest row and column.
        /// </summary>
        private bool TryFindCoverAimCell(in EnemyTurnContext context, in EffectiveSkill skill,
            HashSet<Vector2Int> zone, HashSet<Vector2Int> covered, out Vector2Int aimCell)
        {
            aimCell = default;
            var self = context.Self;
            var target = context.Target;
            if (!SkillTargeting.CanSelect(self, skill.Skill) || target == null || target.IsDead || zone.Count == 0)
                return false;

            var grid = context.Grid;
            var found = false;
            var best = (Value: float.MinValue, HitsPlayer: false, Distance: int.MaxValue);
            foreach (var cell in SkillTargeting.GetTargetableCells(grid, self, skill.Skill))
            {
                var area = SkillTargeting.GetAreaCells(grid, self, skill, cell);
                var value = 0f;
                var hitsPlayer = false;
                foreach (var areaCell in area)
                {
                    if (!zone.Contains(areaCell)) continue;

                    var isPlayerCell = areaCell == target.CurrentGridPos;
                    hitsPlayer |= isPlayerCell;
                    var weight = isPlayerCell ? playerCellWeight : escapeCellWeight;
                    value += covered.Contains(areaCell) ? weight * alreadyCoveredFactor : weight;
                }

                if (value <= 0f) continue;

                foreach (var character in SkillTargeting.GetAffectedCharacters(grid, self, skill, cell))
                {
                    if (character is Enemy && character != self)
                        value -= allyHitPenalty;
                }

                var distance = Mathf.Abs(cell.x - target.CurrentGridPos.x) + Mathf.Abs(cell.y - target.CurrentGridPos.y);
                var candidate = (Value: value, HitsPlayer: hitsPlayer, Distance: distance);
                if (found && !IsBetterCover(candidate, best)) continue;

                found = true;
                best = candidate;
                aimCell = cell;
            }

            return found && best.Value >= minCoverValue;
        }

        private static bool IsBetterCover((float Value, bool HitsPlayer, int Distance) candidate,
            (float Value, bool HitsPlayer, int Distance) best)
        {
            if (!Mathf.Approximately(candidate.Value, best.Value)) return candidate.Value > best.Value;
            if (candidate.HitsPlayer != best.HitsPlayer) return candidate.HitsPlayer;
            return candidate.Distance < best.Distance;
        }

        /// <summary>The player's cell and every cell it could walk to on its next turn.</summary>
        private static HashSet<Vector2Int> GetPlayerZone(in EnemyTurnContext context)
        {
            var zone = new HashSet<Vector2Int>();
            var target = context.Target;
            if (target == null || target.IsDead) return zone;

            var start = target.CurrentGridPos;
            zone.Add(start);
            var walk = target.WalkDistance;
            for (var y = -walk; y <= walk; y++)
            {
                for (var x = -walk; x <= walk; x++)
                {
                    var cell = start + new Vector2Int(x, y);
                    if (cell != start && GridRules.CanWalkTo(context.Grid, target, cell))
                        zone.Add(cell);
                }
            }

            return zone;
        }

        /// <summary>Cells the prepared strikes of the other enemies already cover.</summary>
        private static HashSet<Vector2Int> GetCoveredCells(Character self)
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

        private bool TryFindDisplacementAimCell(in EnemyTurnContext context, in EffectiveSkill skill,
            List<Character> allies, out Vector2Int aimCell)
        {
            aimCell = default;
            var self = context.Self;
            if (!SkillTargeting.CanSelect(self, skill.Skill) || context.Target == null || context.Target.IsDead)
                return false;

            var found = false;
            var bestScore = float.MinValue;
            foreach (var cell in SkillTargeting.GetTargetableCells(context.Grid, self, skill.Skill))
            {
                if (!SkillTargeting.GetAffectedCharacters(context.Grid, self, skill, cell).Contains(context.Target))
                    continue;

                var score = DisplacementScoring.Score(context.Grid, self, skill, cell, allies, displacement);
                if (found && score <= bestScore) continue;

                found = true;
                bestScore = score;
                aimCell = cell;
            }

            return found && bestScore >= displacement.MinScore;
        }

        private static List<Character> GetAllies(in EnemyTurnContext context)
        {
            var allies = new List<Character>();
            foreach (var ally in context.GetAllies(true))
                allies.Add(ally);
            return allies;
        }
    }
}
