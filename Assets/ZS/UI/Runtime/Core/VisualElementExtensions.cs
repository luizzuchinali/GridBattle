using UnityEngine.UIElements;

namespace ZS.UI
{
    public static class VisualElementExtensions
    {
        /// <summary>
        /// Shows or hides the element through an inline <c>display</c> style. Showing
        /// removes the inline value, so the USS-resolved display applies again.
        /// </summary>
        public static void SetDisplayed(this VisualElement element, bool displayed)
        {
            if (displayed)
                element.style.display = StyleKeyword.Null;
            else
                element.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Whether the element is not hidden by <see cref="SetDisplayed"/>.
        /// </summary>
        public static bool IsDisplayed(this VisualElement element)
        {
            return element.style.display.keyword != StyleKeyword.Undefined ||
                   element.style.display.value != DisplayStyle.None;
        }
    }
}
