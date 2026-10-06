using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Pushes or pulls the characters the skill affects, after the skill's damage (effects run in list order, so
    /// put it before an effect that depends on the new positions). Targets that died from the damage are skipped,
    /// and so are characters that cannot be displaced (<see cref="CharacterConfig.CanBeDisplaced"/>). Several
    /// targets are moved in a fixed order (see <see cref="Displacement.OrderTargets"/>). Stopping against an
    /// obstacle, the edge of the grid or another character deals collision damage and a terrain cell where the
    /// character stops reacts at once (<see cref="DisplacementResolver"/>, values in <c>CombatSettings</c>).
    /// </summary>
    [Serializable]
    public sealed class DisplaceSkillEffect : SkillEffect
    {
        [SerializeField]
        [Tooltip("Away from the caster, toward the caster (a pull: it stops next to the caster) or away from the center of the skill's area.")]
        private EDisplacementMode mode = EDisplacementMode.AwayFromCaster;

        [SerializeField]
        [Min(1)]
        [Tooltip("Cells each target is moved (it stops earlier when something is in the way).")]
        private int distance = 1;

        public EDisplacementMode Mode => mode;
        public int Distance => Mathf.Max(1, distance);

        public override void Apply(in SkillContext context)
        {
            var targets = new List<Character>();
            foreach (var character in context.Affected)
            {
                if (character != null && !character.IsDead)
                    targets.Add(character);
            }

            // The distance is the authored one plus what the caster's talents add (EffectiveSkill).
            DisplacementResolver.Apply(context.Grid, context.Caster, targets, mode,
                context.Effective.GetDisplacementDistance(this), context.TargetPos);
        }

        /// <summary>The authored skill's displacement effect, or null when it has none. For what the caster really uses see <see cref="EffectiveSkill.FindDisplacement"/>.</summary>
        public static DisplaceSkillEffect Find(SkillDefinition skill)
        {
            if (skill == null) return null;

            foreach (var effect in skill.Effects)
            {
                if (effect is DisplaceSkillEffect displace)
                    return displace;
            }

            return null;
        }
    }
}
