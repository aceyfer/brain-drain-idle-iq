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
    /// Builds a RESTORATION fill bar next to the existing RP number readout: a new
    /// RestorationBarRow (label + fill bar + the reparented pointsText) sits in the gap
    /// between TextsRow and restorationProgressText inside CurrencyHeader, and wires
    /// HUDController.restorationFillImage. Menu: BrainDrain/Fix Restoration Bar Wiring.
    /// Idempotent -- safe to re-run.
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

            Transform currencyHeader = FindInScene("CurrencyHeader");
            if (currencyHeader == null)
            {
                Debug.LogError("[RestorationBarWireFix] CurrencyHeader not found.");
                return;
            }

            Transform pointsTextTf = hud.PointsText != null ? hud.PointsText.transform : FindInScene("PointsText");
            if (pointsTextTf == null)
            {
                Debug.LogError("[RestorationBarWireFix] PointsText not found (and HUDController.PointsText is unassigned).");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(currencyHeader.gameObject, "Add Restoration Bar");

            Transform row = currencyHeader.Find("RestorationBarRow");
            if (row == null)
            {
                row = CreateRow(currencyHeader);
            }

            Image fillImage = BuildLabelAndBar(row);

            if (pointsTextTf.parent != row)
            {
                pointsTextTf.SetParent(row, false);
            }
            pointsTextTf.SetAsLastSibling();
            StylePointsTextForRow(pointsTextTf);

            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("restorationFillImage").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[RestorationBarWireFix] Done. Save the scene (Ctrl+S).\n" +
                      "RestorationBarRow built under CurrencyHeader (RESTORATION label + fill bar + reparented RP number). " +
                      "The gap it's anchored into (between TextsRow and restorationProgressText) is tight -- " +
                      "check for overlap and nudge RestorationBarRow's anchoredPosition/height if needed.");
        }

        private static Transform CreateRow(Transform currencyHeader)
        {
            var rowGo = new GameObject("RestorationBarRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create RestorationBarRow");
            rowGo.transform.SetParent(currencyHeader, false);

            var rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -192f);
            rt.sizeDelta = new Vector2(-48f, 20f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            Transform textsRow = currencyHeader.Find("TextsRow");
            if (textsRow != null)
            {
                rowGo.transform.SetSiblingIndex(textsRow.GetSiblingIndex() + 1);
            }

            return rowGo.transform;
        }

        private static Image BuildLabelAndBar(Transform row)
        {
            TextMeshProUGUI referenceFont = FindReferenceFont(row);

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

        private static TextMeshProUGUI FindReferenceFont(Transform row)
        {
            Transform currencyHeader = row.parent;
            Transform textsRow = currencyHeader != null ? currencyHeader.Find("TextsRow") : null;
            Transform cashText = textsRow != null ? textsRow.Find("CashText") : null;
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
