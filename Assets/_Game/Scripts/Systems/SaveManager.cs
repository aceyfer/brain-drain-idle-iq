using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BrainDrain.Core;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Serializable snapshot of all persisted game state. globalIQ/playerIQ are the same field:
    /// the request's "world population IQ" concept doesn't correspond to anything in the current
    /// model, so per the resolved design call it's aliased to PlayerIQManager.PlayerIQ instead.
    /// </summary>
    [Serializable]
    public struct PlayerData
    {
        public double currentBrains;
        public double cumulativeBrains;
        public double rebirthMultiplier;
        public int rebirthCount;
        public float playerIQ;
        public List<BuildingSaveEntry> buildingLevels;

        public double currentCash;
        public double cashMultiplier;
        public double currentPoints;
        public double pointsConversionRate;

        /// <summary>
        /// PlayerTapHandler's permanent tap-payout multiplier. Used to be a constant 1.0 with no
        /// save-side need, but RebirthManager now permanently bumps it +5% per rebirth -- without
        /// persisting it, that bonus would silently reset to 1.0 on every reload.
        /// </summary>
        public double tapMultiplier;

        /// <summary>
        /// Not in the original spec for this field list, but added anyway: a player-facing
        /// toggle is exactly the kind of state that silently and confusingly resets every
        /// session if left unpersisted.
        /// </summary>
        public bool autoConvertCash;

        public int currentChapter;

        public double worldRestorationPointsSpent;

        public string equippedOutfitId;

        // -- Illumisnotti rewrite (2026-06-21): Shop 2/Shop 3/God Tier Store persisted state --
        public List<string> cashShopOwnedItemIds;
        public int companionTier;
        public List<string> pointsShopOwnedItemIds;
        public bool secretEndingUnlocked;
        public List<string> godTierStoreOwnedItemIds;
        public bool cogsVoicepackDisdainOwned;
        public bool y2kGlitchSlumThemeOwned;
        public bool illumisnottiMembershipCardOwned;
        public bool holographicTrashCanFlexOwned;
        public float offlineExtensionHoursGranted;

        /// <summary>
        /// The four CurrencyManager shop-multiplier aggregates (ShopCashMultiplier/
        /// ShopAllMultiplier/ShopCashToPointsMultiplier/ShopAllPointGainsMultiplier), saved
        /// directly as their final values rather than re-derived from owned-item lists on load
        /// (unlike UpgradeManager.LoadBuildingLevels' replay-from-levels pattern) -- see
        /// CurrencyManager.LoadShopMultipliers and CashShopManager/PointsShopManager.LoadState's
        /// comments for why those managers' own LoadState calls deliberately do NOT re-apply
        /// each owned item's effect (it's already baked into these four numbers).
        /// </summary>
        public double shopCashMultiplier;
        public double shopAllMultiplier;
        public double shopCashToPointsMultiplier;
        public double shopAllPointGainsMultiplier;

        /// <summary>
        /// Unix seconds (UTC) as of the first launch of the game.
        /// </summary>
        public long firstLaunchUnixSeconds;

        /// <summary>
        /// Unix seconds (UTC) as of the last successful SaveGame(). Stored as a long, not a
        /// DateTime, since JsonUtility does not serialize DateTime's internal fields. Drives
        /// PlayerIQManager's offline-decay-on-load calculation.
        /// </summary>
        public long lastActiveUnixSeconds;

        // -- Hot Chick offline BPPS decay (2026-06-22) --
        /// <summary>How many Hot Chicks have been purchased (0-6). Extends the offline-BPPS-decay window by 24h each.</summary>
        public int hotChickCount;
        /// <summary>The CurrencyManager.OfflineBPPSMultiplier computed on the last load, gathered live from CurrencyManager at save time (see SaveGame) so it round-trips correctly.</summary>
        public float offlineBPPSMultiplier;
    }

    /// <summary>
    /// Persists and restores game state to/from JSON in Application.persistentDataPath.
    /// Reads the save file on Awake (DefaultExecutionOrder -200, well before GameManager's -100
    /// and everything else) and writes it whenever GameManager.OnSaveRequested fires.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "braindrain_save.json";

