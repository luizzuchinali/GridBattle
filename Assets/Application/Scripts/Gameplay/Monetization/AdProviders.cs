using System;
using System.Collections;
using UnityEngine;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>The two ad formats of the game (GDD 7). There is no banner.</summary>
    public enum EAdKind
    {
        /// <summary>Full screen ad shown at a natural pause of the run (forced).</summary>
        Interstitial,

        /// <summary>Ad shown before each use of a talent tool (reroll, ban, skip); the use happens when it is watched to the end.</summary>
        Rewarded
    }

    /// <summary>Where the ads come from: the LevelPlay SDK, the game's own test ads or nothing.</summary>
    internal interface IAdsProvider
    {
        /// <summary>The game draws the ad itself (test ads): the UI shows a screen while it plays.</summary>
        bool IsSimulated { get; }

        /// <summary>Starts the SDK and loads the first ads. Called once.</summary>
        void Initialize();

        /// <summary>An ad of <paramref name="kind"/> is loaded and can be shown now.</summary>
        bool IsReady(EAdKind kind);

        /// <summary>
        /// Shows an ad of <paramref name="kind"/> (check <see cref="IsReady"/> first). <paramref name="onFinished"/>
        /// is called once, on the main thread, with whether the ad was shown and whether a rewarded ad was watched
        /// to the end.
        /// </summary>
        void Show(EAdKind kind, Action<bool, bool> onFinished);

        /// <summary>Frees the SDK objects.</summary>
        void Dispose();
    }

    /// <summary>No ads (PC builds, an Editor set to Off, a mobile platform turned off in the settings).</summary>
    internal sealed class NoAdsProvider : IAdsProvider
    {
        public bool IsSimulated => false;

        public void Initialize()
        {
        }

        public bool IsReady(EAdKind kind) => false;

        public void Show(EAdKind kind, Action<bool, bool> onFinished) => onFinished?.Invoke(false, false);

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Test ads of the game itself, for the Editor: always ready, they last
    /// <see cref="MonetizationSettings.SimulatedAdSeconds"/> real seconds (the UI draws a test screen meanwhile)
    /// and a rewarded one gives its reward unless it is closed early with <see cref="Finish"/>.
    /// </summary>
    internal sealed class SimulatedAdsProvider : IAdsProvider
    {
        private readonly MonoBehaviour _host;
        private readonly Func<float> _seconds;
        private Coroutine _running;
        private Action<bool, bool> _onFinished;

        public SimulatedAdsProvider(MonoBehaviour host, Func<float> seconds)
        {
            _host = host;
            _seconds = seconds;
        }

        public bool IsSimulated => true;

        public void Initialize()
        {
        }

        public bool IsReady(EAdKind kind) => _onFinished == null;

        public void Show(EAdKind kind, Action<bool, bool> onFinished)
        {
            if (_onFinished != null || _host == null)
            {
                onFinished?.Invoke(false, false);
                return;
            }

            _onFinished = onFinished;
            _running = _host.StartCoroutine(Play(kind));
        }

        /// <summary>Ends the test ad now: watched to the end (a rewarded one rewards) or closed early.</summary>
        public void Finish(bool watchedToEnd, EAdKind kind)
        {
            if (_onFinished == null) return;

            if (_running != null && _host != null)
                _host.StopCoroutine(_running);
            _running = null;

            var callback = _onFinished;
            _onFinished = null;
            callback(true, watchedToEnd && kind == EAdKind.Rewarded);
        }

        public void Dispose()
        {
            if (_running != null && _host != null)
                _host.StopCoroutine(_running);
            _running = null;
            _onFinished = null;
        }

        private IEnumerator Play(EAdKind kind)
        {
            // Real time: an ad does not care about the game's time scale.
            yield return new WaitForSecondsRealtime(_seconds());
            _running = null;
            Finish(true, kind);
        }
    }
}
