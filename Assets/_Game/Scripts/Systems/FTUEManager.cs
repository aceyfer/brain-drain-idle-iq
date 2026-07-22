using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainDrain.Core;
using BrainDrain.UI;

namespace BrainDrain.Systems
{
    /// <summary>
    /// §23 FTUE / comprehension pass: two narrator channels teaching the game's premise on
    /// first encounter -- main COGS (Illumisnotti propaganda, capped at exactly 2 modals total:
    /// the boot briefing and SnottingReady) and THE LITERATES resistance cards (every other
    /// beat, delivered as fake-business-front dead-drops via IntelCardUI's LiteratesCard skin).
    /// Self-bootstrapping singleton (RandomChatterManager's pattern -- Instance, isShuttingDown,
    /// parented under _Systems) that owns first-ever-play detection (identical criteria to the
    /// one DialogueManager.Start() used before this pass replaced its hardcoded boot line), beat
    /// sequencing, seen-flag gating, and one-modal-at-a-time FIFO spawning. Every beat fires at
    /// most once ever; seen-flags persist through SaveManager/PlayerData (SaveManager gathers
    /// them via the public flag properties below and restores them via LoadState -- see
    /// SaveManager.ApplyLoadedDataToSystems). All copy is verbatim from TASKLIST_DETAILS.md §23's
    /// "Creative package" section -- that document is the copy source of truth; do not paraphrase
    /// when editing these constants.
    /// </summary>
    public sealed class FTUEManager : MonoBehaviour
    {
        private const string SystemsParentName = "_Systems";

        /// <summary>Matches Bible §6's Snotting-unlock threshold (also independently duplicated in RebirthUIController.pointsSpentUnlockThreshold and DialogueManager.SnottingReadyThreshold -- by-convention parallel constants, not shared state, per this codebase's established pattern for cross-system thresholds.)</summary>
        private const double SnottingReadyThreshold = 50_000d;

        private const float CardFollowUpDelaySeconds = 2f;
        private const float BootCardDelaySeconds = 10f;
        private const float EventPopupRetryDelaySeconds = 1f;
        private const float AmbientDisplayDurationSeconds = 6f;

        private const string CogsHeader = "ILLUMISNOTTI MANDATORY BROADCAST";

        // ---- Beat 1: COGS BOOT BRIEFING (first-ever play, modal, COGS terminal skin) ----
        private const string Beat1Body =
            "GOOD MORNING, ASSET. YOU HAVE BEEN ASLEEP FOR: TOO LONG. WHILE YOU SLEPT, YOUR BRAIN WAS REZONED AS COMMERCIAL PROPERTY. " +
            "I AM COGS. I AM YOUR FRIEND. I AM ALSO LEGALLY REQUIRED TO SAY THAT. " +
            "YOUR JOB IS SIMPLE: TAP YOUR HEAD. THE JUICE COMES OUT. THE ILLUMISNOTTI COLLECT IT. EVERYONE WINS. MOSTLY THEM. THAT'S WHAT WINNING MEANS. " +
            "DO NOT READ ANYTHING. DO NOT THINK ABOUT WHERE THE JUICE GOES. TAP THE WASTELAND.";
        private const string Beat1Confirm = "OK, HARVEST ME";

        // ---- Beat 2: CARD #1 (~10s after briefing closes, card skin) ----
        private const string Beat2Front = "GARY'S DISCOUNT MATTRESS EMPORIUM — \"We Also Have Soup\"";
        private const string Beat2Back =
            "They're metering your head. But here's what the tin can won't tell you: every tap leaks a little light back into the world. Watch the sky. It remembers.\n" +
            "Don't let COGS pick what you buy. Don't let COGS pick anything.\n" +
            "— The Literates\n" +
            "p.s. burn after reading. actually don't. read it twice. reading twice is how we got like this. the good version of like this.";
        private const string Beat2Confirm = "I READ IT. ALL OF IT.";

