using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Gameplay.Stats
{
    /// <summary>
    /// Effective attributes of a character: base values (config × spawn scaling)
    /// combined with the modifiers provided by its active states.
    /// value = (base + Σ flat) × (1 + Σ percent).
    /// </summary>
    public sealed class CharacterStats
    {
        private readonly float[] _base = new float[AttributeInfo.Count];
        private readonly List<AttributeModifier> _buffer = new();
        private readonly Action<List<AttributeModifier>> _collectModifiers;

        /// <param name="collectModifiers">Fills the list with the current modifiers (from the states).</param>
        public CharacterStats(Action<List<AttributeModifier>> collectModifiers)
        {
            _collectModifiers = collectModifiers;
        }

        public float GetBase(EAttribute attribute) => _base[(int)attribute];

        public void SetBase(EAttribute attribute, float value) => _base[(int)attribute] = value;

        public float Get(EAttribute attribute)
        {
            var flat = 0f;
            var percent = 0f;

            _buffer.Clear();
            _collectModifiers?.Invoke(_buffer);
            foreach (var modifier in _buffer)
            {
                if (modifier.Attribute != attribute) continue;

                if (modifier.Type == EModifierType.Flat)
                    flat += modifier.Value;
                else
                    percent += modifier.Value;
            }

            var value = (_base[(int)attribute] + flat) * (1f + percent);
            return AttributeInfo.IsNonNegative(attribute) ? Mathf.Max(0f, value) : value;
        }

        /// <summary>Value rounded to the nearest whole number (for integer attributes).</summary>
        public int GetInt(EAttribute attribute) => Mathf.RoundToInt(Get(attribute));
    }
}
