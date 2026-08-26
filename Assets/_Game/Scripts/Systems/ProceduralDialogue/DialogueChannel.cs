namespace BrainDrain.Systems
{
    /// <summary>
    /// Which display pipeline a procedural dialogue template feeds. COGS plugs into
    /// DialogueManager exactly like a NarratorLine; STREET plugs into ChatterBubble's
    /// pedestrian ambient chatter, which is separately tuned and never shares timing logic
    /// with DialogueManager. See PROCEDURAL_DIALOGUE_SPEC.md "Channels".
    /// </summary>
    public enum DialogueChannel
    {
        COGS,
        STREET
    }
}
