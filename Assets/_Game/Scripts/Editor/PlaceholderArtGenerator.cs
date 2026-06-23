#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Generates simple procedural placeholder sprites (thick black outline, neon fill, no
    /// external art) for COGS portraits and Player Character appearance stages, matching
    /// AnimationController.GetRandomSplatSprite's existing precedent of hand-drawn Texture2D
    /// placeholders -- just persisted to disk as real Sprite assets instead of an ephemeral
    /// runtime texture. Run via the BrainDrain menu; this only ever runs in the Editor.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        private const int TextureSize = 128;
        private const float OutlineThickness = 8f;
        private static readonly Color FeatureColor = Color.black;

        private const string COGSArtFolder = "Assets/_Game/Art/COGS";
        private const string CharacterArtFolder = "Assets/_Game/Art/PlayerCharacter";
        private const string CharacterDataFolder = "Assets/_Game/Character";

        [MenuItem("BrainDrain/Generate Placeholder Art/COGS + Player Character")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(COGSArtFolder);
            Directory.CreateDirectory(CharacterArtFolder);
            Directory.CreateDirectory(CharacterDataFolder);
            AssetDatabase.Refresh();

            GenerateCOGSPortraits();
            GenerateCharacterAppearanceStages();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlaceholderArtGenerator] Done. Generated sprites, assigned them to data assets, and wired both controllers' arrays in the active scene. Save the scene (Ctrl+S) to persist the wiring.");
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

        // ===================== Player Character silhouettes =====================

        private struct CharacterStageDef
        {
            public string Name;
            public int MinRebirthCount;
            public Color OutlineColor;
        }

        private static void GenerateCharacterAppearanceStages()
        {
            // No CharacterAppearanceStage assets exist yet, and the class has no enum to match
            // against -- these 5 stages/thresholds are a fresh, reasonable default, not a
            // pre-established spec. Adjust freely.
            CharacterStageDef[] stageDefs =
            {
                new CharacterStageDef { Name = "DimOutline", MinRebirthCount = 0, OutlineColor = HexColor("#4A4E5D") },
                new CharacterStageDef { Name = "CyanOutline", MinRebirthCount = 1, OutlineColor = HexColor("#00F0FF") },
                new CharacterStageDef { Name = "MagentaOutline", MinRebirthCount = 3, OutlineColor = HexColor("#FF007F") },
                new CharacterStageDef { Name = "GoldOutline", MinRebirthCount = 6, OutlineColor = HexColor("#FFD500") },
                new CharacterStageDef { Name = "WhiteHotOutline", MinRebirthCount = 11, OutlineColor = Color.white },
            };

            List<CharacterAppearanceStage> existing = FindAllAssets<CharacterAppearanceStage>();
            existing.Sort((a, b) => a.minRebirthCount.CompareTo(b.minRebirthCount));

            var orderedStages = new List<UnityEngine.Object>();

            for (int i = 0; i < stageDefs.Length; i++)
            {
                Texture2D tex = CreateDetailedCharacterTexture(i, stageDefs[i].OutlineColor);
                string pngPath = $"{CharacterArtFolder}/PlayerCharacter_{i}_{stageDefs[i].Name}.png";
                Sprite sprite = SaveTextureAsSprite(tex, pngPath);

                CharacterAppearanceStage stage = i < existing.Count ? existing[i] : null;
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<CharacterAppearanceStage>();
                    string assetPath = $"{CharacterDataFolder}/CharacterAppearanceStage_{i}_{stageDefs[i].Name}.asset";
                    AssetDatabase.CreateAsset(stage, assetPath);
                }

                stage.sprite = sprite;
                stage.minRebirthCount = stageDefs[i].MinRebirthCount;
                stage.stageName = stageDefs[i].Name;
                EditorUtility.SetDirty(stage);

                orderedStages.Add(stage);
            }

            AssignListToComponent<PlayerCharacterController>("appearanceStages", orderedStages);
        }

        private static Texture2D CreateDetailedCharacterTexture(int stageIndex, Color outlineColor)
        {
            Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            ClearTransparent(tex);

            // Pass 1: Chunky outer outline (outlineColor, padding = 4f)
            DrawCharacterComposite(tex, stageIndex, outlineColor, 4f, true);

            // Pass 2: Chunky inner shadow/border (solid black, padding = 0f)
            DrawCharacterComposite(tex, stageIndex, Color.black, 0f, true);

            // Pass 3: Detailed coloring (padding = 0f, isOutline = false)
            DrawCharacterComposite(tex, stageIndex, Color.clear, 0f, false);

            tex.Apply();
            return tex;
        }

        private static void DrawCharacterComposite(Texture2D tex, int stageIndex, Color c, float pad, bool isOutline)
        {
            float cx = TextureSize * 0.5f;
            float headCenterY = TextureSize * 0.28f;
            float headRadius = TextureSize * 0.16f + pad;

            float bodyTop = TextureSize * 0.44f - pad;
            float bodyBottom = TextureSize * 0.94f + pad;
            float bodyHalfWidthTop = TextureSize * 0.26f + pad;
            float bodyHalfWidthBottom = TextureSize * 0.18f + pad;

            Color bodyColor = isOutline ? c : HexColor("#0045A5"); // Vault Blue
            Color stripeColor = isOutline ? c : HexColor("#FFCC00"); // Vault Yellow Stripe
            Color skinColor = isOutline ? c : HexColor("#FFDAB9"); // Peach skin
            Color hairColor = isOutline ? c : HexColor("#5C2E0B"); // Brown hair
            Color eyesColor = isOutline ? c : Color.white;
            Color pupilsColor = isOutline ? c : Color.black;
            Color mouthColor = isOutline ? c : Color.black;

            // 1. Draw Body
            FillTrapezoid(tex, cx, bodyTop, bodyBottom, bodyHalfWidthTop, bodyHalfWidthBottom, bodyColor);

            // Yellow middle stripe (only if not outline)
            if (!isOutline)
            {
                FillTrapezoid(tex, cx, bodyTop, bodyBottom, 3f, 3f, stripeColor);
            }

            // 2. Draw Neck
            Color neckColor = isOutline ? c : skinColor;
            FillTrapezoid(tex, cx, TextureSize * 0.40f - pad, TextureSize * 0.45f + pad, 6f + pad, 6f + pad, neckColor);

            // 3. Draw Head
            Color headColor = isOutline ? c : skinColor;
            FillCircle(tex, new Vector2(cx, headCenterY), headRadius, headColor);

            // 4. Draw Messy Hair (drawn on top/sides of head)
            // Draw overlapping hair circles to create a funny bed-head
            float hairRadius = 6f + pad;
            Vector2[] hairPoints = {
                new Vector2(cx - 15f, headCenterY - 12f),
                new Vector2(cx, headCenterY - 18f),
                new Vector2(cx + 15f, headCenterY - 12f),
                new Vector2(cx - 18f, headCenterY),
                new Vector2(cx + 18f, headCenterY),
                new Vector2(cx - 8f, headCenterY - 16f),
                new Vector2(cx + 8f, headCenterY - 16f)
            };
            foreach (var pt in hairPoints)
            {
                FillCircle(tex, pt, hairRadius, hairColor);
            }

            // 5. Draw Face Features (only if stageIndex < 4 or isOutline is true)
            if (stageIndex < 4)
            {
                Vector2 leftEye = new Vector2(cx - 7f, headCenterY - 2f);
                Vector2 rightEye = new Vector2(cx + 7f, headCenterY - 2f);
                float eyeRad = 4f + pad;

                // Draw Eyes Background
                FillCircle(tex, leftEye, eyeRad, eyesColor);
                FillCircle(tex, rightEye, eyeRad, eyesColor);

                if (!isOutline)
                {
                    // Pupils slightly crossed
                    FillCircle(tex, leftEye + new Vector2(1f, 0f), 1.5f, pupilsColor);
                    FillCircle(tex, rightEye + new Vector2(-1f, 0f), 1.5f, pupilsColor);
                }

                // Mouth
                FillCircle(tex, new Vector2(cx, headCenterY + 7f), 3f + pad, mouthColor);
            }

            // 6. Stage-specific accessories
            if (stageIndex == 0)
            {
                // Dazed cryo frost on suit
                if (!isOutline)
                {
                    Color frostColor = HexColor("#E0FFFF");
                    FillCircle(tex, new Vector2(cx - 12f, bodyTop + 15f), 2f, frostColor);
                    FillCircle(tex, new Vector2(cx + 12f, bodyTop + 25f), 1.5f, frostColor);
                    FillCircle(tex, new Vector2(cx - 8f, bodyTop + 35f), 2.5f, frostColor);
                }
            }
            else if (stageIndex == 1)
            {
                // Visor across eyes
                Color visorColor = isOutline ? c : HexColor("#00FFFF"); // Neon Cyan
                float visorW = 18f + pad;
                float visorH = 6f + pad;
                FillEllipse(tex, new Vector2(cx, headCenterY - 2f), visorW, visorH, visorColor);
                if (!isOutline)
                {
                    // Add a bright white reflection line on visor
                    DrawThickLine(tex, new Vector2(cx - 10f, headCenterY - 4f), new Vector2(cx + 2f, headCenterY - 4f), 1.5f, Color.white);
                }
            }
            else if (stageIndex == 2)
            {
                // Magenta Vest
                Color vestColor = isOutline ? c : HexColor("#FF007F"); // Magenta
                FillTrapezoid(tex, cx - 18f - pad, bodyTop, bodyBottom, 5f + pad, 5f + pad, vestColor);
                FillTrapezoid(tex, cx + 18f + pad, bodyTop, bodyBottom, 5f + pad, 5f + pad, vestColor);
            }
            else if (stageIndex == 3)
            {
                // Gold Crown/Headpiece of spikes
                Color crownColor = isOutline ? c : HexColor("#FFD500"); // Gold
                float spikeW = 3f + pad;
                float spikeH = 10f + pad;
                FillEllipse(tex, new Vector2(cx, headCenterY - 22f), spikeW, spikeH, crownColor);
                FillEllipse(tex, new Vector2(cx - 12f, headCenterY - 20f), spikeW, spikeH, crownColor);
                FillEllipse(tex, new Vector2(cx + 12f, headCenterY - 20f), spikeW, spikeH, crownColor);
            }
            else if (stageIndex == 4)
            {
                // Giant Exposed Pulsating Brain inside a glass dome!
                Color brainColor = isOutline ? c : HexColor("#FF69B4"); // Pink lobes
                float lobeRad = 7f + pad;
                
                // Brain lobes
                Vector2[] brainLobes = {
                    new Vector2(cx - 6f, headCenterY - 6f),
                    new Vector2(cx + 6f, headCenterY - 6f),
                    new Vector2(cx - 8f, headCenterY + 4f),
                    new Vector2(cx + 8f, headCenterY + 4f),
                    new Vector2(cx, headCenterY - 12f)
                };
                foreach (var lobe in brainLobes)
                {
                    FillCircle(tex, lobe, lobeRad, brainColor);
                }

                // Glass Dome around the brain
                Color domeColor = isOutline ? c : new Color(0.9f, 0.95f, 1f, 0.4f);
                float domeRad = 23f + pad;
                if (isOutline)
                {
                    FillCircle(tex, new Vector2(cx, headCenterY), domeRad, c);
                }
                else
                {
                    // Draw a thin white glass reflections circle
                    for (int y = 0; y < TextureSize; y++)
                    {
                        for (int x = 0; x < TextureSize; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, headCenterY));
                            if (d >= domeRad - 1.5f && d <= domeRad + 1.5f)
                            {
                                tex.SetPixel(x, y, Color.white);
                            }
                        }
                    }
                }

                // Gold chains on chest
                Color goldColor = isOutline ? c : HexColor("#FFD500");
                FillCircle(tex, new Vector2(cx - 10f, bodyTop + 14f), 4f + pad, goldColor);
                FillCircle(tex, new Vector2(cx + 10f, bodyTop + 14f), 4f + pad, goldColor);
                FillCircle(tex, new Vector2(cx, bodyTop + 18f), 5f + pad, goldColor);
            }
        }

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

        private static void FillTrapezoid(Texture2D tex, float cx, float yTop, float yBottom, float halfWidthTop, float halfWidthBottom, Color color)
        {
            int minY = Mathf.Max(0, Mathf.FloorToInt(yTop));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(yBottom));

            for (int y = minY; y <= maxY; y++)
            {
                float t = Mathf.InverseLerp(yTop, yBottom, y);
                float halfWidth = Mathf.Lerp(halfWidthTop, halfWidthBottom, t);
                int minX = Mathf.Max(0, Mathf.FloorToInt(cx - halfWidth));
                int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + halfWidth));

                for (int x = minX; x <= maxX; x++)
                {
                    tex.SetPixel(x, y, color);
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
