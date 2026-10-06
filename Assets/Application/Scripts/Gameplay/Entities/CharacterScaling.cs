using System;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Multipliers applied to a character's base attributes on spawn (enemy
    /// strength by map depth). Not a state: it is not shown or removable.
    /// </summary>
    [Serializable]
    public struct CharacterScaling
    {
        public static readonly CharacterScaling None = new(1f, 1f);

        [SerializeField]
        private float hpMultiplier;

        [SerializeField]
        private float damageMultiplier;

        public CharacterScaling(float hpMultiplier, float damageMultiplier)
        {
            this.hpMultiplier = hpMultiplier;
            this.damageMultiplier = damageMultiplier;
        }

        public float HpMultiplier => hpMultiplier <= 0f ? 1f : hpMultiplier;
        public float DamageMultiplier => damageMultiplier <= 0f ? 1f : damageMultiplier;
    }
}
