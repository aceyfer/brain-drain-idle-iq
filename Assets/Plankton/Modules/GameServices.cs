using System;
using UnityEngine;
using Plankton.Proxy;

namespace Plankton
{
    public static class GameServices
    {
        private const string logName = "[Plankton] [GameServices]";
        private static ProxyGameServices proxyGameServices = new ProxyGameServices();

        public static bool IsSignedIn { get; private set; } = false;

        public static void Initialize(Action<bool> onSignInCallback)
        {
            Debug.Log($"{logName} Initialize game services!");
            PlanktonMono.onGameServiceSignInResult = succeed =>
            {
                IsSignedIn = succeed;
                onSignInCallback?.Invoke(succeed);
            };
            proxyGameServices.Initialize();
        }

        public static void SignIn()
        {
            Debug.Log($"{logName} Signing in play services!");
            proxyGameServices.SignIn();
#if UNITY_EDITOR
            PlanktonMono.CallInUnityThread(() => PlanktonMono.onGameServiceSignInResult?.Invoke(false));
#endif
        }

        public static void GetPlayerInfo(Action<PlayerInfo> callback)
        {
            Debug.Log($"{logName} GetPlayerInfo {callback}");
            PlanktonMono.onGameServicePlayerInfoResult = json =>
            {
                try
                {
                    Debug.Log($"{logName} GetPlayerInfo result: {json}");
                    var result = JsonUtility.FromJson<PlayerInfo>(json);
                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(new PlayerInfo());
                }
            };
            proxyGameServices.GetPlayerInfo();
#if UNITY_EDITOR
            PlanktonMono.CallInUnityThread(() => callback?.Invoke(new PlayerInfo()));
#endif
        }

        public static void GetServerSideParams(Action<ServerSideParams> callback)
        {
            Debug.Log($"{logName} GetServerSideParams {callback}");
            PlanktonMono.onGameServiceServerSideResult = json =>
            {
                try
                {
                    Debug.Log($"{logName} GetServerSideParams result: {json}");
                    var result = JsonUtility.FromJson<ServerSideParams>(json);
                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(new ServerSideParams());
                }
            };
            proxyGameServices.GetServerSideParams();
#if UNITY_EDITOR
            PlanktonMono.CallInUnityThread(() => callback?.Invoke(new ServerSideParams()));
#endif
        }

        public static void Load(string fileName, Action<string> onCompleted)
        {
            Debug.Log($"{logName} Loading from play services...");

            PlanktonMono.onGameServiceLoadData = json =>
            {
                Debug.Log($"{logName} Data loaded from play services. json: {json}");
                try
                {
                    onCompleted?.Invoke(json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    onCompleted?.Invoke(null);
                }
            };

            if (IsSignedIn)
            {
                proxyGameServices.Load(fileName);
#if UNITY_EDITOR
                onCompleted?.Invoke(string.Empty);
#endif
            }
            else onCompleted?.Invoke(string.Empty);
        }

        public static void Save(string fileName, string data, string description, Action<bool> callback)
        {
            Debug.Log($"{logName} Save data to play services. signedIn = {IsSignedIn}");

            PlanktonMono.onGameServiceSaveResult = success =>
            {
                Debug.Log($"{logName} Save data to play services result = {success}");
                callback?.Invoke(success);
            };

            if (IsSignedIn)
            {
                proxyGameServices.Save(fileName, data, "Automated saved data");
#if UNITY_EDITOR
                callback?.Invoke(true);
#endif
            }
            else callback?.Invoke(false);
        }

        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public class PlayerInfo
        {
            public bool success = false;
            public string playerId = string.Empty;
            public string displayName = string.Empty;
        }

        public class ServerSideParams
        {
            public bool success = false;
            public string androidAuthCode = string.Empty;
            public string iosPublicKeyUrl = string.Empty;
            public string iosSignature = string.Empty;
            public string iosSalt = string.Empty;
            public long iosTimestamp = 0;
        }
    }
}