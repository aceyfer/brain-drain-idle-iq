#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyAnalytics : Proxy
    {
        private const string logName = "[Plankton] [Analytics]";

        private readonly AndroidJavaObject objectAnalytics = null;

        public ProxyAnalytics()
        {
            Debug.Log($"{logName} Getting android objects...");
            objectAnalytics = GetAndroidObject("getAnalytics");
#if !UNITY_EDITOR
            if (objectAnalytics == null) Debug.LogWarning($"{logName} objectAnalytics is null!");
#endif
            Debug.Log($"{logName} Collected android objects objectAnalytics:{objectAnalytics}");
        }

        public void SetUserProperty(string providers, string key, string value)
        {
            objectAnalytics?.Call("setUserProperty", providers, key, value);
        }

        public void LogEvent(string providers, string eventName, string jsonParams)
        {
            objectAnalytics?.Call("logEvent", providers, eventName, jsonParams);
        }

        public void TrackRevenue(string providers, string eventName, double amount, string currency)
        {
            objectAnalytics?.Call("trackRevenue", providers, eventName, amount, currency);
        }

        public string GetAppsFlyerUID()
        {
            FreeVersion.NotAvailable();
            return string.Empty;
        }

        public string GetTenjinInstallationId()
        {
            FreeVersion.NotAvailable();
            return string.Empty;
        }

        public void SetRemoteConfigMinimumFetchInterval(long interval)
        {
            FreeVersion.NotAvailable();
        }

        public void FetchRemoteConfig()
        {
            FreeVersion.NotAvailable();
        }

        public string GetRemoteConfigValue(string key, string defaultValue)
        {
            FreeVersion.NotAvailable();
            return defaultValue;
        }
    }
}
#endif