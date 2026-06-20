using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays the satirical random-event pop-up modal and routes the player's choice back
    /// into RandomEventManager. Includes the "fake close button" dark-pattern gimmick: the
    /// first click dodges the button to a random nearby spot; only the second click actually
    /// dismisses the event for free.
    /// </summary>
    public sealed class RandomEventUIController : MonoBehaviour
    {
        private const float DodgeOffsetRangeX = 50f;
        private const float DodgeOffsetRangeY = 30f;

        [Header("UI Panels")]
        [SerializeField] private GameObject eventPopupPanel;

        [Header("Visual Fields")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI actionButtonText;
        [SerializeField] private TextMeshProUGUI niceTryText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Button fakeCloseButton;

        private RectTransform fakeCloseButtonRect;
        private Vector2 fakeCloseButtonOriginalPosition;
        private bool fakeCloseHasDodged;
        private BrainRotEventData activeEventData;

        private void Awake()
        {
            if (fakeCloseButton != null)
            {
                fakeCloseButtonRect = fakeCloseButton.GetComponent<RectTransform>();
                if (fakeCloseButtonRect != null)
                {
                    fakeCloseButtonOriginalPosition = fakeCloseButtonRect.anchoredPosition;
                }
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }

            if (fakeCloseButton != null)
            {
                fakeCloseButton.onClick.AddListener(OnFakeCloseButtonClicked);
            }

            // Initialize canvas state to false on startup (inactive/click-through)
            SetCanvasState(false);
        }

        private void SetCanvasState(bool active)
        {
            if (eventPopupPanel != null)
            {
                eventPopupPanel.SetActive(active);
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

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (RandomEventManager.Instance == null)
            {
                Debug.LogWarning("[RandomEventUIController] RandomEventManager.Instance is null; cannot subscribe.", this);
                return;
            }

            RandomEventManager.Instance.OnRandomEventTriggered -= HandleRandomEventTriggered;
            RandomEventManager.Instance.OnRandomEventTriggered += HandleRandomEventTriggered;
        }

        private void UnsubscribeFromEvents()
        {
            if (RandomEventManager.Instance == null)
            {
                return;
            }

            RandomEventManager.Instance.OnRandomEventTriggered -= HandleRandomEventTriggered;
        }

        private void HandleRandomEventTriggered(BrainRotEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            activeEventData = eventData;
            ResetFakeCloseButton();

            if (titleText != null)
            {
                titleText.text = eventData.eventTitle;
            }

            if (descriptionText != null)
            {
                descriptionText.text = eventData.eventDescription;
            }

            if (actionButtonText != null)
            {
                actionButtonText.text = eventData.choiceButtonText;
            }

            SetCanvasState(true);

            if (eventPopupPanel != null)
            {
                RectTransform panelRect = eventPopupPanel.GetComponent<RectTransform>();
                CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = GetComponentInParent<CanvasGroup>();
                }

                AnimationController.PlayPopupSpawn(panelRect, canvasGroup);
            }
        }

        /// <summary>
        /// Fake 'X' button: the first click dodges to a random nearby position and reveals
        /// a "Nice try!" sub-text instead of closing. Only the second click actually closes.
        /// </summary>
        public void OnFakeCloseButtonClicked()
        {
            if (!fakeCloseHasDodged)
            {
                fakeCloseHasDodged = true;
                DodgeFakeCloseButton();

                if (niceTryText != null)
                {
                    niceTryText.text = "Nice try!";
                    niceTryText.gameObject.SetActive(true);
                }

                return;
            }

            ClosePopup();
        }

        private void OnActionButtonClicked()
        {
            if (RandomEventManager.Instance != null && activeEventData != null)
            {
                RandomEventManager.Instance.ApplyEventEffects(activeEventData);
            }

            ClosePopup();
        }

        private void ClosePopup()
        {
            SetCanvasState(false);
            activeEventData = null;
        }

        private void DodgeFakeCloseButton()
        {
            if (fakeCloseButtonRect == null)
            {
                return;
            }

            float offsetX = UnityEngine.Random.Range(-DodgeOffsetRangeX, DodgeOffsetRangeX);
            float offsetY = UnityEngine.Random.Range(-DodgeOffsetRangeY, DodgeOffsetRangeY);
            fakeCloseButtonRect.anchoredPosition = fakeCloseButtonOriginalPosition + new Vector2(offsetX, offsetY);
        }

        private void ResetFakeCloseButton()
        {
            fakeCloseHasDodged = false;

            if (fakeCloseButtonRect != null)
            {
                fakeCloseButtonRect.anchoredPosition = fakeCloseButtonOriginalPosition;
            }

            if (niceTryText != null)
            {
                niceTryText.gameObject.SetActive(false);
            }
        }
    }
}
