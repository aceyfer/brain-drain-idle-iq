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

            // Keep static rows, destroy only others
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (child.name == "Library" || child.name.StartsWith("UpgradeSlot_") || child.name.StartsWith("Static_"))
                {
                    continue;
                }
                Destroy(child.gameObject);
            }
            spawnedSlots.Clear();

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

                    UpgradeSlotUI slot = null;
                    // Look for existing matching slot under Content
                    for (int c = 0; c < content.childCount; c++)
                    {
                        Transform child = content.GetChild(c);
                        UpgradeSlotUI childSlot = child.GetComponent<UpgradeSlotUI>();
                        if (childSlot != null && (child.name == $"UpgradeSlot_{data.buildingName}" || (data.buildingName == "The Literal Library" && child.name == "Library")))
                        {
                            slot = childSlot;
                            slot.name = $"UpgradeSlot_{data.buildingName}";
                            break;
                        }
                    }

                    if (slot == null)
                    {
                        slot = Instantiate(slotPrefab, content);
                        slot.name = $"UpgradeSlot_{data.buildingName}";
                    }

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
                Transform child = content.GetChild(i);
                if (child.name == "Library" || child.name.StartsWith("UpgradeSlot_") || child.name.StartsWith("Static_"))
                {
                    continue;
                }
                Destroy(child.gameObject);
            }

            spawnedSlots.Clear();
        }

        private void SubscribeToEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnBrainPowerChanged -= HandleBrainPowerChanged;
                currencyManager.OnBrainPowerChanged += HandleBrainPowerChanged;
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCumulativeBrainPowerChanged;
                currencyManager.OnCumulativeBrainPowerChanged += HandleCumulativeBrainPowerChanged;
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
                currencyManager.OnBrainPowerChanged -= HandleBrainPowerChanged;
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCumulativeBrainPowerChanged;
            }

            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingsChanged -= RefreshAllSlots;
            }
        }

        private void HandleBrainPowerChanged(double _)
        {
            RefreshAllSlots();
        }

        private void HandleCumulativeBrainPowerChanged(double _)
        {
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                {
                    spawnedSlots[i].RefreshState(currencyManager);
                }
            }
        }
    }
}