#if UNITY_EDITOR
        /// <summary>
        /// EditorPrefs key (not PlayerPrefs/save data -- this is a per-machine Editor testing
        /// preference, not part of the game's own state) controlling whether entering Play Mode
        /// keeps the existing save (true, default) or wipes it for a fresh start each time
        /// (false). Toggled by DebugCheats/TestingMenuShortcuts.
        /// </summary>
        public const string KeepSaveEditorPrefsKey = "BrainDrain_KeepSave";

        /// <summary>
        /// Editor "Stop" doesn't fire OnApplicationQuit/Pause/Focus the way a real device would
        /// -- without this, progress made since the last 60s autosave could silently be lost
        /// when stopping Play Mode, undermining the whole point of KeepSaveEditorPrefsKey.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterExitPlayModeSaveHook()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode && instance != null)
            {
                instance.SaveGame();
            }
        }
#endif

        private static SaveManager instance;

        /// <summary>
        /// Self-bootstrapping: creates a hosting GameObject on first access if nothing placed
        /// one in the scene. Necessary here specifically because nothing else ever calls *into*
        /// SaveManager (it only calls out to other managers) -- without this, a missing scene
        /// GameObject would leave the whole save system permanently inert with no way to notice.
        /// </summary>
        public static SaveManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<SaveManager>();
                if (instance == null)
                {
                    var hostObject = new GameObject("SaveManager (Auto)");
                    instance = hostObject.AddComponent<SaveManager>();
                }

                return instance;
            }
        }

        /// <summary>The most recently loaded or saved snapshot.</summary>
        public PlayerData LoadedData { get; private set; }

        /// <summary>Unix seconds of the first launch of the game.</summary>
        public long FirstLaunchUnixSeconds => LoadedData.firstLaunchUnixSeconds;

        /// <summary>
        /// Public so DebugCheats can delete the save file directly while not in Play Mode,
        /// without going through the self-bootstrapping Instance accessor -- which would
        /// otherwise create a permanent stray "SaveManager (Auto)" GameObject in the open
        /// Edit-mode scene.
        /// </summary>
        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool(KeepSaveEditorPrefsKey, true))
            {
                DeleteSave();
            }
#endif

            LoadGame();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSaveRequested -= SaveGame;
                GameManager.Instance.OnSaveRequested += SaveGame;
            }
            else
            {
                Debug.LogWarning("[SaveManager] GameManager.Instance is null; SaveGame will not be hooked to OnSaveRequested.", this);
            }

            ApplyLoadedDataToSystems();

#if UNITY_ANDROID && !UNITY_EDITOR
            Assets.SimpleAndroidNotifications.NotificationManager.CancelAll();
