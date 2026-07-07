using System;
using UnityEngine;

namespace BrainDrain.Systems
{
    [Serializable]
    public struct GaryFragment
    {
        [TextArea(1, 2)]
        public string text;
        [Tooltip("Minimum restoration stage index (1-6) for this fragment to be eligible.")]
        public int minStage;
        [Tooltip("Trigger tags this fragment matches ('purchase', 'idle'). Leave empty for generic.")]
        public string[] tags;
    }

    [CreateAssetMenu(fileName = "GaryBarkLibrary", menuName = "BrainDrain/Gary Bark Library")]
    public sealed class GaryBarkLibrary : ScriptableObject
    {
        [Header("Stage 2+ Openers")]
        public GaryFragment[] openers;

        [Header("Stage 1+ Observations")]
        public GaryFragment[] observations;

        [Header("Stage 4+ Closers")]
        public GaryFragment[] closers;

        // TODO(BadWordsPack) — arrays wired, content intentionally empty; populate when Bad Words Pack ships
        [Header("Spicy Openers (Bad Words Pack — leave empty)")]
        public GaryFragment[] spicyOpeners;

        [Header("Spicy Closers (Bad Words Pack — leave empty)")]
        public GaryFragment[] spicyClosers;
    }
}
