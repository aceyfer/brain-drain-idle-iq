using UnityEngine;

namespace BrainDrain.UI
{
    /// <summary>
    /// One-time setup that insets UI content for iOS notches/Dynamic Island.
    /// Attach to a CustomSafeArea child GameObject of the main Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaManager : MonoBehaviour
    {
        private void Awake()
        {
            RectTransform rect = (RectTransform)transform;
            ApplySafeArea(rect);
        }

        private static void ApplySafeArea(RectTransform safeAreaRect)
        {
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Top/bottom only -- force full width regardless of what the safe area says.
            anchorMin.x = 0f;
            anchorMax.x = 1f;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }
    }
}
