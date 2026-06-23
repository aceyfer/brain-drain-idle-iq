#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyUtils : Proxy
    {
        private const string logName = "[Plankton] [Utils]";

        public void ShowRateus()
        {
            Debug.Log($"{logName} calling ShowRateus");
        }

        public void ShareText(string text)
        {
            Debug.Log($"{logName} calling ShareText");
        }

        public void ShareFile(string path, string text)
        {
            Debug.Log($"{logName} calling ShareFile");
        }
    }
}
#endif