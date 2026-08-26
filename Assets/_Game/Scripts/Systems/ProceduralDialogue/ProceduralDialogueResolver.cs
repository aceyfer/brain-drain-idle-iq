using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Resolves a fully-formed line of dialogue from the loaded template/word-bank pool
    /// (PROCEDURAL_DIALOGUE_SPEC.md "Resolver"). Standalone: this class has no MonoBehaviour
    /// dependency and doesn't touch DialogueManager -- Phase 1 only builds the resolver itself,
    /// exercised via the Editor preview window (step 4), not yet wired into the live game.
    ///
    /// Slot syntax inside a template's text: {category}, {category:N} (numbered instance --
    /// same N within one resolution = same word, different N = guaranteed distinct where the
    /// word bank has enough eligible entries to allow it), {category+} (plural), {a category}
    /// (article + word), {^category} (capitalize). Modifiers combine in a fixed order:
    /// ^, then "a ", then the category name, then + or :N, e.g. {^a animal:1}.
    /// </summary>
    public sealed class ProceduralDialogueResolver
    {
        /// <summary>Mirrors DialogueManager.MaxHistoryEntries -- same cap, same reasoning (a bounded log, not unbounded growth).</summary>
        private const int MaxResolvedInstanceHistory = 50;

        /// <summary>Mirrors DialogueManager.TryFireLine's last-10 lookback window for anti-repeat.</summary>
        private const int RecentLookbackWindow = 10;

        private const int MaxResolutionAttempts = 10;

        private static readonly Regex SlotPattern = new(
            @"\{(\^)?(a\s+)?([A-Za-z_][A-Za-z0-9_]*)(\+)?(:(\d+))?\}",
            RegexOptions.Compiled);

        private readonly Dictionary<string, List<WordBankEntry>> wordBanksByCategory;
        private readonly List<DialogueTemplate> templates;

        /// <summary>
        /// Second lookback list alongside DialogueManager's own `history` (SS20b) -- extends the
        /// exact same pattern (capped list + last-N lookback) rather than a new data structure,
        /// but tracks resolved template-INSTANCE strings (the actual generated text, which
        /// differs run to run for slotted templates) instead of NarratorLine references.
        /// </summary>
        private readonly List<string> resolvedInstanceHistory = new();

        private readonly System.Random defaultRng = new();

        public ProceduralDialogueResolver(Dictionary<string, List<WordBankEntry>> wordBanksByCategory, List<DialogueTemplate> templates)
        {
            this.wordBanksByCategory = wordBanksByCategory ?? new Dictionary<string, List<WordBankEntry>>();
            this.templates = templates ?? new List<DialogueTemplate>();
        }

        /// <summary>Convenience factory: loads word banks + templates from Resources and constructs a resolver over them.</summary>
        public static ProceduralDialogueResolver LoadFromResources()
        {
            return new ProceduralDialogueResolver(ProceduralDialogueLoader.LoadWordBanks(), ProceduralDialogueLoader.LoadTemplates());
        }

        /// <summary>
        /// Resolves one line of dialogue. Never returns null or an empty string
        /// (PROCEDURAL_DIALOGUE_SPEC.md "Resolver"): if anti-repeat filtering would leave
        /// nothing after MaxResolutionAttempts tries, the filter is dropped and the last
        /// attempt is accepted rather than looping forever or failing.
        /// </summary>
        /// <param name="seed">Optional. Passing the same seed (and the same resolver state) reproduces the same output, for repro'ing a bad line.</param>
        public string Resolve(DialogueChannel channel, int stage, NarratorTriggerType triggerType, string buildingId = null, int? seed = null)
        {
            System.Random rng = seed.HasValue ? new System.Random(seed.Value) : defaultRng;

            List<DialogueTemplate> candidates = templates.Where(t =>
                t.Channel == channel
                && !t.Ending
                && t.TriggerType == triggerType
                && stage >= t.MinStage && stage <= t.MaxStage
                && (string.IsNullOrWhiteSpace(t.BuildingId) || t.BuildingId == buildingId)
            ).ToList();

            if (candidates.Count == 0)
            {
                Debug.LogError($"[ProceduralDialogueResolver] No eligible templates for channel={channel} stage={stage} trigger={triggerType} buildingId='{buildingId}'.");
                return "...";
            }

            string firstAttempt = null;
            for (int attempt = 0; attempt < MaxResolutionAttempts; attempt++)
            {
                DialogueTemplate template = WeightedPick(candidates, t => t.Weight, rng);
                string resolved = ResolveTemplateText(template, stage, rng);
                firstAttempt ??= resolved;

                if (!WasRecentlyUsed(resolved))
                {
                    RecordUsage(resolved);
                    return resolved;
                }
            }

            // Widen: every attempt collided with recent history -- drop the anti-repeat filter
            // rather than ever returning null/empty.
            RecordUsage(firstAttempt);
            return firstAttempt;
        }

        /// <summary>
        /// Preview-only: resolves one line filtered by channel+stage alone (ignoring
        /// triggerType/buildingId), backing the Editor preview window's "choose stage and
        /// channel, dump 50 resolved lines" tool (PROCEDURAL_DIALOGUE_SPEC.md "Tooling").
        /// Deliberately does not touch the anti-repeat history Resolve() uses -- a preview dump
        /// is meant to show the pool's actual breadth, not simulate a live play session.
        /// </summary>
        public string ResolvePreview(DialogueChannel channel, int stage, int? seed = null)
        {
            System.Random rng = seed.HasValue ? new System.Random(seed.Value) : defaultRng;

            List<DialogueTemplate> candidates = templates.Where(t =>
                t.Channel == channel
                && !t.Ending
                && stage >= t.MinStage && stage <= t.MaxStage
            ).ToList();

            if (candidates.Count == 0)
            {
                return "...";
            }

            DialogueTemplate template = WeightedPick(candidates, t => t.Weight, rng);
            return ResolveTemplateText(template, stage, rng);
        }

        private string ResolveTemplateText(DialogueTemplate template, int stage, System.Random rng)
        {
            var assignedByCategoryAndN = new Dictionary<(string category, int n), WordBankEntry>();
            var usedEntryIdsByCategory = new Dictionary<string, HashSet<string>>();

            return SlotPattern.Replace(template.Text, match =>
            {
                bool capitalize = match.Groups[1].Success;
                bool useArticle = match.Groups[2].Success;
                string category = match.Groups[3].Value;
                bool plural = match.Groups[4].Success;
                int n = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : -1;
                bool numbered = n >= 0;

                WordBankEntry entry = ResolveWordSlot(category, stage, numbered, n, assignedByCategoryAndN, usedEntryIdsByCategory, rng);
                if (entry == null)
                {
                    Debug.LogError($"[ProceduralDialogueResolver] Template '{template.Id}' references category '{category}' with no eligible word at stage {stage}.");
                    return $"[{category}]";
                }

                string word = plural ? entry.plural : entry.text;
                if (useArticle)
                {
                    word = $"{entry.article} {word}".Trim();
                }
                if (capitalize && word.Length > 0)
                {
                    word = char.ToUpperInvariant(word[0]) + word.Substring(1);
                }

                return word;
            });
        }

        private WordBankEntry ResolveWordSlot(
            string category,
            int stage,
            bool numbered,
            int n,
            Dictionary<(string, int), WordBankEntry> assignedByCategoryAndN,
            Dictionary<string, HashSet<string>> usedEntryIdsByCategory,
            System.Random rng)
        {
            if (numbered && assignedByCategoryAndN.TryGetValue((category, n), out WordBankEntry cached))
            {
                return cached;
            }

            if (!wordBanksByCategory.TryGetValue(category, out List<WordBankEntry> entries) || entries.Count == 0)
            {
                return null;
            }

            List<WordBankEntry> eligible = entries.Where(e => stage >= e.minStage && stage <= e.maxStage).ToList();
            if (eligible.Count == 0)
            {
                return null;
            }

            if (numbered && usedEntryIdsByCategory.TryGetValue(category, out HashSet<string> used) && used.Count > 0)
            {
                // Guaranteed-distinct is best-effort: if the word bank is too small to give every
                // numbered instance its own word, fall back to the full eligible pool rather than failing.
                List<WordBankEntry> distinct = eligible.Where(e => !used.Contains(e.id)).ToList();
                if (distinct.Count > 0)
                {
                    eligible = distinct;
                }
            }

            WordBankEntry chosen = WeightedPick(eligible, e => e.weight, rng);

            if (numbered)
            {
                assignedByCategoryAndN[(category, n)] = chosen;
                if (!usedEntryIdsByCategory.TryGetValue(category, out HashSet<string> set))
                {
                    set = new HashSet<string>();
                    usedEntryIdsByCategory[category] = set;
                }
                set.Add(chosen.id);
            }

            return chosen;
        }

        private static T WeightedPick<T>(IReadOnlyList<T> items, Func<T, float> getWeight, System.Random rng)
        {
            float total = 0f;
            foreach (T item in items)
            {
                total += Mathf.Max(0f, getWeight(item));
            }

            if (total <= 0f)
            {
                return items[rng.Next(items.Count)];
            }

            float roll = (float)(rng.NextDouble() * total);
            float cumulative = 0f;
            foreach (T item in items)
            {
                cumulative += Mathf.Max(0f, getWeight(item));
                if (roll <= cumulative)
                {
                    return item;
                }
            }

            return items[items.Count - 1];
        }

        private bool WasRecentlyUsed(string resolved)
        {
            int start = Math.Max(0, resolvedInstanceHistory.Count - RecentLookbackWindow);
            for (int i = start; i < resolvedInstanceHistory.Count; i++)
            {
                if (resolvedInstanceHistory[i] == resolved)
                {
                    return true;
                }
            }

            return false;
        }

        private void RecordUsage(string resolved)
        {
            resolvedInstanceHistory.Add(resolved);
            while (resolvedInstanceHistory.Count > MaxResolvedInstanceHistory)
            {
                resolvedInstanceHistory.RemoveAt(0);
            }
        }
    }
}
