using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Which kind of timed/special effect an event applies on top of its instant Brain
    /// Power/Cash/IQ deltas. Added 2026-06-21 for the Illumisnotti interference events.
    /// InstantOnly preserves the original 8 events' exact prior behavior (no timed component).
    /// </summary>
    public enum EventEffectType
    {
        InstantOnly,
        LockRandomBuilding,
        FreezeTap,
        BrainPowerProductionPercent,
        AllMultipliersPercent,
        RandomBuffOrDebuff
    }

    /// <summary>
    /// Authoring data for one satirical random chaos event: a pop-up that nudges
    /// Brain Power and PlayerIQ when the player accepts it.
    /// </summary>
    [CreateAssetMenu(fileName = "BrainRotEventData", menuName = "BrainDrain/Brain Rot Event")]
    public sealed class BrainRotEventData : ScriptableObject
    {
        [Header("Presentation")]
        public string eventTitle;
        [TextArea(2, 4)]
        public string eventDescription;
        public string choiceButtonText;

        [Header("Effects")]
        public double brainPowerRewardOrPenalty;
        public float playerIQImpact;

        [Tooltip("Instant Cash reward/penalty, mirroring brainPowerRewardOrPenalty -- added 2026-06-21 for events like Illumisnotti Leak. 0 for the original 8 events, which never touched Cash.")]
        public double cashRewardOrPenalty;

        [Tooltip("Not yet consumed by RandomEventManager.ApplyEventEffects — reserved pending a decision on temporary vs. permanent application.")]
        public double multiplierSpike;

        [Header("Timed Effect (Illumisnotti interference events, added 2026-06-21)")]
        public EventEffectType effectType = EventEffectType.InstantOnly;
        [Tooltip("Seconds the timed effect lasts. Unused for InstantOnly/LockRandomBuilding (which uses this too, for the lock duration)/FreezeTap (also uses this for freeze duration).")]
        public float effectDurationSeconds;
        [Tooltip("Percent magnitude for BrainPowerProductionPercent/AllMultipliersPercent/RandomBuffOrDebuff, e.g. -0.25 for -25%. For RandomBuffOrDebuff, the sign is rerolled at apply time -- author this as the magnitude only.")]
        public float effectMagnitudePercent;
    }
}
