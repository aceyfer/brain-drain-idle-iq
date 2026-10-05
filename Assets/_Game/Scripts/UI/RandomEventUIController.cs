using System.Collections;
using DG.Tweening;
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

        // 2026-10-04 art pass (E): broadcast-interrupt restyle.
        private const float HeaderStripHeight = 48f;
        private const float HeaderGap = 16f;
        private const float TitleGlitchDuration = 0.2f;
        private const float TitleGlitchJitterInterval = 1f / 30f;
        private const float TitleGlitchJitterRange = 4f;

        private static Sprite frameSprite;
        private static Sprite headerStripSprite;
        private static Sprite scanlinesSprite;
        private static Sprite alertButtonSprite;
        private static bool spritesLoaded;

        private Image headerStripImage;
        private TextMeshProUGUI headerLabel;
        private RectTransform headerRect;
        private Coroutine titleGlitchRoutine;

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

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            frameSprite = Resources.Load<Sprite>("UI/Generated/Alert_Frame");
            headerStripSprite = Resources.Load<Sprite>("UI/Generated/Alert_HeaderStrip");
            scanlinesSprite = Resources.Load<Sprite>("UI/Generated/Alert_Scanlines");
            alertButtonSprite = Resources.Load<Sprite>("UI/Generated/Alert_Button");
            spritesLoaded = true;
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
        /// 2026-10-04 (E): now also assigns Alert_Frame/Alert_Button sprites and builds the
        /// header/scanlines chrome -- see EnsureBroadcastChrome below.
        /// </summary>
        private void ApplyPaletteStyle()
        {
            EnsureSpritesLoaded();
            Color cyan = new Color32(0x00, 0xDD, 0xEB, 0xFF);

            if (eventPopupPanel != null)
            {
                // 2026-10-04 (E): "Panel: Alert_Frame (9-slice) replaces the flat magenta-border
                // fill." eventPopupPanel (AdwareEventPopup) is the OUTER object -- its own Image
                // now carries the whole frame sprite (dark fill + magenta border + cyan corner
                // brackets baked into one 9-slice), replacing its old flat magenta color.
                Image borderImage = eventPopupPanel.GetComponent<Image>();
                if (borderImage != null && frameSprite != null)
                {
                    borderImage.sprite = frameSprite;
                    borderImage.type = Image.Type.Sliced;
                    borderImage.color = Color.white; // art is pre-colored; no tint
                }

                // PopupInnerBody is the 8px-inset child that used to carry its own flat fill
                // color -- now fully transparent since Alert_Frame's own baked fill (behind it,
                // on the outer object) already provides the panel background.
                Transform innerBody = eventPopupPanel.transform.Find("PopupInnerBody");
                Image panelFillImage = innerBody != null ? innerBody.GetComponent<Image>() : null;
                if (panelFillImage != null)
                {
                    panelFillImage.color = new Color(0f, 0f, 0f, 0f);
                }

                // 2026-10-04: both Images are scene-authored m_RaycastTarget: 1 and never touched
                // by any other code path. Decorative-only (border frame / panel fill behind the
                // actual content) -- investigating ActionButton's click still not registering
                // after 0a89eeb. Sibling order already puts both BEHIND the content, so this
                // shouldn't matter per standard raycast depth sorting, but it removes them as a
                // variable rather than leaving two true raycastTargets sitting unexplained.
                if (borderImage != null) borderImage.raycastTarget = false;
                if (panelFillImage != null) panelFillImage.raycastTarget = false;

                EnsureBroadcastChrome(innerBody);
            }

            if (actionButton != null && alertButtonSprite != null)
            {
                Image actionImage = actionButton.GetComponent<Image>();
                if (actionImage != null)
                {
                    actionImage.sprite = alertButtonSprite;
                    actionImage.type = Image.Type.Sliced;
                    actionImage.color = Color.white;
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

        /// <summary>
        /// Idempotent (Transform.Find before creating, same convention this whole session's art
        /// passes use): builds the header band (Alert_HeaderStrip + label) and the full-panel
        /// Alert_Scanlines overlay once, inside PopupInnerBody. LayoutForPortrait repositions
        /// headerRect every call; this method only ever creates the GameObjects and sets their
        /// non-positional properties (sprite, color, raycastTarget).
        /// </summary>
        private void EnsureBroadcastChrome(Transform innerBody)
        {
            if (innerBody == null) { return; }

            Transform existingHeader = innerBody.Find("BroadcastHeader");
            GameObject headerObject = existingHeader != null ? existingHeader.gameObject : null;
            if (headerObject == null)
            {
                headerObject = new GameObject("BroadcastHeader", typeof(RectTransform), typeof(Image));
                headerObject.transform.SetParent(innerBody, false);
            }
            headerRect = headerObject.GetComponent<RectTransform>();
            headerStripImage = headerObject.GetComponent<Image>();
            if (headerStripSprite != null)
            {
                headerStripImage.sprite = headerStripSprite;
                headerStripImage.type = Image.Type.Tiled;
            }
            headerStripImage.color = Color.white;
            headerStripImage.raycastTarget = false;

            Transform existingLabel = headerObject.transform.Find("Label");
            GameObject labelObject = existingLabel != null ? existingLabel.gameObject : null;
            if (labelObject == null)
            {
                labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(headerObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
            headerLabel = labelObject.GetComponent<TextMeshProUGUI>();
            if (headerLabel == null) { headerLabel = labelObject.AddComponent<TextMeshProUGUI>(); }
            // "If the font lacks warning-sign glyph, use !!" -- this project's default TMP font
            // (LiberationSans SDF) is already confirmed missing other pictographic glyphs
            // (TASKLIST #30, U+2794 right-arrow rendered as a box), so U+26A0 is assumed missing
            // too rather than risking the same regression to verify it live.
            headerLabel.text = "!! BROADCAST INTERRUPT !!";
            // 2026-10-05 PALETTE LOCKDOWN audit follow-up: was white, unreadable against the
            // stripe -- Cyan bold reads clearly against the now-dark Surface/Deep Cyan stripes.
            headerLabel.color = Palette.Cyan;
            headerLabel.fontStyle = FontStyles.Bold;
            headerLabel.alignment = TextAlignmentOptions.Center;
            headerLabel.fontSize = 24f;
            headerLabel.enableAutoSizing = true;
            headerLabel.fontSizeMin = 20f;
            headerLabel.fontSizeMax = 24f;
            headerLabel.textWrappingMode = TextWrappingModes.NoWrap;
            headerLabel.raycastTarget = false;

            Transform existingScanlines = innerBody.Find("Scanlines");
            GameObject scanlinesObject = existingScanlines != null ? existingScanlines.gameObject : null;
            if (scanlinesObject == null)
            {
                scanlinesObject = new GameObject("Scanlines", typeof(RectTransform), typeof(Image));
                scanlinesObject.transform.SetParent(innerBody, false);
                RectTransform scanlinesRect = scanlinesObject.GetComponent<RectTransform>();
                scanlinesRect.anchorMin = Vector2.zero;
                scanlinesRect.anchorMax = Vector2.one;
                scanlinesRect.offsetMin = Vector2.zero;
                scanlinesRect.offsetMax = Vector2.zero;
            }
            scanlinesObject.transform.SetAsLastSibling(); // over everything, including the header
            Image scanlinesImage = scanlinesObject.GetComponent<Image>();
            if (scanlinesSprite != null)
            {
                scanlinesImage.sprite = scanlinesSprite;
                scanlinesImage.type = Image.Type.Tiled;
            }
            scanlinesImage.color = new Color(1f, 1f, 1f, 0.4f);
            scanlinesImage.raycastTarget = false;
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

                // 2026-10-04 (E): this popup's own open beat -- AnimationController.PlayPopupSpawn
                // is shared with RebirthUIController, so a bespoke local animation here (rather
                // than editing that shared method) can't regress the other consumer.
                PlayBroadcastOpenAnimation(panelRect, canvasGroup);
            }

            if (titleGlitchRoutine != null) { StopCoroutine(titleGlitchRoutine); }
            titleGlitchRoutine = StartCoroutine(TitleGlitchRoutine());
        }

        /// <summary>CanvasGroup alpha 0->1 and panel scale 0.85->1.05->1 over 0.25s total,
        /// unscaled time -- this class never touches Time.timeScale, so the open beat must not
        /// either. Two independent tweens (fade + a scale sequence) rather than one Sequence with
        /// Join/Append, since Join would stretch the sequence's first slot to the LONGER of the
        /// two durations and push the total past 0.25s.</summary>
        private void PlayBroadcastOpenAnimation(RectTransform panelRect, CanvasGroup canvasGroup)
        {
            if (panelRect == null) { return; }

            DOTween.Kill(panelRect);
            panelRect.localScale = Vector3.one * 0.85f;
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.Append(panelRect.DOScale(1.05f, 0.15f).SetEase(Ease.OutQuad));
            scaleSeq.Append(panelRect.DOScale(1f, 0.10f).SetEase(Ease.InQuad));
            scaleSeq.SetUpdate(true).SetId(panelRect);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.25f).SetUpdate(true).SetId(panelRect);
            }
        }

        /// <summary>0.2s title glitch on every open: a single-frame magenta ghost copy offset a
        /// few px (classic RGB-split flash), then +/-4px x-jitter at 30Hz for the rest of the
        /// window, then settle back to the authored position.</summary>
        private IEnumerator TitleGlitchRoutine()
        {
            if (titleText == null) { yield break; }

            RectTransform rect = titleText.rectTransform;
            Vector2 basePos = rect.anchoredPosition;

            GameObject ghostObject = new GameObject("TitleGlitchGhost", typeof(RectTransform));
            ghostObject.transform.SetParent(rect.parent, false);
            RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.anchorMin = rect.anchorMin;
            ghostRect.anchorMax = rect.anchorMax;
            ghostRect.pivot = rect.pivot;
            ghostRect.sizeDelta = rect.sizeDelta;
            ghostRect.anchoredPosition = basePos + new Vector2(TitleGlitchJitterRange, 0f);
            TextMeshProUGUI ghost = ghostObject.AddComponent<TextMeshProUGUI>();
            ghost.text = titleText.text;
            ghost.font = titleText.font;
            ghost.fontSize = titleText.fontSize;
            ghost.fontStyle = titleText.fontStyle;
            ghost.alignment = titleText.alignment;
            // 2026-10-04 PALETTE LOCKDOWN: was magenta (retired) -- Cyan at the same 0.6 alpha.
            ghost.color = new Color(Palette.Cyan.r, Palette.Cyan.g, Palette.Cyan.b, 0.6f);
            ghost.raycastTarget = false;
            yield return null; // exactly one frame
            Destroy(ghostObject);

            float elapsed = 0f;
            while (elapsed < TitleGlitchDuration)
            {
                float jitterX = UnityEngine.Random.Range(-TitleGlitchJitterRange, TitleGlitchJitterRange);
                rect.anchoredPosition = basePos + new Vector2(jitterX, 0f);
                yield return new WaitForSecondsRealtime(TitleGlitchJitterInterval);
                elapsed += TitleGlitchJitterInterval;
            }

            rect.anchoredPosition = basePos;
            titleGlitchRoutine = null;
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
            // 2026-10-04 (E): reserves room for the new header band at the top -- everything below
            // it (title/body/action/nice-try) shifts down by the same HeaderStripHeight+HeaderGap,
            // added once here rather than touching the original 80/24/32/48 constants themselves.
            float headerOffset = HeaderStripHeight + HeaderGap;
            float height = 80f + headerOffset + titleHeight + 24f + bodyHeight + 32f + actionHeight + 48f;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.localScale = Vector3.one;
            panel.sizeDelta = new Vector2(width, height);

            if (headerRect != null)
            {
                // Edge-to-edge within PopupInnerBody (flush against the 8px rim), not inset by
                // the extra 32px content padding title/body/action use -- a banner should run the
                // full width of the frame's interior.
                float innerWidth = width - 16f;
                PlaceTop(headerRect, 0f, 0f, innerWidth, HeaderStripHeight);
            }
            if (titleText != null) PlaceTop(titleText.rectTransform, 32f, 32f + headerOffset, contentWidth - 88f, titleHeight);
            if (descriptionText != null) PlaceTop(descriptionText.rectTransform, 32f, 56f + headerOffset + titleHeight, contentWidth, bodyHeight);
            if (actionButton != null)
            {
                PlaceTop(actionButton.transform as RectTransform, 32f, 88f + headerOffset + titleHeight + bodyHeight, contentWidth, actionHeight);
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
