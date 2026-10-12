using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>
    /// Ads from Unity LevelPlay (Ads Mediation package): one interstitial (forced ads) and one rewarded ad unit,
    /// loaded as soon as the SDK starts and loaded again after every show; a failed load is retried with a growing
    /// delay. The privacy answers of <see cref="MonetizationSettings"/> are passed before the SDK starts. In the
    /// Editor the SDK plays mock ads (only with the Android or iOS build target).
    /// </summary>
    internal sealed class LevelPlayAdsProvider : IAdsProvider
    {
        /// <summary>Some networks report the reward right after the close: the close waits this long for it.</summary>
        private const float RewardGraceSeconds = 1f;

        private const int MaxRetryDoublings = 3;

        private readonly MonoBehaviour _host;
        private readonly MonetizationSettings _settings;
        private readonly string _appKey;
        private readonly string _interstitialUnitId;
        private readonly string _rewardedUnitId;

        private LevelPlayInterstitialAd _interstitial;
        private LevelPlayRewardedAd _rewarded;
        private bool _initStarted;
        private bool _adsCreated;
        private int _interstitialFailures;
        private int _rewardedFailures;
        private int _initFailures;

        private Action<bool, bool> _onFinished;
        private EAdKind _showing;
        private bool _rewardEarned;
        private Coroutine _rewardWait;

        public LevelPlayAdsProvider(MonoBehaviour host, MonetizationSettings settings)
        {
            _host = host;
            _settings = settings;
#if UNITY_IOS
            _appKey = settings.IosAppKey;
            _interstitialUnitId = settings.IosInterstitialAdUnitId;
            _rewardedUnitId = settings.IosRewardedAdUnitId;
#else
            _appKey = settings.AndroidAppKey;
            _interstitialUnitId = settings.AndroidInterstitialAdUnitId;
            _rewardedUnitId = settings.AndroidRewardedAdUnitId;
#endif
        }

        /// <summary>Fired when an ad finished loading (the UI may offer the rewarded ad now).</summary>
        public event Action ReadinessChanged;

        public bool IsSimulated => false;

        public void Initialize()
        {
            if (_initStarted) return;

            if (string.IsNullOrEmpty(_appKey))
            {
                Debug.LogWarning("LevelPlay: no App Key for this platform in MonetizationSettings; ads are off.");
                return;
            }

            _initStarted = true;
            ApplyPrivacy();
#if UNITY_IOS
            // The game (and its sound) pauses while an ad covers it.
            LevelPlay.SetPauseGame(true);
#endif
            if (Debug.isDebugBuild && _settings.AdaptersDebug)
                LevelPlay.SetAdaptersDebug(true);
            if (Debug.isDebugBuild && _settings.LaunchTestSuite)
                LevelPlay.SetMetaData("is_test_suite", "enable");

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(_appKey);

#if UNITY_EDITOR
            // In the Editor the init callback may not fire; the mock ads work without it.
            CreateAds();
#endif
        }

        public bool IsReady(EAdKind kind)
        {
            if (_onFinished != null) return false;

            return kind == EAdKind.Interstitial
                ? _interstitial != null && _interstitial.IsAdReady()
                : _rewarded != null && _rewarded.IsAdReady();
        }

        public void Show(EAdKind kind, Action<bool, bool> onFinished)
        {
            if (!IsReady(kind))
            {
                onFinished?.Invoke(false, false);
                return;
            }

            _onFinished = onFinished;
            _showing = kind;
            _rewardEarned = false;
            if (kind == EAdKind.Interstitial)
                _interstitial.ShowAd();
            else
                _rewarded.ShowAd();
        }

        public void Dispose()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;

            if (_interstitial != null)
            {
                _interstitial.OnAdLoaded -= OnInterstitialLoaded;
                _interstitial.OnAdLoadFailed -= OnInterstitialLoadFailed;
                _interstitial.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
                _interstitial.OnAdClosed -= OnInterstitialClosed;
                _interstitial.DestroyAd();
                _interstitial = null;
            }

            if (_rewarded != null)
            {
                _rewarded.OnAdLoaded -= OnRewardedLoaded;
                _rewarded.OnAdLoadFailed -= OnRewardedLoadFailed;
                _rewarded.OnAdDisplayFailed -= OnRewardedDisplayFailed;
                _rewarded.OnAdRewarded -= OnRewardedRewarded;
                _rewarded.OnAdClosed -= OnRewardedClosed;
                _rewarded.DestroyAd();
                _rewarded = null;
            }

            _onFinished = null;
        }

        // ------------------------------------------------------------------------------------ init

        private void ApplyPrivacy()
        {
            if (_settings.GdprConsent != EConsentValue.NotSet)
                LevelPlayPrivacySettings.SetGDPRConsent(_settings.GdprConsent == EConsentValue.Yes);
            if (_settings.CcpaDoNotSell != EConsentValue.NotSet)
                LevelPlayPrivacySettings.SetCCPA(_settings.CcpaDoNotSell == EConsentValue.Yes);
            if (_settings.ChildDirected != EConsentValue.NotSet)
                LevelPlayPrivacySettings.SetCOPPA(_settings.ChildDirected == EConsentValue.Yes);
        }

        private void OnInitSuccess(LevelPlayConfiguration configuration)
        {
            Debug.Log("LevelPlay SDK initialized successfully");
            _initFailures = 0;
            CreateAds();

            if (Debug.isDebugBuild && _settings.LaunchTestSuite)
                LevelPlay.LaunchTestSuite();
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            Debug.LogWarning($"LevelPlay initialization failed: {error.ErrorCode} {error.ErrorMessage}");
            Retry(() => LevelPlay.Init(_appKey), ref _initFailures);
        }

        private void CreateAds()
        {
            if (_adsCreated) return;
            _adsCreated = true;

            if (!string.IsNullOrEmpty(_interstitialUnitId))
            {
                _interstitial = new LevelPlayInterstitialAd(_interstitialUnitId);
                _interstitial.OnAdLoaded += OnInterstitialLoaded;
                _interstitial.OnAdLoadFailed += OnInterstitialLoadFailed;
                _interstitial.OnAdDisplayFailed += OnInterstitialDisplayFailed;
                _interstitial.OnAdClosed += OnInterstitialClosed;
                _interstitial.LoadAd();
            }
            else
            {
                Debug.LogWarning("LevelPlay: no interstitial ad unit id in MonetizationSettings; forced ads are off.");
            }

            if (!string.IsNullOrEmpty(_rewardedUnitId))
            {
                _rewarded = new LevelPlayRewardedAd(_rewardedUnitId);
                _rewarded.OnAdLoaded += OnRewardedLoaded;
                _rewarded.OnAdLoadFailed += OnRewardedLoadFailed;
                _rewarded.OnAdDisplayFailed += OnRewardedDisplayFailed;
                _rewarded.OnAdRewarded += OnRewardedRewarded;
                _rewarded.OnAdClosed += OnRewardedClosed;
                _rewarded.LoadAd();
            }
            else
            {
                Debug.LogWarning("LevelPlay: no rewarded ad unit id in MonetizationSettings; the rewarded ad is off.");
            }
        }

        // ------------------------------------------------------------------------------------ interstitial

        private void OnInterstitialLoaded(LevelPlayAdInfo info)
        {
            _interstitialFailures = 0;
            ReadinessChanged?.Invoke();
        }

        private void OnInterstitialLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"LevelPlay: interstitial failed to load: {error.ErrorCode} {error.ErrorMessage}");
            Retry(() => _interstitial?.LoadAd(), ref _interstitialFailures);
        }

        private void OnInterstitialDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"LevelPlay: interstitial failed to show: {error.ErrorCode} {error.ErrorMessage}");
            Finish(EAdKind.Interstitial, false, false);
            _interstitial?.LoadAd();
        }

        private void OnInterstitialClosed(LevelPlayAdInfo info)
        {
            Finish(EAdKind.Interstitial, true, false);
            _interstitial?.LoadAd();
        }

        // ------------------------------------------------------------------------------------ rewarded

        private void OnRewardedLoaded(LevelPlayAdInfo info)
        {
            _rewardedFailures = 0;
            ReadinessChanged?.Invoke();
        }

        private void OnRewardedLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"LevelPlay: rewarded ad failed to load: {error.ErrorCode} {error.ErrorMessage}");
            Retry(() => _rewarded?.LoadAd(), ref _rewardedFailures);
        }

        private void OnRewardedDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"LevelPlay: rewarded ad failed to show: {error.ErrorCode} {error.ErrorMessage}");
            Finish(EAdKind.Rewarded, false, false);
            _rewarded?.LoadAd();
        }

        private void OnRewardedRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            _rewardEarned = true;

            // The close came first and is waiting for the reward.
            if (_rewardWait != null)
            {
                _host.StopCoroutine(_rewardWait);
                _rewardWait = null;
                Finish(EAdKind.Rewarded, true, true);
            }
        }

        private void OnRewardedClosed(LevelPlayAdInfo info)
        {
            _rewarded?.LoadAd();
            if (_rewardEarned || _host == null)
            {
                Finish(EAdKind.Rewarded, true, _rewardEarned);
                return;
            }

            _rewardWait = _host.StartCoroutine(WaitForReward());
        }

        private IEnumerator WaitForReward()
        {
            yield return new WaitForSecondsRealtime(RewardGraceSeconds);
            _rewardWait = null;
            Finish(EAdKind.Rewarded, true, _rewardEarned);
        }

        // ------------------------------------------------------------------------------------ helpers

        private void Finish(EAdKind kind, bool shown, bool rewarded)
        {
            if (_onFinished == null || _showing != kind) return;

            var callback = _onFinished;
            _onFinished = null;
            callback(shown, rewarded);
        }

        /// <summary>Runs <paramref name="action"/> after the retry delay, doubled for each failure in a row (up to 8x).</summary>
        private void Retry(Action action, ref int failures)
        {
            var delay = _settings.LoadRetrySeconds * (1 << Mathf.Min(failures, MaxRetryDoublings));
            failures++;
            if (_host != null)
                _host.StartCoroutine(After(delay, action));
        }

        private static IEnumerator After(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }
    }
}
