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
        private int currentRankIndex;


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

        /// <summary>Index into <see cref="RankDefinitions"/> for the currently active rank.</summary>
        public int CurrentRankIndex => currentRankIndex;

        /// <summary>Determines rank name based on cumulative Brains earned.</summary>
        public string GetRankName(double cumulativeBrains)
        {
            if (rankDefinitions == null || rankDefinitions.Length == 0)
            {
                return "Unknown";
            }

            return rankDefinitions[CalculateRankIndex(cumulativeBrains)].rankName;
        }

        /// <summary>Fired once after the tick loop is running.</summary>
        public event Action OnGameInitialized;

        /// <summary>Fired every second by the background tick loop.</summary>
        public event Action OnSecondTick;

        /// <summary>Fired when the active Idiocracy Rank index changes. Passes the new rank index.</summary>
        public event Action<int> OnRankChanged;

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
            SubscribeToCurrencyForRank();
            UpdateRankFromCumulativeBrains(currencyManager != null ? currencyManager.CumulativeBrains : 0d);
            OnGameInitialized?.Invoke();
        }

        private void OnDestroy()
        {
            StopTickLoop();
            UnsubscribeFromCurrencyForRank();

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

        private void SubscribeToCurrencyForRank()
        {
            if (currencyManager == null)
            {
                return;
            }

            currencyManager.OnCumulativeBrainsChanged -= HandleCumulativeBrainsChangedForRank;
            currencyManager.OnCumulativeBrainsChanged += HandleCumulativeBrainsChangedForRank;
        }

        private void UnsubscribeFromCurrencyForRank()
        {
            if (currencyManager == null)
            {
                return;
            }

            currencyManager.OnCumulativeBrainsChanged -= HandleCumulativeBrainsChangedForRank;
        }

        private void HandleCumulativeBrainsChangedForRank(double cumulativeBrains)
        {
            UpdateRankFromCumulativeBrains(cumulativeBrains);
        }

        private void UpdateRankFromCumulativeBrains(double cumulativeBrains)
        {
            int newRankIndex = CalculateRankIndex(cumulativeBrains);
            if (newRankIndex == currentRankIndex)
            {
                return;
            }

            currentRankIndex = newRankIndex;
            OnRankChanged?.Invoke(currentRankIndex);
        }

        private int CalculateRankIndex(double cumulativeBrains)
        {
            if (rankDefinitions == null || rankDefinitions.Length == 0)
            {
                return 0;
            }

            int rankIndex = 0;
            for (int i = 0; i < rankDefinitions.Length; i++)
            {
                if (cumulativeBrains >= rankDefinitions[i].threshold)
                {
                    rankIndex = i;
                }
                else
                {
                    break;
                }
            }

            return rankIndex;
        }
    }
}
