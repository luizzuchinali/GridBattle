using System;
using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers.Audio;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Entity details modal (GDD 4.1): name, HP, role and behavior (enemies), effective attributes,
    /// skills, active states and the terrain of the cell. Opened by a long tap or right click on a cell
    /// with a <see cref="EntityDetailsRequest"/> argument. It closes only with its X button (releasing the
    /// finger or the button does not), never consumes the turn, and while it is open it holds
    /// <see cref="TurnBlockers"/> so the game accepts no actions.
    /// </summary>
    [Preserve]
    public sealed class EntityDetailsController : ViewController
    {
        private const string BlockerReason = "Entity details";

        private EntityDetailsRequest _request;
        private IDisposable _blocker;
        private ScrollView _scroll;
        private Label _title;
        private Image _portrait;

        protected override void OnCreate()
        {
            Loc.LocaleChanged += OnLocaleChanged;
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
        }

        protected override void OnDestroy()
        {
            Loc.LocaleChanged -= OnLocaleChanged;
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            ReleaseBlocker();
        }

        protected override void OnBind(VisualElement root)
        {
            _scroll = root.Q<ScrollView>("details-scroll");
            _title = root.Q<Label>("details-title");
            _portrait = root.Q<Image>("details-portrait");

            var closeButton = root.Q<Button>("close-button");
            closeButton.clicked += OnCloseClicked;

            // The dim background swallows every pointer event: nothing behind the window reacts.
            var background = root.Q<VisualElement>("overlay-background");
            background.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            Render();
        }

        protected override void OnEnter(object args)
        {
            _request = args as EntityDetailsRequest;
            if (_blocker == null)
                _blocker = TurnBlockers.Acquire(BlockerReason);

            Render();
            AudioManager.Play(ESfx.WindowOpen);
            TutorialService.Notify(ETutorialTrigger.FirstLongPress);
        }

        protected override void OnExit()
        {
            // The game resumes as soon as the window starts closing.
            ReleaseBlocker();
        }

        /// <summary>Only the X button closes the window: the back action does not.</summary>
        protected override bool OnBack()
        {
            return true;
        }

        private void OnCloseClicked()
        {
            AudioManager.Play(ESfx.WindowClose);
            Close();
        }

        private void OnLocaleChanged()
        {
            Render();
        }

        /// <summary>A new battle replaces everything the window shows: close it.</summary>
        private void OnGridInitialized(GridInitializedEvent e)
        {
            if (_request == null || !IsVisible) return;

            ReleaseBlocker();
            Close();
        }

        private void ReleaseBlocker()
        {
            _blocker?.Dispose();
            _blocker = null;
        }

        /// <summary>Fills the window from the request (again after a language change or a UI reload).</summary>
        private void Render()
        {
            if (_request == null || _scroll == null) return;

            var model = EntityDetailsModel.Build(_request, HudSettings.Current.ShowAllAttributes);
            _title.text = model.Title;
            _portrait.sprite = model.Portrait;
            _portrait.EnableInClassList("details-portrait--hidden", model.Portrait == null);

            var content = _scroll.contentContainer;
            content.Clear();

            if (model.IsCharacter)
            {
                AddRow(content, HudText.Get("details.hp"), model.Hp + " / " + model.MaxHp, null, "details-row--hp");

                if (model.HasRole)
                {
                    AddSection(content, HudText.Get("details.role"));
                    AddRow(content, model.RoleName, string.Empty, model.RoleIcon);
                    if (!string.IsNullOrEmpty(model.Behavior))
                    {
                        AddSection(content, HudText.Get("details.behavior"));
                        AddText(content, model.Behavior);
                    }
                }

                AddSection(content, HudText.Get("details.attributes"));
                foreach (var attribute in model.Attributes)
                    AddRow(content, attribute.Label, attribute.Value);

                AddSection(content, HudText.Get("details.skills"));
                if (model.Skills.Count == 0)
                    AddText(content, HudText.Get("details.none"));
                foreach (var skill in model.Skills)
                {
                    AddRow(content, skill.Name, skill.Status, skill.Icon,
                        skill.Ready ? "details-row--ready" : "details-row--waiting");
                }

                AddSection(content, HudText.Get("details.states"));
                if (model.States.Count == 0)
                    AddText(content, HudText.Get("details.none"));
                foreach (var state in model.States)
                {
                    var kindClass = state.Kind == EStateKind.Debuff ? "details-row--debuff" :
                        state.Kind == EStateKind.Buff ? "details-row--buff" : null;
                    AddRow(content, state.Name, state.Duration, state.Icon, kindClass);
                }
            }

            if (model.HasTerrain)
            {
                // Without a character the title already names the terrain: only its effect is left to say.
                if (!model.TerrainOnly)
                {
                    AddSection(content, HudText.Get("details.terrain"));
                    AddRow(content, model.TerrainName, string.Empty, model.TerrainIcon);
                }

                if (!string.IsNullOrEmpty(model.TerrainDescription))
                    AddText(content, model.TerrainDescription);
            }

            _scroll.scrollOffset = Vector2.zero;
        }

        private static void AddSection(VisualElement parent, string title)
        {
            var label = new Label(title);
            label.AddToClassList("details-section-title");
            parent.Add(label);
        }

        private static void AddText(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("details-text");
            parent.Add(label);
        }

        private static void AddRow(VisualElement parent, string label, string value, Sprite icon = null,
            string modifier = null)
        {
            var row = new VisualElement();
            row.AddToClassList("details-row");
            if (!string.IsNullOrEmpty(modifier))
                row.AddToClassList(modifier);

            if (icon != null)
            {
                var image = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                image.AddToClassList("details-icon");
                row.Add(image);
            }

            var name = new Label(label);
            name.AddToClassList("details-row__label");
            row.Add(name);

            if (!string.IsNullOrEmpty(value))
            {
                var text = new Label(value);
                text.AddToClassList("details-row__value");
                row.Add(text);
            }

            parent.Add(row);
        }
    }
}
