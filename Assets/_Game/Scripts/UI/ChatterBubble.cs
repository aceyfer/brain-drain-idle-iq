using UnityEngine;
using TMPro;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays a temporary floating UI speech bubble that drifts upward and fades out,
    /// tracking a target pedestrian's X coordinate if available.
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
        /// Sets the string text of the label.
        /// </summary>
        public void SetText(string text)
        {
            if (textLabel != null)
            {
                textLabel.text = text;
            }
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

            // Fade out
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            if (elapsed >= floatDuration)
            {
                Destroy(gameObject);
            }
        }
    }
}
