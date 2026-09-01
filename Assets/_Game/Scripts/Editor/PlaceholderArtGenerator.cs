#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    /// Generates simple procedural placeholder sprites (thick black outline, neon fill, no
    /// external art) for COGS portraits, matching AnimationController.GetRandomSplatSprite's
    /// existing precedent of hand-drawn Texture2D placeholders -- just persisted to disk as
    /// real Sprite assets instead of an ephemeral runtime texture. Run via the BrainDrain menu;
    /// this only ever runs in the Editor.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        private const int TextureSize = 128;
        private const float OutlineThickness = 8f;
        private static readonly Color FeatureColor = Color.black;

        private const string COGSArtFolder = "Assets/_Game/Art/COGS";

        [MenuItem("BrainDrain/Generate Placeholder Art/COGS")]
        public static void GenerateAll()
        {
            if (EditorToolGuard.BlockedByPlayMode("PlaceholderArtGenerator.GenerateAll")) return;
            Directory.CreateDirectory(COGSArtFolder);
            AssetDatabase.Refresh();

            GenerateCOGSPortraits();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlaceholderArtGenerator] Done. Generated sprites, assigned them to data assets, and wired the COGS portrait controller's array in the active scene. Save the scene (Ctrl+S) to persist the wiring.");
        }

        // ===================== COGS portraits =====================

        private struct COGSExpressionDef
        {
            public string Name;
            public Color NeonColor;
            public Action<Texture2D, Vector2, float> DrawFace;
        }

        private static void GenerateCOGSPortraits()
        {
            COGSExpressionDef[] expressions =
            {
                new COGSExpressionDef { Name = "Neutral", NeonColor = HexColor("#00F0FF"), DrawFace = DrawNeutralFace },
                new COGSExpressionDef { Name = "Smug", NeonColor = HexColor("#FF007F"), DrawFace = DrawSmugFace },
                new COGSExpressionDef { Name = "Concerned", NeonColor = HexColor("#FFB000"), DrawFace = DrawConcernedFace },
                new COGSExpressionDef { Name = "Smirking", NeonColor = HexColor("#FF6A00"), DrawFace = DrawSmirkingFace },
                new COGSExpressionDef { Name = "Horrified", NeonColor = HexColor("#FF003C"), DrawFace = DrawHorrifiedFace },
                new COGSExpressionDef { Name = "Unhinged", NeonColor = HexColor("#FF00F0"), DrawFace = DrawUnhingedFace },
            };

            List<COGSStage> stages = FindAllAssets<COGSStage>();
            stages.Sort((a, b) => a.minRebirthCount.CompareTo(b.minRebirthCount));

            var orderedStages = new List<UnityEngine.Object>();

            for (int i = 0; i < expressions.Length; i++)
            {
                Texture2D tex = CreateFaceTexture(expressions[i].NeonColor, expressions[i].DrawFace);
                string path = $"{COGSArtFolder}/COGS_{i}_{expressions[i].Name}.png";
                Sprite sprite = SaveTextureAsSprite(tex, path);

                if (i < stages.Count && stages[i] != null)
                {
                    stages[i].portraitSprite = sprite;
                    EditorUtility.SetDirty(stages[i]);
                    orderedStages.Add(stages[i]);
                }
                else
                {
                    Debug.LogWarning($"[PlaceholderArtGenerator] No COGSStage asset at sorted index {i} ('{expressions[i].Name}') to receive a portrait -- expected 6 existing COGSStage assets.");
                }
            }

            AssignListToComponent<COGSPortraitController>("stages", orderedStages);
        }

        private static Texture2D CreateFaceTexture(Color neonColor, Action<Texture2D, Vector2, float> drawFeatures)
        {
            Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            ClearTransparent(tex);

            Vector2 center = new Vector2(TextureSize / 2f, TextureSize / 2f);
            float outerRadius = TextureSize * 0.42f;

            FillCircle(tex, center, outerRadius, Color.black);
            FillCircle(tex, center, outerRadius - OutlineThickness, neonColor);

            drawFeatures(tex, center, outerRadius - OutlineThickness);

            tex.Apply();
            return tex;
        }

        private static void DrawNeutralFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.15f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.15f * r);
            float eyeRadius = 0.12f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);
            FillCircle(tex, rightEye, eyeRadius, FeatureColor);

            DrawPolyline(tex, new[]
            {
                c + new Vector2(-0.3f * r, 0.32f * r),
                c + new Vector2(0.3f * r, 0.32f * r)
            }, 0.06f * r, FeatureColor);
        }

        private static void DrawSmugFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.15f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.15f * r);
            float eyeRadius = 0.12f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);
            // Right eye squinted into a thin line -- a smug half-wink.
            DrawThickLine(tex, rightEye + new Vector2(-eyeRadius, 0f), rightEye + new Vector2(eyeRadius, 0f), 0.05f * r, FeatureColor);
            DrawThickLine(tex, rightEye + new Vector2(-eyeRadius, -eyeRadius * 1.6f), rightEye + new Vector2(eyeRadius, -eyeRadius * 2.2f), 0.05f * r, FeatureColor);

            DrawPolyline(tex, new[]
            {
                c + new Vector2(-0.3f * r, 0.3f * r),
                c + new Vector2(0.05f * r, 0.28f * r),
                c + new Vector2(0.35f * r, 0.12f * r)
            }, 0.06f * r, FeatureColor);
        }

        private static void DrawConcernedFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.1f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.1f * r);
            float eyeRadius = 0.13f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);
            FillCircle(tex, rightEye, eyeRadius, FeatureColor);

            // Inward-angled worried eyebrows.
            DrawThickLine(tex, leftEye + new Vector2(-eyeRadius * 1.2f, -eyeRadius * 1.6f), leftEye + new Vector2(eyeRadius * 1.2f, -eyeRadius * 2.6f), 0.05f * r, FeatureColor);
            DrawThickLine(tex, rightEye + new Vector2(eyeRadius * 1.2f, -eyeRadius * 1.6f), rightEye + new Vector2(-eyeRadius * 1.2f, -eyeRadius * 2.6f), 0.05f * r, FeatureColor);

            DrawPolyline(tex, new[]
            {
                c + new Vector2(-0.28f * r, 0.28f * r),
                c + new Vector2(0f, 0.38f * r),
                c + new Vector2(0.28f * r, 0.28f * r)
            }, 0.06f * r, FeatureColor);
        }

        private static void DrawSmirkingFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.15f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.15f * r);
            float eyeRadius = 0.12f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);
            FillCircle(tex, rightEye, eyeRadius, FeatureColor);

            DrawPolyline(tex, new[]
            {
                c + new Vector2(-0.32f * r, 0.3f * r),
                c + new Vector2(0.1f * r, 0.26f * r),
                c + new Vector2(0.38f * r, 0.05f * r)
            }, 0.07f * r, FeatureColor);
        }

        private static void DrawHorrifiedFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.15f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.15f * r);
            float eyeRadius = 0.2f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);
            FillCircle(tex, rightEye, eyeRadius, FeatureColor);
            FillCircle(tex, leftEye, eyeRadius * 0.4f, Color.white);
            FillCircle(tex, rightEye, eyeRadius * 0.4f, Color.white);

            DrawThickLine(tex, leftEye + new Vector2(-eyeRadius, -eyeRadius * 1.8f), leftEye + new Vector2(eyeRadius * 0.4f, -eyeRadius * 2.4f), 0.05f * r, FeatureColor);
            DrawThickLine(tex, rightEye + new Vector2(eyeRadius, -eyeRadius * 1.8f), rightEye + new Vector2(-eyeRadius * 0.4f, -eyeRadius * 2.4f), 0.05f * r, FeatureColor);

            FillEllipse(tex, c + new Vector2(0f, 0.32f * r), 0.14f * r, 0.22f * r, FeatureColor);
        }

        private static void DrawUnhingedFace(Texture2D tex, Vector2 c, float r)
        {
            Vector2 leftEye = c + new Vector2(-0.35f * r, -0.15f * r);
            Vector2 rightEye = c + new Vector2(0.35f * r, -0.15f * r);
            float eyeRadius = 0.14f * r;

            FillCircle(tex, leftEye, eyeRadius, FeatureColor);

            // Crazy "X" eye.
            DrawThickLine(tex, rightEye + new Vector2(-eyeRadius, -eyeRadius), rightEye + new Vector2(eyeRadius, eyeRadius), 0.05f * r, FeatureColor);
            DrawThickLine(tex, rightEye + new Vector2(-eyeRadius, eyeRadius), rightEye + new Vector2(eyeRadius, -eyeRadius), 0.05f * r, FeatureColor);

            // Jagged manic grin.
            DrawPolyline(tex, new[]
            {
                c + new Vector2(-0.32f * r, 0.22f * r),
                c + new Vector2(-0.16f * r, 0.36f * r),
                c + new Vector2(0f, 0.2f * r),
                c + new Vector2(0.16f * r, 0.36f * r),
                c + new Vector2(0.32f * r, 0.22f * r)
            }, 0.06f * r, FeatureColor);
        }

        // ===================== Nudge pointer (Option B, tutorial-direction-and-cogs-trust.md) =====================

        private const string NudgeArtFolder = "Assets/_Game/Art/UI";
        private const string NudgePointerObjectName = "UINudgePointer (Generated)";
        private static readonly Color NudgeArrowColor = HexColor("#FFD700"); // same gold already used for UpgradeSlotUI's "TOTAL" highlight -- reads as a player-facing hint, distinct from COGS's own cyan/pink neon palette.

        /// <summary>
        /// Generates the single "look here" arrow sprite UINudgePointer.cs uses and wires an
        /// instance of that component into the active scene under the root Canvas -- the
        /// procedural-art route (Option B.1) tutorial-direction-and-cogs-trust.md recommended
        /// over hand-authoring a pointer/spotlight asset. Deliberately arrow-only for this pass:
        /// a ring/spotlight variant would reuse the same FillEllipse-minus-inner-FillEllipse
        /// technique CreateFaceTexture's outline already demonstrates, but nothing calls for one
        /// yet, so it isn't built speculatively. Idempotent: re-running finds and updates the
        /// existing object instead of duplicating it, same as GenerateCOGSPortraits's stage
        /// lookup.
        /// </summary>
        [MenuItem("BrainDrain/Generate Placeholder Art/Nudge Pointer")]
        public static void GenerateNudgePointer()
        {
            if (EditorToolGuard.BlockedByPlayMode("PlaceholderArtGenerator.GenerateNudgePointer")) return;
            Directory.CreateDirectory(NudgeArtFolder);
            AssetDatabase.Refresh();

            Sprite arrowSprite = GenerateArrowSprite();
            WireNudgePointerObject(arrowSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlaceholderArtGenerator] Done. Generated Assets/_Game/Art/UI/NudgeArrow.png and created/updated '" + NudgePointerObjectName + "' under Canvas in the active scene (starts hidden -- UINudgePointer.Awake() disables its Image until something calls PointAt). Save the scene (Ctrl+S) to persist the wiring.");
        }

        private static Sprite GenerateArrowSprite()
        {
            const int width = 96;
            const int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            ClearTransparent(tex);

            // Shaft: black outline pass (wider) then gold fill (narrower) -- the same two-pass
            // outline technique CreateFaceTexture uses for the COGS portrait circles.
            // NOTE: SaveTextureAsSprite flips the texture vertically before writing it to disk
            // (see that method's comment), so a low pre-flip y ends up at the TOP of the saved
            // sprite and a high pre-flip y ends up at the BOTTOM -- the same convention
            // CreateFaceTexture's eyes (negative offset -> top) and mouth (positive offset ->
            // bottom) already rely on. The tip needs to land at the bottom (pointing down at
            // whatever this hovers above), so it gets the highest y here, not the lowest.
            Vector2 shaftTop = new Vector2(width * 0.5f, height * 0.08f);
            Vector2 shaftBottom = new Vector2(width * 0.5f, height * 0.58f);
            DrawThickLine(tex, shaftTop, shaftBottom, width * 0.30f, Color.black);
            DrawThickLine(tex, shaftTop, shaftBottom, width * 0.16f, NudgeArrowColor);

            // Arrowhead: a downward-pointing triangle, same outline trick via a larger black
            // triangle scaled out from the centroid, then a smaller gold triangle on top.
            Vector2 tip = new Vector2(width * 0.5f, height * 0.94f);
            Vector2 headLeft = new Vector2(width * 0.10f, height * 0.54f);
            Vector2 headRight = new Vector2(width * 0.90f, height * 0.54f);
            Vector2 centroid = (tip + headLeft + headRight) / 3f;
            const float outlineScale = 1.35f;

            FillTriangle(tex, centroid + (tip - centroid) * outlineScale, centroid + (headLeft - centroid) * outlineScale, centroid + (headRight - centroid) * outlineScale, Color.black);
            FillTriangle(tex, tip, headLeft, headRight, NudgeArrowColor);

            tex.Apply();
            return SaveTextureAsSprite(tex, $"{NudgeArtFolder}/NudgeArrow.png");
        }

        /// <summary>
        /// Finds (or creates) the pointer's GameObject as a direct child of the scene's root
        /// "Canvas" object -- it has to live at that level (not under a scroll view's content, or
        /// it would clip when a target row scrolls out of view) since UINudgePointer repositions
        /// it every frame in that canvas's local space via RectTransformUtility. Pivot is bottom-
        /// center (0.5, 0) so anchoredPosition always refers to the arrow's tip, matching how
        /// UINudgePointer.RepositionOverTarget hovers that tip just above a target's top edge.
        /// </summary>
        private static void WireNudgePointerObject(Sprite arrowSprite)
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                Debug.LogWarning("[PlaceholderArtGenerator] No GameObject named 'Canvas' found in the active scene -- cannot host the nudge pointer. Create '" + NudgePointerObjectName + "' manually under your root canvas with an Image (sprite: " + AssetDatabase.GetAssetPath(arrowSprite) + ") and a UINudgePointer component.");
                return;
            }

            Transform existing = canvasObject.transform.Find(NudgePointerObjectName);
            GameObject nudgeObject = existing != null ? existing.gameObject : null;

            if (nudgeObject == null)
            {
                nudgeObject = new GameObject(NudgePointerObjectName, typeof(RectTransform));
                nudgeObject.transform.SetParent(canvasObject.transform, false);
                Undo.RegisterCreatedObjectUndo(nudgeObject, "Create UINudgePointer");
            }

            RectTransform rect = nudgeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(64f, 85.3f); // matches the 96x128 arrow texture's aspect ratio at a readable on-screen size

            Image image = nudgeObject.GetComponent<Image>();
            if (image == null) image = nudgeObject.AddComponent<Image>();
            image.sprite = arrowSprite;
            image.preserveAspect = true;
            image.raycastTarget = false; // purely visual -- must never intercept the tap meant for whatever it's pointing at
            image.enabled = false; // UINudgePointer.Awake() also does this at runtime; set here too so the saved scene doesn't show it lit up in the Editor

            if (nudgeObject.GetComponent<UINudgePointer>() == null)
            {
                nudgeObject.AddComponent<UINudgePointer>();
            }

            // 2026-08-31: living directly under the root Canvas (plain sibling order, no
            // override) is not enough on its own -- several other UI systems (e.g.
            // ShopUIController's per-tab content) give their own subtree a nested Canvas with
            // overrideSorting so THEY layer correctly among themselves, and a nested
            // overrideSorting Canvas renders relative to other Canvases purely by sortingOrder,
            // ignoring Transform sibling position in a shared ancestor entirely. Without its own
            // override here, this pointer could never render above that content no matter its
            // sibling index. UINudgePointer.Awake() also enforces this at runtime (so it's correct
            // even if this generator isn't re-run against an already-saved object), but setting it
            // here too keeps a freshly-generated object correct without needing Play mode.
            Canvas nudgeCanvas = nudgeObject.GetComponent<Canvas>();
            if (nudgeCanvas == null) nudgeCanvas = nudgeObject.AddComponent<Canvas>();
            nudgeCanvas.overrideSorting = true;
            nudgeCanvas.sortingOrder = 10; // above ShopUIController.TabContentSortingOrder (1), below IntelCardUI.OverlaySortingOrder (500)

            nudgeObject.transform.SetAsLastSibling();
            EditorUtility.SetDirty(nudgeObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void FillTriangle(Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (IsInsideTriangle(p, a, b, c))
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p - a, b - a);
            float d2 = Cross(p - b, c - b);
            float d3 = Cross(p - c, a - c);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // ===================== Shared asset-pipeline helpers =====================

        private static List<T> FindAllAssets<T>() where T : UnityEngine.Object
        {
            var results = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }
            return results;
        }

        private static Sprite SaveTextureAsSprite(Texture2D texture, string assetPath)
        {
            // Flip the texture vertically before saving so character is upright
            Texture2D flipped = new Texture2D(texture.width, texture.height, texture.format, false);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    flipped.SetPixel(x, texture.height - 1 - y, texture.GetPixel(x, y));
                }
            }
            flipped.Apply();

            byte[] pngBytes = flipped.EncodeToPNG();
            File.WriteAllBytes(assetPath, pngBytes);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(flipped);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        /// <summary>
        /// Finds (or creates) a scene instance of TComponent and overwrites its named list field
        /// via SerializedObject -- safe regardless of the field's C# access level, and avoids
        /// hand-editing the .unity YAML directly. Marks the active scene dirty; does not save it
        /// (left to the caller, to avoid racing a concurrently-open Editor session's own save).
        /// </summary>
        private static void AssignListToComponent<TComponent>(string fieldName, List<UnityEngine.Object> items) where TComponent : Component
        {
            TComponent controller = UnityEngine.Object.FindAnyObjectByType<TComponent>();
            if (controller == null)
            {
                var host = new GameObject(typeof(TComponent).Name);
                controller = host.AddComponent<TComponent>();
                Undo.RegisterCreatedObjectUndo(host, "Create " + typeof(TComponent).Name);
            }

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty listProp = so.FindProperty(fieldName);
            if (listProp == null)
            {
                Debug.LogWarning($"[PlaceholderArtGenerator] Could not find serialized field '{fieldName}' on {typeof(TComponent).Name}.");
                return;
            }

            listProp.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        // ===================== Pixel-drawing primitives =====================

        private static void ClearTransparent(Texture2D tex)
        {
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }
            tex.SetPixels(pixels);
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

        private static void FillEllipse(Texture2D tex, Vector2 center, float radiusX, float radiusY, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radiusX));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(center.x + radiusX));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radiusY));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(center.y + radiusY));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (x + 0.5f - center.x) / radiusX;
                    float ny = (y + 0.5f - center.y) / radiusY;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawThickLine(Texture2D tex, Vector2 from, Vector2 to, float thickness, Color color)
        {
            float distance = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
                FillCircle(tex, point, thickness * 0.5f, color);
            }
        }

        private static void DrawPolyline(Texture2D tex, Vector2[] points, float thickness, Color color)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawThickLine(tex, points[i], points[i + 1], thickness, color);
            }
        }
    }
}
#endif
