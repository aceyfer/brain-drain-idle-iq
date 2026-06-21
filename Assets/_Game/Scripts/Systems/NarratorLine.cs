using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>Which gameplay event a NarratorLine can fire in response to.</summary>
    public enum NarratorTriggerType
    {
        FirstTap,
        BuildingPurchase,
        Rebirth,
        EventOutcome,
        IQMilestone,
        TapWithoutPurchase,
        CashConverted,
        OfflineDecayReturn
    }

    /// <summary>
    /// Authoring data for one narrator dialogue line. DialogueManager matches lines by
    /// triggerType, current RebirthCount falling within [minRebirthCount, maxRebirthCount], and
    /// (for BuildingPurchase) buildingName if set.
    /// </summary>
    [CreateAssetMenu(fileName = "NarratorLine", menuName = "BrainDrain/Narrator Line")]
    public sealed class NarratorLine : ScriptableObject
    {
        [Header("Trigger")]
        public NarratorTriggerType triggerType;
        [Tooltip("Optional. If set, only matches a BuildingPurchase trigger for this exact building name.")]
        public string buildingName;
        public int minRebirthCount;
        public int maxRebirthCount;

        [Header("Content")]
        [TextArea(2, 4)]
        public string dialogueLine;
        public float displayDurationSeconds = 3f;
    }
}
