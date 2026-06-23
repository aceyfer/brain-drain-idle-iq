using UnityEngine;

namespace Plankton
{
    public static class FreeVersion
    {
        public static void NotAvailable(string feature = "This feature")
        {
            Debug.LogWarning($"{feature} is not available in the Free version. Upgrade to the full version of Plankton from: https://tinyurl.com/assetstore-plankton");
        }
    }
}