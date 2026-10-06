using System.Collections.Generic;
using GridBattle.Core.Randomness;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>
    /// What the consumable node offers (mapa_e_nos / consumiveis 2.8): draws the
    /// options from the pool by <see cref="ConsumableDefinition.DropWeight"/>. Pure
    /// and deterministic for a given <see cref="Rng"/> state: the run manager passes
    /// the Consumables stream (<c>GameRandom.Stream(ERandomStream.Consumables)</c>), so
    /// the same seed always offers the same items.
    /// </summary>
    public static class ConsumableGrant
    {
        /// <summary>
        /// Draws <see cref="ConsumableSettings.ChoiceCount"/> distinct options from the settings'
        /// pool. A count of 1 is the "random grant"; more is a choice the player makes. Fewer
        /// options come back when the pool has fewer candidates.
        /// </summary>
        public static List<ConsumableDefinition> Roll(Rng rng, ConsumableSettings settings) =>
            Roll(rng, settings.Pool, settings.ChoiceCount);

        /// <summary>
        /// Weighted draw without repetition of up to <paramref name="count"/> options. Null entries,
        /// duplicates and definitions with a non-positive drop weight are never drawn.
        /// </summary>
        public static List<ConsumableDefinition> Roll(Rng rng, [CanBeNull] IReadOnlyList<ConsumableDefinition> pool,
            int count)
        {
            var result = new List<ConsumableDefinition>();
            if (pool == null || count <= 0) return result;

            var candidates = new List<ConsumableDefinition>();
            var weights = new List<float>();
            foreach (var definition in pool)
            {
                if (definition == null || definition.DropWeight <= 0f || candidates.Contains(definition)) continue;

                candidates.Add(definition);
                weights.Add(definition.DropWeight);
            }

            while (result.Count < count && candidates.Count > 0)
            {
                var index = rng.WeightedIndex(weights);
                if (index < 0) break;

                result.Add(candidates[index]);
                candidates.RemoveAt(index);
                weights.RemoveAt(index);
            }

            return result;
        }
    }
}
