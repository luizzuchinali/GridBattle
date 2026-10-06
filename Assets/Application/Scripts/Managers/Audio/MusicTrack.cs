using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// The music of one <see cref="EMusicContext"/>: one or more looping clips and
    /// what to play when it has none.
    /// </summary>
    [Serializable]
    public sealed class MusicTrack
    {
        [SerializeField]
        private EMusicContext context;

        [SerializeField]
        [Tooltip("Looping music clips. Each time the context starts one is picked at random, never the one that played last when there are several (e.g. several battle tracks). Empty = use the fallback context.")]
        private List<AudioClip> clips = new();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume of this track, relative to the library music level and the player's volume option.")]
        private float volume = 1f;

        [SerializeField]
        [Tooltip("Context whose track plays when this one has no clips (the map shares the menu calm track, the boss falls back to the battle track). None = silence.")]
        private EMusicContext fallbackContext;

        public MusicTrack()
        {
        }

        /// <summary>A track for <paramref name="context"/> with its default fallback and no clips yet.</summary>
        public MusicTrack(EMusicContext context)
        {
            this.context = context;
            fallbackContext = context switch
            {
                EMusicContext.Map => EMusicContext.Menu,
                EMusicContext.Boss => EMusicContext.Battle,
                _ => EMusicContext.None
            };
        }

        public EMusicContext Context => context;
        public IReadOnlyList<AudioClip> Clips => clips;
        public float Volume => volume;
        public EMusicContext FallbackContext => fallbackContext;

        /// <summary>At least one clip is assigned.</summary>
        public bool HasClips
        {
            get
            {
                for (var i = 0; i < clips.Count; i++)
                {
                    if (clips[i] != null) return true;
                }

                return false;
            }
        }
    }
}
