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

        /// <summary>Hard minimum for all IQ operations. Offline decay, events, and saves never push IQ below this.</summary>
        private const float MinPlayerIQ = 1f;

        /// <summary>Overcharged IQ drains toward this baseline at OverchargeDecayPerSecond while the app is running.</summary>
        private const float OverchargeDecayPerSecond = 0.1f;

        /// <summary>PlayerIQ never decays below this floor, no matter how long the app was closed.</summary>
        private const float OfflineDecayFloor = MinPlayerIQ;

        /// <summary>Offline time beyond this is not decayed any further -- decay reaches the floor at exactly this duration.</summary>
        private const float OfflineDecayMaxHours = 8f;

        /// <summary>Flat IQ restored per tap while recovering from offline decay (PlayerIQ below the 100 baseline). No effect once back at 100 -- IQ growth above that only comes from infrastructure spend/building purchases/events, unchanged.</summary>
        private const float IQRestorePerTap = 1f;

        private float playerIQ = StartingPlayerIQ;
        private int lastMilestoneIndex;

        /// <summary>
        /// Added 2026-06-21 for the God Tier Store's "24-Hour Corporate Cloak" -- extends the
        /// offline-decay window (real hours before IQ reaches OfflineDecayFloor) by this many
        /// hours. Starts at 0 (no effect on non-owners, identical to pre-existing behavior).
        /// "Convenience not power" per the item's own description -- it doesn't raise the floor
        /// or PlayerIQ itself, only how long the player has before reaching the existing floor.
        /// </summary>
        private float bonusOfflineDecayMaxHours;

        /// <summary>
        /// Added 2026-08-02 for the God Tier Store's Brain Freeze product line -- Unix seconds
        /// (UTC) at which the current freeze expires. 0 (default) means no active freeze. Unlike
        /// bonusOfflineDecayMaxHours, this is NOT a permanent bonus: while
        /// DateTimeOffset.UtcNow.ToUnixTimeSeconds() is less than this value, PlayerIQ is fully
        /// immune to decay from any source (offline decay on load, Overcharge decay while
        /// running) -- not a slower decay curve like the Corporate Cloak, an outright pause.
        /// Runs on real wall-clock time deliberately (not Time.time, which resets on app
        /// restart), so the timer keeps counting down while the app is closed.
        /// </summary>
        private long brainFreezeExpiryUnixSeconds;

        /// <summary>Unix seconds (UTC) the current Brain Freeze expires at, or 0 if none is active. For SaveManager persistence.</summary>
        public long BrainFreezeExpiryUnixSeconds => brainFreezeExpiryUnixSeconds;

        /// <summary>True while a Brain Freeze is currently active (real wall-clock time has not yet reached the expiry).</summary>
        public bool IsBrainFreezeActive => DateTimeOffset.UtcNow.ToUnixTimeSeconds() < brainFreezeExpiryUnixSeconds;

        /// <summary>Seconds remaining on the current freeze, 0 if none active. For a future UI pass -- not consumed anywhere yet.</summary>
        public float BrainFreezeSecondsRemaining => IsBrainFreezeActive
            ? brainFreezeExpiryUnixSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            : 0f;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSecondTick -= DecayOvercharge;
                GameManager.Instance.OnSecondTick += DecayOvercharge;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnSecondTick -= DecayOvercharge;
        }

        private void DecayOvercharge()
        {
            if (playerIQ <= StartingPlayerIQ)
                return;

            if (IsBrainFreezeActive)
                return;

            float previous = playerIQ;
            playerIQ = Mathf.Max(StartingPlayerIQ, playerIQ - OverchargeDecayPerSecond);

            if (!Mathf.Approximately(previous, playerIQ))
                OnPlayerIQChanged?.Invoke(playerIQ);
        }

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
        /// purchase, or a random event), clamped at MinPlayerIQ (1).
        /// </summary>
        public void ModifyPlayerIQ(float delta)
        {
            float previousIQ = playerIQ;
            playerIQ = Mathf.Max(MinPlayerIQ, playerIQ + delta);

            if (!Mathf.Approximately(previousIQ, playerIQ))
            {
                OnPlayerIQChanged?.Invoke(playerIQ);
                CheckMilestone();
            }
        }

        /// <summary>Directly restores PlayerIQ from a save file. Migrates invalid/0 values from pre-1.0 saves to MinPlayerIQ.</summary>
        public void LoadState(float restoredPlayerIQ)
        {
            playerIQ = Mathf.Max(MinPlayerIQ, restoredPlayerIQ);
            lastMilestoneIndex = Mathf.FloorToInt(playerIQ / MilestoneInterval);
            OnPlayerIQChanged?.Invoke(playerIQ);

            float normalized = Mathf.InverseLerp(MinPlayerIQ, StartingPlayerIQ, playerIQ);
            float productionMultiplier = Mathf.Lerp(0.25f, 1f, normalized);
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
        /// Adds a small flat IQ restore per tap while IQ is below the 100 baseline (both on a
        /// fresh save starting at IQ 1 and after offline decay). Clamped so taps never push
        /// IQ above StartingPlayerIQ (100). Above 100, IQ only grows from infrastructure
        /// spending and building purchases -- tapping stops affecting it entirely.
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

        /// <summary>
        /// Resets IQ to the fresh-run baseline (MinPlayerIQ = 1) on Snotting (Rebirth).
        /// Also clears lastMilestoneIndex so milestones fire correctly in the new run.
        /// </summary>
        public void ResetForRebirth()
        {
            playerIQ = MinPlayerIQ;
            lastMilestoneIndex = 0;
            OnPlayerIQChanged?.Invoke(playerIQ);
        }

        /// <summary>Permanently extends the offline-decay window by this many hours. Used by GodTierStoreManager when the "24-Hour Corporate Cloak" is stub-purchased.</summary>
        public void ExtendOfflineDecayWindow(float additionalHours)
        {
            if (additionalHours > 0f)
            {
                bonusOfflineDecayMaxHours += additionalHours;
            }
        }

        /// <summary>
        /// Activates (or stacks onto) a Brain Freeze. Stacking is additive/queued: if a freeze
        /// is already active, the new duration extends from the CURRENT expiry, not from now --
        /// buy 24h, use 10h, buy 48h more, and 62h remain from that moment (24 + 48 - 10), not a
        /// fresh independent 48h timer. Used by GodTierStoreManager when a Brain Freeze product
        /// is stub-purchased.
        /// </summary>
        public void ApplyBrainFreeze(float durationHours)
        {
            if (durationHours <= 0f)
            {
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long durationSeconds = (long)(durationHours * 3600d);
            long baseline = Math.Max(now, brainFreezeExpiryUnixSeconds);
            brainFreezeExpiryUnixSeconds = baseline + durationSeconds;
        }

        /// <summary>Restores the Brain Freeze expiry directly from a save file -- no stacking math, unlike ApplyBrainFreeze. Must run before LoadStateWithOfflineDecay so that load's decay calculation sees the correct freeze state.</summary>
        public void SetBrainFreezeExpiry(long expiryUnixSeconds)
        {
            brainFreezeExpiryUnixSeconds = expiryUnixSeconds;
        }

        private float ApplyOfflineDecay(float iq, DateTime lastActiveUtc)
        {
            if (iq <= OfflineDecayFloor)
            {
                return iq;
            }

            if (IsBrainFreezeActive)
            {
                return iq;
            }

            double offlineHours = (DateTime.UtcNow - lastActiveUtc).TotalHours;
            if (offlineHours <= 0d)
            {
                return iq;
            }

            float effectiveMaxHours = OfflineDecayMaxHours + bonusOfflineDecayMaxHours;
            float t = (float)Math.Min(1d, offlineHours / effectiveMaxHours);
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
