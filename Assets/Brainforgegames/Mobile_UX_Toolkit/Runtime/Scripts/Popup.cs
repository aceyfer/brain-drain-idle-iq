using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrainforgeGames.MobileUXToolkit
{
    public enum PopupIcons
    {
        NoIcon,
        Warning,
        Info,
        Hint
    }
    [Serializable]
    public class PopupIconsHandler
    {
        public PopupIcons iconType;
        public Sprite sprite;
        public Color popupColor;

    }

    public class Popup : MonoBehaviour
    {
        public static Popup Instance;

        [Header("UI")]
        [SerializeField] GameObject popupRoot;
        [SerializeField] Image topBarBg;
        [SerializeField] Image popupIconImage;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Tween")]
        [SerializeField] float showDuration = 0.18f;
        [SerializeField] float hideDuration = 0.14f;
        [SerializeField] ScreenTransition.TransitionEase showEase = ScreenTransition.TransitionEase.OutBack;
        [SerializeField] ScreenTransition.TransitionEase hideEase = ScreenTransition.TransitionEase.In;


        [Header("Buttons")]
        [SerializeField] Button primaryButton;
        [SerializeField] TextMeshProUGUI primaryButtonText;
        [SerializeField] Button secondaryButton;
        [SerializeField] TextMeshProUGUI secondaryButtonText;

        [Header("Icons")]
        [SerializeField] List<PopupIconsHandler> popupIconsList;
        [Space(20)]
        Action onPrimaryButtonClicked;
        Action onSecondaryButtonClicked;

        Coroutine autoCloseRoutine;

        Coroutine scaleRoutine;
        bool isHiding;


        void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            popupRoot.SetActive(false);
            popupRoot.transform.localScale = Vector3.zero;
            scaleRoutine = null;


            primaryButton.onClick.AddListener(OnPrimaryClicked);
            secondaryButton.onClick.AddListener(OnSecondaryClicked);
        }

        void StopScaleRoutineIfAny()
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }
        }

        IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration, ScreenTransition.TransitionEase ease, Action onComplete)
        {
            if (popupRoot == null)
            {
                scaleRoutine = null;
                yield break;
            }

            if (duration <= 0f)
            {
                popupRoot.transform.localScale = to;
                onComplete?.Invoke();
                scaleRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float e = ScreenTransition.EvaluateEase(u, ease);
                popupRoot.transform.localScale = Vector3.LerpUnclamped(from, to, e);
                yield return null;
            }

            popupRoot.transform.localScale = to;
            onComplete?.Invoke();
            scaleRoutine = null;
        }


        /* ================= PUBLIC API ================= */

        // 0-button popup (auto close)
        public static void ShowAuto(string title, string message, float autoCloseSeconds = 2, PopupIcons iconType = PopupIcons.NoIcon)
        {
            if (Instance == null)
            {
                Debug.LogWarning("Popup not present in scene");
                return;
            }

            Instance.InternalShow(
                title,
                message,
                null,
                null,
                null,
                null,
                autoCloseSeconds,
                iconType
            );
        }

        // 1-button popup
        public static void Show(
            string title,
            string message,
            string _primaryButtonText,
            Action onPrimaryButtonAction = null,
            PopupIcons iconType = PopupIcons.NoIcon
        )
        {
            Show(title, message, _primaryButtonText, null, onPrimaryButtonAction, null, iconType);
        }

        // 2-button popup
        public static void Show(
            string title,
            string message,
            string primaryText,
            string secondaryText,
            Action onPrimaryAction = null,
            Action onSecondaryAction = null,
            PopupIcons iconType = PopupIcons.NoIcon
        )
        {
            if (Instance == null)
            {
                Debug.LogWarning("Popup not present in scene");
                return;
            }

            Instance.InternalShow(
                title,
                message,
                primaryText,
                secondaryText,
                onPrimaryAction,
                onSecondaryAction,
                0,
                iconType
            );
        }

        public static void Hide()
        {
            if (Instance == null) return;
            Instance.HideInternal();
        }

        /* ================= INTERNAL ================= */

        void InternalShow(
            string title,
            string message,
            string primaryText,
            string secondaryText,
            Action onPrimaryAction,
            Action onSecondaryAction,
            float autoCloseSeconds,
            PopupIcons iconType
        )
        {
            StopAutoCloseIfAny();

            titleText.text = title;
            messageText.text = message;


            onPrimaryButtonClicked = onPrimaryAction;
            onSecondaryButtonClicked = onSecondaryAction;

            // Primary button
            bool hasPrimary = !string.IsNullOrEmpty(primaryText);
            primaryButton.gameObject.SetActive(hasPrimary);
            if (hasPrimary)
                primaryButtonText.text = primaryText;

            // Secondary button
            bool hasSecondary = !string.IsNullOrEmpty(secondaryText);
            secondaryButton.gameObject.SetActive(hasSecondary);
            if (hasSecondary)
                secondaryButtonText.text = secondaryText;

            // Set Icon + Color
            if (popupIconImage != null)
            {
                PopupIconsHandler popupData = GetPopupData(iconType);
                if (popupData != null)
                {
                    popupIconImage.sprite = popupData.sprite;
                    primaryButton.transform.GetComponent<Image>().color = secondaryButton.transform.GetComponent<Image>().color
                    = topBarBg.color = popupData.popupColor;
                    popupIconImage.gameObject.SetActive(popupData.sprite != null);
                }
            }
            isHiding = false;

            //ScreenTransition easing
            StopScaleRoutineIfAny();

            if (popupRoot != null)
            {
                popupRoot.transform.localScale = Vector3.zero;
                popupRoot.SetActive(true);
                scaleRoutine = StartCoroutine(ScaleTo(Vector3.zero, Vector3.one, showDuration, showEase, null));
            }


            if (autoCloseSeconds > 0f)
                autoCloseRoutine = StartCoroutine(AutoCloseAfter(autoCloseSeconds));
        }

        void HideInternal()
        {
            if (popupRoot == null) return;
            if (!popupRoot.activeSelf) return;
            if (isHiding) return;

            isHiding = true;
            StopAutoCloseIfAny();

            //ScreenTransition easing
            StopScaleRoutineIfAny();

            if (popupRoot != null)
            {
                Vector3 from = popupRoot.transform.localScale;
                scaleRoutine = StartCoroutine(ScaleTo(from, Vector3.zero, hideDuration, hideEase, () =>
                {
                    popupRoot.SetActive(false);
                    isHiding = false;
                }));
            }
            else
            {
                Debug.LogWarning("Popup Root Empty");
            }
        }


        PopupIconsHandler GetPopupData(PopupIcons iconType)
        {
            if (popupIconsList == null || popupIconsList.Count <= 0) return null;
            foreach (var item in popupIconsList)
            {
                if (item.iconType == iconType)
                {
                    return item;
                }
            }
            return null;
        }

        void OnPrimaryClicked()
        {
            StopAutoCloseIfAny();
            HideInternal();
            onPrimaryButtonClicked?.Invoke();
            ClearActions();
        }

        void OnSecondaryClicked()
        {
            StopAutoCloseIfAny();
            HideInternal();
            onSecondaryButtonClicked?.Invoke();
            ClearActions();
        }

        void ClearActions()
        {
            onPrimaryButtonClicked = null;
            onSecondaryButtonClicked = null;
            autoCloseRoutine = null;
        }

        void StopAutoCloseIfAny()
        {
            if (autoCloseRoutine != null)
            {
                StopCoroutine(autoCloseRoutine);
                autoCloseRoutine = null;
            }
        }

        IEnumerator AutoCloseAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            autoCloseRoutine = null;
            HideInternal();
        }
    }
}