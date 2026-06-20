using System;
using UnityEngine;
using UnityEngine.Serialization;
using BrainDrain.Systems;

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
        private const int AutoSaveIntervalSeconds = 60;

        private static readonly object InstanceLock = new();
        private static GameManager instance;

        private int secondsSinceLastAutoSave;

        [Header("Core Systems")]
        [FormerlySerializedAs("iqDecaySystem")]
        [FormerlySerializedAs("worldProgressionManager")]
        [SerializeField] private PlayerIQManager playerIQManager;
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

        /// <summary>Direct reference to the player IQ simulation.</summary>
        public PlayerIQManager PlayerIQSystem => playerIQManager;

        /// <summary>Direct reference to the currency system.</summary>
        public CurrencyManager Currency => currencyManager;

        /// <summary>Idiocracy Rank Definitions array.</summary>
        public RankDefinition[] RankDefinitions => rankDefinitions;

        /// <summary>Index into <see cref="RankDefinitions"/> for the currently active rank.</summary>
        public int CurrentRankIndex => currentRankIndex;

        /// <summary>Determines rank name based on cumulative Brain Power earned.</summary>
        public string GetRankName(double cumulativeBrainPower)
        {
            if (rankDefinitions == null || rankDefinitions.Length == 0)
            {
                return "Unknown";
            }

            return rankDefinitions[CalculateRankIndex(cumulativeBrainPower)].rankName;
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

            // SaveManager's self-bootstrapping Instance property only creates it on first
            // access -- but nothing else ever calls *into* SaveManager (it only calls out to
            // other managers), so something has to proactively touch it once. This is the
            // earliest, most central place to guarantee that happens.
            _ = SaveManager.Instance;

            ResolveCoreReferences();
        }

        private void Start()
        {
            StartTickLoop();
            SubscribeToCurrencyForRank();
            UpdateRankFromCumulativeBrainPower(currencyManager != null ? currencyManager.CumulativeBrainPower : 0d);
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

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                RequestSave();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                RequestSave();
            }
        }

        private void OnApplicationQuit()
        {
            RequestSave();
        }

        private void ResolveCoreReferences()
        {
            if (playerIQManager == null)
            {
                playerIQManager = GetComponentInChildren<PlayerIQManager>(true);

                if (playerIQManager == null)
                {
                    playerIQManager = FindAnyObjectByType<PlayerIQManager>();
                }

                if (playerIQManager == null)
                {
                    Debug.LogError("[GameManager] PlayerIQManager reference is missing.", this);
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
            UpdateAutoSaveTimer();
        }

        private void UpdateAutoSaveTimer()
        {
            secondsSinceLastAutoSave++;
            if (secondsSinceLastAutoSave < AutoSaveIntervalSeconds)
            {
                return;
            }

            secondsSinceLastAutoSave = 0;
            RequestSave();
        }

        private void SubscribeToCurrencyForRank()
        {
            if (currencyManager == null)
            {
                return;
            }

            currencyManager.OnCumulativeBrainPowerChanged -= HandleCumulativeBrainPowerChangedForRank;
            currencyManager.OnCumulativeBrainPowerChanged += HandleCumulativeBrainPowerChangedForRank;
        }

        private void UnsubscribeFromCurrencyForRank()
        {
            if (currencyManager == null)
            {
                return;
            }

            currencyManager.OnCumulativeBrainPowerChanged -= HandleCumulativeBrainPowerChangedForRank;
        }

        private void HandleCumulativeBrainPowerChangedForRank(double cumulativeBrainPower)
        {
            UpdateRankFromCumulativeBrainPower(cumulativeBrainPower);
        }

        private void UpdateRankFromCumulativeBrainPower(double cumulativeBrainPower)
        {
            int newRankIndex = CalculateRankIndex(cumulativeBrainPower);
            if (newRankIndex == currentRankIndex)
            {
                return;
            }

            currentRankIndex = newRankIndex;
            OnRankChanged?.Invoke(currentRankIndex);
        }

        private int CalculateRankIndex(double cumulativeBrainPower)
        {
            if (rankDefinitions == null || rankDefinitions.Length == 0)
            {
                return 0;
            }

            int rankIndex = 0;
            for (int i = 0; i < rankDefinitions.Length; i++)
            {
                if (cumulativeBrainPower >= rankDefinitions[i].threshold)
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
