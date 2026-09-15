using GridBattle.UI.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class StartScreenView : View
    {
        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            var tapToPlayElement = root.Q<VisualElement>("tap-to-play-label");
            tapToPlayElement.RegisterCallback<TransitionEndEvent, VisualElement>(
                (_, target) => { target.ToggleInClassList("opacity-0"); }, tapToPlayElement);
            tapToPlayElement.schedule.Execute(() => tapToPlayElement.AddToClassList("opacity-0")).StartingIn(100);

            var container = root.Q<VisualElement>("container");
            container.RegisterCallback<PointerUpEvent>(e => { EventBus.Raise(new StartScreenTapEvent(e.position)); });
        }
    }
}