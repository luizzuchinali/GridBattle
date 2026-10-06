using System;
using GridBattle.Gameplay.Entities;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Combat
{
    /// <summary>
    /// A set of <see cref="EDamageKind"/>s (one bit per kind, in declaration order), used by state effects to say which
    /// hits they react to. <see cref="EDamageKind.Pure"/> has no bit on purpose: exact damage is never modified.
    /// </summary>
    [Flags]
    public enum EDamageKindMask
    {
        None = 0,
        BasicAttack = 1 << 0,
        Skill = 1 << 1,
        Consumable = 1 << 2,
        Periodic = 1 << 3,
        Thorns = 1 << 4,
        Terrain = 1 << 5,
        Collision = 1 << 7,

        /// <summary>Hits the holder aims on purpose: basic attacks and skills (the default of the conditional effects).</summary>
        Attacks = BasicAttack | Skill,

        /// <summary>Attacks, skills, consumables and collisions: everything a character causes by acting.</summary>
        AnyAction = BasicAttack | Skill | Consumable | Collision,
    }

    /// <summary>Helpers over <see cref="EDamageKind"/> and <see cref="EDamageKindMask"/>.</summary>
    public static class DamageKinds
    {
        /// <summary>The mask bit of a kind (0 for <see cref="EDamageKind.Pure"/>).</summary>
        public static EDamageKindMask ToMask(EDamageKind kind) =>
            kind == EDamageKind.Pure ? EDamageKindMask.None : (EDamageKindMask)(1 << (int)kind);

        /// <summary>Whether <paramref name="mask"/> includes <paramref name="kind"/>.</summary>
        public static bool Contains(this EDamageKindMask mask, EDamageKind kind) => (mask & ToMask(kind)) != 0;
    }

    /// <summary>
    /// What a state effect needs to know about one hit before its damage is computed: who hits whom and with what
    /// kind of damage. The effects answer through <see cref="DamageAdjustment"/>; they must be pure (no random
    /// draws, no changes to the game), because the same hook also runs for predictions (AI, simulation bot).
    /// </summary>
    public readonly struct DamageContext
    {
        /// <summary>Who deals the damage (null for damage over time, terrain...).</summary>
        [CanBeNull]
        public Character Attacker { get; }

        public Character Target { get; }
        public EDamageKind Kind { get; }

        public DamageContext([CanBeNull] Character attacker, Character target, EDamageKind kind)
        {
            Attacker = attacker;
            Target = target;
            Kind = kind;
        }
    }

    /// <summary>
    /// The sum of what the states of both sides add to one hit, applied by <see cref="DamageCalculator"/>:
    /// <c>base + OutgoingFlat</c> is multiplied by <c>1 + OutgoingPercent</c> (right after the attacker's Damage
    /// Dealt) and the damage that comes out of the defense by <c>1 + IncomingPercent</c> (next to the target's
    /// Damage Taken). All zero = the plain formula.
    /// </summary>
    public struct DamageAdjustment
    {
        /// <summary>Added to the base damage of the hit before the multipliers.</summary>
        public float OutgoingFlat;

        /// <summary>Fraction added to the damage the attacker deals (0.25 = +25%).</summary>
        public float OutgoingPercent;

        /// <summary>Fraction added to the damage the target takes (-0.2 = 20% less).</summary>
        public float IncomingPercent;

        /// <summary>Whether nothing changes (the plain formula applies).</summary>
        public readonly bool IsNeutral => OutgoingFlat == 0f && OutgoingPercent == 0f && IncomingPercent == 0f;

        public void AddOutgoing(float percent, float flat = 0f)
        {
            OutgoingPercent += percent;
            OutgoingFlat += flat;
        }

        public void AddIncoming(float percent)
        {
            IncomingPercent += percent;
        }
    }
}
