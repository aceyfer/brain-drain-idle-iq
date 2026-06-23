using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns Hot Chick companion progression: a single evolving sprite/character across 6
    /// sequential tiers (CompanionTierData), each gated and priced independently. Strictly
    /// sequential -- tier N requires already owning tier N-1, unlike the Cash Shop's 5
    /// independent one-off items (see CashShopManager). Each purchased tier permanently,
    /// additively grants its Cash-per-second percent bonus via
    /// CurrencyManager.AddShopCashMultiplierBonus.
    /// </summary>
    public sealed class CompanionManager : MonoBehaviour
    {
        [Header("Tiers")]
        [Tooltip("All 6 CompanionTierData assets, in any order -- resolved by tierIndex, not list order.")]
        [SerializeField] private List<CompanionTierData> tiers = new();

        // -- Hot Chick purchase system (2026-06-22) --
        // Deliberately separate from currentTier/tiers/CompanionTierData above: that system
        // grants permanent Cash-per-second bonuses per sequential tier asset; this one is a
        // flat 6-purchase counter (hotChickCount) that only extends the offline-BPPS-decay
        // window (see CurrencyManager.SetOfflineBPPSMultiplier/SaveManager's decay calc) and has
        // no Cash-bonus effect of its own. Kept in the same manager class per "use whichever
        // existing structure fits, do not create duplicate manager classes" rather than a new
        // HotChickManager, since this is conceptually still "the Hot Chick companion."
        private static readonly double[] HotChickPrices =
        {
            1_000_000d, 5_000_000d, 25_000_000d, 150_000_000d, 750_000_000d, 5_000_000_000d
        };
        private const int MaxHotChickCount = 6;

        private int hotChickCount;
        private int currentTier;

        private static CompanionManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static CompanionManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<CompanionManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("CompanionManager (Auto)");
                    instance = hostObject.AddComponent<CompanionManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of the configured tiers for UI population.</summary>
        public IReadOnlyList<CompanionTierData> Tiers => tiers;

        /// <summary>Currently owned tier, 0 if none purchased yet.</summary>
        public int CurrentTier => currentTier;

        /// <summary>Fired after a tier is successfully purchased or restored from a save. Passes the new tier index.</summary>
        public event Action<int> OnTierChanged;

        /// <summary>How many Hot Chicks have been purchased so far (0-6). Distinct from CurrentTier above.</summary>
        public int HotChickCount => hotChickCount;

        /// <summary>True once all 6 Hot Chicks are owned -- the purchase button should be disabled/hidden.</summary>
        public bool IsHotChickMaxed => hotChickCount >= MaxHotChickCount;

        /// <summary>Fired after a Hot Chick is successfully purchased or restored from a save. Passes the new count.</summary>
        public event Action<int> OnHotChickCountChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        /// <summary>Returns the next purchasable tier's data, or null if all 6 are owned or the list is misconfigured.</summary>
        public CompanionTierData GetNextTier()
        {
            foreach (CompanionTierData tier in tiers)
            {
                if (tier != null && tier.tierIndex == currentTier + 1)
                {
                    return tier;
                }
            }

            return null;
        }

        /// <summary>Returns true when the next tier's gate (real hours since first launch / RebirthCount / RebirthCount+WorldStage) is currently met.</summary>
        public bool IsNextTierGateMet()
        {
            CompanionTierData next = GetNextTier();
            return next != null && IsGateMet(next);
        }

        private static bool IsGateMet(CompanionTierData tier)
        {
            switch (tier.gateType)
            {
                case CompanionGateType.RealHoursSinceFirstLaunch:
                    long firstLaunch = SaveManager.Instance != null
                        ? SaveManager.Instance.FirstLaunchUnixSeconds
                        : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    double hoursSinceLaunch = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - firstLaunch) / 3600d;
                    return hoursSinceLaunch >= tier.gateRealHours;

                case CompanionGateType.RebirthCount:
                    return RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= tier.gateRebirthCount;

                case CompanionGateType.RebirthCountAndWorldStage:
                    bool rebirthMet = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= tier.gateRebirthCount;
                    return rebirthMet && IsWorldStageMet(tier.gateWorldStageIndex);

                default:
                    return false;
            }
        }

        private static bool IsWorldStageMet(int stageIndex)
        {
            WorldRestorationManager worldRestoration = WorldRestorationManager.Instance;
            if (worldRestoration == null)
            {
                return false;
            }

            IReadOnlyList<WorldRestorationStage> stages = worldRestoration.Stages;
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Count || stages[stageIndex] == null)
            {
                return false;
            }

            return worldRestoration.CumulativePointsSpentOnRestoration >= stages[stageIndex].pointsRequired;
        }

        /// <summary>Attempts to purchase the next sequential tier. Returns true on success.</summary>
        public bool TryPurchaseNextTier()
        {
            CompanionTierData next = GetNextTier();
            if (next == null || !IsGateMet(next))
            {
                return false;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null || !currencyManager.SpendCash(next.cost))
            {
                return false;
            }

            currentTier = next.tierIndex;
            currencyManager.AddShopCashMultiplierBonus(next.cashPerSecondPercent);
            OnTierChanged?.Invoke(currentTier);
            return true;
        }

        /// <summary>
        /// Directly restores the owned tier from a save file. Does NOT re-grant the tier's Cash
        /// multiplier bonus -- that's already folded into CurrencyManager's single saved
        /// shopCashMultiplier aggregate (see CurrencyManager.LoadShopMultipliers); re-applying it
        /// here would double-count.
        /// </summary>
        public void LoadState(int restoredTier)
        {
            currentTier = restoredTier;
            OnTierChanged?.Invoke(currentTier);
        }

        /// <summary>Returns the Cash price of the next Hot Chick purchase, or double.MaxValue if all 6 are already owned.</summary>
        public double GetNextHotChickPrice()
        {
            return hotChickCount < MaxHotChickCount ? HotChickPrices[hotChickCount] : double.MaxValue;
        }

        /// <summary>
        /// Attempts to purchase the next Hot Chick (flat price table, not CompanionTierData-
        /// driven). Returns true on success. Blocked only by insufficient Cash or already
        /// owning all 6 -- the Day-2 gate that used to apply to the first purchase was removed
        /// 2026-06-22.
        /// </summary>
        public bool TryPurchaseNextHotChick()
        {
            if (IsHotChickMaxed)
            {
                return false;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null || !currencyManager.SpendCash(HotChickPrices[hotChickCount]))
            {
                return false;
            }

            hotChickCount++;

            // "Recalculate the decay window immediately" -- the window itself is derived live
            // from hotChickCount each time SaveManager computes it (24h * (1 + hotChickCount)),
            // so incrementing hotChickCount above already updates it. The only direct action
            // needed here is resetting the multiplier to 1.0, since elapsedHours is trivially 0
            // for an online purchase happening right now.
            currencyManager.SetOfflineBPPSMultiplier(1f);

            OnHotChickCountChanged?.Invoke(hotChickCount);
            GameManager.Instance?.RequestSave();
            return true;
        }

        /// <summary>
        /// Directly restores hotChickCount from a save file. Separate from LoadState (which
        /// restores CurrentTier) since these are two independent pieces of state living in the
        /// same manager. Fires OnHotChickCountChanged so a subscribed HotChickSpawner can sync
        /// its spawned-sprite count to the restored value on load.
        /// </summary>
        public void LoadHotChickCount(int restoredCount)
        {
            hotChickCount = restoredCount;
            OnHotChickCountChanged?.Invoke(hotChickCount);
        }
    }
}
