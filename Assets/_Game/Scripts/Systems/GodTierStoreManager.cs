using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.Systems.Commerce;

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
    /// Owns the 9 God Shop items -- real-money-only, cosmetics/QoL, never power (class doc stale
    /// count fixed 2026-09-20, see §12). Real purchases route through IapCommerceService (the
    /// sole Unity IAP touchpoint); this class stays the catalog/effect/save owner, never talking
    /// to UnityEngine.Purchasing directly. RequestPurchase is the only production-reachable entry
    /// point -- it starts an async store purchase and does NOT grant anything itself. The actual
    /// grant only ever happens in GrantVerifiedEntitlement (private, called from
    /// HandlePurchaseApproved once IapCommerceService reports a backend-approved purchase) or
    /// ReconcileExistingEntitlement (public, called from IapCommerceService's boot/resume
    /// reconciliation for non-consumables already owned per the store). See
    /// Assets/Plans/iap-integration-plan.md for the full design.
    /// </summary>
    public sealed class GodTierStoreManager : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private List<GodTierStoreItemData> items = new();

        private readonly HashSet<string> ownedItemIds = new();
        private float offlineExtensionHoursGranted;

        /// <summary>productId -> item, built once per Awake/Items-change. Fails closed per-item
        /// (logs and skips) on a blank/duplicate productId rather than throwing -- one
        /// misconfigured catalog row must not take the whole store down.</summary>
        private readonly Dictionary<string, GodTierStoreItemData> itemsByProductId = new();

        /// <summary>
        /// Idempotency ledger for real purchases -- keyed by IapCommerceService's
        /// PurchaseValidationResult.GrantTransactionId, persisted via SaveManager
        /// (godTierStoreProcessedTransactionIds). Grows by one entry per real purchase ever made
        /// (not per app boot -- reconciliation of already-owned non-consumables goes through
        /// ReconcileExistingEntitlement instead, which never touches this set), so it stays small
        /// for a 9-item catalog even across a long play history; no pruning needed.
        /// </summary>
        private readonly HashSet<string> processedTransactionIds = new();

        /// <summary>Cached so OnDestroy unsubscribes from the exact instance Start subscribed to,
        /// never by re-resolving .Instance (which would self-bootstrap a stray host during
        /// teardown -- the same footgun already documented for DialogueDisplayUI in TASKLIST_DETAILS §19).</summary>
        private IapCommerceService subscribedCommerceService;

        /// <summary>
        /// Ledger of still-active timed consumable purchases (e.g. an owned-but-not-yet-expired
        /// Brain Freeze family item) -- separate from ownedItemIds, which consumables never join
        /// (see GrantVerifiedEntitlement). Backs THE WALLET's "item + time remaining" display. Access only
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

        /// <summary>Read-only view for SaveManager -- see processedTransactionIds' own doc comment.</summary>
        public IReadOnlyCollection<string> ProcessedTransactionIds => processedTransactionIds;

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

        /// <summary>Fired after an item is successfully purchased/granted/reconciled, or the owned set is restored from a save.</summary>
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
            BuildProductLookup();
        }

        private void Start()
        {
            // Touching IapCommerceService.Instance here self-bootstraps it (if nothing placed one
            // in the scene) and kicks off Connect/FetchProducts -- deliberately done from Start,
            // not lazily on shop-open, so readiness has time to resolve before the player ever
            // sees the God Shop panel.
            subscribedCommerceService = IapCommerceService.Instance;
            if (subscribedCommerceService != null)
            {
                subscribedCommerceService.OnPurchaseApproved -= HandlePurchaseApproved;
                subscribedCommerceService.OnPurchaseApproved += HandlePurchaseApproved;
                subscribedCommerceService.OnExistingEntitlementFound -= ReconcileExistingEntitlement;
                subscribedCommerceService.OnExistingEntitlementFound += ReconcileExistingEntitlement;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (subscribedCommerceService != null)
            {
                subscribedCommerceService.OnPurchaseApproved -= HandlePurchaseApproved;
                subscribedCommerceService.OnExistingEntitlementFound -= ReconcileExistingEntitlement;
                subscribedCommerceService = null;
            }

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        /// <summary>
        /// Builds productId -> item once per Awake (and can be safely re-run if items is ever
        /// changed at runtime). Fails closed on a blank productId (item stays unpurchasable, logs
        /// once) or a duplicate productId across two items (BOTH become unpurchasable rather than
        /// guessing which one "wins" -- an ambiguous catalog is a config bug to fix, not to paper
        /// over).
        /// </summary>
        private void BuildProductLookup()
        {
            itemsByProductId.Clear();
            var duplicates = new HashSet<string>();

            foreach (GodTierStoreItemData item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.productId))
                {
                    Debug.LogError($"[GodTierStoreManager] '{item.itemId}' has no productId configured -- it cannot be purchased through the store until one is set.", this);
                    continue;
                }

                if (itemsByProductId.ContainsKey(item.productId) || duplicates.Contains(item.productId))
                {
                    duplicates.Add(item.productId);
                    itemsByProductId.Remove(item.productId);
                    Debug.LogError($"[GodTierStoreManager] Duplicate productId '{item.productId}' across multiple catalog items -- all of them are unpurchasable until this is fixed.", this);
                    continue;
                }

                itemsByProductId[item.productId] = item;
            }
        }

        private GodTierStoreItemData FindItemByProductId(string productId)
        {
            return !string.IsNullOrWhiteSpace(productId) && itemsByProductId.TryGetValue(productId, out GodTierStoreItemData item)
                ? item
                : null;
        }

        private static string Redact(string value) =>
            string.IsNullOrEmpty(value) || value.Length <= 6 ? "***" : value.Substring(0, 4) + "…" + value.Substring(value.Length - 2);

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
        /// The only production-reachable purchase entry point -- called from the God Shop Buy
        /// button. Starts an async store purchase via IapCommerceService; grants NOTHING itself.
        /// The actual grant only happens later, in HandlePurchaseApproved, once a backend has
        /// verified the purchase. A displayed price is never proof of payment (per the plan's
        /// binding project rules) -- this method cannot be used to skip that.
        /// </summary>
        public void RequestPurchase(GodTierStoreItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.productId))
            {
                Debug.LogWarning("[GodTierStoreManager] RequestPurchase called with no productId configured -- cannot start a store purchase.", this);
                return;
            }

            if (!item.isConsumable && IsItemOwned(item))
            {
                return; // already owned; UI shouldn't be calling this, but don't start a pointless purchase either
            }

            IapCommerceService.Instance?.BeginPurchase(item.productId);
        }

        /// <summary>
        /// Fires once IapCommerceService reports a backend-approved, durably-fulfilled purchase
        /// (see IapCommerceService.OnPurchasePending's confirm-after-grant ordering -- by the
        /// time this runs, the backend has already recorded the grant in its own ledger).
        /// </summary>
        private void HandlePurchaseApproved(PurchaseGrantEventArgs args)
        {
            GodTierStoreItemData item = FindItemByProductId(args.ProductId);
            if (item == null)
            {
                Debug.LogWarning($"[GodTierStoreManager] Approved purchase for unrecognized productId '{Redact(args.ProductId)}' -- left ungranted for investigation, never mapped heuristically.", this);
                return;
            }

            GrantVerifiedEntitlement(item, args.TransactionId);
        }

        /// <summary>
        /// The only place that actually grants a God Shop effect for a NEW purchase. Private --
        /// unreachable from any button, production or otherwise; only HandlePurchaseApproved and
        /// the UNITY_EDITOR debug hook below call this. Idempotent per transactionId: a replayed
        /// approval (retry, app restart, duplicate event) for a transactionId already in
        /// processedTransactionIds is a safe no-op, so a Brain Freeze purchase can't accidentally
        /// double its duration from a callback replay -- each NEW purchase still gets its own
        /// transactionId from the store, so genuine repeat Brain Freeze purchases keep stacking
        /// exactly as before (the deliberate exception called out in the plan).
        /// </summary>
        private void GrantVerifiedEntitlement(GodTierStoreItemData item, string transactionId)
        {
            if (item == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(transactionId) && processedTransactionIds.Contains(transactionId))
            {
                Debug.Log($"[GodTierStoreManager] Transaction {Redact(transactionId)} already processed -- skipping duplicate grant for '{item.itemId}'.", this);
                return;
            }

            if (!item.isConsumable)
            {
                if (IsItemOwned(item))
                {
                    return;
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

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                processedTransactionIds.Add(transactionId);
            }

            OnItemsChanged?.Invoke();

            // A real-money grant must not risk being lost to the next periodic autosave --
            // request one immediately, same precedent as RebirthManager after a Snotting.
            GameManager.Instance?.RequestSave();
        }

        /// <summary>
        /// App boot/resume reconciliation for a non-consumable the STORE already confirms is
        /// owned (IapCommerceService.OnExistingEntitlementFound, from FetchPurchases -- not a new
        /// purchase, so there's no transactionId and this never touches processedTransactionIds).
        /// If this device's local save already knows about the item, this is a pure no-op beyond
        /// the targeted profanity re-sync (matching LoadState's own idempotent-UnlockProfanity-
        /// never-force-the-toggle pattern) -- it must NEVER re-run ApplyItemEffect for an
        /// already-known item, or the Corporate Cloak's offline-extension hours would double-add
        /// on every single boot. Only a genuinely new discovery (e.g. account-linked cross-device
        /// restore onto a fresh local save, §12 decision 2) applies the one-time effect.
        /// </summary>
        public void ReconcileExistingEntitlement(string productId)
        {
            GodTierStoreItemData item = FindItemByProductId(productId);
            if (item == null || item.isConsumable)
            {
                return; // consumables are never reconciled this way -- see class doc
            }

            bool alreadyKnownOwned = ownedItemIds.Contains(item.itemId);
            ownedItemIds.Add(item.itemId);

            if (!alreadyKnownOwned)
            {
                ApplyItemEffect(item);
                OnItemsChanged?.Invoke();
                GameManager.Instance?.RequestSave();
            }
            else if (item.effectType == GodTierStoreEffectType.UnlockProfanityPack)
            {
                RandomChatterManager.Instance?.UnlockProfanity();
            }
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

        /// <summary>
        /// Restores the processed-transaction idempotency ledger from a save
        /// (SaveManager.PlayerData.godTierStoreProcessedTransactionIds). Separate call from
        /// LoadState, same precedent as CurrencyManager.LoadShopMultipliers being its own call
        /// rather than growing LoadState's already-long parameter list further.
        /// </summary>
        public void LoadProcessedTransactionIds(IEnumerable<string> restoredIds)
        {
            processedTransactionIds.Clear();
            if (restoredIds == null)
            {
                return;
            }

            foreach (string id in restoredIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    processedTransactionIds.Add(id);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only test hook: grants the 24-hour Brain Freeze item (itemId "brain_freeze")
        /// exactly as a real approved purchase would, so THE WALLET's two-independent-entries
        /// behavior (buying the same 24-hour item twice back to back) can be verified from the
        /// Inspector without going through a real store purchase. Each call uses a fresh GUID as
        /// its transactionId specifically so repeat clicks stack (matching real repeat purchases)
        /// rather than being deduped as replays of the same transaction. Compiles out of any
        /// build, matching DailyEngagementCapManager's DebugBurnFullRateAllowance precedent.
        /// </summary>
        [ContextMenu("DEBUG: Buy Brain Freeze (24h)")]
        private void DebugBuyBrainFreeze()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == "brain_freeze")
                {
                    GrantVerifiedEntitlement(items[i], "debug-" + Guid.NewGuid());
                    Debug.Log($"[GodTierStoreManager] DEBUG buy brain_freeze -> granted. Active timed purchases: {ActiveTimedPurchases.Count}");
                    return;
                }
            }

            Debug.LogWarning("[GodTierStoreManager] DEBUG buy brain_freeze -- no item with itemId 'brain_freeze' found in items.");
        }
#endif
    }
}
