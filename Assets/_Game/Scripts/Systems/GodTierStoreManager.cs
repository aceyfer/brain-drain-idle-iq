using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.Systems.Commerce;

namespace BrainDrain.Systems
{
    /// <summary>
    /// LEGACY, migration-read-only as of the 2026-10-06 FREEZE INVENTORY amendment. Used to be
    /// THE WALLET's live per-purchase ledger (buy the same item twice, see two countdowns); that
    /// model is gone -- see GodTierStoreManager's class doc. Still referenced by
    /// SaveManager.PlayerData.activeTimedPurchases purely so a save written before this amendment
    /// can still be read; GodTierStoreManager.LoadState consumes it once, to derive a best-effort
    /// display itemId for whatever freeze PlayerIQManager.BrainFreezeExpiryUnixSeconds already
    /// protects (that value itself is never touched by this migration), then never writes to or
    /// reads from this shape again. New saves persist FreezeInventoryEntry instead.
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
    /// 2026-10-06 FREEZE INVENTORY: one Brain-Freeze-family itemId's unlimited-purchase charge
    /// count. Persisted as a flat list (SaveManager.PlayerData.freezeInventory) since JsonUtility
    /// can't serialize Dictionary directly -- same reason BuildingSaveEntry/ActiveTimedPurchase
    /// are flat structs rather than dictionary entries.
    /// </summary>
    [Serializable]
    public struct FreezeInventoryEntry
    {
        public string itemId;
        public int count;

        public FreezeInventoryEntry(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
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
    ///
    /// 2026-10-06 FREEZE INVENTORY AMENDMENT (Aceyfer): replaces the Brain Freeze family's old
    /// "buy = activate, repeat purchases stack" model entirely. Buying a freeze item now only
    /// adds one charge to freezeInventory -- GrantVerifiedEntitlement never touches
    /// PlayerIQManager for a freeze purchase anymore. The player explicitly activates a charge
    /// via ActivateFreeze(itemId), and only one freeze can ever be active at a time (enforced
    /// here via PlayerIQManager.IsBrainFreezeActive, the same single merged-expiry field that
    /// already existed -- this amendment changes WHEN that field gets set, not what it is or how
    /// the IQ-floor effect itself works). Caps total active protection at whichever single item's
    /// own duration (max 7 days, Deep Freeze), eliminating the old model's unbounded stacking.
    /// </summary>
    public sealed class GodTierStoreManager : MonoBehaviour
    {
        [Header("Items")]
        [SerializeField] private List<GodTierStoreItemData> items = new();

        private readonly HashSet<string> ownedItemIds = new();
        private float offlineExtensionHoursGranted;

        /// <summary>itemId -> unlimited-purchase charge count for the Brain Freeze family.
        /// Nothing here ever caps or limits a purchase -- see GrantVerifiedEntitlement.</summary>
        private readonly Dictionary<string, int> freezeInventory = new();

        /// <summary>Which itemId the CURRENTLY active freeze (if any) came from, for display only
        /// -- the real on/off state is PlayerIQManager.IsBrainFreezeActive, never duplicated here.
        /// Null/empty once that expires (cleared by HandleSecondTick's transition detection).</summary>
        private string activeFreezeItemId;

        /// <summary>Tick-to-tick edge detection for "a freeze JUST expired" (see HandleSecondTick)
        /// -- seeded from the real state on every Start/LoadState so a load into an
        /// already-expired freeze never fires a false "just ended" toast.</summary>
        private bool wasFreezeActiveLastTick;

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
        private GameManager subscribedGameManager;

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

        /// <summary>Charges currently owned for one freeze itemId. Never negative; 0 for an
        /// unrecognized or never-purchased itemId.</summary>
        public int GetFreezeInventoryCount(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) && freezeInventory.TryGetValue(itemId, out int count) ? count : 0;

        /// <summary>Snapshot of every freeze itemId with a positive charge count, for SaveManager
        /// to persist and FreezeInventoryCloudSync to mirror. Zero-count entries are never
        /// included -- GetFreezeInventoryCount already treats a missing key as 0, so there is
        /// nothing meaningful to persist about an item nobody owns any charges of.</summary>
        public IEnumerable<FreezeInventoryEntry> FreezeInventorySnapshot
        {
            get
            {
                foreach (KeyValuePair<string, int> kvp in freezeInventory)
                {
                    if (kvp.Value > 0) { yield return new FreezeInventoryEntry(kvp.Key, kvp.Value); }
                }
            }
        }

        /// <summary>Which itemId the active freeze (if any) came from -- display only, see its
        /// own field doc comment. Null/empty when HasActiveFreeze is false.</summary>
        public string ActiveFreezeItemId => activeFreezeItemId;

        /// <summary>The single authoritative "is a freeze currently protecting the player" check
        /// -- always PlayerIQManager's own merged expiry field, never a second source of truth.</summary>
        public bool HasActiveFreeze => PlayerIQManager.Instance != null && PlayerIQManager.Instance.IsBrainFreezeActive;

        /// <summary>Fired after an item is successfully purchased/granted/reconciled, or the owned/inventory state is restored from a save.</summary>
        public event Action OnItemsChanged;

        /// <summary>Fired the instant an active freeze's protection window ends (tick-detected,
        /// see HandleSecondTick) -- itemId is whichever freeze just ended (display only), count is
        /// the remaining inventory for that SAME itemId. UI (THE WALLET) uses this for the
        /// "Freeze ended. Use another? (xN left)" nudge; never fires for a freeze that was already
        /// expired before this session started (see wasFreezeActiveLastTick's seeding in
        /// Start/LoadState).</summary>
        public event Action<string, int> OnFreezeExpired;

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
                subscribedCommerceService.OnOwnedNonConsumablesReported -= HandleOwnedNonConsumablesReported;
                subscribedCommerceService.OnOwnedNonConsumablesReported += HandleOwnedNonConsumablesReported;
            }

            subscribedGameManager = GameManager.Instance;
            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnSecondTick -= HandleSecondTick;
                subscribedGameManager.OnSecondTick += HandleSecondTick;
            }

