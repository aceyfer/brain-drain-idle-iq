#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-12 pre-launch size audit, shipped-build win #1. The audit found every PNG under
    /// Assets/_Game/Sprites (Backgrounds, Pedestrians, Buildings, VintageTV, etc.) had no
    /// Android-specific platformSettings override -- so Android silently inherited
    /// DefaultTexturePlatform's textureCompression: 0 (Uncompressed), even though Standalone and
    /// iOS were already correctly set to Compressed on the exact same files. Android is the
    /// actual launch platform (PROJECT_BIBLE.md SS1), so it was the one platform shipping these
    /// uncompressed.
    ///
    /// This adds an explicit Android override -- ASTC 6x6 (nothing in the existing Standalone/iOS
    /// settings specifies ETC2 anywhere, they both just say "Automatic" format + Compressed, so
    /// there's no existing signal pointing at ETC2; ASTC 6x6 is Unity's own recommended balanced
    /// default for modern Android), max size 2048, compressionQuality 50 (matching the one file
    /// that already had a correct Android override, Sidewalk.png) -- to every PNG that doesn't
    /// already have one. Anything already overridden (Sidewalk.png, ButtonBorder_Stage5.png) is
    /// left exactly as authored, not touched twice. Pedestrians_backup_20260625/ is explicitly
    /// excluded -- a protected zone per PROJECT_BIBLE.md SS5, no edits without explicit approval,
    /// and this pass doesn't have that for the backup specifically.
    ///
    /// Import-setting only: TextureImporterPlatformSettings only carries format/maxSize/
    /// compression fields. Texture type, sprite mode, sprite border, pivot, and every other
    /// TextureImporter field are untouched. Goes through TextureImporter.SetPlatformTextureSettings
    /// + SaveAndReimport so Unity itself serializes the .meta -- never hand-edited YAML.
    /// </summary>
    public static class AndroidTextureCompressionFix
    {
        private const string TargetRoot = "Assets/_Game/Sprites";
        private const string ExcludedFolderMarker = "/Pedestrians_backup_20260625/";
        private const int TargetMaxSize = 2048;
        private const int TargetCompressionQuality = 50;

        [MenuItem("BrainDrain/Fix Android Texture Compression")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("AndroidTextureCompressionFix")) { return; }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TargetRoot });
            int changed = 0;
            int skippedAlreadyOverridden = 0;
            int skippedExcludedFolder = 0;
            int skippedNotPng = 0;
            var changedPaths = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skippedNotPng++;
                        continue;
                    }

                    if (path.Contains(ExcludedFolderMarker))
                    {
                        skippedExcludedFolder++;
                        continue;
                    }

                    if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    {
                        continue;
                    }

                    TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                    if (androidSettings.overridden)
                    {
                        skippedAlreadyOverridden++;
                        continue;
                    }

                    androidSettings.overridden = true;
                    androidSettings.maxTextureSize = TargetMaxSize;
                    androidSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                    androidSettings.textureCompression = TextureImporterCompression.Compressed;
                    androidSettings.compressionQuality = TargetCompressionQuality;
                    androidSettings.crunchedCompression = false;
                    androidSettings.allowsAlphaSplitting = false;

                    importer.SetPlatformTextureSettings(androidSettings);
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();

                    changed++;
                    changedPaths.Add(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[AndroidTextureCompressionFix] Added an Android override (ASTC 6x6, max {TargetMaxSize}, quality {TargetCompressionQuality}) to {changed} texture(s) under {TargetRoot} (excluding Pedestrians_backup_20260625). Skipped: {skippedAlreadyOverridden} already overridden, {skippedExcludedFolder} in the excluded backup folder, {skippedNotPng} non-PNG (JPGs were already correctly compressed).");
            foreach (string changedPath in changedPaths)
            {
                Debug.Log($"[AndroidTextureCompressionFix]   + {changedPath}");
            }
        }
    }
}
#endif
