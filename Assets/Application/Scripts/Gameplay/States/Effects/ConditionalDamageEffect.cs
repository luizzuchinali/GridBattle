using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Rules;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.States.Effects
{
    /// <summary>Which side of a hit a <see cref="ConditionalDamageEffect"/> changes.</summary>
    public enum EDamageDirection
    {
        /// <summary>The damage the holder deals (the opponent is the target).</summary>
        Dealt,

        /// <summary>The damage the holder takes (the opponent is the attacker).</summary>
        Taken
    }

    /// <summary>
    /// When a <see cref="ConditionalDamageEffect"/> applies. "Opponent" is the target of the hit for damage the holder
    /// deals and the attacker for damage the holder takes; conditions about the opponent are false when there is none
    /// (damage over time, terrain).
    /// </summary>
    public enum EDamageCondition
    {
        /// <summary>Always.</summary>
        Always,

        /// <summary>Once for every enemy of the holder adjacent to it, up to the cap (a brawler's bonus).</summary>
        PerAdjacentEnemy,

        /// <summary>No enemy of the holder is adjacent to it.</summary>
        NoAdjacentEnemy,

        /// <summary>The opponent carries a harmful state (a debuff of any kind).</summary>
        OpponentHasNegativeState,

        /// <summary>The opponent carries at least one of the listed states (e.g. Poisoned).</summary>
        OpponentHasState,

        /// <summary>The holder walked or teleported during its previous turn ("hit and run").</summary>
        HolderMovedLastTurn,

        /// <summary>The holder did not walk during its previous turn (standing still).</summary>
        HolderDidNotMoveLastTurn,

        /// <summary>The opponent has no ally adjacent to it.</summary>
        OpponentIsolated,

        /// <summary>The opponent is at full HP (an opening strike).</summary>
        OpponentAtFullHp
    }

    /// <summary>
    /// A passive that changes damage depending on the situation: +15% damage per adjacent enemy, 20% less damage
    /// taken while surrounded, +30% damage against poisoned targets, +40% right after moving... The condition, the
    /// kinds of damage it reacts to and the amount are Inspector fields; the amount is per stack (a talent rank).
    /// Basic attacks and skills by default: periodic damage, thorns, terrain and exact damage are only changed when
    /// the mask says so. The hook is pure, so the AI and the simulation bot see the same numbers
    /// (<see cref="CombatResolver.PredictDamage"/>). Damage dealt: the base damage gets <c>+ flat</c>, then the
    /// attacker's multipliers and <c>x (1 + percent)</c>; damage taken: <c>x (1 + percent)</c> next to the
    /// target's Damage Taken (negative percent = reduction, flat is ignored).
    /// </summary>
    [Serializable]
    public sealed class ConditionalDamageEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("Damage the holder deals (bonus) or damage the holder takes (reduction).")]
        private EDamageDirection direction = EDamageDirection.Dealt;

        [SerializeField]
        [Tooltip("Kinds of damage the effect reacts to. Attacks = basic attack and skills. Periodic, thorns and terrain damage are normally left out.")]
        private EDamageKindMask appliesTo = EDamageKindMask.Attacks;

        [SerializeField]
        private EDamageCondition condition = EDamageCondition.Always;

        [SerializeField]
        [Tooltip("Opponent Has State only: the states that count (any of them).")]
        private List<StateDefinition> states = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Per Adjacent Enemy only: the most adjacent enemies that count.")]
        private int maxCount = 3;

        [SerializeField]
        [Tooltip("Fraction added to the damage per count and stack (0.15 = +15%; for Taken, -0.1 = 10% less).")]
        private float percent = 0.15f;

        [SerializeField]
        [Tooltip("Damage dealt only: flat damage added to the base damage per count and stack.")]
        private float flat;

        public EDamageDirection Direction => direction;
        public EDamageKindMask AppliesToKinds => appliesTo;
        public EDamageCondition Condition => condition;
        public IReadOnlyList<StateDefinition> States => states;
        public int MaxCount => Mathf.Max(1, maxCount);
        public float Percent => percent;
        public float Flat => flat;

        public override void ModifyOutgoingDamage(Character holder, StateInstance state, in DamageContext context,
            ref DamageAdjustment adjustment)
        {
            if (direction != EDamageDirection.Dealt || !appliesTo.Contains(context.Kind)) return;

            var amount = Evaluate(holder, context.Target) * Mathf.Max(1, state.Stacks);
            if (amount > 0)
                adjustment.AddOutgoing(percent * amount, flat * amount);
        }

        public override void ModifyIncomingDamage(Character holder, StateInstance state, in DamageContext context,
            ref DamageAdjustment adjustment)
        {
            if (direction != EDamageDirection.Taken || !appliesTo.Contains(context.Kind)) return;

            var amount = Evaluate(holder, context.Attacker) * Mathf.Max(1, state.Stacks);
            if (amount > 0)
                adjustment.AddIncoming(percent * amount);
        }

        /// <summary>
        /// How many times the condition holds for the <paramref name="holder"/> against the
        /// <paramref name="opponent"/> (0 = not met; 1 for the yes-or-no conditions; the capped number of adjacent
        /// enemies for <see cref="EDamageCondition.PerAdjacentEnemy"/>).
        /// </summary>
        public int Evaluate(Character holder, [CanBeNull] Character opponent)
        {
            if (holder == null) return 0;

            switch (condition)
            {
                case EDamageCondition.Always:
                    return 1;
                case EDamageCondition.PerAdjacentEnemy:
                    return Mathf.Min(MaxCount, GridRules.CountAdjacentOpponents(holder.Grid, holder));
                case EDamageCondition.NoAdjacentEnemy:
                    return GridRules.CountAdjacentOpponents(holder.Grid, holder) == 0 ? 1 : 0;
                case EDamageCondition.OpponentHasNegativeState:
                    return opponent != null && HasNegativeState(opponent) ? 1 : 0;
                case EDamageCondition.OpponentHasState:
                    return opponent != null && HasAnyState(opponent) ? 1 : 0;
                case EDamageCondition.HolderMovedLastTurn:
                    return holder.MovedLastTurn ? 1 : 0;
                case EDamageCondition.HolderDidNotMoveLastTurn:
                    return holder.MovedLastTurn ? 0 : 1;
                case EDamageCondition.OpponentIsolated:
                    return opponent != null && !opponent.IsDead && !GridRules.HasAdjacentAlly(holder.Grid, opponent) ? 1 : 0;
                case EDamageCondition.OpponentAtFullHp:
                    return opponent != null && opponent.Current >= opponent.MaxHp ? 1 : 0;
                default:
                    return 0;
            }
        }

        private static bool HasNegativeState(Character character)
        {
            foreach (var state in character.States.All)
            {
                if (!state.Removed && state.Definition.Kind == EStateKind.Debuff)
                    return true;
            }

            return false;
        }

        private bool HasAnyState(Character character)
        {
            foreach (var wanted in states)
            {
                if (wanted != null && character.States.Has(wanted))
                    return true;
            }

            return false;
        }
    }
}
