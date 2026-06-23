using UnityEngine;
using UnityEngine.Android;

namespace Assets.SimpleAndroidNotifications.Helpers
{
    public static class PermissionManager
    {
        public static int GetApiLevel()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            return new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");

            #else

            return 0;

            #endif
        }

        public static bool HasPostNotifications()
        {
            return GetApiLevel() < 33 || Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS");
        }

        public static void RequestPostNotifications()
        {
            if (GetApiLevel() < 33) return;

            const string key = "NotificationManager.PermissionRequests";

            var attempts = PlayerPrefs.GetInt(key);

            if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS") && PlayerPrefs.GetInt(key) < 2)
            {
                PlayerPrefs.SetInt(key, ++attempts);
                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
        }
        
        public static bool RequestScheduleExactAlarm()
        {
            const string key = "NotificationManager.SCHEDULE_EXACT_ALARM";

            var attempts = PlayerPrefs.GetInt(key);

            if (Permission.HasUserAuthorizedPermission("android.permission.SCHEDULE_EXACT_ALARM")) return true;

            if (PlayerPrefs.GetInt(key) < 2)
            {
                PlayerPrefs.SetInt(key, ++attempts);
                Permission.RequestUserPermission("android.permission.SCHEDULE_EXACT_ALARM");
            }

            return false;
        }

        public static void OpenNotificationSettings()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            var packageName = currentActivityObject.Call<string>("getPackageName");
            using var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.APP_NOTIFICATION_SETTINGS");

            intentObject.Call<AndroidJavaObject>("putExtra", "android.provider.extra.APP_PACKAGE", packageName);
            currentActivityObject.Call("startActivity", intentObject);

            #endif
        }
    }
}