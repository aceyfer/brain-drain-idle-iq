using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core.Events;

namespace BrainDrain.UI
{
    /// <summary>
    /// Event-driven 3-tab shop view layer (BP / Cash / RP). Pure consumer of
    /// EventBus&lt;CurrencyChanged&gt;, EventBus&lt;ItemPurchased&gt;, and
    /// EventBus&lt;StageAdvanced&gt; — no Update polling, no economy mutation.
    /// Each tab owns a child Canvas; inactive tabs set canvas.enabled = false.
    /// </summary>
    public sealed class ShopTabView : MonoBehaviour
    {
        private const int RowPoolSize = 10;

        [Serializable]
        private sealed class TabPanel
        {
            public ShopQuery.ShopTab tab;
            public Canvas tabCanvas;
            public ScrollRect scrollRect;
            public RectTransform content;
            public Button tabButton;
        }

        [Header("Tabs")]
        [SerializeField] private TabPanel[] tabPanels;

        [Header("List")]
        [SerializeField] private ShopRowView rowPrefab;
        [SerializeField] private float rowHeight = 180f;

        private readonly List<VirtualizedTabList> virtualizedLists = new(3);
        private ShopQuery.ShopTab activeTab = ShopQuery.ShopTab.BpUpgrades;
        private bool subscribed;

        private void Awake()
        {
            BuildVirtualizedLists();
            WireTabButtons();
        }

        private void OnEnable()
        {
            SubscribeToShopEvents();
            SelectTab(activeTab);
            RefreshAllTabs();
        }

        private void OnDisable()
        {
            UnsubscribeFromShopEvents();
        }

        public void SelectTab(ShopQuery.ShopTab tab)
        {
            activeTab = tab;

            for (int i = 0; i < tabPanels.Length; i++)
            {
                TabPanel panel = tabPanels[i];
                if (panel == null)
                {
                    continue;
                }

                bool isActive = panel.tab == tab;
                if (panel.tabCanvas != null)
                {
                    panel.tabCanvas.enabled = isActive;
                }

                SetTabButtonHighlight(panel.tabButton, isActive);
            }

            VirtualizedTabList list = GetList(tab);
            list?.Refresh();
        }

        private void SubscribeToShopEvents()
        {
            if (subscribed)
            {
                return;
            }

            EventBus<CurrencyChanged>.Subscribe(HandleCurrencyChanged);
            EventBus<ItemPurchased>.Subscribe(HandleItemPurchased);
            EventBus<StageAdvanced>.Subscribe(HandleStageAdvanced);
            subscribed = true;
        }

        private void UnsubscribeFromShopEvents()
        {
            if (!subscribed)
            {
                return;
            }

            EventBus<CurrencyChanged>.Unsubscribe(HandleCurrencyChanged);
            EventBus<ItemPurchased>.Unsubscribe(HandleItemPurchased);
            EventBus<StageAdvanced>.Unsubscribe(HandleStageAdvanced);
            subscribed = false;
        }

        private void HandleCurrencyChanged(CurrencyChanged _) => RefreshAllTabs();
        private void HandleItemPurchased(ItemPurchased _) => RefreshAllTabs();
        private void HandleStageAdvanced(StageAdvanced _) => RefreshAllTabs();

        private void RefreshAllTabs()
        {
            for (int i = 0; i < virtualizedLists.Count; i++)
            {
                virtualizedLists[i].Refresh();
            }
        }

        private void BuildVirtualizedLists()
        {
            virtualizedLists.Clear();

            if (tabPanels == null)
            {
                return;
            }

            for (int i = 0; i < tabPanels.Length; i++)
            {
                TabPanel panel = tabPanels[i];
                if (panel?.content == null || rowPrefab == null)
                {
                    continue;
                }

                DisableLayoutComponents(panel.content);
                if (panel.content.parent != null)
                {
                    DisableLayoutComponents(panel.content.parent);
                }

                VirtualizedTabList list = new VirtualizedTabList(
                    panel.tab,
                    panel.scrollRect,
                    panel.content,
                    rowPrefab,
                    rowHeight,
                    RowPoolSize,
                    StubPurchase);
                list.Initialize();
                virtualizedLists.Add(list);

                if (panel.scrollRect != null)
                {
                    panel.scrollRect.onValueChanged.RemoveListener(list.HandleScrollChanged);
                    panel.scrollRect.onValueChanged.AddListener(list.HandleScrollChanged);
                }
            }
        }

        private void WireTabButtons()
        {
            if (tabPanels == null)
            {
                return;
            }

            for (int i = 0; i < tabPanels.Length; i++)
            {
                TabPanel panel = tabPanels[i];
                if (panel?.tabButton == null)
                {
                    continue;
                }

                ShopQuery.ShopTab capturedTab = panel.tab;
                panel.tabButton.onClick.RemoveAllListeners();
                panel.tabButton.onClick.AddListener(() => SelectTab(capturedTab));
            }
        }

