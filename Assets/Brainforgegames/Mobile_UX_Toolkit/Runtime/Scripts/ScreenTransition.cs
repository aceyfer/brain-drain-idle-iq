using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace BrainforgeGames.MobileUXToolkit
{
    public sealed class ScreenTransition : MonoBehaviour
    {
        public enum SlideDirection { Left, Right, Up, Down }

        [Header("References (full-screen UI Images)")]
        [SerializeField] Image fadeImage;     // Fullscreen Image (black), alpha animated
        [SerializeField] Image slideImage;    // Fullscreen Image (black), moves in/out
        [SerializeField] Image circleImage;   // Fullscreen Image (black), Type=Filled, Radial 360

        [Header("Defaults")]
        [SerializeField] Color color = Color.black;
        [SerializeField] float defaultDuration = 0.25f;

        static ScreenTransition _instance;
        Coroutine _routine;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Prepare();
        }

        void Prepare()
        {
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(false);
                fadeImage.raycastTarget = false;
                fadeImage.color = new Color(color.r, color.g, color.b, 0f);
            }

            if (slideImage != null)
            {
                slideImage.gameObject.SetActive(false);
                slideImage.raycastTarget = false;
                slideImage.color = color;
                slideImage.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (circleImage != null)
            {
                circleImage.gameObject.SetActive(false);
                circleImage.raycastTarget = false;
                circleImage.color = color;
                circleImage.fillAmount = 0f;
            }
        }

        static bool EnsureInstance()
        {
            if (_instance != null) return true;
            Debug.LogError("[ScreenTransition] No ScreenTransition instance found. Add the ScreenTransition prefab once (it persists).");
            return false;
        }

        void StartRoutine(IEnumerator routine)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(routine);
        }

        #region Public Static APIs

        public static void FadeOut(float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;
            _instance.StartRoutine(_instance.FadeRoutine(outToBlack: true, duration, onComplete));
        }

        public static void FadeIn(float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;

            _instance.StartRoutine(_instance.FadeRoutine(outToBlack: false, duration, onComplete));
        }

        public static void SlideOut(SlideDirection direction, float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;
            _instance.StartRoutine(_instance.SlideRoutine(outToCover: true, direction, duration, onComplete));
        }

        public static void SlideIn(SlideDirection direction, float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;
            _instance.StartRoutine(_instance.SlideRoutine(outToCover: false, direction, duration, onComplete));
        }

        public static void CloseCircle(float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;
            _instance.StartRoutine(_instance.CircleFillRoutine(close: true, duration, onComplete));
        }

        public static void OpenCircle(float duration = -1f, Action onComplete = null)
        {
            if (!EnsureInstance()) return;
            _instance.StartRoutine(_instance.CircleFillRoutine(close: false, duration, onComplete));
        }

        #endregion

        #region Co-Routines

        IEnumerator FadeRoutine(bool outToBlack, float duration, Action onComplete)
        {
            duration = duration <= 0f ? defaultDuration : duration;

            if (fadeImage == null)
            {
                Debug.LogError("[ScreenTransition] Fade Image reference missing.");
                yield break;
            }

            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true;

            float start = outToBlack ? 0f : 1f;
            float end = outToBlack ? 1f : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / duration);
                float eased = EaseInOut(n);

                float a = Mathf.Lerp(start, end, eased);
                fadeImage.color = new Color(color.r, color.g, color.b, a);

                yield return null;
            }

            fadeImage.color = new Color(color.r, color.g, color.b, end);
            fadeImage.raycastTarget = outToBlack;

            if (!outToBlack)
            {
                fadeImage.raycastTarget = false;
                fadeImage.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        IEnumerator SlideRoutine(bool outToCover, SlideDirection dir, float duration, Action onComplete)
        {
            duration = duration <= 0f ? defaultDuration : duration;

            if (slideImage == null)
            {
                Debug.LogError("[ScreenTransition] Slide Image reference missing.");
                yield break;
            }

            RectTransform rt = slideImage.rectTransform;

            slideImage.gameObject.SetActive(true);
            slideImage.raycastTarget = true;
            slideImage.color = color;

            Vector2 off = GetSlideOffset(dir, rt);

            Vector2 from = outToCover ? off : Vector2.zero;
            Vector2 to = outToCover ? Vector2.zero : off;

            rt.anchoredPosition = from;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / duration);

                float eased = outToCover ? EaseOutBack(n) : EaseIn(n);

                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            rt.anchoredPosition = to;
            slideImage.raycastTarget = outToCover;

            if (!outToCover)
            {
                slideImage.raycastTarget = false;
                slideImage.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        IEnumerator CircleFillRoutine(bool close, float duration, Action onComplete)
        {
            duration = duration <= 0f ? defaultDuration : duration;

            if (!close) duration += 0.25f;

            if (circleImage == null)
            {
                Debug.LogError("[ScreenTransition] Circle Image reference missing.");
                yield break;
            }

            circleImage.gameObject.SetActive(true);
            circleImage.raycastTarget = true;
            circleImage.color = color;

            // CLOSE: 0 -> 1 (cover)
            // OPEN : 1 -> 0 (reveal)
            float start = close ? 0f : 1f;
            float end = close ? 1f : 0f;

            circleImage.fillAmount = start;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / duration);

                float eased = close ? EaseIn(n) : EaseOutBack(n);
                circleImage.fillAmount = Mathf.Lerp(start, end, eased);

                yield return null;
            }

            circleImage.fillAmount = end;

            if (!close)
            {
                circleImage.raycastTarget = false;
                circleImage.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        }

        #endregion

        #region Helpers

        Vector2 GetSlideOffset(SlideDirection dir, RectTransform rt)
        {
            float w = rt.rect.width + 80f;
            float h = rt.rect.height + 80f;

            return dir switch
            {
                SlideDirection.Left => new Vector2(-w, 0f),
                SlideDirection.Right => new Vector2(w, 0f),
                SlideDirection.Up => new Vector2(0f, h),
                SlideDirection.Down => new Vector2(0f, -h),
                _ => new Vector2(w, 0f)
            };
        }

        #endregion

        #region Easing
        public enum TransitionEase
        {
            Linear,
            In,
            InOut,
            OutBack
        }
        public static float EaseIn(float t) => t * t;

        public static float EaseInOut(float t)
        {
            // Smooth classic curve
            return t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        public static float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        public static float EvaluateEase(float t, TransitionEase ease)
        {
            t = Mathf.Clamp01(t);

            return ease switch
            {
                TransitionEase.Linear => t,
                TransitionEase.In => EaseIn(t),
                TransitionEase.InOut => EaseInOut(t),
                TransitionEase.OutBack => EaseOutBack(t),
                _ => t
            };
        }
        #endregion
    }
}