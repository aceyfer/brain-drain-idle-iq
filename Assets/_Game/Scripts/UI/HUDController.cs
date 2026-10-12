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

        // 2026-10-04 PALETTE LOCKDOWN: every HUD text/icon color now resolves through Palette's
        // 7 tokens -- these locally-named fields stay (readability at each SetTextColor call
        // site below) but their values are no longer independently authored.
        private static readonly Color BoneWhite = Palette.White;
        private static readonly Color BrainPowerColor = Palette.Cyan;
        // 2026-10-12 play-test fix: was Palette.Cyan -- CashText's face color combined with a
        // stray Oswald_GoldUnderlay.mat (see the fontSharedMaterial reset in EnforceMinimumFontSizes
        // below) to read greenish/olive. White face + the "/s" segment in Palette.Glow (see
        // HUDNumericFormatter.SetCash) replaces that combination entirely.
        private static readonly Color CashColor = Palette.White;
        private static readonly Color RestorationColor = Palette.Cyan;
        private static readonly Color IllumisnottyColor = Palette.Cyan;
        private static readonly Color SecondaryColor = Palette.White;
        private static readonly Color LockedColor = Palette.Dim;

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

        // 2026-10-12 scoreboard redesign -- built once by BuildScoreboardPanel, never
        // Inspector-serialized (same "own it in code" convention as the restoration bar fields
        // above). scoreboardEdgeImage is also read by the Step 4 idle glow pulse.
        private bool scoreboardPanelBuilt;
        private Image scoreboardEdgeImage;
        private float restorationDisplayedFraction;
        private float restorationTargetFraction;
        private float nextRestorationSheenTime;
        private bool restorationSheenActive;
        private float restorationSheenStartTime;
        private float restorationFlashStartTime = -1f;

        private const float RestorationFillGlideRate = 1.5f; // fraction/sec, Mathf.MoveTowards
        private const float RestorationFlashDuration = 0.15f;
        private const float RestorationSheenInterval = 4f;
        // 2026-10-12 scoreboard redesign: 0.8 -> 0.7, matching the redesign spec's "every 4s over
        // 0.7s" exactly -- the sweep mechanism itself (RestorationSheenInterval/TickRestorationBar
        // Animation below) already existed and needed no other changes.
        private const float RestorationSheenSweepDuration = 0.7f;
        private const float RestorationSproutBobAmplitude = 2f;
        private const float RestorationSproutBobPeriod = 2f;

        // 2026-10-04 PALETTE LOCKDOWN (darker restoration bar): steady fill is now Deep Cyan
        // (was Lime); the gain-flash still lands on it from Glow, same as before.
        private static readonly Color RestorationFillSteady = Palette.DeepCyan;
        private static readonly Color RestorationFlashColor = Palette.Glow;

        [Header("High-IQ Celebration")]
        [Tooltip("Optional. CanvasGroup on the root HUD canvas, pulsed during the celebration beat.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("Optional. Full-screen Image (alpha 0 at rest) used for the cyan tint and white flash.")]
        [SerializeField] private Image celebrationFlashOverlay;

        [Header("Scoreboard Redesign (2026-10-12)")]
        [Tooltip("Optional override. BrainsCounterText's glow material loads automatically from "
            + "Resources/UI/Scoreboard/Oswald_NeonBlueGlow.mat (GetScoreboardGlowMaterial) -- only "
            + "assign this if you want a DIFFERENT material without touching code.")]
        [SerializeField] private Material brainsCounterGlowMaterial;

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

        // 2026-10-12 scoreboard redesign: exponential count-up for brainPowerCounterText/cashText,
        // driven inside the existing 0.1s flush (TextFlushIntervalSeconds) rather than a new
        // per-frame tick -- the DISPLAYED value chases pendingBrainPower/pendingCash by a fixed
        // fraction each flush, instead of snapping straight to the target. Seeded to the real
        // starting value in InitializeHUD so cold boot shows the correct number immediately;
        // only live changes afterward animate.
        private const double CounterCatchUpRate = 0.35;
        private double displayedBrainPowerCounter;
        private double displayedCashCounter;

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
                // Seeded to the real starting value BEFORE the first flush -- without this, cold
                // boot would count up from 0 to currency.BrainPower/CurrentCash instead of just
                // showing the correct number immediately, since StepTowardExponential only
                // animates CHANGES from whatever displayed*Counter already holds.
                displayedBrainPowerCounter = currency.BrainPower;
                displayedCashCounter = currency.CurrentCash;
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
                // 2026-10-12 scoreboard redesign: a NEW, separate handler for the milestone spark
                // burst -- deliberately not re-subscribing HandleRestorationMilestone itself (see
                // the comment right above), since that would reintroduce the exact DOTween-vs-
                // MoveTowards fight it was unsubscribed to avoid.
                worldRestoration.OnRestorationStageChanged -= HandleRestorationMilestoneSpark;
                worldRestoration.OnRestorationStageChanged += HandleRestorationMilestoneSpark;
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
            TickScoreboardGlowPulse();

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
                displayedBrainPowerCounter = StepTowardExponential(displayedBrainPowerCounter, pendingBrainPower, CounterCatchUpRate);
                // Stays dirty (re-flushed next 0.1s tick) until the count-up actually reaches the
                // target -- a brand new currency change arriving mid-animation just retargets the
                // chase smoothly rather than resetting it.
                dirtyBrainPower = displayedBrainPowerCounter != pendingBrainPower;
                HUDNumericFormatter.SetBrainPowerCounter(brainPowerCounterText, displayedBrainPowerCounter);
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
                displayedCashCounter = StepTowardExponential(displayedCashCounter, pendingCash, CounterCatchUpRate);
                dirtyCash = displayedCashCounter != pendingCash;
                CurrencyManager currency = CurrencyManager.Instance;
                double cps = currency != null ? currency.CashPerSecond : 0d;
                bool throttled = DailyEngagementCapManager.Instance != null && DailyEngagementCapManager.Instance.IsThrottled;
                HUDNumericFormatter.SetCash(cashText, displayedCashCounter, cps, throttled);
            }

            if (dirtyPoints)
            {
                dirtyPoints = false;
                HUDNumericFormatter.SetPoints(pointsText, pendingPoints, pendingPointsRebirthActivated);
            }
        }

        /// <summary>Moves current a fixed fraction of the way toward target each call -- an
        /// exponential approach, not a linear one, so the count-up reads fast-then-settling rather
        /// than mechanical. Snaps to target once within a relative epsilon (0.05% of the target, or
        /// 0.5 absolute, whichever is larger) so huge late-game numbers actually converge instead
        /// of crawling asymptotically forever, and so a target of exactly 0 is reachable at all.</summary>
        private static double StepTowardExponential(double current, double target, double rate)
        {
            double diff = target - current;
            double epsilon = System.Math.Max(0.5, System.Math.Abs(target) * 0.0005);
            if (System.Math.Abs(diff) <= epsilon)
            {
                return target;
            }

            return current + diff * rate;
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
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleRestorationMilestoneSpark;
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

        /// <summary>2026-10-12 scoreboard redesign: milestone-only spark burst (stage unlock /
        /// restoration crossing -- WorldRestorationManager only exposes one "a stage just changed"
        /// event, so that's the single milestone this hooks). Same stageIndex &lt; 1 guard as
        /// HandleRestorationMilestone above, so the initial boot/load resolution to the baseline
        /// stage doesn't fire a burst. Deliberately NOT wired to tap events -- the existing tap
        /// particles (PlaySplatParticles/PlayExtractionParticles) are untouched.</summary>
        private void HandleRestorationMilestoneSpark(WorldRestorationStage stage)
        {
            if (stage == null || stage.stageIndex < 1)
            {
                return;
            }

            RectTransform header = brainPowerCounterText != null ? brainPowerCounterText.transform.parent as RectTransform : null;
            AnimationController.PlayMilestoneSparkBurst(header);
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
                // 2026-10-12 scoreboard redesign: dropped the inline <size=16> tag (the label's
                // own floor is 22pt, see EnforceMinimumFontSizes -- a 16pt span inside it read
                // smaller than the rest of the line) and the raw off-palette green hex (#75F04C)
                // in favor of Palette.Cyan ("NeonBlue"), matching the "ready" state's established
                // color everywhere else in this codebase (e.g. RebirthUIController's own
                // ReadyLabelColor).
                restorationProgressText.text =
                    $"{stageName.ToUpper()} — {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(finalThreshold)} ({percent:F1}%) | <color=#{ColorUtility.ToHtmlStringRGBA(Palette.Cyan)}>SNOTTING READY</color>";
            }
            else
            {
                // Same <size=16> removal, plus the raw off-palette gold hex (#C99A38) replaced
                // with Palette.Dim, the established locked/unaffordable token.
                restorationProgressText.text =
                    $"{stageName.ToUpper()} — {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(finalThreshold)} ({percent:F1}%) | <color=#{ColorUtility.ToHtmlStringRGBA(Palette.Dim)}>SNOTTING LOCKED {NumberFormatter.Format(cumulativePointsSpent)}/{NumberFormatter.Format(threshold)}</color>";
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
            BuildScoreboardPanel();
        }

        /// <summary>Cached once per domain reload -- avoids a repeated Resources.Load on every
        /// ApplyVisualStyle call (e.g. every World Restoration stage change, see HandleThemeApplied-
        /// style callers elsewhere in this project).</summary>
        private static TMP_FontAsset cachedOswaldFont;

        private static TMP_FontAsset GetOswaldFont()
        {
            if (cachedOswaldFont == null)
            {
                cachedOswaldFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            }
            return cachedOswaldFont;
        }

        /// <summary>2026-10-12 play-test fix: brainsCounterGlowMaterial required a manual Inspector
        /// assignment to ever take effect, which is a scene write -- moved the material itself
        /// (same guid, via git mv) under Resources/UI/Scoreboard so this can load it the same way
        /// GetOswaldFont loads the font above. Cached once per domain reload for the same reason.
        /// The Inspector field stays as an optional override (checked first, in
        /// ApplyScoreboardGlowMaterial below), not removed.</summary>
        private static Material cachedScoreboardGlowMaterial;

        private static Material GetScoreboardGlowMaterial()
        {
            if (cachedScoreboardGlowMaterial == null)
            {
                cachedScoreboardGlowMaterial = Resources.Load<Material>("UI/Scoreboard/Oswald_NeonBlueGlow");
            }
            return cachedScoreboardGlowMaterial;
        }

        /// <summary>2026-10-12 scoreboard redesign: every scoreboard label unified onto Oswald Bold
        /// SDF, replacing whatever mix of fonts (LiberationSans included) the scene authored.</summary>
        private void UnifyScoreboardFont()
        {
            TMP_FontAsset oswald = GetOswaldFont();
            if (oswald == null)
            {
                Debug.LogWarning("[HUDController] Oswald Bold SDF not found at Resources/Fonts & Materials -- scoreboard font left as-authored.", this);
                return;
            }

            SetFont(capacityText, oswald);
            SetFont(playerIQText, oswald);
            SetFont(rankText, oswald);
            SetFont(illumisnottyTitleText, oswald);
            SetFont(brainPowerCounterText, oswald);
            SetFont(cumulativeBrainPowerCounterText, oswald);
            SetFont(rebirthCountText, oswald);
            SetFont(bppsText, oswald);
            SetFont(cashText, oswald);
            SetFont(pointsText, oswald);
            SetFont(restorationProgressText, oswald);
        }

        private static void SetFont(TextMeshProUGUI label, TMP_FontAsset font)
        {
            if (label != null) { label.font = font; }
        }

        /// <summary>
        /// 2026-10-01 (item 8): HUD font sizes are otherwise entirely scene-baked -- neither
        /// HUDNumericFormatter nor this controller's update methods ever touch fontSize/
        /// fontSizeMin/fontSizeMax, they only set text content and color -- so a handful of
        /// secondary/tertiary labels shipped with autosize floors well under the visual guide's
        /// "nothing under 20, secondary labels >= 24, primary BP/cash numbers >= 36" rule. Fixed
        /// here (no .unity scene write) rather than in the scene file, following the same
        /// override-at-ApplyVisualStyle-time pattern as EnsureRestorationBarClipped above.
        ///
        /// 2026-10-12 scoreboard redesign: five labels now get a FIXED size (not an autosize
        /// floor/ceiling) per the redesign spec -- playerIQText, illumisnottyTitleText,
        /// brainPowerCounterText, bppsText, cashText. This supersedes the old BPPS-copies-IQ's-font
        /// block (which could repoint BPPS to whatever playerIQText's own scene-authored font
        /// happened to be, LiberationSans included) and the old 36-40pt cash floor (the original
        /// visual guide's "primary numbers >= 36" rule, replaced by this redesign's fixed 27pt).
        /// Everything else keeps its prior floor-based sizing.
        ///
        /// 2026-10-10 sizing pass: bumped all five per Aceyfer's explicit ask ("the panel has
        /// plenty of unused space... fill it"). Re-reads the pass's two seemingly-opposed lines --
        /// "keep autosize off on the counters to avoid CPU spikes" vs. "let the counters fall back
        /// to autosize... so they never overflow" -- as: default OFF for everything in this bump,
        /// EXCEPT the three magnitude-unbounded numeric counters (brainPowerCounterText/bppsText/
        /// cashText), which get autosize-with-max specifically so a value like "999.99T BRAIN
        /// POWER" shrinks to fit the header's width instead of clipping, while short everyday
        /// values still render at (or near) the new max. playerIQText/illumisnottyTitleText aren't
        /// runaway-magnitude counters, so they stay truly fixed. RebalanceScoreboardLayout (below)
        /// depends on these constants, not the labels' live fontSize, so call order relative to it
        /// doesn't matter.
        /// </summary>
        private const float IQFontSize = 40f;
        private const float IllumisnottyTitleFontSize = 34f;
        private const float BrainsCounterFontMin = 40f;
        private const float BrainsCounterFontMax = 64f;
        // 2026-10-12 play-test fix: was 26 -- too low a floor let BPPS/CashText autosize collapse
        // far below their 38pt max once TextsRow's allocated height was tight (see
        // RebalanceScoreboardLayout's rewrite below, which now guarantees that row enough height
        // for 38pt anyway, but this floor stays raised as a second line of defense).
        private const float CounterFontMin = 30f;
        private const float CounterFontMax = 38f;
        private const float RestorationFontFloor = 28f;
        private const float RestorationFontCeiling = 30f;

        private void EnforceMinimumFontSizes()
        {
            UnifyScoreboardFont();

            RaiseFontFloor(rankText, 24f, 26f);
            RaiseFixedFontSize(cumulativeBrainPowerCounterText, 24f);
            RaiseFixedFontSize(rebirthCountText, 24f);
            RaiseFontFloor(restorationProgressText, RestorationFontFloor, RestorationFontCeiling);
            if (restorationProgressText != null)
            {
                // Only label allowed to wrap (1-2 lines) -- every other scoreboard label below
                // stays single-line per the sizing pass's explicit instruction.
                restorationProgressText.textWrappingMode = TextWrappingModes.Normal;
            }

            SetFixedLabel(playerIQText, IQFontSize, TextAlignmentOptions.Left);
            SetFixedLabel(illumisnottyTitleText, IllumisnottyTitleFontSize, TextAlignmentOptions.Right);
            SetAutosizeLabel(brainPowerCounterText, BrainsCounterFontMin, BrainsCounterFontMax, null);
            SetAutosizeLabel(bppsText, CounterFontMin, CounterFontMax, null);
            SetAutosizeLabel(cashText, CounterFontMin, CounterFontMax, null);

            // 2026-10-12 play-test fix: Inspector field checked first (optional override, still
            // supported for anyone who wants a different material without touching code), falling
            // back to the Resources-loaded one so this takes effect with no scene write at all.
            Material glowMaterial = brainsCounterGlowMaterial != null ? brainsCounterGlowMaterial : GetScoreboardGlowMaterial();
            if (glowMaterial != null && brainPowerCounterText != null)
            {
                brainPowerCounterText.fontSharedMaterial = glowMaterial;
            }

            // 2026-10-12 play-test fix: root cause of CashText's greenish/olive tint --
            // Editor/CashTextLegibilityFix.cs (an Editor-only scene tool) assigned CashText's
            // fontSharedMaterial to Oswald_GoldUnderlay.mat, a clone of Oswald_CyanGlow.mat with an
            // amber/gold _UnderlayColor. That amber glow halo bleeding around the (now White) face
            // is what read as greenish-olive, not a vertex-color or rich-text issue. That fix
            // predates BuildScoreboardPanel's solid black-glass backing -- the backdrop-contrast
            // problem it solved no longer applies now that every scoreboard label sits on a solid
            // panel, so resetting to the font's own default material here (same one every other
            // unmodified scoreboard label already uses) is safe. Scene's serialized reference is
            // left as-authored; this only overrides at runtime, same pattern as the glow material
            // above.
            if (cashText != null)
            {
                TMP_FontAsset oswaldForCash = GetOswaldFont();
                if (oswaldForCash != null && oswaldForCash.material != null)
                {
                    cashText.fontSharedMaterial = oswaldForCash.material;
                }
            }

            RebalanceScoreboardLayout();
        }

        private bool loggedScoreboardRectsOnce;

        /// <summary>2026-10-12 play-test fix round 2: logs resolved RectTransform.rect (the actual
        /// on-screen width/height after anchors+sizeDelta are applied), not sizeDelta directly --
        /// sizeDelta.x is legitimately negative for every stretch-anchored scoreboard label (it's an
        /// inset from full width, e.g. -48 means "parent width minus 48px"), which read as a bogus
        /// "negative width" bug in the round-1 log. .rect is also only trustworthy here because
        /// ForceScoreboardLayoutAndAutosize (called first, in RebalanceScoreboardLayout) has already
        /// forced an immediate rebuild -- reading it before that forced rebuild (as round 1 did, via
        /// a deferred FlushDirtyTexts hook) could still race Unity's own lazy layout pass.</summary>
        private void LogScoreboardRectsOnce()
        {
            if (loggedScoreboardRectsOnce) { return; }
            loggedScoreboardRectsOnce = true;

            LogLabelRect(playerIQText, "IQText");
            LogLabelRect(illumisnottyTitleText, "IllumisnottyTitleText");
            LogLabelRect(brainPowerCounterText, "BrainsCounterText");
            LogLabelRect(bppsText, "BPPSText");
            LogLabelRect(cashText, "CashText");
            LogLabelRect(restorationProgressText, "IllumisnottyGripText");
            LogLabelRect(cumulativeBrainPowerCounterText, "CumulativeBPText");
            LogLabelRect(rebirthCountText, "RebirthCountText");
        }

        private static void LogLabelRect(TextMeshProUGUI label, string name)
        {
            if (label == null) { return; }
            Rect r = label.rectTransform.rect;
            Debug.Log($"[HUDController] Scoreboard rect '{name}': rect=({r.width:F2}, {r.height:F2}), fontSize={label.fontSize:F1}, autosize={label.enableAutoSizing}, min={label.fontSizeMin:F1}, max={label.fontSizeMax:F1}");
        }

        /// <summary>2026-10-12 play-test fix round 2, requested assert-style check: warns (every
        /// call, not just once, so a regression is caught immediately rather than only at the first
        /// startup log) if a scoreboard label ends up with a non-positive resolved width/height, or
        /// if autosize has settled below its own configured floor -- both are genuine invalid states,
        /// not just small-looking text.</summary>
        private static void ValidateScoreboardRect(TextMeshProUGUI label, string name)
        {
            if (label == null) { return; }
            Rect r = label.rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f)
            {
                Debug.LogWarning($"[HUDController] Scoreboard label '{name}' has a non-positive resolved rect ({r.width:F2} x {r.height:F2}) -- its own RectTransform or a parent LayoutGroup/ContentSizeFitter needs a closer look.");
            }
            if (label.enableAutoSizing && label.fontSize < label.fontSizeMin - 0.1f)
            {
                Debug.LogWarning($"[HUDController] Scoreboard label '{name}' resolved fontSize ({label.fontSize:F1}) below its own fontSizeMin ({label.fontSizeMin:F1}) -- autosize is stale; needs a ForceMeshUpdate after whatever changed its rect/min/max.");
            }
        }

        /// <summary>Forces a fixed (non-autosizing), single-line font size, matching the scoreboard
        /// redesign's "fixed Npt" spec rather than an autosize floor/ceiling. Alignment only touched
        /// when a value is passed -- most of these labels keep whatever alignment the scene
        /// authored.</summary>
        private static void SetFixedLabel(TextMeshProUGUI label, float size, TextAlignmentOptions? alignment)
        {
            if (label == null) { return; }
            label.enableAutoSizing = false;
            label.fontSize = size;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (alignment.HasValue) { label.alignment = alignment.Value; }
        }

        /// <summary>2026-10-10 sizing pass: the magnitude-unbounded numeric counters (BrainsCounter/
        /// BPPS/Cash) -- autosizes down from max toward min, single-line, so a long formatted value
        /// shrinks to fit the header's width instead of clipping, while short values render near
        /// max.</summary>
        private static void SetAutosizeLabel(TextMeshProUGUI label, float min, float max, TextAlignmentOptions? alignment)
        {
            if (label == null) { return; }
            label.enableAutoSizing = true;
            label.fontSizeMin = min;
            label.fontSizeMax = max;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (alignment.HasValue) { label.alignment = alignment.Value; }
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
        /// 2026-10-10 scoreboard sizing pass (rewritten 2026-10-12, see RebalanceScoreboardLayout's
        /// own doc comment for why): re-balances CurrencyHeader's own height and each of its 7
        /// children's row (Y position + height) around the bumped font sizes above, entirely
        /// code-driven -- no .unity write. Six rows, top to bottom: [IQText | IllumisnottyTitleText]
        /// side by side, BrainsCounterText, TextsRow (BPPS/Cash, a HorizontalLayoutGroup --
        /// confirmed by reading the scene directly: ChildControlHeight/ChildForceExpandHeight are
        /// both already on, so resizing TextsRow's own RectTransform is enough, its two children
        /// don't need touching individually), restorationProgressText, cumulativeBrainPowerCounterText,
        /// rebirthCountText.
        ///
        /// Every row gets the height its own target font size needs -- no row is ever shrunk below
        /// that. Leftover budget (if the live safe height has room to spare) becomes extra gap
        /// between rows instead of being left as dead space at the bottom.
        ///
        /// The safe budget itself is read LIVE from the scene (ComputeMaxSafeHeaderHeight), not
        /// hardcoded: CurrencyHeader's own top offset vs. the Dia-Log button's (LogOpenButton) top
        /// offset, both under the same CustomSafeArea parent, minus a safety margin -- the exact
        /// "keep the side buttons clear" boundary, self-correcting if either one ever moves.
        /// </summary>
        private const float ScoreboardRowGap = 4f;
        private const float ScoreboardTopPadding = 6f;
        private const float ScoreboardBottomPadding = 6f;
        private const float ScoreboardButtonSafetyMargin = 16f;
        private const float ScoreboardFallbackMaxHeight = 300f;
        private const string DiaLogButtonName = "LogOpenButton"; // same lookup PocketPanelUI/TimedPurchaseWalletUI/RewardedAdRecoveryHudButtonUI already use

        private static float EstimateRowHeight(float fontSize, int lines = 1)
        {
            return (fontSize * 1.05f * lines) + 4f;
        }

        private static float ComputeMaxSafeHeaderHeight(RectTransform header)
        {
            GameObject diaLogButton = GameObject.Find(DiaLogButtonName);
            RectTransform diaLogRect = diaLogButton != null ? diaLogButton.GetComponent<RectTransform>() : null;
            if (diaLogRect == null)
            {
                Debug.LogWarning("[HUDController] Dia-Log button (LogOpenButton) not found -- scoreboard height clamp using a conservative fallback instead of a live read.");
                return ScoreboardFallbackMaxHeight;
            }

            float headerTopOffset = -header.anchoredPosition.y;
            float diaLogTopOffset = -diaLogRect.anchoredPosition.y;
            return Mathf.Max(100f, (diaLogTopOffset - headerTopOffset) - ScoreboardButtonSafetyMargin);
        }

        private static void SetRowRect(RectTransform rect, float topOffset, float height)
        {
            if (rect == null) { return; }
            Vector2 pos = rect.anchoredPosition;
            pos.y = -topOffset;
            rect.anchoredPosition = pos;
            Vector2 size = rect.sizeDelta;
            size.y = height;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// 2026-10-12 play-test fix, rewritten from the original flexScale-based compression: that
        /// approach shrank BrainsCounter/TextsRow/restoration's row heights proportionally whenever
        /// the live safe budget came in tighter than their combined "ideal" (max-font) height --
        /// which is exactly what crushed TextsRow (BPPS/CashText) down far enough that their
        /// autosize collapsed well below 38pt, reported as "renders TINY." No row is ever shrunk
        /// below its own ideal height now -- every row gets the height it actually needs at its
        /// target font size, full stop. If the live safe budget has room to spare once every row
        /// has what it needs, the leftover is distributed as EXTRA GAP between all 6 rows (not as
        /// further growth past each row's target), so the panel fills the full budget without
        /// looking top-heavy -- previously, surplus budget just sat as dead space below
        /// RebirthCountText, since the old code only ever shrank toward the budget, never grew
        /// gaps to fill it. If the budget is genuinely too tight to fit everyone's target height
        /// even with zero extra gap, the header grows past the "safe" budget instead of crushing a
        /// row below readable size -- logged loudly so the actual overage is visible, since
        /// readable size now outranks strict adherence to the side-button clearance margin.
        ///
        /// 2026-10-12 play-test fix ROUND 2: the row-height math above was correct (319px computed,
        /// within a few px of the real budget), but setting TextsRow's own RectTransform.sizeDelta
        /// doesn't synchronously resize its HorizontalLayoutGroup-controlled children -- Unity
        /// defers LayoutGroup rebuilds to end-of-frame (CanvasUpdateRegistry), so BPPSText/CashText
        /// were still carrying their scene-authored (0, 0) rect at the moment TMP's autosize last
        /// ran, which is why they rendered (and logged) as 0x0 with fontSize clamped to an absolute
        /// floor of 18 -- below even their own fontSizeMin, since TMP's autosize will violate its own
        /// floor rather than render into a truly zero-sized box. The same staleness hit
        /// restorationProgressText's fontSize (18, min 28) even though its own rect was correctly
        /// sized (67px) -- its RectTransform resize doesn't by itself re-trigger TMP's internal
        /// autosize fit either; that only happens on the next SetText or a forced update.
        /// ForceScoreboardLayoutAndAutosize (below) closes both gaps: LayoutRebuilder.
        /// ForceRebuildLayoutImmediate forces TextsRow's HorizontalLayoutGroup to size its children
        /// NOW instead of at an unspecified later frame, and ForceMeshUpdate forces each
        /// autosize-enabled label to redo its fit against its current (now-correct) rect and
        /// fontSizeMin/Max immediately, rather than waiting on the next natural text change.
        /// ValidateScoreboardRect (called for all 7 labels afterward) is the requested assert-style
        /// check -- it uses RectTransform.rect (the resolved on-screen size), not sizeDelta, since
        /// sizeDelta.x is legitimately negative for every stretch-anchored label here and reading it
        /// directly is what made round 1's "-48 width" look like a bug when it wasn't.
        /// </summary>
        private void RebalanceScoreboardLayout()
        {
            if (brainPowerCounterText == null) { return; }
            RectTransform header = brainPowerCounterText.transform.parent as RectTransform;
            if (header == null) { return; }

            RectTransform iqRect = playerIQText != null ? playerIQText.rectTransform : null;
            RectTransform titleRect = illumisnottyTitleText != null ? illumisnottyTitleText.rectTransform : null;
            RectTransform brainsRect = brainPowerCounterText.rectTransform;
            RectTransform textsRowRect = bppsText != null ? bppsText.transform.parent as RectTransform : null;
            RectTransform restorationRect = restorationProgressText != null ? restorationProgressText.rectTransform : null;
            RectTransform cumulativeRect = cumulativeBrainPowerCounterText != null ? cumulativeBrainPowerCounterText.rectTransform : null;
            RectTransform rebirthRect = rebirthCountText != null ? rebirthCountText.rectTransform : null;

            float rowAHeight = EstimateRowHeight(Mathf.Max(IQFontSize, IllumisnottyTitleFontSize));
            float rowBHeight = EstimateRowHeight(BrainsCounterFontMax);
            float rowCHeight = EstimateRowHeight(CounterFontMax); // TextsRow (BPPS/Cash) -- guaranteed, never shrunk below 38pt's needs
            float rowDHeight = EstimateRowHeight(RestorationFontCeiling, 2); // up to 2 lines
            float rowEHeight = EstimateRowHeight(24f); // cumulative BP label -- unchanged by this pass
            float rowFHeight = EstimateRowHeight(24f); // rebirth count label -- unchanged by this pass
            float rowsTotal = rowAHeight + rowBHeight + rowCHeight + rowDHeight + rowEHeight + rowFHeight;

            const int gapCount = 5;
            float baseContentHeight = rowsTotal + (ScoreboardRowGap * gapCount) + ScoreboardTopPadding + ScoreboardBottomPadding;

            float maxSafeHeight = ComputeMaxSafeHeaderHeight(header);
            float surplus = maxSafeHeight - baseContentHeight;
            float effectiveGap = surplus > 0f ? ScoreboardRowGap + (surplus / gapCount) : ScoreboardRowGap;

            float totalHeight = rowsTotal + (effectiveGap * gapCount) + ScoreboardTopPadding + ScoreboardBottomPadding;
            if (totalHeight > maxSafeHeight)
            {
                Debug.LogWarning($"[HUDController] Scoreboard content ({totalHeight:F0}px) exceeds the safe budget before the side buttons ({maxSafeHeight:F0}px) even with zero extra gap -- a row's font size may need trimming, or the side buttons moved down.");
            }

            Vector2 headerSize = header.sizeDelta;
            headerSize.y = totalHeight;
            header.sizeDelta = headerSize;

            float y = ScoreboardTopPadding;
            SetRowRect(iqRect, y, rowAHeight);
            SetRowRect(titleRect, y, rowAHeight);
            y += rowAHeight + effectiveGap;

            SetRowRect(brainsRect, y, rowBHeight);
            y += rowBHeight + effectiveGap;

            SetRowRect(textsRowRect, y, rowCHeight);
            y += rowCHeight + effectiveGap;

            SetRowRect(restorationRect, y, rowDHeight);
            y += rowDHeight + effectiveGap;

            SetRowRect(cumulativeRect, y, rowEHeight);
            y += rowEHeight + effectiveGap;

            SetRowRect(rebirthRect, y, rowFHeight);

            ForceScoreboardLayoutAndAutosize(textsRowRect);

            ValidateScoreboardRect(playerIQText, "IQText");
            ValidateScoreboardRect(illumisnottyTitleText, "IllumisnottyTitleText");
            ValidateScoreboardRect(brainPowerCounterText, "BrainsCounterText");
            ValidateScoreboardRect(bppsText, "BPPSText");
            ValidateScoreboardRect(cashText, "CashText");
            ValidateScoreboardRect(restorationProgressText, "IllumisnottyGripText");
            ValidateScoreboardRect(cumulativeBrainPowerCounterText, "CumulativeBPText");
            ValidateScoreboardRect(rebirthCountText, "RebirthCountText");

            LogScoreboardRectsOnce();
        }

        /// <summary>2026-10-12 play-test fix round 2: forces TextsRow's HorizontalLayoutGroup to
        /// immediately size BPPSText/CashText (instead of waiting for Unity's deferred end-of-frame
        /// layout pass), then forces every autosize-enabled scoreboard label to immediately redo its
        /// fit against its now-current rect and fontSizeMin/Max (instead of waiting for the next
        /// SetText call). See RebalanceScoreboardLayout's "ROUND 2" doc comment above for the full
        /// root-cause writeup.</summary>
        private void ForceScoreboardLayoutAndAutosize(RectTransform textsRowRect)
        {
            if (textsRowRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(textsRowRect);
            }

            ForceLabelMeshUpdate(brainPowerCounterText);
            ForceLabelMeshUpdate(bppsText);
            ForceLabelMeshUpdate(cashText);
            ForceLabelMeshUpdate(restorationProgressText);
        }

        private static void ForceLabelMeshUpdate(TextMeshProUGUI label)
        {
            if (label != null && label.enableAutoSizing)
            {
                label.ForceMeshUpdate();
            }
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
            restorationFillImage.color = RestorationFillSteady;
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

            // 2026-10-12 scoreboard redesign: empty, disabled art hook for a future painted
            // circuit "tattoo" overlay on the restoration bar -- explicitly NOT in scope for this
            // pass (decorative art, will come from Aceyfer's Leonardo pipeline per the no-
            // procedural-art rule). Built here, between Sheen and FrameOverlay, so a later pass
            // only has to assign a sprite and flip enabled on; no sprite is assigned now and the
            // Image starts disabled so it has zero visual effect until then.
            Transform tattooTransform = track.Find("RestorationTattooOverlay");
            GameObject tattooObject = tattooTransform != null
                ? tattooTransform.gameObject
                : new GameObject("RestorationTattooOverlay", typeof(RectTransform), typeof(Image));
            tattooObject.transform.SetParent(track, false);
            Image tattooImage = tattooObject.GetComponent<Image>();
            tattooImage.type = Image.Type.Simple;
            tattooImage.preserveAspect = false;
            tattooImage.raycastTarget = false;
            tattooImage.color = Color.white;
            tattooImage.enabled = false;
            RectTransform tattooRect = tattooImage.rectTransform;
            tattooRect.anchorMin = Vector2.zero;
            tattooRect.anchorMax = Vector2.one;
            tattooRect.offsetMin = Vector2.zero;
            tattooRect.offsetMax = Vector2.zero;

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
            tattooObject.transform.SetSiblingIndex(2);
            overlayObject.transform.SetSiblingIndex(3);
            sproutObject.transform.SetSiblingIndex(4);

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

            // Gain flash: fixed palette flash (Glow -> Deep Cyan), triggered once per target
            // increase in UpdateRestorationProgressText, not re-triggered every glide frame.
            if (restorationFlashStartTime >= 0f)
            {
                float flashT = Mathf.Clamp01((Time.unscaledTime - restorationFlashStartTime) / RestorationFlashDuration);
                restorationFillImage.color = Color.Lerp(RestorationFlashColor, RestorationFillSteady, flashT);
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

        /// <summary>
        /// 2026-10-12 scoreboard redesign: gives CurrencyHeader a fixed, code-owned black-glass
        /// look -- solid Black-ish fill, a NeonBlue/Glow edge ring, a subtle scanline overlay --
        /// so it reads identically on all 6 World Restoration stages. Resolved via
        /// brainPowerCounterText's own parent (same no-new-field pattern as HeaderPanelRect above)
        /// rather than a new serialized reference, so this needs no scene wiring. Idempotent
        /// (Find-before-create), same convention as BuildRestorationBar; safe to call repeatedly
        /// from ApplyVisualStyle.
        /// </summary>
        private void BuildScoreboardPanel()
        {
            if (brainPowerCounterText == null) { return; }

            Transform header = brainPowerCounterText.transform.parent;
            if (header == null) { return; }

            Sprite blackGlassSprite = Resources.Load<Sprite>("UI/Scoreboard/Scoreboard_BlackGlass");
            Sprite neonEdgeSprite = Resources.Load<Sprite>("UI/Scoreboard/Scoreboard_NeonEdge");
            Sprite scanlineSprite = Resources.Load<Sprite>("UI/Scoreboard/Scoreboard_Scanline");

            if (blackGlassSprite == null || neonEdgeSprite == null || scanlineSprite == null)
            {
                Debug.LogWarning("[HUDController] Scoreboard art missing from Resources/UI/Scoreboard -- CurrencyHeader left as scene-authored.", this);
                return;
            }

            // Fill lives directly on CurrencyHeader's own Image component -- fixed, code-owned,
            // and never touched by AccentBarStageView/TopBG's per-stage swap (that component
            // targets a completely different GameObject under RainOverlay, confirmed by reading
            // the scene directly, not assumed), so the panel reads identically on all 6 stages by
            // construction, not by coincidence.
            Image headerFill = header.GetComponent<Image>();
            if (headerFill == null) { headerFill = header.gameObject.AddComponent<Image>(); }
            headerFill.sprite = blackGlassSprite;
            headerFill.type = Image.Type.Sliced;
            headerFill.color = Color.white; // alpha/tint already baked into the art
            headerFill.raycastTarget = true;

            // Scanline: behind all the text children (first sibling), subtle (8-12% alpha baked
            // into the art, not an Image.color tint).
            Image scanlineImage = BuildOrReuseScoreboardLayer(header, "ScoreboardScanline", scanlineSprite, Image.Type.Simple);
            scanlineImage.transform.SetAsFirstSibling();

            // Edge: glowing rim on top of everything, including the text -- a frame, not a backdrop.
            scoreboardEdgeImage = BuildOrReuseScoreboardLayer(header, "ScoreboardEdge", neonEdgeSprite, Image.Type.Sliced);
            scoreboardEdgeImage.transform.SetAsLastSibling();

            scoreboardPanelBuilt = true;
        }

        private static Image BuildOrReuseScoreboardLayer(Transform parent, string name, Sprite sprite, Image.Type type)
        {
            Transform existing = parent.Find(name);
            GameObject layerObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(parent, false);

            Image image = layerObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return image;
        }

        /// <summary>Idle breathing glow on the scoreboard's neon edge -- alpha 0.5-0.95, sine on
        /// unscaledTime*2.2 (continues while paused/menus open, matching the restoration bar's own
        /// unscaledTime convention above). Zero-GC: struct Color mutation only, no allocation.</summary>
        private void TickScoreboardGlowPulse()
        {
            if (!scoreboardPanelBuilt || scoreboardEdgeImage == null)
            {
                return;
            }

            float sine01 = (Mathf.Sin(Time.unscaledTime * 2.2f) + 1f) * 0.5f;
            Color c = scoreboardEdgeImage.color;
            c.a = Mathf.Lerp(0.5f, 0.95f, sine01);
            scoreboardEdgeImage.color = c;
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
