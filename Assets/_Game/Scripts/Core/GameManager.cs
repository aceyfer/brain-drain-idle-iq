using System;
using UnityEngine;

namespace BrainDrain.Core
{
    [System.Serializable]
    public struct RankDefinition
    {
        public string rankName;
        public int threshold;
    }

    /// <summary>
    /// Central orchestrator for Brain Drain: Idle IQ.
    /// Owns the global 1-second simulation tick and wires core systems together.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        private const float TickIntervalSeconds = 1f;

        private static readonly object InstanceLock = new();
        private static GameManager instance;

        [Header("Core Systems")]
        [SerializeField] private IQDecaySystem iqDecaySystem;
        [SerializeField] private CurrencyManager currencyManager;

        [Header("Idiocracy Ranks")]
        [SerializeField] private RankDefinition[] rankDefinitions;

        private bool tickLoopActive;


        /// <summary>Thread-safe singleton accessor. Returns null if no instance exists in the scene.</summary>
        public static GameManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (InstanceLock)
                {
                    if (instance != null)
                    {
                        return instance;
                    }

                    instance = FindAnyObjectByType<GameManager>();
                }

                return instance;
            }
        }

        /// <summary>Direct reference to the IQ decay simulation.</summary>
        public IQDecaySystem IQDecay => iqDecaySystem;

        /// <summary>Direct reference to the currency system.</summary>
        public CurrencyManager Currency => currencyManager;

        /// <summary>Idiocracy Rank Definitions array.</summary>
        public RankDefinition[] RankDefinitions => rankDefinitions;

        /// <summary>Determines rank name based on cumulative Brains earned.</summary>
        public string GetRankName(double cumulativeBrains)
        {
            if (rankDefinitions == null || rankDefinitions.Length == 0)
            {
                return "Unknown";
            }

            string currentRank = rankDefinitions[0].rankName;
            for (int i = 0; i < rankDefinitions.Length; i++)
            {
                if (cumulativeBrains >= rankDefinitions[i].threshold)
                {
                    currentRank = rankDefinitions[i].rankName;
                }
                else
                {
                    break;
                }
            }
            return currentRank;
        }

        /// <summary>Fired once after the tick loop is running.</summary>
        public event Action OnGameInitialized;

        /// <summary>Fired every second by the background tick loop.</summary>
        public event Action OnSecondTick;

        /// <summary>Hook for a future SaveSystem to persist state on demand.</summary>
        public event Action OnSaveRequested;

        private void Awake()
        {
            lock (InstanceLock)
            {
                if (instance != null && instance != this)
                {
                    Debug.LogWarning("[GameManager] Duplicate instance destroyed.", this);
                    Destroy(gameObject);
                    return;
                }

                instance = this;
            }

            DontDestroyOnLoad(gameObject);
            ResolveCoreReferences();
        }

        private void Start()
        {
            StartTickLoop();
            OnGameInitialized?.Invoke();
        }

        private void OnDestroy()
        {
            StopTickLoop();

            lock (InstanceLock)
            {
                if (instance == this)
                {
                    instance = null;
                }
            }
        }

        /// <summary>Requests a save through the future SaveSystem hook.</summary>
        public void RequestSave()
        {
            OnSaveRequested?.Invoke();
        }

        private void ResolveCoreReferences()
        {
            if (iqDecaySystem == null)
            {
                iqDecaySystem = GetComponentInChildren<IQDecaySystem>(true);

                if (iqDecaySystem == null)
                {
                    iqDecaySystem = FindAnyObjectByType<IQDecaySystem>();
                }

                if (iqDecaySystem == null)
                {
                    Debug.LogError("[GameManager] IQDecaySystem reference is missing.", this);
                }
            }

            if (currencyManager == null)
            {
                currencyManager = GetComponentInChildren<CurrencyManager>(true);

                if (currencyManager == null)
                {
                    currencyManager = FindAnyObjectByType<CurrencyManager>();
                }

                if (currencyManager == null)
                {
                    Debug.LogError("[GameManager] CurrencyManager reference is missing.", this);
                }
            }
        }

        private void StartTickLoop()
        {
            if (tickLoopActive)
            {
                return;
            }

            InvokeRepeating(nameof(ProcessSecondTick), TickIntervalSeconds, TickIntervalSeconds);
            tickLoopActive = true;
        }

        private void StopTickLoop()
        {
            if (!tickLoopActive)
            {
                return;
            }

            CancelInvoke(nameof(ProcessSecondTick));
            tickLoopActive = false;
        }

        private void ProcessSecondTick()
        {
            OnSecondTick?.Invoke();
        }
    }
}
