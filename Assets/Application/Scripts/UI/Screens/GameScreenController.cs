using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// In-game HUD: menu button and XP bar.
    /// </summary>
    [Preserve]
    public sealed class GameScreenController : ViewController
    {
        /// <summary>
        /// The screen's XP bar (null until the UI loads).
        /// </summary>
        public XpBarView XpBar { get; private set; }

        /// <summary>
        /// Layer where floating effects (e.g. XP orbs) are added: the panel root, in
        /// panel coordinates.
        /// </summary>
        public VisualElement EffectsLayer => Context.Layer.Root;

        protected override void OnCreate()
        {
            EventBus.Subscribe<PlayerXpChangedEvent>(OnXpChanged);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnXpChanged);
        }

        protected override void OnBind(VisualElement root)
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
            var player = Object.FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return;

            XpBar.SetState(player.Level, player.CurrentXp, player.XpToNextLevel);
        }
    }
}
