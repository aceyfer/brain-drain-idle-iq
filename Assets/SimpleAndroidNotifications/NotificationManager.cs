using System;
using UnityEngine;

namespace Assets.SimpleAndroidNotifications
{
    public static class NotificationManager
    {
        /// <summary>
        /// Should be the same as declared in AndroidManifest.xml!
        /// </summary>
        public const string UnityActivityClassName = "com.unity3d.player.UnityPlayerActivity";

        private static AndroidJavaClass JavaClass => new("com.hippogames.simpleandroidnotifications.Controller");

        /// <summary>
        /// Schedule simple notification without app icon.
        /// </summary>
        public static int Send(TimeSpan delay, string title, string message, Color smallIconColor, NotificationIcon smallIcon = 0)
        {
            return SendCustom(new NotificationParams
            {
                Id = UnityEngine.Random.Range(0, int.MaxValue),
                Delay = delay,
                Title = title,
                Message = message,
                Ticker = message,
                Sound = true,
                Vibrate = true,
                Light = true,
                SmallIcon = smallIcon,
                SmallIconColor = smallIconColor,
                LargeIcon = ""
            });
        }

        /// <summary>
        /// Schedule notification with app icon.
        /// </summary>
        public static int SendWithAppIcon(TimeSpan delay, string title, string message, Color smallIconColor, NotificationIcon smallIcon = 0)
        {
            return SendCustom(new NotificationParams
            {
                Id = UnityEngine.Random.Range(0, int.MaxValue),
                Delay = delay,
                Title = title,
                Message = message,
                Ticker = message,
                Sound = true,
                Vibrate = true,
                Light = true,
                SmallIcon = smallIcon,
                SmallIconColor = smallIconColor,
                LargeIcon = "app_icon"
            });
        }

        /// <summary>
        /// Schedule customizable notification.
        /// </summary>
        public static int SendCustom(NotificationParams notificationParams)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            if (!PermissionManager.HasPostNotifications())
            {
                Debug.LogWarning("Notifications disallowed.");
                //PermissionManager.OpenNotificationSettings();
                return 0;
            }

            var p = notificationParams;
            var delay = (long) p.Delay.TotalMilliseconds;
			
            JavaClass.CallStatic("SetNotification", p.Id, delay, p.Title, p.Message, p.Ticker,
                p.Sound ? 1 : 0, p.Vibrate ? 1 : 0, p.Light ? 1 : 0, p.LargeIcon, GetSmallIconName(p.SmallIcon), ColorToInt(p.SmallIconColor), UnityActivityClassName);

            #else

            Debug.LogWarning("Simple Android Notifications are not supported for current platform. Build and play this scene on android device!");

            #endif

            return notificationParams.Id;
        }

        /// <summary>
        /// Cancel notification by id.
        /// </summary>
        public static void Cancel(int id)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            JavaClass.CallStatic("CancelScheduledNotification", id);

            #endif
        }

        /// <summary>
        /// Cancel all notifications.
        /// </summary>
        public static void CancelAll()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR

            JavaClass.CallStatic("CancelAllScheduledNotifications");

            #endif
        }

        private static int ColorToInt(Color color)
        {
            var smallIconColor = (Color32) color;
            
            return smallIconColor.r * 65536 + smallIconColor.g * 256 + smallIconColor.b;
        }

        private static string GetSmallIconName(NotificationIcon icon)
        {
            return "anp_" + icon.ToString().ToLower();
        }
    }
}