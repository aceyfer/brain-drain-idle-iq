using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>Which of the two §23 FTUE narrator channels a modal is dressed as.</summary>
    public enum IntelCardSkin
    {
        /// <summary>Illumisnotty propaganda terminal: near-black background, terminal green text. Capped at exactly 2 uses total (FTUEManager owns the cap).</summary>
        COGSTerminal,

        /// <summary>THE LITERATES resistance dead-drop card: aged-paper background and dark ink. The default channel for every other FTUE beat.</summary>
        LiteratesCard
    }

    /// <summary>
    /// One-shot, code-built modal for the §23 FTUE pass (precedent: BackgroundPedestrianManager's
    /// runtime-built pedestrian UI, Bible §8's "own it in code" pattern -- no prefab, no scene
    /// wiring). <see cref="Show"/> builds a full-screen overlay Canvas above gameplay UI with a
    /// raycast-blocking dim backdrop, a centered card, and a single confirm button; confirming
    /// destroys the overlay and invokes the callback. Stateless and disposable -- FTUEManager owns
    /// beat sequencing, seen-flag gating, and one-at-a-time FIFO queuing; this class only ever
    /// shows one card at a time, on request. Never touches Time.timeScale -- gameplay keeps
    /// running behind the modal.
    ///
    /// 2026-10-04 art pass (C): the LiteratesCard (paper) skin now opens on its business-card
    /// FRONT (same look as PocketPanelUI's restyled cards, scaled up, no tilt -- a single centered
    /// modal card reads oddly tilted the way a tilted stack of list rows doesn't) and flips to the
    /// intel-message BACK on tap or after 0.6s, matching a real business card's "flip it over"
    /// affordance. The COGSTerminal skin is unchanged except for an added Alert_Scanlines overlay.
    /// </summary>
    public static class IntelCardUI
    {
        private const int OverlaySortingOrder = 500;
        private const float FlipHalfDuration = 0.12f;
        private const float AutoFlipDelay = 0.6f;

        private static readonly Color CogsBackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color CogsCardColor = new Color(0.03f, 0.03f, 0.03f, 0.97f);
        private static readonly Color CogsTextColor = new Color(0.15f, 1f, 0.35f, 1f);
        private static readonly Color CogsConfirmFillColor = new Color(0.15f, 1f, 0.35f, 0.18f);

        private static readonly Color CardBackdropColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color CardPaperColor = new Color(0.90f, 0.85f, 0.72f, 1f);
        private static readonly Color32 CardInk = new Color32(0x1B, 0x0F, 0x2E, 255);
        private static readonly Color CardBodyInk = new Color32(0x1B, 0x0F, 0x2E, 217);

        private static Sprite paperSprite;
        private static Sprite shadowSprite;
        private static Sprite monogramRingSprite;
        private static Sprite alertButtonSprite;
        private static Sprite scanlinesSprite;
        private static bool spritesLoaded;

        private static void EnsureSpritesLoaded()
        {
            if (spritesLoaded) { return; }
            paperSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Paper");
            shadowSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Shadow");
            monogramRingSprite = Resources.Load<Sprite>("UI/Generated/BizCard_Monogram_Ring");
            alertButtonSprite = Resources.Load<Sprite>("UI/Generated/Alert_Button");
            scanlinesSprite = Resources.Load<Sprite>("UI/Generated/Alert_Scanlines");
            spritesLoaded = true;
        }

        /// <summary>
        /// Builds and shows one modal card. headerText/bodyText/confirmText are rendered exactly
        /// as passed (callers own casing/verbatim copy -- this method never transforms text).
        /// onConfirmed fires once the player taps the confirm button, after the overlay has
        /// already torn itself down.
        /// </summary>
        public static void Show(IntelCardSkin skin, string headerText, string bodyText, string confirmText, Action onConfirmed)
        {
            EnsureSpritesLoaded();
            bool isCogs = skin == IntelCardSkin.COGSTerminal;

            GameObject overlayObject = new GameObject("IntelCardUI_Overlay", typeof(RectTransform));
            NudgeModalScope.SetOpen(overlayObject, true);
            Canvas canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = overlayObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayObject.AddComponent<GraphicRaycaster>();

            // Backdrop: untouched by this pass -- same input-blocking raycast-target Image, same
            // colors, same construction, per the brief's own "don't change the modal backdrop or
            // its input blocking."
            GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform));
            backdropObject.transform.SetParent(overlayObject.transform, false);
            RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            Image backdropImage = backdropObject.AddComponent<Image>();
            backdropImage.color = isCogs ? CogsBackdropColor : CardBackdropColor;
            backdropImage.raycastTarget = true;

            if (isCogs)
            {
                BuildCogsCard(overlayObject.transform, headerText, bodyText, confirmText, onConfirmed, overlayObject);
            }
            else
            {
                BuildLiteratesFlipCard(overlayObject.transform, headerText, bodyText, confirmText, onConfirmed, overlayObject);
            }
        }

        // ---- COGSTerminal: unchanged layout, + Alert_Scanlines overlay -------------------------

        private static void BuildCogsCard(Transform overlayTransform, string headerText, string bodyText, string confirmText, Action onConfirmed, GameObject overlayObject)
        {
            GameObject cardObject = new GameObject("Card", typeof(RectTransform));
            cardObject.transform.SetParent(overlayTransform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 1200f);
            cardRect.anchoredPosition = Vector2.zero;
            LayoutElement cardSize = cardObject.AddComponent<LayoutElement>();
            cardSize.preferredWidth = 940f;
            ContentSizeFitter cardFitter = cardObject.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Image cardImage = cardObject.AddComponent<Image>();
            cardImage.color = CogsCardColor;

            VerticalLayoutGroup layout = cardObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 56, 48);
            layout.spacing = 32f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            TextMeshProUGUI header = CreateText(cardObject.transform, headerText, 40f, CogsTextColor, FontStyles.Bold);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;

            TextMeshProUGUI body = CreateText(cardObject.transform, bodyText, 30f, CogsTextColor, FontStyles.Normal);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Button confirmButton = CreateConfirmButton(cardObject.transform, confirmText, CogsConfirmFillColor, CogsTextColor);
            confirmButton.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlayObject);
                onConfirmed?.Invoke();
            });

            // 2026-10-04 (C): scanline overlay, tiled over the whole card, never intercepting input.
            if (scanlinesSprite != null)
            {
                GameObject scanlinesObject = new GameObject("Scanlines", typeof(RectTransform));
                scanlinesObject.transform.SetParent(cardObject.transform, false);
                scanlinesObject.transform.SetAsLastSibling();
                RectTransform scanlinesRect = scanlinesObject.GetComponent<RectTransform>();
                scanlinesRect.anchorMin = Vector2.zero;
                scanlinesRect.anchorMax = Vector2.one;
                scanlinesRect.offsetMin = Vector2.zero;
                scanlinesRect.offsetMax = Vector2.zero;
                Image scanlinesImage = scanlinesObject.AddComponent<Image>();
                scanlinesImage.sprite = scanlinesSprite;
                scanlinesImage.type = Image.Type.Tiled;
                scanlinesImage.color = new Color(1f, 1f, 1f, 0.4f);
                scanlinesImage.raycastTarget = false;
                // A VerticalLayoutGroup drives this card's size, so this overlay (added after the
                // group's own children) would otherwise be treated as another layout child and
                // pushed below the confirm button. Ignore layout and let its stretch anchors cover
                // the card instead.
                scanlinesObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
        }

        // ---- LiteratesCard: business-card front, flip to ink back ------------------------------

        private static void BuildLiteratesFlipCard(Transform overlayTransform, string frontText, string bodyText, string confirmText, Action onConfirmed, GameObject overlayObject)
        {
            SplitFrontTitle(frontText, out string businessName, out string tagline);

            GameObject cardObject = new GameObject("Card", typeof(RectTransform));
            cardObject.transform.SetParent(overlayTransform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 1200f);
            cardRect.anchoredPosition = Vector2.zero;

            GameObject flipTargetObject = new GameObject("FlipTarget", typeof(RectTransform));
            flipTargetObject.transform.SetParent(cardObject.transform, false);
            RectTransform flipRect = flipTargetObject.GetComponent<RectTransform>();
            flipRect.anchorMin = Vector2.zero;
            flipRect.anchorMax = Vector2.one;
            flipRect.offsetMin = Vector2.zero;
            flipRect.offsetMax = Vector2.zero;

            // ---- FRONT: same business-card design as PocketPanelUI's restyled cards, scaled up.
            // No tilt here (unlike the Pocket list) -- a single centered modal card reads as a
            // mistake when tilted, where a stack of tilted list rows reads as a deliberate pile.
            GameObject frontFace = new GameObject("FrontFace", typeof(RectTransform));
            frontFace.transform.SetParent(flipTargetObject.transform, false);
            RectTransform frontRect = frontFace.GetComponent<RectTransform>();
            frontRect.anchorMin = frontRect.anchorMax = new Vector2(0.5f, 0.5f);
            frontRect.pivot = new Vector2(0.5f, 0.5f);
            frontRect.sizeDelta = new Vector2(820f, 460f); // scaled-up BizCard_Paper (512x288) aspect
            frontRect.anchoredPosition = Vector2.zero;

            if (shadowSprite != null)
            {
                GameObject shadowObject = new GameObject("Shadow", typeof(RectTransform));
                shadowObject.transform.SetParent(frontFace.transform, false);
                RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                shadowRect.offsetMin = new Vector2(8f, -8f);
                shadowRect.offsetMax = new Vector2(8f, -8f);
                Image shadowImage = shadowObject.AddComponent<Image>();
                shadowImage.sprite = shadowSprite;
                shadowImage.type = Image.Type.Sliced;
                shadowImage.raycastTarget = false;
            }

            GameObject paperObject = new GameObject("Paper", typeof(RectTransform));
            paperObject.transform.SetParent(frontFace.transform, false);
            RectTransform paperRect = paperObject.GetComponent<RectTransform>();
            paperRect.anchorMin = Vector2.zero;
            paperRect.anchorMax = Vector2.one;
            paperRect.offsetMin = Vector2.zero;
            paperRect.offsetMax = Vector2.zero;
            Image paperImage = paperObject.AddComponent<Image>();
            if (paperSprite != null) { paperImage.sprite = paperSprite; paperImage.type = Image.Type.Sliced; }
            else { paperImage.color = CardPaperColor; }
            paperImage.raycastTarget = false;

            const float ringSize = 128f;
            GameObject ringObject = new GameObject("Ring", typeof(RectTransform));
            ringObject.transform.SetParent(frontFace.transform, false);
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0f, 0.5f);
            ringRect.anchorMax = new Vector2(0f, 0.5f);
            ringRect.pivot = new Vector2(0f, 0.5f);
            ringRect.sizeDelta = new Vector2(ringSize, ringSize);
            ringRect.anchoredPosition = new Vector2(32f, 0f);
            Image ringImage = ringObject.AddComponent<Image>();
            ringImage.sprite = monogramRingSprite;
            ringImage.raycastTarget = false;
            TextMeshProUGUI initials = CreateInkLabel(ringObject.transform, GetInitials(businessName), 40f, 24f, FontStyles.Bold, false);
            initials.alignment = TextAlignmentOptions.Center;

            float textLeft = 32f + ringSize + 24f;
            GameObject nameObject = new GameObject("NameText", typeof(RectTransform));
            nameObject.transform.SetParent(frontFace.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(textLeft, 0f);
            nameRect.offsetMax = new Vector2(-24f, -16f);
            TextMeshProUGUI nameLabel = CreateInkLabel(nameObject.transform, businessName.ToUpperInvariant(), 34f, 26f, FontStyles.Bold, false);
            nameLabel.alignment = TextAlignmentOptions.BottomLeft;

            GameObject taglineObject = new GameObject("TaglineText", typeof(RectTransform));
            taglineObject.transform.SetParent(frontFace.transform, false);
            RectTransform taglineRect = taglineObject.GetComponent<RectTransform>();
            taglineRect.anchorMin = Vector2.zero;
            taglineRect.anchorMax = new Vector2(1f, 0.5f);
            taglineRect.offsetMin = new Vector2(textLeft, 16f);
            taglineRect.offsetMax = new Vector2(-24f, 0f);
            TextMeshProUGUI taglineLabel = CreateInkLabel(taglineObject.transform, tagline, 26f, 22f, FontStyles.Italic, true);
            taglineLabel.alignment = TextAlignmentOptions.TopLeft;

            // ---- BACK: intel message on paper, ink #1B0F2E @85%, non-italic. Sign-off is already
            // embedded in bodyText (IntelCardCatalog's own verbatim copy), not a separate element.
            GameObject backFace = new GameObject("BackFace", typeof(RectTransform));
            backFace.transform.SetParent(flipTargetObject.transform, false);
            RectTransform backRect = backFace.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            backFace.SetActive(false);

            GameObject backPaperObject = new GameObject("Paper", typeof(RectTransform));
            backPaperObject.transform.SetParent(backFace.transform, false);
            RectTransform backPaperRect = backPaperObject.GetComponent<RectTransform>();
            backPaperRect.anchorMin = Vector2.zero;
            backPaperRect.anchorMax = Vector2.one;
            backPaperRect.offsetMin = Vector2.zero;
            backPaperRect.offsetMax = Vector2.zero;
            Image backPaperImage = backPaperObject.AddComponent<Image>();
            if (paperSprite != null) { backPaperImage.sprite = paperSprite; backPaperImage.type = Image.Type.Sliced; }
            else { backPaperImage.color = CardPaperColor; }
            backPaperImage.raycastTarget = false;

            GameObject backBodyObject = new GameObject("BodyText", typeof(RectTransform));
            backBodyObject.transform.SetParent(backFace.transform, false);
            RectTransform backBodyRect = backBodyObject.GetComponent<RectTransform>();
            backBodyRect.anchorMin = Vector2.zero;
            backBodyRect.anchorMax = Vector2.one;
            backBodyRect.offsetMin = new Vector2(48f, 160f);
            backBodyRect.offsetMax = new Vector2(-48f, -56f);
            TextMeshProUGUI backBody = CreateText(backBodyObject.transform, bodyText, 30f, CardBodyInk, FontStyles.Normal);

            // Confirm button -- hidden until the flip completes. "Alert_Button recolored to
            // ink-on-paper": Alert_Button.png bakes its cyan border/glow as solid RGB with uniform
            // alpha across the whole pill, so tinting it is the only way to "recolor" it without a
            // second generated sprite -- this collapses the border/fill/glow into one ink-colored
            // pill rather than a true two-tone outline, a deliberate simplification flagged here,
            // not an oversight. "No glow" is satisfied structurally: the tint removes the cyan/glow
            // hue entirely, leaving a flat ink pill with the label on top.
            GameObject confirmObject = new GameObject("ConfirmButton", typeof(RectTransform));
            confirmObject.transform.SetParent(backFace.transform, false);
            RectTransform confirmRect = confirmObject.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0f, 0f);
            confirmRect.anchorMax = new Vector2(1f, 0f);
            confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(0f, 48f);
            confirmRect.sizeDelta = new Vector2(-96f, 96f);
            Image confirmImage = confirmObject.AddComponent<Image>();
            if (alertButtonSprite != null)
            {
                confirmImage.sprite = alertButtonSprite;
                confirmImage.type = Image.Type.Sliced;
                confirmImage.color = CardInk;
            }
            else
            {
                confirmImage.color = new Color(CardInk.r / 255f, CardInk.g / 255f, CardInk.b / 255f, 0.14f);
            }
            Button confirmButton = confirmObject.AddComponent<Button>();
            confirmButton.targetGraphic = confirmImage;
            confirmButton.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlayObject);
                onConfirmed?.Invoke();
            });

            GameObject confirmLabelObject = new GameObject("Label", typeof(RectTransform));
            confirmLabelObject.transform.SetParent(confirmObject.transform, false);
            RectTransform confirmLabelRect = confirmLabelObject.GetComponent<RectTransform>();
            confirmLabelRect.anchorMin = Vector2.zero;
            confirmLabelRect.anchorMax = Vector2.one;
            confirmLabelRect.offsetMin = Vector2.zero;
            confirmLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI confirmLabel = confirmLabelObject.AddComponent<TextMeshProUGUI>();
            confirmLabel.text = confirmText;
            confirmLabel.color = Color.white;
            confirmLabel.fontStyle = FontStyles.Bold;
            confirmLabel.alignment = TextAlignmentOptions.Center;
            confirmLabel.fontSize = 28f;
            confirmLabel.enableAutoSizing = true;
            confirmLabel.fontSizeMin = 20f;
            confirmLabel.fontSizeMax = 28f;
            confirmLabel.raycastTarget = false;

            confirmObject.SetActive(false);

            // ---- Tap-anywhere-or-0.6s flip. Unscaled time (SetUpdate(true)) since this never
            // touches Time.timeScale, matching the class's own stated contract.
            bool hasFlipped = false;

            GameObject tapCatcherObject = new GameObject("TapToFlip", typeof(RectTransform));
            tapCatcherObject.transform.SetParent(flipTargetObject.transform, false);
            RectTransform tapRect = tapCatcherObject.GetComponent<RectTransform>();
            tapRect.anchorMin = Vector2.zero;
            tapRect.anchorMax = Vector2.one;
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;
            Image tapImage = tapCatcherObject.AddComponent<Image>();
            tapImage.color = new Color(0f, 0f, 0f, 0f);
            Button tapButton = tapCatcherObject.AddComponent<Button>();
            tapButton.targetGraphic = tapImage;

            void DoFlip()
            {
                if (hasFlipped) { return; }
                hasFlipped = true;
                DOTween.Kill(flipTargetObject);
                tapCatcherObject.SetActive(false);

                flipTargetObject.transform.DOScaleX(0f, FlipHalfDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetUpdate(true)
                    .SetId(flipTargetObject)
                    .OnComplete(() =>
                    {
                        frontFace.SetActive(false);
                        backFace.SetActive(true);
                        flipTargetObject.transform.DOScaleX(1f, FlipHalfDuration)
                            .SetEase(Ease.InOutQuad)
                            .SetUpdate(true)
                            .SetId(flipTargetObject)
                            .OnComplete(() => confirmObject.SetActive(true));
                    });
            }

            tapButton.onClick.AddListener(DoFlip);
            DOVirtual.DelayedCall(AutoFlipDelay, DoFlip, true).SetId(flipTargetObject);
        }

        /// <summary>Splits "BUSINESS NAME — \"tagline\"" the way every IntelCardCatalog.Front
        /// string is already formatted -- same split PocketPanelUI uses for the same cards.</summary>
        private static void SplitFrontTitle(string front, out string name, out string tagline)
        {
            const string separator = " — ";
            int index = front != null ? front.IndexOf(separator, StringComparison.Ordinal) : -1;
            if (index < 0)
            {
                name = front ?? string.Empty;
                tagline = string.Empty;
                return;
            }
            name = front.Substring(0, index).Trim();
            tagline = front.Substring(index + separator.Length).Trim();
        }

        /// <summary>1-2 letter monogram from a business name's word-initials, e.g. "GARY'S
        /// DISCOUNT MATTRESS EMPORIUM" -> per-word initials "GDME" -> first two -> "GD".</summary>
        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }

            var acronym = new System.Text.StringBuilder();
            foreach (string word in name.Split(' '))
            {
                foreach (char c in word)
                {
                    if (char.IsLetter(c))
                    {
                        acronym.Append(char.ToUpperInvariant(c));
                        break;
                    }
                }
                if (acronym.Length >= 2) { break; }
            }

            return acronym.ToString();
        }

        private static TextMeshProUGUI CreateInkLabel(Transform parent, string text, float maxSize, float minSize, FontStyles style, bool fadedInk)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = fadedInk ? new Color(CardInk.r / 255f, CardInk.g / 255f, CardInk.b / 255f, 0.7f) : (Color)CardInk;
            label.fontStyle = style;
            label.fontSize = maxSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = maxSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Color color, FontStyles style)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = color;
            label.fontStyle = style;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(22f, fontSize * 0.5f);
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            return label;
        }

        private static Button CreateConfirmButton(Transform parent, string confirmText, Color fillColor, Color textColor)
        {
            GameObject buttonObject = new GameObject("ConfirmButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            Image image = buttonObject.AddComponent<Image>();
            image.color = fillColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = confirmText;
            text.color = textColor;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 28f;
            text.raycastTarget = false;

            return button;
        }
    }
}
