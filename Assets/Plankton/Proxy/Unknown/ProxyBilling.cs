#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyBilling : Proxy
    {
        private const string logName = "[Plankton] [Billing]";

        public ProxyBilling(string marketName, string rsaKey = "")
        {
            Debug.Log($"{logName} Initializing market:{marketName}");
        }

        public void Purchase(string sku, string payload)
        {
            Debug.Log($"{logName} Purchase {sku} Payload {payload}");
        }

        public void Purchase(string sku, bool autoAck = false, bool autoConsume = false, string obfuscatedAccountId = "", string obfuscatedProfiledId = "")
        {
            Debug.Log($"{logName} Purchase sku:{sku} autoAck:{autoAck} autoConsume:{autoConsume} obfuscatedAccountId:{obfuscatedAccountId} obfuscatedProfiledId:{obfuscatedProfiledId}");
        }

        public void Acknowledge(string token)
        {
            Debug.Log($"{logName} AcknowledgePurchase token:{token}");
        }

        public void Consume(string token)
        {
            Debug.Log($"{logName} Consume token:{token}");
        }

        public void GetSkuDetails(string camaSeparatedSkus)
        {
            Debug.Log($"{logName} GetSkuDetails {camaSeparatedSkus}");
        }

        public void GetPurchases()
        {
            Debug.Log($"{logName} GetPurchases");
        }
    }
}
#endif