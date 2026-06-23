#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using Debug = UnityEngine.Debug;
using System.Xml;
using Newtonsoft.Json;


namespace Plankton.Editor
{
    public class PostProcessBuild
    {
        private static string[] PlanktonFrameworks = {
            "PlanktonCoreSDK",
            "PlanktonAnalyticsCoreSDK",
            "PlanktonAnalyticsFirebaseSDK",
            "PlanktonAdsCoreSDK",
            "PlanktonAdsAdMobSDK",
            "PlanktonGameAnalyticsSDK",
        };
        private const string PluginsIosPath = "Assets/Plugins/iOS";
        private const string InfoPlistFile = "/Info.plist";
        private const string FirebasePlistFile = "/GoogleService-Info.plist";
        private const string PlanktonConfigFile = "/plankton_config.json";
        private const string SkAdNetworksXml = "/sk_ad_networks.xml";
        private const string PodFile = "/Podfile";

        private static string rootPath = "";

        private static Dictionary<string, object> PlanktonConfig = new Dictionary<string, object>()
        {
            // Permanent values
            { "ITSAppUsesNonExemptEncryption", false },
        };

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

            rootPath = path;

            Debug.Log(">>> Post Process Build <<<");

            AddBuildAutomationFiles();
            ModifyInfoPlist();
            InstallPods();
            ModifyXcodeProject();
        }

        private static void AddBuildAutomationFiles()
        {
            CopyFile(PluginsIosPath + PlanktonConfigFile, rootPath + PlanktonConfigFile);
            CopyFile(PluginsIosPath + SkAdNetworksXml, rootPath + SkAdNetworksXml);
            CopyFile(PluginsIosPath + PodFile, rootPath + PodFile);
        }

        private static void CopyFile(string src, string dest)
        {
            if (File.Exists(src))
            {
                Debug.Log($"Copying [{src}] into [{dest}]");
                File.Copy(src, dest);
            }
            else
            {
                {
                    Debug.Log($"Failed to copy! [{src}] already exists.");
                }
            }
        }

        private static void ModifyXcodeProject()
        {
            Debug.Log(">> Modifying xcode project ... <<");

            string projPath = PBXProject.GetPBXProjectPath(rootPath);
            var project = new PBXProject();
            project.ReadFromFile(projPath);

            var mainTarget = project.GetUnityMainTargetGuid();
            var unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();

            if (IsFirebaseActive() && !string.IsNullOrEmpty(PlanktonSettings.Preset.ios.firebase.ios_plist)) {
                AddFirebasePlist(project, mainTarget, rootPath);
            }
            AddCapabilities(project, mainTarget, projPath);
            ModifyBuildProperties(project, projPath, unityFrameworkTarget);
            AddAppLovinXCFramework(project, mainTarget);
            AddTenjinXCFramework(project, mainTarget);
            AddPlanktonXCFrameworks(project, mainTarget);
            
            project.WriteToFile(projPath);
        }

        private static void AddFirebasePlist(PBXProject project, string target, string path)
        {
            var sourceFilePath = PluginsIosPath + FirebasePlistFile;
            if (File.Exists(sourceFilePath))
            {
                Debug.Log(">> Adding firebase config to path ... <<");
                File.Copy(sourceFilePath, path + FirebasePlistFile);
                var file = project.AddFile(path + FirebasePlistFile, path + FirebasePlistFile, PBXSourceTree.Absolute);
                project.AddFileToBuild(target, file);
            }
            else
            {
                Debug.Log($"File doesn't exist: {sourceFilePath}");
            }
        }

