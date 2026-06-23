using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single Shop 3 (Points Power Shop) item row -- mirrors
    /// CashShopSlotUI's locked/affordable/too-expensive/owned pattern, gated on RebirthCount
    /// and (optionally) a World Restoration stage rather than RebirthCount alone.
    /// </summary>
    public sealed class PointsShopSlotUI : MonoBehaviour
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

        private PointsShopItemData boundData;
        private PointsShopManager boundManager;

        public void Bind(PointsShopItemData data, PointsShopManager manager)
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
                if (descriptionText != null) descriptionText.text = "The Illumisnotti haven't been weakened enough yet.";
                string gateDescription = boundData.gateWorldStageIndex >= 0
                    ? $"REBIRTH {boundData.gateRebirthCount} + WORLD STAGE {boundData.gateWorldStageIndex} REQUIRED"
                    : $"REBIRTH {boundData.gateRebirthCount} REQUIRED";
                if (costText != null) costText.text = gateDescription;
                ApplyAccent(LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            bool affordable = currency != null && currency.CanAffordPoints(boundData.cost);
            if (descriptionText != null) descriptionText.text = boundData.description;
            if (costText != null) costText.text = $"{NumberFormatter.Format(boundData.cost)} POINTS";
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
