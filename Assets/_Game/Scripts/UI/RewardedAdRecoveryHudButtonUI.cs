using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// §57: persistent HUD reopen button for the rewarded-ad recovery ladder. Fully code-built
    /// and self-bootstrapping -- no prefab, no scene wiring (PocketPanelUI/TimedPurchaseWalletUI
    /// precedent, Bible §8's "own it in code"). Anchors two slots below THE WALLET's own button
    /// (three below Dia-Log/LogOpenButton -- slot 1 is Pocket, slot 2 is Wallet), computed
    /// independently from LogOpenButton's own rect, same reasoning as those two: Start() order
    /// between this class and theirs is not guaranteed.
    ///
    /// Exists because RewardedAdRecoveryUIController's popup only ever opens itself automatically
    /// off PlayerIQManager.OnOfflineDecayApplied -- once the player closes it there was previously
    /// no way back to the remaining ads on the ladder (see RewardedAdRecoveryUIController's own
    /// hiddenByPlayer/ReopenPopup doc comments for the companion fix). This button is that way
    /// back: visible only while a recovery is still pending and the ladder isn't maxed, tapping it
    /// calls the existing popup's ReopenPopup() rather than building a second popup.
    /// </summary>
    public sealed class RewardedAdRecoveryHudButtonUI : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const string DiaLogButtonName = "LogOpenButton";
        private const float ButtonGap = 12f;
        private const int SlotIndex = 3; // 1 = Pocket, 2 = Wallet, 3 = this button

        // 2026-10-04 PALETTE LOCKDOWN: was gold -- now Cyan, same 0.22 alpha as the other
        // bottom-row open buttons (Pocket/Wallet).
        private static readonly Color ButtonFillColor = new Color(Palette.Cyan.r, Palette.Cyan.g, Palette.Cyan.b, 0.22f);

        private static RewardedAdRecoveryHudButtonUI instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing
        /// placed one in the scene (matches TimedPurchaseWalletUI/PocketPanelUI -- nothing else
        /// calls into this class, so without this the button would silently never build).</summary>
        public static RewardedAdRecoveryHudButtonUI Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<RewardedAdRecoveryHudButtonUI>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("RewardedAdRecoveryHudButtonUI");
                    instance = hostObject.AddComponent<RewardedAdRecoveryHudButtonUI>();
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private GameObject buttonObject;
        private TextMeshProUGUI label;
        private RewardedAdRecoveryUIController popupController;
        private RewardedAdRecoveryManager subscribedManager;
        private bool built;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent != null)
            {
                transform.SetParent(systemsParent.transform, false);
            }
        }

        private void Start()
        {
            Build();
            SubscribeToEvents();
            RefreshVisuals();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void SubscribeToEvents()
        {
            RewardedAdRecoveryManager manager = RewardedAdRecoveryManager.Instance;
            if (manager == null)
            {
                return;
            }

            subscribedManager = manager;
            subscribedManager.OnRecoveryStateChanged -= RefreshVisuals;
            subscribedManager.OnRecoveryStateChanged += RefreshVisuals;
        }

        private void UnsubscribeFromEvents()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnRecoveryStateChanged -= RefreshVisuals;
            subscribedManager = null;
        }

        /// <summary>
        /// Locates the scene's Dia-Log button the same way PocketPanelUI/TimedPurchaseWalletUI do,
        /// purely to find the HUD Canvas to parent into and anchor this button one slot beneath
        /// Wallet's. Bails gracefully if the button isn't present -- this is non-critical and never
        /// blocks boot.
        /// </summary>
        private void Build()
        {
            if (built)
            {
                return;
            }

            GameObject diaLogButton = GameObject.Find(DiaLogButtonName);
            RectTransform diaLogRect = diaLogButton != null ? diaLogButton.GetComponent<RectTransform>() : null;
            if (diaLogRect == null)
            {
                Debug.LogWarning("[RewardedAdRecoveryHudButtonUI] Dia-Log button (LogOpenButton) not found; RECOVER IQ button not built.", this);
                return;
            }

            RectTransform canvasRect = diaLogRect.parent as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogWarning("[RewardedAdRecoveryHudButtonUI] Dia-Log button's parent is not a RectTransform; RECOVER IQ button not built.", this);
                return;
            }

            buttonObject = new GameObject("RecoverIQButton", typeof(RectTransform));
            buttonObject.transform.SetParent(canvasRect, false);
            buttonObject.transform.SetAsLastSibling();

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = diaLogRect.anchorMin;
            rect.anchorMax = diaLogRect.anchorMax;
            rect.pivot = diaLogRect.pivot;
            rect.sizeDelta = diaLogRect.sizeDelta;
            rect.anchoredPosition = diaLogRect.anchoredPosition + new Vector2(0f, -SlotIndex * (diaLogRect.sizeDelta.y + ButtonGap));

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonFillColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            label = labelObject.AddComponent<TextMeshProUGUI>();
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            // All four side buttons copy LogOpenButton's 140x50 rect. Give this
            // two-line label its own compact range rather than a 20pt hard floor.
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 20f;
            label.lineSpacing = -20f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            // 2026-10-05 ART PASS 2: was UniversalButtonBorderApplier.Instance?.ApplyToButton --
            // this button now owns its own static Alert_Frame look instead (see
            // AlertFrameButtonStyle's doc comment for why it's excluded from that system). Moved
            // here (after the label exists) since Apply() colors the label too.
            AlertFrameButtonStyle.Apply(button);

            built = true;
        }

        private void OnClicked()
        {
            if (popupController == null)
            {
                popupController = FindAnyObjectByType<RewardedAdRecoveryUIController>();
            }

            popupController?.ReopenPopup();
        }

        /// <summary>Single source of truth for both visibility and label text -- called on every
        /// recovery-state change rather than splitting "show" and "update text" into separate
        /// paths, same reasoning as RewardedAdRecoveryUIController.RefreshVisuals.</summary>
        private void RefreshVisuals()
        {
            if (!built || buttonObject == null)
            {
                return;
            }

            RewardedAdRecoveryManager manager = RewardedAdRecoveryManager.Instance;
            bool visible = manager != null
                && manager.HasPendingRecovery
                && manager.AdsWatchedThisEvent < manager.MaxAdsForEvent;

            buttonObject.SetActive(visible);

            if (visible && label != null)
            {
                label.text = $"RECOVER IQ\n{manager.AdsWatchedThisEvent}/{manager.MaxAdsForEvent}";
            }
        }
    }
}
