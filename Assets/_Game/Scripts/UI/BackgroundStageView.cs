using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Event-driven view that swaps the main Canvas background Image to match the active
    /// World Restoration stage (BG1–BG6). Never polls stage in Update (BD-1/BD-2).
    ///
    /// This is the backdrop path actually visible in Game view: the whole project renders
    /// through one Screen Space - Overlay Canvas with no compositing camera (see
    /// RainEffectView's class doc), and this Image is that Canvas's own full-screen opaque
    /// background, drawn on top of anything a camera renders. WorldRestorationManager also
    /// cross-fades a separate set of world-space SpriteRenderers
    /// (restorationStageObjects/fadeSpeed) keyed off the same stage events -- confirmed 2026-09-16
    /// that path is redundant dead work, permanently hidden behind this Image, not a second
    /// visible transition. Left alone here (no scene edit) rather than removed in this pass.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class BackgroundStageView : MonoBehaviour
    {
        [SerializeField] private Sprite[] stageSprites;

        [Tooltip("Total fade-through-dark duration (seconds) for a real stage crossing after boot. Boot/load always snaps instantly -- see hasResolvedInitialStage.")]
        [SerializeField] private float transitionSeconds = 0.6f;

        private Image backgroundImage;
        private Coroutine transitionRoutine;
        private bool hasResolvedInitialStage;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = false;
            // Stretch to fully fill this full-screen Image (0,0-1,1 anchors) instead of
            // fitting within it -- HUD text, the sidewalk, and pedestrians all render in
            // separate layers on top of this one, so a small aspect mismatch between the
            // stage art and the device screen is fine to absorb as stretch rather than
            // leaving visible letterbox gaps above/below the art.
            backgroundImage.preserveAspect = false;
            ApplyDefaultStageSprite();
        }

        private void ApplyDefaultStageSprite()
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (stageSprites == null || stageSprites.Length == 0 || stageSprites[0] == null)
            {
                Debug.LogWarning("[BackgroundStageView] stageSprites[0] is missing; background may flash white until Start.", this);
                return;
            }

            backgroundImage.sprite = stageSprites[0];
        }

        private void Start()
        {
            SubscribeToRestorationEvents();
            ApplyStageIndex(ResolveCurrentStageIndex(), snap: true);
            hasResolvedInitialStage = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromRestorationEvents();
        }

        private void SubscribeToRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
            manager.OnRestorationStageChanged += HandleRestorationStageChanged;
        }

        private void UnsubscribeFromRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            // hasResolvedInitialStage is already true by the time any real OnRestorationStageChanged
            // fires after Start() -- including WorldRestorationManager's own initial boot resolve,
            // whichever component's Start() runs first. The same-sprite no-op guard below is what
            // actually protects boot/load from a spurious fade if that first event lands here anyway.
            ApplyStageIndex(stage != null ? stage.stageIndex : 0, snap: !hasResolvedInitialStage);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStage?.stageIndex ?? 0;
        }

        private void ApplyStageIndex(int index, bool snap)
        {
            if (stageSprites == null || stageSprites.Length == 0 || backgroundImage == null)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, stageSprites.Length - 1);
            Sprite sprite = stageSprites[index];
            if (sprite == null || sprite == backgroundImage.sprite)
            {
                // Also the safety net for rapid/repeated stage-change calls that resolve to the
                // sprite already on screen (or already mid-transition toward) -- no-op instead of
                // restarting a fade to where we already are.
                return;
            }

            if (snap || !gameObject.activeInHierarchy)
            {
                if (transitionRoutine != null)
                {
                    StopCoroutine(transitionRoutine);
                    transitionRoutine = null;
                }

                backgroundImage.sprite = sprite;
                SetAlpha(1f);
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(FadeThroughDark(sprite));
        }

        /// <summary>
        /// Restrained fade-through-dark: fades this Image's own alpha to 0 (revealing whatever
        /// sits behind the Overlay Canvas, which reads as a brief dip to black since nothing else
        /// occupies this full-screen layer), swaps the sprite while invisible, then fades back to
        /// 1. Symmetric halves so total time matches transitionSeconds regardless of the alpha this
        /// started from (handles a fade restarted mid-flight from a rapid repeat stage change).
        /// </summary>
        private IEnumerator FadeThroughDark(Sprite nextSprite)
        {
            float half = Mathf.Max(0.05f, transitionSeconds * 0.5f);

            float t = 0f;
            float startAlpha = backgroundImage.color.a;
            while (t < half)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / half)));
                yield return null;
            }

            SetAlpha(0f);
            backgroundImage.sprite = nextSprite;

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / half)));
                yield return null;
            }

            SetAlpha(1f);
            transitionRoutine = null;
        }

        private void SetAlpha(float alpha)
        {
            Color c = backgroundImage.color;
            c.a = alpha;
            backgroundImage.color = c;
        }
    }
}
