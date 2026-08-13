using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Singleton home for all core UI feedback animations (tap squash/stretch, idle breathing,
    /// goo splat particles, affordable-slot pulse, popup spawn shake). Most effects are still
    /// hand-rolled coroutines -- simple one-shots/loops don't need a tweening engine -- but
    /// PlayButtonPunch, PlayFloatingRewardText, PlaySlide, and PlayBrainPowerCounterPunch use
    /// DOTween (now present at Assets/Plugins/Demigiant/DOTween) for smoother curves on the
    /// effects that benefited most. Other scripts trigger effects via the static wrapper methods, e.g.
    /// AnimationController.PlayTapAnim(transform).
    /// </summary>
    public sealed class AnimationController : MonoBehaviour
    {
        private static Sprite cachedSplatSprite;
        private static Sprite[] cachedSplatSprites;

        [SerializeField] private Sprite _neonRingSprite;
        [SerializeField] private Sprite _radialGlowSprite;
        [SerializeField] private Sprite[] _gooSplatSprites;
        [SerializeField] private Material _floatingTextMaterial;

        public static AnimationController Instance { get; private set; }

        private static bool isQuitting;
        private readonly List<GameObject> _trackedVfxObjects = new();

        private readonly Dictionary<Transform, Coroutine> tapAnimCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> breathingCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> boredFidgetCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> excitedBounceCoroutines = new();
        private readonly Dictionary<Transform, Tween> buttonPunchTweens = new();
        private readonly Dictionary<RectTransform, Coroutine> affordablePulseCoroutines = new();
        private readonly Dictionary<RectTransform, Coroutine> denialShakeCoroutines = new();
        private readonly Dictionary<TextMeshProUGUI, Coroutine> textFlashCoroutines = new();
        private readonly Dictionary<TextMeshProUGUI, Color> textFlashBaseColors = new();
        private readonly Dictionary<RectTransform, Tween> slideTweens = new();
        private readonly Dictionary<TextMeshProUGUI, Color> brainPowerCounterBaseColors = new();

        private const int MaxConcurrentExtractionBursts = 4;
        private int activeExtractionBursts;

        private sealed class ExtractionBurstState
        {
            public int RemainingParticles;
            public bool ArrivalFired;
            public Action OnArrival;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            isQuitting = true;
            StopAllCoroutines();
            foreach (var go in _trackedVfxObjects)
            {
                if (go != null)
                {
                    KillVfxTweens(go);
                    Destroy(go);
                }
            }
            _trackedVfxObjects.Clear();
            if (Instance == this) Instance = null;
        }

        // Kills DOTween tweens on all components that VFX spawners use as tween targets.
        // go.transform covers RectTransform (DOAnchorPos, DOScale).
        // TextMeshProUGUI covers label.DOFade. Image covers future color tweens.
        // DOTween.Kill(null) is a documented safe no-op.
        private static void KillVfxTweens(GameObject go)
        {
            go.transform.DOKill();
            DOTween.Kill(go.GetComponent<TextMeshProUGUI>());
            DOTween.Kill(go.GetComponent<Image>());
        }

        // Normal-path cleanup: unregister from tracking, kill tweens, destroy.
        private void DestroyTrackedVfx(GameObject go)
        {
            if (go == null) return;
            _trackedVfxObjects.Remove(go);
            KillVfxTweens(go);
            Destroy(go);
        }

        private static AnimationController EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            if (isQuitting)
            {
                return null;
            }

            var hostObject = new GameObject("AnimationController (Auto)");
            return hostObject.AddComponent<AnimationController>();
        }

        // ----- Tap squash & stretch -----------------------------------------------------

        /// <summary>
        /// Plays the tap squash/stretch/settle sequence on target.localScale.
        /// Total duration 0.24s: 0.06s squash (ease-out) -> 0.08s bounce (ease-out) -> 0.10s settle (ease-in-out).
        /// </summary>
        public static void PlayTapAnim(Transform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.tapAnimCoroutines, target, controller.TapAnimRoutine(target));
        }

        private IEnumerator TapAnimRoutine(Transform target)
        {
            Vector3 baseScale = Vector3.one;
            yield return ScaleOverTime(target, baseScale, new Vector3(1.15f, 0.87f, 1f), 0.06f, EaseOutQuad);
            yield return ScaleOverTime(target, target.localScale, new Vector3(0.93f, 1.08f, 1f), 0.08f, EaseOutQuad);
            yield return ScaleOverTime(target, target.localScale, baseScale, 0.10f, EaseInOutQuad);
        }

        // ----- Idle breathing -------------------------------------------------------------

        /// <summary>Starts an infinite 1.0 &lt;-&gt; 1.03 sine breathing loop (1.4s each way) on target.localScale.</summary>
        public static void PlayIdleBreathing(Transform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.breathingCoroutines, target, controller.IdleBreathingRoutine(target));
        }

        /// <summary>Stops the breathing loop started by PlayIdleBreathing and resets scale to 1.</summary>
        public static void StopIdleBreathing(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.localScale = Vector3.one;

            if (Instance == null)
            {
                return;
            }

            if (Instance.breathingCoroutines.TryGetValue(target, out Coroutine running) && running != null)
            {
                Instance.StopCoroutine(running);
            }

            Instance.breathingCoroutines.Remove(target);
        }

        private IEnumerator IdleBreathingRoutine(Transform target)
        {
            const float halfDuration = 1.4f;

            while (true)
            {
                yield return ScaleOverTime(target, Vector3.one, Vector3.one * 1.03f, halfDuration, EaseInOutSine);
                yield return ScaleOverTime(target, Vector3.one * 1.03f, Vector3.one, halfDuration, EaseInOutSine);
            }
        }

        // ----- Bored fidget (Player Character) ---------------------------------------------

        /// <summary>Starts an infinite +/-4 degree Z-tilt wobble (0.45s each way) on target.localRotation.</summary>
        public static void PlayBoredFidget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.boredFidgetCoroutines, target, controller.BoredFidgetRoutine(target));
        }

        /// <summary>Stops the wobble started by PlayBoredFidget and resets rotation to identity.</summary>
        public static void StopBoredFidget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.localRotation = Quaternion.identity;

            if (Instance == null)
            {
                return;
            }

            if (Instance.boredFidgetCoroutines.TryGetValue(target, out Coroutine running) && running != null)
            {
                Instance.StopCoroutine(running);
            }

            Instance.boredFidgetCoroutines.Remove(target);
        }

        private IEnumerator BoredFidgetRoutine(Transform target)
        {
            const float halfDuration = 0.45f;
            const float maxTiltDegrees = 4f;

            while (true)
            {
                yield return RotateOverTime(target, -maxTiltDegrees, maxTiltDegrees, halfDuration, EaseInOutSine);
                yield return RotateOverTime(target, maxTiltDegrees, -maxTiltDegrees, halfDuration, EaseInOutSine);
            }
        }

        // ----- Excited bounce (Player Character) --------------------------------------------

        /// <summary>Starts an infinite 1.0 &lt;-&gt; 1.12 scale bounce loop (0.18s each way) on target.localScale.</summary>
        public static void PlayExcitedBounce(Transform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.excitedBounceCoroutines, target, controller.ExcitedBounceRoutine(target));
        }

        /// <summary>Stops the bounce started by PlayExcitedBounce and resets scale to 1.</summary>
        public static void StopExcitedBounce(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.localScale = Vector3.one;

            if (Instance == null)
            {
                return;
            }

            if (Instance.excitedBounceCoroutines.TryGetValue(target, out Coroutine running) && running != null)
            {
                Instance.StopCoroutine(running);
            }

            Instance.excitedBounceCoroutines.Remove(target);
        }

        private IEnumerator ExcitedBounceRoutine(Transform target)
        {
            const float halfDuration = 0.18f;

            while (true)
            {
                yield return ScaleOverTime(target, Vector3.one, Vector3.one * 1.12f, halfDuration, EaseOutQuad);
                yield return ScaleOverTime(target, target.localScale, Vector3.one, halfDuration, EaseInOutQuad);
            }
        }

        // ----- Tap button punch (separate from the character's own tap squash/stretch) ------

        /// <summary>Plays a quick DOTween scale punch (DOPunchScale, 0.2 punch, 0.1s, vibrato 1, elasticity 0.5) on target.localScale.</summary>
        public static void PlayButtonPunch(Transform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            KillExisting(controller.buttonPunchTweens, target);

            target.localScale = Vector3.one;
            controller.buttonPunchTweens[target] = target.DOPunchScale(Vector3.one * 0.2f, 0.1f, 1, 0.5f);
        }

        // ----- Floating reward text ----------------------------------------------------------

        private static readonly Color FloatingRewardTextColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);

        /// <summary>
        /// Spawns transient text inside parent at screenPosition that rises ~70px and fades out
        /// over 0.8s, then self-destructs. Mirrors PlaySplatParticles' screen-to-local-point and
        /// transient-GameObject pattern.
        /// </summary>
        public static void PlayFloatingRewardText(string text, Vector2 screenPosition, RectTransform parent)
        {
            if (parent == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            EnsureInstance()?.SpawnFloatingRewardText(text, screenPosition, parent);
        }

        private void SpawnFloatingRewardText(string text, Vector2 screenPosition, RectTransform parent)
        {
            if (isQuitting) return;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera screenCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, screenCamera, out Vector2 localPoint);

            GameObject textObject = new GameObject("FloatingRewardText", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            _trackedVfxObjects.Add(textObject);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(220f, 50f);
            textRect.anchoredPosition = localPoint;

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 48f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = FloatingRewardTextColor;
            label.raycastTarget = false;
            if (_floatingTextMaterial != null)
            {
                label.fontSharedMaterial = _floatingTextMaterial;
            }

            const float lifetime = 0.8f;
            const float riseDistance = 70f;

            textRect.DOAnchorPos(localPoint + new Vector2(0f, riseDistance), lifetime).SetEase(Ease.OutQuad);
            label.DOFade(0f, lifetime).SetEase(Ease.Linear).OnComplete(() =>
            {
                if (textRect != null)
                {
                    DestroyTrackedVfx(textRect.gameObject);
                }
            });
        }

        // ----- Text color flash (e.g. HUDController's IQ readout on tap) --------------------

        private static readonly Color TextFlashYellow = Color.yellow;

        /// <summary>Briefly flashes text.color to yellow and back (0.1s in, 0.2s out), then restores its original color exactly.</summary>
        public static void PlayIQFlash(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            EnsureInstance()?.PlayTextFlash(text, TextFlashYellow);
        }

        private void PlayTextFlash(TextMeshProUGUI text, Color flashColor)
        {
            if (!textFlashBaseColors.TryGetValue(text, out Color baseColor))
            {
                baseColor = text.color;
                textFlashBaseColors[text] = baseColor;
            }

            if (textFlashCoroutines.TryGetValue(text, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
            }

            // Always restart from the true base color so rapid repeated flashes can't drift.
            text.color = baseColor;
            textFlashCoroutines[text] = StartCoroutine(TextFlashRoutine(text, baseColor, flashColor));
        }

        private static IEnumerator TextFlashRoutine(TextMeshProUGUI text, Color baseColor, Color flashColor)
        {
            const float toFlashDuration = 0.1f;
            const float backDuration = 0.2f;

            yield return ColorOverTime(text, baseColor, flashColor, toFlashDuration);
            yield return ColorOverTime(text, flashColor, baseColor, backDuration);
        }

        private static IEnumerator ColorOverTime(TextMeshProUGUI text, Color from, Color to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (text == null)
                {
                    yield break;
                }

                text.color = Color.LerpUnclamped(from, to, t);
                yield return null;
            }

            if (text != null)
            {
                text.color = to;
            }
        }

        // ----- Brain Power counter punch (tap-extraction particle arrival reaction) ---------

        private static readonly Color BrainPowerPunchFlashColor = new Color(0.6f, 0.9f, 1f, 1f);

        /// <summary>
        /// Subtle DOTween scale punch + brief brightness flash on counterText -- fired once per
        /// tap, when the tap-extraction particles land. Same cached-base-color anti-drift idea as
        /// PlayIQFlash (always restarts from the true base color so rapid taps can't drift it),
        /// via DOTween instead of a coroutine, with a smaller punch since this fires on every tap.
        /// </summary>
        public static void PlayBrainPowerCounterPunch(TextMeshProUGUI counterText)
        {
            if (counterText == null)
            {
                return;
            }

            EnsureInstance()?.DoBrainPowerCounterPunch(counterText);
        }

        private void DoBrainPowerCounterPunch(TextMeshProUGUI counterText)
        {
            if (!brainPowerCounterBaseColors.TryGetValue(counterText, out Color baseColor))
            {
                baseColor = counterText.color;
                brainPowerCounterBaseColors[counterText] = baseColor;
            }

            counterText.transform.DOKill();
            DOTween.Kill(counterText);

            counterText.transform.localScale = Vector3.one;
            counterText.color = baseColor;

            counterText.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 1, 0.5f);

            Sequence flash = DOTween.Sequence();
            flash.Append(counterText.DOColor(BrainPowerPunchFlashColor, 0.08f));
            flash.Append(counterText.DOColor(baseColor, 0.14f));
            flash.SetTarget(counterText);
        }

        // ----- Restoration vessel: plunger move + milestone surge --------------------------

        private readonly Dictionary<RectTransform, Tween> plungerMoveTweens = new();
        private readonly Dictionary<Image, Sequence> restorationSurgeSequences = new();

        private static readonly Color RestorationSurgeFlashColor = new Color(0.85f, 0.98f, 1f, 1f);

        /// <summary>Smoothly tweens plunger's anchoredPosition.x to targetX (DOAnchorPosX, OutQuad) -- called on every restoration progress update, alongside restorationFillImage.fillAmount, so the plunger travels rather than snaps.</summary>
        public static void PlayPlungerMove(RectTransform plunger, float targetX, float duration = 0.3f)
        {
            if (plunger == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            if (controller == null)
            {
                return;
            }

            KillExisting(controller.plungerMoveTweens, plunger);
            controller.plungerMoveTweens[plunger] = plunger.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// One-shot milestone celebration: snaps the fill to full and flashes it, holds briefly,
        /// jolts the plunger in place, then settles fill/color/plunger back to the real values for
        /// the newly-entered stage segment (passed in by the caller, since this method has no
        /// knowledge of WorldRestorationManager or track-width math -- it only plays the shapes
        /// it's given). Overfillplungerx is the plunger's full-track-width X (the "overfill" snap
        /// target), distinct from settledPlungerX (where it settles back to for the new segment).
        /// </summary>
        public static void PlayRestorationMilestoneSurge(Image fillImage, RectTransform plunger, float overfillPlungerX, Color settledColor, float settledFillAmount, float settledPlungerX)
        {
            if (fillImage == null)
            {
                return;
            }

            EnsureInstance()?.DoRestorationMilestoneSurge(fillImage, plunger, overfillPlungerX, settledColor, settledFillAmount, settledPlungerX);
        }

        private void DoRestorationMilestoneSurge(Image fillImage, RectTransform plunger, float overfillPlungerX, Color settledColor, float settledFillAmount, float settledPlungerX)
        {
            if (restorationSurgeSequences.TryGetValue(fillImage, out Sequence existingSeq) && existingSeq != null && existingSeq.IsActive())
            {
                existingSeq.Kill();
            }

            if (plunger != null)
            {
                KillExisting(plungerMoveTweens, plunger);
                plunger.DOKill();
                plunger.anchoredPosition = new Vector2(overfillPlungerX, plunger.anchoredPosition.y);
            }

            fillImage.fillAmount = 1f;
            fillImage.color = RestorationSurgeFlashColor;

            const float preJoltPause = 0.05f;
            const float joltDuration = 0.25f;
            const float settleDuration = 0.35f;

            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(preJoltPause);
            if (plunger != null)
            {
                seq.Append(plunger.transform.DOPunchScale(Vector3.one * 0.25f, joltDuration, 4, 0.6f));
            }
            else
            {
                seq.AppendInterval(joltDuration);
            }

            seq.Append(fillImage.DOFillAmount(settledFillAmount, settleDuration).SetEase(Ease.InOutQuad));
            seq.Join(fillImage.DOColor(settledColor, settleDuration));
            if (plunger != null)
            {
                seq.Join(plunger.DOAnchorPosX(settledPlungerX, settleDuration).SetEase(Ease.InOutQuad));
            }
            seq.SetTarget(fillImage);

            restorationSurgeSequences[fillImage] = seq;
        }

        // ----- Goo splat particles ---------------------------------------------------------

        /// <summary>
        /// Spawns 4-6 short-lived placeholder "goo splat" particles inside parent, radiating out
        /// from screenPosition. Each particle travels 40-80px over a 0.3s lifetime and fades out
        /// over the final 0.1s.
        /// </summary>
        public static void PlaySplatParticles(Vector2 screenPosition, RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            EnsureInstance()?.SpawnSplatParticles(screenPosition, parent);
        }

        private void SpawnSplatParticles(Vector2 screenPosition, RectTransform parent)
        {
            if (isQuitting) return;
            int count = UnityEngine.Random.Range(4, 7);

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera screenCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, screenCamera, out Vector2 localPoint);

            for (int i = 0; i < count; i++)
            {
                GameObject particleObject = new GameObject("GooSplatParticle", typeof(RectTransform), typeof(Image));
                particleObject.transform.SetParent(parent, false);
                _trackedVfxObjects.Add(particleObject);

                RectTransform particleRect = particleObject.GetComponent<RectTransform>();
                // Make particles slightly larger so the detailed shapes are easily visible
                particleRect.sizeDelta = new Vector2(24f, 24f);
                particleRect.anchoredPosition = localPoint;

                Image particleImage = particleObject.GetComponent<Image>();
                particleImage.sprite = GetRandomSplatSprite();
                particleImage.raycastTarget = false;

                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(40f, 80f);
                Vector2 destination = localPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                StartCoroutine(SplatParticleRoutine(particleRect, particleImage, localPoint, destination));
            }
        }

        private IEnumerator SplatParticleRoutine(RectTransform particleRect, Image particleImage, Vector2 from, Vector2 to)
        {
            const float lifetime = 0.3f;
            const float fadeStart = 0.2f;
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                particleRect.anchoredPosition = Vector2.LerpUnclamped(from, to, EaseOutQuad(t));

                if (elapsed >= fadeStart)
                {
                    float fadeT = Mathf.Clamp01((elapsed - fadeStart) / (lifetime - fadeStart));
                    Color color = particleImage.color;
                    color.a = Mathf.Lerp(1f, 0f, fadeT);
                    particleImage.color = color;
                }

                yield return null;
            }

            if (particleRect != null)
            {
                DestroyTrackedVfx(particleRect.gameObject);
            }
        }

        // ----- Tap extraction particles (harvest rises from the tap point to the BP counter) --

        private static readonly Color ExtractionBlueColor = new Color(0.25f, 0.75f, 1f, 1f);
        private static readonly Color ExtractionBlackColor = new Color(0.05f, 0.05f, 0.08f, 1f);

        /// <summary>
        /// Spawns 6-10 small particles around screenPosition inside parent -- ~70% glowing blue,
        /// ~30% black wisps. Each hangs briefly with a slight outward drift, then eases in toward
        /// destination's current position. Black particles fade out at 60% of the way there and
        /// never arrive; only blue arrives, firing onArrival once, on the first one to land.
        /// Reuses GetGlowSprite() (tinted per-tone) rather than a new procedural sprite -- same
        /// soft radial shape works for both a glow and a wisp, just different color/size.
        /// Capped at MaxConcurrentExtractionBursts concurrent bursts; over the cap, the spawn is
        /// skipped outright (not queued) so tap-spam can't accumulate GameObjects.
        /// </summary>
        public static void PlayExtractionParticles(Vector2 screenPosition, RectTransform parent, RectTransform destination, Action onArrival)
        {
            if (parent == null || destination == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            if (controller == null)
            {
                return;
            }

            if (controller.activeExtractionBursts >= MaxConcurrentExtractionBursts)
            {
                return;
            }

            controller.SpawnExtractionParticles(screenPosition, parent, destination, onArrival);
        }

        private void SpawnExtractionParticles(Vector2 screenPosition, RectTransform parent, RectTransform destination, Action onArrival)
        {
            if (isQuitting) return;

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera screenCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, screenCamera, out Vector2 originLocalPoint);

            // Convert destination's current world position the same way the tap origin was converted,
            // so both points land correctly in parent's local space regardless of hierarchy depth.
            Vector2 destinationScreenPoint = RectTransformUtility.WorldToScreenPoint(screenCamera, destination.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, destinationScreenPoint, screenCamera, out Vector2 destinationLocalPoint);

            int count = UnityEngine.Random.Range(6, 11);
            activeExtractionBursts++;

            var burst = new ExtractionBurstState { RemainingParticles = count, ArrivalFired = false, OnArrival = onArrival };

            for (int i = 0; i < count; i++)
            {
                bool isBlue = UnityEngine.Random.value < 0.7f;

                Vector2 direction = UnityEngine.Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector2.up;
                }
                direction.Normalize();

                float radius = UnityEngine.Random.Range(30f, 50f);
                Vector2 spawnPoint = originLocalPoint + direction * radius;
                Vector2 driftPoint = spawnPoint + direction * 10f;
                float startDelay = UnityEngine.Random.Range(0f, 0.08f);

                GameObject particleObject = new GameObject(isBlue ? "ExtractionParticleBlue" : "ExtractionParticleBlack", typeof(RectTransform), typeof(Image));
                particleObject.transform.SetParent(parent, false);
                _trackedVfxObjects.Add(particleObject);

                RectTransform particleRect = particleObject.GetComponent<RectTransform>();
                particleRect.sizeDelta = isBlue ? new Vector2(20f, 20f) : new Vector2(16f, 16f);
                particleRect.anchoredPosition = spawnPoint;

                Image particleImage = particleObject.GetComponent<Image>();
                particleImage.sprite = GetGlowSprite();
                particleImage.color = isBlue ? ExtractionBlueColor : ExtractionBlackColor;
                particleImage.raycastTarget = false;

                StartCoroutine(ExtractionParticleRoutine(particleRect, particleImage, spawnPoint, driftPoint, destinationLocalPoint, isBlue, startDelay, burst));
            }
        }

        private IEnumerator ExtractionParticleRoutine(RectTransform particleRect, Image particleImage, Vector2 spawnPoint, Vector2 driftPoint, Vector2 destinationPoint, bool isBlue, float startDelay, ExtractionBurstState burst)
        {
            const float hangDuration = 0.1f;
            const float travelDuration = 0.45f;
            const float blackStopFraction = 0.6f;

            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            float hangElapsed = 0f;
            while (hangElapsed < hangDuration)
            {
                hangElapsed += Time.deltaTime;
                if (particleRect == null)
                {
                    CompleteExtractionParticle(burst);
                    yield break;
                }

                float t = EaseOutQuad(Mathf.Clamp01(hangElapsed / hangDuration));
                particleRect.anchoredPosition = Vector2.LerpUnclamped(spawnPoint, driftPoint, t);
                yield return null;
            }

            Vector2 travelEnd = isBlue ? destinationPoint : Vector2.LerpUnclamped(driftPoint, destinationPoint, blackStopFraction);

            float travelElapsed = 0f;
            while (travelElapsed < travelDuration)
            {
                travelElapsed += Time.deltaTime;
                if (particleRect == null || particleImage == null)
                {
                    CompleteExtractionParticle(burst);
                    yield break;
                }

                float eased = EaseInQuad(Mathf.Clamp01(travelElapsed / travelDuration));
                particleRect.anchoredPosition = Vector2.LerpUnclamped(driftPoint, travelEnd, eased);

                if (!isBlue)
                {
                    Color color = particleImage.color;
                    color.a = Mathf.Lerp(1f, 0f, eased);
                    particleImage.color = color;
                }

                yield return null;
            }

            if (particleRect != null)
            {
                particleRect.anchoredPosition = travelEnd;
            }

            if (isBlue && !burst.ArrivalFired)
            {
                burst.ArrivalFired = true;
                burst.OnArrival?.Invoke();
            }

            CompleteExtractionParticle(burst);
            if (particleRect != null)
            {
                DestroyTrackedVfx(particleRect.gameObject);
            }
        }

        private void CompleteExtractionParticle(ExtractionBurstState burst)
        {
            burst.RemainingParticles--;
            if (burst.RemainingParticles <= 0)
            {
                activeExtractionBursts = Mathf.Max(0, activeExtractionBursts - 1);
            }
        }

        // ----- Touch Ripple (Futuristic Neon Ring + Glow) ---------------------------

        private static Sprite cachedRingSprite;
        private static Sprite cachedGlowSprite;

        /// <summary>
        /// Spawns a futuristic expanding ring and a soft glow at the click position, tinted
        /// extraction-blue (matches AnimationController.ExtractionBlueColor / the RESTORATION
        /// fill) -- was neon green until the tap-feedback palette shifted to blue.
        /// </summary>
        public static void PlayTouchRipple(Vector2 screenPosition, RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            EnsureInstance()?.SpawnTouchRipple(screenPosition, parent);
        }

        private void SpawnTouchRipple(Vector2 screenPosition, RectTransform parent)
        {
            if (isQuitting) return;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera screenCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, screenCamera, out Vector2 localPoint);

            // 1. Create Glowing Core
            GameObject glowGo = new GameObject("TouchGlow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(parent, false);
            _trackedVfxObjects.Add(glowGo);
            RectTransform glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(120f, 120f);
            glowRect.anchoredPosition = localPoint;
            glowRect.localScale = new Vector3(0.1f, 0.1f, 1f);

            Image glowImg = glowGo.GetComponent<Image>();
            glowImg.sprite = GetGlowSprite();
            // Was neon green (0.224, 1, 0.078) -- retinted to extraction blue, matching
            // ExtractionBlueColor / the RESTORATION fill. GetGlowSprite() loads RadialGlow.png,
            // which is neutral white pixel data (flattened from the old baked-green art) with
            // only alpha carrying the falloff shape -- this tint is the sprite's sole source of
            // color now, so it renders as clean blue, not the dark/muddy result the old
            // baked-green sprite would have produced.
            glowImg.color = new Color(0.25f, 0.75f, 1f, 0.6f);
            glowImg.raycastTarget = false;

            // 2. Create Expanding Ring
            GameObject ringGo = new GameObject("TouchRing", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(parent, false);
            _trackedVfxObjects.Add(ringGo);
            RectTransform ringRect = ringGo.GetComponent<RectTransform>();
            ringRect.sizeDelta = new Vector2(150f, 150f);
            ringRect.anchoredPosition = localPoint;
            ringRect.localScale = new Vector3(0.1f, 0.1f, 1f);

            Image ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = GetRingSprite();
            // Was neon green (0.224, 1, 0.078) -- retinted to extraction blue. GetRingSprite()'s
            // NeonRing.png is a neutral shape (no baked color), so this one tints cleanly.
            ringImg.color = new Color(0.25f, 0.75f, 1f, 1f);
            ringImg.raycastTarget = false;

            StartCoroutine(TouchRippleRoutine(ringRect, ringImg, glowRect, glowImg));
        }

        private IEnumerator TouchRippleRoutine(RectTransform ringRect, Image ringImage, RectTransform glowRect, Image glowImage)
        {
            const float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float tGlow = EaseOutQuad(t);
                float tRing = EaseOutQuad(t);

                if (glowRect != null && glowImage != null)
                {
                    glowRect.localScale = Vector3.LerpUnclamped(new Vector3(0.1f, 0.1f, 1f), new Vector3(1.2f, 1.2f, 1f), tGlow);
                    Color col = glowImage.color;
                    col.a = Mathf.Lerp(0.6f, 0f, t);
                    glowImage.color = col;
                }

                if (ringRect != null && ringImage != null)
                {
                    ringRect.localScale = Vector3.LerpUnclamped(new Vector3(0.1f, 0.1f, 1f), new Vector3(1.8f, 1.8f, 1f), tRing);
                    Color col = ringImage.color;
                    col.a = Mathf.Lerp(1f, 0f, t);
                    ringImage.color = col;
                }

                yield return null;
            }

            if (glowRect != null) DestroyTrackedVfx(glowRect.gameObject);
            if (ringRect != null) DestroyTrackedVfx(ringRect.gameObject);
        }

        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            if (Instance != null && Instance._neonRingSprite != null)
            {
                cachedRingSprite = Instance._neonRingSprite;
                return cachedRingSprite;
            }

            // Procedural Ring fallback
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f - 2f;
            float innerRadius = outerRadius - 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = 0f;
                    if (d <= outerRadius && d >= innerRadius)
                    {
                        alpha = 1f;
                    }
                    else if (d > outerRadius && d < outerRadius + 1.5f)
                    {
                        alpha = 1f - (d - outerRadius) / 1.5f;
                    }
                    else if (d < innerRadius && d > innerRadius - 1.5f)
                    {
                        alpha = 1f - (innerRadius - d) / 1.5f;
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedRingSprite;
        }

        private static Sprite GetGlowSprite()
        {
            if (cachedGlowSprite != null) return cachedGlowSprite;

            if (Instance != null && Instance._radialGlowSprite != null)
            {
                cachedGlowSprite = Instance._radialGlowSprite;
                return cachedGlowSprite;
            }

            // Procedural Glow fallback
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(1f - d / radius);
                    alpha = alpha * alpha; // quadratic fade for softer glow
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedGlowSprite;
        }

        private static Sprite GetRandomSplatSprite()
        {
            if (cachedSplatSprites != null && cachedSplatSprites.Length > 0)
            {
                return cachedSplatSprites[UnityEngine.Random.Range(0, cachedSplatSprites.Length)];
            }

            if (Instance != null && Instance._gooSplatSprites != null && Instance._gooSplatSprites.Length > 0)
            {
                cachedSplatSprites = Instance._gooSplatSprites;
                return cachedSplatSprites[UnityEngine.Random.Range(0, cachedSplatSprites.Length)];
            }

            // Fallback to legacy single procedural sprite
            if (cachedSplatSprite != null)
            {
                return cachedSplatSprite;
            }

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color pink = new Color(1f, 0.42f, 0.71f, 1f);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    texture.SetPixel(x, y, new Color(pink.r, pink.g, pink.b, dist <= radius ? 1f : 0f));
                }
            }

            texture.Apply();
            cachedSplatSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedSplatSprite;
        }

        // ----- Affordable building slot pulse -----------------------------------------------

        /// <summary>
        /// Starts an infinite sine pulse on rect.localScale (1.0-1.02) and graphic's alpha
        /// channel (0.4-1.0), period 1.0s. Only overrides alpha, so it composes with whatever
        /// already set the graphic's base RGB color.
        /// </summary>
        public static void PlayAffordablePulse(RectTransform rect, Graphic graphic)
        {
            if (rect == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.affordablePulseCoroutines, rect, controller.AffordablePulseRoutine(rect, graphic));
        }

        /// <summary>Stops the pulse started by PlayAffordablePulse and resets scale to 1.</summary>
        public static void StopAffordablePulse(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.localScale = Vector3.one;

            if (Instance == null)
            {
                return;
            }

            if (Instance.affordablePulseCoroutines.TryGetValue(rect, out Coroutine running) && running != null)
            {
                Instance.StopCoroutine(running);
            }

            Instance.affordablePulseCoroutines.Remove(rect);
        }

        private IEnumerator AffordablePulseRoutine(RectTransform rect, Graphic graphic)
        {
            const float period = 1.0f;
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                float phase = (elapsed % period) / period;
                float sine = (Mathf.Sin(phase * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f;

                if (rect != null)
                {
                    rect.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.02f, sine);
                }

                if (graphic != null)
                {
                    Color color = graphic.color;
                    color.a = Mathf.Lerp(0.4f, 1.0f, sine);
                    graphic.color = color;
                }

                yield return null;
            }
        }

        // ----- Popup spawn shake -----------------------------------------------------------

        /// <summary>
        /// Shakes rect.anchoredPosition with decaying random offsets (6 steps over 0.2s,
        /// settling exactly at its original position), while fading canvasGroup's alpha 0-&gt;1
        /// over the first 0.08s if provided.
        /// </summary>
        public static void PlayPopupSpawn(RectTransform rect, CanvasGroup canvasGroup = null)
        {
            if (rect == null)
            {
                return;
            }

            EnsureInstance()?.StartCoroutine(PopupSpawnRoutine(rect, canvasGroup));
        }

        private static IEnumerator PopupSpawnRoutine(RectTransform rect, CanvasGroup canvasGroup)
        {
            const int shakeSteps = 6;
            const float duration = 0.2f;
            const float stepDuration = duration / shakeSteps;
            const float fadeDuration = 0.08f;
            const float maxOffset = 8f;

            Vector2 center = rect.anchoredPosition;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            Vector2 current = center;
            float totalElapsed = 0f;

            for (int step = 0; step < shakeSteps; step++)
            {
                float decay = 1f - ((float)step / shakeSteps);
                Vector2 target = center + new Vector2(
                    UnityEngine.Random.Range(-maxOffset, maxOffset),
                    UnityEngine.Random.Range(-maxOffset, maxOffset)) * decay;

                Vector2 stepStart = current;
                float stepElapsed = 0f;

                while (stepElapsed < stepDuration)
                {
                    stepElapsed += Time.deltaTime;
                    totalElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(stepElapsed / stepDuration);
                    current = Vector2.LerpUnclamped(stepStart, target, t);
                    rect.anchoredPosition = current;

                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = Mathf.Clamp01(totalElapsed / fadeDuration);
                    }

                    yield return null;
                }
            }

            rect.anchoredPosition = center;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        // ----- Denial shake (locked-button tap feedback) -----------------------------------

        /// <summary>
        /// Shakes rect.anchoredPosition with decaying random offsets (6 steps over ~0.2s,
        /// settling exactly back at its original position) -- a refusal cue for an already-
        /// visible, already-interactable element (e.g. a locked-but-tappable button), not an
        /// entrance animation. Deliberately a standalone duplicate of PopupSpawnRoutine's shake
        /// loop above rather than a shared refactor: no alpha fade / CanvasGroup handling here,
        /// and PlayPopupSpawn's own entrance behavior must not change as a side effect of this
        /// method existing.
        /// </summary>
        public static void PlayDenialShake(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            controller.StopAndReplace(controller.denialShakeCoroutines, target, controller.DenialShakeRoutine(target));
        }

        private IEnumerator DenialShakeRoutine(RectTransform rect)
        {
            const int shakeSteps = 6;
            const float duration = 0.2f;
            const float stepDuration = duration / shakeSteps;
            const float maxOffset = 8f;

            Vector2 center = rect.anchoredPosition;
            Vector2 current = center;

            for (int step = 0; step < shakeSteps; step++)
            {
                float decay = 1f - ((float)step / shakeSteps);
                Vector2 target = center + new Vector2(
                    UnityEngine.Random.Range(-maxOffset, maxOffset),
                    UnityEngine.Random.Range(-maxOffset, maxOffset)) * decay;

                Vector2 stepStart = current;
                float stepElapsed = 0f;

                while (stepElapsed < stepDuration)
                {
                    stepElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(stepElapsed / stepDuration);
                    current = Vector2.LerpUnclamped(stepStart, target, t);
                    rect.anchoredPosition = current;
                    yield return null;
                }
            }

            rect.anchoredPosition = center;
        }

        // ----- Generic panel slide (e.g. ShopUIController's slide-up-from-bottom panel) -----

        /// <summary>
        /// Slides rect.anchoredPosition from "from" to "to" over duration via DOTween
        /// (Ease.InOutQuad), then invokes onComplete (e.g. to deactivate the panel after a
        /// slide-down-to-close). Reusable primitive -- callers own what "resting" vs
        /// "offscreen" actually means for their panel.
        /// </summary>
        public static void PlaySlide(RectTransform rect, Vector2 from, Vector2 to, float duration, Action onComplete = null)
        {
            if (rect == null)
            {
                return;
            }

            AnimationController controller = EnsureInstance();
            KillExisting(controller.slideTweens, rect);

            rect.anchoredPosition = from;
            Tween tween = rect.DOAnchorPos(to, duration).SetEase(Ease.InOutQuad);
            if (onComplete != null)
            {
                tween.OnComplete(() => onComplete());
            }

            controller.slideTweens[rect] = tween;
        }

        // ----- High-IQ milestone celebration ------------------------------------------------

        private static readonly Color CelebrationTintColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);

        /// <summary>
        /// Plays a celebratory beat when PlayerIQ crosses a milestone: the HUD canvas alpha
        /// pulses 0.7-1.0 (period 0.5s) while a full-screen overlay tints toward neon cyan and
        /// back over 2.0s, capped off with a quick white flash (alpha 1-&gt;0 over 0.4s).
        /// </summary>
        public static void PlayHighIQCelebration(CanvasGroup hudCanvasGroup, Image flashOverlay)
        {
            EnsureInstance()?.StartCoroutine(HighIQCelebrationRoutine(hudCanvasGroup, flashOverlay));
        }

        private static IEnumerator HighIQCelebrationRoutine(CanvasGroup hudCanvasGroup, Image flashOverlay)
        {
            const float tintDuration = 2.0f;
            const float pulsePeriod = 0.5f;
            const float flashDuration = 0.4f;

            float elapsed = 0f;
            float half = tintDuration / 2f;

            while (elapsed < tintDuration)
            {
                elapsed += Time.deltaTime;

                if (flashOverlay != null)
                {
                    float tintAlpha = elapsed <= half
                        ? Mathf.Lerp(0f, 0.5f, elapsed / half)
                        : Mathf.Lerp(0.5f, 0f, (elapsed - half) / half);
                    flashOverlay.color = new Color(CelebrationTintColor.r, CelebrationTintColor.g, CelebrationTintColor.b, tintAlpha);
                }

                if (hudCanvasGroup != null)
                {
                    float phase = (elapsed % pulsePeriod) / pulsePeriod;
                    float sine = (Mathf.Sin(phase * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f;
                    hudCanvasGroup.alpha = Mathf.Lerp(0.95f, 1.0f, sine);
                }

                yield return null;
            }

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 1f;
            }

            if (flashOverlay != null)
            {
                float flashElapsed = 0f;
                while (flashElapsed < flashDuration)
                {
                    flashElapsed += Time.deltaTime;
                    flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, flashElapsed / flashDuration));
                    yield return null;
                }

                flashOverlay.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        // ----- Shared helpers ---------------------------------------------------------------

        private void StopAndReplace<T>(Dictionary<T, Coroutine> map, T key, IEnumerator routine)
        {
            if (map.TryGetValue(key, out Coroutine existing) && existing != null)
            {
                StopCoroutine(existing);
            }

            map[key] = StartCoroutine(routine);
        }

        /// <summary>DOTween equivalent of StopAndReplace: kills any in-flight tween for key before the caller starts a new one.</summary>
        private static void KillExisting<T>(Dictionary<T, Tween> map, T key)
        {
            if (map.TryGetValue(key, out Tween existing) && existing != null && existing.IsActive())
            {
                existing.Kill();
            }

            map.Remove(key);
        }

        private static IEnumerator ScaleOverTime(Transform target, Vector3 from, Vector3 to, float duration, Func<float, float> ease)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / duration));
                if (target == null)
                {
                    yield break;
                }

                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = to;
            }
        }

        private static IEnumerator RotateOverTime(Transform target, float fromDegrees, float toDegrees, float duration, Func<float, float> ease)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / duration));
                if (target == null)
                {
                    yield break;
                }

                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(fromDegrees, toDegrees, t));
                yield return null;
            }

            if (target != null)
            {
                target.localRotation = Quaternion.Euler(0f, 0f, toDegrees);
            }
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseInQuad(float t) => t * t;

        private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
}
