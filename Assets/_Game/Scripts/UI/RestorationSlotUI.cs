using UnityEngine;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// One row in the RP Restorations shop tab. Spends Restoration Points toward a
    /// WorldRestorationStage milestone threshold via WorldRestorationManager.
    /// </summary>
    public sealed class RestorationSlotUI : MonoBehaviour
    {
        private static readonly Color CompleteColor = new Color32(0x39, 0xFF, 0x14, 0xFF);
        private static readonly Color AffordableColor = new Color32(0xFF, 0x3D, 0xB4, 0xFF);
        private static readonly Color TooExpensiveColor = new Color32(0x7F, 0x8C, 0x8D, 0xFF);
        private static readonly Color LockedColor = new Color32(0x8A, 0x8D, 0x9B, 0xFF);

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private UnityEngine.UI.Image background;

        private WorldRestorationStage boundStage;

        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI DescriptionText { get => descriptionText; set => descriptionText = value; }
        public TextMeshProUGUI CostText { get => costText; set => costText = value; }
        public UnityEngine.UI.Button BuyButton { get => buyButton; set => buyButton = value; }
        public UnityEngine.UI.Image Background { get => background; set => background = value; }

        public void Bind(WorldRestorationStage stage)
        {
            boundStage = stage;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }
        }

        private void HandleBuyClicked()
        {
            if (boundStage == null)
            {
                return;
            }

            WorldRestorationManager manager = WorldRestorationManager.Instance;
            CurrencyManager currency = CurrencyManager.Instance;
            if (manager == null || currency == null)
            {
                return;
            }

            double cumulative = manager.CumulativePointsSpentOnRestoration;
            double needed = boundStage.pointsRequired - cumulative;
            if (needed <= 0d)
            {
                return;
            }

            double spendAmount = System.Math.Min(currency.CurrentPoints, needed);
            if (spendAmount <= 0d)
            {
                return;
            }

            manager.TrySpendPointsOnRestoration(spendAmount);
        }

        public void RefreshState(CurrencyManager currency)
        {
            if (boundStage == null)
            {
                return;
            }

            WorldRestorationManager manager = WorldRestorationManager.Instance;
            double cumulative = manager != null ? manager.CumulativePointsSpentOnRestoration : 0d;
            double currentPoints = currency != null ? currency.CurrentPoints : 0d;
            double needed = boundStage.pointsRequired - cumulative;
            bool complete = needed <= 0d;
            bool affordable = !complete && currentPoints > 0d;

            if (nameText != null)
            {
                nameText.text = boundStage.stageName.ToUpper();
                nameText.fontSize = 32f;
            }

            if (descriptionText != null)
            {
                descriptionText.text = complete
                    ? "Restoration milestone reached."
                    : $"Invest RP to advance the city toward Stage {boundStage.stageIndex + 1}.";
                descriptionText.fontSize = 26f;
            }

            if (costText != null)
            {
                costText.text = complete
                    ? "COMPLETE"
                    : $"{NumberFormatter.Format(boundStage.pointsRequired)} RP TOTAL";
                costText.fontSize = 30f;
            }

            Color accent = complete ? CompleteColor : affordable ? AffordableColor : TooExpensiveColor;
            ApplyAccent(accent);

            if (buyButton != null)
            {
                buyButton.interactable = affordable;
            }
        }

        private void ApplyAccent(Color accent)
        {
            if (background != null)
            {
                background.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            }

            if (nameText != null) nameText.color = Color.white;
            if (descriptionText != null) descriptionText.color = Color.white;
            if (costText != null) costText.color = accent;
        }
    }
}
