using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Talents;
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
    /// Talent choice (GDD Mechanic 3, interface 4.1): opens over the battle or the map when a level is reached or
    /// a talent node is entered, while the game is paused by the talent session. Shows the level (or "Talent
    /// node"), one card per option (icon, name, rank, description, "fits your build" and "new skill" badges) and
    /// the tools with the uses left: reroll, ban (a mode: tap Ban, then the card to ban; tap Ban again to cancel)
    /// and skip (after a confirmation). Every answer goes through <see cref="TalentService"/>; the window only
    /// redraws from the talent events. It cannot be closed by the back action or the backdrop: it closes when the
    /// last pending offer is resolved, after a short acquisition effect (visual only, the game resumes at once).
    /// </summary>
    [Preserve]
    public sealed class TalentChoiceController : ViewController
    {
        private const string HiddenClass = "talent-card--hidden";
        private const string BanClass = "talent-card--ban";
        private const string MatchClass = "talent-card--match";
        private const string ChosenClass = "talent-card--chosen";
        private const string DimClass = "talent-card--dim";
        private const string ClosingClass = "talent-window--closing";
        private const string SelectedToolClass = "text-button--selected";

        private readonly List<Button> _cards = new();

        private TalentOffer _offer;
        private TalentOffer _pendingOffer;
        private bool _banMode;
        private bool _askingSkip;
        private bool _resolved;
        private bool _exited;
        private int _chosenIndex = -1;
        private IVisualElementScheduledItem _closeItem;
        private IVisualElementScheduledItem _nextItem;

        private VisualElement _window;
        private Label _title;
        private Label _subtitle;
        private Label _queued;
        private ScrollView _scroll;
        private Button _rerollButton;
        private Button _banButton;
        private Button _skipButton;

        /// <summary>The offer on screen (null when there is none).</summary>
        public TalentOffer Offer => _offer;

        /// <summary>Ban mode is on: the next card tapped is banned.</summary>
        public bool IsBanMode => _banMode;

        /// <summary>The offer was answered: the window plays its effect and is about to close or show the next offer.</summary>
        public bool IsResolved => _resolved;

        /// <summary>The cards of the options on screen, in order.</summary>
        public IReadOnlyList<Button> Cards => _cards;

        public Button RerollButton => _rerollButton;
        public Button BanButton => _banButton;
        public Button SkipButton => _skipButton;

        protected override void OnCreate()
        {
            EventBus.Subscribe<TalentOfferOpenedEvent>(OnOfferOpened);
            EventBus.Subscribe<TalentOfferChangedEvent>(OnOfferChanged);
            EventBus.Subscribe<TalentAcquiredEvent>(OnTalentAcquired);
            EventBus.Subscribe<TalentOfferResolvedEvent>(OnOfferResolved);
            Loc.LocaleChanged += OnLocaleChanged;
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<TalentOfferOpenedEvent>(OnOfferOpened);
            EventBus.Unsubscribe<TalentOfferChangedEvent>(OnOfferChanged);
            EventBus.Unsubscribe<TalentAcquiredEvent>(OnTalentAcquired);
            EventBus.Unsubscribe<TalentOfferResolvedEvent>(OnOfferResolved);
            Loc.LocaleChanged -= OnLocaleChanged;
            CancelScheduled();
        }

        protected override void OnBind(VisualElement root)
        {
            _window = root.Q<VisualElement>("talent-window");
            _title = root.Q<Label>("talent-title");
            _subtitle = root.Q<Label>("talent-subtitle");
            _queued = root.Q<Label>("talent-queued");
            _scroll = root.Q<ScrollView>("talent-scroll");
            _scroll.HideScrollerWhenContentFits();
            _rerollButton = root.Q<Button>("talent-reroll-button");
            _banButton = root.Q<Button>("talent-ban-button");
            _skipButton = root.Q<Button>("talent-skip-button");

            _rerollButton.OnClick(OnRerollClicked);
            _banButton.OnClick(OnBanClicked);
            _skipButton.OnClick(OnSkipClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render(true);
        }

        protected override void OnEnter(object args)
        {
            _exited = false;
            _resolved = false;
            _banMode = false;
            _askingSkip = false;
            _chosenIndex = -1;

            // The service always holds the latest version of the open offer (the event's may be stale).
            _offer = TalentService.CurrentOffer ?? args as TalentOffer;

            Render(true);
            AudioManager.Play(ESfx.WindowOpen);
        }

        protected override void OnExit()
        {
            _exited = true;
            CancelScheduled();
        }

        /// <summary>The offer has to be answered: the back action does nothing.</summary>
        protected override bool OnBack()
        {
            return true;
        }

        // ------------------------------------------------------------------------------------ talent events

        /// <summary>The next offer of a kill that gave several levels (or a fresh one while the effect plays).</summary>
        private void OnOfferOpened(TalentOfferOpenedEvent e)
        {
            if (_exited) return;

            // A new offer keeps the window open (it may have been about to close after the previous pick).
            _closeItem?.Pause();
            _closeItem = null;
            _window?.RemoveFromClassList(ClosingClass);

            // Let the acquisition effect of the previous pick play before the next offer replaces it.
            var effect = ModalsSettings.Current.AcquireEffectSeconds;
            if (_resolved && _chosenIndex >= 0 && effect > 0f && Root != null)
            {
                _pendingOffer = e.Offer;
                _nextItem?.Pause();
                _nextItem = Root.schedule.Execute(ShowPendingOffer).StartingIn((long)(effect * 1000f));
                return;
            }

            ShowOffer(e.Offer);
        }

        private void ShowPendingOffer()
        {
            if (_exited || _pendingOffer == null) return;

            var offer = _pendingOffer;
            _pendingOffer = null;
            ShowOffer(offer);
        }

        private void ShowOffer(TalentOffer offer)
        {
            CancelScheduled();
            _window?.RemoveFromClassList(ClosingClass);
            _offer = offer;
            _banMode = false;
            _resolved = false;
            _chosenIndex = -1;
            Render(true);
        }

        private void OnOfferChanged(TalentOfferChangedEvent e)
        {
            if (_exited) return;

            if (_pendingOffer != null)
            {
                _pendingOffer = e.Offer;
                return;
            }

            _offer = e.Offer;
            if (e.Change == ETalentOfferChange.Queued)
            {
                RefreshQueued();
                return;
            }

            // Rerolled or banned: new cards.
            _banMode = false;
            Render(true);
        }

        /// <summary>The acquisition effect: the chosen card shines and the others fade.</summary>
        private void OnTalentAcquired(TalentAcquiredEvent e)
        {
            if (_exited || _chosenIndex < 0 || _chosenIndex >= _cards.Count) return;

            for (var i = 0; i < _cards.Count; i++)
            {
                _cards[i].EnableInClassList(ChosenClass, i == _chosenIndex);
                _cards[i].EnableInClassList(DimClass, i != _chosenIndex);
                _cards[i].RemoveFromClassList(BanClass);
            }
        }

        private void OnOfferResolved(TalentOfferResolvedEvent e)
        {
            if (_exited) return;

            _resolved = true;
            _banMode = false;
            RefreshTools();

            // Another offer follows right after: the window stays.
            if (e.HasMoreOffers) return;

            BeginClose();
        }

        private void OnLocaleChanged()
        {
            Render(false);
        }

        // ------------------------------------------------------------------------------------ closing

        private void BeginClose()
        {
            var effect = ModalsSettings.Current.AcquireEffectSeconds;
            if (effect <= 0f || Root == null || _window == null)
            {
                CloseNow();
                return;
            }

            var seconds = new List<TimeValue> { new(effect, TimeUnit.Second) };
            _window.style.transitionDuration = seconds;
            _window.AddToClassList(ClosingClass);
            _closeItem?.Pause();
            _closeItem = Root.schedule.Execute(CloseNow).StartingIn((long)(effect * 1000f));
        }

        private void CloseNow()
        {
            if (_exited) return;

            _exited = true;
            Close();
        }

        private void CancelScheduled()
        {
            _closeItem?.Pause();
            _closeItem = null;
            _nextItem?.Pause();
            _nextItem = null;
            _pendingOffer = null;
            if (_window != null)
                _window.style.transitionDuration = StyleKeyword.Null;
        }

        // ------------------------------------------------------------------------------------ rendering

        private void Render(bool animate)
        {
            if (_title == null) return;

            var content = _scroll.contentContainer;
            content.Clear();
            _cards.Clear();
            _scroll.scrollOffset = Vector2.zero;

            if (_offer == null)
            {
                _title.text = HudText.Get("talent.title.node");
                _subtitle.text = string.Empty;
                _queued.SetDisplayed(false);
                RefreshTools();
                return;
            }

            _title.text = _offer.Source == ETalentOfferSource.TalentNode
                ? HudText.Get("talent.title.node")
                : HudText.Format("talent.title.level", _offer.Level);
            RefreshSubtitle();
            RefreshQueued();

            var stagger = ModalsSettings.Current.CardStaggerMs;
            for (var i = 0; i < _offer.Options.Count; i++)
            {
                var card = CreateCard(_offer.Options[i], i);
                _cards.Add(card);
                content.Add(card);

                // Cards fade in one after the other (the transition runs when the class is removed).
                if (animate && stagger > 0)
                {
                    card.AddToClassList(HiddenClass);
                    var shown = card;
                    card.schedule.Execute(() => shown.RemoveFromClassList(HiddenClass)).StartingIn(30 + i * stagger);
                }
            }

            RefreshTools();
        }

        private Button CreateCard(TalentOption option, int index)
        {
            var talent = option.Talent;
            var card = new Button { name = "talent-card-" + index, text = string.Empty };
            card.AddToClassList("talent-card");
            card.EnableInClassList(MatchClass, option.MatchesBuild);
            card.EnableInClassList(BanClass, _banMode);

            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("talent-card__top");

            var icon = new Image { sprite = talent.Icon, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("talent-card__icon");
            top.Add(icon);

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("talent-card__texts");

            var titleRow = new VisualElement { pickingMode = PickingMode.Ignore };
            titleRow.AddToClassList("talent-card__title-row");
            var name = new Label(talent.GetDisplayName()) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("talent-card__name");
            titleRow.Add(name);
            if (option.MaxRank > 1)
            {
                var rank = new Label(RomanNumerals.Rank(option.NextRank, option.MaxRank))
                    { pickingMode = PickingMode.Ignore };
                rank.AddToClassList("talent-card__rank");
                titleRow.Add(rank);
            }

            texts.Add(titleRow);

            var description = talent.GetDescription();
            if (!string.IsNullOrEmpty(description))
            {
                var desc = new Label(description) { pickingMode = PickingMode.Ignore };
                desc.AddToClassList("talent-card__desc");
                texts.Add(desc);
            }

            top.Add(texts);
            card.Add(top);

            if (option.MatchesBuild || option.UnlocksSkill)
            {
                var badges = new VisualElement { pickingMode = PickingMode.Ignore };
                badges.AddToClassList("talent-card__badges");

                if (option.MatchesBuild)
                {
                    var tags = new List<string>();
                    foreach (var tag in option.SharedTags)
                    {
                        if (tag != null)
                            tags.Add(tag.GetDisplayName());
                    }

                    badges.Add(CreateBadge("badge--match", ModalsSettings.Current.BuildMatchIcon,
                        HudText.Format("talent.match", string.Join(", ", tags))));
                }

                if (option.UnlocksSkill)
                {
                    var skill = option.Skill;
                    var skillIcon = skill != null && skill.Icon != null ? skill.Icon : ModalsSettings.Current.NewSkillIcon;
                    var skillName = skill != null ? skill.GetDisplayName() : string.Empty;
                    badges.Add(CreateBadge("badge--skill", skillIcon, HudText.Format("talent.new_skill", skillName)));
                }

                card.Add(badges);
            }

            card.OnClick(() => OnCardClicked(index));
            return card;
        }

        private static VisualElement CreateBadge(string modifier, Sprite icon, string text)
        {
            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.AddToClassList("badge");
            badge.AddToClassList(modifier);

            if (icon != null)
            {
                var image = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                image.AddToClassList("badge__icon");
                badge.Add(image);
            }

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("badge__label");
            badge.Add(label);
            return badge;
        }

        private void RefreshSubtitle()
        {
            _subtitle.text = _banMode ? HudText.Get("talent.choose_ban") : HudText.Get("talent.choose");
        }

        private void RefreshQueued()
        {
            var queued = _offer != null ? _offer.QueuedAfter : 0;
            _queued.SetDisplayed(queued > 0);
            _queued.text = queued > 0 ? HudText.Format("talent.queued", queued) : string.Empty;
        }

        /// <summary>Texts, uses left and the disabled look of the three tools.</summary>
        private void RefreshTools()
        {
            if (_rerollButton == null) return;

            var rerolls = _offer != null ? _offer.RerollsLeft : 0;
            var bans = _offer != null ? _offer.BansLeft : 0;
            var skips = _offer != null ? _offer.SkipsLeft : 0;
            var live = _offer != null && !_resolved;

            _rerollButton.text = HudText.Format("talent.reroll", rerolls);
            _rerollButton.SetEnabled(live && _offer.CanReroll && !_banMode);

            _banButton.text = _banMode ? HudText.Get("talent.ban_cancel") : HudText.Format("talent.ban", bans);
            _banButton.SetEnabled(live && (_banMode || _offer.CanBan));
            _banButton.EnableInClassList(SelectedToolClass, _banMode);

            _skipButton.text = HudText.Format("talent.skip", skips);
            _skipButton.SetEnabled(live && _offer.CanSkip && !_banMode);
        }

        private void SetBanMode(bool on)
        {
            _banMode = on;
            foreach (var card in _cards)
                card.EnableInClassList(BanClass, on);

            RefreshSubtitle();
            RefreshTools();
        }

        // ------------------------------------------------------------------------------------ input

        private bool CanAct => _offer != null && !_resolved && !_askingSkip && !_exited;

        private void OnCardClicked(int index)
        {
            if (!CanAct || index < 0 || index >= _offer.Options.Count) return;

            AudioManager.Play(ESfx.ButtonTap);
            if (_banMode)
            {
                // The ban redraws the offer (TalentOfferChangedEvent); if it fails the mode just ends.
                var banned = TalentService.Ban(index);
                if (!banned)
                    SetBanMode(false);
                return;
            }

            _chosenIndex = index;
            if (!TalentService.Choose(index))
                _chosenIndex = -1;
        }

        private void OnRerollClicked()
        {
            if (!CanAct || !_offer.CanReroll || _banMode) return;

            AudioManager.Play(ESfx.ButtonTap);
            TalentService.Reroll();
        }

        private void OnBanClicked()
        {
            if (!CanAct) return;
            if (!_banMode && !_offer.CanBan) return;

            AudioManager.Play(ESfx.ButtonTap);
            SetBanMode(!_banMode);
        }

        private async void OnSkipClicked()
        {
            if (!CanAct || !_offer.CanSkip || _banMode) return;

            AudioManager.Play(ESfx.ButtonTap);
            var settings = ModalsSettings.Current;
            if (settings.ConfirmSkip && settings.ConfirmView != null)
            {
                _askingSkip = true;
                bool confirmed;
                try
                {
                    var request = new ConfirmRequest(HudText.Get("talent.skip.title"), HudText.Get("talent.skip.message"));
                    confirmed = await Context.Navigator.ShowModal<bool>(settings.ConfirmView, request);
                }
                finally
                {
                    _askingSkip = false;
                }

                if (!confirmed || _exited || _resolved || _offer == null) return;
            }

            TalentService.Skip();
        }
    }
}
