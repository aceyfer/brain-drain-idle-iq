using UnityEngine;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Authoring data for one wearable outfit. Unlock is automatic (RebirthCount-gated, same
    /// model as CharacterAppearanceStage/COGSStage), but unlike those, the player chooses which
    /// unlocked outfit is actually equipped via WardrobeManager -- purely cosmetic, no gameplay
    /// effect. outfitId is the stable save-persisted key (sprite/name can change without
    /// breaking a save, the same reason UpgradeManager keys building ownership by buildingName
    /// rather than by BuildingData reference).
    /// </summary>
    [CreateAssetMenu(fileName = "OutfitData", menuName = "BrainDrain/Outfit Data")]
    public sealed class OutfitData : ScriptableObject
    {
        public string outfitId;
        public string outfitName;
        public Sprite sprite;
        public int minRebirthCountToUnlock;
    }
}
