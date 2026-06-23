#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGameServices : Proxy
    {
        private const string logName = "[Plankton] [PlayServices]";

        public void Initialize()
        {
            Debug.Log($"{logName} calling Initialize");
            FreeVersion.NotAvailable(logName);
        }

        public void SignIn()
        {
            Debug.Log($"{logName} calling SignIn");
            FreeVersion.NotAvailable(logName);
        }

        public void GetPlayerInfo()
        {
            Debug.Log($"{logName} calling GetPlayerInfo");
            FreeVersion.NotAvailable(logName);
        }
        
        public void GetServerSideParams()
        {
            Debug.Log($"{logName} calling GetServerSideParams");
            FreeVersion.NotAvailable(logName);
        }

        public void Load(string filename)
        {
            Debug.Log($"{logName} calling Load filename:{filename}");
            FreeVersion.NotAvailable(logName);
        }

        public void Save(string filename, string data, string description)
        {
            Debug.Log($"{logName} calling Save filename:{filename} data:{data} description:{description}");
            FreeVersion.NotAvailable(logName);
        }
    }
}
#endif