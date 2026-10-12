#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// 2026-10-12 pre-launch size audit, shipped-build win #2. All 4 CyberWare background-music
    /// tracks (BackgroundMusicManager / SettingsUIController's track picker) were imported as
    /// Decompress On Load with preloadAudioData on and no Android-specific override -- so each
    /// ~20-29MB Vorbis-compressed loop fully decodes into a raw PCM buffer in RAM the instant it's
    /// touched, instead of streaming off disk. Compression format (Vorbis) and quality are left
    /// exactly as authored -- this only changes Load Type, on both the default import settings
    /// and an explicit Android override (nothing was overriding Android before, so it was
    /// silently inheriting the same wrong default), plus preloadAudioData off.
    /// </summary>
    public static class MusicStreamingFix
    {
        private static readonly string[] TargetTracks =
        {
            "Assets/CyberWare - Game Music Assets/Find and Seek/Loops/0120_Find-and-Seek_Full_65bpm4-4_L28M.wav",
            "Assets/CyberWare - Game Music Assets/Gutters Filled with Light/Loops/0110_Gutters-Filled-With-Light_G1-1_65bpm4-4_L28M.wav",
            "Assets/CyberWare - Game Music Assets/Intrusion Detected/Loops/0130_Intrusion-Detected_Full_130bpm4-4_L48M.wav",
            "Assets/CyberWare - Game Music Assets/T.SUM-12/Loops/0140_T.SUM-12_Full_107bpm4-4_L32M.wav",
        };

        [MenuItem("BrainDrain/Fix Music Streaming Settings")]
        public static void Fix()
        {
            if (EditorToolGuard.BlockedByPlayMode("MusicStreamingFix")) { return; }

            int changed = 0;

            foreach (string path in TargetTracks)
            {
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                {
                    Debug.LogWarning($"[MusicStreamingFix] Could not load an AudioImporter at '{path}' -- skipped.");
                    continue;
                }

                // Read the current default settings first so compressionFormat/quality/sample
                // rate are carried forward unchanged -- only loadType is touched. The same
                // struct is then reused verbatim as the Android override, per the explicit ask.
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;

                importer.defaultSampleSettings = settings;
                importer.SetOverrideSampleSettings("Android", settings);
                importer.preloadAudioData = false;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                changed++;
                Debug.Log($"[MusicStreamingFix]   + {path}");
            }

            Debug.Log($"[MusicStreamingFix] Set Load Type = Streaming (default + Android override) and preloadAudioData = false on {changed}/{TargetTracks.Length} tracks. Compression format/quality left untouched.");
        }
    }
}
#endif
