using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation.Transitions
{
    /// <summary>
    /// Generic USS-driven transition (fade, slide…): the incoming view is shown
    /// with <c>enterFromClass</c>, which is removed on the next frame so its USS
    /// transition animates it in; the outgoing view gets <c>exitToClass</c> and is
    /// hidden after <c>duration</c>. The animated properties and timings live in
    /// USS (see ZSTransitions.uss).
    /// </summary>
    [CreateAssetMenu(fileName = "UssClassTransition", menuName = "ZS/UI/Transitions/USS Class Transition", order = 1)]
    public class UssClassTransition : ViewTransition
    {
        [SerializeField]
        [Tooltip("Initial state of the incoming view (e.g. zs-fade--hidden).")]
        private string enterFromClass = "zs-fade--hidden";

        [SerializeField]
        [Tooltip("Final state of the outgoing view (e.g. zs-fade--hidden).")]
        private string exitToClass = "zs-fade--hidden";

        [SerializeField]
        [Min(0f)]
        [Tooltip("Should match the USS transition-duration.")]
        private float duration = 0.2f;

        public override async Awaitable Run(TransitionScope scope)
        {
            var cancellation = scope.CancellationToken;

            SetClass(scope.ToElements, enterFromClass, true);
            scope.ShowTo();
            await Awaitable.NextFrameAsync(cancellation);

            SetClass(scope.ToElements, enterFromClass, false);
            SetClass(scope.FromElements, exitToClass, true);
            if (duration > 0f)
                await Awaitable.WaitForSecondsAsync(duration, cancellation);

            scope.HideFrom();
            SetClass(scope.FromElements, exitToClass, false);
        }

        private static void SetClass(IReadOnlyList<VisualElement> elements, string className, bool enabled)
        {
            if (string.IsNullOrEmpty(className)) return;
            foreach (var element in elements)
                element.EnableInClassList(className, enabled);
        }
    }
}
