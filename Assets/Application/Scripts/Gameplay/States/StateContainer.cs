using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.States
{
    /// <summary>
    /// The states active on one character. Applies the stack policy, runs the
    /// effects' hooks and counts durations in turns of the holder: the duration
    /// goes down at the end of each of the holder's turns, so a state applied
    /// before the holder acted in the current global turn counts that turn as the
    /// first one, and a state applied after counts the next one (GDD 3.1).
    /// </summary>
    public sealed class StateContainer
    {
        private readonly Character _owner;
        private readonly List<StateInstance> _states = new();

        public StateContainer(Character owner)
        {
            _owner = owner;
        }

        public IReadOnlyList<StateInstance> All => _states;

        /// <summary>Raised whenever a state is added, removed or changes duration/stacks.</summary>
        public event Action Changed;

        [CanBeNull]
        public StateInstance Find(StateDefinition definition, string sourceId = null)
        {
            foreach (var state in _states)
            {
                if (state.Definition == definition && state.SourceId == sourceId && !state.Removed)
                    return state;
            }

            return null;
        }

        public bool Has(StateDefinition definition)
        {
            foreach (var state in _states)
            {
                if (state.Definition == definition && !state.Removed)
                    return true;
            }

            return false;
        }

        public StateInstance Apply(in StateGrant grant, string sourceId = null) =>
            Apply(grant.State, grant.Duration, grant.Stacks, sourceId);

        /// <summary>
        /// Applies a state for <paramref name="duration"/> turns of the holder
        /// (<see cref="StateInstance.Permanent"/> for permanent). Instances with
        /// different <paramref name="sourceId"/> are kept apart (e.g. one per
        /// talent); with the same source, the state's stack policy decides.
        /// </summary>
        [CanBeNull]
        public StateInstance Apply(StateDefinition definition, int duration, int stacks = 1, string sourceId = null)
        {
            if (definition == null || _owner.IsDead) return null;
            if (_owner.Config != null && _owner.Config.IsImmuneTo(definition)) return null;

            stacks = Mathf.Max(1, stacks);
            var existing = Find(definition, sourceId);
            if (existing == null)
            {
                var state = new StateInstance(definition, duration, Mathf.Min(stacks, definition.MaxStacks), sourceId);
                _states.Add(state);
                foreach (var effect in definition.Effects)
                    effect?.OnApplied(_owner, state);

                EventBus.Raise(new StateAppliedEvent(_owner, state, false));
                NotifyChanged();
                return state;
            }

            switch (definition.StackPolicy)
            {
                case EStackPolicy.IgnoreIfPresent:
                    return existing;
                case EStackPolicy.RefreshDuration:
                    existing.Remaining = LongestDuration(existing.Remaining, duration);
                    break;
                case EStackPolicy.AddDuration:
                    existing.Remaining = existing.IsPermanent || duration < 0
                        ? StateInstance.Permanent
                        : existing.Remaining + duration;
                    break;
                case EStackPolicy.AddStacks:
                    existing.Stacks = Mathf.Min(existing.Stacks + stacks, definition.MaxStacks);
                    existing.Remaining = LongestDuration(existing.Remaining, duration);
                    break;
            }

            foreach (var effect in definition.Effects)
                effect?.OnReapplied(_owner, existing);

            EventBus.Raise(new StateAppliedEvent(_owner, existing, true));
            NotifyChanged();
            return existing;
        }

        public bool Remove(StateDefinition definition, string sourceId = null)
        {
            var state = Find(definition, sourceId);
            if (state == null) return false;

            RemoveInstance(state);
            return true;
        }

        /// <summary>Removes every state granted by <paramref name="sourceId"/> (e.g. a talent).</summary>
        public void RemoveBySource(string sourceId)
        {
            foreach (var state in Snapshot())
            {
                if (state.SourceId == sourceId)
                    RemoveInstance(state);
            }
        }

        /// <summary>Removes every state that is not permanent.</summary>
        public void RemoveTemporary()
        {
            foreach (var state in Snapshot())
            {
                if (!state.IsPermanent)
                    RemoveInstance(state);
            }
        }

        public void Clear()
        {
            foreach (var state in Snapshot())
                RemoveInstance(state);
        }

        /// <summary>Called by an effect that ends its own state (e.g. a depleted shield).</summary>
        public void MarkForRemoval(StateInstance state)
        {
            state.Removed = true;
        }

        public void OnTurnStarted()
        {
            foreach (var state in Snapshot())
            {
                if (state.Removed || _owner.IsDead) continue;
                foreach (var effect in state.Definition.Effects)
                    effect?.OnTurnStarted(_owner, state);
            }

            FlushRemovals();
        }

        /// <summary>End of the holder's turn: effects run, then durations go down.</summary>
        public void OnTurnEnded()
        {
            foreach (var state in Snapshot())
            {
                if (state.Removed || _owner.IsDead) continue;
                foreach (var effect in state.Definition.Effects)
                    effect?.OnTurnEnded(_owner, state);
            }

            var changed = false;
            foreach (var state in Snapshot())
            {
                if (state.Removed || state.IsPermanent) continue;

                state.Remaining--;
                changed = true;
                if (state.Remaining <= 0)
                    state.Removed = true;
            }

            FlushRemovals();
            if (changed)
                NotifyChanged();
        }

        public void CollectModifiers(List<AttributeModifier> into)
        {
            foreach (var state in _states)
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                    effect?.CollectModifiers(state, into);
            }
        }

        /// <summary>Lets the states add their conditional bonuses to a hit the holder is about to deal.</summary>
        public void CollectOutgoingDamage(in DamageContext context, ref DamageAdjustment adjustment)
        {
            foreach (var state in _states)
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                {
                    if (effect != null)
                        effect.ModifyOutgoingDamage(_owner, state, context, ref adjustment);
                }
            }
        }

        /// <summary>Lets the states add their conditional reductions or increases to a hit the holder is about to take.</summary>
        public void CollectIncomingDamage(in DamageContext context, ref DamageAdjustment adjustment)
        {
            foreach (var state in _states)
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                {
                    if (effect != null)
                        effect.ModifyIncomingDamage(_owner, state, context, ref adjustment);
                }
            }
        }

        /// <summary>Lets the states add what they change about <paramref name="skill"/> (see <see cref="EffectiveSkill"/>).</summary>
        public void CollectSkillModifiers(SkillDefinition skill, ref SkillModifiers modifiers)
        {
            foreach (var state in _states)
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                {
                    if (effect != null)
                        effect.CollectSkillModifiers(_owner, state, skill, ref modifiers);
                }
            }
        }

        public EBehaviorRestriction Restrictions
        {
            get
            {
                var result = EBehaviorRestriction.None;
                foreach (var state in _states)
                {
                    if (state.Removed) continue;
                    foreach (var effect in state.Definition.Effects)
                    {
                        if (effect != null)
                            result |= effect.Restrictions;
                    }
                }

                return result;
            }
        }

        public bool IsRestricted(EBehaviorRestriction restriction) => (Restrictions & restriction) != 0;

        public int GetRunModifier(ERunModifier modifier)
        {
            var total = 0;
            foreach (var state in _states)
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                {
                    if (effect != null)
                        total += effect.GetRunModifier(modifier, state);
                }
            }

            return total;
        }

        /// <summary>Total shield points of the active states.</summary>
        public int TotalShield
        {
            get
            {
                var total = 0;
                foreach (var state in _states)
                {
                    if (!state.Removed)
                        total += state.Shield;
                }

                return total;
            }
        }

        /// <summary>Lets shield effects absorb damage. Returns the damage that reaches HP.</summary>
        public int Absorb(int damage)
        {
            foreach (var state in Snapshot())
            {
                if (damage <= 0) break;
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                {
                    if (effect != null && damage > 0)
                        damage = effect.AbsorbDamage(_owner, state, damage);
                }
            }

            FlushRemovals();
            return damage;
        }

        public void NotifyDamageDealt(in HitResult hit)
        {
            foreach (var state in Snapshot())
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                    effect?.OnDamageDealt(_owner, state, hit);
            }

            FlushRemovals();
        }

        public void NotifyDamageReceived(in HitResult hit)
        {
            foreach (var state in Snapshot())
            {
                if (state.Removed) continue;
                foreach (var effect in state.Definition.Effects)
                    effect?.OnDamageReceived(_owner, state, hit);
            }

            FlushRemovals();
        }

        /// <summary>Restores a saved state without running OnApplied (the saved shield is kept).</summary>
        public StateInstance Restore(StateDefinition definition, int remaining, int stacks, string sourceId, int shield)
        {
            var state = new StateInstance(definition, remaining, stacks, sourceId) { Shield = shield };
            _states.Add(state);
            NotifyChanged();
            return state;
        }

        /// <summary>Copy for iteration: effects may add or remove states while running.</summary>
        private List<StateInstance> Snapshot() => new(_states);

        private void RemoveInstance(StateInstance state)
        {
            state.Removed = true;
            FlushRemovals();
        }

        private void FlushRemovals()
        {
            var removedAny = false;
            for (var i = _states.Count - 1; i >= 0; i--)
            {
                var state = _states[i];
                if (!state.Removed) continue;

                _states.RemoveAt(i);
                removedAny = true;
                foreach (var effect in state.Definition.Effects)
                    effect?.OnRemoved(_owner, state);
                EventBus.Raise(new StateRemovedEvent(_owner, state));
            }

            if (removedAny)
                NotifyChanged();
        }

        private void NotifyChanged() => Changed?.Invoke();

        private static int LongestDuration(int current, int incoming)
        {
            if (current < 0 || incoming < 0) return StateInstance.Permanent;
            return Mathf.Max(current, incoming);
        }
    }
}
