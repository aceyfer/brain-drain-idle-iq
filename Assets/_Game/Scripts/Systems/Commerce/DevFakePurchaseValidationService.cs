#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// Editor-only stand-in for the real backend, so the full purchase UX (Buy tap -> pending ->
    /// validating -> granted) can be exercised without real money or a deployed server. Auto-
    /// approves every request immediately. Compiled entirely out of any build via the file-level
    /// UNITY_EDITOR guard -- matches this project's existing Editor-only-testing convention (see
    /// "Editor-only progression testing system" in CLAUDE.md) -- so it is not just disabled but
    /// literally absent from a release binary. IapCommerceService additionally only ever
    /// references this type from behind its own #if UNITY_EDITOR branch; either guard alone
    /// would already make this impossible to reach in production.
    /// </summary>
    public sealed class DevFakePurchaseValidationService : IPurchaseValidationService
    {
        public void ValidatePurchase(PurchaseValidationRequest request, Action<PurchaseValidationResult> onComplete)
        {
            Debug.Log($"[DEV FAKE STORE] Auto-approving productId='{request.ProductId}' -- no real backend, no real money. Editor-only.");
            onComplete?.Invoke(PurchaseValidationResult.Success(request.TransactionId));
        }
    }
}
#endif
