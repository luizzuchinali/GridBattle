using System;
using UnityEngine;

namespace GridBattle.Gameplay.Stats
{
    public enum EModifierType
    {
        /// <summary>Added to the base value.</summary>
        Flat,

        /// <summary>Fraction added to the multiplier: value = (base + flat) × (1 + Σ percent).</summary>
        Percent
    }

    /// <summary>A change to one attribute, granted by a state.</summary>
    [Serializable]
    public struct AttributeModifier
    {
        [SerializeField]
        private EAttribute attribute;

        [SerializeField]
        private EModifierType type;

        [SerializeField]
        [Tooltip("Flat: amount added (e.g. +10 defense, +0.05 crit chance). Percent: fraction (0.1 = +10%).")]
        private float value;

        public AttributeModifier(EAttribute attribute, EModifierType type, float value)
        {
            this.attribute = attribute;
            this.type = type;
            this.value = value;
        }

        public EAttribute Attribute => attribute;
        public EModifierType Type => type;
        public float Value => value;

        public AttributeModifier Scaled(float factor) => new(attribute, type, value * factor);
    }
}
