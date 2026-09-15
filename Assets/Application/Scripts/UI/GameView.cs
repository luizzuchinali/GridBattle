using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class GameView : View
    {
        private VisualElement _root;
        private VisualElement _container;

        protected override void Awake()
        {
            base.Awake();

            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        protected void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            _container = _root.Q<VisualElement>("container");
            _container.ToggleInClassList("display-none");
            _container.ToggleInClassList("translate-right");
        }

        protected override void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            _root = rootElement;
        }
    }
}