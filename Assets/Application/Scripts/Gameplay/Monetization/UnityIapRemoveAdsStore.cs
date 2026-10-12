using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GridBattle.Gameplay.Monetization
{
    /// <summary>
    /// The remove ads purchase through Unity IAP 5 (Google Play / App Store): a non-consumable product. Flow:
    /// connect → fetch the product (price) and the account's purchases → buy → the store reports the order as
    /// pending → the purchase is granted and the profile saved → only then the order is confirmed (an unconfirmed
    /// order is delivered again at the next launch, so a failed save never loses the purchase). Granting is
    /// idempotent (a flag), so an order delivered twice changes nothing. Restore asks the store for the account's
    /// purchases (required by Apple for non-consumables). In the Editor the package uses its fake store.
    /// </summary>
    internal sealed class UnityIapRemoveAdsStore : IRemoveAdsStore
    {
        private readonly string _productId;
        private readonly string _storeProductId;

        private StoreController _store;
        private IRemoveAdsStoreListener _listener;
        private Product _product;
        private bool _restoring;
        private bool _disposed;

        public UnityIapRemoveAdsStore(MonetizationSettings settings)
        {
            _productId = settings.RemoveAdsProductId;
#if UNITY_IOS
            var platformId = settings.AppleProductId;
#else
            var platformId = settings.GooglePlayProductId;
#endif
            _storeProductId = string.IsNullOrEmpty(platformId) ? _productId : platformId;
        }

        public ERemoveAdsStatus Status { get; private set; } = ERemoveAdsStatus.Loading;

        public string Price => _product != null && _product.metadata != null ? _product.metadata.localizedPriceString : null;

        public async void Initialize(IRemoveAdsStoreListener listener)
        {
            _listener = listener;
            if (string.IsNullOrEmpty(_productId))
            {
                Debug.LogWarning("Unity IAP: no remove ads product id in MonetizationSettings; the purchase is off.");
                SetStatus(ERemoveAdsStatus.Unavailable);
                return;
            }

            _store = UnityIAPServices.StoreController();

            // Every event is subscribed before Connect: orders pending from a previous session may arrive at once.
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnCheckEntitlement += OnCheckEntitlement;

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Unity IAP: could not connect to the store: {e.Message}");
                SetStatus(ERemoveAdsStatus.Unavailable);
            }
        }

        public void Buy()
        {
            // Owned here while the profile says otherwise (the Editor's testing menu): the store answers "already
            // owned", which grants the purchase again.
            if (_store == null || _product == null ||
                (Status != ERemoveAdsStatus.Ready && Status != ERemoveAdsStatus.Owned)) return;

            SetStatus(ERemoveAdsStatus.Purchasing);
            _store.PurchaseProduct(_product);
        }

        public void Restore()
        {
            if (_store == null || _restoring) return;

            _restoring = true;
            _store.RestoreTransactions((success, error) =>
            {
                if (_disposed) return;

                if (!success)
                {
                    Debug.LogWarning($"Unity IAP: restore failed: {error}");
                    _restoring = false;
                    _listener.OnStoreMessage(ERemoveAdsMessage.RestoreFailed);
                    return;
                }

                // The restored purchases arrive as pending orders and in the purchases fetched now; the answer is
                // given when that fetch returns.
                _store.FetchPurchases();
            });
        }

        public void Dispose()
        {
            _disposed = true;
            if (_store == null) return;

            _store.OnStoreConnected -= OnStoreConnected;
            _store.OnStoreDisconnected -= OnStoreDisconnected;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasesFetched -= OnPurchasesFetched;
            _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
            _store.OnCheckEntitlement -= OnCheckEntitlement;
        }

        // ------------------------------------------------------------------------------------ store

        private void OnStoreConnected()
        {
            _store.FetchProducts(new List<ProductDefinition>
            {
                new(_productId, _storeProductId, ProductType.NonConsumable),
            });
            _store.FetchPurchases();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"Unity IAP: store disconnected: {failure.Message}");
            if (Status != ERemoveAdsStatus.Owned)
                SetStatus(ERemoveAdsStatus.Unavailable);
        }

        private void OnProductsFetched(List<Product> products)
        {
            foreach (var product in products)
            {
                if (IsRemoveAds(product))
                    _product = product;
            }

            if (_product == null)
            {
                Debug.LogWarning($"Unity IAP: the store has no product '{_storeProductId}'.");
                SetStatus(ERemoveAdsStatus.Unavailable);
                return;
            }

            if (Status != ERemoveAdsStatus.Owned)
                SetStatus(ERemoveAdsStatus.Ready);
            _store.CheckEntitlement(_product);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"Unity IAP: product fetch failed: {failure.FailureReason}");
            if (Status != ERemoveAdsStatus.Owned)
                SetStatus(ERemoveAdsStatus.Unavailable);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            var found = false;
            foreach (var order in orders.ConfirmedOrders)
            {
                if (Contains(order))
                    found = true;
            }

            if (found)
                Grant();

            if (!_restoring) return;

            _restoring = false;
            _listener.OnStoreMessage(found || Status == ERemoveAdsStatus.Owned
                ? ERemoveAdsMessage.Restored
                : ERemoveAdsMessage.NothingToRestore);
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"Unity IAP: purchases fetch failed: {failure.FailureReason} {failure.Message}");
            if (!_restoring) return;

            _restoring = false;
            _listener.OnStoreMessage(ERemoveAdsMessage.RestoreFailed);
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement.Product != null && IsRemoveAds(entitlement.Product) &&
                entitlement.Status == EntitlementStatus.FullyEntitled)
                Grant();
        }

        // ------------------------------------------------------------------------------------ purchase

        private void OnPurchasePending(PendingOrder order)
        {
            if (!Contains(order)) return;

            // Save before confirming: a confirmed purchase that was not saved would be lost.
            if (Grant())
                _store.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed && Contains(order))
                Debug.LogWarning($"Unity IAP: confirmation failed: {failed.FailureReason} {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            if (Status == ERemoveAdsStatus.Owned) return;

            Debug.LogWarning($"Unity IAP: purchase failed: {order.FailureReason} {order.Details}");
            SetStatus(_product != null ? ERemoveAdsStatus.Ready : ERemoveAdsStatus.Unavailable);
            _listener.OnStoreMessage(order.FailureReason == PurchaseFailureReason.UserCancelled
                ? ERemoveAdsMessage.PurchaseCancelled
                : ERemoveAdsMessage.PurchaseFailed);
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            // Nothing is granted until the purchase is approved (it then arrives as a pending order).
            if (Contains(order) && Status != ERemoveAdsStatus.Owned)
                SetStatus(ERemoveAdsStatus.Deferred);
        }

        // ------------------------------------------------------------------------------------ helpers

        /// <summary>Grants the purchase through the listener; true when it is saved.</summary>
        private bool Grant()
        {
            if (!_listener.OnEntitled()) return false;

            SetStatus(ERemoveAdsStatus.Owned);
            return true;
        }

        private bool IsRemoveAds(Product product)
        {
            var definition = product != null ? product.definition : null;
            return definition != null && (definition.id == _productId || definition.storeSpecificId == _storeProductId);
        }

        private bool Contains(Order order)
        {
            if (order == null || order.CartOrdered == null) return false;

            foreach (var item in order.CartOrdered.Items())
            {
                if (item != null && IsRemoveAds(item.Product))
                    return true;
            }

            return false;
        }

        private void SetStatus(ERemoveAdsStatus status)
        {
            if (_disposed) return;

            Status = status;
            _listener.OnStoreChanged();
        }
    }
}
