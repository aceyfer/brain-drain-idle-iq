using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// GTA-style scrollable history of narrator dialogue lines (SS20b), backed by
    /// DialogueManager.History/OnHistoryChanged. Opened via openButton, closed via
    /// closeButton or by tapping openButton again while open.
    /// </summary>
    public sealed class DialogueLogPanelUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private CanvasGroup panelGroup;
        private DialogueManager subscribedDialogueManager;
        private bool isVisible;

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
                subscribedDialogueManager.OnHistoryChanged += HandleHistoryChanged;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from the exact instance Awake subscribed to, not a fresh lookup --
            // matches the §19-4a convention (91b8fde): never FindAnyObjectByType in teardown.
            if (subscribedDialogueManager != null)
            {
                subscribedDialogueManager.OnHistoryChanged -= HandleHistoryChanged;
                subscribedDialogueManager = null;
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

        /// <summary>Single owner of the panel's hidden/shown state (code-owned presentation
        /// state, Bible §8). Alpha + raycast gating, never GameObject SetActive.</summary>
        private void SetPanelHidden(bool hidden)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = hidden ? 0f : 1f;
            panelGroup.blocksRaycasts = !hidden;
            panelGroup.interactable = !hidden;
        }

        private void HandleHistoryChanged()
        {
            if (!isVisible)
            {
                return;
            }

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

        private static string FormatTimestamp(float sessionTime)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(sessionTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
