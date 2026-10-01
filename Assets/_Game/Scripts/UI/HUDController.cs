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

        /// <summary>Scene lookup so other systems (e.g. UINudgePointer's clamp-below-the-header
        /// use via UpgradeSlotUI) can find the live HUD without their own Inspector reference.</summary>
        public static HUDController Instance => FindAnyObjectByType<HUDController>();

        /// <summary>
        /// 2026-09-30: the WHOLE header panel (CurrencyHeader -- holds BRAIN POWER, cash, and the
        /// World Restoration stage-progress line), not just the BRAIN POWER text's own rect.
        /// Clamping the FTUE nudge arrow against only the first line left the stage-progress line
        /// below it unprotected -- confirmed live ("UTOPIA ACHIEVED - 250.00M" still got covered).
        /// Derived from brainPowerCounterText's own parent rather than a new serialized field, so
        /// this needs no scene wiring. Null before this HUD is wired up.
        /// </summary>
        public RectTransform HeaderPanelRect => brainPowerCounterText != null ? brainPowerCounterText.rectTransform.parent as RectTransform : null;

        /// <summary>PlayerIQ interval between celebration beats (every 1000 points).</summary>
        private const float IQCelebrationMilestoneInterval = 1000f;
        private const float TextFlushIntervalSeconds = 0.1f;

        /// <summary>
        /// Minimum real-time gap between IQ-rise swirl bursts (added 2026-09-17). PlayerIQ can
        /// rise in small +1 increments on almost every tap while recovering toward 100 (see
        /// PlayerIQManager.RestoreIQFromTap), and a full Portal-style particle burst on every
        /// single one of those would read as visual spam rather than a celebration -- the same
        /// "give it room to breathe" pacing principle as DialogueManager's repeat-trigger
        /// cooldown. This gates AnimationController.PlayIQRiseSwirl itself, independent of
        /// PlayIQFlash's per-tap text flash (HandleTapRewardEarned below), which stays uncapped.
        /// </summary>
        private const float IQSwirlCooldownSeconds = 1.2f;

        private static readonly Color BoneWhite = new Color32(242, 240, 232, 255);
        private static readonly Color BrainPowerColor = new Color32(0, 221, 235, 255);
        private static readonly Color CashColor = new Color32(245, 197, 66, 255);
        private static readonly Color RestorationColor = new Color32(117, 240, 76, 255);
        private static readonly Color IllumisnottyColor = new Color32(201, 154, 56, 255);
        private static readonly Color SecondaryColor = new Color32(155, 168, 181, 255);
        private static readonly Color LockedColor = new Color32(89, 97, 106, 190);

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
        [Tooltip("Soft glow Image placed behind fill. Optional -- no effect if unassigned.")]
        [SerializeField] private Image restorationGlowImage;
        [Tooltip("Plunger disk Image inside the vessel; its anchoredPosition.x is lerped across the track width by the same fraction that drives restorationFillImage. Optional -- no effect if unassigned.")]
        [SerializeField] private Image restorationPlungerImage;
        [Tooltip("Non-uniform X scale applied to the plunger so its ellipse matches the 3/4-angle vessel's tube-opening ellipse. Tune live in the Inspector; reapplied on every InitializeHUD.")]
        [SerializeField] private float plungerEllipseScaleX = 0.35f;

        // 2026-10-01 restoration-bar redesign ("paint bucket" method) -- built/reused entirely at
        // runtime by BuildRestorationBar, never Inspector-serialized, so adding these fields is
        // not a scene write. See BuildRestorationBar's doc comment for the Step-0 root-cause
        // diagnosis (restorationFillImage's old built-in-UISprite sprite, not anything a mask
        // could clip away) and what the bar's fraction actually measures.
        private Image restorationTrackImage;
        private Image restorationFrameOverlayImage;
        private RectTransform restorationSheenMaskRect;
        private Image restorationSheenImage;
        private RectTransform restorationSproutRect;
        private Image restorationSproutImage;
        private bool restorationBarBuilt;
        private float restorationDisplayedFraction;
        private float restorationTargetFraction;
        private float nextRestorationSheenTime;
        private bool restorationSheenActive;
        private float restorationSheenStartTime;
        private float restorationFlashStartTime = -1f;

        private const float RestorationFillGlideRate = 1.5f; // fraction/sec, Mathf.MoveTowards
        private const float RestorationFlashDuration = 0.15f;
        private const float RestorationSheenInterval = 4f;
        private const float RestorationSheenSweepDuration = 0.8f;
        private const float RestorationSproutBobAmplitude = 2f;
        private const float RestorationSproutBobPeriod = 2f;

        private static readonly Color RestorationLime = new Color32(0x39, 0xFF, 0x14, 0xFF);
        private static readonly Color RestorationFlashColor = new Color32(0x80, 0xF4, 0xFF, 0xFF);

        [Header("High-IQ Celebration")]
        [Tooltip("Optional. CanvasGroup on the root HUD canvas, pulsed during the celebration beat.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("Optional. Full-screen Image (alpha 0 at rest) used for the cyan tint and white flash.")]
        [SerializeField] private Image celebrationFlashOverlay;

        private int lastIQMilestoneIndex;
        private float nextTextFlushTime;
        private float lastKnownPlayerIQ = -1f;
        private float nextIQSwirlAllowedTime;

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

        public Image RestorationGlowImage
        {
            get => restorationGlowImage;
            set => restorationGlowImage = value;
        }

        public void ForceUpdatePointsLockState(int rebirthCount)
        {
            UpdatePointsLockState(rebirthCount);
        }

        private void Start()
        {
            ApplyVisualStyle();

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
            ApplyVisualStyle();

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
                // Seeded to the current value before the first UpdatePlayerIQText call below, so
                // that call's rise-check sees "no change" and doesn't fire a swirl on scene load.
                lastKnownPlayerIQ = playerIQManager.PlayerIQ;
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
                // 2026-10-01: HandleRestorationMilestone's plunger/vessel "surge to full, jolt,
                // settle" animation is retired along with the rest of the old vessel-era bar --
                // it drove restorationFillImage.fillAmount/color directly via DOTween, which would
                // fight TickRestorationBarAnimation's own per-frame MoveTowards smoothing. Left
                // unsubscribed rather than deleted (method body + ComputePlungerTargetX untouched)
                // in case a future pass wants a stage-crossing beat on the new bar.
                worldRestoration.OnRestorationStageChanged -= HandleRestorationStageChangedForLabel;
                worldRestoration.OnRestorationStageChanged += HandleRestorationStageChangedForLabel;
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
            // Unconditional, every frame -- the restoration bar's glide/sheen/sprout/flash need
            // real per-frame granularity, unlike the throttled numeric-text flush below.
            TickRestorationBarAnimation();

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
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleRestorationStageChangedForLabel;
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
                    playerIQText.text = $"IQ: {playerIQ:F0} <color=#00DDEB>OVERCHARGED</color>";
                else
                    playerIQText.text = $"IQ: {playerIQ:F0}";
            }

            // Portal-style swirl on a genuine rise only (never on offline/overcharge decay ticks
            // downward), rate-limited so a string of +1 tap-recovery gains doesn't spawn a burst
            // per tap -- see IQSwirlCooldownSeconds.
            if (playerIQ > lastKnownPlayerIQ && Time.unscaledTime >= nextIQSwirlAllowedTime && playerIQText != null)
            {
                nextIQSwirlAllowedTime = Time.unscaledTime + IQSwirlCooldownSeconds;
                AnimationController.PlayIQRiseSwirl(playerIQText.rectTransform);
            }
            lastKnownPlayerIQ = playerIQ;

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
        /// Fixes the restoration-name lag reported in live QA (CODEX_VISUAL_STYLE_HANDOFF_2026-09-15.md,
        /// P0 bug): WorldRestorationManager fires OnRestorationProgressChanged (which last set
        /// restorationProgressText via UpdateRestorationProgressText) before it updates CurrentStage,
        /// so a spend that crosses a stage threshold briefly shows the previous stage's name until the
        /// next progress change. Re-runs only the label text once CurrentStage has actually updated --
        /// deliberately calls RefreshRestorationProgressLabel rather than UpdateRestorationProgressText,
        /// so this does not replay the fill/glow/plunger gain-pulse animation, which already played once
        /// for this same points-spent change.
        /// </summary>
        private void HandleRestorationStageChangedForLabel(WorldRestorationStage _)
        {
            var worldRestoration = WorldRestorationManager.Instance;
            if (worldRestoration == null)
            {
                return;
            }

            RefreshRestorationProgressLabel(worldRestoration.CumulativePointsSpentOnRestoration);
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

            float startX = restorationFillImage.rectTransform.offsetMin.x;
            float cavityWidth = restorationFillImage.rectTransform.rect.width;
            float plungerVisualWidth = restorationPlungerImage.rectTransform.rect.width * restorationPlungerImage.rectTransform.localScale.x;
            float travelRange = Mathf.Max(0f, cavityWidth - plungerVisualWidth);
            return startX + Mathf.Lerp(0f, travelRange, fraction);
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
                    img.color = isRebirthActivated ? RestorationColor : LockedColor;
                }
                var txt = pointsShopButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.color = isRebirthActivated ? RestorationColor : LockedColor;
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

            if (restorationFillImage != null)
            {
                // 2026-10-01: no longer touches fillAmount/color directly -- TickRestorationBarAnimation
                // owns the actual glide toward this target every frame (Mathf.MoveTowards). This
                // only records the new target and triggers the fixed-color gain flash on increase.
                float fraction = worldRestoration != null ? worldRestoration.StageProgressFraction : 0f;

                if (fraction > restorationTargetFraction)
                {
                    restorationFlashStartTime = Time.unscaledTime;
                }

                restorationTargetFraction = fraction;
            }

            RefreshRestorationProgressLabel(cumulativePointsSpent);
        }

        /// <summary>
        /// Sets restorationProgressText's text only (stage name, percent, Snotting gate state) --
        /// split out of UpdateRestorationProgressText so HandleRestorationStageChangedForLabel can
        /// re-run just this part after WorldRestorationManager.CurrentStage actually changes, without
        /// re-touching restorationFillImage/restorationGlowImage/restorationPlungerImage or replaying
        /// their animations, which already ran once for this same points-spent change.
        /// </summary>
        private void RefreshRestorationProgressLabel(double cumulativePointsSpent)
        {
            if (restorationProgressText == null)
            {
                return;
            }

            var worldRestoration = WorldRestorationManager.Instance;
            double percent = worldRestoration != null ? worldRestoration.RestorationPercent : 0d;
            double finalThreshold = worldRestoration != null && worldRestoration.Stages.Count > 0 ? worldRestoration.Stages[worldRestoration.Stages.Count - 1].pointsRequired : 0d;
            string stageName = worldRestoration != null && worldRestoration.CurrentStage != null
                ? worldRestoration.CurrentStage.stageName
                : "DYSTOPIA";

            bool snottingUnlocked = RebirthManager.Instance != null && RebirthManager.Instance.RebirthCount >= 1;
            // Fails closed: if RebirthManager isn't resolved, there's no way to verify the real
            // gate, so use a sentinel no cumulativePointsSpent value can ever reach rather than a
            // stale copy of the last-known threshold -- showing "SNOTTING READY" without being
            // able to confirm it is exactly the bug this threshold got centralized to prevent.
            double threshold = RebirthManager.Instance != null ? RebirthManager.Instance.SnottingUnlockThreshold : double.PositiveInfinity;

            if (snottingUnlocked)
            {
                restorationProgressText.text =
                    $"{stageName.ToUpper()} — {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(finalThreshold)} ({percent:F1}%)";
            }
            else if (cumulativePointsSpent >= threshold)
            {
                restorationProgressText.text =
                    $"{stageName.ToUpper()} — {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(finalThreshold)} ({percent:F1}%) | <color=#75F04C><size=16>SNOTTING READY</size></color>";
            }
            else
            {
                restorationProgressText.text =
                    $"{stageName.ToUpper()} — {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(finalThreshold)} ({percent:F1}%) | <color=#C99A38><size=16>SNOTTING LOCKED {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(threshold)}</size></color>";
            }
        }

        /// <summary>
        /// Applies the visual guide's semantic palette without changing scene geometry or HUD
        /// update ownership. Numeric formatters continue to own only their displayed values.
        /// </summary>
        private void ApplyVisualStyle()
        {
            SetTextColor(capacityText, SecondaryColor);
            SetTextColor(playerIQText, BoneWhite);
            SetTextColor(rankText, BoneWhite);
            SetTextColor(illumisnottyTitleText, IllumisnottyColor);
            SetTextColor(brainPowerCounterText, BrainPowerColor);
            SetTextColor(cumulativeBrainPowerCounterText, SecondaryColor);
            SetTextColor(rebirthCountText, IllumisnottyColor);
            SetTextColor(bppsText, BrainPowerColor);
            SetTextColor(cashText, CashColor);
            SetTextColor(pointsText, RestorationColor);
            SetTextColor(restorationProgressText, BoneWhite);

            // 2026-10-01: the old direct fill/glow/plunger color assignments here are retired
            // along with the rest of the vessel-era bar -- BuildRestorationBar owns all of the
            // new bar's one-time setup (sprites, child layers, colors), and internally calls
            // EnsureRestorationBarClipped itself.
            BuildRestorationBar();
            EnforceMinimumFontSizes();
        }

        /// <summary>
        /// 2026-10-01 (item 8): HUD font sizes are otherwise entirely scene-baked -- neither
        /// HUDNumericFormatter nor this controller's update methods ever touch fontSize/
        /// fontSizeMin/fontSizeMax, they only set text content and color -- so a handful of
        /// secondary/tertiary labels shipped with autosize floors well under the visual guide's
        /// "nothing under 20, secondary labels >= 24, primary BP/cash numbers >= 36" rule. Fixed
        /// here (no .unity scene write) rather than in the scene file, following the same
        /// override-at-ApplyVisualStyle-time pattern as EnsureRestorationBarClipped above. Only
        /// the labels found to actually violate the floor are touched -- brainPowerCounterText,
        /// playerIQText, capacityText, and pointsText were already compliant in the scene and are
        /// left at their authored values.
        /// </summary>
        private void EnforceMinimumFontSizes()
        {
            RaiseFontFloor(rankText, 24f, 26f);
            RaiseFontFloor(illumisnottyTitleText, 24f, 26f);
            RaiseFixedFontSize(cumulativeBrainPowerCounterText, 24f);
            RaiseFixedFontSize(rebirthCountText, 24f);
            // BPPS used a different font and a 24pt floor beside the IQ label's 34pt bold
            // type. Match the IQ typography, not merely the minimum readability threshold.
            // Both share the header's scale; the BPPS row is 42 units high (IQ is 40).
            if (bppsText != null && playerIQText != null)
            {
                bppsText.font = playerIQText.font;
                bppsText.fontStyle = playerIQText.fontStyle;
                bppsText.enableAutoSizing = false;
                bppsText.fontSize = Mathf.Max(24f, playerIQText.enableAutoSizing
                    ? playerIQText.fontSizeMax : playerIQText.fontSize);
                bppsText.textWrappingMode = TextWrappingModes.NoWrap;
            }
            else
            {
                RaiseFixedFontSize(bppsText, 34f);
            }
            RaiseFontFloor(restorationProgressText, 24f, 26f);

            // cashText is a primary currency number per the visual guide ("primary numbers (BP,
            // cash) >= 36") -- its scene autosize range (14-18, base 36) meant it could never
            // actually render at 36 in practice.
            RaiseFontFloor(cashText, 36f, 40f);
        }

        private static void RaiseFontFloor(TextMeshProUGUI label, float minFloor, float maxFloor)
        {
            if (label == null || !label.enableAutoSizing) { return; }
            if (label.fontSizeMin < minFloor) { label.fontSizeMin = minFloor; }
            if (label.fontSizeMax < maxFloor) { label.fontSizeMax = maxFloor; }
        }

        private static void RaiseFixedFontSize(TextMeshProUGUI label, float minSize)
        {
            if (label == null) { return; }
            if (label.enableAutoSizing)
            {
                RaiseFontFloor(label, minSize, minSize);
                return;
            }
            if (label.fontSize < minSize) { label.fontSize = minSize; }
        }

        /// <summary>
        /// 2026-10-01 restoration-bar redesign ("paint bucket" method, Aceyfer's explicit spec).
        ///
        /// STEP 0 ROOT CAUSE: restorationFillImage's scene-authored sprite was Unity's built-in
        /// "UISprite" (fileID 10905, guid 0000000000000000f000000000000000 -- confirmed by
        /// reading the live scene YAML directly) under Image.Type.Filled. That sprite carries
        /// 9-slice border metadata meant for Simple/Sliced buttons; a bordered sprite's Filled
        /// mesh generator can leave a soft sliver of its own border/corner geometry visible
        /// regardless of fillAmount -- exactly the "spills past its edges, even at 0 points"
        /// symptom, and a sprite-shape defect rendering INSIDE the track's own rect, which is
        /// why EnsureRestorationBarClipped's RectMask2D (commit c8a30eb, kept below) never
        /// changed what actually rendered. Compounding it: the Track GameObject's own background
        /// Image was scene-authored disabled (m_Enabled: 0), so there was no crisp rectangular
        /// frame to visually contain the fill -- it read as a free-floating blob rather than a
        /// bar. restorationGlowImage/restorationPlungerImage are both already unwired in the live
        /// scene (fileID: 0 on HUDController's own serialized fields) and play no part in this;
        /// both are defensively disabled below anyway in case a future wiring pass reintroduces
        /// either.
        ///
        /// STEP 0, what the bar measures: fillAmount tracks WorldRestorationManager.
        /// StageProgressFraction -- PER-STAGE progress (0-1 within the current World Restoration
        /// stage segment: (cumulativePointsSpent - currentStage.pointsRequired) / (nextStage.
        /// pointsRequired - currentStage.pointsRequired)). This is a DIFFERENT number from
        /// restorationProgressText's "(NN.N%)", which is RestorationPercent -- TOTAL cumulative
        /// progress toward the FINAL stage's threshold. The bar resets to empty every time a new
        /// stage begins; the percent in the text keeps climbing toward 100 exactly once, at the
        /// very end.
        ///
        /// FIX: restorationFillImage's sprite is replaced with a purpose-built capsule Fill
        /// sprite (Assets/Resources/UI/RestorationBar/RestorationBar_Fill.png, see
        /// RestorationBarArtGenerator) that is pixel-identical to the matching Frame sprite's
        /// hole BY CONSTRUCTION -- flood-filled from the frame texture itself, not re-derived from
        /// a separate formula -- so it is geometrically incapable of drawing outside the frame's
        /// own hole shape. Layers, in sibling order under the Track: Fill (reused
        /// restorationFillImage, untouched RectTransform) / Sheen (RectMask2D'd to the live fill
        /// width) / FrameOverlay (FrameOutlineOnly redraws the rim on top, transparent center) /
        /// Sprout (last, rides the leading edge, drawn above everything). Built/reused here
        /// entirely at runtime via Resources.Load -- no .unity write, no new Inspector fields.
        /// Idempotent (Transform.Find before creating), safe to call repeatedly from
        /// ApplyVisualStyle.
        /// </summary>
        private void BuildRestorationBar()
        {
            if (restorationFillImage == null) { return; }

            Transform track = restorationFillImage.transform.parent;
            if (track == null) { return; }

            Sprite frameSprite = Resources.Load<Sprite>("UI/RestorationBar/RestorationBar_Frame");
            Sprite frameOutlineSprite = Resources.Load<Sprite>("UI/RestorationBar/RestorationBar_FrameOutlineOnly");
            Sprite fillSprite = Resources.Load<Sprite>("UI/RestorationBar/RestorationBar_Fill");
            Sprite sheenSprite = Resources.Load<Sprite>("UI/RestorationBar/RestorationBar_Sheen");
            Sprite sproutSprite = Resources.Load<Sprite>("UI/RestorationBar/RestorationBar_Sprout");

            if (frameSprite == null || frameOutlineSprite == null || fillSprite == null)
            {
                Debug.LogWarning("[HUDController] Restoration bar art missing from Resources/UI/RestorationBar -- run BrainDrain > Tools > Generate Restoration Bar Art.");
                return;
            }

            RectTransform trackRect = track.GetComponent<RectTransform>();
            LayoutElement trackLayout = track.GetComponent<LayoutElement>();
            // trackRect.rect.height can read 0 before the first layout pass settles (same
            // zero-size-on-first-frame gotcha as UpgradeSlotUI's header clamp) -- the serialized
            // LayoutElement.preferredHeight is available immediately and doesn't depend on a live
            // layout rebuild, so prefer it whenever it's actually set.
            float barHeight = trackLayout != null && trackLayout.preferredHeight > 0f
                ? trackLayout.preferredHeight
                : Mathf.Max(1f, trackRect != null ? trackRect.rect.height : 96f);

            // Reuse the scene's existing (previously disabled, spriteless) track Image as Frame.
            restorationTrackImage = track.GetComponent<Image>();
            if (restorationTrackImage != null)
            {
                restorationTrackImage.enabled = true;
                restorationTrackImage.sprite = frameSprite;
                restorationTrackImage.type = Image.Type.Simple;
                restorationTrackImage.preserveAspect = false;
                restorationTrackImage.color = Color.white; // art is pre-colored; no tint
                restorationTrackImage.raycastTarget = false;
            }

            // Fill reuses the existing restorationFillImage GameObject/RectTransform as-is -- only
            // its sprite/type/color change here. fillAmount itself is driven every frame by
            // TickRestorationBarAnimation, never set directly in this one-time setup.
            restorationFillImage.sprite = fillSprite;
            restorationFillImage.type = Image.Type.Filled;
            restorationFillImage.fillMethod = Image.FillMethod.Horizontal;
            restorationFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            restorationFillImage.fillClockwise = true;
            restorationFillImage.preserveAspect = false;
            restorationFillImage.color = RestorationLime;
            restorationFillImage.raycastTarget = false;
            restorationDisplayedFraction = restorationFillImage.fillAmount;
            restorationTargetFraction = restorationDisplayedFraction;

            // Sheen: a RectMask2D'd child whose width is kept equal to the live fill width every
            // frame (TickRestorationBarAnimation), with a small sweeping highlight Image inside
            // it so the sweep can never show past the real fill edge.
            Transform sheenMaskTransform = track.Find("RestorationSheenMask");
            GameObject sheenMaskObject = sheenMaskTransform != null
                ? sheenMaskTransform.gameObject
                : new GameObject("RestorationSheenMask", typeof(RectTransform));
            sheenMaskObject.transform.SetParent(track, false);
            restorationSheenMaskRect = sheenMaskObject.GetComponent<RectTransform>();
            restorationSheenMaskRect.anchorMin = new Vector2(0f, 0f);
            restorationSheenMaskRect.anchorMax = new Vector2(0f, 1f);
            restorationSheenMaskRect.pivot = new Vector2(0f, 0.5f);
            restorationSheenMaskRect.anchoredPosition = Vector2.zero;
            restorationSheenMaskRect.sizeDelta = new Vector2(0f, 0f);
            if (sheenMaskObject.GetComponent<RectMask2D>() == null) { sheenMaskObject.AddComponent<RectMask2D>(); }

            Transform sheenTransform = sheenMaskObject.transform.Find("RestorationSheen");
            GameObject sheenObject = sheenTransform != null
                ? sheenTransform.gameObject
                : new GameObject("RestorationSheen", typeof(RectTransform), typeof(Image));
            sheenObject.transform.SetParent(sheenMaskObject.transform, false);
            restorationSheenImage = sheenObject.GetComponent<Image>();
            restorationSheenImage.sprite = sheenSprite;
            restorationSheenImage.type = Image.Type.Simple;
            restorationSheenImage.preserveAspect = false;
            restorationSheenImage.raycastTarget = false;
            restorationSheenImage.color = Color.white;
            restorationSheenImage.enabled = false;
            RectTransform sheenRect = restorationSheenImage.rectTransform;
            sheenRect.anchorMin = new Vector2(0f, 0.5f);
            sheenRect.anchorMax = new Vector2(0f, 0.5f);
            sheenRect.pivot = new Vector2(0.5f, 0.5f);
            sheenRect.sizeDelta = new Vector2(160f, barHeight);

            // Frame overlay: FrameOutlineOnly redraws the rim on top of Fill/Sheen (transparent
            // center) so the fill edge looks tucked under it without covering the fill itself.
            Transform overlayTransform = track.Find("RestorationFrameOverlay");
            GameObject overlayObject = overlayTransform != null
                ? overlayTransform.gameObject
                : new GameObject("RestorationFrameOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(track, false);
            restorationFrameOverlayImage = overlayObject.GetComponent<Image>();
            restorationFrameOverlayImage.sprite = frameOutlineSprite;
            restorationFrameOverlayImage.type = Image.Type.Simple;
            restorationFrameOverlayImage.preserveAspect = false;
            restorationFrameOverlayImage.raycastTarget = false;
            restorationFrameOverlayImage.color = Color.white;
            RectTransform overlayRect = restorationFrameOverlayImage.rectTransform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Sprout: rides the leading edge of the fill, drawn above everything else (last sibling).
            Transform sproutTransform = track.Find("RestorationSprout");
            GameObject sproutObject = sproutTransform != null
                ? sproutTransform.gameObject
                : new GameObject("RestorationSprout", typeof(RectTransform), typeof(Image));
            sproutObject.transform.SetParent(track, false);
            restorationSproutImage = sproutObject.GetComponent<Image>();
            restorationSproutImage.sprite = sproutSprite;
            restorationSproutImage.type = Image.Type.Simple;
            restorationSproutImage.preserveAspect = true;
            restorationSproutImage.raycastTarget = false;
            restorationSproutImage.color = Color.white;
            restorationSproutRect = restorationSproutImage.rectTransform;
            float sproutSize = barHeight * 1.2f;
            restorationSproutRect.anchorMin = new Vector2(0f, 0.5f);
            restorationSproutRect.anchorMax = new Vector2(0f, 0.5f);
            restorationSproutRect.pivot = new Vector2(0.5f, 0.5f);
            restorationSproutRect.sizeDelta = new Vector2(sproutSize, sproutSize);

            sheenMaskObject.transform.SetSiblingIndex(1);
            overlayObject.transform.SetSiblingIndex(2);
            sproutObject.transform.SetSiblingIndex(3);

            // Defensively retire the old vessel-era graphics (see diagnosis above) -- both are
            // already unwired/null in the live scene; this only matters if a future wiring pass
            // reintroduces either.
            if (restorationGlowImage != null) { restorationGlowImage.enabled = false; }
            if (restorationPlungerImage != null) { restorationPlungerImage.enabled = false; }

            EnsureRestorationBarClipped();

            restorationBarBuilt = true;
        }

        /// <summary>
        /// Every-frame glide/sheen/sprout/flash driver for the new bar, called unconditionally
        /// from LateUpdate (not gated by the throttled numeric-text flush -- these need real
        /// per-frame granularity to read as a glide/sweep rather than a series of steps).
        /// </summary>
        private void TickRestorationBarAnimation()
        {
            if (!restorationBarBuilt || restorationFillImage == null) { return; }

            float dt = Time.unscaledDeltaTime;
            float previousDisplayed = restorationDisplayedFraction;
            restorationDisplayedFraction = Mathf.MoveTowards(restorationDisplayedFraction, restorationTargetFraction, RestorationFillGlideRate * dt);
            restorationFillImage.fillAmount = restorationDisplayedFraction;

            float trackWidth = restorationFillImage.rectTransform.rect.width;
            float fillWidthPixels = trackWidth * restorationDisplayedFraction;

            // Gain flash: fixed palette flash (#80F4FF -> lime), triggered once per target
            // increase in UpdateRestorationProgressText, not re-triggered every glide frame.
            if (restorationFlashStartTime >= 0f)
            {
                float flashT = Mathf.Clamp01((Time.unscaledTime - restorationFlashStartTime) / RestorationFlashDuration);
                restorationFillImage.color = Color.Lerp(RestorationFlashColor, RestorationLime, flashT);
                if (flashT >= 1f) { restorationFlashStartTime = -1f; }
            }

            if (restorationSheenMaskRect != null)
            {
                restorationSheenMaskRect.sizeDelta = new Vector2(fillWidthPixels, 0f);
            }

            if (restorationDisplayedFraction <= 0.0001f)
            {
                restorationSheenActive = false;
                if (restorationSheenImage != null) { restorationSheenImage.enabled = false; }
            }
            else
            {
                if (!restorationSheenActive && Time.unscaledTime >= nextRestorationSheenTime)
                {
                    restorationSheenActive = true;
                    restorationSheenStartTime = Time.unscaledTime;
                    nextRestorationSheenTime = restorationSheenStartTime + RestorationSheenInterval;
                    if (restorationSheenImage != null) { restorationSheenImage.enabled = true; }
                }

                if (restorationSheenActive)
                {
                    float sweepT = Mathf.Clamp01((Time.unscaledTime - restorationSheenStartTime) / RestorationSheenSweepDuration);
                    if (restorationSheenImage != null)
                    {
                        RectTransform sheenRect = restorationSheenImage.rectTransform;
                        Vector2 pos = sheenRect.anchoredPosition;
                        pos.x = Mathf.Lerp(0f, fillWidthPixels, sweepT);
                        sheenRect.anchoredPosition = pos;
                    }

                    if (sweepT >= 1f)
                    {
                        restorationSheenActive = false;
                        if (restorationSheenImage != null) { restorationSheenImage.enabled = false; }
                    }
                }
            }

            // Sprout rides the leading edge; hidden at the extremes, bobbing gently in between.
            bool sproutVisible = restorationDisplayedFraction > 0.001f && restorationDisplayedFraction < 0.999f;
            if (restorationSproutImage != null)
            {
                restorationSproutImage.enabled = sproutVisible;
                if (sproutVisible && restorationSproutRect != null)
                {
                    float sproutHalf = restorationSproutRect.sizeDelta.x * 0.5f;
                    float clampedX = Mathf.Clamp(fillWidthPixels, sproutHalf, Mathf.Max(sproutHalf, trackWidth - sproutHalf));
                    float bobY = Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / RestorationSproutBobPeriod)) * RestorationSproutBobAmplitude;
                    restorationSproutRect.anchoredPosition = new Vector2(clampedX, bobY);
                }
            }

            // At 100% the sprout hands off to a one-shot brightness pulse on the fill instead
            // (reuses the existing gain-pulse tween -- "gentle brightness pulse" per spec).
            if (previousDisplayed < 0.999f && restorationDisplayedFraction >= 0.999f)
            {
                AnimationController.PlayRestorationGainPulse(restorationFillImage, null, null);
            }
        }

        /// <summary>
        /// 2026-09-30 (item 6): "green blob over the restoration bar at Stage 5" -- traced live:
        /// restorationGlowImage is unwired in the current scene (RestorationBarGlow, the object
        /// RestorationBarWireFix.cs would build, does not exist there either), so the actual
        /// on-screen culprit is restorationFillImage itself. Its parent (RestorationBarTrack) has
        /// no RectMask2D, so nothing stops a Type.Filled Image's own rect from rendering outside
        /// the track's visual bounds if its geometry doesn't match exactly -- at high fill
        /// fraction (Stage 5) with alpha lerped up to 1.0 (UpdateRestorationProgressText), a fully
        /// opaque bright-green rect poking past the track reads as a smudge/blob rather than a
        /// clean bar edge. Adding a RectMask2D to the track clips the fill (and restorationGlowImage
        /// or the plunger, if either is ever added as a sibling child) to the track's own rect,
        /// guaranteed, regardless of the fill Image's own sprite/geometry. Idempotent -- checked
        /// every ApplyVisualStyle() call, safe to call repeatedly.
        /// </summary>
        private void EnsureRestorationBarClipped()
        {
            if (restorationFillImage == null) { return; }

            Transform track = restorationFillImage.transform.parent;
            if (track == null) { return; }

            if (track.GetComponent<RectMask2D>() == null)
            {
                track.gameObject.AddComponent<RectMask2D>();
            }
        }

        private static void SetTextColor(TextMeshProUGUI label, Color color)
        {
            if (label != null)
            {
                label.color = color;
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
