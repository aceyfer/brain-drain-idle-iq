using System.Collections.Generic;
using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public class GeneralAnalytics
    {
        private const string logName = "[GeneralAnalytics]";
        private static ProxyAnalytics proxyAnalytics = null;
        private static readonly List<string> builder = new List<string>(64);

        private static bool IsNotInitialized
        {
            get
            {
                if (proxyAnalytics == null)
                    Debug.Log($"{logName} Feature needs to be initialized first!");
                return proxyAnalytics == null;
            }
        }

        public static void Initialize()
        {
            if (proxyAnalytics != null)
            {
                Debug.Log($"{logName} Feature already initialized!");
                return;
            }

            try
            {
                proxyAnalytics = new ProxyAnalytics();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void SetUserProperty(Provider providers, string key, string value)
        {
            if (IsNotInitialized) return;

            try
            {
                var providerList = CollectProviders(providers);
                Debug.Log($"{logName} SetUserProperty {key} : {value}");
                proxyAnalytics.SetUserProperty(providerList, key, value);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void TrackRevenue(Provider providers, string eventName, double amount, string currency)
        {
            if (IsNotInitialized) return;

            try
            {
                var providerList = CollectProviders(providers);
                Debug.Log($"{logName} TrackRevenue {providerList} {eventName} {amount} {currency}");
                proxyAnalytics.TrackRevenue(providerList, eventName, amount, currency);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // simply add additional parameters in paired Name,Value sequence.
        // NewEvent(Provider.Default, "level_end", "world", "starting_world", "score", 39, "health", 5)
        public static void LogEvent(Provider providers, string eventName, params object[] args)
        {
            if (IsNotInitialized) return;

            Utils.Jsoner.AddParams(args);
            LogEvent(eventName, Utils.Jsoner.GetJsonAndClear(), providers);
        }

        public static void LogEvent(Provider providers, string eventName, Dictionary<string, string> args)
        {
            if (IsNotInitialized) return;

            Utils.Jsoner.Add(args);
            LogEvent(eventName, Utils.Jsoner.GetJsonAndClear(), providers);
        }


        private static void LogEvent(string eventName, string jsonParams = "{}", Provider providers = Provider.Default)
        {
            try
            {
                var providerList = CollectProviders(providers);
                Debug.Log($"{logName} LogEvent {providerList} {eventName} {jsonParams}");
                proxyAnalytics.LogEvent(providerList, eventName, jsonParams);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static string GetAppsFlyerUID()
        {
            if (IsNotInitialized) return string.Empty;

            return proxyAnalytics.GetAppsFlyerUID();
        }

        public static string GetTenjinInstallationId()
        {
            if (IsNotInitialized) return string.Empty;

            return proxyAnalytics.GetTenjinInstallationId();
        }

        private static string CollectProviders(Provider providers)
        {
            builder.Clear();
            if (providers.HasFlag(Provider.Default) ||
                providers.HasFlag(Provider.Firebase)) builder.Add("firebase");
            if (providers.HasFlag(Provider.Yandex)) builder.Add("yandex");
            if (providers.HasFlag(Provider.Appsflyer)) builder.Add("appsflyer");
            if (providers.HasFlag(Provider.Tenjin)) builder.Add("tenjin");
#if METRIX
            if (providers.HasFlag(Provider.Metrix)) builder.Add("metrix");
#endif
            return string.Join(",", builder);
        }


        public static class RemoteConfig
        {
            public static void SetMinimumFetchInterval(long interval) 
            {
                if (IsNotInitialized) return;

                Debug.Log($"{logName} SetMinimumFetchInterval RemoteConfig to {interval} seconds.");
                proxyAnalytics.SetRemoteConfigMinimumFetchInterval(interval);
            }
            
            public static void Fetch(System.Action<string> onSucceed, System.Action onFailed)
            {
                if (IsNotInitialized)
                {
                    onFailed?.Invoke();
                    return;
                }

                PlanktonMono.onRemoteConfigFetched = json =>
                {
                    Debug.Log($"{logName} Remote config fetched. Keys:[{json}]");
                    onSucceed?.Invoke(json);
                };

                PlanktonMono.onRemoteConfigFailed = () =>
                {
                    Debug.Log($"{logName} Failed to fetch remote config!");
                    onFailed?.Invoke();
                };

                Debug.Log($"{logName} Fetching RemoteConfig...");
                proxyAnalytics.FetchRemoteConfig();
            }

            public static string Get(string key, string defaultValue)
            {
                if (IsNotInitialized) return defaultValue;

                try
                {
                    Debug.Log($"{logName} GetRemoteConfigValue for key:<{key}> with defaultValue:<{defaultValue}>");
#if UNITY_EDITOR
                    return defaultValue;
#else
                    return proxyAnalytics.GetRemoteConfigValue(key, defaultValue);
#endif
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return defaultValue;
                }
            }

            public static int Get(string key, int defaultValue)
            {
                if (IsNotInitialized) return defaultValue;

                try
                {
                    Debug.Log($"{logName} GetRemoteConfigValue for key:<{key}> with defaultValue:<{defaultValue}>");
#if UNITY_EDITOR
                    return defaultValue;
#else
                    var rvalue = proxyAnalytics.GetRemoteConfigValue(key, defaultValue.ToString());
                    if (int.TryParse(rvalue, out int res))
                        return res;
                    else
                        return defaultValue;
#endif
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return defaultValue;
                }
            }

            public static float Get(string key, float defaultValue)
            {
                if (IsNotInitialized) return defaultValue;

                try
                {
                    Debug.Log($"{logName} GetRemoteConfigValue for key:<{key}> with defaultValue:<{defaultValue}>");
#if UNITY_EDITOR
                    return defaultValue;
#else
                    var rvalue = proxyAnalytics.GetRemoteConfigValue(key, defaultValue.ToString());
                    if (float.TryParse(rvalue, out float res))
                        return res;
                    else
                        return defaultValue;
#endif
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    return defaultValue;
                }
            }
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        [System.Flags]
        public enum Provider
        {
            Default = 1 << 0,
            Firebase = 1 << 1,
            Yandex = 1 << 2,
            Appsflyer = 1 << 3,
            Tenjin = 1 << 4,
#if METRIX
            Metrix = 1 << 5,
#endif
        }
    }
}