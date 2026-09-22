using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>Store connection/product-fetch state -- UI reads this rather than assuming the shop is purchasable.</summary>
    public enum CommerceReadiness
    {
        NotInitialized,
        Connecting,
        Ready,
        Unavailable
    }

    /// <summary>Per-row purchase lifecycle state, for a row's busy/spinner UI. Not persisted -- purely transient UX feedback.</summary>
    public enum PurchaseRequestState
    {
        Idle,
        Pending,
        ValidatingWithBackend,
        Granted,
        Failed
    }

    /// <summary>Coarse reason a purchase didn't result in a grant -- UI-facing categorization, not a full error dump.</summary>
    public enum PurchaseOutcome
    {
        Cancelled,
        Declined,
        NetworkFailure,
        ValidationRejected,
        Unavailable,
        AlreadyPending,
        Unknown
    }

    public readonly struct PurchaseGrantEventArgs
    {
        public readonly string ProductId;
        public readonly string TransactionId;

        public PurchaseGrantEventArgs(string productId, string transactionId)
        {
            ProductId = productId;
            TransactionId = transactionId;
        }
    }

    public readonly struct PurchaseFailureEventArgs
    {
        public readonly string ProductId;
        public readonly PurchaseOutcome Outcome;
        public readonly string Message;

        public PurchaseFailureEventArgs(string productId, PurchaseOutcome outcome, string message)
        {
            ProductId = productId;
            Outcome = outcome;
            Message = message;
        }
    }

    /// <summary>
    /// The one place in the project allowed to talk to Unity IAP directly -- GodTierStoreManager
    /// and the God Shop UI only ever see the plain string/enum/DTO surface below (BeginPurchase,
    /// readiness, OnPurchaseApproved/OnPurchaseRejected/OnExistingEntitlementFound,
    /// GetLocalizedPrice), never a UnityEngine.Purchasing type. That boundary is what lets a
    /// later iOS StoreKit path slot in without touching effect/catalog logic (per the plan's
    /// "keep platform-specific validation behind a small purchase/entitlement adapter").
    ///
    /// COMPILE-VERIFIED 2026-09-21 against the actually-installed com.unity.purchasing 5.4.3
    /// (batch-mode Unity, zero compiler errors) -- originally written blind against Unity's
    /// documented Order/CartItem/StoreController API with no license available, which got one
    /// member access wrong (ExtractPurchaseToken guessed `order.receipt.Payload`; the real shape
    /// is `order.Info.TransactionID`/`order.Info.Receipt` -- fixed once a real compile caught it,
    /// see that method's own doc comment for the correction). This does not mean the purchase
    /// flow has been exercised at runtime -- no Play Mode pass, no real store connection, no
    /// backend to validate against yet. Compiling clean only proves the API surface matches;
    /// Phase 4 (Google Play internal test) is still the real verification.
    /// </summary>
    public sealed class IapCommerceService : MonoBehaviour
    {
        private static IapCommerceService instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static IapCommerceService Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<IapCommerceService>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("IapCommerceService (Auto)");
                    instance = hostObject.AddComponent<IapCommerceService>();
                }

                return instance;
            }
        }

        // Production must never be able to resolve to the Editor-only fake store -- this branch
        // is the compile-time guarantee, on top of DevFakePurchaseValidationService's own
        // UNITY_EDITOR guard on the class itself (belt and suspenders, see its file).
        //
        // 2026-09-22: production branch swapped from UnconfiguredPurchaseValidationService to the
        // real backend per §12 decision 1 -- "the call site should keep pointing at whatever type
        // implements IPurchaseValidationService for real; swapping the interface binding is the
        // whole point of the interface existing" (see UnconfiguredPurchaseValidationService's own
        // doc comment). UNVERIFIED, WRITTEN BLIND -- this session has no Unity Editor/compiler
        // access (file-bridge only). UgsCloudCodeValidationService references Unity.Services.
        // Authentication and Unity.Services.CloudCode, NEITHER of which is installed yet
        // (Packages/manifest.json unchanged by this pass -- add both via Package Manager > Add
        // package by name, do not hand-edit a guessed version number). Until those packages are
        // added, THIS WHOLE FILE WILL FAIL TO COMPILE, not just silently no-op -- same as any
        // other missing-dependency state in this project, surfaced loudly rather than worked
        // around. Needs the same real compile pass §12's IAP install already went through once
        // (that pass is what caught the ExtractPurchaseToken guess above as wrong) before this can
        // be trusted. If that's not acceptable yet, revert this one line to
        // UnconfiguredPurchaseValidationService (fails closed, always safe) until verified.
#if UNITY_EDITOR
        private readonly IPurchaseValidationService validationService = new DevFakePurchaseValidationService();
#else
        private readonly IPurchaseValidationService validationService = new UgsCloudCodeValidationService();
