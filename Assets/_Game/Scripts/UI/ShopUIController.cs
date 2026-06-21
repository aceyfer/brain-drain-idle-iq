using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Populates the shop scroll list with one UpgradeSlotUI per building template, sorted by
    /// unlock order (cheapest/earliest-unlocking first), and keeps each row's state in sync
    /// with currency and player level. Also owns the shop panel's open/closed state -- hidden
    /// by default, opened via shopButton, closed via closeButton -- since the buildings list
    /// now lives in a popup rather than the main game view.
    /// </summary>
    public sealed class ShopUIController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Layout")]
        [SerializeField] private RectTransform content;
        [SerializeField] private UpgradeSlotUI slotPrefab;

        [Header("Panel Visibility")]
        [Tooltip("The shop's outer panel/background GameObject, shown/hidden as a whole. Distinct from 'content', which is just the scrollable rows container.")]
        [SerializeField] private GameObject shopPanel;
        [Tooltip("Opens the shop. Lives outside shopPanel (e.g. in the persistent HUD), since it must be clickable while the shop is closed.")]
        [SerializeField] private Button shopButton;
        [Tooltip("Closes the shop. Lives inside shopPanel.")]
        [SerializeField] private Button closeButton;

        private const float SlideDurationSeconds = 0.3f;

        private readonly List<UpgradeSlotUI> spawnedSlots = new(8);
        private static readonly List<BuildingData> SortedTemplatesBuffer = new(8);
        private bool built;

        private RectTransform shopPanelRect;
        private Vector2 shopPanelRestingPosition;
        private bool shopPanelRestingPositionCaptured;

        private void Awake()
        {
            if (shopButton != null) shopButton.onClick.AddListener(OpenShop);
            if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

            if (shopPanel != null)
            {
                // Capture the authored resting position before hiding -- this is the anchor
                // both the slide-up-open and slide-down-close animations measure from/to.
                shopPanelRect = shopPanel.GetComponent<RectTransform>();
                if (shopPanelRect != null)
                {
                    shopPanelRestingPosition = shopPanelRect.anchoredPosition;
                    shopPanelRestingPositionCaptured = true;
                }

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

        /// <summary>Opens the shop panel, sliding it up from offscreen-below into its authored resting position.</summary>
        public void OpenShop()
        {
            if (shopPanel == null)
            {
                return;
            }

            shopPanel.SetActive(true);

            if (shopPanelRect != null && shopPanelRestingPositionCaptured)
            {
                Vector2 offscreenBelow = shopPanelRestingPosition - new Vector2(0f, shopPanelRect.rect.height);
                AnimationController.PlaySlide(shopPanelRect, offscreenBelow, shopPanelRestingPosition, SlideDurationSeconds);
            }
        }

        /// <summary>Closes the shop panel, sliding it back down offscreen, then deactivating it once the slide finishes.</summary>
        public void CloseShop()
        {
            if (shopPanel == null)
            {
                return;
            }

            if (shopPanelRect != null && shopPanelRestingPositionCaptured)
            {
                Vector2 offscreenBelow = shopPanelRestingPosition - new Vector2(0f, shopPanelRect.rect.height);
                GameObject panelToHide = shopPanel;
                AnimationController.PlaySlide(shopPanelRect, shopPanelRestingPosition, offscreenBelow, SlideDurationSeconds, () =>
                {
                    if (panelToHide != null)
                    {
                        panelToHide.SetActive(false);
                    }
                });
            }
            else
            {
                shopPanel.SetActive(false);
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
                // Display in unlock order (cheapest/earliest-unlocking first), independent of
                // however buildingTemplates happens to be ordered in the Inspector.
                SortedTemplatesBuffer.Clear();
                SortedTemplatesBuffer.AddRange(templates);
                SortedTemplatesBuffer.Sort((a, b) =>
                {
                    if (a == null) return b == null ? 0 : 1;
                    if (b == null) return -1;
                    return a.unlockCumulativeBrainPower.CompareTo(b.unlockCumulativeBrainPower);
                });

                for (int i = 0; i < SortedTemplatesBuffer.Count; i++)
                {
                    BuildingData data = SortedTemplatesBuffer[i];
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

                    // Force sibling order to match the sort, regardless of whether this slot
                    // was just instantiated (appends at the end) or reused from a prior build.
                    slot.transform.SetSiblingIndex(i);

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
