using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public class PushNotification
    {
        private const string logName = "[PushNotification]";

        private static readonly ProxyPushNotification proxyPushNotification = new ProxyPushNotification();

        public static void GetToken(System.Action<string> callback)
        {
            PlanktonMono.onFcmTokenReceived = json =>
            {
                try
                {
                    Debug.Log($"{logName} FcmNewToken: {json}");
                    var data = JsonUtility.FromJson<Token>(json);
                    callback?.Invoke(data.token);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            proxyPushNotification.GetToken();

#if UNITY_EDITOR
            PlanktonMono.CallInUnityThread(() => callback?.Invoke("fake_token"));
#endif
        }

        public static void SetMessageReceivedCallback(System.Action<Message> callback)
        {
            PlanktonMono.onFcmMessageReceived = json =>
            {
                try
                {
                    Debug.Log($"{logName} FcmMessageReceived: {json}");
                    var message = JsonUtility.FromJson<Message>(json);
                    callback?.Invoke(message);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        [System.Serializable]
        public class Message
        {
            public string title = string.Empty;
            public string body = string.Empty;
            public override string ToString() => $"title:{title} body:{body}";
        }

        [System.Serializable]
        public class Token
        {
            public string success = string.Empty;
            public string token = string.Empty;
        }
    }
}