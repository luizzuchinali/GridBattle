using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    public class ScreenTransitionView : View
    {
        private const float CoverDuration = 0.3f;
        private const float RevealDuration = 0.3f;
        private const float CoverPauseDuration = 0.2f;

        private VisualElement _container;
        private Coroutine _transition;

        public bool IsTransitioning => _transition != null;

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            _container = root.Q<VisualElement>("container");
        }

        /// <summary>
        /// Slides a dark overlay in from the right until it covers the screen, invokes
        /// <paramref name="onCovered"/> to swap the views while covered, then keeps
        /// sliding the overlay out to the left to reveal the new view underneath.
        /// </summary>
        public void Transition(Action onCovered)
        {
            if (IsTransitioning)
            {
                return;
            }

            if (_container == null)
            {
                // Panel not loaded yet; swap views immediately as a fallback.
                onCovered?.Invoke();
                return;
            }

            _transition = StartCoroutine(TransitionRoutine(onCovered));
        }

        private IEnumerator TransitionRoutine(Action onCovered)
        {
            // 1. Reveal the overlay while parked off-screen to the right and let it render.
            Show();
            yield return null;

            // 2. Slide in from the right until the screen is fully covered.
            _container.RemoveFromClassList("translate-right");
            yield return new WaitForSeconds(CoverDuration);
            yield return new WaitForSeconds(CoverPauseDuration);

            // 3. Screen fully covered: swap the views underneath.
            onCovered?.Invoke();

            // 4. Keep sliding out to the left, revealing the new view.
            _container.AddToClassList("translate-left");
            yield return new WaitForSeconds(RevealDuration);

            // 5. Hide and park the overlay off-screen right for the next transition.
            Hide();
            _container.RemoveFromClassList("translate-left");
            _container.AddToClassList("translate-right");
            _transition = null;
        }
    }
}
