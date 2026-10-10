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

        /// <summary>True once the player taps the popup's own close button, for THIS pending
        /// recovery. View-only -- never touches RewardedAdRecoveryManager.HasPendingRecovery, so
        /// remaining ladder progress survives a close. Cleared automatically the moment a brand
        /// new pending recovery starts (see RefreshVisuals' false->true transition check below),
        /// so the popup still auto-opens for a fresh offline-decay event same as before this flag
        /// existed; otherwise cleared by ReopenPopup() (the new HUD button's entry point).</summary>
        private bool hiddenByPlayer;

        /// <summary>Tracks HasPendingRecovery across refreshes purely to detect the false->true
        /// transition that means "a new recovery just started" -- see hiddenByPlayer's own doc
        /// comment for why that transition matters.</summary>
        private bool lastKnownHasPendingRecovery;

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
            hiddenByPlayer = true;
            RefreshVisuals();
        }

        /// <summary>Called by the RECOVER IQ HUD button. Clears a prior player-initiated close
        /// and re-shows the popup -- opening/closing this view never touches
        /// HasPendingRecovery/AdsWatchedThisEvent, only an actual ad watch or a brand new
        /// offline-decay event does.</summary>
        public void ReopenPopup()
        {
            hiddenByPlayer = false;
            RefreshVisuals();

            RewardedAdRecoveryManager manager = RewardedAdRecoveryManager.Instance;
            bool hasPending = manager != null && manager.HasPendingRecovery;
            Debug.Log($"[RewardedAdRecoveryUIController] ReopenPopup: hasPending={hasPending}, suppressedByOtherPanel={suppressedByOtherPanel}, hiddenByPlayer={hiddenByPlayer} -> shown={hasPending && !suppressedByOtherPanel && !hiddenByPlayer}");
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
            bool hasPending = manager != null && manager.HasPendingRecovery;

            if (hasPending && !lastKnownHasPendingRecovery)
            {
                // false -> true transition: a brand new recovery just started (the only way
                // HasPendingRecovery becomes true is HandleOfflineDecayApplied), so clear any
                // earlier close from a previous event -- this event auto-opens, same as always.
                hiddenByPlayer = false;
            }
            lastKnownHasPendingRecovery = hasPending;

            if (manager == null || !hasPending || suppressedByOtherPanel || hiddenByPlayer)
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

                // 2026-10-11 play-test fix: scene-authored default (RewardedAdRecoveryWireFix)
                // was a 12-18pt floor/ceiling in flat grey -- set here too so the fix holds
                // regardless of scene state, same code-owns-presentation pattern as
                // RebirthUIController.UpdateVisuals().
                progressText.color = Palette.Glow;
                progressText.fontSizeMin = 24f;
                if (progressText.fontSizeMax < 24f) { progressText.fontSizeMax = 24f; }
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
