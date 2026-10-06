using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Rules for aiming skills: range, area, who is affected, and whether a skill
    /// can be used. Pure static functions over the grid and the characters, shared
    /// by the player's input, the enemy AI, highlights and
    /// <see cref="SkillDefinition"/>. Every number comes from the <see cref="EffectiveSkill"/> of the caster (the
    /// authored skill plus the modifiers of its states): the overloads that take a <see cref="SkillDefinition"/>
    /// resolve it once, and loops resolve it themselves and call the overloads that take it.
    /// </summary>
    public static class SkillTargeting
    {
        /// <summary>Maximum selection distance: the skill's range plus the caster's Skill Range attribute.</summary>
        public static int GetMaxRange(Character caster, SkillDefinition skill) =>
            GetMaxRange(caster, EffectiveSkill.Resolve(caster, skill));

        /// <inheritdoc cref="GetMaxRange(Character, SkillDefinition)"/>
        public static int GetMaxRange(Character caster, in EffectiveSkill skill)
        {
            return skill.Range + caster.Stats.GetInt(EAttribute.SkillRange);
        }

        /// <summary>
        /// Whether the target cell is close enough to the caster. Self-centered
        /// skills (range 0) only accept the caster's own cell; unlimited ones accept
        /// any cell. Does not check the grid bounds.
        /// </summary>
        public static bool IsTargetInRange(Character caster, SkillDefinition skill, Vector2Int targetPos) =>
            IsTargetInRange(caster, EffectiveSkill.Resolve(caster, skill), targetPos);

        /// <inheritdoc cref="IsTargetInRange(Character, SkillDefinition, Vector2Int)"/>
        public static bool IsTargetInRange(Character caster, in EffectiveSkill skill, Vector2Int targetPos)
        {
            if (skill.UnlimitedRange) return true;
            if (skill.IsSelfCentered) return targetPos == caster.CurrentGridPos;

            return GridDistance.IsWithin(CombatResolver.Settings.SkillRangeMetric, caster.CurrentGridPos, targetPos,
                GetMaxRange(caster, skill));
        }

        /// <summary>Cells of the skill's area (inside the grid) when aimed at <paramref name="targetPos"/>.</summary>
        public static List<Vector2Int> GetAreaCells(GridController grid, Character caster, SkillDefinition skill,
            Vector2Int targetPos) =>
            GetAreaCells(grid, caster, EffectiveSkill.Resolve(caster, skill), targetPos);

        /// <inheritdoc cref="GetAreaCells(GridController, Character, SkillDefinition, Vector2Int)"/>
        public static List<Vector2Int> GetAreaCells(GridController grid, Character caster, in EffectiveSkill skill,
            Vector2Int targetPos)
        {
            // Terrain that blocks skill areas (open question, off by default) is skipped.
            return SkillArea.GetCells(skill.AreaShape, skill.AreaSize, caster.CurrentGridPos, targetPos,
                cell => grid.IsValidPosition(cell) && !grid.BlocksSkillArea(cell));
        }

        /// <summary>
        /// Living characters inside the area that the skill's target filter reaches,
        /// in cell order (row, then column).
        /// </summary>
        public static List<Character> GetAffectedCharacters(GridController grid, Character caster,
            SkillDefinition skill, Vector2Int targetPos) =>
            GetAffectedCharacters(grid, caster, EffectiveSkill.Resolve(caster, skill), targetPos);

        /// <inheritdoc cref="GetAffectedCharacters(GridController, Character, SkillDefinition, Vector2Int)"/>
        public static List<Character> GetAffectedCharacters(GridController grid, Character caster,
            in EffectiveSkill skill, Vector2Int targetPos)
        {
            return GetAffectedCharacters(grid, caster, skill.Skill, GetAreaCells(grid, caster, skill, targetPos));
        }

        private static List<Character> GetAffectedCharacters(GridController grid, Character caster,
            SkillDefinition skill, List<Vector2Int> areaCells)
        {
            var affected = new List<Character>();
            foreach (var cell in areaCells)
            {
                if (grid.GetContent(cell) is Character character && !character.IsDead &&
                    IsAffected(caster, character, skill.Affects))
                    affected.Add(character);
            }

            return affected;
        }

        /// <summary>Whether <paramref name="candidate"/> passes the target filter for <paramref name="caster"/>.</summary>
        public static bool IsAffected(Character caster, Character candidate, ESkillTargetFilter filter)
        {
            return filter switch
            {
                ESkillTargetFilter.Enemies => !GridRules.AreAllies(caster, candidate),
                ESkillTargetFilter.Allies => candidate != caster && GridRules.AreAllies(caster, candidate),
                ESkillTargetFilter.AlliesAndSelf => GridRules.AreAllies(caster, candidate),
                ESkillTargetFilter.Self => candidate == caster,
                _ => true,
            };
        }

        /// <summary>Resolves the area and the affected characters of a use, before any damage.</summary>
        public static SkillContext CreateContext(GridController grid, Character caster, SkillDefinition skill,
            Vector2Int targetPos) =>
            CreateContext(grid, caster, EffectiveSkill.Resolve(caster, skill), targetPos);

        /// <inheritdoc cref="CreateContext(GridController, Character, SkillDefinition, Vector2Int)"/>
        public static SkillContext CreateContext(GridController grid, Character caster, in EffectiveSkill skill,
            Vector2Int targetPos)
        {
            var cells = GetAreaCells(grid, caster, skill, targetPos);
            var affected = GetAffectedCharacters(grid, caster, skill.Skill, cells);
            return new SkillContext(caster, grid, skill.Skill, skill, targetPos, cells, affected);
        }

        /// <summary>
        /// Whether the caster is able to use the skill at all right now (alive, not
        /// silenced, cooldown ready), wherever it is aimed. This is what lets a
        /// skill button be pressed.
        /// </summary>
        public static bool CanSelect(Character caster, SkillDefinition skill)
        {
            if (caster == null || skill == null || caster.IsDead) return false;
            if (caster.States.IsRestricted(EBehaviorRestriction.PreventSkills)) return false;

            return caster.Cooldowns.IsReady(skill);
        }

        /// <summary>
        /// Whether the target cell is a valid aim for the skill: inside the grid,
        /// in range, the effects accept it and (when the skill requires it) at least
        /// one character would be affected. Ignores cooldown and restrictions
        /// (see <see cref="CanSelect"/>).
        /// </summary>
        public static bool IsValidTarget(GridController grid, Character caster, SkillDefinition skill,
            Vector2Int targetPos) =>
            IsValidTarget(grid, caster, EffectiveSkill.Resolve(caster, skill), targetPos);

        /// <inheritdoc cref="IsValidTarget(GridController, Character, SkillDefinition, Vector2Int)"/>
        public static bool IsValidTarget(GridController grid, Character caster, in EffectiveSkill skill,
            Vector2Int targetPos)
        {
            if (!grid.IsValidPosition(targetPos)) return false;
            if (!IsTargetInRange(caster, skill, targetPos)) return false;

            var context = CreateContext(grid, caster, skill, targetPos);
            if (skill.Skill.RequiresAffectedTarget && context.Affected.Count == 0) return false;

            foreach (var effect in skill.Effects)
            {
                if (effect != null && !effect.CanUse(context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Full use check: <see cref="CanSelect"/> plus <see cref="IsValidTarget(GridController, Character, SkillDefinition, Vector2Int)"/>.
        /// </summary>
        public static bool CanUse(GridController grid, Character caster, SkillDefinition skill, Vector2Int targetPos)
        {
            return CanSelect(caster, skill) && IsValidTarget(grid, caster, skill, targetPos);
        }

        /// <summary>
        /// Every cell the skill can currently be aimed at (valid target cells, see
        /// <see cref="IsValidTarget(GridController, Character, SkillDefinition, Vector2Int)"/>), in row-then-column
        /// order. Used for the range highlights. Empty if the caster cannot use the skill right now.
        /// </summary>
        public static List<Vector2Int> GetTargetableCells(GridController grid, Character caster,
            SkillDefinition skill)
        {
            var cells = new List<Vector2Int>();
            if (!CanSelect(caster, skill)) return cells;

            var effective = EffectiveSkill.Resolve(caster, skill);
            var size = MeasureGrid(grid);
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsValidTarget(grid, caster, effective, cell))
                        cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>
        /// Picks a cell to aim the skill at so that <paramref name="opponent"/> is
        /// hit: the opponent's own cell if valid, otherwise the nearest valid cell
        /// (to the caster; ties by row then column) whose area reaches it. Used by the
        /// enemy AI; deterministic. Returns false if no cell works.
        /// </summary>
        public static bool TryFindAimCell(GridController grid, Character caster, SkillDefinition skill,
            Character opponent, out Vector2Int aimCell)
        {
            aimCell = default;
            if (!CanSelect(caster, skill) || opponent == null || opponent.IsDead) return false;

            var effective = EffectiveSkill.Resolve(caster, skill);
            if (Reaches(grid, caster, effective, opponent.CurrentGridPos, opponent))
            {
                aimCell = opponent.CurrentGridPos;
                return true;
            }

            var found = false;
            var bestDistance = int.MaxValue;
            var size = MeasureGrid(grid);
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!Reaches(grid, caster, effective, cell, opponent)) continue;

                    var distance = Mathf.Abs(cell.x - caster.CurrentGridPos.x) +
                                   Mathf.Abs(cell.y - caster.CurrentGridPos.y);
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    aimCell = cell;
                    found = true;
                }
            }

            return found;
        }

        private static bool Reaches(GridController grid, Character caster, in EffectiveSkill skill, Vector2Int cell,
            [NotNull] Character opponent)
        {
            if (!IsValidTarget(grid, caster, skill, cell)) return false;

            return GetAffectedCharacters(grid, caster, skill, cell).Contains(opponent);
        }

        /// <summary>Width and height of the grid (<see cref="GridController.Size"/>).</summary>
        public static Vector2Int MeasureGrid(GridController grid)
        {
            return grid.Size;
        }
    }
}
