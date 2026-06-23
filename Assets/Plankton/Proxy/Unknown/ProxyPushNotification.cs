#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyPushNotification : Proxy
    {
        private const string logName = "[Plankton] [PushNotification]";

        public void GetToken()
        {
            Debug.Log($"{logName} calling GetFirebaseToken");
        }
    }
}
#endif