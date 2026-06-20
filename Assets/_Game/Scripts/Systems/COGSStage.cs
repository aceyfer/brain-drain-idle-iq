using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Authoring data for one stage of the COGS narrator portrait's visual progression.
    /// COGSPortraitController matches the highest stage whose minRebirthCount is at or below
    /// the player's current RebirthCount. actUnlockName is reserved for a future narrative-act
    /// system; nothing currently reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "COGSStage", menuName = "BrainDrain/COGS Stage")]
    public sealed class COGSStage : ScriptableObject
    {
        public int stageIndex;
        public Sprite portraitSprite;
        public int minRebirthCount;
        public string stageName;
        public string actUnlockName;
    }
}
