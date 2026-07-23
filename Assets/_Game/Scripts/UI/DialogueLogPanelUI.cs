using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// GTA-style scrollable history of narrator dialogue lines (SS20b) plus pedestrian street
    /// chatter (§24b), backed by DialogueManager.History/OnHistoryChanged and
    /// RandomChatterManager.History/OnHistoryChanged respectively. Two code-built tabs (COGS /
    /// STREET, IntelCardUI's code-built-UI precedent -- Bible §8, no prefab/scene changes) switch
    /// which history renders in the shared log text/scroll view. Opened via openButton, closed
    /// via closeButton or by tapping openButton again while open.
    /// </summary>
    public sealed class DialogueLogPanelUI : MonoBehaviour
    {
        private enum LogTab
        {
            Cogs,
            Street
        }

        private const float TabBarHeight = 80f;
        private static readonly Color ActiveTabColor = new Color(0f, 0.94f, 1f, 0.35f);
        private static readonly Color InactiveTabColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color ActiveTabTextColor = Color.white;
        private static readonly Color InactiveTabTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Panel")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private CanvasGroup panelGroup;
        private DialogueManager subscribedDialogueManager;
        private RandomChatterManager subscribedChatterManager;
        private bool isVisible;
        private LogTab activeTab = LogTab.Cogs;

        private Image cogsTabImage;
        private Image streetTabImage;
        private TextMeshProUGUI cogsTabLabel;
        private TextMeshProUGUI streetTabLabel;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelGroup = panelRoot.GetComponent<CanvasGroup>();
                if (panelGroup == null)
                {
                    panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
                }
                SetPanelHidden(true);
            }

            // Unity's default Scroll View ships opaque light-gray Images (on the ScrollRect's
            // own GameObject and its Viewport child) that cover the panel's dark chip laid down
            // in the §20b layout tune, washing the log out (§24a, found 2026-07-22). Code-owned
            // per Bible §8 so it can't regress via an Editor reset: kill only the visual (alpha
            // 0), leave the Viewport's Mask/raycast function untouched.
            if (scrollRect != null)
            {
                Image scrollRectImage = scrollRect.GetComponent<Image>();
                if (scrollRectImage != null)
                {
                    Color scrollRectColor = scrollRectImage.color;
                    scrollRectColor.a = 0f;
                    scrollRectImage.color = scrollRectColor;
                }

                if (scrollRect.viewport != null)
                {
                    Image viewportImage = scrollRect.viewport.GetComponent<Image>();
                    if (viewportImage != null)
                    {
                        Color viewportColor = viewportImage.color;
                        viewportColor.a = 0f;
                        viewportImage.color = viewportColor;
                    }
                }
            }

            BuildTabBar();

            if (openButton != null)
            {
                openButton.onClick.RemoveListener(ToggleOpen);
                openButton.onClick.AddListener(ToggleOpen);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (DialogueManager.Instance != null)
            {
                subscribedDialogueManager = DialogueManager.Instance;
                subscribedDialogueManager.OnHistoryChanged += HandleDialogueHistoryChanged;
            }

            if (RandomChatterManager.Instance != null)
            {
                subscribedChatterManager = RandomChatterManager.Instance;
                subscribedChatterManager.OnHistoryChanged += HandleChatterHistoryChanged;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from the exact instances Awake subscribed to, not a fresh lookup --
            // matches the §19-4a convention (91b8fde): never FindAnyObjectByType in teardown.
            if (subscribedDialogueManager != null)
            {
                subscribedDialogueManager.OnHistoryChanged -= HandleDialogueHistoryChanged;
                subscribedDialogueManager = null;
            }

            if (subscribedChatterManager != null)
            {
                subscribedChatterManager.OnHistoryChanged -= HandleChatterHistoryChanged;
                subscribedChatterManager = null;
            }
        }

        private void ToggleOpen()
        {
            if (isVisible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (panelRoot == null) return;

            activeTab = LogTab.Cogs;
            UpdateTabVisuals();
            Rebuild();
            SetPanelHidden(false);
            isVisible = true;

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void Close()
        {
            if (panelRoot == null) return;

            SetPanelHidden(true);
            isVisible = false;
        }

        private void SelectTab(LogTab tab)
        {
            if (activeTab == tab)
            {
                return;
            }

            activeTab = tab;
            UpdateTabVisuals();
            Rebuild();

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>Single owner of the panel's hidden/shown state (code-owned presentation
        /// state, Bible §8). Alpha + raycast gating, never GameObject SetActive.</summary>
        private void SetPanelHidden(bool hidden)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = hidden ? 0f : 1f;
            panelGroup.blocksRaycasts = !hidden;
            panelGroup.interactable = !hidden;
        }

        private void HandleDialogueHistoryChanged()
        {
            if (!isVisible || activeTab != LogTab.Cogs)
            {
                return;
            }

            RebuildWithRepin();
        }

        private void HandleChatterHistoryChanged()
        {
            if (!isVisible || activeTab != LogTab.Street)
            {
                return;
            }

            RebuildWithRepin();
        }

        /// <summary>Shared near-bottom re-pin rule for both tabs: if the reader was already at
        /// the bottom before a new line arrived, snap back to the bottom after rebuilding so new
        /// lines stay visible; otherwise leave their scroll position alone.</summary>
        private void RebuildWithRepin()
        {
            bool wasNearBottom = scrollRect == null || scrollRect.verticalNormalizedPosition < 0.05f;

            Rebuild();

            if (wasNearBottom && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void Rebuild()
        {
            if (logText == null)
            {
                return;
            }

            if (activeTab == LogTab.Cogs)
            {
                RebuildCogs();
            }
            else
            {
                RebuildStreet();
            }
        }

        private void RebuildCogs()
        {
            IReadOnlyList<DialogueManager.DialogueLogEntry> history = DialogueManager.Instance != null
                ? DialogueManager.Instance.History
                : null;

            if (history == null || history.Count == 0)
            {
                logText.text = "<color=#888888>NO TRANSMISSIONS LOGGED.</color>";
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < history.Count; i++)
            {
                DialogueManager.DialogueLogEntry entry = history[i];
                if (i > 0)
                {
                    sb.AppendLine();
                }
                sb.Append("<color=#888888>[").Append(FormatTimestamp(entry.SessionTime)).Append("]</color> ").Append(entry.Text);
            }

            logText.text = sb.ToString();
        }

        private void RebuildStreet()
        {
            IReadOnlyList<RandomChatterManager.ChatterLogEntry> history = RandomChatterManager.Instance != null
                ? RandomChatterManager.Instance.History
                : null;

            if (history == null || history.Count == 0)
            {
                logText.text = "<color=#888888>THE STREET IS QUIET.</color>";
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < history.Count; i++)
            {
                RandomChatterManager.ChatterLogEntry entry = history[i];
                if (i > 0)
                {
                    sb.AppendLine();
                }
                sb.Append("<color=#888888>[").Append(FormatTimestamp(entry.SessionTime)).Append("]</color> ").Append(entry.Text);
            }

            logText.text = sb.ToString();
        }

        private static string FormatTimestamp(float sessionTime)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(sessionTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        // ===================== Tab bar construction (§24b) =====================
        // Built entirely from code, parented into the existing panelRoot -- IntelCardUI's
        // code-built-UI precedent (Bible §8) -- so this ships with zero scene/prefab edits.

        private void BuildTabBar()
        {
            if (panelRoot == null)
            {
                return;
            }

            GameObject tabBarObject = new GameObject("LogTabBar", typeof(RectTransform));
            tabBarObject.transform.SetParent(panelRoot, false);
            tabBarObject.transform.SetAsLastSibling();

            RectTransform tabBarRect = tabBarObject.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = Vector2.zero;
            tabBarRect.sizeDelta = new Vector2(0f, TabBarHeight);

            HorizontalLayoutGroup layout = tabBarObject.AddComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            cogsTabImage = CreateTabButton(tabBarObject.transform, "COGS", () => SelectTab(LogTab.Cogs), out cogsTabLabel);
            streetTabImage = CreateTabButton(tabBarObject.transform, "STREET", () => SelectTab(LogTab.Street), out streetTabLabel);

            // Shrink the ScrollRect's own rect to make room for the tab bar above it, entirely
            // via code (no scene edit) -- pulling offsetMax down works regardless of the
            // ScrollRect's original stretch anchors/margins, unlike touching sizeDelta directly.
            if (scrollRect != null)
            {
                RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
                if (scrollRectTransform != null)
                {
                    Vector2 offsetMax = scrollRectTransform.offsetMax;
                    scrollRectTransform.offsetMax = new Vector2(offsetMax.x, offsetMax.y - TabBarHeight);
                }
            }

            UpdateTabVisuals();
        }

        private static Image CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI labelText)
        {
            GameObject buttonObject = new GameObject($"{label}TabButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();

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
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 28f;
            text.raycastTarget = false;

            labelText = text;
            return image;
        }

        private void UpdateTabVisuals()
        {
            bool cogsActive = activeTab == LogTab.Cogs;

            if (cogsTabImage != null)
            {
                cogsTabImage.color = cogsActive ? ActiveTabColor : InactiveTabColor;
            }

            if (streetTabImage != null)
            {
                streetTabImage.color = cogsActive ? InactiveTabColor : ActiveTabColor;
            }

            if (cogsTabLabel != null)
            {
                cogsTabLabel.color = cogsActive ? ActiveTabTextColor : InactiveTabTextColor;
            }

            if (streetTabLabel != null)
            {
                streetTabLabel.color = cogsActive ? InactiveTabTextColor : ActiveTabTextColor;
            }
        }
    }
}
