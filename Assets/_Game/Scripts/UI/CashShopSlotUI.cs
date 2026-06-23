using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single generic Cash Shop item row -- mirrors UpgradeSlotUI's
    /// locked/affordable/too-expensive pattern, plus a 4th "owned" state since these items are
    /// one-time permanent purchases, not repeatable building levels. The Hot Chick companion
    /// has its own dedicated row inside CashShopUIController rather than using this slot type,
    /// since it's a single sequential-tier progression, not a list of independent items.
    /// </summary>
    public sealed class CashShopSlotUI : MonoBehaviour
    {
        private static readonly Color LockedColor = new Color32(0x4A, 0x4E, 0x5D, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0xFF, 0x00, 0x7F, 0xFF);
        private static readonly Color OwnedColor = new Color32(0x39, 0xFF, 0x14, 0xFF);

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Interaction")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Image background;

        private CashShopItemData boundData;
        private CashShopManager boundManager;

        public void Bind(CashShopItemData data, CashShopManager manager)
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
            boundManager?.TryPurchaseItem(boundData);
        }

        public void RefreshState(CurrencyManager currency)
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = boundData.displayName;
            }

            bool owned = boundManager.IsItemOwned(boundData);
            if (owned)
            {
                if (descriptionText != null) descriptionText.text = boundData.description;
                if (costText != null) costText.text = "OWNED";
                ApplyAccent(OwnedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            bool unlocked = boundManager.IsItemUnlocked(boundData);
            if (!unlocked)
            {
                if (descriptionText != null) descriptionText.text = "Access restricted by the Snotty Council.";
                if (costText != null) costText.text = $"REBIRTH {boundData.gateRebirthCount} REQUIRED";
                ApplyAccent(LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            bool affordable = currency != null && currency.CanAffordCash(boundData.cost);
            if (descriptionText != null) descriptionText.text = boundData.description;
            if (costText != null) costText.text = $"{NumberFormatter.Format(boundData.cost)} CASH";
            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor);
            if (buyButton != null) buyButton.interactable = true;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null) background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (nameText != null) nameText.color = accent;
            if (costText != null) costText.color = accent;
        }
    }
}
