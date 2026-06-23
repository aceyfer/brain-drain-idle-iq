#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class Proxy
    {
        private const string Version = "3.8.0";
        private const string logName = "[Plankton]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInitialize()
        {
            Debug.Log($"{logName} Initializing Plankton Version {Version}");
            Debug.LogWarning($"{logName} Plankton will not work on this platform!");
            Debug.Log($"{logName} Initialized Plankton.");
        }
    }
}
#endif