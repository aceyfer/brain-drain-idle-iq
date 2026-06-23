using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single God Tier Store row. No affordable/too-expensive states --
    /// these are real-money items with no in-game currency check -- just Owned vs. not. The
    /// "Buy" button calls GodTierStoreManager.StubPurchase directly, which is a clearly marked
    /// placeholder (see GodTierStoreManager's class doc) that does NOT charge real money; wire a
    /// real IAP plugin's purchase-success callback to call StubPurchase instead of this button
    /// before shipping.
    /// </summary>
    public sealed class GodTierStoreSlotUI : MonoBehaviour
    {
        private static readonly Color AvailableColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);
        private static readonly Color OwnedColor = new Color32(0x39, 0xFF, 0x14, 0xFF);

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;

        [Header("Interaction")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private GodTierStoreItemData boundData;
        private GodTierStoreManager boundManager;

        public void Bind(GodTierStoreItemData data, GodTierStoreManager manager)
        {
            boundData = data;
            boundManager = manager;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }
        }

        private void HandleBuyClicked()
        {
            boundManager?.StubPurchase(boundData);
        }

        public void RefreshState()
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            if (nameText != null) nameText.text = boundData.displayName;
            if (descriptionText != null) descriptionText.text = boundData.description;

            bool owned = boundManager.IsItemOwned(boundData);
            if (priceText != null)
            {
                priceText.text = owned ? "OWNED" : boundData.realMoneyPriceDisplay;
            }

            ApplyAccent(owned ? OwnedColor : AvailableColor);
            if (buyButton != null) buyButton.interactable = !owned;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = accent;
            if (priceText != null) priceText.color = accent;
        }
    }
}
