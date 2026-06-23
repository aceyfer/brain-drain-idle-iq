#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    public class ProxyUtils : Proxy
    {
        private const string logName = "[Plankton] [Utils]";

        public void ShowRateus()
        {
            Debug.Log($"{logName} ShowRateus");
#if !UNITY_EDITOR
            IosProxy.RateApp();
#endif
        }

        public void ShareText(string text)
        {
            Debug.Log($"{logName} ShareText: {text}");
#if !UNITY_EDITOR
            IosProxy.ShareText(text);
#endif
        }


        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        private class IosProxy
        {
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void RateApp();
            [DllImport("__Internal", CharSet = CharSet.Ansi)] public static extern void ShareText([MarshalAs(UnmanagedType.LPStr)] string text);
        }
    }
}

#endif