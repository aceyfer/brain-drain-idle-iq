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
    /// Builds the RESTORATION UI as three stacked bands under CustomSafeArea, bottom to top:
    /// RestorationVesselRow (glass vessel only -- non-interactive, no raycast targets, the one
    /// thing allowed to sit at the true bottom edge / gesture zone, since CustomSafeArea's own
    /// local y=0 is already safe-area-clipped by SafeAreaManager -- see that class for why),
    /// RestorationInteractiveRow (every tap target: ShopButton, ConvertButton, RestorationLabel,
    /// PointsText, RestoreButton -- all pulled out of EconomyBar/the old info row into one row,
    /// entirely above the vessel), then EconomyBar itself, moved up to sit above the interactive
    /// row (its anchors DO move now -- ShopButton/ConvertButton no longer live in it, so it ends up
    /// an empty container kept only for scene-hygiene, not because anything still renders there).
    /// Wires HUDController.restorationFillImage/restorationGlowImage.
    /// Menu: Tools/Eighth Kind/Fix Restoration Bar Wiring.
    /// Idempotent -- safe to re-run. Migrates the old single combined RestorationBarRow (or an
    /// intermediate RestorationInfoRow) by renaming it to RestorationInteractiveRow in place and
    /// moving RestorationBarTrack out of it into RestorationVesselRow.
    ///
    /// 2026-08-25: the glass-tube/plunger visual (RestorationBarPlunger + RestorationBarVesselFrame,
    /// backed by xp_bar_plunger.png/xp_bar_frame.png) was abandoned and removed -- the source art was
    /// deleted and the two objects were removed from the scene by hand. This tool no longer creates
    /// or wires either one; the vessel is Track + Glow + Fill only, a plain colored bar.
    /// </summary>
    public static class RestorationBarWireFix
    {
        private static readonly Color TrackColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        // Matches AnimationController.ExtractionBlueColor exactly (duplicated, not referenced --
        // this is static scene-authoring data, same as TrackColor/label color already were) so the
        // vessel liquid and the tap-extraction particles read as the same substance.
        private static readonly Color FillColor = new Color(0.25f, 0.75f, 1f, 1f);

        // CanvasScaler's reference resolution height -- the fixed frame every pixel value in this
        // file (VesselRowHeight, InteractiveRowHeight, etc.) is authored against. Needed to convert
        // EconomyBar's fraction-based anchors to/from the same pixel space as the other two rows.
        private const float ReferenceHeight = 1920f;

        // RestorationVesselRow's total sizeDelta.y -- vessel only, full width, non-interactive.
        // Anchored to CustomSafeArea's own bottom (y=0), which is already safe-area-clipped by
        // SafeAreaManager -- see the class doc comment. This is the one element allowed there.
        private const float VesselRowHeight = 90f;

        // ShopButton/ConvertButton's CURRENT rendered height, traced from EconomyBar's existing
        // nested layout: EconomyBar (134.4px = 0.07 * 1920) -> its VerticalLayoutGroup (padding
        // 8/8) -> ButtonsRow's HorizontalLayoutGroup (padding 8/8) -> 134.4 - 16 - 16 = 102.4px.
        // Preserved here so moving them into the new row doesn't shrink their tap size.
        private const float ShopConvertHeight = 102.4f;

        // Confirmed live 2026-08-25: minHeight is independent of preferredHeight, not tied to it --
        // ShopButton/ConvertButton's actual minHeight is 52, separate from ShopConvertHeight above.
        private const float ShopConvertMinHeight = 52f;

        // Height for the RestorationLabel/PointsText/RestoreButton sub-group -- deliberately
        // shorter than ShopConvertHeight. Stretching text/a smaller button to 102.4px would leave
        // them dwarfed in a mostly-empty box; the two groups get their own explicit
        // LayoutElement.preferredHeight instead of one uniform force-expanded height (see
        // ApplyInteractiveRowLayout), vertically centered together via the row's MiddleLeft align.
        private const float InfoElementHeight = 52f;

        private const float RowPaddingHorizontal = 12f;
        private const float RowPaddingVertical = 6f;
        private const float RowSpacing = 8f;

        // RestorationInteractiveRow's total sizeDelta.y: tall enough for the taller Shop/Convert
        // group plus this row's own vertical padding.
        private const float InteractiveRowHeight = ShopConvertHeight + (RowPaddingVertical * 2f);

        [MenuItem("Tools/Eighth Kind/Fix Restoration Bar Wiring")]
        public static void Run()
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

            Transform shopButtonTf = FindInScene("ShopButton");
            if (shopButtonTf == null)
            {
                Debug.LogError("[RestorationBarWireFix] ShopButton not found.");
                return;
            }

            Transform convertButtonTf = FindInScene("ConvertButton");
            if (convertButtonTf == null)
            {
                Debug.LogError("[RestorationBarWireFix] ConvertButton not found.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(customSafeArea.gameObject, "Add Restoration Bar");

            Transform interactiveRow = ResolveInteractiveRow(customSafeArea);
            ApplyInteractiveRowAnchors(interactiveRow);
            ApplyInteractiveRowLayout(interactiveRow);
            interactiveRow.SetSiblingIndex(economyBar.GetSiblingIndex() + 1);

            Transform vesselRow = ResolveVesselRow(customSafeArea);
            ApplyVesselRowAnchors(vesselRow);
            vesselRow.SetSiblingIndex(economyBar.GetSiblingIndex() + 1);

            // EconomyBar moves last, after the two new rows have staked out [0, VesselRowHeight]
            // and [VesselRowHeight, VesselRowHeight + InteractiveRowHeight] -- its own height
            // (read from its current anchors, not duplicated) is preserved, only its position moves.
            ApplyEconomyBarAnchors(economyBar, VesselRowHeight + InteractiveRowHeight);

            BuildVessel(vesselRow, out Image fillImage, out Image glowImage);
            BuildLabel(interactiveRow, vesselRow);

            if (pointsTextTf.parent != vesselRow) pointsTextTf.SetParent(vesselRow, false);
            StylePointsTextForRow(pointsTextTf);

            if (restoreButtonTf.parent != interactiveRow) restoreButtonTf.SetParent(interactiveRow, false);
            StyleRestoreButtonForRow(restoreButtonTf);

            if (shopButtonTf.parent != interactiveRow) shopButtonTf.SetParent(interactiveRow, false);
            StyleShopConvertButtonForRow(shopButtonTf);

            if (convertButtonTf.parent != interactiveRow) convertButtonTf.SetParent(interactiveRow, false);
            StyleShopConvertButtonForRow(convertButtonTf);

            // Force sibling order every run, regardless of build/reparent order above: Shop, Convert,
            // Label, PointsText, RestoreButton -- matches the spec's left-to-right reading order.
            shopButtonTf.SetSiblingIndex(0);
            convertButtonTf.SetSiblingIndex(1);
            interactiveRow.Find("RestorationLabel")?.SetSiblingIndex(2);
            restoreButtonTf.SetSiblingIndex(3);

            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("restorationFillImage").objectReferenceValue = fillImage;
            so.FindProperty("restorationGlowImage").objectReferenceValue = glowImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[RestorationBarWireFix] Done. Save the scene (Ctrl+S).\n" +
                      "Stack (bottom to top of CustomSafeArea, local px) -- unchanged from last run: " +
                      "RestorationVesselRow [0, 90] (non-interactive) -> RestorationInteractiveRow " +
                      "[90, 204.4] -> EconomyBar [204.4, 338.8] (moved, height preserved, now empty). " +
                      "2026-08-25: RestorationBarPlunger and RestorationBarVesselFrame (the glass-tube " +
                      "art attempt) were removed for good -- xp_bar_frame.png/xp_bar_plunger.png " +
                      "deleted, this tool no longer builds or wires either object. Vessel is now just " +
                      "Track + Glow + Fill, a plain colored bar. RestorationLabel/Shop/Convert font and " +
                      "sizing fixes from earlier today are unaffected.");
        }

        // ----- RestorationInteractiveRow (all tap targets, above the vessel) --------------------

        /// <summary>Finds RestorationInteractiveRow, or migrates an older RestorationInfoRow or the original combined RestorationBarRow by renaming it in place, or creates fresh.</summary>
        private static Transform ResolveInteractiveRow(Transform customSafeArea)
        {
            Transform row = customSafeArea.Find("RestorationInteractiveRow");
            if (row != null)
            {
                return row;
            }

            Transform legacy = customSafeArea.Find("RestorationInfoRow");
            if (legacy == null) legacy = FindInScene("RestorationInfoRow");
            if (legacy == null) legacy = customSafeArea.Find("RestorationBarRow");
            if (legacy == null) legacy = FindInScene("RestorationBarRow");

            if (legacy != null)
            {
                Undo.RecordObject(legacy.gameObject, "Rename Restoration Interactive Row");
                legacy.gameObject.name = "RestorationInteractiveRow";
                if (legacy.parent != customSafeArea)
                {
                    legacy.SetParent(customSafeArea, false);
                }
                return legacy;
            }

            var rowGo = new GameObject("RestorationInteractiveRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create RestorationInteractiveRow");
            rowGo.transform.SetParent(customSafeArea, false);
            return rowGo.transform;
        }

        /// <summary>Anchored to CustomSafeArea's bottom (y=0, same point VesselRow uses), offset up by VesselRowHeight so its bottom edge sits exactly at the vessel's top edge. Applied every run.</summary>
        private static void ApplyInteractiveRowAnchors(Transform row)
        {
            var rt = row.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Restoration Interactive Row Anchors");
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, VesselRowHeight);
            rt.sizeDelta = new Vector2(0f, InteractiveRowHeight);
            EditorUtility.SetDirty(rt);
        }

        /// <summary>
        /// childForceExpandHeight is OFF -- ShopButton/ConvertButton need ShopConvertHeight while
        /// Label/PointsText/RestoreButton need only InfoElementHeight, and force-expand can't give
        /// different children different heights. Each gets its own LayoutElement.preferredHeight
        /// instead (see StyleShopConvertButtonForRow / StylePointsTextForRow / StyleRestoreButtonForRow
        /// / BuildLabel), vertically centered together via MiddleLeft alignment.
        /// </summary>
        private static void ApplyInteractiveRowLayout(Transform row)
        {
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                return;
            }

            Undo.RecordObject(hlg, "Restoration Interactive Row Layout");
            hlg.spacing = RowSpacing;
            hlg.padding = new RectOffset((int)RowPaddingHorizontal, (int)RowPaddingHorizontal, (int)RowPaddingVertical, (int)RowPaddingVertical);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            EditorUtility.SetDirty(hlg);
        }

        private static void BuildLabel(Transform interactiveRow, Transform vesselRow)
        {
            TextMeshProUGUI referenceFont = FindReferenceFont();

            // RestorationLabel now lives on the vessel row (overlaid on the bar), not the button
            // row -- check there first and reuse it in place. Fall back to interactiveRow for scenes
            // that haven't been migrated yet; only create fresh (in interactiveRow) if neither has one.
            Transform labelTf = vesselRow.Find("RestorationLabel");
            if (labelTf == null)
            {
                labelTf = interactiveRow.Find("RestorationLabel");
            }
            if (labelTf == null)
            {
                var labelGo = new GameObject("RestorationLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create RestorationLabel");
                labelGo.transform.SetParent(interactiveRow, false);
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
            // Confirmed live 2026-08-25: fontSizeMin 24 / fontSizeMax 32 (auto-sized render ~27.3).
            label.fontSize = 32f;
            label.fontSizeMax = 32f;
            label.fontSizeMin = 24f;
            label.enableAutoSizing = true;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            // Was (0.7,0.7,0.7,1) -- flagged as low-contrast. Brightened toward near-white.
            label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            label.raycastTarget = false;
            EditorUtility.SetDirty(label);

            var labelLayout = labelTf.GetComponent<LayoutElement>();
            if (labelLayout == null) labelLayout = Undo.AddComponent<LayoutElement>(labelTf.gameObject);
            labelLayout.preferredWidth = 150f;
            labelLayout.flexibleWidth = 0f;
            labelLayout.preferredHeight = InfoElementHeight;
            // Defensive: guarantees InfoElementHeight even if some layout edge case would otherwise
            // clamp preferredHeight down (see StyleShopConvertButtonForRow's comment on the same fix).
            labelLayout.minHeight = InfoElementHeight;
            EditorUtility.SetDirty(labelLayout);
        }

        private static void StylePointsTextForRow(Transform pointsTextTf)
        {
            var layout = pointsTextTf.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Undo.RecordObject(layout, "Restoration Row Points Layout");
                layout.flexibleWidth = 0f;
                // Was 130f -- widened alongside the font bump below so autosizing doesn't shrink
                // back down to fit inside a too-narrow box.
                layout.preferredWidth = 140f;
                layout.preferredHeight = InfoElementHeight;
                layout.minHeight = InfoElementHeight;
                layout.layoutPriority = 1;
                EditorUtility.SetDirty(layout);
            }

            var tmp = pointsTextTf.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Restoration Row Points Font");
                tmp.fontSizeMin = 24f;
                tmp.fontSizeMax = 32f;
                EditorUtility.SetDirty(tmp);
            }
        }

        /// <summary>Fixed width/height for RestoreButton in its new row -- does not touch the button's own Button/Image/label components.</summary>
        private static void StyleRestoreButtonForRow(Transform restoreButtonTf)
        {
            var layout = restoreButtonTf.GetComponent<LayoutElement>();
            if (layout == null) layout = Undo.AddComponent<LayoutElement>(restoreButtonTf.gameObject);

            Undo.RecordObject(layout, "Restoration Row Restore Button Layout");
            layout.flexibleWidth = 0f;
            layout.preferredWidth = 120f;
            layout.preferredHeight = InfoElementHeight;
            layout.minHeight = InfoElementHeight;
            EditorUtility.SetDirty(layout);
        }

        /// <summary>
        /// ShopConvertHeight tall, flexible-width -- does not touch the button's own Button/Image/
        /// label components. minHeight (52) is confirmed independent of preferredHeight (102.4),
        /// not tied to it -- see ShopConvertMinHeight.
        /// </summary>
        private static void StyleShopConvertButtonForRow(Transform buttonTf)
        {
            var layout = buttonTf.GetComponent<LayoutElement>();
            if (layout == null) layout = Undo.AddComponent<LayoutElement>(buttonTf.gameObject);

            Undo.RecordObject(layout, "Restoration Row Shop/Convert Button Layout");
            layout.preferredWidth = 150f;
            layout.flexibleWidth = -1f;
            layout.flexibleHeight = -1f;
            layout.minWidth = 100f;
            layout.preferredHeight = ShopConvertHeight;
            layout.minHeight = ShopConvertMinHeight;
            EditorUtility.SetDirty(layout);
        }

        // ----- RestorationVesselRow (vessel only, full width, true bottom edge) -----------------

        private static Transform ResolveVesselRow(Transform customSafeArea)
        {
            Transform vesselRow = customSafeArea.Find("RestorationVesselRow");
            if (vesselRow != null)
            {
                return vesselRow;
            }

            var vesselGo = new GameObject("RestorationVesselRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(vesselGo, "Create RestorationVesselRow");
            vesselGo.transform.SetParent(customSafeArea, false);
            return vesselGo.transform;
        }

        /// <summary>Pinned to CustomSafeArea's own bottom (y=0) -- already safe-area-clipped by SafeAreaManager, so this is the one element allowed at the true edge. Applied every run.</summary>
        private static void ApplyVesselRowAnchors(Transform row)
        {
            var rt = row.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Restoration Vessel Row Anchors");
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, VesselRowHeight);
            EditorUtility.SetDirty(rt);
        }

        // ----- EconomyBar (moved, not owned by this tool otherwise) ------------------------------

        /// <summary>
        /// Moves EconomyBar's Y anchors so its bottom edge sits exactly at bottomY (same pixel space
        /// as VesselRowHeight/InteractiveRowHeight), preserving its current height -- READ from its
        /// existing anchorMax.y - anchorMin.y, not a hardcoded duplicate -- rather than resizing it.
        /// ShopButton/ConvertButton have already been moved out into the interactive row by the time
        /// this runs, so EconomyBar ends up empty; its position still moves, for scene hygiene (no
        /// floating empty container spatially overlapping live UI), not because anything renders here.
        /// </summary>
        private static void ApplyEconomyBarAnchors(Transform economyBar, float bottomY)
        {
            var rt = economyBar.GetComponent<RectTransform>();
            float heightFraction = rt.anchorMax.y - rt.anchorMin.y;
            float bottomFraction = bottomY / ReferenceHeight;

            Undo.RecordObject(rt, "Restoration EconomyBar Anchors");
            rt.anchorMin = new Vector2(rt.anchorMin.x, bottomFraction);
            rt.anchorMax = new Vector2(rt.anchorMax.x, bottomFraction + heightFraction);
            EditorUtility.SetDirty(rt);
        }

        // ----- Vessel build (Track/Glow/Fill only -- Plunger/VesselFrame removed 2026-08-25) -----

        /// <summary>
        /// Builds/migrates RestorationBarTrack as a direct, fully stretch-anchored child of
        /// vesselRow (no LayoutGroup/LayoutElement involved -- it's the row's only child, so it just
        /// fills the row's entire rect), then Glow/Fill inside it. Forces sibling order Glow(0)/Fill(1)
        /// every run.
        /// </summary>
        private static void BuildVessel(Transform vesselRow, out Image fillImage, out Image glowImage)
        {
            Transform trackTf = vesselRow.Find("RestorationBarTrack");
            if (trackTf == null)
            {
                // Migrate Track out of the old info row (built by an earlier version of this tool), if present.
                Transform legacyTrack = FindInScene("RestorationBarTrack");
                if (legacyTrack != null)
                {
                    trackTf = legacyTrack;
                    trackTf.SetParent(vesselRow, false);
                }
                else
                {
                    var trackGo = new GameObject("RestorationBarTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(trackGo, "Create RestorationBarTrack");
                    trackGo.transform.SetParent(vesselRow, false);
                    trackTf = trackGo.transform;
                }
            }
            else if (trackTf.parent != vesselRow)
            {
                trackTf.SetParent(vesselRow, false);
            }

            var trackRt = trackTf.GetComponent<RectTransform>();
            trackRt.anchorMin = Vector2.zero;
            trackRt.anchorMax = Vector2.one;
            trackRt.pivot = new Vector2(0.5f, 0.5f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = Vector2.zero;
            trackRt.offsetMin = Vector2.zero;
            trackRt.offsetMax = Vector2.zero;

            var track = trackTf.GetComponent<Image>();
            Undo.RecordObject(track, "Restoration Bar Track");
            track.color = TrackColor;
            track.raycastTarget = false;
            track.enabled = false;
            EditorUtility.SetDirty(track);

            Vector2 fillOffsetMin = new Vector2(59.4f, 18.0f);
            Vector2 fillOffsetMax = new Vector2(-59.4f, -18.0f);

            Transform glowTf = trackTf.Find("RestorationBarGlow");
            if (glowTf == null)
            {
                var glowGo = new GameObject("RestorationBarGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(glowGo, "Create RestorationBarGlow");
                glowGo.transform.SetParent(trackTf, false);
                glowTf = glowGo.transform;
            }

            var glowRt = glowTf.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = fillOffsetMin;
            glowRt.offsetMax = fillOffsetMax;

            var glow = glowTf.GetComponent<Image>();
            Undo.RecordObject(glow, "Restoration Bar Glow");
            glow.color = new Color(FillColor.r, FillColor.g, FillColor.b, 0.4f);
            glow.type = Image.Type.Filled;
            glow.fillMethod = Image.FillMethod.Horizontal;
            glow.fillOrigin = (int)Image.OriginHorizontal.Left;
            glow.fillAmount = 0f;
            glow.raycastTarget = false;
            EditorUtility.SetDirty(glow);

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
            fillRt.offsetMin = fillOffsetMin;
            fillRt.offsetMax = fillOffsetMax;

            var fill = fillTf.GetComponent<Image>();
            Undo.RecordObject(fill, "Restoration Bar Fill");
            fill.color = FillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            EditorUtility.SetDirty(fill);

            if (fill.sprite != null && glow.sprite == null)
            {
                glow.sprite = fill.sprite;
            }

            // Sibling order: Glow (0), Fill (1)
            glowTf.SetSiblingIndex(0);
            fillTf.SetSiblingIndex(1);

            fillImage = fill;
            glowImage = glow;
        }

        private static TextMeshProUGUI FindReferenceFont()
        {
            Transform cashText = FindInScene("CashText");
            return cashText != null ? cashText.GetComponent<TextMeshProUGUI>() : null;
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
