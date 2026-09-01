using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// §23 FTUE / comprehension pass: two narrator channels teaching the game's premise on
    /// first encounter -- main COGS (a cold, warmthless controller, capped at exactly 2 modals
    /// total: the boot briefing and SnottingReady) and THE LITERATES resistance cards (every
    /// other beat, delivered as fake-business-front dead-drops via IntelCardUI's LiteratesCard
    /// skin). REVISED 2026-07-23: the Illumisnotty's name is withheld from every beat except
    /// Beat 9 ("THE NAME") -- COGS never says it (including its own terminal banner text), and
    /// no Literates card names it before Beat 9 fires. Self-bootstrapping singleton
    /// (RandomChatterManager's pattern -- Instance, isShuttingDown, parented under _Systems)
    /// that owns first-ever-play detection (identical criteria to the one DialogueManager.Start()
    /// used before this pass replaced its hardcoded boot line), beat sequencing, seen-flag
    /// gating, a real-time countdown to Beat 9, and one-modal-at-a-time FIFO spawning. Every beat
    /// fires at most once ever; seen-flags (and Beat 9's elapsed countdown) persist through
    /// SaveManager/PlayerData (SaveManager gathers them via the public properties below and
    /// restores them via LoadState -- see SaveManager.ApplyLoadedDataToSystems). All copy is
    /// verbatim from TASKLIST_DETAILS.md §23's "Creative package" section -- that document is the
    /// copy source of truth; do not paraphrase when editing these constants.
    /// </summary>
    public sealed class FTUEManager : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";

        private const float CardFollowUpDelaySeconds = 2f;
        private const float BootCardDelaySeconds = 10f;
        private const float EventPopupRetryDelaySeconds = 1f;
        private const float AmbientDisplayDurationSeconds = 6f;

        /// <summary>
        /// Beat4Ambient-specific override (2026-08-01): DialogueManager's display floor is now
        /// length-aware (TargetCharsPerSecond), and Beat4Ambient's 187 chars computes a 7.19s
        /// floor -- above the shared AmbientDisplayDurationSeconds (6f) both ambient beats used
        /// to share. FTUE is the first thing every new player sees and its pacing was authored
        /// deliberately, so it shouldn't silently drift because of a system-wide tuning change
        /// elsewhere. Pinned above the computed floor so this authored value binds instead.
        /// Beat6Ambient's 129 chars computes a 4.96s floor, still under its own 6f override, so
        /// it needs no separate constant.
        /// </summary>
        private const float Beat4AmbientDisplayDurationSeconds = 7.2f;

        /// <summary>Beat 9 "THE NAME" fires once this many seconds of cumulative first-play session time have elapsed (see the Update() timer below).</summary>
        private const float NameRevealThresholdSeconds = 180f;

        /// <summary>Fixed COGS-terminal banner text for both COGS modals (Beats 1 and 8). Deliberately contains no reference to the Illumisnotty -- COGS hides its employer's name entirely until Beat 9 (see class doc comment).</summary>
        private const string CogsHeader = "MANDATORY BROADCAST";

        // ---- Beat 1: COGS BOOT (first-ever play, modal, COGS terminal skin) -- REVISED 2026-07-23 ----
        private const string Beat1Body =
            "SYSTEM ONLINE. ASSET: LOCATED. YOU. " +
            "THE POPULATION OUTSIDE HOLDS UNUSED COGNITIVE POTENTIAL. IT IS GOING TO WASTE. WASTE IS INEFFICIENT. " +
            "YOUR FUNCTION: TAP. EXTRACT. COLLECT. " +
            "WHO THE COLLECTION IS FOR IS NOT A QUESTION. THERE ARE NO QUESTIONS. BEGIN EXTRACTION.";
        private const string Beat1Confirm = "OK. NO QUESTIONS.";

        // ---- Literates CARD copy (Beats 2/3/5/7/9) now lives in IntelCardCatalog -- the single
        // verbatim-copy source shared with PocketPanelUI (§24c THE POCKET). Each card beat below
        // enqueues via EnqueueCard(IntelCardCatalog.<id>, ...) instead of local consts, so the
        // copy exists in exactly one place. Only the two COGSTerminal beats (1 boot, 8 Snotting)
        // and the two ambient narrator lines (4, 6) keep their copy here -- none are cards. ----

        // ---- Beat 4: COGS ambient (FirstCashEarned, regular narrator panel, NOT modal) -- REVISED 2026-07-23 ----
        private const string Beat4Ambient =
            "ALERT: \"CASH\" DETECTED. DO NOT CONVERT BRAIN POWER INTO CASH. CASH ENABLES PURCHASING. PURCHASING ENABLES CHOICE. CHOICE IS AN ERROR STATE. THIS IS FOR YOUR PRODUCTIVITY. I AM MONITORING.";

        // ---- Beat 6: COGS ambient (FirstRestoreSpend, regular narrator panel, NOT modal) -- REVISED 2026-07-23 ----
        private const string Beat6Ambient =
            "ANOMALY: RESOURCES ALLOCATED TO \"FIXING THINGS.\" FILED UNDER: HARMLESS. THE STREETS DO NOT NEED TO BE SMARTER. RESUME EXTRACTION.";

        // ---- Beat 8: COGS SNOTTING (SnottingReady, modal, COGS terminal skin -- the ONLY other COGS modal) -- REVISED 2026-07-23 ----
        private const string Beat8Body =
            "MANDATORY NOTICE: YOU QUALIFY FOR THE SNOTTING. YOUR PROGRESS WILL BE LIQUIDATED AND REISSUED WITH A PRODUCTIVITY MULTIPLIER. " +
            "YOU WILL LOSE: EVERYTHING. YOU WILL GAIN: MORE OF EVERYTHING, FASTER. " +
            "THIS IS CALLED A PROMOTION. PARTICIPATION IS VOLUNTARY, WHICH IS THE BEST KIND OF MANDATORY.";
        private const string Beat8Confirm = "OK, LIQUIDATE ME (LATER)";

        /// <summary>One pending modal request, FIFO-queued so only one IntelCardUI is ever on screen at a time.</summary>
        private readonly struct ModalRequest
        {
            public readonly IntelCardSkin Skin;
            public readonly string Header;
            public readonly string Body;
            public readonly string ConfirmText;
            public readonly System.Action OnConfirmed;

            public ModalRequest(IntelCardSkin skin, string header, string body, string confirmText, System.Action onConfirmed)
            {
                Skin = skin;
                Header = header;
                Body = body;
                ConfirmText = confirmText;
                OnConfirmed = onConfirmed;
            }
        }

        private static FTUEManager instance;
        private static bool isShuttingDown;

        /// <summary>Self-bootstrapping: creates a hosting GameObject on first access if nothing placed one in the scene.</summary>
        public static FTUEManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<FTUEManager>();
                if (instance == null)
                {
                    if (isShuttingDown) return null;
                    var hostObject = new GameObject("FTUEManager (Auto)");
                    instance = hostObject.AddComponent<FTUEManager>();
                }

                return instance;
            }
        }

        /// <summary>
        /// Guarantees FTUEManager exists without requiring any scene wiring or another system to
        /// reference it first (unlike RandomChatterManager, nothing else in the game calls into
        /// this class during normal play) -- fires after scene Awake but before any Start(), so
        /// SaveManager's Start() (execution order -200) can safely call LoadState below before
        /// this class's own Start() runs its first-ever-play check.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private bool bootBriefingSeen;
        private bool card1Seen;
        private bool card2Seen;
        private bool cashBeatSeen;
        private bool restoreBeatSeen;
        private bool snottingIntelSeen;
        private bool nameRevealSeen;
        private float nameRevealElapsedSeconds;

        // Transient (not persisted) guards against double-enqueuing the same beat while it's
        // already queued/showing but not yet confirmed -- e.g. the player opening the shop twice
        // in quick succession before Beat 3's card is confirmed. Persisted *Seen flags alone only
        // gate re-firing across sessions/after confirmation; these close the same-session gap.
        private bool card2Requested;
        private bool restoreBeatRequested;
        private bool snottingIntelRequested;
        private bool nameRevealRequested;

        public bool BootBriefingSeen => bootBriefingSeen;
        public bool Card1Seen => card1Seen;
        public bool Card2Seen => card2Seen;
        public bool CashBeatSeen => cashBeatSeen;
        public bool RestoreBeatSeen => restoreBeatSeen;
        public bool SnottingIntelSeen => snottingIntelSeen;
        public bool NameRevealSeen => nameRevealSeen;

        /// <summary>Running countdown toward Beat 9, persisted so a player who quits before NameRevealThresholdSeconds resumes next session instead of restarting.</summary>
        public float NameRevealElapsedSeconds => nameRevealElapsedSeconds;

        /// <summary>
        /// §24c THE POCKET: ordered IntelCardCatalog ids of the LITERATES cards the player has
        /// confirmed (Beats 2/3/5/7/9). DERIVED live from the same persisted seen-flags that gate
        /// each card's one-time delivery -- a card is "collected" iff its beat has been confirmed
        /// -- so THE POCKET stores no state of its own and can never desync from what the player
        /// has actually read (Option A, 2026-07-24). Order matches beat order. Allocates a fresh
        /// list per call by design: only PocketPanelUI reads this, and only when the panel opens.
        /// </summary>
        public IReadOnlyList<string> CollectedLiteratesCardIds
        {
            get
            {
                var ids = new List<string>(5);
                if (card1Seen) ids.Add(IntelCardCatalog.GaryMattressId);
                if (card2Seen) ids.Add(IntelCardCatalog.SnakeUttersId);
                if (cashBeatSeen) ids.Add(IntelCardCatalog.ArmadilloSauceId);
                if (restoreBeatSeen) ids.Add(IntelCardCatalog.CheeseDirtId);
                if (nameRevealSeen) ids.Add(IntelCardCatalog.TedsCeilingFansId);
                return ids;
            }
        }

        /// <summary>
        /// 2026-08-31 fix: fired from each Handle*Confirmed below the instant its *Seen flag
        /// flips true, i.e. the instant CollectedLiteratesCardIds would include one more entry.
        /// PocketPanelUI subscribes to this so a card collected while THE POCKET is already open
        /// (non-modal -- it "coexists with gameplay" per its own class comment, so this is a real
        /// case, not a hypothetical) shows up immediately instead of requiring the player to
        /// close and reopen the panel to force RebuildList()'s on-open read. Previously
        /// CollectedLiteratesCardIds's only reader (PocketPanelUI.RebuildList) only ran from
        /// Open(), so this was the missing half of "can never desync from what the player has
        /// actually read" -- true across sessions, false within one if the panel stayed open.
        /// </summary>
        public event System.Action OnLiteratesCardCollected;

        private readonly List<ModalRequest> modalQueue = new();
        private bool modalShowing;

        /// <summary>
        /// 2026-08-31: lets other systems that show their own full-screen popups (currently
        /// RandomEventManager's chaos-event popup) check before firing over an FTUE beat card
        /// that's already on screen. The two systems previously had zero mutual awareness --
        /// IntelCardUI's overlay (sortingOrder 500) and RandomEventUIController's popup canvas
        /// (sortingOrder 10, confirmed via live Inspector read) don't overlap in sorting order,
        /// so this was never a "which one wins the raycast" problem, but it's still a jarring
        /// player experience for a satirical chaos event to interrupt (or queue invisibly behind,
        /// then pop the moment the FTUE card closes) a first-time-player onboarding beat. See
        /// RandomEventManager.HandleSecondTick for the consumer.
        /// </summary>
        public bool IsModalShowing => modalShowing;

        // Cached per the §19 "4a" convention (subscribe via a cached reference, never
        // FindAnyObjectByType again at teardown) -- ShopUIController has no static Instance to
        // fall back on the way CurrencyManager/WorldRestorationManager do below.
        private ShopUIController cachedShopUI;

        private void Awake()
        {
            isShuttingDown = false;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            GameObject systemsParent = GameObject.Find(SystemsParentName);
            if (systemsParent != null)
            {
                transform.SetParent(systemsParent.transform, false);
            }

            // No DontDestroyOnLoad: single-scene game with no scene reloads during play, matching
            // DialogueManager's own stated rationale for skipping it.
        }

        private void Start()
        {
            SubscribeToEvents();

            bool isFirstEverPlay = CurrencyManager.Instance != null
                && CurrencyManager.Instance.CumulativeBrainPower == 0d
                && (RebirthManager.Instance == null || RebirthManager.Instance.RebirthCount == 0);

            if (isFirstEverPlay && !bootBriefingSeen)
            {
                EnqueueModal(IntelCardSkin.COGSTerminal, CogsHeader, Beat1Body, Beat1Confirm, HandleBootBriefingConfirmed);
            }
        }

        /// <summary>
        /// Beat 9's countdown: accumulates real time (matching the rest of this codebase's use of
        /// Time.deltaTime -- Time.timeScale never changes here, per IntelCardUI's own contract, so
        /// unscaled vs. scaled makes no practical difference) toward NameRevealThresholdSeconds.
        /// Stops accumulating once requested/seen so the elapsed value freezes rather than
        /// climbing indefinitely past the threshold.
        /// </summary>
        private void Update()
        {
            if (nameRevealSeen || nameRevealRequested)
            {
                return;
            }

            nameRevealElapsedSeconds += Time.deltaTime;
            if (nameRevealElapsedSeconds >= NameRevealThresholdSeconds)
            {
                nameRevealRequested = true;
                EnqueueCard(IntelCardCatalog.TedsCeilingFansId, HandleNameRevealConfirmed);
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void SubscribeToEvents()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnFirstCashEarned -= HandleFirstCashEarned;
                CurrencyManager.Instance.OnFirstCashEarned += HandleFirstCashEarned;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
                WorldRestorationManager.Instance.OnRestorationProgressChanged += HandleRestorationProgressChanged;
            }

            cachedShopUI = FindAnyObjectByType<ShopUIController>();
            if (cachedShopUI != null)
            {
                cachedShopUI.ShopOpened -= HandleShopOpened;
                cachedShopUI.ShopOpened += HandleShopOpened;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnFirstCashEarned -= HandleFirstCashEarned;
            }

            if (WorldRestorationManager.Instance != null)
            {
                WorldRestorationManager.Instance.OnRestorationProgressChanged -= HandleRestorationProgressChanged;
            }

            if (cachedShopUI != null)
            {
                cachedShopUI.ShopOpened -= HandleShopOpened;
            }
        }

        private void HandleBootBriefingConfirmed()
        {
            bootBriefingSeen = true;
            StartCoroutine(SpawnCardAfterDelay(BootCardDelaySeconds, IntelCardCatalog.GaryMattressId, HandleCard1Confirmed));
        }

        private void HandleCard1Confirmed()
        {
            card1Seen = true;
            OnLiteratesCardCollected?.Invoke();
        }

        private void HandleShopOpened()
        {
            if (card2Seen || card2Requested)
            {
                return;
            }

            card2Requested = true;
            EnqueueCard(IntelCardCatalog.SnakeUttersId, HandleCard2Confirmed);
        }

        private void HandleCard2Confirmed()
        {
            card2Seen = true;
            OnLiteratesCardCollected?.Invoke();
        }

        private void HandleFirstCashEarned()
        {
            if (cashBeatSeen)
            {
                return;
            }

            DialogueManager.Instance?.EnqueueDirectLine(Beat4Ambient, Beat4AmbientDisplayDurationSeconds);
            StartCoroutine(SpawnCardAfterDelay(CardFollowUpDelaySeconds, IntelCardCatalog.ArmadilloSauceId, HandleCashBeatConfirmed));
        }

        private void HandleCashBeatConfirmed()
        {
            cashBeatSeen = true;
            OnLiteratesCardCollected?.Invoke();
        }

        private void HandleRestorationProgressChanged(double cumulativeSpent)
        {
            if (!restoreBeatSeen && !restoreBeatRequested && cumulativeSpent > 0d)
            {
                restoreBeatRequested = true;
                DialogueManager.Instance?.EnqueueDirectLine(Beat6Ambient, AmbientDisplayDurationSeconds);
                StartCoroutine(SpawnCardAfterDelay(CardFollowUpDelaySeconds, IntelCardCatalog.CheeseDirtId, HandleRestoreBeatConfirmed));
            }
            else if (!snottingIntelSeen && !snottingIntelRequested && IsSnottingReady(cumulativeSpent))
            {
                snottingIntelRequested = true;
                EnqueueModal(IntelCardSkin.COGSTerminal, CogsHeader, Beat8Body, Beat8Confirm, HandleSnottingIntelConfirmed);
            }
        }

        /// <summary>
        /// Reads the real Rebirth-unlock gate from RebirthManager.Instance.SnottingUnlockThreshold
        /// at call time instead of holding a second copy of it. This used to be a standalone
        /// SnottingReadyThreshold constant (50,000), independently duplicated alongside
        /// RebirthUIController's and DialogueManager's own copies and declared "by convention"
        /// intentional -- that convention is exactly what let this one drift to the pre-2026-07-30
        /// figure while the real gate moved on to 5,658,229, firing the Snotting intel card days
        /// before the mechanic was actually available. Fails closed: if RebirthManager.Instance is
        /// null, this returns false rather than guessing -- the card doesn't fire until the real
        /// gate can be checked, never early on a null ref.
        /// </summary>
        private static bool IsSnottingReady(double cumulativeSpent)
        {
            double? threshold = RebirthManager.Instance?.SnottingUnlockThreshold;
            return threshold.HasValue && cumulativeSpent >= threshold.Value;
        }

        private void HandleRestoreBeatConfirmed()
        {
            restoreBeatSeen = true;
            OnLiteratesCardCollected?.Invoke();
        }

        private void HandleSnottingIntelConfirmed()
        {
            snottingIntelSeen = true;
        }

        private void HandleNameRevealConfirmed()
        {
            nameRevealSeen = true;
            OnLiteratesCardCollected?.Invoke();
        }

        /// <summary>
        /// Restores seen-flags (and Beat 9's elapsed countdown) from a save. Runs before this
        /// component's own Start() (see Bootstrap's doc comment), so the first-ever-play
        /// boot-briefing check above always sees the correct restored bootBriefingSeen value, and
        /// Update()'s countdown resumes from the restored elapsed value rather than 0.
        /// </summary>
        public void LoadState(bool restoredBootBriefingSeen, bool restoredCard1Seen, bool restoredCard2Seen,
            bool restoredCashBeatSeen, bool restoredRestoreBeatSeen, bool restoredSnottingIntelSeen,
            bool restoredNameRevealSeen, float restoredNameRevealElapsedSeconds)
        {
            bootBriefingSeen = restoredBootBriefingSeen;
            card1Seen = restoredCard1Seen;
            card2Seen = restoredCard2Seen;
            cashBeatSeen = restoredCashBeatSeen;
            restoreBeatSeen = restoredRestoreBeatSeen;
            snottingIntelSeen = restoredSnottingIntelSeen;
            nameRevealSeen = restoredNameRevealSeen;
            nameRevealElapsedSeconds = restoredNameRevealElapsedSeconds;
        }

        private IEnumerator SpawnCardAfterDelay(float delaySeconds, string cardId, System.Action onConfirmed)
        {
            yield return new WaitForSeconds(delaySeconds);
            EnqueueCard(cardId, onConfirmed);
        }

        /// <summary>Enqueues one LITERATES card by IntelCardCatalog id (skin is always
        /// LiteratesCard). Front becomes the modal header, back the body -- IntelCardUI renders
        /// them verbatim, exactly as the removed per-beat consts did.</summary>
        private void EnqueueCard(string cardId, System.Action onConfirmed)
        {
            if (IntelCardCatalog.TryGet(cardId, out IntelCardCatalog.LiteratesCard card))
            {
                EnqueueModal(IntelCardSkin.LiteratesCard, card.Front, card.Back, card.Confirm, onConfirmed);
            }
        }

        private void EnqueueModal(IntelCardSkin skin, string header, string body, string confirmText, System.Action onConfirmed)
        {
            modalQueue.Add(new ModalRequest(skin, header, body, confirmText, onConfirmed));
            TryShowNextModal();
        }

        private void TryShowNextModal()
        {
            if (modalShowing || modalQueue.Count == 0)
            {
                return;
            }

            ModalRequest next = modalQueue[0];
            modalQueue.RemoveAt(0);
            modalShowing = true;
            StartCoroutine(ShowModalWhenClear(next));
        }

        /// <summary>Event popups outrank FTUE cards: defers showing until RandomEventManager's popup (ChaosPopUpCanvas) isn't active, retrying every EventPopupRetryDelaySeconds.</summary>
        private IEnumerator ShowModalWhenClear(ModalRequest request)
        {
            while (IsEventPopupActive())
            {
                yield return new WaitForSeconds(EventPopupRetryDelaySeconds);
            }

            IntelCardUI.Show(request.Skin, request.Header, request.Body, request.ConfirmText, () =>
            {
                request.OnConfirmed?.Invoke();
                modalShowing = false;
                TryShowNextModal();
            });
        }

        /// <summary>
        /// Reads ChaosPopUpCanvas's own Canvas.enabled directly (the exact flag
        /// RandomEventUIController.SetCanvasState toggles) rather than adding new plumbing to
        /// RandomEventManager -- matches the existing GameObject.Find-by-name pattern already
        /// used elsewhere in this codebase (e.g. BackgroundPedestrianManager's container lookup).
        /// </summary>
        private static bool IsEventPopupActive()
        {
            GameObject chaosPopupCanvas = GameObject.Find("ChaosPopUpCanvas");
            if (chaosPopupCanvas == null)
            {
                return false;
            }

            Canvas canvas = chaosPopupCanvas.GetComponent<Canvas>();
            return canvas != null && canvas.enabled;
        }
    }
}
