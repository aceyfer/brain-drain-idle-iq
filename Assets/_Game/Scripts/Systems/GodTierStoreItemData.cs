using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>What a God Tier Store item actually does once stub-purchased.</summary>
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
    /// power. NO real payment processing exists in this project (no Unity IAP package, no store
    /// product IDs) -- GodTierStoreManager.StubPurchase grants the item immediately and is
    /// clearly marked as a placeholder for real IAP integration, not a working purchase flow.
    /// realMoneyPriceDisplay is a display-only string; nothing actually charges it yet.
    /// </summary>
    [CreateAssetMenu(fileName = "GodTierStoreItemData", menuName = "BrainDrain/God Tier Store Item")]
    public sealed class GodTierStoreItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        [Tooltip("Display only, e.g. \"$1.99\" -- no real IAP plugin is wired up to actually charge this.")]
        public string realMoneyPriceDisplay;

        [Header("Effect")]
        public GodTierStoreEffectType effectType;
        [Tooltip("Used only by OfflineProgressionExtension -- added to PlayerIQManager's offline-decay-max-hours window.")]
        public float offlineExtensionHours;
        [Tooltip("Used only by BrainFreezeIQImmunity -- real-time hours PlayerIQ is protected at a floor of 113 (after an immediate jump to at least 200 on purchase). Stacks additively onto any currently-active freeze's duration and re-triggers the 200 jump.")]
        public float freezeDurationHours;
    }
}
