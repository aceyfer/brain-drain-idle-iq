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

            ApplyPaletteStyle();
        }

        /// <summary>
        /// 2026-09-30 polish pass: the popup shipped with off-palette scene-baked colors --
        /// AdwareEventPopup (the outer object, == eventPopupPanel) carried a cyan fill acting as
        /// a border, its inset child PopupInnerBody carried a magenta fill as the actual panel,
        /// the title was green and bold, and the body was black and ITALIC -- unreadable against
        /// the magenta panel, confirmed directly in the scene data (m_fontColor {0,0,0,1},
        /// m_fontStyle 2 == Italic). No .unity scene writes are permitted, so this asserts the
        /// palette once here instead -- code-owns-presentation (Bible §8), same convention as
        /// ChatterBubble.SetText. Applied once at Awake() rather than per-event, since nothing
        /// here varies per BrainRotEventData asset -- every event shares this one style pass.
        /// </summary>
        private void ApplyPaletteStyle()
        {
            Color baseFill = new Color32(0x1B, 0x0F, 0x2E, 242); // ~95% alpha (242/255)
            Color magenta = new Color32(0xFF, 0x14, 0x93, 0xFF);
            Color cyan = new Color32(0x00, 0xDD, 0xEB, 0xFF);

            if (eventPopupPanel != null)
            {
                // eventPopupPanel (AdwareEventPopup) is the OUTER object -- its own Image becomes
                // the alert-colored border/frame, since this event popup is inherently an alert.
                Image borderImage = eventPopupPanel.GetComponent<Image>();
                if (borderImage != null)
                {
                    borderImage.color = magenta;
                }

                // PopupInnerBody is the 8px-inset child that carries the actual panel fill.
                Transform innerBody = eventPopupPanel.transform.Find("PopupInnerBody");
                Image panelFillImage = innerBody != null ? innerBody.GetComponent<Image>() : null;
                if (panelFillImage != null)
                {
                    panelFillImage.color = baseFill;
                }
            }

            if (titleText != null)
            {
                titleText.color = cyan;
                titleText.fontStyle = FontStyles.Bold;
            }

            if (descriptionText != null)
            {
                descriptionText.color = Color.white;
                descriptionText.fontStyle = FontStyles.Normal; // clears the scene-baked Italic flag

                // Body must read at least 60% of the title's size -- both labels auto-size, so
                // enforce the ratio on both ends of their range rather than a single fontSize
                // snapshot. Mathf.Max so this only ever raises the floor, never shrinks an
                // already-larger authored value.
                if (titleText != null)
                {
                    descriptionText.fontSizeMin = Mathf.Max(descriptionText.fontSizeMin, titleText.fontSizeMin * 0.6f);
                    descriptionText.fontSizeMax = Mathf.Max(descriptionText.fontSizeMax, titleText.fontSizeMax * 0.6f);
                }
            }

            if (actionButtonText != null)
            {
                actionButtonText.color = cyan;
            }

            if (fakeCloseButton != null)
            {
                // The "X" glyph is a TMP child under FakeCloseButton, not a serialized field of
                // its own -- resolved by component search rather than a new Inspector reference,
                // since adding one would need a scene write to wire it.
                TextMeshProUGUI glyph = fakeCloseButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (glyph != null)
                {
                    glyph.color = Color.white;
                }
            }
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
