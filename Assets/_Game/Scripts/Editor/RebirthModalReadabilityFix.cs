#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-10 play-test fix: the Snotting confirmation (RebirthModal) read as tiny text
    /// floating on a see-through panel with wasted space above. Root cause confirmed by reading
    /// the scene YAML directly (not guessed): RebirthModal's own background Image was a
    /// near-black 94%-alpha scrim (0.04, 0.04, 0.06, 0.94), not a real card color, and
    /// MultiplierValueText -- the ONE element carrying all the dynamic lose/keep/reward content
    /// -- was anchored to just a 10%-tall band (y 0.28-0.38) of the modal's height, while the
    /// static flavor text (DescriptionText) and the dead zone above/below it ate the rest. This
    /// tool is the idempotent fix (same pattern as ShopPanelLayoutFix/RewardedAdRecoveryWireFix):
    /// re-run safe, sets exact target values every time rather than reading-then-nudging.
    ///
    /// Deliberately leaves TitleText, DescriptionText, ConfirmButton, and CancelButton positions
    /// untouched -- the ask was to recompose MultiplierValueText's own space and the panel's
    /// fill, not to rebuild the whole modal, and touching the buttons risks the "approved Snotting
    /// glow and shared button borders" Aceyfer explicitly said to keep.
    /// </summary>
    public static class RebirthModalReadabilityFix
    {
        private const string RebirthModalObjectName = "RebirthModal";
        private const string MultiplierValueTextObjectName = "MultiplierValueText";

        [MenuItem("BrainDrain/Fix Rebirth Modal Readability")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("RebirthModalReadabilityFix.Fix")) return;

            RebirthUIController controller = Object.FindAnyObjectByType<RebirthUIController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("[RebirthModalReadabilityFix] No RebirthUIController found in the scene. Aborting.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty panelProp = so.FindProperty("rebirthModalPanel");
            SerializedProperty textProp = so.FindProperty("multiplierText");

            GameObject panel = panelProp != null ? panelProp.objectReferenceValue as GameObject : null;
            Component textComponent = textProp != null ? textProp.objectReferenceValue as Component : null;

            if (panel == null || panel.name != RebirthModalObjectName)
            {
                Debug.LogWarning($"[RebirthModalReadabilityFix] rebirthModalPanel is unwired or unexpected ('{panel?.name}'). Aborting.");
                return;
            }

            if (textComponent == null || textComponent.gameObject.name != MultiplierValueTextObjectName)
            {
                Debug.LogWarning($"[RebirthModalReadabilityFix] multiplierText is unwired or unexpected ('{textComponent?.gameObject.name}'). Aborting.");
                return;
            }

            bool changed = false;

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null && panelImage.color != Palette.Surface)
            {
                Debug.Log($"[RebirthModalReadabilityFix] RebirthModal background: {panelImage.color} -> Palette.Surface {Palette.Surface}.");
                panelImage.color = Palette.Surface;
                EditorUtility.SetDirty(panelImage);
                changed = true;
            }

            RectTransform textRect = textComponent.transform as RectTransform;
            if (textRect != null)
            {
                // Was (0.05, 0.28)-(0.95, 0.38) -- a 10%-tall band. Reclaims the dead space between
                // DescriptionText's bottom (0.35, left at 0.66 here for a small gap) and the
                // buttons' top (~0.17, left at 0.19 here for a small gap) -- 47% of the modal's
                // height, matching "title > what you lose > what you keep/gain" needing real room.
                Vector2 targetMin = new Vector2(0.05f, 0.19f);
                Vector2 targetMax = new Vector2(0.95f, 0.66f);
                if (textRect.anchorMin != targetMin || textRect.anchorMax != targetMax)
                {
                    Debug.Log($"[RebirthModalReadabilityFix] MultiplierValueText anchors: ({textRect.anchorMin}-{textRect.anchorMax}) -> ({targetMin}-{targetMax}).");
                    textRect.anchorMin = targetMin;
                    textRect.anchorMax = targetMax;
                    EditorUtility.SetDirty(textRect);
                    changed = true;
                }
            }

            if (!changed)
            {
                Debug.Log("[RebirthModalReadabilityFix] Already up to date -- nothing to change.");
                return;
            }

            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[RebirthModalReadabilityFix] Done. Save the scene (Ctrl+S) to persist.");
        }
    }
}
#endif
