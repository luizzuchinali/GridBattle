using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class BackgroundView : View
    {
        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
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