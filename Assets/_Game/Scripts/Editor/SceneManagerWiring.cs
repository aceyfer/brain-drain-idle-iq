#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Places RandomEventManager and DialogueManager as real GameObjects under the scene's
    /// existing "_Systems" parent (instead of relying on their self-bootstrapping Instance
    /// getters, which create an inert auto-instance with empty lists), and populates their
    /// asset-pool lists with every matching asset currently on disk via SerializedObject --
    /// safe regardless of the fields' private access level, and avoids hand-editing the
    /// .unity YAML directly. Idempotent: re-running replaces each list wholesale rather than
    /// appending, so it can't accumulate duplicates across repeated runs.
    /// </summary>
    public static class SceneManagerWiring
    {
        private const string SystemsParentName = "_Systems";

        [MenuItem("BrainDrain/Wire Scene Managers/RandomEventManager + DialogueManager")]
        public static void WireSceneManagers()
        {
            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent == null)
            {
                systemsParent = new GameObject(SystemsParentName);
                Undo.RegisterCreatedObjectUndo(systemsParent, "Create " + SystemsParentName);
            }

            RandomEventManager eventManager = PlaceUnderParent<RandomEventManager>(systemsParent.transform);
            DialogueManager dialogueManager = PlaceUnderParent<DialogueManager>(systemsParent.transform);

            List<BrainRotEventData> events = FindAllAssets<BrainRotEventData>();
            List<NarratorLine> lines = FindAllAssets<NarratorLine>();

            AssignListToComponent(eventManager, "potentialEvents", ToObjectList(events));
            AssignListToComponent(dialogueManager, "narratorLines", ToObjectList(lines));

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[SceneManagerWiring] Wired RandomEventManager ({events.Count} events) and DialogueManager ({lines.Count} lines) under '{SystemsParentName}', and saved the scene.");
        }

        private static T PlaceUnderParent<T>(Transform parent) where T : Component
        {
            T existing = Object.FindAnyObjectByType<T>();
            if (existing != null)
            {
                existing.transform.SetParent(parent, false);
                return existing;
            }

            var host = new GameObject(typeof(T).Name);
            host.transform.SetParent(parent, false);
            T component = host.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(host, "Create " + typeof(T).Name);
            return component;
        }

        private static List<T> FindAllAssets<T>() where T : Object
        {
            var results = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }
            return results;
        }

        private static List<Object> ToObjectList<T>(List<T> items) where T : Object
        {
            var result = new List<Object>(items.Count);
            result.AddRange(items);
            return result;
        }

        private static void AssignListToComponent(Component target, string fieldName, List<Object> items)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(target);
            SerializedProperty listProp = so.FindProperty(fieldName);
            if (listProp == null)
            {
                Debug.LogWarning($"[SceneManagerWiring] Could not find serialized field '{fieldName}' on {target.GetType().Name}.");
                return;
            }

            listProp.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
