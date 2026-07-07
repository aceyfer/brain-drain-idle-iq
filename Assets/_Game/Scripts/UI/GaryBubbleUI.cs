using System.Collections;
using UnityEngine;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays Gary's assembled barks in a panel that slides in from the right side of the
    /// screen — distinct from COGS's DialogueDisplayUI (slides from left, COGS portrait, narrator
    /// pool). This is a completely separate display path; DialogueManager is untouched.
    /// Wire bubblePanel and barkText in Inspector, then place this component on an always-active
    /// parent GameObject so Awake subscriptions fire before any barks can arrive.
    /// </summary>
    public sealed class GaryBubbleUI : MonoBehaviour
    {
        private const float SlideDuration = 0.3f;
        private const float HoldDuration  = 3.5f;

        [SerializeField] private RectTransform bubblePanel;
        [SerializeField] private TextMeshProUGUI barkText;
        [Tooltip("Optional speaker nameplate. Set label text to 'Gary' in Inspector.")]
        [SerializeField] private TextMeshProUGUI speakerLabel;

        private Vector2 restingPos;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (bubblePanel != null)
            {
                restingPos = bubblePanel.anchoredPosition;
                // Park offscreen right; SetActive(false) so it's invisible until first bark
                bubblePanel.anchoredPosition = restingPos + new Vector2(bubblePanel.rect.width + 20f, 0f);
                bubblePanel.gameObject.SetActive(false);
            }

            // Subscribe before any potential early barks (mirrors DialogueDisplayUI.Awake pattern)
            if (GaryBarkManager.Instance != null)
            {
                GaryBarkManager.Instance.OnGaryBark -= HandleGaryBark;
                GaryBarkManager.Instance.OnGaryBark += HandleGaryBark;
            }
        }

        private void OnDestroy()
        {
            GaryBarkManager manager = FindAnyObjectByType<GaryBarkManager>();
            if (manager != null) manager.OnGaryBark -= HandleGaryBark;
        }

        private void HandleGaryBark(string bark)
        {
            if (bubblePanel == null || barkText == null) return;

            if (activeRoutine != null) StopCoroutine(activeRoutine);

            bubblePanel.gameObject.SetActive(true);
            barkText.text = bark;
            activeRoutine = StartCoroutine(SlideRoutine());
        }

        private IEnumerator SlideRoutine()
        {
            Vector2 offscreenRight = restingPos + new Vector2(bubblePanel.rect.width + 20f, 0f);

            yield return Slide(offscreenRight, restingPos, SlideDuration);
            yield return new WaitForSeconds(HoldDuration);
            yield return Slide(restingPos, offscreenRight, SlideDuration);

            activeRoutine = null;
            bubblePanel.gameObject.SetActive(false);
        }

        private IEnumerator Slide(Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            bubblePanel.anchoredPosition = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bubblePanel.anchoredPosition = Vector2.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            bubblePanel.anchoredPosition = to;
        }
    }
}
