using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GameScreenView : View
    {
        protected override UIScreen? Screen => UIScreen.Game;

        /// <summary>
        /// The screen's XP bar (null until the UI loads).
        /// </summary>
        public XpBarView XpBar { get; private set; }

        /// <summary>
        /// Layer where floating effects (e.g. XP orbs) are added: the panel root, in
        /// panel coordinates.
        /// </summary>
        public VisualElement EffectsLayer => Root;

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<PlayerXpChangedEvent>(OnXpChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnXpChanged);
        }

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            var menuButton = root.Q<Button>("menu-button");
            menuButton.RegisterCallback<PointerUpEvent>(_ => { EventBus.Raise(new MenuOpenedEvent()); });

            XpBar = new XpBarView(root);
            UpdateXpBarFromPlayer();
        }

        private void OnXpChanged(PlayerXpChangedEvent e)
        {
            XpBar?.SetState(e.Level, e.CurrentXp, e.XpToNextLevel);
        }

        /// <summary>
        /// Restores the bar and level state after a UI reload, reading the current
        /// PlayerCharacter state.
        /// </summary>
        private void UpdateXpBarFromPlayer()
        {
            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return;

            XpBar.SetState(player.Level, player.CurrentXp, player.XpToNextLevel);
        }
    }
}
