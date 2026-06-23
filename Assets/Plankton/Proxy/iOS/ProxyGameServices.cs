#if UNITY_IOS
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGameServices : Proxy
    {
        private const string logName = "[Plankton] [GameCenter]";


        public void Initialize()
        {
            Debug.Log($"{logName} calling Initialize");
            FreeVersion.NotAvailable();
        }

        public void SignIn()
        {
            Debug.Log($"{logName} calling SignIn");
            FreeVersion.NotAvailable();
        }

        public void GetPlayerInfo()
        {
            Debug.Log($"{logName} calling GetPlayerInfo");
            FreeVersion.NotAvailable();
        }

        public void GetServerSideParams()
        {
            Debug.Log($"{logName} calling GetServerSideParams");
            FreeVersion.NotAvailable();
        }

        public void Load(string fileName)
        {
            Debug.Log($"{logName} Load fileName:{fileName}");
            FreeVersion.NotAvailable();
        }

        public void Save(string fileName, string data, string description)
        {
            Debug.Log($"{logName} Save fileName:{fileName} data:{data}");
            FreeVersion.NotAvailable();
        }
    }
}

#endif