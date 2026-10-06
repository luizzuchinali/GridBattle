using System;
using GridBattle.Data;
using GridBattle.Gameplay.Simulation;
using UnityEngine;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// Plays the game's sound effects and music, configured by the
    /// <see cref="AudioLibrary"/>. It bootstraps itself (no scene setup): a
    /// DontDestroyOnLoad object with a pool of effect voices and two music sources
    /// for crossfades. Gameplay events are mapped to effects by
    /// <see cref="AudioEventHooks"/>; other modules (UI, map, talents, skills,
    /// terrain, consumables) call <see cref="Play(ESfx)"/> directly. Everything is
    /// safe to call before the manager exists or when the library has no clips.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const string ObjectName = "AudioManager";

        private static AudioManager _instance;
        private static float _sfxVolume = 1f;
        private static float _musicVolume = 1f;
        private static bool _manualMusicPause;
        private static bool _talentPause;
        private static bool _contextRequested;
        private static EMusicContext _requestedContext;

        private AudioLibrary _library;
        private AudioEventHooks _hooks;

        // Sound effect voices (pool) and the state needed to throttle and steal them.
        private AudioSource[] _voices;
        private int[] _voiceSfx;
        private float[] _voiceBaseVolume;
        private float[] _voiceStartTime;
        private float[] _lastPlayTime;
        private int[] _lastSfxClip;

        // Music: two channels crossfade; _active is the one that should be audible.
        private MusicChannel[] _channels;
        private int _active;
        private EMusicContext _playingContext;
        private int[] _lastMusicClip;
        private bool _sourcesPaused;

        /// <summary>
        /// Raised for every effect requested through <see cref="Play(ESfx)"/>, even
        /// when nothing is audible (no clips, throttled). For tests and tools.
        /// </summary>
        public static event Action<ESfx> Played;

        /// <summary>Raised when the requested music context changes. For tests and tools.</summary>
        public static event Action<EMusicContext> MusicContextChanged;

        /// <summary>Player volume of the sound effects (0..1). Applied immediately; persistence belongs to the options module.</summary>
        public static float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                if (_instance != null)
                    _instance.ApplySfxVolume();
            }
        }

        /// <summary>Player volume of the music (0..1). Applied immediately; persistence belongs to the options module.</summary>
        public static float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_instance != null)
                    _instance.ApplyMusicVolumes();
            }
        }

        /// <summary>The music context last requested (None until something asks; the menu is requested at startup).</summary>
        public static EMusicContext MusicContext => _requestedContext;

        /// <summary>The music is currently paused (by <see cref="PauseMusic"/> or by the talent pause when the library says so).</summary>
        public static bool IsMusicPaused =>
            _manualMusicPause || (_talentPause && _instance != null && !_instance._library.KeepMusicDuringTalentPause);

        /// <summary>A track is audible (or fading in) on the music sources.</summary>
        public static bool IsMusicPlaying => _instance != null && _instance._playingContext != EMusicContext.None;

        /// <summary>The library in use, or null while the manager does not exist.</summary>
        public static AudioLibrary Library => _instance != null ? _instance._library : null;

        /// <summary>Voices of sound effects playing right now.</summary>
        public static int ActiveSfxVoices
        {
            get
            {
                if (_instance == null) return 0;

                var count = 0;
                foreach (var voice in _instance._voices)
                {
                    if (voice.isPlaying) count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Plays a sound effect. Does nothing (silently) when the cue has no clips,
        /// when it was played too recently or when it already has too many voices.
        /// </summary>
        public static void Play(ESfx sfx)
        {
            if (SimMode.IsActive) return;

            Played?.Invoke(sfx);
            if (_instance != null)
                _instance.PlayCue(sfx);
        }

        /// <summary>Plays a sound effect at a world position. The game is 2D, so the position is ignored.</summary>
        public static void Play(ESfx sfx, Vector3 worldPosition)
        {
            Play(sfx);
        }

        /// <summary>
        /// Switches the music to the track of <paramref name="context"/> with a
        /// crossfade. Requesting the context that is already playing does nothing
        /// (the track keeps going), so Menu and Map share a track without restarting.
        /// </summary>
        public static void SetMusicContext(EMusicContext context)
        {
            var changed = !_contextRequested || _requestedContext != context;
            _requestedContext = context;
            _contextRequested = true;

            if (_instance != null && !SimMode.IsActive)
                _instance.StartMusic(context);
            if (changed)
                MusicContextChanged?.Invoke(context);
        }

        /// <summary>Pauses or resumes the music (pause menu).</summary>
        public static void PauseMusic(bool paused)
        {
            _manualMusicPause = paused;
            if (_instance != null)
                _instance.ApplyMusicPause();
        }

        /// <summary>
        /// The talent choice opened or closed. The music pauses only when the
        /// library's Keep Music During Talent Pause is off (GDD 5.2 open question).
        /// </summary>
        public static void SetTalentPause(bool active)
        {
            _talentPause = active;
            if (_instance != null)
                _instance.ApplyMusicPause();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _sfxVolume = 1f;
            _musicVolume = 1f;
            _manualMusicPause = false;
            _talentPause = false;
            _contextRequested = false;
            _requestedContext = EMusicContext.None;
            Played = null;
            MusicContextChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var host = new GameObject(ObjectName);
            DontDestroyOnLoad(host);
            host.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _library = GameSettings.Get<AudioLibrary>();
            _library.EnsureEntries();

            CreateVoices();
            CreateMusicChannels();

            _hooks = new AudioEventHooks();
            _hooks.Subscribe();

            // The game starts on the menu unless something already asked for another context.
            if (_contextRequested)
            {
                StartMusic(_requestedContext);
            }
            else
            {
                SetMusicContext(EMusicContext.Menu);
            }

            ApplyMusicPause();
        }

        private void OnDestroy()
        {
            _hooks?.Unsubscribe();
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            var paused = IsMusicPaused;
            var crossfade = _library.CrossfadeSeconds;
            var step = crossfade > 0f ? Time.unscaledDeltaTime / crossfade : 1f;

            if (!paused)
            {
                foreach (var channel in _channels)
                {
                    channel.Level = Mathf.MoveTowards(channel.Level, channel.Target, step);
                    if (channel.Target <= 0f && channel.Level <= 0f && channel.Source.clip != null)
                        channel.Silence();
                }
            }

            ApplyMusicVolumes();
        }

        // --- Sound effects ---

        private void CreateVoices()
        {
            var count = _library.SfxVoices;
            _voices = new AudioSource[count];
            _voiceSfx = new int[count];
            _voiceBaseVolume = new float[count];
            _voiceStartTime = new float[count];
            for (var i = 0; i < count; i++)
            {
                _voices[i] = CreateSource();
                _voiceSfx[i] = -1;
            }

            var slots = 0;
            foreach (var value in Enum.GetValues(typeof(ESfx)))
                slots = Mathf.Max(slots, (int)value + 1);
            _lastPlayTime = new float[slots];
            _lastSfxClip = new int[slots];
            for (var i = 0; i < slots; i++)
            {
                _lastPlayTime[i] = float.NegativeInfinity;
                _lastSfxClip[i] = -1;
            }
        }

        private void PlayCue(ESfx sfx)
        {
            var cue = _library.GetCue(sfx);
            if (cue == null || !cue.HasClips) return;

            var key = (int)sfx;
            var now = Time.realtimeSinceStartup;
            if (now - _lastPlayTime[key] < cue.MinInterval) return;
            if (CountVoices(key) >= cue.MaxSimultaneous) return;

            var clipIndex = AudioSelection.PickIndex(cue.Clips, _lastSfxClip[key]);
            if (clipIndex < 0) return;

            var voice = AcquireVoice();
            var baseVolume = cue.Volume * _library.SfxLevel;
            _voices[voice].clip = cue.Clips[clipIndex];
            _voices[voice].pitch = cue.Pitch * (1f + UnityEngine.Random.Range(-cue.PitchVariance, cue.PitchVariance));
            _voices[voice].volume = baseVolume * _sfxVolume;
            _voices[voice].Play();

            _voiceSfx[voice] = key;
            _voiceBaseVolume[voice] = baseVolume;
            _voiceStartTime[voice] = now;
            _lastPlayTime[key] = now;
            _lastSfxClip[key] = clipIndex;
        }

        private int CountVoices(int key)
        {
            var count = 0;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voiceSfx[i] == key && _voices[i].isPlaying) count++;
            }

            return count;
        }

        /// <summary>A free voice, or the oldest one (cut) when every voice is busy.</summary>
        private int AcquireVoice()
        {
            var oldest = 0;
            for (var i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) return i;
                if (_voiceStartTime[i] < _voiceStartTime[oldest]) oldest = i;
            }

            _voices[oldest].Stop();
            return oldest;
        }

        private void ApplySfxVolume()
        {
            for (var i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].isPlaying)
                    _voices[i].volume = _voiceBaseVolume[i] * _sfxVolume;
            }
        }

        // --- Music ---

        private void CreateMusicChannels()
        {
            _channels = new[] { new MusicChannel(CreateSource(true)), new MusicChannel(CreateSource(true)) };
            _active = 0;
            _playingContext = EMusicContext.None;

            var slots = 0;
            foreach (var value in Enum.GetValues(typeof(EMusicContext)))
                slots = Mathf.Max(slots, (int)value + 1);
            _lastMusicClip = new int[slots];
            for (var i = 0; i < _lastMusicClip.Length; i++)
                _lastMusicClip[i] = -1;
        }

        private void StartMusic(EMusicContext requested)
        {
            var effective = _library.ResolveMusicContext(requested);
            if (effective == _playingContext) return;

            // Fade out whatever is playing.
            var outgoing = _channels[_active];
            outgoing.Target = 0f;
            _playingContext = EMusicContext.None;
            if (effective == EMusicContext.None) return;

            var track = _library.GetTrack(effective);
            var key = (int)effective;
            var clipIndex = AudioSelection.PickIndex(track.Clips, _lastMusicClip[key]);
            if (clipIndex < 0) return;

            _lastMusicClip[key] = clipIndex;
            _playingContext = effective;
            _active = 1 - _active;

            var incoming = _channels[_active];
            incoming.TrackVolume = track.Volume;
            incoming.Target = 1f;
            if (_library.CrossfadeSeconds <= 0f)
            {
                incoming.Level = 1f;
                outgoing.Level = 0f;
                outgoing.Silence();
            }

            incoming.Source.clip = track.Clips[clipIndex];
            incoming.Source.Play();
            if (IsMusicPaused)
                incoming.Source.Pause();
            ApplyMusicVolumes();
        }

        private void ApplyMusicVolumes()
        {
            var volume = _library.MusicLevel * _musicVolume;
            foreach (var channel in _channels)
                channel.Source.volume = channel.Level * channel.TrackVolume * volume;
        }

        private void ApplyMusicPause()
        {
            var paused = IsMusicPaused;
            if (paused == _sourcesPaused) return;

            _sourcesPaused = paused;
            foreach (var channel in _channels)
            {
                if (paused) channel.Source.Pause();
                else channel.Source.UnPause();
            }
        }

        private AudioSource CreateSource(bool loop = false)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private sealed class MusicChannel
        {
            public MusicChannel(AudioSource source)
            {
                Source = source;
            }

            public AudioSource Source { get; }

            /// <summary>Fade position, 0 (silent) to 1 (full).</summary>
            public float Level { get; set; }

            /// <summary>Where the fade is heading.</summary>
            public float Target { get; set; }

            /// <summary>Volume of the track being played (from the library).</summary>
            public float TrackVolume { get; set; } = 1f;

            /// <summary>Stops the source and releases its clip.</summary>
            public void Silence()
            {
                Source.Stop();
                Source.clip = null;
            }
        }
    }
}
