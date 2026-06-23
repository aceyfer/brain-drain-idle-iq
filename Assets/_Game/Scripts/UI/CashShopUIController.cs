using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Shop 2 (Cash Shop) popup -- mirrors ShopUIController's build-one-row-per-template,
    /// open/close-as-a-popup pattern. Owns two sections: a dedicated Hot Chick companion row
    /// (single evolving sprite/quote across 6 sequential tiers, driven directly by this
    /// controller rather than a CashShopSlotUI, since it's one progression, not a list) and the
    /// 5 generic one-off items (CashShopItemData, one CashShopSlotUI row each, instantiated
    /// under content like UpgradeSlotUI rows).
    ///
    /// SCENE WIRING NOT YET DONE: this script is code-complete, but no panel/button/Content
    /// hierarchy exists in SampleScene.unity yet for it to bind to -- consistent with this
    /// session's established practice of leaving new-GameObject scene construction to a
    /// dedicated Editor tool (see ShopPanelLayoutFix.cs's precedent for Shop 1) rather than
    /// hand-editing the scene YAML for an entire new popup from scratch.
    /// </summary>
    public sealed class CashShopUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CashShopManager cashShopManager;
        [SerializeField] private CompanionManager companionManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Hot Chick Companion Row")]
        [SerializeField] private TextMeshProUGUI companionQuoteText;
        [SerializeField] private TextMeshProUGUI companionCostText;
        [SerializeField] private TextMeshProUGUI companionTierText;
        [SerializeField] private Button companionBuyButton;
        [SerializeField] private Image companionBackground;

        [Header("Generic Items")]
        [SerializeField] private RectTransform content;
        [SerializeField] private CashShopSlotUI slotPrefab;

        private static readonly Color CompanionAffordableColor = new Color32(0x00, 0xF0, 0xFF, 0xFF);
        private static readonly Color CompanionLockedColor = new Color32(0x4A, 0x4E, 0x5D, 0xFF);
        private static readonly Color CompanionMaxedColor = new Color32(0x39, 0xFF, 0x14, 0xFF);

        private readonly List<CashShopSlotUI> spawnedSlots = new(8);
        private bool built;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(OpenShop);
            if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
            if (companionBuyButton != null) companionBuyButton.onClick.AddListener(HandleCompanionBuyClicked);

            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        private void Start()
        {
            ResolveDependencies();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += BuildShop;
            }

            BuildShop();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= BuildShop;
            }
        }

        public void OpenShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
                RefreshAll();
            }
        }

        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        private void ResolveDependencies()
        {
            if (cashShopManager == null) cashShopManager = CashShopManager.Instance;
            if (companionManager == null) companionManager = CompanionManager.Instance;
            if (currencyManager == null) currencyManager = CurrencyManager.Instance;
        }

        private void BuildShop()
        {
            if (built)
            {
                RefreshAll();
                return;
            }

            ResolveDependencies();

            if (cashShopManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[CashShopUIController] Missing references; cannot build Cash Shop.", this);
                return;
            }

            IReadOnlyList<CashShopItemData> items = cashShopManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                CashShopItemData data = items[i];
                if (data == null) continue;

                CashShopSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"CashShopSlot_{data.itemId}";
                slot.transform.SetSiblingIndex(i);
                slot.Bind(data, cashShopManager);
                spawnedSlots.Add(slot);
            }

            built = true;
            SubscribeToEvents();
            RefreshAll();
        }

        private void SubscribeToEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnCashChanged.RemoveListener(HandleCashChanged);
                currencyManager.OnCashChanged.AddListener(HandleCashChanged);
            }

            if (cashShopManager != null)
            {
                cashShopManager.OnItemsChanged -= RefreshAll;
                cashShopManager.OnItemsChanged += RefreshAll;
            }

            if (companionManager != null)
            {
                companionManager.OnTierChanged -= HandleCompanionTierChanged;
                companionManager.OnTierChanged += HandleCompanionTierChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnCashChanged.RemoveListener(HandleCashChanged);
            }

            if (cashShopManager != null)
            {
                cashShopManager.OnItemsChanged -= RefreshAll;
            }

            if (companionManager != null)
            {
                companionManager.OnTierChanged -= HandleCompanionTierChanged;
            }
        }

        private void HandleCashChanged(double _) => RefreshAll();
        private void HandleCompanionTierChanged(int _) => RefreshAll();

        private void HandleCompanionBuyClicked()
        {
            companionManager?.TryPurchaseNextTier();
        }

        private void RefreshAll()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                spawnedSlots[i]?.RefreshState(currencyManager);
            }

            RefreshCompanionRow();
        }

        private void RefreshCompanionRow()
        {
            if (companionManager == null)
            {
                return;
            }

            CompanionTierData next = companionManager.GetNextTier();

            if (next == null)
            {
                // All 6 tiers owned.
                if (companionQuoteText != null) companionQuoteText.text = "SHE IS THE ILLUMISNOTTI NOW.";
                if (companionCostText != null) companionCostText.text = "MAXED";
                if (companionTierText != null) companionTierText.text = $"TIER {companionManager.CurrentTier}/6";
                ApplyCompanionAccent(CompanionMaxedColor);
                if (companionBuyButton != null) companionBuyButton.interactable = false;
                return;
            }

            bool gateMet = companionManager.IsNextTierGateMet();
            if (companionTierText != null) companionTierText.text = $"TIER {companionManager.CurrentTier}/6";
            if (companionQuoteText != null) companionQuoteText.text = next.quote;

            if (!gateMet)
            {
                if (companionCostText != null) companionCostText.text = DescribeCompanionGate(next);
                ApplyCompanionAccent(CompanionLockedColor);
                if (companionBuyButton != null) companionBuyButton.interactable = false;
                return;
            }

            bool affordable = currencyManager != null && currencyManager.CanAffordCash(next.cost);
            if (companionCostText != null) companionCostText.text = $"{NumberFormatter.Format(next.cost)} CASH";
            ApplyCompanionAccent(affordable ? CompanionAffordableColor : CompanionLockedColor);
            if (companionBuyButton != null) companionBuyButton.interactable = affordable;
        }

        private static string DescribeCompanionGate(CompanionTierData tier)
        {
            switch (tier.gateType)
            {
                case CompanionGateType.RealHoursSinceFirstLaunch:
                    return $"{tier.gateRealHours:F0}H SINCE FIRST LAUNCH REQUIRED";
                case CompanionGateType.RebirthCount:
                    return $"REBIRTH {tier.gateRebirthCount} REQUIRED";
                case CompanionGateType.RebirthCountAndWorldStage:
                    return $"REBIRTH {tier.gateRebirthCount} + WORLD STAGE {tier.gateWorldStageIndex} REQUIRED";
                default:
                    return "LOCKED";
            }
        }

        private void ApplyCompanionAccent(Color accent)
        {
            if (companionBackground != null) companionBackground.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            if (companionCostText != null) companionCostText.color = accent;
        }
    }
}
