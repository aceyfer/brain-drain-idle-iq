#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-06 FREEZE INVENTORY: Aceyfer's "clear plastic slushy cup" icon concept for the
    /// Brain Freeze family's 3 rarity tiers. Same code-generated "paint bucket" approach as
    /// CryoChamberBackdropGenerator/CardAndEventArtGenerator: flat shapes, Palette rarity tokens
    /// only, no external art. A straw, a translucent plastic cup outline, a tinted rim band, a
    /// tinted jagged-top slush fill with a few lighter icy speckles. The inventory count badge
    /// ("x5") is deliberately NOT baked in here -- same project convention as every other
    /// generated sprite (text always comes from a live TMP component, never rasterized), added at
    /// runtime by TimedPurchaseWalletUI/GodTierStoreSlotUI on top of whichever of these 3 PNGs
    /// matches the item's rarityTier.
    ///
    /// No live Editor session was available to run this menu item for the committed PNGs -- they
    /// were generated offline via a Python/PIL port of this same layout and committed directly,
    /// same as CryoChamberBackdropGenerator's own PNGs. Re-running this menu item reproduces the
    /// same cup layout/colors, not a byte-identical image.
    /// </summary>
    public static class FreezeCupIconGenerator
    {
        private const string OutputFolder = "Assets/Resources/UI/Generated";
        private const int W = 160, H = 160;

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 PlasticStroke = new Color32(255, 255, 255, 190);

        // Cup trapezoid: wider at the rim, narrower at the base.
        private const float RimY = 34f, RimHalfWidth = 40f;
        private const float BaseY = 128f, BaseHalfWidth = 28f;
        private const float CenterX = 80f;
        private const float StrokeWidth = 3f;
        private const float RimBandHeight = 10f;
        private const float SlushBaseInset = 4f; // leaves a sliver of visible cup base below the slush

        private static readonly (string FileName, Color32 Tint)[] Tiers =
        {
            ("FreezeCup_Uncommon.png", new Color32(0x1E, 0xFF, 0x00, 0xFF)),
            ("FreezeCup_Rare.png", new Color32(0x00, 0x70, 0xDD, 0xFF)),
            ("FreezeCup_Epic.png", new Color32(0xA3, 0x35, 0xEE, 0xFF)),
        };

        [MenuItem("BrainDrain/Tools/Generate Freeze Cup Icons")]
        public static void Generate()
        {
            if (EditorToolGuard.BlockedByPlayMode("FreezeCupIconGenerator")) { return; }
            if (!Directory.Exists(OutputFolder)) { Directory.CreateDirectory(OutputFolder); }

            for (int i = 0; i < Tiers.Length; i++)
            {
                Color32[] pixels = new Color32[W * H];
                for (int p = 0; p < pixels.Length; p++) { pixels[p] = Transparent; }

                DrawCup(pixels, Tiers[i].Tint, seed: i);
                WritePng(pixels, Tiers[i].FileName);
                AssetDatabase.Refresh();
                ConfigureSprite(Tiers[i].FileName);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[FreezeCupIconGenerator] Wrote 3 tier cup icons to " + OutputFolder);
        }

        private static void DrawCup(Color32[] pixels, Color32 tint, int seed)
        {
            // Straw: a thin white-translucent diagonal rect poking out the top-left of the rim.
            DrawThickLine(pixels, new Vector2(62f, 6f), new Vector2(86f, 40f), 6f, PlasticStroke);

            float slushTopBase = RimY + RimBandHeight + 14f;
            var rnd = new System.Random(2000 + seed);

            for (int y = Mathf.FloorToInt(RimY - StrokeWidth); y < Mathf.CeilToInt(BaseY + StrokeWidth); y++)
            {
                if (y < 0 || y >= H) { continue; }
                float t = Mathf.Clamp01((y - RimY) / (BaseY - RimY));
                float halfWidth = Mathf.Lerp(RimHalfWidth, BaseHalfWidth, t);
                float x0 = CenterX - halfWidth, x1 = CenterX + halfWidth;

                // Jagged slush top: a low-frequency sine wiggle around slushTopBase so the fill
                // edge reads as icy/uneven rather than a flat waterline.
                float wiggle = Mathf.Sin(t * 9f + seed * 1.7f) * 6f;
                bool pastRim = y > RimY + RimBandHeight;
                bool inSlush = pastRim && y >= slushTopBase + wiggle && y < BaseY - SlushBaseInset;

                for (int x = Mathf.FloorToInt(x0 - StrokeWidth); x < Mathf.CeilToInt(x1 + StrokeWidth); x++)
                {
                    if (x < 0 || x >= W) { continue; }
                    float px = x + 0.5f, py = y + 0.5f;
                    int idx = y * W + x;

                    bool inBody = px >= x0 && px <= x1;
                    bool onStroke = !inBody && px >= x0 - StrokeWidth && px <= x1 + StrokeWidth;

                    if (y <= RimY + RimBandHeight && inBody)
                    {
                        // Rim band -- full-brightness tint, the "rim glow".
                        pixels[idx] = tint;
                    }
                    else if (inSlush && inBody)
                    {
                        bool speckle = ((x * 13 + y * 7 + seed * 29) % 97) < 5;
                        pixels[idx] = speckle ? Scale(tint, 1.3f) : Scale(tint, 0.55f);
                    }
                    else if (onStroke || (inBody && (Mathf.Abs(px - x0) <= StrokeWidth || Mathf.Abs(px - x1) <= StrokeWidth)))
                    {
                        pixels[idx] = PlasticStroke;
                    }
                    // else: inside the clear (unfilled) part of the cup -- stays transparent, the
                    // "clear plastic" look.
                }
            }

            // Base stroke (bottom edge of the cup).
            DrawThickLine(pixels, new Vector2(CenterX - BaseHalfWidth, BaseY), new Vector2(CenterX + BaseHalfWidth, BaseY), StrokeWidth, PlasticStroke);
        }

        private static void DrawThickLine(Color32[] pixels, Vector2 a, Vector2 b, float thickness, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness));
            int maxX = Mathf.Min(W, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness));
            int maxY = Mathf.Min(H, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness));
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    if (DistToSegment(x + 0.5f, y + 0.5f, a.x, a.y, b.x, b.y) <= thickness * 0.5f)
                    {
                        pixels[y * W + x] = color;
                    }
                }
            }
        }

        private static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float apx = px - ax, apy = py - ay;
            float abLenSq = abx * abx + aby * aby;
            float t = abLenSq > 0f ? Mathf.Clamp01((apx * abx + apy * aby) / abLenSq) : 0f;
            float cx = ax + abx * t, cy = ay + aby * t;
            float dx = px - cx, dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static Color32 Scale(Color32 c, float factor)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * factor), 0, 255),
                255);
        }

        private static void WritePng(Color32[] pixels, string fileName)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var flipped = new Color32[pixels.Length];
            for (int y = 0; y < H; y++)
            {
                System.Array.Copy(pixels, y * W, flipped, (H - 1 - y) * W, W);
            }
            tex.SetPixels32(flipped);
            tex.Apply(false, false);
            File.WriteAllBytes(OutputFolder + "/" + fileName, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSprite(string fileName)
        {
            string path = OutputFolder + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[FreezeCupIconGenerator] No TextureImporter for '{path}'."); return; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = Vector4.zero;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
#endif
