using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>Which kind of gate must be met before a companion tier can be purchased.</summary>
    public enum CompanionGateType
    {
        RealHoursSinceFirstLaunch,
        RebirthCount,
        RebirthCountAndWorldStage
    }

    /// <summary>
    /// Authoring data for one tier of the Hot Chick companion -- THE evolving Cash Shop
    /// centerpiece: a single sprite that visually evolves across 6 sequential tiers, each gated
    /// and priced independently, rather than 6 separate one-off items. CompanionManager owns
    /// progression (must purchase tier N-1 before N is purchasable) and grants each tier's
    /// CashPerSecond percent bonus permanently, additively, on purchase.
    /// </summary>
    [CreateAssetMenu(fileName = "CompanionTierData", menuName = "BrainDrain/Companion Tier")]
    public sealed class CompanionTierData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("1-6, sequential. CompanionManager requires owning tier N-1 before N is purchasable.")]
        public int tierIndex;
        [TextArea(2, 4)]
        public string quote;

        [Header("Cost & Gate")]
        public double cost;
        public CompanionGateType gateType;
        [Tooltip("Used when gateType is RealHoursSinceFirstLaunch -- real-world hours since SaveManager.PlayerData.firstLaunchUnixSeconds.")]
        public float gateRealHours;
        [Tooltip("Used when gateType is RebirthCount or RebirthCountAndWorldStage.")]
        public int gateRebirthCount;
        [Tooltip("Used when gateType is RebirthCountAndWorldStage -- index into WorldRestorationManager.Stages.")]
        public int gateWorldStageIndex;

        [Header("Effect")]
        [Tooltip("Permanent, additive Cash-per-second percent bonus granted on purchase, e.g. 0.10 for +10%.")]
        public float cashPerSecondPercent;
    }
}
