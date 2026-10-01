using GridBattle.UI.Events;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Title screen: blinking "tap to play" label; a tap anywhere raises
    /// StartScreenTapEvent.
    /// </summary>
    [Preserve]
    public sealed class StartScreenController : ViewController
    {
        protected override void OnBind(VisualElement root)
        {
            var tapToPlay = root.Q<VisualElement>("tap-to-play-label");
            tapToPlay.RegisterCallback<TransitionEndEvent, VisualElement>(
                (_, target) => { target.ToggleInClassList("opacity-0"); }, tapToPlay);
            tapToPlay.schedule.Execute(() => tapToPlay.AddToClassList("opacity-0")).StartingIn(100);

            root.RegisterCallback<PointerUpEvent>(e => { EventBus.Raise(new StartScreenTapEvent(e.position)); });
        }
    }
}
