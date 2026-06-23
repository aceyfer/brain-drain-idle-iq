#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// BrainDrain/Testing menu shortcuts for instant progression testing -- the Editor-menu
    /// equivalent of DebugCheatPanel's in-game buttons, sharing the same DebugCheats
    /// implementations so the actual cheat logic exists exactly once.
    /// </summary>
    public static class TestingMenuShortcuts
    {
        [MenuItem("BrainDrain/Testing/Add 10K Brain Power")]
        private static void Add10KBrainPower() => RequirePlayMode(() => DebugCheats.AddBrainPower(10000d));

        [MenuItem("BrainDrain/Testing/Add 1M Brain Power")]
        private static void Add1MBrainPower() => RequirePlayMode(() => DebugCheats.AddBrainPower(1000000d));

        [MenuItem("BrainDrain/Testing/Max All Buildings")]
        private static void MaxAllBuildingsMenuItem() => RequirePlayMode(DebugCheats.MaxAllBuildings);

        [MenuItem("BrainDrain/Testing/Force Rebirth")]
        private static void ForceRebirthMenuItem() => RequirePlayMode(DebugCheats.ForceRebirth);

        [MenuItem("BrainDrain/Testing/Reset Save")]
        private static void ResetSaveMenuItem() => DebugCheats.ClearSave();

        [MenuItem("BrainDrain/Testing/Toggle Persistent Save")]
        private static void TogglePersistentSave()
        {
            bool current = EditorPrefs.GetBool(SaveManager.KeepSaveEditorPrefsKey, true);
            EditorPrefs.SetBool(SaveManager.KeepSaveEditorPrefsKey, !current);
            Debug.Log($"[TestingMenuShortcuts] Persistent save ({SaveManager.KeepSaveEditorPrefsKey}) is now {(!current ? "ON -- Play Mode will keep your save" : "OFF -- Play Mode will start fresh each time")}.");
        }

        /// <summary>
        /// Most cheats reach into live singletons that only exist once Play Mode has actually
        /// started -- calling them outside Play Mode would either no-op (a null Instance) or,
        /// worse, self-bootstrap a stray manager GameObject sitting in Edit mode permanently.
        /// </summary>
        private static void RequirePlayMode(System.Action action)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestingMenuShortcuts] This only works in Play Mode.");
                return;
            }

            action();
        }
    }
}
#endif
