using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// God Tier Store popup -- mirrors ShopUIController's build-one-row-per-template,
    /// open/close-as-a-popup pattern. Real-money items; see GodTierStoreManager's class doc for
    /// the stubbed-purchase caveat (no real IAP plugin is wired up in this project).
    ///
    /// SCENE WIRING NOT YET DONE: code-complete, but no panel/button/Content hierarchy exists in
    /// SampleScene.unity yet -- see CashShopUIController's identical note.
    /// </summary>
    public sealed class GodTierStoreUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GodTierStoreManager godTierStoreManager;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Items")]
        [SerializeField] private RectTransform content;
        [SerializeField] private GodTierStoreSlotUI slotPrefab;

        private readonly List<GodTierStoreSlotUI> spawnedSlots = new(8);
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
            if (godTierStoreManager == null)
            {
                godTierStoreManager = GodTierStoreManager.Instance;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += BuildStore;
            }

            BuildStore();
        }

        private void OnDestroy()
        {
            if (godTierStoreManager != null)
            {
                godTierStoreManager.OnItemsChanged -= RefreshAllSlots;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= BuildStore;
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

        private void BuildStore()
        {
            if (built)
            {
                RefreshAllSlots();
                return;
            }

            if (godTierStoreManager == null)
            {
                godTierStoreManager = GodTierStoreManager.Instance;
            }

            if (godTierStoreManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[GodTierStoreUIController] Missing references; cannot build God Tier Store.", this);
                return;
            }

            IReadOnlyList<GodTierStoreItemData> items = godTierStoreManager.Items;
            for (int i = 0; i < items.Count; i++)
            {
                GodTierStoreItemData data = items[i];
                if (data == null) continue;

                GodTierStoreSlotUI slot = Instantiate(slotPrefab, content);
                slot.name = $"GodTierStoreSlot_{data.itemId}";
                slot.transform.SetSiblingIndex(i);
                slot.Bind(data, godTierStoreManager);
                spawnedSlots.Add(slot);
            }

            built = true;
            godTierStoreManager.OnItemsChanged -= RefreshAllSlots;
            godTierStoreManager.OnItemsChanged += RefreshAllSlots;
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                spawnedSlots[i]?.RefreshState();
            }
        }
    }
}
