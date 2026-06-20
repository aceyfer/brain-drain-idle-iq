using UnityEngine;

namespace BrainDrain.UI
{
    /// <summary>
    /// One-time setup that insets UI content for iOS notches/Dynamic Island. Attach to the
    /// same GameObject as the main Canvas.
    ///
    /// The Canvas's own RectTransform can't be used for this directly: Unity's Canvas component
    /// continuously overwrites its own RectTransform to match the screen every frame, so any
    /// anchors/offsets set on it are silently overridden. Instead this creates (or reuses) a
    /// "SafeArea" child RectTransform stretched under the Canvas and insets *that* — parent your
    /// actual HUD content under that child, not directly under the Canvas, for this to have any
    /// effect. This is the standard Unity safe-area pattern.
    ///
    /// Insets top/bottom only per spec (left/right safe-area insets only matter in landscape,
    /// which this portrait-only game doesn't use). Resolution-independence comes from using
    /// normalized anchors (Screen.safeArea divided by Screen.width/height) rather than pixel
    /// offsets -- CanvasScaler's reference resolution/scale factor isn't actually part of this
    /// calculation, since anchor fractions are already resolution-independent by construction.
    ///
    /// Awake-only by design (no Update loop, per spec): won't react to safe-area changes that
    /// can occur without a device rotation (e.g. the iOS in-call status bar growing taller,
    /// the Dynamic Island expanding for a Live Activity). Acceptable given this is explicitly a
    /// one-time setup script for a portrait-only game.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class SafeAreaManager : MonoBehaviour
    {
        private const string SafeAreaChildName = "SafeArea";

        /// <summary>The child RectTransform this script insets. Parent HUD content under this, not the Canvas itself.</summary>
        public RectTransform SafeAreaRect { get; private set; }

        private void Awake()
        {
            RectTransform canvasRect = (RectTransform)transform;
            SafeAreaRect = FindOrCreateSafeAreaChild(canvasRect);
            ApplySafeArea(SafeAreaRect);
        }

        private static RectTransform FindOrCreateSafeAreaChild(RectTransform canvasRect)
        {
            Transform existing = canvasRect.Find(SafeAreaChildName);
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            var safeAreaObject = new GameObject(SafeAreaChildName, typeof(RectTransform));
            RectTransform rect = (RectTransform)safeAreaObject.transform;
            rect.SetParent(canvasRect, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
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
