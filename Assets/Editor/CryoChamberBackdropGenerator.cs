#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-05 ART PASS 2: Stage 0 "Cryo Chamber" backdrop (Aceyfer's concept -- wake in a
    /// room of cryo pods, some still asleep, some broken, some labeled DECEASED). Same
    /// "paint bucket" code-generated-art approach as CardAndEventArtGenerator/
    /// RestorationBarArtGenerator: flat shapes, Palette tokens only, no external art.
    ///
    /// Outputs two sprites: the static room (committed as Stage0_CryoChamber.png, assigned to
    /// BackgroundStageView.stageSprites[0] at runtime -- see CryoChamberStageEffects.cs) and an
    /// isolated rim-light-only layer (Stage0_CryoChamber_PodGlow.png, transparent elsewhere) that
    /// CryoChamberStageEffects pulses on top for the "slow 4s pulse on the pod lights" beat. The
    /// "DECEASED" tags are a plain Dim rectangle here, not baked text -- CryoChamberStageEffects
    /// places real TMP labels over them instead, matching every other generated sprite in this
    /// project (text always comes from a TMP component, never rasterized into a texture).
    ///
    /// No live Editor session was available to run this menu item for the committed PNGs -- they
    /// were generated offline via a Python/PIL port of this same layout and committed directly.
    /// Re-running this menu item reproduces the same pod layout/colors, not a byte-identical
    /// image (PIL's antialiasing and this file's pixel math don't match exactly).
    /// </summary>
    public static class CryoChamberBackdropGenerator
    {
        private const string OutputFolder = "Assets/Resources/UI/Generated";
        private const int W = 768, H = 1344;

        private static readonly Color32 Base = new Color32(0x1B, 0x0F, 0x2E, 255);
        private static readonly Color32 Surface = new Color32(0x2A, 0x1A, 0x45, 255);
        private static readonly Color32 DeepCyan = new Color32(0x00, 0x83, 0x8C, 255);
        private static readonly Color32 Glow = new Color32(0x80, 0xF4, 0xFF, 255);
        private static readonly Color32 Dim = new Color32(153, 153, 153, 255);
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        private const float PodsYTop = 420f;
        private const float PodsYBottom = 980f;
        private const int PodCount = 6;
        private const float PodWidth = 92f;

        public static readonly int[] CrackedPodIndices = { 1, 4 };
        public static readonly int[] DeceasedPodIndices = { 0, 5 };

        [MenuItem("BrainDrain/Tools/Generate Cryo Chamber Backdrop")]
        public static void Generate()
        {
            if (EditorToolGuard.BlockedByPlayMode("CryoChamberBackdropGenerator")) { return; }
            if (!Directory.Exists(OutputFolder)) { Directory.CreateDirectory(OutputFolder); }

            Color32[] room = new Color32[W * H];
            Color32[] glow = new Color32[W * H];
            for (int i = 0; i < room.Length; i++) { room[i] = Base; glow[i] = Transparent; }

            float gap = (W - PodCount * PodWidth) / (PodCount + 1);
            for (int i = 0; i < PodCount; i++)
            {
                float x0 = gap + i * (PodWidth + gap);
                float x1 = x0 + PodWidth;
                bool cracked = System.Array.IndexOf(CrackedPodIndices, i) >= 0;
                bool deceased = System.Array.IndexOf(DeceasedPodIndices, i) >= 0;
                DrawPod(room, glow, x0, PodsYTop, x1, PodsYBottom, cracked, deceased, i);
            }

            // Composite a dimmed copy of the glow layer onto the room for the static baked look.
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    if (glow[idx].a == 0) { continue; }
                    room[idx] = Lerp(room[idx], Glow, (glow[idx].a / 255f) * 0.4f);
                }
            }

            // Fog gradient, bottom ~18% of the frame.
            int fogTop = Mathf.RoundToInt(H * 0.82f);
            for (int y = fogTop; y < H; y++)
            {
                float t = (y - fogTop) / (float)(H - fogTop);
                float a = Mathf.Pow(t, 1.4f) * 0.85f;
                Color32 fogColor = new Color32((byte)(Base.r / 2), (byte)(Base.g / 2), (byte)(Base.b / 2), 255);
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    room[idx] = Lerp(room[idx], fogColor, a);
                }
            }

            WritePng(room, "Stage0_CryoChamber.png");
            WritePng(glow, "Stage0_CryoChamber_PodGlow.png");

            AssetDatabase.Refresh();
            ConfigureSprite("Stage0_CryoChamber.png");
            ConfigureSprite("Stage0_CryoChamber_PodGlow.png");
            AssetDatabase.SaveAssets();
            Debug.Log("[CryoChamberBackdropGenerator] Wrote Stage0_CryoChamber.png + _PodGlow.png to " + OutputFolder);
        }

        private static void DrawPod(Color32[] room, Color32[] glow, float x0, float y0, float x1, float y1, bool cracked, bool deceased, int seedIndex)
        {
            float cx = (x0 + x1) * 0.5f;
            float w = x1 - x0;
            float h = y1 - y0;
            float radius = w * 0.42f;
            const float framePad = 6f;
            const float rimWidth = 4f;

            Color32 glass = cracked
                ? Scale(DeepCyan, 0.5f)
                : Scale(DeepCyan, 0.78f);

            for (int y = Mathf.FloorToInt(y0 - framePad - rimWidth); y < Mathf.CeilToInt(y1 + framePad + rimWidth); y++)
            {
                if (y < 0 || y >= H) { continue; }
                for (int x = Mathf.FloorToInt(x0 - framePad - rimWidth); x < Mathf.CeilToInt(x1 + framePad + rimWidth); x++)
                {
                    if (x < 0 || x >= W) { continue; }
                    float px = x + 0.5f, py = y + 0.5f;
                    int idx = y * W + x;

                    bool inGlass = InRoundedRect(px, py, x0, y0, x1, y1, radius);
                    bool inFrame = !inGlass && InRoundedRect(px, py, x0 - framePad, y0 - framePad, x1 + framePad, y1 + framePad, radius + framePad);
                    bool inRimOuter = InRoundedRect(px, py, x0 - rimWidth, y0 - rimWidth, x1 + rimWidth, y1 + rimWidth, radius + rimWidth);

                    if (inGlass)
                    {
                        Color32 pixel = glass;
                        if (!cracked)
                        {
                            float bodyW = w * 0.55f, bodyH = h * 0.62f;
                            float bx = cx, by = y0 + h * 0.22f + bodyH * 0.5f;
                            if (InEllipse(px, py, bx, by, bodyW * 0.5f, bodyH * 0.5f))
                            {
                                pixel = Scale(glass, 0.7f);
                            }
                        }
                        room[idx] = pixel;
                    }
                    else if (inFrame)
                    {
                        room[idx] = Surface;
                    }

                    // Rim light ring: inside the outer rim boundary, outside the glass -- i.e. a
                    // thin band hugging the glass edge.
                    if (inRimOuter && !inGlass)
                    {
                        float distIn = Mathf.Abs(SignedDistToRoundedRect(px, py, x0, y0, x1, y1, radius));
                        if (distIn <= rimWidth)
                        {
                            glow[idx] = Glow;
                        }
                    }
                }
            }

            if (cracked)
            {
                var rnd = new System.Random(1000 + seedIndex);
                Vector2 prev = new Vector2(cx + Lerp(rnd, -10f, 10f), y0 + 4f);
                int steps = 7;
                for (int s = 1; s <= steps; s++)
                {
                    float py = y0 + h * s / steps;
                    float px = cx + Lerp(rnd, -w * 0.32f, w * 0.32f);
                    Vector2 cur = new Vector2(px, py);
                    DrawThickLine(room, prev, cur, 4f, Base);
                    prev = cur;
                }
            }

            if (deceased)
            {
                float tagW = w * 0.82f, tagH = 26f;
                float tagX0 = cx - tagW * 0.5f;
                float tagY0 = y1 - tagH - 10f;
                FillRect(room, tagX0, tagY0, tagX0 + tagW, tagY0 + tagH, Dim);
            }
        }

        // ---- shared pixel helpers (same math CardAndEventArtGenerator/RestorationBarArtGenerator use) ----

        private static bool InRoundedRect(float px, float py, float x0, float y0, float x1, float y1, float radius)
        {
            if (px < x0 || px > x1 || py < y0 || py > y1) { return false; }
            if (px < x0 + radius && py < y0 + radius) { return Dist(px, py, x0 + radius, y0 + radius) <= radius; }
            if (px > x1 - radius && py < y0 + radius) { return Dist(px, py, x1 - radius, y0 + radius) <= radius; }
            if (px < x0 + radius && py > y1 - radius) { return Dist(px, py, x0 + radius, y1 - radius) <= radius; }
            if (px > x1 - radius && py > y1 - radius) { return Dist(px, py, x1 - radius, y1 - radius) <= radius; }
            return true;
        }

        private static float SignedDistToRoundedRect(float px, float py, float x0, float y0, float x1, float y1, float radius)
        {
            // Approximate: distance to the nearest edge/corner arc, negative inside. Good enough
            // for a thin rim band, not used for anything requiring exactness.
            float dx = Mathf.Max(x0 - px, 0f, px - x1);
            float dy = Mathf.Max(y0 - py, 0f, py - y1);
            if (dx > 0f || dy > 0f) { return Mathf.Sqrt(dx * dx + dy * dy); }
            float edgeDist = Mathf.Min(px - x0, x1 - px, py - y0, y1 - py);
            return -edgeDist;
        }

        private static bool InEllipse(float px, float py, float cx, float cy, float rx, float ry)
        {
            float dx = (px - cx) / rx, dy = (py - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }

        private static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static void FillRect(Color32[] pixels, float x0, float y0, float x1, float y1, Color32 color)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0)); y < Mathf.Min(H, Mathf.CeilToInt(y1)); y++)
            {
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0)); x < Mathf.Min(W, Mathf.CeilToInt(x1)); x++)
                {
                    pixels[y * W + x] = color;
                }
            }
        }

        private static void DrawThickLine(Color32[] pixels, Vector2 a, Vector2 b, float thickness, Color32 color)
        {
            float minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness));
            float maxX = Mathf.Min(W, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness));
            float minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness));
            float maxY = Mathf.Min(H, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness));
            for (int y = (int)minY; y < maxY; y++)
            {
                for (int x = (int)minX; x < maxX; x++)
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
            return Dist(px, py, cx, cy);
        }

        private static float Lerp(System.Random rnd, float a, float b)
        {
            return a + (float)rnd.NextDouble() * (b - a);
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
            // Texture2D is bottom-up; our pixel math above is top-down (y=0 at the top, matching
            // every other generator in this project) -- flip on write, same as SaveTextureAsSprite
            // in PlaceholderArtGenerator/CardAndEventArtGenerator.
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
            if (importer == null) { Debug.LogWarning($"[CryoChamberBackdropGenerator] No TextureImporter for '{path}'."); return; }

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
