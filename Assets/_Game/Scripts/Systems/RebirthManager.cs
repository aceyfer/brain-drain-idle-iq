using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    public class RebirthManager : MonoBehaviour
    {
        private const double MultiplierBonusPerSqrtBrain = 0.002;
        private const double MinimumBonusThreshold = 0.1;

        public static RebirthManager Instance { get; private set; }

        /// <summary>Minimum multiplier bonus required to allow a rebirth.</summary>
        public double MinBonusThreshold => MinimumBonusThreshold;

        /// <summary>Multiplier bonus the player would receive if they rebirthed right now.</summary>
        public double PendingMultiplierIncrease
        {
            get
            {
                CurrencyManager currencyManager = CurrencyManager.Instance;
                if (currencyManager == null) return 0d;
                return MultiplierBonusPerSqrtBrain * System.Math.Sqrt(currencyManager.CumulativeBrains);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void TriggerRebirth()
        {
            if (CurrencyManager.Instance == null) return;

            double cumulative = CurrencyManager.Instance.CumulativeBrains;

            // Calculate the permanent multiplier bonus using the square root curve
            double bonus = MultiplierBonusPerSqrtBrain * System.Math.Sqrt(cumulative);

            // Guardrail: Player must earn at least a +0.1 bonus to turn their skull inside out
            if (bonus < MinimumBonusThreshold)
            {
                Debug.LogWarning("Not enough Neuro-Sludge accumulated to rebirth yet!");
                return;
            }

            // Execute hard wipe in precise order
            if (IQDecaySystem.Instance != null) IQDecaySystem.Instance.ResetDecayState();
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.ResetBuildings();

            // Finalize currency reset and apply the meta-progression power spike
            CurrencyManager.Instance.ExecuteRebirth(bonus);

            Debug.Log($"Rebirth successful! Added +{bonus:F3} to the global income multiplier.");
        }
    }
}