#endif
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSaveRequested -= SaveGame;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Reads PlayerData from disk into LoadedData, or sensible defaults if no save file
        /// exists yet (first launch).
        /// </summary>
        public void LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                LoadedData = CreateDefaultData();
                return;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                data.buildingLevels ??= new List<BuildingSaveEntry>();

                // Saves written before tapMultiplier existed deserialize it as 0 (JsonUtility
                // zero-fills missing fields, it doesn't apply CreateDefaultData's 1d default) --
                // 0 is never a real value here, so treat it the same as "field didn't exist yet".
                if (data.tapMultiplier <= 0d)
                {
                    data.tapMultiplier = 1d;
                }

                // Same migration guard, same reason, for the four shop-multiplier aggregates
                // added 2026-06-21 -- saves predating these fields deserialize them as 0, never
                // a legitimate value since they only ever start at 1 and increase.
                if (data.shopCashMultiplier <= 0d) data.shopCashMultiplier = 1d;
                if (data.shopAllMultiplier <= 0d) data.shopAllMultiplier = 1d;
                if (data.shopCashToPointsMultiplier <= 0d) data.shopCashToPointsMultiplier = 1d;
                if (data.shopAllPointGainsMultiplier <= 0d) data.shopAllPointGainsMultiplier = 1d;

                data.cashShopOwnedItemIds ??= new List<string>();
                data.pointsShopOwnedItemIds ??= new List<string>();
                data.godTierStoreOwnedItemIds ??= new List<string>();

                LoadedData = data;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveManager] Failed to load save file, using defaults: {exception.Message}", this);
                LoadedData = CreateDefaultData();
            }
        }

        /// <summary>Gathers current state from the active core singletons and writes it to disk as JSON.</summary>
        public void SaveGame()
        {
            PlayerData data = CreateDefaultData();

            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager != null)
            {
                data.currentBrains = currencyManager.BrainPower;
                data.cumulativeBrains = currencyManager.CumulativeBrainPower;
                data.rebirthMultiplier = currencyManager.RebirthMultiplier;
                data.currentCash = currencyManager.CurrentCash;
                data.cashMultiplier = currencyManager.CashMultiplier;
                data.currentPoints = currencyManager.CurrentPoints;
                data.pointsConversionRate = currencyManager.PointsConversionRate;
                data.autoConvertCash = currencyManager.AutoConvertCash;
            }

            if (PlayerTapHandler.Instance != null)
            {
                data.tapMultiplier = PlayerTapHandler.Instance.TapMultiplier;
            }

            if (RebirthManager.Instance != null)
            {
                data.rebirthCount = RebirthManager.Instance.RebirthCount;
            }

            if (ChapterManager.Instance != null)
            {
                data.currentChapter = ChapterManager.Instance.CurrentChapterNumber;
            }

            if (WorldRestorationManager.Instance != null)
            {
                data.worldRestorationPointsSpent = WorldRestorationManager.Instance.CumulativePointsSpentOnRestoration;
            }

            if (WardrobeManager.Instance != null && WardrobeManager.Instance.EquippedOutfit != null)
            {
                data.equippedOutfitId = WardrobeManager.Instance.EquippedOutfit.outfitId;
            }

            if (currencyManager != null)
            {
                data.shopCashMultiplier = currencyManager.ShopCashMultiplier;
                data.shopAllMultiplier = currencyManager.ShopAllMultiplier;
                data.shopCashToPointsMultiplier = currencyManager.ShopCashToPointsMultiplier;
                data.shopAllPointGainsMultiplier = currencyManager.ShopAllPointGainsMultiplier;
                data.offlineBPPSMultiplier = currencyManager.OfflineBPPSMultiplier;
            }

            if (CompanionManager.Instance != null)
            {
                data.hotChickCount = CompanionManager.Instance.HotChickCount;
            }

            if (CashShopManager.Instance != null)
            {
                data.cashShopOwnedItemIds = new List<string>();
                foreach (CashShopItemData item in CashShopManager.Instance.Items)
                {
                    if (item != null && CashShopManager.Instance.IsItemOwned(item))
                    {
                        data.cashShopOwnedItemIds.Add(item.itemId);
                    }
                }
            }

            if (CompanionManager.Instance != null)
            {
                data.companionTier = CompanionManager.Instance.CurrentTier;
            }

            if (PointsShopManager.Instance != null)
            {
                data.pointsShopOwnedItemIds = new List<string>();
                foreach (PointsShopItemData item in PointsShopManager.Instance.Items)
                {
                    if (item != null && PointsShopManager.Instance.IsItemOwned(item))
                    {
                        data.pointsShopOwnedItemIds.Add(item.itemId);
                    }
                }

                data.secretEndingUnlocked = PointsShopManager.Instance.SecretEndingUnlocked;
            }

            if (GodTierStoreManager.Instance != null)
            {
                data.godTierStoreOwnedItemIds = new List<string>();
                foreach (GodTierStoreItemData item in GodTierStoreManager.Instance.Items)
                {
                    if (item != null && GodTierStoreManager.Instance.IsItemOwned(item))
                    {
                        data.godTierStoreOwnedItemIds.Add(item.itemId);
                    }
                }

                data.cogsVoicepackDisdainOwned = GodTierStoreManager.Instance.CogsVoicepackDisdainOwned;
                data.y2kGlitchSlumThemeOwned = GodTierStoreManager.Instance.Y2KGlitchSlumThemeOwned;
                data.illumisnottiMembershipCardOwned = GodTierStoreManager.Instance.IllumisnottiMembershipCardOwned;
                data.holographicTrashCanFlexOwned = GodTierStoreManager.Instance.HolographicTrashCanFlexOwned;
                data.offlineExtensionHoursGranted = GodTierStoreManager.Instance.OfflineExtensionHoursGranted;
            }

            PlayerIQManager playerIQManager = PlayerIQManager.Instance;
            if (playerIQManager != null)
            {
                data.playerIQ = playerIQManager.PlayerIQ;
            }

            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                data.buildingLevels = new List<BuildingSaveEntry>();
                foreach (KeyValuePair<string, int> entry in upgradeManager.BuildingLevels)
                {
                    data.buildingLevels.Add(new BuildingSaveEntry { buildingName = entry.Key, level = entry.Value });
                }
            }

            data.lastActiveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            LoadedData = data;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {exception.Message}", this);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            Assets.SimpleAndroidNotifications.NotificationManager.CancelAll();
            Assets.SimpleAndroidNotifications.NotificationManager.SendWithAppIcon(
                System.TimeSpan.FromHours(2),
                "Brain Drain: Idle IQ",
                "Your IQ is slipping... come back.",
                Color.red,
                Assets.SimpleAndroidNotifications.NotificationIcon.Message
            );
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Assets.SimpleAndroidNotifications.NotificationManager.CancelAll();
#endif
            }
            else
            {
                SaveGame();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveGame();
            }
            else
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Assets.SimpleAndroidNotifications.NotificationManager.CancelAll();
#endif
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        /// <summary>Deletes the save file from disk, if present, and resets LoadedData to defaults. For future debug/reset use.</summary>
        public void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            LoadedData = CreateDefaultData();
        }

        /// <summary>
        /// Pushes LoadedData into the active core singletons. LoadGame() alone only populates
        /// this in-memory snapshot; without this step the loaded save would never actually
        /// reach gameplay state.
        /// </summary>
        private void ApplyLoadedDataToSystems()
        {
            PlayerData data = LoadedData;

            CurrencyManager.Instance?.LoadState(
                data.currentBrains,
                data.cumulativeBrains,
                data.rebirthMultiplier,
                data.currentCash,
                data.cashMultiplier,
                data.currentPoints,
                data.pointsConversionRate,
                data.autoConvertCash);
            CurrencyManager.Instance?.LoadShopMultipliers(
                data.shopCashMultiplier,
                data.shopAllMultiplier,
                data.shopCashToPointsMultiplier,
                data.shopAllPointGainsMultiplier);

            // GodTierStoreManager.LoadState must run BEFORE PlayerIQManager.LoadStateWithOfflineDecay:
            // it's what re-grants the "24-Hour Corporate Cloak"'s offline-decay-window extension
            // (PlayerIQManager.bonusOfflineDecayMaxHours starts at 0 on every fresh load), and
            // that extension needs to already be in place before this load's decay calculation
            // runs, or an owner of the Cloak would get under-credited for this one load.
            GodTierStoreManager.Instance?.LoadState(
                data.godTierStoreOwnedItemIds,
                data.cogsVoicepackDisdainOwned,
                data.y2kGlitchSlumThemeOwned,
                data.illumisnottiMembershipCardOwned,
                data.holographicTrashCanFlexOwned,
                data.offlineExtensionHoursGranted);

            DateTime lastActiveUtc = DateTimeOffset.FromUnixTimeSeconds(data.lastActiveUnixSeconds).UtcDateTime;
            PlayerIQManager.Instance?.LoadStateWithOfflineDecay(data.playerIQ, lastActiveUtc);
            RebirthManager.Instance?.LoadState(data.rebirthCount);
            UpgradeManager.Instance?.LoadBuildingLevels(data.buildingLevels);

            // Hot Chick offline BPPS decay (2026-06-22). Deliberately placed AFTER
            // UpgradeManager.LoadBuildingLevels rather than immediately after the PlayerIQ
            // offline-decay call above (as the literal task spec described) -- idleBpps is not
            // one of CurrencyManager.LoadState's restored fields (see LoadBuildingLevels' own
            // doc comment: BPPS/CPS are deliberately re-derived from buildingLevels, not saved
            // directly), so reading CurrencyManager.IdleBPPS any earlier in this method would
            // see a stale/zero value and silently clamp every player's multiplier to 1.0
            // regardless of actual building ownership. Reusing lastActiveUtc computed above, as
            // instructed, rather than recomputing it.
            ApplyHotChickOfflineDecay(data, lastActiveUtc);

            ChapterManager.Instance?.LoadState(data.currentChapter);
            WorldRestorationManager.Instance?.LoadState(data.worldRestorationPointsSpent);
            WardrobeManager.Instance?.LoadState(data.equippedOutfitId);
            PlayerTapHandler.Instance?.SetTapMultiplier(data.tapMultiplier);
            CashShopManager.Instance?.LoadState(data.cashShopOwnedItemIds);
            CompanionManager.Instance?.LoadState(data.companionTier);
            CompanionManager.Instance?.LoadHotChickCount(data.hotChickCount);
            PointsShopManager.Instance?.LoadState(data.pointsShopOwnedItemIds, data.secretEndingUnlocked);
        }

        /// <summary>
        /// Computes CurrencyManager.OfflineBPPSMultiplier from real-world elapsed offline hours
        /// against a decay window of 24h * (1 + hotChickCount), linearly interpolating toward a
        /// floor that brings idle BPPS down to effectively 1 (never below, and never applied if
        /// idleBpps is already <= 1, since there's nothing meaningful left to decay toward).
        /// Logs a console-only debug report; not surfaced to any UI this session.
        /// </summary>
        private static void ApplyHotChickOfflineDecay(PlayerData data, DateTime lastActiveUtc)
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            double currentIdleBPPS = currencyManager != null ? currencyManager.IdleBPPS : 0d;
            double decayWindowHours = 24.0 * (1 + data.hotChickCount);
            double elapsedHours = (DateTime.UtcNow - lastActiveUtc).TotalHours;

            float multiplier;
            if (elapsedHours <= 0d)
            {
                multiplier = 1.0f;
            }
            else if (currentIdleBPPS <= 1d)
            {
                // Already at or below the 1-BPPS floor -- nothing to decay toward, and
                // 1.0 / currentIdleBPPS would divide by ~0 if idleBpps is exactly 0.
                multiplier = 1.0f;
            }
            else if (elapsedHours >= decayWindowHours)
            {
                multiplier = (float)(1.0d / currentIdleBPPS);
            }
            else
            {
                float floorMultiplier = (float)(1.0d / currentIdleBPPS);
                float t = (float)(elapsedHours / decayWindowHours);
                multiplier = Mathf.Lerp(1.0f, floorMultiplier, t);
            }

            currencyManager?.SetOfflineBPPSMultiplier(multiplier);

            int elapsedWholeHours = (int)elapsedHours;
            int elapsedMinutes = (int)((elapsedHours - elapsedWholeHours) * 60d);
            double effectiveBPPS = currentIdleBPPS * multiplier;
            Debug.Log(
                "[HotChick] Offline decay report:\n" +
                $" Elapsed: {elapsedWholeHours}h {elapsedMinutes}m\n" +
                $" Hot Chicks owned: {data.hotChickCount}\n" +
                $" Decay window: {decayWindowHours:F0}h\n" +
                $" BPPS multiplier applied: {multiplier:F2}\n" +
                $" Effective BPPS this session: {effectiveBPPS:F2}");
        }

        private static PlayerData CreateDefaultData()
        {
            return new PlayerData
            {
                currentBrains = 0d,
                cumulativeBrains = 0d,
                rebirthMultiplier = 1d,
                rebirthCount = 0,
                playerIQ = 100f,
                buildingLevels = new List<BuildingSaveEntry>(),
                currentCash = 0d,
                cashMultiplier = 1d,
                currentPoints = 0d,
                pointsConversionRate = 0.1d,
                tapMultiplier = 1d,
                autoConvertCash = false,
                currentChapter = 0,
                worldRestorationPointsSpent = 0d,
                equippedOutfitId = null,
                cashShopOwnedItemIds = new List<string>(),
                companionTier = 0,
                pointsShopOwnedItemIds = new List<string>(),
                secretEndingUnlocked = false,
                godTierStoreOwnedItemIds = new List<string>(),
                cogsVoicepackDisdainOwned = false,
                y2kGlitchSlumThemeOwned = false,
                illumisnottiMembershipCardOwned = false,
                holographicTrashCanFlexOwned = false,
                offlineExtensionHoursGranted = 0f,
                shopCashMultiplier = 1d,
                shopAllMultiplier = 1d,
                shopCashToPointsMultiplier = 1d,
                shopAllPointGainsMultiplier = 1d,
                firstLaunchUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                lastActiveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                hotChickCount = 0,
                offlineBPPSMultiplier = 1f
            };
        }
    }
}
