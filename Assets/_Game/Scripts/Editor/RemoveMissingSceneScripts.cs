#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Removes all missing-script MonoBehaviour components from every GameObject
    /// in the active scene. Safe to re-run — idempotent when nothing is missing.
    ///
    /// Menu: BrainDrain/Testing/Remove Missing Scene Scripts
    /// </summary>
    public static class RemoveMissingSceneScripts
    {
        [MenuItem("BrainDrain/Testing/Remove Missing Scene Scripts")]
        public static void RemoveMissingScripts()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] allObjects = scene.GetRootGameObjects();

            int totalRemoved = 0;
            int goCount = 0;

            foreach (GameObject root in allObjects)
            {
                totalRemoved += ProcessRecursive(root, ref goCount);
            }

            if (totalRemoved == 0)
            {
                Debug.Log("[RemoveMissingSceneScripts] No missing-script components found. Scene is clean.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RemoveMissingSceneScripts] Removed {totalRemoved} missing-script component(s) " +
                      $"from {goCount} GameObject(s). Scene saved.");
        }

        private static int ProcessRecursive(GameObject go, ref int goCount)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                goCount++;
                Debug.Log($"[RemoveMissingSceneScripts]   {go.name}: removed {removed} missing script(s)");
            }

            foreach (Transform child in go.transform)
            {
                removed += ProcessRecursive(child.gameObject, ref goCount);
            }

            return removed;
        }
    }
}
#endif
