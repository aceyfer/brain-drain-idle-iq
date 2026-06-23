using TMPro;
using UnityEngine;
using System.Collections;
namespace BrainforgeGames.MobileUXToolkit
{
    public class Toast : MonoBehaviour
    {
        public static Toast Instance;

        [Header("UI")]
        [SerializeField] GameObject toastRoot;
        [SerializeField] RectTransform toastRect;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Motion")]
        [SerializeField] float showDuration = 0.22f;
        [SerializeField] float hideDuration = 0.18f;
        [SerializeField] ScreenTransition.TransitionEase showEase = ScreenTransition.TransitionEase.InOut;
        [SerializeField] ScreenTransition.TransitionEase hideEase = ScreenTransition.TransitionEase.In;

        [Header("Position (Y)")]
        [SerializeField] float hiddenY = -250f;   // below screen
        [SerializeField] float shownY = 80f;      // little above bottom

        Coroutine moveRoutine;
        Coroutine lifeRoutine;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            if (toastRect != null)
                toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, hiddenY);
            moveRoutine = null;
        }

        /* ================= PUBLIC API ================= */

        public static void Show(string message, float staySeconds = 2f)
        {
            if (Instance == null)
            {
                Debug.LogWarning("Toast not present in scene");
                return;
            }

            Instance.InternalShow(message, staySeconds);
        }

        /* ================= INTERNAL ================= */

        void InternalShow(string message, float staySeconds)
        {
            if (toastRoot == null || toastRect == null || messageText == null)
            {
                Debug.LogWarning("Toast missing references");
                return;
            }

            StopLifeIfAny();
            StopMoveRoutineIfAny();

            messageText.text = message;

            toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, hiddenY);
            toastRoot.SetActive(true);

            moveRoutine = StartCoroutine(
        MoveY(hiddenY, shownY, showDuration, showEase, null)
    );

            lifeRoutine = StartCoroutine(Life(staySeconds));
        }

        IEnumerator Life(float staySeconds)
        {
            yield return new WaitForSecondsRealtime(staySeconds);
            lifeRoutine = null;
            InternalHide();
        }

        void InternalHide()
        {
            if (toastRoot == null || toastRect == null) return;
            if (!toastRoot.activeSelf) return;

            StopLifeIfAny();
            StopMoveRoutineIfAny();

            float fromY = toastRect.anchoredPosition.y;
            moveRoutine = StartCoroutine(
                MoveY(fromY, hiddenY, hideDuration, hideEase, () =>
                {
                    toastRoot.SetActive(false);
                })
            );
        }

        void StopLifeIfAny()
        {
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
                lifeRoutine = null;
            }
        }

        void StopMoveRoutineIfAny()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
        }


        IEnumerator MoveY(float fromY, float toY, float duration, ScreenTransition.TransitionEase ease, System.Action onComplete)
        {
            if (toastRect == null)
            {
                moveRoutine = null;
                yield break;
            }

            if (duration <= 0f)
            {
                toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, toY);
                onComplete?.Invoke();
                moveRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float e = ScreenTransition.EvaluateEase(u, ease);

                float y = Mathf.LerpUnclamped(fromY, toY, e);
                toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, y);
                yield return null;
            }

            toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, toY);
            onComplete?.Invoke();
            moveRoutine = null;
        }
    }
}