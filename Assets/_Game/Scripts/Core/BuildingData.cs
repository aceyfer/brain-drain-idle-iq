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

        /// <summary>
        /// One stage's display-name/description override. stageIndex matches
        /// WorldRestorationStage.stageIndex (0-5). displayName/description are independently
        /// nullable -- see the evolutions field's tooltip below for inherit semantics.
        /// </summary>
        [System.Serializable]
        public class BuildingEvolution
        {
            public int stageIndex;
            public string displayName;
            [TextArea(2, 5)]
            public string description;
        }

        [Header("World Restoration Evolution")]
        [Tooltip("Per-stage overrides for displayName/description. Null/empty on either field " +
            "means \"inherit the last non-empty value from an earlier stage\" -- name and " +
            "description inherit independently. An empty array is a no-op: GetDisplayName/" +
            "GetDescription fall back to buildingName/description exactly as before this field " +
            "existed. Not required to be sorted or contiguous by stageIndex.")]
        public BuildingEvolution[] evolutions;

        /// <summary>
        /// Resolves the display name for a given World Restoration stage: the highest
        /// stageIndex &lt;= worldStageIndex in evolutions with a non-empty displayName wins,
        /// falling back to buildingName if none qualifies (including when evolutions is
        /// null/empty). Pure -- no side effects, no singleton access.
        /// </summary>
        public string GetDisplayName(int worldStageIndex)
        {
            int clampedStage = Mathf.Max(0, worldStageIndex);
            string resolved = null;
            int resolvedStageIndex = -1;

            if (evolutions != null)
            {
                for (int i = 0; i < evolutions.Length; i++)
                {
                    BuildingEvolution evo = evolutions[i];
                    if (evo == null || evo.stageIndex < 0 || evo.stageIndex > clampedStage)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(evo.displayName))
                    {
                        continue;
                    }
                    if (evo.stageIndex >= resolvedStageIndex)
                    {
                        resolvedStageIndex = evo.stageIndex;
                        resolved = evo.displayName;
                    }
                }
            }

            return string.IsNullOrEmpty(resolved) ? buildingName : resolved;
        }

        /// <summary>
        /// Same resolution as GetDisplayName, run independently for description. Deliberately
        /// not sharing a helper with GetDisplayName -- name and description inherit separately
        /// and must never accidentally couple.
        /// </summary>
        public string GetDescription(int worldStageIndex)
        {
            int clampedStage = Mathf.Max(0, worldStageIndex);
            string resolved = null;
            int resolvedStageIndex = -1;

            if (evolutions != null)
            {
                for (int i = 0; i < evolutions.Length; i++)
                {
                    BuildingEvolution evo = evolutions[i];
                    if (evo == null || evo.stageIndex < 0 || evo.stageIndex > clampedStage)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(evo.description))
                    {
                        continue;
                    }
                    if (evo.stageIndex >= resolvedStageIndex)
                    {
                        resolvedStageIndex = evo.stageIndex;
                        resolved = evo.description;
                    }
                }
            }

            return string.IsNullOrEmpty(resolved) ? description : resolved;
        }
    }
}
