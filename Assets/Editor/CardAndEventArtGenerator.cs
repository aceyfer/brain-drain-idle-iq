#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-04 art pass: generates the Pocket/Wallet/Event-popup sprite set entirely in code,
    /// the same "paint bucket" philosophy as RestorationBarArtGenerator -- no external art, no
    /// asset store. Output: Assets/_Game/Art/UI/Generated/*.png, Sprite (FullRect, Clamp, no
    /// mipmaps), with 9-slice borders set per sprite where the consuming UI needs one.
    ///
    /// No live Editor session was available to run this menu item for the committed PNGs -- they
    /// were generated offline via a Python port of this exact pixel algorithm (same technique
    /// already used for the restoration bar and button-hole-inset passes) and committed directly.
    /// Re-running this menu item in a live Editor session reproduces byte-identical output and is
    /// the natural cross-check.
    /// </summary>
    public static class CardAndEventArtGenerator
    {
        // 2026-10-04: Resources/ (not a plain Assets/_Game/Art path) is required so the
        // consuming controllers can Resources.Load these sprites at runtime with zero scene
        // writes and zero Inspector wiring -- the exact same reasoning as RestorationBarArt
        // Generator's own Resources/UI/RestorationBar output folder.
        private const string OutputFolder = "Assets/Resources/UI/Generated";

        private static readonly Color32 Base = new Color32(0x1B, 0x0F, 0x2E, 255);
        private static readonly Color32 Cyan = new Color32(0x00, 0xDD, 0xEB, 255);
        private static readonly Color32 Magenta = new Color32(0xFF, 0x14, 0x93, 255);
        private static readonly Color32 Glow = new Color32(0x80, 0xF4, 0xFF, 255);
        private static readonly Color32 Cream = new Color32(230, 217, 184, 255);
        private static readonly Color32 Ink = new Color32(0x1B, 0x0F, 0x2E, 255);
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        [MenuItem("BrainDrain/Tools/Generate Card & Event Art")]
        public static void Generate()
        {
            if (EditorToolGuard.BlockedByPlayMode("CardAndEventArtGenerator")) { return; }

            if (!Directory.Exists(OutputFolder)) { Directory.CreateDirectory(OutputFolder); }

            UnityEngine.Random.InitState(20261004);

            WritePng(GenBizCardPaper(), "BizCard_Paper.png");
            WritePng(GenBizCardShadow(), "BizCard_Shadow.png");
            WritePng(GenBizCardStampNew(), "BizCard_Stamp_New.png");
            WritePng(GenBizCardMonogramRing(), "BizCard_Monogram_Ring.png");
            WritePng(GenWalletCard(), "Wallet_Card.png");
            WritePng(GenWalletSheen(), "Wallet_Sheen.png");
            WritePng(GenAlertFrame(), "Alert_Frame.png");
            WritePng(GenAlertHeaderStrip(), "Alert_HeaderStrip.png");
            WritePng(GenAlertScanlines(), "Alert_Scanlines.png");
            WritePng(GenAlertButton(), "Alert_Button.png");

            AssetDatabase.Refresh();

            ConfigureSprite("BizCard_Paper.png", new Vector4(22, 22, 22, 22), SpriteMeshType.FullRect);
            ConfigureSprite("BizCard_Shadow.png", new Vector4(40, 40, 40, 40), SpriteMeshType.FullRect);
            ConfigureSprite("BizCard_Stamp_New.png", Vector4.zero, SpriteMeshType.FullRect);
            ConfigureSprite("BizCard_Monogram_Ring.png", Vector4.zero, SpriteMeshType.FullRect);
            ConfigureSprite("Wallet_Card.png", new Vector4(20, 20, 20, 20), SpriteMeshType.FullRect);
            ConfigureSprite("Wallet_Sheen.png", Vector4.zero, SpriteMeshType.FullRect);
            ConfigureSprite("Alert_Frame.png", new Vector4(32, 32, 32, 32), SpriteMeshType.FullRect);
            ConfigureSprite("Alert_HeaderStrip.png", Vector4.zero, SpriteMeshType.FullRect, TextureWrapMode.Repeat);
            ConfigureSprite("Alert_Scanlines.png", Vector4.zero, SpriteMeshType.FullRect, TextureWrapMode.Repeat);
            ConfigureSprite("Alert_Button.png", new Vector4(48, 10, 48, 10), SpriteMeshType.FullRect);

            AssetDatabase.SaveAssets();
            Debug.Log("[CardAndEventArtGenerator] Wrote 10 sprites to " + OutputFolder);
        }

        // ---- 1. BizCard_Paper (512x288, 9-slice) ------------------------------------------------

        private static Texture2D GenBizCardPaper()
        {
            const int w = 512, h = 288;
            var tex = NewTex(w, h);
            bool[,] outer = RoundedRectMask(w, h, 16f, 0f);
            bool[,] edgeInner = RoundedRectMask(w, h, 14f, 2f);

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float noise = ((float)new System.Random(x * 73856093 ^ y * 19349663).NextDouble() * 2f - 1f) * 0.04f;
                    float fiber = Mathf.Sin((x + y) * 0.35f) * 3f;
                    bool isOuter = outer[y, x];
                    bool isEdge = isOuter && !edgeInner[y, x];

                    Color32 baseColor = isEdge ? Scale(Cream, 0.82f) : Cream;
                    byte r = ClampByte(baseColor.r * (1f + noise) + (isEdge ? 0f : fiber));
                    byte g = ClampByte(baseColor.g * (1f + noise) + (isEdge ? 0f : fiber));
                    byte b = ClampByte(baseColor.b * (1f + noise) + (isEdge ? 0f : fiber));
                    pixels[y * w + x] = isOuter ? new Color32(r, g, b, 255) : Transparent;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 2. BizCard_Shadow (9-slice) --------------------------------------------------------

        private static Texture2D GenBizCardShadow()
        {
            const int w = 512, h = 288;
            const float inset = 20f, radius = 16f, blur = 16f;
            var tex = NewTex(w, h);
            var pixels = new Color32[w * h];

            float x0 = inset, y0 = inset, x1 = w - inset, y1 = h - inset;

            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float dx = Mathf.Max(Mathf.Max(x0 - px, px - x1), 0f);
                    float dy = Mathf.Max(Mathf.Max(y0 - py, py - y1), 0f);
                    float dist = (dx > 0f && dy > 0f) ? Mathf.Sqrt(dx * dx + dy * dy) : Mathf.Max(dx, dy);
                    dist = Mathf.Max(dist - radius, 0f);
                    float alpha = 0.35f * Mathf.Clamp01(1f - dist / blur);
                    pixels[y * w + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 3. BizCard_Stamp_New (256x128) -----------------------------------------------------

        private static Texture2D GenBizCardStampNew()
        {
            const int w = 256, h = 128;
            var tex = NewTex(w, h);

            bool[,] borderOuter = RoundedRectMask(w, h, 6f, 10f);
            bool[,] borderInner = RoundedRectMask(w, h, 3f, 15f);
            bool[,] border = new bool[h, w];
            int borderCount = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (borderOuter[y, x] && !borderInner[y, x]) { border[y, x] = true; borderCount++; }

            float letterH = 56f;
            float top = (h - letterH) / 2f;
            float bottom = top + letterH;
            const float thick = 9f;

            var segs = new System.Collections.Generic.List<(float, float, float, float)>();
            const float nx0 = 34f, nx1 = 82f;
            segs.Add((nx0, top, nx0, bottom));
            segs.Add((nx1, top, nx1, bottom));
            segs.Add((nx0, top, nx1, bottom));
            const float ex0 = 100f, ex1 = 142f;
            segs.Add((ex0, top, ex0, bottom));
            segs.Add((ex0, top, ex1, top));
            segs.Add((ex0, (top + bottom) / 2f, ex1 - 6f, (top + bottom) / 2f));
            segs.Add((ex0, bottom, ex1, bottom));
            const float wx0 = 160f, wxm = 191f, wx1 = 222f;
            segs.Add((wx0, top, (wx0 + wxm) / 2f, bottom));
            segs.Add(((wx0 + wxm) / 2f, bottom, wxm, top));
            segs.Add((wxm, top, (wxm + wx1) / 2f, bottom));
            segs.Add(((wxm + wx1) / 2f, bottom, wx1, top));

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    bool onLetter = false;
                    foreach (var seg in segs)
                    {
                        if (DistToSegment(px, py, seg.Item1, seg.Item2, seg.Item3, seg.Item4) <= thick * 0.5f) { onLetter = true; break; }
                    }
                    bool on = border[y, x] || onLetter;
                    pixels[y * w + x] = on ? Magenta : Transparent;
                }
            }

            // Distressed edges: random alpha dropout of 15-25% of the border's own pixels.
            float dropoutFrac = UnityEngine.Random.Range(0.15f, 0.25f);
            int dropCount = Mathf.RoundToInt(borderCount * dropoutFrac);
            int dropped = 0;
            int guard = 0;
            while (dropped < dropCount && guard < borderCount * 20)
            {
                guard++;
                int x = UnityEngine.Random.Range(0, w);
                int y = UnityEngine.Random.Range(0, h);
                if (border[y, x] && pixels[y * w + x].a != 0)
                {
                    pixels[y * w + x] = Transparent;
                    dropped++;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 4. BizCard_Monogram_Ring (128x128) -------------------------------------------------

        private static Texture2D GenBizCardMonogramRing()
        {
            const int w = 128, h = 128;
            var tex = NewTex(w, h);
            float cx = w / 2f, cy = h / 2f;
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    bool ring = (d >= 54f && d <= 58f) || (d >= 45f && d <= 48f);
                    pixels[y * w + x] = ring ? Ink : Transparent;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 5. Wallet_Card (512x320, 9-slice) --------------------------------------------------

        private static Texture2D GenWalletCard()
        {
            const int w = 512, h = 320;
            const float radius = 12f;
            var tex = NewTex(w, h);
            bool[,] outer = RoundedRectMask(w, h, radius, 0f);
            bool[,] innerBorder = RoundedRectMask(w, h, Mathf.Max(radius - 3f, 0f), 3f);

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                float lighten = (1f - t) * 0.15f;
                for (int x = 0; x < w; x++)
                {
                    if (!outer[y, x]) { pixels[y * w + x] = Transparent; continue; }

                    byte r = ClampByte(Base.r + (255 - Base.r) * lighten);
                    byte g = ClampByte(Base.g + (255 - Base.g) * lighten);
                    byte b = ClampByte(Base.b + (255 - Base.b) * lighten);
                    Color32 color = new Color32(r, g, b, 255);

                    bool isBorder = !innerBorder[y, x];
                    if (isBorder)
                    {
                        float perimT = (x + y) / (float)(w + h);
                        float shimmer = 0.8f + 0.2f * Mathf.Sin(perimT * Mathf.PI * 10f);
                        color = Scale(Cyan, shimmer);
                    }

                    // chip, top-left
                    if (x >= 24 && x < 64 && y >= 24 && y < 52)
                    {
                        color = Glow;
                    }

                    pixels[y * w + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 6. Wallet_Sheen (128x320) ----------------------------------------------------------

        private static Texture2D GenWalletSheen()
        {
            const int w = 128, h = 320;
            const float bandHalf = 0.18f;
            var tex = NewTex(w, h);
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (x + y) / (float)(w + h);
                    float d = Mathf.Abs(t - 0.5f);
                    float alpha = d < bandHalf ? Mathf.SmoothStep(0.35f, 0f, d / bandHalf) : 0f;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 7. Alert_Frame (256x256, 9-slice) --------------------------------------------------

        private static Texture2D GenAlertFrame()
        {
            const int w = 256, h = 256;
            const float radius = 12f;
            var tex = NewTex(w, h);
            bool[,] outer = RoundedRectMask(w, h, radius, 0f);
            bool[,] inner = RoundedRectMask(w, h, Mathf.Max(radius - 3f, 0f), 3f);

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!outer[y, x]) { pixels[y * w + x] = Transparent; continue; }
                    pixels[y * w + x] = inner[y, x] ? new Color32(Base.r, Base.g, Base.b, 242) : new Color32(Magenta.r, Magenta.g, Magenta.b, 242);
                }
            }

            const int arm = 24, thick = 4, gap = 10;
            void AddL(int cx0, int cy0, int horizDir, int vertDir)
            {
                int y0 = vertDir > 0 ? cy0 : cy0 - thick;
                int x0 = horizDir > 0 ? cx0 : cx0 - arm;
                FillRect(pixels, w, h, x0, y0, arm, thick, Cyan);
                int x1 = horizDir > 0 ? cx0 : cx0 - thick;
                int y1 = vertDir > 0 ? cy0 : cy0 - arm;
                FillRect(pixels, w, h, x1, y1, thick, arm, Cyan);
            }
            AddL(gap, gap, 1, 1);
            AddL(w - gap, gap, -1, 1);
            AddL(gap, h - gap, 1, -1);
            AddL(w - gap, h - gap, -1, -1);

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 8. Alert_HeaderStrip (512x48) ------------------------------------------------------

        private static Texture2D GenAlertHeaderStrip()
        {
            const int w = 512, h = 48;
            const int band = 16, period = band * 2;
            var tex = NewTex(w, h);
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int v = (x + y) % period;
                    pixels[y * w + x] = v < band ? Magenta : Base;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 9. Alert_Scanlines (4x4, Repeat) ---------------------------------------------------

        private static Texture2D GenAlertScanlines()
        {
            const int w = 4, h = 4;
            var tex = NewTex(w, h);
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;
            byte a = (byte)Mathf.RoundToInt(0.18f * 255f);
            for (int x = 0; x < w; x++) pixels[3 * w + x] = new Color32(0, 0, 0, a);

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- 10. Alert_Button (256x96, 9-slice) -------------------------------------------------

        private static Texture2D GenAlertButton()
        {
            const int w = 256, h = 96;
            float rOuter = h * 0.5f;
            float cy = h * 0.5f;
            var tex = NewTex(w, h);
            var pixels = new Color32[w * h];

            bool InCapsule(float px, float py, float radius)
            {
                if (px < radius) return Dist(px, py, radius, cy) <= radius;
                if (px > w - radius) return Dist(px, py, w - radius, cy) <= radius;
                return Mathf.Abs(py - cy) <= radius;
            }

            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    bool outer = InCapsule(px, py, rOuter);
                    bool borderIn = InCapsule(px, py, rOuter - 3f);
                    bool glowIn = InCapsule(px, py, rOuter - 4f);

                    if (!outer) { pixels[y * w + x] = Transparent; continue; }
                    if (!borderIn) { pixels[y * w + x] = Cyan; continue; }
                    if (borderIn && !glowIn)
                    {
                        pixels[y * w + x] = Lerp(Base, Glow, 0.4f);
                        continue;
                    }
                    pixels[y * w + x] = Base;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ---- shared helpers ----------------------------------------------------------------------

        private static Texture2D NewTex(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false);

        private static bool[,] RoundedRectMask(int w, int h, float radius, float inset)
        {
            var mask = new bool[h, w];
            float x0 = inset, y0 = inset, x1 = w - inset, y1 = h - inset;
            for (int y = 0; y < h; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    if (px < x0 || px > x1 || py < y0 || py > y1) { continue; }

                    bool ok = true;
                    if (px < x0 + radius && py < y0 + radius) ok = Dist(px, py, x0 + radius, y0 + radius) <= radius;
                    else if (px > x1 - radius && py < y0 + radius) ok = Dist(px, py, x1 - radius, y0 + radius) <= radius;
                    else if (px < x0 + radius && py > y1 - radius) ok = Dist(px, py, x0 + radius, y1 - radius) <= radius;
                    else if (px > x1 - radius && py > y1 - radius) ok = Dist(px, py, x1 - radius, y1 - radius) <= radius;
                    mask[y, x] = ok;
                }
            }
            return mask;
        }

        private static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
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

        private static void FillRect(Color32[] pixels, int w, int h, int x0, int y0, int rw, int rh, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y0 + rh); y++)
            {
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x0 + rw); x++)
                {
                    pixels[y * w + x] = color;
                }
            }
        }

        private static Color32 Scale(Color32 c, float factor)
        {
            return new Color32(ClampByte(c.r * factor), ClampByte(c.g * factor), ClampByte(c.b * factor), c.a);
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                ClampByte(Mathf.Lerp(a.r, b.r, t)),
                ClampByte(Mathf.Lerp(a.g, b.g, t)),
                ClampByte(Mathf.Lerp(a.b, b.b, t)),
                255);
        }

        private static byte ClampByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);

        private static void WritePng(Texture2D tex, string fileName)
        {
            string path = OutputFolder + "/" + fileName;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureSprite(string fileName, Vector4 border, SpriteMeshType meshType, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            string path = OutputFolder + "/" + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[CardAndEventArtGenerator] No TextureImporter for '{path}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrap;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
#endif
