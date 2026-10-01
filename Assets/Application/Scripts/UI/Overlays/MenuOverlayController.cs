using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// In-game menu modal: tapping outside the panel closes it.
    /// </summary>
    [Preserve]
    public sealed class MenuOverlayController : ViewController
    {
        protected override void OnBind(VisualElement root)
        {
            var overlayBackground = root.Q<VisualElement>("overlay-background");
            overlayBackground.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.target == overlayBackground)
                    Close();
            });
        }
    }
}
