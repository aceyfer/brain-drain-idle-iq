using UnityEngine;
using System.Collections.Generic;
using Plankton.Proxy;

namespace Plankton
{
    public class Billing
    {
        protected const string logName = "[Billing]";

        public static bool blackBackground = false;

        private static ProxyBilling proxyBilling = null;
        private static bool initialized = false;
        private static string androidCustomRsaKey = string.Empty;

        private static bool IsNotInitialized
        {
            get
            {
                if (!initialized || proxyBilling == null)
                    Debug.Log($"{logName} Feature needs to be initialized first!");
                return !initialized || proxyBilling == null;
            }
        }

        public static void SetRsaKey(string rsaKey)
        {
            androidCustomRsaKey = rsaKey;
        }

        public static void Initialize(System.Action<bool> callback)
        {
            if (initialized)
            {
                Debug.Log($"{logName} Feature already initialized!");
                callback?.Invoke(true);
                return;
            }

            PlanktonMono.onBillingInitializationResult = json =>
            {
                var result = Utils.Jsoner.FromJson(json, new InitializationResult());
                initialized = result.success.ToLower() == "true";
                callback?.Invoke(initialized);
            };

            try
            {
#if UNITY_IOS
                proxyBilling = new ProxyBilling();
#elif MYKET
                proxyBilling = new ProxyBilling("Myket", androidCustomRsaKey);
#elif BAZAAR
                proxyBilling = new ProxyBilling("Bazaar", androidCustomRsaKey);
#elif UNITY_ANDROID
                proxyBilling = new ProxyBilling("GooglePlay");
#endif

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() =>
                {
                    initialized = true;
                    callback?.Invoke(initialized);
                });
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(false);
            }
        }

