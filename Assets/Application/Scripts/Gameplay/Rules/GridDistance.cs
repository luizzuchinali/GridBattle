using UnityEngine;

namespace GridBattle.Gameplay.Rules
{
    public enum EDistanceMetric
    {
        /// <summary>Straight-line distance (diagonal ≈ 1.41).</summary>
        Euclidean,

        /// <summary>|dx| + |dy| (diamond-shaped ranges).</summary>
        Manhattan,

        /// <summary>max(|dx|, |dy|) (square ranges, diagonals count as 1).</summary>
        Chebyshev
    }

    /// <summary>The single place where grid distances are measured.</summary>
    public static class GridDistance
    {
        public static float Measure(EDistanceMetric metric, Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            return metric switch
            {
                EDistanceMetric.Manhattan => Mathf.Abs(delta.x) + Mathf.Abs(delta.y),
                EDistanceMetric.Chebyshev => Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)),
                _ => Vector2Int.Distance(from, to),
            };
        }

        public static bool IsWithin(EDistanceMetric metric, Vector2Int from, Vector2Int to, int range) =>
            Measure(metric, from, to) <= range;
    }
}
