namespace BrainDrain.Systems
{
    /// <summary>
    /// A fully-resolved, load-time-validated dialogue template (PROCEDURAL_DIALOGUE_SPEC.md
    /// "Data schemas + loaders"). Unlike the raw JSON it's parsed from, channel and triggerType
    /// have already been parsed into real enum values, and minStage/maxStage have already been
    /// resolved into an actual RestorationPercent range via RestorationStageBands -- so anything
    /// consuming a DialogueTemplate gates on RestorationPercent exactly the way
    /// DialogueManager.TryFireLine already gates NarratorLine, never on stage index directly.
    /// </summary>
    public sealed class DialogueTemplate
    {
        public string Id;
        public DialogueChannel Channel;
        public string Text;
        public int MinStage;
        public int MaxStage;
        public float MinRestorationPercent;
        public float MaxRestorationPercent;
        public float Weight;
        public bool Ending;
        public NarratorTriggerType TriggerType;
        public string BuildingId;
    }
}