        private VirtualizedTabList GetList(ShopQuery.ShopTab tab)
        {
            for (int i = 0; i < virtualizedLists.Count; i++)
            {
                if (virtualizedLists[i].Tab == tab)
                {
                    return virtualizedLists[i];
                }
            }

            return null;
        }

        private static void SetTabButtonHighlight(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic is Image image)
            {
                image.color = selected
                    ? new Color(0f, 0.94f, 1f, 1f)
                    : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
        }

        private static void DisableLayoutComponents(Transform target)
        {
            if (target == null)
            {
                return;
            }

            VerticalLayoutGroup vertical = target.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.enabled = false;
            }

            HorizontalLayoutGroup horizontal = target.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
            {
                horizontal.enabled = false;
            }

            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }
        }

        /// <summary>
        /// Stub until EconomyManager purchase routing is implemented.
        /// </summary>
        private static void StubPurchase(string itemId)
        {
            Debug.Log($"[ShopTabView] Purchase stub invoked for '{itemId}'. Wire to EconomyManager.TryPurchase when ready.");
        }

        private sealed class VirtualizedTabList
        {
            private readonly ShopQuery.ShopTab tab;
            private readonly ScrollRect scrollRect;
            private readonly RectTransform content;
            private readonly ShopRowView rowPrefab;
            private readonly float rowHeight;
            private readonly int poolSize;
            private readonly Action<string> purchaseStub;

            private readonly ShopRowView[] pool;
            private IReadOnlyList<ShopQuery.ShopRowSnapshot> rows = Array.Empty<ShopQuery.ShopRowSnapshot>();

            public ShopQuery.ShopTab Tab => tab;

            public VirtualizedTabList(
                ShopQuery.ShopTab tab,
                ScrollRect scrollRect,
                RectTransform content,
                ShopRowView rowPrefab,
                float rowHeight,
                int poolSize,
                Action<string> purchaseStub)
            {
                this.tab = tab;
                this.scrollRect = scrollRect;
                this.content = content;
                this.rowPrefab = rowPrefab;
                this.rowHeight = rowHeight;
                this.poolSize = poolSize;
                this.purchaseStub = purchaseStub;
                pool = new ShopRowView[poolSize];
            }

            public void Initialize()
            {
                ConfigureContentRoot();

                for (int i = 0; i < poolSize; i++)
                {
                    ShopRowView row = UnityEngine.Object.Instantiate(rowPrefab, content);
                    row.name = $"ShopRow_{tab}_{i}";
                    row.ConfigurePurchaseStub(purchaseStub);
                    ConfigureRowRect(row.GetComponent<RectTransform>());
                    pool[i] = row;
                }
            }

            public void Refresh()
            {
                rows = ShopQuery.GetRows(tab);
                UpdateContentHeight();
                RebindVisibleRows();
            }

            public void HandleScrollChanged(Vector2 _)
            {
                RebindVisibleRows();
            }

            private void ConfigureContentRoot()
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
            }

            private static void ConfigureRowRect(RectTransform rect)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }

            private void UpdateContentHeight()
            {
                float height = Mathf.Max(rowHeight, rows.Count * rowHeight);
                content.sizeDelta = new Vector2(0f, height);
            }

            private void RebindVisibleRows()
            {
                if (rows.Count == 0)
                {
                    for (int i = 0; i < poolSize; i++)
                    {
                        pool[i].gameObject.SetActive(false);
                    }

                    return;
                }

                int firstVisible = ResolveFirstVisibleIndex();
                int lastVisible = Mathf.Min(rows.Count - 1, firstVisible + poolSize - 1);
                firstVisible = Mathf.Max(0, lastVisible - poolSize + 1);

                for (int poolIndex = 0; poolIndex < poolSize; poolIndex++)
                {
                    int dataIndex = firstVisible + poolIndex;
                    ShopRowView row = pool[poolIndex];

                    if (dataIndex >= rows.Count)
                    {
                        row.gameObject.SetActive(false);
                        continue;
                    }

                    row.gameObject.SetActive(true);
                    PositionRow(row.GetComponent<RectTransform>(), dataIndex);
                    row.Bind(rows[dataIndex]);
                }
            }

            private int ResolveFirstVisibleIndex()
            {
                if (scrollRect == null || rows.Count <= poolSize)
                {
                    return 0;
                }

                float scrollOffset = Mathf.Max(0f, content.anchoredPosition.y);
                int index = Mathf.FloorToInt(scrollOffset / rowHeight);
                return Mathf.Clamp(index, 0, Mathf.Max(0, rows.Count - poolSize));
            }

            private void PositionRow(RectTransform rect, int dataIndex)
            {
                rect.anchoredPosition = new Vector2(0f, -dataIndex * rowHeight);
                rect.sizeDelta = new Vector2(0f, rowHeight);
            }
        }
    }
}
