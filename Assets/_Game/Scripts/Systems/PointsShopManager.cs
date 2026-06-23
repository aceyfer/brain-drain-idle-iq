using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns the 6 Shop 3 (Points Power Shop) Illumisnotti-dismantling items. The final item,
    /// The Grand Snotting (GrandSnottingCapstone), is a one-time capstone: applies a literal
    /// 10x multiplicative jump (not an additive percent like the other 5) to Brain
    /// Power/Cash/tap, and permanently sets SecretEndingUnlocked -- no actual ending
    /// sequence/UI exists yet, the flag exists for a future one to check.
    /// </summary>
    public sealed class PointsShopManager : MonoBehaviour
    {
        private const double GrandSnottingMultiplierFactor = 10d;

        [Header("Items")]
        [SerializeField] private List<PointsShopItemData> items = new();

        private readonly HashSet<string> ownedItemIds = new();

        private static PointsShopManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static PointsShopManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<PointsShopManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("PointsShopManager (Auto)");
                    instance = hostObject.AddComponent<PointsShopManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of the configured items for UI population.</summary>
        public IReadOnlyList<PointsShopItemData> Items => items;

        /// <summary>True once The Grand Snotting has been purchased. No ending sequence consumes this yet.</summary>
        public bool SecretEndingUnlocked { get; private set; }

        /// <summary>Fired after an item is successfully purchased or the owned set is restored from a save.</summary>
        public event Action OnItemsChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public bool IsItemOwned(PointsShopItemData item) => item != null && ownedItemIds.Contains(item.itemId);

        public bool IsItemUnlocked(PointsShopItemData item)
        {
            if (item == null)
            {
                return false;
            }

            bool rebirthMet = RebirthManager.Instance == null || RebirthManager.Instance.RebirthCount >= item.gateRebirthCount;
            bool stageMet = item.gateWorldStageIndex < 0 || IsWorldStageMet(item.gateWorldStageIndex);
            return rebirthMet && stageMet;
        }

        private static bool IsWorldStageMet(int stageIndex)
        {
            WorldRestorationManager worldRestoration = WorldRestorationManager.Instance;
            if (worldRestoration == null)
            {
                return false;
            }

            IReadOnlyList<WorldRestorationStage> stages = worldRestoration.Stages;
            if (stages == null || stageIndex >= stages.Count || stages[stageIndex] == null)
            {
                return false;
            }

            return worldRestoration.CumulativePointsSpentOnRestoration >= stages[stageIndex].pointsRequired;
        }

        /// <summary>Attempts a one-time purchase of a Points Shop item. Returns true on success.</summary>
        public bool TryPurchaseItem(PointsShopItemData item)
        {
            if (item == null || IsItemOwned(item) || !IsItemUnlocked(item))
            {
                return false;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null || !currencyManager.SpendPoints(item.cost))
            {
                return false;
            }

            ownedItemIds.Add(item.itemId);
            ApplyItemEffect(currencyManager, item);

            if (item.unlocksSecretEnding)
            {
                SecretEndingUnlocked = true;
            }

            // Priority, not queued: a purchase reaction shouldn't wait behind background
            // narrative lines the way DialogueManager.EnqueueDirectLine's queue would make it.
            if (!string.IsNullOrWhiteSpace(item.cogsReactionLine))
            {
                DialogueManager.Instance?.ShowPriorityLine(item.cogsReactionLine);
            }

            OnItemsChanged?.Invoke();
            return true;
        }

        private static void ApplyItemEffect(CurrencyManager currencyManager, PointsShopItemData item)
        {
            switch (item.effectType)
            {
                case PointsShopEffectType.PointsConversionRatePercent:
                    currencyManager.AddPointsConversionRateBonus(item.effectPercent);
                    break;

                case PointsShopEffectType.CashToPointsConversionPercent:
                    currencyManager.AddShopCashToPointsBonus(item.effectPercent);
                    break;

                case PointsShopEffectType.AllPointGainsPercent:
                    currencyManager.AddShopAllPointGainsBonus(item.effectPercent);
                    break;

                case PointsShopEffectType.GrandSnottingCapstone:
                    currencyManager.MultiplyShopAllMultiplier(GrandSnottingMultiplierFactor);
                    PlayerTapHandler playerTapHandler = PlayerTapHandler.Instance;
                    if (playerTapHandler != null)
                    {
                        playerTapHandler.SetTapMultiplier(playerTapHandler.TapMultiplier * GrandSnottingMultiplierFactor);
                    }

                    break;
            }
        }

        /// <summary>
        /// Restores which items are owned (for UI display) and SecretEndingUnlocked. Does NOT
        /// re-apply any item's effect -- those are already folded into CurrencyManager's saved
        /// multiplier aggregates and PlayerTapHandler's saved tapMultiplier; re-applying here,
        /// especially The Grand Snotting's 10x jump, would double-count on every load.
        /// </summary>
        public void LoadState(IEnumerable<string> restoredOwnedItemIds, bool restoredSecretEndingUnlocked)
        {
            ownedItemIds.Clear();

            if (restoredOwnedItemIds != null)
            {
                foreach (string itemId in restoredOwnedItemIds)
                {
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        ownedItemIds.Add(itemId);
                    }
                }
            }

            SecretEndingUnlocked = restoredSecretEndingUnlocked;
            OnItemsChanged?.Invoke();
        }
    }
}
