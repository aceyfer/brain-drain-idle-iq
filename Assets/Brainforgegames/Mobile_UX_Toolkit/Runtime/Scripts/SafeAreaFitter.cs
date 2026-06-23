using UnityEngine;
using UnityEngine.UI;

namespace BrainforgeGames.MobileUXToolkit
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        [Tooltip("Apply safe area only in portrait mode?")]
        public bool portraitOnly = false;

        [Tooltip("Apply safe area only in landscape mode?")]
        public bool landscapeOnly = false;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;

            if (portraitOnly && !IsPortrait())
                safe = new Rect(0, 0, Screen.width, Screen.height);

            if (landscapeOnly && !IsLandscape())
                safe = new Rect(0, 0, Screen.width, Screen.height);

            _lastSafeArea = safe;
            _lastOrientation = Screen.orientation;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }

        private bool IsPortrait()
        {
            return Screen.height >= Screen.width;
        }

        private bool IsLandscape()
        {
            return Screen.width > Screen.height;
        }
    }
}