using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>Concrete UnityEvent subclass required for a ChapterData payload to show up in the Inspector. Shared by OnChapterUnlocked and OnNamePromptRequested.</summary>
    [System.Serializable]
    public sealed class ChapterDataUnityEvent : UnityEvent<ChapterData> { }

    /// <summary>
    /// Drives the 12-chapter narrative arc. Chapters unlock strictly in sequence -- only the
    /// immediately-next chapter's condition is ever checked, so currentChapterNumber alone
    /// (not a set/bitmask) is sufficient to represent progress, matching how it's persisted.
    /// Checked every 10s (folded into GameManager.OnSecondTick) and immediately on the relevant
    /// currency/rebirth change events.
    /// </summary>
    public sealed class ChapterManager : MonoBehaviour
    {
        private const float CheckIntervalSeconds = 10f;

        /// <summary>
        /// Literal sentinel from spec: a chapter whose playerTitle is exactly this string fires
        /// OnNamePromptRequested instead of auto-assigning CurrentTitle. A dedicated bool field
        /// on ChapterData would be more robust than a magic-string check, but the given schema
        /// doesn't include one and this isn't the place to unilaterally add one.
        /// </summary>
        private const string NamePromptSentinel = "[Awaiting Name]";

        [Header("Chapter Pool")]
        [SerializeField] private List<ChapterData> chapters = new();

        /// <summary>Fired when a new chapter unlocks (including name-prompt chapters). Passes the newly unlocked chapter.</summary>
        public ChapterDataUnityEvent OnChapterUnlocked = new();

        /// <summary>Fired instead of auto-assigning CurrentTitle when a name-prompt chapter unlocks. Passes that chapter.</summary>
        public ChapterDataUnityEvent OnNamePromptRequested = new();

        private static ChapterManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static ChapterManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<ChapterManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("ChapterManager (Auto)");
                    instance = hostObject.AddComponent<ChapterManager>();
                }

                return instance;
            }
        }

        /// <summary>The highest chapter number unlocked so far. 0 if none yet.</summary>
        public int CurrentChapterNumber { get; private set; }

        /// <summary>The player's current title, derived from the highest unlocked chapter that isn't awaiting a custom name.</summary>
        public string CurrentTitle { get; private set; } = "";

        private float secondsSinceLastCheck;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[ChapterManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SortChapters();
        }

        private void Start()
        {
            SubscribeToEvents();
            CheckForChapterUnlock();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>Directly assigns a custom title, e.g. after the player completes a name-prompt flow.</summary>
        public void SetPlayerNamedTitle(string customTitle)
        {
            if (string.IsNullOrWhiteSpace(customTitle))
            {
                return;
            }

            CurrentTitle = customTitle;
        }

        /// <summary>
        /// Directly restores progress from a save file. Silent: does not re-fire
        /// OnChapterUnlocked/OnNamePromptRequested or replay the COGS reaction line, since this
        /// represents chapters already unlocked in a previous session, not new unlocks.
        /// </summary>
        public void LoadState(int restoredChapterNumber)
        {
            CurrentChapterNumber = restoredChapterNumber;
            CurrentTitle = ResolveTitleUpToChapter(restoredChapterNumber);
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
                GameManager.Instance.OnSecondTick += HandleSecondTick;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCurrencyEvent;
                currencyManager.OnCumulativeBrainPowerChanged += HandleCurrencyEvent;
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
                currencyManager.OnPointsChanged.AddListener(HandlePointsChanged);
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                currencyManager.OnCumulativeBrainPowerChanged -= HandleCurrencyEvent;
                currencyManager.OnPointsChanged.RemoveListener(HandlePointsChanged);
            }

            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }
        }

        private void HandleSecondTick()
        {
            secondsSinceLastCheck += 1f;
            if (secondsSinceLastCheck < CheckIntervalSeconds)
            {
                return;
            }

            secondsSinceLastCheck = 0f;
            CheckForChapterUnlock();
        }

        private void HandleCurrencyEvent(double _)
        {
            CheckForChapterUnlock();
        }

        private void HandlePointsChanged(double _)
        {
            CheckForChapterUnlock();
        }

        private void HandleRebirthCountChanged(int _)
        {
            CheckForChapterUnlock();
        }

        private void CheckForChapterUnlock()
        {
            while (TryAdvanceToNextChapter())
            {
                // Keep advancing in case multiple thresholds are already satisfied at once
                // (e.g. a high RebirthCount carried over while currentChapterNumber lagged behind).
            }
        }

        private bool TryAdvanceToNextChapter()
        {
            ChapterData nextChapter = FindChapterByNumber(CurrentChapterNumber + 1);
            if (nextChapter == null || !IsConditionMet(nextChapter))
            {
                return false;
            }

            UnlockChapter(nextChapter);
            return true;
        }

        private void UnlockChapter(ChapterData chapter)
        {
            CurrentChapterNumber = chapter.chapterNumber;

            bool isNamePromptChapter = chapter.playerTitle == NamePromptSentinel;
            if (!isNamePromptChapter)
            {
                CurrentTitle = chapter.playerTitle;
            }

            OnChapterUnlocked?.Invoke(chapter);

            if (isNamePromptChapter)
            {
                OnNamePromptRequested?.Invoke(chapter);
            }

            if (DialogueManager.Instance != null && !string.IsNullOrWhiteSpace(chapter.cogsReactionLine))
            {
                DialogueManager.Instance.EnqueueDirectLine(chapter.cogsReactionLine);
            }

            GameManager.Instance?.RequestSave();
        }

        private static bool IsConditionMet(ChapterData chapter)
        {
            switch (chapter.unlockConditionType)
            {
                case ChapterUnlockConditionType.CumulativeBrainPower:
                    return CurrencyManager.Instance != null
                        && CurrencyManager.Instance.CumulativeBrainPower >= chapter.unlockThreshold;

                case ChapterUnlockConditionType.RebirthCount:
                    return RebirthManager.Instance != null
                        && RebirthManager.Instance.RebirthCount >= chapter.unlockThreshold;

                case ChapterUnlockConditionType.PointsSpent:
                    // Aliased to current Points balance: nothing spends Points yet, so "current"
                    // and "lifetime spent" are the same number today. Revisit if/when a real
                    // Points-spending mechanic is built.
                    return CurrencyManager.Instance != null
                        && CurrencyManager.Instance.CurrentPoints >= chapter.unlockThreshold;

                case ChapterUnlockConditionType.WorldRestorationPercent:
                    return WorldRestorationManager.Instance != null
                        && WorldRestorationManager.Instance.RestorationPercent >= chapter.unlockThreshold;

                default:
                    return false;
            }
        }

        private ChapterData FindChapterByNumber(int number)
        {
            for (int i = 0; i < chapters.Count; i++)
            {
                if (chapters[i] != null && chapters[i].chapterNumber == number)
                {
                    return chapters[i];
                }
            }

            return null;
        }

        private string ResolveTitleUpToChapter(int chapterNumber)
        {
            string resolvedTitle = "";

            for (int i = 0; i < chapters.Count; i++)
            {
                ChapterData chapter = chapters[i];
                if (chapter == null || chapter.chapterNumber > chapterNumber)
                {
                    continue;
                }

                if (chapter.playerTitle == NamePromptSentinel)
                {
                    continue;
                }

                resolvedTitle = chapter.playerTitle;
            }

            return resolvedTitle;
        }

        private void SortChapters()
        {
            chapters.RemoveAll(chapter => chapter == null);
            chapters.Sort((a, b) => a.chapterNumber.CompareTo(b.chapterNumber));
        }
    }
}
