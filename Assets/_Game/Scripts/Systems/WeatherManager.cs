using UnityEngine;
using BrainDrain.Core;
using BrainDrain.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Owns the random rain-burst timing loop: wait a random interval, run a rain burst for a
    /// random duration, stop, repeat. Purely ambient/cosmetic -- no economy or gameplay effect.
    /// Mirrors RandomEventManager's tick-driven countdown pattern exactly (never polls in
    /// Update). Interval/duration ranges are Claude Code's judgment call for §61, not yet
    /// Aceyfer-approved in detail -- flag before shipping if the pacing feels off.
    /// </summary>
    public sealed class WeatherManager : MonoBehaviour
    {
        private const float MinSecondsBetweenRain = 90f;
        private const float MaxSecondsBetweenRain = 240f;
        private const float MinRainDurationSeconds = 20f;
        private const float MaxRainDurationSeconds = 60f;

        [SerializeField] private RainEffectView rainView;

        private float secondsUntilNextChange;
        private bool isRaining;

        private static WeatherManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static WeatherManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<WeatherManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("WeatherManager (Auto)");
                    instance = hostObject.AddComponent<WeatherManager>();
                }

                return instance;
            }
        }

        /// <summary>
        /// Enable/disable rain bursts. Smog stays regardless -- purely visual atmosphere tied
        /// directly to World Restoration stage, not gated by this. Exposed as a hook for a
        /// future Settings toggle (not built this pass -- see §61 notes); defaults on.
        /// </summary>
        public bool RainEnabled { get; set; } = true;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[WeatherManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            ScheduleNextChange();
            SubscribeToGameTick();
            SubscribeToRestorationEvents();

            if (rainView != null)
            {
                rainView.SetStageIndex(ResolveCurrentStageIndex());
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameTick();
            UnsubscribeFromRestorationEvents();

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void SubscribeToGameTick()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[WeatherManager] GameManager.Instance is null; cannot subscribe to tick.", this);
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

        private void SubscribeToRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
            manager.OnRestorationStageChanged += HandleRestorationStageChanged;
        }

        private void UnsubscribeFromRestorationEvents()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.OnRestorationStageChanged -= HandleRestorationStageChanged;
        }

        private void HandleRestorationStageChanged(WorldRestorationStage stage)
        {
            rainView?.SetStageIndex(stage != null ? stage.stageIndex : 0);
        }

        private static int ResolveCurrentStageIndex()
        {
            WorldRestorationManager manager = WorldRestorationManager.Instance;
            return manager?.CurrentStageIndex ?? 0;
        }

        private void HandleSecondTick()
        {
            secondsUntilNextChange -= 1f;
            if (secondsUntilNextChange > 0f)
            {
                return;
            }

            if (isRaining)
            {
                StopRain();
            }
            else if (RainEnabled)
            {
                StartRain();
            }
            else
            {
                // Weather disabled -- keep re-checking on the same cadence rather than freezing
                // the loop, so rain resumes promptly once RainEnabled flips back on.
                ScheduleNextChange();
            }
        }

        private void StartRain()
        {
            isRaining = true;
            rainView?.StartRain();
            secondsUntilNextChange = Random.Range(MinRainDurationSeconds, MaxRainDurationSeconds);
        }

        private void StopRain()
        {
            isRaining = false;
            rainView?.StopRain();
            ScheduleNextChange();
        }

        private void ScheduleNextChange()
        {
            secondsUntilNextChange = Random.Range(MinSecondsBetweenRain, MaxSecondsBetweenRain);
        }
    }
}
