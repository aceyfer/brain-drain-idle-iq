using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;
using BrainDrain.Core;

namespace BrainDrain.UI
{
    public sealed class RebirthUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject rebirthModalPanel;

        [Header("Visual Fields")]
        [SerializeField] private TextMeshProUGUI multiplierText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Visibility Gate")]
        [Tooltip("The 'REBIRTH' button GameObject in the HUD that opens this modal. Hidden until pointsSpentUnlockThreshold is reached, then stays visible permanently (CumulativePointsSpentOnRestoration only ever increases).")]
        [SerializeField] private GameObject rebirthTriggerButton;
        [Tooltip("Cumulative Points spent on World Restoration required before the REBIRTH button appears. Deliberately high (50,000, up from an earlier 1,000) so a player can't reach Rebirth within their first day -- see CLAUDE.md's 2026-06-21 rebalance notes.")]
        [SerializeField] private double pointsSpentUnlockThreshold = 50000d;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

            // Hide immediately, before the real threshold check in Start(), so there's no
            // single-frame flash of the button prior to WorldRestorationManager being ready.
            if (rebirthTriggerButton != null)
            {
                rebirthTriggerButton.SetActive(false);
            }
        }

        private void Start()
        {
            ApplyTriggerButtonVisibility();

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationProgressChanged += HandleRestorationProgressChanged;
            }
        }

        private void OnDestroy()
        {
            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
            }
        }

        private void HandleRestorationProgressChanged(double _)
        {
            ApplyTriggerButtonVisibility();
        }

        /// <summary>Reveals the REBIRTH trigger button once enough Points have been spent on World Restoration. Never re-hides it, since that progress is monotonic.</summary>
        private void ApplyTriggerButtonVisibility()
        {
            if (rebirthTriggerButton == null)
            {
                return;
            }

            double spent = WorldRestorationManager.Instance != null
                ? WorldRestorationManager.Instance.CumulativePointsSpentOnRestoration
                : 0d;
            bool shouldBeVisible = spent >= pointsSpentUnlockThreshold;

            if (rebirthTriggerButton.activeSelf != shouldBeVisible)
            {
                rebirthTriggerButton.SetActive(shouldBeVisible);
            }
        }

        public void OpenModal()
        {
            if (rebirthModalPanel != null)
            {
                rebirthModalPanel.SetActive(true);
                UpdateVisuals();

                RectTransform panelRect = rebirthModalPanel.GetComponent<RectTransform>();
                CanvasGroup panelCanvasGroup = rebirthModalPanel.GetComponent<CanvasGroup>();
                AnimationController.PlayPopupSpawn(panelRect, panelCanvasGroup);
            }
        }

        public void CloseModal()
        {
            if (rebirthModalPanel != null)
            {
                rebirthModalPanel.SetActive(false);
            }
        }

        private void UpdateVisuals()
        {
            if (multiplierText != null && RebirthManager.Instance != null)
            {
                double pending = RebirthManager.Instance.PendingMultiplierIncrease;
                int nextTier = RebirthManager.Instance.RebirthCount + 1;
                multiplierText.text = $"+{NumberFormatter.Format(pending)}x MULTIPLIER\nBECOME {RebirthManager.GetIllumisnottiTitle(nextTier).ToUpper()}";
            }
        }

        private void OnConfirmClicked()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.TriggerRebirth();
            }
            CloseModal();
        }

        private void OnCancelClicked()
        {
            CloseModal();
        }
    }
}
