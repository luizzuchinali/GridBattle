using System;
using UnityEngine;

namespace GridBattle.Gameplay.Terrain
{
    /// <summary>A terrain definition at a grid position (editor/debug lists).</summary>
    [Serializable]
    public struct TerrainPlacement
    {
        [SerializeField]
        private TerrainDefinition terrain;

        [SerializeField]
        private Vector2Int position;

        public TerrainPlacement(TerrainDefinition terrain, Vector2Int position)
        {
            this.terrain = terrain;
            this.position = position;
        }

        public TerrainDefinition Terrain => terrain;
        public Vector2Int Position => position;
    }
}
