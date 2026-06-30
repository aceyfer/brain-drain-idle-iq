#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Editor-only debug cheat panel for instant progression testing. Triple-tap the HUD's IQ
    /// text within TripleTapWindowSeconds to toggle it open/closed. Fully self-building --
    /// self-bootstraps via RuntimeInitializeOnLoadMethod, finds HUDController.PlayerIQText
    /// itself, and constructs its own panel/buttons procedurally, so there's no Inspector
    /// wiring step that could be left unset (the exact failure mode that broke the Shop panel
    /// twice earlier this session). Entirely #if UNITY_EDITOR -- compiles out completely in
    /// any build, even a Development Build, since UNITY_EDITOR is only ever defined inside the
    /// Editor itself.
    /// </summary>
    public sealed class DebugCheatPanel : MonoBehaviour
    {
        private const string PanelOpenEditorPrefsKey = "BrainDrain_DebugPanelOpen";
        private const float TripleTapWindowSeconds = 0.6f;
        private const int TripleTapCount = 3;

        private static readonly Color HotPink = new Color32(0xFF, 0x14, 0x93, 0xFF);
        private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.92f);
        private static readonly Color ButtonFill = new Color(1f, 0.078f, 0.576f, 0.22f);

        private static DebugCheatPanel instance;

        public static DebugCheatPanel Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var host = new GameObject("DebugCheatPanel (Auto)");
                instance = host.AddComponent<DebugCheatPanel>();
                return instance;
            }
        }

        private readonly List<float> recentTapTimes = new();
        private GameObject panelObject;
        private bool builtUI;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            HUDController hud = FindAnyObjectByType<HUDController>();
            if (hud == null || hud.PlayerIQText == null)
            {
                Debug.LogWarning("[DebugCheatPanel] Could not find HUDController.PlayerIQText to hook the triple-tap trigger to.");
                return;
            }

            HookTripleTap(hud.PlayerIQText);

            RectTransform canvasRect = hud.PlayerIQText.canvas != null ? hud.PlayerIQText.canvas.transform as RectTransform : null;
            BuildPanel(canvasRect);

            bool wasOpen = EditorPrefs.GetBool(PanelOpenEditorPrefsKey, false);
            SetPanelVisible(wasOpen);
        }

        private void HookTripleTap(TextMeshProUGUI text)
        {
            GameObject host = text.gameObject;
            Button tapButton = host.GetComponent<Button>();
            if (tapButton == null)
            {
                tapButton = host.AddComponent<Button>();
                tapButton.transition = Selectable.Transition.None;
            }

            text.raycastTarget = true;
            tapButton.onClick.AddListener(RegisterTap);
        }

        private void RegisterTap()
        {
            float now = Time.unscaledTime;
            recentTapTimes.Add(now);
            recentTapTimes.RemoveAll(t => now - t > TripleTapWindowSeconds);

            if (recentTapTimes.Count >= TripleTapCount)
            {
                recentTapTimes.Clear();
                TogglePanel();
            }
        }

        private void TogglePanel()
        {
            if (panelObject == null)
            {
                return;
            }

            SetPanelVisible(!panelObject.activeSelf);
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelObject == null)
            {
                return;
            }

            panelObject.SetActive(visible);
            EditorPrefs.SetBool(PanelOpenEditorPrefsKey, visible);
        }

        /// <summary>
        /// Opens the cheat panel programmatically (e.g. from the BrainDrain/Testing menu)
        /// without requiring the triple-tap trigger on the IQ text.
        /// </summary>
        public void ShowPanel() => SetPanelVisible(true);

        // ===================== Panel construction =====================

        private void BuildPanel(RectTransform canvasRect)
        {
            if (builtUI || canvasRect == null)
            {
                return;
            }

            builtUI = true;

            panelObject = new GameObject("DebugCheatPanel_UI", typeof(RectTransform));
            panelObject.transform.SetParent(canvasRect, false);
            panelObject.transform.SetAsLastSibling();

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(440f, 0f);
            panelRect.anchoredPosition = Vector2.zero;

            Image background = panelObject.AddComponent<Image>();
            background.color = BackgroundColor;

            // Hot pink border: a slightly larger sibling Image placed behind the background.
            GameObject borderObject = new GameObject("Border", typeof(RectTransform));
            borderObject.transform.SetParent(panelObject.transform, false);
            borderObject.transform.SetAsFirstSibling();
            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-4f, -4f);
            borderRect.offsetMax = new Vector2(4f, 4f);
            borderObject.AddComponent<Image>().color = HotPink;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 12, 12);
            layoutGroup.spacing = 6f;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            ContentSizeFitter sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateLabel(contentObject.transform, "DEBUG CHEATS (EDITOR ONLY)", 16f);

            CreateButton(contentObject.transform, "+10K BRAIN POWER", () => DebugCheats.AddBrainPower(10000d));
            CreateButton(contentObject.transform, "+100K BRAIN POWER", () => DebugCheats.AddBrainPower(100000d));
            CreateButton(contentObject.transform, "+1K CASH", () => DebugCheats.AddCash(1000d));
            CreateButton(contentObject.transform, "+500 POINTS", () => DebugCheats.AddPoints(500d));
            CreateButton(contentObject.transform, "MAX ALL BUILDINGS", DebugCheats.MaxAllBuildings);
            CreateButton(contentObject.transform, "FORCE REBIRTH", DebugCheats.ForceRebirth);
            CreateButton(contentObject.transform, "SET IQ TO 60", () => DebugCheats.SetPlayerIQ(60f));

            BuildWorldRestoreRow(contentObject.transform);

            CreateButton(contentObject.transform, "UNLOCK SNOTTING (50K)", () =>
            {
                DebugCheats.UnlockSnotting();
                FindAnyObjectByType<RebirthUIController>()?.RefreshTriggerButton();
            });

            CreateButton(contentObject.transform, "LOG CLICK BLOCKERS", LogClickBlockers);
            CreateButton(contentObject.transform, "CLEAR SAVE (FRESH START)", DebugCheats.ClearSave);
            CreateButton(contentObject.transform, "CLOSE", () => SetPanelVisible(false));
        }

        private void BuildWorldRestoreRow(Transform parent)
        {
            WorldRestorationManager worldRestoration = WorldRestorationManager.Instance;
            IReadOnlyList<WorldRestorationStage> stages = worldRestoration != null ? worldRestoration.Stages : null;
            if (stages == null || stages.Count == 0)
            {
                return;
            }

            CreateLabel(parent, "WORLD RESTORE STAGE", 13f);

            GameObject rowObject = new GameObject("WorldRestoreRow", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 4f;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;

            LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
            rowElement.minHeight = 44f;
            rowElement.preferredHeight = 44f;

            double snottingThreshold = FindAnyObjectByType<RebirthUIController>()?.SnottingUnlockThreshold ?? 50000d;

            for (int i = 0; i < stages.Count; i++)
            {
                double threshold = stages[i].pointsRequired;
                string label = threshold < 1d ? "0" : NumberFormatter.Format(threshold);
                if (System.Math.Abs(threshold - snottingThreshold) < 1d)
                {
                    label += "\nSNOTTING";
                }
                CreateButton(rowObject.transform, label, () => DebugCheats.JumpToWorldRestorationStage(threshold));
            }
        }

        private void LogClickBlockers()
        {
            RebirthUIController rebirthUI = FindAnyObjectByType<RebirthUIController>();
            if (rebirthUI == null)
            {
                Debug.LogWarning("[DebugCheatPanel] LogClickBlockers: No RebirthUIController found in scene.");
                return;
            }

            GameObject triggerBtn = rebirthUI.TriggerButtonObject;
            if (triggerBtn == null)
            {
                Debug.LogWarning("[DebugCheatPanel] LogClickBlockers: RebirthUIController.rebirthTriggerButton is null.");
                return;
            }

            RectTransform rt = triggerBtn.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogWarning("[DebugCheatPanel] LogClickBlockers: Snotting button has no RectTransform.");
                return;
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // Average bottom-left and top-right corners for center
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            Canvas canvas = triggerBtn.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

            PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[DebugCheatPanel] Click blockers at Snotting button screen pos {screenPos} (triggerActive={triggerBtn.activeSelf}):");
            if (results.Count == 0)
            {
                sb.AppendLine("  (no hits — nothing intercepting at that position)");
            }
            else
            {
                for (int i = 0; i < results.Count; i++)
                {
                    RaycastResult r = results[i];
                    sb.AppendLine($"  [{i}] \"{r.gameObject.name}\"  depth={r.depth}  dist={r.distance:F2}  sortOrder={r.sortingOrder}");
                }
            }
            Debug.Log(sb.ToString());
        }

        private static void CreateLabel(Transform parent, string text, float fontSize)
        {
            GameObject labelObject = new GameObject($"Label_{text}", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 8f;
            layout.preferredHeight = fontSize + 8f;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = HotPink;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = fontSize;
            label.raycastTarget = false;
        }

        private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject($"Btn_{label}", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonFill;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
            text.raycastTarget = false;

            return button;
        }
    }
}
#endif
