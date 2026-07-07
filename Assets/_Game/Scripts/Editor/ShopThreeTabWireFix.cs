#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Wires the 3-tab shop layout on ShopPanel: BP UPGRADES, CASH INVESTMENTS, RP RESTORATIONS.
    /// Menu: BrainDrain/Wire 3-Tab Shop
    /// </summary>
    public static class ShopThreeTabWireFix
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string UpgradeSlotPrefabPath = "Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab";
        private const string RestorationSlotPrefabPath = "Assets/_Game/Prefabs/UI/RestorationSlotPrefab.prefab";

        [MenuItem("BrainDrain/Wire 3-Tab Shop")]
        public static void WireThreeTabShop()
        {
            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            ShopUIController shopUI = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            if (shopUI == null)
            {
                Debug.LogError("[ShopThreeTabWireFix] ShopUIController not found.");
                return;
            }

            Transform shopPanel = shopUI.transform;
            ScrollRect bpScroll = shopPanel.GetComponentInChildren<ScrollRect>(true);
            if (bpScroll == null)
            {
                Debug.LogError("[ShopThreeTabWireFix] ShopScrollView not found under ShopPanel.");
                return;
            }

            Transform existingScroll = bpScroll.transform;
            existingScroll.name = "BpUpgradesScrollView";

            Transform tabBar = shopPanel.Find("ShopTabBar");
            if (tabBar == null)
            {
                tabBar = CreateTabBar(shopPanel);
            }

            Transform bpPanel = EnsureTabPanel(shopPanel, "BpUpgradesPanel", existingScroll, out RectTransform bpContent);
            Transform cashPanel = EnsureClonedTabPanel(shopPanel, bpScroll.gameObject, "CashInvestmentsPanel", "CashInvestmentsScrollView", out RectTransform cashContent);
            Transform rpPanel = EnsureClonedTabPanel(shopPanel, bpScroll.gameObject, "RpRestorationsPanel", "RpRestorationsScrollView", out RectTransform rpContent);

            existingScroll.SetParent(bpPanel, false);
            StretchFull(existingScroll as RectTransform);

            Button bpTab = tabBar.Find("BpTabButton")?.GetComponent<Button>();
            Button cashTab = tabBar.Find("CashTabButton")?.GetComponent<Button>();
            Button rpTab = tabBar.Find("RpTabButton")?.GetComponent<Button>();

            UpgradeSlotUI slotPrefab = AssetDatabase.LoadAssetAtPath<UpgradeSlotUI>(UpgradeSlotPrefabPath);
            RestorationSlotUI restorationPrefab = EnsureRestorationSlotPrefab(slotPrefab);

            SerializedObject so = new SerializedObject(shopUI);
            so.FindProperty("bpTabButton").objectReferenceValue = bpTab;
            so.FindProperty("cashTabButton").objectReferenceValue = cashTab;
            so.FindProperty("rpTabButton").objectReferenceValue = rpTab;
            so.FindProperty("bpTabPanel").objectReferenceValue = bpPanel.gameObject;
            so.FindProperty("cashTabPanel").objectReferenceValue = cashPanel.gameObject;
            so.FindProperty("rpTabPanel").objectReferenceValue = rpPanel.gameObject;
            so.FindProperty("bpContent").objectReferenceValue = bpContent;
            so.FindProperty("cashContent").objectReferenceValue = cashContent;
            so.FindProperty("rpContent").objectReferenceValue = rpContent;
            if (so.FindProperty("slotPrefab").objectReferenceValue == null)
            {
                so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            }

            so.FindProperty("restorationSlotPrefab").objectReferenceValue = restorationPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            bpPanel.gameObject.SetActive(true);
            cashPanel.gameObject.SetActive(false);
            rpPanel.gameObject.SetActive(false);

            EditorUtility.SetDirty(shopUI);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ShopThreeTabWireFix] 3-tab shop wired: BP / Cash / RP panels and tab buttons assigned.");
        }

        private static Transform CreateTabBar(Transform shopPanel)
        {
            GameObject tabBarGo = new GameObject("ShopTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(tabBarGo, "Create ShopTabBar");
            tabBarGo.transform.SetParent(shopPanel, false);

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

            CreateTabButton(tabBarGo.transform, "BpTabButton", "BP UPGRADES");
            CreateTabButton(tabBarGo.transform, "CashTabButton", "CASH INVESTMENTS");
            CreateTabButton(tabBarGo.transform, "RpTabButton", "RP RESTORATIONS");

            tabBarGo.transform.SetSiblingIndex(1);
            return tabBarGo.transform;
        }

        private static void CreateTabButton(Transform parent, string name, string label)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Tab Button");
            buttonGo.transform.SetParent(parent, false);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.35f, 0.35f, 0.4f, 1f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonGo.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            StretchFull(textRect);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private static Transform EnsureTabPanel(Transform shopPanel, string panelName, Transform scrollTransform, out RectTransform content)
        {
            Transform panel = shopPanel.Find(panelName);
            if (panel == null)
            {
                GameObject panelGo = new GameObject(panelName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(panelGo, "Create Tab Panel");
                panelGo.transform.SetParent(shopPanel, false);
                panel = panelGo.transform;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(0f, 0f);
            panelRect.offsetMax = new Vector2(0f, -56f);

            ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
            content = scroll != null ? scroll.content : null;
            return panel;
        }

        private static Transform EnsureClonedTabPanel(
            Transform shopPanel,
            GameObject sourceScroll,
            string panelName,
            string scrollName,
            out RectTransform content)
        {
            Transform panel = shopPanel.Find(panelName);
            GameObject scrollGo;

            if (panel == null)
            {
                GameObject panelGo = new GameObject(panelName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(panelGo, "Create Tab Panel");
                panelGo.transform.SetParent(shopPanel, false);
                panel = panelGo.transform;

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = new Vector2(0f, 0f);
                panelRect.offsetMax = new Vector2(0f, -56f);

                scrollGo = Object.Instantiate(sourceScroll, panel);
                scrollGo.name = scrollName;
                Undo.RegisterCreatedObjectUndo(scrollGo, "Clone Scroll View");
            }
            else
            {
                Transform existingScroll = panel.Find(scrollName);
                scrollGo = existingScroll != null ? existingScroll.gameObject : Object.Instantiate(sourceScroll, panel);
                scrollGo.name = scrollName;
            }

            StretchFull(scrollGo.GetComponent<RectTransform>());

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            content = scroll != null ? scroll.content : null;
            if (content != null)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(content.GetChild(i).gameObject);
                }
            }

            panel.SetSiblingIndex(panelName == "CashInvestmentsPanel" ? 3 : 4);
            return panel;
        }

        private static RestorationSlotUI EnsureRestorationSlotPrefab(UpgradeSlotUI upgradePrefab)
        {
            RestorationSlotUI existing = AssetDatabase.LoadAssetAtPath<RestorationSlotUI>(RestorationSlotPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            if (upgradePrefab == null)
            {
                Debug.LogWarning("[ShopThreeTabWireFix] UpgradeSlotPrefab missing; RP rows will not build until prefab exists.");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(upgradePrefab.gameObject) as GameObject;
            Object.DestroyImmediate(instance.GetComponent<UpgradeSlotUI>(), true);
            RestorationSlotUI restoration = instance.AddComponent<RestorationSlotUI>();

            UpgradeSlotUI source = upgradePrefab;
            SerializedObject sourceSo = new SerializedObject(source);
            SerializedObject destSo = new SerializedObject(restoration);
            CopyProperty(sourceSo, destSo, "nameText");
            CopyProperty(sourceSo, destSo, "descriptionText");
            CopyProperty(sourceSo, destSo, "costText");
            CopyProperty(sourceSo, destSo, "buyButton");
            CopyProperty(sourceSo, destSo, "background");
            destSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, RestorationSlotPrefabPath);
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<RestorationSlotUI>(RestorationSlotPrefabPath);
        }

        private static void CopyProperty(SerializedObject source, SerializedObject dest, string propertyName)
        {
            SerializedProperty src = source.FindProperty(propertyName);
            SerializedProperty dst = dest.FindProperty(propertyName);
            if (src != null && dst != null)
            {
                dst.objectReferenceValue = src.objectReferenceValue;
            }
        }

        private static void StretchFull(RectTransform rect)
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
    }
}
#endif
