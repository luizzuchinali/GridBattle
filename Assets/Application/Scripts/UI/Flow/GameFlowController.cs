using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Managers;
using GridBattle.UI.Events;
using UnityEngine;
using ZS.UI.Navigation;

namespace GridBattle.UI.Flow
{
    /// <summary>
    /// Game-specific screen flow: translates game/UI events into navigation on
    /// the UIRoot (Start → MainMenu → Game → MainMenu on death, menu modal).
    /// </summary>
    [RequireComponent(typeof(UIRoot))]
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField]
        private ViewDefinition mainMenuScreen;

        [SerializeField]
        private ViewDefinition gameScreen;

        [SerializeField]
        private ViewDefinition menuOverlay;

        private UIRoot _uiRoot;

        private Navigator Navigator => _uiRoot.Navigator;

        private void Awake()
        {
            _uiRoot = GetComponent<UIRoot>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Subscribe<MenuOpenedEvent>(OnMenuOpened);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StartScreenTapEvent>(OnStartScreenTap);
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Unsubscribe<MenuOpenedEvent>(OnMenuOpened);
        }

        private void OnStartScreenTap(StartScreenTapEvent e)
        {
            Navigator.Replace(mainMenuScreen);
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            // The run starts right away, even if the screen transition request is
            // ignored because another transition is running.
            Navigator.Replace(gameScreen);
            GameStateManager.Instance.StartRun(e.Character);
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (e.Character is not PlayerCharacter) return;

            Navigator.Replace(mainMenuScreen);
        }

        private void OnMenuOpened(MenuOpenedEvent e)
        {
            Navigator.ShowModal(menuOverlay);
        }
    }
}
