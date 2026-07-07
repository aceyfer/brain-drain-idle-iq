using System;
using System.IO;
using System.Text;
using BrainDrain.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BrainDrain.EditorTools
{
    [InitializeOnLoad]
    public static class Phase2AThreeIssueEditor
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string PedestrianFolder = "Assets/_Game/Sprites/Pedestrians";
        private const string MarkerPath = "C:/Users/aceyf/Documents/Codex/2026-07-05/brain-drain-phase-2a-cleanup-editor/work/run-three-issue.marker";
        private const string DonePath = "C:/Users/aceyf/Documents/Codex/2026-07-05/brain-drain-phase-2a-cleanup-editor/work/three-issue-report-v2.txt";

        static Phase2AThreeIssueEditor()
        {
            EditorApplication.update += RunIfRequested;
        }

        private static void RunIfRequested()
        {
            if (!File.Exists(MarkerPath) || File.Exists(DonePath))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= RunIfRequested;
            Run();
        }

        public static void Run()
        {
            StringBuilder report = new StringBuilder();

            // Fresh disk load is required before saving; stale in-memory scenes can clobber prior fixes.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            report.AppendLine("[Issue 1] Visual-only image disables");
            DisableImage("Canvas/COGSWorldPortraitUI", report);
            DisableImage("Canvas/CustomSafeArea/PlayerCharacter_Anchor", report);
            report.AppendLine();

            report.AppendLine("[Issue 2] BackgroundPedestrianManager sprite pools (read-only)");
            ReportPedestrianPools(report);
            report.AppendLine();

            report.AppendLine("[Issue 3] Stage backdrop fit");
            FitStageBackdrops(report);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            File.WriteAllText(DonePath, report.ToString());
        }

        private static void DisableImage(string path, StringBuilder report)
        {
            GameObject go = GameObject.Find(path);
            if (go == null)
            {
                report.AppendLine($"{path}: not found");
                return;
            }

            go.SetActive(true);

            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                report.AppendLine($"{path}: no Image component");
                return;
            }

            image.enabled = false;
            image.raycastTarget = false;
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(image);

            report.AppendLine($"{path}: activeSelf={go.activeSelf}, Image.enabled={image.enabled}, raycastTarget={image.raycastTarget}");
        }

        private static void ReportPedestrianPools(StringBuilder report)
        {
            BackgroundPedestrianManager manager = UnityEngine.Object.FindAnyObjectByType<BackgroundPedestrianManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                report.AppendLine("BackgroundPedestrianManager: not found");
                return;
            }

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty dystopian = so.FindProperty("dystopianPedestrianSprites");
            SerializedProperty utopian = so.FindProperty("utopianPedestrianSprites");

            report.AppendLine($"Manager path: {GetPath(manager.transform)}");
            AppendSpriteArray(report, "dystopianPedestrianSprites", dystopian);
            AppendSpriteArray(report, "utopianPedestrianSprites", utopian);

            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { PedestrianFolder });
            Array.Sort(spriteGuids, StringComparer.OrdinalIgnoreCase);
            report.AppendLine("Available pedestrian sprites:");
            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                report.AppendLine($"- {sprite?.name ?? Path.GetFileNameWithoutExtension(path)} :: {path}");
            }
        }

        private static void AppendSpriteArray(StringBuilder report, string label, SerializedProperty property)
        {
            if (property == null || !property.isArray)
            {
                report.AppendLine($"{label}: property not found");
                return;
            }

            report.AppendLine($"{label}[{property.arraySize}]:");
            for (int i = 0; i < property.arraySize; i++)
            {
                UnityEngine.Object sprite = property.GetArrayElementAtIndex(i).objectReferenceValue;
                report.AppendLine($"  [{i}] {(sprite != null ? sprite.name : "null")} ({(sprite != null ? AssetDatabase.GetAssetPath(sprite) : "n/a")})");
            }
        }

        private static void FitStageBackdrops(StringBuilder report)
        {
            Camera camera = Camera.main ?? GameObject.Find("Main Camera")?.GetComponent<Camera>();
            Transform root = GameObject.Find("RestorationBackdrops")?.transform;
            if (camera == null || root == null)
            {
                report.AppendLine($"camera found={camera != null}, RestorationBackdrops found={root != null}");
                return;
            }

            if (!camera.orthographic)
            {
                report.AppendLine("Main Camera is not orthographic; no backdrop fit applied.");
                return;
            }

            float cameraHeight = camera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * camera.aspect;
            Vector3 cameraCenter = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);

            report.AppendLine($"Camera view: width={cameraWidth:F3}, height={cameraHeight:F3}, y=[{camera.transform.position.y - camera.orthographicSize:F3}, {camera.transform.position.y + camera.orthographicSize:F3}]");

            Transform stage1 = root.Find("Stage1_Backdrop");
            AppendBackdropBefore(report, "Stage1 before", stage1, cameraHeight);

            for (int i = 1; i <= 6; i++)
            {
                Transform stage = root.Find($"Stage{i}_Backdrop");
                if (stage == null)
                {
                    report.AppendLine($"Stage{i}_Backdrop: missing");
                    continue;
                }

                SpriteRenderer renderer = stage.GetComponent<SpriteRenderer>();
                Sprite sprite = GetSerializedSprite(renderer);
                if (renderer == null || sprite == null)
                {
                    report.AppendLine($"{GetPath(stage)}: no SpriteRenderer or sprite; not changed");
                    continue;
                }

                Vector2 spriteWorldSize = sprite.bounds.size;
                float scale = Mathf.Max(cameraWidth / spriteWorldSize.x, cameraHeight / spriteWorldSize.y);
                stage.position = new Vector3(cameraCenter.x, cameraCenter.y, stage.position.z);
                stage.localScale = new Vector3(scale, scale, stage.localScale.z);
                renderer.sortingOrder = -20;
                renderer.sprite = sprite;

                EditorUtility.SetDirty(stage);
                EditorUtility.SetDirty(renderer);
                report.AppendLine($"{GetPath(stage)} after: position={stage.position}, scale={stage.localScale}, spriteWorld={spriteWorldSize}, sortingOrder={renderer.sortingOrder}");
            }
        }

        private static void AppendBackdropBefore(StringBuilder report, string label, Transform stage, float cameraHeight)
        {
            if (stage == null)
            {
                report.AppendLine($"{label}: missing");
                return;
            }

            SpriteRenderer renderer = stage.GetComponent<SpriteRenderer>();
            Sprite sprite = GetSerializedSprite(renderer);
            if (renderer == null || sprite == null)
            {
                report.AppendLine($"{label}: {GetPath(stage)} has no SpriteRenderer or sprite; position={stage.position}, scale={stage.localScale}");
                return;
            }

            Bounds bounds = renderer.bounds;
            report.AppendLine($"{label}: path={GetPath(stage)}, position={stage.position}, scale={stage.localScale}, spriteWorld={sprite.bounds.size}, boundsHeight={bounds.size.y:F3}, cameraHeight={cameraHeight:F3}, sortingOrder={renderer.sortingOrder}");
        }

        private static Sprite GetSerializedSprite(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            if (renderer.sprite != null)
            {
                return renderer.sprite;
            }

            SerializedObject so = new SerializedObject(renderer);
            SerializedProperty spriteProperty = so.FindProperty("m_Sprite");
            return spriteProperty != null ? spriteProperty.objectReferenceValue as Sprite : null;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
