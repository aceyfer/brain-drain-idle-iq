#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainDrain.Core;
using BrainDrain.Systems;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Collection of one-shot visual polish fixes that wire art assets into scene components
    /// via SerializedObject. Each item is idempotent — safe to re-run.
    ///
    /// Menu: BrainDrain/Visual Polish/...
    /// </summary>
    public static class VisualPolishFix
    {
        // ── 1. AnimationController sprite wiring ──────────────────────────────────
        // AnimationController previously loaded tap VFX sprites only inside
        // #if UNITY_EDITOR via AssetDatabase, so builds silently got procedural
        // white/pink shapes instead of the real art. The class now has [SerializeField]
        // fields for these sprites; this tool wires them to the existing art assets.

        [MenuItem("BrainDrain/Visual Polish/Wire AnimationController Sprites")]
        public static void WireAnimationControllerSprites()
        {
            AnimationController ac = Object.FindAnyObjectByType<AnimationController>(FindObjectsInactive.Include);
            if (ac == null)
            {
                Debug.LogError("[VisualPolishFix] AnimationController not found in scene.");
                return;
            }

            Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Game/Sprites/UI/Generated/NeonRing.png");
            Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Game/Sprites/UI/Generated/RadialGlowGreen.png");
            Material floatingTextMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Game/Materials/TMP/Bangers_PinkShadow.mat");

            Object[] splatAssets = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/_Game/Sprites/Particles/GooSplats.png");
            Sprite[] splatSprites = splatAssets.OfType<Sprite>().ToArray();

            if (ringSprite == null)  Debug.LogWarning("[VisualPolishFix] NeonRing.png not found.");
            if (glowSprite == null)  Debug.LogWarning("[VisualPolishFix] RadialGlowGreen.png not found.");
            if (floatingTextMat == null) Debug.LogWarning("[VisualPolishFix] Bangers_PinkShadow.mat not found.");
            if (splatSprites.Length == 0) Debug.LogWarning("[VisualPolishFix] No GooSplat sprites found.");

            SerializedObject so = new SerializedObject(ac);

            SetObjectProperty(so, "_neonRingSprite", ringSprite);
            SetObjectProperty(so, "_radialGlowSprite", glowSprite);
            SetObjectProperty(so, "_floatingTextMaterial", floatingTextMat);

            SerializedProperty splatArray = so.FindProperty("_gooSplatSprites");
            if (splatArray != null)
            {
                splatArray.arraySize = splatSprites.Length;
                for (int i = 0; i < splatSprites.Length; i++)
                {
                    splatArray.GetArrayElementAtIndex(i).objectReferenceValue = splatSprites[i];
                }
            }
            else
            {
                Debug.LogWarning("[VisualPolishFix] _gooSplatSprites property not found on AnimationController. " +
                                 "Recompile may be needed after adding the field.");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ac);
            SaveScene("[VisualPolishFix] AnimationController sprites wired.");
        }

        // ── 2. HUD celebration overlay wiring ────────────────────────────────────
        // HUDController.hudCanvasGroup and HUDController.celebrationFlashOverlay
        // are both {fileID:0}. Without them, PlayHighIQCelebration does nothing.
        // This tool:
        //   a) Ensures a CanvasGroup exists on the HUD root and wires hudCanvasGroup.
        //   b) Creates a full-screen "CelebrationFlashOverlay" Image on the root Canvas
        //      (alpha 0, non-raycast) and wires celebrationFlashOverlay.
        // Idempotent: finds existing "CelebrationFlashOverlay" by name on re-run.

        [MenuItem("BrainDrain/Visual Polish/Wire HUD Celebration Overlay")]
        public static void WireHUDCelebrationOverlay()
        {
            HUDController hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogError("[VisualPolishFix] HUDController not found in scene.");
                return;
            }

            // a) CanvasGroup on HUD root
            CanvasGroup hudCG = hud.GetComponent<CanvasGroup>();
            if (hudCG == null)
            {
                hudCG = hud.gameObject.AddComponent<CanvasGroup>();
            }

            // b) CelebrationFlashOverlay Image on the root Canvas
            Canvas rootCanvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (rootCanvas == null)
            {
                Debug.LogError("[VisualPolishFix] No Canvas found in scene.");
                return;
            }

            const string overlayName = "CelebrationFlashOverlay";
            Transform existing = rootCanvas.transform.Find(overlayName);
            GameObject overlayGO;
            if (existing != null)
            {
                overlayGO = existing.gameObject;
            }
            else
            {
                overlayGO = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlayGO.transform.SetParent(rootCanvas.transform, false);
            }

            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImage = overlayGO.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0f);
            overlayImage.raycastTarget = false;

            // Wire both fields via SerializedObject
            SerializedObject so = new SerializedObject(hud);
            SetObjectProperty(so, "hudCanvasGroup", hudCG);
            SetObjectProperty(so, "celebrationFlashOverlay", overlayImage);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);
            SaveScene("[VisualPolishFix] HUD celebration overlay wired.");
        }

        // ── 3. Wire PlayerTapHandler particle container + tap button visual ───────
        // particleContainer and tapButtonVisual are both {fileID:0} on PlayerTapHandler.
        // Without particleContainer, tap VFX spawns relative to the wrong rect.
        // This tool finds the first Canvas and wires particleContainer to its
        // RectTransform, and wires tapButtonVisual to the TapButton's own RectTransform
        // so PlayButtonPunch has a target.

        [MenuItem("BrainDrain/Visual Polish/Wire TapHandler Particle Container")]
        public static void WireTapHandlerParticleContainer()
        {
            PlayerTapHandler tapHandler = Object.FindAnyObjectByType<PlayerTapHandler>(FindObjectsInactive.Include);
            if (tapHandler == null)
            {
                Debug.LogError("[VisualPolishFix] PlayerTapHandler not found in scene.");
                return;
            }

            // Find the root Canvas
            Canvas rootCanvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (rootCanvas == null)
            {
                Debug.LogError("[VisualPolishFix] No Canvas found in scene.");
                return;
            }

            // Find MainTapButton by name for the tapButtonVisual
            GameObject tapButtonGO = GameObject.Find("MainTapButton");
            if (tapButtonGO == null)
            {
                // Try alternative names
                tapButtonGO = GameObject.Find("TapButton");
            }

            SerializedObject so = new SerializedObject(tapHandler);
            SetObjectProperty(so, "particleContainer", rootCanvas.GetComponent<RectTransform>());

            if (tapButtonGO != null)
            {
                SetObjectProperty(so, "tapButtonVisual", tapButtonGO.transform);
            }
            else
            {
                Debug.LogWarning("[VisualPolishFix] Could not find 'MainTapButton' or 'TapButton' in scene. " +
                                 "Wire tapButtonVisual manually.");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tapHandler);
            SaveScene("[VisualPolishFix] TapHandler particle container wired.");
        }

        // ── Run All ───────────────────────────────────────────────────────────────

        [MenuItem("BrainDrain/Visual Polish/Run All Polish Fixes")]
        public static void RunAll()
        {
            WireAnimationControllerSprites();
            WireHUDCelebrationOverlay();
            WireTapHandlerParticleContainer();
            Debug.Log("[VisualPolishFix] All polish fixes applied.");
        }

        [MenuItem("BrainDrain/Fix Static UI Raycasts")]
        public static void FixStaticUIRaycasts()
        {
            Transform safeArea = GameObject.Find("Canvas/CustomSafeArea")?.transform;
            if (safeArea == null)
            {
                Debug.LogError("[VisualPolishFix] Canvas/CustomSafeArea not found.");
                return;
            }

            int changed = DisableStaticRaycastsUnder(safeArea);
            SaveScene($"[VisualPolishFix] Disabled raycastTarget on {changed} static UI graphic(s) under CustomSafeArea.");
        }

        private static int DisableStaticRaycastsUnder(Transform root)
        {
            int changed = 0;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (ShouldKeepRaycastTarget(image.gameObject))
                {
                    continue;
                }

                if (image.raycastTarget)
                {
                    image.raycastTarget = false;
                    EditorUtility.SetDirty(image);
                    changed++;
                }
            }

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                if (ShouldKeepRaycastTarget(label.gameObject))
                {
                    continue;
                }

                if (label.raycastTarget)
                {
                    label.raycastTarget = false;
                    EditorUtility.SetDirty(label);
                    changed++;
                }
            }

            return changed;
        }

        private static bool ShouldKeepRaycastTarget(GameObject go)
        {
            if (go.GetComponent<Button>() != null)
            {
                return true;
            }

            if (go.GetComponent<ScrollRect>() != null)
            {
                return true;
            }

            if (go.name == "MainTapButton" || go.name == "ShopOverlayShade")
            {
                return true;
            }

            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void SetObjectProperty(SerializedObject so, string propertyPath, Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyPath);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"[VisualPolishFix] Property '{propertyPath}' not found on {so.targetObject.GetType().Name}. " +
                                 "Recompile scripts and retry.");
            }
        }

        private static void SaveScene(string logMessage)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(logMessage);
        }
    }
}
#endif
