using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BrainDrain.Core;
using BrainDrain.Core.Events;

namespace BrainDrain.Systems
{
    /// <summary>
    /// Translates the game's internal economy events (CurrencyManager balance changes, shop
    /// purchases, chapter unlocks) into zero-allocation EventBus raises so UI/audio/analytics
    /// can subscribe without taking hard references to every originating system.
    ///
    /// Execution order 10: runs after every shop (default 0) but within the same frame, so
    /// SeedShopSnapshots() sees the post-LoadState owned set rather than an empty one
    /// (SaveManager fires ApplyLoadedDataToSystems at order -200, long before we arrive).
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class EconomyManager : MonoBehaviour
    {
        private static EconomyManager instance;
        private static bool isShuttingDown;

        // Per-shop owned-item snapshots. Seeded once in Start() from current state;
        // HandleXxxShopChanged() diffs against them so only genuine purchases raise ItemPurchased
        // (not the OnItemsChanged that LoadState fires on every cold boot).
        private readonly HashSet<string> cashOwned    = new HashSet<string>();
        private readonly HashSet<string> pointsOwned  = new HashSet<string>();
        private readonly HashSet<string> premiumOwned = new HashSet<string>();

        public static EconomyManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<EconomyManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var host = new GameObject("EconomyManager (Auto)");
                    instance = host.AddComponent<EconomyManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            BindCurrencyEvents();
            SeedShopSnapshots();
            BindShopEvents();
            BindStageEvents();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnbindCurrencyEvents();
            UnbindShopEvents();
            UnbindStageEvents();
            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        // ------------------------------------------------------------------ CurrencyChanged

        private void BindCurrencyEvents()
        {
            CurrencyManager cm = CurrencyManager.Instance;
            if (cm == null) return;
            cm.OnBrainPowerChanged += HandleBrainPowerChanged;
            cm.OnCashChanged.AddListener(HandleCashChanged);
            cm.OnPointsChanged.AddListener(HandlePointsChanged);
            cm.OnNeuronsChanged += HandleNeuronsChanged;
        }

        private void UnbindCurrencyEvents()
        {
            CurrencyManager cm = CurrencyManager.Instance;
            if (cm == null) return;
            cm.OnBrainPowerChanged -= HandleBrainPowerChanged;
            cm.OnCashChanged.RemoveListener(HandleCashChanged);
            cm.OnPointsChanged.RemoveListener(HandlePointsChanged);
            cm.OnNeuronsChanged -= HandleNeuronsChanged;
        }

        private static void HandleBrainPowerChanged(double _) =>
            EventBus<CurrencyChanged>.Raise(new CurrencyChanged { currency = Currency.BrainPower });

        private static void HandleCashChanged(double _) =>
            EventBus<CurrencyChanged>.Raise(new CurrencyChanged { currency = Currency.Cash });

        private static void HandlePointsChanged(double _) =>
            EventBus<CurrencyChanged>.Raise(new CurrencyChanged { currency = Currency.Points });

        private static void HandleNeuronsChanged(int _) =>
            EventBus<CurrencyChanged>.Raise(new CurrencyChanged { currency = Currency.Neurons });

        // ------------------------------------------------------------------ ItemPurchased

        private void SeedShopSnapshots()
        {
            CashShopManager cash = CashShopManager.Instance;
            if (cash != null)
                foreach (CashShopItemData item in cash.Items)
                    if (cash.IsItemOwned(item))
                        cashOwned.Add(item.itemId);

            PointsShopManager pts = PointsShopManager.Instance;
            if (pts != null)
                foreach (PointsShopItemData item in pts.Items)
                    if (pts.IsItemOwned(item))
                        pointsOwned.Add(item.itemId);

            // Premium ownership sources from GodTierStoreManager (the surviving premium manager
            // per TASKLIST_DETAILS §9/§10 -- PremiumShopManager is retired).
            GodTierStoreManager god = GodTierStoreManager.Instance;
            if (god != null)
                foreach (GodTierStoreItemData item in god.Items)
                    if (god.IsItemOwned(item))
                        premiumOwned.Add(item.itemId);
        }

        private void BindShopEvents()
        {
            if (CashShopManager.Instance != null)
                CashShopManager.Instance.OnItemsChanged += HandleCashShopChanged;
            if (PointsShopManager.Instance != null)
                PointsShopManager.Instance.OnItemsChanged += HandlePointsShopChanged;
            if (GodTierStoreManager.Instance != null)
                GodTierStoreManager.Instance.OnItemsChanged += HandleGodTierStoreChanged;
        }

        private void UnbindShopEvents()
        {
            if (CashShopManager.Instance != null)
                CashShopManager.Instance.OnItemsChanged -= HandleCashShopChanged;
            if (PointsShopManager.Instance != null)
                PointsShopManager.Instance.OnItemsChanged -= HandlePointsShopChanged;
            if (GodTierStoreManager.Instance != null)
                GodTierStoreManager.Instance.OnItemsChanged -= HandleGodTierStoreChanged;
        }

        private void HandleCashShopChanged()
        {
            CashShopManager mgr = CashShopManager.Instance;
            if (mgr == null) return;
            foreach (CashShopItemData item in mgr.Items)
                if (mgr.IsItemOwned(item) && cashOwned.Add(item.itemId))
                    RaiseItemPurchased(item.itemId);
        }

        private void HandlePointsShopChanged()
        {
            PointsShopManager mgr = PointsShopManager.Instance;
            if (mgr == null) return;
            foreach (PointsShopItemData item in mgr.Items)
                if (mgr.IsItemOwned(item) && pointsOwned.Add(item.itemId))
                    RaiseItemPurchased(item.itemId);
        }

        private void HandleGodTierStoreChanged()
        {
            GodTierStoreManager mgr = GodTierStoreManager.Instance;
            if (mgr == null) return;
            foreach (GodTierStoreItemData item in mgr.Items)
                if (mgr.IsItemOwned(item) && premiumOwned.Add(item.itemId))
                    RaiseItemPurchased(item.itemId);
        }

        private static void RaiseItemPurchased(string itemId)
        {
            EventBus<ItemPurchased>.Raise(new ItemPurchased { itemId = FnvHash(itemId) });
        }

        // FNV-1a 32-bit — deterministic across processes and Unity-Mono builds,
        // unlike string.GetHashCode() which is randomised in .NET Core.
        private static int FnvHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in s)
                    hash = (hash ^ (uint)c) * 16777619u;
                return (int)hash;
            }
        }

        // ------------------------------------------------------------------ StageAdvanced

        private void BindStageEvents()
        {
            ChapterManager cm = ChapterManager.Instance;
            if (cm == null) return;
            cm.OnChapterUnlocked.RemoveListener(HandleChapterUnlocked);
            cm.OnChapterUnlocked.AddListener(HandleChapterUnlocked);
        }

        private void UnbindStageEvents()
        {
            ChapterManager cm = ChapterManager.Instance;
            if (cm == null) return;
            cm.OnChapterUnlocked.RemoveListener(HandleChapterUnlocked);
        }

        private static void HandleChapterUnlocked(ChapterData chapter)
        {
            if (chapter == null) return;
            EventBus<StageAdvanced>.Raise(new StageAdvanced
            {
                newStage = chapter.chapterNumber,
                newRank  = GameManager.Instance != null ? GameManager.Instance.CurrentRankIndex : 0,
                title    = chapter.playerTitle    ?? string.Empty,
                cogsBeat = chapter.cogsReactionLine ?? string.Empty,
            });
        }
    }
}
