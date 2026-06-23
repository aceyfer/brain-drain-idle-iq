using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>Which permanent bonus a Points Shop item grants on purchase.</summary>
    public enum PointsShopEffectType
    {
        PointsConversionRatePercent,
        CashToPointsConversionPercent,
        AllPointGainsPercent,
        GrandSnottingCapstone
    }

    /// <summary>
    /// Authoring data for one Shop 3 (Points Power Shop) Illumisnotti-dismantling item. The
    /// final item, The Grand Snotting (GrandSnottingCapstone), is a one-time capstone purchase:
    /// PointsShopManager applies it as a literal 10x multiplicative jump (not an additive
    /// percent like the other 5) across Brain Power/Cash/tap, and sets the permanent
    /// SecretEndingUnlocked save flag -- there is no actual ending sequence/UI built yet, the
    /// flag exists for a future one to check.
    /// </summary>
    [CreateAssetMenu(fileName = "PointsShopItemData", menuName = "BrainDrain/Points Shop Item")]
    public sealed class PointsShopItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Cost & Gate")]
        public double cost;
        [Tooltip("Minimum RebirthCount required. 0 = no gate.")]
        public int gateRebirthCount;
        [Tooltip("Minimum World Restoration stage index required. -1 = no gate.")]
        public int gateWorldStageIndex = -1;

        [Header("Effect")]
        public PointsShopEffectType effectType;
        [Tooltip("e.g. 0.10 for +10%. Unused (ignored) for GrandSnottingCapstone, which is hardcoded to a 10x jump.")]
        public float effectPercent;
        public bool unlocksSecretEnding;

        [Header("Narrative")]
        [Tooltip("COGS's reaction line on purchase, shown immediately via DialogueManager.ShowPriorityLine. Added 2026-06-22.")]
        [TextArea(2, 4)]
        public string cogsReactionLine;
    }
}
