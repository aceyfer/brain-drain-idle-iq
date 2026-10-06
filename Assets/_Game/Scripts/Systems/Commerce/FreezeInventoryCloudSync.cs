using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.Systems.Commerce
{
    /// <summary>
    /// 2026-10-06 FREEZE INVENTORY AMENDMENT (Aceyfer, rule 5 -- "no lost purchases"): mirrors
    /// GodTierStoreManager's freeze inventory + active-freeze state to UGS Cloud Save, keyed
    /// implicitly to the signed-in player's UGS identity (Cloud Save's Data.Player.* API is
    /// always scoped to whoever GooglePlayGamesAuthService.EnsureSignedInAsync most recently
    /// signed in -- see that file for how/whether that identity survives a reinstall; THIS file
    /// never re-derives or overrides that choice, it only ensures SOME identity exists before
    /// calling Cloud Save, same as UgsCloudCodeValidationService already does for purchase
    /// validation).
    ///
    /// PushAsync is called right after every grant (purchase) and activation -- see
    /// GodTierStoreManager.GrantFreezeInventory/ActivateFreeze. ReconcileOnLaunchAsync runs once
    /// from GodTierStoreManager.Start(), AFTER local save data has already loaded, and merges by
    /// taking the per-item MAX of local vs. cloud counts (GodTierStoreManager.
    /// ReconcileFreezeInventory) and whichever active-freeze expiry protects longer
    /// (AdoptCloudActiveFreezeIfLonger) -- so a charge paid for on one device, or still-active
    /// protection time, is never lost and never duplicated by the merge itself. After a
    /// successful reconcile, the merged state is pushed back so cloud converges too.
    ///
    /// All calls are fire-and-forget (async void) by design, matching
    /// UgsCloudCodeValidationService's own "never blocks the caller, logs and keeps going on any
    /// failure" convention -- a freeze purchase/activation must never visibly hang or fail the UI
    /// because Cloud Save happened to be unreachable; it just keeps the local grant/activation
    /// (already durable via the normal save file) and tries the mirror again on the NEXT
    /// grant/activate/launch. There is no separate background retry loop -- out of scope for a
    /// consumable-count mirror; "retry later" per rule 5 is satisfied by every subsequent
    /// push/launch naturally re-attempting against current local state.
    ///
    /// REQUIRES com.unity.services.cloudsave (added to Packages/manifest.json this same pass,
    /// version picked WITHOUT a live Package Manager session -- same caveat
    /// UgsCloudCodeValidationService's own doc comment raises for hand-editing manifest.json: the
    /// version there is a best-effort guess, NOT verified against a real resolve. Confirm/update
    /// it via Package Manager > Add package by name the next time the project is opened in the
    /// Editor). UNVERIFIED, written blind against Unity's Cloud Save SDK documentation -- same
    /// disclaimer as GooglePlayGamesAuthService, no live Editor/device available this session to
    /// test the actual SaveAsync/LoadAsync call shapes against a real UGS project.
    /// </summary>
    public static class FreezeInventoryCloudSync
    {
        private const string InventoryKey = "braindrain_freezeInventory";
        private const string ActiveItemIdKey = "braindrain_activeFreezeItemId";
        private const string ActiveExpiryKey = "braindrain_activeFreezeExpiryUnixSeconds";

        private static bool reconcileInFlight;

        /// <summary>Pushes the CURRENT local freeze inventory + active-freeze state to Cloud
        /// Save, overwriting whatever was there before. Safe to call freely -- GodTierStoreManager
        /// always calls this AFTER the local grant/activation already happened, so a failed push
        /// never loses the local state, only delays the mirror.</summary>
        public static async void PushAsync(GodTierStoreManager manager)
        {
            if (manager == null) { return; }

            try
            {
                await GooglePlayGamesAuthService.EnsureSignedInAsync();

                var inventory = new Dictionary<string, int>();
                foreach (FreezeInventoryEntry entry in manager.FreezeInventorySnapshot)
                {
                    inventory[entry.itemId] = entry.count;
                }

                long activeExpiry = PlayerIQManager.Instance != null ? PlayerIQManager.Instance.BrainFreezeExpiryUnixSeconds : 0L;
                var data = new Dictionary<string, object>
                {
                    { InventoryKey, inventory },
                    { ActiveItemIdKey, manager.ActiveFreezeItemId ?? string.Empty },
                    { ActiveExpiryKey, activeExpiry },
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            }
            catch (Exception ex)
            {
                // Unreachable/not configured/rate-limited/etc -- keep local state (already
                // durable via the normal save file), try again on the next grant/activate/launch.
                Debug.LogWarning($"[FreezeInventoryCloudSync] Push failed, keeping local state only: {ex.Message}");
            }
        }

        /// <summary>Fetches cloud state once (on launch, after local load) and merges it into the
        /// local manager by taking the per-item max / longer-active-expiry -- never overwrites
        /// local state outright. Re-pushes the merged result so cloud converges too. No-ops if
        /// already in flight (guards against GodTierStoreManager.Start() somehow running twice in
        /// one session) or if manager is null.</summary>
        public static async void ReconcileOnLaunchAsync(GodTierStoreManager manager)
        {
            if (manager == null || reconcileInFlight) { return; }
            reconcileInFlight = true;

            try
            {
                await GooglePlayGamesAuthService.EnsureSignedInAsync();

                var keys = new HashSet<string> { InventoryKey, ActiveItemIdKey, ActiveExpiryKey };
                Dictionary<string, Unity.Services.CloudSave.Models.Item> cloud =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (cloud.TryGetValue(InventoryKey, out var inventoryItem))
                {
                    Dictionary<string, int> cloudInventory = inventoryItem.Value.GetAs<Dictionary<string, int>>();
                    manager.ReconcileFreezeInventory(cloudInventory);
                }

                bool hasItemId = cloud.TryGetValue(ActiveItemIdKey, out var itemIdItem);
                bool hasExpiry = cloud.TryGetValue(ActiveExpiryKey, out var expiryItem);
                if (hasItemId && hasExpiry)
                {
                    string cloudItemId = itemIdItem.Value.GetAs<string>();
                    long cloudExpiry = expiryItem.Value.GetAs<long>();
                    if (!string.IsNullOrWhiteSpace(cloudItemId))
                    {
                        manager.AdoptCloudActiveFreezeIfLonger(cloudItemId, cloudExpiry);
                    }
                }

                // Push the merged result back so cloud converges to the same max, not just this
                // device's local view.
                PushAsync(manager);
            }
            catch (Exception ex)
            {
                // Unreachable/not configured/rate-limited/etc -- per rule 5, "keep local and
                // retry later." Nothing to merge this launch; local state (already loaded from
                // the normal save file before this ever runs) stays exactly as it was.
                Debug.LogWarning($"[FreezeInventoryCloudSync] Launch reconcile failed, keeping local state only: {ex.Message}");
            }
            finally
            {
                reconcileInFlight = false;
            }
        }
    }
}
