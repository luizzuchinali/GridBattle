using System.Collections.Generic;
using GridBattle.Managers;
using UnityEngine;

namespace GridBattle.Gameplay.Forecast
{
    /// <summary>
    /// The small pixel art arrows of the forecast (pushes and pulls), drawn in code so they need no asset: 8 directions,
    /// 10x10 pixels with a dark outline, at the game's pixels per unit. The straight and diagonal shapes are turned by
    /// whole quarter turns of the pixel grid (never by rotating the sprite), so they stay pixel perfect.
    /// </summary>
    public static class ForecastSprites
    {
        private const int Size = 10;

        // Pointing right (straight) and up-right (diagonal), in an 8x8 box drawn with a 1 pixel margin; y grows upward.
        private static readonly Vector2Int[] Straight =
        {
            new(0, 3), new(1, 3), new(2, 3), new(3, 3), new(0, 4), new(1, 4), new(2, 4), new(3, 4),
            new(4, 1), new(4, 2), new(4, 3), new(4, 4), new(4, 5), new(4, 6),
            new(5, 2), new(5, 3), new(5, 4), new(5, 5),
            new(6, 3), new(6, 4),
        };

        private static readonly Vector2Int[] Diagonal =
        {
            new(1, 1), new(2, 2), new(3, 3), new(4, 4), new(5, 5),
            new(2, 1), new(3, 2), new(4, 3), new(5, 4),
            new(6, 6), new(3, 6), new(4, 6), new(5, 6), new(6, 3), new(6, 4), new(6, 5),
        };

        private static readonly Dictionary<Vector2Int, Sprite> Cache = new();

        /// <summary>
        /// The arrow for a grid direction (x right, y down, each component in -1..1), or null for no direction.
        /// </summary>
        public static Sprite GetArrow(Vector2Int gridDirection)
        {
            if (gridDirection == Vector2Int.zero) return null;
            if (Cache.TryGetValue(gridDirection, out var cached) && cached != null) return cached;

            // The grid's y grows downward; the texture's grows upward.
            var dir = new Vector2Int(Mathf.Clamp(gridDirection.x, -1, 1), -Mathf.Clamp(gridDirection.y, -1, 1));
            var diagonal = dir.x != 0 && dir.y != 0;
            var turns = diagonal
                ? dir switch { { x: 1, y: 1 } => 0, { x: -1, y: 1 } => 1, { x: -1, y: -1 } => 2, _ => 3 }
                : dir switch { { x: 1 } => 0, { y: 1 } => 1, { x: -1 } => 2, _ => 3 };

            var fill = new bool[Size, Size];
            foreach (var pixel in diagonal ? Diagonal : Straight)
            {
                var p = pixel;
                for (var i = 0; i < turns; i++)
                    p = new Vector2Int(7 - p.y, p.x); // a quarter turn counterclockwise inside the 8x8 box
                fill[p.x + 1, p.y + 1] = true;
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "ForecastArrow",
            };

            var outline = new Color(0.08f, 0.06f, 0.1f, 1f);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var color = fill[x, y] ? Color.white : IsNextToFill(fill, x, y) ? outline : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), GameConfigManager.Ppu);
            sprite.name = "ForecastArrow";
            Cache[gridDirection] = sprite;
            return sprite;
        }

        private static bool IsNextToFill(bool[,] fill, int x, int y)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if ((dx == 0) == (dy == 0)) continue;

                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && fill[nx, ny])
                        return true;
                }
            }

            return false;
        }

        /// <summary>Play Mode runs without a domain reload: the textures do not survive it.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();
    }
}
