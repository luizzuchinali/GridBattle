using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay.Run;
using UnityEngine;

namespace GridBattle.Gameplay.Terrain
{
    /// <summary>
    /// Deterministic terrain generation (GDD Mechanic 5): from a seed (the caller's
    /// <see cref="Rng"/>, e.g. a stream derived from the run seed and the map node)
    /// and the depth, it picks where obstacles, hazards and bonus cells go. Pure:
    /// the only source of randomness is the <see cref="Rng"/> argument. The result
    /// never touches the player spawn or the clear radius around it, and the
    /// obstacles never split the grid: every non-obstacle cell stays connected to
    /// the player spawn (4-neighbor flood fill).
    /// </summary>
    public static class TerrainGenerator
    {
        private static readonly Vector2Int[] Neighbors =
        {
            new(0, -1), new(-1, 0), new(1, 0), new(0, 1)
        };

        /// <summary>
        /// Generates the terrain of one battle. Specs are ordered obstacles, then
        /// hazards, then bonus cells. Returns an empty list when the depth has no
        /// band or the pools are empty.
        /// </summary>
        public static List<TerrainCellSpec> Generate(Rng rng, int depth, int width, int height,
            Vector2Int playerPos, TerrainGenerationSettings settings)
        {
            var result = new List<TerrainCellSpec>();
            if (rng == null || settings == null || width <= 0 || height <= 0) return result;

            var band = settings.GetBand(depth);
            if (band == null) return result;

            // The counts are always drawn in the same order so the stream stays aligned.
            var obstacleCount = DrawCount(rng, band.ObstacleMin, band.ObstacleMax);
            var hazardCount = DrawCount(rng, band.HazardMin, band.HazardMax);
            var bonusCount = DrawCount(rng, band.BonusMin, band.BonusMax);
            if (!HasEntries(settings.GetPool(ETerrainKind.Obstacle), ETerrainKind.Obstacle)) obstacleCount = 0;
            if (!HasEntries(settings.GetPool(ETerrainKind.Hazard), ETerrainKind.Hazard)) hazardCount = 0;
            if (!HasEntries(settings.GetPool(ETerrainKind.Bonus), ETerrainKind.Bonus)) bonusCount = 0;

            var candidates = new List<Vector2Int>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!IsWithinClearRadius(cell, playerPos, settings.ClearRadiusAroundPlayer))
                        candidates.Add(cell);
                }
            }

            var obstacles = PlaceObstacles(rng, width, height, playerPos, candidates, obstacleCount,
                settings.MaxAttempts);

            var free = new List<Vector2Int>(candidates.Count);
            foreach (var cell in candidates)
            {
                if (!obstacles.Contains(cell))
                    free.Add(cell);
            }

            rng.Shuffle(free);
            hazardCount = Mathf.Min(hazardCount, free.Count);
            bonusCount = Mathf.Min(bonusCount, free.Count - hazardCount);

            foreach (var cell in obstacles)
                Add(result, cell, rng, settings.GetPool(ETerrainKind.Obstacle), ETerrainKind.Obstacle);
            for (var i = 0; i < hazardCount; i++)
                Add(result, free[i], rng, settings.GetPool(ETerrainKind.Hazard), ETerrainKind.Hazard);
            for (var i = 0; i < bonusCount; i++)
                Add(result, free[hazardCount + i], rng, settings.GetPool(ETerrainKind.Bonus), ETerrainKind.Bonus);

            return result;
        }

        /// <summary>
        /// Whether <paramref name="cell"/> is inside the area around the player
        /// spawn that stays free of terrain (Chebyshev distance).
        /// </summary>
        public static bool IsWithinClearRadius(Vector2Int cell, Vector2Int playerPos, int radius)
        {
            return Mathf.Max(Mathf.Abs(cell.x - playerPos.x), Mathf.Abs(cell.y - playerPos.y)) <= radius;
        }

        /// <summary>
        /// Whether every cell that is not in <paramref name="blocked"/> can be reached
        /// from <paramref name="start"/> through non-blocked cells (4-neighbor
        /// moves). False if the start itself is blocked or outside the grid.
        /// </summary>
        public static bool IsConnected(int width, int height, Vector2Int start,
            ICollection<Vector2Int> blocked)
        {
            if (start.x < 0 || start.x >= width || start.y < 0 || start.y >= height) return false;

            var isBlocked = new bool[width, height];
            var blockedCount = 0;
            foreach (var cell in blocked)
            {
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) continue;
                if (isBlocked[cell.x, cell.y]) continue;

                isBlocked[cell.x, cell.y] = true;
                blockedCount++;
            }

            if (isBlocked[start.x, start.y]) return false;

            var visited = new bool[width, height];
            var queue = new Queue<Vector2Int>();
            visited[start.x, start.y] = true;
            queue.Enqueue(start);
            var reached = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                reached++;
                foreach (var step in Neighbors)
                {
                    var next = current + step;
                    if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height) continue;
                    if (visited[next.x, next.y] || isBlocked[next.x, next.y]) continue;

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return reached == width * height - blockedCount;
        }

        /// <summary>
        /// Cells where a character can be placed at battle start: inside the grid,
        /// not blocked by terrain and (unless <paramref name="includeHazards"/>) not
        /// a hazard, in row-then-column order. The caller removes the player spawn
        /// and anything else it reserves. Terrain ids are resolved through the
        /// <see cref="GameDatabase"/>; unknown ids count as no terrain.
        /// </summary>
        public static List<Vector2Int> GetSpawnableCells(int width, int height,
            IReadOnlyList<TerrainCellSpec> terrain, bool includeHazards = false)
        {
            var definitions = new Dictionary<Vector2Int, TerrainDefinition>();
            if (terrain != null && GameDatabase.Instance != null)
            {
                foreach (var spec in terrain)
                {
                    var definition = GameDatabase.Instance.Get<TerrainDefinition>(spec.TerrainId);
                    if (definition != null)
                        definitions[new Vector2Int(spec.X, spec.Y)] = definition;
                }
            }

            var cells = new List<Vector2Int>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (definitions.TryGetValue(cell, out var definition))
                    {
                        if (definition.BlocksMovement) continue;
                        if (!includeHazards && definition.Kind == ETerrainKind.Hazard) continue;
                    }

                    cells.Add(cell);
                }
            }

            return cells;
        }

        private static List<Vector2Int> PlaceObstacles(Rng rng, int width, int height, Vector2Int playerPos,
            List<Vector2Int> candidates, int count, int maxAttempts)
        {
            count = Mathf.Min(count, candidates.Count);
            if (count <= 0) return new List<Vector2Int>();

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var shuffled = new List<Vector2Int>(candidates);
                rng.Shuffle(shuffled);
                var chosen = shuffled.GetRange(0, count);
                if (IsConnected(width, height, playerPos, chosen))
                    return chosen;
            }

            // No layout kept the grid whole: play without obstacles.
            return new List<Vector2Int>();
        }

        private static int DrawCount(Rng rng, int min, int max)
        {
            return rng.Range(min, max + 1);
        }

        private static bool HasEntries(IReadOnlyList<TerrainPoolEntry> pool, ETerrainKind kind)
        {
            foreach (var entry in pool)
            {
                if (entry != null && entry.Terrain != null && entry.Terrain.Kind == kind && entry.Weight > 0f)
                    return true;
            }

            return false;
        }

        private static void Add(List<TerrainCellSpec> result, Vector2Int cell, Rng rng,
            IReadOnlyList<TerrainPoolEntry> pool, ETerrainKind kind)
        {
            var definition = Pick(rng, pool, kind);
            if (definition == null) return;

            result.Add(new TerrainCellSpec { X = cell.x, Y = cell.y, TerrainId = definition.Id });
        }

        private static TerrainDefinition Pick(Rng rng, IReadOnlyList<TerrainPoolEntry> pool, ETerrainKind kind)
        {
            var entries = new List<TerrainPoolEntry>();
            var weights = new List<float>();
            foreach (var entry in pool)
            {
                if (entry == null || entry.Terrain == null || entry.Terrain.Kind != kind) continue;

                entries.Add(entry);
                weights.Add(entry.Weight);
            }

            var index = rng.WeightedIndex(weights);
            return index < 0 ? null : entries[index].Terrain;
        }
    }
}
