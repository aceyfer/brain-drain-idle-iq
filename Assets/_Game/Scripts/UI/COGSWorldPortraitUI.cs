using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Keeps a persistent world-area Image in sync with COGSPortraitController's current
    /// stage. Distinct from DialogueDisplayUI's own avatarImage, which only exists inside the
    /// transient dialogue panel and slides in/out with each line -- this is COGS's actual
    /// presence in the world area, always visible regardless of whether she's currently
    /// "talking". Both can coexist; this doesn't replace DialogueDisplayUI's avatar slot.
    /// </summary>
    public sealed class COGSWorldPortraitUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;

        private void Start()
        {
            if (COGSPortraitController.Instance != null)
            {
                COGSPortraitController.Instance.OnStageChanged.AddListener(HandleStageChanged);

                // Covers both Start() orderings: if COGSPortraitController already resolved its
                // initial stage before this ran, apply it now. If not, the listener above will
                // catch it once COGSPortraitController.Start() runs (still within Awake-then-
                // Start ordering guarantees for the same frame).
                if (COGSPortraitController.Instance.CurrentStage != null)
                {
                    HandleStageChanged(COGSPortraitController.Instance.CurrentStage);
                }
            }
        }

        private void OnDestroy()
        {
            if (COGSPortraitController.Instance != null)
            {
                COGSPortraitController.Instance.OnStageChanged.RemoveListener(HandleStageChanged);
            }
        }

        private void HandleStageChanged(COGSStage stage)
        {
            if (portraitImage != null && stage != null)
            {
                portraitImage.sprite = stage.portraitSprite;
            }
        }
    }
}
