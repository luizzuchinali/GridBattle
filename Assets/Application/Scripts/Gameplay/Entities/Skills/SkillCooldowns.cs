using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Stats;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Cooldowns of one character's skills, counted in that character's turns
    /// (for the player, one per consumed action: GDD "decrements on every player
    /// action"). Using a skill starts its cooldown
    /// (<see cref="SkillDefinition.Cooldown"/> minus the owner's Cooldown
    /// Reduction attribute and the cooldown modifiers of its talents, see <see cref="EffectiveSkill"/>, never below
    /// <see cref="SkillSettings.MinimumCooldown"/> when the skill has a cooldown). <see cref="Tick"/> runs at the end of every
    /// turn of the owner and skips the skills used during that very turn, so a
    /// cooldown of 1 means exactly one action in between. Plain data: no runtime
    /// state lives in the <see cref="SkillDefinition"/> assets. Raises
    /// <see cref="SkillCooldownsChangedEvent"/> whenever something changes.
    /// </summary>
    public sealed class SkillCooldowns
    {
        private readonly Character _owner;
        private readonly Dictionary<SkillDefinition, int> _remaining = new();
        private readonly HashSet<SkillDefinition> _triggeredThisTurn = new();
        private readonly List<SkillDefinition> _buffer = new();
        private int _pendingReduction;

        public SkillCooldowns(Character owner)
        {
            _owner = owner;
        }

        /// <summary>Whether the skill can be used (no cooldown running).</summary>
        public bool IsReady(SkillDefinition skill) => GetRemaining(skill) <= 0;

        /// <summary>Owner turns left until the skill is ready again (0 = ready).</summary>
        public int GetRemaining(SkillDefinition skill)
        {
            return skill != null && _remaining.TryGetValue(skill, out var remaining) ? remaining : 0;
        }

        /// <summary>Starts the cooldown after a successful use of the skill.</summary>
        public void Trigger(SkillDefinition skill)
        {
            if (skill == null || skill.Cooldown <= 0) return;

            // The skill's cooldown with the owner's talent modifiers, then its Cooldown Reduction attribute.
            var reduced = EffectiveSkill.Resolve(_owner, skill).Cooldown - _owner.Stats.GetInt(EAttribute.CooldownReduction);
            var value = Mathf.Max(SkillSettings.Current.MinimumCooldown, reduced);
            if (value <= 0)
            {
                // Reduction (with a minimum of 0) removed the cooldown completely.
                if (_remaining.Remove(skill))
                    NotifyChanged();
                return;
            }

            _remaining[skill] = value;
            _triggeredThisTurn.Add(skill);
            NotifyChanged();
        }

        /// <summary>
        /// Shortens every running cooldown by <paramref name="turns"/> at the end of the owner's current turn
        /// (an on-kill effect: the skill just used is included, which a reduction applied at once would miss because
        /// its cooldown only starts after the use).
        /// </summary>
        public void QueueReduction(int turns)
        {
            if (turns > 0)
                _pendingReduction += turns;
        }

        /// <summary>
        /// End of one of the owner's turns: the queued reduction is applied, then every running cooldown goes
        /// down by 1, except the skills triggered during this turn.
        /// </summary>
        public void Tick()
        {
            var changed = ApplyPendingReduction();
            _buffer.Clear();
            _buffer.AddRange(_remaining.Keys);

            foreach (var skill in _buffer)
            {
                if (_triggeredThisTurn.Contains(skill)) continue;

                var value = _remaining[skill] - 1;
                if (value <= 0)
                    _remaining.Remove(skill);
                else
                    _remaining[skill] = value;
                changed = true;
            }

            _triggeredThisTurn.Clear();
            if (changed)
                NotifyChanged();
        }

        private bool ApplyPendingReduction()
        {
            if (_pendingReduction <= 0) return false;

            var reduction = _pendingReduction;
            _pendingReduction = 0;
            if (_remaining.Count == 0) return false;

            _buffer.Clear();
            _buffer.AddRange(_remaining.Keys);
            foreach (var skill in _buffer)
            {
                var value = _remaining[skill] - reduction;
                if (value <= 0)
                {
                    _remaining.Remove(skill);
                    _triggeredThisTurn.Remove(skill);
                }
                else
                {
                    _remaining[skill] = value;
                }
            }

            return true;
        }

        /// <summary>Makes every skill ready (new battle).</summary>
        public void Reset()
        {
            _pendingReduction = 0;
            if (_remaining.Count == 0 && _triggeredThisTurn.Count == 0) return;

            _remaining.Clear();
            _triggeredThisTurn.Clear();
            NotifyChanged();
        }

        /// <summary>Running cooldowns for saving (key = <see cref="GameDefinition.Id"/> of the skill).</summary>
        public List<CounterState> Capture()
        {
            var result = new List<CounterState>();
            foreach (var pair in _remaining)
            {
                if (pair.Value > 0 && !string.IsNullOrEmpty(pair.Key.Id))
                    result.Add(new CounterState { Key = pair.Key.Id, Value = pair.Value });
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return result;
        }

        /// <summary>Replaces the running cooldowns with saved ones. Unknown skill ids are ignored.</summary>
        public void Restore(IEnumerable<CounterState> saved)
        {
            _remaining.Clear();
            _triggeredThisTurn.Clear();
            _pendingReduction = 0;

            if (saved != null)
            {
                var database = GameDatabase.Instance;
                foreach (var counter in saved)
                {
                    if (counter == null || counter.Value <= 0 || database == null) continue;

                    var skill = database.Get<SkillDefinition>(counter.Key);
                    if (skill != null)
                        _remaining[skill] = counter.Value;
                }
            }

            NotifyChanged();
        }

        private void NotifyChanged() => EventBus.Raise(new SkillCooldownsChangedEvent(_owner));
    }
}
