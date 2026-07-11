using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.UI
{
    /// <summary>
    /// Manages the Convert modal/panel.
    /// BP→Cash conversion and Cash→Restoration Points conversion are both available from the
    /// start of a fresh save. The Points Shop (deep upgrade tier) stays locked until after the
    /// first Snotting; that gate lives on the PointsShopButton in HUDController, not here.
    /// </summary>
    public sealed class ConvertUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject convertPanel;

        [Header("Brain Power to $ Conversion")]
        [SerializeField] private TextMeshProUGUI bpToCashStatusText;
        [SerializeField] private Button convertBPAmountButton; // Convert 50%
        [SerializeField] private Button convertAllBPButton;     // Convert 100%

        [Header("$ to Points Conversion")]
        [SerializeField] private GameObject pointsConversionGroup; // to hide/lock
        [SerializeField] private TextMeshProUGUI cashToPointsStatusText;
        [SerializeField] private Button convertAllCashButton;
        [SerializeField] private TextMeshProUGUI pointsLockedWarningText;

        [Header("Close Components")]
        [SerializeField] private Button closeButton;

        public void Configure(
            GameObject panel,
            TextMeshProUGUI bpStatusText,
            Button bp50Button,
            Button bp100Button,
            GameObject pointsGroup,
            TextMeshProUGUI pointsStatus,
            Button pointsAllButton,
            TextMeshProUGUI warningText,
            Button closeBtn)
        {
            convertPanel = panel;
            bpToCashStatusText = bpStatusText;
            convertBPAmountButton = bp50Button;
            convertAllBPButton = bp100Button;
            pointsConversionGroup = pointsGroup;
            cashToPointsStatusText = pointsStatus;
            convertAllCashButton = pointsAllButton;
            pointsLockedWarningText = warningText;
            closeButton = closeBtn;
        }

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (convertBPAmountButton != null) convertBPAmountButton.onClick.AddListener(ConvertHalfBP);
            if (convertAllBPButton != null) convertAllBPButton.onClick.AddListener(ConvertAllBP);
            if (convertAllCashButton != null) convertAllCashButton.onClick.AddListener(ConvertAllCash);
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnBrainPowerChanged += HandleBrainPowerChanged;
                CurrencyManager.Instance.OnCashChanged.RemoveListener(HandleCashChanged);
                CurrencyManager.Instance.OnCashChanged.AddListener(HandleCashChanged);
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnBrainPowerChanged -= HandleBrainPowerChanged;
                currencyChangedCleanup();
            }
        }

        private void currencyChangedCleanup()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCashChanged.RemoveListener(HandleCashChanged);
            }
        }

        public void OpenPanel()
        {
            if (convertPanel != null)
            {
                convertPanel.SetActive(true);
                RefreshVisuals();
                
                RectTransform panelRect = convertPanel.GetComponent<RectTransform>();
                CanvasGroup panelCanvasGroup = convertPanel.GetComponent<CanvasGroup>();
                if (panelRect != null)
                {
                    AnimationController.PlayPopupSpawn(panelRect, panelCanvasGroup);
                }
            }
        }

        public void ClosePanel()
        {
            if (convertPanel != null)
            {
                convertPanel.SetActive(false);
            }
        }

        /// <summary>Whether the convert panel is currently visible.</summary>
        public bool IsOpen => convertPanel != null && convertPanel.activeSelf;

        /// <summary>Toggles the convert panel open or closed.</summary>
        public void TogglePanel()
        {
            if (IsOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        private void HandleBrainPowerChanged(double _) => RefreshVisuals();
        private void HandleCashChanged(double _) => RefreshVisuals();

        private void RefreshVisuals()
        {
            var currency = CurrencyManager.Instance;
            if (currency == null) return;

            // BP to $
            double bp = currency.BrainPower;
            double estimatedCash = bp / 1000d; // Exchange rate: 1000 BP = 1 $
            if (bpToCashStatusText != null)
            {
                bpToCashStatusText.text = $"Current BP: {NumberFormatter.Format(bp)}\nRate: 1,000 BP -> $1\nYields: ${NumberFormatter.Format(estimatedCash)}";
                bpToCashStatusText.fontSize = 28f;
            }

            if (convertBPAmountButton != null)
            {
                convertBPAmountButton.interactable = bp >= 1000d;
                var text = convertBPAmountButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"CONVERT 50%\n(+${NumberFormatter.Format(estimatedCash * 0.5f)})";
                    text.fontSize = 24f;
                }
            }

            if (convertAllBPButton != null)
            {
                convertAllBPButton.interactable = bp >= 1000d;
                var text = convertAllBPButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"CONVERT 100%\n(+${NumberFormatter.Format(estimatedCash)})";
                    text.fontSize = 24f;
                }
            }

            // $ to Restoration Points — available from the start, no rebirth gate.
            // The Points Shop (deeper upgrade tier) is locked post-rebirth separately via
            // HUDController.UpdatePointsLockState / pointsShopButton.interactable.
            if (pointsConversionGroup != null)
            {
                pointsConversionGroup.SetActive(true);
            }

            // The locked-warning widget is no longer needed; hide it unconditionally so any
            // scene that still has it wired doesn't show stale copy.
            if (pointsLockedWarningText != null)
            {
                pointsLockedWarningText.gameObject.SetActive(false);
            }

            double cash = currency.CurrentCash;
            double rate = currency.PointsConversionRate * currency.ShopCashToPointsMultiplier * currency.ShopAllPointGainsMultiplier;
            double estimatedPoints = cash * rate;

            if (cashToPointsStatusText != null)
            {
                cashToPointsStatusText.text = $"Current $: ${NumberFormatter.Format(cash)}\nRate: $1 -> {rate:F2} Restoration Points\nYields: {NumberFormatter.Format(estimatedPoints)} Pts";
                cashToPointsStatusText.fontSize = 28f;
            }

            if (convertAllCashButton != null)
            {
                convertAllCashButton.interactable = cash > 0d;
                var text = convertAllCashButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"CONVERT ALL $\n(+{NumberFormatter.Format(estimatedPoints)} Pts)";
                    text.fontSize = 24f;
                }
            }
        }

        private void ConvertHalfBP()
        {
            var currency = CurrencyManager.Instance;
            if (currency == null) return;

            double bpToConvert = currency.BrainPower * 0.5d;
            bpToConvert = System.Math.Floor(bpToConvert);
            if (bpToConvert >= 1000d)
            {
                bpToConvert = System.Math.Floor(bpToConvert / 1000d) * 1000d;
                if (currency.SpendBrainPower(bpToConvert))
                {
                    double cashEarned = bpToConvert / 1000d;
                    currency.AddCash(cashEarned);
                }
            }
        }

        private void ConvertAllBP()
        {
            var currency = CurrencyManager.Instance;
            if (currency == null) return;

            double bpToConvert = currency.BrainPower;
            bpToConvert = System.Math.Floor(bpToConvert / 1000d) * 1000d;
            if (bpToConvert >= 1000d)
            {
                if (currency.SpendBrainPower(bpToConvert))
                {
                    double cashEarned = bpToConvert / 1000d;
                    currency.AddCash(cashEarned);
                }
            }
        }

        private void ConvertAllCash()
        {
            var currency = CurrencyManager.Instance;
            if (currency == null) return;

            currency.ConvertCashToPoints(currency.CurrentCash);
        }
    }
}