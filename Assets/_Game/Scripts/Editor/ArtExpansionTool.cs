#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Procedural art generator for the 2026-08-31 art pass (Aceyfer: "universal button
    /// borders...evolution stage of 1-6...the Top could use its own unique set...the sidewalk
    /// perfectly at peds feet"). No image-generation tool is available in this session, so
    /// everything here is drawn pixel-by-pixel in C# following PlaceholderArtGenerator's
    /// established technique (outline/fill primitives -> Texture2D -> PNG -> Sprite asset),
    /// rather than painted/AI-generated imagery like the existing BG1-6 skylines. Idempotent:
    /// re-running regenerates the same sprites and re-finds (rather than duplicates) the scene
    /// objects it wires them onto.
    ///
    /// IMPORTANT CAVEAT (found while building this): WorldRestorationManager currently only has
    /// 3 configured WorldRestorationStage assets -- stageIndex 0 (Toxic Wasteland), 2 (Patchwork
    /// Recovery Zone), and 5 (Utopia Achieved). ResolveStage can therefore never actually return
    /// stageIndex 1, 3, or 4 during real play, no matter how many Points are spent -- the same
    /// ceiling BG2.png/BG4.png/BG5.png are already quietly sitting behind (see BackgroundStageView).
    /// The 6-slot arrays below are built anyway to match that existing precedent and to be ready
    /// the moment stages 1/3/4 get configured, but only indices 0/2/5 are reachable today.
    /// </summary>
    public static class ArtExpansionTool
    {
        private const string BorderArtFolder = "Assets/_Game/Sprites/UI/Generated";
        private const string AccentArtFolder = "Assets/_Game/Sprites/UI/Generated";
        private const string SidewalkArtFolder = "Assets/_Game/Sprites/UI/Generated";

        // Stage palette, index 0..5, dystopian -> utopian. Shared across borders and the TopBG
        // accent strip so both evolve in visual lockstep.
        private static readonly Color[] StageBase = new[]
        {
            HexColor("#5B4A2F"), // 0 Toxic Wasteland -- sickly rust/olive
            HexColor("#6B5F4E"), // 1 -- fading grime
            HexColor("#8B9096"), // 2 Patchwork Recovery Zone -- mixed silver/patched
            HexColor("#4C86A8"), // 3 -- cooling steel-blue
            HexColor("#7FD6E8"), // 4 -- bright chrome
            HexColor("#FFC93C"), // 5 Utopia Achieved -- warm gold
        };

        private static readonly Color[] StageAccent = new[]
        {
            HexColor("#2E2418"), // 0
            HexColor("#3A3226"), // 1
            HexColor("#5B6066"), // 2
            HexColor("#2C5C77"), // 3
            HexColor("#3FA8BF"), // 4
            HexColor("#FFF3C4"), // 5 -- glow highlight
        };

        private static readonly bool[] StageGrimy = { true, true, false, false, false, false };
        private static readonly bool[] StageGlowing = { false, false, false, false, true, true };

        [MenuItem("BrainDrain/Generate Placeholder Art/All (Borders + Top Bar + Sidewalk)")]
        public static void GenerateAll()
        {
            if (EditorToolGuard.BlockedByPlayMode("ArtExpansionTool.GenerateAll")) return;

            GenerateButtonBorders();
            GenerateTopBarStageArt();
            GenerateSidewalk();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtExpansionTool] Done. Save the scene (Ctrl+S) to persist the wiring.");
        }

        // ===================== 1. Universal button borders =====================

        private const int BorderTextureSize = 48;
        private const float BorderCornerRadius = 10f;
        private const float BorderThickness = 6f;
        private const int BorderSpriteBorder = 16;

        [MenuItem("BrainDrain/Generate Placeholder Art/Universal Button Borders")]
        public static void GenerateButtonBorders()
        {
            if (EditorToolGuard.BlockedByPlayMode("ArtExpansionTool.GenerateButtonBorders")) return;
            Directory.CreateDirectory(BorderArtFolder);
            AssetDatabase.Refresh();

            var sprites = new UnityEngine.Object[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D tex = DrawBorderFrameTexture(BorderTextureSize, BorderCornerRadius, BorderThickness,
                    StageBase[i], StageAccent[i], StageGrimy[i], StageGlowing[i]);
                string path = $"{BorderArtFolder}/ButtonBorder_Stage{i}.png";
                sprites[i] = SaveTextureAsSlicedSprite(tex, path, BorderSpriteBorder);
            }

            WireUniversalButtonBorderApplier(sprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtExpansionTool] Generated 6 button border stages and wired UniversalButtonBorderApplier.");
        }

        private static Texture2D DrawBorderFrameTexture(int size, float cornerRadius, float thickness, Color baseColor, Color accentColor, bool grimy, bool glow)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ClearTransparent(tex);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            Vector2 halfSize = new Vector2(size / 2f - 1f, size / 2f - 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float sd = SdRoundBox(p, halfSize, cornerRadius);

                    // sd <= 0 is inside the outer rounded rect; the frame band is the last
                    // `thickness` units of that before the hollow (transparent) middle.
                    float frameOuter = 0f;
                    float frameInner = -thickness;

                    if (sd > frameOuter + 1f)
                    {
                        // Fully outside -- transparent, unless this stage glows.
                        if (glow)
                        {
                            float glowFalloff = Mathf.Clamp01(1f - (sd - frameOuter) / 10f);
                            if (glowFalloff > 0f)
                            {
                                Color glowColor = accentColor;
                                glowColor.a = glowFalloff * 0.35f;
                                BlendPixel(tex, x, y, glowColor);
                            }
                        }
                        continue;
                    }

                    if (sd < frameInner - 1f)
                    {
                        continue; // deep inside -- hollow middle, button's own content shows through
                    }

                    // Antialias both edges of the band over ~1px.
                    float outerAlpha = Mathf.Clamp01((frameOuter + 1f - sd) / 2f);
                    float innerAlpha = Mathf.Clamp01((sd - (frameInner - 1f)) / 2f);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);
                    if (alpha <= 0f) { continue; }

                    // Bevel: lerp from baseColor at the outer edge to accentColor at the inner edge.
                    float t = Mathf.InverseLerp(frameOuter, frameInner, sd);
                    Color color = Color.Lerp(baseColor, accentColor, t);

                    if (grimy)
                    {
                        float grime = Hash01(x, y);
                        color *= Mathf.Lerp(0.75f, 1f, grime);
                    }

                    color.a = alpha;
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return tex;
        }

        private static void WireUniversalButtonBorderApplier(UnityEngine.Object[] sprites)
        {
            var applier = Object.FindAnyObjectByType<UniversalButtonBorderApplier>();
            if (applier == null)
            {
                var host = new GameObject("UniversalButtonBorderApplier");
                applier = host.AddComponent<UniversalButtonBorderApplier>();
                Undo.RegisterCreatedObjectUndo(host, "Create UniversalButtonBorderApplier");
            }

            AssignArrayField(applier, "stageBorderSprites", sprites);
            EditorUtility.SetDirty(applier);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ===================== 2. TopBG stage-evolving accent art =====================

        private const int AccentStripWidth = 256;
        private const int AccentStripHeight = 40;

        [MenuItem("BrainDrain/Generate Placeholder Art/Top Bar Stage Art")]
        public static void GenerateTopBarStageArt()
        {
            if (EditorToolGuard.BlockedByPlayMode("ArtExpansionTool.GenerateTopBarStageArt")) return;
            Directory.CreateDirectory(AccentArtFolder);
            AssetDatabase.Refresh();

            var sprites = new UnityEngine.Object[6];
            for (int i = 0; i < 6; i++)
            {
                Texture2D tex = DrawAccentStripTexture(AccentStripWidth, AccentStripHeight, StageBase[i], StageAccent[i], StageGrimy[i], StageGlowing[i]);
                string path = $"{AccentArtFolder}/TopBarAccent_Stage{i}.png";
                sprites[i] = SaveTextureAsSimpleSprite(tex, path);
            }

            WireAccentBarStageView("TopBG", sprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtExpansionTool] Generated 6 Top Bar accent stages and wired AccentBarStageView onto TopBG.");
        }

        private static Texture2D DrawAccentStripTexture(int width, int height, Color baseColor, Color accentColor, bool grimy, bool glow)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                float verticalT = y / (float)(height - 1);
                // Beveled panel: lighter near the top, darker near the bottom.
                Color rowColor = Color.Lerp(Color.Lerp(baseColor, Color.white, 0.18f), baseColor * 0.6f, verticalT);
                rowColor.a = 1f;

                for (int x = 0; x < width; x++)
                {
                    Color color = rowColor;

                    if (grimy)
                    {
                        float grime = Hash01(x, y);
                        color *= Mathf.Lerp(0.8f, 1f, grime);
                        color.a = 1f; // grime darkens RGB only -- this strip must stay fully opaque
                    }

                    tex.SetPixel(x, y, color);
                }
            }

            // Center accent line: a row of small rivets for grimy stages, a smooth gold
            // pinstripe with a soft glow for the healed stages.
            int centerY = height / 2;
            if (glow)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int y = centerY + dy;
                        if (y < 0 || y >= height) { continue; }
                        float falloff = 1f - Mathf.Abs(dy) / 3f;
                        Color existing = tex.GetPixel(x, y);
                        Color blended = Color.Lerp(existing, accentColor, falloff * 0.8f);
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
            else
            {
                int spacing = 18;
                int rivetRadius = grimy ? 2 : 1;
                for (int x = spacing / 2; x < width; x += spacing)
                {
                    FillCircle(tex, new Vector2(x, centerY), rivetRadius, accentColor);
                }
            }

            // Reinforce the bevel with a crisp line at each edge -- row 0 is already the
            // lightest row of the gradient above, row (height-1) already the darkest, so these
            // push the same two edges further rather than fighting the gradient direction.
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, 0, Color.Lerp(baseColor, Color.white, 0.35f));
                tex.SetPixel(x, height - 1, baseColor * 0.4f);
            }

            tex.Apply();
            return tex;
        }

        private static void WireAccentBarStageView(string targetObjectName, UnityEngine.Object[] sprites)
        {
            GameObject target = GameObject.Find(targetObjectName);
            if (target == null)
            {
                Debug.LogWarning($"[ArtExpansionTool] No GameObject named '{targetObjectName}' found in the active scene -- cannot wire AccentBarStageView.");
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning($"[ArtExpansionTool] '{targetObjectName}' has no Image component -- cannot wire AccentBarStageView.");
                return;
            }

            AccentBarStageView view = target.GetComponent<AccentBarStageView>();
            if (view == null)
            {
                view = target.gameObject.AddComponent<AccentBarStageView>();
                Undo.RegisterCreatedObjectUndo(target, "Add AccentBarStageView");
            }

            AssignArrayField(view, "stageSprites", sprites);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ===================== 3. Sidewalk at pedestrian baseline =====================

        private const int SidewalkWidth = 512;
        private const int SidewalkHeight = 56;
        private const string SidewalkObjectName = "Sidewalk (Generated)";

        [MenuItem("BrainDrain/Generate Placeholder Art/Sidewalk")]
        public static void GenerateSidewalk()
        {
            if (EditorToolGuard.BlockedByPlayMode("ArtExpansionTool.GenerateSidewalk")) return;
            Directory.CreateDirectory(SidewalkArtFolder);
            AssetDatabase.Refresh();

            Texture2D tex = DrawSidewalkTexture(SidewalkWidth, SidewalkHeight);
            string path = $"{SidewalkArtFolder}/Sidewalk.png";
            Sprite sprite = SaveTextureAsSimpleSprite(tex, path);

            WireSidewalk(sprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtExpansionTool] Generated Sidewalk.png and wired '" + SidewalkObjectName + "' at PedestrianContainer's own baseline. Save the scene (Ctrl+S) to persist.");
        }

        private static Texture2D DrawSidewalkTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color concrete = HexColor("#B7B5AC");
            Color joint = HexColor("#847F73");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float speckle = Hash01(x, y);
                    Color color = concrete * Mathf.Lerp(0.94f, 1.04f, speckle);
                    color.a = 1f;
                    tex.SetPixel(x, y, color);
                }
            }

            // Curb highlight along the very top edge (where pedestrians' feet meet it).
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, height - 1, Color.Lerp(concrete, Color.white, 0.4f));
                tex.SetPixel(x, height - 2, Color.Lerp(concrete, Color.white, 0.15f));
            }

            // Evenly spaced expansion joints.
            int jointSpacing = 64;
            for (int x = jointSpacing / 2; x < width; x += jointSpacing)
            {
                for (int y = 0; y < height - 2; y++)
                {
                    tex.SetPixel(x, y, joint);
                }
            }

            tex.Apply();
            return tex;
        }

        private static void WireSidewalk(Sprite sprite)
        {
            GameObject pedestrianContainer = GameObject.Find("PedestrianContainer");
            if (pedestrianContainer == null)
            {
                Debug.LogWarning("[ArtExpansionTool] No GameObject named 'PedestrianContainer' found -- cannot align the sidewalk. Create '" + SidewalkObjectName + "' manually with sprite " + AssetDatabase.GetAssetPath(sprite) + ".");
                return;
            }

            RectTransform pedRect = pedestrianContainer.GetComponent<RectTransform>();
            Transform parent = pedestrianContainer.transform.parent;
            if (pedRect == null || parent == null)
            {
                Debug.LogWarning("[ArtExpansionTool] PedestrianContainer has no RectTransform/parent -- cannot align the sidewalk.");
                return;
            }

            // Match PedestrianContainer's own bottom anchor exactly -- that Y (read live, not
            // hardcoded) is precisely where walkBaselineY=0 puts pedestrians' feet, so the
            // sidewalk's top edge lines up with them regardless of any future repositioning.
            float baselineY = pedRect.anchorMin.y;

            Transform existing = parent.Find(SidewalkObjectName);
            GameObject sidewalkObject = existing != null ? existing.gameObject : null;
            if (sidewalkObject == null)
            {
                sidewalkObject = new GameObject(SidewalkObjectName, typeof(RectTransform));
                sidewalkObject.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(sidewalkObject, "Create Sidewalk");
            }

            RectTransform rect = sidewalkObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, baselineY);
            rect.anchorMax = new Vector2(1f, baselineY);
            // 2026-08-31 fix: this used to be pivot (0.5, 1) so the strip hung DOWNWARD from the
            // baseline. That put it entirely below PedestrianContainer's own occupied band
            // (which extends UPWARD from this same baseline), landing squarely in the bottom
            // toolbar's screen space instead -- confirmed by selecting Sidewalk (Generated) in
            // the Scene view and seeing its rect exactly overlap the SHOP/CONVERT/RESTORE bar,
            // fully occluded by those opaque buttons (a live red Color test produced zero visible
            // change, which is what sent this investigation looking for a render bug before the
            // real, purely positional cause was found). Pivot (0.5, 0) instead anchors the
            // sidewalk's BOTTOM edge to the baseline and lets it extend UPWARD, into the lower
            // portion of PedestrianContainer's own band -- i.e. directly behind/under the
            // pedestrians' feet, which is where "sidewalk at peds' feet" actually needs it.
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 48f);

            Image image = sidewalkObject.GetComponent<Image>();
            if (image == null) { image = sidewalkObject.AddComponent<Image>(); }
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            // Behind pedestrians (and everything else in this parent), in front of BackgroundRoot.
            sidewalkObject.transform.SetAsFirstSibling();

            EditorUtility.SetDirty(sidewalkObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ===================== Shared pixel-drawing / asset-pipeline helpers =====================

        private static float SdRoundBox(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x) - halfSize.x + radius, Mathf.Abs(p.y) - halfSize.y + radius);
            float outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(d.x, d.y), 0f);
            return outside + inside - radius;
        }

        /// <summary>Cheap deterministic per-pixel pseudo-random in [0,1) -- no Unity Random seed state, so regenerating is reproducible.</summary>
        private static float Hash01(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) { return; }
            Color existing = tex.GetPixel(x, y);
            float outA = color.a + existing.a * (1f - color.a);
            if (outA <= 0f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); return; }
            Color blended = (color * color.a + existing * existing.a * (1f - color.a)) / outA;
            blended.a = outA;
            tex.SetPixel(x, y, blended);
        }

        private static void FillCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(center.x + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(center.y + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void ClearTransparent(Texture2D tex)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }
            tex.SetPixels(pixels);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        /// <summary>Flips vertically before saving (texture-space Y grows downward when authored top-to-bottom above; sprite-space Y grows upward) -- same convention PlaceholderArtGenerator's SaveTextureAsSprite uses.</summary>
        private static Texture2D FlipVertical(Texture2D texture)
        {
            Texture2D flipped = new Texture2D(texture.width, texture.height, texture.format, false);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    flipped.SetPixel(x, texture.height - 1 - y, texture.GetPixel(x, y));
                }
            }
            flipped.Apply();
            return flipped;
        }

        private static Sprite SaveTextureAsSimpleSprite(Texture2D texture, string assetPath)
        {
            Texture2D flipped = FlipVertical(texture);
            byte[] pngBytes = flipped.EncodeToPNG();
            File.WriteAllBytes(assetPath, pngBytes);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(flipped);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        /// <summary>
        /// 2026-08-31 fix: a Sliced Image's rendered border/corner thickness in canvas units is
        /// (spriteBorder pixels / spritePixelsPerUnit) * the Image's own Pixels Per Unit
        /// Multiplier. At Unity's default import PPU of 100, this 48px texture's 16px slice
        /// border rendered at just 16/100 = 0.16 canvas units -- sub-pixel at this project's
        /// canvas scale, so the border frame was completely invisible on every button even
        /// though the sprite's own pixels were correctly drawn and fully opaque (confirmed by
        /// direct pixel inspection -- this was a render-size bug, not a content bug). A low PPU
        /// of 2 makes that same 16px slice render at 8 canvas units, which live-tested visibly
        /// on RestoreButton (equivalent to Pixels Per Unit Multiplier=50 at the old PPU=100,
        /// confirmed in Play mode before baking this in). Deliberately only applied here, not to
        /// SaveTextureAsSimpleSprite -- Type.Simple images stretch to fill their RectTransform
        /// regardless of PPU, so TopBarAccent/Sidewalk were never affected by this.
        /// </summary>
        private const float BorderSpritePixelsPerUnit = 2f;

        private static Sprite SaveTextureAsSlicedSprite(Texture2D texture, string assetPath, int border)
        {
            Texture2D flipped = FlipVertical(texture);
            byte[] pngBytes = flipped.EncodeToPNG();
            File.WriteAllBytes(assetPath, pngBytes);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(flipped);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = BorderSpritePixelsPerUnit;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        /// <summary>Overwrites a serialized array/list field via SerializedObject -- safe regardless of the field's C# access level. Same technique PlaceholderArtGenerator.AssignListToComponent uses for List&lt;T&gt; fields, generalized here for a plain array field.</summary>
        private static void AssignArrayField(Component component, string fieldName, UnityEngine.Object[] items)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty arrayProp = so.FindProperty(fieldName);
            if (arrayProp == null)
            {
                Debug.LogWarning($"[ArtExpansionTool] Could not find serialized field '{fieldName}' on {component.GetType().Name}.");
                return;
            }

            arrayProp.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedProperties();
        }
    }
}
#endif
