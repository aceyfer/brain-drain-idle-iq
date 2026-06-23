#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyAnalytics : Proxy
    {
        private const string logName = "[Plankton] [Analytics]";

        public void SetUserProperty(string providers, string key, string value)
        {
            Debug.Log($"{logName} SetUserProperty providers:{providers} key:{key} value:{value}");
        }

        public void LogEvent(string providers, string eventName, string jsonParams)
        {
            Debug.Log($"{logName} LogEvent providers:{providers} eventName:{eventName} jsonParams:{jsonParams}");
        }

        public void TrackRevenue(string providers, string eventName, double amount, string currency)
        {
            Debug.Log($"{logName} TrackRevenue providers:{providers} eventName:{eventName} amount:{amount} currency:{currency}");
        }

        public string GetAppsFlyerUID()
        {
            Debug.Log($"{logName} GetAppsFlyerUID returned string.Empty");
            return string.Empty;
        }

        public string GetTenjinInstallationId()
        {
            Debug.Log($"{logName} GetTenjinInstallationId returned string.Empty");
            return string.Empty;
        }

        public void SetRemoteConfigMinimumFetchInterval(long interval)
        {
            Debug.Log($"{logName} SetRemoteConfigMinimumFetchInterval interval:{interval}");
        }

        public void FetchRemoteConfig()
        {
            Debug.Log($"{logName} FetchRemoteConfig");
        }

        public string GetRemoteConfigValue(string key, string defaultValue)
        {
            Debug.Log($"{logName} GetRemoteConfigValue key:{key} value:{defaultValue}");
            return defaultValue;
        }
    }
}
#endif