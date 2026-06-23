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

        private static RebirthManager instance;

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
                    var hostObject = new GameObject("RebirthManager (Auto)");
                    instance = hostObject.AddComponent<RebirthManager>();
                }

                return instance;
            }
        }

        /// <summary>Permanent +5% global production bonus granted by each Rebirth.</summary>
        public double PendingMultiplierIncrease => FlatMultiplierBonusPerRebirth;

        /// <summary>How many times the player has rebirthed this session.</summary>
        public int RebirthCount { get; private set; }

        /// <summary>Fired when RebirthCount changes. Passes the new count.</summary>
        public event Action<int> OnRebirthCountChanged;

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

            Debug.Log($"The Snotting #{RebirthCount} complete! You are now {GetIllumisnottiTitle(RebirthCount)}. Added +{FlatMultiplierBonusPerRebirth:P0} to the global income multiplier, +{FlatCashMultiplierBonusPerRebirth:P0} to the Cash multiplier, +{FlatPointsConversionRateBonusPerRebirth:P0} to the Points conversion rate, and +{FlatTapMultiplierBonusPerRebirth:P0} to the tap multiplier.");

            GameManager.Instance?.RequestSave();
        }

        /// <summary>Directly restores RebirthCount from a save file.</summary>
        public void LoadState(int restoredRebirthCount)
        {
            RebirthCount = restoredRebirthCount;
            OnRebirthCountChanged?.Invoke(RebirthCount);
        }

        /// <summary>
        /// The Illumisnotti title earned at a given Snotting (Rebirth) tier, displayed under the
        /// HUD's IQ readout. Added 2026-06-21 as part of the Illumisnotti narrative rewrite --
        /// "Rebirth" is reflavored as "The Snotting" in player-facing text; RebirthCount/
        /// RebirthManager/TriggerRebirth themselves are deliberately NOT renamed in code, to
        /// avoid an invasive rename across every system that already references them.
        /// </summary>
        public static string GetIllumisnottiTitle(int rebirthCount)
        {
            switch (rebirthCount)
            {
                case 0: return string.Empty;
                case 1: return "Junior Associate Snott";
                case 2: return "Regional Snott Manager";
                case 3: return "Vice President of Snottery";
                case 4: return "Lord Snott (Provisional)";
                case 5: return "Grand Illumisnotti";
                default: return "Supreme Snott Eternal";
            }
        }
    }
}
