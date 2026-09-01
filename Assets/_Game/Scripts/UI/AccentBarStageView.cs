using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Same event-driven sprite-swap pattern as BackgroundStageView, generalized for any single
    /// UI Image that needs its own per-World-Restoration-stage art. BackgroundStageView stays
    /// dedicated to the full-screen Skyline background (photographic art, preserveAspect on);
    /// this is for smaller UI chrome strips like TopBG's status bar, which historically was just
    /// a flat solid color with no unique art of its own -- stretches to fill its rect instead of
    /// preserving aspect, since it's UI chrome, not a photo. Never polls stage in Update.
    ///
    /// 2026-08-31: added per Aceyfer's "the Top could use its own unique set" request. Generated/
    /// wired by ArtExpansionTool.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class AccentBarStageView : MonoBehaviour
    {
        [SerializeField] private Sprite[] stageSprites;

        private Image targetImage;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
            targetImage.preserveAspect = false;
            // This Image previously served as a flat solid-color panel (e.g. TopBG's near-black
            // fill) -- Image.color tints whatever sprite is assigned, so leaving that old flat
            // color in place would multiply our new art down to near-black. The art itself now
            // carries all the color; this Image should just show it unmodified.
            targetImage.color = Color.white;
            ApplyDefaultStageSprite();
        }

        private void ApplyDefaultStageSprite()
        {
            if (targetImage == null) { return; }
            if (stageSprites == null || stageSprites.Length == 0 || stageSprites[0] == null)
            {
                Debug.LogWarning("[AccentBarStageView] stageSprites[0] is missing.", this);
                return;
            }
            targetImage.sprite = stageSprites[0];
        }

        private void Start()
        {
            SubscribeToRestorationEvents();
            ApplyStageIndex(ResolveCurrentStageIndex());
        }

        private void OnDestroy() { UnsubscribeFromRestorationEvents(); }

        private void SubscribeToRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null) { return; }
            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
            manager.OnRestorationStageChanged += HandleRestorationStageChanged;
        }

        private void UnsubscribeFromRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null) { return; }
            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            ApplyStageIndex(stage != null ? stage.stageIndex : 0);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStage?.stageIndex ?? 0;
        }

        private void ApplyStageIndex(int index)
        {
            if (stageSprites == null || stageSprites.Length == 0 || targetImage == null) { return; }
            index = Mathf.Clamp(index, 0, stageSprites.Length - 1);
            Sprite sprite = stageSprites[index];
            if (sprite != null) { targetImage.sprite = sprite; }
        }
    }
}
