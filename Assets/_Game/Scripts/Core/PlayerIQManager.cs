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

        /// <summary>Brain Freeze jumps PlayerIQ to at least this value on purchase (never demotes an already-higher IQ). Matches the Overcharge curve's own cap at IQ 200 (see CurrencyManager.GetIQProductionMultiplier).</summary>
        private const float BrainFreezeStartIQ = 200f;

        /// <summary>While a Brain Freeze is active, PlayerIQ decays (both live Overcharge decay and offline decay) but never below this value. Deliberate Illuminati/masonic numerology matching the Illumisnotty lore -- do not round.</summary>
        private const float BrainFreezeFloor = 113f;

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
        /// (UTC) at which the current freeze expires. 0 (default) means no active freeze.
        /// Redesigned 2026-08-03: purchase jumps PlayerIQ to at least BrainFreezeStartIQ (200);
        /// while active, PlayerIQ still decays (both live Overcharge decay and offline decay) but
        /// never below BrainFreezeFloor (113) -- a high floor the player buys, not a full pause.
        /// Tapping still raises IQ back toward 200 at any time while active (see
        /// RestoreIQFromTap). Runs on real wall-clock time deliberately (not Time.time, which
        /// resets on app restart), so the timer keeps counting down while the app is closed.
        /// </summary>
        private long brainFreezeExpiryUnixSeconds;

        /// <summary>Unix seconds (UTC) the current Brain Freeze expires at, or 0 if none is active. For SaveManager persistence.</summary>
        public long BrainFreezeExpiryUnixSeconds => brainFreezeExpiryUnixSeconds;

        /// <summary>True while a Brain Freeze is currently active (real wall-clock time has not yet reached the expiry). While true, PlayerIQ decays only down to BrainFreezeFloor (113), not fully immune -- see DecayOvercharge/ApplyOfflineDecay.</summary>
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

        /// <summary>
        /// Drains PlayerIQ back toward StartingPlayerIQ (100) at OverchargeDecayPerSecond while
        /// above it. While a Brain Freeze is active, the floor is BrainFreezeFloor (113) instead
        /// -- decay still runs, it just can't go below the purchased floor. The floor swap is
        /// re-evaluated every tick via the live IsBrainFreezeActive check, so the transition back
        /// to 100 the instant a freeze expires needs no special-case code: decay just continues
        /// from wherever IQ currently sits.
        /// </summary>
        private void DecayOvercharge()
        {
            float floor = IsBrainFreezeActive ? BrainFreezeFloor : StartingPlayerIQ;

            if (playerIQ <= floor)
                return;

            float previous = playerIQ;
            playerIQ = Mathf.Max(floor, playerIQ - OverchargeDecayPerSecond);

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
        /// Adds a small flat IQ restore per tap while IQ is below the current ceiling (both on a
        /// fresh save starting at IQ 1 and after offline decay). Normally the ceiling is
        /// StartingPlayerIQ (100) -- above that, IQ only grows from infrastructure spending and
        /// building purchases, tapping stops affecting it. While a Brain Freeze is active, the
        /// ceiling is BrainFreezeStartIQ (200) instead -- this is the entire point of the
        /// redesigned Brain Freeze: come back, tap up from the 113 floor to 200, as often as you
        /// like, for the freeze's duration.
        /// </summary>
        public void RestoreIQFromTap()
        {
            float ceiling = IsBrainFreezeActive ? BrainFreezeStartIQ : StartingPlayerIQ;

            if (playerIQ >= ceiling)
            {
                return;
            }

            float previousIQ = playerIQ;
            playerIQ = Mathf.Min(ceiling, playerIQ + IQRestorePerTap);

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
        /// Also clears lastMilestoneIndex so milestones fire correctly in the new run, and clears
        /// any active Brain Freeze (decided 2026-08-03): letting the freeze's floor/timer survive
        /// a Rebirth would let a paying player time a voluntary prestige reset for an instant free
        /// IQ refill, which is exactly the pay-to-win this mechanic is designed to avoid. The
        /// purchase being cut short by the player's own choice to Rebirth is intentional.
        /// </summary>
        public void ResetForRebirth()
        {
            playerIQ = MinPlayerIQ;
            lastMilestoneIndex = 0;
            brainFreezeExpiryUnixSeconds = 0;
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
        /// fresh independent 48h timer. Every purchase -- first or stacking -- also jumps
        /// PlayerIQ to at least BrainFreezeStartIQ (200); Mathf.Max rather than a flat set so an
        /// already-higher IQ (from ordinary building/infrastructure growth, which has no ceiling)
        /// is never demoted by buying this. Used by GodTierStoreManager when a Brain Freeze
        /// product is stub-purchased.
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

            float previousIQ = playerIQ;
            playerIQ = Mathf.Max(playerIQ, BrainFreezeStartIQ);

            if (!Mathf.Approximately(previousIQ, playerIQ))
            {
                OnPlayerIQChanged?.Invoke(playerIQ);
                CheckMilestone();
            }
        }

        /// <summary>Restores the Brain Freeze expiry directly from a save file -- no stacking math, unlike ApplyBrainFreeze. Must run before LoadStateWithOfflineDecay so that load's decay calculation sees the correct freeze state.</summary>
        public void SetBrainFreezeExpiry(long expiryUnixSeconds)
        {
            brainFreezeExpiryUnixSeconds = expiryUnixSeconds;
        }

        /// <summary>
        /// Simplification, deliberately approved over a precise two-phase calculation (2026-08-03):
        /// if a Brain Freeze was still active at the moment the app closed (lastActiveUtc precedes
        /// brainFreezeExpiryUnixSeconds), the ENTIRE offline gap decays toward BrainFreezeFloor
        /// (113) instead of OfflineDecayFloor (1) -- even if the freeze's real expiry actually fell
        /// somewhere inside that gap. Precisely splitting the gap at the freeze's expiry moment
        /// would need a two-phase lerp; erring generous on a real-money QoL purchase was judged not
        /// worth that complexity. brainFreezeExpiryUnixSeconds must already be restored (via
        /// SetBrainFreezeExpiry, called before this) for the comparison below to be correct.
        /// </summary>
        private float ApplyOfflineDecay(float iq, DateTime lastActiveUtc)
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

            long lastActiveUnixSeconds = new DateTimeOffset(lastActiveUtc).ToUnixTimeSeconds();
            bool freezeActiveAtClose = lastActiveUnixSeconds < brainFreezeExpiryUnixSeconds;
            float floor = freezeActiveAtClose ? BrainFreezeFloor : OfflineDecayFloor;

            if (iq <= floor)
            {
                return iq;
            }

            float effectiveMaxHours = OfflineDecayMaxHours + bonusOfflineDecayMaxHours;
            float t = (float)Math.Min(1d, offlineHours / effectiveMaxHours);
            return Mathf.Lerp(iq, floor, t);
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
