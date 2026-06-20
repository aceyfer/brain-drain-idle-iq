using System;
using UnityEngine;

namespace BrainDrain.Core
{
    /// <summary>
    /// Tracks the player's personal PlayerIQ stat. Replaces the old IQDecaySystem/
    /// WorldProgressionManager negative-decay and world-restoration models now that the
    /// game has shifted to a pure positive accumulation loop: PlayerIQ starts at 100 and
    /// scales infinitely upward as the player absorbs Brain Power and buys buildings.
    /// </summary>
    public sealed class PlayerIQManager : MonoBehaviour
    {
        private const float StartingPlayerIQ = 100f;
        private const float MilestoneInterval = 1000f;

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

        /// <summary>Directly restores PlayerIQ from a save file.</summary>
        public void LoadState(float restoredPlayerIQ)
        {
            playerIQ = restoredPlayerIQ;
            lastMilestoneIndex = Mathf.FloorToInt(playerIQ / MilestoneInterval);
            OnPlayerIQChanged?.Invoke(playerIQ);
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
