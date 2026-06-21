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
        /// Not in the original spec for this field list, but added anyway: a player-facing
        /// toggle is exactly the kind of state that silently and confusingly resets every
        /// session if left unpersisted.
        /// </summary>
        public bool autoConvertCash;

        public int currentChapter;

        public double worldRestorationPointsSpent;

        public string equippedOutfitId;
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

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

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
            PlayerIQManager.Instance?.LoadState(data.playerIQ);
            RebirthManager.Instance?.LoadState(data.rebirthCount);
            UpgradeManager.Instance?.LoadBuildingLevels(data.buildingLevels);
            ChapterManager.Instance?.LoadState(data.currentChapter);
            WorldRestorationManager.Instance?.LoadState(data.worldRestorationPointsSpent);
            WardrobeManager.Instance?.LoadState(data.equippedOutfitId);
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
                autoConvertCash = false,
                currentChapter = 0,
                worldRestorationPointsSpent = 0d,
                equippedOutfitId = null
            };
        }
    }
}
