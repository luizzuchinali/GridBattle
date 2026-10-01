using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation.Transitions
{
    /// <summary>
    /// A cover element (in its own layer) slides over the screen, the views are
    /// swapped while it is covered, then it slides away. The movement comes from
    /// USS classes (with a USS transition on the cover element):
    /// <list type="number">
    /// <item>show the cover, wait one frame;</item>
    /// <item>remove <c>parkedClass</c> and wait cover + hold durations;</item>
    /// <item>swap the views, add <c>exitClass</c> and wait the reveal duration;</item>
    /// <item>hide the cover and park it again.</item>
    /// </list>
    /// If the cover is not available (layer not loaded) the swap is immediate.
    /// </summary>
    [CreateAssetMenu(fileName = "CoverTransition", menuName = "ZS/UI/Transitions/Cover Transition", order = 0)]
    public class CoverTransition : ViewTransition
    {
        [SerializeField]
        [Tooltip("Layer that holds the cover element.")]
        private UILayerDefinition coverLayer;

        [SerializeField]
        private string coverElementName = "container";

        [SerializeField]
        [Tooltip("Class that parks the cover off-screen before covering.")]
        private string parkedClass = "translate-right";

        [SerializeField]
        [Tooltip("Class that moves the cover off-screen after the swap.")]
        private string exitClass = "translate-left";

        [SerializeField]
        [Min(0f)]
        private float coverDuration = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Extra time fully covered before swapping.")]
        private float holdDuration = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float revealDuration = 0.3f;

        public override async Awaitable Run(TransitionScope scope)
        {
            var cover = FindCover(scope);
            if (cover == null)
            {
                scope.Swap();
                return;
            }

            var cancellation = scope.CancellationToken;

            cover.SetDisplayed(true);
            await Awaitable.NextFrameAsync(cancellation);

            cover.RemoveFromClassList(parkedClass);
            await Awaitable.WaitForSecondsAsync(coverDuration, cancellation);
            await Awaitable.WaitForSecondsAsync(holdDuration, cancellation);

            scope.Swap();

            cover.AddToClassList(exitClass);
            await Awaitable.WaitForSecondsAsync(revealDuration, cancellation);

            cover.SetDisplayed(false);
            cover.RemoveFromClassList(exitClass);
            cover.AddToClassList(parkedClass);
        }

        private VisualElement FindCover(TransitionScope scope)
        {
            var layer = scope.Root != null ? scope.Root.GetLayer(coverLayer) : null;
            return layer?.Root?.Q<VisualElement>(coverElementName);
        }
    }
}
