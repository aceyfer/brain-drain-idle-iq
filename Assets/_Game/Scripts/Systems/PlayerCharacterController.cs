using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    public enum PlayerCharacterState
    {
        Idle,
        Bored,
        Tapping,
        Excited
    }

    /// <summary>
    /// Drives the on-screen Player Character's behavioral state machine and its independent
    /// (non-COGS) appearance progression. Idle/Bored/Excited are ambient loops handed to
    /// AnimationController; Tapping is the one-shot squash/stretch reaction to PlayerTapHandler.
    /// Bored kicks in after sitting idle too long; Excited fires from building purchases,
    /// rebirths, and IQ milestones, then both fall back to Idle on a short hold timer.
    /// </summary>
    public sealed class PlayerCharacterController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform characterVisualTarget;
        [SerializeField] private SpriteRenderer appearanceRenderer;
        [SerializeField] private List<CharacterAppearanceStage> appearanceStages = new();

        [Header("Mood Tuning")]
        [SerializeField] private float boredAfterIdleSeconds = 20f;
        [SerializeField] private float tappingHoldSeconds = 0.35f;
        [SerializeField] private float excitedHoldSeconds = 2.5f;

        private static PlayerCharacterController instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static PlayerCharacterController Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<PlayerCharacterController>();
                if (instance == null)
                {
                    var hostObject = new GameObject("PlayerCharacterController (Auto)");
                    instance = hostObject.AddComponent<PlayerCharacterController>();
                }

                return instance;
            }
        }

        public PlayerCharacterState CurrentState { get; private set; } = PlayerCharacterState.Idle;

        public CharacterAppearanceStage CurrentAppearanceStage { get; private set; }

        /// <summary>Fired whenever the resolved state actually changes (repeated Tapping re-entries excluded). Passes the new state.</summary>
        public event Action<PlayerCharacterState> OnStateChanged;

        private bool hasEnteredInitialState;
        private Coroutine boredWatcherCoroutine;
        private Coroutine returnToIdleCoroutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[PlayerCharacterController] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SortAppearanceStages();
        }

        private void Start()
        {
            ApplyAppearanceForRebirthCount(RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0);
            EnterState(PlayerCharacterState.Idle);

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>Call on every player tap. Always wins immediately and restarts the return-to-Idle hold timer.</summary>
        public void NotifyTap()
        {
            EnterState(PlayerCharacterState.Tapping);
            RestartReturnToIdle(tappingHoldSeconds);
        }

        private void SubscribeToEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
            }

            if (PlayerIQManager.Instance != null)
            {
                PlayerIQManager.Instance.OnIQMilestoneCrossed -= HandleIQMilestoneCrossed;
                PlayerIQManager.Instance.OnIQMilestoneCrossed += HandleIQMilestoneCrossed;
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnBuildingPurchased -= HandleBuildingPurchased;
                UpgradeManager.Instance.OnBuildingPurchased += HandleBuildingPurchased;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }

            if (PlayerIQManager.Instance != null)
            {
                PlayerIQManager.Instance.OnIQMilestoneCrossed -= HandleIQMilestoneCrossed;
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnBuildingPurchased -= HandleBuildingPurchased;
            }
        }

        private void HandleRebirthCountChanged(int rebirthCount)
        {
            ApplyAppearanceForRebirthCount(rebirthCount);
            TriggerExcited();
        }

        private void HandleIQMilestoneCrossed(float _) => TriggerExcited();

        private void HandleBuildingPurchased(BuildingData _) => TriggerExcited();

        private void TriggerExcited()
        {
            EnterState(PlayerCharacterState.Excited);
            RestartReturnToIdle(excitedHoldSeconds);
        }

        private void RestartReturnToIdle(float afterSeconds)
        {
            if (returnToIdleCoroutine != null)
            {
                StopCoroutine(returnToIdleCoroutine);
            }

            returnToIdleCoroutine = StartCoroutine(ReturnToIdleAfter(afterSeconds));
        }

        private IEnumerator ReturnToIdleAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            EnterState(PlayerCharacterState.Idle);
        }

        private void StartBoredWatcher()
        {
            StopBoredWatcher();
            boredWatcherCoroutine = StartCoroutine(BoredWatcherRoutine());
        }

        private void StopBoredWatcher()
        {
            if (boredWatcherCoroutine != null)
            {
                StopCoroutine(boredWatcherCoroutine);
                boredWatcherCoroutine = null;
            }
        }

        private IEnumerator BoredWatcherRoutine()
        {
            yield return new WaitForSeconds(boredAfterIdleSeconds);
            EnterState(PlayerCharacterState.Bored);
        }

        private void EnterState(PlayerCharacterState newState)
        {
            PlayerCharacterState previous = CurrentState;
            bool isNoOpReentry = hasEnteredInitialState && previous == newState && newState != PlayerCharacterState.Tapping;
            if (isNoOpReentry)
            {
                return;
            }

            if (hasEnteredInitialState)
            {
                ExitVisualsFor(previous);
            }

            CurrentState = newState;
            EnterVisualsFor(newState);
            hasEnteredInitialState = true;

            OnStateChanged?.Invoke(newState);

            if (newState == PlayerCharacterState.Idle)
            {
                StartBoredWatcher();
            }
            else if (previous == PlayerCharacterState.Idle)
            {
                StopBoredWatcher();
            }
        }

        private void EnterVisualsFor(PlayerCharacterState state)
        {
            if (characterVisualTarget == null)
            {
                return;
            }

            switch (state)
            {
                case PlayerCharacterState.Idle:
                    AnimationController.PlayIdleBreathing(characterVisualTarget);
                    break;
                case PlayerCharacterState.Bored:
                    AnimationController.PlayBoredFidget(characterVisualTarget);
                    break;
                case PlayerCharacterState.Tapping:
                    AnimationController.PlayTapAnim(characterVisualTarget);
                    break;
                case PlayerCharacterState.Excited:
                    AnimationController.PlayExcitedBounce(characterVisualTarget);
                    break;
            }
        }

        private void ExitVisualsFor(PlayerCharacterState state)
        {
            if (characterVisualTarget == null)
            {
                return;
            }

            switch (state)
            {
                case PlayerCharacterState.Idle:
                    AnimationController.StopIdleBreathing(characterVisualTarget);
                    break;
                case PlayerCharacterState.Bored:
                    AnimationController.StopBoredFidget(characterVisualTarget);
                    break;
                case PlayerCharacterState.Excited:
                    AnimationController.StopExcitedBounce(characterVisualTarget);
                    break;
                case PlayerCharacterState.Tapping:
                    break;
            }
        }

        private void ApplyAppearanceForRebirthCount(int rebirthCount)
        {
            CharacterAppearanceStage resolved = ResolveAppearanceStage(rebirthCount);
            if (resolved == null || resolved == CurrentAppearanceStage)
            {
                return;
            }

            CurrentAppearanceStage = resolved;

            if (appearanceRenderer != null)
            {
                appearanceRenderer.sprite = resolved.sprite;
            }
        }

        private CharacterAppearanceStage ResolveAppearanceStage(int rebirthCount)
        {
            CharacterAppearanceStage resolved = null;

            for (int i = 0; i < appearanceStages.Count; i++)
            {
                CharacterAppearanceStage stage = appearanceStages[i];
                if (stage == null)
                {
                    continue;
                }

                if (rebirthCount >= stage.minRebirthCount)
                {
                    resolved = stage;
                }
                else
                {
                    break;
                }
            }

            return resolved;
        }

        private void SortAppearanceStages()
        {
            appearanceStages.RemoveAll(stage => stage == null);
            appearanceStages.Sort((a, b) => a.minRebirthCount.CompareTo(b.minRebirthCount));
        }
    }
}
