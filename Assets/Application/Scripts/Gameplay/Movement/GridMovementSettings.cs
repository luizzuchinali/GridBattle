using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// How grid entities animate when they change cells: small hops from the old
    /// position to the center of the new cell.
    /// </summary>
    [CreateAssetMenu(fileName = "GridMovementSettings", menuName = "GridBattle/Grid Movement Settings", order = 20)]
    public class GridMovementSettings : ScriptableObject
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Hops per cell of distance (the total is rounded, minimum 1).")]
        private float hopsPerCell = 2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Peak height of each hop, in world units (PPU 100: 0.04 = 4px).")]
        private float hopHeight = 0.04f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Duration of a single hop, in seconds.")]
        private float hopDuration = 0.12f;

        public float HopsPerCell => hopsPerCell;
        public float HopHeight => hopHeight;
        public float HopDuration => hopDuration;
    }
}
