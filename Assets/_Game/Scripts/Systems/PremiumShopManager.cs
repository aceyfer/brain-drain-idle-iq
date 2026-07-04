using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    public enum PremiumItemType
    {
        UnlockProfanityPack,
        BonusNeurons
    }

    [System.Serializable]
    public sealed class PremiumItemData
    {
        public string itemId;
        public string displayName;
        public string description;
        public int neuronCost;
        public PremiumItemType itemType;
    }

    public sealed class PremiumShopManager : MonoBehaviour
    {
        [Header("Premium Catalog")]
        [SerializeField] private List<PremiumItemData> items = new List<PremiumItemData>();

        private readonly HashSet<string> purchasedItemIds = new HashSet<string>();

        private static PremiumShopManager instance;
        private static bool isShuttingDown;

        public static PremiumShopManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<PremiumShopManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var host = new GameObject("PremiumShopManager (Auto)");
                    instance = host.AddComponent<PremiumShopManager>();
                }
                return instance;
            }
        }

        public IReadOnlyList<PremiumItemData> Items => items;

        public event Action OnItemsChanged;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Load saved purchases
            LoadState();

            // Setup default items if list is empty
            if (items.Count == 0)
            {
                items.Add(new PremiumItemData
                {
                    itemId = "premium_profanity_pack",
                    displayName = "UNFILTERED PROFANITY PACK",
                    description = "Unlocks the Tier 3 unfiltered swear word dialogue lines in the narrator commentary! Go into the settings panel to toggle it on/off anytime.",
                    neuronCost = 50,
                    itemType = PremiumItemType.UnlockProfanityPack
                });
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        public bool IsItemPurchased(string itemId) => purchasedItemIds.Contains(itemId);

        public bool TryPurchaseItem(PremiumItemData item)
        {
            if (item == null || IsItemPurchased(item.itemId)) return false;

            CurrencyManager currency = CurrencyManager.Instance;
            if (currency == null || !currency.CanAffordNeurons(item.neuronCost)) return false;

            if (currency.SpendNeurons(item.neuronCost))
            {
                purchasedItemIds.Add(item.itemId);
                ApplyPremiumItemEffect(item);
                SaveState();
                OnItemsChanged?.Invoke();
                return true;
            }

            return false;
        }

        private void ApplyPremiumItemEffect(PremiumItemData item)
        {
            switch (item.itemType)
            {
                case PremiumItemType.UnlockProfanityPack:
                    if (RandomChatterManager.Instance != null)
                    {
                        RandomChatterManager.Instance.UnlockProfanity();
                        RandomChatterManager.Instance.ToggleProfanity(true);
                    }
                    break;
            }
        }

        private void SaveState()
        {
            string csv = string.Join(",", purchasedItemIds);
            PlayerPrefs.SetString("BrainDrain_PremiumPurchases", csv);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            purchasedItemIds.Clear();
            string saved = PlayerPrefs.GetString("BrainDrain_PremiumPurchases", "");
            if (!string.IsNullOrWhiteSpace(saved))
            {
                string[] split = saved.Split(',');
                foreach (var id in split)
                {
                    purchasedItemIds.Add(id);
                }
            }

            // Sync profanity state in case it was purchased previously
            if (purchasedItemIds.Contains("premium_profanity_pack"))
            {
                if (RandomChatterManager.Instance != null)
                {
                    RandomChatterManager.Instance.UnlockProfanity();
                }
            }
        }
    }
}