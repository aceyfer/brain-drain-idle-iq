using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>Concrete UnityEvent subclass required for a string payload to show up in the Inspector.</summary>
    [System.Serializable]
    public sealed class DialogueLineUnityEvent : UnityEvent<string> { }

    /// <summary>
    /// Listens for gameplay triggers (first tap, building purchase, rebirth, event outcome,
    /// IQ milestone), picks a matching NarratorLine at random (filtered by trigger type,
    /// building name, and current RebirthCount range), and fires OnDialogueLine for UI to
    /// display. Avoids repeating any line seen in the last 10 history entries (falling back to
    /// a narrower last-line check, then the unfiltered candidate pool, so anti-repeat filtering
    /// can never itself suppress a fire -- SS20b); queues up to 2 lines while one is displaying,
    /// with a minimum gap between lines, same-trigger coalescing in the queue, and a per-trigger
    /// cooldown on repeatable triggers (SS20). Line-pool tier selection is gated
    /// on WorldRestorationManager.RestorationPercent (switched from RebirthCount 2026-06-22 --
    /// see TryFireLine).
    ///
    /// Also accepts ad-hoc lines that bypass the trigger/pool system entirely (e.g. ChapterManager's
    /// COGS reaction lines) via EnqueueDirectLine -- these go through the exact same display/queue
    /// pipeline as pool-driven lines, just without trigger-type matching or repeat-avoidance.
    /// </summary>
    public sealed class DialogueManager : MonoBehaviour
    {
        private const int MaxQueueDepth = 2;
        private const int MaxHistoryEntries = 50;
        private const float DefaultDisplayDurationSeconds = 3f;

        /// <summary>
        /// Absolute floor on how long any line stays on screen, regardless of its requested
        /// Duration -- applies below TargetCharsPerSecond's length-aware floor too, so a very
        /// short line still holds this long. Applied in Display() to every line -- direct,
        /// pooled, and priority alike.
        /// </summary>
        private const float MinLineDisplaySeconds = 4.5f;

        /// <summary>
        /// Comfortable reading pace used to scale the display floor with line length (2026-08-01),
        /// replacing the old flat-4.5s-regardless-of-length floor that left long lines (e.g. the
        /// 235-char Illumisnotti Tier4 breakdown lines) running at 50+ chars/sec -- unreadable.
        /// Deliberately not the same rate as ChatterBubble.cs's pedestrian-chatter pipeline (that
        /// system is tuned for brief ambient barks, not dense narrative COGS lines) -- the two
        /// pipelines stay independently tuned.
        /// </summary>
        private const float TargetCharsPerSecond = 26f;
        private const int TapsWithoutPurchaseThreshold = 25;
        private const double SnottingReadyThreshold = 50_000d;

        /// <summary>
        /// Pause between one line finishing and the next queued line displaying. Must exceed
        /// DialogueDisplayUI's total slide overhead (2x SlideDurationSeconds = 0.6s): the UI
        /// occupies the screen for displayDuration + 0.6s, while this manager's timer only
        /// waits displayDuration -- without this gap the next OnDialogueLine fires mid-slide-out
        /// and visibly yanks the panel back (the SS20 "lines interrupt each other" symptom).
        /// 1.0s = 0.6s slide overhead + 0.4s breathing room. If DialogueDisplayUI's
        /// SlideDurationSeconds ever changes, revisit this constant.
        /// </summary>
        private const float MinGapSeconds = 1f;

        /// <summary>
        /// Repeatable triggers (see RepeatableTriggers) can't fire again within this window.
        /// Kills line floods from the 10s cash auto-convert tick, fast idle-income IQ-milestone
        /// bursts, rapid tap streaks, and building-purchase spam-buy sessions (added 2026-07-24 --
        /// a live test's spam-buy burst queued one comment per purchase, landing lines at
        /// [00:41]/[00:46]/[00:50] in the dialogue log; now only the burst's first purchase
        /// speaks, the rest stay silent for 20s) -- one-shot triggers (First*, Rebirth, stage
        /// changes) are exempt since they structurally can't flood.
        /// </summary>
        private const float RepeatTriggerCooldownSeconds = 20f;

        private static readonly HashSet<NarratorTriggerType> RepeatableTriggers = new()
        {
            NarratorTriggerType.IQMilestone,
            NarratorTriggerType.CashConverted,
            NarratorTriggerType.TapWithoutPurchase,
            NarratorTriggerType.EventOutcome,
            NarratorTriggerType.BuildingPurchase,
        };

        /// <summary>One line ready to display, regardless of whether it came from the NarratorLine pool or was injected directly.</summary>
        private readonly struct DialogueEntry
        {
            public readonly string Text;
            public readonly float Duration;
            public readonly NarratorLine SourceLine;

            public DialogueEntry(string text, float duration, NarratorLine sourceLine)
            {
                Text = text;
                Duration = duration > 0f ? duration : DefaultDisplayDurationSeconds;
                SourceLine = sourceLine;
            }
        }

        /// <summary>One recorded line in the session's dialogue history, exposed via History for UI (the dialogue log panel, SS20b).</summary>
        public readonly struct DialogueLogEntry
        {
            public readonly string Text;
            public readonly float SessionTime;
            public readonly NarratorLine SourceLine;

            public DialogueLogEntry(string text, float sessionTime, NarratorLine sourceLine)
            {
                Text = text;
                SessionTime = sessionTime;
                SourceLine = sourceLine;
            }
        }

        [Header("Line Pool")]
        [SerializeField] private List<NarratorLine> narratorLines = new();

        /// <summary>Fired with the line of dialogue to display. Concrete UnityEvent so it's wireable in the Inspector too.</summary>
        public DialogueLineUnityEvent OnDialogueLine = new();

        /// <summary>Fired whenever a line is appended to History -- consumed by the dialogue log panel UI (SS20b).</summary>
        public event System.Action OnHistoryChanged;

        private static DialogueManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static DialogueManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<DialogueManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("DialogueManager (Auto)");
                    instance = hostObject.AddComponent<DialogueManager>();
                }

                return instance;
            }
        }

        /// <summary>The display duration of the line most recently sent via OnDialogueLine.</summary>
        public float CurrentDisplayDurationSeconds { get; private set; } = DefaultDisplayDurationSeconds;

        /// <summary>Read-only view of the session's dialogue history (oldest first), capped at MaxHistoryEntries -- backs the dialogue log panel UI (SS20b).</summary>
        public IReadOnlyList<DialogueLogEntry> History => history;

        /// <summary>FIFO pending list (index 0 = next up). A List rather than a Queue so
        /// same-trigger coalescing can replace a stale queued entry in place.</summary>
        private readonly List<DialogueEntry> queuedEntries = new();
        private readonly List<DialogueLogEntry> history = new();
        private readonly Dictionary<NarratorTriggerType, float> lastRepeatableFireTime = new();
        private bool isDisplaying;
        private NarratorLine lastPlayedLine;
        private int tapsSinceLastPurchase;
        private Coroutine activeDisplayCoroutine;
        private bool hasSeenFirstRestore;
        private bool hasSeenSnottingReady;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            // No DontDestroyOnLoad: this is a single-scene game with no scene reloads during
            // play, so it serves no purpose -- and it warns/no-ops anyway once this GameObject
            // is parented under _Systems rather than at scene root.
        }

        private void Start()
        {
            SubscribeToEvents();

            // First-ever-play boot line used to be hardcoded here; FTUEManager (§23) now owns
            // first-ever-play detection and the boot briefing sequence in its place.
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void SubscribeToEvents()
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.OnFirstBrainPowerEarned -= HandleFirstTap;
                currencyManager.OnFirstBrainPowerEarned += HandleFirstTap;
                currencyManager.OnCashConverted -= HandleCashConverted;
                currencyManager.OnCashConverted += HandleCashConverted;
                currencyManager.OnFirstCashEarned -= HandleFirstCashEarned;
                currencyManager.OnFirstCashEarned += HandleFirstCashEarned;
            }

            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingPurchased -= HandleBuildingPurchased;
                upgradeManager.OnBuildingPurchased += HandleBuildingPurchased;
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirth;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirth;
            }

            if (RandomEventManager.Instance != null)
            {
                RandomEventManager.Instance.OnEventResolved -= HandleEventResolved;
                RandomEventManager.Instance.OnEventResolved += HandleEventResolved;
            }

            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager != null)
            {
                playerIQManager.OnIQMilestoneCrossed -= HandleIQMilestone;
                playerIQManager.OnIQMilestoneCrossed += HandleIQMilestone;
                playerIQManager.OnOfflineDecayApplied -= HandleOfflineDecayApplied;
                playerIQManager.OnOfflineDecayApplied += HandleOfflineDecayApplied;
            }

            PlayerTapHandler tapHandler = PlayerTapHandler.Instance;
            if (tapHandler != null)
            {
                tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;
                tapHandler.OnTapRewardEarned += HandleTapRewardEarned;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationProgressChanged += HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleRestorationStageChanged;
                WorldRestorationManager.Instance.OnRestorationStageChanged += HandleRestorationStageChanged;
            }

            if (DailyEngagementCapManager.Instance != null)
            {
                DailyEngagementCapManager.Instance.OnThrottleOnset -= HandleDailyCapThrottleOnset;
                DailyEngagementCapManager.Instance.OnThrottleOnset += HandleDailyCapThrottleOnset;
            }
        }

        private void UnsubscribeFromEvents()
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.OnFirstBrainPowerEarned -= HandleFirstTap;
                currencyManager.OnCashConverted -= HandleCashConverted;
                currencyManager.OnFirstCashEarned -= HandleFirstCashEarned;
            }

            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnBuildingPurchased -= HandleBuildingPurchased;
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirth;
            }

            if (RandomEventManager.Instance != null)
            {
                RandomEventManager.Instance.OnEventResolved -= HandleEventResolved;
            }

            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager != null)
            {
                playerIQManager.OnIQMilestoneCrossed -= HandleIQMilestone;
                playerIQManager.OnOfflineDecayApplied -= HandleOfflineDecayApplied;
            }

            PlayerTapHandler tapHandler = PlayerTapHandler.Instance;
            if (tapHandler != null)
            {
                tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationStageChanged -= HandleRestorationStageChanged;
            }

            if (DailyEngagementCapManager.Instance != null)
            {
                DailyEngagementCapManager.Instance.OnThrottleOnset -= HandleDailyCapThrottleOnset;
            }
        }

        private void HandleFirstTap()
        {
            TryFireLine(NarratorTriggerType.FirstTap, null);
            EnqueueDirectLine("OPEN BP SHOP → BUY BUILDINGS → EARN BRAIN POWER WHILE YOU SLEEP.", 4f);
        }

        /// <summary>Resets the tap-without-purchase counter and attempts a purchase-comment
        /// line. BuildingPurchase is a RepeatableTriggers member (added 2026-07-24), so
        /// TryFireLine's own 20s cooldown coalesces spam-buy bursts into a single line rather
        /// than one per purchase.</summary>
        private void HandleBuildingPurchased(BuildingData building)
        {
            tapsSinceLastPurchase = 0;
            TryFireLine(NarratorTriggerType.BuildingPurchase, building != null ? building.buildingName : null);
        }

        private void HandleRebirth(int rebirthCount)
        {
            TryFireLine(NarratorTriggerType.Rebirth, null);
        }

        private void HandleEventResolved(BrainRotEventData eventData)
        {
            TryFireLine(NarratorTriggerType.EventOutcome, null);
        }

        private void HandleIQMilestone(float currentIQ)
        {
            TryFireLine(NarratorTriggerType.IQMilestone, null);
        }

        private void HandleCashConverted(double amount)
        {
            TryFireLine(NarratorTriggerType.CashConverted, null);
        }

        private void HandleOfflineDecayApplied(float amountLost)
        {
            TryFireLine(NarratorTriggerType.OfflineDecayReturn, null);
        }

        private void HandleFirstCashEarned()
        {
            TryFireLine(NarratorTriggerType.FirstCashEarned, null);
        }

        private void HandleRestorationProgressChanged(double cumulativeSpent)
        {
            if (!hasSeenFirstRestore && cumulativeSpent > 0d)
            {
                hasSeenFirstRestore = true;
                TryFireLine(NarratorTriggerType.FirstRestoreSpend, null);
            }
            else if (!hasSeenSnottingReady && cumulativeSpent >= SnottingReadyThreshold)
            {
                hasSeenSnottingReady = true;
                TryFireLine(NarratorTriggerType.SnottingReady, null);
            }
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            if (stage == null || stage.pointsRequired <= 0d)
            {
                return;
            }

            TryFireLine(NarratorTriggerType.RestorationStageChange, null);
        }

        /// <summary>OnThrottleOnset fires at most once per local day (re-armed only at the next
        /// day's rollover), so this is a one-shot trigger like Rebirth/RestorationStageChange --
        /// not added to RepeatableTriggers since it structurally can't flood.</summary>
        private void HandleDailyCapThrottleOnset()
        {
            TryFireLine(NarratorTriggerType.DailyCapThrottleOnset, null);
        }

        /// <summary>Counts taps since the last building purchase; fires every TapsWithoutPurchaseThreshold taps as long as the player keeps tapping without buying anything.</summary>
        private void HandleTapRewardEarned(double _)
        {
            tapsSinceLastPurchase++;
            if (tapsSinceLastPurchase >= TapsWithoutPurchaseThreshold)
            {
                tapsSinceLastPurchase = 0;
                TryFireLine(NarratorTriggerType.TapWithoutPurchase, null);
            }
        }

        private void TryFireLine(NarratorTriggerType triggerType, string buildingName)
        {
            // Flood gate: repeatable triggers respect a cooldown (SS20). Checked before
            // candidate selection so a suppressed fire costs nothing.
            if (RepeatableTriggers.Contains(triggerType))
            {
                if (lastRepeatableFireTime.TryGetValue(triggerType, out float lastTime)
                    && Time.time - lastTime < RepeatTriggerCooldownSeconds)
                {
                    return;
                }
            }

            // Tier selection switched 2026-06-22 from RebirthCount to WorldRestorationManager's
            // RestorationPercent -- RebirthCount-gated tone tiers meant a player who never
            // rebirths sees the exact same 6 lines forever, while restoration progress climbs
            // continuously and visibly regardless of Rebirth activity, giving a real degrading
            // arc tied to "the population is healing and harder to manipulate" rather than to a
            // Rebirth-count milestone the player may rarely hit.
            float currentRestorationPercent = WorldRestorationManager.Instance != null
                ? (float)WorldRestorationManager.Instance.RestorationPercent
                : 0f;

            List<NarratorLine> candidates = narratorLines.Where(line =>
                line != null
                && line.triggerType == triggerType
                && currentRestorationPercent >= line.minRestorationPercent
                && currentRestorationPercent <= line.maxRestorationPercent
                && (string.IsNullOrWhiteSpace(line.buildingName) || line.buildingName == buildingName)
            ).ToList();

            if (candidates.Count == 0)
            {
                return;
            }

            // Anti-repeat (SS20b): avoid any line seen in the last 10 history entries, falling
            // back to the narrower last-line check, then the unfiltered candidate pool -- never
            // let anti-repeat filtering itself empty the list and silently suppress a fire.
            int recentWindowStart = Mathf.Max(0, history.Count - 10);
            HashSet<NarratorLine> recentLines = new();
            for (int i = recentWindowStart; i < history.Count; i++)
            {
                if (history[i].SourceLine != null)
                {
                    recentLines.Add(history[i].SourceLine);
                }
            }

            List<NarratorLine> pickFrom = candidates.Where(line => !recentLines.Contains(line)).ToList();
            if (pickFrom.Count == 0)
            {
                pickFrom = candidates.Count > 1
                    ? candidates.Where(line => line != lastPlayedLine).ToList()
                    : candidates;
            }
            if (pickFrom.Count == 0)
            {
                pickFrom = candidates;
            }

            NarratorLine chosen = pickFrom[Random.Range(0, pickFrom.Count)];

            if (RepeatableTriggers.Contains(triggerType))
            {
                lastRepeatableFireTime[triggerType] = Time.time;
            }

            Enqueue(new DialogueEntry(chosen.dialogueLine, chosen.displayDurationSeconds, chosen));
        }

        /// <summary>
        /// Injects an ad-hoc line (e.g. ChapterManager's cogsReactionLine) directly into the same
        /// display/queue pipeline as pool-driven lines, bypassing trigger-type matching and
        /// repeat-avoidance entirely.
        /// </summary>
        public void EnqueueDirectLine(string line, float displayDurationSeconds = DefaultDisplayDurationSeconds)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            Enqueue(new DialogueEntry(line, displayDurationSeconds, null));
        }

        /// <summary>
        /// Displays line immediately, interrupting whatever is currently showing (stopping its
        /// finish-timer coroutine so it can't later pull a queued entry out from under this one)
        /// and clearing any queued lines. For reactions to a specific player action (e.g. a
        /// Points Shop purchase's cogsReactionLine) that shouldn't have to wait behind
        /// background narrative lines the way EnqueueDirectLine's queue does. Added 2026-06-22.
        /// </summary>
        public void ShowPriorityLine(string line, float displayDurationSeconds = DefaultDisplayDurationSeconds)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (activeDisplayCoroutine != null)
            {
                StopCoroutine(activeDisplayCoroutine);
            }

            // Interrupts the active line but PRESERVES the queue (changed for SS20 -- was
            // queuedEntries.Clear()): pending pool lines resume after this one finishes + gap.
            Display(new DialogueEntry(line, displayDurationSeconds, null));
        }

        private void Enqueue(DialogueEntry entry)
        {
            if (!isDisplaying)
            {
                Display(entry);
                return;
            }

            // Burst coalescing (SS20): a pool-driven entry replaces any queued entry of the
            // same trigger type in place -- newest wins, queue position kept. Direct lines
            // (null SourceLine) never coalesce against each other.
            if (entry.SourceLine != null)
            {
                for (int i = 0; i < queuedEntries.Count; i++)
                {
                    NarratorLine queuedSource = queuedEntries[i].SourceLine;
                    if (queuedSource != null && queuedSource.triggerType == entry.SourceLine.triggerType)
                    {
                        queuedEntries[i] = entry;
                        return;
                    }
                }
            }

            if (queuedEntries.Count >= MaxQueueDepth)
            {
                // Was a silent bare return (SS20 audit High #1) -- still drops, but never silently.
                Debug.Log($"[DialogueManager] Queue full ({MaxQueueDepth}), dropping line: \"{entry.Text}\"");
                return;
            }

            queuedEntries.Add(entry);
        }

        private void Display(DialogueEntry entry)
        {
            lastPlayedLine = entry.SourceLine;
            isDisplaying = true;

            float lengthAwareSeconds = entry.Text.Length / TargetCharsPerSecond;
            float computedFloor = Mathf.Max(MinLineDisplaySeconds, lengthAwareSeconds);
            CurrentDisplayDurationSeconds = Mathf.Max(entry.Duration, computedFloor);

            history.Add(new DialogueLogEntry(entry.Text, Time.unscaledTime, entry.SourceLine));
            while (history.Count > MaxHistoryEntries)
            {
                history.RemoveAt(0);
            }
            OnHistoryChanged?.Invoke();

            OnDialogueLine?.Invoke(entry.Text);

            activeDisplayCoroutine = StartCoroutine(WaitForLineToFinish(CurrentDisplayDurationSeconds));
        }

        private IEnumerator WaitForLineToFinish(float duration)
        {
            yield return new WaitForSeconds(duration);

            // Min-gap pacing (SS20): isDisplaying stays TRUE through the gap so lines arriving
            // mid-gap queue rather than displaying instantly (which would defeat the gap).
            yield return new WaitForSeconds(MinGapSeconds);

            if (queuedEntries.Count > 0)
            {
                DialogueEntry next = queuedEntries[0];
                queuedEntries.RemoveAt(0);
                Display(next);
            }
            else
            {
                isDisplaying = false;
            }
        }
    }
}
