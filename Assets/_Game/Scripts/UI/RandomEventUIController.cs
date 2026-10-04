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
        private float lastCanvasWidth;

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

                // 2026-10-04: both Images are scene-authored m_RaycastTarget: 1 and never touched
                // by any other code path. Decorative-only (border frame / panel fill behind the
                // actual content) -- investigating ActionButton's click still not registering
                // after 0a89eeb. Sibling order already puts both BEHIND the content, so this
                // shouldn't matter per standard raycast depth sorting, but it removes them as a
                // variable rather than leaving two true raycastTargets sitting unexplained.
                if (borderImage != null) borderImage.raycastTarget = false;
                if (panelFillImage != null) panelFillImage.raycastTarget = false;
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
            ApplyPaletteStyle();
            LayoutForPortrait();
            ResetFakeCloseButton();

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
            // Close before effect callbacks: a subscriber exception must not leave the
            // notice covering the game, and a second click must not grant the effect twice.
            BrainRotEventData chosenEvent = activeEventData;
            ClosePopup();
            if (RandomEventManager.Instance != null && chosenEvent != null)
            {
                RandomEventManager.Instance.ApplyEventEffects(chosenEvent);
            }
        }

        private void LateUpdate()
        {
            if (activeEventData == null || eventPopupPanel == null) return;
            var parent = eventPopupPanel.transform.parent as RectTransform;
            if (parent != null && !Mathf.Approximately(parent.rect.width, lastCanvasWidth))
                LayoutForPortrait();
        }

        private void LayoutForPortrait()
        {
            if (eventPopupPanel == null) return;
            var panel = eventPopupPanel.transform as RectTransform;
            var canvasRect = panel != null ? panel.parent as RectTransform : null;
            if (canvasRect == null) return;
            Canvas.ForceUpdateCanvases();
            lastCanvasWidth = canvasRect.rect.width;
            float width = Mathf.Min(Mathf.Max(760f, lastCanvasWidth * 0.8f), lastCanvasWidth - 32f);
            if (width <= 0f) return;

            // The scene's 350x450 panel and 55-high action were desktop-sized. Measure
            // each label at its readable size, then grow the panel instead of shrinking type.
            float contentWidth = width - 80f; // 8-unit rim + 32-unit padding on both sides
            float titleHeight = MeasureLabel(titleText, 40f, contentWidth - 88f, 64f);
            float bodyHeight = MeasureLabel(descriptionText, 30f, contentWidth, 90f);
            float actionHeight = MeasureLabel(actionButtonText, 30f, contentWidth - 32f, 96f) + 24f;
            float height = 80f + titleHeight + 24f + bodyHeight + 32f + actionHeight + 48f;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.localScale = Vector3.one;
            panel.sizeDelta = new Vector2(width, height);

            if (titleText != null) PlaceTop(titleText.rectTransform, 32f, 32f, contentWidth - 88f, titleHeight);
            if (descriptionText != null) PlaceTop(descriptionText.rectTransform, 32f, 56f + titleHeight, contentWidth, bodyHeight);
            if (actionButton != null)
            {
                PlaceTop(actionButton.transform as RectTransform, 32f, 88f + titleHeight + bodyHeight, contentWidth, actionHeight);
                actionButton.interactable = true;
                // A transparent/cullable Image is skipped by GraphicRaycaster even when
                // raycastTarget is true. Keep the whole action rect available for clicks.
                Graphic hitSurface = actionButton.GetComponent<Graphic>();
                if (hitSurface != null)
                {
                    hitSurface.raycastTarget = true;
                    hitSurface.canvasRenderer.cullTransparentMesh = false;
                }
                if (actionButton.targetGraphic != null) actionButton.targetGraphic.raycastTarget = true;
            }
            if (actionButtonText != null)
            {
                RectTransform labelRect = actionButtonText.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(16f, 12f);
                labelRect.offsetMax = new Vector2(-16f, -12f);
                actionButtonText.alignment = TextAlignmentOptions.Center;
            }
            if (fakeCloseButtonRect != null)
            {
                fakeCloseButtonRect.anchorMin = fakeCloseButtonRect.anchorMax = Vector2.one;
                fakeCloseButtonRect.pivot = Vector2.one;
                fakeCloseButtonRect.sizeDelta = new Vector2(72f, 72f);
                fakeCloseButtonOriginalPosition = new Vector2(-16f, -16f);
                if (!fakeCloseHasDodged) fakeCloseButtonRect.anchoredPosition = fakeCloseButtonOriginalPosition;
                fakeCloseButtonRect.SetAsLastSibling();
            }
            if (niceTryText != null)
            {
                MeasureLabel(niceTryText, 24f, contentWidth, 32f);
                PlaceTop(niceTryText.rectTransform, 32f, height - 48f, contentWidth, 32f);
            }
        }

        private static float MeasureLabel(TextMeshProUGUI label, float size, float width, float minHeight)
        {
            if (label == null) return minHeight;
            label.enableAutoSizing = false;
            label.fontSize = size;
            label.fontSizeMin = label.fontSizeMax = size;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return Mathf.Max(minHeight, Mathf.Ceil(label.GetPreferredValues(label.text, width, Mathf.Infinity).y));
        }

        private static void PlaceTop(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
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
