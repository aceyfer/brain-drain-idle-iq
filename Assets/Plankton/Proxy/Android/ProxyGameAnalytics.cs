#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGameAnalytics : Proxy
    {
        private const string logName = "[Plankton] [GameAnalaytics]";

        private readonly AndroidJavaObject objectGameAnalytics = null;

        public ProxyGameAnalytics(string json)
        {
            Debug.Log($"{logName} Initializing json:{json}");
            objectGameAnalytics = GetAndroidObject("getGameAnalytics");
#if !UNITY_EDITOR
            if (objectGameAnalytics != null)
            {
                objectGameAnalytics.Call("initialize", json);
                Debug.Log($"{logName} Called initialization");
            }
            else Debug.LogWarning($"{logName} objectAnalytics is null!");
#endif
        }

        public void SetCustomDimension01(string value)
        {
            objectGameAnalytics?.Call("setCustomDimension01", value);
        }

        public void SetCustomDimension02(string value)
        {
            objectGameAnalytics?.Call("setCustomDimension02", value);
        }

        public void SetCustomDimension03(string value)
        {
            objectGameAnalytics?.Call("setCustomDimension03", value);
        }

        public void AddBusinessEvent(string currency, int amount, string itemType, string itemId, string cartType, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addBusinessEvent", currency, amount, itemType, itemId, cartType, fields, mergeFields);
        }

        public void AddBusinessEventWithReceipt(string currency, int amount, string itemType, string itemId, string cartType, string receipt, string store, string signature, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addBusinessEventWithReceipt", currency, amount, itemType, itemId, cartType, receipt, store, signature, fields, mergeFields);
        }

        public void AddResourceEvent(int flowType, string currency, float amount, string itemType, string itemId, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addResourceEvent", flowType, currency, amount, itemType, itemId, fields, mergeFields);
        }

        public void AddProgressionEvent(int progressionStatus, string progression01, string progression02, string progression03, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addProgressionEvent", progressionStatus, progression01, progression02, progression03, fields, mergeFields);
        }

        public void AddProgressionEventWithScore(int progressionStatus, string progression01, string progression02, string progression03, double score, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addProgressionEventWithScore", progressionStatus, progression01, progression02, progression03, score, fields, mergeFields);
        }

        public void AddDesignEvent(string eventId, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addDesignEvent", eventId, fields, mergeFields);
        }

        public void AddDesignEventWithValue(string eventId, float value, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addDesignEventWithValue", eventId, value, fields, mergeFields);
        }

        public void AddErrorEvent(int severity, string message, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addErrorEvent", severity, message, fields, mergeFields);
        }

        public void AddAdEvent(int adAction, int adType, string adSdkName, string adPlacement, string fields, bool mergeFields)
        {
            objectGameAnalytics?.Call("addAdEvent", adAction, adType, adSdkName, adPlacement, fields, mergeFields);
        }

        public void AddAdEvent(string json)
        {
            Debug.Log($"{logName} AddAdEvent: {json}");
            objectGameAnalytics?.Call("addAdEvent", json);
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
            return defaultValue;
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
    }
}
#endif