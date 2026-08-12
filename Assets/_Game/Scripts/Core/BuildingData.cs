using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>Which currency a building purchase deducts from.</summary>
    public enum CostType
    {
        BrainPower,
        Cash
    }

    /// <summary>
    /// Authoring data for a purchasable idle building or structure.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "BrainDrain/Building Data")]
    public sealed class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key, independent of buildingName -- mirrors CashShopItemData.itemId/OutfitData.outfitId's role.")]
        public string buildingId;
        public string buildingName;
        [TextArea(2, 4)]
        public string description;

        [Header("Progression")]
        public double unlockCumulativeBrainPower;
        public CostType costType = CostType.BrainPower;
        public double baseCost = 10d;
        public double costMultiplier = 1.15d;

        [Header("Production")]
        public double baseBrainPowerPerSecond = 1d;
        [Tooltip("Cash per second per level. 0 for buildings that don't produce Cash (everything except Underground Economy, currently).")]
        public double baseCashPerSecond;

        /// <summary>Flat Brain-Power-per-tap added per owned level. 0 for every building except Apex Brain Greens (the sole tap scaler). Keeps tapping bounded by level x cost, not by idle income.</summary>
        public double tapBrainPowerPerLevel;
    }
}
