#if UNITY_EDITOR
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Shared cheat implementations for instant progression testing, used by both the in-game
    /// DebugCheatPanel (UI) and the BrainDrain/Testing Editor menu shortcuts, so the actual
    /// logic exists exactly once. Entirely #if UNITY_EDITOR -- compiles out completely in any
    /// build (even a Development Build), since UNITY_EDITOR is only ever defined inside the
    /// Editor itself.
    /// </summary>
    public static class DebugCheats
    {
        /// <summary>Level every owned building is pushed to by MaxAllBuildings.</summary>
        private const int MaxBuildingLevel = 10;

        /// <summary>Safety cap on the buy-loop per building, in case of an unexpected edge case (e.g. cost overflow) -- never actually hit in normal use.</summary>
        private const int MaxBuildingLevelGuard = MaxBuildingLevel * 2;

        public static void AddBrainPower(double amount)
        {
            CurrencyManager.Instance?.AddBrainPower(amount);
        }

        public static void AddCash(double amount)
        {
            CurrencyManager.Instance?.AddCash(amount);
        }

        public static void AddPoints(double amount)
        {
            CurrencyManager.Instance?.AddPoints(amount);
        }

        public static void ForceRebirth()
        {
            RebirthManager.Instance?.TriggerRebirth();
        }

        /// <summary>
        /// Directly sets PlayerIQ (e.g. to 60, to interactively test the tap-to-restore
        /// recovery mechanic). Does NOT replay the "welcome back" narrator line -- that's tied
        /// to PlayerIQManager.LoadStateWithOfflineDecay's real elapsed-time calculation at app
        /// launch, not just a value change, so it isn't something a live Play Mode button can
        /// trigger on its own.
        /// </summary>
        public static void SetPlayerIQ(float value)
        {
            PlayerIQManager.Instance?.LoadState(value);
        }

        /// <summary>Jumps World Restoration straight to a specific stage's threshold.</summary>
        public static void JumpToWorldRestorationStage(double pointsRequired)
        {
            WorldRestorationManager.Instance?.LoadState(pointsRequired);
        }

        /// <summary>
        /// Pushes every building template to MaxBuildingLevel, bypassing normal cost/unlock
        /// gating by granting exactly the Brain Power needed before each purchase -- routed
        /// through the real TryBuyBuilding/AddBrainPower pathway rather than poking
        /// UpgradeManager.LoadBuildingLevels directly, since that replays AddIdleBPPS/
        /// AddCashPerSecond additively with no reset, which would double-count BPPS/CPS if
        /// buildings were already owned before this cheat runs. AddBrainPower also raises
        /// CumulativeBrainPower, so higher-tier buildings unlock naturally as this proceeds.
        /// </summary>
        public static void MaxAllBuildings()
        {
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (upgradeManager == null || currencyManager == null)
            {
                return;
            }

            foreach (BuildingData building in upgradeManager.BuildingTemplates)
            {
                if (building == null)
                {
                    continue;
                }

                int guard = 0;
                while (upgradeManager.GetBuildingLevel(building) < MaxBuildingLevel && guard < MaxBuildingLevelGuard)
                {
                    double cost = upgradeManager.GetCurrentCost(building);
                    currencyManager.AddBrainPower(cost);
                    upgradeManager.TryBuyBuilding(building);
                    guard++;
                }
            }
        }

        /// <summary>
        /// Deletes the save file and, if currently in Play Mode, stops it -- a true "fresh
        /// start" requires Awake()/Start() to actually re-run against the now-missing save
        /// file, which a live button press can't simulate without also patching every derived
        /// value (idleBpps/cashPerSecond have no public reset) that LoadBuildingLevels/
        /// LoadState don't zero out on their own. Outside Play Mode (e.g. the Editor menu item
        /// run before pressing Play), deletes the file directly via the static path rather than
        /// touching SaveManager.Instance, which would otherwise self-bootstrap a permanent
        /// stray GameObject into the open Edit-mode scene.
        /// </summary>
        public static void ClearSave()
        {
            if (Application.isPlaying)
            {
                SaveManager.Instance?.DeleteSave();
                Debug.Log("[DebugCheats] Save cleared. Stopping Play Mode -- press Play again for a fresh start.");
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                if (System.IO.File.Exists(SaveManager.SaveFilePath))
                {
                    System.IO.File.Delete(SaveManager.SaveFilePath);
                }

                Debug.Log("[DebugCheats] Save cleared.");
            }
        }
    }
}
#endif
