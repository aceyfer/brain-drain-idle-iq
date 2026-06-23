#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGameAnalytics : Proxy
    {
        private const string logName = "[Plankton] [GameAnalaytics]";

        public ProxyGameAnalytics(string json)
        {
            Debug.Log($"{logName} Initializing json:{json}");
        }

        public void SetCustomDimension01(string value)
        {
            Debug.Log($"{logName} SetCustomDimension01 value:{value}");
        }

        public void SetCustomDimension02(string value)
        {
            Debug.Log($"{logName} SetCustomDimension02 value:{value}");
        }

        public void SetCustomDimension03(string value)
        {
            Debug.Log($"{logName} SetCustomDimension03 value:{value}");
        }

        public void AddBusinessEvent(string currency, int amount, string itemType, string itemId, string cartType, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddBusinessEvent currency:{currency}, amount:{amount}, itemType:{itemType}, itemId:{itemId}, cartType:{cartType}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddBusinessEventWithReceipt(string currency, int amount, string itemType, string itemId, string cartType, string receipt, string store, string signature, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddBusinessEventWithReceipt currency:{currency}, amount:{amount}, itemType:{itemType}, itemId:{itemId}, cartType:{cartType}, receipt:{receipt}, store:{store}, signature:{signature}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddResourceEvent(int flowType, string currency, float amount, string itemType, string itemId, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddResourceEvent flowType:{flowType}, currency:{currency}, amount:{amount}, itemType:{itemType}, itemId:{itemId}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddProgressionEvent(int progressionStatus, string progression01, string progression02, string progression03, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddProgressionEvent progressionStatus:{progressionStatus}, progression01:{progression01}, progression02:{progression02}, progression03:{progression03}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddProgressionEventWithScore(int progressionStatus, string progression01, string progression02, string progression03, double score, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddProgressionEventWithScore progressionStatus:{progressionStatus}, progression01:{progression01}, progression02:{progression02}, progression03:{progression03}, score:{score}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddDesignEvent(string eventId, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddDesignEvent eventId:{eventId}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddDesignEventWithValue(string eventId, float value, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddDesignEventWithValue eventId:{eventId}, value:{value}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddErrorEvent(int severity, string message, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddErrorEvent severity:{severity}, message:{message}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddAdEvent(int adAction, int adType, string adSdkName, string adPlacement, string fields, bool mergeFields)
        {
            Debug.Log($"{logName} AddAdEvent adAction:{adAction}, adType:{adType}, adSdkName:{adSdkName}, adPlacement:{adPlacement}, fields:{fields}, mergeFields:{mergeFields}");
        }

        public void AddAdEvent(string json)
        {
            Debug.Log($"{logName} AddAdEvent json:{json}");
        }

        public void AddImpressionAdMobEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionAdMobEvent: {json}");
        }

        public void AddImpressionMaxEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionMaxEvent: {json}");
        }

        public void AddImpressionIronSourceEvent(string json)
        {
            Debug.Log($"{logName} AddImpressionIronSourceEvent: {json}");
        }

        public bool IsRemoteConfigsReady()
        {
            Debug.Log($"{logName} IsRemoteConfigsReady return false");
            return false;
        }

        public string GetRemoteConfigsValueAsString(string key, string defaultValue)
        {
            Debug.Log($"{logName} GetRemoteConfigsValueAsString key:{key} value:{defaultValue}");
            return defaultValue;
        }

        public string GetABTestingId()
        {
            Debug.Log($"{logName} GetABTestingId return string.Empty");
            return string.Empty;
        }

        public string GetABTestingVariantId()
        {
            Debug.Log($"{logName} GetABTestingVariantId return string.Empty");
            return string.Empty;
        }
    }
}
#endif