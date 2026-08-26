using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using BrainDrain.Systems;

namespace BrainDrain.EditorTools
{
    /// <summary>
    /// PROCEDURAL_DIALOGUE_SPEC.md "Tooling": for every stage (0-5) x channel combination,
    /// asserts at least one legal template exists, and for every word-bank category referenced
    /// by those templates' slot syntax, asserts at least one legal word exists in that same
    /// stage window. Fails loudly (one Debug.LogError per gap, same convention as
    /// NarratorLineValidator) rather than silently leaving a combination that would resolve to
    /// nothing at runtime.
    ///
    /// Wired into the same "BrainDrain/Validate/" menu group as NarratorLineValidator.
    /// Manual-invocation only, matching that tool's own reasoning: no import-time validation
    /// cost on every asset change.
    /// </summary>
    public static class ProceduralDialogueValidator
    {
        private static readonly Regex SlotCategoryPattern = new(
            @"\{(?:\^)?(?:a\s+)?([A-Za-z_][A-Za-z0-9_]*)(?:\+)?(?::\d+)?\}",
            RegexOptions.Compiled);

        [MenuItem("BrainDrain/Validate/Procedural Dialogue")]
        public static void Validate()
        {
            if (EditorToolGuard.BlockedByPlayMode("ProceduralDialogueValidator.Validate")) return;

            Dictionary<string, List<WordBankEntry>> wordBanks = ProceduralDialogueLoader.LoadWordBanks();
            List<DialogueTemplate> templates = ProceduralDialogueLoader.LoadTemplates();

            int errorCount = 0;

            foreach (DialogueChannel channel in (DialogueChannel[])Enum.GetValues(typeof(DialogueChannel)))
            {
                for (int stage = 0; stage < RestorationStageBands.StageCount; stage++)
                {
                    List<DialogueTemplate> eligibleTemplates = templates.FindAll(t =>
                        t.Channel == channel && stage >= t.MinStage && stage <= t.MaxStage);

                    if (eligibleTemplates.Count == 0)
                    {
                        Debug.LogError($"[ProceduralDialogueValidator] No legal template exists for channel={channel}, stage={stage}.");
                        errorCount++;
                        continue;
                    }

                    HashSet<string> referencedCategories = new();
                    foreach (DialogueTemplate template in eligibleTemplates)
                    {
                        foreach (Match match in SlotCategoryPattern.Matches(template.Text))
                        {
                            referencedCategories.Add(match.Groups[1].Value);
                        }
                    }

                    foreach (string category in referencedCategories)
                    {
                        bool hasLegalWord = wordBanks.TryGetValue(category, out List<WordBankEntry> entries)
                            && entries.Exists(e => stage >= e.minStage && stage <= e.maxStage);

                        if (!hasLegalWord)
                        {
                            Debug.LogError($"[ProceduralDialogueValidator] Category '{category}' (referenced by a channel={channel} stage={stage} template) has no legal word in that stage window.");
                            errorCount++;
                        }
                    }
                }
            }

            if (errorCount == 0)
            {
                Debug.Log($"[ProceduralDialogueValidator] Clean: {templates.Count} templates across {wordBanks.Count} word bank categories cover every stage (0-{RestorationStageBands.StageCount - 1}) x channel combination, all referenced categories resolvable.");
            }
            else
            {
                Debug.LogError($"[ProceduralDialogueValidator] {errorCount} coverage gap(s) found. See errors above.");
            }
        }
    }
}
