using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    public sealed class PremiumShopSlotUI : MonoBehaviour
    {
        private static readonly Color PurchasedColor = new Color32(0x39, 0xFF, 0x14, 0xFF); // Neon green
        private static readonly Color AffordableColor = new Color32(0x00, 0xF0, 0xFF, 0xFF); // Neon blue
        private static readonly Color TooExpensiveColor = new Color32(0xFF, 0x00, 0x7F, 0xFF); // Hot pink

        [Header("Text (Large fonts)")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Interaction")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private PremiumItemData boundData;
        private PremiumShopManager boundManager;

        public void Bind(PremiumItemData data, PremiumShopManager manager)
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
            if (boundManager != null && boundData != null)
            {
                boundManager.TryPurchaseItem(boundData);
            }
        }

        public void RefreshState(CurrencyManager currency)
        {
            if (boundData == null || boundManager == null) return;

            // Display text
            if (nameText != null)
            {
                nameText.text = boundData.displayName;
                nameText.fontSize = 32f; // Large font
            }

            if (descriptionText != null)
            {
                descriptionText.text = boundData.description;
                descriptionText.fontSize = 24f; // Large font
            }

            bool purchased = boundManager.IsItemPurchased(boundData.itemId);
            if (purchased)
            {
                if (costText != null)
                {
                    costText.text = "PURCHASED";
                    costText.fontSize = 28f; // Large font
                }
                ApplyAccent(PurchasedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            bool affordable = currency != null && currency.Neurons >= boundData.neuronCost;
            if (costText != null)
            {
                costText.text = $"{boundData.neuronCost} NEURONS";
                costText.fontSize = 28f; // Large font
            }

            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor);
            if (buyButton != null) buyButton.interactable = affordable;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = accent;
            if (costText != null) costText.color = accent;
        }
    }
}