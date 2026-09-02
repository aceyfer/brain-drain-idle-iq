#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Gives CashText a dedicated TMP underlay material (Oswald_GoldUnderlay.mat) instead of
    /// relying on tuning AtmosphereOverlay's haze alpha to stay out of its way. TMP's underlay is
    /// a soft dilated glow behind the glyph, not an opaque backing plate -- so a bright, fully
    /// saturated color contrasts against nearly any backdrop, while a dark/near-black underlay
    /// (this fix's first attempt) only helps against a solid light background and can blend
    /// straight into a dark scene, which is what was actually happening. Root-caused by Aceyfer
    /// via a byte-for-byte diff against Oswald_CyanGlow.mat (proven working on "0 BP") that
    /// isolated _UnderlayColor as the only differing property.
    ///
    /// Oswald_GoldUnderlay.mat -- renamed from Oswald_DarkUnderlay.mat, same GUID preserved via
    /// git mv so any already-serialized scene reference stays valid -- is still a clone of this
    /// project's own existing Assets/_Game/Materials/TMP/Oswald_CyanGlow.mat, but now with
    /// _UnderlayColor matching CyanGlow's own brightness/alpha recipe (fully saturated, alpha 1)
    /// just a gold/amber hue instead of cyan, so CashText isn't visually identical to BPPSText.
    /// _UnderlayDilate/_UnderlaySoftness (0.35/0.55) are unchanged -- confirmed not the problem.
    ///
    /// Only CashText's own m_sharedMaterial reference changes. The font's bundled default
    /// material (Oswald Bold SDF's own embedded material, used by every other text object in the
    /// project that doesn't override it) is untouched -- confirmed via a guid reference count in
    /// the scene file before this tool was written, so this is a zero-blast-radius, per-object
    /// change, not a shared-asset edit.
    /// </summary>
    public static class CashTextLegibilityFix
    {
        private const string MaterialPath = "Assets/_Game/Materials/TMP/Oswald_GoldUnderlay.mat";

        [MenuItem("BrainDrain/Fix Cash Text Legibility (Gold Underlay)")]
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

            Material goldUnderlay = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (goldUnderlay == null)
            {
                Debug.LogWarning($"[CashTextLegibilityFix] Could not load material at '{MaterialPath}' -- aborting.");
                return;
            }

            tmp.fontSharedMaterial = goldUnderlay;
            EditorUtility.SetDirty(tmp);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[CashTextLegibilityFix] CashText now uses Oswald_GoldUnderlay. Save the scene (Ctrl+S) to persist.");
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
