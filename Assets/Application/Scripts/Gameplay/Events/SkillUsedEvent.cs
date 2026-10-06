using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="SkillDefinition.Execute"/> when a skill is used, after
    /// its damage and effects were applied (hook for skill audio and VFX; the
    /// cells flash on their own).
    /// </summary>
    public class SkillUsedEvent
    {
        public Character Caster { get; }
        public SkillDefinition Skill { get; }

        /// <summary>Cell the skill was aimed at.</summary>
        public Vector2Int TargetPos { get; }

        /// <summary>Cells of the area of effect (inside the grid), sorted by row then column.</summary>
        public IReadOnlyList<Vector2Int> AreaCells { get; }

        /// <summary>Characters that were hit or otherwise affected (resolved before the damage).</summary>
        public IReadOnlyList<Character> Affected { get; }

        public SkillUsedEvent(Character caster, SkillDefinition skill, Vector2Int targetPos,
            IReadOnlyList<Vector2Int> areaCells, IReadOnlyList<Character> affected)
        {
            Caster = caster;
            Skill = skill;
            TargetPos = targetPos;
            AreaCells = areaCells;
            Affected = affected;
        }
    }
}
