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
    /// triggerType, current WorldRestorationManager.RestorationPercent falling within
    /// [minRestorationPercent, maxRestorationPercent], and (for BuildingPurchase) buildingName
    /// if set.
    /// </summary>
    [CreateAssetMenu(fileName = "NarratorLine", menuName = "BrainDrain/Narrator Line")]
    public sealed class NarratorLine : ScriptableObject
    {
        [Header("Trigger")]
        public NarratorTriggerType triggerType;
        [Tooltip("Optional. If set, only matches a BuildingPurchase trigger for this exact building name.")]
        public string buildingName;

        /// <summary>
        /// No longer read by DialogueManager's trigger-matching filter (replaced 2026-06-22 by
        /// minRestorationPercent/maxRestorationPercent below). Left in place rather than removed
        /// since deleting fields from a ScriptableObject already serialized across 70+ existing
        /// assets is a bigger, unrequested change than this fix calls for -- these two are now
        /// purely informational/legacy.
        /// </summary>
        public int minRebirthCount;
        public int maxRebirthCount;

        [Tooltip("World Restoration percent range [min,max] this line is eligible in. Lines that don't explicitly set these default to the full 0-100 range (always eligible regardless of restoration progress) -- correct for the per-building/one-off lines that were previously gated [0, int.MaxValue] on RebirthCount, i.e. already \"always eligible.\"")]
        public float minRestorationPercent = 0f;
        public float maxRestorationPercent = 100f;

        [Header("Content")]
        [TextArea(2, 4)]
        public string dialogueLine;
        public float displayDurationSeconds = 3f;
    }
}
