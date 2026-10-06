using GridBattle.Core;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Consumable node offer (GDD 2.8, consumiveis): lists the option(s) with icon, name and description; taking
    /// one calls <c>RunManager.ChooseConsumableOffer</c>. With a full inventory (policy AskPlayer) it shows the
    /// carried items to discard one (<c>ReplaceConsumable</c>) or leave the offer (<c>DeclineConsumableOffer</c>).
    /// Opened with the <see cref="ConsumableOfferEvent"/> as argument. It closes only through its own choices.
    /// </summary>
    [Preserve]
    public sealed class ConsumableOfferController : ViewController
    {
        private enum EStep
        {
            ChooseOption,
            DiscardOne
        }

        private ConsumableOfferEvent _offer;
        private EStep _step;
        private int _chosen;
        private Label _title;
        private Label _subtitle;
        private ScrollView _scroll;
        private Button _declineButton;
        private Button _backButton;

        /// <summary>What the window is asking now ("choose an option" or "discard one").</summary>
        public bool IsDiscardStep => _step == EStep.DiscardOne;

        /// <summary>Number of buttons in the list (options or carried items).</summary>
        public int ListCount => _scroll != null ? _scroll.contentContainer.childCount : 0;

        /// <summary>The list container (for tests).</summary>
        public VisualElement ListContainer => _scroll != null ? _scroll.contentContainer : null;

        /// <summary>The "leave it" button of the discard step.</summary>
        public Button DeclineButton => _declineButton;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += Render;
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= Render;
        }

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("offer-title");
            _subtitle = root.Q<Label>("offer-subtitle");
            _scroll = root.Q<ScrollView>("offer-scroll");
            _declineButton = root.Q<Button>("offer-decline-button");
            _backButton = root.Q<Button>("offer-back-button");

            _declineButton.OnClick(OnDeclineClicked);
            _backButton.OnClick(OnBackClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _offer = args as ConsumableOfferEvent;
            _chosen = -1;

            // A single option with a full inventory goes straight to choosing what to discard.
            if (_offer != null && _offer.InventoryFull && _offer.Options.Count == 1)
            {
                _chosen = 0;
                _step = EStep.DiscardOne;
            }
            else
            {
                _step = EStep.ChooseOption;
            }

            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        /// <summary>The offer has to be answered: the back action does nothing.</summary>
        protected override bool OnBack()
        {
            return true;
        }

        private void Render()
        {
            if (_title == null) return;

            var content = _scroll.contentContainer;
            content.Clear();
            _scroll.scrollOffset = Vector2.zero;

            if (_offer == null || _offer.Options.Count == 0)
            {
                _title.text = HudText.Get("consumable_offer.title");
                _subtitle.text = string.Empty;
                _declineButton.SetDisplayed(false);
                _backButton.SetDisplayed(false);
                return;
            }

            if (_step == EStep.ChooseOption)
                RenderOptions(content);
            else
                RenderDiscard(content);
        }

        private void RenderOptions(VisualElement content)
        {
            _title.text = HudText.Get("consumable_offer.title");
            _subtitle.text = _offer.Options.Count > 1 ? HudText.Get("consumable_offer.choose") : string.Empty;
            _subtitle.SetDisplayed(_offer.Options.Count > 1);
            _declineButton.SetDisplayed(false);
            _backButton.SetDisplayed(false);

            for (var i = 0; i < _offer.Options.Count; i++)
            {
                var index = i;
                content.Add(CreateItemButton(_offer.Options[i], "offer-option-" + i, () => OnOptionClicked(index)));
            }
        }

        private void RenderDiscard(VisualElement content)
        {
            var offered = _chosen >= 0 && _chosen < _offer.Options.Count ? _offer.Options[_chosen] : null;

            _title.text = HudText.Get("consumable_offer.full");
            _subtitle.text = offered != null
                ? HudText.Format("consumable_offer.discard", offered.GetDisplayName())
                : HudText.Get("consumable_offer.discard_generic");
            _subtitle.SetDisplayed(true);
            _declineButton.SetDisplayed(true);
            _declineButton.text = HudText.Get("consumable_offer.decline");
            _backButton.SetDisplayed(_offer.Options.Count > 1);
            _backButton.text = HudText.Get("consumable_offer.back");

            var runs = RunManager.Instance;
            var run = runs != null ? runs.CurrentRun : null;
            if (run == null) return;

            var inventory = new ConsumableInventory(run.Player.ConsumableIds);
            for (var slot = 0; slot < inventory.Slots; slot++)
            {
                var item = inventory.Get(slot);
                if (item == null) continue;

                var index = slot;
                content.Add(CreateItemButton(item, "offer-slot-" + slot, () => OnDiscardClicked(index)));
            }
        }

        /// <summary>A tappable row: icon, name and description of a consumable.</summary>
        private static Button CreateItemButton(ConsumableDefinition item, string name, System.Action onClick)
        {
            var button = new Button { name = name, text = string.Empty };
            button.AddToClassList("offer-item");

            var icon = new Image { sprite = item.Icon, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("offer-item__icon");
            button.Add(icon);

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("offer-item__texts");
            var title = new Label(item.GetDisplayName()) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("offer-item__name");
            texts.Add(title);
            var description = item.GetDescription();
            if (!string.IsNullOrEmpty(description))
            {
                var text = new Label(description) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("offer-item__desc");
                texts.Add(text);
            }

            button.Add(texts);
            button.OnClick(onClick);
            return button;
        }

        private void OnOptionClicked(int index)
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.ButtonTap);
            switch (runs.ChooseConsumableOffer(index))
            {
                case EConsumableOfferResult.NeedsSlotChoice:
                    _chosen = index;
                    _step = EStep.DiscardOne;
                    Render();
                    break;
                case EConsumableOfferResult.Invalid:
                    // The offer is gone (the run moved on): nothing left to ask.
                    Close();
                    break;
                default:
                    // Taken (or lost by the inventory policy): the node is complete.
                    Close();
                    break;
            }
        }

        private void OnDiscardClicked(int slot)
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.ButtonTap);
            if (runs.ReplaceConsumable(slot))
                Close();
        }

        private void OnDeclineClicked()
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.WindowClose);
            runs.DeclineConsumableOffer();
            Close();
        }

        private void OnBackClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            _step = EStep.ChooseOption;
            _chosen = -1;
            Render();
        }
    }
}
