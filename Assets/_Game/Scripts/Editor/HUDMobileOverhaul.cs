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
    /// Refined mobile HUD layout. Run in Edit Mode via
    /// BrainDrain > Fix HUD Layout (Mobile Overhaul). Idempotent — safe to re-run.
    ///
    /// Layout (bottom → top of SafeArea, portrait 1080×1920):
    ///   y 0.14–0.26  ButtonsArea  ~230 px  — two rows: BP SHOP|$ SHOP / CONVERT|RESTORE
    ///   y 0.26–0.32  EconomyStrip ~115 px  — dark panel: $CASH left | POINTS right, 28 px
    ///   y 0.32–0.375 SnottingBtn  ~106 px  — full-width, neon pink / grey locked
    /// City art / pedestrians are above y=0.375 and untouched.
    /// </summary>
    public static class HUDMobileOverhaul
    {
        private const float ButtonsAreaMinY = 0.14f;
        private const float ButtonsAreaMaxY = 0.26f;

        private const float EcoStripMinY = 0.26f;
        private const float EcoStripMaxY = 0.32f;

        private const float SnottingMinY = 0.32f;
        private const float SnottingMaxY = 0.375f;

        private const float SideMarginX = 0.02f;

        private static readonly Color EcoStripBg = new Color(0.04f, 0.04f, 0.07f, 0.90f);

        [MenuItem("BrainDrain/Fix HUD Layout (Mobile Overhaul)")]
        private static void RunOverhaul()
        {
            HUDController hud = Object.FindAnyObjectByType<HUDController>();
            if (hud == null)
            {
                Debug.LogError("[HUDMobileOverhaul] HUDController not found. Open the game scene first.");
                return;
            }

            // 1. Hide secondary clutter.
            SetActive(hud.CumulativeBrainPowerCounterText, false, "CumulativeBrainPower");
            SetActive(hud.RebirthCountText,               false, "RebirthCount");
            SetActive(hud.BPPSText,                       false, "BPPS");

            // 2. Primary HUD text — large, readable.
            SetFont(hud.BrainPowerCounterText,   40f, min: 28f);
            SetFont(hud.PlayerIQText,            40f, min: 28f);
            SetFont(hud.RankText,                26f, min: 18f);
            SetFont(hud.RestorationProgressText, 26f, min: 18f);

            // 3. Shop buttons → two rows inside EconomyBar.
            RestructureButtonRow();

            // 4. TextsRow → standalone dark EconomyStrip above the buttons.
            ExtractEconomyStrip(hud);

            // 5. EconomyBar (now buttons-only) → ButtonsArea band.
            SizeButtonsArea();

            // 6. Snotting button → full-width stripe above the EconomyStrip.
            RepositionSnottingButton();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[HUDMobileOverhaul] Done. Save the scene (Ctrl+S).\n" +
                      "Stack (bottom→top): ButtonsArea(14-26%) | EcoStrip(26-32%) | Snotting(32-37.5%)");
        }

        // ── Helpers: text ─────────────────────────────────────────────────────

        private static void SetActive(TextMeshProUGUI tmp, bool active, string label)
        {
            if (tmp == null)
            {
                Debug.LogWarning($"[HUDMobileOverhaul] {label} not wired — skipped.");
                return;
            }
            if (tmp.gameObject.activeSelf == active) return;
            Undo.RecordObject(tmp.gameObject, "HUD Mobile Overhaul");
            tmp.gameObject.SetActive(active);
            EditorUtility.SetDirty(tmp.gameObject);
        }

        private static void SetFont(TextMeshProUGUI tmp, float maxSize, float min = 0f)
        {
            if (tmp == null) return;
            Undo.RecordObject(tmp, "HUD Mobile Font");
            tmp.fontSize         = maxSize;
            tmp.fontSizeMax      = maxSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = min > 0f ? min : maxSize * 0.5f;
            EditorUtility.SetDirty(tmp);
        }

        // ── Step 3: button rows ───────────────────────────────────────────────

        private static void RestructureButtonRow()
        {
            Transform buttonRowTf = FindInScene("ButtonsRow");
            if (buttonRowTf == null)
            {
                Debug.LogWarning("[HUDMobileOverhaul] ButtonsRow not found — skipping.");
                return;
            }

            // Idempotency: PrimaryRow already exists means we've run before.
            if (buttonRowTf.Find("PrimaryRow") != null)
            {
                RenameAndStyleButtons(buttonRowTf);
                return;
            }

            Transform shopBtn       = buttonRowTf.Find("ShopButton");
            Transform cashShopBtn   = buttonRowTf.Find("CashShopButton");
            Transform premiumBtn    = buttonRowTf.Find("PremiumShopButton");
            Transform pointsShopBtn = buttonRowTf.Find("PointsShopButton");
            Transform convertBtn    = buttonRowTf.Find("ConvertButton");
            Transform restoreBtn    = buttonRowTf.Find("RestoreButton");

            if (shopBtn == null || cashShopBtn == null || convertBtn == null || restoreBtn == null)
            {
                Debug.LogWarning("[HUDMobileOverhaul] Expected buttons not all found — skipping restructure.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(buttonRowTf.gameObject, "HUD Mobile Buttons");

            var oldHLG = buttonRowTf.GetComponent<HorizontalLayoutGroup>();
            if (oldHLG != null) Undo.DestroyObjectImmediate(oldHLG);

            var vlg = Undo.AddComponent<VerticalLayoutGroup>(buttonRowTf.gameObject);
            vlg.spacing              = 8f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = true;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            var primaryRow   = MakeRow("PrimaryRow",   buttonRowTf);
            var secondaryRow = MakeRow("SecondaryRow", buttonRowTf);

            shopBtn.SetParent(primaryRow.transform,    false);
            cashShopBtn.SetParent(primaryRow.transform,  false);
            convertBtn.SetParent(secondaryRow.transform, false);
            restoreBtn.SetParent(secondaryRow.transform, false);

            if (premiumBtn    != null) { premiumBtn.SetParent(secondaryRow.transform,    false); premiumBtn.gameObject.SetActive(false); }
            if (pointsShopBtn != null) { pointsShopBtn.SetParent(secondaryRow.transform, false); pointsShopBtn.gameObject.SetActive(false); }

            RenameAndStyleButtons(buttonRowTf);
            Debug.Log("[HUDMobileOverhaul] ButtonsRow split: PrimaryRow (BP SHOP|$ SHOP) / SecondaryRow (CONVERT|RESTORE).");
        }

        private static void RenameAndStyleButtons(Transform buttonRowTf)
        {
            SetButtonLabel(buttonRowTf, "ShopButton",     "BP SHOP");
            SetButtonLabel(buttonRowTf, "CashShopButton", "$ SHOP");
            SetButtonLabel(buttonRowTf, "ConvertButton",  "CONVERT");
            SetButtonLabel(buttonRowTf, "RestoreButton",  "RESTORE");

            foreach (Transform row in new[] { buttonRowTf.Find("PrimaryRow"), buttonRowTf.Find("SecondaryRow") })
            {
                if (row == null) continue;
                foreach (Transform btn in row)
                {
                    if (!btn.gameObject.activeSelf) continue;
                    var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp == null) continue;
                    Undo.RecordObject(tmp, "HUD Mobile Button Font");
                    tmp.fontSize         = 32f;
                    tmp.fontSizeMax      = 32f;
                    tmp.fontSizeMin      = 20f;
                    tmp.enableAutoSizing = true;
                    tmp.raycastTarget    = false;
                    EditorUtility.SetDirty(tmp);
                }
            }
        }

        private static void SetButtonLabel(Transform root, string buttonName, string label)
        {
            var go  = FindChildRecursive(root, buttonName);
            if (go == null) return;
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null) return;
            Undo.RecordObject(tmp, "HUD Mobile Button Label");
            tmp.text = label;
            EditorUtility.SetDirty(tmp);
        }

        private static GameObject MakeRow(string name, Transform parent)
        {
            var row = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row, $"Create {name}");
            row.transform.SetParent(parent, false);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 8f;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;

            row.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return row;
        }

        // ── Step 4: Economy strip ─────────────────────────────────────────────

        private static void ExtractEconomyStrip(HUDController hud)
        {
            Transform economyBarTf = FindInScene("EconomyBar");
            if (economyBarTf == null)
            {
                Debug.LogWarning("[HUDMobileOverhaul] EconomyBar not found — EconomyStrip skipped.");
                return;
            }

            // TextsRow is a child of EconomyBar on the first run; already a sibling on re-run.
            Transform textRowTf      = economyBarTf.Find("TextsRow");
            bool      alreadyExtracted = textRowTf == null;
            if (alreadyExtracted)
            {
                textRowTf = FindInScene("TextsRow");
                if (textRowTf == null) { Debug.LogWarning("[HUDMobileOverhaul] TextsRow not found."); return; }
            }

            // Reparent to SafeArea so it's independent of EconomyBar.
            if (!alreadyExtracted)
            {
                // Record both the old parent (EconomyBar) and new parent (SafeArea) before moving.
                Undo.RegisterFullObjectHierarchyUndo(economyBarTf.gameObject,        "Extract Economy Strip");
                Undo.RegisterFullObjectHierarchyUndo(economyBarTf.parent.gameObject, "Extract Economy Strip");
                textRowTf.SetParent(economyBarTf.parent, false);
                textRowTf.SetSiblingIndex(economyBarTf.GetSiblingIndex() + 1);
            }

            // Anchor to EcoStrip band.
            var rt = textRowTf.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "Economy Strip Rect");
                rt.anchorMin        = new Vector2(SideMarginX, EcoStripMinY);
                rt.anchorMax        = new Vector2(1f - SideMarginX, EcoStripMaxY);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = Vector2.zero;
                EditorUtility.SetDirty(rt);
            }

            // Dark background Image (add if missing, update if present).
            var img = textRowTf.GetComponent<Image>();
            if (img == null) img = Undo.AddComponent<Image>(textRowTf.gameObject);
            Undo.RecordObject(img, "Economy Strip BG");
            img.color         = EcoStripBg;
            img.raycastTarget = false;
            EditorUtility.SetDirty(img);

            // HorizontalLayoutGroup: comfortable padding so text isn't flush with edges.
            var hlg = textRowTf.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                Undo.RecordObject(hlg, "Economy Strip HLG");
                hlg.padding              = new RectOffset(18, 18, 10, 10);
                hlg.spacing              = 8f;
                hlg.childForceExpandWidth  = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth      = true;
                hlg.childControlHeight     = true;
                EditorUtility.SetDirty(hlg);
            }

            // Font + alignment: cash left, points right, both 28 px.
            StyleEcoText(hud.CashText,   TextAlignmentOptions.MidlineLeft,  28f, 22f);
            StyleEcoText(hud.PointsText, TextAlignmentOptions.MidlineRight, 28f, 22f);

            Debug.Log($"[HUDMobileOverhaul] EconomyStrip → y [{EcoStripMinY}–{EcoStripMaxY}]  cash(L) | points(R)  28 px.");
        }

        private static void StyleEcoText(TextMeshProUGUI tmp, TextAlignmentOptions align, float maxSize, float min)
        {
            if (tmp == null) return;
            Undo.RecordObject(tmp, "Eco Strip Text");
            tmp.fontSize         = maxSize;
            tmp.fontSizeMax      = maxSize;
            tmp.fontSizeMin      = min;
            tmp.enableAutoSizing = true;
            tmp.alignment        = align;
            EditorUtility.SetDirty(tmp);
        }

        // ── Step 5: EconomyBar → buttons-only size ────────────────────────────

        private static void SizeButtonsArea()
        {
            Transform economyBarTf = FindInScene("EconomyBar");
            if (economyBarTf == null) return;

            var rt = economyBarTf.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "HUD Mobile ButtonsArea");
            rt.anchorMin        = new Vector2(SideMarginX, ButtonsAreaMinY);
            rt.anchorMax        = new Vector2(1f - SideMarginX, ButtonsAreaMaxY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
            EditorUtility.SetDirty(rt);
            Debug.Log($"[HUDMobileOverhaul] ButtonsArea → y [{ButtonsAreaMinY}–{ButtonsAreaMaxY}]  (~230 px, two ~103 px rows).");
        }

        // ── Step 6: Snotting button ───────────────────────────────────────────

        private static void RepositionSnottingButton()
        {
            var rebirthUI = Object.FindAnyObjectByType<RebirthUIController>();
            if (rebirthUI == null)
            {
                Debug.LogWarning("[HUDMobileOverhaul] RebirthUIController not found.");
                return;
            }

            GameObject snottingBtn = rebirthUI.TriggerButtonObject;
            if (snottingBtn == null)
            {
                Debug.LogWarning("[HUDMobileOverhaul] TriggerButtonObject not wired.");
                return;
            }

            var rt = snottingBtn.GetComponent<RectTransform>();
            if (rt == null) return;

            Undo.RecordObject(rt, "HUD Mobile Snotting");
            rt.anchorMin        = new Vector2(SideMarginX, SnottingMinY);
            rt.anchorMax        = new Vector2(1f - SideMarginX, SnottingMaxY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0f);
            EditorUtility.SetDirty(rt);

            var tmp = snottingBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "HUD Mobile Snotting Font");
                tmp.fontSize         = 46f;
                tmp.fontSizeMax      = 46f;
                tmp.fontSizeMin      = 28f;
                tmp.enableAutoSizing = true;
                tmp.raycastTarget    = false;
                EditorUtility.SetDirty(tmp);
            }

            Debug.Log($"[HUDMobileOverhaul] Snotting → y [{SnottingMinY}–{SnottingMaxY}]  (~106 px, full-width).");
        }

        // ── Scene-search helpers ──────────────────────────────────────────────

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