        public static void StartPurchase(string sku, System.Action<PurchaseStatus, PurchaseResult> callback, string androidObfuscatedAccountId = "", string androidObfuscatedProfiledId = "", bool androidAutoAck = false, bool androidAutoConsume = false)
        {
            if (IsNotInitialized)
            {
                callback?.Invoke(PurchaseStatus.Failed, new PurchaseResult() { status = "failed" });
                return;
            }

#if UNITY_IOS
            Debug.Log($"{logName} Start purchase {sku}");
#elif MYKET
            Debug.Log($"{logName} Start purchase {sku} payload:{androidObfuscatedAccountId}{androidObfuscatedProfiledId}");
#elif BAZAAR
            Debug.Log($"{logName} Start purchase {sku} payload:{androidObfuscatedAccountId}{androidObfuscatedProfiledId}");
#elif UNITY_ANDROID
            Debug.Log($"{logName} Start purchase {sku} obfuscatedAccountId:{androidObfuscatedAccountId} obfuscatedProfiledId:{androidObfuscatedProfiledId} autoAck:{androidAutoAck} autoConsume:{androidAutoConsume}");
#endif

            PlanktonMono.fullscreenBox = blackBackground;
            PlanktonMono.onPurchaseResult = json =>
            {
                PlanktonMono.fullscreenBox = false;
                var result = Utils.Jsoner.FromJson(json, new PurchaseResult());
                var status = ConvertPurchaseStatus(result.status);
                callback?.Invoke(status, result);
            };

            try
            {
#if UNITY_IOS
                proxyBilling.Purchase(sku);
#elif MYKET
                proxyBilling.Purchase(sku, androidObfuscatedAccountId + androidObfuscatedProfiledId);
#elif BAZAAR
                proxyBilling.Purchase(sku, androidObfuscatedAccountId + androidObfuscatedProfiledId);
#elif UNITY_ANDROID
                proxyBilling.Purchase(sku, androidAutoAck, androidAutoConsume, androidObfuscatedAccountId, androidObfuscatedProfiledId);
#endif

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() =>
                {
                    PlanktonMono.fullscreenBox = false;
                    callback?.Invoke(PurchaseStatus.Purchased, new PurchaseResult() { status = "purchased", token = $"fake_token_{sku}" });
                });
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                PlanktonMono.fullscreenBox = false;
                callback?.Invoke(PurchaseStatus.Failed, new PurchaseResult() { status = "failed" });
            }
        }

        public static void FinishPurchase(string tokenOrTransactionId, System.Action<bool, string> callback, bool isAndroidConsumable = true)
        {
            if (IsNotInitialized)
            {
                callback?.Invoke(false, string.Empty);
                return;
            }

#if UNITY_IOS
            Debug.Log($"{logName} ConsumeOrAcknowledgeOrFinishTransaction token:{tokenOrTransactionId}");
#elif MYKET
            Debug.Log($"{logName} ConsumeOrAcknowledgeOrFinishTransaction token:{tokenOrTransactionId}");
#elif BAZAAR
            Debug.Log($"{logName} ConsumeOrAcknowledgeOrFinishTransaction token:{tokenOrTransactionId}");
#elif UNITY_ANDROID
            Debug.Log($"{logName} ConsumeOrAcknowledgeOrFinishTransaction isAndroidConsumable:{isAndroidConsumable} token:{tokenOrTransactionId}");
#endif

            PlanktonMono.onConsumeOrAcknowledgeResult = json =>
            {
                var result = Utils.Jsoner.FromJson(json, new ConsumeOrAckResult());
                callback?.Invoke(result.success.ToLower() == "true", result.token);
            };

            try
            {
                if (string.IsNullOrEmpty(tokenOrTransactionId) == false)
                {
#if UNITY_IOS
                    proxyBilling.FinishTransaction(tokenOrTransactionId);
#elif MYKET
                    proxyBilling.Consume(tokenOrTransactionId);
#elif BAZAAR
                    proxyBilling.Consume(tokenOrTransactionId);
#elif UNITY_ANDROID
                    if (isAndroidConsumable)
                        proxyBilling.Consume(tokenOrTransactionId);
                    else
                        proxyBilling.Acknowledge(tokenOrTransactionId);
#endif

#if UNITY_EDITOR
                    PlanktonMono.CallInUnityThread(() => callback?.Invoke(true, tokenOrTransactionId));
#endif
                }
                else callback?.Invoke(false, string.Empty);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void GetSkuDetails(System.Action<bool, List<Detail>> callback, params string[] skus)
        {
            if (IsNotInitialized)
            {
                callback?.Invoke(false, new List<Detail>());
                return;
            }

            var allSkus = string.Join(",", skus);
            Debug.Log($"{logName} GetSkuDetails {allSkus}");

            PlanktonMono.onSkuDetailsResult = json =>
            {
                var result = Utils.Jsoner.FromJson(json, new SkuDetailsResult());
                callback?.Invoke(result.success.ToLower() == "true", result.result);
            };

            try
            {
                proxyBilling.GetSkuDetails(allSkus);

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(false, new List<Detail>()));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(false, new List<Detail>());
            }
        }

        public static void GetPurchases(System.Action<bool, List<History>> callback)
        {
            if (IsNotInitialized)
            {
                callback?.Invoke(false, new List<History>());
                return;
            }
            Debug.Log($"{logName} GetPurchases");

            PlanktonMono.onPurchaseHistoryResult = json =>
            {
                var historyResult = Utils.Jsoner.FromJson(json, new HistoryResult());
                if (historyResult.success.ToLower() == "true")
                {
                    var list = new List<History>();
                    foreach (var item in historyResult.result)
                        list.Add(ConvertHistory(item));
                    callback?.Invoke(true, list);
                }
                else callback?.Invoke(false, new List<History>());
            };

            try
            {
                proxyBilling.GetPurchases();

#if UNITY_EDITOR
                PlanktonMono.CallInUnityThread(() => callback?.Invoke(false, new List<History>()));
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                callback?.Invoke(false, new List<History>());
            }
        }

#if UNITY_IOS
        public static void RestorePurchases(System.Action<RestorePurchaseResult> onSucceed)
        {
            if (IsNotInitialized) return;
            Debug.Log($"{logName} RestorePurchases");

            PlanktonMono.onRestorePurchasesSuccess = json =>
            {
                var result = Utils.Jsoner.FromJson(json, new RestorePurchaseResult());
                onSucceed?.Invoke(result);
            };

            try
            {
                proxyBilling.RestorePurchases();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
#endif

        private static PurchaseStatus ConvertPurchaseStatus(string status)
        {
            switch (status.ToLower())
            {
                case "purchased": return PurchaseStatus.Purchased;
                case "pending": return PurchaseStatus.Pending;
                case "acked": return PurchaseStatus.Acked;
                default: return PurchaseStatus.Failed;
            }
        }

        private static History ConvertHistory(HistoryResult.Item item)
        {
            return new History()
            {
                status = ConvertPurchaseStatus(item.status),
                sku = item.sku,
                token = item.token,
                payload = item.payload,
                obfuscated_account_id = item.obfuscated_account_id,
                obfuscated_profile_id = item.obfuscated_profile_id,
                androidSignature = item.androidSignature
            };
        }

        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public enum PurchaseStatus
        {
            None,
            Purchased,
            Pending,
            Failed,
            Acked
        }

        [System.Serializable]
        private class InitializationResult
        {
            public string success = string.Empty;
        }

        [System.Serializable]
        public class PurchaseResult
        {
            public string status = string.Empty;
            public string token = string.Empty;
            public string obfuscated_profile_id = string.Empty;
            public string obfuscated_account_id = string.Empty;
            public string payload = string.Empty;
            public string androidSignature = string.Empty;
            public string iosReceipt = string.Empty;
            public string failureReason = string.Empty;
            public string errorCode = string.Empty;
        }

        [System.Serializable]
        public class RestorePurchaseResult
        {
            public string sku = string.Empty;
            public string token = string.Empty;
            public string iosReceipt = string.Empty;
        }

        [System.Serializable]
        private class ConsumeOrAckResult
        {
            public string success = string.Empty;
            public string token = string.Empty;
        }

        [System.Serializable]
        private class SkuDetailsResult
        {
            public string success = string.Empty;
            public List<Detail> result = new List<Detail>();
        }

        [System.Serializable]
        private class HistoryResult
        {
            public string success = string.Empty;
            public List<Item> result = new List<Item>();

            [System.Serializable]
            public class Item
            {
                public string sku = string.Empty;
                public string token = string.Empty;
                public string status = string.Empty;
                public string payload = string.Empty;
                public string obfuscated_account_id = string.Empty;
                public string obfuscated_profile_id = string.Empty;
                public string androidSignature = string.Empty;
            }
        }

        [System.Serializable]
        public class History
        {
            public PurchaseStatus status = PurchaseStatus.None;
            public string sku = string.Empty;
            public string token = string.Empty;
            public string payload = string.Empty;
            public string obfuscated_account_id = string.Empty;
            public string obfuscated_profile_id = string.Empty;
            public string androidSignature = string.Empty;
        }

        [System.Serializable]
        public class Detail
        {
            public string sku = string.Empty;
            public string title = string.Empty;
            public string description = string.Empty;
            public string priceFormatted = string.Empty;
            public string priceCurrency = string.Empty;
            public float priceAmount = 0;
        }
    }
}