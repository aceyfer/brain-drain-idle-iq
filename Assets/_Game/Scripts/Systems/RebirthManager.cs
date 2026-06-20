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

            Debug.Log($"Rebirth #{RebirthCount} successful! Added +{FlatMultiplierBonusPerRebirth:P0} to the global income multiplier, +{FlatCashMultiplierBonusPerRebirth:P0} to the Cash multiplier, and +{FlatPointsConversionRateBonusPerRebirth:P0} to the Points conversion rate.");

            GameManager.Instance?.RequestSave();
        }

        /// <summary>Directly restores RebirthCount from a save file.</summary>
        public void LoadState(int restoredRebirthCount)
        {
            RebirthCount = restoredRebirthCount;
            OnRebirthCountChanged?.Invoke(RebirthCount);
        }
    }
}