#endif

        private StoreController storeController;
        private readonly Dictionary<string, Product> productsByProductId = new();
        private readonly HashSet<string> pendingProductIds = new();
        private CommerceReadiness readiness = CommerceReadiness.NotInitialized;

        public CommerceReadiness Readiness => readiness;
        public bool IsReady => readiness == CommerceReadiness.Ready;
        public bool IsOffline => Application.internetReachability == NetworkReachability.NotReachable;

        public event Action<CommerceReadiness> OnReadinessChanged;
        public event Action<string, PurchaseRequestState> OnPurchaseStateChanged;

        /// <summary>Fired once a purchase is backend-approved and durably granted -- GodTierStoreManager applies the effect from here.</summary>
        public event Action<PurchaseGrantEventArgs> OnPurchaseApproved;

        /// <summary>Fired on cancel/decline/network-failure/validation-rejection -- UI feedback only, never a grant.</summary>
        public event Action<PurchaseFailureEventArgs> OnPurchaseRejected;

        /// <summary>
        /// Fired for each non-consumable the store confirms is already owned (app boot/resume
        /// reconciliation via FetchPurchases) -- GodTierStoreManager.ReconcileExistingEntitlement
        /// handles this idempotently and does NOT re-run stacking/additive effects for entries
        /// already known locally, see that method's own doc comment.
        /// </summary>
        public event Action<string> OnExistingEntitlementFound;

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

        private void Start()
        {
            InitializeCommerce();
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

        private void InitializeCommerce()
        {
            readiness = CommerceReadiness.Connecting;
            OnReadinessChanged?.Invoke(readiness);

            var godShop = GodTierStoreManager.Instance;
            IReadOnlyList<GodTierStoreItemData> items = godShop != null ? godShop.Items : null;
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[IapCommerceService] No God Shop items configured; nothing to fetch from the store.", this);
                readiness = CommerceReadiness.Unavailable;
                OnReadinessChanged?.Invoke(readiness);
                return;
            }

            var catalogProvider = new CatalogProvider();
            int addedCount = 0;
            foreach (GodTierStoreItemData item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.productId))
                {
                    continue; // GodTierStoreManager.BuildProductLookup already logs this; not this service's job to log it twice
                }

                ProductType type = item.isConsumable ? ProductType.Consumable : ProductType.NonConsumable;
                catalogProvider.AddProduct(item.productId, type);
                addedCount++;
            }

            if (addedCount == 0)
            {
                Debug.LogWarning("[IapCommerceService] No God Shop items have a productId configured; nothing to fetch from the store.", this);
                readiness = CommerceReadiness.Unavailable;
                OnReadinessChanged?.Invoke(readiness);
                return;
            }

            storeController = UnityIAPServices.StoreController();
            storeController.OnStoreDisconnected += HandleStoreDisconnected;
            storeController.OnProductsFetched += HandleProductsFetched;
            storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
            storeController.OnPurchasesFetched += HandlePurchasesFetched;
            storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
            storeController.OnPurchasePending += HandlePurchasePending;
            storeController.OnPurchaseFailed += HandlePurchaseFailed;

            storeController.Connect().ContinueWith(_ =>
            {
                catalogProvider.FetchProducts(list => storeController.FetchProducts(list));
            });
        }

        /// <summary>
        /// Starts an async purchase for a store product id. Guards against double taps/overlapping
        /// transactions for the same row (plan requirement) -- a second BeginPurchase call for a
        /// product already in flight is rejected, not queued or silently dropped without feedback.
        /// </summary>
        public void BeginPurchase(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return;
            }

            if (readiness != CommerceReadiness.Ready)
            {
                RaiseFailure(productId, PurchaseOutcome.Unavailable, "Store not ready.");
                return;
            }

            if (!pendingProductIds.Add(productId))
            {
                RaiseFailure(productId, PurchaseOutcome.AlreadyPending, "A purchase for this item is already in progress.");
                return;
            }

            RaiseStateChanged(productId, PurchaseRequestState.Pending);
            storeController.PurchaseProduct(productId);
        }

        public string GetLocalizedPrice(string productId)
        {
            if (!string.IsNullOrWhiteSpace(productId)
                && productsByProductId.TryGetValue(productId, out Product product)
                && product?.metadata != null
                && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
            {
                return product.metadata.localizedPriceString;
            }

            return null; // caller falls back to GodTierStoreItemData.realMoneyPriceDisplay (editor/offline preview only)
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            readiness = CommerceReadiness.Unavailable;
            OnReadinessChanged?.Invoke(readiness);
            Debug.LogWarning($"[IapCommerceService] Store disconnected: {failure}", this);
        }

        private void HandleProductsFetched(List<Product> products)
        {
            productsByProductId.Clear();
            foreach (Product product in products)
            {
                if (product?.definition == null || string.IsNullOrEmpty(product.definition.id))
                {
                    continue;
                }

                productsByProductId[product.definition.id] = product;
            }

            readiness = CommerceReadiness.Ready;
            OnReadinessChanged?.Invoke(readiness);

            // Fetch existing purchases now that products resolved -- reconciles non-consumable
            // ownership (HandlePurchasesFetched) before any Buy button is meaningfully usable.
            storeController.FetchPurchases();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            readiness = CommerceReadiness.Unavailable;
            OnReadinessChanged?.Invoke(readiness);
            Debug.LogWarning($"[IapCommerceService] Product fetch failed: {failure}", this);
        }

        /// <summary>
        /// App boot/resume reconciliation for non-consumables. An empty or partial fetch here
        /// (e.g. while offline) must never be read as "ownership revoked" -- this method only
        /// ever adds confirmations, it never removes anything, so that invariant holds by
        /// construction rather than needing a special case.
        /// </summary>
        private void HandlePurchasesFetched(Orders orders)
        {
            foreach (var confirmedOrder in orders.ConfirmedOrders)
            {
                var cartItem = confirmedOrder.CartOrdered.Items().FirstOrDefault();
                if (cartItem?.Product?.definition == null)
                {
                    continue;
                }

                if (cartItem.Product.definition.type == ProductType.Consumable)
                {
                    continue; // consumables are never re-granted from a fetch replay -- see GodTierStoreManager.ReconcileExistingEntitlement
                }

                OnExistingEntitlementFound?.Invoke(cartItem.Product.definition.id);
            }
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"[IapCommerceService] Purchases fetch failed -- offline-owned-item reconciliation may be incomplete this session: {failure}", this);
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            var cartItem = order.CartOrdered.Items().FirstOrDefault();
            if (cartItem?.Product?.definition == null)
            {
                Debug.LogWarning("[IapCommerceService] PendingOrder with no resolvable product id -- left unfulfilled for investigation, never mapped heuristically.", this);
                return;
            }

            string productId = cartItem.Product.definition.id;
            string purchaseToken = ExtractPurchaseToken(order);

            RaiseStateChanged(productId, PurchaseRequestState.ValidatingWithBackend);

            var request = new PurchaseValidationRequest(
                productId: productId,
                purchaseToken: purchaseToken,
                transactionId: purchaseToken);

            validationService.ValidatePurchase(request, result =>
            {
                pendingProductIds.Remove(productId);

                if (result.Approved)
                {
                    RaiseStateChanged(productId, PurchaseRequestState.Granted);
                    OnPurchaseApproved?.Invoke(new PurchaseGrantEventArgs(productId, result.GrantTransactionId));

                    // Confirm only AFTER the grant is reported -- GodTierStoreManager's own
                    // GrantVerifiedEntitlement call (from its OnPurchaseApproved handler) has
                    // already run synchronously by this point, so the local save reflects the
                    // grant before Unity IAP is told the order is finalized. Do not reorder this.
                    storeController.ConfirmPurchase(order);
                }
                else
                {
                    RaiseFailure(productId, PurchaseOutcome.ValidationRejected, result.FailureReason);
                    // Deliberately NOT confirmed -- an unapproved order stays pending and safely
                    // replays via OnPurchasePending/OnPurchasesFetched on a later connect, per
                    // the plan's "confirm only after durable fulfillment succeeds" ordering.
                }
            });
        }

        private void HandlePurchaseFailed(FailedOrder order)
        {
            var cartItem = order?.CartOrdered?.Items().FirstOrDefault();
            string productId = cartItem?.Product?.definition?.id;

            if (!string.IsNullOrEmpty(productId))
            {
                pendingProductIds.Remove(productId);
            }

            RaiseFailure(productId ?? "unknown", PurchaseOutcome.Declined, "Purchase failed or was canceled.");
        }

        /// <summary>
        /// Fixed 2026-09-21 against the real installed package (Library/PackageCache) after a
        /// batch-mode compile caught the original guess (`order.receipt.Payload`) as wrong --
        /// `PendingOrder` has no `receipt` member. The real shape: `Order.Info` (`IOrderInfo`)
        /// exposes `Receipt` (raw JSON, present only on a PendingOrder -- empty once confirmed)
        /// and `TransactionID`. On Google Play specifically, `IOrderInfo.TransactionID` IS the
        /// purchase token -- Unity's own doc comment on `IGoogleOrderInfo.PurchaseToken`: "On
        /// Google Play, IOrderInfo.TransactionID contains the purchase token; this property
        /// returns the same value under its Google name." Preferring TransactionID over the raw
        /// Receipt JSON keeps this method returning exactly the opaque token a Google Play
        /// Developer API validation call needs, nothing to parse client-side (see
        /// IPurchaseValidationService's own doc comment on why only an opaque token crosses that
        /// boundary). Falls back to Receipt only if TransactionID is ever empty (e.g. a future
        /// non-Google platform behind this same adapter).
        /// </summary>
        private static string ExtractPurchaseToken(PendingOrder order)
        {
            string transactionId = order?.Info?.TransactionID;
            return !string.IsNullOrEmpty(transactionId) ? transactionId : order?.Info?.Receipt;
        }

        private void RaiseStateChanged(string productId, PurchaseRequestState state)
        {
            OnPurchaseStateChanged?.Invoke(productId, state);
        }

        private void RaiseFailure(string productId, PurchaseOutcome outcome, string message)
        {
            RaiseStateChanged(productId, PurchaseRequestState.Failed);
            OnPurchaseRejected?.Invoke(new PurchaseFailureEventArgs(productId, outcome, message));
        }
    }
}
