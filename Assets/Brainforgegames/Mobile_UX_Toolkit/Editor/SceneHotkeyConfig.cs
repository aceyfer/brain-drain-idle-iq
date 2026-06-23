#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace BrainforgeGames.MobileUXToolkit
{
    [CreateAssetMenu(menuName = "Brainforge/Scene Hotkey Config", fileName = "SceneHotkeyConfig")]
    public class SceneHotkeyConfig : ScriptableObject
    {
        [Tooltip("Index 0 = F1, 1 = F2 ... 11 = F12")]
        public List<SceneAsset> scenes = new List<SceneAsset>(12);
    }
}
#endif