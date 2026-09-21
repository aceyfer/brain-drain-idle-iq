using System;
using UnityEngine;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// Production default until a real backend is wired in. Always fails closed -- this is what
    /// guarantees "no production UI path can call a free grant" (Assets/Plans/iap-integration-
    /// plan.md's release-acceptance list) by construction: a release build can never resolve to
    /// DevFakePurchaseValidationService (Editor-only, see its own file), so without a real
    /// implementation swapped in here, nothing can ever be granted outside the Editor.
    ///
    /// Replace this class's body -- not its call site -- once the real backend exists (§12
    /// decision 1: server-authoritative, backend owned by Aceyfer, recommended path UGS Cloud
    /// Code). The call site (IapCommerceService's production branch) should keep pointing at
    /// whatever type implements IPurchaseValidationService for real; swapping the interface
    /// binding is the whole point of the interface existing.
    /// </summary>
    public sealed class UnconfiguredPurchaseValidationService : IPurchaseValidationService
    {
        public void ValidatePurchase(PurchaseValidationRequest request, Action<PurchaseValidationResult> onComplete)
        {
            Debug.LogWarning($"[IapCommerceService] No purchase backend configured -- productId='{request.ProductId}' left unvalidated and ungranted. Wire a real IPurchaseValidationService before this can ship.");
            onComplete?.Invoke(PurchaseValidationResult.Failure("Store not available right now."));
        }
    }
}
