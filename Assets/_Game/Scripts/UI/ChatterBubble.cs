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
                rectTransform.anchoredPosition = new Vector2(lastKnownX, startY);
            }
        }

        /// <summary>
        /// Sets the label text, asserts the readability floor on the font, and scales the
        /// bubble's lifetime with reading length. Presentation state is code-owned (Bible
        /// §8): the prefab serialized floatDuration 2s and TMP auto-sizing down to 14pt --
        /// unreadable, and dialogue is this game's identity. Duration: max(4s, 2s + 0.06s
        /// per character), so a typical bark holds 5-7s. Also asserts a dark background chip
        /// (0.06, 0.06, 0.1, 0.92), white label text, a 24pt font-size floor, and a 300x90
        /// minimum rect so text stops wrapping inside a tiny chip -- the prefab shipped with
        /// white-on-white text and an undersized rect, both unreadable in practice.
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
                rectTransform.sizeDelta = Vector2.Max(rectTransform.sizeDelta, new Vector2(300f, 90f));
            }

            int chars = string.IsNullOrEmpty(text) ? 0 : text.Length;
            floatDuration = Mathf.Max(4f, 2f + chars * 0.06f);
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
                rectTransform.anchoredPosition = new Vector2(lastKnownX, currentY);
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
    }
}
