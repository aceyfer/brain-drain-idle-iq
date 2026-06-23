using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>Which permanent bonus a Cash Shop item grants on purchase.</summary>
    public enum CashShopEffectType
    {
        BrainPowerTapPercent,
        CashPerSecondPercent,
        AllMultipliersPercent
    }

    /// <summary>
    /// Authoring data for one Shop 2 (Cash Shop) cosmetic/multiplier item -- the 5 generic,
    /// one-time permanent purchases (Golden Cardboard Crown, Shivering Designer Micro-Dog,
    /// Overpriced Branded Sneakers, Private VIP Velvet Rope, Solid Gold Gaming Throne). The Hot
    /// Chick companion is a separate, sequential-tier system -- see CompanionTierData -- since
    /// it evolves a single sprite across 6 tiers rather than being a flat list of independent
    /// one-off items like these.
    /// </summary>
    [CreateAssetMenu(fileName = "CashShopItemData", menuName = "BrainDrain/Cash Shop Item")]
    public sealed class CashShopItemData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, independent of displayName -- mirrors BuildingData.buildingName/OutfitData.outfitId's role.")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Cost & Gate")]
        public double cost;
        [Tooltip("Minimum RebirthCount required to purchase. 0 = no gate.")]
        public int gateRebirthCount;

        [Header("Effect")]
        public CashShopEffectType effectType;
        [Tooltip("e.g. 0.05 for +5%.")]
        public float effectPercent;
    }
}
