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
        public double unlockCumulativeBrainPower;
        public double baseCost = 10d;
        public double costMultiplier = 1.15d;

        [Header("Production")]
        public double baseBrainPowerPerSecond = 1d;
        [Tooltip("Cash per second per level. 0 for buildings that don't produce Cash (everything except Underground Economy, currently).")]
        public double baseCashPerSecond;
    }
}
