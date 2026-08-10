using System;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    public class RebirthManager : MonoBehaviour
    {
        private const double FlatMultiplierBonusPerRebirth = 0.05;
        private const double FlatCashMultiplierBonusPerRebirth = 0.1;
        private const double FlatPointsConversionRateBonusPerRebirth = 0.05;
        private const double FlatTapMultiplierBonusPerRebirth = 0.05;

        /// <summary>
        /// Single source of truth for the Snotting-unlock gate. Used to hold three independent
        /// copies (RebirthUIController.pointsSpentUnlockThreshold, DialogueManager's
        /// SnottingReadyThreshold constant, FTUEManager's SnottingReadyThreshold constant) kept
        /// "in sync by convention" -- that convention failed in practice: DialogueManager's and
        /// FTUEManager's copies both drifted to the pre-2026-07-30 figure (50,000) after
        /// RebirthUIController's was retuned to 5,658,229, firing "you can Snot now" narrator
        /// lines days before the button was actually interactable. All three now read
        /// SnottingUnlockThreshold from here instead. See PROJECT_BIBLE.md §6.
        /// </summary>
        [Tooltip("Cumulative RP spent on World Restoration required before the Snotting UI trigger becomes interactable.")]
        [SerializeField] private double pointsSpentUnlockThreshold = 5658229d;

        private static RebirthManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static RebirthManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<RebirthManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("RebirthManager (Auto)");
                    instance = hostObject.AddComponent<RebirthManager>();
                }

                return instance;
            }
        }

        /// <summary>Permanent +5% global production bonus granted by each Rebirth.</summary>
        public double PendingMultiplierIncrease => FlatMultiplierBonusPerRebirth;

        /// <summary>Permanent +10% Cash multiplier bonus granted by each Rebirth.</summary>
        public double PendingCashMultiplierIncrease => FlatCashMultiplierBonusPerRebirth;

        /// <summary>Permanent +5% tap payout multiplier bonus granted by each Rebirth.</summary>
        public double PendingTapMultiplierIncrease => FlatTapMultiplierBonusPerRebirth;

        /// <summary>How many times the player has rebirthed this session.</summary>
        public int RebirthCount { get; private set; }

        /// <summary>The RP-spent threshold that unlocks the Snotting UI trigger. Single owner of this value -- see the backing field's doc comment.</summary>
        public double SnottingUnlockThreshold => pointsSpentUnlockThreshold;

        /// <summary>Fired when RebirthCount changes. Passes the new count.</summary>
        public event Action<int> OnRebirthCountChanged;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
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
        /// Resets current Brain Power and building tiers to zero, increments RebirthCount, and
        /// applies a permanent, stacking +5% global production bonus to all future Brain Power
        /// accumulation.
        /// </summary>
        public void TriggerRebirth()
        {
            if (CurrencyManager.Instance == null) return;

            RebirthCount++;
            OnRebirthCountChanged?.Invoke(RebirthCount);

            if (UpgradeManager.Instance != null) UpgradeManager.Instance.ResetBuildings();

            CurrencyManager.Instance.ExecuteRebirth(
                FlatMultiplierBonusPerRebirth,
                FlatCashMultiplierBonusPerRebirth,
                FlatPointsConversionRateBonusPerRebirth);

            PlayerTapHandler.Instance?.AddTapMultiplier(FlatTapMultiplierBonusPerRebirth);

            WorldRestorationManager.Instance?.ResetProgress();
            BrainDrain.Core.PlayerIQManager.Instance?.ResetForRebirth();

            Debug.Log($"The Snotting #{RebirthCount} complete! You are now {GetIllumisnottyTitle(RebirthCount)}. Added +{FlatMultiplierBonusPerRebirth:P0} to the global income multiplier, +{FlatCashMultiplierBonusPerRebirth:P0} to the Cash multiplier, +{FlatPointsConversionRateBonusPerRebirth:P0} to the Points conversion rate, and +{FlatTapMultiplierBonusPerRebirth:P0} to the tap multiplier.");

            GameManager.Instance?.RequestSave();
        }

        /// <summary>Directly restores RebirthCount from a save file.</summary>
        public void LoadState(int restoredRebirthCount)
        {
            RebirthCount = restoredRebirthCount;
            OnRebirthCountChanged?.Invoke(RebirthCount);
        }

        // OBSOLETE — HUD rankText now uses HUDController.GetStageRankTitle (stage-derived).
        // Remaining callers (illumisnottyTitleText, RebirthUIController, IllumisnottyManagerUI, DebugCheats) not yet migrated.
        /// <summary>
        /// The Illumisnotty title earned at a given Snotting (Rebirth) tier, displayed from game
        /// start under the HUD's IQ readout and in the Illumisnotty panel. "Rebirth" is reflavored
        /// as "The Snotting" in player-facing text; RebirthCount/RebirthManager/TriggerRebirth
        /// themselves are deliberately NOT renamed in code, to avoid an invasive rename across
        /// every system that already references them.
        /// </summary>
        public static string GetIllumisnottyTitle(int rebirthCount)
        {
            if (rebirthCount >= 11) return "BUNKER SUPREME";
            if (rebirthCount >= 6) return "ILLUMISNOTTY INTERN";
            if (rebirthCount >= 4) return "BUNKER BUREAUCRAT";
            if (rebirthCount >= 2) return "UNDER-SNOT ELITE";
            return "SNOTTY ROOKIE";
        }
    }
}
