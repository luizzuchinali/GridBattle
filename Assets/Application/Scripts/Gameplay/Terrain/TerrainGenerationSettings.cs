using System;
using System.Collections.Generic;
using GridBattle.Data;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Terrain
{
    /// <summary>
    /// How many terrain cells of each kind a battle gets at a given depth (a band
    /// applies from <see cref="MinDepth"/> until the next band starts).
    /// </summary>
    [Serializable]
    public sealed class TerrainDepthBand
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("First depth (map row) this band applies to.")]
        private int minDepth = 1;

        [SerializeField]
        [Min(0)]
        private int obstacleMin;

        [SerializeField]
        [Min(0)]
        private int obstacleMax;

        [SerializeField]
        [Min(0)]
        private int hazardMin;

        [SerializeField]
        [Min(0)]
        private int hazardMax;

        [SerializeField]
        [Min(0)]
        private int bonusMin;

        [SerializeField]
        [Min(0)]
        private int bonusMax;

        public TerrainDepthBand()
        {
        }

        public TerrainDepthBand(int minDepth, int obstacleMin, int obstacleMax, int hazardMin, int hazardMax,
            int bonusMin, int bonusMax)
        {
            this.minDepth = minDepth;
            this.obstacleMin = obstacleMin;
            this.obstacleMax = obstacleMax;
            this.hazardMin = hazardMin;
            this.hazardMax = hazardMax;
            this.bonusMin = bonusMin;
            this.bonusMax = bonusMax;
        }

        public int MinDepth => minDepth;
        public int ObstacleMin => obstacleMin;
        public int ObstacleMax => Mathf.Max(obstacleMin, obstacleMax);
        public int HazardMin => hazardMin;
        public int HazardMax => Mathf.Max(hazardMin, hazardMax);
        public int BonusMin => bonusMin;
        public int BonusMax => Mathf.Max(bonusMin, bonusMax);
    }

    /// <summary>A terrain definition that can be drawn by the generator, with its relative weight.</summary>
    [Serializable]
    public sealed class TerrainPoolEntry
    {
        [SerializeField]
        private TerrainDefinition terrain;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance among the pool's entries (0 = never drawn).")]
        private float weight = 1f;

        public TerrainPoolEntry()
        {
        }

        public TerrainPoolEntry(TerrainDefinition terrain, float weight = 1f)
        {
            this.terrain = terrain;
            this.weight = weight;
        }

        public TerrainDefinition Terrain => terrain;
        public float Weight => weight;
    }

    /// <summary>
    /// Configuration of the terrain generator (GDD Mechanic 5): how much terrain
    /// each depth gets, which terrain can appear, and the safety margins that keep
    /// the grid playable. Quantities and positions are open questions in
    /// grid_e_terreno.md; the defaults are deliberately modest.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainGenerationSettings", menuName = "GridBattle/Terrain/Terrain Generation Settings",
        order = 1)]
    public class TerrainGenerationSettings : ScriptableObject, IGameSettings
    {
        [Header("Quantities by depth")]
        [SerializeField]
        [Tooltip("Bands by depth: the band with the highest Min Depth not above the battle's depth applies. No matching band = no terrain. Depths are balance depths, stretched to the map length (Map Generation Settings > Balance Floor Count).")]
        private List<TerrainDepthBand> bands = new()
        {
            new TerrainDepthBand(1, 0, 0, 0, 0, 0, 0),
            new TerrainDepthBand(4, 0, 2, 0, 1, 0, 1),
            new TerrainDepthBand(13, 1, 3, 1, 2, 0, 1),
        };

        [Header("Pools")]
        [SerializeField]
        [Tooltip("Obstacles that can be drawn (entries of another kind are ignored).")]
        private List<TerrainPoolEntry> obstaclePool = new();

        [SerializeField]
        [Tooltip("Hazard cells that can be drawn.")]
        private List<TerrainPoolEntry> hazardPool = new();

        [SerializeField]
        [Tooltip("Bonus cells that can be drawn.")]
        private List<TerrainPoolEntry> bonusPool = new();

        [Header("Safety")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Cells around the player's spawn (Chebyshev distance) that never get terrain.")]
        private int clearRadiusAroundPlayer = 1;

        [SerializeField]
        [Min(1)]
        [Tooltip("Obstacle layouts tried before giving up and dropping the obstacles (a layout is rejected if it splits the grid).")]
        private int maxAttempts = 20;

        public IReadOnlyList<TerrainDepthBand> Bands => bands;
        public IReadOnlyList<TerrainPoolEntry> ObstaclePool => obstaclePool;
        public IReadOnlyList<TerrainPoolEntry> HazardPool => hazardPool;
        public IReadOnlyList<TerrainPoolEntry> BonusPool => bonusPool;
        public int ClearRadiusAroundPlayer => clearRadiusAroundPlayer;
        public int MaxAttempts => Mathf.Max(1, maxAttempts);

        /// <summary>The band that applies to <paramref name="depth"/>, or null if none does.</summary>
        [CanBeNull]
        public TerrainDepthBand GetBand(int depth)
        {
            TerrainDepthBand best = null;
            foreach (var band in bands)
            {
                if (band == null || band.MinDepth > depth) continue;
                if (best == null || band.MinDepth >= best.MinDepth)
                    best = band;
            }

            return best;
        }

        /// <summary>The pool of a terrain kind.</summary>
        public IReadOnlyList<TerrainPoolEntry> GetPool(ETerrainKind kind) => kind switch
        {
            ETerrainKind.Obstacle => obstaclePool,
            ETerrainKind.Hazard => hazardPool,
            _ => bonusPool,
        };

#if UNITY_EDITOR
        private void OnValidate()
        {
            Check(obstaclePool, ETerrainKind.Obstacle);
            Check(hazardPool, ETerrainKind.Hazard);
            Check(bonusPool, ETerrainKind.Bonus);
        }

        private void Check(List<TerrainPoolEntry> pool, ETerrainKind kind)
        {
            foreach (var entry in pool)
            {
                if (entry?.Terrain != null && entry.Terrain.Kind != kind)
                    Debug.LogWarning($"{name}: {entry.Terrain.name} is a {entry.Terrain.Kind} but sits in the {kind} pool; it will be ignored.", this);
            }
        }
#endif
    }
}
