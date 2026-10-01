using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    /// <summary>
    /// XP bar of the GameScreen. The only place that knows the bar's elements and
    /// measurements in the UXML: applies the state (level/progress) and exposes
    /// the track geometry for effects that target it.
    /// </summary>
    public sealed class XpBarView
    {
        // Measurements from GameScreenView.uxml: the frame (xp-bar-detail-2) is 112px
        // and the progress (xp-bar-progress) takes up to 110px, with a 1px border on
        // each side.
        private const float FrameWidth = 112f;
        private const float TrackMaxWidth = 110f;
        private const float TrackInset = 1f;

        private readonly VisualElement _progress;
        private readonly VisualElement _frame;
        private readonly Label _levelLabel;

        public XpBarView(VisualElement root)
        {
            _progress = root.Q<VisualElement>("xp-bar-progress");
            _frame = root.Q<VisualElement>("xp-bar-detail-2");
            _levelLabel = root.Q<Label>("current-level");
        }

        /// <summary>
        /// Converts XP progress (current / toNextLevel) into the bar width and
        /// updates the label with the current level.
        /// </summary>
        public void SetState(int level, int currentXp, int xpToNextLevel)
        {
            if (_progress == null) return;
            if (xpToNextLevel <= 0) return;

            var progress = Mathf.Clamp01((float)currentXp / xpToNextLevel);
            _progress.style.width = progress * TrackMaxWidth;

            if (_levelLabel != null)
                _levelLabel.text = level.ToString();
        }

        /// <summary>
        /// Bar track in panel coordinates. Derived from the frame by ratio, so it is
        /// immune to the panel scale.
        /// </summary>
        public bool TryGetTrack(out XpBarTrack track)
        {
            if (_frame == null)
            {
                track = default;
                return false;
            }

            var frameRect = _frame.worldBound;
            track = new XpBarTrack(
                frameRect.xMin + frameRect.width * (TrackInset / FrameWidth),
                frameRect.width * (TrackMaxWidth / FrameWidth),
                frameRect.center.y);
            return true;
        }
    }

    /// <summary>
    /// XP bar track in panel coordinates.
    /// </summary>
    public readonly struct XpBarTrack
    {
        private readonly float _left;
        private readonly float _width;
        private readonly float _centerY;

        public XpBarTrack(float left, float width, float centerY)
        {
            _left = left;
            _width = width;
            _centerY = centerY;
        }

        /// <summary>
        /// Point at the right edge of the progress when the bar is at
        /// <paramref name="progress"/> (0..1).
        /// </summary>
        public Vector2 GetPoint(float progress) => new(_left + progress * _width, _centerY);
    }
}
