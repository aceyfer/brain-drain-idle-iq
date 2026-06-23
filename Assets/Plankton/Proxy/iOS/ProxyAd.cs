#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    public class ProxyAd : Proxy
    {
        private const string logName = "[Plankton] [Ad]";

        public ProxyAd(string json)
        {
            Debug.Log($"{logName} Initializing with json:{json}");
#if !UNITY_EDITOR
            IosProxy.InitializeAds(json);
#endif
        }

        public bool IsOnline()
        {
#if !UNITY_EDITOR
            return IosProxy.IsOnline();
#else
            return true;
#endif
        }

        // Available types: banner, interstitial, rewarded, rewarded_interstitial(AdMob Only), app_open
        public bool IsReady(string type)
        {
#if !UNITY_EDITOR
            return IosProxy.IsAdReady(type);
#else
            return true;
#endif
        }

        // Available types: banner, interstitial, rewarded, rewarded_interstitial(AdMob Only), app_open
        public void Show(string type, string placement)
        {
#if !UNITY_EDITOR
            IosProxy.ShowAd(type, placement);
#endif
        }

        // Available types: banner
        public void Hide(string type)
        {
#if !UNITY_EDITOR
            IosProxy.HideAd(type);
#endif
        }

        public void TestAdMob()
        {
#if !UNITY_EDITOR
            IosProxy.ShowAdMobDebugger();
#endif
        }

        public void TestMax()
        {
            FreeVersion.NotAvailable();    
        }

        public void TestLevelPlay()
        {
            FreeVersion.NotAvailable();
        }

        public void SetAdMobSsvCustomData(string type, string customData)
        {
#if !UNITY_EDITOR
            IosProxy.SetAdMobSsvCustomData(type, customData);
#endif
        }

        public void SetLevelPlayRewardedServerParameters(string parameters, bool clearPreviousParams)
        {
            FreeVersion.NotAvailable();
        }

        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        private static class IosProxy
        {
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void InitializeAds([MarshalAs(UnmanagedType.LPStr)] string json);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern bool IsOnline();
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern bool IsAdReady([MarshalAs(UnmanagedType.LPStr)] string type);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void ShowAd([MarshalAs(UnmanagedType.LPStr)] string type, [MarshalAs(UnmanagedType.LPStr)] string placement);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void HideAd([MarshalAs(UnmanagedType.LPStr)] string type);
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void ShowAdMobDebugger();
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void SetAdMobSsvCustomData([MarshalAs(UnmanagedType.LPStr)] string type, [MarshalAs(UnmanagedType.LPStr)] string customData);
        }
    }
}

#endif