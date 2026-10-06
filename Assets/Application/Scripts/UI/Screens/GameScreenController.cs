using GridBattle.Core;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// In-game HUD (GDD 4.2): XP bar with the level, depth, menu button, item bar and skill bar. Also
    /// opens the entity details window on a long tap or right click on a cell (GDD 4.1).
    /// </summary>
    [Preserve]
    public sealed class GameScreenController : ViewController
    {
        private DepthView _depthView;
        private SkillBarView _skillBar;
        private ItemBarView _itemBar;
        private int _depth;

        /// <summary>
        /// The screen's XP bar (null until the UI loads).
        /// </summary>
        public XpBarView XpBar { get; private set; }

        /// <summary>The skill bar (null until the UI loads).</summary>
        public SkillBarView SkillBar => _skillBar;

        /// <summary>The item bar (null until the UI loads).</summary>
        public ItemBarView ItemBar => _itemBar;

        /// <summary>The depth next to the XP bar (null until the UI loads).</summary>
        public DepthView Depth => _depthView;

        /// <summary>
        /// Layer where floating effects (e.g. XP orbs) are added: the panel root, in
        /// panel coordinates.
        /// </summary>
        public VisualElement EffectsLayer => Context.Layer.Root;

        protected override void OnCreate()
        {
            EventBus.Subscribe<PlayerXpChangedEvent>(OnXpChanged);
            EventBus.Subscribe<RunProgressChangedEvent>(OnRunProgressChanged);
            EventBus.Subscribe<CellLongPressEvent>(OnCellLongPress);
            Loc.LocaleChanged += OnLocaleChanged;
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnXpChanged);
            EventBus.Unsubscribe<RunProgressChangedEvent>(OnRunProgressChanged);
            EventBus.Unsubscribe<CellLongPressEvent>(OnCellLongPress);
            Loc.LocaleChanged -= OnLocaleChanged;
            DisposeBars();
        }

        protected override void OnBind(VisualElement root)
        {
            var menuButton = root.Q<Button>("menu-button");
            menuButton.OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new MenuOpenedEvent());
            });

            XpBar = new XpBarView(root);
            UpdateXpBarFromPlayer();

            _depthView = new DepthView(root);
            _depthView.SetDepth(_depth);

            // The bars subscribe to the game events themselves: drop the ones of the previous UI.
            DisposeBars();
            _skillBar = new SkillBarView(root.Q<VisualElement>("skill-bar"));
            _itemBar = new ItemBarView(root.Q<VisualElement>("item-bar"));
        }

        private void OnXpChanged(PlayerXpChangedEvent e)
        {
            XpBar?.SetState(e.Level, e.CurrentXp, e.XpToNextLevel);
        }

        private void OnRunProgressChanged(RunProgressChangedEvent e)
        {
            _depth = e.Depth;
            _depthView?.SetDepth(_depth);
        }

        private void OnLocaleChanged()
        {
            _depthView?.RefreshTexts();
        }

        /// <summary>
        /// A long tap (or right click) on a cell: opens the details of its entity and terrain. Needs
        /// something to show, and the game input free (the details are not stacked on another window).
        /// </summary>
        private void OnCellLongPress(CellLongPressEvent e)
        {
            if (!IsVisible || e.Cell == null) return;
            if (GameplayInput.IsBlocked || Context.Navigator.HasModal) return;

            var view = HudSettings.Current.EntityDetailsView;
            if (view == null) return;

            var character = e.Cell.GetContent() as Character;
            var terrain = e.Cell.Terrain;
            if (character == null && terrain == null) return;

            _ = Context.Navigator.ShowModal(view, new EntityDetailsRequest(character, terrain));
        }

        private void DisposeBars()
        {
            _skillBar?.Dispose();
            _skillBar = null;
            _itemBar?.Dispose();
            _itemBar = null;
        }

        /// <summary>
        /// Restores the bar and level state after a UI reload, reading the current
        /// PlayerCharacter state.
        /// </summary>
        private void UpdateXpBarFromPlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return;

            XpBar.SetState(player.Level, player.CurrentXp, player.XpToNextLevel);
        }
    }
}
