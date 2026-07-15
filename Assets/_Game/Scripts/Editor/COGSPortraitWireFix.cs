#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Verifies and repairs COGS portrait sprite wiring after Unity's first import of the
    /// new portrait files at Assets/_Game/Sprites/Characters/COGs/.
    ///
    /// Run via: BrainDrain/COGS/Wire COGS Portraits
    ///
    /// What it does:
    ///   1. Re-imports all 6 COGS textures to force Unity to apply the pre-written meta settings.
    ///   2. Loads each resulting Sprite and writes it into the matching COGSStage ScriptableObject.
    ///   3. Reports success/failure for each stage.
    ///
    /// Idempotent: safe to re-run. Re-running after the initial import is a no-op (already wired).
    /// </summary>
    public static class COGSPortraitWireFix
    {
        private static readonly (string path, string assetPath)[] PortraitMap = new[]
        {
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs1.png",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_0_CorruptedCRT.asset"
            ),
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs2.jpg",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_1_PatchedCRT.asset"
            ),
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs3.jpg",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_2_Flatscreen.asset"
            ),
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs4.jpg",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_3_CleanMonitor.asset"
            ),
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs5.jpg",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_4_HologramProto.asset"
            ),
            (
                "Assets/_Game/Sprites/Characters/COGs/COGs6.jpg",
                "Assets/_Game/Dialogue/COGSStages/COGSStage_5_FullHologram.asset"
            ),
        };

        [MenuItem("BrainDrain/COGS/Wire COGS Portraits")]
        public static void WireCOGSPortraits()
        {
            if (EditorToolGuard.BlockedByPlayMode("COGSPortraitWireFix.WireCOGSPortraits")) return;
            bool anyError = false;

            // Step 1: force-reimport all textures so Unity applies our pre-written meta settings.
            foreach (var (texturePath, _) in PortraitMap)
            {
                if (!System.IO.File.Exists(texturePath))
                {
                    Debug.LogError($"[COGSPortraitWireFix] Texture not found on disk: {texturePath}");
                    anyError = true;
                    continue;
                }
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh();

            // Step 2: load each Sprite and update the matching COGSStage asset.
            foreach (var (texturePath, stagePath) in PortraitMap)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (sprite == null)
                {
                    Debug.LogError($"[COGSPortraitWireFix] Could not load Sprite from {texturePath}. " +
                                   "Check import settings (must be Sprite 2D/UI, Single mode).");
                    anyError = true;
                    continue;
                }

                COGSStage stage = AssetDatabase.LoadAssetAtPath<COGSStage>(stagePath);
                if (stage == null)
                {
                    Debug.LogError($"[COGSPortraitWireFix] Could not load COGSStage at {stagePath}.");
                    anyError = true;
                    continue;
                }

                SerializedObject so = new SerializedObject(stage);
                SerializedProperty prop = so.FindProperty("portraitSprite");
                if (prop == null)
                {
                    Debug.LogError($"[COGSPortraitWireFix] 'portraitSprite' property not found on {stagePath}.");
                    anyError = true;
                    continue;
                }

                if (prop.objectReferenceValue == sprite)
                {
                    Debug.Log($"[COGSPortraitWireFix] {stage.stageName}: already wired to {sprite.name}, skipping.");
                    continue;
                }

                prop.objectReferenceValue = sprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(stage);
                Debug.Log($"[COGSPortraitWireFix] {stage.stageName}: wired to {sprite.name} ✓");
            }

            AssetDatabase.SaveAssets();

            if (!anyError)
            {
                Debug.Log("[COGSPortraitWireFix] All 6 COGS portrait stages wired successfully.");
            }
            else
            {
                Debug.LogWarning("[COGSPortraitWireFix] Completed with errors. See above for details.");
            }
        }

        /// <summary>
        /// Diagnostic: logs the current portraitSprite reference for each stage without changing anything.
        /// </summary>
        [MenuItem("BrainDrain/COGS/Inspect COGS Portrait Wiring")]
        public static void InspectCOGSPortraitWiring()
        {
            if (EditorToolGuard.BlockedByPlayMode("COGSPortraitWireFix.InspectCOGSPortraitWiring")) return;
            foreach (var (texturePath, stagePath) in PortraitMap)
            {
                COGSStage stage = AssetDatabase.LoadAssetAtPath<COGSStage>(stagePath);
                if (stage == null)
                {
                    Debug.LogWarning($"[COGSPortraitWireFix] Could not load: {stagePath}");
                    continue;
                }

                string spriteInfo = stage.portraitSprite != null
                    ? $"{stage.portraitSprite.name} (at {AssetDatabase.GetAssetPath(stage.portraitSprite)})"
                    : "NULL";

                Debug.Log($"[COGSPortraitWireFix] Stage {stage.stageIndex} ({stage.stageName}): {spriteInfo}");
            }
        }
    }
}
#endif
