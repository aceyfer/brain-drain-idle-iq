using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace BrainforgeGames.MobileUXToolkit
{
    public sealed class InternetAvailabilityChecker : MonoBehaviour
    {
        public enum CheckMode
        {
            Continuous,
            OnDemandOnly
        }

        // =========================
        // Inspector Configuration
        // =========================

        [Header("Mode")]
        [SerializeField] private CheckMode mode = CheckMode.Continuous;

        [Header("Continuous Checking")]
        [SerializeField] private float checkIntervalSeconds = 3f;

        [Tooltip("If reachability is NotReachable, wait this long before declaring offline (prevents brief wifi flips).")]
        [SerializeField] private float offlineDebounceSeconds = 1f;

        [Header("Probe (Real Internet Confirmation)")]
        [SerializeField] private string probeUrl = "https://www.google.com/generate_204";

        [SerializeField] private float requestTimeoutSeconds = 3f;

        [Header("Offline Popup Behavior")]
        [Tooltip("If true, this component handles the offline popup internally (devs don't need external subscriptions).")]
        [SerializeField] private bool showPopupWhenOffline = true;

        [Tooltip("If true, popup will keep coming back until internet is restored. User can close it; it returns after cooldown.")]
        [SerializeField] private bool forcePopupUntilOnline = true;

        [Tooltip("Cooldown AFTER user closes the popup. If still offline after this, popup is shown again.")]
        [SerializeField] private float popupCooldownAfterCloseSeconds = 8f;

        [Tooltip("Safety anti-spam: minimum time between popup show calls.")]
        [SerializeField] private float popupMinIntervalSeconds = 0.5f;

        [Header("Popup Elements")]
        [SerializeField] private GameObject onShowOfflinePopup;
        [SerializeField] private Button closePopupBtn;

        // =========================
        // Static API (easy to call)
        // =========================

        /// <summary>Last known internet state (after last completed check).</summary>
        public static bool IsOnline { get; private set; } = true;

        /// <summary>Raised when internet status changes.</summary>
        public static event Action<bool> OnInternetStatusChanged;

        /// <summary>
        /// On-demand: checks internet and returns result via callback.
        /// Does NOT show popup automatically.
        /// </summary>
        public static void CheckNow(Action<bool> callback = null)
        {
            if (!TryGetInstance(out var inst))
            {

                callback?.Invoke(false);
                return;
            }

            inst.StartCoroutine(inst.CheckRoutine(callback, allowPopupBehavior: false));
        }

        /// <summary>
        /// On-demand: checks internet and if offline, may show popup depending on inspector settings.
        /// Useful for internet-required actions in OnDemandOnly games.
        /// </summary>
        public static void CheckNowAndMaybeShowPopup(Action<bool> callback = null)
        {
            if (!TryGetInstance(out var inst))
            {
                callback?.Invoke(false);
                return;
            }

            inst.StartCoroutine(inst.CheckRoutine(callback, allowPopupBehavior: true));
        }

        /// <summary>
        /// MUST be called by popup close button.
        /// Example: Close button -> InternetAvailabilityChecker.NotifyOfflinePopupClosed();
        /// </summary>
        public static void NotifyOfflinePopupClosed()
        {
            if (!TryGetInstance(out var inst))
                return;

            inst.HandlePopupClosed();
        }

        // =========================
        // Internals
        // =========================

        private static InternetAvailabilityChecker _instance;

        private Coroutine _continuousLoop;
        private bool _isChecking;

        private float _offlineSince = -1f;

        private bool _popupVisible;
        private float _lastPopupShowTime = -999f;
        private float _nextPopupAllowedTime = -999f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (mode == CheckMode.Continuous)
                StartContinuous();
        }

        private void OnEnable()
        {
            if (_instance == this && mode == CheckMode.Continuous)
                StartContinuous();
        }

        private void OnDisable()
        {
            if (_instance == this)
                StopContinuous();
        }

        private void StartContinuous()
        {
            if (_continuousLoop != null) return;
            _continuousLoop = StartCoroutine(ContinuousLoop());
        }

        private void StopContinuous()
        {
            if (_continuousLoop == null) return;
            StopCoroutine(_continuousLoop);
            _continuousLoop = null;
        }

        private IEnumerator ContinuousLoop()
        {
            while (true)
            {
                yield return CheckRoutine(callback: null, allowPopupBehavior: true);
                yield return new WaitForSecondsRealtime(checkIntervalSeconds);
            }
        }

        private IEnumerator CheckRoutine(Action<bool> callback, bool allowPopupBehavior)
        {
            if (_isChecking) yield break;
            _isChecking = true;

            // 1) Fast hint: reachability
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (_offlineSince < 0f) _offlineSince = Time.unscaledTime;

                if ((Time.unscaledTime - _offlineSince) >= offlineDebounceSeconds)
                {
                    SetState(false);

                    if (allowPopupBehavior)
                        HandleOfflinePopupPolicy();
                }

                callback?.Invoke(false);
                _isChecking = false;
                yield break;
            }

            // reachability says we have *some* path; reset debounce timer
            _offlineSince = -1f;

            // 2) Real probe (confirms actual internet access)
            bool ok = false;
            using (var req = UnityWebRequest.Get(probeUrl))
            {
                req.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                ok = (req.result == UnityWebRequest.Result.Success);
#else
            ok = !(req.isNetworkError || req.isHttpError);
#endif
            }

            SetState(ok);

            if (!ok && allowPopupBehavior)
                HandleOfflinePopupPolicy();

            callback?.Invoke(ok);
            _isChecking = false;
        }

        private void SetState(bool online)
        {
            if (IsOnline == online) return;

            IsOnline = online;
            OnInternetStatusChanged?.Invoke(IsOnline);

            if (IsOnline)
            {
                // Internet restored: reset forcing + timers so future offline behaves fresh.
                _popupVisible = false;
                _nextPopupAllowedTime = -999f;
                _offlineSince = -1f;
            }
        }

        private void HandleOfflinePopupPolicy()
        {
            if (!showPopupWhenOffline) return;

            // If popup currently open, do nothing.
            if (_popupVisible) return;

            // If we don't force until online, show only once per offline period.
            if (!forcePopupUntilOnline)
            {
                // Use +infinity to mean "already shown; don't show again until online"
                if (_nextPopupAllowedTime == float.PositiveInfinity)
                    return;

                TryShowOfflinePopup();
                _nextPopupAllowedTime = float.PositiveInfinity;
                return;
            }

            // Force mode: show whenever allowed time passes and still offline.
            if (Time.unscaledTime < _nextPopupAllowedTime)
                return;

            TryShowOfflinePopup();
        }

        private void TryShowOfflinePopup()
        {
            // Anti-spam safety
            if (Time.unscaledTime - _lastPopupShowTime < popupMinIntervalSeconds)
                return;

            _lastPopupShowTime = Time.unscaledTime;
            _popupVisible = true;

            if (onShowOfflinePopup != null)
            {
                onShowOfflinePopup.SetActive(true);
            }
            else
                Debug.LogWarning("[InternetAvailabilityChecker] Offline popup requested, but onShowOfflinePopup is not assigned.");
        }

        public void HandlePopupClosed()
        {
            _popupVisible = false;
            onShowOfflinePopup.SetActive(false);
            if (IsOnline) return;

            if (forcePopupUntilOnline)
            {
                _nextPopupAllowedTime = Time.unscaledTime + Mathf.Max(0f, popupCooldownAfterCloseSeconds);
            }
        }

        private static bool TryGetInstance(out InternetAvailabilityChecker inst)
        {
            inst = _instance;
            if (inst != null) return true;

            Debug.LogWarning("[InternetAvailabilityChecker] No instance found. Add this component once in a bootstrap/persistent scene object.");
            return false;
        }
    }
}