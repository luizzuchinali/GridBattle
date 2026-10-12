using System.Collections;
using UnityEngine;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>Where the remove ads purchase stands, for the window that sells it.</summary>
    public enum ERemoveAdsStatus
    {
        /// <summary>No store on this platform, or the store could not be reached.</summary>
        Unavailable,

        /// <summary>Connecting to the store and reading the price.</summary>
        Loading,

        /// <summary>The price is known: it can be bought.</summary>
        Ready,

        /// <summary>The store's purchase dialog is open.</summary>
        Purchasing,

        /// <summary>The purchase waits for an approval (Ask to Buy, a pending payment).</summary>
        Deferred,

        /// <summary>Bought: the forced ads are off.</summary>
        Owned
    }

    /// <summary>The outcome of the last purchase or restore, shown once in the window.</summary>
    public enum ERemoveAdsMessage
    {
        None,
        PurchaseFailed,
        PurchaseCancelled,
        Restored,
        NothingToRestore,
        RestoreFailed
    }

    /// <summary>What a store reports back to the monetization service (always on the main thread).</summary>
    internal interface IRemoveAdsStoreListener
    {
        /// <summary>The status or the price changed.</summary>
        void OnStoreChanged();

        /// <summary>
        /// The player owns the purchase (bought now, restored, or found at startup). The listener grants it and
        /// saves; it returns false if saving failed, and the store then leaves the purchase unconfirmed so the
        /// store delivers it again later.
        /// </summary>
        bool OnEntitled();

        /// <summary>A purchase or restore ended with <paramref name="message"/> to show.</summary>
        void OnStoreMessage(ERemoveAdsMessage message);
    }

    /// <summary>The store that sells the remove ads purchase: Unity IAP, the game's test store or none.</summary>
    internal interface IRemoveAdsStore
    {
        ERemoveAdsStatus Status { get; }

        /// <summary>Price in the player's currency, as the store formats it (null until known).</summary>
        string Price { get; }

        /// <summary>Connects, reads the price and checks whether the player already owns the purchase.</summary>
        void Initialize(IRemoveAdsStoreListener listener);

        void Buy();

        /// <summary>Asks the store for the purchases of the player's account (a new device, a reinstall).</summary>
        void Restore();

        void Dispose();
    }

    /// <summary>No store (PC builds, an Editor set to Off, a mobile platform turned off in the settings).</summary>
    internal sealed class NoRemoveAdsStore : IRemoveAdsStore
    {
        public ERemoveAdsStatus Status => ERemoveAdsStatus.Unavailable;
        public string Price => null;

        public void Initialize(IRemoveAdsStoreListener listener)
        {
        }

        public void Buy()
        {
        }

        public void Restore()
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// The game's test store, for the Editor: a fixed price, a purchase that succeeds after
    /// <see cref="MonetizationSettings.SimulatedStoreSeconds"/>, and a restore that finds the purchase only when
    /// the profile already has it (or <see cref="MonetizationSettings.SimulatedAccountOwnsPurchase"/> is on).
    /// </summary>
    internal sealed class SimulatedRemoveAdsStore : IRemoveAdsStore
    {
        private readonly MonoBehaviour _host;
        private readonly MonetizationSettings _settings;
        private IRemoveAdsStoreListener _listener;
        private bool _owned;

        public SimulatedRemoveAdsStore(MonoBehaviour host, MonetizationSettings settings)
        {
            _host = host;
            _settings = settings;
        }

        public ERemoveAdsStatus Status { get; private set; } = ERemoveAdsStatus.Loading;
        public string Price => _settings.SimulatedPrice;

        public void Initialize(IRemoveAdsStoreListener listener)
        {
            _listener = listener;
            Status = ERemoveAdsStatus.Ready;
            _listener.OnStoreChanged();
        }

        public void Buy()
        {
            if ((Status != ERemoveAdsStatus.Ready && Status != ERemoveAdsStatus.Owned) || _host == null) return;

            Status = ERemoveAdsStatus.Purchasing;
            _listener.OnStoreChanged();
            _host.StartCoroutine(Answer(() =>
            {
                _owned = true;
                Status = _listener.OnEntitled() ? ERemoveAdsStatus.Owned : ERemoveAdsStatus.Ready;
                _listener.OnStoreChanged();
            }));
        }

        public void Restore()
        {
            if (_host == null) return;

            _host.StartCoroutine(Answer(() =>
            {
                if (_owned || _settings.SimulatedAccountOwnsPurchase)
                {
                    _listener.OnEntitled();
                    Status = ERemoveAdsStatus.Owned;
                    _listener.OnStoreMessage(ERemoveAdsMessage.Restored);
                }
                else
                {
                    _listener.OnStoreMessage(ERemoveAdsMessage.NothingToRestore);
                }

                _listener.OnStoreChanged();
            }));
        }

        public void Dispose()
        {
        }

        private IEnumerator Answer(System.Action action)
        {
            yield return new WaitForSecondsRealtime(_settings.SimulatedStoreSeconds);
            action();
        }
    }
}
