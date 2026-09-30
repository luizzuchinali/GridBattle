using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Managers;
using GridBattle.UI.Events;
using UnityEngine;

namespace GridBattle.UI.Controllers
{
    using GridBattle.UI;
    public class NavigationController : MonoBehaviour
    {
        [SerializeField]
        private UIScreen currentScreen = UIScreen.Start;

        /// <summary>
        /// Tela atual do fluxo. Serializado no GameObject, então sobrevive ao
        /// domain reload durante o Play Mode e é reaplicado pelas Views no reload da UI.
        /// </summary>
        public UIScreen CurrentScreen => currentScreen;

        public void OnEnable()
        {
            EventBus.Subscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
            ApplyScreenVisibility();
        }

        public void OnDisable()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        /// <summary>
        /// Responsabilidade central de visibilidade: todas as views de tela que não
        /// correspondem à tela atual ficam com display-none. Views auxiliares
        /// (overlays, transições) controlam a própria visibilidade e são ignoradas.
        /// </summary>
        public void ApplyScreenVisibility()
        {
            foreach (var view in FindObjectsByType<View>())
            {
                if (!view.IsScreenView) continue;

                if (view.BelongsToScreen(CurrentScreen))
                    view.Show();
                else
                    view.Hide();
            }
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            var screenTransitionView = FindAnyObjectByType<ScreenTransitionView>();
            screenTransitionView.Transition(() =>
            {
                currentScreen = UIScreen.MainMenu;
                ApplyScreenVisibility();
            });
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            var screenTransitionView = FindAnyObjectByType<ScreenTransitionView>();
            screenTransitionView.Transition(() =>
            {
                currentScreen = UIScreen.Game;
                ApplyScreenVisibility();
            });

            GameStateManager.Instance.StartRun(e.Character);
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (e.Character is not PlayerCharacter) return;

            var screenTransitionView = FindAnyObjectByType<ScreenTransitionView>();
            screenTransitionView.Transition(() =>
            {
                currentScreen = UIScreen.MainMenu;
                ApplyScreenVisibility();
            });
        }
    }
}