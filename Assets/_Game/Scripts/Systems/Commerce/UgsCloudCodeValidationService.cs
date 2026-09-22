using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// Production purchase-validation backend for §12. Calls the "validatePurchase" Cloud Code
    /// script (Assets/CloudCode/validatePurchase.js -- see Assets/Plans/iap-backend-scoping.md for
    /// the full design), which verifies the purchase against the Google Play Developer API using a
    /// service-account credential that never leaves the server. This class only ever sends the
    /// same opaque productId/purchaseToken/transactionId triple IPurchaseValidationService's own
    /// boundary already restricts client code to -- see PurchaseValidationTypes.cs's doc comment.
    ///
    /// NOT YET WIRED IN as of writing -- IapCommerceService's production branch still points at
    /// UnconfiguredPurchaseValidationService. Swap that one line once com.unity.services.
    /// authentication and com.unity.services.cloudcode are actually added to the project (via
    /// Package Manager > Add package by name, so Unity resolves real current versions -- do NOT
    /// hand-edit Packages/manifest.json with a guessed version number here, same lesson §12's
    /// com.unity.purchasing install already taught this project) and the Cloud Code script is
    /// deployed. Until then this file won't even compile (the two Unity.Services.* usings below
    /// don't resolve), which is a correct, visible failure state -- not something to silently
    /// work around.
    ///
    /// Sign-in (§12 decision 2) is delegated to GooglePlayGamesAuthService, which prefers a
    /// Google-linked UGS identity over an anonymous one -- see that file for the account-linking
    /// design and its own "unverified" caveats. This class no longer signs in anonymously itself;
    /// it just ensures SOME identity exists before calling Cloud Code.
    /// </summary>
    public sealed class UgsCloudCodeValidationService : IPurchaseValidationService
    {
        private const string ValidatePurchaseFunctionName = "validatePurchase";

        [Serializable]
        private sealed class ValidatePurchaseResponse
        {
            public bool approved;
            public string grantTransactionId;
            public string failureReason;
        }

        /// <summary>
        /// Fire-and-forget by design (matches the interface's callback contract) -- all failure
        /// paths, including ones from awaited calls, are caught below and reported through
        /// onComplete rather than as an unobserved exception. onComplete is guaranteed to be
        /// invoked exactly once, on the Unity main thread's synchronization context.
        /// </summary>
        public async void ValidatePurchase(PurchaseValidationRequest request, Action<PurchaseValidationResult> onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            PurchaseValidationResult result;
            try
            {
                result = await ValidateAsync(request);
            }
            catch (Exception ex)
            {
                // Infra/programmer errors (UGS not configured, packages missing, unexpected
                // exception shape, etc.) still must not throw across this boundary --
                // IPurchaseValidationService's own contract reserves throwing for a null request.
                // Log loudly, fail the purchase safely instead of crashing the caller.
                Debug.LogError($"[UgsCloudCodeValidationService] Unhandled error validating productId='{request.ProductId}': {ex}");
                result = PurchaseValidationResult.Failure("Store validation failed unexpectedly. Please try again.");
            }

            onComplete(result);
        }

        private static async Task<PurchaseValidationResult> ValidateAsync(PurchaseValidationRequest request)
        {
            await EnsureSignedInAsync();

            var args = new Dictionary<string, object>
            {
                { "productId", request.ProductId },
                { "purchaseToken", request.PurchaseToken },
                { "transactionId", request.TransactionId },
            };

            ValidatePurchaseResponse response;
            try
            {
                response = await CloudCodeService.Instance.CallEndpointAsync<ValidatePurchaseResponse>(ValidatePurchaseFunctionName, args);
            }
            catch (CloudCodeRateLimitedException ex)
            {
                Debug.LogWarning($"[UgsCloudCodeValidationService] Rate limited validating productId='{request.ProductId}': {ex.Message}");
                return PurchaseValidationResult.Failure("Store is busy right now, please try again in a moment.");
            }
            catch (CloudCodeException ex)
            {
                // Covers script-level errors thrown inside validatePurchase.js, network failures,
                // and UGS-side outages alike -- all surface here as a rejected-not-approved result,
                // per IPurchaseValidationService's "never throw for an ordinary rejection" contract.
                // IapCommerceService leaves the order unconfirmed on any Failure result, so this is
                // always safely retryable on a later connect, never a lost purchase.
                Debug.LogWarning($"[UgsCloudCodeValidationService] Cloud Code call failed validating productId='{request.ProductId}': {ex.Message}");
                return PurchaseValidationResult.Failure("Could not reach the store to verify this purchase.");
            }

            if (response == null)
            {
                return PurchaseValidationResult.Failure("Store returned no verification result.");
            }

            if (!response.approved)
            {
                return PurchaseValidationResult.Failure(
                    string.IsNullOrEmpty(response.failureReason) ? "Purchase could not be verified." : response.failureReason);
            }

            // grantTransactionId should always be set by the script when approved (it's Google's
            // own orderId) -- falling back to the client's transactionId only guards against a
            // malformed/older script response, never expected in normal operation.
            string grantId = string.IsNullOrEmpty(response.grantTransactionId) ? request.TransactionId : response.grantTransactionId;
            return PurchaseValidationResult.Success(grantId);
        }

        private static Task EnsureSignedInAsync() => GooglePlayGamesAuthService.EnsureSignedInAsync();
    }
}
