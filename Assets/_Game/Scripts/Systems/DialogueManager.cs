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
    /// display. Never repeats the same line twice in a row; queues up to 2 lines while one is
    /// displaying. Gated on RebirthCount rather than PlayerIQ, since PlayerIQ only ever
    /// increases in the current model and can't represent a degrading tone.
    ///
    /// Also accepts ad-hoc lines that bypass the trigger/pool system entirely (e.g. ChapterManager's
    /// COGS reaction lines) via EnqueueDirectLine -- these go through the exact same display/queue
    /// pipeline as pool-driven lines, just without trigger-type matching or repeat-avoidance.
    /// </summary>
    public sealed class DialogueManager : MonoBehaviour
    {
        private const int MaxQueueDepth = 2;
        private const float DefaultDisplayDurationSeconds = 3f;

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

        [Header("Line Pool")]
        [SerializeField] private List<NarratorLine> narratorLines = new();

        /// <summary>Fired with the line of dialogue to display. Concrete UnityEvent so it's wireable in the Inspector too.</summary>
        public DialogueLineUnityEvent OnDialogueLine = new();

        private static DialogueManager instance;

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
                    var hostObject = new GameObject("DialogueManager (Auto)");
                    instance = hostObject.AddComponent<DialogueManager>();
                }

                return instance;
            }
        }

        /// <summary>The display duration of the line most recently sent via OnDialogueLine.</summary>
        public float CurrentDisplayDurationSeconds { get; private set; } = DefaultDisplayDurationSeconds;

        private readonly Queue<DialogueEntry> queuedEntries = new();
        private bool isDisplaying;
        private NarratorLine lastPlayedLine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
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
            }
        }

        private void UnsubscribeFromEvents()
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.OnFirstBrainPowerEarned -= HandleFirstTap;
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
            }
        }

        private void HandleFirstTap()
        {
            TryFireLine(NarratorTriggerType.FirstTap, null);
        }

        private void HandleBuildingPurchased(BuildingData building)
        {
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

        private void TryFireLine(NarratorTriggerType triggerType, string buildingName)
        {
            int currentRebirthCount = RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0;

            List<NarratorLine> candidates = narratorLines.Where(line =>
                line != null
                && line.triggerType == triggerType
                && currentRebirthCount >= line.minRebirthCount
                && currentRebirthCount <= line.maxRebirthCount
                && (string.IsNullOrWhiteSpace(line.buildingName) || line.buildingName == buildingName)
            ).ToList();

            if (candidates.Count == 0)
            {
                return;
            }

            List<NarratorLine> pickFrom = candidates.Count > 1
                ? candidates.Where(line => line != lastPlayedLine).ToList()
                : candidates;

            NarratorLine chosen = pickFrom[Random.Range(0, pickFrom.Count)];
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

        private void Enqueue(DialogueEntry entry)
        {
            if (!isDisplaying)
            {
                Display(entry);
                return;
            }

            if (queuedEntries.Count >= MaxQueueDepth)
            {
                return;
            }

            queuedEntries.Enqueue(entry);
        }

        private void Display(DialogueEntry entry)
        {
            lastPlayedLine = entry.SourceLine;
            isDisplaying = true;
            CurrentDisplayDurationSeconds = entry.Duration;

            OnDialogueLine?.Invoke(entry.Text);

            StartCoroutine(WaitForLineToFinish(CurrentDisplayDurationSeconds));
        }

        private IEnumerator WaitForLineToFinish(float duration)
        {
            yield return new WaitForSeconds(duration);

            isDisplaying = false;

            if (queuedEntries.Count > 0)
            {
                Display(queuedEntries.Dequeue());
            }
        }
    }
}
