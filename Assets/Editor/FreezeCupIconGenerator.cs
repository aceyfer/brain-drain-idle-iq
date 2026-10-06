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
    /// only, no external art.
    ///
    /// 2026-10-07 play-test fix: first version read as a flat solid-green cup -- the "clear
    /// plastic" and "slush" ideas never actually came through visually. Rebuilt with real
    /// translucent alpha on the cup/dome body (so it genuinely reads as see-through plastic when
    /// composited over a dark row background, not just a pale fill color), a diagonal highlight
    /// streak (the classic glossy-plastic glare cue), a dome lid + straw (neither existed before),
    /// and a layered rim glow (inner full-brightness band + an outer softer halo, approximating a
    /// real glow falloff instead of one flat-brightness band).
    ///
    /// Outputs the same 3 named PNGs at the same 160x160 size as before (no .meta/sprite-rect
    /// changes needed). The inventory count badge ("xN") is NOT baked in here -- added at runtime
    /// via a live TMP label, matching this project's "text always comes from TMP" convention.
    ///
    /// No live Editor session was available to run this menu item for the committed PNGs -- they
    /// were generated offline via a Python/PIL port of this same layout and committed directly,
    /// same precedent as CryoChamberBackdropGenerator's own PNGs.
    /// </summary>
    public static class FreezeCupIconGenerator
    {
        private const string OutputFolder = "Assets/Resources/UI/Generated";
        private const int W = 160, H = 160;

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 PlasticStroke = new Color32(255, 255, 255, 210);
        private static readonly Color32 PlasticClear = new Color32(255, 255, 255, 64); // ~25% alpha
        private static readonly Color32 PlasticHighlight = new Color32(255, 255, 255, 130); // ~50% alpha

        // Dome lid, sits on top of the cup rim.
        private const float DomeCenterX = 80f;
        private const float DomeBaseY = 46f;
        private const float DomeTopY = 14f;
        private const float DomeHalfWidth = 44f;

        // Cup trapezoid: wider at the rim, narrower at the base.
        private const float RimY = 46f, RimHalfWidth = 40f;
        private const float BaseY = 144f, BaseHalfWidth = 28f;
        private const float CenterX = 80f;
        private const float StrokeWidth = 3f;
        private const float RimGlowInnerHeight = 8f;
        private const float RimGlowOuterHeight = 16f;
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
            DrawDome(pixels, tint);
            DrawCupBody(pixels, tint, seed);

            // Straw, drawn last so it reads as poking up through the dome.
            DrawThickLine(pixels, new Vector2(62f, -4f), new Vector2(88f, 32f), 6f, PlasticStroke);
        }

        private static void DrawDome(Color32[] pixels, Color32 tint)
        {
            float ry = DomeBaseY - DomeTopY;

            for (int y = Mathf.FloorToInt(DomeTopY - StrokeWidth); y <= Mathf.CeilToInt(DomeBaseY); y++)
            {
                if (y < 0 || y >= H) { continue; }
                for (int x = Mathf.FloorToInt(DomeCenterX - DomeHalfWidth - StrokeWidth); x <= Mathf.CeilToInt(DomeCenterX + DomeHalfWidth + StrokeWidth); x++)
                {
                    if (x < 0 || x >= W) { continue; }
                    float px = x + 0.5f, py = y + 0.5f;
                    if (py > DomeBaseY + 0.5f) { continue; }

                    float nx = (px - DomeCenterX) / DomeHalfWidth;
                    float ny = (py - DomeBaseY) / ry;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    int idx = y * W + x;

                    if (dist <= 1f)
                    {
                        pixels[idx] = HighlightOrClear(px, py, tint, seedShift: 0);
                    }
                    else if (dist <= 1f + StrokeWidth / ry)
                    {
                        pixels[idx] = PlasticStroke;
                    }
                }
            }
        }

        private static void DrawCupBody(Color32[] pixels, Color32 tint, int seed)
        {
            float slushTopBase = RimY + RimGlowOuterHeight + 14f;
            var rnd = new System.Random(2000 + seed);

            for (int y = Mathf.FloorToInt(RimY - StrokeWidth); y < Mathf.CeilToInt(BaseY + StrokeWidth); y++)
            {
                if (y < 0 || y >= H) { continue; }
                float t = Mathf.Clamp01((y - RimY) / (BaseY - RimY));
                float halfWidth = Mathf.Lerp(RimHalfWidth, BaseHalfWidth, t);
                float x0 = CenterX - halfWidth, x1 = CenterX + halfWidth;

                float wiggle = Mathf.Sin(t * 9f + seed * 1.7f) * 6f;
                bool pastRimGlow = y > RimY + RimGlowOuterHeight;
                bool inSlush = pastRimGlow && y >= slushTopBase + wiggle && y < BaseY - SlushBaseInset;

                // Layered rim glow: full-brightness inner band, softer outer halo -- approximates
                // a glow falloff instead of one flat-brightness rectangle.
                bool inRimGlowInner = y <= RimY + RimGlowInnerHeight;
                bool inRimGlowOuter = !inRimGlowInner && y <= RimY + RimGlowOuterHeight;

                for (int x = Mathf.FloorToInt(x0 - StrokeWidth); x < Mathf.CeilToInt(x1 + StrokeWidth); x++)
                {
                    if (x < 0 || x >= W) { continue; }
                    float px = x + 0.5f, py = y + 0.5f;
                    int idx = y * W + x;

                    bool inBody = px >= x0 && px <= x1;
                    bool onStroke = !inBody && px >= x0 - StrokeWidth && px <= x1 + StrokeWidth;

                    if (inRimGlowInner && inBody)
                    {
                        pixels[idx] = tint;
                    }
                    else if (inRimGlowOuter && inBody)
                    {
                        pixels[idx] = Lerp(tint, Scale(tint, 0.8f), 0.4f);
                    }
                    else if (inSlush && inBody)
                    {
                        bool speckle = ((x * 13 + y * 7 + seed * 29) % 97) < 5;
                        pixels[idx] = speckle ? Scale(tint, 1.3f) : Scale(tint, 0.55f);
                    }
                    else if (inBody)
                    {
                        pixels[idx] = HighlightOrClear(px, py, tint, seedShift: seed);
                    }
                    else if (onStroke || (inBody && (Mathf.Abs(px - x0) <= StrokeWidth || Mathf.Abs(px - x1) <= StrokeWidth)))
                    {
                        pixels[idx] = PlasticStroke;
                    }
                }
            }

            DrawThickLine(pixels, new Vector2(CenterX - BaseHalfWidth, BaseY), new Vector2(CenterX + BaseHalfWidth, BaseY), StrokeWidth, PlasticStroke);
        }

        /// <summary>Clear plastic (white ~25% alpha) everywhere in the dome/cup body above the
        /// slush line, except inside a diagonal highlight band (the "glossy plastic glare" cue,
        /// ~50% alpha) running from upper-left to lower-right across the whole icon.</summary>
        private static Color32 HighlightOrClear(float px, float py, Color32 tint, int seedShift)
        {
            // Diagonal line through the icon: py = px - 20 (a 45-degree band). Distance to that
            // line (in a simple axis-independent sense, good enough for a ~18px-wide band) decides
            // whether this pixel falls inside the highlight streak.
            float lineDist = Mathf.Abs((py - px + 20f)) / 1.4142f;
            return lineDist <= 9f ? PlasticHighlight : PlasticClear;
        }

        // ---- shared pixel helpers ----

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

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)), 0, 255),
                255);
        }

        private static Color32 Scale(Color32 c, float factor)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * factor), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * factor), 0, 255),
                c.a);
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
