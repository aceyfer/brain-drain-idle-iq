using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Plankton.Proxy
{
    [Preserve]
    public class PlanktonMono : MonoBehaviour
    {
        private const string logName = "[Plankton] [Callback]";

#pragma warning disable IDE0051 // unused private members

        private void Update()
        {
            lock (unityThreadActions)
            {
                for (int i = 0; i < unityThreadActions.Count; i++)
                {
                    try
                    {
                        unityThreadActions[i]?.Invoke();
                    }
                    catch { }
                }
                unityThreadActions.Clear();
            }
        }


        [Preserve]
        private void OnAdInitialized()
        {
            Debug.Log($"{logName} OnAdInitialized");
            CallInUnityThread(() =>
            {
                onAdInitialized?.Invoke();
                onAdInitialized = null;
            });
        }

        [Preserve]
        private void OnAdClosed(string json)
        {
            Debug.Log($"{logName} OnAdClosed : {json}");
            CallInUnityThread(() =>
            {
                onAdClosed?.Invoke(json);
                onAdClosed = null;
            });
        }

        [Preserve]
        private void OnRemoteConfigFetched(string json)
        {
            Debug.Log($"{logName} OnRemoteConfigFetched : {json}");
            CallInUnityThread(() =>
            {
                onRemoteConfigFetched?.Invoke(json);
                onRemoteConfigFetched = null;
            });
        }

        [Preserve]
        private void OnRemoteConfigFetchFailed(string empty)
        {
            Debug.Log($"{logName} OnRemoteConfigFetchFailed : {empty}");
            CallInUnityThread(() =>
            {
                onRemoteConfigFailed?.Invoke();
                onRemoteConfigFailed = null;
            });
        }

        [Preserve]
        private void OnSignInResult(string boolean)
        {
            Debug.Log($"{logName} OnSignInResult : {boolean}");
            CallInUnityThread(() =>
            {
                onGameServiceSignInResult?.Invoke(boolean.ToLower() == "true");
            });
        }

        [Preserve]
        private void OnPlayerInfoResult(string json)
        {
            Debug.Log($"{logName} OnPlayerInfoResult : {json}");
            CallInUnityThread(() =>
            {
                onGameServicePlayerInfoResult?.Invoke(json);
                onGameServicePlayerInfoResult = null;
            });
        }

        [Preserve]
        private void OnServerSideParamsResult(string json)
        {
            Debug.Log($"{logName} OnServerSideParamsResult : {json}");
            CallInUnityThread(() =>
            {
                onGameServiceServerSideResult?.Invoke(json);
                onGameServiceServerSideResult = null;
            });
        }

        [Preserve]
        private void OnSaveResult(string boolean)
        {
            Debug.Log($"{logName} OnSaveResult : {boolean}");
            CallInUnityThread(() =>
            {
                onGameServiceSaveResult?.Invoke(boolean.ToLower() == "true");
                onGameServiceSaveResult = null;
            });
        }

        [Preserve]
        private void OnLoadResult(string data)
        {
            Debug.Log($"{logName} OnLoadResult : {data}");
            CallInUnityThread(() =>
            {
                onGameServiceLoadData?.Invoke(data);
                onGameServiceLoadData = null;
            });
        }

        [Preserve]
        private void OnRateAppResult(string boolean)
        {
            Debug.Log($"{logName} OnRateAppResult : {boolean}");
            CallInUnityThread(() =>
            {
                onRateAppResult?.Invoke(boolean.ToLower() == "true");
                onRateAppResult = null;
            });
        }

        [Preserve]
        private void OnPurchaseResult(string json)
        {
            Debug.Log($"{logName} OnPurchaseResult : {json}");
            CallInUnityThread(() =>
            {
                onPurchaseResult?.Invoke(json);
                // onPurchaseResult = null;
            });
        }

        [Preserve]
        private void OnSkuDetailsResult(string json)
        {
            Debug.Log($"{logName} OnSkuDetailsResult : {json}");
            CallInUnityThread(() =>
            {
                onSkuDetailsResult?.Invoke(json);
                onSkuDetailsResult = null;
            });
        }

        [Preserve]
        private void OnPurchasesResult(string json)
        {
            Debug.Log($"{logName} OnPurchaseHistoryResult : {json}");
            CallInUnityThread(() =>
            {
                onPurchaseHistoryResult?.Invoke(json);
                onPurchaseHistoryResult = null;
            });
        }

        [Preserve]
        private void OnLogReceived(string log)
        {
            if (string.IsNullOrEmpty(log)) return;

            //if (log[0] == 'E')
            //    SeganX.Console.AddLog(log, Color.red * Color.gray);
            //else if (log[0] == 'W')
            //    SeganX.Console.AddLog(log, Color.yellow * Color.gray);
            //else
            //    SeganX.Console.AddLog(log, Color.gray);
        }

        [Preserve]
        private void OnGameAnalyticsInitialized()
        {
            Debug.Log($"{logName} OnGameAnalyticsInitialized");
            CallInUnityThread(() =>
            {
                onGameAnalyticsInitialized?.Invoke();
                onGameAnalyticsInitialized = null;
            });
        }

        [Preserve]
        private void OnBillingInitializationResult(string json)
        {
            Debug.Log($"{logName} OnBillingInitializationResult : {json}");
            CallInUnityThread(() =>
            {
                onBillingInitializationResult?.Invoke(json);
                onBillingInitializationResult = null;
            });
        }

        [Preserve]
        private void OnConsumeOrAcknowledgeResult(string json)
        {

            Debug.Log($"{logName} OnConsumeOrAcknowledgeResult : {json}");
            CallInUnityThread(() =>
            {
                onConsumeOrAcknowledgeResult?.Invoke(json);
                onConsumeOrAcknowledgeResult = null;
            });
        }

        [Preserve]
        private void OnRequestConsentInfoUpdateResult(string json)
        {

            Debug.Log($"{logName} OnRequestConsentInfoUpdateResult : {json}");
            CallInUnityThread(() =>
            {
                onRequestConsentInfoUpdateResult?.Invoke(json);
                onRequestConsentInfoUpdateResult = null;
            });
        }

        [Preserve]
        private void OnConsentFormDismissed(string json)
        {

            Debug.Log($"{logName} OnConsentFormDismissed : {json}");
            CallInUnityThread(() =>
            {
                onConsentFormDismissed?.Invoke(json);
                onConsentFormDismissed = null;
            });
        }

        [Preserve]
        private void OnRestorePurchasesSuccess(string json)
        {
            Debug.Log($"{logName} OnRestorePurchasesSuccess : {json}");
            CallInUnityThread(() => onRestorePurchasesSuccess?.Invoke(json));
        }

        [Preserve]
        private void OnFcmTokenReceived(string json)
        {
            Debug.Log($"{logName} OnFcmTokenReceived : {json}");
            CallInUnityThread(() => onFcmTokenReceived?.Invoke(json));
        }

        [Preserve]
        private void OnFcmMessageReceived(string json)
        {
            Debug.Log($"{logName} OnFcmMessageReceived : {json}");
            CallInUnityThread(() => onFcmMessageReceived?.Invoke(json));
        }

        [Preserve]
        private void OnATTAuthorizationResult(string json)
        {
            Debug.Log($"{logName} OnATTAuthorizationResult : {json}");
            CallInUnityThread(() => onATTAuthorizationResult?.Invoke(json));
        }

        [Preserve]
        private void OnAdAction(string json)
        {
            Debug.Log($"{logName} OnAdAction : {json}");
            CallInUnityThread(() => OnAdActionEvent?.Invoke(json));
        }

        [Preserve]
        private void OnAdMobImpressionRevenue(string json)
        {
            Debug.Log($"{logName} OnAdMobImpressionRevenue : {json}");
            CallInUnityThread(() => OnAdMobImpressionRevenueEvent?.Invoke(json));
        }

        [Preserve]
        private void OnAppLovinMaxImpressionRevenue(string json)
        {
            Debug.Log($"{logName} OnAppLovinMaxImpressionRevenue : {json}");
            CallInUnityThread(() => OnAppLovinMaxImpressionRevenueEvent?.Invoke(json));
        }

        [Preserve]
        private void OnLevelPlayImpressionRevenue(string json)
        {
            Debug.Log($"{logName} OnLevelPlayImpressionRevenue : {json}");
            CallInUnityThread(() => OnLevelPlayImpressionRevenueEvent?.Invoke(json));
        }

        [Preserve]
        private void OnGUI()
        {
            if (fullscreenBox)
            {
                var rect = new Rect(0, 0, Screen.width, Screen.height);
                GUI.depth = -1;
                GUI.DrawTexture(rect, bannerTexture);
            }

#if UNITY_EDITOR
            if (editorFullscreenAdCaption != null && editorFullscreenAdTimer > 0)
            {
                editorFullscreenAdTimer -= Time.unscaledDeltaTime;
                var rect = new Rect(0, 0, Screen.width, Screen.height);
                GUI.depth = -1;
                GUI.Box(rect, editorFullscreenAdCaption);
                if (editorFullscreenAdTimer <= 0)
                {
                    editorFullscreenAdCaption = null;
                    onAdClosed?.Invoke("{\"displayed\":\"true\",\"reward_earned\":\"true\"}");
                }
            }
            else
#endif
                switch (bannerBox)
                {
                    case 1:
                        {
                            int heigh = Mathf.RoundToInt(Screen.height * 0.075f);
                            var rect = new Rect(0, Screen.height - heigh, Screen.width, heigh);
                            GUI.depth = -1;
                            GUI.DrawTexture(rect, bannerTexture);
                        }
                        break;
                    case 2:
                        {
#if UNITY_EDITOR
                            int heigh = Mathf.RoundToInt(Screen.height * 0.075f);
                            var rect = new Rect(0, 0, Screen.width, heigh);
                            GUI.depth = -1;
                            GUI.DrawTexture(rect, bannerTexture);
#endif
                        }
                        break;
                }
        }

#pragma warning restore IDE0051 // unused private members


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// STATIC MEMBERS
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static readonly List<Action> unityThreadActions = new List<Action>(16);
        private static Texture2D bannerTexture;

        public static bool fullscreenBox = false;
        public static int bannerBox = 0;
#if UNITY_EDITOR
        public static float editorFullscreenAdTimer = 0;
        public static string editorFullscreenAdCaption = null;
#endif

        public static Action onAdInitialized = null;
        public static Action<string> onAdClosed = null;
    
        public static Action<string> onRemoteConfigFetched = null;
        public static Action onRemoteConfigFailed = null;
        public static Action<bool> onGameServiceSignInResult = null;
        public static Action<string> onGameServicePlayerInfoResult = null;
        public static Action<string> onGameServiceServerSideResult = null;
        public static Action<bool> onGameServiceSaveResult = null;
        public static Action<string> onGameServiceLoadData = null;
        public static Action<bool> onRateAppResult = null;
        public static Action<string> onPurchaseResult = null;
        public static Action<string> onSkuDetailsResult = null;
        public static Action<string> onPurchaseHistoryResult = null;
        public static Action onGameAnalyticsInitialized = null;
        public static Action<string> onBillingInitializationResult = null;
        public static Action<string> onConsumeOrAcknowledgeResult = null;
        public static Action<string> onRequestConsentInfoUpdateResult = null;
        public static Action<string> onConsentFormDismissed = null;
        public static Action<string> onRestorePurchasesSuccess = null;
        public static Action<string> onFcmTokenReceived = null;
        public static Action<string> onFcmMessageReceived = null;
        public static Action<string> onATTAuthorizationResult = null;

        public static event Action<string> OnAdActionEvent = null;
        public static event Action<string> OnAdMobImpressionRevenueEvent = null;
        public static event Action<string> OnAppLovinMaxImpressionRevenueEvent = null;
        public static event Action<string> OnLevelPlayImpressionRevenueEvent = null;

        public static void CallInUnityThread(Action action)
        {
            lock (unityThreadActions)
            {
                unityThreadActions.Add(action);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInitialize()
        {
            var mono = new GameObject("Plankton", typeof(PlanktonMono));
            DontDestroyOnLoad(mono);

            bannerTexture = new Texture2D(1, 1);
            bannerTexture.SetPixel(0, 0, Color.black);
            bannerTexture.Apply();

#if UNITY_EDITOR
#else
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
#endif
        }
    }
}
