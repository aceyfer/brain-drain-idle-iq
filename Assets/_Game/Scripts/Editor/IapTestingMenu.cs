#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BrainDrain.Core;
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
        // GodTierStoreItemData.itemId (not productId) -- what GetFreezeInventoryCount/
        // ActivateFreeze/DebugBuyBrainFreeze key on, see BrainFreeze.asset.
        private const string BrainFreezeItemId = "brain_freeze";
        private const string BadWordsPackProductId = "com.eighthkind.braindrain.badwordspack";

        [MenuItem("BrainDrain/Testing/IAP/Simulate Success (Brain Freeze 24h)")]
        private static void SimulateSuccess() => RequirePlayMode(() =>
        {
            IapCommerceService.Instance?.DebugSimulateApprovedGrant(BrainFreeze24ProductId);
            Debug.Log("[IapTestingMenu] Simulated an approved grant for Brain Freeze 24h -- 2026-10-06: a grant now only adds a Wallet charge, it doesn't activate -- check THE WALLET for the new row ('x1'), then use 'Activate Brain Freeze' below to actually start it.");
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
                + "NOT confirming the order'. The charge IS in the wallet's in-memory inventory right now (THE WALLET shows 'x1'), same as a real "
                + "app that crashes in the gap between grant and save -- the next real connect/relaunch replays the unconfirmed order instead of "
                + "losing it. Run 'Add 10K Brain Power' or any other action that calls RequestSave() again afterward to confirm a later successful "
                + "save recovers normally.");
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

        // ── Freeze Inventory (2026-10-06 amendment) ────────────────────────────────────
        // Exercises the charge-based wallet model: unlimited purchases just add charges,
        // only one freeze can ever be active, expiry fires a toast nudge (never
        // auto-consumes), and the Cloud Save mirror survives a simulated reinstall.

        [MenuItem("BrainDrain/Testing/IAP/Freeze Inventory/Buy Brain Freeze x3")]
        private static void SimulateBuyFreezeX3() => RequirePlayMode(() =>
        {
            GodTierStoreManager godShop = GodTierStoreManager.Instance;
            if (godShop == null) { return; }

            godShop.DebugBuyBrainFreeze();
            godShop.DebugBuyBrainFreeze();
            godShop.DebugBuyBrainFreeze();
            Debug.Log($"[IapTestingMenu] Bought Brain Freeze x3 -- wallet now has {godShop.GetFreezeInventoryCount(BrainFreezeItemId)} charge(s) (expect 3, or +3 if the wallet already had charges).");
        });

        [MenuItem("BrainDrain/Testing/IAP/Freeze Inventory/Activate Brain Freeze (locks others)")]
        private static void SimulateActivateFreeze() => RequirePlayMode(() =>
        {
            GodTierStoreManager godShop = GodTierStoreManager.Instance;
            if (godShop == null) { return; }

            int before = godShop.GetFreezeInventoryCount(BrainFreezeItemId);
            godShop.DebugActivateBrainFreeze();
            int after = godShop.GetFreezeInventoryCount(BrainFreezeItemId);
            Debug.Log($"[IapTestingMenu] Activated Brain Freeze -- inventory {before} -> {after}. Open THE WALLET: every row's Use button should now read 'Active — ...' and be disabled, and an active card with a countdown pill should be pinned above the rows.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Freeze Inventory/Force Expire Active Freeze (nudge test)")]
        private static void SimulateForceExpireFreeze() => RequirePlayMode(() =>
        {
            if (PlayerIQManager.Instance == null) { return; }
            if (!PlayerIQManager.Instance.IsBrainFreezeActive)
            {
                Debug.LogWarning("[IapTestingMenu] No freeze is currently active -- run 'Activate Brain Freeze' first.");
                return;
            }

            PlayerIQManager.Instance.SetBrainFreezeExpiry(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1);
            Debug.Log("[IapTestingMenu] Forced the active freeze's expiry into the past -- the 'Freeze ended. Use another? (xN left)' toast should appear within ~1s, on the next GameManager.OnSecondTick (silent if the item has 0 charges left).");
        });

        [MenuItem("BrainDrain/Testing/IAP/Freeze Inventory/Simulate Reinstall Restore From Cloud")]
        private static void SimulateReinstallRestore() => RequirePlayMode(() =>
        {
            GodTierStoreManager godShop = GodTierStoreManager.Instance;
            if (godShop == null) { return; }

            if (godShop.GetFreezeInventoryCount(BrainFreezeItemId) <= 0)
            {
                godShop.DebugBuyBrainFreeze();
                Debug.Log("[IapTestingMenu] Wallet was empty -- bought 1 Brain Freeze charge first so there's something to restore.");
            }

            FreezeInventoryCloudSync.PushAsync(godShop);
            Debug.Log("[IapTestingMenu] Pushed the current wallet to Cloud Save. Wiping LOCAL inventory only (simulating a reinstall)...");

            godShop.DebugWipeLocalFreezeInventory();
            Debug.Log($"[IapTestingMenu] Local wallet wiped -- now {godShop.GetFreezeInventoryCount(BrainFreezeItemId)} charge(s) (expect 0). Reconciling from Cloud Save...");

            FreezeInventoryCloudSync.ReconcileOnLaunchAsync(godShop);
            Debug.Log("[IapTestingMenu] Reconcile requested (async, fire-and-forget) -- check the Console in a moment and THE WALLET for the charge count to come back from cloud.");
        });

        [MenuItem("BrainDrain/Testing/IAP/Freeze Inventory/Reset Freeze Tutorial Seen Flag")]
        private static void ResetFreezeTutorialSeen() => RequirePlayMode(() =>
        {
            GodTierStoreManager.Instance?.DebugResetFreezeTutorialSeen();
            Debug.Log("[IapTestingMenu] Freeze tutorial seen-flag reset -- the next 'Buy Brain Freeze x3' (or any freeze grant/restore) will show the tutorial popup again.");
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
