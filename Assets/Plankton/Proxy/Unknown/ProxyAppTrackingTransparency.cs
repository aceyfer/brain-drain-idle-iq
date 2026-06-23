#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyAppTrackingTransparency : Proxy
    {
        private const string logName = "[Plankton] [AppTrackingTransparency]";

        private const string authorizationStatus = "Authorized";

        public void RequestTrackingAuthorization()
        {
            Debug.Log($"{logName} calling RequestTrackingAuthorization");

            PlanktonMono.CallInUnityThread(() =>
            {
                var json = $"{{\"status\": \"{authorizationStatus}\"}}";
                Debug.Log($"{logName} OnATTAuthorizationResult : {json}");
                PlanktonMono.onATTAuthorizationResult?.Invoke(json);
            });
        }

        public string GetTrackingAuthorizationStatus()
        {
            Debug.Log($"{logName} calling GetTrackingAuthorizationStatus");
            return authorizationStatus;
        }
    }
}
#endif