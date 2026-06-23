#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// One-shot scene fix for the shop panel layout. The "ShopPanel" GameObject already exists
    /// (background Image + ShopScrollView + the ShopUIController component itself), but
    /// ShopUIController.shopPanel/shopButton/closeButton were all left unwired (fileID 0) --
    /// so the panel just sat permanently active at its authored anchors (0,0.45)-(1,0.875)
    /// with nothing controlling visibility. This:
    ///   1. Resizes the panel to the bottom 60% of the screen.
    ///   2. Builds a SHOP button styled to match the existing ConvertButton (same rounded
    ///      sprite/font/size, cyan instead of hot pink), inserted into the same EconomyBar
    ///      HorizontalLayoutGroup row immediately to Convert's left.
    ///   3. Builds a close/X button anchored to the panel's top-right corner.
    ///   4. Wires all three fields via SerializedObject (safe regardless of private access
    ///      level, no hand-edited YAML) -- ShopUIController.Awake() already wires the
    ///      resulting buttons' onClick to OpenShop()/CloseShop() and hides the panel by
    ///      default once shopPanel is actually set, so no separate onClick wiring is needed.
    ///   5. Hides the panel and saves the scene.
    /// Idempotent: finds and updates existing ShopButton/CloseButton GameObjects on re-run
    /// rather than duplicating them.
    /// </summary>
    public static class ShopPanelLayoutFix
    {
        [MenuItem("BrainDrain/Fix Shop Panel Layout")]
        public static void FixShopPanelLayout()
        {
            ShopUIController shopUI = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            if (shopUI == null)
            {
                Debug.LogError("[ShopPanelLayoutFix] No ShopUIController found in the scene.");
                return;
            }

            GameObject shopPanel = shopUI.gameObject;

            GameObject convertButton = GameObject.Find("ConvertButton");
            if (convertButton == null)
            {
                Debug.LogError("[ShopPanelLayoutFix] Could not find 'ConvertButton' to copy styling from.");
                return;
            }

            // 1) Resize the panel to the bottom 60% of the screen.
            RectTransform panelRect = shopPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.6f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            // 2) Build the SHOP button, matching ConvertButton's style, just left of it.
            Button shopButton = BuildShopButton(convertButton);

            // 3) Build the close/X button, top-right of the panel.
            Button closeButton = BuildCloseButton(shopPanel);

            // 4) Wire ShopUIController's fields.
            SerializedObject so = new SerializedObject(shopUI);
            SerializedProperty shopPanelProp = so.FindProperty("shopPanel");
            SerializedProperty shopButtonProp = so.FindProperty("shopButton");
            SerializedProperty closeButtonProp = so.FindProperty("closeButton");

            if (shopPanelProp == null || shopButtonProp == null || closeButtonProp == null)
            {
                Debug.LogError("[ShopPanelLayoutFix] Could not find shopPanel/shopButton/closeButton fields on ShopUIController.");
                return;
            }

            shopPanelProp.objectReferenceValue = shopPanel;
            shopButtonProp.objectReferenceValue = shopButton;
            closeButtonProp.objectReferenceValue = closeButton;
            so.ApplyModifiedProperties();

            // Deliberately NOT calling shopPanel.SetActive(false) here. ShopUIController lives
            // on this same GameObject -- if it's saved inactive, Awake() never runs at all when
            // the scene loads (Unity only calls Awake on objects active at load, or on first
            // activation), which means shopButton/closeButton's onClick.AddListener calls in
            // Awake() never happen either, silently breaking both buttons. ShopUIController's
            // own Awake() already calls SetActive(false) itself, AFTER registering the
            // listeners, which is the correct place for it: it only runs once the object has
            // actually had a chance to wake up.
            EditorUtility.SetDirty(shopUI);
            Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ShopPanelLayoutFix] Shop panel resized to bottom 60%, SHOP/X buttons created and wired (left active -- ShopUIController.Awake() hides it once Play Mode actually runs). Scene saved.");
        }

        private static Button BuildShopButton(GameObject convertButton)
        {
            Transform parentRow = convertButton.transform.parent;
            Image convertImage = convertButton.GetComponent<Image>();
            LayoutElement convertLayout = convertButton.GetComponent<LayoutElement>();
            TextMeshProUGUI convertText = convertButton.GetComponentInChildren<TextMeshProUGUI>();

            Transform existingHost = parentRow.Find("ShopButton");
            GameObject host = existingHost != null ? existingHost.gameObject : new GameObject("ShopButton", typeof(RectTransform));
            host.transform.SetParent(parentRow, false);
            host.transform.SetSiblingIndex(convertButton.transform.GetSiblingIndex());

            Image image = host.GetComponent<Image>();
            if (image == null) image = host.AddComponent<Image>();
            image.sprite = convertImage != null ? convertImage.sprite : null;
            image.type = Image.Type.Sliced;
            image.color = HexColor("#00F0FF"); // cyan -- distinct from CONVERT's hot pink

            Button button = host.GetComponent<Button>();
            if (button == null) button = host.AddComponent<Button>();
            button.targetGraphic = image;

            LayoutElement layout = host.GetComponent<LayoutElement>();
            if (layout == null) layout = host.AddComponent<LayoutElement>();
            layout.minWidth = convertLayout != null ? convertLayout.minWidth : 150f;
            layout.flexibleWidth = convertLayout != null ? convertLayout.flexibleWidth : 0.8f;
            layout.layoutPriority = convertLayout != null ? convertLayout.layoutPriority : 1;

            Transform existingTextHost = host.transform.Find("ButtonText");
            GameObject textHost = existingTextHost != null ? existingTextHost.gameObject : new GameObject("ButtonText", typeof(RectTransform));
            textHost.transform.SetParent(host.transform, false);
            RectTransform textRect = textHost.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI label = textHost.GetComponent<TextMeshProUGUI>();
            if (label == null) label = textHost.AddComponent<TextMeshProUGUI>();
            label.text = "SHOP";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (convertText != null)
            {
                label.font = convertText.font;
                label.enableAutoSizing = convertText.enableAutoSizing;
                label.fontSizeMin = convertText.fontSizeMin;
                label.fontSizeMax = convertText.fontSizeMax;
                label.fontSize = convertText.fontSize;
            }

            return button;
        }

        private static Button BuildCloseButton(GameObject shopPanel)
        {
            Transform existingHost = shopPanel.transform.Find("CloseButton");
            GameObject host = existingHost != null ? existingHost.gameObject : new GameObject("CloseButton", typeof(RectTransform));
            host.transform.SetParent(shopPanel.transform, false);

            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = new Vector2(-16f, -16f);

            Image image = host.GetComponent<Image>();
            if (image == null) image = host.AddComponent<Image>();
            image.color = HexColor("#4A4E5D");

            Button button = host.GetComponent<Button>();
            if (button == null) button = host.AddComponent<Button>();
            button.targetGraphic = image;

            Transform existingTextHost = host.transform.Find("CloseText");
            GameObject textHost = existingTextHost != null ? existingTextHost.gameObject : new GameObject("CloseText", typeof(RectTransform));
            textHost.transform.SetParent(host.transform, false);
            RectTransform textRect = textHost.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI label = textHost.GetComponent<TextMeshProUGUI>();
            if (label == null) label = textHost.AddComponent<TextMeshProUGUI>();
            label.text = "X";
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.raycastTarget = false;

            return button;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
#endif
