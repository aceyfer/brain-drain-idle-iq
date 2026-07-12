using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// RETIRED BY DESIGN (Aceyfer, Jul 12 2026): COGS appears only inside the dialogue
    /// panel; a persistent world-area portrait just consumed screen space. This script now
    /// only asserts its Image off (presentation state is code-owned, Bible §8) because
    /// scene edits are embargoed -- the hosting GameObject and this script are ledgered in
    /// §19 for full removal in the post-embargo scene cleanup pass.
    /// </summary>
    public sealed class COGSWorldPortraitUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;

        private void Start()
        {
            if (portraitImage != null)
            {
                portraitImage.enabled = false;
            }
        }
    }
}
