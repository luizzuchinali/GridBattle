using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
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
            // panel will needed to extract proper dimensions
            if (panel == null)
                return;

#if UNITY_EDITOR
            // RuntimePanelUtils.ScreenToPanel are not working with editor's panel
            if (panel.contextType == ContextType.Editor)
            {
                return;
            }
#endif

            var safeArea = UnityEngine.Screen.safeArea;
            var screenHeight = (float)UnityEngine.Screen.height;

            var safeAreaLeftTop = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safeArea.xMin, screenHeight - safeArea.yMax));
            var safeAreaRightBottom = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(UnityEngine.Screen.width - safeArea.xMax, safeArea.yMin));

            // setting padding. but you can experiment with margings as well
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

