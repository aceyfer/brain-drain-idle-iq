using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>Serializable DTO for persisting one building's owned level (see SaveManager).</summary>
    [Serializable]
    public struct BuildingSaveEntry
    {
        public string buildingName;
        public int level;
    }

    /// <summary>
    /// Manages building ownership and purchases. Each purchased level registers its BPPS
    /// contribution once via CurrencyManager.AddIdleBPPS, which pays out on the single global
    /// per-second tick rather than a separate per-frame production loop, and bumps PlayerIQ.
    /// </summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        /// <summary>Flat PlayerIQ granted per building level purchased (any tier).</summary>
        private const float PlayerIQGainPerPurchase = 1f;

        [Header("Dependencies")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private PlayerIQManager playerIQManager;

        [Header("Building Templates")]
        [SerializeField] private List<BuildingData> buildingTemplates = new();

        private readonly Dictionary<string, int> buildingLevels = new(16);

        /// <summary>Convenient scene-lookup accessor, since GameManager does not hub this reference.</summary>
        public static UpgradeManager Instance => FindAnyObjectByType<UpgradeManager>();

        /// <summary>Read-only view of owned building levels keyed by building name.</summary>
        public IReadOnlyDictionary<string, int> BuildingLevels => buildingLevels;

        /// <summary>Read-only view of the configured building templates for UI population.</summary>
        public IReadOnlyList<BuildingData> BuildingTemplates => buildingTemplates;

        /// <summary>Fired after a building is successfully purchased so UI can refresh.</summary>
        public event Action OnBuildingsChanged;

        /// <summary>Fired after a building is successfully purchased. Passes the purchased building's data.</summary>
        public event Action<BuildingData> OnBuildingPurchased;

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>Returns the current owned level for a building template.</summary>
        public int GetBuildingLevel(BuildingData building)
        {
            if (building == null || string.IsNullOrWhiteSpace(building.buildingName))
            {
                return 0;
            }

            return buildingLevels.TryGetValue(building.buildingName, out int level) ? level : 0;
        }

        /// <summary>Returns the Brain Power cost for the next purchase of the given building.</summary>
        public double GetCurrentCost(BuildingData building)
        {
            if (building == null)
            {
                return double.MaxValue;
            }

            int level = GetBuildingLevel(building);
            return building.baseCost * Math.Pow(building.costMultiplier, level);
        }

        /// <summary>Returns true when the player's cumulative Brain Power meets the building's unlock requirement.</summary>
        public bool IsUnlocked(BuildingData building)
        {
            ResolveReferences();
            return building != null && currencyManager != null && currencyManager.CumulativeBrainPower >= building.unlockCumulativeBrainPower;
        }

        /// <summary>
        /// Attempts to purchase one level of a building after validating unlock and cost.
        /// On success, registers the building's BPPS contribution as permanent idle income
        /// and grants a flat PlayerIQ bump.
        /// </summary>
        public void TryBuyBuilding(BuildingData building)
        {
            if (building == null || string.IsNullOrWhiteSpace(building.buildingName))
            {
                Debug.LogWarning("[UpgradeManager] TryBuyBuilding ignored: invalid building data.", this);
                return;
            }

            ResolveReferences();

            if (currencyManager == null)
            {
                Debug.LogWarning("[UpgradeManager] TryBuyBuilding failed: missing core references.", this);
                return;
            }

            if (currencyManager.CumulativeBrainPower < building.unlockCumulativeBrainPower)
            {
                return;
            }

            double cost = GetCurrentCost(building);
            if (!currencyManager.SpendBrainPower(cost))
            {
                return;
            }

            buildingLevels.TryGetValue(building.buildingName, out int level);
            buildingLevels[building.buildingName] = level + 1;
            currencyManager.AddIdleBPPS(building.baseBrainPowerPerSecond);
            currencyManager.AddCashPerSecond(building.baseCashPerSecond);
            playerIQManager?.ModifyPlayerIQ(PlayerIQGainPerPurchase);

            OnBuildingsChanged?.Invoke();
            OnBuildingPurchased?.Invoke(building);
        }

        /// <summary>Clears all owned building levels back to baseline and notifies UI to refresh.</summary>
        public void ResetBuildings()
        {
            buildingLevels.Clear();
            OnBuildingsChanged?.Invoke();
        }

        /// <summary>
        /// Restores building ownership from save data and re-derives the idle BPPS/CPS those
        /// levels produce (both are otherwise only ever built incrementally via AddIdleBPPS/
        /// AddCashPerSecond at purchase time, so a direct dictionary restore alone would leave
        /// restored buildings generating zero income until the next purchase).
        /// </summary>
        public void LoadBuildingLevels(IEnumerable<BuildingSaveEntry> savedLevels)
        {
            ResolveReferences();

            buildingLevels.Clear();

            if (savedLevels != null)
            {
                foreach (BuildingSaveEntry entry in savedLevels)
                {
                    if (string.IsNullOrWhiteSpace(entry.buildingName) || entry.level <= 0)
                    {
                        continue;
                    }

                    buildingLevels[entry.buildingName] = entry.level;
                }
            }

            if (currencyManager != null)
            {
                for (int i = 0; i < buildingTemplates.Count; i++)
                {
                    BuildingData building = buildingTemplates[i];
                    if (building == null || !buildingLevels.TryGetValue(building.buildingName, out int level) || level <= 0)
                    {
                        continue;
                    }

                    currencyManager.AddIdleBPPS(level * building.baseBrainPowerPerSecond);
                    currencyManager.AddCashPerSecond(level * building.baseCashPerSecond);
                }
            }

            OnBuildingsChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (currencyManager == null && gameManager != null)
            {
                currencyManager = gameManager.Currency;
            }

            if (currencyManager == null)
            {
                currencyManager = CurrencyManager.Instance;
            }

            if (playerIQManager == null && gameManager != null)
            {
                playerIQManager = gameManager.PlayerIQSystem;
            }

            if (playerIQManager == null)
            {
                playerIQManager = PlayerIQManager.Instance;
            }
        }
    }
}
