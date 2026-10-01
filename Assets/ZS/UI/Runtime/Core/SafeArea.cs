using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI
{
    /// <summary>
    /// Container that pads its content to the device safe area (notches, rounded
    /// corners, system bars). Use it as the root element of full-screen views.
    /// </summary>
    [UxmlElement]
    public partial class SafeArea : VisualElement
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public SafeArea()
        {
            if (panel != null)
            {
                panel.visualTree.RegisterCallback<GeometryChangedEvent>(UpdateGeometry);
            }
            else
            {
                RegisterCallback<GeometryChangedEvent>(UpdateGeometry);
            }
        }

        private void UpdateGeometry(GeometryChangedEvent evt)
        {
            // A panel is needed to convert screen coordinates.
            if (panel == null)
                return;

#if UNITY_EDITOR
            // RuntimePanelUtils.ScreenToPanel does not work with editor panels.
            if (panel.contextType == ContextType.Editor)
            {
                return;
            }
#endif

            var safeArea = Screen.safeArea;
            var screenHeight = (float)Screen.height;

            var safeAreaLeftTop = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMin, screenHeight - safeArea.yMax));
            var safeAreaRightBottom = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width - safeArea.xMax, safeArea.yMin));

            // Padding (not margins), so the element itself still covers the full screen.
            if (safeAreaLeftTop.x != 0)
                style.paddingLeft = safeAreaLeftTop.x;

            if (safeAreaLeftTop.y != 0)
                style.paddingTop = safeAreaLeftTop.y;

            if (safeAreaRightBottom.x != 0)
                style.paddingRight = safeAreaRightBottom.x;

            if (safeAreaRightBottom.y != 0)
                style.paddingBottom = safeAreaRightBottom.y;
        }
    }
}
