#if UNITY_IOS
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyBilling : Proxy
    {
        private const string logName = "[Plankton] [Billing]";

        public ProxyBilling(): base()
        {
            Debug.Log($"{logName} Initializing market:AppStore");
            FreeVersion.NotAvailable();
        }

        public void Purchase(string sku)
        {
            Debug.Log($"{logName} Purchase {sku}");
            FreeVersion.NotAvailable();
        }

        public void FinishTransaction(string transactionId)
        {
            Debug.Log($"{logName} FinishTransaction {transactionId}");
            FreeVersion.NotAvailable();
        }

        public void RestorePurchases()
        {
            Debug.Log($"{logName} RestorePurchases");
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