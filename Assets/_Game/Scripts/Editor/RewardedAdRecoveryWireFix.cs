#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Builds and wires §57's rewarded-ad idle-window recovery popup into the active scene:
    /// idempotent, same pattern as WeatherSystemWireFix/ButtonThemeWireFix -- finds and updates
    /// existing objects by name on re-run rather than duplicating.
    ///
    /// Places "RewardedAdRecoveryPopup" as a direct child of the root Canvas (same level as
    /// ChaosPopUpCanvas), with its OWN local Canvas (overrideSorting, sortingOrder 400 -- above
    /// normal HUD, below IntelCardUI's first-run onboarding cards at 500) and its own local
    /// CanvasGroup. That second point is load-bearing, not cosmetic: RewardedAdRecoveryUIController
    /// mirrors RandomEventUIController's GetComponentInParent&lt;Canvas/CanvasGroup&gt; fallback,
    /// and §63 already showed that fallback is only safe when the popup owns its own local
    /// components rather than climbing to the root Canvas -- this tool guarantees that
    /// precondition instead of leaving it to chance.
    ///
    /// Also places RewardedAdRecoveryManager under the scene's "_Systems" parent, matching
    /// SceneManagerWiring's own parenting convention for RandomEventManager/DialogueManager --
    /// not strictly required (the class self-bootstraps if missing), but keeps it discoverable
    /// in the same place as this project's other Systems singletons instead of floating loose.
    /// </summary>
    public static class RewardedAdRecoveryWireFix
    {
        private const string SystemsParentName = "_Systems";
        private const string PopupObjectName = "RewardedAdRecoveryPopup";
        private const int PopupSortingOrder = 400;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        private static readonly Color WatchAdButtonColor = new Color(0f, 0.941f, 1f, 1f); // matches the project's existing #00F0FF accent
        private static readonly Color CloseButtonColor = new Color(0.35f, 0.35f, 0.35f, 0.85f);

        [MenuItem("BrainDrain/Fix Rewarded Ad Recovery Popup")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("RewardedAdRecoveryWireFix.Fix")) return;

            Transform canvasTransform = FindInSceneIncludingInactive("Canvas");
            if (canvasTransform == null)
            {
                Debug.LogWarning("[RewardedAdRecoveryWireFix] No 'Canvas' found in the scene -- cannot place the popup. Aborting.");
                return;
            }

            GameObject popup = FindOrCreatePopup(canvasTransform);
            RewardedAdRecoveryUIController controller = WireController(popup);
            WireManagerHost();

            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RewardedAdRecoveryWireFix] Done -- RewardedAdRecoveryPopup built/updated and RewardedAdRecoveryUIController wired. Save the scene (Ctrl+S) to persist.");
        }

        private static GameObject FindOrCreatePopup(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find(PopupObjectName);
            GameObject popup = existing != null ? existing.gameObject : null;

            if (popup == null)
            {
                popup = new GameObject(PopupObjectName, typeof(RectTransform));
                popup.transform.SetParent(canvasTransform, false);
                Undo.RegisterCreatedObjectUndo(popup, "Create " + PopupObjectName);
            }

            SetFullScreenRect(popup);

            // Own local Canvas + CanvasGroup -- see class doc comment for why this is a hard
            // requirement, not a style choice.
            Canvas ownCanvas = popup.GetComponent<Canvas>();
            if (ownCanvas == null) { ownCanvas = popup.AddComponent<Canvas>(); }
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = PopupSortingOrder;

            if (popup.GetComponent<GraphicRaycaster>() == null)
            {
                popup.AddComponent<GraphicRaycaster>();
            }

            CanvasGroup ownGroup = popup.GetComponent<CanvasGroup>();
            if (ownGroup == null) { ownGroup = popup.AddComponent<CanvasGroup>(); }

            // Backdrop -- full-screen dim behind the centered panel, blocks clicks to whatever's
            // underneath while the popup is open (RewardedAdRecoveryUIController's SetCanvasState
            // gates blocksRaycasts on the shared CanvasGroup, not on this Image specifically).
            Image backdrop = popup.GetComponent<Image>();
            if (backdrop == null) { backdrop = popup.AddComponent<Image>(); }
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            GameObject panel = FindOrCreateChild(popup.transform, "Panel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(560f, 420f);

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null) { panelImage = panel.AddComponent<Image>(); }
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;

            BuildTitleText(panel.transform);
            BuildProgressText(panel.transform);
            BuildWatchAdButton(panel.transform);
            BuildCloseButton(panel.transform);

            return popup;
        }

        private static void BuildTitleText(Transform panelTransform)
        {
            GameObject titleObject = FindOrCreateChild(panelTransform, "TitleText");
            RectTransform rect = titleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.55f);
            rect.anchorMax = new Vector2(0.95f, 0.92f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = titleObject.GetComponent<TextMeshProUGUI>();
            if (text == null) { text = titleObject.AddComponent<TextMeshProUGUI>(); }
            text.text = "COGS docked you 0 IQ while you were gone.\nWatch an ad to get some back.";
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 28f;
            text.raycastTarget = false;
        }

        private static void BuildProgressText(Transform panelTransform)
        {
            GameObject progressObject = FindOrCreateChild(panelTransform, "ProgressText");
            RectTransform rect = progressObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.40f);
            rect.anchorMax = new Vector2(0.95f, 0.52f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = progressObject.GetComponent<TextMeshProUGUI>();
            if (text == null) { text = progressObject.AddComponent<TextMeshProUGUI>(); }
            text.text = "0/8 ads watched";
            text.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            text.raycastTarget = false;
        }

        private static void BuildWatchAdButton(Transform panelTransform)
        {
            GameObject buttonObject = FindOrCreateChild(panelTransform, "WatchAdButton");
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.2f);
            rect.anchorMax = new Vector2(0.9f, 0.36f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            if (image == null) { image = buttonObject.AddComponent<Image>(); }
            image.color = WatchAdButtonColor;

            Button button = buttonObject.GetComponent<Button>();
            if (button == null) { button = buttonObject.AddComponent<Button>(); }
            button.targetGraphic = image;

            BuildButtonLabel(buttonObject.transform, "WATCH AD", Color.black);
        }

        private static void BuildCloseButton(Transform panelTransform)
        {
            GameObject buttonObject = FindOrCreateChild(panelTransform, "CloseButton");
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.06f);
            rect.anchorMax = new Vector2(0.7f, 0.18f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            if (image == null) { image = buttonObject.AddComponent<Image>(); }
            image.color = CloseButtonColor;

            Button button = buttonObject.GetComponent<Button>();
            if (button == null) { button = buttonObject.AddComponent<Button>(); }
            button.targetGraphic = image;

            BuildButtonLabel(buttonObject.transform, "NOT NOW", Color.white);
        }

        private static void BuildButtonLabel(Transform buttonTransform, string label, Color textColor)
        {
            GameObject textObject = FindOrCreateChild(buttonTransform, "Label");
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null) { text = textObject.AddComponent<TextMeshProUGUI>(); }
            text.text = label;
            text.color = textColor;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 20f;
            text.raycastTarget = false;
        }

        private static RewardedAdRecoveryUIController WireController(GameObject popup)
        {
            RewardedAdRecoveryUIController controller = popup.GetComponent<RewardedAdRecoveryUIController>();
            if (controller == null) { controller = popup.AddComponent<RewardedAdRecoveryUIController>(); }

            Transform panel = popup.transform.Find("Panel");

            AssignObjectField(controller, "recoveryPopupPanel", panel != null ? panel.gameObject : null);
            AssignObjectField(controller, "titleText", panel != null ? panel.Find("TitleText")?.GetComponent<TextMeshProUGUI>() : null);
            AssignObjectField(controller, "progressText", panel != null ? panel.Find("ProgressText")?.GetComponent<TextMeshProUGUI>() : null);
            AssignObjectField(controller, "watchAdButton", panel != null ? panel.Find("WatchAdButton")?.GetComponent<Button>() : null);
            AssignObjectField(controller, "closeButton", panel != null ? panel.Find("CloseButton")?.GetComponent<Button>() : null);

            return controller;
        }

        private static void WireManagerHost()
        {
            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent == null)
            {
                systemsParent = new GameObject(SystemsParentName);
                Undo.RegisterCreatedObjectUndo(systemsParent, "Create " + SystemsParentName);
            }

            RewardedAdRecoveryManager existing = Object.FindAnyObjectByType<RewardedAdRecoveryManager>();
            if (existing != null)
            {
                existing.transform.SetParent(systemsParent.transform, false);
                return;
            }

            var host = new GameObject(nameof(RewardedAdRecoveryManager));
            host.transform.SetParent(systemsParent.transform, false);
            host.AddComponent<RewardedAdRecoveryManager>();
            Undo.RegisterCreatedObjectUndo(host, "Create " + nameof(RewardedAdRecoveryManager));
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static void SetFullScreenRect(GameObject go)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>GameObject.Find/transform.Find on a loose root only ever sees active objects. This walks every scene root's hierarchy directly (including inactive ones) to find a GameObject by exact name -- same helper as WeatherSystemWireFix, needed here since this popup should exist (inactive) even before any offline-decay event has ever fired.</summary>
        private static Transform FindInSceneIncludingInactive(string name)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindRecursive(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform current, string name)
        {
            if (current.name == name)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindRecursive(current.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>Overwrites a serialized single-object field via SerializedObject -- safe regardless of the field's C# access level. Same technique every other WireFix tool this session uses.</summary>
        private static void AssignObjectField(Component component, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[RewardedAdRecoveryWireFix] Could not find serialized field '{fieldName}' on {component.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
