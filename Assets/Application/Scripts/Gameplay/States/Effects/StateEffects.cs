using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Stats;
using UnityEngine;

namespace GridBattle.Gameplay.States.Effects
{
    public enum EPeriodicTiming
    {
        HolderTurnStart,
        HolderTurnEnd
    }

    /// <summary>Changes attributes while the state is active (e.g. Weakened: Damage Dealt -0.5).</summary>
    [Serializable]
    public sealed class AttributeModifierEffect : StateEffect
    {
        [SerializeField]
        private List<AttributeModifier> modifiers = new();

        public override void CollectModifiers(StateInstance state, List<AttributeModifier> into)
        {
            foreach (var modifier in modifiers)
                into.Add(modifier.Scaled(state.Stacks));
        }
    }

    /// <summary>Damage on each of the holder's turns (e.g. Poisoned).</summary>
    [Serializable]
    public sealed class PeriodicDamageEffect : StateEffect
    {
        [SerializeField]
        [Min(0)]
        private int flatDamage = 1;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the holder's max HP added to the damage.")]
        private float maxHpFraction;

        [SerializeField]
        private EPeriodicTiming timing = EPeriodicTiming.HolderTurnStart;

        public override void OnTurnStarted(Character holder, StateInstance state)
        {
            if (timing == EPeriodicTiming.HolderTurnStart) Tick(holder, state);
        }

        public override void OnTurnEnded(Character holder, StateInstance state)
        {
            if (timing == EPeriodicTiming.HolderTurnEnd) Tick(holder, state);
        }

        /// <summary>Base damage one tick deals to <paramref name="holder"/> at <paramref name="stacks"/> (before defense and modifiers).</summary>
        public int GetDamagePerTurn(Character holder, int stacks)
        {
            // The max-HP part can be reduced per character (CharacterConfig.MaxHpPercentDamageMultiplier).
            var multiplier = holder.Config != null ? holder.Config.MaxHpPercentDamageMultiplier : 1f;
            var percent = Mathf.RoundToInt(maxHpFraction * multiplier * holder.MaxHp);
            return (flatDamage + percent) * Mathf.Max(1, stacks);
        }

        private void Tick(Character holder, StateInstance state)
        {
            var amount = GetDamagePerTurn(holder, state.Stacks);
            if (amount > 0)
                CombatResolver.DealDamage(null, holder, amount, EDamageKind.Periodic);
        }
    }

    /// <summary>Healing on each of the holder's turns (e.g. Regeneration).</summary>
    [Serializable]
    public sealed class PeriodicHealEffect : StateEffect
    {
        [SerializeField]
        [Min(0)]
        private int flatHeal = 1;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the holder's max HP added to the healing.")]
        private float maxHpFraction;

        [SerializeField]
        private EPeriodicTiming timing = EPeriodicTiming.HolderTurnStart;

        public override void OnTurnStarted(Character holder, StateInstance state)
        {
            if (timing == EPeriodicTiming.HolderTurnStart) Tick(holder, state);
        }

        public override void OnTurnEnded(Character holder, StateInstance state)
        {
            if (timing == EPeriodicTiming.HolderTurnEnd) Tick(holder, state);
        }

        private void Tick(Character holder, StateInstance state)
        {
            var amount = (flatHeal + Mathf.RoundToInt(maxHpFraction * holder.MaxHp)) * state.Stacks;
            if (amount > 0)
                CombatResolver.Heal(holder, amount);
        }
    }

    /// <summary>When the holder is hit by an attack or skill, damage is returned to the attacker (e.g. Thorn Armor).</summary>
    [Serializable]
    public sealed class ThornsEffect : StateEffect
    {
        [SerializeField]
        [Min(0)]
        private int flatDamage = 1;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Fraction of the damage received that is returned.")]
        private float receivedFraction;

        public override void OnDamageReceived(Character holder, StateInstance state, in HitResult hit)
        {
            if (!hit.IsDirect || hit.Attacker == null || hit.Attacker.IsDead) return;

            var amount = (flatDamage + Mathf.RoundToInt(receivedFraction * hit.Damage)) * state.Stacks;
            if (amount > 0)
                CombatResolver.DealDamage(holder, hit.Attacker, amount, EDamageKind.Thorns);
        }
    }

    /// <summary>Heals the holder for part of the damage it deals with attacks and skills (life steal).</summary>
    [Serializable]
    public sealed class LifeStealEffect : StateEffect
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float fraction = 0.1f;

        public override void OnDamageDealt(Character holder, StateInstance state, in HitResult hit)
        {
            if (!hit.IsDirect || holder.IsDead) return;

            var amount = Mathf.RoundToInt(hit.Damage * fraction * state.Stacks);
            if (amount > 0)
                CombatResolver.Heal(holder, amount);
        }
    }

    /// <summary>Temporary HP that absorbs damage before real HP (Shield).</summary>
    [Serializable]
    public sealed class ShieldEffect : StateEffect
    {
        [SerializeField]
        [Min(0)]
        private int flatAmount = 10;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the holder's max HP added to the shield.")]
        private float maxHpFraction;

        [SerializeField]
        [Tooltip("Removes the state as soon as the shield is depleted.")]
        private bool removeWhenDepleted = true;

        public override void OnApplied(Character holder, StateInstance state)
        {
            state.Shield = Amount(holder, state);
        }

        public override void OnReapplied(Character holder, StateInstance state)
        {
            state.Shield = Mathf.Max(state.Shield, Amount(holder, state));
        }

        public override int AbsorbDamage(Character holder, StateInstance state, int damage)
        {
            var absorbed = Mathf.Min(state.Shield, damage);
            state.Shield -= absorbed;
            if (state.Shield <= 0 && removeWhenDepleted)
                holder.States.MarkForRemoval(state);
            return damage - absorbed;
        }

        private int Amount(Character holder, StateInstance state) =>
            (flatAmount + Mathf.RoundToInt(maxHpFraction * holder.MaxHp)) * state.Stacks;
    }

    /// <summary>Forbids actions while active (e.g. rooted: cannot move).</summary>
    [Serializable]
    public sealed class BehaviorRestrictionEffect : StateEffect
    {
        [SerializeField]
        private EBehaviorRestriction restrictions = EBehaviorRestriction.PreventMovement;

        public override EBehaviorRestriction Restrictions => restrictions;
    }

    /// <summary>Changes run settings (talent options, rerolls, bans, skips). Only meaningful on the player.</summary>
    [Serializable]
    public sealed class RunModifierEffect : StateEffect
    {
        [SerializeField]
        private ERunModifier modifier;

        [SerializeField]
        private int amount = 1;

        public override int GetRunModifier(ERunModifier requested, StateInstance state) =>
            requested == modifier ? amount * state.Stacks : 0;
    }
}
