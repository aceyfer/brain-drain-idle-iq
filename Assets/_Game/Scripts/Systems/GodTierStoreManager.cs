using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// One active timed (consumable) purchase's remaining-time record -- itemId only, no
    /// duplicated displayName/description, so a display surface (TimedPurchaseWalletUI) always
    /// resolves fresh text by looking itemId back up in GodTierStoreManager.Items rather than
    /// risking stale copy baked in at purchase time. Reused directly as both the runtime ledger
    /// entry and the SaveManager DTO -- same precedent as BuildingSaveEntry (UpgradeManager).
    /// Multiple entries can share the same itemId: buying the same 24-hour item twice back to
    /// back is meant to produce two independent entries/countdowns, not one merged entry, so
    /// itemId is deliberately not a dictionary key anywhere in this system.
    /// </summary>
    [Serializable]
    public struct ActiveTimedPurchase
    {
        public string itemId;
        public long expiryUnixSeconds;

        public ActiveTimedPurchase(string itemId, long expiryUnixSeconds)
        {
            this.itemId = itemId;
            this.expiryUnixSeconds = expiryUnixSeconds;
        }
    }

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

        /// <summary>
        /// Ledger of still-active timed consumable purchases (e.g. an owned-but-not-yet-expired
        /// Brain Freeze family item) -- separate from ownedItemIds, which consumables never join
        /// (see StubPurchase). Backs THE WALLET's "item + time remaining" display. Access only
        /// through the pruning ActiveTimedPurchases property below, never this field directly.
        /// </summary>
        private readonly List<ActiveTimedPurchase> activeTimedPurchases = new();

        private static GodTierStoreManager instance;
        private static bool isShuttingDown;

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
                    if (isShuttingDown) return null;
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
        public bool IllumisnottyMembershipCardOwned { get; private set; }
        public bool HolographicTrashCanFlexOwned { get; private set; }
        public float OfflineExtensionHoursGranted => offlineExtensionHoursGranted;

        /// <summary>
        /// Every still-active timed consumable purchase, pruned of anything whose expiry has
        /// already passed each time this is read. Multiple purchases of the same item -- even
        /// back to back -- each get their own independent entry, so buying two 24-hour items
        /// shows as two separate countdowns rather than being silently merged into one.
        /// </summary>
        public IReadOnlyList<ActiveTimedPurchase> ActiveTimedPurchases
        {
            get
            {
                PruneExpiredTimedPurchases();
                return activeTimedPurchases;
            }
        }

        /// <summary>Fired after an item is successfully (stub-)purchased or the owned set is restored from a save.</summary>
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

        public bool IsItemOwned(GodTierStoreItemData item) => item != null && ownedItemIds.Contains(item.itemId);

        /// <summary>
        /// Three-state result for ResolveConsumableStatus. Unknown is deliberately distinct from
        /// NotConsumable -- an unresolved itemId (no matching entry in items, or a null entry)
        /// must never be conflated with a positive confirmation that the item isn't consumable.
        /// </summary>
        private enum ConsumableStatus
        {
            Unknown,
            Consumable,
            NotConsumable
        }

        /// <summary>
        /// Resolves whether a given itemId belongs to a consumable item in the configured list.
        /// Returns Unknown if no matching entry is found (or the matching entry is null) -- the
        /// caller must treat Unknown the same as NotConsumable (i.e. never strip), since an
        /// unresolvable lookup is not a positive confirmation of anything. Used only by
        /// LoadState's save migration.
        /// </summary>
        private ConsumableStatus ResolveConsumableStatus(string itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                GodTierStoreItemData item = items[i];
                if (item != null && item.itemId == itemId)
                {
                    return item.isConsumable ? ConsumableStatus.Consumable : ConsumableStatus.NotConsumable;
                }
            }

            return ConsumableStatus.Unknown;
        }

        /// <summary>
        /// PLACEHOLDER -- does not charge real money. Grants the item immediately. Call this
        /// from a real IAP plugin's purchase-success callback once one is integrated; until
        /// then, calling it directly (e.g. from a "Buy" button) gives the item away for free.
        /// Non-consumables are tracked in ownedItemIds and can only ever be bought once (the
        /// original behavior). Consumables (e.g. the Brain Freeze family) are NEVER added to
        /// ownedItemIds -- ownership and active-duration are separate concepts for them, so they
        /// stay purchasable indefinitely; ApplyItemEffect's own target handles stacking the new
        /// duration onto whatever's already active.
        /// </summary>
        public bool StubPurchase(GodTierStoreItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (!item.isConsumable)
            {
                if (IsItemOwned(item))
                {
                    return false;
                }

                ownedItemIds.Add(item.itemId);
            }

            // Timed consumables (currently the Brain Freeze family) additionally get their own
            // independent wallet entry -- deliberately separate from the stacking math inside
            // ApplyItemEffect/PlayerIQManager.ApplyBrainFreeze, which stays the sole source of
            // truth for the actual gameplay floor. This ledger only ever powers display (THE
            // WALLET's per-item countdowns); buying the same item twice adds two entries here so
            // both show up separately, even though the underlying IQ-floor effect still merges
            // into one stacked expiry as it always has.
            if (item.isConsumable && item.freezeDurationHours > 0f)
            {
                PruneExpiredTimedPurchases();
                long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)(item.freezeDurationHours * 3600f);
                activeTimedPurchases.Add(new ActiveTimedPurchase(item.itemId, expiry));
            }

            ApplyItemEffect(item);
            OnItemsChanged?.Invoke();
            return true;
        }

        /// <summary>Drops any ledger entry whose expiry has already passed. Called on every read
        /// (ActiveTimedPurchases getter) and before every new entry is added, so the list never
        /// grows unbounded and a stale entry never lingers past its own countdown reaching zero.</summary>
        private void PruneExpiredTimedPurchases()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            for (int i = activeTimedPurchases.Count - 1; i >= 0; i--)
            {
                if (activeTimedPurchases[i].expiryUnixSeconds <= now)
                {
                    activeTimedPurchases.RemoveAt(i);
                }
            }
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
                    IllumisnottyMembershipCardOwned = true;
                    break;

                case GodTierStoreEffectType.TrashCanFlexCosmetic:
                    HolographicTrashCanFlexOwned = true;
                    break;

                case GodTierStoreEffectType.UnlockProfanityPack:
                    if (RandomChatterManager.Instance != null)
                    {
                        RandomChatterManager.Instance.UnlockProfanity();
                        // Force-enable only on PURCHASE. On load, the player's own on/off
                        // choice (persisted by RandomChatterManager) must win -- see LoadState.
                        RandomChatterManager.Instance.ToggleProfanity(true);
                    }
                    break;

                case GodTierStoreEffectType.BrainFreezeIQImmunity:
                    PlayerIQManager.Instance?.ApplyBrainFreeze(item.freezeDurationHours);
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
        public void LoadState(IEnumerable<string> restoredOwnedItemIds, bool restoredVoicepack, bool restoredTheme, bool restoredMembershipCard, bool restoredTrashCanFlex, float restoredOfflineExtensionHours, IEnumerable<ActiveTimedPurchase> restoredActiveTimedPurchases = null)
        {
            ownedItemIds.Clear();
            if (restoredOwnedItemIds != null)
            {
                foreach (string itemId in restoredOwnedItemIds)
                {
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        continue;
                    }

                    // Migration (2026-08-05, hardened 2026-08-06): an itemId is only ever
                    // stripped when POSITIVELY CONFIRMED consumable. Anything unresolved
                    // (ConsumableStatus.Unknown, e.g. an itemId not wired into items) is
                    // preserved, same as a confirmed NotConsumable -- silently deleting a paid
                    // non-consumable is never acceptable, so an unresolved lookup must never be
                    // treated as grounds to strip. The item's actual active-duration state (e.g.
                    // Brain Freeze's expiry) is persisted separately and is unaffected either way.
                    if (ResolveConsumableStatus(itemId) == ConsumableStatus.Consumable)
                    {
                        continue;
                    }

                    ownedItemIds.Add(itemId);
                }
            }

            CogsVoicepackDisdainOwned = restoredVoicepack;
            Y2KGlitchSlumThemeOwned = restoredTheme;
            IllumisnottyMembershipCardOwned = restoredMembershipCard;
            HolographicTrashCanFlexOwned = restoredTrashCanFlex;

            offlineExtensionHoursGranted = restoredOfflineExtensionHours;
            if (restoredOfflineExtensionHours > 0f)
            {
                PlayerIQManager.Instance?.ExtendOfflineDecayWindow(restoredOfflineExtensionHours);
            }

            // Restore the wallet ledger, dropping anything that already expired while the app was
            // closed -- matches this project's existing real-time-decay convention (e.g.
            // DailyEngagementCapManager's day rollover) of never resurrecting stale timed state.
            activeTimedPurchases.Clear();
            if (restoredActiveTimedPurchases != null)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                foreach (ActiveTimedPurchase purchase in restoredActiveTimedPurchases)
                {
                    if (!string.IsNullOrWhiteSpace(purchase.itemId) && purchase.expiryUnixSeconds > now)
                    {
                        activeTimedPurchases.Add(purchase);
                    }
                }
            }

            // Targeted re-sync for the ONE effect whose state lives outside this manager:
            // RandomChatterManager persists profanity in its own PlayerPrefs keys, which can
            // diverge from the JSON save (save file deleted for testing while prefs survive,
            // or vice versa). UnlockProfanity() is internally guarded/idempotent, so re-calling
            // is safe. Deliberately NOT ToggleProfanity(true) here -- enabled is the player's
            // own toggle choice and must survive loads. Do NOT generalize this loop to other
            // effect types: the offline-extension re-grant is already handled above via
            // restoredOfflineExtensionHours, and re-applying it per-item would double-count.
            foreach (GodTierStoreItemData item in items)
            {
                if (item != null
                    && item.effectType == GodTierStoreEffectType.UnlockProfanityPack
                    && ownedItemIds.Contains(item.itemId))
                {
                    RandomChatterManager.Instance?.UnlockProfanity();
                }
            }

            OnItemsChanged?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only test hook: buys the 24-hour Brain Freeze item (itemId "brain_freeze")
        /// exactly as a real "Buy" button would, so THE WALLET's two-independent-entries behavior
        /// (buying the same 24-hour item twice back to back) can be verified from the Inspector
        /// without the God Tier Store's own popup being wired into the scene yet. Compiles out of
        /// any build, matching DailyEngagementCapManager's DebugBurnFullRateAllowance precedent.
        /// </summary>
        [ContextMenu("DEBUG: Buy Brain Freeze (24h)")]
        private void DebugBuyBrainFreeze()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == "brain_freeze")
                {
                    bool bought = StubPurchase(items[i]);
                    Debug.Log($"[GodTierStoreManager] DEBUG buy brain_freeze -> {bought}. Active timed purchases: {ActiveTimedPurchases.Count}");
                    return;
                }
            }

            Debug.LogWarning("[GodTierStoreManager] DEBUG buy brain_freeze -- no item with itemId 'brain_freeze' found in items.");
        }
#endif
    }
}
