#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    public class Proxy
    {
        private const string Version = "3.8.0";
        private const string logName = "[Plankton]";
        private static bool initialized = false;

        [DllImport("__Internal", CharSet = CharSet.Ansi)]
        private static extern void Initialize();

        public Proxy()
        {
            if (initialized) return;

            Debug.Log($"{logName} Initializing Plankton Version {Version}");
#if !UNITY_EDITOR
            Initialize();
#endif
            initialized = true;
            Debug.Log($"{logName} Initialized Plankton.");
        }

        public void DisplayFreeVersion()
        {
            Debug.LogWarning("This feature is not available in this Free version. To get a full version of Plankton please download it from: https://assetstore.unity.com/packages/tools/integration/268720");
        }
    }
}
#endif