using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    public sealed class PremiumShopUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PremiumShopManager premiumShopManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Items container")]
        [SerializeField] private RectTransform content;
        [SerializeField] private PremiumShopSlotUI slotPrefab;

        private readonly List<PremiumShopSlotUI> spawnedSlots = new List<PremiumShopSlotUI>();
        private bool built;

        private void Awake()
        {
            if (openButton != null)
            {
                openButton.onClick.AddListener(OpenShop);
                openButton.gameObject.SetActive(false); // Hidden for early game demo
            }
            if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

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
            if (premiumShopManager == null) premiumShopManager = PremiumShopManager.Instance;
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

            if (premiumShopManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[PremiumShopUIController] Missing references; cannot build Premium Shop.", this);
                return;
            }

            // Clear old slots if any
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }
            spawnedSlots.Clear();

            IReadOnlyList<PremiumItemData> items = premiumShopManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PremiumItemData data = items[i];
                if (data == null) continue;

                PremiumShopSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"PremiumShopSlot_{data.itemId}";
                slot.transform.SetSiblingIndex(i);
                slot.Bind(data, premiumShopManager);
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
                currencyManager.OnNeuronsChanged -= HandleNeuronsChanged;
                currencyManager.OnNeuronsChanged += HandleNeuronsChanged;
            }

            if (premiumShopManager != null)
            {
                premiumShopManager.OnItemsChanged -= RefreshAll;
                premiumShopManager.OnItemsChanged += RefreshAll;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnNeuronsChanged -= HandleNeuronsChanged;
            }

            if (premiumShopManager != null)
            {
                premiumShopManager.OnItemsChanged -= RefreshAll;
            }
        }

        private void HandleNeuronsChanged(int _) => RefreshAll();

        private void RefreshAll()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                spawnedSlots[i]?.RefreshState(currencyManager);
            }
        }
    }
}