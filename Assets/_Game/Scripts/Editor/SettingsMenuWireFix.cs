#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BrainDrain.UI;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// One-shot scene builder for the Settings menu (§41): a gear button (scene-authored,
    /// parented under CustomSafeArea -- not a Canvas sibling, the exact mistake §36 fixed for
    /// LogOpenButton) that opens a panel with a mute toggle and 4 background-music track rows.
    ///
    /// Builds:
    ///   1. SettingsButton -- top-right, under LogOpenButton, parented under CustomSafeArea.
    ///   2. SettingsPanel -- background + title + mute toggle + 4 track rows + close button,
    ///      SettingsUIController living on the panel itself (same self-deactivating Awake()
    ///      pattern as ShopUIController -- the panel must start ACTIVE so Awake() actually runs
    ///      and wires the buttons, then it hides itself).
    ///   3. Wires SettingsUIController's fields, MainUIController's settingsButton/
    ///      settingsUIController fields, and BackgroundMusicManager's availableTracks (the 4
    ///      loop-safe "Full" CyberWare clips).
    /// Idempotent: finds and updates existing SettingsButton/SettingsPanel GameObjects on
    /// re-run rather than duplicating them, same convention as ShopPanelLayoutFix.
    /// </summary>
    public static class SettingsMenuWireFix
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly string[] TrackNames =
        {
            "Find and Seek",
            "Gutters Filled with Light",
            "Intrusion Detected",
            "T.SUM-12",
        };

        // The 4 loop-safe "Full" clips, one per song, in the default Aceyfer asked for.
        // NOTE: "Gutters Filled with Light" has no "_Full_" file in the CyberWare pack -- its
        // Loops/ folder only ships three "G1-1/G1-2/G1-3" variants with no combined mix. G1-1 is
        // used here as a best-guess stand-in (it's also the clip BackgroundMusicManager.
        // backgroundMusicClip already hardcoded before this change, so it's a proven-working
        // asset, not a random pick) -- flagged for Aceyfer to confirm/replace.
        private static readonly string[] TrackClipPaths =
        {
            "Assets/CyberWare - Game Music Assets/Find and Seek/Loops/0120_Find-and-Seek_Full_65bpm4-4_L28M.wav",
            "Assets/CyberWare - Game Music Assets/Gutters Filled with Light/Loops/0110_Gutters-Filled-With-Light_G1-1_65bpm4-4_L28M.wav",
            "Assets/CyberWare - Game Music Assets/Intrusion Detected/Loops/0130_Intrusion-Detected_Full_130bpm4-4_L48M.wav",
            "Assets/CyberWare - Game Music Assets/T.SUM-12/Loops/0140_T.SUM-12_Full_107bpm4-4_L32M.wav",
        };

        [MenuItem("BrainDrain/Build Settings Menu")]
        public static void BuildSettingsMenu()
        {
            if (EditorToolGuard.BlockedByPlayMode("SettingsMenuWireFix.BuildSettingsMenu")) return;

            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Transform safeArea = GameObject.Find("Canvas/CustomSafeArea")?.transform;
            if (safeArea == null)
            {
                Debug.LogError("[SettingsMenuWireFix] Canvas/CustomSafeArea not found.");
                return;
            }

            Button settingsButton = BuildSettingsButton(safeArea);
            SettingsUIController settingsUI = BuildSettingsPanel(safeArea);

            WireMainUIController(settingsButton, settingsUI);
            WireBackgroundMusicTracks();

            EditorUtility.SetDirty(settingsUI);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[SettingsMenuWireFix] Settings gear button + panel built and wired (mute toggle, 4 track rows). Scene saved.");
        }

        private static Button BuildSettingsButton(Transform safeArea)
        {
            Transform existing = safeArea.Find("SettingsButton");
            GameObject host = existing != null ? existing.gameObject : new GameObject("SettingsButton", typeof(RectTransform));
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(host, "Create SettingsButton");
            }
            host.transform.SetParent(safeArea, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            // Top-right, below the runtime-built PocketOpenButton. PocketPanelUI places that
            // 140x50 button at y=-402 (LogOpenButton's -340 minus its 50px height and 12px gap),
            // so y=-464 leaves the same 12px gap beneath it. The previous y=-400 overlapped the
            // Pocket button almost exactly and one click could open both panels.
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(50f, 50f);
            rect.anchoredPosition = new Vector2(-20f, -464f);

            Image image = host.GetComponent<Image>();
            if (image == null) image = host.AddComponent<Image>();
            image.color = HexColor("#4A4E5D");

            Button button = host.GetComponent<Button>();
            if (button == null) button = host.AddComponent<Button>();
            button.targetGraphic = image;

            // No gear-icon sprite exists in the project yet (art debt, not urgent) -- plain text
            // avoids the risk of an unsupported glyph rendering as a tofu box (the exact bug §30
            // hit with a missing arrow glyph).
            Transform existingTextHost = host.transform.Find("SettingsButtonText");
            GameObject textHost = existingTextHost != null ? existingTextHost.gameObject : new GameObject("SettingsButtonText", typeof(RectTransform));
            textHost.transform.SetParent(host.transform, false);
            RectTransform textRect = textHost.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI label = textHost.GetComponent<TextMeshProUGUI>();
            if (label == null) label = textHost.AddComponent<TextMeshProUGUI>();
            label.text = "SET";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.raycastTarget = false;

            return button;
        }

        private static SettingsUIController BuildSettingsPanel(Transform safeArea)
        {
            Transform existingPanel = safeArea.Find("SettingsPanel");
            GameObject panel = existingPanel != null
                ? existingPanel.gameObject
                : new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            if (existingPanel == null)
            {
                Undo.RegisterCreatedObjectUndo(panel, "Create SettingsPanel");
            }
            panel.transform.SetParent(safeArea, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.25f);
            panelRect.anchorMax = new Vector2(0.9f, 0.75f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            Image background = panel.GetComponent<Image>();
            if (background == null) background = panel.AddComponent<Image>();
            background.color = HexColor("#1A1D26");

            if (panel.GetComponent<CanvasGroup>() == null)
            {
                panel.AddComponent<CanvasGroup>();
            }

            BuildTitle(panel.transform);
            Toggle muteToggle = BuildMuteToggle(panel.transform);
            Button[] trackButtons = new Button[TrackNames.Length];
            TextMeshProUGUI[] trackLabels = new TextMeshProUGUI[TrackNames.Length];
            BuildTrackRows(panel.transform, trackButtons, trackLabels);
            Button closeButton = BuildCloseButton(panel);

            SettingsUIController settingsUI = panel.GetComponent<SettingsUIController>();
            if (settingsUI == null)
            {
                settingsUI = panel.AddComponent<SettingsUIController>();
            }

            SerializedObject so = new SerializedObject(settingsUI);
            so.FindProperty("settingsPanel").objectReferenceValue = panel;
            so.FindProperty("muteToggle").objectReferenceValue = muteToggle;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;

            SerializedProperty buttonsProp = so.FindProperty("trackButtons");
            SerializedProperty labelsProp = so.FindProperty("trackLabels");
            buttonsProp.arraySize = trackButtons.Length;
            labelsProp.arraySize = trackLabels.Length;
            for (int i = 0; i < trackButtons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = trackButtons[i];
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = trackLabels[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Deliberately NOT calling panel.SetActive(false) here -- SettingsUIController lives
            // on this same GameObject, and if it's saved inactive, Awake() never runs at all
            // (Unity only calls Awake on objects active at load), which would silently break
            // every button's onClick wiring (the exact §36-adjacent bug ShopPanelLayoutFix's own
            // comment describes). SettingsUIController.Awake() deactivates itself, AFTER wiring.
            return settingsUI;
        }

        private static void BuildTitle(Transform panel)
        {
            Transform existing = panel.Find("Title");
            GameObject host = existing != null ? existing.gameObject : new GameObject("Title", typeof(RectTransform));
            host.transform.SetParent(panel, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 60f);
            rect.anchoredPosition = new Vector2(0f, -16f);

            TextMeshProUGUI label = host.GetComponent<TextMeshProUGUI>();
            if (label == null) label = host.AddComponent<TextMeshProUGUI>();
            label.text = "SETTINGS";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 32f;
            label.raycastTarget = false;
        }

        private static Toggle BuildMuteToggle(Transform panel)
        {
            Transform existingRow = panel.Find("MuteRow");
            GameObject row = existingRow != null ? existingRow.gameObject : new GameObject("MuteRow", typeof(RectTransform));
            row.transform.SetParent(panel, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(-64f, 48f);
            rowRect.anchoredPosition = new Vector2(0f, -96f);

            Transform existingToggleHost = row.transform.Find("MuteToggle");
            GameObject toggleHost = existingToggleHost != null
                ? existingToggleHost.gameObject
                : new GameObject("MuteToggle", typeof(RectTransform));
            toggleHost.transform.SetParent(row.transform, false);

            RectTransform toggleRect = toggleHost.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 0.5f);
            toggleRect.anchorMax = new Vector2(0f, 0.5f);
            toggleRect.pivot = new Vector2(0f, 0.5f);
            toggleRect.sizeDelta = new Vector2(32f, 32f);
            toggleRect.anchoredPosition = Vector2.zero;

            Toggle toggle = toggleHost.GetComponent<Toggle>();
            if (toggle == null) toggle = toggleHost.AddComponent<Toggle>();

            Transform existingBox = toggleHost.transform.Find("Box");
            GameObject box = existingBox != null ? existingBox.gameObject : new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(toggleHost.transform, false);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.one;
            boxRect.sizeDelta = Vector2.zero;
            boxRect.anchoredPosition = Vector2.zero;
            Image boxImage = box.GetComponent<Image>();
            if (boxImage == null) boxImage = box.AddComponent<Image>();
            boxImage.color = HexColor("#4A4E5D");

            Transform existingCheck = box.transform.Find("Checkmark");
            GameObject check = existingCheck != null ? existingCheck.gameObject : new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(box.transform, false);
            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.sizeDelta = Vector2.zero;
            checkRect.anchoredPosition = Vector2.zero;
            Image checkImage = check.GetComponent<Image>();
            if (checkImage == null) checkImage = check.AddComponent<Image>();
            checkImage.color = HexColor("#00F0FF");

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            Transform existingLabelHost = row.transform.Find("MuteLabel");
            GameObject labelHost = existingLabelHost != null ? existingLabelHost.gameObject : new GameObject("MuteLabel", typeof(RectTransform));
            labelHost.transform.SetParent(row.transform, false);
            RectTransform labelRect = labelHost.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(44f, 0f);
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelHost.GetComponent<TextMeshProUGUI>();
            if (label == null) label = labelHost.AddComponent<TextMeshProUGUI>();
            label.text = "MUTE";
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 24f;
            label.raycastTarget = false;

            return toggle;
        }

        private static void BuildTrackRows(Transform panel, Button[] trackButtons, TextMeshProUGUI[] trackLabels)
        {
            Transform existingContainer = panel.Find("TrackRows");
            GameObject container = existingContainer != null
                ? existingContainer.gameObject
                : new GameObject("TrackRows", typeof(RectTransform));
            container.transform.SetParent(panel, false);

            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(32f, 72f);
            containerRect.offsetMax = new Vector2(-32f, -160f);

            VerticalLayoutGroup layoutGroup = container.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = container.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 12f;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            for (int i = 0; i < TrackNames.Length; i++)
            {
                Transform existingRow = container.transform.Find($"TrackRow{i}");
                GameObject row = existingRow != null ? existingRow.gameObject : new GameObject($"TrackRow{i}", typeof(RectTransform));
                row.transform.SetParent(container.transform, false);
                row.transform.SetSiblingIndex(i);

                LayoutElement layoutElement = row.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = row.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 48f;
                layoutElement.flexibleWidth = 1f;

                Image rowImage = row.GetComponent<Image>();
                if (rowImage == null) rowImage = row.AddComponent<Image>();
                rowImage.color = HexColor("#2A2E3D");

                Button button = row.GetComponent<Button>();
                if (button == null) button = row.AddComponent<Button>();
                button.targetGraphic = rowImage;

                Transform existingLabelHost = row.transform.Find("TrackLabel");
                GameObject labelHost = existingLabelHost != null ? existingLabelHost.gameObject : new GameObject("TrackLabel", typeof(RectTransform));
                labelHost.transform.SetParent(row.transform, false);
                RectTransform labelRect = labelHost.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(16f, 0f);
                labelRect.offsetMax = new Vector2(-16f, 0f);

                TextMeshProUGUI label = labelHost.GetComponent<TextMeshProUGUI>();
                if (label == null) label = labelHost.AddComponent<TextMeshProUGUI>();
                label.text = TrackNames[i];
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSize = 22f;
                label.raycastTarget = false;

                trackButtons[i] = button;
                trackLabels[i] = label;
            }
        }

        private static Button BuildCloseButton(GameObject settingsPanel)
        {
            Transform existingHost = settingsPanel.transform.Find("CloseButton");
            GameObject host = existingHost != null ? existingHost.gameObject : new GameObject("CloseButton", typeof(RectTransform));
            host.transform.SetParent(settingsPanel.transform, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = new Vector2(-16f, -16f);

            Image image = host.GetComponent<Image>();
            if (image == null) image = host.AddComponent<Image>();
            image.color = HexColor("#4A4E5D");

            Button button = host.GetComponent<Button>();
            if (button == null) button = host.AddComponent<Button>();
            button.targetGraphic = image;

            Transform existingTextHost = host.transform.Find("CloseText");
            GameObject textHost = existingTextHost != null ? existingTextHost.gameObject : new GameObject("CloseText", typeof(RectTransform));
            textHost.transform.SetParent(host.transform, false);
            RectTransform textRect = textHost.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI label = textHost.GetComponent<TextMeshProUGUI>();
            if (label == null) label = textHost.AddComponent<TextMeshProUGUI>();
            label.text = "X";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.raycastTarget = false;

            return button;
        }

        private static void WireMainUIController(Button settingsButton, SettingsUIController settingsUI)
        {
            MainUIController mainUI = Object.FindAnyObjectByType<MainUIController>(FindObjectsInactive.Include);
            if (mainUI == null)
            {
                Debug.LogWarning("[SettingsMenuWireFix] No MainUIController found in the scene; settingsButton/settingsUIController not wired.");
                return;
            }

            SerializedObject so = new SerializedObject(mainUI);
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("settingsUIController").objectReferenceValue = settingsUI;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mainUI);
        }

        private static void WireBackgroundMusicTracks()
        {
            BackgroundMusicManager musicManager = Object.FindAnyObjectByType<BackgroundMusicManager>(FindObjectsInactive.Include);
            if (musicManager == null)
            {
                Debug.LogWarning("[SettingsMenuWireFix] No BackgroundMusicManager found in the scene; availableTracks not wired.");
                return;
            }

            SerializedObject so = new SerializedObject(musicManager);
            SerializedProperty tracksProp = so.FindProperty("availableTracks");
            tracksProp.arraySize = TrackClipPaths.Length;
            for (int i = 0; i < TrackClipPaths.Length; i++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(TrackClipPaths[i]);
                if (clip == null)
                {
                    Debug.LogWarning($"[SettingsMenuWireFix] Could not load AudioClip at '{TrackClipPaths[i]}'.");
                }

                tracksProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(musicManager);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
#endif
