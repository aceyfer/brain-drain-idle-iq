using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public class GoogleUmp
    {
        private const string logName = "[GoogleUmp]";

        private static readonly ProxyGoogleUmp proxyGoogleUmp = new ProxyGoogleUmp();

        public static void RequestConsentInfoUpdate(System.Action<ConsentResults> callback, DebugGeography debugGeography = DebugGeography.Disabled, string testDeviceHashedId = "", bool tagForUnderAgeOfConsent = false)
        {
            Debug.Log($"{logName} RequestConsentInfoUpdate setDebugGeography:{debugGeography} setTagForUnderAgeOfConsent:{tagForUnderAgeOfConsent}, testDeviceHashedId:{testDeviceHashedId}");

            PlanktonMono.onRequestConsentInfoUpdateResult = json =>
            {
                Debug.Log($"{logName} onRequestConsentInfoUpdateResult : {json}");
                var result = JsonUtility.FromJson<ConsentResults>(json);
                callback?.Invoke(result);
            };

            try
            {
                proxyGoogleUmp.RequestConsentInfoUpdate(debugGeography.ToString().ToLower(), testDeviceHashedId, tagForUnderAgeOfConsent);

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(new ConsentResults() { success = true }));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(new ConsentResults() { error_message = ex.Message });
            }
        }

        public static void LoadAndShowConsentFormIfRequired(System.Action<ConsentResults> callback)
        {
            Debug.Log($"{logName} LoadAndShowConsentFormIfRequired");

            PlanktonMono.onConsentFormDismissed = json =>
            {
                Debug.Log($"{logName} onConsentFormDismissed : {json}");
                var result = JsonUtility.FromJson<ConsentResults>(json);
                callback?.Invoke(result);
            };

            try
            {
                proxyGoogleUmp.LoadAndShowConsentFormIfRequired();

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(new ConsentResults() { success = true }));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(new ConsentResults() { error_message = ex.Message });
            }
        }

        public static void ShowPrivacyOptionsForm(System.Action<ConsentResults> callback)
        {
            Debug.Log($"{logName} ShowPrivacyOptionsForm");

            PlanktonMono.onConsentFormDismissed = json =>
            {
                var result = JsonUtility.FromJson<ConsentResults>(json);
                callback?.Invoke(result);
            };

            try
            {
                proxyGoogleUmp.ShowPrivacyOptionsForm();

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(new ConsentResults() { success = true }));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(new ConsentResults() { error_message = ex.Message });
            }
        }

        public static void ResetConsentInformation()
        {
            Debug.Log($"{logName} ResetConsentInformation");
            try
            {
                proxyGoogleUmp.ResetConsentInformation();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static bool CanRequestAds()
        {
            try
            {
                var result = proxyGoogleUmp.CanRequestAds();
                Debug.Log($"{logName} CanRequestAds: {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public static bool IsPrivacyOptionsRequired()
        {
            Debug.Log($"{logName} IsPrivacyOptionsRequired");
            try
            {
                return proxyGoogleUmp.IsPrivacyOptionsRequired();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public enum DebugGeography
        {
            Disabled,
            Eea,
            Other
        }

        [System.Serializable]
        public class ConsentResults
        {
            public bool success = false;
            public int error_code = 0;
            public string error_message = string.Empty;
            public override string ToString() => $"success:{success}, error_code:{error_code}, error_message:{error_message}";
        }
    }
}