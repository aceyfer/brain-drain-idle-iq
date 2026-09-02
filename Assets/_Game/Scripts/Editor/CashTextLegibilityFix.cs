#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Gives CashText a dedicated TMP underlay material (Oswald_DarkUnderlay.mat) instead of
    /// relying on tuning AtmosphereOverlay's haze alpha to stay out of its way. A soft, centered
    /// (zero-offset) dark halo drawn behind the glyphs stays legible against any background tint
    /// -- haze, pedestrians, whatever's behind it -- regardless of the exact alpha value, which
    /// is the more robust fix Aceyfer asked for after the 0.34->0.20 alpha cut alone wasn't
    /// enough at a small Editor Game view size.
    ///
    /// Oswald_DarkUnderlay.mat is a clone of this project's own existing
    /// Assets/_Game/Materials/TMP/Oswald_CyanGlow.mat (already live on the "0 BP" readout, same
    /// UNDERLAY_ON technique, proven working in this exact project) with _UnderlayColor swapped
    /// from cyan to near-opaque black -- same _UnderlayDilate/_UnderlaySoftness (0.35/0.55) as
    /// that proven precedent, not invented from scratch.
    ///
    /// Only CashText's own m_sharedMaterial reference changes. The font's bundled default
    /// material (Oswald Bold SDF's own embedded material, used by every other text object in the
    /// project that doesn't override it) is untouched -- confirmed via a guid reference count in
    /// the scene file before this tool was written, so this is a zero-blast-radius, per-object
    /// change, not a shared-asset edit.
    /// </summary>
    public static class CashTextLegibilityFix
    {
        private const string MaterialPath = "Assets/_Game/Materials/TMP/Oswald_DarkUnderlay.mat";

        [MenuItem("BrainDrain/Fix Cash Text Legibility (Dark Underlay)")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("CashTextLegibilityFix.Fix")) return;

            Transform cashTextTransform = FindInSceneIncludingInactive("CashText");
            if (cashTextTransform == null)
            {
                Debug.LogWarning("[CashTextLegibilityFix] No 'CashText' found in the scene -- aborting.");
                return;
            }

            TextMeshProUGUI tmp = cashTextTransform.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogWarning("[CashTextLegibilityFix] 'CashText' has no TextMeshProUGUI component -- aborting.");
                return;
            }

            Material darkUnderlay = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (darkUnderlay == null)
            {
                Debug.LogWarning($"[CashTextLegibilityFix] Could not load material at '{MaterialPath}' -- aborting.");
                return;
            }

            tmp.fontSharedMaterial = darkUnderlay;
            EditorUtility.SetDirty(tmp);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[CashTextLegibilityFix] CashText now uses Oswald_DarkUnderlay. Save the scene (Ctrl+S) to persist.");
        }

        // Same inactive-safe lookup as WeatherSystemWireFix -- GameObject.Find only sees active
        // objects, and this project already hit that exact bug once this session (WorldRoot).
        private static Transform FindInSceneIncludingInactive(string name)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindRecursive(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform current, string name)
        {
            if (current.name == name)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindRecursive(current.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
