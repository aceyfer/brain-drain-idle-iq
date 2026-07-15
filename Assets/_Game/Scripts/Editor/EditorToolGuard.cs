using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared play-mode guard for BrainDrain Editor menu tools (TASKLIST SS8). Scene-mutating
/// Editor tools must never run during (or entering) Play mode -- runtime code owns
/// presentation state (Bible SS8), and an Editor-time mutation mid-Play both fights the
/// runtime asserts and can bake transient Play state into a save. Same check pair
/// AutoSceneFixes already uses. Global namespace on purpose: reachable from every Editor
/// tool without adding usings. Menu entry points are synchronous user invocations, so a
/// single entry check suffices here; the delayCall re-check + try/catch chokepoint pattern
/// (see AutoSceneFixes) applies only to deferred execution paths.
/// </summary>
internal static class EditorToolGuard
{
    /// <summary>Returns true (and logs a warning) if the Editor is in or entering Play mode.</summary>
    internal static bool BlockedByPlayMode(string toolName)
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"[{toolName}] Blocked: Editor tools can't run in or entering Play mode (SS8 guard).");
            return true;
        }

        return false;
    }
}
