using System.IO;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Tools
{
    public enum EPlaceholderShape
    {
        Circle,
        Square,
        Diamond,
        Triangle,
        Cross,
        Ring
    }

    /// <summary>
    /// Generates simple pixel-art placeholder icons (a colored shape with a dark
    /// outline) for content that has no art yet: states, roles, terrain, talents,
    /// consumables, map nodes. Replace the PNG (same path) to swap in real art
    /// without touching any reference.
    /// </summary>
    public static class PlaceholderIcons
    {
        public const string Folder = "Assets/Application/Art/Sprites/Placeholders";

        /// <summary>Creates (or overwrites) a 16×16 icon and returns its sprite.</summary>
        public static Sprite Create(string subFolder, string fileName, Color color, EPlaceholderShape shape,
            int size = 16)
        {
            var folder = $"{Folder}/{subFolder}";
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{fileName}.png";

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var outline = Color.Lerp(color, Color.black, 0.65f);
            var center = (size - 1) / 2f;
            var radius = size / 2f - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var inside = Inside(shape, x - center, y - center, radius);
                    var insideInner = Inside(shape, x - center, y - center, radius - 1.25f);
                    texture.SetPixel(x, y, inside ? (insideInner ? color : outline) : Color.clear);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool Inside(EPlaceholderShape shape, float dx, float dy, float r)
        {
            if (r <= 0f) return false;
            var ax = Mathf.Abs(dx);
            var ay = Mathf.Abs(dy);
            return shape switch
            {
                EPlaceholderShape.Square => ax <= r && ay <= r,
                EPlaceholderShape.Diamond => ax + ay <= r,
                EPlaceholderShape.Triangle => dy >= -r && dy <= r && ax <= (dy + r) / 2f,
                EPlaceholderShape.Cross => (ax <= r * 0.35f && ay <= r) || (ay <= r * 0.35f && ax <= r),
                EPlaceholderShape.Ring => dx * dx + dy * dy <= r * r && dx * dx + dy * dy >= (r * 0.45f) * (r * 0.45f),
                _ => dx * dx + dy * dy <= r * r,
            };
        }
    }
}
