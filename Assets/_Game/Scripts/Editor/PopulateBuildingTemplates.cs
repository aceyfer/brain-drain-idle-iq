#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Finds every BuildingData asset under Assets/_Game/Buildings/ and adds any that are
    /// missing from UpgradeManager.buildingTemplates. Run once after adding new building assets.
    /// Menu: BrainDrain/Populate Building Templates
    /// </summary>
    public static class PopulateBuildingTemplates
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string BuildingsFolder = "Assets/_Game/Buildings";

        [MenuItem("BrainDrain/Populate Building Templates")]
        public static void Populate()
        {
            if (EditorToolGuard.BlockedByPlayMode("PopulateBuildingTemplates.Populate")) return;
            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            UpgradeManager manager = Object.FindAnyObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogError("[PopulateBuildingTemplates] UpgradeManager not found in scene.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:BuildingData", new[] { BuildingsFolder });
            var allBuildings = new List<BuildingData>(guids.Length);
            foreach (string guid in guids)
            {
                BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data != null) allBuildings.Add(data);
            }

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty templates = so.FindProperty("buildingTemplates");

            var existingPaths = new HashSet<string>();
            for (int i = 0; i < templates.arraySize; i++)
            {
                Object existing = templates.GetArrayElementAtIndex(i).objectReferenceValue;
                if (existing != null) existingPaths.Add(AssetDatabase.GetAssetPath(existing));
            }

            int added = 0;
            foreach (BuildingData data in allBuildings)
            {
                string path = AssetDatabase.GetAssetPath(data);
                if (existingPaths.Contains(path)) continue;

                int idx = templates.arraySize;
                templates.arraySize = idx + 1;
                templates.GetArrayElementAtIndex(idx).objectReferenceValue = data;
                existingPaths.Add(path);
                added++;
                Debug.Log($"[PopulateBuildingTemplates] Added: {data.buildingName}");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[PopulateBuildingTemplates] Done. Added {added} building(s). Total in list: {templates.arraySize}.");
        }
    }
}
#endif
