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
        }

        public void OnDisable()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            var screenTransitionView = FindAnyObjectByType<ScreenTransitionView>();
            screenTransitionView.Transition(() =>
            {
                currentScreen = UIScreen.MainMenu;
                FindAnyObjectByType<StartScreenView>().Hide();
                FindAnyObjectByType<MainMenuScreenView>().Show();
            });
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            var screenTransitionView = FindAnyObjectByType<ScreenTransitionView>();
            screenTransitionView.Transition(() =>
            {
                currentScreen = UIScreen.Game;
                FindAnyObjectByType<MainMenuScreenView>().Hide();
                FindAnyObjectByType<GameScreenView>().Show();
            });
            
            GameStateManager.StartRun(e.Character);
        }
    }
}