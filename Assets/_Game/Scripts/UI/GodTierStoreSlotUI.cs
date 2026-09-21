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
        private static readonly Color AvailableColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);
        private static readonly Color OwnedColor = new Color32(0x39, 0xFF, 0x14, 0xFF);
        private static readonly Color UnavailableColor = new Color32(0x80, 0x80, 0x80, 0xFF);

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

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
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
                else
                {
                    // Store-localized price wins whenever the store has actually returned one;
                    // realMoneyPriceDisplay is only the editor/offline preview fallback (see
                    // GodTierStoreItemData's own doc comment) -- e.g. while storeReady is still
                    // false during initial connect/fetch.
                    string localizedPrice = commerce?.GetLocalizedPrice(boundData.productId);
                    priceText.text = !string.IsNullOrEmpty(localizedPrice) ? localizedPrice : boundData.realMoneyPriceDisplay;
                }
            }

            Color accent = owned ? OwnedColor : (offline || !storeReady) ? UnavailableColor : AvailableColor;
            ApplyAccent(accent);
            if (buyButton != null) buyButton.interactable = profanityToggle || canPurchase;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = accent;
            if (priceText != null) priceText.color = accent;
        }
    }
}
