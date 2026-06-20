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

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
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
                multiplierText.text = $"+{NumberFormatter.Format(pending)}x MULTIPLIER";
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
