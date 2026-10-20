using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Forecast
{
    /// <summary>
    /// What one player action aimed at a cell would do, computed before it is confirmed (Into the Breach style): the
    /// cells of its area, the damage each character would take (the hit, collisions and the terrain a push sends it
    /// to; no critical hit, before shields), who would die, where pushed, pulled, swapped or dashing characters would
    /// end and the displacement of each one, and who a stun would stop. Pure: reads the board, changes nothing. The
    /// forecast view draws it and <see cref="ThreatForecast"/> uses the new positions to show where the enemies'
    /// prepared strikes would land.
    /// </summary>
    public sealed class ActionForecast
    {
        private readonly Dictionary<Character, Vector2Int> _finals = new();

        public Vector2Int Target { get; private set; }

        /// <summary>Cells of the area, inside the grid.</summary>
        public List<Vector2Int> Area { get; } = new();

        /// <summary>Damage each character would take, in the order they were found.</summary>
        public Dictionary<Character, int> Damage { get; } = new();

        /// <summary>Characters the action would kill (damage at least their HP plus shields).</summary>
        public HashSet<Character> Killed { get; } = new();

        /// <summary>The pushes and pulls of the action, in the order they would be applied.</summary>
        public List<DisplacementForecast> Moves { get; } = new();

        /// <summary>Characters that would change cells (displaced, swapped, the dashing caster) and where they end.</summary>
        public IReadOnlyDictionary<Character, Vector2Int> Finals => _finals;

        /// <summary>
        /// Survivors that would get a state forbidding skills (stunned, frozen, blinded): a strike they prepared and
        /// that can be interrupted is cancelled (see <see cref="Enemy"/>).
        /// </summary>
        public HashSet<Character> Interrupted { get; } = new();

        /// <summary>The cell <paramref name="character"/> would stand on after the action (its own when it stays).</summary>
        public Vector2Int GetPosition(Character character) =>
            _finals.TryGetValue(character, out var position) ? position : character.CurrentGridPos;

        /// <summary>Forecast of <paramref name="skill"/> used by <paramref name="caster"/> on <paramref name="target"/>.</summary>
        public static ActionForecast ForSkill(GridController grid, Character caster, SkillDefinition skill, Vector2Int target)
        {
            var forecast = new ActionForecast { Target = target };
            if (grid == null || caster == null || skill == null) return forecast;

            var effective = EffectiveSkill.Resolve(caster, skill);
            var context = SkillTargeting.CreateContext(grid, caster, effective, target);
            forecast.Area.AddRange(context.AreaCells);

            if (skill.Damage > 0 && effective.Damage > 0)
            {
                foreach (var hit in context.Affected)
                    forecast.AddDamage(hit, CombatResolver.PredictDamage(caster, hit, effective.Damage, EDamageKind.Skill));
            }

            // The effects in their order: a dash moves the caster before a push starts from the new cell.
            foreach (var effect in effective.Effects)
            {
                switch (effect)
                {
                    case DashToTargetSkillEffect:
                        var destination = DashToTargetSkillEffect.GetDestination(grid, caster.CurrentGridPos, target);
                        if (destination != caster.CurrentGridPos)
                            forecast._finals[caster] = destination;
                        break;
                    case SwapSkillEffect:
                        forecast.AddSwap(grid, caster, target);
                        break;
                    case DisplaceSkillEffect displace:
                        forecast.AddDisplacement(grid, caster, context.Affected, displace.Mode,
                            effective.GetDisplacementDistance(displace), target);
                        break;
                    case ApplyStatesSkillEffect apply:
                        forecast.AddInterruptions(caster, context.Affected, apply);
                        break;
                }
            }

            return forecast;
        }

        /// <summary>Forecast of the item <paramref name="item"/> used by <paramref name="user"/> on <paramref name="target"/>.</summary>
        public static ActionForecast ForConsumable(GridController grid, Character user, ConsumableDefinition item,
            Vector2Int target)
        {
            var forecast = new ActionForecast { Target = target };
            if (grid == null || user == null || item == null) return forecast;

            var context = ConsumableRules.CreateContext(grid, user, item, target);
            forecast.Area.AddRange(context.AreaCells);

            // The effects in their order, as ConsumableDefinition.Execute applies them.
            foreach (var effect in item.Effects)
            {
                switch (effect)
                {
                    case DamageConsumableEffect damage when damage.Damage > 0:
                        foreach (var hit in context.Affected)
                            forecast.AddDamage(hit, CombatResolver.PredictDamage(user, hit, damage.Damage, EDamageKind.Consumable));
                        break;
                    case DisplaceConsumableEffect displace:
                        forecast.AddDisplacement(grid, user, context.Affected, displace.Mode, displace.Distance, target);
                        break;
                    case SwapConsumableEffect:
                        forecast.AddSwap(grid, user, target);
                        break;
                    case ApplyStatesConsumableEffect apply:
                        forecast.AddInterruptions(user, context.Affected, apply.States,
                            apply.Recipient == EConsumableRecipient.User);
                        break;
                }
            }

            return forecast;
        }

        private void AddDamage([CanBeNull] Character character, int amount)
        {
            if (character == null || amount <= 0) return;

            Damage.TryGetValue(character, out var total);
            total += amount;
            Damage[character] = total;
            if (total >= character.Current + character.States.TotalShield)
                Killed.Add(character);
        }

        private void AddInterruptions(Character caster, IReadOnlyList<Character> affected, ApplyStatesSkillEffect apply) =>
            AddInterruptions(caster, affected, apply.States, apply.Recipient == ESkillEffectRecipient.Caster);

        private void AddInterruptions(Character caster, IReadOnlyList<Character> affected,
            IReadOnlyList<StateGrant> states, bool onCaster)
        {
            StateDefinition interrupting = null;
            foreach (var grant in states)
            {
                if (grant.IsValid && Restricts(grant.State, EBehaviorRestriction.PreventSkills))
                {
                    interrupting = grant.State;
                    break;
                }
            }

            if (interrupting == null) return;

            if (onCaster)
            {
                Interrupt(caster, interrupting);
                return;
            }

            foreach (var character in affected)
                Interrupt(character, interrupting);
        }

        private void Interrupt([CanBeNull] Character character, StateDefinition state)
        {
            if (character == null || character.IsDead || Killed.Contains(character)) return;
            if (character.Config != null && character.Config.IsImmuneTo(state)) return;

            Interrupted.Add(character);
        }

        private static bool Restricts(StateDefinition state, EBehaviorRestriction restriction)
        {
            foreach (var effect in state.Effects)
            {
                if (effect != null && (effect.Restrictions & restriction) != 0)
                    return true;
            }

            return false;
        }

        private void AddSwap(GridController grid, Character caster, Vector2Int target)
        {
            if (!grid.IsValidPosition(target)) return;

            var from = GetPosition(caster);
            var other = grid.GetContent(target) as Character;
            if (other == null || other == caster || other.IsDead) return;

            if (Killed.Contains(other))
            {
                // The target dies from the damage: the caster takes its cell.
                _finals[caster] = target;
                return;
            }

            if (!other.CanBeDisplaced) return;

            _finals[caster] = target;
            _finals[other] = from;
        }

        private void AddDisplacement(GridController grid, Character caster, IReadOnlyList<Character> affected,
            EDisplacementMode mode, int distance, Vector2Int target)
        {
            // Targets that die from the hit are not moved (see DisplaceSkillEffect).
            var survivors = new List<Character>();
            foreach (var character in affected)
            {
                if (character != null && !character.IsDead && !Killed.Contains(character))
                    survivors.Add(character);
            }

            foreach (var move in DisplacementResolver.Predict(grid, caster, survivors, mode, distance, target))
            {
                Moves.Add(move);
                if (move.Result.CellsMoved > 0)
                    _finals[move.Result.Target] = move.Result.Final;
                AddDamage(move.Result.Target, move.CollisionDamage + move.TerrainDamage);
                AddDamage(move.HitCharacter, move.HitCharacterDamage);
            }
        }
    }
}
