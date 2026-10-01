using UnityEngine;
using TMPro;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays a temporary floating UI speech bubble that drifts upward while tracking a
    /// target pedestrian's X coordinate if available. Holds full opacity for the first 70%
    /// of its lifetime, then fades linearly to zero over the final 30% (avoids the bubble
    /// looking half-gone at mid-life despite SetText's longer duration).
    /// </summary>
    public sealed class ChatterBubble : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textLabel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private UnityEngine.UI.Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float floatDuration = 2.5f;
        [SerializeField] private float floatDistance = 40f;

        private RectTransform rectTransform;
        private RectTransform targetPedestrian;
        private float verticalOffset;
        private float lastKnownX;
        private float startY;
        private float elapsed;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (textLabel != null)
            {
                textLabel.raycastTarget = false;
            }

            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = false;
            }
        }

        private void Start()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// Starts tracking a pedestrian to follow their movement horizontally.
        /// </summary>
        public void TrackPedestrian(RectTransform pedestrian, float offset)
        {
            targetPedestrian = pedestrian;
            verticalOffset = offset;
            
            if (pedestrian != null)
            {
                lastKnownX = pedestrian.anchoredPosition.x;
                startY = pedestrian.anchoredPosition.y + offset;
            }
            else
            {
                lastKnownX = rectTransform != null ? rectTransform.anchoredPosition.x : 0f;
                startY = rectTransform != null ? rectTransform.anchoredPosition.y : 0f;
            }
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(ClampXToParentBounds(lastKnownX), startY);
            }
        }

        /// <summary>Fixed bubble width (matches TextLabel's own wrap width) and minimum height floor.</summary>
        private const float MinBubbleWidth = 300f;
        private const float MinBubbleHeight = 90f;

        /// <summary>Matches TextLabel's own stretch inset in the prefab (sizeDelta -50,-50
        /// relative to this rect) -- 25px padding per side, both axes.</summary>
        private const float BubblePadding = 50f;

        /// <summary>
        /// 2026-10-01: root cause of "last line still touches the bottom edge" surviving the
        /// symmetric-offset fix in 6d458d2 -- that fix inset TextLabel symmetrically from the
        /// BUBBLE ROOT's rect, which was already correct, but Background (the visible shape) is
        /// Image.Type.Sliced using Speechbubble_0001.png, whose import border is asymmetric:
        /// {left: 84, bottom: 112, right: 112, top: 84} texture px (see the sprite's own .meta).
        /// Direct pixel analysis of that texture (256x256) shows WHY: the top-84 border is almost
        /// entirely the solid rounded body (opaque data starts ~row 24 of 84, i.e. ~20px of
        /// genuinely empty margin), while the bottom-112 border is roughly HALF solid body and
        /// half a tapering tail/pointer shape (opaque pixel count collapses from ~230 to ~20
        /// around row 198 of that 112px span) -- a chat-bubble pointer baked into the art, not a
        /// defect in the sprite itself. A flat symmetric RectTransform inset has no awareness of
        /// this, so the same 25px looked fine against the top's mostly-solid border but nowhere
        /// near enough against the bottom's tail-eaten one.
        /// Converted to this project's actual UI-pixel space (sprite pixelsPerUnit 128, Canvas
        /// reference pixelsPerUnit 100, per Image's own border-scaling formula
        /// texturePx / (spritePPU / canvasRefPPU)): top border = 84 / 1.28 = 65.6px on screen,
        /// bottom border = 112 / 1.28 = 87.5px -- a 21.9px raw size difference before even
        /// accounting for the extra tail-taper space inside the bottom border. This constant
        /// closes that gap (plus a small safety margin for the taper) by adding extra inset on
        /// the bottom only, leaving the top's existing halfPadding alone. No live Editor session
        /// was available to visually tune this further -- flagged for Play-test confirmation.
        /// </summary>
        private const float BubbleBottomExtraInset = 24f;

        /// <summary>
        /// Sets the label text, asserts the readability floor on the font, and scales the
        /// bubble's lifetime with reading length. Presentation state is code-owned (Bible
        /// §8): the prefab serialized floatDuration 2s and TMP auto-sizing down to 14pt --
        /// unreadable, and dialogue is this game's identity. Duration: max(4s, 2s + 0.06s
        /// per character), so a typical bark holds 5-7s. Also asserts a dark background chip
        /// (0.06, 0.06, 0.1, 0.92), white label text, and a 24pt font-size floor -- the prefab
        /// shipped with white-on-white text, both unreadable in practice.
        /// </summary>
        public void SetText(string text)
        {
            if (textLabel != null)
            {
                textLabel.text = text;
                textLabel.enableAutoSizing = true;
                textLabel.fontSizeMin = 24f;
                textLabel.fontSizeMax = 28f;
                textLabel.color = Color.white;

                // 2026-09-30 fix: explicit combined alignment (Center = horizontal Center +
                // vertical Middle in one enum) overrides whatever ambiguous legacy
                // m_textAlignment the prefab shipped with (serialized as 65535, not a valid
                // single TextAlignmentOptions value -- likely why text rendered top-anchored
                // instead of centered, eating into the bottom padding budget).
                textLabel.alignment = TextAlignmentOptions.Center;

                // Force a perfectly symmetric inset on all 4 sides via anchors/offsets directly,
                // rather than trusting the prefab's own sizeDelta/anchoredPosition (which carried
                // a +5 Y anchoredPosition bias -- "pivot/anchor offsets push the text down" per
                // the reported bug). BubblePadding is the same constant ComputeBubbleSize uses
                // for the outer bubble's height budget, so the two stay in lockstep.
                RectTransform labelRect = textLabel.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                float halfPadding = BubblePadding * 0.5f;
                // Bottom gets extra inset to offset Background's sliced sprite tail -- see
                // BubbleBottomExtraInset's doc comment. Top is untouched (already clears its own,
                // much smaller, border dead-zone with the plain halfPadding).
                labelRect.offsetMin = new Vector2(halfPadding, halfPadding + BubbleBottomExtraInset);
                labelRect.offsetMax = new Vector2(-halfPadding, -halfPadding);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = ComputeBubbleSize(text);
            }

            int chars = string.IsNullOrEmpty(text) ? 0 : text.Length;
            floatDuration = Mathf.Max(4f, 2f + chars * 0.06f);
        }

        /// <summary>
        /// Grows the bubble to actually fit its text instead of only flooring to a fixed minimum
        /// -- the old Vector2.Max(existing, (300,90)) never grew past that floor, so anything
        /// longer than a couple short lines spilled straight out of the background (TMP's
        /// overflowMode on TextLabel is Overflow, not Truncate/ScrollRect, so it never clips
        /// itself). Width stays fixed at the floor (matches TextLabel's own wrap width); only
        /// height grows to fit. Measured at fontSizeMax (28) rather than whatever auto-sizing
        /// last resolved the label to, so the bubble is sized for the worst case (most vertical
        /// space needed) and is never undersized once auto-sizing picks the actual render size.
        /// </summary>
        private Vector2 ComputeBubbleSize(string text)
        {
            float width = Mathf.Max(rectTransform.sizeDelta.x, MinBubbleWidth);

            float preferredHeight = 0f;
            if (textLabel != null && !string.IsNullOrEmpty(text))
            {
                float measuredFontSize = textLabel.fontSize;
                textLabel.fontSize = textLabel.fontSizeMax;
                preferredHeight = textLabel.GetPreferredValues(text, width - BubblePadding, 0f).y;
                textLabel.fontSize = measuredFontSize;
            }

            // + BubbleBottomExtraInset: the outer rect's height budget must grow by the same
            // amount the bottom inset grew, or the extra inset just eats into the text's
            // available height instead of the bubble actually growing to clear the tail.
            return new Vector2(width, Mathf.Max(MinBubbleHeight, preferredHeight + BubblePadding + BubbleBottomExtraInset));
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / floatDuration);

            // Track pedestrian horizontal/vertical base position if active
            if (targetPedestrian != null)
            {
                lastKnownX = targetPedestrian.anchoredPosition.x;
                startY = targetPedestrian.anchoredPosition.y + verticalOffset;
            }

            // Drifting upward on Y axis
            float currentY = startY + (t * floatDistance);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(ClampXToParentBounds(lastKnownX), currentY);
            }

            // Fade out: hold full opacity through 70% of lifetime, then fade over the last 30%
            if (canvasGroup != null)
            {
                float alpha = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            if (elapsed >= floatDuration)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Extra breathing room kept between the bubble's own edge and the parent
        /// container's edge, so a clamped bubble doesn't sit flush against the screen edge --
        /// 2026-09-30, "cut off at the left edge" report: a zero-margin clamp reads as clipped
        /// even when technically inside bounds.</summary>
        private const float ScreenEdgeMarginPixels = 20f;

        /// <summary>
        /// Keeps the bubble's horizontal position fully inside its parent container (2026-09-17
        /// legibility pass): TrackPedestrian/Update previously copied the tracked pedestrian's
        /// raw anchoredPosition.x with no bound, so a bubble spawned on a pedestrian near either
        /// edge of the street (pedestrians walk from fully off-screen inward) could render
        /// partially or entirely outside the visible safe area -- unreadable no matter how good
        /// the font/contrast is. Uses this bubble's own current rect width (now dynamically sized
        /// by SetText/ComputeBubbleSize, never just the 300px floor) against the parent's width,
        /// so it degrades gracefully if the parent is ever narrower than the bubble itself
        /// (clamps to center rather than producing a negative range).
        /// </summary>
        private float ClampXToParentBounds(float desiredX)
        {
            if (rectTransform == null || !(rectTransform.parent is RectTransform parentRect))
            {
                return desiredX;
            }

            float halfBubbleWidth = rectTransform.rect.width * 0.5f;
            float halfParentWidth = parentRect.rect.width * 0.5f;
            float maxOffset = Mathf.Max(0f, halfParentWidth - halfBubbleWidth - ScreenEdgeMarginPixels);
            return Mathf.Clamp(desiredX, -maxOffset, maxOffset);
        }
    }
}
