# BRAIN DRAIN — TASKLIST DETAILS (Child)

Parent: `TASKLIST.md`. This file holds the "why," the acceptance criteria, the decision log, and the session changelog. The task force reads the parent first; come here only for the task you're on.

---

## §1 CanvasGuard fix — RESOLVED 2026-07-09
**Problem:** CanvasGuard.OnHierarchyChanged → EnforceOverlayOnAllCanvases runs during Play mode, throws `InvalidOperationException` via `MarkSceneDirty`, AND reverts Tab_BP's canvas.
**Actual diagnosis (revised the original problem framing):** CanvasGuard only ever checks `Canvas.renderMode`, never `.enabled` — it was never actually incompatible with the nested-per-tab-canvas pattern (Cash/RP's canvases, both correctly Overlay, were never flagged). The real trigger was two separate things: (a) no Play-mode guard anywhere in the file, so any `GameObject.SetActive()` firing `EditorApplication.hierarchyChanged` during Play re-ran the scan; (b) Tab_BP's Canvas had a genuine pre-existing defect — saved as `renderMode 2` (WorldSpace) instead of `0` (Overlay) like Cash/RP — which CanvasGuard was correctly (if unsafely) trying to revert. No exemption for nested sub-canvases was needed or added.
**Fix landed:** play-mode guard + try/catch chokepoint in `EnforceOverlayOnAllCanvases()` (commit `1d2b100`). Tab_BP's `renderMode` corrected to 0 as its own separate commit (`da6809f`), since that's the actual root-cause data fix, distinct from CanvasGuard's own safety bug.
**Accept:** tab clicks produce no CanvasGuard warning, no exception, confirmed via full re-test (§4).

## §2 Stray giant shop element — RESOLVED 2026-07-09
**Cause confirmed:** the stray element *was* Tab_BP — not an orphaned child of the old single-scroll `ShopScrollView` branch as originally suspected. Tab_BP's `Canvas.renderMode` was saved as `2` (WorldSpace) instead of `0` (ScreenSpaceOverlay) like Tab_Cash/Tab_RP — a pre-existing scene defect, not something the shop collapse introduced. A WorldSpace canvas renders at whatever world-space scale/position its RectTransform implies rather than screen-space, which is exactly "shop-row-like element rendering huge in world space above the pedestrians."
**Fix:** `Canvas.m_RenderMode` 2 → 0 on Tab_BP, matching Cash/RP exactly (commit `da6809f`). No hierarchy path was ever needed in the end — found by comparing the three tab canvases' serialized values directly.
**Confirmed gone** in the full re-test (§4).

## §3 FindObjectsSortMode console line — CLOSED (by inference, not literal confirmation)
The literal console line was requested twice and never actually provided (both times came through as an unfilled `[PASTE ...]` placeholder). Grepped the entire project for the actually-deprecated legacy APIs (`FindObjectOfType`/`FindObjectsOfType`) that would produce a `FindObjectsSortMode`-mentioning deprecation notice — zero matches anywhere in `Assets/_Game/Scripts`. All in-project uses of the modern API (`FindAnyObjectByType`, `FindObjectsByType`) are already correct and non-deprecated, including `AutoSceneFixes.RemoveDuplicateRandomEventManagers()`'s own `FindObjectsByType<RandomEventManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)` call. Whatever produced that message is not our code. Closing on the strength of the full clean re-test (§4) coming back with no unexplained console warnings reported — if it resurfaces, reopen with the actual line.

## §4 Full clean re-test checklist — PASSED 2026-07-09
Tabs switch clean, buys work on all 3 tabs, close/reopen correct, no exceptions, stray element confirmed gone (§2). Reported green in full by Aceyfer.

## §5 Commit train — LANDED 2026-07-09
Final order and hashes:
1. `439134d` — NumberFormatter sub-1 decimal fix
2. `7c9df6f` — GaryBubbleUI raycast-blocking + polish
3. `ed0f41f` — HUDController cold-boot rank sync
4. `1d5b9f5` — ShopUIController + ShopTabView collapse fixes (Awake build, scene-wide guard, Canvas/GraphicRaycaster ownership)
5. `da6809f` — Tab_BP renderMode fix (own commit, own hunk, staged via isolated patch apply since it shared a file with commit 6's changes)
6. `27fb988` — PopulateBuildingTemplates tool + 9-building wiring + ShopTabView component removal + ShopTabBar reparent + font auto-size
7. `d77c29c` — ShopThreeTabWireFix Play-mode guard
8. `1fc4604` — AutoSceneFixes chokepoint hardening
9. `1d2b100` — CanvasGuard Play-mode guard
10. (this commit) — Bible §8 scar-tissue updates + TASKLIST/TASKLIST_DETAILS sync
TMP font assets deliberately left uncommitted throughout, per the Bible's standing "never commit TMP font assets" rule.

## §6 BG1-BG6 sprite import — RESOLVED 2026-07-12
**Corrected diagnosis:** the actual fix was Texture Importer **Sprite Mode: Multiple → Single** (not Texture Type, which was already correctly set to Sprite (2D and UI)) on all six backgrounds — Multiple mode left `spriteID` empty and never generated the `fileID: 21300000` sub-asset every scene reference expected. Fixed on all six, not just BG1: `Assets/_Game/Sprites/Backgrounds/BG1.jpg.meta` through `BG6.png.meta`.
**Result:** healed 7 dangling scene references in one shot — the 6x `BackgroundStageView.stageSprites[]` entries plus `SkylineBG`'s own `m_Sprite` reference — without touching `SampleScene.unity` at all (the scene already pointed at the correct GUID/fileID combination; the asset just wasn't producing that fileID until the import mode changed).
**Commit:** `36c76c1`. Six `.meta` files, nothing else.

## §7 Doc sync
- Bible §6 economy table: 7 → 16 buildings (add JumperCables, DefrostDrip, CranialMicrowave, SynapseSpaceHeater, CryoSludgeEspresso, IQOverclockChip, LemonadeGriftStand, DoomscrollBillboard, HOAProtectionRacket).
- CLAUDE.md inventory: add Gary system, PremiumShopManager, chapter system (COGSStage/ChapterManager/IllumisnottiManagerUI), pedestrian stack, EventBus.
- Note shop-family split: BP/Cash/RP tab shop (ShopUIController) vs Shop-3/Points family (PointsShopPanel/CashShopPanel/ConvertPanel/PremiumShopPanel) — different systems, don't conflate.

## §8 Unguarded Editor tools (12)
SceneManagerWiring, ShopPanelLayoutFix, PlaceholderArtGenerator, PedestrianAlphaTest, HUDMobileOverhaul, FixCOGSDialogueLayout, ConsolidateShopButton, COGSPortraitWireFix, RemoveMissingSceneScripts, MainUIControllerWireFix, PopulateBuildingTemplates, VisualPolishFix (6 items). Add the same isPlaying/isPlayingOrWillChangePlaymode guard pattern to each entry point. One batch commit. Pattern rule (Bible): wrap Editor-only API calls in try/catch at the chokepoint too — flags alone race.

## §9 PremiumShopManager vs GodTierStoreManager — RESOLVED 2026-07-10
**Verdict:** duplicated responsibility, both claimed premium purchases. Resolution: `GodTierStoreManager` is the sole surviving premium manager; the entire `PremiumShopManager`/`PremiumShopUIController`/`PremiumShopSlotUI` trio + its slot prefab were deleted, and every code reference (`EconomyManager`, `HUDController`, `VisualPolishFix`) was stripped and repointed at `GodTierStoreManager`. Folded into and closed by §16 Phase A — see there for commit hashes.

## §10 DECISION — No premium soft currency ("neurons") — RESOLVED 2026-07-10
**Decision (2026-07-09, Aceyfer):** "Neurons" premium currency was never approved and is rejected. ALL premium purchases are direct real-world currency. Example: Bad Words Pack = **$5.00 USD**, not 50 neurons.
**Rationale:** owner intent; simpler economy; no soft-currency obfuscation layer.
**Done:** neuron currency purged repo-wide (code + CLAUDE.md), Bad Words Pack landed in God Tier Store at direct $5.00, the 2,500-Cash ProfanityPack (an in-game-currency path to profanity content, violating the no-soft-currency rule on its own terms even without literal "neurons") was killed outright rather than repriced. See §16 Phase A for commit hashes.
**Note:** direct-price IAP is fine on both stores; price tiers are set in App Store Connect/Play Console, not hardcoded — code should reference product IDs, not dollar amounts (see §12).

## §11 First-playable cut line
Proposal to react to (Aceyfer decides): v1 ships = core click loop, 6-stage World Restoration, 16-building shop, ranks, Gary + COGS barks, rebirth, offline decay. v1 waits = ShopTabView virtualization, Shop-3/Points polish, chapter art beyond current, Bad Words Pack (needs §12). Write the verdict here, then the Bible First-Playable Checklist becomes the single source.

## §12 IAP wiring
Direct-currency products via Unity IAP (or store-native): define product IDs (e.g. `bd_badwords_pack`), configure price tiers in store consoles, route purchase → entitlement flag → content unlock. No neurons anywhere (§10). Needs Apple/Google dev accounts + tax/banking setup — start that paperwork early, it has multi-day lead time.

## §13 Art debt
- COGS Level 1 portrait (reference-anchoring problem unsolved — pipeline: Leonardo Cinematic Kino → Grok → rembg).
- Sweep for remaining placeholder art (PlaceholderArtGenerator output still live anywhere?).
- BG1 fixed in §6.

## §14 Device build
iOS target already selected in Editor. Build to a real device early — safe-area (CustomSafeArea), tap targets, auto-size text at 1080x1920, and performance with pedestrians are all things editor Play mode under-tests. Log device issues as new tasks; don't hotfix unlogged.

## §15 Store presence
Name check (trademark/collision), age rating questionnaire (satire + "Bad Words Pack" likely bumps rating — check before finalizing), 5–8 screenshots, short + long description, privacy policy URL (required by both stores), AcEclipse Games publisher identity.

## §16 Replace third shop tab with "God Shop" real-currency store — SHIPPED 2026-07-11 (decided 2026-07-09, Phase A complete 2026-07-10, Phase B complete 2026-07-11)
**Decision:** the RP/World Restoration shop tab is cut. That third tab slot becomes the **God Shop** (direct real-world currency only, per the no-neurons decision in §10, backed solely by `GodTierStoreManager`). World Restoration progression itself stays in the game — it's just no longer presented as a shop tab; how/where it surfaces instead is undecided, not scoped here.
**Why now:** the RP tab's only remaining bug (BuildRestorationTab() Awake-order race, fixed in commit `5572122`) became moot the moment this decision landed — no point polishing a tab that's being replaced.

**Phase A — Consolidation — COMPLETE 2026-07-10.** §9's audit (do both `PremiumShopManager` and `GodTierStoreManager` claim premium purchases?) ran first and confirmed duplication; everything below is its resolution plus the no-neurons purge from §10, landed together as one arc:
1. `939222f` — Bad Words Pack migrated into God Tier Store as a direct $5.00 real-currency item (first God Shop item).
2. `1024b91` — 2,500-Cash `ProfanityPack` killed (an in-game-currency path to the same content the God Shop now sells directly — had to go regardless of the literal "neuron" wording).
3. `eb8a638` — `PremiumShopPanel`/`PremiumShopButton`/`PremiumShopManager` component removed from the scene (math-audited pure subtraction, see the scene-audit method in Bible §8).
4. `0b5048a` — all code references to the PremiumShop trio stripped; `EconomyManager` premium tracking repointed to `GodTierStoreManager`.
5. `cf51935` — the retired `PremiumShopManager`/`PremiumShopUIController`/`PremiumShopSlotUI` source files + slot prefab deleted outright.
6. `34841b7` — neuron premium currency purged repo-wide (`CurrencyManager`, `EventBus`, `EconomyManager`, `CLAUDE.md`).
Doc closeout for Phase A: `516ce70`.

**Phase B — Build the God Shop tab UI — COMPLETE 2026-07-11.** Backed solely by `GodTierStoreManager`; no second manager, no scene-anchor guessing — reused the geometry/draw-order-owned-in-code pattern from the BP/Cash tab work (Bible §8) rather than hand-authoring anchors again.
1. `8d15c22` (**B1**) — third tab retargeted from RP to `ShopTab.GodShop`; runtime `GodTierStoreSlotUI` template built with clone-refs-captured-first (the retired `RestorationSlotUI` runtime template read its field references directly off the *source* prefab instead of off the clone it just instantiated — a live footgun now documented as a do-not-copy pattern, since editing the clone's fields wrote into the wrong object). Bad Words Pack's owned→profanity-toggle affordance (tap an owned row to flip `RandomChatterManager.ProfanityEnabled`) rebuilt here, replacing the dead `CashShopSlotUI` branches (see deferred list below).
2. `0f0809c` (**B2**) — tab buttons render a single code-owned label. `Tab_RP`'s button carried RP-era leftover children (a progress readout, a convert icon) that stacked into unreadable clutter once the label text changed to "GOD SHOP" — fixed by having code own the entire button face (disable all pre-existing children, render one label) rather than chasing whatever a saved scene happens to have.
3. `9cb8075` (**B3**) — the rebirth trigger button suppressed while the shop is open. It draws above the tab bar (established sorting order) and was burying the "GOD SHOP" tab label whenever both were visible. Suppression owned by `RebirthUIController`'s own visibility gate — a sole-owner pattern (one flag, one place setting it) rather than two systems fighting over the same button's `SetActive` state.
4. `de5d4c0` (**B4**) — `➔` replaced with `->` in `ConvertUIController`'s status text. The glyph isn't in `LiberationSans SDF`'s character set, so TMP fell back to a substitute font and spammed console warnings every refresh.

## §17 COGS portrait/dialogue visibility — RESOLVED 2026-07-12
**Symptom as originally reported:** COGS portrait rendered when Play Mode *stopped*, not during Play — inverted visibility.
**Root causes, three stacked** (each one only became visible once the prior was fixed):
1. **Duplicate-destroy of shared host** (`1e01517`) — `COGSPortraitController`'s duplicate-singleton guard destroyed the *shared host GameObject* rather than just its own component, and its `Find` call excluded inactive objects — so a correctly-configured-but-inactive instance lost to an empty auto-bootstrapped stand-in. Fixed: destroy the component, not the host; `Find` now includes inactive; a configured instance always beats an empty auto-host.
2. **Awake self-deactivation froze the co-hosted controller** (`7fac5d8`) — `DialogueDisplayUI` self-`SetActive(false)`'d its own panel to hide it, but `COGSPortraitController` lives on the *same* GameObject — deactivating it froze `COGSPortraitController.Start()` before it ever ran, and (per the singleton-teardown pattern) let an `(Auto)` impostor spawn instead. Fixed: the dialogue panel now stays permanently active; visibility is owned by its own offscreen anchored-position, not `SetActive`.
3. **Boot-nub regression from fix 2** (`05c7c13`) — moving hidden-state ownership to position math broke specifically at `Awake()`, because `rect.width` reads `0` before the first layout pass runs, so the offscreen-position math computed a wrong (still-onscreen) position at boot. Fixed: hidden state is now gated by `CanvasGroup.alpha`, independent of any layout-dependent size read.
4. **Image serialized `m_Enabled: 0`** (`5d3085c`) — separately, `COGSWorldPortraitUI`'s own `Image` component was saved disabled in the scene, so even with the controller/visibility chain fully correct, nothing would render. Fixed: `enabled` asserted explicitly at the point of use (same "own it in code, don't trust scene state" pattern as the shop work), not left to whatever the scene happened to save.
**Commits:** `1e01517`, `7fac5d8`, `05c7c13`, `5d3085c`. Closes §17.

## §18 Oversized pedestrians
Flagged by Aceyfer, not yet diagnosed. No detail beyond the report itself yet — needs its own investigation pass.

## §19 Deferred — dead code awaiting explicit rip approval, and open cosmetics
Nothing here blocks anything; logged so it doesn't get silently forgotten or opportunistically ripped mid-unrelated-commit.

**Dead code, retained pending explicit approval to remove:**
- `RestorationSlotUI` — the RP-tab slot prefab/script, unused now that the third tab is the God Shop. Also the source of the clone-refs-captured-first footgun documented in §16 Phase B1 — worth reading before anyone reaches for this class as a template again.
- `GodTierStoreUIController` — superseded by the God Shop tab work landing directly in `ShopUIController`; not wired to anything live.
- `CashShopSlotUI`'s seven dead `itemId == "profanity_pack"` special-case branches — still retained per the dead-code convention (§10 decision log), still slated for the still-PARKED Cash-family reconciliation, not this pass.
- `ShopQuery`/`ShopTabView`'s own, separate `ShopTab` enums — both still say `RpRestorations`, not `GodShop`. These are part of the dormant `ShopTabView` virtualization system (parked, per `TASKLIST.md`'s PARKED list), not `ShopUIController`'s enum (which was renamed in B1) — don't conflate the two when this family eventually gets reconciled.

**Open cosmetics, not blocking:**
- God Shop tab labels currently render in TMP's default fallback font rather than the project's usual font asset — a one-line font-asset assignment if the visual mismatch bothers us; not diagnosed as broken, just unstyled.
- The rebirth-unlock threshold (50,000 RP spent, per Bible §4) has not actually been tested in an unlocked state since the RP tab was cut — worth a real Play-mode pass to confirm the "SNOTTING" trigger button still correctly reveals itself at that threshold now that RP is no longer a shop tab.

**Deferred, audited not fixed (added 2026-07-12, off the §17 COGSPortraitController hardening pass):**
- `GodTierStoreManager`'s `Instance` auto-bootstrap has an `isShuttingDown` guard scoped to app-quit only — a scene-transition teardown (loading a second scene) could still spawn a ghost auto-host, the same class of bug just fixed in `COGSPortraitController`. **Currently unreachable**: the project is single-scene, so there's no scene-transition teardown path to trigger it [Codex audit F6, verified]. Revisit if a second scene is ever added.
- The same self-bootstrapping-singleton pattern template that caused the §17 bug exists in other managers beyond `COGSPortraitController` (now hardened) — worth a pattern-level review pass across all of them, but only once a second scene actually makes the failure mode reachable; not urgent while single-scene.
- `ShopUIController.restorationSlotPrefab` — a dead serialized field (RP-tab-era leftover, cut when the third tab became the God Shop) still sits in the saved scene. Unity will drop it automatically the next time the scene gets a legitimate save; not worth a dedicated commit to remove by hand.
- Remove COGSWorldPortraitUI GameObject (scene fileIDs 706123450000000001-005, direct Canvas child) + COGSWorldPortraitUI.cs + its .meta once scene-save embargo lifts. Script currently asserts the Image off.

## §20 Dialogue pacing/queue in DialogueManager
Lines currently interrupt each other — no queue, no minimum-gap pacing, no flood coalescing when multiple triggers fire close together. Goal: add a queue + minimum-gap pacing + flood coalescing (collapse rapid-fire triggers into one line rather than stacking/interrupting). Needs a `DialogueManager` audit first, then design sign-off from Aceyfer before implementation. Not started.

## §21 World portrait position tune
The world portrait's position is code-owned (per §17's fixes) but may overlap THE SNOTTING (rebirth trigger) button on screen — pending Aceyfer's visual check in Play Mode before this is scoped as a real bug or closed as fine. Not started.

