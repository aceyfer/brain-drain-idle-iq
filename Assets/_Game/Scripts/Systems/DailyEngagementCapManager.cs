using System;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Tracks a real-calendar-day budget of "counted productive time": the first
    /// FullRateSecondsPerDay seconds the app is open and ticking each local calendar day earn
    /// currency at full rate, and every second after that (until the next local day rollover)
    /// earns at ThrottledMultiplier instead. Applies uniformly to tap-granted Brain Power and
    /// idle BPPS/CPS -- CurrencyManager and PlayerTapHandler both read
    /// ProductionThrottleMultiplier and multiply it into their existing chains
    /// (GetIQProductionMultiplier(), offlineBPPSMultiplier, tapMultiplier, etc.); this class
    /// never reaches into either of them directly. Counted time only advances via
    /// GameManager.OnSecondTick, so it only accrues while the app is actually open and ticking.
    /// </summary>
    public sealed class DailyEngagementCapManager : MonoBehaviour
    {
        private const float FullRateSecondsPerDay = 2700f; // 45 minutes
        private const double ThrottledMultiplier = 0.15d;
        private const string DayKeyFormat = "yyyy-MM-dd";

        private float countedSecondsToday;
        private string currentDayKey;

        [Header("Debug (read-only) -- mirrors of live runtime state for Inspector visibility only.")]
        [Tooltip("Editing these here does nothing; they're overwritten every tick from the private fields above. The real tuning constants are FullRateSecondsPerDay/ThrottledMultiplier at the top of this file.")]
        [SerializeField] private float debugCountedSecondsToday;
        [SerializeField] private string debugCurrentDayKey;
        [SerializeField] private float debugSecondsRemainingAtFullRate;
        [SerializeField] private bool debugIsThrottled;
        [SerializeField] private double debugProductionThrottleMultiplier;

        private static DailyEngagementCapManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping, matching every other Systems singleton in this project.</summary>
        public static DailyEngagementCapManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                if (isShuttingDown)
                {
                    return null;
                }

                instance = FindAnyObjectByType<DailyEngagementCapManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("DailyEngagementCapManager (Auto)");
                    instance = hostObject.AddComponent<DailyEngagementCapManager>();
                }

                return instance;
            }
        }

        /// <summary>
        /// 1.0 while under today's full-rate allowance, ThrottledMultiplier (0.15) once it's
        /// used up. Multiply this into any Brain Power/Cash gain alongside
        /// GetIQProductionMultiplier()/offlineBPPSMultiplier -- never replace them.
        /// </summary>
        public double ProductionThrottleMultiplier => countedSecondsToday < FullRateSecondsPerDay ? 1d : ThrottledMultiplier;

        /// <summary>True once today's full-rate allowance has been used up.</summary>
        public bool IsThrottled => countedSecondsToday >= FullRateSecondsPerDay;

        /// <summary>Seconds of full-rate counted time left today, floored at 0.</summary>
        public float SecondsRemainingAtFullRate => Mathf.Max(0f, FullRateSecondsPerDay - countedSecondsToday);

        /// <summary>Counted productive seconds so far today. Public for runtime readouts (e.g. DebugCheatPanel) -- the serialized debug field above mirrors this for the Inspector.</summary>
        public float CountedSecondsToday => countedSecondsToday;

        /// <summary>The local calendar-day key ("yyyy-MM-dd") counted time is currently accruing against. Exposed so day-rollover behavior is verifiable.</summary>
        public string CurrentDayKey => currentDayKey;

        /// <summary>Fired exactly once per local day, the instant countedSecondsToday crosses FullRateSecondsPerDay. Never fires again until the next day's rollover re-arms it. For dialogue/UI to react to the onset moment specifically, as opposed to polling IsThrottled.</summary>
        public event Action OnThrottleOnset;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(currentDayKey))
            {
                currentDayKey = TodayKey();
            }

            RefreshDebugMirrors();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
                GameManager.Instance.OnSecondTick += HandleSecondTick;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
            }

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void HandleSecondTick()
        {
            RolloverIfNewDay();

            bool wasThrottled = IsThrottled;
            countedSecondsToday += 1f;

            if (!wasThrottled && IsThrottled)
            {
                OnThrottleOnset?.Invoke();
            }

            RefreshDebugMirrors();
        }

        private void RolloverIfNewDay()
        {
            string today = TodayKey();
            if (today != currentDayKey)
            {
                currentDayKey = today;
                countedSecondsToday = 0f;
            }
        }

        private void RefreshDebugMirrors()
        {
            debugCountedSecondsToday = countedSecondsToday;
            debugCurrentDayKey = currentDayKey;
            debugSecondsRemainingAtFullRate = SecondsRemainingAtFullRate;
            debugIsThrottled = IsThrottled;
            debugProductionThrottleMultiplier = ProductionThrottleMultiplier;
        }

        private static string TodayKey() => DateTime.Now.ToString(DayKeyFormat);

        /// <summary>
        /// Restores counted time and day marker from a save. A day key that doesn't match today
        /// -- including one that's missing entirely, e.g. a save predating this feature -- resets
        /// to a fresh full-rate allowance for today rather than carrying over a stale count.
        /// </summary>
        public void LoadState(float restoredCountedSeconds, string restoredDayKey)
        {
            string today = TodayKey();
            if (!string.IsNullOrEmpty(restoredDayKey) && restoredDayKey == today)
            {
                currentDayKey = today;
                countedSecondsToday = Mathf.Max(0f, restoredCountedSeconds);
            }
            else
            {
                currentDayKey = today;
                countedSecondsToday = 0f;
            }

            RefreshDebugMirrors();
        }

        /// <summary>Current counted-seconds/day-key pair for SaveManager to persist.</summary>
        public (float countedSeconds, string dayKey) GetSaveState()
        {
            if (string.IsNullOrEmpty(currentDayKey))
            {
                currentDayKey = TodayKey();
            }

            return (countedSecondsToday, currentDayKey);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only test hook: jumps counted seconds to just under the full-rate window so
        /// throttle onset (including the OnThrottleOnset event) can be verified within seconds
        /// instead of waiting 45 real minutes. Used by DebugCheatPanel's "Burn cap allowance"
        /// button. Compiles out of any build, matching DebugCheats.cs's own guard.
        /// </summary>
        public void DebugBurnFullRateAllowance()
        {
            countedSecondsToday = Mathf.Max(0f, FullRateSecondsPerDay - 1f);
            RefreshDebugMirrors();
        }

        /// <summary>
        /// Editor-only test hook: forces the next tick to detect a new calendar day, exercising
        /// the exact same RolloverIfNewDay path a real midnight crossing would take. Used by
        /// DebugCheatPanel's "Reset cap day" button.
        /// </summary>
        public void DebugForceDayRollover()
        {
            currentDayKey = null;
            RefreshDebugMirrors();
        }
#endif
    }
}
