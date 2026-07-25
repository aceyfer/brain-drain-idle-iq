#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrainDrain.Debugging
{
    /// <summary>
    /// DEV-ONLY dialogue frequency tracker. Tallies how many times each line fires in the two
    /// dialogue pools -- COGS narrator (DialogueManager) and pedestrian chatter (RandomChatterManager)
    /// -- so you can spot "parroting" (a line repeating before the pool has cycled) and decide
    /// whether to add lines or tighten anti-repeat. Auto-dumps a sorted report to the Console every
    /// DumpEveryNFires fires per pool; call DialogueFrequencyTracker.DumpNow() to force one.
    ///
    /// Wrapped in UNITY_EDITOR || DEVELOPMENT_BUILD so it compiles out of release entirely. Fully
    /// self-bootstrapping, no scene wiring, no gameplay impact (read-only observer of history events).
    /// The whole codebase runs on the new Input System (activeInputHandler:1), so this deliberately
    /// avoids the legacy Input API and uses auto-dump instead of a hotkey.
    /// </summary>
    public sealed class DialogueFrequencyTracker : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";
        private const int DumpEveryNFires = 15;

        private static DialogueFrequencyTracker instance;

        private readonly Dictionary<string, int> cogsCounts = new();
        private readonly Dictionary<string, int> streetCounts = new();
        private readonly List<string> cogsOrder = new();
        private readonly List<string> streetOrder = new();
        private int cogsSinceDump;
        private int streetSinceDump;

        private BrainDrain.Systems.DialogueManager dialogue;
        private BrainDrain.Systems.RandomChatterManager chatter;
        private bool cogsBound;
        private bool streetBound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var host = new GameObject("DialogueFrequencyTracker");
            instance = host.AddComponent<DialogueFrequencyTracker>();
        }

        /// <summary>Force an immediate report for both pools (e.g. from a debug button).</summary>
        public static void DumpNow()
        {
            if (instance == null)
            {
                return;
            }

            instance.Dump("COGS", instance.cogsCounts, instance.cogsOrder);
            instance.Dump("STREET", instance.streetCounts, instance.streetOrder);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent != null)
            {
                transform.SetParent(systemsParent.transform, false);
            }
        }

        private void Update()
        {
            // Late-bind: the managers self-bootstrap independently, so keep trying until both are up.
            if (!cogsBound || !streetBound)
            {
                TryBind();
            }
        }

        private void TryBind()
        {
            if (!cogsBound)
            {
                dialogue = BrainDrain.Systems.DialogueManager.Instance;
                if (dialogue != null)
                {
                    dialogue.OnHistoryChanged += HandleCogs;
                    cogsBound = true;
                }
            }

            if (!streetBound)
            {
                chatter = BrainDrain.Systems.RandomChatterManager.Instance;
                if (chatter != null)
                {
                    chatter.OnHistoryChanged += HandleStreet;
                    streetBound = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (cogsBound && dialogue != null)
            {
                dialogue.OnHistoryChanged -= HandleCogs;
            }
            if (streetBound && chatter != null)
            {
                chatter.OnHistoryChanged -= HandleStreet;
            }
            if (instance == this)
            {
                instance = null;
            }
        }

        private void HandleCogs()
        {
            IReadOnlyList<BrainDrain.Systems.DialogueManager.DialogueLogEntry> h = dialogue.History;
            if (h.Count == 0)
            {
                return;
            }

            Tally(cogsCounts, cogsOrder, h[h.Count - 1].Text);
            if (++cogsSinceDump >= DumpEveryNFires)
            {
                cogsSinceDump = 0;
                Dump("COGS", cogsCounts, cogsOrder);
            }
        }

        private void HandleStreet()
        {
            IReadOnlyList<BrainDrain.Systems.RandomChatterManager.ChatterLogEntry> h = chatter.History;
            if (h.Count == 0)
            {
                return;
            }

            Tally(streetCounts, streetOrder, h[h.Count - 1].Text);
            if (++streetSinceDump >= DumpEveryNFires)
            {
                streetSinceDump = 0;
                Dump("STREET", streetCounts, streetOrder);
            }
        }

        private static void Tally(Dictionary<string, int> counts, List<string> order, string line)
        {
            counts.TryGetValue(line, out int n);
            counts[line] = n + 1;
            order.Add(line);
        }

        private void Dump(string label, Dictionary<string, int> counts, List<string> order)
        {
            int total = order.Count;
            if (total == 0)
            {
                Debug.Log($"[DialogueFreq] {label}: no lines yet.");
                return;
            }

            // Closest gap between two consecutive fires of the SAME line -- the parroting signal.
            // A gap smaller than the pool size means the pool repeated before cycling through.
            int minGap = int.MaxValue;
            Dictionary<string, int> lastIndex = new();
            for (int i = 0; i < order.Count; i++)
            {
                if (lastIndex.TryGetValue(order[i], out int prev))
                {
                    minGap = Mathf.Min(minGap, i - prev);
                }
                lastIndex[order[i]] = i;
            }
            string gap = minGap == int.MaxValue ? "none yet" : minGap.ToString();

            StringBuilder sb = new();
            sb.AppendLine($"[DialogueFreq] {label} — {total} fires, {counts.Count} unique lines, closest repeat gap: {gap}");
            foreach (KeyValuePair<string, int> kv in counts.OrderByDescending(k => k.Value))
            {
                string t = kv.Key.Length > 64 ? kv.Key.Substring(0, 61) + "..." : kv.Key;
                sb.AppendLine($"  {kv.Value,3}x  {t}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
#endif
