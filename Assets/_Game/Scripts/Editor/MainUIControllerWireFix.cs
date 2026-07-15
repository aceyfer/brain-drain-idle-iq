#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.UI;
using BrainDrain.Systems;
using BrainDrain.Core;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Wires MainUIController on Canvas, re-anchors the UI PedestrianContainer to the mock
    /// street band, and deactivates the legacy world-space PedestrianContainer.
    /// Menu: BrainDrain/Wire Main UI Controller
    /// </summary>
    public static class MainUIControllerWireFix
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("BrainDrain/Wire Main UI Controller")]
        public static void WireMainUIController()
        {
            if (EditorToolGuard.BlockedByPlayMode("MainUIControllerWireFix.WireMainUIController")) return;
            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[MainUIControllerWireFix] Canvas not found.");
                return;
            }

            MainUIController mainUI = canvas.GetComponent<MainUIController>();
            if (mainUI == null)
            {
                mainUI = Undo.AddComponent<MainUIController>(canvas);
            }

            ShopUIController shopUI = Object.FindAnyObjectByType<ShopUIController>(FindObjectsInactive.Include);
            ConvertUIController convertUI = Object.FindAnyObjectByType<ConvertUIController>(FindObjectsInactive.Include);

            SerializedObject so = new SerializedObject(mainUI);
            AssignButton(so, "shopButton", "ShopButton");
            AssignButton(so, "convertButton", "ConvertButton");
            AssignButton(so, "restoreButton", "RestoreButton");

            if (shopUI != null)
            {
                so.FindProperty("shopUIController").objectReferenceValue = shopUI;
            }

            if (convertUI != null)
            {
                so.FindProperty("convertUIController").objectReferenceValue = convertUI;
            }

            GameObject shade = GameObject.Find("ShopOverlayShade");
            if (shade == null)
            {
                shade = CreateShopOverlayShade(canvas.transform);
            }

            so.FindProperty("shopOverlayShade").objectReferenceValue = shade;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (shopUI != null)
            {
                SerializedObject shopSo = new SerializedObject(shopUI);
                shopSo.FindProperty("shopButton").objectReferenceValue = null;
                shopSo.ApplyModifiedPropertiesWithoutUndo();
            }

            FixUIPedestrianContainer();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[MainUIControllerWireFix] MainUIController wired and pedestrian container re-anchored.");
        }

        [MenuItem("BrainDrain/Fix Pedestrian Visuals")]
        public static void FixPedestrianVisuals()
        {
            if (EditorToolGuard.BlockedByPlayMode("MainUIControllerWireFix.FixPedestrianVisuals")) return;
            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            FixUIPedestrianContainer();
            ReactivateLegacyPedestrianContainer();
            ForceLegacyPedestriansStageOne();
            UpdateBackgroundPedestrianManagerDefaults();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[MainUIControllerWireFix] Pedestrian visuals restored: legacy container active, Stage 1 locked, BPM scale/wobble updated.");
        }

        [MenuItem("BrainDrain/Wire Background Stage View")]
        public static void WireBackgroundStageView()
        {
            if (EditorToolGuard.BlockedByPlayMode("MainUIControllerWireFix.WireBackgroundStageView")) return;
            if (!EditorSceneManager.GetActiveScene().isLoaded
                || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Transform backgroundRoot = GameObject.Find("Canvas/BackgroundRoot")?.transform;
            if (backgroundRoot == null)
            {
                Debug.LogError("[MainUIControllerWireFix] Canvas/BackgroundRoot not found.");
                return;
            }

            Transform skyline = backgroundRoot.Find("SkylineBG");
            if (skyline == null)
            {
                skyline = backgroundRoot.Find("BottomBG/SkylineBG");
            }

            if (skyline == null)
            {
                Debug.LogError("[MainUIControllerWireFix] SkylineBG not found.");
                return;
            }

            skyline.SetParent(backgroundRoot, false);
            skyline.SetAsFirstSibling();

            RectTransform skylineRect = skyline.GetComponent<RectTransform>();
            if (skylineRect != null)
            {
                skylineRect.anchorMin = Vector2.zero;
                skylineRect.anchorMax = Vector2.one;
                skylineRect.anchoredPosition = Vector2.zero;
                skylineRect.sizeDelta = Vector2.zero;
                skylineRect.pivot = new Vector2(0.5f, 0.5f);
                EditorUtility.SetDirty(skylineRect);
            }

            Image skylineImage = skyline.GetComponent<Image>();
            if (skylineImage != null)
            {
                skylineImage.raycastTarget = false;
                skylineImage.preserveAspect = true;
                EditorUtility.SetDirty(skylineImage);
            }

            BackgroundStageView stageView = skyline.GetComponent<BackgroundStageView>();
            if (stageView == null)
            {
                stageView = Undo.AddComponent<BackgroundStageView>(skyline.gameObject);
            }

            Sprite[] stageSprites = LoadStageBackgroundSprites();
            SerializedObject stageViewSo = new SerializedObject(stageView);
            SerializedProperty spritesProp = stageViewSo.FindProperty("stageSprites");
            spritesProp.arraySize = stageSprites.Length;
            for (int i = 0; i < stageSprites.Length; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = stageSprites[i];
            }

            stageViewSo.ApplyModifiedPropertiesWithoutUndo();

            if (stageSprites.Length > 0 && skylineImage != null)
            {
                skylineImage.sprite = stageSprites[0];
            }

            GameObject restorationBackdrops = GameObject.Find("RestorationBackdrops");
            if (restorationBackdrops != null)
            {
                restorationBackdrops.SetActive(false);
                EditorUtility.SetDirty(restorationBackdrops);
            }

            PatchGameManagerRankDefinitions();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[MainUIControllerWireFix] BackgroundStageView wired: SkylineBG full-screen, BG1–BG6 assigned, RestorationBackdrops deactivated.");
        }

        private static Sprite[] LoadStageBackgroundSprites()
        {
            string[] paths =
            {
                "Assets/_Game/Sprites/Backgrounds/BG1.jpg",
                "Assets/_Game/Sprites/Backgrounds/BG2.png",
                "Assets/_Game/Sprites/Backgrounds/BG3.png",
                "Assets/_Game/Sprites/Backgrounds/BG4.png",
                "Assets/_Game/Sprites/Backgrounds/BG5.png",
                "Assets/_Game/Sprites/Backgrounds/BG6.png",
            };

            Sprite[] sprites = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                if (sprites[i] == null)
                {
                    Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(paths[i]);
                    for (int j = 0; j < subAssets.Length; j++)
                    {
                        if (subAssets[j] is Sprite sprite)
                        {
                            sprites[i] = sprite;
                            break;
                        }
                    }
                }
            }

            return sprites;
        }

        private static void PatchGameManagerRankDefinitions()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gameManager == null)
            {
                Debug.LogWarning("[MainUIControllerWireFix] GameManager not found for rank patch.");
                return;
            }

            SerializedObject so = new SerializedObject(gameManager);
            SerializedProperty ranksProp = so.FindProperty("rankDefinitions");
            ranksProp.arraySize = 6;
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(0), "Cryo Nobody", 0);
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(1), "Unregistered Outcast", 0);
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(2), "Inmate #418293", 500);
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(3), "IQ Test Champion", 5000);
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(4), "Secretary of Interior", 50000);
            SetRankDefinition(ranksProp.GetArrayElementAtIndex(5), "Mr. President", 500000);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);
        }

        private static void SetRankDefinition(SerializedProperty rankProp, string rankName, int threshold)
        {
            rankProp.FindPropertyRelative("rankName").stringValue = rankName;
            rankProp.FindPropertyRelative("threshold").intValue = threshold;
        }

        private static void UpdateBackgroundPedestrianManagerDefaults()
        {
            BackgroundPedestrianManager manager = Object.FindAnyObjectByType<BackgroundPedestrianManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogWarning("[MainUIControllerWireFix] BackgroundPedestrianManager not found.");
                return;
            }

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("pedestrianWidth").floatValue = 140f;
            so.FindProperty("pedestrianHeight").floatValue = 280f;
            so.FindProperty("walkBaselineY").floatValue = 0f;
            so.FindProperty("wobbleVerticalAmplitude").floatValue = 14f;
            so.FindProperty("wobbleRotationAmplitude").floatValue = 16f;
            so.FindProperty("wobbleFrequency").floatValue = 8.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static void ForceLegacyPedestriansStageOne()
        {
            PedestrianWalkController[] walkers = Object.FindObjectsByType<PedestrianWalkController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < walkers.Length; i++)
            {
                PedestrianWalkController walk = walkers[i];
                if (walk == null)
                {
                    continue;
                }

                walk.enabled = true;
                walk.SetStage(1);
                EditorUtility.SetDirty(walk);

                PedestrianWobble wobble = walk.GetComponent<PedestrianWobble>();
                if (wobble != null)
                {
                    wobble.enabled = true;
                    EditorUtility.SetDirty(wobble);
                }

                Animator animator = walk.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                    EditorUtility.SetDirty(animator);
                }

                if (walk.transform.localScale != Vector3.one)
                {
                    walk.transform.localScale = Vector3.one;
                    EditorUtility.SetDirty(walk.transform);
                }
            }
        }

        private static void ReactivateLegacyPedestrianContainer()
        {
            GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "PedestrianContainer" && roots[i].GetComponent<RectTransform>() == null)
                {
                    roots[i].SetActive(true);
                    EditorUtility.SetDirty(roots[i]);
                    return;
                }
            }
        }

        private static void AssignButton(SerializedObject so, string propertyName, string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                Debug.LogWarning($"[MainUIControllerWireFix] Button '{objectName}' not found.");
                return;
            }

            Button button = go.GetComponent<Button>();
            if (button != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = button;
            }
        }

        private static GameObject CreateShopOverlayShade(Transform canvasTransform)
        {
            Transform safeArea = canvasTransform.Find("CustomSafeArea");
            Transform parent = safeArea != null ? safeArea : canvasTransform;

            var shadeGo = new GameObject("ShopOverlayShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(shadeGo, "Create ShopOverlayShade");
            shadeGo.transform.SetParent(parent, false);

            RectTransform rt = shadeGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();

            Image img = shadeGo.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = true;

            Button btn = shadeGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            shadeGo.SetActive(false);
            return shadeGo;
        }

        private static void FixUIPedestrianContainer()
        {
            Transform safeArea = GameObject.Find("Canvas/CustomSafeArea")?.transform;
            if (safeArea == null)
            {
                return;
            }

            Transform container = safeArea.Find("PedestrianContainer");
            if (container == null)
            {
                return;
            }

            RectTransform rt = container.GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            // Mock #street: bottom ~26% band above the bottom nav bar.
            rt.anchorMin = new Vector2(0f, 0.12f);
            rt.anchorMax = new Vector2(1f, 0.38f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }
    }
}
#endif
