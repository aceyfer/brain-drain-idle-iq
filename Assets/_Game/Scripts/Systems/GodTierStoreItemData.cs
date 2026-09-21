using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>What a God Tier Store item actually does once purchased (verified) and granted.</summary>
    public enum GodTierStoreEffectType
    {
        VoicepackDisdain,
        UIThemeGlitchSlum,
        OfflineProgressionExtension,
        MembershipCardCosmetic,
        TrashCanFlexCosmetic,
        UnlockProfanityPack,
        BrainFreezeIQImmunity // APPEND-ONLY enum: assets store effectType as a raw int; always add new values at the end, never insert above this comment
    }

    /// <summary>
    /// Authoring data for one God Tier Store item -- real-money-only, cosmetics/QoL, never
    /// power. Real purchases route through IapCommerceService (Unity IAP) with server-side
    /// validation (§12); GodTierStoreManager.RequestPurchase starts a purchase, and only a
    /// backend-approved result ever reaches GrantVerifiedEntitlement. productId is the real
    /// App Store Connect / Play Console product SKU -- see its own tooltip below.
    /// realMoneyPriceDisplay is a display-only fallback for when the store hasn't returned a
    /// localized price yet (offline, or Editor Play Mode without a live store connection); the
    /// store's own localized price is authoritative whenever it's available.
    /// </summary>
    [CreateAssetMenu(fileName = "GodTierStoreItemData", menuName = "BrainDrain/God Tier Store Item")]
    public sealed class GodTierStoreItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        [Tooltip("Real App Store Connect / Play Console product SKU, e.g. \"com.eighthkind.braindrain.brainfreeze\". Must match the store listing exactly once a product is live there -- do not change after that point. Empty for items with no store SKU registered yet.")]
        public string productId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        [Tooltip("Display-only fallback, e.g. \"$1.99\" -- used only when the store hasn't returned a localized price yet. The real charge amount is always Play Console's own price for productId.")]
        public string realMoneyPriceDisplay;

        [Header("Effect")]
        public GodTierStoreEffectType effectType;
        [Tooltip("If true, this is a timed consumable -- GodTierStoreManager.GrantVerifiedEntitlement never adds it to ownedItemIds, so it can be bought again and again (its own effect target, e.g. PlayerIQManager.ApplyBrainFreeze, is what stacks the new duration onto whatever's already active). If false (default), it's a permanent one-time unlock tracked via ownedItemIds/IsItemOwned as usual.")]
        public bool isConsumable;
        [Tooltip("Used only by OfflineProgressionExtension -- added to PlayerIQManager's offline-decay-max-hours window.")]
        public float offlineExtensionHours;
        [Tooltip("Used only by BrainFreezeIQImmunity -- real-time hours PlayerIQ is protected at a floor of 113 (after an immediate jump to at least 200 on purchase). Stacks additively onto any currently-active freeze's duration and re-triggers the 200 jump.")]
        public float freezeDurationHours;
    }
}
