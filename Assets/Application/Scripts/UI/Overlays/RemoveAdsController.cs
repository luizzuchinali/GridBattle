using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Monetization;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Remove ads window (GDD 7, interface 4.3): what the purchase gives, Buy with the store's price, Restore
    /// purchases (required by Apple for a non-consumable) and the state of the purchase (loading, waiting for the
    /// store, waiting for an approval, bought) or the outcome of the last purchase or restore. Every action goes
    /// through <see cref="MonetizationService"/>; the window redraws on <see cref="MonetizationChangedEvent"/>.
    /// </summary>
    [Preserve]
    public sealed class RemoveAdsController : ModalController<bool>
    {
        private const string GoodClass = "remove-ads-status--good";
        private const string BadClass = "remove-ads-status--bad";

        private Label _title;
        private Label _text;
        private Label _status;
        private Button _buyButton;
        private Button _restoreButton;

        public Button BuyButton => _buyButton;
        public Button RestoreButton => _restoreButton;

        /// <summary>The status line on screen (for tests).</summary>
        public string StatusText => _status != null ? _status.text : string.Empty;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
            EventBus.Subscribe<MonetizationChangedEvent>(OnMonetizationChanged);
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
            EventBus.Unsubscribe<MonetizationChangedEvent>(OnMonetizationChanged);
        }

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("remove-ads-title");
            _text = root.Q<Label>("remove-ads-text");
            _status = root.Q<Label>("remove-ads-status");
            _buyButton = root.Q<Button>("remove-ads-buy-button");
            _restoreButton = root.Q<Button>("remove-ads-restore-button");

            _buyButton.OnClick(OnBuyClicked);
            _restoreButton.OnClick(OnRestoreClicked);
            root.Q<Button>("remove-ads-close-button").OnClick(OnCloseClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            root.Q<VisualElement>("overlay-background").RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Render();
        }

        protected override void OnEnter(object args)
        {
            MonetizationService.ClearStoreMessage();
            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        protected override bool OnBack()
        {
            OnCloseClicked();
            return true;
        }

        private void OnBuyClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            MonetizationService.BuyRemoveAds();
            Render();
        }

        private void OnRestoreClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            MonetizationService.RestorePurchases();
            Render();
        }

        private void OnCloseClicked()
        {
            AudioManager.Play(ESfx.WindowClose);
            Close(MonetizationService.AdsRemoved);
        }

        private void OnMonetizationChanged(MonetizationChangedEvent e)
        {
            Render();
        }

        private void Render()
        {
            if (_title == null) return;

            var status = MonetizationService.RemoveAdsStatus;
            var owned = status == ERemoveAdsStatus.Owned;
            var price = MonetizationService.RemoveAdsPrice;

            _title.text = HudText.Get("remove_ads.title");
            _text.text = owned ? HudText.Get("remove_ads.owned_text") : HudText.Get("remove_ads.text");

            _buyButton.text = string.IsNullOrEmpty(price)
                ? HudText.Get("remove_ads.buy_no_price")
                : HudText.Format("remove_ads.buy", price);
            _buyButton.SetDisplayed(!owned);
            _buyButton.SetEnabled(status == ERemoveAdsStatus.Ready);

            _restoreButton.text = HudText.Get("remove_ads.restore");
            _restoreButton.SetDisplayed(!owned && status != ERemoveAdsStatus.Unavailable);
            _restoreButton.SetEnabled(status == ERemoveAdsStatus.Ready || status == ERemoveAdsStatus.Deferred);

            var (text, good, bad) = GetStatusLine(status, MonetizationService.StoreMessage);
            _status.text = text;
            _status.SetDisplayed(!string.IsNullOrEmpty(text));
            _status.EnableInClassList(GoodClass, good);
            _status.EnableInClassList(BadClass, bad);
        }

        /// <summary>The line under the description: the outcome of the last action first, else the state.</summary>
        private static (string text, bool good, bool bad) GetStatusLine(ERemoveAdsStatus status, ERemoveAdsMessage message)
        {
            switch (message)
            {
                case ERemoveAdsMessage.PurchaseFailed:
                    return (HudText.Get("remove_ads.failed"), false, true);
                case ERemoveAdsMessage.PurchaseCancelled:
                    return (HudText.Get("remove_ads.cancelled"), false, false);
                case ERemoveAdsMessage.Restored:
                    return (HudText.Get("remove_ads.restored"), true, false);
                case ERemoveAdsMessage.NothingToRestore:
                    return (HudText.Get("remove_ads.nothing_restored"), false, false);
                case ERemoveAdsMessage.RestoreFailed:
                    return (HudText.Get("remove_ads.restore_failed"), false, true);
            }

            return status switch
            {
                ERemoveAdsStatus.Owned => (HudText.Get("remove_ads.owned"), true, false),
                ERemoveAdsStatus.Loading => (HudText.Get("remove_ads.loading"), false, false),
                ERemoveAdsStatus.Purchasing => (HudText.Get("remove_ads.purchasing"), false, false),
                ERemoveAdsStatus.Deferred => (HudText.Get("remove_ads.deferred"), false, false),
                ERemoveAdsStatus.Unavailable => (HudText.Get("remove_ads.unavailable"), false, true),
                _ => (string.Empty, false, false),
            };
        }
    }
}
