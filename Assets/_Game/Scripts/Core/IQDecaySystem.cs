using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Known decay modifier source identifiers and preset values for buildings.
    /// </summary>
    public static class DecayModifierSources
    {
        public const string University = "University";

        /// <summary>Global decay multiplier applied by the University (-15%).</summary>
        public const float UniversityDecayMultiplier = 0.85f;
    }

    /// <summary>
    /// Tracks global IQ, cumulative-Brains-driven level escalation, and external decay modifiers
    /// from structures such as the Library or University.
    /// </summary>
    public sealed class IQDecaySystem : MonoBehaviour
    {
        private const float StartingIQ = 100f;
        private const float MinimumIQ = 0f;
        private const int LevelsPerTier = 10;
        private const double BrainsPerLevelUnit = 500.0;
        private const float LevelProgressionExponent = 1f / 2.2f;

        [Header("Decay Tuning")]
        [SerializeField] private float baseDecayRate = 0.25f;
        [SerializeField] private float tierEscalationMultiplier = 2.5f;

        private readonly Dictionary<string, float> additiveModifiers = new(8);
        private readonly Dictionary<string, float> multiplicativeModifiers = new(4);

        private float currentIQ = StartingIQ;
        private int currentLevel = 1;
        private float netDecayRate;

        /// <summary>Convenient accessor routed through GameManager when available.</summary>
        public static IQDecaySystem Instance
        {
            get
            {
                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.IQDecay;
                }

                return FindAnyObjectByType<IQDecaySystem>();
            }
        }

        /// <summary>Current global IQ value.</summary>
        public float CurrentIQ => currentIQ;

        /// <summary>Current brain-drain level derived from cumulative Brains earned.</summary>
        public int CurrentLevel => currentLevel;

        /// <summary>Effective decay per second after tier escalation and modifiers.</summary>
        public float NetDecayRate => netDecayRate;

        /// <summary>Fired when IQ changes. Passes the new IQ value.</summary>
        public event Action<float> OnIQChanged;

        /// <summary>Fired when level changes. Passes the new level.</summary>
        public event Action<int> OnLevelChanged;

        /// <summary>Fired when the net decay rate changes. Passes the new rate.</summary>
        public event Action<float> OnDecayRateChanged;

        private void Start()
        {
            SubscribeToGameTick();
            SubscribeToCurrencyUpdates();
            UpdateLevelFromCumulativeBrains(CurrencyManager.Instance != null
                ? CurrencyManager.Instance.CumulativeBrains
                : 0d);
            RecalculateNetDecayRate(emitEvent: true);
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameTick();
            UnsubscribeFromCurrencyUpdates();
        }

        /// <summary>
        /// Registers or updates a decay modifier from an external source.
        /// Additive modifiers subtract a flat amount from gross decay.
        /// Multiplicative modifiers scale net decay after additive reductions (e.g. University x0.85).
        /// </summary>
        public void AddModifier(string source, float amount, bool multiplicative = false)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                Debug.LogWarning("[IQDecaySystem] AddModifier ignored: source name is empty.", this);
                return;
            }

            if (multiplicative)
            {
                if (amount <= 0f)
                {
                    Debug.LogWarning($"[IQDecaySystem] Multiplicative modifier from '{source}' must be greater than zero.", this);
                    return;
                }

                multiplicativeModifiers[source] = amount;
            }
            else
            {
                if (amount < 0f)
                {
                    Debug.LogWarning($"[IQDecaySystem] AddModifier clamped negative amount from '{source}'.", this);
                    amount = 0f;
                }

                additiveModifiers[source] = amount;
            }

            RecalculateNetDecayRate(emitEvent: true);
        }

        /// <summary>Removes a previously registered additive or multiplicative decay modifier.</summary>
        public void RemoveModifier(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            bool removed = additiveModifiers.Remove(source) | multiplicativeModifiers.Remove(source);

            if (removed)
            {
                RecalculateNetDecayRate(emitEvent: true);
            }
        }

        /// <summary>Applies the University's passive global decay reduction (-15%).</summary>
        public void ApplyUniversityModifier()
        {
            AddModifier(DecayModifierSources.University, DecayModifierSources.UniversityDecayMultiplier, multiplicative: true);
        }

        /// <summary>Removes the University's passive global decay reduction.</summary>
        public void RemoveUniversityModifier()
        {
            RemoveModifier(DecayModifierSources.University);
        }

        /// <summary>
        /// Restores global IQ, such as when the player educates the populace via tapping.
        /// </summary>
        public void RestoreIQ(float amount)
        {
            if (amount <= 0f || currentIQ >= StartingIQ)
            {
                return;
            }

            float previousIQ = currentIQ;
            currentIQ = Mathf.Min(StartingIQ, currentIQ + amount);

            if (Mathf.Approximately(previousIQ, currentIQ))
            {
                return;
            }

            OnIQChanged?.Invoke(currentIQ);
        }

        /// <summary>
        /// Directly applies a signed IQ delta (e.g. from a random event), clamped to [0, 100].
        /// Unlike RestoreIQ, this allows IQ to decrease as well as increase.
        /// </summary>
        public void ModifyIQNatively(float delta)
        {
            float previousIQ = currentIQ;
            currentIQ = Mathf.Clamp(currentIQ + delta, MinimumIQ, StartingIQ);

            if (!Mathf.Approximately(previousIQ, currentIQ))
            {
                OnIQChanged?.Invoke(currentIQ);
            }
        }

        /// <summary>
        /// Resets IQ to full and clears all active decay modifiers (e.g. Chaos spikes,
        /// University, the Literal Library) so a rebirth starts with a clean decay state.
        /// </summary>
        public void ResetDecayState()
        {
            currentIQ = StartingIQ;
            additiveModifiers.Clear();
            multiplicativeModifiers.Clear();

            RecalculateNetDecayRate(emitEvent: true);
            OnIQChanged?.Invoke(currentIQ);
        }

        private void SubscribeToGameTick()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[IQDecaySystem] GameManager.Instance is null; cannot subscribe to tick.", this);
                return;
            }

            GameManager.Instance.OnSecondTick -= HandleSecondTick;
            GameManager.Instance.OnSecondTick += HandleSecondTick;
        }

        private void UnsubscribeFromGameTick()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnSecondTick -= HandleSecondTick;
        }

        private void SubscribeToCurrencyUpdates()
        {
            if (CurrencyManager.Instance == null)
            {
                Debug.LogError("[IQDecaySystem] CurrencyManager.Instance is null; cannot subscribe to Brains updates.", this);
                return;
            }

            CurrencyManager.Instance.OnCumulativeBrainsChanged -= HandleCumulativeBrainsChanged;
            CurrencyManager.Instance.OnCumulativeBrainsChanged += HandleCumulativeBrainsChanged;
        }

        private void UnsubscribeFromCurrencyUpdates()
        {
            if (CurrencyManager.Instance == null)
            {
                return;
            }

            CurrencyManager.Instance.OnCumulativeBrainsChanged -= HandleCumulativeBrainsChanged;
        }

        private void HandleCumulativeBrainsChanged(double cumulativeBrains)
        {
            UpdateLevelFromCumulativeBrains(cumulativeBrains);
        }

        private void HandleSecondTick()
        {
            if (currentIQ <= MinimumIQ || netDecayRate <= 0f)
            {
                return;
            }

            float previousIQ = currentIQ;
            currentIQ = Mathf.Max(MinimumIQ, currentIQ - netDecayRate);

            if (!Mathf.Approximately(previousIQ, currentIQ))
            {
                OnIQChanged?.Invoke(currentIQ);
            }
        }

        private void UpdateLevelFromCumulativeBrains(double cumulativeBrains)
        {
            int newLevel = CalculateLevelFromCumulativeBrains(cumulativeBrains);
            if (newLevel == currentLevel)
            {
                return;
            }

            currentLevel = newLevel;
            OnLevelChanged?.Invoke(currentLevel);
            RecalculateNetDecayRate(emitEvent: true);
        }

        private void RecalculateNetDecayRate(bool emitEvent)
        {
            int tier = (currentLevel - 1) / LevelsPerTier;
            float tierMultiplier = Mathf.Pow(tierEscalationMultiplier, tier);
            float grossDecayRate = baseDecayRate * tierMultiplier;

            float totalReduction = 0f;
            foreach (float amount in additiveModifiers.Values)
            {
                totalReduction += amount;
            }

            float newRate = Mathf.Max(0f, grossDecayRate - totalReduction);

            foreach (float multiplier in multiplicativeModifiers.Values)
            {
                newRate *= multiplier;
            }

            if (emitEvent && !Mathf.Approximately(netDecayRate, newRate))
            {
                netDecayRate = newRate;
                OnDecayRateChanged?.Invoke(netDecayRate);
                return;
            }

            netDecayRate = newRate;
        }

        private static int CalculateLevelFromCumulativeBrains(double cumulativeBrains)
        {
            if (cumulativeBrains <= 0d)
            {
                return 1;
            }

            float normalizedBrains = (float)(cumulativeBrains / BrainsPerLevelUnit);
            return Mathf.FloorToInt(Mathf.Pow(normalizedBrains, LevelProgressionExponent)) + 1;
        }
    }
}
