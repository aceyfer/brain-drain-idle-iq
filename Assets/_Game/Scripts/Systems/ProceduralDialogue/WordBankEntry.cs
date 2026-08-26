using System;

namespace BrainDrain.Systems
{
    /// <summary>
    /// One authored word-bank entry, as specified in PROCEDURAL_DIALOGUE_SPEC.md's
    /// "Data schemas + loaders" section. Also doubles as the raw JSON DTO for
    /// ProceduralDialogueLoader (JsonUtility deserializes directly into this shape) --
    /// plural/article are never inferred at runtime, only ever read from these fields.
    /// </summary>
    [Serializable]
    public sealed class WordBankEntry
    {
        public string id;
        public string text;
        public string plural;
        public string article;
        public int minStage;
        public int maxStage;
        public float weight = 1f;
    }
}
