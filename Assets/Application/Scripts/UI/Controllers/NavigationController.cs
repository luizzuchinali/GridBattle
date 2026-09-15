using GridBattle.UI.Events;
using UnityEngine;

namespace GridBattle.UI.Controllers
{
    public class NavigationController : MonoBehaviour
    {
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
            FindAnyObjectByType<StartScreenView>().Hide();
            FindAnyObjectByType<MainMenuScreenView>().Show();
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            FindAnyObjectByType<MainMenuScreenView>().Hide();
            FindAnyObjectByType<GameScreenView>().Show();
        }
    }
}