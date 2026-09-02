#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BrainDrain.Systems;
using BrainDrain.UI;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Builds and wires §61's weather system (ambient smog/haze + random rain bursts) into the
    /// active scene: idempotent, same pattern as ArtExpansionTool -- finds and updates existing
    /// objects by name on re-run rather than duplicating.
    ///
    /// Inserts one new top-level Canvas child, "AtmosphereOverlay", immediately after WorldRoot
    /// (above the backdrop/pedestrians, below CustomSafeArea's HUD/buttons so those stay fully
    /// legible and clickable through the haze). RainOverlay nests inside it as a raindrop pool
    /// container -- see RainEffectView's class doc for why this is UI Images rather than either
    /// installed rain ParticleSystem package (Assets/Rain Particles, Assets/Rainy VFX): this
    /// project's whole visible game is one Screen Space - Overlay Canvas with no compositing
    /// camera, so a camera-rendered ParticleSystem from either package would render behind the
    /// Canvas and never be visible. Rain Particles' own streak texture is copied in and reused
    /// as a UI sprite instead, since it inspected as the more complete/usable of the two (real
    /// dedicated Rain Sprite/Rain Ground Hit textures and materials, vs Rainy VFX's base prefab
    /// relying on Unity's generic built-in particle material).
    /// </summary>
    public static class WeatherSystemWireFix
    {
        private const string GeneratedArtFolder = "Assets/_Game/Sprites/UI/Generated";
        private const string RainStreakSourcePath = "Assets/Rain Particles/Textures/Rain Sprite.png";
        private const string RainStreakDestPath = GeneratedArtFolder + "/RainStreak.png";
        private const int DropCount = 28;

        [MenuItem("BrainDrain/Fix Weather System (Smog + Rain)")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("WeatherSystemWireFix.Fix")) return;

            GameObject worldRoot = GameObject.Find("WorldRoot");
            if (worldRoot == null || worldRoot.transform.parent == null)
            {
                Debug.LogWarning("[WeatherSystemWireFix] No 'WorldRoot' found under the Canvas -- cannot place AtmosphereOverlay. Aborting.");
                return;
            }

            Transform canvasTransform = worldRoot.transform.parent;

            GameObject atmosphereOverlay = FindOrCreateChild(canvasTransform, "AtmosphereOverlay", out bool atmosphereCreated);
            SetFullScreenRect(atmosphereOverlay);
            atmosphereOverlay.transform.SetSiblingIndex(worldRoot.transform.GetSiblingIndex() + 1);

            Image hazeImage = atmosphereOverlay.GetComponent<Image>();
            if (hazeImage == null) { hazeImage = atmosphereOverlay.AddComponent<Image>(); }
            hazeImage.sprite = null;
            hazeImage.type = Image.Type.Simple;
            hazeImage.raycastTarget = false;

            WeatherAtmosphereView atmosphereView = atmosphereOverlay.GetComponent<WeatherAtmosphereView>();
            if (atmosphereView == null) { atmosphereView = atmosphereOverlay.AddComponent<WeatherAtmosphereView>(); }

            GameObject rainOverlay = FindOrCreateChild(atmosphereOverlay.transform, "RainOverlay", out bool rainCreated);
            SetFullScreenRect(rainOverlay);

            Sprite rainStreak = ImportRainStreakSprite();
            Image[] drops = BuildDropPool(rainOverlay.transform, rainStreak);

            RainEffectView rainEffectView = rainOverlay.GetComponent<RainEffectView>();
            if (rainEffectView == null) { rainEffectView = rainOverlay.AddComponent<RainEffectView>(); }
            AssignObjectField(rainEffectView, "dropContainer", rainOverlay.GetComponent<RectTransform>());
            AssignArrayField(rainEffectView, "drops", drops);

            GameObject weatherManagerHost = GameObject.Find("WeatherManager");
            if (weatherManagerHost == null)
            {
                weatherManagerHost = new GameObject("WeatherManager");
                Undo.RegisterCreatedObjectUndo(weatherManagerHost, "Create WeatherManager");
            }

            WeatherManager weatherManager = weatherManagerHost.GetComponent<WeatherManager>();
            if (weatherManager == null) { weatherManager = weatherManagerHost.AddComponent<WeatherManager>(); }
            AssignObjectField(weatherManager, "rainView", rainEffectView);

            EditorUtility.SetDirty(atmosphereOverlay);
            EditorUtility.SetDirty(rainOverlay);
            EditorUtility.SetDirty(weatherManagerHost);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WeatherSystemWireFix] Done (AtmosphereOverlay {(atmosphereCreated ? "created" : "found")}, RainOverlay {(rainCreated ? "created" : "found")}, {drops.Length} drops). Save the scene (Ctrl+S) to persist.");
        }

        private static GameObject FindOrCreateChild(Transform parent, string name, out bool created)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                created = false;
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            created = true;
            return go;
        }

        private static void SetFullScreenRect(GameObject go)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null) { rect = go.AddComponent<RectTransform>(); }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Sprite ImportRainStreakSprite()
        {
            Directory.CreateDirectory(GeneratedArtFolder);

            if (!File.Exists(RainStreakDestPath))
            {
                AssetDatabase.CopyAsset(RainStreakSourcePath, RainStreakDestPath);
                AssetDatabase.ImportAsset(RainStreakDestPath, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(RainStreakDestPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(RainStreakDestPath);
        }

        private static Image[] BuildDropPool(Transform parent, Sprite streakSprite)
        {
            var drops = new Image[DropCount];
            for (int i = 0; i < DropCount; i++)
            {
                string name = $"Drop_{i:00}";
                Transform existing = parent.Find(name);
                GameObject dropObject = existing != null ? existing.gameObject : null;
                if (dropObject == null)
                {
                    dropObject = new GameObject(name, typeof(RectTransform));
                    dropObject.transform.SetParent(parent, false);
                    Undo.RegisterCreatedObjectUndo(dropObject, "Create " + name);
                }

                RectTransform rect = dropObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(6f, 42f);

                Image image = dropObject.GetComponent<Image>();
                if (image == null) { image = dropObject.AddComponent<Image>(); }
                image.sprite = streakSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.raycastTarget = false;

                drops[i] = image;
            }

            return drops;
        }

        /// <summary>Overwrites a serialized single-object field via SerializedObject -- safe regardless of the field's C# access level. Same technique ArtExpansionTool.AssignArrayField uses, generalized for a single reference.</summary>
        private static void AssignObjectField(Component component, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[WeatherSystemWireFix] Could not find serialized field '{fieldName}' on {component.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void AssignArrayField(Component component, string fieldName, Object[] items)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty arrayProp = so.FindProperty(fieldName);
            if (arrayProp == null)
            {
                Debug.LogWarning($"[WeatherSystemWireFix] Could not find serialized field '{fieldName}' on {component.GetType().Name}.");
                return;
            }

            arrayProp.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedProperties();
        }
    }
}
#endif
