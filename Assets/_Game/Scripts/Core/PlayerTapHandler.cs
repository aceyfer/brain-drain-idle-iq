using UnityEngine;
using UnityEngine.Serialization;
using BrainDrain.Systems;

namespace BrainDrain.Core
{
    /// <summary>
    /// Handles player taps on a full-screen area or 2D button.
    /// Rewards Brain Power per tap and triggers tap/idle feedback animations.
    /// </summary>
    public sealed class PlayerTapHandler : MonoBehaviour
    {
        [Header("Tap Rewards")]
        [FormerlySerializedAs("baseTapBrains")]
        [SerializeField] private double baseTapBrainPower = 1d;
        [SerializeField] private double tapMultiplier = 1d;

        [Header("References")]
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Feedback Animation")]
        [Tooltip("RectTransform under a Canvas that goo splat particles spawn into. Falls back to the first Canvas found in the scene.")]
        [SerializeField] private RectTransform particleContainer;

        /// <summary>Brain Power awarded on each tap before multipliers.</summary>
        public double BaseTapBrainPower => baseTapBrainPower;

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
            if (currencyManager == null)
            {
                ResolveReferences();

                if (currencyManager == null)
                {
                    Debug.LogWarning("[PlayerTapHandler] Missing core references; tap ignored.", this);
                    return;
                }
            }

            double brainPowerEarned = baseTapBrainPower * tapMultiplier;
            currencyManager.AddBrainPower(brainPowerEarned);

            PlayerCharacterController.Instance?.NotifyTap();

            RectTransform particleParent = particleContainer != null ? particleContainer : FindParticleContainer();
            if (particleParent != null)
            {
                AnimationController.PlaySplatParticles(Input.mousePosition, particleParent);
            }
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
        }

        private static RectTransform FindParticleContainer()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : null;
        }
    }
}
