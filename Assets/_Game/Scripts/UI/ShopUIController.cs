using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.UI
{
    /// <summary>
    /// Populates the shop scroll list with one UpgradeSlotUI per building template
    /// and keeps each row's state in sync with currency and player level.
    /// </summary>
    public sealed class ShopUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private IQDecaySystem iqDecaySystem;

        [Header("Layout")]
        [SerializeField] private RectTransform content;
        [SerializeField] private UpgradeSlotUI slotPrefab;

        private readonly List<UpgradeSlotUI> spawnedSlots = new(8);
        private bool built;

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

        private void ResolveDependencies()
        {
            if (upgradeManager == null)
            {
                upgradeManager = FindAnyObjectByType<UpgradeManager>();
            }

            if (currencyManager == null)
            {
                currencyManager = CurrencyManager.Instance;
            }

            if (iqDecaySystem == null)
            {
                iqDecaySystem = FindAnyObjectByType<IQDecaySystem>();
            }
        }

        private void BuildShop()
        {
            if (built)
            {
                RefreshAllSlots();
                return;
            }

            ResolveDependencies();

            if (upgradeManager == null || content == null || slotPrefab == null)
            {
                Debug.LogWarning("[ShopUIController] Missing references; cannot build shop.", this);
                return;
            }

            ClearExistingChildren();

            IReadOnlyList<BuildingData> templates = upgradeManager.BuildingTemplates;
            if (templates != null)
            {
                for (int i = 0; i < templates.Count; i++)
                {
                    BuildingData data = templates[i];
                    if (data == null)
                    {
                        continue;
                    }

                    UpgradeSlotUI slot = Instantiate(slotPrefab, content);
                    slot.name = $"UpgradeSlot_{data.buildingName}";
                    slot.Bind(data, upgradeManager);
                    spawnedSlots.Add(slot);
                }
            }

            built = true;
            SubscribeToEvents();
            RefreshAllSlots();
        }

        private void ClearExistingChildren()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            spawnedSlots.Clear();
        }

        private void SubscribeToEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnBrainsChanged -= HandleBrainsChanged;
                currencyManager.OnBrainsChanged += HandleBrainsChanged;
            }

            if (iqDecaySystem != null)
            {
                iqDecaySystem.OnLevelChanged -= HandleLevelChanged;
                iqDecaySystem.OnLevelChanged += HandleLevelChanged;
            }

            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingsChanged -= RefreshAllSlots;
                upgradeManager.OnBuildingsChanged += RefreshAllSlots;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnBrainsChanged -= HandleBrainsChanged;
            }

            if (iqDecaySystem != null)
            {
                iqDecaySystem.OnLevelChanged -= HandleLevelChanged;
            }

            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingsChanged -= RefreshAllSlots;
            }
        }

        private void HandleBrainsChanged(double _)
        {
            RefreshAllSlots();
        }

        private void HandleLevelChanged(int _)
        {
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                {
                    spawnedSlots[i].RefreshState(currencyManager, iqDecaySystem);
                }
            }
        }
    }
}
