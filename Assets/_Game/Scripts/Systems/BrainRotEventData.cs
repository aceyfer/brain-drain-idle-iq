using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Authoring data for one satirical random chaos event: a pop-up that nudges
    /// Brains and IQ when the player accepts it.
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
        public double brainRewardOrPenalty;
        public float iqImpact;

        [Tooltip("Not yet consumed by RandomEventManager.ApplyEventEffects — reserved pending a decision on temporary vs. permanent application.")]
        public double multiplierSpike;
    }
}
