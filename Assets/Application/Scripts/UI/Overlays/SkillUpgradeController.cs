using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// The camp and the skill upgrade (GDD 2.1 and 5): opened with a <see cref="CampEnteredEvent"/> it asks whether to
    /// rest (<c>RunManager.CampRest</c>) or to train (<c>RunManager.CampTrain</c>); opened with a
    /// <see cref="SkillUpgradeOfferEvent"/> it lists the skills that can be upgraded, then the variants of the chosen
    /// one with what each changes (<c>RunManager.ChooseSkillUpgrade</c>), or lets the player go
    /// (<c>RunManager.DeclineSkillUpgrade</c>). Each answer that opens the other choice closes this window: the run
    /// raises the next event and the flow opens a new one. It closes only through its own choices.
    /// </summary>
    [Preserve]
    public sealed class SkillUpgradeController : ViewController
    {
        private enum EStep
        {
            Camp,
            ChooseSkill,
            ChooseVariant
        }

        private CampEnteredEvent _camp;
        private SkillUpgradeOfferEvent _offer;
        private EStep _step;
        private SkillDefinition _chosen;
        private Label _title;
        private Label _subtitle;
        private ScrollView _scroll;
        private Button _declineButton;
        private Button _backButton;

        /// <summary>Whether the window shows the camp's rest or train choice.</summary>
        public bool IsCampStep => _step == EStep.Camp;

        /// <summary>Whether the window shows the variants of one skill.</summary>
        public bool IsVariantStep => _step == EStep.ChooseVariant;

        /// <summary>Number of buttons in the list.</summary>
        public int ListCount => _scroll != null ? _scroll.contentContainer.childCount : 0;

        /// <summary>The list container (for tests).</summary>
        public VisualElement ListContainer => _scroll != null ? _scroll.contentContainer : null;

        /// <summary>The "not now" button.</summary>
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
            _title = root.Q<Label>("upgrade-title");
            _subtitle = root.Q<Label>("upgrade-subtitle");
            _scroll = root.Q<ScrollView>("upgrade-scroll");
            _declineButton = root.Q<Button>("upgrade-decline-button");
            _backButton = root.Q<Button>("upgrade-back-button");

            _declineButton.OnClick(OnDeclineClicked);
            _backButton.OnClick(OnBackClicked);

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _camp = args as CampEnteredEvent;
            _offer = args as SkillUpgradeOfferEvent;
            _chosen = null;

            if (_camp != null)
                _step = EStep.Camp;
            else if (_offer != null && _offer.Skills.Count == 1)
                ChooseSkillStep(_offer.Skills[0]);
            else
                _step = EStep.ChooseSkill;

            Render();
            AudioManager.Play(ESfx.WindowOpen);
        }

        /// <summary>The choice has to be answered: the back action does nothing.</summary>
        protected override bool OnBack()
        {
            return true;
        }

        private void ChooseSkillStep(SkillDefinition skill)
        {
            _chosen = skill;
            _step = EStep.ChooseVariant;
        }

        private void Render()
        {
            if (_title == null) return;

            var content = _scroll.contentContainer;
            content.Clear();
            _scroll.scrollOffset = Vector2.zero;

            switch (_step)
            {
                case EStep.Camp:
                    RenderCamp(content);
                    break;
                case EStep.ChooseSkill:
                    RenderSkills(content);
                    break;
                default:
                    RenderVariants(content);
                    break;
            }
        }

        private void RenderCamp(VisualElement content)
        {
            _title.text = HudText.Get("camp.title");
            _subtitle.text = HudText.Get("camp.subtitle");
            _subtitle.SetDisplayed(true);
            _declineButton.SetDisplayed(false);
            _backButton.SetDisplayed(false);

            var heal = _camp != null ? _camp.HealAmount : 0;
            content.Add(CreateRow("camp-rest", null, HudText.Get("camp.rest"),
                heal > 0 ? HudText.Format("camp.rest_desc", heal) : HudText.Get("camp.rest_full"), null, OnRestClicked));
            content.Add(CreateRow("camp-train", null, HudText.Get("camp.train"), HudText.Get("camp.train_desc"), null,
                OnTrainClicked));
        }

        private void RenderSkills(VisualElement content)
        {
            _title.text = HudText.Get("upgrade.title");
            _subtitle.text = HudText.Get(_offer != null && _offer.Source == ESkillUpgradeSource.Elite
                ? "upgrade.subtitle_elite"
                : "upgrade.subtitle");
            _subtitle.SetDisplayed(true);
            _declineButton.SetDisplayed(true);
            _declineButton.text = HudText.Get(IsFromCamp ? "upgrade.back_to_camp" : "upgrade.decline");
            _backButton.SetDisplayed(false);

            if (_offer == null) return;

            for (var i = 0; i < _offer.Skills.Count; i++)
            {
                var skill = _offer.Skills[i];
                if (skill == null) continue;

                content.Add(CreateRow("upgrade-skill-" + i, skill.Icon, skill.GetDisplayName(),
                    HudText.Format("upgrade.variants", CountVariants(skill)), null, () => OnSkillClicked(skill)));
            }
        }

        private void RenderVariants(VisualElement content)
        {
            _title.text = _chosen != null ? _chosen.GetDisplayName() : HudText.Get("upgrade.title");
            _subtitle.text = HudText.Get("upgrade.choose_variant");
            _subtitle.SetDisplayed(true);

            // With a single skill on offer the list step is skipped: "back" is the way out of the window.
            var single = _offer != null && _offer.Skills.Count == 1;
            _backButton.SetDisplayed(!single);
            _backButton.text = HudText.Get("upgrade.back");
            _declineButton.SetDisplayed(single);
            _declineButton.text = HudText.Get(IsFromCamp ? "upgrade.back_to_camp" : "upgrade.decline");

            if (_chosen == null) return;

            for (var i = 0; i < _chosen.Upgrades.Count; i++)
            {
                var variant = _chosen.Upgrades[i];
                if (variant == null) continue;

                var index = i;
                content.Add(CreateRow("upgrade-variant-" + i, variant.Icon, variant.GetDisplayName(),
                    variant.GetDescription(), GetChanges(_chosen, variant), () => OnVariantClicked(index)));
            }
        }

        private bool IsFromCamp => _offer != null && _offer.Source == ESkillUpgradeSource.Camp;

        private static int CountVariants(SkillDefinition skill)
        {
            var count = 0;
            foreach (var variant in skill.Upgrades)
            {
                if (variant != null)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// The lines of the skill sheet the variant changes ("Damage: 2 → 3"), so the choice reads at a glance.
        /// The sheets are read without an owner: the asset's numbers, the same for both.
        /// </summary>
        private static List<string> GetChanges(SkillDefinition original, SkillDefinition variant)
        {
            var before = ActionDetailsModel.ForSkill(original, null);
            var after = ActionDetailsModel.ForSkill(variant, null);
            var changes = new List<string>();

            foreach (var fact in after.Facts)
            {
                var previous = FindFact(before, fact.Label);
                if (previous == null)
                    changes.Add(HudText.Format("upgrade.change_new", fact.Label, fact.Value));
                else if (previous != fact.Value)
                    changes.Add(HudText.Format("upgrade.change", fact.Label, previous, fact.Value));
            }

            foreach (var fact in before.Facts)
            {
                if (FindFact(after, fact.Label) == null)
                    changes.Add(HudText.Format("upgrade.change_removed", fact.Label));
            }

            return changes;
        }

        [CanBeNull]
        private static string FindFact(ActionDetailsModel model, string label)
        {
            foreach (var fact in model.Facts)
            {
                if (fact.Label == label)
                    return fact.Value;
            }

            return null;
        }

        /// <summary>A tappable row: icon, name, description and the list of changes (variants).</summary>
        private static Button CreateRow(string name, [CanBeNull] Sprite icon, string title, string description,
            [CanBeNull] List<string> changes, System.Action onClick)
        {
            var button = new Button { name = name, text = string.Empty };
            button.AddToClassList("upgrade-item");

            if (icon != null)
            {
                var image = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                image.AddToClassList("upgrade-item__icon");
                button.Add(image);
            }

            var texts = new VisualElement { pickingMode = PickingMode.Ignore };
            texts.AddToClassList("upgrade-item__texts");
            var label = new Label(title) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("upgrade-item__name");
            texts.Add(label);

            if (!string.IsNullOrEmpty(description))
            {
                var text = new Label(description) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("upgrade-item__desc");
                texts.Add(text);
            }

            if (changes != null)
            {
                foreach (var change in changes)
                {
                    var line = new Label(change) { pickingMode = PickingMode.Ignore };
                    line.AddToClassList("upgrade-item__change");
                    texts.Add(line);
                }
            }

            button.Add(texts);
            button.OnClick(onClick);
            return button;
        }

        private void OnRestClicked()
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.ButtonTap);
            runs.CampRest();
            Close();
        }

        private void OnTrainClicked()
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.ButtonTap);

            // The run raises the skill upgrade offer and the flow opens it in a new window.
            if (runs.CampTrain())
                Close();
        }

        private void OnSkillClicked(SkillDefinition skill)
        {
            AudioManager.Play(ESfx.ButtonTap);
            ChooseSkillStep(skill);
            Render();
        }

        private void OnVariantClicked(int index)
        {
            var runs = RunManager.Instance;
            if (runs == null || _chosen == null) return;

            if (runs.ChooseSkillUpgrade(_chosen, index) || runs.Phase != ERunPhase.SkillUpgrade)
                Close();
        }

        private void OnDeclineClicked()
        {
            var runs = RunManager.Instance;
            if (runs == null) return;

            AudioManager.Play(ESfx.WindowClose);

            // From a camp the run goes back to the rest or train choice, which opens in a new window.
            runs.DeclineSkillUpgrade();
            Close();
        }

        private void OnBackClicked()
        {
            AudioManager.Play(ESfx.ButtonTap);
            _step = EStep.ChooseSkill;
            _chosen = null;
            Render();
        }
    }
}
