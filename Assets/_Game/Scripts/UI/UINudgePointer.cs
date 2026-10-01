using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;

namespace BrainDrain.UI
{
    /// <summary>
    /// Generic "look here" pointer overlay -- Assets/Plans/tutorial-direction-and-cogs-trust.md's
    /// Option B, procedural-art route (B.1): a single reusable arrow that can be aimed at any
    /// RectTransform in the scene, bobbing gently just above it until dismissed. Deliberately
    /// singular and general-purpose (PointAt(target) / Hide()) rather than a bespoke pointer per
    /// feature -- the first caller is UpgradeSlotUI's first-affordable-building nudge, but nothing
    /// about this class is specific to the shop.
    ///
    /// This script only supplies runtime behavior; the GameObject it lives on (an Image sourcing
    /// a generated arrow sprite, parented directly under the scene's root Canvas so it isn't
    /// clipped by a scroll view) is created/updated by
    /// PlaceholderArtGenerator.GenerateNudgePointer's "BrainDrain/Generate Placeholder Art/Nudge
    /// Pointer" Editor menu item -- see that file for why Instance below deliberately does not
    /// auto-create a hosting object the way FTUEManager/RandomChatterManager's Instance do.
    ///
    /// Auto-dismisses on ANY building purchase (UpgradeManager.OnBuildingPurchased) -- a
    /// reasonable default while the only caller is itself about a building purchase; revisit this
    /// coupling if a future non-purchase nudge wants this same pointer without that auto-hide
    /// rule.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UINudgePointer : MonoBehaviour
    {
        private const float BobAmplitudePixels = 10f;
        private const float BobPeriodSeconds = 1.1f;

        /// <summary>How far above the target's top edge the arrow's tip hovers, before the bob offset is added.</summary>
        private const float VerticalOffsetPixels = 56f;

        /// <summary>
        /// Extra breathing room kept between the arrow's own visual top edge and a
        /// clampToVisibleArea's top edge, so the arrow doesn't end up touching whatever it's
        /// being kept clear of (e.g. a shop tab bar).
        /// </summary>
        private const float ClampTopPaddingPixels = 12f;

        /// <summary>
        /// 2026-08-31 root-cause fix: this object lives directly under the root Canvas
        /// (sortingOrder 0, no override), but plenty of other UI it needs to hover above does NOT
        /// share that simple sibling-order world -- e.g. ShopUIController's tab content
        /// (TabContentSortingOrder = 1) gives each shop tab its own overrideSorting Canvas so tabs
        /// layer correctly over each other. A nested Canvas with overrideSorting renders relative
        /// to OTHER Canvases by sortingOrder alone, completely ignoring Transform sibling position
        /// in an ancestor Canvas -- so no amount of SetAsLastSibling() on our own RectTransform
        /// could ever win against it (confirmed live: sibling order was verified correct every
        /// frame, yet the arrow stayed invisible specifically over shop rows, rendering fine only
        /// over plain-Canvas HUD content that has no such override). The fix is to give ourselves
        /// the same kind of override, one clearly above the shop's (1) but comfortably below the
        /// modal intel-card/chaos-popup overlays (IntelCardUI.OverlaySortingOrder = 500) -- a
        /// modal popup SHOULD still cover a background hint arrow.
        /// </summary>
        private const int OverrideSortingOrder = 10;

        private static readonly Vector3[] WorldCornersBuffer = new Vector3[4];

        private static UINudgePointer instance;
        private static bool isShuttingDown;

        /// <summary>
        /// Unlike FTUEManager/RandomChatterManager's Instance, this deliberately does NOT
        /// auto-create a hosting GameObject on first access -- a runtime-created one would have no
        /// Image/sprite and just be an invisible pointer. Only
        /// PlaceholderArtGenerator.GenerateNudgePointer (Editor-only) can supply that, so a null
        /// Instance here means the generator hasn't been run yet; callers already null-check via
        /// UINudgePointer.Instance?.PointAt(...).
        /// </summary>
        public static UINudgePointer Instance
        {
            get
            {
                if (instance != null) return instance;
                if (isShuttingDown) return null;
                instance = FindAnyObjectByType<UINudgePointer>(FindObjectsInactive.Include);
                return instance;
            }
        }

        private RectTransform selfRect;
        private Image image;
        private RectTransform currentTarget;
        private RectTransform currentClampArea;
        private RectTransform currentClampBelowArea;
        private float bobPhase;
        private bool isShowing;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            selfRect = GetComponent<RectTransform>();
            image = GetComponent<Image>();

            // Enforced here (not just in PlaceholderArtGenerator's Editor-only setup) so the fix
            // holds regardless of whether the scene's baked object was regenerated -- see
            // OverrideSortingOrder's comment for why this is required at all.
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas == null)
            {
                ownCanvas = gameObject.AddComponent<Canvas>();
            }
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = OverrideSortingOrder;

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnBuildingPurchased -= HandleAnyBuildingPurchased;
                UpgradeManager.Instance.OnBuildingPurchased += HandleAnyBuildingPurchased;
            }
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnBuildingPurchased -= HandleAnyBuildingPurchased;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnBuildingPurchased -= HandleAnyBuildingPurchased;
            }

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void HandleAnyBuildingPurchased(BuildingData purchased)
        {
            Hide();
        }

        /// <summary>
        /// Aims the pointer at `target` and shows it, bobbing above it until Hide() is called or
        /// any building purchase fires.
        /// </summary>
        /// <param name="target">The RectTransform to hover above.</param>
        /// <param name="clampToVisibleArea">
        /// Optional. If set, the arrow's vertical position is clamped so its own visual top edge
        /// never rises above this area's top edge (e.g. a ScrollRect's viewport) -- without this,
        /// a target sitting near the top of a scroll view can push the arrow's offset+height
        /// footprint up into whatever sits above the scroll view (a tab bar, a close button).
        /// </param>
        /// <param name="clampBelowArea">
        /// Optional, independent of clampToVisibleArea. If set, the arrow is kept clear of this
        /// area's BOTTOM edge instead of its top -- for something the arrow should never overlap
        /// regardless of the scroll viewport's own bounds, e.g. the HUD's "BRAIN POWER" header
        /// (2026-09-30: this object lives on the root Canvas, unclipped by any scroll view, so a
        /// target near the top of a tall viewport could bob the arrow up into the header even
        /// while clampToVisibleArea's own clamp was satisfied). Both clamps apply together; the
        /// arrow respects whichever is more restrictive.
        /// </param>
        public void PointAt(RectTransform target, RectTransform clampToVisibleArea = null, RectTransform clampBelowArea = null)
        {
            if (target == null)
            {
                return;
            }

            currentTarget = target;
            currentClampArea = clampToVisibleArea;
            currentClampBelowArea = clampBelowArea;
            bobPhase = 0f;
            SetVisible(true);
            RepositionOverTarget();
        }

        public void Hide()
        {
            currentTarget = null;
            currentClampArea = null;
            currentClampBelowArea = null;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            isShowing = visible;
            if (image != null)
            {
                image.enabled = visible;
            }
        }

        private void LateUpdate()
        {
            if (!isShowing)
            {
                return;
            }

            // activeSelf (not activeInHierarchy) deliberately -- 2026-08-31 testing found the
            // nudge can legitimately fire while the shop panel itself is closed (RefreshState
            // runs continuously in the background, the same way UpdateAffordablePulse already
            // does, regardless of whether the player has the shop open). In that case every
            // ancestor up to the closed panel is inactive, so activeInHierarchy was false on the
            // very next frame and this hid the arrow permanently before the player ever got to
            // see it. activeSelf only reflects the row's OWN toggle, so a row that's genuinely
            // gone (recycled/removed by the shop's own pooling) still correctly hides the pointer,
            // while a row that's merely inside a currently-closed panel does not. The target's
            // RectTransform layout stays valid to query while inactive, so this keeps repositioning
            // correctly the moment the player opens the shop.
            if (currentTarget == null || !currentTarget.gameObject.activeSelf)
            {
                Hide();
                return;
            }

            bobPhase += Time.unscaledDeltaTime;
            RepositionOverTarget();
        }

        private void RepositionOverTarget()
        {
            if (currentTarget == null || selfRect.parent == null)
            {
                return;
            }

            RectTransform parentRect = (RectTransform)selfRect.parent;

            Camera cam = null;
            Canvas canvas = selfRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            Vector3 worldTop = currentTarget.TransformPoint(new Vector3(0f, currentTarget.rect.yMax, 0f));
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldTop);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint))
            {
                return;
            }

            float bob = Mathf.Sin(bobPhase * (Mathf.PI * 2f) / BobPeriodSeconds) * BobAmplitudePixels;
            float desiredY = localPoint.y + VerticalOffsetPixels + bob;

            // selfRect's pivot is bottom-center (0.5, 0), so anchoredPosition.y is the arrow's
            // TIP, not its visual top -- the top edge sits a further rect.height above that.
            // Two independent clamps can both apply (see PointAt's doc comment); the arrow
            // respects whichever produces the lower (more restrictive) ceiling.
            bool hasCeiling = false;
            float ceilingY = 0f;

            if (currentClampArea != null && TryGetLocalTopEdge(currentClampArea, parentRect, cam, out float clampTopY))
            {
                float candidate = clampTopY - selfRect.rect.height - ClampTopPaddingPixels;
                ceilingY = hasCeiling ? Mathf.Min(ceilingY, candidate) : candidate;
                hasCeiling = true;
            }

            if (currentClampBelowArea != null)
            {
                // 2026-09-30: the HUD header panel is always active in practice, but defensively
                // force its layout current before reading world corners -- a header that hasn't
                // had a layout pass yet (e.g. first frame after a resolution/safe-area change)
                // would otherwise report a stale or zero rect, silently disabling this clamp.
                LayoutRebuilder.ForceRebuildLayoutImmediate(currentClampBelowArea);

                if (TryGetLocalBottomEdge(currentClampBelowArea, parentRect, cam, out float clampBottomY))
                {
                    float candidate = clampBottomY - selfRect.rect.height - ClampTopPaddingPixels;
                    ceilingY = hasCeiling ? Mathf.Min(ceilingY, candidate) : candidate;
                    hasCeiling = true;
                }
            }

            if (hasCeiling && desiredY > ceilingY)
            {
                desiredY = ceilingY;
            }

            selfRect.anchoredPosition = new Vector2(localPoint.x, desiredY);
        }

        /// <summary>
        /// Finds the highest Y (in parentRect's local space) among `area`'s four world corners.
        /// Used to keep the arrow's own top edge from rising above e.g. a ScrollRect viewport's
        /// top -- the same boundary that already visually clips the target row, so it's a natural
        /// "don't go further than what's actually visible" bound rather than an arbitrary offset.
        /// </summary>
        private static bool TryGetLocalTopEdge(RectTransform area, RectTransform parentRect, Camera cam, out float topY)
        {
            area.GetWorldCorners(WorldCornersBuffer);
            bool any = false;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, WorldCornersBuffer[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint))
                {
                    continue;
                }

                any = true;
                if (localPoint.y > maxY)
                {
                    maxY = localPoint.y;
                }
            }

            topY = maxY;
            return any;
        }

        /// <summary>
        /// Mirrors TryGetLocalTopEdge but finds the LOWEST Y among `area`'s four world corners --
        /// used to keep the arrow's own top edge from rising above e.g. the HUD header's bottom
        /// edge (so the arrow stays clear of, not just "above the top of," a fixed HUD element it
        /// should never overlap at all).
        /// </summary>
        private static bool TryGetLocalBottomEdge(RectTransform area, RectTransform parentRect, Camera cam, out float bottomY)
        {
            area.GetWorldCorners(WorldCornersBuffer);
            bool any = false;
            float minY = float.PositiveInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, WorldCornersBuffer[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint))
                {
                    continue;
                }

                any = true;
                if (localPoint.y < minY)
                {
                    minY = localPoint.y;
                }
            }

            bottomY = minY;
            return any;
        }
    }
}
