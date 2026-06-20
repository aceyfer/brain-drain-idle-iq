using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Periodically selects a random satirical chaos event and applies its Brain Power/
    /// PlayerIQ effects on demand, decoupled from any UI via the OnRandomEventTriggered event.
    /// </summary>
    public sealed class RandomEventManager : MonoBehaviour
    {
        private const float MinSecondsBetweenEvents = 90f;
        private const float MaxSecondsBetweenEvents = 180f;

        [Header("Event Pool")]
        [SerializeField] private List<BrainRotEventData> potentialEvents = new();

        private float secondsUntilNextEvent;

        private static RandomEventManager instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static RandomEventManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<RandomEventManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("RandomEventManager (Auto)");
                    instance = hostObject.AddComponent<RandomEventManager>();
                }

                return instance;
            }
        }

        /// <summary>Read-only view of the configured event pool.</summary>
        public IReadOnlyList<BrainRotEventData> PotentialEvents => potentialEvents;

        /// <summary>The event most recently selected by TriggerRandomEvent, if any.</summary>
        public BrainRotEventData CurrentEvent { get; private set; }

        /// <summary>Fired when a random event is selected, so UI can display its pop-up modal.</summary>
        public event Action<BrainRotEventData> OnRandomEventTriggered;

        /// <summary>Fired after an event's effects are applied (i.e. the player accepted it, not declined).</summary>
        public event Action<BrainRotEventData> OnEventResolved;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[RandomEventManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            ScheduleNextEvent();
            SubscribeToGameTick();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameTick();

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>Selects a random event from the pool and notifies subscribers (e.g. a popup UI controller).</summary>
        public void TriggerRandomEvent()
        {
            if (potentialEvents == null || potentialEvents.Count == 0)
            {
                return;
            }

            CurrentEvent = potentialEvents[UnityEngine.Random.Range(0, potentialEvents.Count)];
            OnRandomEventTriggered?.Invoke(CurrentEvent);
        }

        /// <summary>Applies an event's Brain Power and PlayerIQ effects to the active core singletons.</summary>
        public void ApplyEventEffects(BrainRotEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                if (eventData.brainPowerRewardOrPenalty > 0d)
                {
                    currencyManager.AddBrainPower(eventData.brainPowerRewardOrPenalty);
                }
                else if (eventData.brainPowerRewardOrPenalty < 0d)
                {
                    currencyManager.RemoveBrainPower(-eventData.brainPowerRewardOrPenalty);
                }
            }

            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager != null)
            {
                playerIQManager.ModifyPlayerIQ(eventData.playerIQImpact);
            }

            OnEventResolved?.Invoke(eventData);
        }

        private void SubscribeToGameTick()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[RandomEventManager] GameManager.Instance is null; cannot subscribe to tick.", this);
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

        private void HandleSecondTick()
        {
            secondsUntilNextEvent -= 1f;

            if (secondsUntilNextEvent > 0f)
            {
                return;
            }

            TriggerRandomEvent();
            ScheduleNextEvent();
        }

        private void ScheduleNextEvent()
        {
            secondsUntilNextEvent = UnityEngine.Random.Range(MinSecondsBetweenEvents, MaxSecondsBetweenEvents);
        }
    }
}
