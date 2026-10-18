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
            // A fixed timer instead of chaining on TransitionEndEvent: on device the first fade can start before the
            // label has a resolved style, no transition runs, no end event comes and the label stays invisible.
            // The period matches the USS transition (0.5 s delay + 0.5 s fade).
            tapToPlay.schedule.Execute(() => tapToPlay.ToggleInClassList("opacity-0")).StartingIn(100).Every(1000);

            root.RegisterCallback<PointerUpEvent>(e => { EventBus.Raise(new StartScreenTapEvent(e.position)); });
        }
    }
}
