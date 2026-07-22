using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.UI
{
    /// <summary>Which of the two §23 FTUE narrator channels a modal is dressed as.</summary>
    public enum IntelCardSkin
    {
        /// <summary>Illumisnotti propaganda terminal: near-black background, terminal green text. Capped at exactly 2 uses total (FTUEManager owns the cap).</summary>
        COGSTerminal,

        /// <summary>THE LITERATES resistance dead-drop card: aged-paper background, dark text, italic body. The default channel for every other FTUE beat.</summary>
        LiteratesCard
    }

    /// <summary>
    /// One-shot, code-built modal for the §23 FTUE pass (precedent: BackgroundPedestrianManager's
    /// runtime-built pedestrian UI, Bible §8's "own it in code" pattern -- no prefab, no scene
    /// wiring). <see cref="Show"/> builds a full-screen overlay Canvas above gameplay UI with a
    /// raycast-blocking dim backdrop, a centered card, header/body text, and a single confirm
    /// button; confirming destroys the overlay and invokes the callback. Stateless and disposable
    /// -- FTUEManager owns beat sequencing, seen-flag gating, and one-at-a-time FIFO queuing; this
    /// class only ever shows one card at a time, on request. Never touches Time.timeScale --
    /// gameplay keeps running behind the modal.
    /// </summary>
    public static class IntelCardUI
    {
        private const int OverlaySortingOrder = 500;

        private static readonly Color CogsBackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color CogsCardColor = new Color(0.03f, 0.03f, 0.03f, 0.97f);
        private static readonly Color CogsTextColor = new Color(0.15f, 1f, 0.35f, 1f);
        private static readonly Color CogsConfirmFillColor = new Color(0.15f, 1f, 0.35f, 0.18f);

        private static readonly Color CardBackdropColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color CardPaperColor = new Color(0.90f, 0.85f, 0.72f, 1f);
        private static readonly Color CardTextColor = new Color(0.18f, 0.14f, 0.08f, 1f);
        private static readonly Color CardConfirmFillColor = new Color(0.18f, 0.14f, 0.08f, 0.14f);

        /// <summary>
        /// Builds and shows one modal card. headerText/bodyText/confirmText are rendered exactly
        /// as passed (callers own casing/verbatim copy -- this method never transforms text).
        /// onConfirmed fires once the player taps the confirm button, after the overlay has
        /// already torn itself down.
        /// </summary>
        public static void Show(IntelCardSkin skin, string headerText, string bodyText, string confirmText, Action onConfirmed)
        {
            bool isCogs = skin == IntelCardSkin.COGSTerminal;

            GameObject overlayObject = new GameObject("IntelCardUI_Overlay", typeof(RectTransform));
            Canvas canvas = overlayObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = overlayObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayObject.AddComponent<GraphicRaycaster>();

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

            GameObject cardObject = new GameObject("Card", typeof(RectTransform));
            cardObject.transform.SetParent(overlayObject.transform, false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 1200f);
            cardRect.anchoredPosition = Vector2.zero;
            Image cardImage = cardObject.AddComponent<Image>();
            cardImage.color = isCogs ? CogsCardColor : CardPaperColor;

            VerticalLayoutGroup layout = cardObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 56, 48);
            layout.spacing = 32f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            Color textColor = isCogs ? CogsTextColor : CardTextColor;

            TextMeshProUGUI header = CreateText(cardObject.transform, headerText, 40f, textColor, FontStyles.Bold);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;

            TextMeshProUGUI body = CreateText(cardObject.transform, bodyText, 30f, textColor,
                isCogs ? FontStyles.Normal : FontStyles.Italic);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Button confirmButton = CreateConfirmButton(cardObject.transform, confirmText,
                isCogs ? CogsConfirmFillColor : CardConfirmFillColor, textColor);
            confirmButton.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlayObject);
                onConfirmed?.Invoke();
            });
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
            label.fontSizeMin = fontSize * 0.5f;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.enableWordWrapping = true;
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
            text.fontSizeMin = 16f;
            text.fontSizeMax = 28f;
            text.raycastTarget = false;

            return button;
        }
    }
}
