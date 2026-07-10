using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Three-tab shop overlay: BP Upgrades, Cash Investments, and the God Shop (direct
    /// real-currency store backed by GodTierStoreManager; TASKLIST_DETAILS §10/§16).
    /// Each tab owns its own scroll content list; only one tab panel is visible at a time.
    /// </summary>
    public sealed class ShopUIController : MonoBehaviour
    {
        public enum ShopTab
        {
            BpUpgrades = 0,
            CashInvestments = 1,
            GodShop = 2
        }

        [Header("Dependencies")]
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Tab Navigation")]
        [SerializeField] private Button bpTabButton;
        [SerializeField] private Button cashTabButton;
        [SerializeField] private Button rpTabButton;
        [SerializeField] private GameObject bpTabPanel;
        [SerializeField] private GameObject cashTabPanel;
        [SerializeField] private GameObject rpTabPanel;

        [Header("Tab Content")]
        [FormerlySerializedAs("content")]
        [SerializeField] private RectTransform bpContent;
        [SerializeField] private RectTransform cashContent;
        // NOTE: the rp* serialized field names (rpTabButton/rpTabPanel/rpContent) are kept
        // even though the third tab is now the God Shop -- renaming serialized fields breaks
        // scene wiring, and scene saves are under a smuggling embargo (PROJECT_BIBLE.md §8).
        [SerializeField] private RectTransform rpContent;
        [SerializeField] private UpgradeSlotUI slotPrefab;

        [Header("Panel Visibility")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button closeButton;

        private const float SlideDurationSeconds = 0.3f;

        // Draw/raycast layering, owned in code rather than sibling order (see
        // PROJECT_BIBLE.md §8) -- tab content sorts above shopPanel's backdrop and
        // MainTapButton; closeButton sorts above the tab content in turn, so it stays
        // reachable regardless of whether its screen rect happens to overlap the
        // scroll area. shopPanel's own background stays at the root canvas's implicit 0.
        private const int TabContentSortingOrder = 1;
        private const int ShopChromeSortingOrder = 2;

        private readonly List<UpgradeSlotUI> bpSlots = new(8);
        private readonly List<UpgradeSlotUI> cashSlots = new(8);
        private readonly List<GodTierStoreSlotUI> godShopSlots = new(8);
        private static readonly List<BuildingData> SortedTemplatesBuffer = new(8);

        private bool built;
        private ShopTab activeTab = ShopTab.BpUpgrades;
        private GodTierStoreSlotUI runtimeGodShopSlotTemplate;

        private RectTransform shopPanelRect;
        private Vector2 shopPanelRestingPosition;
        private bool shopPanelRestingPositionCaptured;

        // ShopRoot hosts Tab_BP/Tab_Cash/Tab_RP/ShopTabBar as a sibling branch of shopPanel, not
        // a child of it -- shopPanel.SetActive() never reaches it. Resolved automatically from
        // bpTabPanel's own parent rather than a new [SerializeField] so there's no Inspector
        // wiring step to forget (see the ShopPanel Awake() trap precedent in PROJECT_BIBLE.md §8).
        private GameObject shopRoot;

        public event Action ShopClosed;

        private void Awake()
        {
            ResolveDependencies();
            EnsureThreeTabLayout();
            WireTabButtons();

            // Owns each tab panel's on-screen bounds in code, matching shopPanel exactly,
            // regardless of whatever anchors got saved in the scene. Two scene-edit reports
            // this session did not persist as expected (see PROJECT_BIBLE.md §8) -- geometry-
            // critical values now live here, not the .unity file. Must run before BuildShop()
            // so the freshly-instantiated rows' layout computes against final bounds.
            NormalizeTabGeometry();
            NormalizeDrawOrder();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            // Build content while the panel is still active so OpenShop() → RefreshAllSlots()
            // finds a populated slot list even before Start() has run.
            BuildShop();

            // Explicitly own each tab panel's Canvas/GraphicRaycaster state at boot rather than
            // trusting whatever got saved in the scene. Post-collapse, ShopUIController is the
            // only system responsible for this — previously ShopTabView's own SelectTab() call
            // (via its now-removed guard) was silently covering this gap.
            InitializeTabPresentation();

            if (shopPanel != null)
            {
                if (shopPanelRect != null)
                {
                    shopPanelRestingPosition = shopPanelRect.anchoredPosition;
                    shopPanelRestingPositionCaptured = true;
                }

                shopPanel.SetActive(false);
            }

            // ShopRoot (Tab_BP/Cash/RP + ShopTabBar) is a sibling of shopPanel, not a descendant
            // of it -- deactivate it too, or its currently-selected tab's Canvas/GraphicRaycaster
            // keeps rendering and receiving taps at all times, shop open or not.
            if (shopRoot != null)
            {
                shopRoot.SetActive(false);
            }
        }

        private void WireTabButtons()
        {
            if (bpTabButton != null)
            {
                bpTabButton.onClick.RemoveListener(OnBpTabClicked);
                bpTabButton.onClick.AddListener(OnBpTabClicked);
            }

            if (cashTabButton != null)
            {
                cashTabButton.onClick.RemoveListener(OnCashTabClicked);
                cashTabButton.onClick.AddListener(OnCashTabClicked);
            }

            if (rpTabButton != null)
            {
                rpTabButton.onClick.RemoveListener(OnRpTabClicked);
                rpTabButton.onClick.AddListener(OnRpTabClicked);
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
            SelectTab(ShopTab.BpUpgrades);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= BuildShop;
            }
        }

        public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

        public void ToggleShop()
        {
            if (IsOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }

        public void OpenShop()
        {
            if (shopPanel == null)
            {
                return;
            }

            shopPanel.SetActive(true);
            if (shopRoot != null)
            {
                shopRoot.SetActive(true);
            }
            SelectTab(activeTab);
            RefreshAllSlots();

            if (shopPanelRect != null && shopPanelRestingPositionCaptured)
            {
                Vector2 offscreenAbove = shopPanelRestingPosition + new Vector2(0f, shopPanelRect.rect.height);
                AnimationController.PlaySlide(shopPanelRect, offscreenAbove, shopPanelRestingPosition, SlideDurationSeconds);
            }
        }

        public void CloseShop()
        {
            if (shopPanel == null)
            {
                return;
            }

            if (shopPanelRect != null && shopPanelRestingPositionCaptured)
            {
                Vector2 offscreenAbove = shopPanelRestingPosition + new Vector2(0f, shopPanelRect.rect.height);
                GameObject panelToHide = shopPanel;
                GameObject rootToHide = shopRoot;
                AnimationController.PlaySlide(shopPanelRect, shopPanelRestingPosition, offscreenAbove, SlideDurationSeconds, () =>
                {
                    if (panelToHide != null)
                    {
                        panelToHide.SetActive(false);
                    }

                    if (rootToHide != null)
                    {
                        rootToHide.SetActive(false);
                    }

                    ShopClosed?.Invoke();
                });
            }
            else
            {
                shopPanel.SetActive(false);
                if (shopRoot != null)
                {
                    shopRoot.SetActive(false);
                }
                ShopClosed?.Invoke();
            }
        }

        public void SelectTab(ShopTab tab)
        {
            activeTab = tab;

            if (tab == ShopTab.GodShop && godShopSlots.Count == 0)
            {
                // GodTierStoreManager.Instance may have been null at BuildShop() time -- the
                // same cross-object Awake-order race the old RP tab hit (see 5572122). Retry
                // on selection; harmless no-op if the manager still isn't up.
                BuildGodShopTab();
            }

            if (bpTabPanel != null) bpTabPanel.SetActive(tab == ShopTab.BpUpgrades);
            if (cashTabPanel != null) cashTabPanel.SetActive(tab == ShopTab.CashInvestments);
            if (rpTabPanel != null) rpTabPanel.SetActive(tab == ShopTab.GodShop);

            // Each tab panel owns its own nested Canvas + GraphicRaycaster. GameObject.SetActive
            // alone doesn't flip either, and these panels are saved with Cash/RP's Canvas off by
            // default, so without this they never render/raycast. ShopUIController now owns this
            // state exclusively (see the "single shop system" note in PROJECT_BIBLE.md §8) —
            // there is no second system covering this gap anymore.
            SetTabPanelPresentation(bpTabPanel, tab == ShopTab.BpUpgrades);
            SetTabPanelPresentation(cashTabPanel, tab == ShopTab.CashInvestments);
            SetTabPanelPresentation(rpTabPanel, tab == ShopTab.GodShop);

            // Reasserted on every selection, same philosophy as SetTabPanelPresentation
            // above: label text is code-owned state; the scene's saved TMP text is not
            // trusted (PROJECT_BIBLE.md §8).
            AssertTabButtonLabels();

            SetTabButtonHighlight(bpTabButton, tab == ShopTab.BpUpgrades);
            SetTabButtonHighlight(cashTabButton, tab == ShopTab.CashInvestments);
            SetTabButtonHighlight(rpTabButton, tab == ShopTab.GodShop);
        }

        private void AssertTabButtonLabels()
        {
            SetTabButtonLabel(bpTabButton, "BP UPGRADES");
            SetTabButtonLabel(cashTabButton, "CASH INVESTMENTS");
            SetTabButtonLabel(rpTabButton, "GOD SHOP");
        }

        private const string CodeOwnedTabLabelName = "CodeOwnedTabLabel";

        private static void SetTabButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            // Code owns the ENTIRE tab-button face, not just one text field. The scene's
            // Tab_RP button carried RP-era leftovers (progress readout, convert icon) that
            // stacked into unreadable clutter once the label changed; instead of chasing
            // whichever children a saved scene happens to have, disable them all and render
            // a single code-owned label. Idempotent; applied uniformly to all three tabs so
            // they stay visually consistent. (PROJECT_BIBLE.md §8 -- assert state ownership
            // in code at the point of use; scene saves are embargoed anyway.)
            Transform buttonTransform = button.transform;
            for (int i = 0; i < buttonTransform.childCount; i++)
            {
                Transform child = buttonTransform.GetChild(i);
                if (child.name != CodeOwnedTabLabelName && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }

            Transform existing = buttonTransform.Find(CodeOwnedTabLabelName);
            TMPro.TextMeshProUGUI tmp;
            if (existing == null)
            {
                GameObject textGo = new GameObject(
                    CodeOwnedTabLabelName, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                textGo.transform.SetParent(buttonTransform, false);

                RectTransform textRect = (RectTransform)textGo.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10f;
                tmp.fontSizeMax = 20f;
            }
            else
            {
                tmp = existing.GetComponent<TMPro.TextMeshProUGUI>();
                if (!existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(true);
                }
            }

            if (tmp != null && tmp.text != label)
            {
                tmp.text = label;
            }
        }

        private void InitializeTabPresentation()
        {
            SetTabPanelPresentation(bpTabPanel, activeTab == ShopTab.BpUpgrades);
            SetTabPanelPresentation(cashTabPanel, activeTab == ShopTab.CashInvestments);
            SetTabPanelPresentation(rpTabPanel, activeTab == ShopTab.GodShop);
        }

        private static void SetTabPanelPresentation(GameObject panel, bool isVisible)
        {
            if (panel == null)
            {
                return;
            }

            Canvas canvas = panel.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = isVisible;

                // Reassert every call, not just once in Awake -- a canvas left disabled
                // since boot (Cash/RP, until first selected) didn't carry Awake's override
                // through to its first real enable. Idempotent, two field writes, closes
                // the gap regardless of the exact Unity-internal cause.
                canvas.overrideSorting = true;
                canvas.sortingOrder = TabContentSortingOrder;
            }

            GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = isVisible;
            }
        }

        private static void SetTabButtonHighlight(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0f, 0.94f, 1f, 1f)
                    : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
        }

        private void OnBpTabClicked() => SelectTab(ShopTab.BpUpgrades);
        private void OnCashTabClicked() => SelectTab(ShopTab.CashInvestments);
        private void OnRpTabClicked() => SelectTab(ShopTab.GodShop);

        private void EnsureThreeTabLayout()
        {
            if (bpTabPanel != null && cashTabPanel != null && rpTabPanel != null
                && bpContent != null && cashContent != null && rpContent != null)
            {
                return;
            }

            Transform panelRoot = shopPanel != null ? shopPanel.transform : transform;

            ScrollRect sourceScroll = panelRoot.GetComponentInChildren<ScrollRect>(true);
            if (sourceScroll == null)
            {
                Debug.LogWarning("[ShopUIController] Cannot bootstrap tabs: no ScrollRect found.", this);
                return;
            }

            if (bpContent == null)
            {
                bpContent = sourceScroll.content;
            }

            Transform tabBar = panelRoot.Find("ShopTabBar");
            if (tabBar == null)
            {
                tabBar = CreateRuntimeTabBar(panelRoot);
            }

            if (bpTabButton == null) bpTabButton = tabBar.Find("BpTabButton")?.GetComponent<Button>();
            if (cashTabButton == null) cashTabButton = tabBar.Find("CashTabButton")?.GetComponent<Button>();
            if (rpTabButton == null) rpTabButton = tabBar.Find("RpTabButton")?.GetComponent<Button>();

            if (bpTabPanel == null)
            {
                bpTabPanel = EnsureRuntimeTabPanel(panelRoot, "BpUpgradesPanel", sourceScroll.transform, out _);
            }

            if (cashTabPanel == null || cashContent == null)
            {
                cashTabPanel = EnsureRuntimeClonedPanel(
                    panelRoot,
                    sourceScroll.gameObject,
                    "CashInvestmentsPanel",
                    "CashInvestmentsScrollView",
                    out cashContent);
            }

            if (rpTabPanel == null || rpContent == null)
            {
                rpTabPanel = EnsureRuntimeClonedPanel(
                    panelRoot,
                    sourceScroll.gameObject,
                    "GodShopPanel",
                    "GodShopScrollView",
                    out rpContent);
            }
        }

        private static Transform CreateRuntimeTabBar(Transform panelRoot)
        {
            GameObject tabBarGo = new GameObject("ShopTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBarGo.transform.SetParent(panelRoot, false);

            RectTransform tabBarRect = tabBarGo.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.sizeDelta = new Vector2(0f, 56f);
            tabBarRect.anchoredPosition = new Vector2(0f, -3f);

            HorizontalLayoutGroup layout = tabBarGo.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateRuntimeTabButton(tabBarGo.transform, "BpTabButton", "BP UPGRADES");
            CreateRuntimeTabButton(tabBarGo.transform, "CashTabButton", "CASH INVESTMENTS");
            CreateRuntimeTabButton(tabBarGo.transform, "RpTabButton", "GOD SHOP");
            tabBarGo.transform.SetSiblingIndex(1);
            return tabBarGo.transform;
        }

        private static void CreateRuntimeTabButton(Transform parent, string name, string label)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.35f, 0.35f, 0.4f, 1f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textGo.transform.SetParent(buttonGo.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private static GameObject EnsureRuntimeTabPanel(
            Transform panelRoot,
            string panelName,
            Transform scrollTransform,
            out RectTransform content)
        {
            Transform panel = panelRoot.Find(panelName);
            if (panel == null)
            {
                GameObject panelGo = new GameObject(panelName, typeof(RectTransform));
                panelGo.transform.SetParent(panelRoot, false);
                panel = panelGo.transform;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = new Vector2(0f, -56f);

            scrollTransform.SetParent(panel, false);
            StretchRect(scrollTransform as RectTransform);

            ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
            content = scroll != null ? scroll.content : null;
            return panel.gameObject;
        }

        private static GameObject EnsureRuntimeClonedPanel(
            Transform panelRoot,
            GameObject sourceScroll,
            string panelName,
            string scrollName,
            out RectTransform content)
        {
            Transform panel = panelRoot.Find(panelName);
            GameObject scrollGo;

            if (panel == null)
            {
                GameObject panelGo = new GameObject(panelName, typeof(RectTransform));
                panelGo.transform.SetParent(panelRoot, false);
                panel = panelGo.transform;

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = new Vector2(0f, -56f);

                scrollGo = Instantiate(sourceScroll, panel);
                scrollGo.name = scrollName;
            }
            else
            {
                Transform existingScroll = panel.Find(scrollName);
                scrollGo = existingScroll != null ? existingScroll.gameObject : Instantiate(sourceScroll, panel);
                scrollGo.name = scrollName;
            }

            StretchRect(scrollGo.GetComponent<RectTransform>());

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            content = scroll != null ? scroll.content : null;
            if (content != null)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    Destroy(content.GetChild(i).gameObject);
                }
            }

            return panel.gameObject;
        }

        private GodTierStoreSlotUI CreateRuntimeGodShopSlotTemplate(UpgradeSlotUI upgradePrefab)
        {
            GameObject instance = Instantiate(upgradePrefab.gameObject, transform);
            instance.name = "GodShopSlotTemplate";
            instance.SetActive(false);

            // Capture the CLONE's own references before destroying its UpgradeSlotUI --
            // Instantiate remaps internal refs onto the clone's children. (The retired
            // restoration template read them off the source prefab instead, which only
            // worked when slotPrefab was a scene object; do not copy that pattern.)
            UpgradeSlotUI source = instance.GetComponent<UpgradeSlotUI>();
            TMPro.TextMeshProUGUI nameLabel = source.NameText;
            TMPro.TextMeshProUGUI descriptionLabel = source.DescriptionText;
            TMPro.TextMeshProUGUI priceLabel = source.CostText;
            Button buy = source.BuyButton;
            Image backgroundImage = source.Background;
            Destroy(source);

            GodTierStoreSlotUI slot = instance.AddComponent<GodTierStoreSlotUI>();
            slot.AssignRuntimeReferences(nameLabel, descriptionLabel, priceLabel, buy, backgroundImage);
            return slot;
        }

        private static void StretchRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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

            if (shopRoot == null && bpTabPanel != null && bpTabPanel.transform.parent != null)
            {
                shopRoot = bpTabPanel.transform.parent.gameObject;
            }

            if (shopPanelRect == null && shopPanel != null)
            {
                shopPanelRect = shopPanel.GetComponent<RectTransform>();
            }
        }

        private void NormalizeTabGeometry()
        {
            if (shopPanelRect == null)
            {
                return;
            }

            NormalizeTabPanelRect(bpTabPanel, shopPanelRect);
            NormalizeTabPanelRect(cashTabPanel, shopPanelRect);
            NormalizeTabPanelRect(rpTabPanel, shopPanelRect);
        }

        private static void NormalizeTabPanelRect(GameObject tabPanel, RectTransform referenceRect)
        {
            RectTransform tabRect = tabPanel != null ? tabPanel.GetComponent<RectTransform>() : null;
            if (tabRect == null)
            {
                return;
            }

            tabRect.anchorMin = referenceRect.anchorMin;
            tabRect.anchorMax = referenceRect.anchorMax;
            tabRect.offsetMin = Vector2.zero;
            tabRect.offsetMax = Vector2.zero;

            ScrollRect scrollRect = tabPanel.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null)
            {
                StretchRect(scrollRect.GetComponent<RectTransform>());
            }
        }

        private void NormalizeDrawOrder()
        {
            // Tab content's sortingOrder is asserted every call in SetTabPanelPresentation
            // (Awake's InitializeTabPresentation + every SelectTab call), not just once here
            // -- see that method for why.

            // closeButton has no Canvas of its own today -- it's raycasted only via the root
            // canvas. Give it one so it can sort above the tab content regardless of whether
            // its corner happens to overlap the scroll area (it does, post-geometry-fix).
            // shopPanel's own background Image stays raycastTarget=true and at the root
            // canvas's implicit order 0 -- deliberate catch-all for any gap in the scroll
            // content (row spacing, area below the last row) so nothing falls through to
            // MainTapButton, per explicit instruction.
            if (closeButton != null)
            {
                SetOverrideSorting(closeButton.gameObject, ShopChromeSortingOrder, ensureRaycaster: true);
            }
        }

        private static void SetOverrideSorting(GameObject target, int sortingOrder, bool ensureRaycaster = false)
        {
            if (target == null)
            {
                return;
            }

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = target.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (ensureRaycaster && target.GetComponent<GraphicRaycaster>() == null)
            {
                target.AddComponent<GraphicRaycaster>();
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

            if (upgradeManager == null || bpContent == null || slotPrefab == null)
            {
                Debug.LogWarning("[ShopUIController] Missing references; cannot build shop.", this);
                return;
            }

            ClearContentChildren(bpContent);
            ClearContentChildren(cashContent);
            ClearContentChildren(rpContent);
            bpSlots.Clear();
            cashSlots.Clear();
            godShopSlots.Clear();

            BuildBuildingTabs();
            BuildGodShopTab();

            built = true;
            SubscribeToEvents();
            RefreshAllSlots();
        }

        private void BuildBuildingTabs()
        {
            IReadOnlyList<BuildingData> templates = upgradeManager.BuildingTemplates;
            if (templates == null)
            {
                return;
            }

            SortedTemplatesBuffer.Clear();
            SortedTemplatesBuffer.AddRange(templates);
            SortedTemplatesBuffer.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                return a.unlockCumulativeBrainPower.CompareTo(b.unlockCumulativeBrainPower);
            });

            int bpIndex = 0;
            int cashIndex = 0;

            for (int i = 0; i < SortedTemplatesBuffer.Count; i++)
            {
                BuildingData data = SortedTemplatesBuffer[i];
                if (data == null)
                {
                    continue;
                }

                bool isCash = UpgradeManager.IsCashCost(data);
                RectTransform targetContent = isCash ? cashContent : bpContent;
                if (targetContent == null)
                {
                    continue;
                }

                UpgradeSlotUI slot = Instantiate(slotPrefab, targetContent);
                slot.name = $"UpgradeSlot_{data.buildingName}";
                slot.Bind(data, upgradeManager);

                if (isCash)
                {
                    slot.transform.SetSiblingIndex(cashIndex++);
                    cashSlots.Add(slot);
                }
                else
                {
                    slot.transform.SetSiblingIndex(bpIndex++);
                    bpSlots.Add(slot);
                }
            }
        }

        private void BuildGodShopTab()
        {
            if (rpContent == null)
            {
                return;
            }

            GodTierStoreManager store = GodTierStoreManager.Instance;
            if (store == null)
            {
                return;
            }

            if (runtimeGodShopSlotTemplate == null)
            {
                if (slotPrefab == null)
                {
                    return;
                }

                runtimeGodShopSlotTemplate = CreateRuntimeGodShopSlotTemplate(slotPrefab);
            }

            ClearContentChildren(rpContent);
            godShopSlots.Clear();

            IReadOnlyList<GodTierStoreItemData> items = store.Items;
            for (int i = 0; i < items.Count; i++)
            {
                GodTierStoreItemData item = items[i];
                if (item == null)
                {
                    continue;
                }

                GodTierStoreSlotUI slot = Instantiate(runtimeGodShopSlotTemplate, rpContent);
                slot.name = $"GodShopSlot_{item.itemId}";
                slot.gameObject.SetActive(true);
                slot.Bind(item, store);
                slot.RefreshState();
                godShopSlots.Add(slot);
            }
        }

        private static void ClearContentChildren(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private void SubscribeToEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnBrainPowerChanged -= HandleCurrencyChanged;
                currencyManager.OnBrainPowerChanged += HandleCurrencyChanged;
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCurrencyChanged;
                currencyManager.OnCumulativeBrainPowerChanged += HandleCurrencyChanged;
                currencyManager.OnCashChanged.RemoveListener(HandleCashChanged);
                currencyManager.OnCashChanged.AddListener(HandleCashChanged);
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
                currencyManager.OnPointsChanged.AddListener(HandlePointsChanged);
            }

            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingsChanged -= RefreshAllSlots;
                upgradeManager.OnBuildingsChanged += RefreshAllSlots;
            }

            GodTierStoreManager store = GodTierStoreManager.Instance;
            if (store != null)
            {
                store.OnItemsChanged -= HandleGodShopItemsChanged;
                store.OnItemsChanged += HandleGodShopItemsChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (currencyManager != null)
            {
                currencyManager.OnBrainPowerChanged -= HandleCurrencyChanged;
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCurrencyChanged;
                currencyManager.OnCashChanged.RemoveListener(HandleCashChanged);
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
            }

            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingsChanged -= RefreshAllSlots;
            }

            if (GodTierStoreManager.Instance != null)
            {
                GodTierStoreManager.Instance.OnItemsChanged -= HandleGodShopItemsChanged;
            }
        }

        private void HandleCurrencyChanged(double _) => RefreshAllSlots();
        private void HandleCashChanged(double _) => RefreshAllSlots();
        private void HandlePointsChanged(double _) => RefreshAllSlots();
        private void HandleGodShopItemsChanged() => RefreshAllSlots();

        private void RefreshAllSlots()
        {
            for (int i = 0; i < bpSlots.Count; i++)
            {
                bpSlots[i]?.RefreshState(currencyManager);
            }

            for (int i = 0; i < cashSlots.Count; i++)
            {
                cashSlots[i]?.RefreshState(currencyManager);
            }

            for (int i = 0; i < godShopSlots.Count; i++)
            {
                godShopSlots[i]?.RefreshState();
            }
        }
    }
}
