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

            double cost = boundManager.GetCurrentCost(boundData);
            int level = boundManager.GetBuildingLevel(boundData);

            // Reveal = first-affordable latch: owned, or lifetime currency ever reached the real
            // unlock gate (unlockCumulativeBrainPower), not baseCost -- baseCost is the price,
            // not the progression gate, and using it let far-tier buildings (e.g. Brain-Rot
            // Think Tank, gated at 725,000 unlockCumulativeBrainPower) reveal as soon as
            // cumulative currency passed their much lower baseCost instead. Always compared
            // against CumulativeBrainPower regardless of the item's own costType -- the unlock
            // gate is BP-denominated for every building, Cash-priced or not (PROJECT_BIBLE.md
            // §6 lists "Unlock (cum. BP)" as the gate column for both tabs), matching
            // pastPurchaseGate below and ShopQuery.cs's equivalent check. Cumulative BP is
            // monotonic, so this never un-reveals on spend.
            bool isCash = UpgradeManager.IsCashCost(boundData);
            bool everAfforded = currency != null && currency.CumulativeBrainPower >= boundData.unlockCumulativeBrainPower;
            bool unlocked = level > 0 || everAfforded;

            // Purchase still gated by the BP progression gate (economy unchanged).
            bool pastPurchaseGate = currency != null && currency.CumulativeBrainPower >= boundData.unlockCumulativeBrainPower;
            bool affordable = unlocked && pastPurchaseGate && boundManager.CanAffordBuilding(boundData);
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
                    if (boundData.tapBrainPowerPerLevel > 0)
                    {
                        if (perLevel.Length > 0) perLevel += "  ";
                        perLevel += $"+{NumberFormatter.Format(boundData.tapBrainPowerPerLevel)} BP/tap";
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
                        string totalTap = boundData.tapBrainPowerPerLevel > 0
                            ? $"+{NumberFormatter.Format(level * boundData.tapBrainPowerPerLevel)} BP/tap"
                            : "";
                        string totalParts = "";
                        if (totalBP.Length > 0) totalParts = totalBP;
                        if (totalC.Length > 0) totalParts += (totalParts.Length > 0 ? "  " : "") + totalC;
                        if (totalTap.Length > 0) totalParts += (totalParts.Length > 0 ? "  " : "") + totalTap;
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
                    nameText.text = ClassificationTier.GetLabel(boundData.unlockCumulativeBrainPower);
                    nameText.fontSize = 32f;
                }
                if (countText != null) countText.text = string.Empty;
                if (costText != null)
                {
                    // Same number as the "REACH X BP" text below once revealed -- both locked
                    // sub-states show unlockCumulativeBrainPower, the real gate. Never $-prefixed
                    // here even for Cash-priced buildings: unlockCumulativeBrainPower is always a
                    // BP figure, and a "$" in front of a BP number is misleading regardless of
                    // the item's own costType. $ formatting is reserved for an actual Cash price
                    // on an unlocked, purchasable row (see below).
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
                costText.text = !pastPurchaseGate
                    ? $"REACH {NumberFormatter.Format(boundData.unlockCumulativeBrainPower)} BP"
                    : isCash
                        ? $"${NumberFormatter.Format(cost)}"
                        : $"{NumberFormatter.Format(cost)} BP";
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
