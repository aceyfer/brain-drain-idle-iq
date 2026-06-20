using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Singleton home for all core UI feedback animations (tap squash/stretch, idle breathing,
    /// goo splat particles, affordable-slot pulse, popup spawn shake). No DOTween/LeanTween are
    /// present in this project, so every effect is driven by hand-rolled coroutines. Other
    /// scripts trigger effects via the static wrapper methods, e.g. AnimationController.PlayTapAnim(transform).
    /// </summary>
    public sealed class AnimationController : MonoBehaviour
    {
        private static Sprite cachedSplatSprite;
        private static Sprite[] cachedSplatSprites;

        public static AnimationController Instance { get; private set; }

        private readonly Dictionary<Transform, Coroutine> tapAnimCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> breathingCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> boredFidgetCoroutines = new();
        private readonly Dictionary<Transform, Coroutine> excitedBounceCoroutines = new();
        private readonly Dictionary<RectTransform, Coroutine> affordablePulseCoroutines = new();

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

        private static AnimationController EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
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

            EnsureInstance().SpawnSplatParticles(screenPosition, parent);
        }

        private void SpawnSplatParticles(Vector2 screenPosition, RectTransform parent)
        {
            int count = UnityEngine.Random.Range(4, 7);

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera screenCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, screenCamera, out Vector2 localPoint);

            for (int i = 0; i < count; i++)
            {
                GameObject particleObject = new GameObject("GooSplatParticle", typeof(RectTransform), typeof(Image));
                particleObject.transform.SetParent(parent, false);

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

        private static IEnumerator SplatParticleRoutine(RectTransform particleRect, Image particleImage, Vector2 from, Vector2 to)
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
                Destroy(particleRect.gameObject);
            }
        }

        private static Sprite GetRandomSplatSprite()
        {
            if (cachedSplatSprites != null && cachedSplatSprites.Length > 0)
            {
                return cachedSplatSprites[UnityEngine.Random.Range(0, cachedSplatSprites.Length)];
            }

#if UNITY_EDITOR
            // Dynamically load the high-quality cartoon splat spritesheet in the editor
            string path = "Assets/_Game/Sprites/Particles/GooSplats.png";
            var loadedAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var spriteList = new List<Sprite>();
            foreach (var asset in loadedAssets)
            {
                if (asset is Sprite sp)
                {
                    spriteList.Add(sp);
                }
            }

            if (spriteList.Count > 0)
            {
                cachedSplatSprites = spriteList.ToArray();
                return cachedSplatSprites[UnityEngine.Random.Range(0, cachedSplatSprites.Length)];
            }
#endif

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

            EnsureInstance().StartCoroutine(PopupSpawnRoutine(rect, canvasGroup));
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

        // ----- High-IQ milestone celebration ------------------------------------------------

        private static readonly Color CelebrationTintColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);

        /// <summary>
        /// Plays a celebratory beat when PlayerIQ crosses a milestone: the HUD canvas alpha
        /// pulses 0.7-1.0 (period 0.5s) while a full-screen overlay tints toward neon cyan and
        /// back over 2.0s, capped off with a quick white flash (alpha 1-&gt;0 over 0.4s).
        /// </summary>
        public static void PlayHighIQCelebration(CanvasGroup hudCanvasGroup, Image flashOverlay)
        {
            EnsureInstance().StartCoroutine(HighIQCelebrationRoutine(hudCanvasGroup, flashOverlay));
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
                    hudCanvasGroup.alpha = Mathf.Lerp(0.7f, 1.0f, sine);
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

        private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
}
