using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// PROCEDURAL_DIALOGUE_SPEC.md "Migrate existing dialogue": turns every existing
    /// NarratorLine asset into a zero-slot COGS template that resolves to itself, carrying
    /// forward its original triggerType/buildingId unchanged and computing minStage/maxStage
    /// automatically from its existing minRestorationPercent/maxRestorationPercent via
    /// RestorationStageBands -- never hand-picked per line. Does not delete or reword any
    /// existing NarratorLine asset; this only emits a parallel JSON template file that the
    /// procedural pipeline reads from.
    ///
    /// Idempotent: re-running overwrites the same output file with a fresh migration of
    /// whatever NarratorLine assets currently exist (matches the project's established
    /// idempotent-Editor-tool convention, e.g. ShopPanelLayoutFix).
    /// </summary>
    public static class ProceduralDialogueMigrationTool
    {
        private const string OutputDirectory = "Assets/_Game/Resources/ProceduralDialogue/Templates";
        private const string OutputFileName = "COGS_Migrated.json";

        [MenuItem("BrainDrain/Procedural Dialogue/Migrate Existing NarratorLines")]
        public static void MigrateExistingNarratorLines()
        {
            if (EditorToolGuard.BlockedByPlayMode("ProceduralDialogueMigrationTool.MigrateExistingNarratorLines")) return;

            string[] guids = AssetDatabase.FindAssets("t:NarratorLine");
            var perStageCounts = new int[RestorationStageBands.StageCount];
            var sb = new StringBuilder();
            sb.Append("[\n");

            int migratedCount = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NarratorLine line = AssetDatabase.LoadAssetAtPath<NarratorLine>(path);
                if (line == null)
                {
                    continue;
                }

                RestorationStageBands.ComputeStageRange(line.minRestorationPercent, line.maxRestorationPercent, out int minStage, out int maxStage);

                if (migratedCount > 0)
                {
                    sb.Append(",\n");
                }

                AppendTemplateJson(sb, line, minStage, maxStage);

                for (int stage = minStage; stage <= maxStage; stage++)
                {
                    perStageCounts[stage]++;
                }

                migratedCount++;
            }

            sb.Append("\n]\n");

            Directory.CreateDirectory(OutputDirectory);
            string outputPath = Path.Combine(OutputDirectory, OutputFileName);
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();

            var summary = new StringBuilder();
            summary.Append($"[ProceduralDialogueMigrationTool] Migrated {migratedCount} NarratorLine assets -> {outputPath}. Per-stage counts (COGS):");
            for (int stage = 0; stage < perStageCounts.Length; stage++)
            {
                summary.Append($" stage{stage}={perStageCounts[stage]}");
            }
            Debug.Log(summary.ToString());
        }

        private static void AppendTemplateJson(StringBuilder sb, NarratorLine line, int minStage, int maxStage)
        {
            string id = line.name;
            string triggerType = line.triggerType.ToString();
            string buildingId = line.buildingId ?? string.Empty;

            sb.Append("  {\n");
            sb.Append($"    \"id\": \"{EscapeJsonString(id)}\",\n");
            sb.Append("    \"channel\": \"COGS\",\n");
            sb.Append($"    \"text\": \"{EscapeJsonString(line.dialogueLine)}\",\n");
            sb.Append($"    \"minStage\": {minStage},\n");
            sb.Append($"    \"maxStage\": {maxStage},\n");
            sb.Append("    \"weight\": 1,\n");
            sb.Append("    \"ending\": false,\n");
            sb.Append($"    \"triggerType\": \"{EscapeJsonString(triggerType)}\",\n");
            sb.Append($"    \"buildingId\": \"{EscapeJsonString(buildingId)}\"\n");
            sb.Append("  }");
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
