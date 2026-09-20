using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Sole owner of bottom navigation: SHOP, CONVERT, RESTORE, and SETTINGS.
    /// Delegates panel visibility to ShopUIController / ConvertUIController / SettingsUIController;
    /// restoration spend stays on WorldRestorationManager (view triggers action only).
    /// </summary>
    public sealed class MainUIController : MonoBehaviour
    {
        [Header("Bottom Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button convertButton;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button settingsButton;

        [Header("Panel Controllers")]
        [SerializeField] private ShopUIController shopUIController;
        [SerializeField] private ConvertUIController convertUIController;
        [SerializeField] private SettingsUIController settingsUIController;
        [Tooltip("So this controller can hide the ad-recovery popup while Shop/Convert/Settings is open and let it back through once none of them are -- see UpdateAdRecoverySuppression's doc comment.")]
        [SerializeField] private RewardedAdRecoveryUIController rewardedAdRecoveryUIController;

        [Header("Optional Overlay")]
        [Tooltip("Dimmer shown while the shop panel is open (matches mock #shade).")]
        [SerializeField] private GameObject shopOverlayShade;

        private RebirthUIController cachedRebirthUI;
        private CurrencyManager cachedCurrency;

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

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
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

        private void Start()
        {
            cachedCurrency = CurrencyManager.Instance;
            if (cachedCurrency != null)
            {
                cachedCurrency.OnBrainPowerChanged += HandleCurrencyChanged;
                cachedCurrency.OnPointsChanged.RemoveListener(HandlePointsChangedUnity);
                cachedCurrency.OnPointsChanged.AddListener(HandlePointsChangedUnity);
            }

            RefreshButtonFaces();
            UpdateAdRecoverySuppression();
        }

        private void OnDestroy()
        {
            if (shopButton != null) shopButton.onClick.RemoveListener(OnShopClicked);
            if (convertButton != null) convertButton.onClick.RemoveListener(OnConvertClicked);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestoreClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);

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

            if (cachedCurrency != null)
            {
                cachedCurrency.OnBrainPowerChanged -= HandleCurrencyChanged;
                cachedCurrency.OnPointsChanged.RemoveListener(HandlePointsChangedUnity);
            }
        }

        private void HandleShopClosed()
        {
            SetShopShadeVisible(false);
            UpdateAdRecoverySuppression();
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

            UpdateAdRecoverySuppression();
        }

        /// <summary>
        /// 2026-09-16: RewardedAdRecoveryUIController shows its popup automatically off
        /// RewardedAdRecoveryManager's own event, with zero awareness of Shop/Convert/Settings --
        /// unlike those three, which already close each other out via the checks in
        /// OnShopClicked/OnConvertClicked/OnSettingsClicked below. Without this, the popup could
        /// still be showing when the player opens one of those three, and the two would visually
        /// stack on top of each other (the startup Settings/ad-popup stacking bug this fixes).
        /// Called at the end of every path that changes which of Shop/Convert/Settings is open,
        /// so the popup hides for as long as any of the three is up and reappears on its own the
        /// moment none of them are. Suppresses the view only -- never touches
        /// RewardedAdRecoveryManager.HasPendingRecovery -- so a player who checks Settings mid-
        /// popup doesn't lose their pending recovery or ads-watched progress.
        /// </summary>
        private void UpdateAdRecoverySuppression()
        {
            if (rewardedAdRecoveryUIController == null)
            {
                return;
            }

            bool anyOtherPanelOpen = (shopUIController != null && shopUIController.IsOpen)
                || (convertUIController != null && convertUIController.IsOpen)
                || (settingsUIController != null && settingsUIController.IsOpen);

            rewardedAdRecoveryUIController.SetSuppressedByOtherPanel(anyOtherPanelOpen);
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

            if (settingsUIController == null)
            {
                settingsUIController = FindAnyObjectByType<SettingsUIController>();
            }

            if (rewardedAdRecoveryUIController == null)
            {
                rewardedAdRecoveryUIController = FindAnyObjectByType<RewardedAdRecoveryUIController>();
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

            if (settingsButton == null)
            {
                settingsButton = FindButtonByName("SettingsButton");
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

            if (settingsUIController != null && settingsUIController.IsOpen)
            {
                settingsUIController.ClosePanel();
            }

            shopUIController.ToggleShop();
            SetShopShadeVisible(shopUIController.IsOpen);
            UpdateAdRecoverySuppression();
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

            if (settingsUIController != null && settingsUIController.IsOpen)
            {
                settingsUIController.ClosePanel();
            }

            convertUIController.TogglePanel();
            UpdateAdRecoverySuppression();
        }

        private void OnSettingsClicked()
        {
            if (settingsUIController == null)
            {
                return;
            }

            if (shopUIController != null && shopUIController.IsOpen)
            {
                shopUIController.CloseShop();
                SetShopShadeVisible(false);
            }

            if (convertUIController != null && convertUIController.IsOpen)
            {
                convertUIController.ClosePanel();
            }

            settingsUIController.TogglePanel();
            UpdateAdRecoverySuppression();
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

        private void HandleCurrencyChanged(double _) => RefreshButtonFaces();
        private void HandlePointsChangedUnity(double _) => RefreshButtonFaces();

        /// <summary>
        /// Live preview text on CONVERT/RESTORE. CONVERT previews the BP-&gt;$ yield at the same
        /// 1,000 BP = $1 rate ConvertUIController uses. RESTORE shows the Points balance it's about
        /// to spend in full, since OnRestoreClicked still spends CurrentPoints wholesale rather than
        /// a fixed per-tap price -- surfacing the live number is the safe fix; changing that to an
        /// actual fixed price is a separate design decision.
        /// </summary>
        private void RefreshButtonFaces()
        {
            if (cachedCurrency == null)
            {
                cachedCurrency = CurrencyManager.Instance;
                if (cachedCurrency == null)
                {
                    return;
                }
            }

            if (convertButton != null)
            {
                var text = convertButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    double previewCash = cachedCurrency.BrainPower / 1000d;
                    text.text = $"CONVERT\n+${NumberFormatter.Format(previewCash)}";
                }
            }

            if (restoreButton != null)
            {
                var text = restoreButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    double points = cachedCurrency.CurrentPoints;
                    text.text = $"RESTORE\n-{NumberFormatter.Format(points)} PTS";
                }
                restoreButton.interactable = cachedCurrency.CurrentPoints > 0d;
            }
        }
    }
}
