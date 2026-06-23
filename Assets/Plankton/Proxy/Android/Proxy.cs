#if UNITY_ANDROID
using UnityEngine;

namespace Plankton.Proxy
{
    public class Proxy
    {
        private const string Version = "3.8.0";
        private const string logName = "[Plankton]";

        private static AndroidJavaObject objectPlankton = null;

        protected AndroidJavaObject GetAndroidObject(string methodName)
        {
            AndroidJavaObject result = null;
            try
            {
                if (objectPlankton == null)
                {
                    Debug.Log($"{logName} Initializing Plankton Version {Version}");
                    objectPlankton = new AndroidJavaObject("com.databite.plankton.Plankton");
                    Debug.Log($"{logName} Initialized Plankton.");
                }
                result = objectPlankton.Call<AndroidJavaObject>(methodName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"{logName} Error! Can't find field name {objectPlankton}->{methodName}");
                Debug.LogException(e);
            }
            return result;
        }
    }
}
#endif