using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Loads word bank and template JSON from Resources/ProceduralDialogue/{WordBanks,Templates}
    /// (TextAssets, so both the Editor and player builds can read them with no scene wiring --
    /// PROCEDURAL_DIALOGUE_SPEC.md "Integration constraint"). One word bank file = one category
    /// (the file's own name, minus extension, is the category id -- not an authored field, since
    /// the spec's word-bank field list doesn't include one). Templates are a flat JSON array per
    /// file; any number of template files may exist.
    ///
    /// JsonUtility can't parse a top-level JSON array, so files are authored as plain arrays
    /// (matching the spec's field list exactly, no wrapper field) and wrapped in memory only
    /// for parsing -- the wrapper is a parser implementation detail, not part of the schema.
    ///
    /// Validates strictly on load: any malformed record (missing id/text, invalid stage range,
    /// non-positive weight, or -- for templates -- a channel/triggerType string that doesn't
    /// match a real enum value) is logged with the offending file and entry id, then skipped.
    /// </summary>
    public static class ProceduralDialogueLoader
    {
        private const string WordBanksResourcePath = "ProceduralDialogue/WordBanks";
        private const string TemplatesResourcePath = "ProceduralDialogue/Templates";

        [Serializable]
        private sealed class JsonArrayWrapper<T>
        {
            public T[] items;
        }

        [Serializable]
        private sealed class RawTemplate
        {
            public string id;
            public string channel;
            public string text;
            public int minStage;
            public int maxStage;
            public float weight = 1f;
            public bool ending;
            public string triggerType;
            public string buildingId;
        }

        public static Dictionary<string, List<WordBankEntry>> LoadWordBanks()
        {
            var result = new Dictionary<string, List<WordBankEntry>>();
            TextAsset[] files = Resources.LoadAll<TextAsset>(WordBanksResourcePath);

            foreach (TextAsset file in files)
            {
                WordBankEntry[] entries = ParseJsonArray<WordBankEntry>(file.text);
                if (entries == null)
                {
                    Debug.LogError($"[ProceduralDialogueLoader] Word bank '{file.name}' is not a valid JSON array. Skipped.");
                    continue;
                }

                var valid = new List<WordBankEntry>();
                foreach (WordBankEntry entry in entries)
                {
                    if (!IsValidWordBankEntry(entry, file.name))
                    {
                        continue;
                    }

                    valid.Add(entry);
                }

                if (valid.Count == 0)
                {
                    continue;
                }

                if (result.ContainsKey(file.name))
                {
                    Debug.LogError($"[ProceduralDialogueLoader] Duplicate word bank category '{file.name}' (a category is one file). Ignoring the duplicate.");
                    continue;
                }

                result[file.name] = valid;
            }

            return result;
        }

        public static List<DialogueTemplate> LoadTemplates()
        {
            var result = new List<DialogueTemplate>();
            TextAsset[] files = Resources.LoadAll<TextAsset>(TemplatesResourcePath);

            foreach (TextAsset file in files)
            {
                RawTemplate[] rawEntries = ParseJsonArray<RawTemplate>(file.text);
                if (rawEntries == null)
                {
                    Debug.LogError($"[ProceduralDialogueLoader] Template file '{file.name}' is not a valid JSON array. Skipped.");
                    continue;
                }

                foreach (RawTemplate raw in rawEntries)
                {
                    DialogueTemplate resolved = ResolveTemplate(raw, file.name);
                    if (resolved != null)
                    {
                        result.Add(resolved);
                    }
                }
            }

            return result;
        }

        private static bool IsValidWordBankEntry(WordBankEntry entry, string fileName)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || string.IsNullOrWhiteSpace(entry.text))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Word bank '{fileName}' has a malformed entry (id='{entry?.id}') -- missing id or text. Skipped.");
                return false;
            }

            if (!IsValidStageRange(entry.minStage, entry.maxStage))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Word bank '{fileName}' entry '{entry.id}' has an invalid stage range [{entry.minStage},{entry.maxStage}]. Skipped.");
                return false;
            }

            if (entry.weight <= 0f)
            {
                Debug.LogError($"[ProceduralDialogueLoader] Word bank '{fileName}' entry '{entry.id}' has a non-positive weight ({entry.weight}). Skipped.");
                return false;
            }

            return true;
        }

        private static DialogueTemplate ResolveTemplate(RawTemplate raw, string fileName)
        {
            if (raw == null || string.IsNullOrWhiteSpace(raw.id) || string.IsNullOrWhiteSpace(raw.text))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Template file '{fileName}' has a malformed entry (id='{raw?.id}') -- missing id or text. Skipped.");
                return null;
            }

            if (!Enum.TryParse(raw.channel, out DialogueChannel channel))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Template file '{fileName}' entry '{raw.id}' has invalid channel '{raw.channel}'. Skipped.");
                return null;
            }

            if (!Enum.TryParse(raw.triggerType, out NarratorTriggerType triggerType))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Template file '{fileName}' entry '{raw.id}' has invalid triggerType '{raw.triggerType}'. Skipped.");
                return null;
            }

            if (!IsValidStageRange(raw.minStage, raw.maxStage))
            {
                Debug.LogError($"[ProceduralDialogueLoader] Template file '{fileName}' entry '{raw.id}' has an invalid stage range [{raw.minStage},{raw.maxStage}]. Skipped.");
                return null;
            }

            if (raw.weight <= 0f)
            {
                Debug.LogError($"[ProceduralDialogueLoader] Template file '{fileName}' entry '{raw.id}' has a non-positive weight ({raw.weight}). Skipped.");
                return null;
            }

            RestorationStageBands.TryGetRange(raw.minStage, out float minPercent, out _);
            RestorationStageBands.TryGetRange(raw.maxStage, out _, out float maxPercent);

            return new DialogueTemplate
            {
                Id = raw.id,
                Channel = channel,
                Text = raw.text,
                MinStage = raw.minStage,
                MaxStage = raw.maxStage,
                MinRestorationPercent = minPercent,
                MaxRestorationPercent = maxPercent,
                Weight = raw.weight,
                Ending = raw.ending,
                TriggerType = triggerType,
                BuildingId = raw.buildingId ?? string.Empty,
            };
        }

        private static bool IsValidStageRange(int minStage, int maxStage)
        {
            return minStage >= 0
                && maxStage < RestorationStageBands.StageCount
                && minStage <= maxStage;
        }

        private static T[] ParseJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            string wrapped = "{\"items\":" + json.Trim() + "}";
            try
            {
                JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
                return wrapper?.items;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProceduralDialogueLoader] JSON parse failure: {e.Message}");
                return null;
            }
        }
    }
}
