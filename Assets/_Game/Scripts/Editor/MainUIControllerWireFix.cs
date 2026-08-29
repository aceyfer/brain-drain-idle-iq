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

            bool usesFullScreenShop = UsesCurrentFullScreenShop();
            GameObject shade = null;
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform candidate in allTransforms)
            {
                if (!candidate.gameObject.scene.IsValid()
                    || !candidate.gameObject.scene.isLoaded
                    || candidate.name != "ShopOverlayShade") continue;

                if (usesFullScreenShop)
                {
                    Undo.DestroyObjectImmediate(candidate.gameObject);
                }
                else if (shade == null)
                {
                    shade = candidate.gameObject;
                }
            }

            if (!usesFullScreenShop && shade == null)
            {
                shade = CreateShopOverlayShade(canvas.transform);
            }

            // The current ShopRoot fills CustomSafeArea, so the old mock's click-to-close
            // dimmer would be invisible behind it (or block it if placed above it).
            so.FindProperty("shopOverlayShade").objectReferenceValue = usesFullScreenShop ? null : shade;
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
            Debug.Log(usesFullScreenShop
                ? "[MainUIControllerWireFix] MainUIController wired for the full-screen shop (legacy overlay omitted) and pedestrian container re-anchored."
                : "[MainUIControllerWireFix] MainUIController wired and pedestrian container re-anchored.");
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

        private static bool UsesCurrentFullScreenShop()
        {
            Transform shopRoot = null;
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform candidate in allTransforms)
            {
                if (candidate.gameObject.scene.IsValid()
                    && candidate.gameObject.scene.isLoaded
                    && candidate.name == "ShopRoot")
                {
                    shopRoot = candidate;
                    break;
                }
            }

            if (shopRoot == null
                || shopRoot.Find("Tab_BP") == null
                || shopRoot.Find("Tab_Cash") == null
                || shopRoot.Find("Tab_RP") == null)
            {
                return false;
            }

            RectTransform rect = shopRoot.GetComponent<RectTransform>();
            return rect != null
                && rect.anchorMin == Vector2.zero
                && rect.anchorMax == Vector2.one
                && rect.anchoredPosition == Vector2.zero
                && rect.sizeDelta == Vector2.zero;
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
