using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Shop 3 (Points Power Shop) popup -- mirrors ShopUIController's build-one-row-per-template,
    /// open/close-as-a-popup pattern.
    ///
    /// SCENE WIRING NOT YET DONE: code-complete, but no panel/button/Content hierarchy exists in
    /// SampleScene.unity yet -- see CashShopUIController's identical note.
    /// </summary>
    public sealed class PointsShopUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PointsShopManager pointsShopManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Items")]
        [SerializeField] private RectTransform content;
        [SerializeField] private PointsShopSlotUI slotPrefab;

        private readonly List<PointsShopSlotUI> spawnedSlots = new(8);
        private bool built;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(OpenShop);
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
                RefreshAllSlots();
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
            if (pointsShopManager == null) pointsShopManager = PointsShopManager.Instance;
            if (currencyManager == null) currencyManager = CurrencyManager.Instance;
        }

        private void BuildShop()
        {
            if (built)
            {
                RefreshAllSlots();
                return;
            }

            ResolveDependencies();

            if (pointsShopManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[PointsShopUIController] Missing references; cannot build Points Shop.", this);
                return;
            }

            IReadOnlyList<PointsShopItemData> items = pointsShopManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PointsShopItemData data = items[i];
                if (data == null) continue;

                PointsShopSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"PointsShopSlot_{data.itemId}";
                slot.transform.SetSiblingIndex(i);
                slot.Bind(data, pointsShopManager);
                spawnedSlots.Add(slot);
            }

            built = true;
            SubscribeToEvents();
            RefreshAllSlots();
        }

        private void SubscribeToEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
                currencyManager.OnPointsChanged.AddListener(HandlePointsChanged);
            }

            if (pointsShopManager != null)
            {
                pointsShopManager.OnItemsChanged -= RefreshAllSlots;
                pointsShopManager.OnItemsChanged += RefreshAllSlots;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
            }

            if (pointsShopManager != null)
            {
                pointsShopManager.OnItemsChanged -= RefreshAllSlots;
            }
        }

        private void HandlePointsChanged(double _) => RefreshAllSlots();

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                spawnedSlots[i]?.RefreshState(currencyManager);
            }
        }
    }
}