            // Seed AFTER LoadState would already have run (SaveManager restores before any
            // other Start()'s normal game-init ordering settles) -- but seed here too as a safety
            // net for a scene that never goes through SaveManager at all (e.g. a test scene),
            // so the very first tick never misreads pre-existing state as a fresh expiry.
            wasFreezeActiveLastTick = HasActiveFreeze;

            // 2026-10-06 FREEZE INVENTORY cloud mirror: reconcile against UGS Cloud Save once per
            // launch, after local state is already loaded (SaveManager's own Start() runs before
            // GameManager's -100 execution order, so LoadState above has already applied by the
            // time this Start() runs). Fire-and-forget -- see FreezeInventoryCloudSync's own doc
            // comment for why this can't block boot on a network round trip.
            FreezeInventoryCloudSync.ReconcileOnLaunchAsync(this);
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
                subscribedCommerceService.OnOwnedNonConsumablesReported -= HandleOwnedNonConsumablesReported;
                subscribedCommerceService = null;
            }

            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnSecondTick -= HandleSecondTick;
                subscribedGameManager = null;
            }

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        /// <summary>Detects the exact tick a freeze's protection window ends (PlayerIQManager's
        /// own expiry, decayed/checked live) and fires OnFreezeExpired exactly once for it. Simple
        /// edge detection rather than a scheduled callback -- this project's established pattern
        /// for "something that lazily expires against wall-clock time" (see
        /// PlayerIQManager.IsBrainFreezeActive itself, or UpgradeManager's LockRandomBuildingFor).</summary>
        private void HandleSecondTick()
        {
            bool isActiveNow = HasActiveFreeze;
            if (wasFreezeActiveLastTick && !isActiveNow)
            {
                string expiredItemId = activeFreezeItemId;
                activeFreezeItemId = null;
                OnFreezeExpired?.Invoke(expiredItemId, GetFreezeInventoryCount(expiredItemId));
                OnItemsChanged?.Invoke();
            }
            wasFreezeActiveLastTick = isActiveNow;
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

        /// <summary>Linear scan by itemId (not productId) -- the catalog is small (9 items) and
        /// this is only ever called from player-initiated activation/debug paths, never per-frame,
        /// so a dictionary isn't worth the extra bookkeeping itemsByProductId already needs.</summary>
        private GodTierStoreItemData FindItemById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) { return null; }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == itemId) { return items[i]; }
            }
            return null;
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
        ///
        /// 2026-10-06 FREEZE INVENTORY: freeze purchases are now ALWAYS allowed regardless of
        /// whether a freeze is currently active -- buying only adds a charge to the wallet, it
        /// never touches the active slot, so there is no "wasted purchase" case to guard against
        /// here (unlike the old stacking model, which never needed this guard either, just for a
        /// different reason). The only remaining guard is the existing non-consumable
        /// already-owned check.
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
        /// processedTransactionIds is a safe no-op.
        ///
        /// 2026-10-06 FREEZE INVENTORY: a Brain-Freeze-family purchase ONLY adds a charge to
        /// freezeInventory now -- it never calls PlayerIQManager, never touches the active slot.
        /// Purchases are unlimited (no cap check here or anywhere else). Grant happens, THEN
        /// RequestSave() below (still synchronous -- SaveManager.SaveGame() runs inline off
        /// GameManager.OnSaveRequested), and only once that's returned does control go back to
        /// IapCommerceService.HandlePurchasePending to decide whether to ConfirmPurchase -- the
        /// existing "grant -> save -> confirm" ordering the IAP safety audit already established
        /// is unchanged by this rewrite, just what "grant" means for a freeze item is different.
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
                ApplyItemEffect(item);
            }
            else if (item.effectType == GodTierStoreEffectType.BrainFreezeIQImmunity)
            {
                GrantFreezeInventory(item.itemId, 1);
            }

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                processedTransactionIds.Add(transactionId);
            }

            OnItemsChanged?.Invoke();

            // A real-money grant must not risk being lost to the next periodic autosave --
            // request one immediately, same precedent as RebirthManager after a Snotting.
            GameManager.Instance?.RequestSave();
        }

        private void GrantFreezeInventory(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) { return; }
            freezeInventory.TryGetValue(itemId, out int current);
            freezeInventory[itemId] = current + amount;
            FreezeInventoryCloudSync.PushAsync(this);
        }

        /// <summary>
        /// 2026-10-06 FREEZE INVENTORY cloud mirror: merges an incoming (already-fetched) per-item
        /// count map into the local wallet by taking the per-item MAX, never the cloud value
        /// outright -- "never lose a paid charge, never duplicate one" means a stale/incomplete
        /// cloud fetch can never erase a charge this device already knows about, and a charge the
        /// cloud already knows about but this device hasn't seen yet is still picked up. Called
        /// only by FreezeInventoryCloudSync's launch reconciliation -- never a purchase/activation
        /// path, so this does NOT touch processedTransactionIds.
        /// </summary>
        public void ReconcileFreezeInventory(IReadOnlyDictionary<string, int> cloudCounts)
        {
            if (cloudCounts == null || cloudCounts.Count == 0) { return; }

            bool changed = false;
            foreach (KeyValuePair<string, int> kvp in cloudCounts)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value <= 0) { continue; }
                int current = GetFreezeInventoryCount(kvp.Key);
                if (kvp.Value > current)
                {
                    freezeInventory[kvp.Key] = kvp.Value;
                    changed = true;
                }
            }

            if (changed)
            {
                OnItemsChanged?.Invoke();
                GameManager.Instance?.RequestSave();
            }
        }

        /// <summary>
        /// 2026-10-06 FREEZE INVENTORY cloud mirror: adopts a cloud-reported active freeze only if
        /// it protects LATER than whatever this device currently has (or this device has none
        /// active right now) -- the same "never lose what was paid for" rule extended to active
        /// protection time, never shortens an already-longer local expiry. Called only by
        /// FreezeInventoryCloudSync's launch reconciliation.
        /// </summary>
        public void AdoptCloudActiveFreezeIfLonger(string itemId, long expiryUnixSeconds)
        {
            if (string.IsNullOrWhiteSpace(itemId) || expiryUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return;
            }

            long localExpiry = PlayerIQManager.Instance != null ? PlayerIQManager.Instance.BrainFreezeExpiryUnixSeconds : 0L;
            if (expiryUnixSeconds <= localExpiry)
            {
                return; // local protection already covers this or more
            }

            PlayerIQManager.Instance?.SetBrainFreezeExpiry(expiryUnixSeconds);
            activeFreezeItemId = itemId;
            wasFreezeActiveLastTick = true;
            OnItemsChanged?.Invoke();
            GameManager.Instance?.RequestSave();
        }

        /// <summary>
        /// Player-initiated activation (THE WALLET's "Use" button) of one already-owned freeze
        /// charge. Only ever succeeds if (a) this itemId has at least one charge, and (b) no
        /// freeze is currently active -- enforcing "only one freeze can run at a time" at the one
        /// place that actually starts one, so there is nothing for any caller to get wrong.
        /// Decrements inventory, sets the active-display itemId, applies the real IQ-floor effect
        /// via the existing ApplyItemEffect/PlayerIQManager.ApplyBrainFreeze plumbing (unchanged),
        /// then saves immediately -- same "don't risk losing a real-money-backed state change to
        /// the next periodic autosave" reasoning as GrantVerifiedEntitlement.
        /// </summary>
        public bool ActivateFreeze(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (HasActiveFreeze)
            {
                Debug.LogWarning($"[GodTierStoreManager] ActivateFreeze('{itemId}') refused -- a freeze is already active.", this);
                return false;
            }

            if (GetFreezeInventoryCount(itemId) <= 0)
            {
                Debug.LogWarning($"[GodTierStoreManager] ActivateFreeze('{itemId}') refused -- no charges owned.", this);
                return false;
            }

            GodTierStoreItemData item = FindItemById(itemId);
            if (item == null || item.effectType != GodTierStoreEffectType.BrainFreezeIQImmunity || item.freezeDurationHours <= 0f)
            {
                Debug.LogWarning($"[GodTierStoreManager] ActivateFreeze('{itemId}') refused -- not a configured freeze item.", this);
                return false;
            }

            freezeInventory[itemId] = GetFreezeInventoryCount(itemId) - 1;
            activeFreezeItemId = itemId;
            wasFreezeActiveLastTick = true;

            ApplyItemEffect(item); // PlayerIQManager.ApplyBrainFreeze(item.freezeDurationHours)
            FreezeInventoryCloudSync.PushAsync(this);

            OnItemsChanged?.Invoke();
            GameManager.Instance?.RequestSave();

            return true;
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

        /// <summary>
        /// IAP RULES: "refunded/voided purchases: on next init, revoke the non-consumable if the
        /// store no longer reports it." reportedProductIds is the COMPLETE current owned set per
        /// IapCommerceService's own doc comment on OnOwnedNonConsumablesReported (only ever fired
        /// from a successful fetch, never a failed/partial one) -- so anything locally owned whose
        /// productId is missing from it has been refunded, charged back, or otherwise voided by
        /// the store. Items with no productId configured are never touched here (they were never
        /// purchasable through the store in the first place, so "the store doesn't report it" is
        /// meaningless for them -- can't distinguish a real revocation from a catalog gap).
        /// </summary>
        private void HandleOwnedNonConsumablesReported(IReadOnlyList<string> reportedProductIds)
        {
            var reportedSet = new HashSet<string>(reportedProductIds);
            List<GodTierStoreItemData> toRevoke = null;

            for (int i = 0; i < items.Count; i++)
            {
                GodTierStoreItemData item = items[i];
                if (item == null || item.isConsumable || string.IsNullOrWhiteSpace(item.productId)) { continue; }
                if (!ownedItemIds.Contains(item.itemId)) { continue; }
                if (reportedSet.Contains(item.productId)) { continue; }

                (toRevoke ??= new List<GodTierStoreItemData>()).Add(item);
            }

            if (toRevoke == null) { return; }

            for (int i = 0; i < toRevoke.Count; i++)
            {
                RevokeEntitlement(toRevoke[i]);
            }

            OnItemsChanged?.Invoke();
            GameManager.Instance?.RequestSave();
        }

        /// <summary>The inverse of ApplyItemEffect -- only ever called from
        /// HandleOwnedNonConsumablesReported. Never player-reachable.</summary>
        private void RevokeEntitlement(GodTierStoreItemData item)
        {
            ownedItemIds.Remove(item.itemId);
            Debug.LogWarning($"[GodTierStoreManager] Revoking '{item.itemId}' -- the store no longer reports this purchase (refund/void).", this);

            switch (item.effectType)
            {
                case GodTierStoreEffectType.VoicepackDisdain:
                    CogsVoicepackDisdainOwned = false;
                    break;

                case GodTierStoreEffectType.UIThemeGlitchSlum:
                    Y2KGlitchSlumThemeOwned = false;
                    break;

                case GodTierStoreEffectType.OfflineProgressionExtension:
                    // Cumulative across (in practice, exactly one) purchases of this item -- only
                    // subtract what THIS item granted, never reset the whole accumulator, in case
                    // a future catalog ever has more than one OfflineProgressionExtension item.
                    // No corresponding "un-extend" on PlayerIQManager -- bonusOfflineDecayMaxHours
                    // is a convenience window, not something that can be cleanly rolled back
                    // mid-session without risking a harsher decay than the player ever saw coming.
                    offlineExtensionHoursGranted = Mathf.Max(0f, offlineExtensionHoursGranted - item.offlineExtensionHours);
                    break;

                case GodTierStoreEffectType.MembershipCardCosmetic:
                    IllumisnottyMembershipCardOwned = false;
                    break;

                case GodTierStoreEffectType.TrashCanFlexCosmetic:
                    HolographicTrashCanFlexOwned = false;
                    break;

                case GodTierStoreEffectType.UnlockProfanityPack:
                    RandomChatterManager.Instance?.LockProfanity();
                    break;

                case GodTierStoreEffectType.BrainFreezeIQImmunity:
                    // Consumable -- never reaches here, filtered out in
                    // HandleOwnedNonConsumablesReported's own isConsumable check.
                    break;
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
                    // 2026-10-06: only ever reached from ActivateFreeze now (not from a purchase
                    // grant anymore -- see GrantVerifiedEntitlement). durationHours comes from
                    // whichever freeze item was just activated.
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
        ///
        /// 2026-10-06 FREEZE INVENTORY: restoredFreezeInventory/restoredActiveFreezeItemId are the
        /// new-format fields (empty/null for a save written before this amendment).
        /// legacyActiveTimedPurchases/legacyBrainFreezeExpiryUnixSeconds are ONLY consulted when
        /// restoredActiveFreezeItemId is blank, purely to derive a display label for whatever
        /// freeze PlayerIQManager.BrainFreezeExpiryUnixSeconds (restored separately and NEVER
        /// touched by this method) already protects -- the migration directive is "stays active
        /// as-is, never converted or deleted," and since that field was already the single
        /// merged source of truth for the actual effect even under the old ledger model, there is
        /// nothing to convert: the protection carries over automatically, only its on-screen label
        /// needs a best-effort guess.
        /// </summary>
        public void LoadState(
            IEnumerable<string> restoredOwnedItemIds,
            bool restoredVoicepack,
            bool restoredTheme,
            bool restoredMembershipCard,
            bool restoredTrashCanFlex,
            float restoredOfflineExtensionHours,
            IEnumerable<FreezeInventoryEntry> restoredFreezeInventory,
            string restoredActiveFreezeItemId,
            IEnumerable<ActiveTimedPurchase> legacyActiveTimedPurchases,
            long legacyBrainFreezeExpiryUnixSeconds)
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

            freezeInventory.Clear();
            if (restoredFreezeInventory != null)
            {
                foreach (FreezeInventoryEntry entry in restoredFreezeInventory)
                {
                    if (string.IsNullOrWhiteSpace(entry.itemId) || entry.count <= 0) { continue; }
                    freezeInventory[entry.itemId] = entry.count;
                }
            }

            activeFreezeItemId = !string.IsNullOrWhiteSpace(restoredActiveFreezeItemId)
                ? restoredActiveFreezeItemId
                : DeriveLegacyActiveFreezeItemId(legacyActiveTimedPurchases, legacyBrainFreezeExpiryUnixSeconds);

            // Seed the edge-detector to the state we just loaded, not whatever it was before --
            // otherwise a load into an already-expired freeze (common: the app was closed past
            // the expiry) would fire a false "just ended" toast on the very next tick.
            wasFreezeActiveLastTick = HasActiveFreeze;

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

        /// <summary>See LoadState's own doc comment for when/why this runs. Prefers an exact
        /// expiry match (the common case: the legacy ledger's one entry IS what produced the
        /// merged expiry); falls back to whichever legacy entry has the latest expiry otherwise.
        /// Returns null if there's nothing currently active to label.</summary>
        private static string DeriveLegacyActiveFreezeItemId(IEnumerable<ActiveTimedPurchase> legacyEntries, long brainFreezeExpiryUnixSeconds)
        {
            if (legacyEntries == null || brainFreezeExpiryUnixSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }

            string best = null;
            long bestExpiry = long.MinValue;
            foreach (ActiveTimedPurchase entry in legacyEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.itemId)) { continue; }
                if (entry.expiryUnixSeconds == brainFreezeExpiryUnixSeconds) { return entry.itemId; }
                if (entry.expiryUnixSeconds > bestExpiry) { bestExpiry = entry.expiryUnixSeconds; best = entry.itemId; }
            }
            return best;
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
        /// Editor-only test hook: grants one 24-hour Brain Freeze charge to the wallet exactly as
        /// a real approved purchase would (itemId "brain_freeze"), so the FREEZE INVENTORY system
        /// can be verified from the Inspector without going through a real store purchase. Each
        /// call uses a fresh GUID as its transactionId specifically so repeat clicks each grant
        /// another charge (matching real repeat purchases) rather than being deduped as replays of
        /// the same transaction. Compiles out of any build, matching
        /// DailyEngagementCapManager.DebugBurnFullRateAllowance precedent.
        /// 2026-10-06: rewritten for the freeze inventory amendment -- used to activate a freeze
        /// directly; now grants exactly one wallet charge, matching real purchase behavior.
        /// </summary>
        [ContextMenu("DEBUG: Buy Brain Freeze (24h) x1")]
        public void DebugBuyBrainFreeze()
        {
            GodTierStoreItemData item = FindItemById("brain_freeze");
            if (item == null)
            {
                Debug.LogWarning("[GodTierStoreManager] DEBUG buy brain_freeze -- no item with itemId 'brain_freeze' found in items.");
                return;
            }

            GrantVerifiedEntitlement(item, "debug-" + Guid.NewGuid());
            Debug.Log($"[GodTierStoreManager] DEBUG buy brain_freeze -> wallet now has {GetFreezeInventoryCount("brain_freeze")} charge(s).");
        }

        /// <summary>Editor-only test hook: activates one owned brain_freeze charge via the real
        /// ActivateFreeze path (same guards a live WALLET "Use" button would hit). Logs the
        /// success/failure reason either way.</summary>
        [ContextMenu("DEBUG: Activate Brain Freeze (24h)")]
        public void DebugActivateBrainFreeze()
        {
            bool activated = ActivateFreeze("brain_freeze");
            Debug.Log($"[GodTierStoreManager] DEBUG activate brain_freeze -> {(activated ? "activated" : "refused, see warning above")}.");
        }
#endif
    }
}
