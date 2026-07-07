using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Event-driven view that swaps the main Canvas background Image to match the active
    /// World Restoration stage (BG1–BG6). Never polls stage in Update (BD-1/BD-2).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class BackgroundStageView : MonoBehaviour
    {
        [SerializeField] private Sprite[] stageSprites;

        private Image backgroundImage;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = true;
            ApplyDefaultStageSprite();
        }

        private void ApplyDefaultStageSprite()
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (stageSprites == null || stageSprites.Length == 0 || stageSprites[0] == null)
            {
                Debug.LogWarning("[BackgroundStageView] stageSprites[0] is missing; background may flash white until Start.", this);
                return;
            }

            backgroundImage.sprite = stageSprites[0];
        }

        private void Start()
        {
            SubscribeToRestorationEvents();
            ApplyStageIndex(ResolveCurrentStageIndex());
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
            ApplyStageIndex(stage != null ? stage.stageIndex : 0);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStage?.stageIndex ?? 0;
        }

        private void ApplyStageIndex(int index)
        {
            if (stageSprites == null || stageSprites.Length == 0 || backgroundImage == null)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, stageSprites.Length - 1);
            Sprite sprite = stageSprites[index];
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
            }
        }
    }
}
