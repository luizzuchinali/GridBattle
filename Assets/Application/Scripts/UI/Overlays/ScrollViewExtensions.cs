using UnityEngine.UIElements;

namespace GridBattle.UI.Overlays
{
    /// <summary>Small helpers for the scroll views of the modal windows.</summary>
    public static class ScrollViewExtensions
    {
        /// <summary>
        /// Shows the vertical scroller only when the content really overflows the viewport. The scroller of
        /// <c>Auto</c> visibility can stay on for content that exactly fits (a float rounding difference), which
        /// shows a useless handle and narrows the text. Call once when the view is bound.
        /// </summary>
        public static void HideScrollerWhenContentFits(this ScrollView scroll)
        {
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Refresh(scroll));
            scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => Refresh(scroll));
        }

        private static void Refresh(ScrollView scroll)
        {
            var overflow = scroll.contentContainer.layout.height - scroll.contentViewport.layout.height > 0.5f;
            var wanted = overflow ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Hidden;
            if (scroll.verticalScrollerVisibility != wanted)
                scroll.verticalScrollerVisibility = wanted;
        }
    }
}
