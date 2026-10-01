#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-01 restoration-bar redesign ("paint bucket" method). Root cause of the old
    /// "soft lime blob" (diagnosed against the live scene, see HUDController.BuildRestorationBar's
    /// own doc comment): restorationFillImage used Unity's built-in UISprite (fileID 10905,
    /// guid 0000000000000000f000000000000000) as its Image.Type.Filled sprite -- a sprite baked
    /// with 9-slice border metadata for Simple/Sliced button use, never meant for Filled-mode
    /// stretching into a thin horizontal gauge. A bordered sprite's Filled mesh generator can
    /// leave soft corner/border geometry visible past where fillAmount says it should end, which
    /// is exactly the "spills past its edges, even at 0 points" symptom -- a sprite-shape defect,
    /// not something RectMask2D (the prior fix, commit c8a30eb) could ever clip away, since the
    /// stray geometry renders inside the track's own rect bounds.
    ///
    /// This generator produces a purpose-built Frame/Fill pair instead: both share one 1024x96
    /// capsule (stadium) shape, hard-edged, with NO sprite border metadata (no 9-slicing, so
    /// Image.Type.Filled can't reintroduce slice-seam artifacts), and the Fill is produced by an
    /// actual 4-connected flood fill seeded at the Frame's center pixel -- not re-derived from the
    /// same analytic formula -- so the two are guaranteed pixel-identical regardless of any later
    /// hand-edits to the frame art.
    ///
    /// Output: Assets/Resources/UI/RestorationBar/*.png, Sprite (2D and UI), FullRect mesh,
    /// Clamp wrap, no mipmaps, zero border (no 9-slice) on every sprite. Resources/ specifically
    /// so HUDController.BuildRestorationBar can Resources.Load them at runtime with zero scene or
    /// prefab wiring -- this pass is code-only, no .unity writes allowed.
    ///
    /// The optional tattoo-vine flourish mentioned in the brief was skipped: anything touching the
    /// interior risks breaking the flood-fill's single connected region (the whole point of this
    /// redesign), and a motif confident enough to not look tacked-on isn't achievable blind
    /// (no live Editor session to preview it against). Flagged per the brief's own permission to
    /// skip it rather than ship something cheap-looking.
    /// </summary>
    public static class RestorationBarArtGenerator
    {
        private const string OutputFolder = "Assets/Resources/UI/RestorationBar";

        private const int BarWidth = 1024;
        private const int BarHeight = 96;
        private const float OuterRadius = BarHeight * 0.5f; // fully round ends
        private const float OutlineThickness = 6f;
        private const float InnerRadius = OuterRadius - OutlineThickness;

        private static readonly Color32 CyanOutline = new Color32(0x00, 0xDD, 0xEB, 0xFF);
        private static readonly Color32 DarkInterior = new Color32(0x1B, 0x0F, 0x2E, 0xFF);
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 Lime = new Color32(0x39, 0xFF, 0x14, 0xFF);

        [MenuItem("BrainDrain/Tools/Generate Restoration Bar Art")]
        public static void Generate()
        {
            if (EditorToolGuard.BlockedByPlayMode("RestorationBarArtGenerator")) { return; }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            Texture2D frame = BuildFrameTexture(includeHole: true);
            Texture2D frameOutlineOnly = BuildFrameTexture(includeHole: false);
            Texture2D fill = BuildFillTextureViaFloodFill(frame);
            Texture2D sheen = BuildSheenTexture();
            Texture2D sprout = BuildSproutTexture();

            WritePng(frame, "RestorationBar_Frame.png");
            WritePng(frameOutlineOnly, "RestorationBar_FrameOutlineOnly.png");
            WritePng(fill, "RestorationBar_Fill.png");
            WritePng(sheen, "RestorationBar_Sheen.png");
            WritePng(sprout, "RestorationBar_Sprout.png");

            AssetDatabase.Refresh();

            ConfigureSpriteImport("RestorationBar_Frame.png");
            ConfigureSpriteImport("RestorationBar_FrameOutlineOnly.png");
            ConfigureSpriteImport("RestorationBar_Fill.png");
            ConfigureSpriteImport("RestorationBar_Sheen.png");
            ConfigureSpriteImport("RestorationBar_Sprout.png");

            AssetDatabase.SaveAssets();
            Debug.Log("[RestorationBarArtGenerator] Wrote 5 sprites to " + OutputFolder);
        }

        // ---- Frame / FrameOutlineOnly -------------------------------------------------------

        private static Texture2D BuildFrameTexture(bool includeHole)
        {
            var tex = new Texture2D(BarWidth, BarHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[BarWidth * BarHeight];

            for (int y = 0; y < BarHeight; y++)
            {
                for (int x = 0; x < BarWidth; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    bool insideOuter = InCapsule(px, py, OuterRadius);
                    bool insideInner = InCapsule(px, py, InnerRadius);

                    Color32 color;
                    if (insideOuter && !insideInner)
                    {
                        color = CyanOutline;
                    }
                    else if (insideInner)
                    {
                        color = includeHole ? DarkInterior : Transparent;
                    }
                    else
                    {
                        color = Transparent;
                    }

                    pixels[y * BarWidth + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static bool InCapsule(float x, float y, float radius)
        {
            float cy = BarHeight * 0.5f;
            if (x < radius)
            {
                return Dist(x, y, radius, cy) <= radius;
            }
            if (x > BarWidth - radius)
            {
                return Dist(x, y, BarWidth - radius, cy) <= radius;
            }
            return Mathf.Abs(y - cy) <= radius;
        }

        private static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ---- Fill: real 4-connected flood fill, seeded at the frame's own center pixel -------

        private static Texture2D BuildFillTextureViaFloodFill(Texture2D frame)
        {
            Color32[] framePixels = frame.GetPixels32();
            var visited = new bool[BarWidth * BarHeight];
            var outPixels = new Color32[BarWidth * BarHeight];
            for (int i = 0; i < outPixels.Length; i++) { outPixels[i] = Transparent; }

            int seedX = BarWidth / 2;
            int seedY = BarHeight / 2;

            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(seedX, seedY));
            visited[seedY * BarWidth + seedX] = true;

            while (stack.Count > 0)
            {
                Vector2Int p = stack.Pop();
                outPixels[p.y * BarWidth + p.x] = Color.white;

                TryVisit(p.x + 1, p.y, framePixels, visited, stack);
                TryVisit(p.x - 1, p.y, framePixels, visited, stack);
                TryVisit(p.x, p.y + 1, framePixels, visited, stack);
                TryVisit(p.x, p.y - 1, framePixels, visited, stack);
            }

            var tex = new Texture2D(BarWidth, BarHeight, TextureFormat.RGBA32, false);
            tex.SetPixels32(outPixels);
            tex.Apply(false, false);
            return tex;
        }

        private static void TryVisit(int x, int y, Color32[] framePixels, bool[] visited, Stack<Vector2Int> stack)
        {
            if (x < 0 || x >= BarWidth || y < 0 || y >= BarHeight) { return; }
            int idx = y * BarWidth + x;
            if (visited[idx]) { return; }

            Color32 c = framePixels[idx];
            // Flood only through the interior fill color -- stops dead at the cyan outline ring
            // (and would stop at transparent too, though the capsule interior never touches it).
            if (!ColorsClose(c, DarkInterior, 6)) { return; }

            visited[idx] = true;
            stack.Push(new Vector2Int(x, y));
        }

        private static bool ColorsClose(Color32 a, Color32 b, int tolerance)
        {
            return System.Math.Abs(a.r - b.r) <= tolerance
                && System.Math.Abs(a.g - b.g) <= tolerance
                && System.Math.Abs(a.b - b.b) <= tolerance
                && System.Math.Abs(a.a - b.a) <= tolerance;
        }

        // ---- Sheen ----------------------------------------------------------------------------

        private static Texture2D BuildSheenTexture()
        {
            const int width = 128;
            const int height = 96;
            const float bandHalfWidth = 0.18f;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (x + y) / (float)(width + height);
                    float d = Mathf.Abs(t - 0.5f);
                    float alpha = d < bandHalfWidth
                        ? Mathf.SmoothStep(0.35f, 0f, d / bandHalfWidth)
                        : 0f;

                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- Sprout -----------------------------------------------------------------------------

        private static Texture2D BuildSproutTexture()
        {
            const int size = 64;
            var silhouette = new bool[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    bool inStem = DistToSegment(px, py, 32f, 6f, 32f, 40f) <= 3f;
                    bool inLeftLeaf = InRotatedEllipse(px, py, 21f, 33f, 14f, 7f, -40f);
                    bool inRightLeaf = InRotatedEllipse(px, py, 43f, 33f, 14f, 7f, 40f);

                    silhouette[y * size + x] = inStem || inLeftLeaf || inRightLeaf;
                }
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    if (silhouette[idx])
                    {
                        pixels[idx] = Lime;
                        continue;
                    }

                    pixels[idx] = HasNearbySilhouette(silhouette, x, y, size, 2)
                        ? DarkInterior
                        : Transparent;
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static bool HasNearbySilhouette(bool[] silhouette, int x, int y, int size, int radius)
        {
            int rSq = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            {
                int ny = y + dy;
                if (ny < 0 || ny >= size) { continue; }
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > rSq) { continue; }
                    int nx = x + dx;
                    if (nx < 0 || nx >= size) { continue; }
                    if (silhouette[ny * size + nx]) { return true; }
                }
            }
            return false;
        }

        private static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax;
            float aby = by - ay;
            float apx = px - ax;
            float apy = py - ay;
            float abLenSq = abx * abx + aby * aby;
            float t = abLenSq > 0f ? Mathf.Clamp01((apx * abx + apy * aby) / abLenSq) : 0f;
            float cx = ax + abx * t;
            float cy = ay + aby * t;
            return Dist(px, py, cx, cy);
        }

        private static bool InRotatedEllipse(float px, float py, float cx, float cy, float a, float b, float angleDegrees)
        {
            float rad = -angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float dx = px - cx;
            float dy = py - cy;
            float rx = dx * cos - dy * sin;
            float ry = dx * sin + dy * cos;
            return (rx * rx) / (a * a) + (ry * ry) / (b * b) <= 1f;
        }

        // ---- Shared import plumbing -------------------------------------------------------------

        private static void WritePng(Texture2D tex, string fileName)
        {
            string path = OutputFolder + "/" + fileName;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImport(string fileName)
        {
            string path = OutputFolder + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[RestorationBarArtGenerator] No TextureImporter for '{path}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = Vector4.zero; // no 9-slice -- Frame/Fill must stretch identically

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