        private static void AddCapabilities(PBXProject project, string target, string projectPath) 
        {
            Debug.Log(">> Adding capabilities ... <<");
            if (!(IsGameCenterActive() || IsFCMActive())) {
                Debug.Log("No need to add capabalities.");
                return;
            }

            var entitlementsFile = project.GetEntitlementFilePathForTarget(target) ?? Path.Combine(rootPath, $"Unity-iPhone.entitlements");
            var manager = new ProjectCapabilityManager(
                projectPath,
                entitlementsFile,
                targetGuid: target
            );

            // Add the iCloud if GameServices is enabled
            if (IsGameCenterActive()) 
            {
                manager.AddiCloud(false, true, false, true, null);
            }
            
            // Add the next two lines if Firebase Push Notification is enabled
            if (IsFCMActive())
            {
                manager.AddPushNotifications(false);
                manager.AddBackgroundModes(BackgroundModesOptions.BackgroundFetch | BackgroundModesOptions.RemoteNotifications);
            } 

            manager.WriteToFile();

            // Add the Game Center if GameServices is enabled - After the manager is written to file
            if (IsGameCenterActive()) 
            {
                PlistDocument entitlements = new PlistDocument();
                entitlements.ReadFromString(File.ReadAllText(entitlementsFile));
                PlistElementDict entitlementsDict = entitlements.root;
                entitlementsDict.SetBoolean("com.apple.developer.game-center", true);
                File.WriteAllText(entitlementsFile, entitlements.WriteToString());
            }

            project.SetBuildProperty(target, "CODE_SIGN_ENTITLEMENTS", entitlementsFile);
            project.AddFile(entitlementsFile, entitlementsFile);
        }

        private static bool IsGameCenterActive() => false;

        private static bool IsFirebaseActive() => PlanktonSettings.Preset.ios.firebase.active;
        
        private static bool IsFCMActive() => false;

        private static void ModifyBuildProperties(PBXProject project, string projectPath, string unityFrameworkTarget)
        {
            Debug.Log(">> Adding build properties ... <<");

            project.SetBuildProperty(unityFrameworkTarget, "ENABLE_BITCODE", "NO");

            project.WriteToFile(projectPath);

            var projectInString = File.ReadAllText(projectPath);

            projectInString = projectInString.Replace("ENABLE_BITCODE = YES;", $"ENABLE_BITCODE = NO;");

            File.WriteAllText(projectPath, projectInString);
        }

        private static void AddAppLovinXCFramework(PBXProject project, string mainTarget)
        {
            var applovinPodsDir = Path.Combine(rootPath, "Pods", "AppLovinSDK");
            if (Directory.Exists(applovinPodsDir)) {
                var appLovinMainDir = Directory.EnumerateDirectories(applovinPodsDir).First();
                var xcframeworkPath = Path.Combine(appLovinMainDir, "AppLovinSDK.xcframework");
                if (Directory.Exists(xcframeworkPath)) {
                    Debug.Log(">> Adding AppLovinSDK.xcframework ... <<");
                    var file = project.AddFile(xcframeworkPath, xcframeworkPath);
                    project.AddFileToEmbedFrameworks(mainTarget, file);
                }
            }
            
        }

        private static void AddTenjinXCFramework(PBXProject project, string mainTarget)
        {
            var xcframeworkPath = Path.Combine(rootPath, "Pods", "TenjinSDK", "TenjinSDK.xcframework");
            if (Directory.Exists(xcframeworkPath)) 
            {
                Debug.Log(">> Adding TenjinSDK.xcframework ... <<");
                var file = project.AddFile(xcframeworkPath, xcframeworkPath);
                project.AddFileToEmbedFrameworks(mainTarget, file);
            }
        }

        private static void AddPlanktonXCFrameworks(PBXProject project, string mainTarget)
        {
            foreach (var framework in PlanktonFrameworks)
            {
                var xcframeworkPath = Path.Combine(rootPath, "Pods", framework, $"{framework}.xcframework");
                if (Directory.Exists(xcframeworkPath)) 
                {
                    Debug.Log($">> Adding {framework}.xcframework ... <<");
                    var file = project.AddFile(xcframeworkPath, xcframeworkPath);
                    project.AddFileToEmbedFrameworks(mainTarget, file);
                }
            }
        }

        private static void ModifyInfoPlist()
        {
            var plistDoc = GetPlistDocument();
            var plistDict = plistDoc.root;

            AddPlanktonConfig(plistDict);
            AddSKAdNetworks(plistDict);
            AddFacebookPlist(plistDict);

            UpdatePlist(plistDoc);
        }

