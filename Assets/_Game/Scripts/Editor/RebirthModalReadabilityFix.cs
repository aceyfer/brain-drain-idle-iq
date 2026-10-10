#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    /// 2026-10-11 round 2 (FixTitleAndDescription/FixButtons): play-test on round 1 found
    /// DescriptionText's old band still overlapping the new MultiplierValueText content, the
    /// title reading pink, and SELL OUT/ABORT rendering tiny (fixed 190x50, 15pt non-autosizing
    /// label) against the panel's full screen width. Repositions Title/Description into their own
    /// non-overlapping rows, recolors the title, and converts both buttons to a stretched
    /// half-width-each layout with a 30pt autosize floor on their labels. Button/title FILL
    /// colors, the approved Snotting glow, and shared button borders are untouched -- only
    /// position/size and (for the title only) label color changed.
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

            changed |= FixTitleAndDescription(panel);
            changed |= FixButtons(panel);

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

        /// <summary>
        /// 2026-10-11 play-test fix round 2: DescriptionText's old band (0.35-0.68) heavily
        /// overlapped MultiplierValueText's new 0.19-0.66 band from the first pass -- the flavor
        /// text was floating on top of the "YOU LOSE" lines. Moves Description to its own row
        /// directly under the title, and raises the title itself to close the ~8%-tall dead zone
        /// that used to sit above it (was 0.72-0.92, well short of the panel's own top edge).
        /// Title's font color was also the retired Hot Pink -- switched to Palette.Cyan.
        /// </summary>
        private static bool FixTitleAndDescription(GameObject panel)
        {
            bool changed = false;

            Transform title = panel.transform.Find("TitleText");
            if (title != null)
            {
                RectTransform titleRect = title as RectTransform;
                Vector2 targetMin = new Vector2(0.05f, 0.86f);
                Vector2 targetMax = new Vector2(0.95f, 0.985f);
                if (titleRect != null && (titleRect.anchorMin != targetMin || titleRect.anchorMax != targetMax))
                {
                    Debug.Log($"[RebirthModalReadabilityFix] TitleText anchors: ({titleRect.anchorMin}-{titleRect.anchorMax}) -> ({targetMin}-{targetMax}).");
                    titleRect.anchorMin = targetMin;
                    titleRect.anchorMax = targetMax;
                    EditorUtility.SetDirty(titleRect);
                    changed = true;
                }

                TextMeshProUGUI titleLabel = title.GetComponent<TextMeshProUGUI>();
                if (titleLabel != null && titleLabel.color != Palette.Cyan)
                {
                    Debug.Log($"[RebirthModalReadabilityFix] TitleText color: {titleLabel.color} -> Palette.Cyan {Palette.Cyan}.");
                    titleLabel.color = Palette.Cyan;
                    EditorUtility.SetDirty(titleLabel);
                    changed = true;
                }
            }

            Transform description = panel.transform.Find("DescriptionText");
            if (description != null)
            {
                RectTransform descriptionRect = description as RectTransform;
                Vector2 targetMin = new Vector2(0.05f, 0.70f);
                Vector2 targetMax = new Vector2(0.95f, 0.84f);
                if (descriptionRect != null && (descriptionRect.anchorMin != targetMin || descriptionRect.anchorMax != targetMax))
                {
                    Debug.Log($"[RebirthModalReadabilityFix] DescriptionText anchors: ({descriptionRect.anchorMin}-{descriptionRect.anchorMax}) -> ({targetMin}-{targetMax}).");
                    descriptionRect.anchorMin = targetMin;
                    descriptionRect.anchorMax = targetMax;
                    EditorUtility.SetDirty(descriptionRect);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// 2026-10-11 play-test fix round 2: SELL OUT / ABORT were a fixed 190x50 point-anchored
        /// pair (tiny against RebirthModal's full-screen-width panel) with a hardcoded 15pt,
        /// non-autosizing label. Converts both to a stretched half-width-each layout and floors
        /// their labels at 30pt via autosizing (so they never render smaller, but can still shrink
        /// slightly if a future longer label needs it). Colors untouched -- SELL OUT's existing
        /// Cyan and ABORT's existing color were not called out as wrong, only the size was.
        /// </summary>
        private static bool FixButtons(GameObject panel)
        {
            bool changed = false;
            changed |= FixButtonRect(panel, "CancelButton", new Vector2(0.05f, 0.03f), new Vector2(0.48f, 0.13f));
            changed |= FixButtonRect(panel, "ConfirmButton", new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.13f));
            return changed;
        }

        private static bool FixButtonRect(GameObject panel, string buttonName, Vector2 targetMin, Vector2 targetMax)
        {
            Transform button = panel.transform.Find(buttonName);
            if (button == null) { return false; }

            bool changed = false;
            RectTransform buttonRect = button as RectTransform;
            if (buttonRect != null)
            {
                bool rectChanged = buttonRect.anchorMin != targetMin
                    || buttonRect.anchorMax != targetMax
                    || buttonRect.anchoredPosition != Vector2.zero
                    || buttonRect.sizeDelta != Vector2.zero;
                if (rectChanged)
                {
                    Debug.Log($"[RebirthModalReadabilityFix] {buttonName} rect: ({buttonRect.anchorMin}-{buttonRect.anchorMax}, pos={buttonRect.anchoredPosition}, size={buttonRect.sizeDelta}) -> ({targetMin}-{targetMax}, stretched).");
                    buttonRect.anchorMin = targetMin;
                    buttonRect.anchorMax = targetMax;
                    buttonRect.anchoredPosition = Vector2.zero;
                    buttonRect.sizeDelta = Vector2.zero;
                    EditorUtility.SetDirty(buttonRect);
                    changed = true;
                }
            }

            TextMeshProUGUI label = button.Find("ButtonText")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                const float floorSize = 30f;
                const float ceilSize = 44f;
                bool labelChanged = !label.enableAutoSizing || label.fontSizeMin < floorSize || label.fontSizeMax != ceilSize;
                if (labelChanged)
                {
                    Debug.Log($"[RebirthModalReadabilityFix] {buttonName} label: fontSize={label.fontSize}, autosize={label.enableAutoSizing} -> autosize [{floorSize}, {ceilSize}].");
                    label.enableAutoSizing = true;
                    label.fontSizeMin = floorSize;
                    label.fontSizeMax = ceilSize;
                    EditorUtility.SetDirty(label);
                    changed = true;
                }
            }

            return changed;
        }
    }
}
#endif
