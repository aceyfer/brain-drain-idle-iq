using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Handles player taps on a full-screen area or 2D button.
    /// Rewards Brains and restores a small amount of global IQ per tap.
    /// </summary>
    public sealed class PlayerTapHandler : MonoBehaviour
    {
        [Header("Tap Rewards")]
        [SerializeField] private double baseTapBrains = 1d;
        [SerializeField] private double tapMultiplier = 1d;
        [SerializeField] private float iqRestorePerTap = 0.5f;

        [Header("References")]
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private IQDecaySystem iqDecaySystem;

        /// <summary>Brains awarded on each tap before multipliers.</summary>
        public double BaseTapBrains => baseTapBrains;

        /// <summary>Current tap payout multiplier.</summary>
        public double TapMultiplier => tapMultiplier;

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>
        /// Processes a player tap. Wire this to a UI Button or input event.
        /// </summary>
        public void OnTap()
        {
            if (currencyManager == null || iqDecaySystem == null)
            {
                ResolveReferences();

                if (currencyManager == null || iqDecaySystem == null)
                {
                    Debug.LogWarning("[PlayerTapHandler] Missing core references; tap ignored.", this);
                    return;
                }
            }

            double brainsEarned = baseTapBrains * tapMultiplier;
            currencyManager.AddBrains(brainsEarned);
            iqDecaySystem.RestoreIQ(iqRestorePerTap);
        }

        /// <summary>Sets the tap payout multiplier for upgrades and temporary boosts.</summary>
        public void SetTapMultiplier(double multiplier)
        {
            tapMultiplier = multiplier < 0d ? 0d : multiplier;
        }

        private void ResolveReferences()
        {
            if (currencyManager == null && GameManager.Instance != null)
            {
                currencyManager = GameManager.Instance.Currency;
            }

            if (iqDecaySystem == null && GameManager.Instance != null)
            {
                iqDecaySystem = GameManager.Instance.IQDecay;
            }
        }
    }
}
