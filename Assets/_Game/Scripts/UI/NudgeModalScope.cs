using System.Collections.Generic;
using UnityEngine;

namespace BrainDrain.UI
{
    /// <summary>Tracks actual panel lifetimes, including overlapping and alpha-hidden panels.</summary>
    public sealed class NudgeModalScope : MonoBehaviour
    {
        private static readonly HashSet<NudgeModalScope> OpenPanels = new();
        public static bool AnyOpen => OpenPanels.Count > 0;

        public static void SetOpen(GameObject panel, bool open)
        {
            if (panel == null) return;
            var scope = panel.GetComponent<NudgeModalScope>();
            if (scope == null) scope = panel.AddComponent<NudgeModalScope>();
            scope.enabled = open;
        }

        private void OnEnable() => OpenPanels.Add(this);
        private void OnDisable() => OpenPanels.Remove(this);
        private void OnDestroy() => OpenPanels.Remove(this);
    }
}
