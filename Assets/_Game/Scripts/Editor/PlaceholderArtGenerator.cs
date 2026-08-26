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
