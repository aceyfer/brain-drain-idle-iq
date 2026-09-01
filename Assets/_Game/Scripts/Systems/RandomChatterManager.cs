using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Self-bootstrapping singleton holding three tiers of ambient one-liner "chatter" text:
    /// Tier 1 (food / brain rot), Tier 2 (Illumisnotty paranoia), and a Tier 3 profanity pack
    /// that's excluded from GetRandomLine's pool until UnlockProfanity() is called. The
    /// profanity unlock persists via PlayerPrefs (not the main SaveManager/PlayerData JSON
    /// save), independent of any other system.
    /// </summary>
    public sealed class RandomChatterManager : MonoBehaviour
    {
        private const string ProfanityUnlockedPrefsKey = "BrainDrain_ProfanityUnlocked";
        private const string ProfanityEnabledPrefsKey = "BrainDrain_ProfanityEnabled";
        private const string SystemsParentName = "_Systems";

        /// <summary>Mirrors DialogueManager.MaxHistoryEntries exactly -- same cap, same reasoning.</summary>
        private const int MaxHistoryEntries = 50;

        /// <summary>How many most-recent spoken lines GetLineForRank excludes from its pool, so
        /// ambient chatter doesn't visibly loop (Tier 1's pool is small). Kept below the smallest
        /// tier pool; filtering falls back to the full pool if it would empty, so a line always fires.</summary>
        private const int RecentChatterWindow = 6;

        /// <summary>One recorded pedestrian chatter line for the §24b STREET log tab, mirroring DialogueManager.DialogueLogEntry (minus SourceLine -- chatter lines are plain strings, not NarratorLine assets).</summary>
        public readonly struct ChatterLogEntry
        {
            public readonly string Text;
            public readonly float SessionTime;

            public ChatterLogEntry(string text, float sessionTime)
            {
                Text = text;
                SessionTime = sessionTime;
            }
        }

        private static RandomChatterManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static RandomChatterManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<RandomChatterManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("RandomChatterManager (Auto)");
                    instance = hostObject.AddComponent<RandomChatterManager>();
                }

                return instance;
            }
        }

        [Header("Tier 1 -- Food / Brain Rot")]
        [SerializeField]
        private List<string> tierOneLines = new List<string>
        {
            "My corndog fell out of my pocket again",
            "Oh no, my iguana drank my Stupaid",
            "I don't know what this is but I'm gonna sniff it",
            "Armadillo sauce goes with everything",
            "I've still got the last bag of discontinued Cheese Dirt and I'm not sharing a speck of it",
            "I found a Stupaid under the couch and it was still good",
            "They discontinued my favorite flavor, Cheese Dirt",
            "My left shoe has been making decisions for me",
            "I traded my refrigerator for a bucket of Stupaid",
            "The label says do not drink but it don't say why",
            "I been eating the same corndog since Tuesday",
            "Stupaid Zero still has the brain taste though",
            "My cat only responds to the Stupaid jingle",
            "I microwaved my Stupaid for forty minutes on accident",
            "They put something in the armadillo sauce and I want more of it",
        };

        [Header("Tier 2 -- Illumisnotty Paranoia")]
        [SerializeField]
        private List<string> tierTwoLines = new List<string>
        {
            "The Illumisnotty put something in my Stupaid",
            "They're hiding the good corn from us",
            "I saw a shadow man near my mailbox again",
            "My microwave is reporting my thoughts to someone",
            "The Illumisnotty microchipped my left shoe and now it won't let me turn left",
            "Every time I think too hard my nose bleeds a little",
            "They cancelled that show because it was making us smart",
            "The Illumisnotty replaced my neighbor with a quieter one",
            "I stopped sleeping and now I can see the grid",
            "They put the mind control in the store brand not the name brand",
            "My dreams have been sponsored by someone I never agreed to",
            "The shadow people took my good Stupaid flavor",
            "I drew a map of their plan but then I ate it",
            "Every helicopter I see is looking specifically at me",
            "The Illumisnotty are scared of armadillo sauce and that's why it's rare",
        };

        [Header("Tier 3 -- Profanity Pack (locked by default)")]
        [SerializeField]
        private List<string> tierThreeLines = new List<string>
        {
            "I don't know who's putting fluoride in the Stupaid but goddamn it works, I feel great",
            "My corndog fell in the damn toilet and I had to think about it",
            "The Illumisnotty can kiss my ass, I found the good Stupaid",
            "What the hell is armadillo sauce and why does it taste like home",
            "I accidentally drank my iguana's Stupaid and honestly it slapped",
            "This bastard microwave keeps reporting my thoughts",
            "I don't know what I'm sniffing but I'll be damned if I stop",
            "The shadow man showed up again and I told him to get the hell out",
            "They discontinued Cheese Dirt flavor and I am so damn mad",
            "My left shoe told me to do something and I said hell no",
            "I traded my fridge for Stupaid and I'd do it again no question",
            "The Illumisnotty are hiding the good corn and that's bull",
            "I been eating this corndog for four days, damn thing won't end",
            "My cat only responds to profanity and the Stupaid jingle",
            "I drew their whole damn plan out and then I sat on it",
        };

        [SerializeField] private bool profanityUnlocked = false;
        [SerializeField] private bool profanityEnabled = false;

        /// <summary>Whether the Tier 3 pack has ever been unlocked. Permanent once true.</summary>
        public bool ProfanityUnlocked => profanityUnlocked;

        /// <summary>Whether the player currently wants Tier 3 lines turned on, independent of unlock status. Toggleable via ToggleProfanity.</summary>
        public bool ProfanityEnabled => profanityEnabled;

        /// <summary>Fired when profanity unlocked or enabled/disabled states change.</summary>
        public event System.Action OnProfanitySettingsChanged;

        private readonly List<ChatterLogEntry> history = new List<ChatterLogEntry>();

        /// <summary>Read-only view of the session's pedestrian chatter history (oldest first), capped at MaxHistoryEntries -- backs the §24b STREET log tab.</summary>
        public IReadOnlyList<ChatterLogEntry> History => history;

        /// <summary>Fired whenever a line is appended to History -- consumed by DialogueLogPanelUI's STREET tab (§24b).</summary>
        public event System.Action OnHistoryChanged;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent != null)
            {
                transform.SetParent(systemsParent.transform, false);
            }

            profanityUnlocked = PlayerPrefs.GetInt(ProfanityUnlockedPrefsKey, 0) == 1;
            profanityEnabled = PlayerPrefs.GetInt(ProfanityEnabledPrefsKey, 0) == 1;
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        /// <summary>
        /// Picks a random line from Tier 1 + Tier 2. Tier 3 only joins the pool once both
        /// profanityUnlocked and profanityEnabled are true.
        /// </summary>
        public string GetRandomLine()
        {
            return GetLineForRank(0);
        }

        /// <summary>
        /// Returns ambient chatter keyed to the player's current Idiocracy Game Rank index
        /// (== World Restoration stage, see GameManager.UpdateRankFromRestorationStage). Low
        /// ranks skew Tier 1 (food rot), mid ranks mix in Tier 2 (Illumisnotty).
        /// Tier 3 (Bad Words Pack) is layered on top at EVERY rank once unlocked and enabled --
        /// 2026-08-31 fix: it used to be additionally gated behind rankIndex > 3, so a player who
        /// bought the pack early (most likely purchase moment) heard zero difference until
        /// reaching a late-game stage, contradicting the store description ("Toggle them on/off
        /// anytime once owned") and the intended design (existing tier lines stay, profanity is
        /// an added layer of evolution, not a late unlock).
        /// </summary>
        public string GetLineForRank(int rankIndex)
        {
            bool includeTierThree = profanityUnlocked && profanityEnabled;
            var pool = new List<string>(32);

            if (rankIndex <= 1)
            {
                pool.AddRange(tierOneLines);
            }
            else if (rankIndex <= 3)
            {
                pool.AddRange(tierOneLines);
                pool.AddRange(tierTwoLines);
            }
            else
            {
                pool.AddRange(tierOneLines);
                pool.AddRange(tierTwoLines);
            }

            if (includeTierThree)
            {
                pool.AddRange(tierThreeLines);
            }

            if (pool.Count == 0)
            {
                return string.Empty;
            }

            // Anti-repeat: drop any line spoken in the last RecentChatterWindow history entries.
            // If that would empty the pool (tiny pool + long window), fall back to the full pool so
            // a line always fires. Mirrors DialogueManager's last-N narrator anti-repeat.
            int windowStart = Mathf.Max(0, history.Count - RecentChatterWindow);
            HashSet<string> recent = new HashSet<string>();
            for (int i = windowStart; i < history.Count; i++)
            {
                recent.Add(history[i].Text);
            }

            List<string> pickFrom = new List<string>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                if (!recent.Contains(pool[i]))
                {
                    pickFrom.Add(pool[i]);
                }
            }
            if (pickFrom.Count == 0)
            {
                pickFrom = pool;
            }

            return pickFrom[Random.Range(0, pickFrom.Count)];
        }

        /// <summary>Permanently unlocks the Tier 3 profanity pack and persists the choice to PlayerPrefs.</summary>
        public void UnlockProfanity()
        {
            if (!profanityUnlocked)
            {
                profanityUnlocked = true;
                PlayerPrefs.SetInt(ProfanityUnlockedPrefsKey, 1);
                PlayerPrefs.Save();
                OnProfanitySettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Turns Tier 3 lines on/off independent of unlock status, and persists the choice to
        /// PlayerPrefs separately from profanityUnlocked.
        /// </summary>
        public void ToggleProfanity(bool enabled)
        {
            if (profanityEnabled != enabled)
            {
                profanityEnabled = enabled;
                PlayerPrefs.SetInt(ProfanityEnabledPrefsKey, enabled ? 1 : 0);
                PlayerPrefs.Save();
                OnProfanitySettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Records a chatter line into History at the moment it's actually spoken (called from
        /// BackgroundPedestrianManager's bubble spawn site, right after SetText) -- deliberately
        /// NOT called from inside GetLineForRank/GetRandomLine, since those could be invoked
        /// speculatively without a bubble ever actually displaying the result.
        /// </summary>
        public void RecordSpokenLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            history.Add(new ChatterLogEntry(text, Time.unscaledTime));
            while (history.Count > MaxHistoryEntries)
            {
                history.RemoveAt(0);
            }

            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Restores state from main save JSON, with PlayerPrefs as migration fallback.
        /// </summary>
        public void LoadState(bool restoredUnlocked, bool restoredEnabled)
        {
            bool changed = false;

            if (profanityUnlocked != restoredUnlocked)
            {
                profanityUnlocked = restoredUnlocked;
                changed = true;
            }

            if (profanityEnabled != restoredEnabled)
            {
                profanityEnabled = restoredEnabled;
                changed = true;
            }

            if (changed)
            {
                OnProfanitySettingsChanged?.Invoke();
            }
        }
    }
}
