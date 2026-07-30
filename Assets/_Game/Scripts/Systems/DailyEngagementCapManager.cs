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

        /// <summary>Seconds of full-rate counted time left today, floored at 0. Exposed for a future UI/dialogue pass -- not consumed anywhere yet.</summary>
        public float SecondsRemainingAtFullRate => Mathf.Max(0f, FullRateSecondsPerDay - countedSecondsToday);

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
            countedSecondsToday += 1f;
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
    }
}
