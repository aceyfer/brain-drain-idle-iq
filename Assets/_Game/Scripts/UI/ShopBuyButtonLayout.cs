using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>One layout coordinator per tab; prices never determine column width.</summary>
    public sealed class ShopBuyButtonLayout : MonoBehaviour
    {
        private const float ColumnWidth = 240f;
        private const float DescriptionGap = 16f;
        private readonly List<Entry> entries = new();
        private readonly Vector3[] corners = new Vector3[4];
        private float previousWidth = -1f;
        private bool dirty;

        private struct Entry
        {
            public RectTransform row;
            public RectTransform description;
            public LayoutElement column;
        }

        public static void Register(Transform row, Button button, TMP_Text price, TMP_Text description)
        {
            if (button == null || price == null || description == null || row.parent == null) return;
            var coordinator = row.parent.GetComponent<ShopBuyButtonLayout>();
            if (coordinator == null) coordinator = row.parent.gameObject.AddComponent<ShopBuyButtonLayout>();
            coordinator.Add(row as RectTransform, button, price, description);
        }

        private void Add(RectTransform row, Button button, TMP_Text price, TMP_Text description)
        {
            if (row == null) return;
            price.alignment = TextAlignmentOptions.Center;
            price.textWrappingMode = TextWrappingModes.NoWrap;
            price.enableAutoSizing = true;
            price.fontSizeMin = 18f;
            price.fontSizeMax = Mathf.Max(30f, price.fontSizeMax);
            price.margin = new Vector4(16f, 4f, 16f, 4f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.spacing = Mathf.Max(DescriptionGap, layout.spacing);
            Transform column = button.transform.parent;
            var element = column.GetComponent<LayoutElement>();
            if (element == null) element = column.gameObject.AddComponent<LayoutElement>();
            foreach (Entry entry in entries) if (entry.row == row) return;
            entries.Add(new Entry { row = row, description = description.rectTransform, column = element });
            dirty = true;
        }

        private void OnEnable() => dirty = true;

        private void LateUpdate()
        {
            var content = transform as RectTransform;
            if (content == null) return;
            float width = content.rect.width;
            if (!dirty && Mathf.Approximately(width, previousWidth)) return;
            if (width <= 0f) return;
            previousWidth = width;
            dirty = false;
            entries.RemoveAll(entry => entry.row == null || entry.column == null || entry.description == null);

            // Reset the authored column cap before measuring so repeated resizes cannot
            // progressively steal space from the description column.
            foreach (Entry entry in entries) SetWidth(entry.column, ColumnWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float commonWidth = ColumnWidth;
            foreach (Entry entry in entries)
            {
                if (!entry.row.gameObject.activeInHierarchy) continue;
                entry.description.GetWorldCorners(corners);
                float descriptionRight = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                    descriptionRight = Mathf.Max(descriptionRight, entry.row.InverseTransformPoint(corner).x);
                var rowLayout = entry.row.GetComponent<HorizontalLayoutGroup>();
                float rightPadding = rowLayout != null ? rowLayout.padding.right : 0f;
                float available = entry.row.rect.xMax - rightPadding - descriptionRight - DescriptionGap;
                commonWidth = Mathf.Min(commonWidth, Mathf.Max(0f, available));
            }
            foreach (Entry entry in entries) SetWidth(entry.column, commonWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static void SetWidth(LayoutElement column, float width)
        {
            column.minWidth = width;
            column.preferredWidth = width;
            column.flexibleWidth = 0f;
        }
    }
}
