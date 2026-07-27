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
        /// Sets up a known non-trivial pre-Snotting state (max buildings, large cash/points
        /// balance, restoration just over the 50k unlock threshold), fires one Snotting cycle,
        /// then logs PASS/FAIL assertions to the Console for checks 3-6:
        ///   3. Current-run values reset (BP, Cash, Points, BPPS, CPS, Restoration, IQ)
        ///   4. Permanent bonuses stacked (RebirthMult, CashMult, PointsRate, TapMult, ShopMults)
        ///   5. Snotting button re-locks (restoration back to 0)
        ///   6. Points Shop ownership unchanged (owned items survive the rebirth)
        /// Call once per cycle from the debug panel or BrainDrain/Testing menu to step through
        /// all 6 Snotting stages. Yellow warning in Console = at least one check failed.
        /// </summary>
        public static void RunSnottingCycleTest()
        {
            CurrencyManager cm          = CurrencyManager.Instance;
            WorldRestorationManager wrm = WorldRestorationManager.Instance;
            PlayerIQManager iqm         = PlayerIQManager.Instance;
            RebirthManager rm           = RebirthManager.Instance;
            PlayerTapHandler pth        = PlayerTapHandler.Instance;
            PointsShopManager psm       = PointsShopManager.Instance;

            if (cm == null || wrm == null || iqm == null || rm == null)
            {
                Debug.LogError("[SnottingTest] One or more required managers are null — is Play Mode running?");
                return;
            }

            // ── SETUP ──────────────────────────────────────────────────────────────────
            MaxAllBuildings();
            cm.AddBrainPower(1_000_000d);
            cm.AddCash(500_000d);
            cm.AddPoints(200_000d);
            wrm.LoadState(50_001d);

            // ── SNAPSHOT BEFORE ────────────────────────────────────────────────────────
            int    countBefore        = rm.RebirthCount;
            double rebirthMultBefore  = cm.RebirthMultiplier;
            double cashMultBefore     = cm.CashMultiplier;
            double pointsRateBefore   = cm.PointsConversionRate;
            double tapMultBefore      = pth != null ? pth.TapMultiplier : 1d;
            double shopCashBefore     = cm.ShopCashMultiplier;
            double shopAllBefore      = cm.ShopAllMultiplier;
            double shopCashPtsBefore  = cm.ShopCashToPointsMultiplier;
            double shopPtsGainsBefore = cm.ShopAllPointGainsMultiplier;
            int    ownedBefore        = CountOwnedPointsShopItems(psm);

            // ── TRIGGER ────────────────────────────────────────────────────────────────
            rm.TriggerRebirth();

            // ── ASSERTIONS ─────────────────────────────────────────────────────────────
            const double tol   = 0.0001d;
            int          countAfter = rm.RebirthCount;

            bool bpReset       = cm.BrainPower                          == 0d;
            bool cashReset     = cm.CurrentCash                         == 0d;
            bool pointsReset   = cm.CurrentPoints                       == 0d;
            bool bppsReset     = cm.IdleBPPS                           == 0d;
            bool cpsReset      = cm.CashPerSecond                       == 0d;
            bool restReset     = wrm.CumulativePointsSpentOnRestoration == 0d;
            bool iqReset       = Mathf.Approximately(iqm.PlayerIQ, 1f);

            bool rebirthMultOk = System.Math.Abs(cm.RebirthMultiplier    - (rebirthMultBefore  + 0.05d)) < tol;
            bool cashMultOk    = System.Math.Abs(cm.CashMultiplier       - (cashMultBefore     + 0.10d)) < tol;
            bool pointsRateOk  = System.Math.Abs(cm.PointsConversionRate - (pointsRateBefore   + 0.05d)) < tol;
            bool tapMultOk     = pth == null || System.Math.Abs(pth.TapMultiplier - (tapMultBefore + 0.05d)) < tol;
            bool shopOk        = System.Math.Abs(cm.ShopCashMultiplier          - shopCashBefore)      < tol
                              && System.Math.Abs(cm.ShopAllMultiplier           - shopAllBefore)       < tol
                              && System.Math.Abs(cm.ShopCashToPointsMultiplier  - shopCashPtsBefore)   < tol
                              && System.Math.Abs(cm.ShopAllPointGainsMultiplier - shopPtsGainsBefore)  < tol;

            bool buttonLocked  = wrm.CumulativePointsSpentOnRestoration == 0d;

            int  ownedAfter    = CountOwnedPointsShopItems(psm);
            bool psOk          = ownedAfter == ownedBefore;

            bool allOk = bpReset && cashReset && pointsReset && bppsReset && cpsReset && restReset && iqReset
                      && rebirthMultOk && cashMultOk && pointsRateOk && tapMultOk && shopOk
                      && buttonLocked && psOk && countAfter == countBefore + 1;

            string title = RebirthManager.GetIllumisnottiTitle(countAfter);
            string tapStr = pth != null ? pth.TapMultiplier.ToString("F3") : "N/A";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[SnottingTest] ══ Cycle {countBefore} → {countAfter}  \"{title}\" ══");
            sb.AppendLine($"  RebirthCount  {countBefore} → {countAfter}  [{(countAfter == countBefore + 1 ? "PASS" : "FAIL")}]");
            sb.AppendLine();
            sb.AppendLine("  ── Check 3: Current-run resets ──────────────────────────");
            sb.AppendLine($"  BrainPower  = {cm.BrainPower:F0}                 [{(bpReset    ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  Cash        = {cm.CurrentCash:F0}                 [{(cashReset  ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  Points      = {cm.CurrentPoints:F0}                 [{(pointsReset ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  IdleBPPS    = {cm.IdleBPPS:F4}              [{(bppsReset  ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  CashPerSec  = {cm.CashPerSecond:F4}              [{(cpsReset   ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  Restoration = {wrm.CumulativePointsSpentOnRestoration:F0}                 [{(restReset  ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  IQ          = {iqm.PlayerIQ:F1}  (expected 1)     [{(iqReset    ? "PASS" : "FAIL")}]");
            sb.AppendLine();
            sb.AppendLine("  ── Check 4: Permanent bonuses stacked ───────────────────");
            sb.AppendLine($"  RebirthMult  {rebirthMultBefore:F3} → {cm.RebirthMultiplier:F3}  (+0.05)  [{(rebirthMultOk ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  CashMult     {cashMultBefore:F3} → {cm.CashMultiplier:F3}  (+0.10)  [{(cashMultOk    ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  PointsRate   {pointsRateBefore:F3} → {cm.PointsConversionRate:F3}  (+0.05)  [{(pointsRateOk  ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  TapMult      {tapMultBefore:F3} → {tapStr}  (+0.05)  [{(tapMultOk     ? "PASS" : "FAIL")}]");
            sb.AppendLine($"  ShopMults    unchanged                     [{(shopOk        ? "PASS" : "FAIL")}]");
            sb.AppendLine();
            sb.AppendLine("  ── Check 5: Snotting button re-locked ───────────────────");
            sb.AppendLine($"  Restoration = 0  (< 50k threshold)        [{(buttonLocked  ? "PASS" : "FAIL")}]");
            sb.AppendLine();
            sb.AppendLine("  ── Check 6: Points Shop ownership preserved ─────────────");
            string psNote = ownedBefore == 0 ? "  ← 0 owned; buy an item first for a stronger test" : "";
            sb.AppendLine($"  OwnedItems   {ownedBefore} → {ownedAfter}  [{(psOk ? "PASS" : "FAIL")}]{psNote}");
            sb.AppendLine();
            sb.AppendLine(allOk
                ? $"  ══ ALL PASSED  (now Snotting #{countAfter}) ════════════════════"
                : "  ══ ONE OR MORE FAILED — see above ════════════════");

            if (allOk)
                Debug.Log(sb.ToString());
            else
                Debug.LogWarning(sb.ToString());
        }

        private static int CountOwnedPointsShopItems(PointsShopManager psm)
        {
            if (psm == null) return 0;
            int count = 0;
            foreach (PointsShopItemData item in psm.Items)
            {
                if (item != null && psm.IsItemOwned(item)) count++;
            }
            return count;
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
        /// Sets cumulative restoration points to exactly 50,000 (the Snotting unlock threshold),
        /// then fires OnRestorationProgressChanged so RebirthUIController can update the button
        /// state. Call RefreshTriggerButton() after this if you need an immediate synchronous
        /// force-refresh on top of the event.
        /// </summary>
        public static void UnlockSnotting()
        {
            WorldRestorationManager.Instance?.LoadState(50000d);
            Debug.Log("[DebugCheats] UnlockSnotting: set CumulativePointsSpentOnRestoration = 50000.");
        }

        // ── Checkpoints ────────────────────────────────────────────────────────────
        // Each checkpoint sets an exact, reproducible game state so bugs at a specific
        // progression stage can be reproduced in seconds rather than hours of play.
        // All use existing manager LoadState/Reset methods -- no save file is touched.

        /// <summary>
        /// Zeros all current-run values (BP, Cash, Points, BPPS, CPS, restoration, IQ,
        /// buildings) while leaving permanent bonuses, RebirthCount, and shop purchases
        /// exactly as they are. Use this to restart a run without wiping a test setup.
        /// </summary>
        public static void FreshRun()
        {
            CurrencyManager cm          = CurrencyManager.Instance;
            WorldRestorationManager wrm = WorldRestorationManager.Instance;
            if (cm == null || wrm == null)
            {
                Debug.LogError("[Checkpoint:FreshRun] Missing managers — is Play Mode running?");
                return;
            }

            UpgradeManager.Instance?.ResetBuildings();
            // LoadState sets multipliers to current values (preserving them) and zeros balances.
            // ExecuteRebirth(0,0,0) then zeros idleBpps/cashPerSecond (LoadState doesn't touch
            // those); the +=0 on multipliers is a no-op.
            cm.LoadState(0d, 0d, cm.RebirthMultiplier, 0d, cm.CashMultiplier, 0d, cm.PointsConversionRate, false, 0d);
            cm.ExecuteRebirth(0d, 0d, 0d);
            wrm.ResetProgress();
            PlayerIQManager.Instance?.ResetForRebirth();

            Debug.Log(
                "[Checkpoint:FreshRun] Current run reset.\n" +
                $"  RebirthCount={RebirthManager.Instance?.RebirthCount ?? 0}  " +
                $"RebirthMult={cm.RebirthMultiplier:F3}  CashMult={cm.CashMultiplier:F3}  " +
                $"PointsRate={cm.PointsConversionRate:F3}  (all preserved)\n" +
                "  BP=0  Cash=0  Points=0  BPPS=0  CPS=0  Restoration=0  IQ=1  Buildings=0");
        }

        /// <summary>
        /// Sets max buildings, adds large currency balances, and unlocks the Snotting button
        /// (restoration → 50,001 so it clears the 50k threshold). Use this to test the
        /// Snotting confirm flow without grinding.
        /// </summary>
        public static void SnottingReady()
        {
            WorldRestorationManager wrm = WorldRestorationManager.Instance;
            if (wrm == null)
            {
                Debug.LogError("[Checkpoint:SnottingReady] Missing managers — is Play Mode running?");
                return;
            }

            MaxAllBuildings();
            CurrencyManager.Instance?.AddBrainPower(1_000_000d);
            CurrencyManager.Instance?.AddCash(500_000d);
            CurrencyManager.Instance?.AddPoints(200_000d);
            // LoadState fires OnRestorationProgressChanged → RebirthUIController unlocks the button.
            wrm.LoadState(50_001d);

            Debug.Log(
                "[Checkpoint:SnottingReady] State ready for Snotting.\n" +
                "  Max buildings, +1M BP, +500k Cash, +200k Points, restoration=50,001 (button UNLOCKED).");
        }

        /// <summary>
        /// Sets the exact post-N-Snottings state: RebirthCount=N, permanent bonuses equal to N
        /// real cycles (RebirthMult=1+0.05N, CashMult=1+0.10N, PointsRate=0.1+0.05N,
        /// TapMult=1+0.05N), all current-run values at zero, Snotting button locked.
        /// Shop purchase multipliers are preserved (they are cross-run permanent upgrades).
        /// </summary>
        public static void AfterSnotting1() => SetAfterSnottingCheckpoint(1);
        public static void AfterSnotting2() => SetAfterSnottingCheckpoint(2);
        public static void AfterSnotting3() => SetAfterSnottingCheckpoint(3);
        public static void AfterSnotting4() => SetAfterSnottingCheckpoint(4);
        public static void AfterSnotting5() => SetAfterSnottingCheckpoint(5);
        public static void AfterSnotting6() => SetAfterSnottingCheckpoint(6);

        private static void SetAfterSnottingCheckpoint(int n)
        {
            CurrencyManager cm          = CurrencyManager.Instance;
            WorldRestorationManager wrm = WorldRestorationManager.Instance;
            if (cm == null || wrm == null)
            {
                Debug.LogError($"[Checkpoint:AfterSnotting{n}] Missing managers — is Play Mode running?");
                return;
            }

            double rebirthMult = 1.0d + 0.05d * n;
            double cashMult    = 1.0d + 0.10d * n;
            double pointsRate  = 0.1d  + 0.05d * n;
            double tapMult     = 1.0d  + 0.05d * n;

            UpgradeManager.Instance?.ResetBuildings();
            // Set exact permanent multipliers for N Snottings; zero all balances.
            cm.LoadState(0d, 0d, rebirthMult, 0d, cashMult, 0d, pointsRate, false, 0d);
            // Zero idleBpps/cashPerSecond (LoadState doesn't touch them); +=0 on mults is no-op.
            cm.ExecuteRebirth(0d, 0d, 0d);
            PlayerTapHandler.Instance?.SetTapMultiplier(tapMult);
            // LoadState fires OnRebirthCountChanged → HUD title, DialogueManager, WardrobeManager.
            RebirthManager.Instance?.LoadState(n);
            // ResetProgress fires OnRestorationProgressChanged → RebirthUIController locks button.
            wrm.ResetProgress();
            PlayerIQManager.Instance?.ResetForRebirth();

            string title = RebirthManager.GetIllumisnottiTitle(n);
            Debug.Log(
                $"[Checkpoint:AfterSnotting{n}] State set.\n" +
                $"  RebirthCount={n}  Title=\"{title}\"\n" +
                $"  RebirthMult={rebirthMult:F2}  CashMult={cashMult:F2}  PointsRate={pointsRate:F2}  TapMult={tapMult:F2}\n" +
                "  BP=0  Cash=0  Points=0  BPPS=0  CPS=0  Restoration=0  IQ=1  Buildings=0\n" +
                "  Snotting button LOCKED (restoration=0 < 50k threshold)");
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
