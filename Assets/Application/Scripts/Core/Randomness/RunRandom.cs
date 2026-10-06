using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GridBattle.Core.Randomness
{
    /// <summary>
    /// Independent random streams of a run. Each system draws from its own
    /// stream so that, for example, an extra critical roll never changes the map
    /// or the talent offers.
    /// </summary>
    public enum ERandomStream
    {
        Map,
        Battle,
        Terrain,
        Talents,
        Combat,
        Consumables,
        AI,
        Misc
    }

    /// <summary>
    /// The random state of a run: the seed plus the current position of each
    /// stream. Serialized with the run so reloading never changes the outcome of
    /// an action (GDD 8.1).
    /// </summary>
    [Serializable]
    public sealed class RunRandom
    {
        [JsonProperty]
        public ulong Seed { get; private set; }

        [JsonProperty]
        private Dictionary<ERandomStream, Rng> _streams = new();

        [JsonConstructor]
        private RunRandom()
        {
        }

        public RunRandom(ulong seed)
        {
            Seed = seed;
        }

        /// <summary>The persistent stream (its position is saved with the run).</summary>
        public Rng Get(ERandomStream stream)
        {
            if (!_streams.TryGetValue(stream, out var rng))
            {
                rng = Rng.FromKeys(Seed, (long)stream);
                _streams[stream] = rng;
            }

            return rng;
        }

        /// <summary>
        /// A generator that depends only on the seed, the stream and
        /// <paramref name="keys"/> (e.g. node id, level, reroll index). Does not
        /// advance any persistent stream, so the result is the same however many
        /// times it is requested.
        /// </summary>
        public Rng Derive(ERandomStream stream, params long[] keys)
        {
            var allKeys = new long[keys.Length + 1];
            allKeys[0] = 1000 + (long)stream;
            Array.Copy(keys, 0, allKeys, 1, keys.Length);
            return Rng.FromKeys(Seed, allKeys);
        }

        /// <summary>A new seed from the clock (the seed is never shown to the player).</summary>
        public static ulong CreateSeed() => (ulong)DateTime.UtcNow.Ticks ^ (ulong)Environment.TickCount * 0x9E3779B97F4A7C15UL;
    }
}
