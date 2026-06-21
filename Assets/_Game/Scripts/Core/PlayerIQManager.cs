using System;
using System.Collections;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Tracks the player's personal PlayerIQ stat. While the app is running, PlayerIQ is pure
    /// positive accumulation (no live decay loop) -- it only ever drops via offline decay,
    /// applied once on load based on real-world time elapsed since the last save. This is a
    /// deliberate, explicitly-confirmed return-to-play hook, distinct from the old always-on
    /// IQDecaySystem/WorldProgressionManager negative-decay model that was fully replaced
    /// earlier in this project's history.
    /// </summary>
    public sealed class PlayerIQManager : MonoBehaviour
    {
        private const float StartingPlayerIQ = 100f;
        private const float MilestoneInterval = 1000f;

        /// <summary>PlayerIQ never decays below this floor, no matter how long the app was closed.</summary>
        private const float OfflineDecayFloor = 60f;

        /// <summary>Offline time beyond this is not decayed any further -- decay reaches the floor at exactly this duration.</summary>
        private const float OfflineDecayMaxHours = 8f;

        /// <summary>Flat IQ restored per tap while recovering from offline decay (PlayerIQ below the 100 baseline). No effect once back at 100 -- IQ growth above that only comes from infrastructure spend/building purchases/events, unchanged.</summary>
        private const float IQRestorePerTap = 1f;

        private float playerIQ = StartingPlayerIQ;
        private int lastMilestoneIndex;

        /// <summary>Convenient accessor routed through GameManager when available.</summary>
        public static PlayerIQManager Instance
        {
            get
            {
                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.PlayerIQSystem;
                }

                return FindAnyObjectByType<PlayerIQManager>();
            }
        }

        /// <summary>The player's current IQ. Starts at 100 and scales infinitely upward.</summary>
        public float PlayerIQ => playerIQ;

        /// <summary>Fired when PlayerIQ changes. Passes the new value.</summary>
        public event Action<float> OnPlayerIQChanged;

        /// <summary>Fired when PlayerIQ crosses a 1000-point milestone. Passes the new value.</summary>
        public event Action<float> OnIQMilestoneCrossed;

        /// <summary>Fired once after offline decay actually drops IQ on load. Passes the amount lost. DialogueManager subscribes to fire its OfflineDecayReturn narrator line -- kept as a plain event rather than calling into Systems directly, matching this class's existing event-based decoupling (e.g. OnIQMilestoneCrossed).</summary>
        public event Action<float> OnOfflineDecayApplied;

        /// <summary>
        /// Applies a signed delta to PlayerIQ (e.g. infrastructure spending, a building
        /// purchase, or a random event), clamped at a minimum of zero.
        /// </summary>
        public void ModifyPlayerIQ(float delta)
        {
            float previousIQ = playerIQ;
            playerIQ = Mathf.Max(0f, playerIQ + delta);

            if (!Mathf.Approximately(previousIQ, playerIQ))
            {
                OnPlayerIQChanged?.Invoke(playerIQ);
                CheckMilestone();
            }
        }

        /// <summary>Directly restores PlayerIQ from a save file, with no transformation.</summary>
        public void LoadState(float restoredPlayerIQ)
        {
            playerIQ = restoredPlayerIQ;
            lastMilestoneIndex = Mathf.FloorToInt(playerIQ / MilestoneInterval);
            OnPlayerIQChanged?.Invoke(playerIQ);
        }

        /// <summary>
        /// Restores PlayerIQ from a save file, first applying offline decay toward
        /// OfflineDecayFloor based on real-world time elapsed since lastActiveUtc (linearly,
        /// reaching the floor at OfflineDecayMaxHours and no further past that). This is the
        /// "return to play" hook -- the player comes back to find IQ dropped and taps to
        /// restore it. Never applies while the app is running, only once on load. If IQ
        /// actually dropped, fires OnOfflineDecayApplied for DialogueManager to react to.
        /// </summary>
        public void LoadStateWithOfflineDecay(float restoredPlayerIQ, DateTime lastActiveUtc)
        {
            float decayedIQ = ApplyOfflineDecay(restoredPlayerIQ, lastActiveUtc);
            float amountLost = restoredPlayerIQ - decayedIQ;

            LoadState(decayedIQ);

            if (amountLost > 0.01f)
            {
                StartCoroutine(NotifyOfflineDecayNextFrame(amountLost));
            }
        }

        /// <summary>
        /// Adds a small flat IQ restore on tap, but only while recovering from offline decay
        /// (current IQ below the 100 baseline) -- clamped so this never pushes IQ above 100.
        /// Once back at 100, taps stop touching IQ again, matching pre-decay behavior.
        /// </summary>
        public void RestoreIQFromTap()
        {
            if (playerIQ >= StartingPlayerIQ)
            {
                return;
            }

            float previousIQ = playerIQ;
            playerIQ = Mathf.Min(StartingPlayerIQ, playerIQ + IQRestorePerTap);

            if (!Mathf.Approximately(previousIQ, playerIQ))
            {
                OnPlayerIQChanged?.Invoke(playerIQ);
                CheckMilestone();
            }
        }

        /// <summary>
        /// Waits one frame before firing OnOfflineDecayApplied. SaveManager runs at
        /// DefaultExecutionOrder -200, well before DialogueManager's/DialogueDisplayUI's
        /// default-order Start() calls have subscribed -- firing synchronously here would be
        /// lost with no listener yet. By the time this resumes (after the entire initial Start
        /// pass has completed), both are guaranteed to be subscribed.
        /// </summary>
        private IEnumerator NotifyOfflineDecayNextFrame(float amountLost)
        {
            yield return null;
            OnOfflineDecayApplied?.Invoke(amountLost);
        }

        private static float ApplyOfflineDecay(float iq, DateTime lastActiveUtc)
        {
            if (iq <= OfflineDecayFloor)
            {
                return iq;
            }

            double offlineHours = (DateTime.UtcNow - lastActiveUtc).TotalHours;
            if (offlineHours <= 0d)
            {
                return iq;
            }

            float t = (float)Math.Min(1d, offlineHours / OfflineDecayMaxHours);
            return Mathf.Lerp(iq, OfflineDecayFloor, t);
        }

        private void CheckMilestone()
        {
            int milestoneIndex = Mathf.FloorToInt(playerIQ / MilestoneInterval);
            if (milestoneIndex <= lastMilestoneIndex)
            {
                return;
            }

            lastMilestoneIndex = milestoneIndex;
            OnIQMilestoneCrossed?.Invoke(playerIQ);
        }
    }
}
