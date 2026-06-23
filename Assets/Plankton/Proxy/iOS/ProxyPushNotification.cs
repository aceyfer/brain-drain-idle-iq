#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    public class ProxyPushNotification : Proxy
    {
        private const string logName = "[Plankton] [PushNotification]";

        public void GetToken()
        {
            Debug.Log($"{logName} calling GetFCMToken");
            FreeVersion.NotAvailable();
        }
    }
}
#endif