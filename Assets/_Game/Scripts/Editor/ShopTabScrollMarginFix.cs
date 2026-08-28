#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// One-shot scene fix for the big gap between the shop tab bar and the item list on all
    /// three shop tabs. Root cause: ShopUIController.EnsureThreeTabLayout() early-returns once
    /// bpTabPanel/cashTabPanel/rpTabPanel/bpContent/cashContent/rpContent are all wired (they
    /// already are), so its StretchRect()/-56-offset construction logic never runs on this
    /// scene -- whatever RectTransform values are saved is exactly what renders.
    ///
    /// Read directly from SampleScene.unity (not assumed from code): Tab_BP_ScrollView,
    /// Tab_Cash_ScrollView, and Tab_RP_ScrollView (NOT "CashInvestmentsScrollView"/
    /// "GodShopScrollView" -- those are the names EnsureRuntimeClonedPanel would assign if it
    /// ever ran on this scene, which it hasn't) all currently carry identical stretch-anchor
    /// values (anchorMin/Max 0,0-1,1, anchoredPosition (0,-110), sizeDelta (0,-470)), which
    /// works out to offsetMin.y = 125 (bottom margin, correct -- clears the bottom nav) and
    /// offsetMax.y = -345 (top margin -- 345px of dead space above the first item, way more
    /// than the 56px-tall ShopTabBar needs).
    ///
    /// Fix: for each of the 3 scroll views, set offsetMax.y to -66 (56px tab bar + a 10px
    /// buffer, matching EnsureRuntimeTabPanel/EnsureRuntimeClonedPanel's own -56 convention for
    /// newly-built panels) while leaving offsetMin.y (bottom margin) untouched -- RectTransform.
    /// offsetMin/offsetMax are real settable properties, so this doesn't require hand-deriving
    /// anchoredPosition/sizeDelta.
    ///
    /// Idempotent: setting offsetMax.y to -66 a second time is a no-op. Looks up each scroll
    /// view by name via Resources.FindObjectsOfTypeAll so it's found regardless of whether the
    /// shop panel happens to be active or inactive when this runs.
    /// </summary>
    public static class ShopTabScrollMarginFix
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const float TargetTopOffset = -66f;

        private static readonly string[] ScrollViewNames =
        {
            "Tab_BP_ScrollView",
            "Tab_Cash_ScrollView",
            "Tab_RP_ScrollView",
        };

        [MenuItem("BrainDrain/Fix Shop Tab Scroll View Margins")]
        public static void FixShopTabScrollViewMargins()
        {
            if (EditorToolGuard.BlockedByPlayMode("ShopTabScrollMarginFix.FixShopTabScrollViewMargins")) return;

            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int fixedCount = 0;
            foreach (string name in ScrollViewNames)
            {
                Transform scrollView = FindInSceneByName(name);
                if (scrollView == null)
                {
                    Debug.LogWarning($"[ShopTabScrollMarginFix] Could not find '{name}' in the scene.");
                    continue;
                }

                RectTransform rect = scrollView.GetComponent<RectTransform>();
                if (rect == null)
                {
                    Debug.LogWarning($"[ShopTabScrollMarginFix] '{name}' has no RectTransform.");
                    continue;
                }

                Vector2 beforeMin = rect.offsetMin;
                Vector2 beforeMax = rect.offsetMax;

                Vector2 newMax = rect.offsetMax;
                newMax.y = TargetTopOffset;
                rect.offsetMax = newMax;

                EditorUtility.SetDirty(rect);
                fixedCount++;

                Debug.Log($"[ShopTabScrollMarginFix] {name}: offsetMin {beforeMin} -> {rect.offsetMin} (unchanged), offsetMax {beforeMax} -> {rect.offsetMax}");
            }

            if (fixedCount == 0)
            {
                Debug.LogWarning("[ShopTabScrollMarginFix] No scroll views were fixed -- nothing to save.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[ShopTabScrollMarginFix] Fixed {fixedCount}/{ScrollViewNames.Length} shop tab scroll views. Scene saved.");
        }

        /// <summary>
        /// Finds a Transform by exact name anywhere in the loaded scene, including inactive
        /// GameObjects -- GameObject.Find only searches active objects, which isn't reliable
        /// here since the shop panel's active state at Edit time isn't guaranteed.
        /// </summary>
        private static Transform FindInSceneByName(string name)
        {
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in all)
            {
                if (t.name == name && !EditorUtility.IsPersistent(t.gameObject) && t.hideFlags == HideFlags.None)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
#endif
