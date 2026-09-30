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
        private const float XpBarMaxWidth = 110f;

        private VisualElement _xpBarProgress;
        private Label _currentLevelLabel;

        protected override UIScreen? Screen => UIScreen.Game;

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

            _xpBarProgress = root.Q<VisualElement>("xp-bar-progress");
            _currentLevelLabel = root.Q<Label>("current-level");
            UpdateXpBarFromPlayer();
        }

        private void OnXpChanged(PlayerXpChangedEvent e)
        {
            UpdateXpBar(e.Level, e.CurrentXp, e.XpToNextLevel);
        }

        /// <summary>
        /// Restaura o estado da barra e do nível após um reload da UI, lendo o
        /// estado atual do PlayerCharacter.
        /// </summary>
        private void UpdateXpBarFromPlayer()
        {
            if (_xpBarProgress == null) return;

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return;

            UpdateXpBar(player.Level, player.CurrentXp, player.XpToNextLevel);
        }

        /// <summary>
        /// Converte o progresso de XP (current / toNextLevel) em % e aplica na
        /// largura da barra (0 a XpBarMaxWidth px). Atualiza também o label
        /// #current-level com o nível atual.
        /// </summary>
        private void UpdateXpBar(int level, int currentXp, int xpToNextLevel)
        {
            if (_xpBarProgress == null) return;
            if (xpToNextLevel <= 0) return;

            var progress = Mathf.Clamp01((float)currentXp / xpToNextLevel);
            _xpBarProgress.style.width = progress * XpBarMaxWidth;

            if (_currentLevelLabel != null)
                _currentLevelLabel.text = level.ToString();
        }
    }
}