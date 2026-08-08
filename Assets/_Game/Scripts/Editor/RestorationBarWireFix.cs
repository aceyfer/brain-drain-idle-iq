#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Builds a full-width RESTORATION fill bar pinned to the bottom of the safe area,
    /// MapleStory/WoW EXP-bar style: RestorationBarRow (label + fill bar + the reparented
    /// pointsText + the reparented RestoreButton) sits as a sibling of EconomyBar under
    /// CustomSafeArea, filling the already-empty band below the bottom nav row. Wires
    /// HUDController.restorationFillImage. Menu: BrainDrain/Fix Restoration Bar Wiring.
    /// Idempotent -- safe to re-run (also migrates a row still parented under the old
    /// CurrencyHeader location from an earlier version of this tool).
    /// </summary>
    public static class RestorationBarWireFix
    {
        private static readonly Color TrackColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        private static readonly Color FillColor = new Color(1f, 0.85f, 0.2f, 1f); // matches pointsText's default gold

        [MenuItem("BrainDrain/Fix Restoration Bar Wiring")]
        private static void Run()
        {
            if (EditorToolGuard.BlockedByPlayMode("RestorationBarWireFix.Run")) return;

            HUDController hud = Object.FindAnyObjectByType<HUDController>();
            if (hud == null)
            {
                Debug.LogError("[RestorationBarWireFix] HUDController not found. Open the game scene first.");
                return;
            }

            Transform customSafeArea = FindInScene("CustomSafeArea");
            if (customSafeArea == null)
            {
                Debug.LogError("[RestorationBarWireFix] CustomSafeArea not found.");
                return;
            }

            Transform economyBar = customSafeArea.Find("EconomyBar");
            if (economyBar == null)
            {
                Debug.LogError("[RestorationBarWireFix] CustomSafeArea/EconomyBar not found.");
                return;
            }

            Transform pointsTextTf = hud.PointsText != null ? hud.PointsText.transform : FindInScene("PointsText");
            if (pointsTextTf == null)
            {
                Debug.LogError("[RestorationBarWireFix] PointsText not found (and HUDController.PointsText is unassigned).");
                return;
            }

            Transform restoreButtonTf = FindInScene("RestoreButton");
            if (restoreButtonTf == null)
            {
                Debug.LogError("[RestorationBarWireFix] RestoreButton not found.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(customSafeArea.gameObject, "Add Restoration Bar");

            Transform row = customSafeArea.Find("RestorationBarRow");
            if (row == null)
            {
                // Migrate a row still under the old CurrencyHeader location, if present.
                Transform legacyRow = FindInScene("RestorationBarRow");
                row = legacyRow != null ? legacyRow : CreateRow(customSafeArea);
            }

            if (row.parent != customSafeArea)
            {
                row.SetParent(customSafeArea, false);
            }
            ApplyRowAnchors(row);
            row.SetSiblingIndex(economyBar.GetSiblingIndex() + 1);

            Image fillImage = BuildLabelAndBar(row);

            if (pointsTextTf.parent != row)
            {
                pointsTextTf.SetParent(row, false);
            }
            pointsTextTf.SetAsLastSibling();
            StylePointsTextForRow(pointsTextTf);

            if (restoreButtonTf.parent != row)
            {
                restoreButtonTf.SetParent(row, false);
            }
            restoreButtonTf.SetAsLastSibling();
            StyleRestoreButtonForRow(restoreButtonTf);

            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("restorationFillImage").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[RestorationBarWireFix] Done. Save the scene (Ctrl+S).\n" +
                      "RestorationBarRow now sits under CustomSafeArea, pinned to the bottom of the safe area, " +
                      "with RestoreButton reparented in. ButtonsRow should now auto-redistribute to Shop/Convert only. " +
                      "Check RestoreButton's font/padding at the new 68px row height -- it was sized for the taller ButtonsRow.");
        }

        private static Transform CreateRow(Transform customSafeArea)
        {
            var rowGo = new GameObject("RestorationBarRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create RestorationBarRow");
            rowGo.transform.SetParent(customSafeArea, false);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(12, 12, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            return rowGo.transform;
        }

        /// <summary>Pins the row to the full-width, bottom-most band of CustomSafeArea (the band already left empty below EconomyBar) -- applied on every run so a legacy or manually-nudged row is corrected too.</summary>
        private static void ApplyRowAnchors(Transform row)
        {
            var rt = row.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Restoration Bar Row Anchors");
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 68f);
            EditorUtility.SetDirty(rt);
        }

        private static Image BuildLabelAndBar(Transform row)
        {
            TextMeshProUGUI referenceFont = FindReferenceFont();

            Transform labelTf = row.Find("RestorationLabel");
            if (labelTf == null)
            {
                var labelGo = new GameObject("RestorationLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create RestorationLabel");
                labelGo.transform.SetParent(row, false);
                labelTf = labelGo.transform;
            }

            var label = labelTf.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(label, "Restoration Label");
            if (referenceFont != null)
            {
                label.font = referenceFont.font;
                label.fontSharedMaterial = referenceFont.fontSharedMaterial;
            }
            label.text = "RESTORATION";
            label.fontSize = 14f;
            label.fontSizeMax = 14f;
            label.fontSizeMin = 10f;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            label.raycastTarget = false;
            EditorUtility.SetDirty(label);

            var labelLayout = labelTf.GetComponent<LayoutElement>();
            if (labelLayout == null) labelLayout = Undo.AddComponent<LayoutElement>(labelTf.gameObject);
            labelLayout.preferredWidth = 92f;
            labelLayout.flexibleWidth = 0f;
            EditorUtility.SetDirty(labelLayout);

            Transform trackTf = row.Find("RestorationBarTrack");
            if (trackTf == null)
            {
                var trackGo = new GameObject("RestorationBarTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(trackGo, "Create RestorationBarTrack");
                trackGo.transform.SetParent(row, false);
                trackTf = trackGo.transform;
            }

            var track = trackTf.GetComponent<Image>();
            Undo.RecordObject(track, "Restoration Bar Track");
            track.color = TrackColor;
            track.raycastTarget = false;
            EditorUtility.SetDirty(track);

            var trackLayout = trackTf.GetComponent<LayoutElement>();
            if (trackLayout == null) trackLayout = Undo.AddComponent<LayoutElement>(trackTf.gameObject);
            trackLayout.flexibleWidth = 1f;
            trackLayout.preferredHeight = 10f;
            EditorUtility.SetDirty(trackLayout);

            Transform fillTf = trackTf.Find("RestorationBarFill");
            if (fillTf == null)
            {
                var fillGo = new GameObject("RestorationBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(fillGo, "Create RestorationBarFill");
                fillGo.transform.SetParent(trackTf, false);
                fillTf = fillGo.transform;
            }

            var fillRt = fillTf.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fill = fillTf.GetComponent<Image>();
            Undo.RecordObject(fill, "Restoration Bar Fill");
            fill.color = FillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            EditorUtility.SetDirty(fill);

            return fill;
        }

        private static TextMeshProUGUI FindReferenceFont()
        {
            Transform cashText = FindInScene("CashText");
            return cashText != null ? cashText.GetComponent<TextMeshProUGUI>() : null;
        }

        private static void StylePointsTextForRow(Transform pointsTextTf)
        {
            var layout = pointsTextTf.GetComponent<LayoutElement>();
            if (layout == null) return;

            Undo.RecordObject(layout, "Restoration Row Points Layout");
            layout.flexibleWidth = 0f;
            layout.preferredWidth = 130f;
            layout.layoutPriority = 1;
            EditorUtility.SetDirty(layout);
        }

        /// <summary>Gives the reparented RestoreButton a fixed width in its new row instead of being force-stretched by the HorizontalLayoutGroup -- does not touch the button's own Button/Image/label components.</summary>
        private static void StyleRestoreButtonForRow(Transform restoreButtonTf)
        {
            var layout = restoreButtonTf.GetComponent<LayoutElement>();
            if (layout == null) layout = Undo.AddComponent<LayoutElement>(restoreButtonTf.gameObject);

            Undo.RecordObject(layout, "Restoration Row Restore Button Layout");
            layout.flexibleWidth = 0f;
            layout.preferredWidth = 120f;
            EditorUtility.SetDirty(layout);
        }

        private static Transform FindInScene(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindChildRecursive(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
