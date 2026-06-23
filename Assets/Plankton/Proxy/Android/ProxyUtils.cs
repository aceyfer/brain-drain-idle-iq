#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyUtils : Proxy
    {
        private const string logName = "[Plankton] [Utils]";

        private readonly AndroidJavaObject objectRateApp = null;
        private readonly AndroidJavaObject objectSharing = null;

        public ProxyUtils()
        {
            Debug.Log($"{logName} getting android objects...");
            objectRateApp = GetAndroidObject("getRateApp");
            objectSharing = GetAndroidObject("getSharing");
            Debug.Log($"{logName} Collected android objects rateApp:{objectRateApp} sharing:{objectSharing}");
        }

        public void ShowRateus()
        {
            Debug.Log($"{logName} calling ShowRateus");
            if (objectRateApp == null)
                Debug.Log("objectRateApp: null");
            else
                Debug.Log("objectRateApp: " + objectRateApp);

            objectRateApp?.Call("show");
        }

        public void ShareText(string text)
        {
            Debug.Log($"{logName} calling ShareText");
            objectSharing?.Call("shareText", text);
        }

        public void ShareFile(string path, string text)
        {
            Debug.Log($"{logName} calling ShareFile");
            FreeVersion.NotAvailable();
        }
    }
}
#endif