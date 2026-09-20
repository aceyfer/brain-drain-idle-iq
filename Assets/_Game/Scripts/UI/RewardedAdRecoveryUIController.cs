using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// §57 rewarded-ad idle-window recovery popup. Mirrors RandomEventUIController's CanvasGroup
    /// show/hide pattern exactly, including its GetComponentInParent&lt;Canvas/CanvasGroup&gt;
    /// fallback -- reacts only to RewardedAdRecoveryManager's own exposed state, never touches
    /// PlayerIQManager or any other Core system directly. IMPORTANT WIRING REQUIREMENT (see the
    /// §63 investigation of this exact fallback on RandomEventUIController/ChaosPopUpCanvas):
    /// this component's host GameObject must have its OWN local Canvas + CanvasGroup, the same
    /// way ChaosPopUpCanvas does, so the fallback resolves to this popup's own components and
    /// never climbs to the root Canvas. Skipping that when wiring this into the scene would risk
    /// the same class of bug that investigation ruled out only because that precondition held.
    ///
    /// Meant to appear alongside (not replace) DialogueManager's existing "welcome back" narrator
    /// line -- both react independently to the same PlayerIQManager.OnOfflineDecayApplied event,
    /// this one indirectly via RewardedAdRecoveryManager rather than subscribing to Core itself.
    /// </summary>
    public sealed class RewardedAdRecoveryUIController : MonoBehaviour
    {
        [Header("UI Panel")]
        [SerializeField] private GameObject recoveryPopupPanel;

        [Header("Visual Fields")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button closeButton;

        private RewardedAdRecoveryManager subscribedManager;

        /// <summary>
        /// 2026-09-16: set by MainUIController while Shop/Convert/Settings is open, so this
        /// popup -- which shows itself automatically off RewardedAdRecoveryManager's own event,
        /// with no awareness of what else is on screen -- stops visually stacking on top of
        /// whichever of those three the player just opened. Suppresses the VIEW only; never
        /// touches RewardedAdRecoveryManager.HasPendingRecovery, so the pending recovery (and
        /// ads-watched progress) is untouched and the popup reappears on its own the moment
        /// MainUIController reports none of Shop/Convert/Settings are open anymore.
        /// </summary>
        private bool suppressedByOtherPanel;

        private void Awake()
        {
            if (watchAdButton != null)
            {
                watchAdButton.onClick.AddListener(OnWatchAdClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            // Initialize canvas state to false on startup (inactive/click-through), same
            // convention as RandomEventUIController.
            SetCanvasState(false);
        }

        private void Start()
        {
            SubscribeToEvents();
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            RewardedAdRecoveryManager manager = RewardedAdRecoveryManager.Instance;
            if (manager == null)
            {
                return;
            }

            subscribedManager = manager;
            subscribedManager.OnRecoveryStateChanged -= HandleRecoveryStateChanged;
            subscribedManager.OnRecoveryStateChanged += HandleRecoveryStateChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnRecoveryStateChanged -= HandleRecoveryStateChanged;
            subscribedManager = null;
        }

        private void HandleRecoveryStateChanged()
        {
            RefreshVisuals();
        }

        private void OnWatchAdClicked()
        {
            RewardedAdRecoveryManager.Instance?.RequestAdWatch();
        }

        private void OnCloseClicked()
        {
            RewardedAdRecoveryManager.Instance?.DismissPendingRecovery();
        }

        /// <summary>Called by MainUIController -- see suppressedByOtherPanel's doc comment.</summary>
        public void SetSuppressedByOtherPanel(bool suppressed)
        {
            if (suppressedByOtherPanel == suppressed)
            {
                return;
            }

            suppressedByOtherPanel = suppressed;
            RefreshVisuals();
        }

        /// <summary>Single source of truth for both visibility and content -- called on every state change rather than splitting "show" and "update text" into separate paths, so there is only one place that can drift out of sync with the manager's real state.</summary>
        private void RefreshVisuals()
        {
            RewardedAdRecoveryManager manager = RewardedAdRecoveryManager.Instance;
            if (manager == null || !manager.HasPendingRecovery || suppressedByOtherPanel)
            {
                SetCanvasState(false);
                return;
            }

            SetCanvasState(true);

            if (titleText != null)
            {
                titleText.text = $"COGS docked you {manager.PendingAmountLost:F0} IQ while you were gone.\nWatch an ad to get some back.";
            }

            if (progressText != null)
            {
                progressText.text = $"{manager.AdsWatchedThisEvent}/{manager.MaxAdsForEvent} ads watched";
            }

            if (watchAdButton != null)
            {
                watchAdButton.interactable = manager.AdsWatchedThisEvent < manager.MaxAdsForEvent;
            }
        }

        private void SetCanvasState(bool active)
        {
            if (recoveryPopupPanel != null)
            {
                recoveryPopupPanel.SetActive(active);
            }

            Canvas parentCanvas = GetComponent<Canvas>();
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            if (parentCanvas != null)
            {
                parentCanvas.enabled = active;
            }

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInParent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
        }
    }
}