        private static PlistDocument GetPlistDocument()
        {
            var plistPath = rootPath + InfoPlistFile;
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            return plist;
        }

        private static void AddPlanktonConfig(PlistElementDict plistDict)
        {
            Debug.Log(">> Adding plankton config ... <<");

            var filePath = rootPath + PlanktonConfigFile;
            if (!File.Exists(filePath))
            {
                Debug.Log($"File doesn't exist: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            var configs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (var kvp in configs)
            {
                PlanktonConfig[kvp.Key] = kvp.Value;
            }

            foreach (var config in PlanktonConfig)
            {
                switch (config.Value)
                {
                    case String s:
                        plistDict.SetString(config.Key, s);
                        break;
                    case Boolean b:
                        plistDict.SetBoolean(config.Key, b);
                        break;
                }
            }
        }

        private static void AddSKAdNetworks(PlistElementDict plistDict)
        {
            Debug.Log(">> Adding SKAdNetworks ... <<");

            var filePath = rootPath + SkAdNetworksXml;
            if (!File.Exists(filePath))
            {
                Debug.Log($"File doesn't exist: {filePath}");
                return;
            }

            var adNetworksArray = plistDict.CreateArray("SKAdNetworkItems");

            XmlDocument doc = new XmlDocument();

            doc.Load(filePath);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                var networkItems = adNetworksArray.AddDict();
                networkItems.SetString("SKAdNetworkIdentifier", node.LastChild.InnerText);
            }
        }

        private static void AddFacebookPlist(PlistElementDict plistDict)
        {
            var configs =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(rootPath + PlanktonConfigFile));
            var facebookAppIdKey = "FacebookAppID";

            if (configs.ContainsKey(facebookAppIdKey))
            {
                Debug.Log(">> Adding Facebook Plist ... <<");

                var adNetworksArray = plistDict.CreateArray("CFBundleURLTypes");
                var networkItems = adNetworksArray.AddDict();
                var secondArray = networkItems.CreateArray("CFBundleURLSchemes");
                secondArray.values.Add(new PlistElementString("fb" + configs[facebookAppIdKey]));
            }
        }

        private static void UpdatePlist(PlistDocument plist)
        {
            var plistPath = rootPath + InfoPlistFile;
            File.WriteAllText(plistPath, plist.WriteToString());
        }
        
        private static void InstallPods()
        {
            ExecuteProcessTerminal(rootPath, "pod install --repo-update"); 
        }

        private static string ExecuteProcessTerminal(string workingDirectory, string command)
        {
            try
            {
                Debug.Log("============== Start Executing [" + command + "] ===============");
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/zsh",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory,
                    Arguments = " -l -c \"" + command + " \"", 
                };

                string path = "/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PATH")))
                {
                    path += ":" + Environment.GetEnvironmentVariable("PATH");
                }
                startInfo.EnvironmentVariables.Remove("PATH");
                startInfo.EnvironmentVariables.Add("PATH", path);
                startInfo.EnvironmentVariables.Remove("LANG");
                startInfo.EnvironmentVariables.Add("LANG", (Environment.GetEnvironmentVariable("LANG") ?? "en_US.UTF-8").Split('.')[0] + ".UTF-8");
                
                Process myProcess = new Process { StartInfo = startInfo };
                myProcess.Start();
                
                string output = myProcess.StandardOutput.ReadToEnd();
                string error = myProcess.StandardError.ReadToEnd();
                
                myProcess.WaitForExit();
                var endLog = "============== End Execution of [" + command + "] ===============\n";
                if (myProcess.ExitCode != 0)
                {
                    Debug.LogError(endLog + $"Command [{command}] failed with exit code {myProcess.ExitCode}");
                    Debug.LogError($"Command [{command}] Output: {output}");
                } else
                {
                    Debug.Log(endLog + $"Command [{command}] executed successfully!");
                    Debug.Log($"Command [{command}] Output: {output}");
                }
 
                return output;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return null;
            }
        }
    }
}

#endif
