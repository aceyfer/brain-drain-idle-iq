using UnityEngine;
using TMPro;
using BrainDrain.Core;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single building row in the shop scroll list.
    /// Binds to a BuildingData template and reflects locked / affordable / too-expensive states.
    /// </summary>
    public sealed class UpgradeSlotUI : MonoBehaviour
    {
        // Neon, high-contrast palette (bloom-ready).
        private static readonly Color LockedColor = new Color32(0x4A, 0x4E, 0x5D, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0xFF, 0x00, 0x7F, 0xFF);

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Interaction")]
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private UnityEngine.UI.Image background;

        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI DescriptionText { get => descriptionText; set => descriptionText = value; }
        public TextMeshProUGUI CostText { get => costText; set => costText = value; }
        public TextMeshProUGUI LevelText { get => levelText; set => levelText = value; }
        public UnityEngine.UI.Button BuyButton { get => buyButton; set => buyButton = value; }
        public UnityEngine.UI.Image Background { get => background; set => background = value; }

        private BuildingData boundData;
        private UpgradeManager boundManager;

        /// <summary>Binds this slot to a building template and wires the buy button.</summary>
        public void Bind(BuildingData data, UpgradeManager manager)
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
                boundManager.TryBuyBuilding(boundData);
            }
        }

        /// <summary>Recomputes labels and visual state based on current currency and player level.</summary>
        public void RefreshState(CurrencyManager currency, IQDecaySystem decay)
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            bool unlocked = decay != null && decay.CurrentLevel >= boundData.unlockPlayerLevel;
            double cost = boundManager.GetCurrentCost(boundData);
            int level = boundManager.GetBuildingLevel(boundData);
            bool affordable = unlocked && currency != null && currency.CanAffordBrains(cost);

            if (levelText != null)
            {
                levelText.text = $"LVL {level}";
            }

            if (descriptionText != null)
            {
                descriptionText.text = unlocked ? boundData.description : "Access restricted by the Ministry.";
            }

            if (!unlocked)
            {
                if (nameText != null) nameText.text = "??? CLASSIFIED ???";
                if (costText != null) costText.text = $"LEVEL {boundData.unlockPlayerLevel} REQUIRED";
                ApplyAccent(LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            if (nameText != null) nameText.text = boundData.buildingName;
            if (costText != null) costText.text = NumberFormatter.Format(cost);

            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor);

            // Keep interactable so the player can attempt purchase; manager silently rejects if unaffordable.
            if (buyButton != null) buyButton.interactable = true;
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null)
            {
                // Subtle translucent tint so neon rows read as glowing panels, not solid blocks.
                background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            }

            if (nameText != null)
            {
                nameText.color = accent;
            }

            if (costText != null)
            {
                costText.color = accent;
            }
        }
    }
}
