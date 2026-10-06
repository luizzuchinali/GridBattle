using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Stats;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// How much an enemy values the outcome of a push or pull (see <see cref="DisplacementScoring"/>), in
    /// "HP-equivalent" points. Lives on <see cref="Actions.UseSkillsAction"/>, so the weights are tuned per asset
    /// in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class DisplacementAiWeights
    {
        [SerializeField]
        [Tooltip("A skill that pushes or pulls is only used when the predicted outcome is worth at least this much " +
                 "(the sum of the items below). Skills without displacement ignore it.")]
        private float minScore = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of each ally (the caster included) that can hit the displaced opponent from where it ends up, minus the ones that could where it started: pulling the player into the middle of the group is good, pushing it out of reach is bad.")]
        private float allyReachValue = 5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of each cell the displaced opponent ends closer to the caster (pulls), charged for each cell it ends farther (pushes).")]
        private float approachValue = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of a lethal outcome (the collision or the terrain where it stops would kill the opponent).")]
        private float killValue = 50f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Value of each harmful state a hazard cell gives to the opponent when it stops there (damage counts on its own).")]
        private float hazardStateValue = 4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Cost of each beneficial state a bonus cell gives to the opponent when it stops there.")]
        private float bonusStateCost = 5f;

        public float MinScore => minScore;
        public float AllyReachValue => allyReachValue;
        public float ApproachValue => approachValue;
        public float KillValue => killValue;
        public float HazardStateValue => hazardStateValue;
        public float BonusStateCost => bonusStateCost;
    }

    /// <summary>
    /// Pure scoring of a push or pull skill use for the enemy AI: predicts the outcome with
    /// <see cref="DisplacementResolver.Predict"/> (the same rules the real use applies) and adds up what it is
    /// worth to the caster's side. Damage to opponents and hazards they are sent onto count in their favor;
    /// damage to the caster's allies (a displaced opponent that hits one) counts against; so do bonus cells.
    /// Allies in attack reach of the displaced opponent, before and after, measure whether it was brought into
    /// the group or sent away from it.
    /// </summary>
    public static class DisplacementScoring
    {
        /// <summary>
        /// Value of the displacement the skill would cause when aimed at <paramref name="aimCell"/>.
        /// <paramref name="allies"/> are the caster's side (the caster included).
        /// </summary>
        public static float Score(GridController grid, Character caster, SkillDefinition skill, Vector2Int aimCell,
            IReadOnlyList<Character> allies, DisplacementAiWeights weights) =>
            Score(grid, caster, EffectiveSkill.Resolve(caster, skill), aimCell, allies, weights);

        /// <inheritdoc cref="Score(GridController, Character, SkillDefinition, Vector2Int, IReadOnlyList{Character}, DisplacementAiWeights)"/>
        public static float Score(GridController grid, Character caster, in EffectiveSkill skill, Vector2Int aimCell,
            IReadOnlyList<Character> allies, DisplacementAiWeights weights)
        {
            var effect = skill.FindDisplacement();
            if (effect == null) return 0f;

            var affected = SkillTargeting.GetAffectedCharacters(grid, caster, skill, aimCell);
            var score = 0f;
            foreach (var forecast in DisplacementResolver.Predict(grid, caster, affected, effect.Mode,
                         skill.GetDisplacementDistance(effect), aimCell))
            {
                var target = forecast.Result.Target;
                var isOpponent = !GridRules.AreAllies(caster, target);
                var sign = isOpponent ? 1f : -1f;

                var damage = forecast.CollisionDamage + forecast.TerrainDamage;
                score += sign * damage;
                if (isOpponent && damage > 0 && damage >= GetEffectiveHp(target))
                    score += weights.KillValue;

                if (forecast.HitCharacter != null)
                {
                    var hitIsOpponent = !GridRules.AreAllies(caster, forecast.HitCharacter);
                    score += (hitIsOpponent ? 1f : -1f) * forecast.HitCharacterDamage;
                    if (hitIsOpponent && forecast.HitCharacterDamage >= GetEffectiveHp(forecast.HitCharacter))
                        score += weights.KillValue;
                }

                if (forecast.ForcedTerrain != null)
                    score += sign * GetTerrainStateValue(forecast.ForcedTerrain, weights);

                if (isOpponent && forecast.Result.CellsMoved > 0)
                {
                    score += weights.AllyReachValue * (CountInReach(allies, forecast.Result.Final) -
                                                       CountInReach(allies, forecast.Result.Start));
                    score += weights.ApproachValue * (GetChebyshev(forecast.Result.Start, caster.CurrentGridPos) -
                                                      GetChebyshev(forecast.Result.Final, caster.CurrentGridPos));
                }
            }

            return score;
        }

        /// <summary>Number of <paramref name="allies"/> that can hit a character standing on <paramref name="cell"/> with their basic attack.</summary>
        public static int CountInReach(IReadOnlyList<Character> allies, Vector2Int cell)
        {
            var count = 0;
            foreach (var ally in allies)
            {
                if (ally == null || ally.IsDead || !ally.CanBasicAttack) continue;
                if (GridRules.IsInAttackRange(ally.CurrentGridPos, cell, ally.AttackDistance))
                    count++;
            }

            return count;
        }

        private static int GetChebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));

        private static float GetTerrainStateValue(TerrainDefinition terrain, DisplacementAiWeights weights)
        {
            // Value for the side that sends the character there: hazards hurt it, bonus cells help it.
            var states = 0;
            foreach (var grant in terrain.States)
            {
                if (grant.IsValid) states++;
            }

            if (states == 0) return 0f;
            return terrain.Kind == ETerrainKind.Bonus
                ? -weights.BonusStateCost * states
                : weights.HazardStateValue * states;
        }

        private static int GetEffectiveHp(Character character)
        {
            return character.Current + character.States.TotalShield;
        }
    }
}
