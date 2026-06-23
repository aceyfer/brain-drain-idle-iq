#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
namespace BrainforgeGames.MobileUXToolkit
{
    public static class SceneHotkeysShortcuts
    {
        private const string ConfigAssetPath = "Assets/Editor/SceneHotkeyConfig.asset";

        [Shortcut("Brainforge/Open Scene F1", KeyCode.F1, ShortcutModifiers.Shift)]
        private static void OpenF1() => OpenIndex(0, "F1");

        [Shortcut("Brainforge/Open Scene F2", KeyCode.F2, ShortcutModifiers.Shift)]
        private static void OpenF2() => OpenIndex(1, "F2");

        [Shortcut("Brainforge/Open Scene F3", KeyCode.F3, ShortcutModifiers.Shift)]
        private static void OpenF3() => OpenIndex(2, "F3");

        [Shortcut("Brainforge/Open Scene F4", KeyCode.F4, ShortcutModifiers.Shift)]
        private static void OpenF4() => OpenIndex(3, "F4");

        [Shortcut("Brainforge/Open Scene F5", KeyCode.F5, ShortcutModifiers.Shift)]
        private static void OpenF5() => OpenIndex(4, "F5");

        [Shortcut("Brainforge/Open Scene F6", KeyCode.F6, ShortcutModifiers.Shift)]
        private static void OpenF6() => OpenIndex(5, "F6");

        [Shortcut("Brainforge/Open Scene F7", KeyCode.F7, ShortcutModifiers.Shift)]
        private static void OpenF7() => OpenIndex(6, "F7");

        [Shortcut("Brainforge/Open Scene F8", KeyCode.F8, ShortcutModifiers.Shift)]
        private static void OpenF8() => OpenIndex(7, "F8");

        [Shortcut("Brainforge/Open Scene F9", KeyCode.F9, ShortcutModifiers.Shift)]
        private static void OpenF9() => OpenIndex(8, "F9");

        [Shortcut("Brainforge/Open Scene F10", KeyCode.F10, ShortcutModifiers.Shift)]
        private static void OpenF10() => OpenIndex(9, "F10");

        [Shortcut("Brainforge/Open Scene F11", KeyCode.F11, ShortcutModifiers.Shift)]
        private static void OpenF11() => OpenIndex(10, "F11");

        [Shortcut("Brainforge/Open Scene F12", KeyCode.F12, ShortcutModifiers.Shift)]
        private static void OpenF12() => OpenIndex(11, "F12");

        private static void OpenIndex(int index, string keyName)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<SceneHotkeyConfig>(ConfigAssetPath);

            if (cfg == null || cfg.scenes == null ||
                index >= cfg.scenes.Count || cfg.scenes[index] == null)
            {
                Debug.LogWarning($"[SceneHotkeys] No scene assigned for Shift+{keyName}");
                return;
            }

            var path = AssetDatabase.GetAssetPath(cfg.scenes[index]);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }
}
#endif