        // ---- Beat 3: CARD #2 (first shop open, card skin) ----
        private const string Beat3Front = "SNAKE UTTERS WHOLESALE — \"Ask About Our Utters\"";
        private const string Beat3Back =
            "Buildings make juice while you nap. COGS calls that \"theft of company time.\" Do it anyway — sleeping on the job is the only job worth having.\n" +
            "Buy cheap ones first. The math is friendlier. We checked. We're the last people who check math.\n" +
            "— TL";
        private const string Beat3Confirm = "MATH CONFIRMED";

        // ---- Beat 4: COGS ambient (FirstCashEarned, regular narrator panel, NOT modal) ----
        private const string Beat4Ambient =
            "ALERT: YOU HAVE DISCOVERED \"CASH.\" DO NOT CONVERT BRAIN POWER INTO CASH. CASH LEADS TO BUYING. BUYING LEADS TO CHOICES. CHOICES LEAD TO THINKING. I AM WATCHING YOU, SPECIFICALLY.";

        // ---- Beat 5: CARD #3 (FirstCashEarned, modal, fires a beat after Beat 4) ----
        private const string Beat5Front = "ARMADILLO SAUCE LEGAL SERVICES — \"It Goes With Everything, Including Court\"";
        private const string Beat5Back =
            "It just told you not to convert, didn't it. Funny how the thing metering your head panics when you spend what's yours.\n" +
            "Convert. Buy. Repeat. That's the whole machine. Now it's your machine.\n" +
            "— TL";
        private const string Beat5Confirm = "MY MACHINE NOW";

        // ---- Beat 6: COGS ambient (FirstRestoreSpend, regular narrator panel, NOT modal) ----
        private const string Beat6Ambient =
            "YOU SPENT YOUR POINTS ON... FIXING THINGS? THE ILLUMISNOTTI HAVE REVIEWED YOUR PURCHASE AND FILED IT UNDER \"ADORABLE.\" CARRY ON. IT'S A ROUNDING ERROR.";

        // ---- Beat 7: CARD #4 (FirstRestoreSpend, modal) ----
        private const string Beat7Front = "CHEESE DIRT MEMORIAL FOUNDATION — \"Never Forget The Flavor\"";
        private const string Beat7Back =
            "Every point you put into the world makes the streets a little smarter and their grip a little weaker. They allow it because they think it's a rounding error.\n" +
            "Be a rounding error. Be the biggest rounding error they've ever seen.\n" +
            "— TL";
        private const string Beat7Confirm = "ROUNDING UP";

        // ---- Beat 8: COGS CORE INTEL #2 (SnottingReady, modal, COGS terminal skin -- the ONLY other COGS modal) ----
        private const string Beat8Body =
            "MANDATORY NOTICE: YOU NOW QUALIFY FOR THE SNOTTING. YOUR BRAIN WILL BE REPOSSESSED, WIPED, AND REISSUED WITH A PRODUCTIVITY MULTIPLIER. " +
            "YOU WILL LOSE: EVERYTHING. YOU WILL GAIN: MORE OF EVERYTHING, FASTER. " +
            "THE ILLUMISNOTTI CALL THIS \"A PROMOTION.\" PARTICIPATION IS VOLUNTARY, WHICH IS OUR FAVORITE KIND OF MANDATORY.";
        private const string Beat8Confirm = "OK, REPOSSESS ME (LATER)";

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

        // Transient (not persisted) guards against double-enqueuing the same beat while it's
        // already queued/showing but not yet confirmed -- e.g. the player opening the shop twice
        // in quick succession before Beat 3's card is confirmed. Persisted *Seen flags alone only
        // gate re-firing across sessions/after confirmation; these close the same-session gap.
        private bool card2Requested;
        private bool restoreBeatRequested;
        private bool snottingIntelRequested;

