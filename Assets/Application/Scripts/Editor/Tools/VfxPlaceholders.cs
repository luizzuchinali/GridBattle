using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GridBattle.Data;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Movement;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Tools
{
    /// <summary>
    /// PLACEHOLDER skill and item visuals, drawn by code like <see cref="PlaceholderIcons"/>: pixel-art frames that
    /// fit a 24x24 square (the cell's width, so the same art serves rows and columns), saved under
    /// <c>Art/Sprites/Placeholders/Vfx</c>, and one <see cref="ActionVisualDefinition"/> per skill and for the Bomb
    /// under <c>Settings/Vfx/ActionVisuals</c>, linked to the skill or item when it has no visual yet. Re-running it
    /// redraws the frames and rewrites the placeholder visuals; replace them with real art without code changes.
    /// Menu <c>GridBattle/Vfx/Generate Placeholder Visuals</c>.
    /// </summary>
    public static class VfxPlaceholders
    {
        private const string SpriteFolder = PlaceholderIcons.Folder + "/Vfx";
        private const string VisualFolder = "Assets/Application/Settings/Vfx/ActionVisuals";
        private const int Box = 24;

        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 White = new(255, 255, 255, 255);
        private static readonly Color32 Yellow = new(255, 222, 89, 255);
        private static readonly Color32 Orange = new(255, 140, 38, 255);
        private static readonly Color32 Red = new(214, 54, 40, 255);
        private static readonly Color32 Cyan = new(120, 220, 255, 255);
        private static readonly Color32 Blue = new(60, 140, 230, 255);
        private static readonly Color32 Green = new(110, 220, 80, 255);
        private static readonly Color32 DarkGreen = new(50, 140, 50, 255);
        private static readonly Color32 Grey = new(170, 170, 180, 255);
        private static readonly Color32 DarkGrey = new(80, 80, 90, 255);
        private static readonly Color32 Brown = new(130, 80, 40, 255);

        [MenuItem("GridBattle/Vfx/Generate Placeholder Visuals")]
        private static void GenerateFromMenu()
        {
            Debug.Log(Generate());
        }

        /// <summary>Draws the frames, writes the visuals and links them. Returns a report.</summary>
        public static string Generate()
        {
            Directory.CreateDirectory(SpriteFolder);
            Directory.CreateDirectory(VisualFolder);

            var art = new Dictionary<string, List<Sprite>>
            {
                ["impact"] = Frames("impact", DrawImpact),
                ["fire"] = Frames("fire", DrawFire),
                ["frost"] = Frames("frost", DrawFrost),
                ["poison"] = Frames("poison", (c, f) => DrawCloud(c, f, Green, DarkGreen)),
                ["smoke"] = Frames("smoke", (c, f) => DrawCloud(c, f, Grey, DarkGrey)),
                ["lightning"] = Frames("lightning", DrawLightning),
                ["wind"] = Frames("wind", DrawWind),
                ["slash"] = Frames("slash", DrawSlash),
                ["shockwave"] = Frames("shockwave", DrawShockwave),
                ["slam"] = Frames("slam", DrawSlam),
                ["orb_fire"] = Frames("orb_fire", (c, f) => DrawOrb(c, f, Orange, Yellow), 2),
                ["bomb"] = Frames("bomb", DrawBomb, 2),
                ["dagger"] = Frames("dagger", DrawDagger, 1),
                ["hook"] = Frames("hook", DrawHook, 1),
            };

            var report = new StringBuilder();
            var lunge = ECasterGesture.Lunge;
            var glow = ECasterGesture.Glow;
            Link(report, Skill("Strike"), Visual("Strike", lunge, Color.white, null, art["slash"], EVisualCellOrder.Sweep, step: 0.06f, facing: EVisualFacing.CasterToTarget));
            Link(report, Skill("ShieldCharge"), Visual("ShieldCharge", ECasterGesture.None, Color.white, null, art["impact"], EVisualCellOrder.AwayFromCaster));
            Link(report, Skill("ShieldBash"), Visual("ShieldBash", lunge, Color.white, null, art["impact"], EVisualCellOrder.Simultaneous));
            Link(report, Skill("FrostNova"), Visual("FrostNova", glow, Cyan, null, art["frost"], EVisualCellOrder.RippleFromTarget, step: 0.06f));
            Link(report, Skill("Fireball"), Visual("Fireball", glow, Orange, art["orb_fire"], art["fire"], EVisualCellOrder.RippleFromTarget, arc: 0.5f, speed: 9f));
            Link(report, Skill("LightningStorm"), Visual("LightningStorm", glow, Yellow, null, art["lightning"], EVisualCellOrder.AwayFromCaster, step: 0.07f, drop: 24, impactFrame: 0));
            Link(report, Skill("Gust"), Visual("Gust", glow, White, null, art["wind"], EVisualCellOrder.RippleFromTarget, step: 0.04f, facing: EVisualFacing.AwayFromTarget));
            Link(report, Skill("PoisonedDagger"), Visual("PoisonedDagger", ECasterGesture.None, Color.white, art["dagger"], art["poison"], EVisualCellOrder.AwayFromCaster, speed: 14f, projectileFaces: true, linger: 0.25f));
            Link(report, Skill("ThrowingDagger"), Visual("ThrowingDagger", ECasterGesture.None, Color.white, art["dagger"], art["impact"], EVisualCellOrder.Simultaneous, speed: 14f, projectileFaces: true));
            Link(report, Skill("PoisonLine"), Visual("PoisonLine", glow, Green, null, art["poison"], EVisualCellOrder.AwayFromCaster, linger: 0.3f));
            Link(report, Skill("SmokeBomb"), Visual("SmokeBomb", ECasterGesture.None, Color.white, art["bomb"], art["smoke"], EVisualCellOrder.RippleFromTarget, arc: 0.4f, speed: 8f, linger: 0.4f));
            Link(report, Skill("Hook"), Visual("Hook", ECasterGesture.None, Color.white, art["hook"], art["impact"], EVisualCellOrder.Simultaneous, speed: 14f, projectileFaces: true));
            Link(report, Skill("GoblinHook"), Visual("GoblinHook", ECasterGesture.None, Color.white, art["hook"], art["impact"], EVisualCellOrder.Simultaneous, speed: 14f, projectileFaces: true));
            Link(report, Skill("KingShockwave"), Visual("KingShockwave", glow, White, null, art["shockwave"], EVisualCellOrder.RippleFromTarget, step: 0.06f));
            Link(report, Skill("KingSlam"), Visual("KingSlam", glow, Red, null, art["slam"], EVisualCellOrder.Simultaneous, drop: 16, impactFrame: 0));
            Link(report, Item("Bomb"), Visual("Bomb", ECasterGesture.None, Color.white, art["bomb"], art["fire"], EVisualCellOrder.RippleFromTarget, arc: 0.6f, speed: 8f));

            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        // ------------------------------------------------------------------------------------ assets

        private static ActionVisualDefinition Visual(string name, ECasterGesture gesture, Color glowColor,
            List<Sprite> projectile, List<Sprite> cell, EVisualCellOrder order, float step = 0.05f, EVisualFacing facing = EVisualFacing.None,
            float arc = 0f, float speed = 10f, bool projectileFaces = false, int drop = 0, float linger = 0f,
            int impactFrame = 1)
        {
            var path = $"{VisualFolder}/{name}Visual.asset";
            var visual = AssetDatabase.LoadAssetAtPath<ActionVisualDefinition>(path);
            if (visual == null)
            {
                visual = ScriptableObject.CreateInstance<ActionVisualDefinition>();
                AssetDatabase.CreateAsset(visual, path);
            }

            var so = new SerializedObject(visual);
            so.FindProperty("casterGesture").enumValueIndex = (int)gesture;
            so.FindProperty("glowColor").colorValue = glowColor;
            SetSprites(so.FindProperty("projectileFrames"), projectile);
            so.FindProperty("projectileCellsPerSecond").floatValue = speed;
            so.FindProperty("projectileArc").floatValue = arc;
            so.FindProperty("projectileFacesDirection").boolValue = projectileFaces;
            SetSprites(so.FindProperty("cellFrames"), cell);
            so.FindProperty("frameSeconds").floatValue = 0.06f;
            so.FindProperty("impactFrame").intValue = impactFrame;
            so.FindProperty("order").enumValueIndex = (int)order;
            so.FindProperty("stepSeconds").floatValue = step;
            so.FindProperty("cellFacing").enumValueIndex = (int)facing;
            so.FindProperty("dropPixels").intValue = drop;
            so.FindProperty("lingerSeconds").floatValue = linger;
            so.FindProperty("tint").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visual);
            return visual;
        }

        private static void SetSprites(SerializedProperty list, List<Sprite> sprites)
        {
            list.arraySize = sprites != null ? sprites.Count : 0;
            for (var i = 0; i < list.arraySize; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static UnityEngine.Object Skill(string name) =>
            GameDatabase.Instance.GetAll<SkillDefinition>().FirstOrDefault(s => s.name == name);

        private static UnityEngine.Object Item(string name) =>
            GameDatabase.Instance.GetAll<ConsumableDefinition>().FirstOrDefault(c => c.name == name);

        /// <summary>Links the visual to the skill or item only when it has none (a designer's choice is kept).</summary>
        private static void Link(StringBuilder report, UnityEngine.Object owner, ActionVisualDefinition visual)
        {
            if (owner == null)
            {
                report.AppendLine($"{visual.name}: no skill or item with that name, not linked.");
                return;
            }

            var so = new SerializedObject(owner);
            var property = so.FindProperty("visual");
            if (property.objectReferenceValue != null && property.objectReferenceValue != visual)
            {
                report.AppendLine($"{owner.name}: keeps its visual {property.objectReferenceValue.name}.");
                return;
            }

            property.objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
            report.AppendLine($"{owner.name}: {visual.name}");
        }

        // ------------------------------------------------------------------------------------ frames

        private static List<Sprite> Frames(string name, Action<Canvas, int> draw, int count = 4)
        {
            var sprites = new List<Sprite>();
            for (var frame = 0; frame < count; frame++)
            {
                var canvas = new Canvas(Box);
                draw(canvas, frame);
                var path = $"{SpriteFolder}/{name}_{frame}.png";
                File.WriteAllBytes(path, canvas.ToTexture().EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            }

            return sprites;
        }

        private static void DrawImpact(Canvas c, int f)
        {
            switch (f)
            {
                case 0:
                    c.Disc(12, 12, 2, White);
                    break;
                case 1:
                    c.Star(12, 12, 7, 4, Yellow);
                    c.Disc(12, 12, 3, White);
                    break;
                case 2:
                    c.Ring(12, 12, 9, 1, Yellow);
                    c.Star(12, 12, 9, 8, White);
                    break;
                default:
                    c.Sparks(12, 12, 10, 8, Yellow, 3);
                    break;
            }
        }

        private static void DrawFire(Canvas c, int f)
        {
            switch (f)
            {
                case 0:
                    c.Disc(12, 12, 4, Orange);
                    c.Disc(12, 12, 2, Yellow);
                    break;
                case 1:
                    c.Disc(12, 12, 8, Red);
                    c.Disc(12, 12, 6, Orange);
                    c.Disc(12, 12, 3, Yellow);
                    break;
                case 2:
                    c.Ring(12, 12, 10, 2, Orange);
                    c.Sparks(12, 12, 9, 10, Yellow, 7);
                    c.Disc(12, 12, 4, Red);
                    break;
                default:
                    c.Sparks(12, 12, 11, 12, Red, 11);
                    c.Sparks(12, 12, 8, 6, Orange, 13);
                    break;
            }
        }

        private static void DrawFrost(Canvas c, int f)
        {
            // A snowflake that grows to the box's edge, with a frozen ring at the impact.
            var length = f switch { 0 => 4, 1 => 9, _ => 11 };
            var color = f == 3 ? Blue : Cyan;
            for (var arm = 0; arm < 6; arm++)
            {
                var angle = arm * Mathf.PI / 3f + Mathf.PI / 2f;
                var x = 12 + Mathf.RoundToInt(Mathf.Cos(angle) * length);
                var y = 12 + Mathf.RoundToInt(Mathf.Sin(angle) * length);
                c.Line(12, 12, x, y, color);
                if (f is 1 or 2)
                    c.Line(12 + (x > 12 ? 1 : 0), 12 + (y > 12 ? 1 : 0), x, y, color);
                if (f >= 1)
                    c.Disc(12 + Mathf.RoundToInt(Mathf.Cos(angle) * length * 0.6f),
                        12 + Mathf.RoundToInt(Mathf.Sin(angle) * length * 0.6f), 1, f == 3 ? Blue : White);
            }

            if (f < 3)
                c.Disc(12, 12, f == 0 ? 2 : 3, White);
            if (f == 1)
                c.Ring(12, 12, 11, 1, White);
            if (f == 2)
                c.Ring(12, 12, 11, 1, Blue);
        }

        private static void DrawCloud(Canvas c, int f, Color32 color, Color32 shade)
        {
            var r = f switch { 0 => 4, 1 => 5, 2 => 6, _ => 6 };
            c.Disc(12, 12, r + 1, shade);
            c.Disc(8, 13, r - 1, shade);
            c.Disc(16, 13, r - 1, shade);
            c.Disc(12, 12, r, color);
            c.Disc(8, 13, r - 2, color);
            c.Disc(16, 13, r - 2, color);
            if (f >= 2)
                c.Sparks(12, 12, 10, 5, color, 17 + f);
        }

        private static void DrawLightning(Canvas c, int f)
        {
            // A two-pixel bolt from the top of the box to its lower half, then the flash where it lands.
            var bolt = new[] { (12, 23), (8, 17), (13, 14), (9, 9), (12, 6) };
            if (f <= 1)
            {
                for (var i = 0; i < bolt.Length - 1; i++)
                {
                    var (x0, y0) = bolt[i];
                    var (x1, y1) = bolt[i + 1];
                    c.Line(x0 - 1, y0, x1 - 1, y1, Yellow);
                    c.Line(x0, y0, x1, y1, White);
                    c.Line(x0 + 1, y0, x1 + 1, y1, f == 0 ? White : Yellow);
                }
            }

            if (f == 1)
                c.Disc(12, 6, 4, White);
            if (f == 2)
            {
                c.Ring(12, 6, 7, 2, Yellow);
                c.Disc(12, 6, 3, White);
            }

            if (f == 3)
                c.Sparks(12, 7, 10, 10, Yellow, 23);
        }

        private static void DrawWind(Canvas c, int f)
        {
            // Drawn facing right: streaks that move right and fade.
            var offset = f * 3;
            var color = f == 3 ? Grey : White;
            c.Line(2 + offset, 8, 10 + offset, 8, color);
            c.Line(4 + offset, 12, 14 + offset, 12, color);
            c.Line(2 + offset, 16, 10 + offset, 16, color);
            if (f < 3)
            {
                c.Set(11 + offset, 9, color);
                c.Set(15 + offset, 13, color);
                c.Set(11 + offset, 17, color);
            }
        }

        private static void DrawSlash(Canvas c, int f)
        {
            // Drawn facing right: a tall crescent cut across the cell, thick in the middle, growing from the top.
            switch (f)
            {
                case 0:
                    c.Crescent(4, 1, 15, 12, 23, White);
                    break;
                case 1:
                    c.Crescent(4, 1, 15, 0, 23, White);
                    c.Crescent(2, 1, 15, 2, 21, Yellow);
                    break;
                case 2:
                    c.Crescent(3, 1, 15, 0, 23, Grey);
                    c.Sparks(19, 12, 4, 5, White, 31);
                    break;
                default:
                    c.Crescent(3, 1, 15, 4, 10, DarkGrey);
                    c.Sparks(19, 12, 5, 4, Grey, 37);
                    break;
            }
        }

        private static void DrawShockwave(Canvas c, int f)
        {
            var r = f switch { 0 => 3, 1 => 6, 2 => 9, _ => 11 };
            c.Ring(12, 12, r, f == 3 ? 1 : 2, f == 3 ? Grey : White);
        }

        private static void DrawSlam(Canvas c, int f)
        {
            switch (f)
            {
                case 0:
                    c.Disc(12, 12, 6, Orange);
                    c.Disc(12, 12, 3, Yellow);
                    break;
                case 1:
                    c.Disc(12, 12, 5, Red);
                    c.Sparks(12, 12, 9, 6, Orange, 53);
                    break;
                case 2:
                    c.Ring(12, 12, 9, 2, Red);
                    c.Sparks(12, 12, 11, 5, Grey, 71);
                    break;
                default:
                    c.Ring(12, 12, 11, 1, DarkGrey);
                    break;
            }
        }

        private static void DrawOrb(Canvas c, int f, Color32 outer, Color32 inner)
        {
            c.Disc(12, 12, f == 0 ? 4 : 3, outer);
            c.Disc(12, 12, 2, inner);
            c.Set(12, 13, White);
        }

        private static void DrawBomb(Canvas c, int f)
        {
            c.Disc(12, 11, 4, DarkGrey);
            c.Set(10, 13, Grey);
            c.Line(14, 15, 16, 17, Brown);
            c.Set(17, 18, f == 0 ? Yellow : Orange);
        }

        private static void DrawDagger(Canvas c, int f)
        {
            // Drawn facing right.
            c.Line(6, 12, 9, 12, Brown);
            c.Line(9, 10, 9, 14, Grey);
            c.Line(10, 12, 18, 12, White);
            c.Line(10, 11, 16, 11, Grey);
        }

        private static void DrawHook(Canvas c, int f)
        {
            // Drawn facing right: a line with a hook at its end.
            c.Line(4, 12, 15, 12, Grey);
            c.Line(15, 12, 18, 15, White);
            c.Line(18, 15, 16, 17, White);
        }

        /// <summary>A tiny pixel canvas (origin at the bottom left, like the texture).</summary>
        private sealed class Canvas
        {
            private readonly int _size;
            private readonly Color32[] _pixels;

            public Canvas(int size)
            {
                _size = size;
                _pixels = Enumerable.Repeat(Clear, size * size).ToArray();
            }

            public void Set(int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= _size || y >= _size) return;
                _pixels[y * _size + x] = color;
            }

            public void Disc(int cx, int cy, int r, Color32 color)
            {
                for (var y = -r; y <= r; y++)
                for (var x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= r * r + r * 0.6f)
                        Set(cx + x, cy + y, color);
                }
            }

            public void Ring(int cx, int cy, int r, int thickness, Color32 color)
            {
                for (var y = -r - 1; y <= r + 1; y++)
                for (var x = -r - 1; x <= r + 1; x++)
                {
                    var d = Mathf.Sqrt(x * x + y * y);
                    if (d <= r + 0.5f && d > r - thickness + 0.5f)
                        Set(cx + x, cy + y, color);
                }
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 color)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                var err = dx + dy;
                while (true)
                {
                    Set(x0, y0, color);
                    if (x0 == x1 && y0 == y1) return;
                    var e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            /// <summary>
            /// The part of a circle centered at (<paramref name="outerX"/>, 12) that lies outside an equal circle
            /// centered at (<paramref name="innerX"/>, 12), between rows <paramref name="yMin"/> and
            /// <paramref name="yMax"/>: a crescent facing right, as thick as the distance between the centers.
            /// </summary>
            public void Crescent(int outerX, int innerX, int r, int yMin, int yMax, Color32 color)
            {
                for (var y = yMin; y <= yMax; y++)
                for (var x = 0; x < _size; x++)
                {
                    var dy = y - 12;
                    var outer = (x - outerX) * (x - outerX) + dy * dy;
                    var inner = (x - innerX) * (x - innerX) + dy * dy;
                    if (outer <= r * r && inner > r * r)
                        Set(x, y, color);
                }
            }

            public void Star(int cx, int cy, int length, int arms, Color32 color)
            {
                for (var arm = 0; arm < arms; arm++)
                {
                    var angle = arm * Mathf.PI * 2f / arms;
                    Line(cx, cy, cx + Mathf.RoundToInt(Mathf.Cos(angle) * length),
                        cy + Mathf.RoundToInt(Mathf.Sin(angle) * length), color);
                }
            }

            /// <summary>Dots around a center, placed by a fixed seed (the same art every time).</summary>
            public void Sparks(int cx, int cy, int radius, int count, Color32 color, int seed)
            {
                var random = new System.Random(seed);
                for (var i = 0; i < count; i++)
                {
                    var angle = random.NextDouble() * Math.PI * 2;
                    var distance = radius * (0.5 + random.NextDouble() * 0.5);
                    Set(cx + (int)Math.Round(Math.Cos(angle) * distance), cy + (int)Math.Round(Math.Sin(angle) * distance),
                        color);
                }
            }

            public Texture2D ToTexture()
            {
                var texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                texture.SetPixels32(_pixels);
                texture.Apply();
                return texture;
            }
        }
    }
}
