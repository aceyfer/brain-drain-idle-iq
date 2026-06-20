using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Authoring data for one stage of the on-screen Player Character's visual progression.
    /// Deliberately independent from COGSStage (the narrator portrait's progression) -- the
    /// world character and the dialogue portrait are separate visual identities that both
    /// happen to key off RebirthCount. Reserved for the upcoming Outfit/Wardrobe system to
    /// extend with additional fields.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterAppearanceStage", menuName = "BrainDrain/Character Appearance Stage")]
    public sealed class CharacterAppearanceStage : ScriptableObject
    {
        public Sprite sprite;
        public int minRebirthCount;
        public string stageName;
    }
}
