using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GridBattle.Core.Randomness
{
    /// <summary>
    /// Small deterministic PRNG (SplitMix64) with a serializable state. Unlike
    /// UnityEngine.Random / System.Random, its sequence is fixed across Unity
    /// versions and platforms, so the same seed always replays the same run.
    /// Use it for every gameplay draw; UnityEngine.Random is for visuals only.
    /// </summary>
    [Serializable]
    public sealed class Rng
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        [JsonProperty]
        public ulong State { get; private set; }

        [JsonConstructor]
        public Rng(ulong state)
        {
            State = state;
        }

        public ulong NextULong()
        {
            State += Golden;
            return Mix(State);
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1 << 24));

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Uniform int in [minInclusive, maxExclusive). Returns min if the range is empty.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            var span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % span));
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();

        /// <summary>True with probability <paramref name="probability"/> (0..1).</summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
            return items[Range(0, items.Count)];
        }

        /// <summary>
        /// Index drawn proportionally to <paramref name="weights"/> (non-positive
        /// weights are never drawn). Returns -1 if every weight is non-positive.
        /// </summary>
        public int WeightedIndex(IReadOnlyList<float> weights)
        {
            var total = 0.0;
            foreach (var weight in weights)
            {
                if (weight > 0f)
                    total += weight;
            }

            if (total <= 0.0) return -1;

            var roll = NextDouble() * total;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                roll -= weights[i];
                if (roll < 0.0) return i;
            }

            for (var i = weights.Count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f) return i;
            }

            return -1;
        }

        /// <summary>Fisher–Yates shuffle in place.</summary>
        public void Shuffle<T>(IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = Range(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>Generator for a sub-sequence identified by <paramref name="keys"/>, without advancing this one.</summary>
        public static Rng FromKeys(ulong seed, params long[] keys)
        {
            var state = Mix(seed ^ Golden);
            foreach (var key in keys)
                state = Mix(state ^ Mix((ulong)key + Golden));
            return new Rng(state);
        }

        private static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
