using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class Background : Screen
    {
        protected override void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            panelRenderer.worldSpaceSize = new Vector2(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
        }
        
        protected override void Awake()
        {
            base.Awake();

            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            PanelRenderer.sortingOrder = -1;
        }
    }
}