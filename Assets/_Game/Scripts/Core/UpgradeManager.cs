using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Manages building ownership, purchases, and per-frame passive production yields.
    /// </summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Building Templates")]
        [SerializeField] private List<BuildingData> buildingTemplates = new();

        private readonly Dictionary<string, int> buildingLevels = new(16);

        private IQDecaySystem iqDecaySystem;

        /// <summary>Read-only view of owned building levels keyed by building name.</summary>
        public IReadOnlyDictionary<string, int> BuildingLevels => buildingLevels;

        /// <summary>Read-only view of the configured building templates for UI population.</summary>
        public IReadOnlyList<BuildingData> BuildingTemplates => buildingTemplates;

        /// <summary>Fired after a building is successfully purchased so UI can refresh.</summary>
        public event Action OnBuildingsChanged;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (currencyManager == null || iqDecaySystem == null || buildingTemplates.Count == 0)
            {
                return;
            }

            double brainsPerSecond = 0d;
            double iqRecoveryPerSecond = 0d;

            for (int i = 0; i < buildingTemplates.Count; i++)
            {
                BuildingData building = buildingTemplates[i];
                if (building == null || string.IsNullOrWhiteSpace(building.buildingName))
                {
                    continue;
                }

                if (!buildingLevels.TryGetValue(building.buildingName, out int level) || level <= 0)
                {
                    continue;
                }

                brainsPerSecond += level * building.baseBrainsPerSecond;
                iqRecoveryPerSecond += level * building.iqRecoveryPerSecond;
            }

            if (brainsPerSecond <= 0d && iqRecoveryPerSecond <= 0d)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            if (brainsPerSecond > 0d)
            {
                currencyManager.AddBrains(brainsPerSecond * deltaTime);
            }

            if (iqRecoveryPerSecond > 0d)
            {
                iqDecaySystem.RestoreIQ((float)(iqRecoveryPerSecond * deltaTime));
            }
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

        /// <summary>Returns the Brains cost for the next purchase of the given building.</summary>
        public double GetCurrentCost(BuildingData building)
        {
            if (building == null)
            {
                return double.MaxValue;
            }

            int level = GetBuildingLevel(building);
            return building.baseCost * Math.Pow(building.costMultiplier, level);
        }

        /// <summary>
        /// Attempts to purchase one level of a building after validating level unlock and cost.
        /// </summary>
        public void TryBuyBuilding(BuildingData building)
        {
            if (building == null || string.IsNullOrWhiteSpace(building.buildingName))
            {
                Debug.LogWarning("[UpgradeManager] TryBuyBuilding ignored: invalid building data.", this);
                return;
            }

            ResolveReferences();

            if (currencyManager == null || iqDecaySystem == null)
            {
                Debug.LogWarning("[UpgradeManager] TryBuyBuilding failed: missing core references.", this);
                return;
            }

            if (iqDecaySystem.CurrentLevel < building.unlockPlayerLevel)
            {
                return;
            }

            double cost = GetCurrentCost(building);
            if (!currencyManager.SpendBrains(cost))
            {
                return;
            }

            buildingLevels.TryGetValue(building.buildingName, out int level);
            buildingLevels[building.buildingName] = level + 1;

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

            if (iqDecaySystem == null && gameManager != null)
            {
                iqDecaySystem = gameManager.IQDecay;
            }

            if (iqDecaySystem == null)
            {
                iqDecaySystem = FindAnyObjectByType<IQDecaySystem>();
            }
        }
    }
}
