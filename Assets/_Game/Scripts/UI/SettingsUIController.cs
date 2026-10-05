using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;
using BrainDrain.Systems.Commerce;

namespace BrainDrain.UI
{
    /// <summary>
    /// Manages the Settings modal/panel: a mute toggle and a picker for which
    /// BackgroundMusicManager track plays. Lives on the panel GameObject itself, same pattern
    /// as ShopUIController -- the panel must start ACTIVE in the scene so Awake() actually gets
    /// to run and wire the buttons' onClick listeners (Unity only calls Awake on objects active
    /// at load), then this deactivates itself at the end of Awake(). A scene-authored inactive
    /// panel would silently break every button on it forever (the exact bug §36-adjacent work
    /// fixed for ShopPanel).
    /// </summary>
    public sealed class SettingsUIController : MonoBehaviour
    {
        [Header("UI Panel")]
        [Tooltip("Self-reference to this GameObject, same idiom as ShopUIController.shopPanel.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Mute")]
        [SerializeField] private Toggle muteToggle;

        [Header("Track Rows (index-aligned with BackgroundMusicManager.availableTracks)")]
        [SerializeField] private Button[] trackButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] trackLabels = new TextMeshProUGUI[4];

        [Header("Close")]
        [SerializeField] private Button closeButton;

        private static readonly string[] TrackNames =
        {
            "Find and Seek",
            "Gutters Filled with Light",
            "Intrusion Detected",
            "T.SUM-12",
        };

        // 2026-10-05 ART PASS 2: was a near-cyan guess (#00F0FF) -- normalized to the exact token.
        private static readonly Color CurrentTrackColor = Palette.Cyan;
        private static readonly Color OtherTrackColor = Color.white;

        // IAP RULES item 2: Bad Words Pack must be restorable even when the automatic boot-time
        // fetch misses it (iOS/edge cases). Built in code, not scene-wired, same "own it in code"
        // pattern as every other runtime-built button in this project.
        private TextMeshProUGUI restoreButtonLabel;
        private IapCommerceService subscribedCommerceService;

        private void Awake()
        {
            // 2026-10-05 ART PASS 2 (item 3, consistency sweep): settingsPanel's own background
            // was still a flat scene-baked fill with no border -- same Surface-fill-plus-Glow-
            // outline treatment DialogueDisplayUI already applies to COGS_Narrator_Panel, so the
            // two modal-style panels read as the same family instead of Settings looking flatter.
            if (settingsPanel != null)
            {
                Image panelImage = settingsPanel.GetComponent<Image>();
                if (panelImage != null) { panelImage.color = Palette.Surface; }

                Outline panelOutline = settingsPanel.GetComponent<Outline>();
                if (panelOutline == null) { panelOutline = settingsPanel.AddComponent<Outline>(); }
                panelOutline.effectColor = Palette.Glow;
                panelOutline.effectDistance = new Vector2(3f, -3f);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener(HandleMuteToggled);
            }

            for (int i = 0; i < trackButtons.Length; i++)
            {
                if (trackButtons[i] == null)
                {
                    continue;
                }

                int index = i; // capture per-iteration value, not the loop variable
                trackButtons[i].onClick.AddListener(() => HandleTrackSelected(index));
            }

            BuildRestoreButton();

            // Hidden by default -- deliberately after wiring, not via a scene-authored inactive
            // GameObject (see class doc comment).
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (subscribedCommerceService != null)
            {
                subscribedCommerceService.OnRestoreCompleted -= HandleRestoreCompleted;
                subscribedCommerceService = null;
            }
        }

