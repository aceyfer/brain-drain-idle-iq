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
        private const float TextFlushIntervalSeconds = 0.1f;

        [Header("UI Text Fields")]
        [SerializeField] private TextMeshProUGUI capacityText;
        [FormerlySerializedAs("iqText")]
        [FormerlySerializedAs("worldRestorationText")]
        [SerializeField] private TextMeshProUGUI playerIQText;
        [SerializeField] private TextMeshProUGUI rankText;
        [Tooltip("Illumisnotty title earned at the current Snotting (Rebirth) tier -- displayed under the IQ readout. Blank until the first Snotting. Added 2026-06-21.")]
        [FormerlySerializedAs("illumisnottiTitleText")]
        [SerializeField] private TextMeshProUGUI illumisnottyTitleText;
        [FormerlySerializedAs("brainsCounterText")]
        [SerializeField] private TextMeshProUGUI brainPowerCounterText;
        [SerializeField] private TextMeshProUGUI cumulativeBrainPowerCounterText;
        [SerializeField] private TextMeshProUGUI rebirthCountText;
        [SerializeField] private TextMeshProUGUI bppsText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI restorationProgressText;

        [Header("Points Locking Visibility")]
        [SerializeField] private Button pointsShopButton;

        [Header("World Restoration")]
        [Tooltip("Spends all current Points on World Restoration when clicked.")]
        [SerializeField] private Button restoreButton;
        [Tooltip("Fill-type Image showing progress toward the next World Restoration stage threshold. Optional -- no effect if unassigned.")]
        [SerializeField] private Image restorationFillImage;
        [Tooltip("Plunger disk Image inside the vessel; its anchoredPosition.x is lerped across the track width by the same fraction that drives restorationFillImage. Optional -- no effect if unassigned.")]
        [SerializeField] private Image restorationPlungerImage;
        [Tooltip("Non-uniform X scale applied to the plunger so its ellipse matches the 3/4-angle vessel's tube-opening ellipse. Tune live in the Inspector; reapplied on every InitializeHUD.")]
        [SerializeField] private float plungerEllipseScaleX = 0.35f;

        [Header("High-IQ Celebration")]
        [Tooltip("Optional. CanvasGroup on the root HUD canvas, pulsed during the celebration beat.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("Optional. Full-screen Image (alpha 0 at rest) used for the cyan tint and white flash.")]
        [SerializeField] private Image celebrationFlashOverlay;

        private int lastIQMilestoneIndex;
        private float nextTextFlushTime;

        private bool dirtyCapacity;
        private bool dirtyBrainPower;
        private bool dirtyCumulativeBrainPower;
        private bool dirtyBpps;
        private bool dirtyCash;
        private bool dirtyPoints;
        private bool dirtyRank;

        private double pendingBrainPower;
        private double pendingCumulativeBrainPower;
        private double pendingCash;
        private double pendingPoints;
        private bool pendingPointsRebirthActivated;

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

        public TextMeshProUGUI IllumisnottyTitleText
        {
            get => illumisnottyTitleText;
            set => illumisnottyTitleText = value;
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

        public Image RestorationFillImage
        {
            get => restorationFillImage;
            set => restorationFillImage = value;
        }

        public void ForceUpdatePointsLockState(int rebirthCount)
        {
            UpdatePointsLockState(rebirthCount);
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
                MarkRankDirty();
                UpdateBrainPowerCounterText(currency.BrainPower);
                UpdateCumulativeBrainPowerCounterText(currency.CumulativeBrainPower);
                UpdateBPPSText();
                UpdateCashText(currency.CurrentCash);
                UpdatePointsText(currency.CurrentPoints);
                currency.OnBrainPowerChanged += UpdateCapacityText;
                currency.OnBrainPowerChanged += UpdateBrainPowerCounterText;
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
                UpdateIllumisnottyTitleText(RebirthManager.Instance.RebirthCount);
                RebirthManager.Instance.OnRebirthCountChanged += UpdateIllumisnottyTitleText;
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

            if (restorationPlungerImage != null)
            {
                Vector3 scale = restorationPlungerImage.rectTransform.localScale;
                scale.x = plungerEllipseScaleX;
                restorationPlungerImage.rectTransform.localScale = scale;
            }

            var worldRestoration = WorldRestorationManager.Instance;
            if (worldRestoration != null)
            {
                UpdateRestorationProgressText(worldRestoration.CumulativePointsSpentOnRestoration);
                worldRestoration.OnRestorationProgressChanged -= UpdateRestorationProgressText;
                worldRestoration.OnRestorationProgressChanged += UpdateRestorationProgressText;
                worldRestoration.OnRestorationStageChanged -= HandleStageChangedForRank;
                worldRestoration.OnRestorationStageChanged += HandleStageChangedForRank;
                worldRestoration.OnRestorationStageChanged -= HandleRestorationMilestone;
                worldRestoration.OnRestorationStageChanged += HandleRestorationMilestone;
            }

            // Re-evaluate the restoration text when the player performs their first Snotting,
            // since that removes the "unlock progress" line from the display.
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChangedForRestorationText;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChangedForRestorationText;
            }

            // Always sync rank on cold boot regardless of whether CurrencyManager was ready above.
            MarkRankDirty();
            FlushDirtyTexts();
        }

        private void LateUpdate()
        {
            if (!HasDirtyNumericText())
            {
                return;
            }

            if (Time.unscaledTime < nextTextFlushTime)
            {
                return;
            }

            nextTextFlushTime = Time.unscaledTime + TextFlushIntervalSeconds;
            FlushDirtyTexts();
        }

        private bool HasDirtyNumericText()
        {
            return dirtyCapacity || dirtyBrainPower || dirtyCumulativeBrainPower || dirtyBpps
                || dirtyCash || dirtyPoints || dirtyRank;
        }

        private void FlushDirtyTexts()
        {
            if (dirtyCapacity)
            {
                dirtyCapacity = false;
                HUDNumericFormatter.SetCapacity(capacityText, pendingBrainPower);
            }

            if (dirtyBrainPower)
            {
                dirtyBrainPower = false;
                HUDNumericFormatter.SetBrainPowerCounter(brainPowerCounterText, pendingBrainPower);
            }

            if (dirtyCumulativeBrainPower)
            {
                dirtyCumulativeBrainPower = false;
                HUDNumericFormatter.SetCumulativeBrainPower(cumulativeBrainPowerCounterText, pendingCumulativeBrainPower);
            }

            if (dirtyRank)
            {
                dirtyRank = false;
                int stageIdx = WorldRestorationManager.Instance?.CurrentStage?.stageIndex ?? 0;
                HUDNumericFormatter.SetRank(rankText, GetStageRankTitle(stageIdx));
            }

            if (dirtyBpps)
            {
                dirtyBpps = false;
                CurrencyManager currency = CurrencyManager.Instance;
                if (currency != null)
                {
                    bool throttled = DailyEngagementCapManager.Instance != null && DailyEngagementCapManager.Instance.IsThrottled;
                    HUDNumericFormatter.SetBpps(bppsText, currency.IdleBPPS, throttled);
                }
            }

            if (dirtyCash)
            {
                dirtyCash = false;
                CurrencyManager currency = CurrencyManager.Instance;
                double cps = currency != null ? currency.CashPerSecond : 0d;
                bool throttled = DailyEngagementCapManager.Instance != null && DailyEngagementCapManager.Instance.IsThrottled;
                HUDNumericFormatter.SetCash(cashText, pendingCash, cps, throttled);
            }

            if (dirtyPoints)
            {
                dirtyPoints = false;
                HUDNumericFormatter.SetPoints(pointsText, pendingPoints, pendingPointsRebirthActivated);
            }
        }

        private void UnsubscribeFromEvents()
        {
            var currency = CurrencyManager.Instance;
            if (currency != null)
            {
                currency.OnBrainPowerChanged -= UpdateCapacityText;
                currency.OnBrainPowerChanged -= UpdateBrainPowerCounterText;
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
                RebirthManager.Instance.OnRebirthCountChanged -= UpdateIllumisnottyTitleText;
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
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleStageChangedForRank;
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleRestorationMilestone;
            }
        }

        private void UpdateBrainPowerCounterText(double brainPower)
        {
            pendingBrainPower = brainPower;
            dirtyBrainPower = true;
        }

        private void UpdateCapacityText(double brainPower)
        {
            pendingBrainPower = brainPower;
            dirtyCapacity = true;
            dirtyBrainPower = true;
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

        private void HandleStageChangedForRank(WorldRestorationStage _)
        {
            MarkRankDirty();
        }

        /// <summary>
        /// Fires the vessel's milestone surge on a real stage crossing (stageIndex 1-5) -- guarded
        /// off stageIndex 0 so the initial boot/load resolution to the baseline stage doesn't fire
        /// it. UpdateRestorationProgressText has already run for this same change by the time this
        /// fires (OnRestorationProgressChanged is invoked before OnRestorationStageChanged in
        /// WorldRestorationManager), so restorationFillImage/restorationPlungerImage are already at
        /// the correct settled values for the new segment -- this reads those as the "settle back to"
        /// targets for the surge rather than recomputing them.
        /// </summary>
        private void HandleRestorationMilestone(WorldRestorationStage stage)
        {
            if (stage == null || stage.stageIndex < 1 || restorationFillImage == null)
            {
                return;
            }

            float settledFraction = restorationFillImage.fillAmount;

            AnimationController.PlayRestorationMilestoneSurge(
                restorationFillImage,
                restorationPlungerImage != null ? restorationPlungerImage.rectTransform : null,
                ComputePlungerTargetX(1f),
                restorationFillImage.color,
                settledFraction,
                ComputePlungerTargetX(settledFraction));
        }

        /// <summary>
        /// Plunger is pivoted on its own left edge (RestorationBarWireFix.BuildVessel), so its
        /// anchoredPosition.x range isn't [0, trackWidth] -- that would let its right edge run past
        /// the track at fraction 1. Range is [0, trackWidth - plungerVisualWidth] instead, where
        /// plungerVisualWidth accounts for plungerEllipseScaleX (applied via localScale.x, not
        /// sizeDelta) so the squashed disk's actual on-screen width is what's subtracted.
        /// </summary>
        private float ComputePlungerTargetX(float fraction)
        {
            if (restorationFillImage == null || restorationPlungerImage == null)
            {
                return 0f;
            }

            float trackWidth = restorationFillImage.rectTransform.rect.width;
            float plungerVisualWidth = restorationPlungerImage.rectTransform.rect.width * restorationPlungerImage.rectTransform.localScale.x;
            float travelRange = Mathf.Max(0f, trackWidth - plungerVisualWidth);
            return Mathf.Lerp(0f, travelRange, fraction);
        }

        private void MarkRankDirty()
        {
            dirtyRank = true;
        }

        private static string GetStageRankTitle(int stageIndex)
        {
            int rank = Mathf.Max(0, stageIndex - 1);
            return rank switch
            {
                0 => "Cryo Nobody",
                1 => "Illumisnotty Intern",
                2 => "Metrics-Compliant Drone",
                3 => "Synergy Synthesizer",
                4 => "Holistic Disruptor",
                _ => "Post-Human CEO",
            };
        }

        private void UpdateCumulativeBrainPowerCounterText(double cumulativeBrainPower)
        {
            pendingCumulativeBrainPower = cumulativeBrainPower;
            dirtyCumulativeBrainPower = true;
        }

        private void UpdateRebirthCountText(int rebirthCount)
        {
            HUDNumericFormatter.SetRebirthCount(rebirthCountText, rebirthCount);
        }

        /// <summary>Updates the Illumisnotty title shown under the IQ readout. Blank (no text) until the first Snotting.</summary>
        private void UpdateIllumisnottyTitleText(int rebirthCount)
        {
            HUDNumericFormatter.SetIllumisnottyTitle(illumisnottyTitleText, RebirthManager.GetIllumisnottyTitle(rebirthCount));
        }

        /// <summary>
        /// Pulled from CurrencyManager.IdleBPPS on every GameManager.OnSecondTick rather than
        /// pushed via a dedicated event, since idleBpps itself only changes at purchase/reset
        /// time -- this just keeps the display in sync with the tick, as the audit asked for.
        /// </summary>
        private void UpdateBPPSText()
        {
            dirtyBpps = true;
        }

        private void UpdateCashText(double currentCash)
        {
            pendingCash = currentCash;
            dirtyCash = true;
        }

        private void UpdatePointsText(double currentPoints)
        {
            pendingPoints = currentPoints;
            pendingPointsRebirthActivated = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= 1;
            dirtyPoints = true;
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

            if (restorationFillImage != null)
            {
                float fraction = worldRestoration != null ? worldRestoration.StageProgressFraction : 0f;
                restorationFillImage.fillAmount = fraction;

                // Glow scales with fill level -- plain alpha lerp, same technique as
                // AnimationController.AffordablePulseRoutine's color.a = Mathf.Lerp(...). No shader.
                Color fillColor = restorationFillImage.color;
                fillColor.a = Mathf.Lerp(0.55f, 1f, fraction);
                restorationFillImage.color = fillColor;

                if (restorationPlungerImage != null)
                {
                    AnimationController.PlayPlungerMove(restorationPlungerImage.rectTransform, ComputePlungerTargetX(fraction));
                }
            }

            bool snottingUnlocked = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= 1;
            // Fails closed: if RebirthManager isn't resolved, there's no way to verify the real
            // gate, so use a sentinel no cumulativePointsSpent value can ever reach rather than a
            // stale copy of the last-known threshold -- showing "SNOTTING READY" without being
            // able to confirm it is exactly the bug this threshold got centralized to prevent.
            double threshold = RebirthManager.Instance != null ? RebirthManager.Instance.SnottingUnlockThreshold : double.PositiveInfinity;

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
    }
}
