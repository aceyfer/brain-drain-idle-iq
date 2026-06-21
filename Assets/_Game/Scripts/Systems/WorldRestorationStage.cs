using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Authoring data for one stage of World Restoration: the dystopia-to-utopia visual
    /// transformation driven by cumulative Points spent on restoration. stageIndex maps
    /// directly to WorldRestorationManager.restorationStageObjects' sibling index, the same way
    /// RankDefinition's array position maps to DioramaManager's backdrop index.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldRestorationStage", menuName = "BrainDrain/World Restoration Stage")]
    public sealed class WorldRestorationStage : ScriptableObject
    {
        public int stageIndex;
        public string stageName;
        public double pointsRequired;
    }
}
