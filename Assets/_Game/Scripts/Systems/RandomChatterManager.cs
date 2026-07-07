using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Self-bootstrapping singleton holding three tiers of ambient one-liner "chatter" text:
    /// Tier 1 (food / brain rot), Tier 2 (Illumisnotti paranoia), and a Tier 3 profanity pack
    /// that's excluded from GetRandomLine's pool until UnlockProfanity() is called. The
    /// profanity unlock persists via PlayerPrefs (not the main SaveManager/PlayerData JSON
    /// save), independent of any other system.
    /// </summary>
    public sealed class RandomChatterManager : MonoBehaviour
    {
        private const string ProfanityUnlockedPrefsKey = "BrainDrain_ProfanityUnlocked";
        private const string ProfanityEnabledPrefsKey = "BrainDrain_ProfanityEnabled";
        private const string SystemsParentName = "_Systems";

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
            "Snake utters taste better cold",
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

        [Header("Tier 2 -- Illumisnotti Paranoia")]
        [SerializeField]
        private List<string> tierTwoLines = new List<string>
        {
            "The Illumisnotty put something in my Stupaid",
            "They're hiding the good corn from us",
            "I saw a shadow man near my mailbox again",
            "My microwave is reporting my thoughts to someone",
            "The Illumisnotty don't want you to know about snake utters",
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
            "I don't know what the hell snake utters are but I bought twelve",
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
        /// Returns ambient chatter keyed to the player's current Idiocracy Game Rank index.
        /// Low ranks skew Tier 1 (food rot), mid ranks mix Tier 2 (Illumisnotti), high ranks
        /// blend all tiers with optional Tier 3 when unlocked and enabled.
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
                if (includeTierThree)
                {
                    pool.AddRange(tierThreeLines);
                }
            }

            if (pool.Count == 0)
            {
                return string.Empty;
            }

            return pool[Random.Range(0, pool.Count)];
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
