using GridBattle.UI.Events;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class StartView : View
    {
        private VisualElement _tapToPlayElement;
        private VisualElement _container;

        protected override void ReloadUICallback(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            _tapToPlayElement = rootElement.Q<VisualElement>("tap-to-play-label");
            _tapToPlayElement.RegisterCallback<TransitionEndEvent, VisualElement>(
                (_, target) => { target.ToggleInClassList("opacity-0"); }, _tapToPlayElement);
            _tapToPlayElement.schedule.Execute(() => _tapToPlayElement.AddToClassList("opacity-0")).StartingIn(100);

            _container = rootElement.Q<VisualElement>("container");
            _container.RegisterCallback<PointerUpEvent, VisualElement>((e, target) =>
            {
                target.ToggleInClassList("display-none");
                target.ToggleInClassList("translate-right");
                EventBus.Raise(new StartScreenTapEvent(e.position));
            }, _container);
        }
    }
}