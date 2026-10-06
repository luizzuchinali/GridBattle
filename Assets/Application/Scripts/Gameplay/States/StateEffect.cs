using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Stats;

namespace GridBattle.Gameplay.States
{
    /// <summary>Actions a state can forbid (GDD 3.1: states may change behaviors).</summary>
    [Flags]
    public enum EBehaviorRestriction
    {
        None = 0,
        PreventMovement = 1 << 0,
        PreventBasicAttack = 1 << 1,
        PreventSkills = 1 << 2,
    }

    /// <summary>
    /// Run configuration values a state can change (GDD Mechanic 3: the number of
    /// talent options, rerolls, bans and skips are run settings that active
    /// states may alter). Only meaningful on the player.
    /// </summary>
    public enum ERunModifier
    {
        TalentOptions,
        Rerolls,
        Bans,
        Skips
    }

    /// <summary>
    /// One effect of a state. Effects are plain serializable classes picked in
    /// the Inspector ([SerializeReference]); every hook is optional. Effects are
    /// shared by every instance of the state, so per-instance data lives in
    /// <see cref="StateInstance"/>. Magnitudes scale with the instance's stacks.
    /// </summary>
    [Serializable]
    public abstract class StateEffect
    {
        public virtual void CollectModifiers(StateInstance state, List<AttributeModifier> into)
        {
        }

        public virtual void OnApplied(Character holder, StateInstance state)
        {
        }

        /// <summary>The state was applied again while active (after the stack policy ran).</summary>
        public virtual void OnReapplied(Character holder, StateInstance state)
        {
        }

        public virtual void OnRemoved(Character holder, StateInstance state)
        {
        }

        /// <summary>Start of the holder's own turn.</summary>
        public virtual void OnTurnStarted(Character holder, StateInstance state)
        {
        }

        /// <summary>End of the holder's own turn, before the duration goes down.</summary>
        public virtual void OnTurnEnded(Character holder, StateInstance state)
        {
        }

        /// <summary>The holder hit someone.</summary>
        public virtual void OnDamageDealt(Character holder, StateInstance state, in HitResult hit)
        {
        }

        /// <summary>The holder was hit.</summary>
        public virtual void OnDamageReceived(Character holder, StateInstance state, in HitResult hit)
        {
        }

        /// <summary>
        /// The holder is about to deal a hit (before its damage is computed): add the conditional bonuses of this
        /// effect to <paramref name="adjustment"/> (<see cref="DamageAdjustment.AddOutgoing"/>). Must be pure (no random
        /// draws, no changes to the game): the same hook runs for predictions by the AI and the simulation bot.
        /// </summary>
        public virtual void ModifyOutgoingDamage(Character holder, StateInstance state, in DamageContext context,
            ref DamageAdjustment adjustment)
        {
        }

        /// <summary>
        /// The holder is about to take a hit (before its damage is computed): add the conditional reductions or
        /// increases of this effect (<see cref="DamageAdjustment.AddIncoming"/>). Pure, like
        /// <see cref="ModifyOutgoingDamage"/>.
        /// </summary>
        public virtual void ModifyIncomingDamage(Character holder, StateInstance state, in DamageContext context,
            ref DamageAdjustment adjustment)
        {
        }

        /// <summary>
        /// Collects what this effect changes about <paramref name="skill"/> when the holder uses it (damage, area,
        /// range, cooldown, displacement, state durations, attached effects). One call per state instance; the
        /// effect scales its values by <see cref="StateInstance.Stacks"/>. Read through
        /// <see cref="EffectiveSkill"/>, never directly.
        /// </summary>
        public virtual void CollectSkillModifiers(Character holder, StateInstance state, SkillDefinition skill,
            ref SkillModifiers modifiers)
        {
        }

        /// <summary>Absorbs part of incoming damage; returns what is left to reach HP.</summary>
        public virtual int AbsorbDamage(Character holder, StateInstance state, int damage) => damage;

        public virtual EBehaviorRestriction Restrictions => EBehaviorRestriction.None;

        public virtual int GetRunModifier(ERunModifier modifier, StateInstance state) => 0;
    }
}
