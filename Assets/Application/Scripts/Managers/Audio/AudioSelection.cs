using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// Clip selection shared by sound effects and music: a random pick among the
    /// assigned clips that avoids repeating the previous one when there is a choice.
    /// Presentation only, so it may use <see cref="Random"/>.
    /// </summary>
    public static class AudioSelection
    {
        /// <summary>Picks a random clip index using Unity's random generator; -1 if there is no clip.</summary>
        public static int PickIndex(IReadOnlyList<AudioClip> clips, int lastIndex) =>
            PickIndex(clips, lastIndex, Random.value);

        /// <summary>
        /// Picks a clip index. Empty (null) slots are ignored and, when more than one
        /// clip is available, <paramref name="lastIndex"/> is excluded.
        /// </summary>
        /// <param name="roll">Random value in [0, 1) choosing among the eligible clips.</param>
        /// <returns>The chosen index in <paramref name="clips"/>, or -1 if no clip is assigned.</returns>
        public static int PickIndex(IReadOnlyList<AudioClip> clips, int lastIndex, float roll)
        {
            if (clips == null) return -1;

            var assigned = 0;
            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null) assigned++;
            }

            if (assigned == 0) return -1;

            var excludeLast = assigned > 1 && lastIndex >= 0 && lastIndex < clips.Count && clips[lastIndex] != null;
            var eligible = excludeLast ? assigned - 1 : assigned;
            var target = Mathf.Clamp((int)(Mathf.Clamp01(roll) * eligible), 0, eligible - 1);

            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null || (excludeLast && i == lastIndex)) continue;
                if (target-- == 0) return i;
            }

            return -1;
        }
    }
}
