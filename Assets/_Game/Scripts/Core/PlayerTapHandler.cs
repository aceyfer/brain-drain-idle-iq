using System;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [Tooltip("Optional. The visible tap button's own RectTransform/Transform, punch-scaled on every tap. No effect if unset.")]
        [SerializeField] private Transform tapButtonVisual;

        /// <summary>Brain Power awarded on each tap before multipliers.</summary>
        public double BaseTapBrainPower => baseTapBrainPower;

        /// <summary>Current tap payout multiplier.</summary>
        public double TapMultiplier => tapMultiplier;

        /// <summary>Fired after a successful tap with the Brain Power earned, for UI feedback (e.g. HUDController's IQ-text flash) that shouldn't be driven directly from Core.</summary>
        public event Action<double> OnTapRewardEarned;

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
            PlayerIQManager.Instance?.RestoreIQFromTap();

            PlayerCharacterController.Instance?.NotifyTap();
            AnimationController.PlayButtonPunch(tapButtonVisual);

            RectTransform particleParent = particleContainer != null ? particleContainer : FindParticleContainer();
            if (particleParent != null)
            {
                Vector2 pointerPosition = GetPointerPosition();
                AnimationController.PlaySplatParticles(pointerPosition, particleParent);
                AnimationController.PlayFloatingRewardText($"+{NumberFormatter.Format(brainPowerEarned)} BRAIN POWER", pointerPosition, particleParent);
            }

            OnTapRewardEarned?.Invoke(brainPowerEarned);
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

        /// <summary>
        /// Returns the current pointer (touch or mouse) screen position via the Input System.
        /// Falls back to the screen center if no pointer device is present (e.g. programmatic taps).
        /// </summary>
        private static Vector2 GetPointerPosition()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null)
            {
                return pointer.position.ReadValue();
            }

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }
}
