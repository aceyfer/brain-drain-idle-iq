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
    /// 
    /// DESIGN NOTE: COGS progresses from a corrupted, cynical and hostile Stage 1 to a godlike, 
    /// clear, and supportive Stage 6. Early stages feature cynical/antagonistic comments; 
    /// later stages reflect clarity and a supportive attitude.
    /// </summary>
    public sealed class COGSPortraitController : MonoBehaviour
    {
        [SerializeField] private List<COGSStage> stages = new();

        /// <summary>Fired whenever the resolved stage actually changes. Passes the new stage.</summary>
        public COGSStageChangedUnityEvent OnStageChanged = new();

        private static COGSPortraitController instance;
        private static bool isShuttingDown;

        /// <summary>True only on instances spawned by the Instance getter's auto-bootstrap path.
        /// Lets Awake distinguish a disposable auto-host from a configured scene instance.</summary>
        private bool wasAutoCreated;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.
        /// The Find INCLUDES inactive objects: the scene instance lives on COGS_Narrator_Panel, which
        /// DialogueDisplayUI deactivates during Awake -- a default (active-only) Find during that window
        /// missed it and spawned an unconfigured "(Auto)" impostor that later won the duplicate race and
        /// destroyed the entire panel (§17).</summary>
        public static COGSPortraitController Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<COGSPortraitController>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("COGSPortraitController (Auto)");
                    instance = hostObject.AddComponent<COGSPortraitController>();
                    instance.wasAutoCreated = true;
                }

                return instance;
            }
        }

        /// <summary>The currently resolved stage, or null before the first resolution has run.</summary>
        public COGSStage CurrentStage { get; private set; }

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                // A configured scene instance always beats an unconfigured auto-created host:
                // the auto-host has an empty stages list and can never resolve a stage.
                if (instance.wasAutoCreated && !wasAutoCreated && stages.Count > 0)
                {
                    Debug.LogWarning("[COGSPortraitController] Configured scene instance replacing empty auto-created host.", this);
                    Destroy(instance.gameObject); // dedicated "(Auto)" host object, exists only to carry the component
                }
                else
                {
                    // NEVER Destroy(gameObject) here: this component may share its host with
                    // unrelated systems (the scene instance lives on COGS_Narrator_Panel alongside
                    // DialogueDisplayUI). Destroying the shared host took the entire narrator
                    // panel down every Play session (§17). Destroy only this component.
                    Debug.LogWarning("[COGSPortraitController] Duplicate component destroyed (component only, host GameObject preserved).", this);
                    Destroy(this);
                    return;
                }
            }

            instance = this;

            SortStages();
        }

        private void Start()
        {
            int initialRebirthCount = RebirthManager.Instance != null ? RebirthManager.Instance.RebirthCount : 0;
            ApplyStageForRebirthCount(initialRebirthCount);

            SubscribeToEvents();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                isShuttingDown = true;
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
