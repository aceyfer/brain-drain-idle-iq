using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Plankton.Editor
{
    [System.Serializable]
    public abstract class Plugin
    {
        public const string planktonIosVersion = "'~> 5.4.0'";

        public bool active = false;

        public virtual string Name => "Base";
        protected virtual bool IsNotAvailable => true;

        public virtual void OnEditorGUI(BuildTarget platform)
        {
            var rect = EditorGUILayout.GetControlRect();
            rect.y += rect.height - 5;
            rect.height = 5;
            GUI.Box(rect, string.Empty);
            if (IsNotAvailable)
                active = EditorGUILayout.ToggleLeft($" {Name} : not available in the free version!", active);
            else
                active = EditorGUILayout.ToggleLeft($" {Name} ", active);
            EditorGUI.indentLevel = 1;
        }

        public abstract void OnPrebuild(FileManager fileManager);
    }

    [System.Serializable]
    public class Plugin_DebugMode : Plugin
    {
        public override string Name => "Debug Mode";

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public abstract class Plugin_AppId : Plugin
    {
        public string appId = string.Empty;

        public virtual string appIdLabel => "App Id";

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);
            if (active) appId = EditorGUILayout.TextField(appIdLabel, appId);
        }
    }

    [System.Serializable]
    public abstract class Plugin_Ad : Plugin_AppId
    {
        public List<Network> mediations = new List<Network>();

        protected void DrawNetworks(BuildTarget platform)
        {
            if (active == false) return;
            var rect = EditorGUILayout.GetControlRect();
            GUI.Box(rect, "Additional Networks : not available in the free version!", EditorStyles.centeredGreyMiniLabel);
            int i = 0;
            foreach (var network in mediations)
            {
                if (i % 4 == 0)
                {
                    rect = EditorGUILayout.GetControlRect();
                    rect.width /= 4;
                }
                network.active = EditorGUI.ToggleLeft(rect, network.name, network.active);
                rect.x += rect.width;
                i++;
            }
        }

        protected void PerformOnFileManager(FileManager fileManager)
        {
            foreach (var item in mediations)
            {
                if (item.active)
                {
                    if (string.IsNullOrEmpty(item.dependency) == false)
                    {
                        fileManager.android.dependencies.Add(item.dependency);
                        if (string.IsNullOrEmpty(item.maven) == false)
                            fileManager.android.repositories.Add(item.maven);
                    }
                    if (string.IsNullOrEmpty(item.pod) == false)
                    {
                        fileManager.ios.dependencies.Add(item.pod);
                    }
                }
            }
        }

        protected void UpdateNetworks(List<Network> networks)
        {
            // remove obsoluted items
            mediations.RemoveAll(m => !networks.Exists(n => n.name == m.name));

            // add or update remained networks
            for (int i = 0; i < networks.Count; i++)
            {
                var network = networks[i];
                var item = mediations.Find(x => x.name == network.name);
                if (item == null)
                {
                    mediations.Add(item = new Network());
                }

                item.name = network.name;
                item.pod = network.pod;
                item.dependency = network.dependency;
                item.maven = network.maven;
            }
        }

        [System.Serializable]
        public class Network
        {
            public bool active = false;
            public string name = string.Empty;
            public string pod = string.Empty;
            public string dependency = string.Empty;
            public string maven = string.Empty;
        }
    }

    [System.Serializable]
    public class Plugin_Admob : Plugin_Ad
    {
        public override string Name => "AdMob";
        protected override bool IsNotAvailable => false;

        private readonly List<Network> networks = new List<Network>
        {
                new Network() { name = "AppLovin" },
                new Network() { name = "Chartboost" },
                new Network() { name = "DT Exchange (Fyber)" },
                new Network() { name = "Meta (Facebook)" },
                new Network() { name = "ironSource" },
                new Network() { name = "Liftoff (Vungle)" },
                new Network() { name = "Mintegral" },
                new Network() { name = "Pangle" },
                new Network() { name = "Unity Ads" },
                new Network() { name = "Premium Ads" },
        };

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);
            UpdateNetworks(networks);
            DrawNetworks(platform);
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            UpdateNetworks(networks);
            PerformOnFileManager(fileManager);
            
            foreach (Network network in networks)
                if (network.active)
                    FreeVersion.NotAvailable($"{Name} network {network.name}");
            fileManager.ios.dependencies.Add($"pod 'PlanktonAdsAdMobSDK', {planktonIosVersion}");
            fileManager.ios.planktonConfig.placeholders.Add($"\"GADApplicationIdentifier\": \"{appId}\"");
            fileManager.android.dependencies.Add("implementation 'com.google.android.gms:play-services-ads:23.6.0'");
            fileManager.android.launcherManifest.placeholders.Add($"<meta-data android:name=\"com.google.android.gms.ads.APPLICATION_ID\" android:value=\"{appId}\"/>");
        }
    }

    [System.Serializable]
    public class Plugin_Applovin : Plugin_Ad
    {
        public string adMobAppId = string.Empty;

        public override string Name => "AppLovin MAX";
        public override string appIdLabel => "SDK Key";

        private readonly List<Network> networks = new List<Network>
        {
                new Network() { name = "AdMob" },
                new Network() { name = "Google Ad Manager" },
                new Network() { name = "DT Exchange (Fyber)" },
                new Network() { name = "ironSource" },
                new Network() { name = "Liftoff (Vungle)" },
                new Network() { name = "Meta (Facebook)" },
                new Network() { name = "Mintegral" },
                new Network() { name = "Pangel" },
                new Network() { name = "Unity Ads" },
                new Network() { name = "Premium Ads" },
        };

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);

            if (active && NeedAdMobAppId())
                adMobAppId = EditorGUILayout.TextField("AdMob App Id", adMobAppId);

            UpdateNetworks(networks);
            DrawNetworks(platform);
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
            foreach (Network network in networks)
                if (network.active)
                    FreeVersion.NotAvailable($"{Name} network {network.name}");
        }

        private bool NeedAdMobAppId()
        {
            var adMob = mediations.Find(x => x.name == "AdMob");
            var googleAdMan = mediations.Find(x => x.name == "Google Ad Manager");
            return (adMob != null && adMob.active) || (googleAdMan != null && googleAdMan.active);
        }
    }

    [System.Serializable]
    public class Plugin_LevelPlay : Plugin_Ad
    {
        public string adMobAppId = string.Empty;

        public override string Name => "LevelPlay";

        public override string appIdLabel => "App Key";

        private readonly List<Network> networks = new List<Network>
        {
                new Network() { name = "AdMob" },
                new Network() { name = "AppLovin" },
                new Network() { name = "Chartboost" },
                new Network() { name = "DT Exchange (Fyber)" },
                new Network() { name = "Liftoff (Vungle)" },
                new Network() { name = "Meta (Facebook)" },
                new Network() { name = "Mintegral" },
                new Network() { name = "Pangle" },
                new Network() { name = "Unity Ads" },
        };

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);

            if (active && NeedAdMobAppId())
                adMobAppId = EditorGUILayout.TextField("AdMob App Id", adMobAppId);
                
            UpdateNetworks(networks);
            DrawNetworks(platform);
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
            foreach (Network network in networks)
                if (network.active)
                    FreeVersion.NotAvailable($"{Name} network {network.name}");
        }

        private bool NeedAdMobAppId()
        {
            var adMob = mediations.Find(x => x.name == "AdMob");
            return adMob != null && adMob.active;
        }
    }

    [System.Serializable]
    public class Plugin_Firebase : Plugin
    {
        public string ios_plist = string.Empty;
        public string gcm_defaultSenderId = string.Empty;
        public string google_storage_bucket = string.Empty;
        public string project_id = string.Empty;
        public string google_api_key = string.Empty;
        public string google_crash_reporting_api_key = string.Empty;
        public string google_app_id = string.Empty;
        public string default_web_client_id = string.Empty;
        public bool activePushNotification = false;

        public override string Name => "Firebase";
        protected override bool IsNotAvailable => false;

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);
            if (active == false) return;

            var lastIndentLevel = EditorGUI.indentLevel;
            switch (platform)
            {
                case BuildTarget.iOS:
                    {
                        if (string.IsNullOrEmpty(ios_plist))
                        {
                            EditorGUILayout.HelpBox("Firebase configuration NOT found!\nPlease download Firebase config file from your Firebase panel and open it via the button below.", MessageType.Warning);
                        }
                        else
                        {
                            activePushNotification = EditorGUILayout.ToggleLeft("Push Notification : not available in the free version!", activePushNotification);
                            EditorGUILayout.HelpBox("Firebase configuration found", MessageType.Info);
                        }

                        if (GUILayout.Button("Open Firebase file"))
                        {
                            var inputJsonFilePath = EditorUtility.OpenFilePanel("Choose firebase config file", "", "plist");
                            if (string.IsNullOrEmpty(inputJsonFilePath)) return;
                            ios_plist = System.IO.File.ReadAllText(inputJsonFilePath);
                        }
                    }
                    break;
                case BuildTarget.Android:
                    {
                        if (string.IsNullOrEmpty(gcm_defaultSenderId) || string.IsNullOrEmpty(project_id) || string.IsNullOrEmpty(google_app_id))
                        {
                            EditorGUILayout.HelpBox("Firebase configuration NOT found!\nPlease download Firebase JSON from your Firebase panel and open it via the button below.", MessageType.Warning);
                        }
                        else
                        {
                            activePushNotification = EditorGUILayout.ToggleLeft("Push Notification : not available in the free version!", activePushNotification);
                            EditorGUILayout.HelpBox("Firebase configuration found", MessageType.Info);
                        }

                        if (GUILayout.Button("Open Firebase Json"))
                        {
                            var inputJsonFilePath = EditorUtility.OpenFilePanel("Choose firebase config file", "", "json");
                            if (string.IsNullOrEmpty(inputJsonFilePath)) return;
                            var inputJsonText = System.IO.File.ReadAllText(inputJsonFilePath);
                            var inputObj = JsonUtility.FromJson<FireBaseJsonObject>(inputJsonText);
                            var client = inputObj.client.Find(x => x.client_info.android_client_info.package_name == Application.identifier);
                            if (client == null)
                            {
                                EditorUtility.DisplayDialog("Error", $"Can't find {Application.identifier} in the specified file!\nPlease select a valid file.", "OK");
                                return;
                            }
                            gcm_defaultSenderId = inputObj.project_info.project_number;
                            google_storage_bucket = inputObj.project_info.storage_bucket;
                            project_id = inputObj.project_info.project_id;
                            google_api_key = client.api_key[0].current_key;
                            google_crash_reporting_api_key = client.api_key[0].current_key;
                            google_app_id = client.client_info.mobilesdk_app_id;
                            default_web_client_id = client.oauth_client.Count > 0 ? client.oauth_client[0].client_id : string.Empty;
                        }
                    }
                    break;
            }
            EditorGUI.indentLevel = lastIndentLevel;
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            if (activePushNotification)
            {
                FreeVersion.NotAvailable($"{Name} PushNotification");
            }

            fileManager.android.dependencies.Add("implementation 'com.google.firebase:firebase-analytics-ktx:21.1.1'");
            fileManager.android.values.placeholders.Add($"<string name=\"gcm_defaultSenderId\" translatable=\"false\">{gcm_defaultSenderId}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"google_storage_bucket\" translatable=\"false\">{google_storage_bucket}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"project_id\" translatable=\"false\">{project_id}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"google_api_key\" translatable=\"false\">{google_api_key}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"google_crash_reporting_api_key\" translatable=\"false\">{google_crash_reporting_api_key}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"google_app_id\" translatable=\"false\">{google_app_id}</string>");
            fileManager.android.values.placeholders.Add($"<string name=\"default_web_client_id\" translatable=\"false\">{default_web_client_id}</string>");

            fileManager.ios.dependencies.Add($"pod 'PlanktonAnalyticsFirebaseSDK', {planktonIosVersion}");
            fileManager.ios.firebase.content = ios_plist;
        }

        /// <summary>
        /// Classes and fields are named same as the json file du to load correctly.
        /// NOTE: DO NOT change any class name or field name in this class.
        /// </summary>
        [System.Serializable]
        private class FireBaseJsonObject
        {
            public Project_info project_info;
            public List<Client> client;
            public string configuration_version;

            [System.Serializable]
            public class Project_info
            {
                public string project_number;
                public string project_id;
                public string storage_bucket;
            }

            [System.Serializable]
            public class Client
            {
                public Client_info client_info;
                public List<Oauth_client> oauth_client;
                public List<Api_key> api_key;
                public Services services;
                public string admob_app_id;

                [System.Serializable]
                public class Client_info
                {
                    public string mobilesdk_app_id;
                    public Android_client_info android_client_info;

                    [System.Serializable]
                    public class Android_client_info { public string package_name; }
                }

                [System.Serializable]
                public class Oauth_client
                {
                    public string client_id;
                    public int client_type;
                }

                [System.Serializable]
                public class Api_key { public string current_key; }

                [System.Serializable]
                public class Services
                {
                    public Appinvite_service appinvite_service;

                    [System.Serializable]
                    public class Appinvite_service
                    {
                        public List<Other_platform_oauth_client> other_platform_oauth_client;

                        [System.Serializable]
                        public class Other_platform_oauth_client
                        {
                            public string client_id;
                            public int client_type;
                        }
                    }
                }

            }
        }
    }

    [System.Serializable]
    public class Plugin_AppsFlyer : Plugin_AppId
    {
        public string appleAppId = string.Empty;
        public string endpointUrl = string.Empty;

        public override string Name => "AppsFlyer";
        public override string appIdLabel => "Dev Key";

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);

            if (active && platform == BuildTarget.iOS)
            {
                appleAppId = EditorGUILayout.TextField("Apple App Id", appleAppId);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advertising attribution report endpoint URL");
                endpointUrl = EditorGUILayout.TextField("", endpointUrl);

            }
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_Tenjin : Plugin_AppId
    {
        public override string Name => "Tenjin";
        public override string appIdLabel => "SDK Key";

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_AppMetrica : Plugin_AppId
    {
        public override string Name => "AppMetrica";
        public override string appIdLabel => "API Key";

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_PlayServices : Plugin_AppId
    {
        public override string Name => "Google Play Services";

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_GameCenter : Plugin
    {
        public override string Name => "Game Center";

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_AppTrackingTransparency : Plugin
    {
        public override string Name => "App Tracking Transparency";

        public string description = "We use your information in order to enhance your game experience, by serving you personalized ads and measuring the performance of our game.";

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);
            EditorGUILayout.HelpBox("A message that informs the user why an app is requesting permission to use data for tracking the user or the device. Required for iOS 14.5 and higher", MessageType.Info);
            if (active)
            {
                EditorGUILayout.LabelField("Tracking Usage Description");
                description = EditorGUILayout.TextArea(description, EditorStyles.textArea);
                if (string.IsNullOrEmpty(description))
                    EditorGUILayout.HelpBox("Default description will be used!", MessageType.Warning);
            }
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_Billing : Plugin
    {
        public override string Name => "In-App Purchase";

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_GameAnalytics : Plugin
    {
        public override string Name => "Game Analytics";
        protected override bool IsNotAvailable => false;

        public override void OnPrebuild(FileManager fileManager)
        {
            fileManager.ios.dependencies.Add($"pod 'PlanktonGameAnalyticsSDK', {planktonIosVersion}");
            fileManager.android.dependencies.Add("implementation 'com.gameanalytics.sdk:gameanalytics-android:6.4.2'");
            fileManager.android.repositories.Add("maven { url 'https://maven.gameanalytics.com/release' }");
        }
    }

    [System.Serializable]
    public class Plugin_UMP : Plugin
    {
        public string adMobAppId = string.Empty;

        public override string Name => "Google UMP";

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);

            if (active)
                adMobAppId = EditorGUILayout.TextField("AdMob App Id", adMobAppId);
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            FreeVersion.NotAvailable(Name);
        }
    }

    [System.Serializable]
    public class Plugin_Others : Plugin
    {
        public string platform = string.Empty;
        public string dependencies = string.Empty;
        public string repositories = string.Empty;

        public override string Name => "Others";
        protected override bool IsNotAvailable => false;

        public override void OnEditorGUI(BuildTarget platform)
        {
            base.OnEditorGUI(platform);

            if (active)
            {
                EditorGUILayout.LabelField("Other Dependencies");
                dependencies = EditorGUILayout.TextArea(dependencies);
                EditorGUILayout.LabelField("Other Repositories");
                repositories = EditorGUILayout.TextArea(repositories);
            }
        }

        public override void OnPrebuild(FileManager fileManager)
        {
            if (platform == "ios")
            {
                fileManager.ios.dependencies.Add(dependencies.Replace("\n", "\n\t"));
                fileManager.ios.repositories.Add(repositories);
            }
            else if (platform == "android")
            {
                fileManager.android.dependencies.Add(dependencies.Replace("\n", "\n\t"));
                fileManager.android.repositories.Add(repositories.Replace("\n", "\n\t\t"));
            }
        }
    }
}