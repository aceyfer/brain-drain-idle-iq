using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;
using System;

namespace BrainDrain.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        /// <summary>PlayerIQ interval between celebration beats (every 1000 points).</summary>
        private const float IQCelebrationMilestoneInterval = 1000f;

        [Header("UI Text Fields")]
        [SerializeField] private TextMeshProUGUI capacityText;
        [FormerlySerializedAs("iqText")]
        [FormerlySerializedAs("worldRestorationText")]
        [SerializeField] private TextMeshProUGUI playerIQText;
        [SerializeField] private TextMeshProUGUI rankText;
        [FormerlySerializedAs("brainsCounterText")]
        [SerializeField] private TextMeshProUGUI brainPowerCounterText;
        [SerializeField] private TextMeshProUGUI cumulativeBrainPowerCounterText;
        [SerializeField] private TextMeshProUGUI rebirthCountText;
        [SerializeField] private TextMeshProUGUI bppsText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI pointsText;

        [Header("Cash/Points Conversion")]
        [SerializeField] private Button convertButton;

        [Header("High-IQ Celebration")]
        [Tooltip("Optional. CanvasGroup on the root HUD canvas, pulsed during the celebration beat.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("Optional. Full-screen Image (alpha 0 at rest) used for the cyan tint and white flash.")]
        [SerializeField] private Image celebrationFlashOverlay;

        private int lastIQMilestoneIndex;

        public TextMeshProUGUI CapacityText
        {
            get => capacityText;
            set => capacityText = value;
        }

        public TextMeshProUGUI PlayerIQText
        {
            get => playerIQText;
            set => playerIQText = value;
        }

        public TextMeshProUGUI RankText
        {
            get => rankText;
            set => rankText = value;
        }

        public TextMeshProUGUI BrainPowerCounterText
        {
            get => brainPowerCounterText;
            set => brainPowerCounterText = value;
        }

        public TextMeshProUGUI CumulativeBrainPowerCounterText
        {
            get => cumulativeBrainPowerCounterText;
            set => cumulativeBrainPowerCounterText = value;
        }

        public TextMeshProUGUI RebirthCountText
        {
            get => rebirthCountText;
            set => rebirthCountText = value;
        }

        public TextMeshProUGUI BPPSText
        {
            get => bppsText;
            set => bppsText = value;
        }

        public TextMeshProUGUI CashText
        {
            get => cashText;
            set => cashText = value;
        }

        public TextMeshProUGUI PointsText
        {
            get => pointsText;
            set => pointsText = value;
        }

        public Button ConvertButton
        {
            get => convertButton;
            set => convertButton = value;
        }

        private void Awake()
        {
            if (convertButton != null)
            {
                convertButton.onClick.RemoveListener(OnConvertClicked);
                convertButton.onClick.AddListener(OnConvertClicked);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized += InitializeHUD;
                InitializeHUD();
            }
            else
            {
                InitializeHUD();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameInitialized -= InitializeHUD;
            }
        }

        private void InitializeHUD()
        {
            UnsubscribeFromEvents();

            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                UpdateCapacityText(currency.BrainPower);
                UpdateRankText(currency.CumulativeBrainPower);
                UpdateBrainPowerCounterText(currency.BrainPower);
                UpdateCumulativeBrainPowerCounterText(currency.CumulativeBrainPower);
                UpdateBPPSText();
                UpdateCashText(currency.CurrentCash);
                UpdatePointsText(currency.CurrentPoints);
                currency.OnBrainPowerChanged += UpdateCapacityText;
                currency.OnBrainPowerChanged += UpdateBrainPowerCounterText;
                currency.OnCumulativeBrainPowerChanged += UpdateRankText;
                currency.OnCumulativeBrainPowerChanged += UpdateCumulativeBrainPowerCounterText;

                // OnCashChanged/OnPointsChanged are UnityEvents (not C# events like the above),
                // so they use AddListener/RemoveListener rather than +=/-=.
                currency.OnCashChanged.RemoveListener(UpdateCashText);
                currency.OnCashChanged.AddListener(UpdateCashText);
                currency.OnPointsChanged.RemoveListener(UpdatePointsText);
                currency.OnPointsChanged.AddListener(UpdatePointsText);
            }

            var playerIQManager = FindAnyObjectByType<PlayerIQManager>();
            if (playerIQManager != null)
            {
                lastIQMilestoneIndex = Mathf.FloorToInt(playerIQManager.PlayerIQ / IQCelebrationMilestoneInterval);
                UpdatePlayerIQText(playerIQManager.PlayerIQ);
                playerIQManager.OnPlayerIQChanged += UpdatePlayerIQText;
            }

            if (RebirthManager.Instance != null)
            {
                UpdateRebirthCountText(RebirthManager.Instance.RebirthCount);
                RebirthManager.Instance.OnRebirthCountChanged += UpdateRebirthCountText;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= UpdateBPPSText;
                GameManager.Instance.OnSecondTick += UpdateBPPSText;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.OnBrainPowerChanged -= UpdateCapacityText;
                currency.OnBrainPowerChanged -= UpdateBrainPowerCounterText;
                currency.OnCumulativeBrainPowerChanged -= UpdateRankText;
                currency.OnCumulativeBrainPowerChanged -= UpdateCumulativeBrainPowerCounterText;
                currency.OnCashChanged.RemoveListener(UpdateCashText);
                currency.OnPointsChanged.RemoveListener(UpdatePointsText);
            }

            var playerIQManager = FindAnyObjectByType<PlayerIQManager>();
            if (playerIQManager != null)
            {
                playerIQManager.OnPlayerIQChanged -= UpdatePlayerIQText;
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= UpdateRebirthCountText;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= UpdateBPPSText;
            }
        }

        private void UpdateBrainPowerCounterText(double brainPower)
        {
            if (brainPowerCounterText != null)
            {
                brainPowerCounterText.text = $"{NumberFormatter.Format(brainPower)} BRAIN POWER";
            }
        }

        private void UpdateCapacityText(double brainPower)
        {
            if (capacityText != null)
            {
                double percent = Math.Min(100.0, (brainPower / 500000.0) * 100.0);
                capacityText.text = $"{percent:F1}% ABSORBED";
            }
        }

        private void UpdatePlayerIQText(float playerIQ)
        {
            if (playerIQText != null)
            {
                playerIQText.text = $"IQ: {playerIQ:F0}";
            }

            int milestoneIndex = Mathf.FloorToInt(playerIQ / IQCelebrationMilestoneInterval);
            if (milestoneIndex > lastIQMilestoneIndex)
            {
                lastIQMilestoneIndex = milestoneIndex;
                AnimationController.PlayHighIQCelebration(hudCanvasGroup, celebrationFlashOverlay);
            }
        }

        private void UpdateRankText(double cumulativeBrainPower)
        {
            if (rankText != null && GameManager.Instance != null)
            {
                rankText.text = GameManager.Instance.GetRankName(cumulativeBrainPower).ToUpper();
            }
        }

        private void UpdateCumulativeBrainPowerCounterText(double cumulativeBrainPower)
        {
            if (cumulativeBrainPowerCounterText != null)
            {
                cumulativeBrainPowerCounterText.text = $"LIFETIME: {NumberFormatter.Format(cumulativeBrainPower)}";
            }
        }

        private void UpdateRebirthCountText(int rebirthCount)
        {
            if (rebirthCountText != null)
            {
                rebirthCountText.text = $"REBIRTHS: {rebirthCount}";
            }
        }

        /// <summary>
        /// Pulled from CurrencyManager.IdleBPPS on every GameManager.OnSecondTick rather than
        /// pushed via a dedicated event, since idleBpps itself only changes at purchase/reset
        /// time -- this just keeps the display in sync with the tick, as the audit asked for.
        /// </summary>
        private void UpdateBPPSText()
        {
            if (bppsText == null)
            {
                return;
            }

            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                bppsText.text = $"{NumberFormatter.Format(currency.IdleBPPS)} BPPS";
            }
        }

        private void UpdateCashText(double currentCash)
        {
            if (cashText == null)
            {
                return;
            }

            var currency = CurrencyManager.Instance;
            double cps = currency != null ? currency.CashPerSecond : 0d;
            cashText.text = $"{NumberFormatter.Format(currentCash)} CASH ({NumberFormatter.Format(cps)}/s)";
        }

        private void UpdatePointsText(double currentPoints)
        {
            if (pointsText != null)
            {
                pointsText.text = $"{NumberFormatter.Format(currentPoints)} POINTS";
            }
        }

        private void OnConvertClicked()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.ConvertCashToPoints(currency.CurrentCash);
            }
        }
    }
}
