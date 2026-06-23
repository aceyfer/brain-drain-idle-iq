using System;
using System.Collections.Generic;
using Plankton.Proxy;
using UnityEngine;

namespace Plankton
{
    public class Ad
    {
        private const string logName = "[Ad]";

        private static ProxyAd proxyAd = null;

        public static bool IsInitialized { get; private set; } = false;
        public static bool IsOnline => proxyAd == null || proxyAd.IsOnline();

        public static event Action<AdActionResult> OnAdAction = null;
        public static event Action<AdMobImpressionRevenue> OnAdMobImpressionRevenue = null;
        public static event Action<AppLovinMaxImpressionRevenue> OnAppLovinMaxImpressionRevenue = null;
        public static event Action<LevelPlayImpressionRevenue> OnLevelPlayImpressionRevenue = null;

        private static bool IsNotInitialized
        {
            get
            {
                if (proxyAd == null)
                    Debug.Log($"{logName} Feature needs to be initialized first!");
                return proxyAd == null;
            }
        }

        public static void Initialize(Builder builder, Action callback)
        {
            if (proxyAd != null)
            {
                Debug.Log($"{logName} Feature already initialized!");
                return;
            }

            PlanktonMono.onAdInitialized = () =>
            {
                IsInitialized = true;
                callback?.Invoke();
            };

            PlanktonMono.OnAdActionEvent += json =>
            {
                try
                {
                    var result = Utils.Jsoner.FromJson(json, new AdActionResult());
                    OnAdAction?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            PlanktonMono.OnAdMobImpressionRevenueEvent += json =>
            {
                try
                {
                    var result = JsonUtility.FromJson<AdMobImpressionRevenue>(json);
                    OnAdMobImpressionRevenue?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            PlanktonMono.OnAppLovinMaxImpressionRevenueEvent += json =>
            {
                try
                {
                    var result = JsonUtility.FromJson<AppLovinMaxImpressionRevenue>(json);
                    OnAppLovinMaxImpressionRevenue?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            PlanktonMono.OnLevelPlayImpressionRevenueEvent += json =>
            {
                try
                {
                    var result = JsonUtility.FromJson<LevelPlayImpressionRevenue>(json);
                    OnLevelPlayImpressionRevenue?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };

            proxyAd = new ProxyAd(builder.ToString());

#if UNITY_EDITOR
            PlanktonMono.CallInUnityThread(PlanktonMono.onAdInitialized);
#endif
        }


        public static class Banner
        {
            public static bool blackBackground = true;

            /// <summary>
            /// Request to show banner at the top of the screen.
            /// </summary>
            /// <param name="placement">The placement would be used in analytics</param>
            public static void ShowAtTop(string placement = "top")
            {
                if (IsNotInitialized) return;
                proxyAd?.Show(Type.BannerAtTop, placement);
                PlanktonMono.bannerBox = blackBackground ? 2 : 0;
            }

            /// <summary>
            /// Request to show banner at the bottom of the screen.
            /// </summary>
            /// <param name="placement">The placement would be used in analytics</param>
            public static void ShowAtBottom(string placement = "bottom")
            {
                if (IsNotInitialized) return;
                proxyAd?.Show(Type.BannerAtBottom, placement);
                PlanktonMono.bannerBox = blackBackground ? 1 : 0;
            }

            /// <summary>
            /// Hide any banner from the screen
            /// </summary>
            public static void Hide()
            {
                proxyAd?.Hide("banner");
                PlanktonMono.bannerBox = 0;
            }

            public static void SetBlackBackground(bool enabled)
            {
                blackBackground = enabled;
            }
        }

        public static class Rewarded
        {
            public static bool blackBackground = false;

            /// <summary>
            /// True if Rewarded Ad has been loaded and ready to show
            /// </summary>
            public static bool IsLoaded =>
#if UNITY_EDITOR
                proxyAd != null;
#else
                IsInitialized && proxyAd.IsReady(Type.Rewarded);
#endif

            /// <summary>
            /// try to show rewarded ad.
            /// </summary>
            /// <param name="onResult">Result callback (displayed, rewarded)</param>
            /// <param name="placement">The placement would be used in analytics</param>
            /// <param name="adMobSsvCustomData">(optional) Extra data passed to the `custom_data` query parameter of the SSV callback</param>
            public static void Show(Action<bool, bool> onResult = null, string placement = "rewarded", string adMobSsvCustomData = "")
            {
                if (IsNotInitialized)
                {
                    onResult?.Invoke(false, false);
                    return;
                }

                var timescale = Time.timeScale;
                PlanktonMono.onAdClosed = json =>
                {
                    Time.timeScale = timescale;
                    AudioListenerSetPause(false);
                    PlanktonMono.fullscreenBox = false;

                    var result = Utils.Jsoner.FromJson(json, new AdClosedResult());
                    onResult?.Invoke(result.displayed.ToLower() == "true", result.reward_earned.ToLower() == "true");
                };

                if (adMobSsvCustomData != string.Empty)
                {
                    proxyAd?.SetAdMobSsvCustomData(Type.Rewarded, adMobSsvCustomData);
                }

                Time.timeScale = 0;
                AudioListenerSetPause(true);
                PlanktonMono.fullscreenBox = blackBackground;
                proxyAd?.Show(Type.Rewarded, placement);

#if UNITY_EDITOR
                PlanktonMono.editorFullscreenAdCaption = $"{Type.Rewarded}\n{placement}";
                PlanktonMono.editorFullscreenAdTimer = 3;
#endif
            }

            public static void SetLevelPlayServerParameters(Dictionary<string, string> parameters, bool clearPreviousParams = false)
            {
                if (IsNotInitialized) return;

                Utils.Jsoner.Add(parameters);
                proxyAd?.SetLevelPlayRewardedServerParameters(Utils.Jsoner.GetJsonAndClear(), clearPreviousParams);
            }
        }
    

        public static class Interstitial
        {
            public static bool blackBackground = false;

            /// <summary>
            /// True if Interstitial Ad has been loaded and ready to show
            /// </summary>
            public static bool IsLoaded =>
#if UNITY_EDITOR
                proxyAd != null;
#else
                IsInitialized && proxyAd.IsReady(Type.Interstitial);
#endif

            /// <summary>
            /// try to show Interstitial ad.
            /// </summary>
            /// <param name="onResult">Result callback (displayed)</param>
            /// <param name="placement">The placement would be used in analytics</param>
            public static void Show(Action<bool> onResult = null, string placement = "interstitial")
            {
                if (IsNotInitialized)
                {
                    onResult?.Invoke(false);
                    return;
                }

                var timescale = Time.timeScale;
                PlanktonMono.onAdClosed = json =>
                {
                    Time.timeScale = timescale;
                    AudioListenerSetPause(false);
                    PlanktonMono.fullscreenBox = false;

                    var result = Utils.Jsoner.FromJson(json, new AdClosedResult());
                    onResult?.Invoke(result.displayed.ToLower() == "true");
                };

                Time.timeScale = 0;
                AudioListenerSetPause(true);
                PlanktonMono.fullscreenBox = blackBackground;
                proxyAd?.Show(Type.Interstitial, placement);

#if UNITY_EDITOR
                PlanktonMono.editorFullscreenAdCaption = $"{Type.Interstitial}\n{placement}";
                PlanktonMono.editorFullscreenAdTimer = 3;
#endif
            }
        }

        public static class RewardedInterstitial
        {
            public static bool blackBackground = false;

            /// <summary>
            /// True if RewardedInterstitial Ad has been loaded and ready to show
            /// </summary>
            public static bool IsLoaded =>
#if UNITY_EDITOR
                proxyAd != null;
#else
                IsInitialized && proxyAd.IsReady(Type.RewardedInterstitial);
#endif

            /// <summary>
            /// try to show RewardedInterstitial ad.
            /// </summary>
            /// <param name="onResult">Result callback (displayed, rewarded)</param>
            /// <param name="placement">The placement would be used in analytics</param>
            /// <param name="adMobSsvCustomData">(optional) Extra data passed to the `custom_data` query parameter of the SSV callback</param>
            public static void Show(Action<bool, bool> onResult = null, string placement = "rewarded interstitial", string adMobSsvCustomData = "")
            {
                if (IsNotInitialized)
                {
                    onResult?.Invoke(false, false);
                    return;
                }

                var timescale = Time.timeScale;
                PlanktonMono.onAdClosed = json =>
                {
                    Time.timeScale = timescale;
                    AudioListenerSetPause(false);
                    PlanktonMono.fullscreenBox = false;

                    var result = Utils.Jsoner.FromJson(json, new AdClosedResult());
                    onResult?.Invoke(result.displayed.ToLower() == "true", result.reward_earned.ToLower() == "true");
                };

                if (adMobSsvCustomData != string.Empty)
                {
                    proxyAd?.SetAdMobSsvCustomData(Type.RewardedInterstitial, adMobSsvCustomData);
                }

                Time.timeScale = 0;
                AudioListenerSetPause(true);
                PlanktonMono.fullscreenBox = blackBackground;
                proxyAd?.Show(Type.RewardedInterstitial, placement);

#if UNITY_EDITOR
                PlanktonMono.editorFullscreenAdCaption = $"{Type.RewardedInterstitial}\n{placement}";
                PlanktonMono.editorFullscreenAdTimer = 3;
#endif
            }
        }

        public static class AppOpen
        {
            public static bool blackBackground = false;

            /// <summary>
            /// True if Interstitial Ad has been loaded and ready to show
            /// </summary>
            public static bool IsLoaded =>
#if UNITY_EDITOR
                proxyAd != null;
#else
                IsInitialized && proxyAd.IsReady(Type.AppOpen);
#endif

            /// <summary>
            /// try to show AppOpen ad.
            /// </summary>
            /// <param name="onResult">Result callback (displayed)</param>
            /// <param name="placement">The placement would be used in analytics</param>
            public static void Show(Action<bool> onResult = null, string placement = "app_open")
            {
                if (IsNotInitialized)
                {
                    onResult?.Invoke(false);
                    return;
                }

                PlanktonMono.onAdClosed = json =>
                {
                    AudioListenerSetPause(false);
                    PlanktonMono.fullscreenBox = false;

                    var result = Utils.Jsoner.FromJson(json, new AdClosedResult());
                    onResult?.Invoke(result.displayed.ToLower() == "true");
                };

                AudioListenerSetPause(true);
                PlanktonMono.fullscreenBox = blackBackground;
                proxyAd?.Show(Type.AppOpen, placement);

#if UNITY_EDITOR
                PlanktonMono.editorFullscreenAdCaption = $"{Type.AppOpen}\n{placement}";
                PlanktonMono.editorFullscreenAdTimer = 3;
#endif
            }
        }

        private static void AudioListenerSetPause(bool value)
        {
            AudioListener.pause = value;
        }

        public static void OpenAdMobDebugger()
        {
            if (IsNotInitialized) return;
            proxyAd.TestAdMob();
        }

        public static void OpenAppLovinMaxDebugger()
        {
            if (IsNotInitialized) return;
            proxyAd.TestMax();
        }

        public static void OpenLevelPlayDebugger()
        {
            if (IsNotInitialized) return;
            proxyAd.TestLevelPlay();
        }

        public static string GetAndroidAdId()
        {
#if UNITY_ANDROID
            if (IsNotInitialized) return string.Empty;
            return proxyAd.GetAndroidAdId();
#else
            return string.Empty;
#endif
        }

        private static string GetProviderName(Provider provider)
        {
            switch (provider)
            {
                case Provider.Admob: return "admob";
                case Provider.AppLovin: return "applovin";
                case Provider.LevelPlay: return "levelplay";
#if TAPSELL
                case Provider.Tapsell: return "tapsellplus";
#endif
                default: return string.Empty;
            }
        }

        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        public enum Provider
        {
            Admob,
            AppLovin,
            LevelPlay,
#if TAPSELL
            Tapsell
#endif
        }

        private static class Type
        {
            public const string Interstitial = "interstitial";
            public const string Rewarded = "rewarded";
            public const string RewardedInterstitial = "rewarded_interstitial";
            public const string BannerAtTop = "banner_top";
            public const string BannerAtBottom = "banner_bottom";
            public const string AppOpen = "app_open";
        }

        [Serializable]
        private class AdClosedResult
        {
            public string displayed;
            public string reward_earned;
        }

        public class AdActionResult
        {
            public string type;
            public string action;
            public string provider;
            public string placement;
        }

        [Serializable]
        public class AdMobImpressionRevenue
        {
            public string adUnitId;
            public string adType;
            public string currencyCode;
            public AdMobImpressionPrecision precision;
            public string networkName;
            public long revenue;
            public string adMobSdkVersion;
            public string responseId;
        }

        [Serializable]
        public class AppLovinMaxImpressionRevenue
        {
            public string adUnitId;
            public string adType;
            public string networkName;
            public string country;
            public string placement;
            public string creativeId;
            public double revenue;
            public string appLovinSdkVersion;
            public string precision;
        }

        [Serializable]
        public class LevelPlayImpressionRevenue
        {
            public string adUnitId;
            public string adType;
            public string networkName;
            public string country;
            public string placement;
            public string creativeId;
            public double revenue;
            public string precision;
            public string allData;
            public string ironSourceSdkVersion;
        }

        public class Builder
        {
            private Provider bannerProvider = Provider.Admob;
            private string bannerZoneId = string.Empty;
            private Provider interstitialProvider = Provider.Admob;
            private string interstitialZoneId = string.Empty;
            private Provider rewardedProvider = Provider.Admob;
            private string rewardedZoneId = string.Empty;
            private Provider rewardedInterstitialProvider = Provider.Admob;
            private string rewardedInterstitialZoneId = string.Empty;
            private Provider appOpenProvider = Provider.Admob;
            private string appOpenZoneId = string.Empty;
            private string admobTestDeviceIds = string.Empty;
            private bool adaptiveBanner = true;

            public Builder SetBanner(Provider provider, string zoneId, bool adaptive = true)
            {
                bannerProvider = provider;
                bannerZoneId = zoneId;
                adaptiveBanner = adaptive;
                return this;
            }

            public Builder SetInterstitial(Provider provider, string zoneId)
            {
                interstitialProvider = provider;
                interstitialZoneId = zoneId;
                return this;
            }

            public Builder SetRewarded(Provider provider, string zoneId)
            {
                rewardedProvider = provider;
                rewardedZoneId = zoneId;
                return this;
            }

            public Builder SetRewardedInterstitial(Provider provider, string zoneId)
            {
                rewardedInterstitialProvider = provider;
                rewardedInterstitialZoneId = zoneId;
                return this;
            }

            public Builder SetAppOpen(Provider provider, string zoneId)
            {
                appOpenProvider = provider;
                appOpenZoneId = zoneId;
                return this;
            }

            public Builder SetAdmobTestDeviceIds(string deviceIds)
            {
                admobTestDeviceIds = deviceIds;
                return this;
            }

            public override string ToString()
            {
                Utils.Jsoner.AddParams("admobTestDeviceIds", admobTestDeviceIds);
                var extras = Utils.Jsoner.GetJsonAndClear();

                Utils.Jsoner.AddParams(
                    "adaptiveBanner", adaptiveBanner,
                    "bannerProvider", GetProviderName(bannerProvider),
                    "bannerZoneId", bannerZoneId,

                    "interstitialProvider", GetProviderName(interstitialProvider),
                    "interstitialZoneId", interstitialZoneId,
                    
                    "rewardedProvider", GetProviderName(rewardedProvider),
                    "rewardedZoneId", rewardedZoneId,
                    
                    "rewardedInterstitialProvider", GetProviderName(rewardedInterstitialProvider),
                    "rewardedInterstitialZoneId", rewardedInterstitialZoneId,

                    "appOpenProvider", GetProviderName(appOpenProvider),
                    "appOpenZoneId", appOpenZoneId
                    );

                Utils.Jsoner.AddJson("extras", extras);
                return Utils.Jsoner.GetJsonAndClear();
            }
        }
    }
}