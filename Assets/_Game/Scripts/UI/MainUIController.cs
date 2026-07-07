using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Sole owner of bottom navigation: SHOP, CONVERT, and RESTORE.
    /// Delegates panel visibility to ShopUIController / ConvertUIController;
    /// restoration spend stays on WorldRestorationManager (view triggers action only).
    /// </summary>
    public sealed class MainUIController : MonoBehaviour
    {
        [Header("Bottom Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button convertButton;
        [SerializeField] private Button restoreButton;

        [Header("Panel Controllers")]
        [SerializeField] private ShopUIController shopUIController;
        [SerializeField] private ConvertUIController convertUIController;

        [Header("Optional Overlay")]
        [Tooltip("Dimmer shown while the shop panel is open (matches mock #shade).")]
        [SerializeField] private GameObject shopOverlayShade;

        private RebirthUIController cachedRebirthUI;

        private void Awake()
        {
            ResolveReferences();

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OnShopClicked);
                shopButton.onClick.AddListener(OnShopClicked);
            }

            if (convertButton != null)
            {
                convertButton.onClick.RemoveListener(OnConvertClicked);
                convertButton.onClick.AddListener(OnConvertClicked);
            }

            if (restoreButton != null)
            {
                restoreButton.onClick.RemoveListener(OnRestoreClicked);
                restoreButton.onClick.AddListener(OnRestoreClicked);
            }

            if (shopOverlayShade != null)
            {
                shopOverlayShade.SetActive(false);
                Button shadeButton = shopOverlayShade.GetComponent<Button>();
                if (shadeButton != null)
                {
                    shadeButton.onClick.RemoveListener(OnShopShadeClicked);
                    shadeButton.onClick.AddListener(OnShopShadeClicked);
                }
            }

            if (shopUIController != null)
            {
                shopUIController.ShopClosed -= HandleShopClosed;
                shopUIController.ShopClosed += HandleShopClosed;
            }
        }

        private void OnDestroy()
        {
            if (shopButton != null) shopButton.onClick.RemoveListener(OnShopClicked);
            if (convertButton != null) convertButton.onClick.RemoveListener(OnConvertClicked);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestoreClicked);

            if (shopOverlayShade != null)
            {
                Button shadeButton = shopOverlayShade.GetComponent<Button>();
                if (shadeButton != null)
                {
                    shadeButton.onClick.RemoveListener(OnShopShadeClicked);
                }
            }

            if (shopUIController != null)
            {
                shopUIController.ShopClosed -= HandleShopClosed;
            }
        }

        private void HandleShopClosed()
        {
            SetShopShadeVisible(false);
        }

        private void OnShopShadeClicked()
        {
            if (shopUIController != null && shopUIController.IsOpen)
            {
                shopUIController.CloseShop();
            }
            else
            {
                SetShopShadeVisible(false);
            }
        }

        private void ResolveReferences()
        {
            if (shopUIController == null)
            {
                shopUIController = FindAnyObjectByType<ShopUIController>();
            }

            if (convertUIController == null)
            {
                convertUIController = FindAnyObjectByType<ConvertUIController>();
            }

            if (shopButton == null)
            {
                shopButton = FindButtonByName("ShopButton");
            }

            if (convertButton == null)
            {
                convertButton = FindButtonByName("ConvertButton");
            }

            if (restoreButton == null)
            {
                restoreButton = FindButtonByName("RestoreButton");
            }

            if (cachedRebirthUI == null)
            {
                cachedRebirthUI = FindAnyObjectByType<RebirthUIController>();
            }
        }

        private static Button FindButtonByName(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private void OnShopClicked()
        {
            if (shopUIController == null)
            {
                return;
            }

            if (convertUIController != null && convertUIController.IsOpen)
            {
                convertUIController.ClosePanel();
            }

            shopUIController.ToggleShop();
            SetShopShadeVisible(shopUIController.IsOpen);
        }

        private void OnConvertClicked()
        {
            if (convertUIController == null)
            {
                return;
            }

            if (shopUIController != null && shopUIController.IsOpen)
            {
                shopUIController.CloseShop();
                SetShopShadeVisible(false);
            }

            convertUIController.TogglePanel();
        }

        private void OnRestoreClicked()
        {
            CurrencyManager currency = CurrencyManager.Instance;
            if (currency != null)
            {
                WorldRestorationManager.Instance?.TrySpendPointsOnRestoration(currency.CurrentPoints);
            }

            cachedRebirthUI?.RefreshTriggerButton();
        }

        private void SetShopShadeVisible(bool visible)
        {
            if (shopOverlayShade != null)
            {
                shopOverlayShade.SetActive(visible);
            }
        }
    }
}
