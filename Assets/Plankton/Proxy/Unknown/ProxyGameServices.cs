#if !UNITY_IOS && !UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class ProxyGameServices : Proxy
    {
        private const string logName = "[Plankton] [PlayServices]";

        public void Initialize()
        {
            Debug.Log($"{logName} calling Initialize");
        }

        public void SignIn()
        {
            Debug.Log($"{logName} calling SignIn");
        }

        public void GetPlayerInfo()
        {
            Debug.Log($"{logName} calling GetPlayerInfo");
        }

        public void GetServerSideParams()
        {
            Debug.Log($"{logName} calling GetServerSideParams");
        }

        public void Load(string filename)
        {
            Debug.Log($"{logName} calling Load filename:{filename}");
        }

        public void Save(string filename, string data, string description)
        {
            Debug.Log($"{logName} calling Save filename:{filename} data:{data} description:{description}");
        }
    }
}
#endif