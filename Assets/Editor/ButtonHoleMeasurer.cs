#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-09-30 polish pass (item 4): measures each stage's ButtonBorder_Stage{N}.png baked
    /// fill hole and writes the result as a normalized labelHoleInsets Vector4 onto the matching
    /// ButtonTheme_Stage{N}.asset, so UniversalButtonBorderApplier can stretch a framed button's
    /// label to the frame's ACTUAL hole instead of the button's own flat rect -- the two differ
    /// per stage (Stage 5's hole sits well below the sprite's geometric center, confirmed by this
    /// same measurement technique in an earlier diagnosis pass), which is why labels sat high on
    /// some stages.
    ///
    /// Read-only against the source art, destructive-safe against import settings: temporarily
    /// flips TextureImporter.isReadable on if needed, measures, then restores the EXACT prior
    /// import settings and reimports -- never leaves a texture's read/write flag permanently
    /// changed as a side effect of running this tool.
    /// </summary>
    public static class ButtonHoleMeasurer
    {
        /// <summary>The baked fill color every stage's border art was flood-filled with -- see
        /// the paint-bucket pass that generated these PNGs.</summary>
        private static readonly Color32 FillColor = new Color32(0x1B, 0x0F, 0x2E, 0xFF);

        /// <summary>Per-channel tolerance for matching FillColor, out of 255 -- mirrors the
        /// flood-fill pass's own compositing, which can leave anti-aliased edge pixels a few
        /// values off pure FillColor.</summary>
        private const int ToleranceByte = 6;

        private const string ButtonThemeFolder = "Assets/_Game/ButtonThemes";

        [MenuItem("BrainDrain/Tools/Measure Button Hole Insets")]
        public static void MeasureAndApply()
        {
            string[] guids = AssetDatabase.FindAssets("t:ButtonTheme", new[] { ButtonThemeFolder });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[ButtonHoleMeasurer] No ButtonTheme assets found under {ButtonThemeFolder}.");
                return;
            }

            int measured = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ButtonTheme theme = AssetDatabase.LoadAssetAtPath<ButtonTheme>(path);
                if (theme == null || theme.borderSprite == null)
                {
                    Debug.LogWarning($"[ButtonHoleMeasurer] Skipping '{path}' -- no borderSprite assigned.");
                    continue;
                }

                if (TryMeasureHoleInsets(theme.borderSprite.texture, out Vector4 insets))
                {
                    SerializedObject so = new SerializedObject(theme);
                    so.FindProperty("labelHoleInsets").vector4Value = insets;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(theme);
                    measured++;
                    Debug.Log($"[ButtonHoleMeasurer] {theme.name}: labelHoleInsets = (L {insets.x:F4}, B {insets.y:F4}, R {insets.z:F4}, T {insets.w:F4})");
                }
                else
                {
                    Debug.LogWarning($"[ButtonHoleMeasurer] {theme.name}: no matching fill pixels found on '{theme.borderSprite.texture.name}' -- labelHoleInsets left unchanged.");
                }
            }

            if (measured > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[ButtonHoleMeasurer] Done -- measured {measured}/{guids.Length} theme(s).");
        }

        /// <summary>
        /// Finds the bounding box of FillColor-matching pixels (within ToleranceByte per channel)
        /// and returns it as normalized (x=left, y=bottom, z=right, w=top) insets from the
        /// texture's own edges -- same component order as Unity's own Sprite.border, and the same
        /// bottom-up Y convention Texture2D.GetPixels32/RectTransform both already use, so no axis
        /// flip is needed when UniversalButtonBorderApplier later turns this into anchor offsets.
        /// </summary>
        private static bool TryMeasureHoleInsets(Texture2D texture, out Vector4 insets)
        {
            insets = default;
            if (texture == null) { return false; }

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ButtonHoleMeasurer] '{path}' has no TextureImporter -- cannot read pixels safely.");
                return false;
            }

            bool originalIsReadable = importer.isReadable;
            bool reimportedForReadability = false;

            try
            {
                if (!originalIsReadable)
                {
                    importer.isReadable = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    reimportedForReadability = true;
                }

                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;

                int minX = width, maxX = -1, minY = height, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        Color32 p = pixels[rowOffset + x];
                        if (p.a < 250) { continue; }
                        if (System.Math.Abs(p.r - FillColor.r) > ToleranceByte) { continue; }
                        if (System.Math.Abs(p.g - FillColor.g) > ToleranceByte) { continue; }
                        if (System.Math.Abs(p.b - FillColor.b) > ToleranceByte) { continue; }

                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }

                if (maxX < 0)
                {
                    return false;
                }

                float left = minX / (float)width;
                float right = (width - 1 - maxX) / (float)width;
                float bottom = minY / (float)height;
                float top = (height - 1 - maxY) / (float)height;

                insets = new Vector4(left, bottom, right, top);
                return true;
            }
            finally
            {
                if (reimportedForReadability)
                {
                    importer.isReadable = originalIsReadable;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
#endif
