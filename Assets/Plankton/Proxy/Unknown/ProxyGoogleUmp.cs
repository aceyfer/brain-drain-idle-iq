#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGoogleUmp : Proxy
    {
        private const string logName = "[Plankton] [GoogleUmp]";

        public void RequestConsentInfoUpdate(string debugGeography, string testDeviceHashedId, bool tagForUnderAgeOfConsent = false)
        {
            Debug.Log($"{logName} calling RequestConsentInfoUpdate debugGeography:{debugGeography} testDeviceHashedId:{testDeviceHashedId} tagForUnderAgeOfConsent:{tagForUnderAgeOfConsent}");
        }

        public void LoadAndShowConsentFormIfRequired()
        {
            Debug.Log($"{logName} calling LoadAndShowConsentFormIfRequired");
        }

        public void ShowPrivacyOptionsForm()
        {
            Debug.Log($"{logName} calling ShowPrivacyOptionsForm");
        }

        public void ResetConsentInformation()
        {
            Debug.Log($"{logName} calling ResetConsentInformation");
        }

        public bool CanRequestAds()
        {
            Debug.Log($"{logName} calling CanRequestAds");
            return false;
        }

        public bool IsPrivacyOptionsRequired()
        {
            Debug.Log($"{logName} calling IsPrivacyOptionsRequired");
            return false;
        }
    }
}
#endif