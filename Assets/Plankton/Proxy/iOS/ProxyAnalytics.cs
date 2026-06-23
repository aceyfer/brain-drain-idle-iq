#if UNITY_IOS
using UnityEngine;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    [Preserve]
    public class ProxyAnalytics : Proxy
    {
        private const string logName = "[Plankton] [Analytics]";

        public ProxyAnalytics() : base() {
#if !UNITY_EDITOR
            IosProxy.InitializeAnalytics();
#endif
        }

        public void SetUserProperty(string providers, string key, string value)
        {
            Debug.Log($"{logName} SetUserProperty providers:{providers} key:{key} value:{value}");
#if !UNITY_EDITOR
            IosProxy.SetUserProperty(providers, key, value);
#endif
        }

        public void LogEvent(string providers, string eventName, string jsonParams)
        {
            Debug.Log($"{logName} LogEvent providers:{providers} eventName:{eventName} jsonParams:{jsonParams}");
#if !UNITY_EDITOR
            IosProxy.LogEvent(providers, eventName, jsonParams);
#endif
        }

        public void TrackRevenue(string providers, string eventName, double amount, string currency)
        {
            Debug.Log($"{logName} TrackRevenue providers:{providers} eventName:{eventName} amount:{amount} currency:{currency}");
#if !UNITY_EDITOR
            IosProxy.TrackRevenue(providers, eventName, currency, amount);
#endif
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
            Debug.Log($"{logName} SetRemoteConfigMinimumFetchInterval interval:{interval}");
            FreeVersion.NotAvailable();
        }

        public void FetchRemoteConfig()
        {
            Debug.Log($"{logName} FetchRemoteConfig");
            FreeVersion.NotAvailable();
        }

        public string GetRemoteConfigValue(string key, string defaultValue)
        {
            Debug.Log($"{logName} GetRemoteConfigValue key:{key} value:{defaultValue}");
            FreeVersion.NotAvailable();
            return defaultValue;
        }

        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        private static class IosProxy
        {
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void InitializeAnalytics();
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void SetUserProperty([MarshalAs(UnmanagedType.LPStr)] string providers, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void LogEvent([MarshalAs(UnmanagedType.LPStr)] string providers, [MarshalAs(UnmanagedType.LPStr)] string eventName, [MarshalAs(UnmanagedType.LPStr)] string paramsJson);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void TrackRevenue([MarshalAs(UnmanagedType.LPStr)] string providers, [MarshalAs(UnmanagedType.LPStr)] string eventName, [MarshalAs(UnmanagedType.LPStr)] string currency, double amount);
        }
    }

}

#endif