using System.Collections.Generic;
using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public class GameAnalytics
    {
        private const string logName = "[GameAnalytics]";

        private static Builder config = null;
        private static ProxyGameAnalytics proxyGameAnalytics = null;
        private static bool initialized = false;
        private static bool subscribedAdMobImpressions = false;
        private static bool subscribedMaxImpressions = false;
        private static bool subscribedIronSourceImpressions = false;
        private static bool subscribedAdEventTracking = false;


        private static bool IsNotInitialized
        {
            get
            {
                if (initialized == false || proxyGameAnalytics == null)
                    Debug.Log($"{logName} Feature needs to be initialized first!");
                return initialized == false && proxyGameAnalytics == null;
            }
        }

        public static void Initialize(Builder builder, System.Action callback)
        {
            if (proxyGameAnalytics != null)
            {
                Debug.Log($"{logName} Feature already initialized!");
                callback?.Invoke();
                return;
            }
            Debug.Log($"{logName} Initializing...");

            PlanktonMono.onGameAnalyticsInitialized = () =>
            {
                Debug.Log($"{logName} Initialized!");
                initialized = true;
                callback?.Invoke();
            };

            config = builder;
            if (string.IsNullOrEmpty(builder.buildVersion))
                builder.buildVersion = Application.version;

            try
            {

                var json = JsonUtility.ToJson(builder);
                proxyGameAnalytics = new ProxyGameAnalytics(json);

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(PlanktonMono.onGameAnalyticsInitialized);
#endif

                Application.logMessageReceivedThreaded += HandleReceivedLog;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void HandleReceivedLog(string condition, string stackTrace, LogType type)
        {
            var severity = ErrorSeverity.Info;
            switch (type)
            {
                case LogType.Log:
                    if (config.submitUnityDebug == false) return;
                    severity = ErrorSeverity.Debug;
                    break;
                case LogType.Assert:
                    if (config.submitUnityInfo == false) return;
                    severity = ErrorSeverity.Info;
                    break;
                case LogType.Warning:
                    if (config.submitUnityWarning == false) return;
                    severity = ErrorSeverity.Warning;
                    break;
                case LogType.Error:
                    if (config.submitUnityError == false) return;
                    severity = ErrorSeverity.Error;
                    break;
                case LogType.Exception:
                    if (config.submitUnityCritical == false) return;
                    severity = ErrorSeverity.Critical;
                    break;

            }

            if (string.IsNullOrEmpty(stackTrace))
                stackTrace = string.Empty;

            condition = condition.Replace('"', '\'').Replace('\n', ' ').Replace('\r', ' ');
            stackTrace = stackTrace.Replace('"', '\'').Replace('\n', ' ').Replace('\r', ' ');
            var message = condition + " " + stackTrace;
            if (message.Length > 8192)
                message = message.Substring(0, 8191);

            try
            {
                AddErrorEvent(severity, message);
            }
            catch { }
        }

        public static void SetCustomDimension01(string value)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} SetCustomDimension01 value:{value}");
                proxyGameAnalytics.SetCustomDimension01(value);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void SetCustomDimension02(string value)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} SetCustomDimension02 value:{value}");
                proxyGameAnalytics.SetCustomDimension02(value);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void SetCustomDimension03(string value)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} SetCustomDimension03 value:{value}");
                proxyGameAnalytics.SetCustomDimension03(value);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddBusinessEvent(string currency, int amount, string itemType, string itemId, string cartType, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddBusinessEvent currency:{currency} amount:{amount} itemType:{itemType} itemId:{itemId} cartType:{cartType} fields:{json}");
                proxyGameAnalytics.AddBusinessEvent(currency, amount, itemType, itemId, cartType, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddBusinessEventWithReceipt(string currency, int amount, string itemType, string itemId, string cartType, string receipt, string store, string signature, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddBusinessEvent currency:{currency} amount:{amount} itemType:{itemType} itemId:{itemId} cartType:{cartType} receipt:{receipt} store:{store} signature:{signature} fields:{json}");
                proxyGameAnalytics.AddBusinessEventWithReceipt(currency, amount, itemType, itemId, cartType, receipt, store, signature, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddResourceEvent(ResourceFlowType flowType, string currency, float amount, string itemType, string itemId, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddResourceEvent flowType:{flowType} currency:{currency} amount:{amount} itemType:{itemType} itemId:{itemId} fields:{json}");
                proxyGameAnalytics.AddResourceEvent((int)flowType, currency, amount, itemType, itemId, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddProgressionEvent(ProgressionStatus progressionStatus, string progression01, string progression02, string progression03, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddProgressionEvent progressionStatus:{progressionStatus} progression01:{progression01} progression02:{progression02} progression03:{progression03} fields:{json}");
                proxyGameAnalytics.AddProgressionEvent((int)progressionStatus, progression01, progression02, progression03, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddProgressionEventWithScore(ProgressionStatus progressionStatus, string progression01, string progression02, string progression03, double score, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddProgressionEvent progressionStatus:{progressionStatus} progression01:{progression01} progression02:{progression02} progression03:{progression03} score:{score} fields:{json}");
                proxyGameAnalytics.AddProgressionEventWithScore((int)progressionStatus, progression01, progression02, progression03, score, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddDesignEvent(string eventId, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddDesignEvent eventId:{eventId} fields:{json}");
                proxyGameAnalytics.AddDesignEvent(eventId, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddDesignEventWithValue(string eventId, float value, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddDesignEventWithValue eventId:{eventId} value:{value} fields:{json}");
                proxyGameAnalytics.AddDesignEventWithValue(eventId, value, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddErrorEvent(ErrorSeverity severity, string message, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddErrorEvent severity:{severity} message:{message} fields:{json}");
                proxyGameAnalytics.AddErrorEvent((int)severity, message, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddAdEvent(AdAction adAction, AdType adType, string adSdkName, string adPlacement, Dictionary<string, string> fields = null, bool mergeFields = false)
        {
            if (IsNotInitialized) return;

            try
            {
                var json = FieldsToJson(fields);
                Debug.Log($"{logName} AddAdEvent adAction:{adAction} adType:{adType} adSdkName:{adSdkName} adPlacement:{adPlacement} fields:{json}");
                proxyGameAnalytics.AddAdEvent((int)adAction, (int)adType, adSdkName, adPlacement, json, mergeFields);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddImpressionAdMobEvent(string adUnitId, string adType, string currencyCode, AdMobImpressionPrecision precision, string networkName, long revenue, string adMobSdkVersion)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} AddImpressionAdMobEvent adUnitId:{adUnitId} adType:{adType} currencyCode:{currencyCode} precision:{precision} networkName:{networkName} revenue:{revenue} adMobSdkVersion:{adMobSdkVersion}");
                Utils.Jsoner.AddParams("adUnitId", adUnitId, "adType", adType, "currencyCode", currencyCode, "precision", (int)precision, "networkName", networkName, "revenue", revenue, "adMobSdkVersion", adMobSdkVersion);
                var json = Utils.Jsoner.GetJsonAndClear();
                proxyGameAnalytics.AddImpressionAdMobEvent(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddImpressionMaxEvent(string adUnitId, string adType, string networkName, string country, string placement, string creativeId, double revenue, string appLovinSdkVersion)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} AddImpressionMaxEvent adUnitId:{adUnitId} adType:{adType} networkName:{networkName} country:{country} placement:{placement} creativeId:{creativeId} revenue:{revenue} appLovinSdkVersion:{appLovinSdkVersion}");
                Utils.Jsoner.AddParams("adUnitId", adUnitId, "adType", adType, "networkName", networkName, "country", country, "placement", placement, "creativeId", creativeId, "revenue", revenue, "appLovinSdkVersion", appLovinSdkVersion);
                var json = Utils.Jsoner.GetJsonAndClear();
                proxyGameAnalytics.AddImpressionMaxEvent(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddImpressionIronSourceEvent(string adUnitId, string adType, string networkName, string country, string placement, string creativeId, double revenue, string precision, string ironSourceSdkVersion)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} AddImpressionIronSourceEvent adUnitId:{adUnitId} adType:{adType} networkName:{networkName} country:{country} placement:{placement} creativeId:{creativeId} revenue:{revenue} precision:{precision} ironSourceSdkVersion:{ironSourceSdkVersion}");
                Utils.Jsoner.AddParams("adUnitId", adUnitId, "adType", adType, "networkName", networkName, "country", country, "placement", placement, "creativeId", creativeId, "revenue", revenue, "precision", precision, "ironSourceSdkVersion", ironSourceSdkVersion);
                var json = Utils.Jsoner.GetJsonAndClear();
                proxyGameAnalytics.AddImpressionIronSourceEvent(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void AddImpressionIronSourceEvent(string allData, string ironSourceSdkVersion)
        {
            if (IsNotInitialized) return;

            try
            {
                Debug.Log($"{logName} AddImpressionIronSourceEvent allData:{allData} ironSourceSdkVersion:{ironSourceSdkVersion}");
                Utils.Jsoner.AddParams("allData", allData, "ironSourceSdkVersion", ironSourceSdkVersion);
                var json = Utils.Jsoner.GetJsonAndClear();
                proxyGameAnalytics.AddImpressionIronSourceEvent(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void SubscribeAdEventTracking()
        {
            if (IsNotInitialized) return;
            if (subscribedAdEventTracking) return;
            subscribedAdEventTracking = true;

            PlanktonMono.OnAdActionEvent += json =>
            {
                try
                {
                    proxyGameAnalytics.AddAdEvent(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
            Debug.Log($"{logName} Subscribed for tracking ad.");
        }
        
        public static void SubscribeAdMobImpressions()
        {
            if (IsNotInitialized) return;
            if (subscribedAdMobImpressions) return;
            subscribedAdMobImpressions = true;

            PlanktonMono.OnAdMobImpressionRevenueEvent += json =>
            {
                try
                {
                    proxyGameAnalytics.AddImpressionAdMobEvent(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
            Debug.Log($"{logName} Subscribed for AdMob impressions");
        }

        public static void SubscribeMaxImpressions()
        {
            if (IsNotInitialized) return;
            if (subscribedMaxImpressions) return;
            subscribedMaxImpressions = true;

            PlanktonMono.OnAppLovinMaxImpressionRevenueEvent += json =>
            {
                try
                {
                    proxyGameAnalytics.AddImpressionMaxEvent(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
            Debug.Log($"{logName} Subscribed for ApplovinMax impressions");
        }

        public static void SubscribeIronSourceImpressions()
        {
            if (IsNotInitialized) return;
            if (subscribedIronSourceImpressions) return;
            subscribedIronSourceImpressions = true;

            PlanktonMono.OnLevelPlayImpressionRevenueEvent += json =>
            {
                try
                {
                    proxyGameAnalytics.AddImpressionIronSourceEvent(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
            Debug.Log($"{logName} Subscribed for LevelPlay (ironSource) mediation impressions");
        }

        public static bool IsRemoteConfigsReady()
        {
            if (IsNotInitialized) return false;

            try
            {
                var result = proxyGameAnalytics.IsRemoteConfigsReady();
                Debug.Log($"{logName} IsRemoteConfigsReady:{result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            return false;
        }

        public static string GetRemoteConfigsValueAsString(string key, string defaultValue)
        {
            if (IsNotInitialized) return string.Empty;

            try
            {
                var result = proxyGameAnalytics.GetRemoteConfigsValueAsString(key, defaultValue);
                Debug.Log($"{logName} GetRemoteConfigsValueAsString:{result} key:{key} defaultValue:{defaultValue}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            return defaultValue;
        }

        public static string GetABTestingId()
        {
            if (IsNotInitialized) return string.Empty;

            try
            {
                var result = proxyGameAnalytics.GetABTestingId();
                Debug.Log($"{logName} GetABTestingId:{result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            return string.Empty;
        }

        public static string GetABTestingVariantId()
        {
            if (IsNotInitialized) return string.Empty;

            try
            {
                var result = proxyGameAnalytics.GetABTestingVariantId();
                Debug.Log($"{logName} GetABTestingVariantId:{result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            return string.Empty;
        }


        private static string FieldsToJson(Dictionary<string, string> fields)
        {
            string json = "{}";
            if (fields != null)
            {
                Utils.Jsoner.Add(fields);
                json = Utils.Jsoner.GetJsonAndClear();
            }
            return json;
        }

        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public enum ErrorSeverity
        {
            Undefined = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5
        }

        public enum ProgressionStatus
        {
            //Undefined progression
            Undefined = 0,
            // User started progression
            Start = 1,
            // User succesfully ended a progression
            Complete = 2,
            // User failed a progression
            Fail = 3
        }

        public enum ResourceFlowType
        {
            //Undefined progression
            Undefined = 0,
            // Source: Used when adding resource to a user
            Source = 1,
            // Sink: Used when removing a resource from a user
            Sink = 2
        }

        public enum AdAction
        {
            Undefined = 0,
            Clicked = 1,
            Show = 2,
            FailedShow = 3,
            RewardReceived = 4,
            Request = 5,
            Loaded = 6
        }

        public enum AdType
        {
            Undefined = 0,
            Video = 1,
            RewardedVideo = 2,
            Playable = 3,
            Interstitial = 4,
            OfferWall = 5,
            Banner = 6,
            AppOpen = 7
        }

        public enum AdError
        {
            Undefined = 0,
            Unknown = 1,
            Offline = 2,
            NoFill = 3,
            InternalError = 4,
            InvalidRequest = 5,
            UnableToPrecache = 6
        }

        [System.Serializable]
        public class Builder
        {
            public string gameKey = string.Empty;
            public string secretKey = string.Empty;
            public string userId = string.Empty;
            public string buildVersion = string.Empty;
            public List<string> resourceCurrencies = new List<string>();
            public List<string> resourceItemTypes = new List<string>();
            public List<string> customDimensions01 = new List<string>();
            public List<string> customDimensions02 = new List<string>();
            public List<string> customDimensions03 = new List<string>();
            public bool infoLog = true;
            public bool verboseLog = false;
            public bool submitUnityDebug = false;
            public bool submitUnityInfo = false;
            public bool submitUnityWarning = true;
            public bool submitUnityError = true;
            public bool submitUnityCritical = true;

            public Builder(string gameKey, string secretKey)
            {
                this.gameKey = gameKey;
                this.secretKey = secretKey;
            }

            public Builder SetUserId(string userId)
            {
                this.userId = userId;
                return this;
            }

            public Builder SetBuildVersion(string buildVersion)
            {
                this.buildVersion = buildVersion;
                return this;
            }

            public Builder ConfigureResourceCurrencies(params string[] currencies)
            {
                resourceCurrencies.AddRange(currencies);
                return this;
            }

            public Builder ConfigureItemTypes(params string[] itemTypes)
            {
                resourceItemTypes.AddRange(itemTypes);
                return this;
            }

            public Builder ConfigureCustomDimensions01(params string[] dimensions)
            {
                customDimensions01.AddRange(dimensions);
                return this;
            }

            public Builder ConfigureCustomDimensions02(params string[] dimensions)
            {
                customDimensions02.AddRange(dimensions);
                return this;
            }

            public Builder ConfigureCustomDimensions03(params string[] dimensions)
            {
                customDimensions03.AddRange(dimensions);
                return this;
            }

            public Builder SetInfoLog(bool enable)
            {
                infoLog = enable;
                return this;
            }

            public Builder SetVerboseLog(bool enable)
            {
                verboseLog = enable;
                return this;
            }

            public Builder SetSubmitUnityDebug(bool enable)
            {
                submitUnityDebug = enable;
                return this;
            }

            public Builder SetSubmitUnityInfo(bool enable)
            {
                submitUnityInfo = enable;
                return this;
            }

            public Builder SetSubmitUnityWarning(bool enable)
            {
                submitUnityWarning = enable;
                return this;
            }

            public Builder SetSubmitUnityError(bool enable)
            {
                submitUnityError = enable;
                return this;
            }

            public Builder SetSubmitUnityCritical(bool enable)
            {
                submitUnityCritical = enable;
                return this;
            }
        }
    }
}