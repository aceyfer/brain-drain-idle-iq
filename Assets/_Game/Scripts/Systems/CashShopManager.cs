using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns the 5 generic, one-time Cash Shop items (Golden Cardboard Crown, Shivering
    /// Designer Micro-Dog, Overpriced Branded Sneakers, Private VIP Velvet Rope, Solid Gold
    /// Gaming Throne) -- independent purchases, unlike the Hot Chick companion's strictly
    /// sequential tiers (see CompanionManager).
    /// </summary>
    public sealed class CashShopManager : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private List<CashShopItemData> items = new();

        private readonly HashSet<string> ownedItemIds = new();

        private static CashShopManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static CashShopManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<CashShopManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("CashShopManager (Auto)");
                    instance = hostObject.AddComponent<CashShopManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of the configured items for UI population.</summary>
        public IReadOnlyList<CashShopItemData> Items => items;

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

        public bool IsItemOwned(CashShopItemData item) => item != null && ownedItemIds.Contains(item.itemId);

        public bool IsItemUnlocked(CashShopItemData item)
        {
            if (item == null)
            {
                return false;
            }

            return RebirthManager.Instance == null || RebirthManager.Instance.RebirthCount >= item.gateRebirthCount;
        }

        /// <summary>Attempts a one-time purchase of a generic Cash Shop item. Returns true on success.</summary>
        public bool TryPurchaseItem(CashShopItemData item)
        {
            if (item == null || IsItemOwned(item) || !IsItemUnlocked(item))
            {
                return false;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null || !currencyManager.SpendCash(item.cost))
            {
                return false;
            }

            ownedItemIds.Add(item.itemId);
            ApplyItemEffect(currencyManager, item);
            OnItemsChanged?.Invoke();
            return true;
        }

        private static void ApplyItemEffect(CurrencyManager currencyManager, CashShopItemData item)
        {
            switch (item.effectType)
            {
                case CashShopEffectType.BrainPowerTapPercent:
                    PlayerTapHandler.Instance?.AddTapMultiplier(item.effectPercent);
                    break;

                case CashShopEffectType.CashPerSecondPercent:
                    currencyManager.AddShopCashMultiplierBonus(item.effectPercent);
                    break;

                case CashShopEffectType.AllMultipliersPercent:
                    currencyManager.AddShopAllMultiplierBonus(item.effectPercent);
                    PlayerTapHandler.Instance?.AddTapMultiplier(item.effectPercent);
                    break;
            }
        }

        /// <summary>
        /// Restores which items are owned, for UI display ("already owned") purposes only. Does
        /// NOT re-apply each item's effect -- those are already folded into CurrencyManager's
        /// saved shopCashMultiplier/shopAllMultiplier aggregates and PlayerTapHandler's saved
        /// tapMultiplier (see CurrencyManager.LoadShopMultipliers); re-applying here would
        /// double-count every owned item's bonus on every load.
        /// </summary>
        public void LoadState(IEnumerable<string> restoredOwnedItemIds)
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

            OnItemsChanged?.Invoke();
        }
    }
}