---

## DECISION LOG
- 2026-07-09 — Shop collapse: ShopUIController is the sole shop system; ShopTabView dormant + detached until virtualization AND real purchase routing exist. Guard in code is the switch.
- 2026-07-09 — No premium soft currency; all premium purchases direct real-world pricing (§10).
- 2026-07-09 — Scene YAML surgery (reparents/anchor-sensitive) = Editor-hands work, never raw YAML edits by agents. Component strips allowed only with verified zero dangling references.
- 2026-07-09 — RP/World Restoration shop tab cut; slot becomes Premium direct-currency store (§16). Restoration progression stays in the game, just not as a shop tab. RP tab's lazy-rebuild fix (`5572122`) is kept in place as harmless — see changelog below for why.
- 2026-07-10 — Store named **"God Shop"**. Direct real currency only, no soft-currency layer, backed solely by `GodTierStoreManager`. Bad Words Pack ($5.00) is its first item.
- 2026-07-10 — The 2,500-Cash `ProfanityPack` was killed, not repriced — it was an in-game-currency path to the same content the God Shop now sells directly, which violates the no-soft-currency-path-to-premium-content rule (§10, hardened by §7 below) regardless of whether it was literally called "neurons."
- 2026-07-10 — `CashShopSlotUI`'s seven dead `itemId == "profanity_pack"` special-case branches are **retained**, not deleted, per the project's standing dead-code convention (leave commented/flagged dead branches for the specific pass that owns cleaning them up, don't opportunistically strip in an unrelated commit) — they'll be stripped during the still-PARKED Cash-family reconciliation, not now.
- 2026-07-10 — Phase B requirement logged: the God Shop slot UI must reimplement the owned→profanity-toggle affordance the old Cash slot used to own (tap an owned row to flip `RandomChatterManager.ProfanityEnabled`), since that behavior currently has no home now that the branches implementing it are dead.
- Standing — Claude Code is sole code editor. One change, one commit. Diagnose before changing. No destructive git without explicit confirmation. Editor/Inspector work = Aceyfer (+ Unity AI when credits allow).

## CHANGELOG (this session, 2026-07-09)
- Landed: commits 1–3 (NumberFormatter sub-1 currency, Gary bubble tap-blocking + hold-time, HUDController rank cold-boot sync).
- Editor work done: 3× ShopTabView components removed (verified clean), ShopTabBar reparented under ShopPanel — reparent later confirmed to have never actually persisted (see below), superseded by the geometry-in-code fix.
- Found & fixed en route: ShopTabView guard was a structural no-op (ancestry vs existence); ShopUIController never toggled Canvas.enabled; tab bar sibling order lost raycasts to MainTapButton; AutoSceneFixes delayCall race.
- Closed as pre-existing/harmless: COGSPortraitController duplicate-destroy warning, BG1 warning (fix queued §6).
- Resolved: CanvasGuard fight (§1) — play-mode guard, not a nested-canvas incompatibility. Stray element (§2) — was Tab_BP stuck in WorldSpace renderMode, same root cause as §1's revert target. FindObjectsSortMode (§3) — closed by inference, source never identified (see §3 note).
- Full re-test (§4) passed green. Commit train (§5) landed — 9 commits.

### Post-train fixes (same session, continued after §5 landed)
- `c25fa63` — **Boot ownership**: ShopRoot (Tab_BP/Cash/RP + ShopTabBar) is a sibling of shopPanel, not a descendant — shopPanel.SetActive() never reached it, so tab content and the tab strip rendered/raycast at all times, shop open or closed. ShopUIController now resolves shopRoot automatically and activates/deactivates it in lockstep with shopPanel. Also: `27fb988`'s claimed ShopTabBar reparent never actually persisted (corrected in `2f3fd06`) — fixed for real here via a pure sibling reorder (ShopRoot moved after MainTapButton in CustomSafeArea), not a reparent.
- `e930d37` — **Layout-group corruption** (third scene-smuggling instance, logged `38a61e1`): all 16 buildings were correctly instantiated every time, but Content's VerticalLayoutGroup was saved disabled on all three tabs, so every row landed at the same position and fully overlapped ("one row per tab"). Cause: a coroutine meant to disable the layout group after one frame got killed by shopPanel deactivating before it could resume, yet the scene had it disabled anyway from an earlier successful run that got baked into a save. Fixed by re-enabling all three layout groups and deleting the coroutine mechanism entirely rather than repairing its timing.
- `e0b0401` — **Geometry moved to code**: Tab_BP/Cash/RP were anchored full-screen while shopPanel is bottom-60% — two independently-sized overlapping rects. Rather than re-editing scene anchors (two prior scene-edit reports this session did not persist as expected, most notably `27fb988`), ShopUIController now normalizes each tab panel's anchors to match shopPanel exactly in code at Awake, making saved scene anchors irrelevant going forward.
- `86bfa95` + `678aacb` — **Sorting layers**: the geometry fix exposed that shopPanel's own near-opaque, raycastTarget=true background (kept exactly as specified — the deliberate catch-all for any gap in the scroll content, so nothing falls through to MainTapButton) was rendering on top of and blocking the now-correctly-sized tab content. Fixed via `Canvas.overrideSorting` owned in code, not sibling order: tab content sorts above the backdrop, closeButton sorts above the tab content in turn (its corner overlaps the scroll area post-geometry-fix, so it needed its own explicit priority). Landed in two commits — the first covered BP only, because the sortingOrder assignment happened once in Awake while Cash/RP sat disabled from boot until first selected; `678aacb` moved the assignment into the same place `.enabled` was already being reasserted on every tab switch, closing the gap for Cash/RP/any future tab uniformly.
- `5572122` — **RP lazy-build**: BuildRestorationTab() lost an Awake-order race against WorldRestorationManager and latched an empty result. Fixed with a lazy retry on first RP tab selection rather than depending on any particular boot ordering. **Superseded same session** by the decision to cut the RP tab entirely (§16) — kept in place as harmless dead code (see decision log).

Tasks 1–5 (Shop Collapse Endgame) all complete — BP and Cash tabs fully verified end to end (open/close, tab switch, buy on both tabs, no exceptions). RP tab superseded by design decision, not left as an unresolved bug. Next up per TASKLIST.md: §6 (BG1 texture import), §7 (doc sync), §8 (batch-guard remaining 12 unguarded Editor tools), §16 (Premium store tab, starts with §9's audit), §17 (COGS portrait inverted visibility), §18 (oversized pedestrians).

### God Shop Phase A — consolidation (session, 2026-07-10)
Applied as a series of pre-generated patches, each verified (diff-audited against an expected fileID/name set, not eyeballed — see Bible §8's scene-audit method entry) before commit:
- `939222f` — Bad Words Pack migrated into God Tier Store as a direct $5.00 real-currency item.
- `1024b91` — 2,500-Cash ProfanityPack removed from CashShopManager (scene reference + asset + .meta).
- `eb8a638` — PremiumShopPanel/PremiumShopButton/PremiumShopManager component removed from the scene. First attempt at this (Editor-hands, uncommitted) came back contaminated with unrelated Win-window geometry drift and PointsConversionGroup fileID churn from a Unity resave — discarded via `git checkout --` and redone from a clean, pre-built patch instead of trying to hand-clean the contaminated save. See Bible §8's scene-smuggling instance #4 for the full account.
- `0b5048a` — code references to the PremiumShop trio stripped (`VisualPolishFix.cs`, `HUDController.cs`, `EconomyManager.cs`); EconomyManager premium tracking repointed to GodTierStoreManager. This patch was generated before `eb8a638` but only applied after — handed over right as the prior session hit its limit, before it could be applied; a rerun of the trio-deletion commit correctly stopped and reported when its own sanity grep caught the still-live references, which is what surfaced this ordering gap.
- `cf51935` — PremiumShopManager.cs/.meta, PremiumShopUIController.cs/.meta, PremiumShopSlotUI.cs/.meta, and the PremiumShopSlotUI prefab/.meta deleted outright. Sanity grep after: zero live references anywhere in `Assets` except the expected `HUDMobileOverhaul.cs` name-string (`Find("PremiumShopButton")`) and one explanatory comment.
- `34841b7` — neuron premium currency purged repo-wide: `CurrencyManager.cs`, `Core/Events/EventBus.cs`, `Systems/EconomyManager.cs`, `CLAUDE.md`. Verified zero case-insensitive "neuron" hits across `Assets/_Game/Scripts` and `CLAUDE.md` before commit.
All six pushed to `origin/main` (`8828543..34841b7`). Phase B (build the actual God Shop tab UI) is scoped in §16 above, not started.

### God Shop Phase B — build-out (session, 2026-07-11)
Also applied as pre-generated patches, each verified against an exact expected file list before commit — see §16 Phase B for the full per-commit detail:
- `8d15c22` (B1) — God Shop tab UI + owned-toggle affordance.
- `0f0809c` (B2) — code-owned single tab-button label.
- `9cb8075` (B3) — rebirth-trigger suppression while the shop is open. First patch attempt for this (v1) was generated against a stale pre-B2 base and failed `git apply` atomically — caught by apply-atomicity itself, nothing partially written, no cleanup needed; a corrected v2 patch (regenerated against `0f0809c`, stash-verified) applied clean. See Bible §8's patch-handoff mirror rule, added specifically off this incident.
- `de5d4c0` (B4) — convert-arrow-glyph replacement, closing out the last known cosmetic rough edge from the shop work.
All four pushed to `origin/main` (`8d15c22..de5d4c0`). §16 is now fully shipped, both phases. Deferred dead-code ledger and open cosmetics logged in §19, not ripped/fixed this pass.
