using System;
using System.Collections.Generic;
using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// All the audio content and tuning of the game (GDD 5.2): one cue per
    /// <see cref="ESfx"/> and one track per <see cref="EMusicContext"/>. Designers
    /// only drop clips into the entries; the <see cref="AudioManager"/> plays them.
    /// Every entry exists from the start (the asset fills in missing ones) and an
    /// entry without clips is simply silent.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "GridBattle/Settings/Audio Library", order = 0)]
    public sealed class AudioLibrary : ScriptableObject, IGameSettings
    {
        private static readonly int SfxSlots = SlotCount(typeof(ESfx));
        private static readonly int MusicSlots = SlotCount(typeof(EMusicContext));

        [Header("Mix")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Designer mix level of every sound effect. The player's SFX volume (Options) multiplies it.")]
        private float sfxLevel = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Designer mix level of the music (usually below the effects). The player's music volume (Options) multiplies it.")]
        private float musicLevel = 0.7f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Size of the pool of sound effect voices. When all are busy the oldest one is cut.")]
        private int sfxVoices = 12;

        [Header("Music")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds of crossfade when the music context changes (0 = instant cut).")]
        private float crossfadeSeconds = 1.5f;

        [SerializeField]
        [Tooltip("The music keeps playing while the talent choice pauses the game (GDD 5.2 open question). Off = the music pauses with it.")]
        private bool keepMusicDuringTalentPause = true;

        [SerializeField]
        [Tooltip("Music context that starts when a battle is won (the run system may override it).")]
        private EMusicContext musicAfterVictory = EMusicContext.Map;

        [SerializeField]
        [Tooltip("Music context that starts when a battle is lost (the run system may override it).")]
        private EMusicContext musicAfterDefeat = EMusicContext.Menu;

        [Header("Automatic effects")]
        [SerializeField]
        [Tooltip("Play the hurt effect on a hit that kills, too. Off = only the death effect plays.")]
        private bool hurtSoundOnKill;

        [SerializeField]
        [Tooltip("Play the state applied/expired effects for permanent states too (talent states are applied at the start of every battle).")]
        private bool stateSoundForPermanentStates;

        [Header("Cues")]
        [SerializeField]
        [Tooltip("One entry per sound effect.")]
        private List<SfxCue> sfxCues = new();

        [SerializeField]
        [Tooltip("One entry per music context.")]
        private List<MusicTrack> musicTracks = new();

        [NonSerialized]
        private SfxCue[] _sfxLookup;

        [NonSerialized]
        private MusicTrack[] _musicLookup;

        public float SfxLevel => sfxLevel;
        public float MusicLevel => musicLevel;
        public int SfxVoices => Mathf.Max(1, sfxVoices);
        public float CrossfadeSeconds => Mathf.Max(0f, crossfadeSeconds);
        public bool KeepMusicDuringTalentPause => keepMusicDuringTalentPause;
        public EMusicContext MusicAfterVictory => musicAfterVictory;
        public EMusicContext MusicAfterDefeat => musicAfterDefeat;
        public bool HurtSoundOnKill => hurtSoundOnKill;
        public bool StateSoundForPermanentStates => stateSoundForPermanentStates;
        public IReadOnlyList<SfxCue> SfxCues => sfxCues;
        public IReadOnlyList<MusicTrack> MusicTracks => musicTracks;

        /// <summary>The cue of <paramref name="sfx"/>, or null if the library has no entry for it.</summary>
        public SfxCue GetCue(ESfx sfx)
        {
            if (_sfxLookup == null)
                _sfxLookup = BuildLookup(sfxCues, SfxSlots, cue => (int)cue.Sfx);

            var index = (int)sfx;
            return index >= 0 && index < _sfxLookup.Length ? _sfxLookup[index] : null;
        }

        /// <summary>The track of <paramref name="context"/>, or null if the library has no entry for it.</summary>
        public MusicTrack GetTrack(EMusicContext context)
        {
            if (_musicLookup == null)
                _musicLookup = BuildLookup(musicTracks, MusicSlots, track => (int)track.Context);

            var index = (int)context;
            return index >= 0 && index < _musicLookup.Length ? _musicLookup[index] : null;
        }

        /// <summary>
        /// The context whose track actually plays for <paramref name="context"/>:
        /// follows the fallbacks of tracks without clips (Map to Menu, Boss to Battle
        /// by default). <see cref="EMusicContext.None"/> if nothing is audible.
        /// </summary>
        public EMusicContext ResolveMusicContext(EMusicContext context)
        {
            // The chain is bounded so a fallback cycle set in the Inspector cannot loop forever.
            for (var step = 0; step <= MusicSlots && context != EMusicContext.None; step++)
            {
                var track = GetTrack(context);
                if (track == null) return EMusicContext.None;
                if (track.HasClips) return context;
                context = track.FallbackContext;
            }

            return EMusicContext.None;
        }

        /// <summary>Adds the entries that are missing (new enum values, a new asset). Returns true if anything changed.</summary>
        public bool EnsureEntries()
        {
            var changed = false;

            foreach (ESfx sfx in Enum.GetValues(typeof(ESfx)))
            {
                if (sfxCues.Exists(cue => cue != null && cue.Sfx == sfx)) continue;
                sfxCues.Add(new SfxCue(sfx));
                changed = true;
            }

            foreach (EMusicContext context in Enum.GetValues(typeof(EMusicContext)))
            {
                if (context == EMusicContext.None) continue;
                if (musicTracks.Exists(track => track != null && track.Context == context)) continue;
                musicTracks.Add(new MusicTrack(context));
                changed = true;
            }

            if (changed)
            {
                sfxCues.Sort((a, b) => a.Sfx.CompareTo(b.Sfx));
                musicTracks.Sort((a, b) => a.Context.CompareTo(b.Context));
                InvalidateLookups();
            }

            return changed;
        }

        private void OnEnable()
        {
            InvalidateLookups();
        }

        private void InvalidateLookups()
        {
            _sfxLookup = null;
            _musicLookup = null;
        }

        private static T[] BuildLookup<T>(List<T> entries, int slots, Func<T, int> keyOf) where T : class
        {
            var lookup = new T[slots];
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var key = keyOf(entry);
                if (key >= 0 && key < slots && lookup[key] == null)
                    lookup[key] = entry;
            }

            return lookup;
        }

        private static int SlotCount(Type enumType)
        {
            var max = 0;
            foreach (var value in Enum.GetValues(enumType))
                max = Mathf.Max(max, Convert.ToInt32(value));
            return max + 1;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            EnsureEntries();
        }

        private void OnValidate()
        {
            InvalidateLookups();
            if (EnsureEntries())
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
