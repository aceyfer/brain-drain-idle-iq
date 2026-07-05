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
        [Tooltip("Illumisnotti title earned at the current Snotting (Rebirth) tier -- displayed under the IQ readout. Blank until the first Snotting. Added 2026-06-21.")]
        [SerializeField] private TextMeshProUGUI illumisnottiTitleText;
        [FormerlySerializedAs("brainsCounterText")]
        [SerializeField] private TextMeshProUGUI brainPowerCounterText;
        [SerializeField] private TextMeshProUGUI cumulativeBrainPowerCounterText;
        [SerializeField] private TextMeshProUGUI rebirthCountText;
        [SerializeField] private TextMeshProUGUI bppsText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI restorationProgressText;

        [Header("Cash/Points Conversion")]
        [SerializeField] private Button convertButton;
        [SerializeField] private ConvertUIController convertUIController;

        [Header("Premium Shop Control")]
        [SerializeField] private Button premiumShopButton;
        [SerializeField] private PremiumShopUIController premiumShopUIController;

        [Header("Points Locking Visibility")]
        [SerializeField] private Button pointsShopButton;

        [Header("World Restoration")]
        [Tooltip("Spends all current Points on World Restoration when clicked.")]
        [SerializeField] private Button restoreButton;

        [Header("High-IQ Celebration")]
        [Tooltip("Optional. CanvasGroup on the root HUD canvas, pulsed during the celebration beat.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("Optional. Full-screen Image (alpha 0 at rest) used for the cyan tint and white flash.")]
        [SerializeField] private Image celebrationFlashOverlay;

        private int lastIQMilestoneIndex;
        private RebirthUIController cachedRebirthUI;

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

        public TextMeshProUGUI IllumisnottiTitleText
        {
            get => illumisnottiTitleText;
            set => illumisnottiTitleText = value;
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

        public TextMeshProUGUI RestorationProgressText
        {
            get => restorationProgressText;
            set => restorationProgressText = value;
        }

        public Button RestoreButton
        {
            get => restoreButton;
            set => restoreButton = value;
        }

        public void ConfigureConvertPanel(ConvertUIController controller, Button pointsButton)
        {
            convertUIController = controller;
            pointsShopButton = pointsButton;
        }

        public void ForceUpdatePointsLockState(int rebirthCount)
        {
            UpdatePointsLockState(rebirthCount);
        }

        private void Awake()
        {
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

            if (premiumShopButton != null)
            {
                premiumShopButton.onClick.RemoveListener(OnPremiumShopClicked);
                premiumShopButton.onClick.AddListener(OnPremiumShopClicked);
            }
        }

        private void OnPremiumShopClicked()
        {
            if (premiumShopUIController != null)
            {
                premiumShopUIController.OpenShop();
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

            var tapHandler = FindAnyObjectByType<PlayerTapHandler>();
            if (tapHandler != null)
            {
                tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;
                tapHandler.OnTapRewardEarned += HandleTapRewardEarned;
            }

            if (RebirthManager.Instance != null)
            {
                UpdateRebirthCountText(RebirthManager.Instance.RebirthCount);
                RebirthManager.Instance.OnRebirthCountChanged += UpdateRebirthCountText;
                UpdateIllumisnottiTitleText(RebirthManager.Instance.RebirthCount);
                RebirthManager.Instance.OnRebirthCountChanged += UpdateIllumisnottiTitleText;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChangedForPoints;
                UpdatePointsLockState(RebirthManager.Instance.RebirthCount);
            }
            else
            {
                UpdatePointsLockState(0);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= UpdateBPPSText;
                GameManager.Instance.OnSecondTick += UpdateBPPSText;
            }

            cachedRebirthUI = FindAnyObjectByType<RebirthUIController>();

            var worldRestoration = WorldRestorationManager.Instance;
            if (worldRestoration != null)
            {
                UpdateRestorationProgressText(worldRestoration.CumulativePointsSpentOnRestoration);
                worldRestoration.OnRestorationProgressChanged -= UpdateRestorationProgressText;
                worldRestoration.OnRestorationProgressChanged += UpdateRestorationProgressText;
            }

            // Re-evaluate the restoration text when the player performs their first Snotting,
            // since that removes the "unlock progress" line from the display.
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChangedForRestorationText;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChangedForRestorationText;
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

            var tapHandler = FindAnyObjectByType<PlayerTapHandler>();
            if (tapHandler != null)
            {
                tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= UpdateRebirthCountText;
                RebirthManager.Instance.OnRebirthCountChanged -= UpdateIllumisnottiTitleText;
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChangedForPoints;
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChangedForRestorationText;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= UpdateBPPSText;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= UpdateRestorationProgressText;
            }

            if (premiumShopButton != null)
            {
                premiumShopButton.onClick.RemoveListener(OnPremiumShopClicked);
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
                if (playerIQ > 100f)
                    playerIQText.text = $"IQ: {playerIQ:F0} <color=#FF8C00>OVERCHARGED</color>";
                else
                    playerIQText.text = $"IQ: {playerIQ:F0}";
            }

            int milestoneIndex = Mathf.FloorToInt(playerIQ / IQCelebrationMilestoneInterval);
            if (milestoneIndex > lastIQMilestoneIndex)
            {
                lastIQMilestoneIndex = milestoneIndex;
                AnimationController.PlayHighIQCelebration(hudCanvasGroup, celebrationFlashOverlay);
            }
        }

        private void HandleTapRewardEarned(double _)
        {
            AnimationController.PlayIQFlash(playerIQText);
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
                rebirthCountText.text = $"SNOTTINGS: {rebirthCount}";
            }
        }

        /// <summary>Updates the Illumisnotti title shown under the IQ readout. Blank (no text) until the first Snotting.</summary>
        private void UpdateIllumisnottiTitleText(int rebirthCount)
        {
            if (illumisnottiTitleText != null)
            {
                illumisnottiTitleText.text = RebirthManager.GetIllumisnottiTitle(rebirthCount).ToUpper();
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
            cashText.text = $"${NumberFormatter.Format(currentCash)} (${NumberFormatter.Format(cps)}/s)";
        }

        private void UpdatePointsText(double currentPoints)
        {
            if (pointsText == null)
            {
                return;
            }

            bool isRebirthActivated = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= 1;
            if (isRebirthActivated)
            {
                pointsText.text = $"{NumberFormatter.Format(currentPoints)} POINTS";
                pointsText.color = Color.white;
            }
            else
            {
                // Pre-Snotting: points are Restoration Points used for World Restoration.
                // Show the balance (not "LOCKED") so the player can see progress.
                pointsText.text = $"{NumberFormatter.Format(currentPoints)} RESTORATION PTS";
                pointsText.color = new Color(1f, 0.85f, 0.2f, 1f); // gold — matches the Snotting progress line
            }
        }

        private void OnConvertClicked()
        {
            if (convertUIController != null)
            {
                convertUIController.OpenPanel();
            }
            else
            {
                var currency = CurrencyManager.Instance;
                if (currency != null)
                {
                    currency.ConvertCashToPoints(currency.CurrentCash);
                }
            }
        }

        private void HandleRebirthCountChangedForPoints(int rebirthCount)
        {
            UpdatePointsLockState(rebirthCount);
        }

        private void UpdatePointsLockState(int rebirthCount)
        {
            bool isRebirthActivated = rebirthCount >= 1;
            if (pointsShopButton != null)
            {
                pointsShopButton.interactable = isRebirthActivated;
                var img = pointsShopButton.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = isRebirthActivated ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
                var txt = pointsShopButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.color = isRebirthActivated ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }

            var currency = CurrencyManager.Instance;
            double currentPoints = currency != null ? currency.CurrentPoints : 0d;
            UpdatePointsText(currentPoints);
        }

        private void UpdateRestorationProgressText(double cumulativePointsSpent)
        {
            if (restorationProgressText == null)
            {
                return;
            }

            var worldRestoration = WorldRestorationManager.Instance;
            double percent = worldRestoration != null ? worldRestoration.RestorationPercent : 0d;
            string stageName = worldRestoration != null && worldRestoration.CurrentStage != null
                ? worldRestoration.CurrentStage.stageName
                : "DYSTOPIA";

            bool snottingUnlocked = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= 1;
            double threshold = cachedRebirthUI != null ? cachedRebirthUI.SnottingUnlockThreshold : 50000d;

            if (snottingUnlocked)
            {
                restorationProgressText.text = $"{stageName.ToUpper()} ({percent:F1}% RESTORED)";
            }
            else if (cumulativePointsSpent >= threshold)
            {
                restorationProgressText.text =
                    $"{stageName.ToUpper()} ({percent:F1}% RESTORED) | <color=#00FF88><size=14>SNOTTING READY</size></color>";
            }
            else
            {
                restorationProgressText.text =
                    $"{stageName.ToUpper()} ({percent:F1}% RESTORED) | <color=#FFD700><size=14>SNOTTING LOCKED {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(threshold)}</size></color>";
            }
        }

        private void HandleRebirthCountChangedForRestorationText(int _)
        {
            var worldRestoration = WorldRestorationManager.Instance;
            double spent = worldRestoration != null ? worldRestoration.CumulativePointsSpentOnRestoration : 0d;
            UpdateRestorationProgressText(spent);
        }

        private void OnRestoreClicked()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                WorldRestorationManager.Instance?.TrySpendPointsOnRestoration(currency.CurrentPoints);
            }
            // Force an immediate button state refresh so the Snotting button goes live the
            // moment the player hits 50K — don't wait for the next event fire.
            cachedRebirthUI?.RefreshTriggerButton();
        }
    }
}
