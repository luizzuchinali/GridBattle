using System;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    /// <summary>Small helpers for UI Toolkit elements shared by the screens.</summary>
    public static class UiExtensions
    {
        /// <summary>
        /// Calls <paramref name="action"/> when the element is clicked (the <see cref="ClickEvent"/> of a tap or
        /// a mouse click). Screens use it instead of <c>Button.clicked</c> so a ClickEvent sent by code (tests) works
        /// the same as a real tap.
        /// </summary>
        public static void OnClick(this VisualElement element, Action action)
        {
            element.RegisterCallback<ClickEvent>(_ => action());
        }

        /// <summary>Sends a <see cref="ClickEvent"/> to the element, as a tap would.</summary>
        public static void SimulateClick(this VisualElement element)
        {
            using var click = ClickEvent.GetPooled();
            click.target = element;
            element.SendEvent(click);
        }
    }
}
