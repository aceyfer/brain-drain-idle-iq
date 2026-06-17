using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Authoring data for a purchasable idle building or structure.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "BrainDrain/Building Data")]
    public sealed class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string buildingName;
        [TextArea(2, 4)]
        public string description;

        [Header("Progression")]
        public int unlockPlayerLevel = 1;
        public double baseCost = 10d;
        public double costMultiplier = 1.15d;

        [Header("Production")]
        public double baseBrainsPerSecond = 1d;
        public double iqRecoveryPerSecond;
    }
}
