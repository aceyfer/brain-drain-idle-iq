using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Which stat ChapterManager checks unlockThreshold against. WorldRestorationPercent
    /// checks WorldRestorationManager.RestorationPercent (a 0-100 value) -- unlockThreshold
    /// should be expressed on that same 0-100 scale for this condition type.
    /// </summary>
    public enum ChapterUnlockConditionType
    {
        CumulativeBrainPower,
        RebirthCount,
        PointsSpent,
        WorldRestorationPercent
    }

    /// <summary>
    /// Authoring data for one chapter of the 12-chapter narrative arc. ChapterManager unlocks
    /// chapters strictly in sequence (chapterNumber order), checking only the immediately-next
    /// chapter's condition.
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterData", menuName = "BrainDrain/Chapter Data")]
    public sealed class ChapterData : ScriptableObject
    {
        [Header("Identity")]
        public int chapterNumber;
        public string chapterTitle;
        public string playerTitle;

        [Header("Unlock Condition")]
        public ChapterUnlockConditionType unlockConditionType;
        public double unlockThreshold;

        [Header("Narrative Content")]
        [TextArea(2, 4)]
        public string introDialogue;
        [TextArea(2, 4)]
        public string cogsReactionLine;
        [TextArea(2, 4)]
        public string ministryBroadcastLine;
    }
}
