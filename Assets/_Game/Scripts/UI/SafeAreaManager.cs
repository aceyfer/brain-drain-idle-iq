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

#if UNITY_ANDROID && !UNITY_EDITOR
            // Screen.safeArea does not reliably exclude the Android navigation/gesture bar -- on many
            // devices (especially gesture nav / edge-to-edge) it reports the full screen as safe even
            // though the bottom N px are covered by the system bar. Query the real inset natively and
            // take the larger of the two so we never under-inset.
            float navBarFraction = GetAndroidBottomInsetFraction();
            if (navBarFraction > anchorMin.y)
            {
                anchorMin.y = navBarFraction;
            }
#endif

            // Top/bottom only -- force full width regardless of what the safe area says.
            anchorMin.x = 0f;
            anchorMax.x = 1f;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static float GetAndroidBottomInsetFraction()
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                using (AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView"))
                using (AndroidJavaObject insets = decorView.Call<AndroidJavaObject>("getRootWindowInsets"))
                {
                    if (insets == null)
                    {
                        return 0f;
                    }

                    int bottomPx = insets.Call<int>("getSystemWindowInsetBottom");
                    return Screen.height > 0 ? bottomPx / (float)Screen.height : 0f;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SafeAreaManager: failed to query Android nav bar inset, falling back to Screen.safeArea only. {e.Message}");
                return 0f;
            }
        }
#endif
    }
}
