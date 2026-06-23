#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyBilling : Proxy
    {
        private const string logName = "[Plankton] [Billing]";

        public ProxyBilling(string marketName, string rsaKey = "")
        {
            Debug.Log($"{logName} Initializing market:{marketName}");
            Utils.Jsoner.AddParams("storeName", marketName, "storeKey", rsaKey);
            var json = Utils.Jsoner.GetJsonAndClear();
            Debug.Log($"{logName} Called initialization {json}");
            FreeVersion.NotAvailable();
        }

        public void Purchase(string sku, string payload)
        {
            Debug.Log($"{logName} Purchase {sku} Payload {payload}");
            FreeVersion.NotAvailable();
        }

        public void Purchase(string sku, bool autoAck = false, bool autoConsume = false, string obfuscatedAccountId = "", string obfuscatedProfiledId = "")
        {
            Debug.Log($"{logName} Purchase sku:{sku} autoAck:{autoAck} autoConsume:{autoConsume} obfuscatedAccountId:{obfuscatedAccountId} obfuscatedProfiledId:{obfuscatedProfiledId}");
            FreeVersion.NotAvailable();
        }

        public void Acknowledge(string token)
        {
            Debug.Log($"{logName} AcknowledgePurchase token:{token}");
            FreeVersion.NotAvailable();
        }

        public void Consume(string token)
        {
            Debug.Log($"{logName} Consume token:{token}");
            FreeVersion.NotAvailable();
        }

        public void GetSkuDetails(string camaSeparatedSkus)
        {
            Debug.Log($"{logName} GetSkuDetails {camaSeparatedSkus}");
            FreeVersion.NotAvailable();
        }

        public void GetPurchases()
        {
            Debug.Log($"{logName} GetPurchases");
            FreeVersion.NotAvailable();
        }
    }
}
#endif