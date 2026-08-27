using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

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

        private static readonly Color CurrentTrackColor = new(0f, 0.941f, 1f, 1f); // cyan, matches the project's existing #00F0FF accent
        private static readonly Color OtherTrackColor = Color.white;

        private void Awake()
        {
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

            // Hidden by default -- deliberately after wiring, not via a scene-authored inactive
            // GameObject (see class doc comment).
            gameObject.SetActive(false);
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
