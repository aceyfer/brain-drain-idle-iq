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
    /// Repairs the COGS_Narrator_Panel layout and wires the close button.
    /// Safe to re-run — idempotent.
    ///
    /// Layout (portrait 1080×1920):
    ///   Panel   : x=5–95% of SafeArea, y=63% bottom anchor, 200 px tall
    ///   Header  : full-width, 48 px at panel top — title text + × close button
    ///   Content : fills panel below header — portrait (90×90) left, dialogue text right
    ///
    /// Gap below CurrencyHeader (~79.2%) : ~120 px clear
    /// Gap above Snotting top   (~37.5%) : ~489 px clear
    /// </summary>
    public static class FixCOGSDialogueLayout
    {
        private const float PanelAnchorMinX = 0.05f;
        private const float PanelAnchorMaxX = 0.95f;
        private const float PanelAnchorY    = 0.63f;
        private const float PanelHeight     = 200f;

        private const float HeaderHeight    = 48f;

        private const float PortraitSize    = 90f;
        private const float PortraitLeft    = 10f;

        [MenuItem("BrainDrain/Fix COGS Dialogue Layout")]
        private static void RunFix()
        {
            var dialogueUI = Object.FindAnyObjectByType<DialogueDisplayUI>();
            if (dialogueUI == null)
            {
                Debug.LogError("[FixCOGSDialogueLayout] DialogueDisplayUI not found. Open the game scene first.");
                return;
            }

            Transform panelTf = FindInScene("COGS_Narrator_Panel");
            if (panelTf == null)
            {
                Debug.LogError("[FixCOGSDialogueLayout] COGS_Narrator_Panel not found.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panelTf.gameObject, "Fix COGS Dialogue Layout");

            FixPanel(panelTf);

            Transform headerTf = FindChildRecursive(panelTf, "Header");
            if (headerTf != null) FixHeader(headerTf);

            Transform contentTf = FindChildRecursive(panelTf, "ContentArea");
            if (contentTf != null)
            {
                FixContentArea(contentTf);

                Transform avatarTf = FindChildRecursive(contentTf, "AvatarFrame");
                if (avatarTf != null) FixAvatar(avatarTf);

                Transform dialogueTf = FindChildRecursive(contentTf, "DialogueText");
                if (dialogueTf != null) FixDialogueText(dialogueTf);
            }

            if (headerTf != null)
                EnsureCloseButton(headerTf, dialogueUI);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FixCOGSDialogueLayout] Done. Panel is 90% wide, 200 px tall at y=63%. Save scene (Ctrl+S).");
        }

        // ── Panel ──────────────────────────────────────────────────────────────

        private static void FixPanel(Transform tf)
        {
            // Root cause fix: both x-anchors were 0.5 → zero width.
            // Correct: stretch across 5%–95% of SafeArea.
            var rt = tf.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(PanelAnchorMinX, PanelAnchorY);
            rt.anchorMax        = new Vector2(PanelAnchorMaxX, PanelAnchorY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, PanelHeight);
            rt.pivot            = new Vector2(0.5f, 0f);
            EditorUtility.SetDirty(rt);
        }

        // ── Header (TitleBar) ──────────────────────────────────────────────────

        private static void FixHeader(Transform tf)
        {
            var rt = tf.GetComponent<RectTransform>();
            // Stretch full width, pin to top of panel, 48 px tall.
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, HeaderHeight);
            rt.pivot            = new Vector2(0.5f, 1f);
            EditorUtility.SetDirty(rt);
        }

        // ── ContentArea ────────────────────────────────────────────────────────

        private static void FixContentArea(Transform tf)
        {
            var rt = tf.GetComponent<RectTransform>();
            // Full stretch, shrunk by header at top.
            // offsetMin.y=0 (bottom flush), offsetMax.y=-HeaderHeight (below header).
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, -HeaderHeight * 0.5f);
            rt.sizeDelta        = new Vector2(0f, -HeaderHeight);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            EditorUtility.SetDirty(rt);
        }

        // ── Portrait ──────────────────────────────────────────────────────────

        private static void FixAvatar(Transform tf)
        {
            var rt = tf.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(PortraitLeft, 0f);
            rt.sizeDelta        = new Vector2(PortraitSize, PortraitSize);
            rt.pivot            = new Vector2(0f, 0.5f);
            EditorUtility.SetDirty(rt);
        }

        // ── Dialogue text ──────────────────────────────────────────────────────

        private static void FixDialogueText(Transform tf)
        {
            // Portrait occupies left 100 px (PortraitLeft=10 + PortraitSize=90).
            // Text starts at 120 px (10 px gap after portrait), ends 10 px from right.
            // With stretch anchor (0,0)-(1,1):
            //   offsetMin.x = 120  → anchoredPosition.x - sizeDelta.x/2 = 120
            //   offsetMax.x = -10  → anchoredPosition.x + sizeDelta.x/2 = -10
            //   ⟹ anchoredPosition.x = (120-10)/2 = 55,  sizeDelta.x = -10-120 = -130
            var rt = tf.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(55f, 0f);
            rt.sizeDelta        = new Vector2(-130f, -16f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            EditorUtility.SetDirty(rt);

            var tmp = tf.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin      = 24f;
                tmp.fontSizeMax      = 32f;
                EditorUtility.SetDirty(tmp);
            }
        }

        // ── Close button ───────────────────────────────────────────────────────

        private static void EnsureCloseButton(Transform headerTf, DialogueDisplayUI dialogueUI)
        {
            Transform existing = headerTf.Find("CloseButton");
            Button closeBtn    = existing != null ? existing.GetComponent<Button>() : null;

            if (closeBtn == null)
            {
                // Remove broken placeholder if any, then create fresh.
                if (existing != null)
                    Undo.DestroyObjectImmediate(existing.gameObject);

                var go = new GameObject("CloseButton", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create COGS CloseButton");
                go.transform.SetParent(headerTf, false);

                var img = go.AddComponent<Image>();
                img.color         = new Color(0f, 0f, 0f, 0.6f);
                img.raycastTarget = true;

                closeBtn = go.AddComponent<Button>();
                closeBtn.targetGraphic = img;

                var labelGo = new GameObject("CloseButtonText", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create COGS CloseButtonText");
                labelGo.transform.SetParent(go.transform, false);

                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin        = Vector2.zero;
                labelRt.anchorMax        = Vector2.one;
                labelRt.anchoredPosition = Vector2.zero;
                labelRt.sizeDelta        = Vector2.zero;

                var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                labelTmp.text          = "×";
                labelTmp.fontSize      = 22f;
                labelTmp.alignment     = TextAlignmentOptions.Center;
                labelTmp.color         = Color.white;
                labelTmp.raycastTarget = false;

                Debug.Log("[FixCOGSDialogueLayout] Created fresh CloseButton under Header.");
            }

            // Right-anchor inside header, 48×48 (full header height).
            var rt = closeBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-4f, 0f);
            rt.sizeDelta        = new Vector2(HeaderHeight, 0f);
            rt.pivot            = new Vector2(1f, 0.5f);
            EditorUtility.SetDirty(rt);

            // Wire to DialogueDisplayUI.closeButton via SerializedObject.
            var so   = new SerializedObject(dialogueUI);
            var prop = so.FindProperty("closeButton");
            if (prop != null)
            {
                prop.objectReferenceValue = closeBtn;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dialogueUI);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
