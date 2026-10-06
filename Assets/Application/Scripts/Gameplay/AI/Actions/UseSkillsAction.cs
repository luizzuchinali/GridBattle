using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Tries the enemy's skills, in order, against the player: for each ready skill
    /// it aims at the player's cell, or at the nearest valid cell whose area still
    /// reaches the player (<see cref="SkillTargeting.TryFindAimCell"/>), and uses
    /// the first one that works. Range, area, cooldown and effects live in each
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
        [Header("Pushing and pulling")]
        [SerializeField]
        private DisplacementAiWeights displacement = new();

        public DisplacementAiWeights DisplacementWeights => displacement;

        public override bool TryExecute(in EnemyTurnContext context)
        {
            var self = context.Self;
            List<Character> allies = null;
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
