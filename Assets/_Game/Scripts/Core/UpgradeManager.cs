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

        // -- Illumisnotti rewrite (2026-06-21): "lock one random building" timed events --
        private readonly List<(double bpps, double cps, float restoreAtTime)> activeBuildingLocks = new();
        private bool lockTickSubscribed;

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

        private void OnDestroy()
        {
            UnsubscribeLockTick();
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

        /// <summary>Returns true when the building's next purchase is priced in Cash.</summary>
        public static bool IsCashCost(BuildingData building)
        {
            return building != null && building.costType == CostType.Cash;
        }

        /// <summary>Returns the purchase cost for the next level of the given building.</summary>
        public double GetCurrentCost(BuildingData building)
        {
            if (building == null)
            {
                return double.MaxValue;
            }

            int level = GetBuildingLevel(building);
            return building.baseCost * Math.Pow(building.costMultiplier, level);
        }

        /// <summary>Sum of flat Brain-Power-per-tap bonuses from every owned level of every building with a non-zero tapBrainPowerPerLevel (currently only Apex Brain Greens). Read by PlayerTapHandler.OnTap each tap.</summary>
        public double GetTotalTapBrainPowerBonus()
        {
            double sum = 0d;
            foreach (BuildingData building in buildingTemplates)
            {
                if (building == null || building.tapBrainPowerPerLevel <= 0d) continue;
                sum += GetBuildingLevel(building) * building.tapBrainPowerPerLevel;
            }
            return sum;
        }

        /// <summary>Returns true when the player's cumulative Brain Power meets the building's unlock requirement.</summary>
        public bool IsUnlocked(BuildingData building)
        {
            ResolveReferences();
            return building != null && currencyManager != null && currencyManager.CumulativeBrainPower >= building.unlockCumulativeBrainPower;
        }

        /// <summary>Returns true when the player can afford the next purchase of the given building.</summary>
        public bool CanAffordBuilding(BuildingData building)
        {
            ResolveReferences();
            if (building == null || currencyManager == null || !IsUnlocked(building))
            {
                return false;
            }

            double cost = GetCurrentCost(building);
            return IsCashCost(building)
                ? currencyManager.CanAffordCash(cost)
                : currencyManager.CanAffordBrainPower(cost);
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
            bool spent = IsCashCost(building)
                ? currencyManager.SpendCash(cost)
                : currencyManager.SpendBrainPower(cost);
            if (!spent)
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

        /// <summary>
        /// Picks one random currently-owned building, temporarily suppresses its exact current
        /// BPPS/CPS contribution for durationSeconds via CurrencyManager.SuppressIdleBPPS/
        /// SuppressCashPerSecond, then automatically restores it on a later
        /// GameManager.OnSecondTick. Used by the "Ministry Inspection"/"Brawndo Spill"
        /// Illumisnotti events. No-ops if the player owns no buildings yet.
        /// </summary>
        public void LockRandomBuildingFor(float durationSeconds)
        {
            ResolveReferences();
            if (currencyManager == null)
            {
                return;
            }

            List<BuildingData> owned = new List<BuildingData>();
            for (int i = 0; i < buildingTemplates.Count; i++)
            {
                BuildingData building = buildingTemplates[i];
                if (building != null && GetBuildingLevel(building) > 0)
                {
                    owned.Add(building);
                }
            }

            if (owned.Count == 0)
            {
                return;
            }

            BuildingData target = owned[UnityEngine.Random.Range(0, owned.Count)];
            int level = GetBuildingLevel(target);
            double bpps = level * target.baseBrainPowerPerSecond;
            double cps = level * target.baseCashPerSecond;

            currencyManager.SuppressIdleBPPS(bpps);
            currencyManager.SuppressCashPerSecond(cps);

            activeBuildingLocks.Add((bpps, cps, Time.time + durationSeconds));
            SubscribeToGameTickForLocks();
        }

        private void SubscribeToGameTickForLocks()
        {
            if (lockTickSubscribed || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnSecondTick += HandleLockRestoreTick;
            lockTickSubscribed = true;
        }

        private void UnsubscribeLockTick()
        {
            if (lockTickSubscribed && GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleLockRestoreTick;
            }
            lockTickSubscribed = false;
        }

        /// <summary>Discards all pending building locks WITHOUT restoring their suppressed output,
        /// and stops the restore tick. Used by the rebirth/Snotting reset: ExecuteRebirth zeros idle
        /// BPPS/CPS wholesale, so a pending RestoreIdleBPPS/RestoreCashPerSecond (both additive) would
        /// inject a pre-reset building's production onto the zeroed baseline of the new run -- phantom
        /// income the player never earned. Clearing the locks here prevents that.</summary>
        private void ClearActiveBuildingLocks()
        {
            activeBuildingLocks.Clear();
            UnsubscribeLockTick();
        }

        private void HandleLockRestoreTick()
        {
            for (int i = activeBuildingLocks.Count - 1; i >= 0; i--)
            {
                (double bpps, double cps, float restoreAtTime) entry = activeBuildingLocks[i];
                if (Time.time < entry.restoreAtTime)
                {
                    continue;
                }

                currencyManager?.RestoreIdleBPPS(entry.bpps);
                currencyManager?.RestoreCashPerSecond(entry.cps);
                activeBuildingLocks.RemoveAt(i);
            }

            // Stop the per-second tick once the last lock has expired; a new lock re-subscribes via
            // SubscribeToGameTickForLocks (its duplicate guard makes re-subscription safe). Removing a
            // handler during its own OnSecondTick dispatch is safe -- the current invocation completes.
            if (activeBuildingLocks.Count == 0)
            {
                UnsubscribeLockTick();
            }
        }

        /// <summary>Clears all owned building levels back to baseline and notifies UI to refresh.
        /// Also discards any pending building locks (see ClearActiveBuildingLocks) so a lock that
        /// was active at Snotting time can't restore pre-reset production into the new run.</summary>
        public void ResetBuildings()
        {
            buildingLevels.Clear();
            ClearActiveBuildingLocks();
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
