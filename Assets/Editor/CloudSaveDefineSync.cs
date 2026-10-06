#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-06 COMPILE BLOCKER FIX: this project deliberately has no .asmdef files (CLAUDE.md
    /// -- "all scripts compile into the default Assembly-CSharp assembly"), so Unity's normal
    /// versionDefines mechanism (an asmdef-only feature: "define a symbol automatically when
    /// package X is present") isn't available for Assembly-CSharp code. This is the
    /// Assembly-CSharp-compatible equivalent, specifically so FreezeInventoryCloudSync.cs can
    /// compile whether or not com.unity.services.cloudsave happens to be installed/resolved --
    /// "the game must never be uncompilable because of an optional package again" (Aceyfer,
    /// 2026-10-06).
    ///
    /// Queries Package Manager once per domain reload (offline/cached list -- no network round
    /// trip, this only needs to know what's ALREADY resolved, not re-resolve anything) and keeps
    /// BRAINDRAIN_CLOUDSAVE in sync with whether com.unity.services.cloudsave is actually part of
    /// the resolved package set. Only touches PlayerSettings (triggering a recompile) on an
    /// actual state change -- installing or removing the package later is picked up automatically
    /// on the next domain reload, no manual define editing ever needed again.
    /// </summary>
    [InitializeOnLoad]
    public static class CloudSaveDefineSync
    {
        private const string DefineSymbol = "BRAINDRAIN_CLOUDSAVE";
        private const string PackageName = "com.unity.services.cloudsave";

        private static ListRequest listRequest;

        static CloudSaveDefineSync()
        {
            listRequest = Client.List(offlineMode: true, includeIndirectDependencies: true);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (listRequest == null || !listRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= Poll;

            bool installed = false;
            if (listRequest.Status == StatusCode.Success)
            {
                foreach (UnityEditor.PackageManager.PackageInfo info in listRequest.Result)
                {
                    if (info.name == PackageName)
                    {
                        installed = true;
                        break;
                    }
                }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                Debug.LogWarning($"[CloudSaveDefineSync] Package list query failed ({listRequest.Error?.message}) -- leaving {DefineSymbol} as-is this session.");
                listRequest = null;
                return;
            }

            SyncDefine(installed);
            listRequest = null;
        }

        private static void SyncDefine(bool installed)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(group);
            string current = PlayerSettings.GetScriptingDefineSymbols(target);

            var symbols = new List<string>(current.Split(';', System.StringSplitOptions.RemoveEmptyEntries));
            bool has = symbols.Contains(DefineSymbol);

            if (installed && !has)
            {
                symbols.Add(DefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
                Debug.Log($"[CloudSaveDefineSync] {PackageName} resolved -- added {DefineSymbol} scripting define. FreezeInventoryCloudSync will recompile with real Cloud Save calls.");
            }
            else if (!installed && has)
            {
                symbols.Remove(DefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
                Debug.LogWarning($"[CloudSaveDefineSync] {PackageName} is NOT resolved -- removed {DefineSymbol} scripting define. FreezeInventoryCloudSync falls back to local-only (see its own one-time warning log at runtime).");
            }
        }
    }
}
#endif
