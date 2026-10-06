using UnityEngine.UIElements;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// The run depth next to the XP bar (open question of interface 4.2: the suggestion is to keep it
    /// near the XP bar). The only place that knows its elements in the UXML.
    /// </summary>
    public sealed class DepthView
    {
        public const string HiddenClass = "hud-depth--hidden";

        private readonly VisualElement _root;
        private readonly Label _caption;
        private readonly Label _value;
        private int _depth;

        public DepthView(VisualElement root)
        {
            _root = root.Q<VisualElement>("hud-depth");
            _caption = root.Q<Label>("hud-depth-caption");
            _value = root.Q<Label>("hud-depth-value");
        }

        /// <summary>Whether the depth is shown (it is hidden while the depth is 0 or lower, see HudSettings).</summary>
        public bool IsVisible => _root != null && !_root.ClassListContains(HiddenClass);

        /// <summary>The depth shown.</summary>
        public int Depth => _depth;

        /// <summary>Shows <paramref name="depth"/>; it stays in place but invisible when there is none yet.</summary>
        public void SetDepth(int depth)
        {
            _depth = depth;
            if (_root == null) return;

            var visible = depth > 0 || !HudSettings.Current.HideDepthWhenZero;
            _root.EnableInClassList(HiddenClass, !visible);
            _value.text = depth.ToString();
            RefreshTexts();
        }

        /// <summary>Applies the current language (called again when it changes).</summary>
        public void RefreshTexts()
        {
            if (_caption != null)
                _caption.text = HudText.Get("hud.depth");
        }
    }
}
