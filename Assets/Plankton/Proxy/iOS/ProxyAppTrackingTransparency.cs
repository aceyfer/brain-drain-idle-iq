#if UNITY_IOS
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyAppTrackingTransparency : Proxy
    {
        private const string logName = "[Plankton] [AppTrackingTransparency]";

        public void RequestTrackingAuthorization()
        {
            Debug.Log($"{logName} calling RequestTrackingAuthorization");
            FreeVersion.NotAvailable();
        }

        public string GetTrackingAuthorizationStatus()
        {
            Debug.Log($"{logName} calling GetTrackingAuthorizationStatus");
            FreeVersion.NotAvailable();
            return string.Empty;
        }

    }
}

#endif