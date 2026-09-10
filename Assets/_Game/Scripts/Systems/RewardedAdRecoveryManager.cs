using System;
using UnityEngine;
using Unity.Services.LevelPlay;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// §57 rewarded-ad idle-window recovery: wraps LevelPlay's rewarded-ad flow and tracks how
    /// many ads the player has watched against the CURRENT offline-decay event, resetting to 0
    /// the moment a new one fires. Self-bootstrapping singleton, same convention as
    /// RandomEventManager/WeatherManager. Subscribes to PlayerIQManager.OnOfflineDecayApplied --
    /// the same event DialogueManager already listens to for its "welcome back" narrator line --
    /// so this manager's popup is meant to appear alongside that line, not replace it.
    ///
    /// In-memory only, deliberately not persisted via SaveManager (explicit call, 2026-09-09):
    /// if the app is killed mid ad-flow, the player just loses that specific recovery
    /// opportunity; whatever IQ was already decayed/saved stays exactly as it was either way.
    /// This mirrors PlayerIQManager.RecoverOfflineDecay's own in-memory lastDecay* fields --
    /// there is nothing here that needs to survive a restart, since a fresh absence simply
    /// produces a fresh OnOfflineDecayApplied event with its own fresh ladder.
    ///
    /// PENDING ACCOUNT SETUP: LevelPlay requires an App Key and a rewarded Ad Unit ID from the
    /// LevelPlay dashboard (docs.unity.com/en-us/grow/levelplay) before any ad will actually load
    /// or show. These are account-specific credentials, not something derivable from code --
    /// AppKey/RewardedAdUnitId below are placeholders. Until Aceyfer creates a LevelPlay account,
    /// registers this app, and drops the real values in, LevelPlay.Init will fail (OnInitFailed
    /// logs a warning, once wired into a live scene) and RequestAdWatch will no-op with a
    /// warning rather than throw. Everything else here -- event wiring, the ads-watched ladder,
    /// the PlayerIQManager.RecoverOfflineDecay call -- is complete and does not depend on those
    /// placeholder values being real.
    /// </summary>
    public sealed class RewardedAdRecoveryManager : MonoBehaviour
    {
        // TODO(Aceyfer): replace both with real values from the LevelPlay dashboard once an
        // account and app entry exist. Nothing in this class can function until both are real.
        private const string AppKey = "REPLACE_WITH_LEVELPLAY_APP_KEY";
        private const string RewardedAdUnitId = "REPLACE_WITH_REWARDED_AD_UNIT_ID";

        /// <summary>Matches PlayerIQManager.RecoverOfflineDecay's own hard cap (4 hours / 0.5h per ad) -- ads past this point recover nothing further, so the ladder never needs to count higher.</summary>
        private const int MaxAdsPerEvent = 8;

        private static RewardedAdRecoveryManager instance;
        private static bool isShuttingDown;

        private LevelPlayRewardedAd rewardedAd;
        private bool sdkInitialized;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static RewardedAdRecoveryManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<RewardedAdRecoveryManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("RewardedAdRecoveryManager (Auto)");
                    instance = hostObject.AddComponent<RewardedAdRecoveryManager>();
                }

                return instance;
            }
        }

        /// <summary>How many ads the player has watched against the currently-pending offline-decay event (0..MaxAdsPerEvent). Reset to 0 whenever a new event fires.</summary>
        public int AdsWatchedThisEvent { get; private set; }

        /// <summary>Read-only mirror of MaxAdsPerEvent for UI binding (e.g. "3/8 watched").</summary>
        public int MaxAdsForEvent => MaxAdsPerEvent;

        /// <summary>True once PlayerIQManager.OnOfflineDecayApplied has fired and the player hasn't dismissed the prompt yet -- the signal RewardedAdRecoveryUIController uses to show its popup.</summary>
        public bool HasPendingRecovery { get; private set; }

        /// <summary>The IQ amount lost by the pending event, for UI display ("COGS docked you N IQ").</summary>
        public float PendingAmountLost { get; private set; }

        /// <summary>Fired whenever AdsWatchedThisEvent changes (a successful ad watch) or a pending recovery starts/clears. UI subscribes to refresh its display.</summary>
        public event Action OnRecoveryStateChanged;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            InitializeSdk();
            SubscribeToOfflineDecayEvents();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromOfflineDecayEvents();
            UnsubscribeFromAdEvents();
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void InitializeSdk()
        {
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
            LevelPlay.OnInitFailed += HandleInitFailed;
            LevelPlay.Init(appKey: AppKey);
        }

        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            sdkInitialized = true;
            CreateAndLoadRewardedAd();
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            sdkInitialized = false;
            Debug.LogWarning($"[RewardedAdRecoveryManager] LevelPlay init failed: {error}. Rewarded-ad recovery unavailable until this succeeds (check AppKey is a real value from the LevelPlay dashboard).", this);
        }

        private void CreateAndLoadRewardedAd()
        {
            rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
            rewardedAd.OnAdRewarded += HandleAdRewarded;
            rewardedAd.OnAdLoadFailed += HandleAdLoadFailed;
            rewardedAd.OnAdDisplayFailed += HandleAdDisplayFailed;
            rewardedAd.LoadAd();
        }

        private void UnsubscribeFromAdEvents()
        {
            if (rewardedAd == null) return;
            rewardedAd.OnAdRewarded -= HandleAdRewarded;
            rewardedAd.OnAdLoadFailed -= HandleAdLoadFailed;
            rewardedAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
        }

        private void SubscribeToOfflineDecayEvents()
        {
            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager == null) return;
            playerIQManager.OnOfflineDecayApplied -= HandleOfflineDecayApplied;
            playerIQManager.OnOfflineDecayApplied += HandleOfflineDecayApplied;
        }

        private void UnsubscribeFromOfflineDecayEvents()
        {
            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager == null) return;
            playerIQManager.OnOfflineDecayApplied -= HandleOfflineDecayApplied;
        }

        private void HandleOfflineDecayApplied(float amountLost)
        {
            AdsWatchedThisEvent = 0;
            PendingAmountLost = amountLost;
            HasPendingRecovery = true;
            OnRecoveryStateChanged?.Invoke();
        }

        /// <summary>
        /// Called by RewardedAdRecoveryUIController's WATCH AD button. No-ops (with a warning
        /// log, not a throw -- this is reachable from a live UI button, never a code bug) if
        /// there's no pending recovery, the ladder is already maxed, the SDK hasn't initialized
        /// yet, or no ad happens to be loaded/ready at this moment.
        /// </summary>
        public void RequestAdWatch()
        {
            if (!HasPendingRecovery || AdsWatchedThisEvent >= MaxAdsPerEvent)
            {
                return;
            }

            if (!sdkInitialized || rewardedAd == null || !rewardedAd.IsAdReady())
            {
                Debug.LogWarning("[RewardedAdRecoveryManager] No rewarded ad ready to show yet.", this);
                return;
            }

            rewardedAd.ShowAd();
        }

        private void HandleAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            AdsWatchedThisEvent = Mathf.Min(MaxAdsPerEvent, AdsWatchedThisEvent + 1);
            PlayerIQManager.Instance?.RecoverOfflineDecay(AdsWatchedThisEvent);
            OnRecoveryStateChanged?.Invoke();

            // Load the next one immediately so a second/third watch in the same session doesn't
            // stall on a fresh network round-trip.
            rewardedAd.LoadAd();
        }

        private void HandleAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[RewardedAdRecoveryManager] Rewarded ad failed to load: {error}", this);
        }

        private void HandleAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.LogWarning($"[RewardedAdRecoveryManager] Rewarded ad failed to display: {error}", this);
        }

        /// <summary>Called by RewardedAdRecoveryUIController's close/dismiss button. Player is done for this event whether or not the ladder was maxed.</summary>
        public void DismissPendingRecovery()
        {
            HasPendingRecovery = false;
            OnRecoveryStateChanged?.Invoke();
        }
    }
}
