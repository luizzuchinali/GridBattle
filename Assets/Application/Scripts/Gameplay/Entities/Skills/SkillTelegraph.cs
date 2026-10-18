using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Run;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// A telegraphed skill being prepared by an enemy (<see cref="SkillDefinition.Telegraphed"/>): who prepares it,
    /// the skill, the aimed cell, the cells of the area fixed when it was prepared (they never follow anybody) and the
    /// turns of the enemy left until it fires (1 = on its next turn). Runtime state owned by the
    /// <see cref="Enemy"/> (never by the skill asset) and saved with the battle (<see cref="TelegraphState"/>).
    /// </summary>
    public sealed class SkillTelegraph
    {
        private readonly List<Vector2Int> _cells;

        public SkillTelegraph(Character caster, SkillDefinition skill, Vector2Int targetPos,
            IEnumerable<Vector2Int> cells, int turnsLeft)
        {
            Caster = caster;
            Skill = skill;
            TargetPos = targetPos;
            _cells = new List<Vector2Int>(cells);
            TurnsLeft = Mathf.Max(1, turnsLeft);
        }

        public Character Caster { get; }
        public SkillDefinition Skill { get; }

        /// <summary>The cell the skill was aimed at when it was prepared.</summary>
        public Vector2Int TargetPos { get; }

        /// <summary>The marked cells: the area of the skill when it was prepared, in row then column order.</summary>
        public IReadOnlyList<Vector2Int> Cells => _cells;

        /// <summary>Turns of the caster left until the skill fires (1 = on its next turn).</summary>
        public int TurnsLeft { get; private set; }

        public bool Contains(Vector2Int cell) => _cells.Contains(cell);

        /// <summary>One of the caster's turns started: one turn less to wait. Returns true when it fires now.</summary>
        public bool Advance()
        {
            TurnsLeft = Mathf.Max(0, TurnsLeft - 1);
            return TurnsLeft <= 0;
        }

        /// <summary>Whether firing now would hit <paramref name="character"/> (on a marked cell, passes the target filter).</summary>
        public bool WouldHit(Character character)
        {
            return character != null && !character.IsDead && Contains(character.CurrentGridPos) &&
                   SkillTargeting.IsAffected(Caster, character, Skill.Affects);
        }

        /// <summary>
        /// The damage the skill would deal to <paramref name="target"/> if it stood on a marked cell when the skill
        /// fires (no critical, no random draw; see <see cref="CombatResolver.PredictDamage"/>). 0 for a skill without
        /// damage or a target it does not affect.
        /// </summary>
        public int PredictDamage(Character target)
        {
            if (target == null || Caster == null || !SkillTargeting.IsAffected(Caster, target, Skill.Affects)) return 0;

            var damage = EffectiveSkill.Resolve(Caster, Skill).Damage;
            return Skill.Damage > 0 && damage > 0
                ? CombatResolver.PredictDamage(Caster, target, damage, EDamageKind.Skill)
                : 0;
        }

        /// <summary>Saved form (skill id, aimed cell, marked cells, turns left).</summary>
        public TelegraphState Capture()
        {
            var state = new TelegraphState
            {
                SkillId = Skill != null ? Skill.Id : null,
                TargetX = TargetPos.x,
                TargetY = TargetPos.y,
                TurnsLeft = TurnsLeft,
            };
            foreach (var cell in _cells)
                state.Cells.Add(new GridCellState { X = cell.x, Y = cell.y });
            return state;
        }

        /// <summary>Rebuilds a preparation from its saved form, or null when the skill is unknown.</summary>
        [CanBeNull]
        public static SkillTelegraph Restore(Character caster, [CanBeNull] TelegraphState state)
        {
            if (state == null || string.IsNullOrEmpty(state.SkillId)) return null;

            var database = GameDatabase.Instance;
            var skill = database != null ? database.Get<SkillDefinition>(state.SkillId) : null;
            if (skill == null)
            {
                Debug.LogWarning($"Telegraphed skill id '{state.SkillId}' not found in the GameDatabase; skipped.");
                return null;
            }

            var cells = new List<Vector2Int>();
            if (state.Cells != null)
            {
                foreach (var cell in state.Cells)
                {
                    if (cell != null)
                        cells.Add(new Vector2Int(cell.X, cell.Y));
                }
            }

            return new SkillTelegraph(caster, skill, new Vector2Int(state.TargetX, state.TargetY), cells,
                state.TurnsLeft);
        }

        /// <summary>
        /// Every preparation on the board, in a deterministic order (caster row, then column): for the cell marks,
        /// the details window and the simulation bot.
        /// </summary>
        public static List<SkillTelegraph> GetAll()
        {
            var result = new List<SkillTelegraph>();
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy != null && !enemy.IsDead && enemy.Telegraph != null)
                    result.Add(enemy.Telegraph);
            }

            result.Sort((a, b) =>
            {
                var posA = a.Caster.CurrentGridPos;
                var posB = b.Caster.CurrentGridPos;
                var byY = posA.y.CompareTo(posB.y);
                return byY != 0 ? byY : posA.x.CompareTo(posB.x);
            });
            return result;
        }

        /// <summary>Every preparation whose marked cells include <paramref name="cell"/> (see <see cref="GetAll"/>).</summary>
        public static List<SkillTelegraph> GetAt(Vector2Int cell)
        {
            var result = GetAll();
            result.RemoveAll(telegraph => !telegraph.Contains(cell));
            return result;
        }
    }
}
