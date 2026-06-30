using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Visual controller for a single building row in the shop scroll list.
    /// Binds to a BuildingData template and reflects locked / affordable / too-expensive states.
    /// </summary>
    public sealed class UpgradeSlotUI : MonoBehaviour
    {
        // Calmer, professional, non-flashy palette.
        private static readonly Color LockedColor = new Color32(0x8A, 0x8D, 0x9B, 0xFF);
        private static readonly Color AffordableColor = new Color32(0x2E, 0x7D, 0x32, 0xFF); // Matte dark green
        private static readonly Color TooExpensiveColor = new Color32(0x7F, 0x8C, 0x8D, 0xFF); // Matte grey or muted slate

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [FormerlySerializedAs("levelText")]
        [SerializeField] private TextMeshProUGUI countText;

        [Header("Interaction")]
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private UnityEngine.UI.Image background;

        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI DescriptionText { get => descriptionText; set => descriptionText = value; }
        public TextMeshProUGUI CostText { get => costText; set => costText = value; }
        public TextMeshProUGUI CountText { get => countText; set => countText = value; }
        public UnityEngine.UI.Button BuyButton { get => buyButton; set => buyButton = value; }
        public UnityEngine.UI.Image Background { get => background; set => background = value; }

        private BuildingData boundData;
        private UpgradeManager boundManager;
        private bool wasAffordable;

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

        /// <summary>Recomputes labels and visual state based on current currency.</summary>
        public void RefreshState(CurrencyManager currency)
        {
            if (boundData == null || boundManager == null)
            {
                return;
            }

            bool unlocked = currency != null && currency.CumulativeBrainPower >= boundData.unlockCumulativeBrainPower;
            double cost = boundManager.GetCurrentCost(boundData);
            int level = boundManager.GetBuildingLevel(boundData);
            bool affordable = unlocked && currency != null && currency.CanAffordBrainPower(cost);
            UpdateAffordablePulse(affordable);

            if (countText != null)
            {
                countText.text = $"OWNED: {level}";
                countText.fontSize = 28f; // Large font
            }

            if (descriptionText != null)
            {
                if (unlocked)
                {
                    double bppsMult = (currency != null) ? (currency.RebirthMultiplier * currency.ShopAllMultiplier) : 1d;
                    double cashMult = (currency != null) ? (currency.CashMultiplier * currency.ShopCashMultiplier * currency.ShopAllMultiplier) : 1d;

                    double singleBpps = boundData.baseBrainPowerPerSecond * bppsMult;
                    double singleCash = boundData.baseCashPerSecond * cashMult;

                    string perLevel = "";
                    if (boundData.baseBrainPowerPerSecond > 0)
                        perLevel += $"+{NumberFormatter.Format(singleBpps)} BP/s";
                    if (boundData.baseCashPerSecond > 0)
                    {
                        if (perLevel.Length > 0) perLevel += "  ";
                        perLevel += $"+${NumberFormatter.Format(singleCash)}/s";
                    }

                    string totalLine = "";
                    if (level > 0)
                    {
                        double totalBpps = level * boundData.baseBrainPowerPerSecond * bppsMult;
                        double totalCash = level * boundData.baseCashPerSecond * cashMult;

                        string totalBP = boundData.baseBrainPowerPerSecond > 0
                            ? $"+{NumberFormatter.Format(totalBpps)} BP/s"
                            : "";
                        string totalC = boundData.baseCashPerSecond > 0
                            ? $"+${NumberFormatter.Format(totalCash)}/s"
                            : "";
                        string totalParts = totalBP.Length > 0 && totalC.Length > 0
                            ? $"{totalBP}  {totalC}"
                            : $"{totalBP}{totalC}";
                        totalLine = $"\n<color=#FFD700>TOTAL ({level}×): {totalParts}</color>";
                    }

                    descriptionText.text = $"{boundData.description}\n<color=#00F0FF><b>Per level: {perLevel}</b></color>{totalLine}";
                }
                else
                {
                    descriptionText.text = "Access restricted by the Ministry.";
                }
                descriptionText.fontSize = 26f;
            }

            if (!unlocked)
            {
                if (nameText != null)
                {
                    nameText.text = "??? CLASSIFIED ???";
                    nameText.fontSize = 32f;
                }
                if (costText != null)
                {
                    costText.text = $"{NumberFormatter.Format(boundData.unlockCumulativeBrainPower)} BP REQUIRED";
                    costText.fontSize = 28f;
                }
                ApplyAccent(LockedColor);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            if (nameText != null)
            {
                nameText.text = boundData.buildingName;
                nameText.fontSize = 32f;
            }
            if (costText != null)
            {
                costText.text = $"{NumberFormatter.Format(cost)} BP";
                costText.fontSize = 30f;
            }

            ApplyAccent(affordable ? AffordableColor : TooExpensiveColor);

            // Keep interactable so the player can attempt purchase; manager silently rejects if unaffordable.
            if (buyButton != null) buyButton.interactable = true;
        }

        private void OnDestroy()
        {
            if (background != null)
            {
                AnimationController.StopAffordablePulse(background.rectTransform);
            }
        }

        private void UpdateAffordablePulse(bool affordable)
        {
            // Disabled per user request (too flashy)
            if (background != null)
            {
                AnimationController.StopAffordablePulse(background.rectTransform);
            }
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
                nameText.color = Color.white;
            }

            if (descriptionText != null)
            {
                descriptionText.color = Color.white;
            }

            if (costText != null)
            {
                costText.color = accent;
            }
        }
    }
}
