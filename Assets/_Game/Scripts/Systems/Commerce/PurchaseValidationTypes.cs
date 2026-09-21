using System;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// A validated purchase awaiting backend approval -- deliberately carries only the minimum
    /// needed to verify+grant (store product id, opaque purchase token, an idempotency key),
    /// never a raw receipt blob or account credential. See "Recommended launch path" in
    /// Assets/Plans/iap-integration-plan.md.
    /// </summary>
    public readonly struct PurchaseValidationRequest
    {
        public readonly string ProductId;
        public readonly string PurchaseToken;
        public readonly string TransactionId;

        public PurchaseValidationRequest(string productId, string purchaseToken, string transactionId)
        {
            ProductId = productId;
            PurchaseToken = purchaseToken;
            TransactionId = transactionId;
        }
    }

    /// <summary>
    /// The backend's durable, idempotent grant decision for one PurchaseValidationRequest.
    /// GrantTransactionId is the idempotency key GodTierStoreManager keys its processed-
    /// transaction ledger on -- for the not-yet-built real backend this should be whatever the
    /// server's own ledger considers unique per fulfillment (typically the purchase token
    /// itself, or a server-issued grant id), not necessarily TransactionId verbatim.
    /// </summary>
    public readonly struct PurchaseValidationResult
    {
        public readonly bool Approved;
        public readonly string GrantTransactionId;
        public readonly string FailureReason;

        private PurchaseValidationResult(bool approved, string grantTransactionId, string failureReason)
        {
            Approved = approved;
            GrantTransactionId = grantTransactionId;
            FailureReason = failureReason;
        }

        public static PurchaseValidationResult Success(string grantTransactionId) =>
            new PurchaseValidationResult(true, grantTransactionId, null);

        public static PurchaseValidationResult Failure(string reason) =>
            new PurchaseValidationResult(false, null, reason);
    }

    /// <summary>
    /// Backend boundary for verifying a purchase before anything is granted. Keeping this as an
    /// interface (rather than IapCommerceService talking to a concrete backend directly) is what
    /// lets a dev-only fake store exist in the Editor and a fail-closed placeholder exist in
    /// production, with the eventual real backend (UGS Cloud Code, or an external HTTPS
    /// endpoint -- Aceyfer's call/ownership, see §12 decision 1) swapped in later without
    /// touching IapCommerceService or GodTierStoreManager at all.
    /// </summary>
    public interface IPurchaseValidationService
    {
        /// <summary>
        /// Validates a purchase and reports the grant decision via onComplete, which must be
        /// invoked on the Unity main thread exactly once. Must never throw for an ordinary
        /// rejection (declined/expired/fraud-suspected/etc.) -- call onComplete with a Failure
        /// result instead; throwing is reserved for genuine programmer error (null request).
        /// </summary>
        void ValidatePurchase(PurchaseValidationRequest request, Action<PurchaseValidationResult> onComplete);
    }
}
