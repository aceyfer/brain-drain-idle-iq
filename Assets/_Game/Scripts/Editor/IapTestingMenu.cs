#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;
using BrainDrain.Systems.Commerce;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// BrainDrain > Testing > IAP -- exercises the IAP safety-audit fixes (grant/save/confirm
    /// ordering, Deferred-state UI, refund revocation, offline/unavailable UI) without a real
    /// store connection or real money. Same RequirePlayMode-gated convention as
    /// TestingMenuShortcuts.cs, since every hook here reaches into live Play Mode singletons.
    /// Unity IAP's own Order/StoreController types aren't constructible from outside the SDK, so
    /// these call new editor-only debug hooks on IapCommerceService/SaveManager/GodTierStoreManager
    /// directly rather than simulating a real purchase end to end -- see those hooks' own doc
    /// comments for exactly what each one does and doesn't cover.
    /// </summary>
    public static class IapTestingMenu
    {
        // Brain Freeze family product ids (see Assets/_Game/GodTierStore/*.asset) -- used as the
        // default targets for these menu items so a single click has a sensible, known-good item
        // to act on without needing a selection first.
        private const string BrainFreeze24ProductId = "com.eighthkind.braindrain.brainfreeze";
        private const string BadWordsPackProductId = "com.eighthkind.braindrain.badwordspack";

        [MenuItem("BrainDrain/Testing/IAP/Simulate Success (Brain Freeze 24h)")]
        private static void SimulateSuccess() => RequirePlayMode(() =>
        {
            IapCommerceService.Instance?.DebugSimulateApprovedGrant(BrainFreeze24ProductId);
            Debug.Log("[IapTestingMenu] Simulated an approved grant for Brain Freeze 24h -- check THE WALLET for the new countdown and the God Shop row for '+24h'.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Simulate Pending -> Complete (Bad Words Pack)")]
        private static void SimulatePendingThenComplete() => RequirePlayMode(() =>
        {
            IapCommerceService commerce = IapCommerceService.Instance;
            if (commerce == null) { return; }

            commerce.DebugSimulatePendingThenComplete(BadWordsPackProductId, 2f);
            Debug.Log("[IapTestingMenu] Bad Words Pack row should show 'PURCHASE PENDING' and refuse new taps now, then resolve to OWNED in ~2s.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Simulate Crash Before Grant (re-grant on relaunch)")]
        private static void SimulateCrashBeforeGrant() => RequirePlayMode(() =>
        {
            if (SaveManager.Instance == null || IapCommerceService.Instance == null) { return; }

            SaveManager.Instance.DebugForceSaveFailureOnce = true;
            IapCommerceService.Instance.DebugSimulateApprovedGrant(BrainFreeze24ProductId);
            Debug.Log("[IapTestingMenu] Granted Brain Freeze 24h with the next save forced to fail -- check the Console for "
                + "'[SaveManager] Failed to write save file' followed by '[IapCommerceService] Grant for ... succeeded but the local save failed -- "
                + "NOT confirming the order'. The item IS active in memory right now (THE WALLET shows it), same as a real app that crashes "
                + "in the gap between grant and save -- the next real connect/relaunch replays the unconfirmed order instead of losing it. "
                + "Run 'Add 10K Brain Power' or any other action that calls RequestSave() again afterward to confirm a later successful save recovers normally.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Simulate Refund (revoke Bad Words Pack)")]
        private static void SimulateRefund() => RequirePlayMode(() =>
        {
            GodTierStoreManager godShop = GodTierStoreManager.Instance;
            IapCommerceService commerce = IapCommerceService.Instance;
            if (godShop == null || commerce == null) { return; }

            GodTierStoreItemData badWordsPack = FindItemByProductId(godShop, BadWordsPackProductId);
            if (badWordsPack == null)
            {
                Debug.LogWarning("[IapTestingMenu] Bad Words Pack item not found in GodTierStoreManager.Items.");
                return;
            }

            if (!godShop.IsItemOwned(badWordsPack))
            {
                // Grant it first so there's something to revoke -- a refund test on an item that
                // was never owned wouldn't demonstrate anything.
                commerce.DebugSimulateApprovedGrant(BadWordsPackProductId);
                Debug.Log("[IapTestingMenu] Bad Words Pack wasn't owned yet -- granted it first so the refund has something to revoke.");
            }

            // Empty list = "the store now reports nothing owned" -- revokes every locally-owned
            // non-consumable whose productId is missing from it, Bad Words Pack included.
            commerce.DebugSimulateOwnedReport(new List<string>());
            Debug.Log("[IapTestingMenu] Simulated the store reporting Bad Words Pack no longer owned -- check the Console for "
                + "'[GodTierStoreManager] Revoking ...' and that the row's button/text went back to buyable and profanity lines got re-locked.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Simulate Store Offline")]
        private static void SimulateStoreOffline() => RequirePlayMode(() =>
        {
            IapCommerceService.Instance?.DebugSimulateOffline();
            Debug.Log("[IapTestingMenu] Forced commerce readiness to Unavailable -- every God Shop row should now show 'STORE UNAVAILABLE' with Buy disabled. Run 'Clear Simulated Offline' to restore normal readiness.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Clear Simulated Offline")]
        private static void ClearSimulatedOffline() => RequirePlayMode(() =>
        {
            IapCommerceService.Instance?.DebugClearSimulatedOffline();
            Debug.Log("[IapTestingMenu] Restored commerce readiness to Ready.");
        });

        private static GodTierStoreItemData FindItemByProductId(GodTierStoreManager godShop, string productId)
        {
            IReadOnlyList<GodTierStoreItemData> items = godShop.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].productId == productId) { return items[i]; }
            }
            return null;
        }

        /// <summary>Same guard TestingMenuShortcuts.cs uses -- every hook here reaches into live
        /// Play Mode singletons that either don't exist or shouldn't be self-bootstrapped in Edit
        /// mode.</summary>
        private static void RequirePlayMode(System.Action action)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[IapTestingMenu] This only works in Play Mode.");
                return;
            }

            action();
        }
    }
}
#endif
