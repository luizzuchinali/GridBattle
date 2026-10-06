using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.States.Effects
{
    /// <summary>
    /// A reward for killing: when a hit of the holder (of the chosen kinds) kills its target, the holder heals, its
    /// running skill cooldowns get shorter and/or it gains states. Amounts are per stack (a talent rank). The
    /// cooldown reduction is applied at the end of the holder's turn, so the skill used for the kill is included.
    /// </summary>
    [Serializable]
    public sealed class OnKillEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("Kinds of damage whose killing blow counts.")]
        private EDamageKindMask appliesTo = EDamageKindMask.Attacks;

        [SerializeField]
        [Min(0)]
        [Tooltip("Turns taken off every running skill cooldown (per stack), at the end of the turn.")]
        private int cooldownReduction;

        [SerializeField]
        [Min(0)]
        [Tooltip("HP the holder recovers (per stack).")]
        private int healFlat;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the holder's max HP it recovers (per stack).")]
        private float healMaxHpFraction;

        [SerializeField]
        [Tooltip("States the holder gains (applied with the grant's duration and stacks).")]
        private List<StateGrant> grantStates = new();

        public EDamageKindMask AppliesToKinds => appliesTo;
        public int CooldownReduction => cooldownReduction;
        public int HealFlat => healFlat;
        public float HealMaxHpFraction => healMaxHpFraction;
        public IReadOnlyList<StateGrant> GrantStates => grantStates;

        public override void OnDamageDealt(Character holder, StateInstance state, in HitResult hit)
        {
            if (!hit.Killed || holder.IsDead || !appliesTo.Contains(hit.Kind)) return;

            var stacks = Mathf.Max(1, state.Stacks);
            if (cooldownReduction > 0)
                holder.Cooldowns.QueueReduction(cooldownReduction * stacks);

            var heal = (healFlat + CombatResolver.Settings.RoundValue(healMaxHpFraction * holder.MaxHp)) * stacks;
            if (heal > 0)
                CombatResolver.Heal(holder, heal);

            foreach (var grant in grantStates)
            {
                if (grant.IsValid)
                    holder.States.Apply(grant);
            }
        }
    }

    /// <summary>
    /// Applies states to the target of the holder's hits (of the chosen kinds): "your basic attacks poison". The
    /// stacks of each grant are multiplied by the stacks of this state (a talent rank). Nothing is applied to a
    /// target that died from the hit.
    /// </summary>
    [Serializable]
    public sealed class ApplyStateOnHitEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("Kinds of damage that apply the states.")]
        private EDamageKindMask appliesTo = EDamageKindMask.BasicAttack;

        [SerializeField]
        [Tooltip("States applied to the target (the grant's duration; its stacks are multiplied by this state's stacks).")]
        private List<StateGrant> states = new();

        public EDamageKindMask AppliesToKinds => appliesTo;
        public IReadOnlyList<StateGrant> States => states;

        public override void OnDamageDealt(Character holder, StateInstance state, in HitResult hit)
        {
            if (hit.Target == null || hit.Target.IsDead || !appliesTo.Contains(hit.Kind)) return;

            var rank = Mathf.Max(1, state.Stacks);
            foreach (var grant in states)
            {
                if (grant.IsValid)
                    hit.Target.States.Apply(grant.State, grant.Duration, grant.Stacks * rank);
            }
        }
    }
}
