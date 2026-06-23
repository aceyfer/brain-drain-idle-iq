using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public class AppTrackingTransparency
    {
        private const string logName = "[AppTrackingTransparency]";

        private static readonly ProxyAppTrackingTransparency proxy = new ProxyAppTrackingTransparency();

        public static void RequestTrackingAuthorization(System.Action<AuthorizationStatus> callback)
        {
            Debug.Log($"{logName} RequestTrackingAuthorization");
            try
            {
                PlanktonMono.onATTAuthorizationResult = json =>
                {
                    var result = JsonUtility.FromJson<ATTAuthorizationResult>(json);
                    var status = ConvertAuthorizationStatus(result.status);
                    callback?.Invoke(status);
                };

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(AuthorizationStatus.Authorized));
#else
                proxy.RequestTrackingAuthorization();
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static AuthorizationStatus GetTrackingAuthorizationStatus()
        {
            Debug.Log($"{logName} GetTrackingAuthorizationStatus");
            try
            {
                var result = proxy.GetTrackingAuthorizationStatus();
                return ConvertAuthorizationStatus(result);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return AuthorizationStatus.Unknown;
            }
        }


        private static AuthorizationStatus ConvertAuthorizationStatus(string status)
        {
            switch (status.ToLower())
            {
                case "notdetermined": return AuthorizationStatus.NotDetermined;
                case "restricted": return AuthorizationStatus.Restricted;
                case "denied": return AuthorizationStatus.Denied;
                case "authorized": return AuthorizationStatus.Authorized;
                default: return AuthorizationStatus.Unknown;
            }
        }

        //////////////////////////////////////////////////////
        /// NESTED MEMBERS
        //////////////////////////////////////////////////////
        public enum AuthorizationStatus
        {
            Unknown,
            NotDetermined,
            Restricted,
            Denied,
            Authorized
        }

        [System.Serializable]
        public class ATTAuthorizationResult
        {
            public string status = string.Empty;
        }

    }
}