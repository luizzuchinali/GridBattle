using System;
using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Run
{
    /// <summary>
    /// Converts the states of a character to and from the saved form (<see cref="StateSnapshot"/>: state id,
    /// remaining turns, stacks, source and shield), so a battle can be rebuilt exactly and a few states can
    /// travel between battles.
    /// </summary>
    public static class StateSnapshots
    {
        /// <summary>
        /// Saved form of the character's states that pass <paramref name="include"/> (null = all), in the order
        /// they were applied.
        /// </summary>
        public static List<StateSnapshot> Capture(Character character, Func<StateInstance, bool> include = null)
        {
            var result = new List<StateSnapshot>();
            if (character == null) return result;

            foreach (var state in character.States.All)
            {
                if (state.Removed || state.Definition == null || string.IsNullOrEmpty(state.Definition.Id)) continue;
                if (include != null && !include(state)) continue;

                result.Add(new StateSnapshot
                {
                    StateId = state.Definition.Id,
                    Remaining = state.Remaining,
                    Stacks = state.Stacks,
                    SourceId = state.SourceId,
                    Shield = state.Shield,
                });
            }

            return result;
        }

        /// <summary>
        /// Adds the saved states to the character exactly as they were (no OnApplied effects, the saved shield is
        /// kept). Unknown ids are skipped with a warning. Returns how many were restored.
        /// </summary>
        public static int Restore(Character character, IEnumerable<StateSnapshot> saved)
        {
            if (character == null || saved == null) return 0;

            var database = GameDatabase.Instance;
            var restored = 0;
            foreach (var snapshot in saved)
            {
                if (snapshot == null) continue;

                var definition = database != null ? database.Get<StateDefinition>(snapshot.StateId) : null;
                if (definition == null)
                {
                    Debug.LogWarning($"State id '{snapshot.StateId}' not found in the GameDatabase; skipped.");
                    continue;
                }

                character.States.Restore(definition, snapshot.Remaining, Mathf.Max(1, snapshot.Stacks),
                    snapshot.SourceId, snapshot.Shield);
                restored++;
            }

            return restored;
        }
    }
}
