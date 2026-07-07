using System;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.Core.Events;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Assembles and fires Gary's ambient barks by combining fragments from a GaryBarkLibrary
    /// ScriptableObject. Assembly structure scales with the current restoration stage (coherence):
    /// stage 1 = single observation (stutter prefix); stage 2-3 = opener + observation;
    /// stage 4+ = opener + observation + closer. Only fragments with minStage <= coherence are
    /// eligible. Subscribed triggers: ItemPurchased (EventBus, 30% / 45s cooldown),
    /// StageAdvanced (EventBus, always), 90s tap-idle (GameManager tick), and IQ threshold
    /// crossings at 75 / 125 (75% / 125% of the 100 starting baseline, via OnPlayerIQChanged).
    /// Anti-repeat: ring buffer of last 12 assembled lines, up to 3 rerolls then skip.
    /// Display: fires OnGaryBark(string) for GaryBubbleUI (separate from COGS DialogueManager).
    /// </summary>
    public sealed class GaryBarkManager : MonoBehaviour
    {
        // ---- Config ----
        private const int RingBufferSize = 12;
        private const int MaxRerolls = 3;
        // StartingPlayerIQ is private const 100f in PlayerIQManager — replicated here as threshold anchor
        private const float IqBaseline = 100f;
        private const float IqLowThreshold = IqBaseline * 0.75f;   // 75  — crosses down: offline decay
        private const float IqHighThreshold = IqBaseline * 1.25f;  // 125 — crosses up: overcharge
        private const float ItemPurchasedCooldown = 45f;
        private const float ItemPurchasedChance = 0.3f;
        private const float IdleThreshold = 90f;
        private const string TagPurchase = "purchase";
        private const string TagIdle = "idle";

        [SerializeField] private GaryBarkLibrary library;

        // ---- Singleton ----
        private static GaryBarkManager instance;
        private static bool isShuttingDown;

        public static GaryBarkManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<GaryBarkManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    instance = new GameObject("GaryBarkManager (Auto)").AddComponent<GaryBarkManager>();
                }
                return instance;
            }
        }

        /// <summary>Fired with the fully assembled bark text. GaryBubbleUI subscribes to display it.</summary>
        public event Action<string> OnGaryBark;

        // ---- State ----
        private readonly Queue<string> recentBarks = new Queue<string>(RingBufferSize);
        private float lastPurchaseBarkTime = float.NegativeInfinity;
        private float lastTapTime;
        private bool hasBarkedThisIdlePeriod;
        private bool hasBarkedIqLow;
        private bool hasBarkedIqHigh;
        private float previousIq;

        // ---- Lifecycle ----
        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        private void Start()
        {
            lastTapTime = Time.time; // prevent false idle-bark on cold boot

            EventBus<ItemPurchased>.Subscribe(HandleItemPurchased);
            EventBus<StageAdvanced>.Subscribe(HandleStageAdvanced);

            PlayerTapHandler tapHandler = FindAnyObjectByType<PlayerTapHandler>();
            if (tapHandler != null)
            {
                tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;
                tapHandler.OnTapRewardEarned += HandleTapRewardEarned;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= HandleSecondTick;
                GameManager.Instance.OnSecondTick += HandleSecondTick;
            }

            if (PlayerIQManager.Instance != null)
            {
                previousIq = PlayerIQManager.Instance.PlayerIQ;
                PlayerIQManager.Instance.OnPlayerIQChanged -= HandlePlayerIQChanged;
                PlayerIQManager.Instance.OnPlayerIQChanged += HandlePlayerIQChanged;
            }
        }

        private void OnApplicationQuit() => isShuttingDown = true;

        private void OnDestroy()
        {
            EventBus<ItemPurchased>.Unsubscribe(HandleItemPurchased);
            EventBus<StageAdvanced>.Unsubscribe(HandleStageAdvanced);

            PlayerTapHandler tapHandler = FindAnyObjectByType<PlayerTapHandler>();
            if (tapHandler != null) tapHandler.OnTapRewardEarned -= HandleTapRewardEarned;

            if (GameManager.Instance != null) GameManager.Instance.OnSecondTick -= HandleSecondTick;
            if (PlayerIQManager.Instance != null) PlayerIQManager.Instance.OnPlayerIQChanged -= HandlePlayerIQChanged;

            if (instance == this) { isShuttingDown = true; instance = null; }
        }

        // ---- Trigger Handlers ----

        private void HandleItemPurchased(ItemPurchased _)
        {
            if (UnityEngine.Random.value > ItemPurchasedChance) return;
            if (Time.time - lastPurchaseBarkTime < ItemPurchasedCooldown) return;
            lastPurchaseBarkTime = Time.time;
            TryBark(TagPurchase, SelectionMode.ByTag);
        }

        private void HandleStageAdvanced(StageAdvanced e)
        {
            TryBark(null, SelectionMode.ExactObsStage, exactObsStage: e.newStage);
        }

        private void HandleTapRewardEarned(double _)
        {
            lastTapTime = Time.time;
            hasBarkedThisIdlePeriod = false;
        }

        private void HandleSecondTick()
        {
            if (!hasBarkedThisIdlePeriod && Time.time - lastTapTime >= IdleThreshold)
            {
                hasBarkedThisIdlePeriod = true;
                TryBark(TagIdle, SelectionMode.ByTag);
            }
        }

        private void HandlePlayerIQChanged(float newIq)
        {
            if (!hasBarkedIqLow && previousIq > IqLowThreshold && newIq <= IqLowThreshold)
            {
                hasBarkedIqLow = true;
                TryBark(null, SelectionMode.PreferLowestStage);
            }
            else if (!hasBarkedIqHigh && previousIq < IqHighThreshold && newIq >= IqHighThreshold)
            {
                hasBarkedIqHigh = true;
                TryBark(null, SelectionMode.PreferHighestStage);
            }
            previousIq = newIq;
        }

        // ---- Assembly ----

        private enum SelectionMode { ByTag, ExactObsStage, PreferLowestStage, PreferHighestStage }

        private void TryBark(string tag, SelectionMode mode, int exactObsStage = 0)
        {
            if (library == null) return;
            int coherence = GetCoherence();

            for (int attempt = 0; attempt <= MaxRerolls; attempt++)
            {
                string bark = AssembleBark(tag, mode, coherence, exactObsStage);
                if (bark == null) return; // pool exhausted — not a reroll situation
                if (!IsRecentBark(bark)) { EmitBark(bark); return; }
            }
            // All rerolls collided with ring buffer — skip this bark
        }

        private string AssembleBark(string tag, SelectionMode mode, int coherence, int exactObsStage)
        {
            bool spicy = IsProfanityActive();
            GaryFragment[] openerPool = MergePools(library.openers, spicy ? library.spicyOpeners : null);
            GaryFragment[] obsPool    = library.observations; // no spicy observations pool
            GaryFragment[] closerPool = MergePools(library.closers, spicy ? library.spicyClosers : null);

            GaryFragment? obs = mode == SelectionMode.ExactObsStage
                ? SelectByExactStage(obsPool, exactObsStage)
                : SelectFragment(obsPool, tag, mode, coherence);

            if (!obs.HasValue) return null;

            if (coherence <= 1)
                return "... " + obs.Value.text;

            GaryFragment? opener = SelectFragment(openerPool, tag, mode, coherence);
            if (!opener.HasValue) return null;

            if (coherence <= 3)
                return opener.Value.text + " " + obs.Value.text;

            GaryFragment? closer = SelectFragment(closerPool, tag, mode, coherence);
            if (!closer.HasValue) return null;

            return opener.Value.text + " " + obs.Value.text + " " + closer.Value.text;
        }

        // ---- Fragment Selection ----

        private GaryFragment? SelectFragment(GaryFragment[] pool, string tag, SelectionMode mode, int coherence)
        {
            List<GaryFragment> eligible = FilterEligible(pool, coherence);
            if (eligible.Count == 0) return null;

            switch (mode)
            {
                case SelectionMode.PreferLowestStage:
                {
                    int minS = int.MaxValue;
                    foreach (var f in eligible) if (f.minStage < minS) minS = f.minStage;
                    eligible = eligible.FindAll(f => f.minStage == minS);
                    break;
                }
                case SelectionMode.PreferHighestStage:
                {
                    int maxS = 0;
                    foreach (var f in eligible) if (f.minStage > maxS) maxS = f.minStage;
                    eligible = eligible.FindAll(f => f.minStage == maxS);
                    break;
                }
                case SelectionMode.ByTag:
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        List<GaryFragment> tagged = FilterByTag(eligible, tag);
                        if (tagged.Count > 0) eligible = tagged;
                        // else fall through to full eligible pool (generic fallback)
                    }
                    break;
                }
                case SelectionMode.ExactObsStage:
                    // observations are pre-selected by exact stage; openers/closers use generic selection (no tag, no stage bias)
                    break;
            }

            return eligible[UnityEngine.Random.Range(0, eligible.Count)];
        }

        private GaryFragment? SelectByExactStage(GaryFragment[] pool, int exactStage)
        {
            var candidates = new List<GaryFragment>();
            if (pool != null)
                foreach (var f in pool)
                    if (f.minStage == exactStage) candidates.Add(f);
            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private List<GaryFragment> FilterEligible(GaryFragment[] pool, int coherence)
        {
            var result = new List<GaryFragment>();
            if (pool == null) return result;
            foreach (var f in pool)
                if (f.minStage <= coherence) result.Add(f);
            return result;
        }

        private List<GaryFragment> FilterByTag(List<GaryFragment> pool, string tag)
        {
            var result = new List<GaryFragment>();
            foreach (var f in pool)
            {
                if (f.tags == null) continue;
                foreach (var t in f.tags)
                    if (t == tag) { result.Add(f); break; }
            }
            return result;
        }

        // ---- Helpers ----

        private int GetCoherence()
        {
            int stage = WorldRestorationManager.Instance?.CurrentStage?.stageIndex ?? 0;
            return Mathf.Max(1, stage);
        }

        private bool IsProfanityActive()
        {
            return RandomChatterManager.Instance != null
                && RandomChatterManager.Instance.ProfanityUnlocked
                && RandomChatterManager.Instance.ProfanityEnabled;
        }

        private bool IsRecentBark(string bark)
        {
            foreach (string s in recentBarks)
                if (s == bark) return true;
            return false;
        }

        private void EmitBark(string bark)
        {
            if (recentBarks.Count >= RingBufferSize) recentBarks.Dequeue();
            recentBarks.Enqueue(bark);
            OnGaryBark?.Invoke(bark);
        }

        private static GaryFragment[] MergePools(GaryFragment[] a, GaryFragment[] b)
        {
            if (b == null || b.Length == 0) return a ?? Array.Empty<GaryFragment>();
            if (a == null || a.Length == 0) return b;
            var merged = new GaryFragment[a.Length + b.Length];
            a.CopyTo(merged, 0);
            b.CopyTo(merged, a.Length);
            return merged;
        }
    }
}
