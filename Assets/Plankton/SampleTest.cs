using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Plankton
{
    public class SampleTest : MonoBehaviour
    {
        private const string logName = "[Test]";

        [SerializeField] private MarketModel marketModel = new MarketModel();
        [SerializeField, Space] private GAModel gameAnalytics = new GAModel();

        private System.Action testModule = null;
        private Vector2 leftScroll, rightScroll = Vector2.zero;

        public static System.Func<System.Action> additionals = null;

        private bool appOpenAdEnabled = false;

        private void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Plankton.PushNotification.SetMessageReceivedCallback(message => Debug.Log($"{logName} New push received: {message}"));
        }

        private void OnGUI()
        {
            GUI.depth = 1;
            GUI.skin.button.fontSize = GUI.skin.label.fontSize = Mathf.RoundToInt(Screen.width * 0.035f);
            GUI.skin.button.contentOffset = Vector2.zero;
            GUI.skin.button.margin = new RectOffset(0, 0, Screen.height / 70, Screen.height / 70);
            GUI.skin.button.padding = new RectOffset(0, 0, Screen.height / 50, Screen.height / 50);
            GUI.skin.verticalScrollbarDownButton.fixedWidth =
                GUI.skin.verticalScrollbarUpButton.fixedWidth =
                GUI.skin.verticalScrollbarThumb.fixedWidth =
                GUI.skin.verticalScrollbar.fixedWidth = Screen.width * 0.03f;


            var rect = new Rect(Screen.safeArea.x, Screen.safeArea.y + Screen.safeArea.height * 0.075f, Screen.safeArea.width * 0.48f, Screen.safeArea.height * (1 - 2 * 0.075f));
            GUILayout.BeginArea(rect);
            leftScroll = GUILayout.BeginScrollView(leftScroll, false, true, null, GUI.skin.verticalScrollbar);
            if (GUILayout.Button("Ad")) testModule = AdTest;
            if (GUILayout.Button("Billing")) testModule = BillingTest;
            if (GUILayout.Button("General Analytics")) testModule = GeneralAnalyticsTest;
            if (GUILayout.Button("Game Analytics")) testModule = GameAnalyticsTest;
            if (GUILayout.Button("Game Services")) testModule = GameServicesTest;
            if (GUILayout.Button("Google Ump")) testModule = GoogleUmpTest;
            if (GUILayout.Button("App Tracking Transparency")) testModule = AppTT;
            if (GUILayout.Button("Utils")) testModule = UtilsTest;
            if (GUILayout.Button("Push Notification")) testModule = PushNotification;

            var additional = additionals?.Invoke();
            if (additional != null) testModule = additional;

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            rect.x += Screen.safeArea.width * 0.5f;
            GUILayout.BeginArea(rect);
            rightScroll = GUILayout.BeginScrollView(rightScroll, false, true, null, GUI.skin.verticalScrollbar);
            testModule?.Invoke();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void AdTest()
        {
            const string module = "[Ad]";

            GUILayout.Label($"{module} is online :{Ad.IsOnline}");
            if (GUILayout.Button("Initialize"))
            {
                var bannerProvider = Ad.Provider.Admob;
                var bannerZoneId = "ca-app-pub-3940256099942544/9214589741";
                var interstitialProvider = Ad.Provider.Admob;
                var interstitialZoneId = "ca-app-pub-3940256099942544/1033173712";
                var rewardedProvider = Ad.Provider.Admob;
                var rewardedZoneId = "ca-app-pub-3940256099942544/5224354917";
                var rewardedInterstitialProvider = Ad.Provider.Admob;
                var rewardedInterstitialZoneId = "ca-app-pub-3940256099942544/5354046379";
                var appOpenProvider = Ad.Provider.Admob;
                var appOpenZoneId = "ca-app-pub-3940256099942544/9257395921";


                var builder = new Ad.Builder()
                    //.SetAdmobTestDeviceIds("test_device_id"); Call this before "Initialize" to enable test mode for AdMob
                    .SetBanner(bannerProvider, bannerZoneId)
                    .SetInterstitial(interstitialProvider, interstitialZoneId)
                    .SetRewarded(rewardedProvider, rewardedZoneId)
                    .SetRewardedInterstitial(rewardedInterstitialProvider, rewardedInterstitialZoneId)
                    .SetAppOpen(appOpenProvider, appOpenZoneId);

                Ad.Initialize(builder, () => Debug.Log($"{module} initialized."));

                // Set revenue listeners for AdMob and AppLovin MAX. LevelPlay will be added soon...
                Ad.OnAdMobImpressionRevenue += result =>
                {
                    if (result != null)
                    {
                        Debug.Log($"{logName} {module} OnAdMobImpressionRevenue: adType={result.adType}, networkName={result.networkName}, adUnitId={result.adUnitId}, currencyCode={result.currencyCode}, precision={result.precision}, revenue={result.revenue}, adMobSdkVersion={result.adMobSdkVersion}, responseId={result.responseId}");
                    }
                };

                Ad.OnAppLovinMaxImpressionRevenue += result =>
                {
                    if (result != null)
                    {
                        Debug.Log($"{logName} {module} OnAppLovinMaxImpressionRevenue: adType={result.adType}, networkName={result.networkName}, adUnitId={result.adUnitId}, country={result.country}, precision={result.precision}, creativeId={result.creativeId}, revenue={result.revenue}, appLovinSdkVersion={result.appLovinSdkVersion}, placement={result.placement}");
                    }
                };

                Ad.OnLevelPlayImpressionRevenue += result =>
                {
                    if (result != null)
                    {
                        Debug.Log($"{logName} {module} OnLevelPlayImpressionRevenue: adType={result.adType}, networkName={result.networkName}, adUnitId={result.adUnitId}, country={result.country}, precision={result.precision}, creativeId={result.creativeId}, revenue={result.revenue}, ironSourceSdkVersion={result.ironSourceSdkVersion}, placement={result.placement}");
                    }
                };
            }

            if (Ad.IsInitialized)
            {
                if (GUILayout.Button($"Show Top Banner"))
                    Ad.Banner.ShowAtTop();

                if (GUILayout.Button($"Show Bottom Banner"))
                    Ad.Banner.ShowAtBottom();

                if (GUILayout.Button($"Hide Banner"))
                    Ad.Banner.Hide();

                if (GUILayout.Button($"Show Interstitial\nis ready: {Ad.Interstitial.IsLoaded}"))
                    Ad.Interstitial.Show((displayed) => Debug.Log($"{logName} {module} Interstitial result displayed:{displayed}"));

                if (GUILayout.Button($"Show Rewarded is\nready: {Ad.Rewarded.IsLoaded}"))
                    Ad.Rewarded.Show((displayed, rewarded) => Debug.Log($"{logName} {module} Rewarded result displayed:{displayed} rewarded:{rewarded}"));

                if (GUILayout.Button($"Show RewardedInterstitial is\nready: {Ad.RewardedInterstitial.IsLoaded}"))
                    Ad.RewardedInterstitial.Show((displayed, rewarded) => Debug.Log($"{logName} {module} RewardedInterstitial result displayed:{displayed} rewarded:{rewarded}"));

                if (GUILayout.Button($"{(appOpenAdEnabled ? "Disable" : "Enable")} AppOpen Ad"))
                {
                    appOpenAdEnabled = !appOpenAdEnabled;
                    Debug.Log($"{logName} {module} AppOpen ad is {(appOpenAdEnabled ? "enabled and will be shown when app returns to foreground.(If it's loaded by then)" : "disabled and won't be shown.")} ");
                }
                
                if (GUILayout.Button("Test AdMob Mediation"))
                    Ad.OpenAdMobDebugger();

                if (GUILayout.Button("Test AppLovin Mediation"))
                    Ad.OpenAppLovinMaxDebugger();

                if (GUILayout.Button("Test LevelPlay Mediation"))
                    Ad.OpenLevelPlayDebugger();

                if (GUILayout.Button("Set LevelPlay SSV Data"))
                    Ad.Rewarded.SetLevelPlayServerParameters(new Dictionary<string, string>() { {"test_key", "test_value"} }, true);

#if UNITY_ANDROID
                if (GUILayout.Button("Get Android Ad ID"))
                {
                    string adId = Ad.GetAndroidAdId();
                    Debug.Log($"{logName} {module} Android Ad ID: {adId}");
                }
#endif
            }
        }

        void OnApplicationPause(bool pause)
        {
            Debug.Log($"OnApplicationPause: {pause}");
            if (!pause && appOpenAdEnabled)
            {
                if (Ad.AppOpen.IsLoaded)
                    Ad.AppOpen.Show();
                else
                    Debug.Log("AppOpen ad is not loaded yet!");
            }
        }

        private void GeneralAnalyticsTest()
        {
            const string module = "[GeneralAnalytics]";

            GUILayout.Label("General Analytics");

            if (GUILayout.Button("Initialize"))
                GeneralAnalytics.Initialize();

            var userPropertyProviders = GeneralAnalytics.Provider.Firebase | GeneralAnalytics.Provider.Yandex;
            if (GUILayout.Button("Set User Property"))
                GeneralAnalytics.SetUserProperty(userPropertyProviders, "tester", "yes");

            var providers = GeneralAnalytics.Provider.Firebase | GeneralAnalytics.Provider.Appsflyer | GeneralAnalytics.Provider.Yandex;
            if (GUILayout.Button("Log Event"))
                GeneralAnalytics.LogEvent(providers, "test_event");

            if (GUILayout.Button("Log Event With Params"))
            {
                var parameters = new Dictionary<string, string>(){
                    { "test_param1", "test_value1" },
                    { "test_param2", "test_value2" },
                    { "test_param3", "test_value3" },
                };
                GeneralAnalytics.LogEvent(providers, "test_event", parameters);
            }

            if (GUILayout.Button("Track Revenue"))
                GeneralAnalytics.TrackRevenue(providers, "test_revenue", 1, "USD");

            if (GUILayout.Button("Remote Config Set Interval"))
                GeneralAnalytics.RemoteConfig.SetMinimumFetchInterval(300);

            if (GUILayout.Button("Remote Config Fetch"))
                GeneralAnalytics.RemoteConfig.Fetch(str => Debug.Log($"{logName} {module} Remote Config fetched. Keys:[{str}]"), () => Debug.Log($"{logName} {module} Remote Config fetch FAILED!"));

            var rcKey = "midgame_interstitial_interval";
            if (GUILayout.Button("Remote Config Get"))
                Debug.Log($"{logName} {module} Remote Config get value for key ({rcKey}) is:{GeneralAnalytics.RemoteConfig.Get(rcKey, "default_value")}");

            if (GUILayout.Button("Get AppsFlyer UID"))
                Debug.Log($"{logName} {module} AppsFlyer UID: {GeneralAnalytics.GetAppsFlyerUID()}");

            if (GUILayout.Button("Get Tenjin Installation ID"))
                Debug.Log($"{logName} {module} Tenjin Installation ID: {GeneralAnalytics.GetTenjinInstallationId()}");
        }

        private void BillingTest()
        {
            const string module = "[Billing]";
#if UNITY_ANDROID
            const string defaultStoreName = "GooglePlay";
#elif UNITY_IOS
            const string defaultStoreName = "AppStore";
#else
            const string defaultStoreName = "PC";
#endif
            GUILayout.Label($"{logName} {module} {defaultStoreName}");

            if (GUILayout.Button("Initialize"))
                Billing.Initialize(succeed => Debug.Log($"{logName} {module} {defaultStoreName} initialization result:{succeed}"));

            if (GUILayout.Button("Get Purchases"))
            {
                Billing.GetPurchases((succeed, list) =>
                {
                    Debug.Log($"{logName} {module} {defaultStoreName} GetPurchases result:{succeed} count:{list.Count}");
                    foreach (var item in list)
                    {
                        Debug.Log($"{logName} {module} {defaultStoreName} procudt: sku={item.sku}, status={item.status}, androidSignature=\"{item.androidSignature}\"");
                        if (item.status == Billing.PurchaseStatus.Purchased && marketModel.purchased.Contains(item.token) == false)
                            marketModel.purchased.Add(item.token);
                    }
                });
            }


            if (GUILayout.Button("Get Sku Details"))
            {
                Billing.GetSkuDetails((succeed, list) =>
                {
                    Debug.Log($"{logName} {module} {defaultStoreName} GetSkuDetails success:{succeed}, count:{list.Count}");
                    foreach (var item in list)
                    {
                        Debug.Log($"{logName} {module} {defaultStoreName} procudt: sku={item.sku}, title={item.title}, desc={item.description}, formatted price={item.priceFormatted}, currency={item.priceCurrency}, price value={item.priceAmount}");
                    }
                },
                marketModel.skus);
            }

            // traverse through all purchased items and display buttons to ack/ consume or finish them
            for (int i = 0; i < marketModel.purchased.Count; i++)
            {
                var item = marketModel.purchased[i];
                if (item != string.Empty)
                {
                    if (GUILayout.Button($"ConsumeOrAcknowledge\nOrFinishTransaction\ntoken:{item.Substring(0, 12)}..."))
                    {
                        Billing.FinishPurchase(item, (succeed, token) =>
                        {
                            Debug.Log($"{logName} {module} {defaultStoreName} Finish result:{succeed} token:{token}");
                            if (succeed) marketModel.purchased.Remove(token);
                        });
                    }
                }
            }

            foreach (var item in marketModel.skus)
            {
                if (GUILayout.Button($"Purchase\n{item}"))
                {
                    Billing.StartPurchase(item, (status, result) =>
                    {
                        Debug.Log($"{logName} {module} {defaultStoreName} Purchase result:{status} status:{result.status} token:\"{result.token}\" failureReason: {result.failureReason} errorCode: {result.errorCode}");
                        if (status == Billing.PurchaseStatus.Purchased &&
                            marketModel.purchased.Contains(result.token) == false)
                            marketModel.purchased.Add(result.token);
                    }
                    , $"test_accountId_{item}", $"test_profileId_{item}");
                }
            }

#if UNITY_IOS
            if (GUILayout.Button("Restore Purchases"))
            {
                Billing.RestorePurchases(result =>
                {
                    Debug.Log($"{logName} {module} RestorePurchases success. sku: {result.sku}, token: \"{result.token}\" iosReceipt: \"{result.iosReceipt.Substring(0, 12)}...\"");
                    if (marketModel.purchased.Contains(result.token) == false)
                        marketModel.purchased.Add(result.token);
                });
            }
#endif
        }


        private void GameAnalyticsTest()
        {
            const string module = "[GameAnalytics]";

            GUILayout.Label("Game Analytics");

            if (GUILayout.Button("Initialize"))
            {
                GameAnalytics.Initialize(
                    new GameAnalytics.Builder(gameAnalytics.gameKey, gameAnalytics.secretKey)
                        .SetUserId("test_user_id")
                                                .ConfigureResourceCurrencies("stone")
                        .ConfigureItemTypes("shop")
                        .ConfigureCustomDimensions01("tester", "player")
                        .ConfigureCustomDimensions02("group1", "group2")
                        .ConfigureCustomDimensions03("Warrior", "Archer"),
                    () => Debug.Log($"{logName} {module} initialized."));
            }

            if (GUILayout.Button("Set Custom Dimension01"))
                GameAnalytics.SetCustomDimension01("tester");

            if (GUILayout.Button("Set Custom Dimension02"))
                GameAnalytics.SetCustomDimension02("group1");

            if (GUILayout.Button("Set Custom Dimension03"))
                GameAnalytics.SetCustomDimension03("Archer");

            if (GUILayout.Button("Business Event"))
                GameAnalytics.AddBusinessEvent("USD", 1, "shop", "stone_pack", "shop_01");

            if (GUILayout.Button("Business Event + Receipt"))
                GameAnalytics.AddBusinessEventWithReceipt("USD", 1, "shop", "stone_pack", "shop_01", "test_receipt", "google_play", "test_signature");

            if (GUILayout.Button("Resource Event"))
                GameAnalytics.AddResourceEvent(GameAnalytics.ResourceFlowType.Source, "stone", 125, "shop", "stone_pack");

            if (GUILayout.Button("Progression Event"))
                GameAnalytics.AddProgressionEvent(GameAnalytics.ProgressionStatus.Start, "world_0", "zone_1", "boos_2");

            if (GUILayout.Button("Progression Event + Score"))
                GameAnalytics.AddProgressionEventWithScore(GameAnalytics.ProgressionStatus.Start, "world_0", "zone_1", "boos_2", 20.0);

            if (GUILayout.Button("Design Event"))
                GameAnalytics.AddDesignEvent("test_event");

            if (GUILayout.Button("Design Event + Value"))
                GameAnalytics.AddDesignEventWithValue("test_event", 1.5f);

            if (GUILayout.Button("Error Event"))
                GameAnalytics.AddErrorEvent(GameAnalytics.ErrorSeverity.Error, "test error message");

            if (GUILayout.Button("Ad Event"))
                GameAnalytics.AddAdEvent(GameAnalytics.AdAction.Clicked, GameAnalytics.AdType.RewardedVideo, Ad.Provider.Admob.ToString(), "test_placement");

            if (GUILayout.Button("Subscribe Ad Events"))
                GameAnalytics.SubscribeAdEventTracking();
                
            if (GUILayout.Button("Subscribe AdMob\nImpressions"))
                GameAnalytics.SubscribeAdMobImpressions();

            if (GUILayout.Button("Subscribe Max\nImpressions"))
                GameAnalytics.SubscribeMaxImpressions();

            if (GUILayout.Button("Subscribe LevelPlay\nImpressions"))
                GameAnalytics.SubscribeIronSourceImpressions();

            if (GUILayout.Button("Impression\nAdMob Event"))
                GameAnalytics.AddImpressionAdMobEvent("test_unit_id", "banner", "US", AdMobImpressionPrecision.Estimated, "UnityAds", 0, "1.2.3");

            if (GUILayout.Button("Impression\nMax Event"))
                GameAnalytics.AddImpressionMaxEvent("test_unit_id", "interstitial", "AppLovin", "US", "win", "test_creative_id", 0, "2.3.4");

            if (GUILayout.Button("Impression\nLevelPlay Event"))
                GameAnalytics.AddImpressionIronSourceEvent("test_unit_id", "rewarded", "ironsource", "US", "win", "test_creative_id", 0, "BID", "3.4.5");

            if (GUILayout.Button("Is RemoteConfigs Ready"))
                Debug.Log($"{logName} {module} IsRemoteConfigsReady result:{GameAnalytics.IsRemoteConfigsReady()}");

            if (GUILayout.Button("Remote Configs Get"))
                Debug.Log($"{logName} {module} Remote Config Get result:{GameAnalytics.GetRemoteConfigsValueAsString("test_key", "default value")}");

            if (GUILayout.Button("AB Testing Id"))
                Debug.Log($"{logName} {module} GetABTestingId result:{GameAnalytics.GetABTestingId()}");

            if (GUILayout.Button("AB Variant Id"))
                Debug.Log($"{logName} {module} GetABTestingVariantId result:{GameAnalytics.GetABTestingVariantId()}");
        }

        private void GameServicesTest()
        {
            const string module = "[GameService]";

            GUILayout.Label($"Game Services\nIsSignedIn:{GameServices.IsSignedIn}");

            if (GUILayout.Button("Initialize"))
                GameServices.Initialize(result => Debug.Log($"{logName} {module} signin result... success:{result}"));

            if (GUILayout.Button("Signin"))
                GameServices.SignIn();

            if (GUILayout.Button("GetPlayerInfo"))
                GameServices.GetPlayerInfo(result => Debug.Log($"{logName} {module} GetPlayerInfo result... success:{result.success}"));

            if (GUILayout.Button("GetServerSideParams"))
                GameServices.GetServerSideParams(result => Debug.Log($"{logName} {module} GetServerSideParams result... success:{result.success}"));

            if (GUILayout.Button("Save"))
                GameServices.Save("test_filename", "{\"test_data\":123456}", "test_description", succeed => Debug.Log($"{logName} {module} save result:{succeed}"));

            if (GUILayout.Button("Load"))
                GameServices.Load("test_filename", json => Debug.Log($"{logName} {module} load result:{json}"));
        }

        private void UtilsTest()
        {
            const string module = "[Utils]";

            GUILayout.Label("Utils");

            if (GUILayout.Button("Show RateUs"))
                Utils.ShowRateUs(succeed => Debug.Log($"{logName} {module} ShowRateUs result:{succeed}"));

            if (GUILayout.Button("Share Text"))
                Utils.ShareText("This is a simple text to share!");
        }

        private void PushNotification()
        {
            const string module = "[PushNotification]";

            GUILayout.Label("Push Notification");

            if (GUILayout.Button("Get Token"))
                Plankton.PushNotification.GetToken(token => Debug.Log($"{logName} {module} GetToken result:{token}"));
        }

        private void GoogleUmpTest()
        {
            const string module = "[GoogleUmp]";

            GUILayout.Label("Google Ump");

            if (GUILayout.Button("Can Request Ads"))
                Debug.Log($"{logName} {module} CanRequestAds:{GoogleUmp.CanRequestAds()}");

            if (GUILayout.Button("Is Privacy Options\nRequired"))
                Debug.Log($"{logName} {module} IsPrivacyOptionsRequired:{GoogleUmp.IsPrivacyOptionsRequired()}");

            if (GUILayout.Button("Request Consent\nInfo Update"))
                GoogleUmp.RequestConsentInfoUpdate(result => Debug.Log($"{logName} {module} RequestConsentInfoUpdate result:{result}"));

            if (GUILayout.Button("Load And Show\nConsent Form\nIf Required"))
                GoogleUmp.LoadAndShowConsentFormIfRequired(result => Debug.Log($"{logName} {module} LoadAndShowConsentFormIfRequired result:{result}"));

            if (GUILayout.Button("Show Privacy\nOptions Form"))
                GoogleUmp.ShowPrivacyOptionsForm(result => Debug.Log($"{logName} {module} ShowPrivacyOptionsForm result:{result}"));

            if (GUILayout.Button("Reset Consent\nInformation"))
                GoogleUmp.ResetConsentInformation();
        }

        private void AppTT()
        {
            const string module = "[AppTrackingTransparency]";

            GUILayout.Label("App Tracking Transparency");

            if (GUILayout.Button("Get Tracking\nAuthorization Status"))
                Debug.Log($"{logName} {module} GetTrackingAuthorizationStatus:{AppTrackingTransparency.GetTrackingAuthorizationStatus()}");

            if (GUILayout.Button("Request Tracking\nAuthorization"))
                AppTrackingTransparency.RequestTrackingAuthorization(result => Debug.Log($"{logName} {module} RequestTrackingAuthorization result:{result}"));
        }


        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        [System.Serializable]
        private class MarketModel
        {
            public string[] skus = new string[0];
            [HideInInspector] public List<string> purchased = new List<string>();
        }

        [System.Serializable]
        private class GAModel
        {
            public string gameKey = string.Empty;
            public string secretKey = string.Empty;
        }
    }
}