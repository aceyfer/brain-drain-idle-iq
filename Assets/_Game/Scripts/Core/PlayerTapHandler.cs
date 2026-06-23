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

        // -- Illumisnotti rewrite (2026-06-21): timed Illumisnotti random-event modifiers --
        private float tapFrozenUntilTime;
        private double temporaryTapPercent;
        private float temporaryTapExpiryTime;

        [Header("References")]
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Feedback Animation")]
        [Tooltip("RectTransform under a Canvas that goo splat particles spawn into. Falls back to the first Canvas found in the scene.")]
        [SerializeField] private RectTransform particleContainer;
        [Tooltip("Optional. The visible tap button's own RectTransform/Transform, punch-scaled on every tap. No effect if unset.")]
        [SerializeField] private Transform tapButtonVisual;

        private static PlayerTapHandler instance;

        /// <summary>Self-bootstrapping accessor, mirroring the rest of the codebase's singleton pattern -- used by RebirthManager to apply the permanent per-rebirth tap-multiplier bonus and by SaveManager to persist/restore it. No auto-creation: the tap handler is always wired to a real UI button in the scene, so a missing instance means it's genuinely absent, not something worth fabricating.</summary>
        public static PlayerTapHandler Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<PlayerTapHandler>();
                return instance;
            }
        }

        /// <summary>Brain Power awarded on each tap before multipliers.</summary>
        public double BaseTapBrainPower => baseTapBrainPower;

        /// <summary>Current tap payout multiplier.</summary>
        public double TapMultiplier => tapMultiplier;

        /// <summary>Fired after a successful tap with the Brain Power earned, for UI feedback (e.g. HUDController's IQ-text flash) that shouldn't be driven directly from Core.</summary>
        public event Action<double> OnTapRewardEarned;

        private void Awake()
        {
            instance = this;
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

            if (Time.time < tapFrozenUntilTime)
            {
                return;
            }

            double brainPowerEarned = baseTapBrainPower * tapMultiplier * GetTemporaryTapFactor();
            currencyManager.AddBrainPower(brainPowerEarned);
            PlayerIQManager.Instance?.RestoreIQFromTap();

            PlayerCharacterController.Instance?.NotifyTap();
            AnimationController.PlayButtonPunch(tapButtonVisual);

            RectTransform particleParent = particleContainer != null ? particleContainer : FindParticleContainer();
            if (particleParent != null)
            {
                Vector2 pointerPosition = GetPointerPosition();
                AnimationController.PlaySplatParticles(pointerPosition, particleParent);
                AnimationController.PlayTouchRipple(pointerPosition, particleParent);
                AnimationController.PlayFloatingRewardText($"+{NumberFormatter.Format(brainPowerEarned)} BRAIN POWER", pointerPosition, particleParent);
            }

            OnTapRewardEarned?.Invoke(brainPowerEarned);
        }

        /// <summary>Sets the tap payout multiplier directly, e.g. restoring a saved value. Replaces rather than stacks -- use AddTapMultiplier for permanent incremental bonuses.</summary>
        public void SetTapMultiplier(double multiplier)
        {
            tapMultiplier = multiplier < 0d ? 0d : multiplier;
        }

        /// <summary>Permanently increases the tap multiplier by a flat amount. Used by RebirthManager for its small per-rebirth tap bonus, so manual tapping doesn't lose all relevance to idle income across many rebirths.</summary>
        public void AddTapMultiplier(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            tapMultiplier += amount;
        }

        // -- Illumisnotti rewrite (2026-06-21): timed Illumisnotti random-event modifiers --

        /// <summary>Ignores taps entirely until the duration elapses. Used by Snott Tax Audit. Extends rather than shortens an already-active freeze.</summary>
        public void FreezeTapsFor(float durationSeconds)
        {
            tapFrozenUntilTime = Mathf.Max(tapFrozenUntilTime, Time.time + durationSeconds);
        }

        /// <summary>Applies a temporary percent modifier to tap payout for the given duration -- the tap-side half of "all multipliers" events (e.g. Lord Snott Tantrum), since CurrencyManager.ApplyTemporaryAllMultiplierModifier only covers idle/Cash production. A new call replaces any still-active one rather than stacking.</summary>
        public void ApplyTemporaryTapModifier(double percent, float durationSeconds)
        {
            temporaryTapPercent = percent;
            temporaryTapExpiryTime = Time.time + durationSeconds;
        }

        private double GetTemporaryTapFactor()
        {
            if (temporaryTapPercent == 0d || Time.time >= temporaryTapExpiryTime)
            {
                temporaryTapPercent = 0d;
                return 1d;
            }

            return 1d + temporaryTapPercent;
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
