using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns the 5 God Tier Store items -- real-money-only, cosmetics/QoL, never power. NO real
    /// payment processing exists in this project (no Unity IAP package, no App Store/Play Store
    /// product IDs configured) -- StubPurchase grants the item immediately and is a clearly
    /// marked placeholder for real IAP integration, not a working purchase flow. Wire a real IAP
    /// plugin's purchase-success callback to call StubPurchase before shipping; do not ship this
    /// as-is, since right now anyone can "buy" these for free.
    /// </summary>
    public sealed class GodTierStoreManager : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private List<GodTierStoreItemData> items = new();

        private readonly HashSet<string> ownedItemIds = new();
        private float offlineExtensionHoursGranted;

        private static GodTierStoreManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static GodTierStoreManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<GodTierStoreManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("GodTierStoreManager (Auto)");
                    instance = hostObject.AddComponent<GodTierStoreManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of the configured items for UI population.</summary>
        public IReadOnlyList<GodTierStoreItemData> Items => items;

        public bool CogsVoicepackDisdainOwned { get; private set; }
        public bool Y2KGlitchSlumThemeOwned { get; private set; }
        public bool IllumisnottiMembershipCardOwned { get; private set; }
        public bool HolographicTrashCanFlexOwned { get; private set; }
        public float OfflineExtensionHoursGranted => offlineExtensionHoursGranted;

        /// <summary>Fired after an item is successfully (stub-)purchased or the owned set is restored from a save.</summary>
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

        public bool IsItemOwned(GodTierStoreItemData item) => item != null && ownedItemIds.Contains(item.itemId);

        /// <summary>
        /// PLACEHOLDER -- does not charge real money. Grants the item immediately. Call this
        /// from a real IAP plugin's purchase-success callback once one is integrated; until
        /// then, calling it directly (e.g. from a "Buy" button) gives the item away for free.
        /// </summary>
        public bool StubPurchase(GodTierStoreItemData item)
        {
            if (item == null || IsItemOwned(item))
            {
                return false;
            }

            ownedItemIds.Add(item.itemId);
            ApplyItemEffect(item);
            OnItemsChanged?.Invoke();
            return true;
        }

        private void ApplyItemEffect(GodTierStoreItemData item)
        {
            switch (item.effectType)
            {
                case GodTierStoreEffectType.VoicepackDisdain:
                    CogsVoicepackDisdainOwned = true;
                    break;

                case GodTierStoreEffectType.UIThemeGlitchSlum:
                    Y2KGlitchSlumThemeOwned = true;
                    break;

                case GodTierStoreEffectType.OfflineProgressionExtension:
                    offlineExtensionHoursGranted += item.offlineExtensionHours;
                    PlayerIQManager.Instance?.ExtendOfflineDecayWindow(item.offlineExtensionHours);
                    break;

                case GodTierStoreEffectType.MembershipCardCosmetic:
                    IllumisnottiMembershipCardOwned = true;
                    break;

                case GodTierStoreEffectType.TrashCanFlexCosmetic:
                    HolographicTrashCanFlexOwned = true;
                    break;
            }
        }

        /// <summary>
        /// Restores owned items and cosmetic flags from a save file. Unlike the Cash/Points Shop
        /// managers, the offline-extension hours DO need re-granting here (PlayerIQManager's
        /// bonusOfflineDecayMaxHours is not itself separately persisted -- it starts at 0 on
        /// every fresh load, so this is the one re-application that's correct, not a double
        /// count, since restoredOfflineExtensionHours is the full accumulated total).
        /// </summary>
        public void LoadState(IEnumerable<string> restoredOwnedItemIds, bool restoredVoicepack, bool restoredTheme, bool restoredMembershipCard, bool restoredTrashCanFlex, float restoredOfflineExtensionHours)
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

            CogsVoicepackDisdainOwned = restoredVoicepack;
            Y2KGlitchSlumThemeOwned = restoredTheme;
            IllumisnottiMembershipCardOwned = restoredMembershipCard;
            HolographicTrashCanFlexOwned = restoredTrashCanFlex;

            offlineExtensionHoursGranted = restoredOfflineExtensionHours;
            if (restoredOfflineExtensionHours > 0f)
            {
                PlayerIQManager.Instance?.ExtendOfflineDecayWindow(restoredOfflineExtensionHours);
            }

            OnItemsChanged?.Invoke();
        }
    }
}
