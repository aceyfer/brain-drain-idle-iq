using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Plankton.Editor
{
    [System.Serializable]
    public class PlanktonSettings
    {
        public bool generatePreBuild = true;
        public bool generateAar = true;
        public PresetData preset = new PresetData();

        //////////////////////////////////////////////////////
        /// STATIC MEMBERS
        //////////////////////////////////////////////////////
        static private PlanktonSettings instance = new PlanktonSettings();
        static private string instanceJson = string.Empty;
        static private string SettingsFilename => Application.dataPath + "/../ProjectSettings/PlanktonSettings.txt";

        static public bool GeneratePreBuild => instance.generatePreBuild;
        static public bool GenerateAarFile => instance.generateAar;
        static public PresetData Preset => instance.preset;

        static PlanktonSettings()
        {
            try
            {
                var json = File.ReadAllText(SettingsFilename);
                instance = JsonUtility.FromJson<PlanktonSettings>(json);
            }
            catch { }
        }

        static private void Save()
        {
            instanceJson = JsonUtility.ToJson(instance, true);
            File.WriteAllText(SettingsFilename, instanceJson);
        }

        static public void LoadPreset(string fileName)
        {
            var json = File.ReadAllText(fileName);
            instance.preset = JsonUtility.FromJson<PresetData>(json);
            Save();
        }

        static private void LoadPreset()
        {
            var directory = Path.GetFullPath(Application.dataPath + "/../UserSettings");
            var filename = EditorUtility.OpenFilePanel("Load Preset", directory, "json");
            if (string.IsNullOrEmpty(filename)) return;
            LoadPreset(filename);
        }

        static private void SavePreset()
        {
            var directory = Path.GetFullPath(Application.dataPath + "/../UserSettings");
            var filename = EditorUtility.SaveFilePanel("Save Preset", directory, "preset", "json");
            if (string.IsNullOrEmpty(filename)) return;
            var json = JsonUtility.ToJson(instance.preset, true);
            File.WriteAllText(filename, json);
        }

        static public void GenerateFiles()
        {
            GUI.FocusControl(null); // clear focus

            var fileManager = new FileManager();
#if UNITY_IOS
            var all = Preset.GetPluginsAsList(BuildTarget.iOS).FindAll(x => x.active == true);
#elif UNITY_ANDROID
            var all = Preset.GetPluginsAsList(BuildTarget.Android).FindAll(x => x.active == true);
#else
            var all = new List<Plugin>();
#endif
            foreach (var plugin in all)
                plugin.OnPrebuild(fileManager);

            fileManager.SaveFiles(instance.generateAar);

            AssetDatabase.Refresh();
        }

        //////////////////////////////////////////////////////
        /// Settings Provider
        //////////////////////////////////////////////////////
        [SettingsProvider]
        static public SettingsProvider CreateSettings()
        {
#if UNITY_ANDROID
            var platformIndex = 0;
#else
            var platformIndex = 1;
#endif

            var provider = new SettingsProvider("Project/Plankton", SettingsScope.Project)
            {
                label = "Plankton",
                keywords = new HashSet<string>(new[] { "Plankton", "Settings", "Core" })
            };

            provider.guiHandler = (searchContext) =>
            {
                var platform = platformIndex == 0 ? BuildTarget.Android : BuildTarget.iOS;

                instance.generatePreBuild = EditorGUILayout.ToggleLeft("Prebuild Auto Generate Plugins", instance.generatePreBuild);
                var rect = EditorGUILayout.GetControlRect();
                rect.width /= 3;
                if (GUI.Button(rect, "Generate Plugins Now")) GenerateFiles();
                rect.x += rect.width;
                if (GUI.Button(rect, "Save Preset")) SavePreset();
                rect.x += rect.width;
                if (GUI.Button(rect, "Load Preset"))
                {
                    LoadPreset();
                    provider.Repaint();
                }

                var nextPlatformIndex = GUILayout.Toolbar(platformIndex, new string[] { "Android", "iOS" });
                if (nextPlatformIndex != platformIndex)
                {
                    platformIndex = nextPlatformIndex;
                    GUI.FocusControl(null);
                    provider.Repaint();
                    return;
                }

                EditorGUILayout.BeginVertical();
                var all = Preset.GetPluginsAsList(platform);
                foreach (var plugin in all)
                {
                    plugin.OnEditorGUI(platform);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndVertical();

                // validate changes
                var json = JsonUtility.ToJson(instance, true);
                if (json != instanceJson)
                {
                    instanceJson = json;
                    Save();
                }
            };


            return provider;
        }



        //////////////////////////////////////////////////////
        /// HELPER CLASSES
        //////////////////////////////////////////////////////
        [System.Serializable]
        public class PresetData
        {
            public IOS ios = new IOS();
            public Android android = new Android();

            public List<Plugin> GetPluginsAsList(BuildTarget platform)
            {
                switch (platform)
                {
                    case BuildTarget.iOS: return ios.List;
                    case BuildTarget.Android: return android.List;
                    default: return new List<Plugin>();
                }
            }

            [System.Serializable]
            public class IOS
            {
                public Plugin_AppTrackingTransparency appTrackingTransparency = new Plugin_AppTrackingTransparency();
                public Plugin_Admob admob = new Plugin_Admob();
                public Plugin_Applovin appLovin = new Plugin_Applovin();
                public Plugin_LevelPlay levelPlay = new Plugin_LevelPlay();
                public Plugin_UMP googleUmp = new Plugin_UMP();
                public Plugin_GameCenter gameCenter = new Plugin_GameCenter();
                public Plugin_AppsFlyer appsFlyer = new Plugin_AppsFlyer();
                public Plugin_Tenjin tenjin = new Plugin_Tenjin();
                public Plugin_AppMetrica appMetrica = new Plugin_AppMetrica();
                public Plugin_Firebase firebase = new Plugin_Firebase();
                public Plugin_GameAnalytics gameAnalytics = new Plugin_GameAnalytics();
                public Plugin_Billing billing = new Plugin_Billing();
                public Plugin_Others others = new Plugin_Others() { platform = "ios" };

                public List<Plugin> List => new List<Plugin>() { appTrackingTransparency, admob, appLovin, levelPlay, googleUmp, billing, gameCenter, firebase, gameAnalytics, appsFlyer, appMetrica, tenjin, others };
            }

            [System.Serializable]
            public class Android
            {
                public Plugin_DebugMode debug = new Plugin_DebugMode();
                public Plugin_Admob admob = new Plugin_Admob();
                public Plugin_Applovin appLovin = new Plugin_Applovin();
                public Plugin_LevelPlay levelPlay = new Plugin_LevelPlay();
                public Plugin_UMP googleUmp = new Plugin_UMP();
                public Plugin_PlayServices playServices = new Plugin_PlayServices();
                public Plugin_AppsFlyer appsFlyer = new Plugin_AppsFlyer();
                public Plugin_Tenjin tenjin = new Plugin_Tenjin();
                public Plugin_AppMetrica appMetrica = new Plugin_AppMetrica();
                public Plugin_Firebase firebase = new Plugin_Firebase();
                public Plugin_GameAnalytics gameAnalytics = new Plugin_GameAnalytics();
                public Plugin_Billing billing = new Plugin_Billing();
                public Plugin_Others others = new Plugin_Others() { platform = "android" };

                public List<Plugin> List => new List<Plugin>() { debug, admob, appLovin, levelPlay, googleUmp, billing, playServices, firebase, gameAnalytics, appsFlyer, appMetrica, tenjin, others };
            }
        }
    }
}