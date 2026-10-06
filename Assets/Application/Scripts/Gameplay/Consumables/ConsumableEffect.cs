using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>Who receives the result of an <see cref="ApplyStatesConsumableEffect"/>.</summary>
    public enum EConsumableRecipient
    {
        /// <summary>The character that used the item.</summary>
        User,

        /// <summary>Every character the item affects (area and target filter).</summary>
        AffectedTargets
    }

    /// <summary>
    /// One result of a consumable (healing, damage, states). Effects are plain
    /// serializable classes picked in the Inspector ([SerializeReference]) and
    /// shared by every use of the item, so they must not hold runtime state.
    /// </summary>
    [Serializable]
    public abstract class ConsumableEffect
    {
        /// <summary>
        /// Whether applying the effect would change anything right now (e.g. a damage
        /// effect needs a character in the area). An item can only be used when at least
        /// one of its effects is useful, so a wasted item never consumes anything.
        /// Default: always.
        /// </summary>
        public virtual bool IsUseful(in ConsumableContext context) => true;

        /// <summary>Applies the effect.</summary>
        public abstract void Apply(in ConsumableContext context);
    }

    /// <summary>Heals the user: a flat amount and/or a fraction of max HP.</summary>
    [Serializable]
    public sealed class HealConsumableEffect : ConsumableEffect
    {
        [SerializeField]
        [Min(0)]
        [Tooltip("HP restored, before the max-HP fraction.")]
        private int flatHeal = 30;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the user's max HP added to the healing.")]
        private float maxHpFraction;

        [SerializeField]
        [Tooltip("The effect is not useful (the item cannot be used) while the user is at full HP, " +
                 "so the item is not wasted by accident.")]
        private bool refuseAtFullHp = true;

        public int FlatHeal => flatHeal;
        public float MaxHpFraction => maxHpFraction;

        /// <summary>HP the effect would restore to <paramref name="user"/> (before the cap at max HP).</summary>
        public int GetAmount(Character user)
        {
            return flatHeal + CombatResolver.Settings.RoundValue(maxHpFraction * user.MaxHp);
        }

        public override bool IsUseful(in ConsumableContext context)
        {
            var user = context.User;
            if (user == null || user.IsDead) return false;

            return !refuseAtFullHp || user.Current < user.MaxHp;
        }

        public override void Apply(in ConsumableContext context)
        {
            var user = context.User;
            if (user == null || user.IsDead) return;

            CombatResolver.Heal(user, GetAmount(user));
        }
    }

    /// <summary>
    /// Damages the affected characters through <see cref="CombatResolver"/> (crit,
    /// defense and damage modifiers apply, as for any <see cref="EDamageKind.Consumable"/> hit).
    /// </summary>
    [Serializable]
    public sealed class DamageConsumableEffect : ConsumableEffect
    {
        [SerializeField]
        [Min(0)]
        [Tooltip("Base damage dealt to every affected character, before the target's defense.")]
        private int damage = 15;

        public int Damage => damage;

        public override bool IsUseful(in ConsumableContext context) => damage > 0 && context.Affected.Count > 0;

        public override void Apply(in ConsumableContext context)
        {
            if (damage <= 0) return;

            foreach (var target in context.Affected)
                CombatResolver.DealDamage(context.User, target, damage, EDamageKind.Consumable);
        }
    }

    /// <summary>Applies states (buffs/debuffs) to the user or to the affected characters.</summary>
    [Serializable]
    public sealed class ApplyStatesConsumableEffect : ConsumableEffect
    {
        [SerializeField]
        private EConsumableRecipient recipient = EConsumableRecipient.User;

        [SerializeField]
        private List<StateGrant> states = new();

        public EConsumableRecipient Recipient => recipient;
        public IReadOnlyList<StateGrant> States => states;

        public override bool IsUseful(in ConsumableContext context)
        {
            return recipient == EConsumableRecipient.User || context.Affected.Count > 0;
        }

        public override void Apply(in ConsumableContext context)
        {
            if (recipient == EConsumableRecipient.User)
            {
                ApplyTo(context.User);
                return;
            }

            foreach (var character in context.Affected)
                ApplyTo(character);
        }

        private void ApplyTo(Character character)
        {
            if (character == null || character.IsDead) return;

            foreach (var grant in states)
            {
                if (grant.IsValid)
                    character.States.Apply(grant);
            }
        }
    }
}
