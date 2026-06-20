using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays narrator dialogue lines from DialogueManager: slides the panel in from the
    /// left, holds for the line's display duration, then slides back out. Font size and letter
    /// spacing degrade as PlayerIQ drops (clean at 100, increasingly chaotic by 20 and below).
    /// </summary>
    public sealed class DialogueDisplayUI : MonoBehaviour
    {
        private const float SlideDurationSeconds = 0.3f;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TextMeshProUGUI lineText;
        [Tooltip("64x64 narrator portrait slot. No art assigned yet -- placeholder until assets arrive.")]
        [SerializeField] private Image avatarImage;

        [Header("IQ Font Degradation")]
        [Tooltip("Optional. Swapped in below IQ 50 as a 'Comic Sans equivalent'. Left unassigned until a real font asset is sourced -- size/spacing degrade regardless.")]
        [SerializeField] private TMP_FontAsset lowIQFontAsset;

        private TMP_FontAsset defaultFontAsset;
        private Vector2 restingPosition;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (panelRect != null)
            {
                restingPosition = panelRect.anchoredPosition;
                panelRect.gameObject.SetActive(false);
            }

            if (lineText != null)
            {
                defaultFontAsset = lineText.font;
            }
        }

        private void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueLine.AddListener(HandleDialogueLine);
            }

            if (COGSPortraitController.Instance != null)
            {
                COGSPortraitController.Instance.OnStageChanged.AddListener(HandleStageChanged);

                // Covers both Start() orderings: if COGSPortraitController already resolved its
                // initial stage before this ran, apply it now. If not, the listener above will
                // catch it once COGSPortraitController.Start() runs (still within Awake-then-
                // Start ordering guarantees for the same frame).
                if (COGSPortraitController.Instance.CurrentStage != null)
                {
                    HandleStageChanged(COGSPortraitController.Instance.CurrentStage);
                }
            }
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueLine.RemoveListener(HandleDialogueLine);
            }

            if (COGSPortraitController.Instance != null)
            {
                COGSPortraitController.Instance.OnStageChanged.RemoveListener(HandleStageChanged);
            }
        }

        private void HandleStageChanged(COGSStage stage)
        {
            if (avatarImage != null && stage != null)
            {
                avatarImage.sprite = stage.portraitSprite;
            }
        }

        private void HandleDialogueLine(string line)
        {
            if (panelRect == null || lineText == null)
            {
                return;
            }

            ApplyIQDegradation();

            float holdDuration = DialogueManager.Instance != null
                ? DialogueManager.Instance.CurrentDisplayDurationSeconds
                : 3f;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            activeRoutine = StartCoroutine(SlideRoutine(line, holdDuration));
        }

        /// <summary>
        /// Anchor points: IQ&gt;=100 -> size 28/spacing 0 (clean); IQ 50 -> size 32/spacing 5
        /// (slightly tracked-out); IQ&lt;=20 -> size 38/spacing 15 (wide-tracked). Linearly
        /// interpolated between anchors, clamped at the ends.
        /// </summary>
        private void ApplyIQDegradation()
        {
            float iq = PlayerIQManager.Instance != null ? PlayerIQManager.Instance.PlayerIQ : 100f;

            float size;
            float spacing;

            if (iq >= 100f)
            {
                size = 28f;
                spacing = 0f;
            }
            else if (iq >= 50f)
            {
                float t = (iq - 50f) / 50f;
                size = Mathf.Lerp(32f, 28f, t);
                spacing = Mathf.Lerp(5f, 0f, t);
            }
            else if (iq >= 20f)
            {
                float t = (iq - 20f) / 30f;
                size = Mathf.Lerp(38f, 32f, t);
                spacing = Mathf.Lerp(15f, 5f, t);
            }
            else
            {
                size = 38f;
                spacing = 15f;
            }

            lineText.fontSize = size;
            lineText.characterSpacing = spacing;
            lineText.font = iq < 50f && lowIQFontAsset != null ? lowIQFontAsset : defaultFontAsset;
        }

        private IEnumerator SlideRoutine(string line, float holdDuration)
        {
            lineText.text = line;
            panelRect.gameObject.SetActive(true);

            Vector2 offscreenLeft = restingPosition - new Vector2(panelRect.rect.width, 0f);

            yield return Slide(offscreenLeft, restingPosition, SlideDurationSeconds);
            yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
            yield return Slide(restingPosition, offscreenLeft, SlideDurationSeconds);

            panelRect.gameObject.SetActive(false);
            panelRect.anchoredPosition = restingPosition;
            activeRoutine = null;
        }

        private IEnumerator Slide(Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            panelRect.anchoredPosition = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }

            panelRect.anchoredPosition = to;
        }
    }
}
