using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Manual-invocation validator for NarratorLine.buildingId against live BuildingData ids.
    /// A typo'd or stale buildingId produces no feedback today -- the line just goes dead, no
    /// error, no warning. This tool makes that case loud: a per-asset Debug.LogError, with the
    /// asset as context so the console entry pings it. An empty buildingId is a separate,
    /// legal wildcard on BuildingPurchase lines (matches every building) -- the generic pool
    /// lines and the Illumisnotty BuildingPurchase pool use this deliberately and permanently,
    /// so it is NOT logged per-asset (that would be ~19 warnings on every clean run, which
    /// trains you to ignore the tool). It's folded into the summary count instead, so a clean
    /// project produces exactly one console line and anything louder than that is a real
    /// signal. Deliberately not hooked into AssetPostprocessor/OnPostprocessAllAssets -- no
    /// validation cost on every asset import, menu-invoked only.
    /// </summary>
    public static class NarratorLineValidator
    {
        [MenuItem("BrainDrain/Validate/Narrator Lines")]
        public static void ValidateNarratorLines()
        {
            if (EditorToolGuard.BlockedByPlayMode("NarratorLineValidator.ValidateNarratorLines")) return;

            string[] buildingGuids = AssetDatabase.FindAssets("t:BuildingData");
            HashSet<string> validBuildingIds = new HashSet<string>();
            foreach (string guid in buildingGuids)
            {
                BuildingData building = AssetDatabase.LoadAssetAtPath<BuildingData>(AssetDatabase.GUIDToAssetPath(guid));
                if (building != null && !string.IsNullOrEmpty(building.buildingId))
                {
                    validBuildingIds.Add(building.buildingId);
                }
            }

            string[] lineGuids = AssetDatabase.FindAssets("t:NarratorLine");
            int errorCount = 0;
            int wildcardBuildingPurchaseCount = 0;

            foreach (string guid in lineGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NarratorLine line = AssetDatabase.LoadAssetAtPath<NarratorLine>(path);
                if (line == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(line.buildingId) && !validBuildingIds.Contains(line.buildingId))
                {
                    Debug.LogError($"[NarratorLineValidator] {path} has buildingId '{line.buildingId}' which does not match any live BuildingData.buildingId.", line);
                    errorCount++;
                }

                if (line.triggerType == NarratorTriggerType.BuildingPurchase && string.IsNullOrEmpty(line.buildingId))
                {
                    // Legal, intentional wildcard behavior (generic "you bought something" pool
                    // lines) -- not worth a per-asset warning, since every one of these is
                    // deliberate and permanent. Counted into the summary instead so a genuinely
                    // clean project still produces exactly one console line, not a wall of
                    // ignorable noise.
                    wildcardBuildingPurchaseCount++;
                }
            }

            Debug.Log($"[NarratorLineValidator] Scanned {lineGuids.Length} NarratorLines against {buildingGuids.Length} BuildingDatas. {errorCount} errors. {wildcardBuildingPurchaseCount} BuildingPurchase lines are intentional wildcards.");
        }
    }
}
