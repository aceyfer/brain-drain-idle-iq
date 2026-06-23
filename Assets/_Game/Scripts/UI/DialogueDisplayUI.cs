using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Displays narrator dialogue lines from DialogueManager: slides the panel in from the
    /// left, holds for the line's display duration, then slides back out. Font size and letter
    /// spacing degrade as RebirthCount climbs (clean at 0, increasingly chaotic by 11+) --
    /// matching the same RebirthCount tiers DialogueManager already gates line *content* on
    /// (0 smug corporate / 1-2 buzzword salad / 3-5 broken grammar / 6-10 mostly enthusiasm /
    /// 11+ pre-collapse). Previously keyed off PlayerIQ directly, which only ever increases and
    /// couldn't represent a degrading tone -- this was a known, documented inconsistency
    /// between line content and line presentation; now both follow the same driver.
    /// </summary>
    public sealed class DialogueDisplayUI : MonoBehaviour
    {
        private const float SlideDurationSeconds = 0.3f;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TextMeshProUGUI lineText;
        [Tooltip("64x64 narrator portrait slot. No art assigned yet -- placeholder until assets arrive.")]
        [SerializeField] private Image avatarImage;

        [Header("RebirthCount Font Degradation")]
        [Tooltip("Optional. Swapped in at RebirthCount 6+ as a 'Comic Sans equivalent'. Left unassigned until a real font asset is sourced -- size/spacing degrade regardless.")]
        [SerializeField] private TMP_FontAsset lowIQFontAsset;

        private TMP_FontAsset defaultFontAsset;
        private Vector2 restingPosition;
        private Coroutine activeRoutine;
        private Outline glowOutline;
        private Coroutine glowRoutine;

        private void Awake()
        {
            if (panelRect != null)
            {
                restingPosition = panelRect.anchoredPosition;
                panelRect.gameObject.SetActive(false);

                // Find or add a dedicated Outline for the Hot Pink glow
                Outline[] outlines = panelRect.GetComponents<Outline>();
                foreach (Outline o in outlines)
                {
                    if (o.effectColor != Color.black)
                    {
                        glowOutline = o;
                    }
                }
                if (glowOutline == null)
                {
                    glowOutline = panelRect.gameObject.AddComponent<Outline>();
                    glowOutline.effectColor = new Color(1f, 0.078f, 0.576f, 0f); // Hot Pink, transparent initially
                    glowOutline.effectDistance = Vector2.zero;
                }
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

            ApplyRebirthDegradation();

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
        /// Anchor points keyed on RebirthCount, matching DialogueManager's own tone tiers:
        /// 0 -> size 28/spacing 0 (clean); 2 -> 32/5; 5 -> 35/10; 10 -> 38/15 (pre-collapse max,
        /// held flat past RebirthCount 11+). Linearly interpolated between anchors.
        /// </summary>
        private void ApplyRebirthDegradation()
        {
            int rebirthCount = RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0;

            float size;
            float spacing;

            if (rebirthCount <= 0)
            {
                size = 28f;
                spacing = 0f;
            }
            else if (rebirthCount <= 2)
            {
                float t = rebirthCount / 2f;
                size = Mathf.Lerp(28f, 32f, t);
                spacing = Mathf.Lerp(0f, 5f, t);
            }
            else if (rebirthCount <= 5)
            {
                float t = (rebirthCount - 2) / 3f;
                size = Mathf.Lerp(32f, 35f, t);
                spacing = Mathf.Lerp(5f, 10f, t);
            }
            else if (rebirthCount <= 10)
            {
                float t = (rebirthCount - 5) / 5f;
                size = Mathf.Lerp(35f, 38f, t);
                spacing = Mathf.Lerp(10f, 15f, t);
            }
            else
            {
                size = 38f;
                spacing = 15f;
            }

            lineText.fontSize = size;
            lineText.characterSpacing = spacing;
            lineText.font = rebirthCount >= 6 && lowIQFontAsset != null ? lowIQFontAsset : defaultFontAsset;
        }

        private IEnumerator GlowPulseRoutine()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                // Subtle pulse: 1.5s period, ping-pong between 2px and 8px thickness, alpha 0.3 to 1.0
                float t = Mathf.PingPong(elapsed * 2.5f, 1f);
                float dist = Mathf.Lerp(2f, 8f, t);
                float alpha = Mathf.Lerp(0.3f, 1.0f, t);

                if (glowOutline != null)
                {
                    glowOutline.effectDistance = new Vector2(dist, -dist);
                    Color col = glowOutline.effectColor;
                    col.a = alpha;
                    glowOutline.effectColor = col;
                }
                yield return null;
            }
        }

        private IEnumerator SlideRoutine(string line, float holdDuration)
        {
            lineText.text = line;
            panelRect.gameObject.SetActive(true);

            if (glowRoutine != null) StopCoroutine(glowRoutine);
            glowRoutine = StartCoroutine(GlowPulseRoutine());

            Vector2 offscreenLeft = restingPosition - new Vector2(panelRect.rect.width, 0f);

            yield return Slide(offscreenLeft, restingPosition, SlideDurationSeconds);
            yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
            yield return Slide(restingPosition, offscreenLeft, SlideDurationSeconds);

            if (glowRoutine != null)
            {
                StopCoroutine(glowRoutine);
                glowRoutine = null;
            }
            if (glowOutline != null)
            {
                glowOutline.effectDistance = Vector2.zero;
                Color col = glowOutline.effectColor;
                col.a = 0f;
                glowOutline.effectColor = col;
            }

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
