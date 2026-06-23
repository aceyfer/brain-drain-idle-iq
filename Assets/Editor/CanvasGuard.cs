#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Enforces Screen Space - Overlay on every Canvas in the active scene. This project is a
    /// 2D mobile UI game -- Screen Space - Camera/World Space break both rendering and click
    /// hit-testing when the assigned camera isn't a dedicated UI camera, and the main Canvas has
    /// been silently flipped to Screen Space - Camera (pointed at the gameplay Main Camera)
    /// multiple times this session despite no code anywhere setting it. Hooks both
    /// EditorApplication.hierarchyChanged and EditorSceneManager.sceneSaved so a regression is
    /// caught and reverted as soon as either fires, rather than waiting for the next manual fix.
    /// </summary>
    public static class CanvasGuard
    {
        private static bool isApplyingFix;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private static void OnHierarchyChanged()
        {
            EnforceOverlayOnAllCanvases();
        }

        private static void OnSceneSaved(Scene scene)
        {
            EnforceOverlayOnAllCanvases();
        }

        /// <summary>
        /// Finds every Canvas in the active scene (not just ones named "Canvas" -- simpler and
        /// more robust than name-filtering, and this project has no Canvas that's legitimately
        /// meant to be anything other than Overlay). Any Canvas found in Camera or World Space
        /// mode gets logged and reverted.
        /// </summary>
        private static void EnforceOverlayOnAllCanvases()
        {
            // Guard against re-entrancy: reverting + saving from inside OnSceneSaved would
            // otherwise trigger another sceneSaved event. Harmless on its own (the second pass
            // finds nothing left to fix), but skipping it outright is cleaner.
            if (isApplyingFix)
            {
                return;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool revertedAny = false;

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                Debug.LogWarning($"[CanvasGuard] Canvas render mode was changed on {canvas.gameObject.name} — reverting to Overlay");

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                EditorUtility.SetDirty(canvas);
                revertedAny = true;
            }

            if (!revertedAny)
            {
                return;
            }

            isApplyingFix = true;
            try
            {
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            finally
            {
                isApplyingFix = false;
            }
        }
    }
}
#endif
