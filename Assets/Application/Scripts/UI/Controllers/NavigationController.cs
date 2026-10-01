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
        /// Current screen of the flow. Serialized on the GameObject, so it survives
        /// domain reloads during Play Mode and is reapplied by the Views on UI reload.
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
        /// Central visibility responsibility: every screen view that doesn't match the
        /// current screen gets display-none. Auxiliary views (overlays, transitions)
        /// control their own visibility and are ignored.
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