using GridBattle.UI.Events;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Background
{
    /// <summary>
    /// World-space background panel sized to the screen. Once a character is
    /// chosen it moves behind the game world (sorting order -1).
    /// </summary>
    [Preserve]
    public sealed class BackgroundController : ViewController
    {
        private const int BehindWorldSortingOrder = -1;

        protected override void OnCreate()
        {
            EventBus.Subscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterChoosenEvent>(OnCharacterChoosen);
        }

        protected override void OnBind(VisualElement root)
        {
            if (Context.Layer.TryGetComponent(out PanelRenderer panelRenderer))
                panelRenderer.worldSpaceSize = new Vector2(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
        }

        private void OnCharacterChoosen(CharacterChoosenEvent e)
        {
            Context.Layer.SortingOrder = BehindWorldSortingOrder;
        }
    }
}
