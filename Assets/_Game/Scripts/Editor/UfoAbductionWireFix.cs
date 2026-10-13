#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// Wires the UFO abduction ambient event (2026-09-22, Aceyfer's "keeping it funny" pedestrian
    /// idea) into the active scene: idempotent, same pattern as WeatherSystemWireFix/
    /// RewardedAdRecoveryWireFix -- finds and updates an existing host by name on re-run rather
    /// than duplicating.
    ///
    /// Places "UfoAbductionController" under the scene's "_Systems" parent, matching
    /// SceneManagerWiring's/RewardedAdRecoveryWireFix's own parenting convention, and wires its
    /// pedestrianManager field to whatever BackgroundPedestrianManager already exists in the
    /// scene. UfoAbductionController builds its own UFO/beam GameObjects entirely at runtime (see
    /// that class's doc comment).
    ///
    /// Updated 2026-09-22, same day as first built: Aceyfer's first real Play Mode look found the
    /// UFO "popping up halfway on the screen" instead of descending from off the top, and no
    /// visible beam. Root cause was parenting everything under
    /// BackgroundPedestrianManager.ContainerRect, which is only the narrow pedestrian walking-band
    /// strip, not the full screen. This tool now also creates/finds "AbductionOverlay" -- a
    /// genuinely full-screen RectTransform, same SetFullScreenRect technique
    /// WeatherSystemWireFix.cs already uses for its "AtmosphereOverlay" -- and wires it to
    /// UfoAbductionController's new fullScreenOverlay field.
    ///
    /// Updated again 2026-09-23: that fix made the beam and cartwheeling pedestrian visible, but
    /// Aceyfer then reported "the beam works and the player abduction happens but no ufo visable."
    /// AbductionOverlay was placed right after AtmosphereOverlay -- deliberately still *below*
    /// CustomSafeArea (the HUD/buttons layer) in Canvas child order. The UFO hovers high on screen
    /// (UfoAbductionController.ufoHoverHeightFraction), right behind the game's opaque top HUD bar,
    /// which lives in CustomSafeArea and renders in front of anything earlier in sibling order --
    /// the beam/pedestrian sit low enough to clear that bar, the UFO never could. This tool now
    /// places AbductionOverlay right after CustomSafeArea instead (see PlaceAfter below), so the
    /// whole abduction renders above the HUD.
    /// </summary>
    public static class UfoAbductionWireFix
    {
        private const string SystemsParentName = "_Systems";

        [MenuItem("BrainDrain/Fix UFO Abduction Event")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("UfoAbductionWireFix.Fix")) return;

            BackgroundPedestrianManager pedestrianManager = Object.FindAnyObjectByType<BackgroundPedestrianManager>();
            if (pedestrianManager == null)
            {
                Debug.LogWarning("[UfoAbductionWireFix] No BackgroundPedestrianManager found in the scene -- cannot wire the abduction controller. Aborting.");
                return;
            }

            // Added 2026-09-22 (testing-feedback fix): the UFO/beam need a genuinely full-screen
            // parent, not pedestrianManager's own short walking-band strip. Same placement logic
            // WeatherSystemWireFix uses for AtmosphereOverlay -- find WorldRoot to get the Canvas,
            // create the overlay as WorldRoot's sibling, full-screen anchors.
            Transform worldRootTransform = FindInSceneIncludingInactive("WorldRoot");
            GameObject worldRoot = worldRootTransform != null ? worldRootTransform.gameObject : null;
            RectTransform abductionOverlayRect = null;
            if (worldRoot == null || worldRoot.transform.parent == null)
            {
                Debug.LogWarning("[UfoAbductionWireFix] No 'WorldRoot' found under the Canvas -- cannot place AbductionOverlay. The controller will still be wired, but abductions will no-op until this is fixed (fullScreenOverlay will be left unassigned).");
            }
            else
            {
                Transform canvasTransform = worldRoot.transform.parent;
                GameObject abductionOverlay = FindOrCreateChild(canvasTransform, "AbductionOverlay", out bool overlayCreated);
                SetFullScreenRect(abductionOverlay);

                // Placed right after CustomSafeArea (2026-09-23 fix -- see class doc comment) so the
                // whole abduction, UFO included, renders in FRONT of the HUD/buttons instead of
                // behind them. Falls back to right after AtmosphereOverlay, then WorldRoot, if
                // CustomSafeArea doesn't exist in this scene for some reason.
                Transform placementReference = canvasTransform.Find("CustomSafeArea");
                if (placementReference == null)
                {
                    placementReference = canvasTransform.Find("AtmosphereOverlay");
                }
                if (placementReference == null)
                {
                    placementReference = worldRoot.transform;
                }
                PlaceAfter(abductionOverlay.transform, placementReference);

                abductionOverlayRect = abductionOverlay.GetComponent<RectTransform>();
                EditorUtility.SetDirty(abductionOverlay);

                Debug.Log($"[UfoAbductionWireFix] AbductionOverlay {(overlayCreated ? "created" : "found")}.");
            }

            GameObject systemsParent = FindInSceneIncludingInactive(SystemsParentName)?.gameObject;
            if (systemsParent == null)
            {
                systemsParent = new GameObject(SystemsParentName);
                Undo.RegisterCreatedObjectUndo(systemsParent, "Create " + SystemsParentName);
            }

            UfoAbductionController controller = Object.FindAnyObjectByType<UfoAbductionController>();
            GameObject host;
            if (controller != null)
            {
                host = controller.gameObject;
                host.transform.SetParent(systemsParent.transform, false);
            }
            else
            {
                host = new GameObject(nameof(UfoAbductionController));
                host.transform.SetParent(systemsParent.transform, false);
                Undo.RegisterCreatedObjectUndo(host, "Create " + nameof(UfoAbductionController));
                controller = host.AddComponent<UfoAbductionController>();
            }

            AssignObjectField(controller, "pedestrianManager", pedestrianManager);
            if (abductionOverlayRect != null)
            {
                AssignObjectField(controller, "fullScreenOverlay", abductionOverlayRect);
            }

            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UfoAbductionWireFix] Done -- UfoAbductionController built/updated under _Systems and wired to the scene's BackgroundPedestrianManager and AbductionOverlay. Save the scene (Ctrl+S) to persist.");
        }

        /// <summary>GameObject.Find only sees active objects -- walks every scene root's hierarchy
        /// directly (including inactive ones) to find a GameObject by exact name. Same helper as
        /// WeatherSystemWireFix/RewardedAdRecoveryWireFix, needed here since _Systems could exist
        /// but happen to be inactive.</summary>
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

        /// <summary>Added 2026-09-22 for AbductionOverlay -- same helper WeatherSystemWireFix uses
        /// for AtmosphereOverlay/RainOverlay: finds an existing named child by exact name, or
        /// creates a new bare RectTransform GameObject under the given parent.</summary>
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

        /// <summary>Added 2026-09-22 for AbductionOverlay -- same helper WeatherSystemWireFix uses:
        /// stretches a RectTransform to exactly match its parent's rect (anchors 0-1, zero offset),
        /// which is what makes rect.width/height genuinely mean "the whole screen" rather than
        /// whatever the parent's own extent happens to be.</summary>
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

        /// <summary>
        /// Moves toMove to sit immediately after reference in the same parent's sibling order --
        /// added 2026-09-23 replacing a plain "reference.GetSiblingIndex() + 1" call, which breaks
        /// on a re-run once toMove already exists somewhere BEFORE reference: SetSiblingIndex
        /// removes toMove from its old slot first, which shifts every later sibling (reference
        /// included) down by one, so "+1" then overshoots by one slot. Reading reference's index
        /// before the move and comparing it to toMove's own current index corrects for that shift
        /// either way (toMove currently before reference, after it, or brand new/appended at the end).
        /// </summary>
        private static void PlaceAfter(Transform toMove, Transform reference)
        {
            int referenceIndex = reference.GetSiblingIndex();
            int currentIndex = toMove.GetSiblingIndex();
            int targetIndex = currentIndex < referenceIndex ? referenceIndex : referenceIndex + 1;
            toMove.SetSiblingIndex(targetIndex);
        }

        /// <summary>Overwrites a serialized single-object field via SerializedObject -- safe
        /// regardless of the field's C# access level. Same technique every other WireFix tool in
        /// this project uses.</summary>
        private static void AssignObjectField(Component component, string fieldName, Object value)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[UfoAbductionWireFix] Could not find serialized field '{fieldName}' on {component.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
