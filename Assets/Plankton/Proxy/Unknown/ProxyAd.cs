#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyAd : Proxy
    {
        private const string logName = "[Plankton] [Ad]";

        public ProxyAd(string json)
        {
            Debug.Log($"{logName} Initializing with json:{json}");
        }

        public bool IsOnline()
        {
            return true;
        }

        // Available types: banner, interstitial, rewarded
        public bool IsReady(string type)
        {
            return false;
        }

        // Available types: banner, interstitial, rewarded
        public void Show(string type, string placement)
        {
            Debug.Log($"{logName} Show type:{type} placement:{placement}");
        }

        // Available types: banner
        public void Hide(string type)
        {
            Debug.Log($"{logName} Hide type:{type}");
        }

        public void TestAdMob()
        {
            Debug.Log($"{logName} TestAdMob");
        }

        public void TestMax()
        {
            Debug.Log($"{logName} TestMax");
        }

        public void TestLevelPlay()
        {
            Debug.Log($"{logName} TestLevelPlay");
        }

        public string GetAndroidAdId()
        {
            return string.Empty;
        }

        public void SetAdMobSsvCustomData(string type, string customData)
        {
            Debug.Log($"{logName} SetAdMobSsvCustomData type:{type}, customData:{customData}");
        }

        public void SetLevelPlayRewardedServerParameters(string parameters, bool clearPreviousParams)
        {
            Debug.Log($"{logName} SetLevelPlayRewardedServerParameters parameters:{parameters}, clearPreviousParams:{clearPreviousParams}");
        }
    }
}
#endif