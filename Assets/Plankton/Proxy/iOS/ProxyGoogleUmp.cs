#if UNITY_IOS
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plankton.Proxy
{
    public class ProxyGoogleUmp : Proxy
    {
        private const string logName = "[Plankton] [Utils]";

        public void RequestConsentInfoUpdate(string debugGeography, string testDeviceHashedId, bool tagForUnderAgeOfConsent)
        {
            Debug.Log($"{logName} calling RequestConsentInfoUpdate debugGeography:{debugGeography} testDeviceHashedId:{testDeviceHashedId} tagForUnderAgeOfConsent:{tagForUnderAgeOfConsent}");
            FreeVersion.NotAvailable();
        }

        public void LoadAndShowConsentFormIfRequired()
        {
            Debug.Log($"{logName} calling LoadAndShowConsentFormIfRequired");
            FreeVersion.NotAvailable();
        }

        public void ShowPrivacyOptionsForm()
        {
            Debug.Log($"{logName} calling ShowPrivacyOptionsForm");
            FreeVersion.NotAvailable();
        }

        public void ResetConsentInformation()
        {
            Debug.Log($"{logName} calling ResetConsentInformation");
            FreeVersion.NotAvailable();
        }

        public bool CanRequestAds()
        {
            Debug.Log($"{logName} calling CanRequestAds");
            FreeVersion.NotAvailable();
            return true;
        }

        public bool IsPrivacyOptionsRequired()
        {
            Debug.Log($"{logName} calling IsPrivacyOptionsRequired");
            FreeVersion.NotAvailable();
            return true;
        }
    }
}

#endif