using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Full-screen ambient haze tint driven by the current World Restoration stage -- thick
    /// brown-toxic smog at stage 0, thinning to clear air by stage 5, same arc as the backdrop
    /// art. Event-driven off WorldRestorationManager.OnRestorationStageChanged, same pattern as
    /// BackgroundStageView (never polls stage in Update). A flat tinted Image rather than
    /// particles, per §61's mobile-performance guidance -- upgrade to layered soft-noise
    /// particles later if this reads as too static.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class WeatherAtmosphereView : MonoBehaviour
    {
        // Stage 0..5, dystopia -> utopia. Claude Code's judgment call for §61, not yet
        // Aceyfer-approved in detail -- flag before shipping if this needs adjusting.
        // Alpha column lowered 2026-09-02 (same proportional arc, ~60% of the original peak):
        // CustomSafeArea's CurrencyHeader/CashText renders on top of this layer (confirmed by
        // Canvas sibling order, not a z-order bug), but at CanvasScaler "Scale With Screen Size"
        // + a small non-maximized Editor Game view, the low actual render resolution combined
        // with CashText's 14-18pt auto-sizing left too little contrast margin against the
        // original 0.34 peak alpha. Real devices always render at native resolution (no
        // analogous shrunk-window case), so this was a mild, not severe, regression there --
        // tightened anyway per Aceyfer's call.
        private static readonly Color[] StageHazeColor =
        {
            new Color(0.45f, 0.36f, 0.14f, 0.20f), // 0 -- thick brown-toxic haze
            new Color(0.44f, 0.38f, 0.20f, 0.16f), // 1
            new Color(0.42f, 0.42f, 0.34f, 0.11f), // 2
            new Color(0.55f, 0.60f, 0.56f, 0.07f), // 3
            new Color(0.75f, 0.84f, 0.88f, 0.04f), // 4
            new Color(1f, 1f, 1f, 0f),              // 5 -- clear air
        };

        private Image hazeImage;

        private void Awake()
        {
            hazeImage = GetComponent<Image>();
            // Own it in code, don't trust scene state -- see §59 (BackgroundStageView shipped
            // the same class of bug by never setting Image.enabled explicitly).
            hazeImage.enabled = true;
            hazeImage.raycastTarget = false;
            ApplyStageColor(0);
        }

        private void Start()
        {
            SubscribeToRestorationEvents();
            ApplyStageColor(ResolveCurrentStageIndex());
        }

        private void OnDestroy()
        {
            UnsubscribeFromRestorationEvents();
        }

        private void SubscribeToRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
            manager.OnRestorationStageChanged += HandleRestorationStageChanged;
        }

        private void UnsubscribeFromRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            ApplyStageColor(stage != null ? stage.stageIndex : 0);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStageIndex ?? 0;
        }

        private void ApplyStageColor(int index)
        {
            if (hazeImage == null)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, StageHazeColor.Length - 1);
            hazeImage.color = StageHazeColor[index];
        }
    }
}
