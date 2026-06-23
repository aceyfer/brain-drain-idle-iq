#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;
using static Plankton.GameAnalytics;

namespace Plankton.Proxy
{
    public class ProxyGameAnalytics : Proxy
    {
        private const string logName = "[Plankton] [GameAnalaytics]";

        public ProxyGameAnalytics(string json) : base()
        {
            Debug.Log($"{logName} Initializing json:{json}");
#if !UNITY_EDITOR
            IosProxy.InitializeGameAnalytics(json);
#endif
            Debug.Log($"{logName} Initialized.");
        }

        public void SetCustomDimension01(string value)
        {
#if !UNITY_EDITOR
            IosProxy.SetCustomDimension01(value);
#endif
        }

        public void SetCustomDimension02(string value)
        {
#if !UNITY_EDITOR
            IosProxy.SetCustomDimension02(value);
#endif
        }

        public void SetCustomDimension03(string value)
        {
#if !UNITY_EDITOR
            IosProxy.SetCustomDimension03(value);
#endif
        }

        public void AddBusinessEvent(string currency, int amount, string itemType, string itemId, string cartType, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddBusinessEvent(currency, amount, itemType, itemId, cartType, "", fields, mergeFields);
#endif
        }

        public void AddBusinessEventWithReceipt(string currency, int amount, string itemType, string itemId, string cartType, string receipt, string store, string signature, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddBusinessEvent(currency, amount, itemType, itemId, cartType, receipt, fields, mergeFields);
#endif
        }

        public void AddResourceEvent(int flowType, string currency, float amount, string itemType, string itemId, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddResourceEvent(flowType, currency, (int)amount, itemType, itemId, fields, mergeFields);
#endif
        }

        public void AddProgressionEvent(int progressionStatus, string progression01, string progression02, string progression03, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddProgressionEvent(progressionStatus, progression01, progression02, progression03, fields, mergeFields);
#endif
        }

        public void AddProgressionEventWithScore(int progressionStatus, string progression01, string progression02, string progression03, double score, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddProgressionEventWithScore(progressionStatus, progression01, progression02, progression03, (int)score, fields, mergeFields);
#endif
        }

        public void AddDesignEvent(string eventId, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddDesignEvent(eventId, fields, mergeFields);
#endif
        }

        public void AddDesignEventWithValue(string eventId, float value, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddDesignEventWithValue(eventId, value, fields, mergeFields);
#endif
        }

        public void AddErrorEvent(int severity, string message, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddErrorEvent(severity, message, fields, mergeFields);
#endif
        }

        public void AddAdEvent(int adAction, int adType, string adSdkName, string adPlacement, string fields, bool mergeFields)
        {
#if !UNITY_EDITOR
            IosProxy.AddAdEvent(adAction, adType, adSdkName, adPlacement, fields, mergeFields);
#endif
        }

        public void AddAdEvent(string json)
        {
#if !UNITY_EDITOR
            IosProxy.AddAdEventWithJson(json);
#endif
        }

        public void AddImpressionAdMobEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionAdMobEvent: {json}");
            FreeVersion.NotAvailable();
        }

        public void AddImpressionMaxEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionMaxEvent: {json}");
            FreeVersion.NotAvailable();
        }

        public void AddImpressionIronSourceEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionIronSourceEvent: {json}");
            FreeVersion.NotAvailable();
        }

        public bool IsRemoteConfigsReady()
        {
            FreeVersion.NotAvailable();
            return false;
        }

        public string GetRemoteConfigsValueAsString(string key, string defaultValue)
        {
            FreeVersion.NotAvailable();
            return string.Empty;
        }

        public string GetABTestingId()
        {
            FreeVersion.NotAvailable();
            return string.Empty;
        }

        public string GetABTestingVariantId()
        {
            FreeVersion.NotAvailable();
            return string.Empty;
        }


        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        private static class IosProxy
        {
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void InitializeGameAnalytics([MarshalAs(UnmanagedType.LPStr)] string json);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void SetCustomDimension01([MarshalAs(UnmanagedType.LPStr)] string customDimension);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void SetCustomDimension02([MarshalAs(UnmanagedType.LPStr)] string customDimension);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void SetCustomDimension03([MarshalAs(UnmanagedType.LPStr)] string customDimension);

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddBusinessEvent(
                [MarshalAs(UnmanagedType.LPStr)] string currency,
                int amount,
                [MarshalAs(UnmanagedType.LPStr)] string itemType,
                [MarshalAs(UnmanagedType.LPStr)] string itemId,
                [MarshalAs(UnmanagedType.LPStr)] string cartType,
                [MarshalAs(UnmanagedType.LPStr)] string receipt,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddResourceEvent(
                int flowType,
                [MarshalAs(UnmanagedType.LPStr)] string currency,
                int amount,
                [MarshalAs(UnmanagedType.LPStr)] string itemType,
                [MarshalAs(UnmanagedType.LPStr)] string itemId,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddProgressionEvent(
                int progressionStatus,
                [MarshalAs(UnmanagedType.LPStr)] string progression01,
                [MarshalAs(UnmanagedType.LPStr)] string progression02,
                [MarshalAs(UnmanagedType.LPStr)] string progression03,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddProgressionEventWithScore(
                int progressionStatus,
                [MarshalAs(UnmanagedType.LPStr)] string progression01,
                [MarshalAs(UnmanagedType.LPStr)] string progression02,
                [MarshalAs(UnmanagedType.LPStr)] string progression03,
                int score,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddDesignEvent(
                [MarshalAs(UnmanagedType.LPStr)] string eventId,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddDesignEventWithValue(
                [MarshalAs(UnmanagedType.LPStr)] string eventId,
                double value,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddErrorEvent(
                int severity,
                [MarshalAs(UnmanagedType.LPStr)] string message,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddAdEvent(
                int adAction,
                int adType,
                [MarshalAs(UnmanagedType.LPStr)] string adSdkName,
                [MarshalAs(UnmanagedType.LPStr)] string adPlacement,
                [MarshalAs(UnmanagedType.LPStr)] string fields,
                bool mergeFields
            );

            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            public static extern void AddAdEventWithJson([MarshalAs(UnmanagedType.LPStr)] string json);

        }
    }
}
#endif