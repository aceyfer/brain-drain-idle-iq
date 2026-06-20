using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BrainDrain.Systems
{
    /// <summary>Concrete UnityEvent subclass required for a COGSStage payload to show up in the Inspector.</summary>
    [Serializable]
    public sealed class COGSStageChangedUnityEvent : UnityEvent<COGSStage> { }

    /// <summary>
    /// Tracks which COGSStage the narrator portrait is currently in, based on RebirthCount
    /// progression. Fires OnStageChanged for any interested listener -- currently
    /// DialogueDisplayUI (which subscribes to update its avatar slot), and per spec, future
    /// world-visual/outfit systems can subscribe the same way without this class knowing about
    /// them.
    /// </summary>
    public sealed class COGSPortraitController : MonoBehaviour
    {
        [SerializeField] private List<COGSStage> stages = new();

        /// <summary>Fired whenever the resolved stage actually changes. Passes the new stage.</summary>
        public COGSStageChangedUnityEvent OnStageChanged = new();

        private static COGSPortraitController instance;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static COGSPortraitController Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<COGSPortraitController>();
                if (instance == null)
                {
                    var hostObject = new GameObject("COGSPortraitController (Auto)");
                    instance = hostObject.AddComponent<COGSPortraitController>();
                }

                return instance;
            }
        }

        /// <summary>The currently resolved stage, or null before the first resolution has run.</summary>
        public COGSStage CurrentStage { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[COGSPortraitController] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SortStages();
        }

        private void Start()
        {
            int initialRebirthCount = RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0;
            ApplyStageForRebirthCount(initialRebirthCount);

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

        private void SubscribeToEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
                RebirthManager.Instance.OnRebirthCountChanged += HandleRebirthCountChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RebirthManager.Instance != null)
            {
                RebirthManager.Instance.OnRebirthCountChanged -= HandleRebirthCountChanged;
            }
        }

        private void HandleRebirthCountChanged(int rebirthCount)
        {
            ApplyStageForRebirthCount(rebirthCount);
        }

        private void ApplyStageForRebirthCount(int rebirthCount)
        {
            COGSStage matchedStage = ResolveStage(rebirthCount);
            if (matchedStage == null || matchedStage == CurrentStage)
            {
                return;
            }

            CurrentStage = matchedStage;
            OnStageChanged?.Invoke(CurrentStage);
        }

        private COGSStage ResolveStage(int rebirthCount)
        {
            COGSStage resolved = null;

            for (int i = 0; i < stages.Count; i++)
            {
                COGSStage stage = stages[i];
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

        private void SortStages()
        {
            stages.RemoveAll(stage => stage == null);
            stages.Sort((a, b) => a.minRebirthCount.CompareTo(b.minRebirthCount));
        }
    }
}