        /// <summary>
        /// IAP RULES item 2: code-built, same "own it in code" pattern as every other runtime
        /// button in this project -- no scene write. Anchored bottom-center of the panel.
        /// Restoring is a real store round-trip (FetchPurchases), so the label gives feedback
        /// rather than looking like a dead click.
        /// </summary>
        private void BuildRestoreButton()
        {
            if (settingsPanel == null) { return; }

            GameObject buttonObject = new GameObject("RestorePurchasesButton", typeof(RectTransform));
            buttonObject.transform.SetParent(settingsPanel.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(360f, 64f);
            rect.anchoredPosition = new Vector2(0f, 24f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(HandleRestoreClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            restoreButtonLabel = labelObject.AddComponent<TextMeshProUGUI>();
            restoreButtonLabel.text = "RESTORE PURCHASES";
            restoreButtonLabel.fontStyle = FontStyles.Bold;
            restoreButtonLabel.alignment = TextAlignmentOptions.Center;
            restoreButtonLabel.fontSize = 22f;
            restoreButtonLabel.enableAutoSizing = true;
            restoreButtonLabel.fontSizeMin = 16f;
            restoreButtonLabel.fontSizeMax = 22f;
            restoreButtonLabel.raycastTarget = false;

            // Same static Alert_Frame look as the other side panels/buttons from the art passes
            // (Base fill + Glow border baked into the sprite, Cyan label) -- excluded from
            // UniversalButtonBorderApplier by name (AlertFrameButtonStyle.ManagedButtonNames) so
            // a later World Restoration stage change can't re-theme it back toward the ornate
            // ButtonBorder_Stage art.
            AlertFrameButtonStyle.Apply(button);
        }

        private void HandleRestoreClicked()
        {
            IapCommerceService commerce = IapCommerceService.Instance;
            if (commerce == null)
            {
                return;
            }

            if (commerce != subscribedCommerceService)
            {
                if (subscribedCommerceService != null)
                {
                    subscribedCommerceService.OnRestoreCompleted -= HandleRestoreCompleted;
                }

                subscribedCommerceService = commerce;
                subscribedCommerceService.OnRestoreCompleted += HandleRestoreCompleted;
            }

            if (restoreButtonLabel != null) { restoreButtonLabel.text = "RESTORING…"; }
            commerce.RestorePurchases();
        }

        private void HandleRestoreCompleted(bool success)
        {
            if (restoreButtonLabel == null) { return; }
            restoreButtonLabel.text = success ? "RESTORED" : "RESTORE FAILED — TRY AGAIN";
        }

        public void OpenPanel()
        {
            if (settingsPanel == null)
            {
                return;
            }

            settingsPanel.SetActive(true);
            RefreshVisuals();

            RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
            CanvasGroup panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelRect != null)
            {
                AnimationController.PlayPopupSpawn(panelRect, panelCanvasGroup);
            }
        }

        public void ClosePanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        /// <summary>Whether the settings panel is currently visible.</summary>
        public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

        /// <summary>Toggles the settings panel open or closed.</summary>
        public void TogglePanel()
        {
            if (IsOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        private void HandleMuteToggled(bool isOn)
        {
            BackgroundMusicManager.Instance?.SetMuted(isOn);
        }

        private void HandleTrackSelected(int index)
        {
            BackgroundMusicManager.Instance?.SelectTrack(index);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            // Reset any stale "RESTORING…"/"RESTORED" feedback from a prior session with this
            // panel open -- a fresh open always starts from the same label.
            if (restoreButtonLabel != null) { restoreButtonLabel.text = "RESTORE PURCHASES"; }

            BackgroundMusicManager musicManager = BackgroundMusicManager.Instance;

            if (muteToggle != null && musicManager != null)
            {
                // SetIsOnWithoutNotify avoids re-firing HandleMuteToggled from this programmatic refresh.
                muteToggle.SetIsOnWithoutNotify(musicManager.IsMuted);
            }

            int currentIndex = musicManager != null ? musicManager.CurrentTrackIndex : -1;
            for (int i = 0; i < trackLabels.Length; i++)
            {
                if (trackLabels[i] == null)
                {
                    continue;
                }

                string name = i < TrackNames.Length ? TrackNames[i] : $"Track {i + 1}";
                bool isCurrent = i == currentIndex;
                trackLabels[i].text = isCurrent ? $"{name} (PLAYING)" : name;
                trackLabels[i].color = isCurrent ? CurrentTrackColor : OtherTrackColor;
            }
        }
    }
}
