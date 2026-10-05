using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;
using BrainDrain.Systems.Commerce;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single God Tier Store row. No affordable/too-expensive states --
    /// these are real-money items with no in-game currency check -- just Owned / Offline / Busy /
    /// Available. The "Buy" button calls GodTierStoreManager.RequestPurchase, which starts a real
    /// async store purchase via IapCommerceService (§12) -- nothing is granted here or by that
    /// call itself; this row only reflects state IapCommerceService/GodTierStoreManager report
    /// back (OnPurchaseStateChanged for busy/error, OnItemsChanged for the eventual grant).
    /// </summary>
    public sealed class GodTierStoreSlotUI : MonoBehaviour
    {
        // 2026-09-29 color pass: gold -> white, matching the bottom bar's bright-label-when-
        // actionable convention (God Shop rows have no separate fill state to touch here -- see
        // ApplyAccent below, which still only recolors label text/background tint, never fill).
        private static readonly Color AvailableColor = Color.white;
        // 2026-10-04 PALETTE LOCKDOWN: OwnedColor was lime -- now Cyan; UnavailableColor now
        // shares the Dim locked/unaffordable role token.
        private static readonly Color OwnedColor = Palette.Cyan;
        private static readonly Color UnavailableColor = Palette.Dim;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;

        [Header("Interaction")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private GodTierStoreItemData boundData;
        private GodTierStoreManager boundManager;
        private IapCommerceService subscribedCommerceService;
        private bool purchaseInFlight;

        /// <summary>
        /// Populates the private serialized references for runtime-created instances (the
        /// UpgradeSlotUI-clone template pattern in ShopUIController). Scene/prefab instances
        /// keep their Inspector-serialized fields and never call this.
        /// </summary>
        public void AssignRuntimeReferences(
            TextMeshProUGUI nameLabel,
            TextMeshProUGUI descriptionLabel,
            TextMeshProUGUI priceLabel,
            Button buy,
            Image backgroundImage)
        {
            nameText = nameLabel;
            descriptionText = descriptionLabel;
            priceText = priceLabel;
            buyButton = buy;
            background = backgroundImage;
        }

        public void Bind(GodTierStoreItemData data, GodTierStoreManager manager)
        {
            boundData = data;
            boundManager = manager;
            purchaseInFlight = false;

            ShopBuyButtonLayout.Register(transform, buyButton, priceText, descriptionText);

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }

            // Fixed here, not on the shared UpgradeSlotPrefab's CostText -- that same TMP object
            // also backs the BP shop's UpgradeSlotUI rows, so editing the prefab's own alignment/
            // margin would misalign those too. CostText ships Right-aligned with zero margin and
            // Overflow mode, so the price renders flush against the button's right edge, under
            // the border frame art -- worst on stages whose border eats furthest into the
            // interior (Stage 2/5). Centering + a real margin keeps it clear of the frame on
            // every stage without touching the prefab at all.
            if (priceText != null)
            {
                priceText.alignment = TextAlignmentOptions.Center;
                priceText.margin = new Vector4(16f, 4f, 16f, 4f);
            }

            // Touching .Instance here is fine (self-bootstraps if needed) -- GodTierStoreManager's
            // own Start already does the same thing, and by the time slots are built/bound the
            // commerce service normally already exists.
            IapCommerceService commerce = IapCommerceService.Instance;
            if (commerce != subscribedCommerceService)
            {
                UnsubscribeFromCommerce();
                subscribedCommerceService = commerce;
                if (subscribedCommerceService != null)
                {
                    subscribedCommerceService.OnPurchaseStateChanged += HandlePurchaseStateChanged;
                    subscribedCommerceService.OnReadinessChanged += HandleReadinessChanged;
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromCommerce();
        }

        private void UnsubscribeFromCommerce()
        {
            if (subscribedCommerceService == null)
            {
                return;
            }

            subscribedCommerceService.OnPurchaseStateChanged -= HandlePurchaseStateChanged;
            subscribedCommerceService.OnReadinessChanged -= HandleReadinessChanged;
            subscribedCommerceService = null;
        }

        private void HandlePurchaseStateChanged(string productId, PurchaseRequestState state)
        {
            if (boundData == null || productId != boundData.productId)
            {
                return;
            }

            purchaseInFlight = state == PurchaseRequestState.Pending || state == PurchaseRequestState.ValidatingWithBackend;
            RefreshState();
        }

        private void HandleReadinessChanged(CommerceReadiness readiness)
        {
            RefreshState();
        }

        private void HandleBuyClicked()
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            // Owned Bad Words Pack: the button becomes the profanity on/off toggle -- the
            // affordance the retired Cash slot used to own (see CashShopSlotUI's dead-branch
            // note). Purchase force-enables; after that the player's choice rules.
            if (boundData.effectType == GodTierStoreEffectType.UnlockProfanityPack
                && boundManager.IsItemOwned(boundData))
            {
                RandomChatterManager chatter = RandomChatterManager.Instance;
                if (chatter != null)
                {
                    chatter.ToggleProfanity(!chatter.ProfanityEnabled);
                    RefreshState();
                }
                return;
            }

            if (purchaseInFlight)
            {
                return; // extra guard on top of IapCommerceService's own double-tap protection
            }

            boundManager.RequestPurchase(boundData);
        }

        public void RefreshState()
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            if (nameText != null) nameText.text = boundData.displayName;
            if (descriptionText != null) descriptionText.text = boundData.description;

            // Consumables (e.g. the Brain Freeze family) are never added to ownedItemIds by
            // GrantVerifiedEntitlement, so boundManager.IsItemOwned would already always read
            // false for them -- this check is made explicit rather than relying on that
            // invariant, so a consumable's row always shows its price and stays interactable by
            // this file's own logic, not by trusting a distant guarantee elsewhere.
            bool owned = !boundData.isConsumable && boundManager.IsItemOwned(boundData);
            bool profanityToggle = owned
                && boundData.effectType == GodTierStoreEffectType.UnlockProfanityPack;

            // IAP RULES: "button label shows +24h/+72h/+7d while one is active" -- repurchasing a
            // timed consumable extends the stack (PlayerIQManager.ApplyBrainFreeze), it never
            // wastes the purchase, so the label communicates that instead of just repeating the
            // price. Checked by itemId, not productId, same key GodTierStoreManager's own ledger
            // uses (ActiveTimedPurchase.itemId).
            bool hasActiveTimedPurchase = boundData.isConsumable && IsThisItemActive();

            IapCommerceService commerce = IapCommerceService.Instance;
            bool offline = commerce == null || commerce.IsOffline;
            bool storeReady = commerce != null && commerce.IsReady;

            // §12 decision 8: show owned items regardless of connectivity, only disable NEW
            // purchases while offline -- ownership/ApplyAccent below never depends on offline.
            bool canPurchase = !owned && !offline && storeReady && !purchaseInFlight;

            if (priceText != null)
            {
                if (profanityToggle)
                {
                    RandomChatterManager chatter = RandomChatterManager.Instance;
                    bool on = chatter != null && chatter.ProfanityEnabled;
                    priceText.text = on ? "OWNED · ON" : "OWNED · OFF";
                }
                else if (owned)
                {
                    priceText.text = "OWNED";
                }
                else if (offline)
                {
                    priceText.text = "OFFLINE";
                }
                else if (purchaseInFlight)
                {
                    priceText.text = "...";
                }
                else if (hasActiveTimedPurchase)
                {
                    priceText.text = FormatExtendLabel(boundData.freezeDurationHours);
                }
                else
                {
#if UNITY_EDITOR
                    // The Editor's dev fake store returns a flat fake price ($0.01) for every
                    // product -- not useful for eyeballing real pricing while testing. Device
                    // builds never hit this branch; they always want the real localized price.
                    priceText.text = boundData.realMoneyPriceDisplay;
#else
                    // Store-localized price wins whenever the store has actually returned one;
                    // realMoneyPriceDisplay is only the offline preview fallback (see
                    // GodTierStoreItemData's own doc comment) -- e.g. while storeReady is still
                    // false during initial connect/fetch.
                    string localizedPrice = commerce?.GetLocalizedPrice(boundData.productId);
                    priceText.text = !string.IsNullOrEmpty(localizedPrice) ? localizedPrice : boundData.realMoneyPriceDisplay;
#endif
                }
            }

            Color accent = owned ? OwnedColor : (offline || !storeReady) ? UnavailableColor : AvailableColor;
            ApplyAccent(accent);
            if (buyButton != null) buyButton.interactable = profanityToggle || canPurchase;
        }

        /// <summary>Whether boundManager currently has an unexpired ActiveTimedPurchase ledger
        /// entry for this row's own itemId -- ActiveTimedPurchases already prunes expired entries
        /// on every read, so "any match" is sufficient, no extra expiry check needed here.</summary>
        private bool IsThisItemActive()
        {
            var active = boundManager.ActiveTimedPurchases;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].itemId == boundData.itemId) { return true; }
            }
            return false;
        }

        /// <summary>Matches the three current Brain Freeze family durations exactly (24h/72h/7d
        /// per the IAP rules spec) with a sensible rule for any future value: a round number of
        /// days at a week or more reads as days, everything else as hours.</summary>
        private static string FormatExtendLabel(float hours)
        {
            int wholeHours = Mathf.RoundToInt(hours);
            if (wholeHours >= 168 && wholeHours % 24 == 0)
            {
                return $"+{wholeHours / 24}d";
            }
            return $"+{wholeHours}h";
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = accent;
            if (priceText != null) priceText.color = accent;
        }
    }
}