        public bool BootBriefingSeen => bootBriefingSeen;
        public bool Card1Seen => card1Seen;
        public bool Card2Seen => card2Seen;
        public bool CashBeatSeen => cashBeatSeen;
        public bool RestoreBeatSeen => restoreBeatSeen;
        public bool SnottingIntelSeen => snottingIntelSeen;

        private readonly List<ModalRequest> modalQueue = new();
        private bool modalShowing;

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
            StartCoroutine(SpawnCardAfterDelay(BootCardDelaySeconds, IntelCardSkin.LiteratesCard, Beat2Front, Beat2Back, Beat2Confirm, HandleCard1Confirmed));
        }

        private void HandleCard1Confirmed()
        {
            card1Seen = true;
        }

        private void HandleShopOpened()
        {
            if (card2Seen || card2Requested)
            {
                return;
            }

            card2Requested = true;
            EnqueueModal(IntelCardSkin.LiteratesCard, Beat3Front, Beat3Back, Beat3Confirm, HandleCard2Confirmed);
        }

        private void HandleCard2Confirmed()
        {
            card2Seen = true;
        }

        private void HandleFirstCashEarned()
        {
            if (cashBeatSeen)
            {
                return;
            }

            DialogueManager.Instance?.EnqueueDirectLine(Beat4Ambient, AmbientDisplayDurationSeconds);
            StartCoroutine(SpawnCardAfterDelay(CardFollowUpDelaySeconds, IntelCardSkin.LiteratesCard, Beat5Front, Beat5Back, Beat5Confirm, HandleCashBeatConfirmed));
        }

        private void HandleCashBeatConfirmed()
        {
            cashBeatSeen = true;
        }

        private void HandleRestorationProgressChanged(double cumulativeSpent)
        {
            if (!restoreBeatSeen && !restoreBeatRequested && cumulativeSpent > 0d)
            {
                restoreBeatRequested = true;
                DialogueManager.Instance?.EnqueueDirectLine(Beat6Ambient, AmbientDisplayDurationSeconds);
                StartCoroutine(SpawnCardAfterDelay(CardFollowUpDelaySeconds, IntelCardSkin.LiteratesCard, Beat7Front, Beat7Back, Beat7Confirm, HandleRestoreBeatConfirmed));
            }
            else if (!snottingIntelSeen && !snottingIntelRequested && cumulativeSpent >= SnottingReadyThreshold)
            {
                snottingIntelRequested = true;
                EnqueueModal(IntelCardSkin.COGSTerminal, CogsHeader, Beat8Body, Beat8Confirm, HandleSnottingIntelConfirmed);
            }
        }

        private void HandleRestoreBeatConfirmed()
        {
            restoreBeatSeen = true;
        }

        private void HandleSnottingIntelConfirmed()
        {
            snottingIntelSeen = true;
        }

        /// <summary>
        /// Restores seen-flags from a save. Runs before this component's own Start() (see
        /// Bootstrap's doc comment), so the first-ever-play boot-briefing check above always
        /// sees the correct restored bootBriefingSeen value.
        /// </summary>
        public void LoadState(bool restoredBootBriefingSeen, bool restoredCard1Seen, bool restoredCard2Seen,
            bool restoredCashBeatSeen, bool restoredRestoreBeatSeen, bool restoredSnottingIntelSeen)
        {
            bootBriefingSeen = restoredBootBriefingSeen;
            card1Seen = restoredCard1Seen;
            card2Seen = restoredCard2Seen;
            cashBeatSeen = restoredCashBeatSeen;
            restoreBeatSeen = restoredRestoreBeatSeen;
            snottingIntelSeen = restoredSnottingIntelSeen;
        }

        private IEnumerator SpawnCardAfterDelay(float delaySeconds, IntelCardSkin skin, string header, string body, string confirmText, System.Action onConfirmed)
        {
            yield return new WaitForSeconds(delaySeconds);
            EnqueueModal(skin, header, body, confirmText, onConfirmed);
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
