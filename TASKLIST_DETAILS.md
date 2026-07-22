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

## §8 Unguarded Editor tools (12) — RESOLVED 2026-07-15
SceneManagerWiring, ShopPanelLayoutFix, PlaceholderArtGenerator, PedestrianAlphaTest, HUDMobileOverhaul, FixCOGSDialogueLayout, ConsolidateShopButton, COGSPortraitWireFix, RemoveMissingSceneScripts, MainUIControllerWireFix, PopulateBuildingTemplates, VisualPolishFix. Audit found **19 MenuItem entry points** across the 12 (COGSPortraitWireFix has 2, MainUIControllerWireFix 3, VisualPolishFix 5), all with zero play-mode checks. **Fixed (`e1ba781`, batch per tasklist):** new shared `EditorToolGuard.BlockedByPlayMode()` helper (global namespace, reachable without usings; same `Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode` pair AutoSceneFixes uses) + a one-line guard at the top of every entry point. Pure additions, no existing logic touched. The Bible-§8 try/catch-chokepoint half of the pattern applies to *deferred* execution (delayCall) — only AutoSceneFixes has that path and it was already guarded; noted in the helper's header so nobody re-litigates it.

## §9 PremiumShopManager vs GodTierStoreManager — RESOLVED 2026-07-10
**Verdict:** duplicated responsibility, both claimed premium purchases. Resolution: `GodTierStoreManager` is the sole surviving premium manager; the entire `PremiumShopManager`/`PremiumShopUIController`/`PremiumShopSlotUI` trio + its slot prefab were deleted, and every code reference (`EconomyManager`, `HUDController`, `VisualPolishFix`) was stripped and repointed at `GodTierStoreManager`. Folded into and closed by §16 Phase A — see there for commit hashes.

## §10 DECISION — No premium soft currency ("neurons") — RESOLVED 2026-07-10
**Decision (2026-07-09, Aceyfer):** "Neurons" premium currency was never approved and is rejected. ALL premium purchases are direct real-world currency. Example: Bad Words Pack = **$5.00 USD**, not 50 neurons.
**Rationale:** owner intent; simpler economy; no soft-currency obfuscation layer.
**Done:** neuron currency purged repo-wide (code + CLAUDE.md), Bad Words Pack landed in God Tier Store at direct $5.00, the 2,500-Cash ProfanityPack (an in-game-currency path to profanity content, violating the no-soft-currency rule on its own terms even without literal "neurons") was killed outright rather than repriced. See §16 Phase A for commit hashes.
**Note:** direct-price IAP is fine on both stores; price tiers are set in App Store Connect/Play Console, not hardcoded — code should reference product IDs, not dollar amounts (see §12).

## §11 First-playable cut line
Proposal to react to (Aceyfer decides): v1 ships = core click loop, 6-stage World Restoration, 16-building shop, ranks, Gary + COGS barks, rebirth, offline decay. v1 waits = ShopTabView virtualization, Shop-3/Points polish, chapter art beyond current, Bad Words Pack (needs §12). Write the verdict here, then the Bible First-Playable Checklist becomes the single source.

**Play-check items moved from §20b (2026-07-21), not dropped by closing that task:**
- [ ] Dialogue log empty-state message displays correctly
- [ ] Dialogue log updates live while the panel is open
- [ ] Dialogue log X (close) button works
- [ ] Narrator line slide-in/out is unaffected by the log panel's presence

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

## §18 Oversized pedestrians — RESOLVED 2026-07-13
**Root causes, three, each its own population of "giants":**
1. **Unnormalized source art + inconsistent hand scales** (`aba3091`, `0ec69f9`) — pedestrian source art spans 1350-1780px with inconsistent per-scene scale overrides. Fixed with a code-owned `targetWorldHeight` normalization at point of use in `PedestrianWalkController.cs`, first at `4.5` (`aba3091`), then recalibrated to `1.2` (`0ec69f9`) once the actual camera math was worked through: camera is `orthoSize 5` (10 visible world units top-to-bottom), so `4.5` rendered pedestrians at 45% of screen height; `1.2` ≈ 12%, the intended scale.
2. **Animator sprite-swap overriding normalization** (`49cda11`) — Unity-AI-authored Animator Controllers on the walker prefabs were swapping sprites mid-walk-cycle *after* the normalization pass had already computed a scale for a *different* sprite (different pixel dimensions), producing giants again despite the fix in cause 1. Fixed by disabling the walker Animators entirely — `PedestrianWobble` already owns motion, the Animators were redundant for the actual gameplay-visible behavior.
3. **Rank-figure diorama compositing over the game via Diorama Camera** (`6d7f709`, closes §18) — a *separate* population from the walking pedestrians: `DioramaManager`'s rank-figure diorama was compositing oversized figures over the main game view through its own Diorama Camera. Retired by design — figures asserted off in code; physical scene/GameObject removal ledgered in §19 for post-embargo cleanup, not done here.
**Commits:** `aba3091`, `0ec69f9`, `49cda11`, `6d7f709`.

**Related, same investigation arc — bubble readability (`de8d202`):** chatter bubble duration now scales with text length (`max(4s, 2s + 0.06s/char)`, prefab previously pinned to a flat `2s`) and font size floor asserted `20-28pt` (prefab previously allowed as low as `14pt`). **Not fully closed** — bubble *rect sizing* (as opposed to duration/font) may still need a follow-up pass; pending Aceyfer's verdict from a fresh Play-mode look.

## §19 Deferred — dead code awaiting explicit rip approval, and open cosmetics
Nothing here blocks anything; logged so it doesn't get silently forgotten or opportunistically ripped mid-unrelated-commit.

**Protocol note, added 2026-07-21 (mirrors `PROJECT_BIBLE.md` §8):** every commit adding a new `.cs` file must be immediately followed by committing its Unity-generated `.meta` before any scene referencing that script's GUID ships. The §20b C3/C4 split left `DialogueLogPanelUI.cs.meta` untracked while `eae2c5d` (scene wiring) already referenced its GUID — a fresh clone would have had Unity assign a *different* GUID to the orphaned script and silently broken the scene wiring. Caught in-session, fixed by committing the `.meta` (`8bb4edc`) before push.

**Protocol note, added 2026-07-21 (mirrors `PROJECT_BIBLE.md` §8):** Unity AI Assistant edits are held to the exact same rules as human Editor-hands work — Play mode must be OFF before any scene-affecting edit, and only the on-disk diff (git, header-multiset verified per the existing §8 rule) is truth, never the Assistant's own "completed successfully" report. Caught for real during the §20b layout pass: the Assistant reported re-parenting `LogCloseButton` from `LogScrollView` to `DialogueLogPanel`; the edit was made while Play mode was active and was silently reverted the instant Play mode stopped, and the Assistant never flagged the loss. Post-session `git diff` showed zero `m_Father` delta anywhere in the scene — caught only because the layout tune (`6f3dba5`) was verified against the actual diff before commit, not against the Assistant's report.

**Pending decision — untracked working-tree artifacts from other AI lanes (added 2026-07-21):** `.cursor/`, `CODEX_FINDINGS.md`, and assorted `.patch` files (`cash-bootstrap-fix.patch`, `retire-legacy-walkers.patch`, `s20-dialogue-pacing.patch`, `s20-docs-closeout.patch`, `s21-s22-docs.patch`, `s7-doc-sync.patch`, `s8-docs-closeout.patch`, `s8-guard-editor-tools.patch`, `shop-sort-tiebreak.patch`) sit untracked in the working tree. Each needs a per-file call from Aceyfer — commit, `.gitignore`, or delete — same machine-local-state risk class as the `Phase2AThreeIssueEditor.cs` marker files below: invisible to git, present only on this machine, silently different (or absent) on a fresh clone.

**Pending decision — package/settings drift from Unity Assistant install (added 2026-07-21):** `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/Packages/com.unity.ai.assistant/Settings.json` now show as modified, introduced by installing/running Unity AI Assistant during the §20b layout session. Same bucket as the untracked artifacts above — Aceyfer's call whether to commit as a real project dependency or revert; not committed this pass.

**Dead code, retained pending explicit approval to remove:**
- `RestorationSlotUI` — the RP-tab slot prefab/script, unused now that the third tab is the God Shop. Also the source of the clone-refs-captured-first footgun documented in §16 Phase B1 — worth reading before anyone reaches for this class as a template again.
- `GodTierStoreUIController` — superseded by the God Shop tab work landing directly in `ShopUIController`; not wired to anything live.
- `CashShopSlotUI`'s seven dead `itemId == "profanity_pack"` special-case branches — still retained per the dead-code convention (§10 decision log), still slated for the still-PARKED Cash-family reconciliation, not this pass.
- `ShopQuery`/`ShopTabView`'s own, separate `ShopTab` enums — both still say `RpRestorations`, not `GodShop`. These are part of the dormant `ShopTabView` virtualization system (parked, per `TASKLIST.md`'s PARKED list), not `ShopUIController`'s enum (which was renamed in B1) — don't conflate the two when this family eventually gets reconciled.
- `Phase2AThreeIssueEditor.cs` (added 2026-07-17, off §19 pass C) — a self-triggering `[InitializeOnLoad]` one-shot tool from 2026-07-05, gated on marker files that live **outside the repo** on disk (`C:/Users/aceyf/Documents/Codex/2026-07-05/.../run-three-issue.marker` + a matching done-report path) rather than anything git-tracked. Its `DisableImage("Canvas/COGSWorldPortraitUI", report)` call (line 49) now targets a GameObject deleted in §19 pass A — harmless no-op if the tool ever fires again (string literal path lookup, not a compiled type reference, so it doesn't block compilation). Flagged as a retirement candidate: any Editor tool whose trigger condition depends on machine-local filesystem state is invisible to git and behaves differently per machine (this PC vs. the stale Mac clone) — worth ripping once explicitly approved, not touched this pass (out of §19 pass C's stated scope).

**Open cosmetics, not blocking:**
- God Shop tab labels currently render in TMP's default fallback font rather than the project's usual font asset — a one-line font-asset assignment if the visual mismatch bothers us; not diagnosed as broken, just unstyled.
- The rebirth-unlock threshold (50,000 RP spent, per Bible §4) has not actually been tested in an unlocked state since the RP tab was cut — worth a real Play-mode pass to confirm the "SNOTTING" trigger button still correctly reveals itself at that threshold now that RP is no longer a shop tab.
- **Pedestrian chatter bubbles are unreadable (added 2026-07-17, off §19 pass C's Play-mode verification)** — too small, despawn too fast, and multiple bubbles may overlap when several fire at once (unconfirmed). Likely lives in `ChatterBubble.prefab` + its spawner (`BackgroundPedestrianManager.SpawnChatterBubble`/`ChatterLoopRoutine`). Distinct from §20's narrator dialogue pacing (shipped `3e44480`, working correctly) — this is the separate, pull-based per-pedestrian chatter pipeline, not `DialogueManager`'s push/queue system (see the pipeline-separation note under §20's Codex audit). Candidate to bundle with the §20b dialogue-log arc since both are dialogue-UX work, not urgent on its own.

**Deferred, audited not fixed (added 2026-07-12, off the §17 COGSPortraitController hardening pass):**
- `GodTierStoreManager`'s `Instance` auto-bootstrap has an `isShuttingDown` guard scoped to app-quit only — a scene-transition teardown (loading a second scene) could still spawn a ghost auto-host, the same class of bug just fixed in `COGSPortraitController`. **Currently unreachable**: the project is single-scene, so there's no scene-transition teardown path to trigger it [Codex audit F6, verified]. Revisit if a second scene is ever added.
- The same self-bootstrapping-singleton pattern template that caused the §17 bug exists in other managers beyond `COGSPortraitController` (now hardened) — worth a pattern-level review pass across all of them, but only once a second scene actually makes the failure mode reachable; not urgent while single-scene.
- ~~`ShopUIController.restorationSlotPrefab` — a dead serialized field...~~ **RESOLVED `2b5919c` (2026-07-16)** — dropped automatically by the §19 pass A scene save, exactly as predicted. No dedicated commit needed.
- ~~Remove COGSWorldPortraitUI GameObject...~~ **DONE `2b5919c` (2026-07-16)** — GameObject (scene fileIDs 706123450000000001-005, direct Canvas child) removed from the scene in §19 pass A. **`COGSWorldPortraitUI.cs` + `.meta` deletion DONE `34f46a0` (2026-07-17)** — §19 pass C. Fully closed.
- ~~Delete `_DioramaContainer` + the five `Diorama_*` figure mounts + `DioramaManager.cs` + the Diorama Camera...~~ **DONE `2b5919c` (2026-07-16)** — `_DioramaContainer`, its Transform, the `DioramaManager` component, all 5 mounts (Diorama_0_Outcast through Diorama_4_President), and the Diorama Camera all removed from the scene in §19 pass A. **`DioramaManager.cs` + `.meta` deletion DONE `34f46a0` (2026-07-17)** — §19 pass C. Fully closed.
- ~~Delete `Pedestrian_1`/`Pedestrian_2`/`Pedestrian_3`...~~ **DONE `2b5919c` (2026-07-16)** — all three spriteless placeholder GameObjects removed from the scene in §19 pass A.
- `BackgroundStageView.stageSprites` prefab wiring is still only partially complete: 17/34 slots wired. Two are blocked on missing art (Aceyfer TODO), not a wiring gap: `Ped1_Stage1.png` and `Ped6_Stage6.png` don't exist yet.
- `Ped1`'s Stage1/Stage2 animation clips have empty sprite keyframes — currently moot since walker Animators are disabled (§18, `49cda11`), but relevant again if Animators are ever re-enabled for any reason.

**Added 2026-07-15 (off the §20 arc):**
- ~~Delete the root-level `PedestrianContainer` GameObject...~~ **FULLY DONE, all three passes closed:** **Scene-object half DONE `2b5919c` (2026-07-16, pass A)** — root GameObject (scene fileID 1053150925) + all six `PedN_Prefab` instances removed from the scene. **Prefab components DONE `4027249` (2026-07-16, pass B)** — `PedestrianWalkController`/`PedestrianWobble` components stripped from all 6 `Ped[1-6]_Prefab.prefab` assets; the prefabs themselves are kept, now serving solely as the Stage-1 art source for `BackgroundPedestrianManager`'s UI pedestrian population. **Scripts DONE `34f46a0` (2026-07-17, pass C)** — `PedestrianWalkController.cs`/`PedestrianWobble.cs` + `.metas` deleted; `MainUIControllerWireFix.cs`'s `FixPedestrianVisuals()` MenuItem (a legacy-walker resurrection tool — reactivated the deleted root container, force-enabled the walkers, force-saved the scene) removed along with its three single-caller helpers (`ReactivateLegacyPedestrianContainer`, `ForceLegacyPedestriansStageOne`, `UpdateBackgroundPedestrianManagerDefaults` — none had any other caller); `FixUIPedestrianContainer`/`AssignButton`/`CreateShopOverlayShade`/`LoadStageBackgroundSprites`/`PatchGameManagerRankDefinitions`/`SetRankDefinition` kept, still used by the surviving `WireMainUIController`/`WireBackgroundStageView` MenuItems. `BackgroundPedestrianManager.InitializeLegacyPedestrians()` (the boot-time assert-off) and its call site also removed — replaced with a one-line comment pointing at pass A.
- `DialogueDisplayUI` subscribe/unsubscribe asymmetry: `Awake` subscribes via `.Instance` (auto-creating), `OnDestroy` unsubscribes via `FindAnyObjectByType` — on both its listeners (`DialogueManager`, `COGSPortraitController`). Low risk while single-scene/single-teardown; fix opportunistically next time the file is open [Codex audit 4a, verified].
- `DialogueManager.SubscribeToEvents()` reaches `PlayerTapHandler` via raw `FindAnyObjectByType` with no retry — a `Start()`-ordering loss would silently kill `TapWithoutPurchase` lines for the session. Swap to `PlayerTapHandler.Instance` for consistency (same null risk at that instant, but matches codebase convention) or add a late-subscribe retry [Codex audit 4b; Codex's claim that PlayerTapHandler lacks an `.Instance` was WRONG — it has one at :37].

### Pass A findings (2026-07-16)
§19 pass A (`2b5919c`) took three verify-then-redo cycles to land clean — two prior attempts each failed a header-multiset check and were stopped before commit (one over-deletion, one under-deletion); see the chat transcript for the full three-attempt record if needed. Findings worth keeping:
- **RestorationBackdrops was parented under `_DioramaContainer`.** `RestorationBackdrops` (the live World Restoration stage backdrops, `WorldRestorationManager.restorationStageObjects`) was, per Bible blocker #2's own closure note, organizationally nested as a child of `_DioramaContainer` rather than a sibling. The first deletion attempt correctly deleted `_DioramaContainer`'s subtree — which took `RestorationBackdrops` and all 6 stage backdrops along with it, nulling all six `restorationStageObjects` references. This was caught by header-multiset verification (an unexpected 20-object deletion beyond the approved manifest) — it was **not** visible or suspicious in the raw `git diff` hunks, which is exactly the failure mode the multiset method exists to catch. Recovered via `git checkout -- SampleScene.unity` (discarding the contaminated save) followed by an Editor redo: `RestorationBackdrops` re-parented to scene root before re-deleting `_DioramaContainer`'s remaining contents. **Ledger gap:** `_DioramaContainer`'s children were never enumerated before the original §19 manifest was written, despite the Bible already documenting the parenting — worth enumerating actual scene children before writing a deletion manifest next time, not just the intended target's own name.
- **Protocol (also in `PROJECT_BIBLE.md` §8):** scene-save verification is header-multiset (`comm` on sorted `--- !u!N &ID` lines from both sides), never raw diff hunks — this file's 423 identically-shaped `Win` objects make Myers diff mispair unrelated blocks and produce phantom-addition false positives. Verify-before-commit, in that order, is canonical.
- **Benign deltas recorded in `2b5919c`** (so a future multiset run doesn't misread them as new drift): negative-zero rotation re-serialization (`{0,0,0,1}` → `{-0,-0,-0,1}`, mathematically identical) on `RestorationBackdrops`' own Transform, and `m_WasSpriteAssigned: 0 → 1` on all 6 backdrop `SpriteRenderer`s — both are Editor bookkeeping side effects of the drag-reparent operation; actual sprite references were unchanged.
- **Unexplained-but-benign, watch item only:** a Rive popup appears at Editor startup; no Rive package is visible in Package Manager → My Assets. Not investigated further this pass.

### Pass C findings (2026-07-17)
Pass C (`34f46a0`) deleted the four retired class files (`PedestrianWalkController.cs`, `PedestrianWobble.cs`, `DioramaManager.cs`, `COGSWorldPortraitUI.cs`) + their `.metas`, updated `BackgroundPedestrianManager.cs` and `MainUIControllerWireFix.cs` to drop all references, and passed repo-wide verification (zero non-comment `.cs` references to the four class names outside two pre-existing, approved doc-comment mentions; zero references to any of the four script GUIDs across every `.prefab`/`.unity`/`.asset` under `Assets/_Game`; zero references to the removed `"BrainDrain/Fix Pedestrian Visuals"` MenuItem path). One unaccounted-for hit surfaced and was explicitly accepted rather than fixed in-pass — see the new `Phase2AThreeIssueEditor.cs` dead-code entry above.
- Compile verified zero errors after all four deletions + both dependent edits.
- Play-mode pass verified UI pedestrians (the `BackgroundPedestrianManager`-spawned population, the sole survivor since pass A/B retired the legacy walkers), World Restoration backdrop stage swaps, and the shop all still function correctly post-trilogy (Aceyfer, 2026-07-16).

## §20 Dialogue pacing/queue in DialogueManager — RESOLVED 2026-07-15
The original framing ("no queue") was stale — a depth-2 queue already existed. The actual defects, found by auditing `CODEX_FINDINGS.md` (untracked, repo root) against the code and reading `DialogueManager.cs`/`DialogueDisplayUI.cs` end to end:
1. **Manager/UI timer desync — the real "interrupt" source.** The manager's `WaitForLineToFinish` waited exactly `duration`, but the UI's real occupancy is `duration + 0.6s` (0.3s slide-in + 0.3s slide-out) — every back-to-back queued pair fired the next line mid-slide-out and visibly yanked the panel. Codex missed this entirely (its "visible interruption is narrow" conclusion was wrong); its insertion-point analysis was still correct.
2. Silent queue-overflow drop at depth 2 (bare `return`, no log).
3. `ShowPriorityLine` cleared the whole queue, not just the active line.
4. Repeatable triggers (IQ milestones, the 10s cash auto-convert tick, 25-tap rewards, event outcomes) had no rate limit.
**Fix (`3e44480`):** 1.0s min-gap between lines (isDisplaying held true through the gap; covers the 0.6s slide overhead — coupling documented at both constants), 20s per-trigger cooldown on the four repeatable triggers, same-trigger queue coalescing (newest wins in place; queue is now a List), priority lines preserve the queue, overflow drops `Debug.Log`ged. Stale class doc comment (RebirthCount-gating claim) corrected same commit.
**Adjacent fixes landed same arc:** `780eb8a` — shop item shuffle: 11/16 buildings share `unlockCumulativeBrainPower=0` and `List.Sort` is unstable, so equal keys landed in arbitrary order per rebuild; comparator now tie-breaks by `baseCost` then name. `06aaa87` — the §18 "giants" were the scene-placed legacy world walkers (`PedestrianContainer` root / Ped1-6) rendering at a scene-serialized `targetWorldHeight` of **4.5** (vs the intended 1.2 from `0ec69f9`), and `BackgroundPedestrianManager.InitializeLegacyPedestrians()` was force-re-enabling them every boot; the method now asserts them OFF each boot instead (retirement pattern, same as COGSWorldPortraitUI/DioramaManager). UI pedestrian population is the sole survivor.
**Codex audit verdict (Cooldown Protocol, Bible §12):** trigger inventory, queue mechanics, and pipeline-separation findings verified accurate; one factual error caught (claimed `PlayerTapHandler` has no `.Instance` — it does, at :37, non-auto-creating); one material analytical miss (the timer desync above). Findings 4a/4b routed to §19; 4c matched the existing §19 pattern-review entry.

### §20b Dialogue log panel + button — CLOSED 2026-07-21
GTA-style scrollable history of narrator lines (Aceyfer request), also the natural home for anti-repeat (skip lines already in recent history).
**Feature code (C3, `e2b08d1`):** `DialogueManager` keeps a 50-entry ring-buffer line history plus a 10-line anti-repeat window (falls back gracefully once history is shorter than the window); new `DialogueLogPanelUI` renders it.
**Scene wiring (C4, `eae2c5d`):** log panel + HUD open button wired into `SampleScene.unity`.
**`.meta` (`8bb4edc`):** the C3/C4 split left `DialogueLogPanelUI.cs.meta` untracked while `eae2c5d` already referenced its GUID — committed after the fact once caught; GUID (`fda989633beb4a74aa3e3ec561ddba53`) verified matching the scene's `m_Script` reference. See the protocol note under §19 and `PROJECT_BIBLE.md` §8 — this is the finding that produced it.
**Layout/readability tune (`6f3dba5`):** panel dark background chip (`0.06, 0.06, 0.1, 0.9`, matching `ChatterBubble`'s scheme), `LogScrollView` resized to fill the panel minus a 40px margin, `LogText` 36→30pt, `LogCloseButton`/`LogOpenButton` re-anchored to top-right corners. Verified via header-multiset diff (zero added/removed headers; all content deltas confined to RectTransform/Image-color/TMP-property changes on objects already in the `eae2c5d` add-set, plus benign scrollbar handle-size/position deltas from the resize) before commit, per Bible §8's canonical scene-verification method.
**Closure note — remaining play-check items MOVED, not dropped:** empty-log message, live update while the panel is open, X close, confirm narrator slide is unaffected — moved to the §11 first-playable checklist rather than blocking closure here, since the feature is fully shipped and these are verification steps, not open code/design work.
**Popup interaction decision (Aceyfer, 2026-07-21):** the dialogue log panel intentionally stays behind event popups — by design, not a bug, not scoped to fix.
**Note, corrected:** `LogCloseButton` remains parented under `LogScrollView`, not the panel directly. Unity AI Assistant reported re-parenting it to `DialogueLogPanel` during this session; that edit was made while Play mode was active and was silently reverted the moment Play mode stopped, and the Assistant's own completed-actions log still claimed success. The post-session `git diff`/header-multiset check (which the `6f3dba5` layout tune above was verified against) showed zero `m_Father` delta anywhere in the scene — the re-parent never persisted. See the new protocol note under §19 and `PROJECT_BIBLE.md` §8. Left as-is; harmless, not a functional issue.
**Note:** the open button reads "Dia-Log" in-scene — cosmetic, not scoped to this pass.

### §20c Chatter bubble + pedestrian spawner fixes — CLOSED 2026-07-21
Three commits, all `.cs`-only — no scene/prefab edits:
1. `429617a` — fade curve. `ChatterBubble.Update()` faded linearly from the moment of spawn (`alpha = 1f - t`) — the real "fades too quick" complaint despite `SetText`'s longer duration. Now holds full opacity through 70% of lifetime, fading only over the final 30%.
2. `c83fa76` — readability. Dark background chip (`0.06, 0.06, 0.1, 0.92`), white label text, 24pt font-size floor (was 20pt), 300×90 minimum rect — code-owned per Bible §8/§2.3. Supersedes the earlier `de8d202` SetText half-fix (§18): that pass raised duration and the font floor to 20pt but left contrast (white text going white-on-white) and rect sizing untouched — this closes both the §18 "not fully closed" note and the §19 "chatter bubbles unreadable" open-cosmetic entry.
3. `8940ee6` — duplicate-NPC fix. `PickStageOneSprite` picked with replacement, so visually identical pedestrians could walk together by chance (confirmed on-screen by Aceyfer). Now excludes sprites already active on screen, falling back to the full candidate pool once population exceeds sprite variety (never returns null because of the filter).
**Aceyfer play-check: passed.**

## §21 World portrait position tune — CLOSED 2026-07-15
Verified clear by Aceyfer in Play mode: nothing overlaps or crowds THE SNOTTING button/badge (top-right). Largely moot since the world portrait itself was retired in the §17/§18 arc. No action taken, none needed.

## §22 Balance re-sim + Cash bootstrap fix — DONE 2026-07-15
The 16-building economy had never been re-simulated (the original `balance_sim.js` modeled the old 7-building set). A fresh greedy-buyer sim against the real `.asset` values found ONE structural break and confirmed the rest:
- **Cash economy deadlock (FIXED, `29f9672`):** every Cash-tab building cost Cash, including Underground Economy (15 Cash) — the intended BP→Cash bridge. With no Cash source until Brain-Rot Think Tank (725k cumBP) or a lucky Illumisnotti Leak event, the whole tab was chicken-and-egg locked; sim showed CPS=0 for 3+ days at 30 min/day. Fix: Underground Economy back to its Bible-tuned `costType: BrainPower`, `baseCost: 75`. Post-fix curve: first Snotting day 4 @ 30 min/day, day 2 @ 60 min/day (3 taps/s greedy-optimal — real players land a day or two later), Utopia ~day 7+ @ 60 min. Daily IQ-decay return hook confirmed working (income at 60% each morning until ~40 taps restore).
- **Bible §6 anomalies (a) IQ Overclock inversion and (b) default-1-BPPS on 3 Cash buildings: LEFT AS-IS by Aceyfer's call** — the original values came from a deliberate speed-run tuning pass for the daily-return loop, and the sim confirms both are noise (high tiers dominate). (c) flat unlock curve: also left; shop sort (`780eb8a`) keeps display sane.
- Underground Economy's shop position (below all unlock-0 buildings despite the 75 cost) is the unlock-first sort working as designed; its ×1.38 multiplier self-limits spam (level 10 ≈ 1,360 BP). Slot UI's per-level CPS display includes the player's effective Cash multiplier — a rebirth-stacked test save showing "+$12/s" for a $5/s base is correct, not a bug.

## §23 FTUE / comprehension pass (COGS-narrated onboarding) — scoped 2026-07-22, BLOCKS §11
**Problem:** the core loop is mechanically playable, but its meaning — that COGS is narrating for the Illumisnotti (the player's actual employer), the brain-harvesting premise, the dystopia→utopia World Restoration arc, and what Snotting/rebirth even represents — is undiscoverable in-game. Nothing currently teaches any of this; a first-time player taps, buys buildings, and sees numbers go up with zero narrative context. This directly blocks §11's two-minute-click bar (a fresh player has to understand what they're doing well before the two-minute mark, not eventually piece it together from narrator barks alone).
**Scope agreed with Claude (chat):**
- **(A) First-ever-play scripted COGS boot briefing (4-5 lines).** Needs a new scripted-sequence path in `DialogueManager` — the current queue (depth 2, min-gap/cooldown/coalescing per §20) is built for independent, self-contained trigger-driven barks, not an ordered narrative chain that must play start-to-finish once at boot. This is new mechanism, not a reuse of the existing queue.
- **(B) Contextual one-shot `NarratorLine`s on existing triggers** (`FirstCashEarned`, `FirstRestoreSpend`, `SnottingReady`) — in-character explanations of each mechanic the first time the player actually encounters it, riding the existing trigger/pool infrastructure (no new plumbing, just new gated-once lines).
- **(C) Deferred, not this pass:** a re-readable "INTEL" briefing card, styled like the §20b dialogue log panel, so a player can revisit the premise/lore after the initial briefing scrolls past. Logged here so it isn't lost, not scoped or started.
**Status:** scoped only, no code written yet. Blocks §11 — the "first playable" cut-line decision can't be finalized while the game's premise is undiscoverable to a first-time player.

## Creative package (approved 2026-07-22)

**Design:** two narrator channels. Main COGS (Illumisnotti propaganda, reverse-psychology anti-tutorials — tells the player NOT to do the thing they should do) vs. THE LITERATES resistance cards (correctly-spelled truth, delivered as fake-business-front dead-drops with handwritten backs). All FTUE beats are OK-confirmed modals except two COGS ambient lines, which run through the regular (non-modal) narrator panel instead. COGS modals are capped at exactly 2 across the entire pass (the boot briefing and SnottingReady) — every other beat routes through THE LITERATES card channel.

**System:** new `FTUEManager` owns seen-flags (persisted in the main save, not a separate file), beat sequencing, and modal spawns. Reusable `IntelCardUI` with two skins (COGS terminal skin, card skin) backs both channels rather than building two separate UIs. Popup-tier stacking: FTUE modals defer behind event popups, never interrupt them. `DialogueManager` itself stays untouched except the boot-briefing sequence, which replaces the currently-hardcoded first-play lines in `Start()`.

### Beat 1 — COGS BOOT BRIEFING (first-ever play, modal, COGS terminal skin)
GOOD MORNING, ASSET. YOU HAVE BEEN ASLEEP FOR: TOO LONG.
WHILE YOU SLEPT, YOUR BRAIN WAS REZONED AS COMMERCIAL PROPERTY.
I AM COGS. I AM YOUR FRIEND. I AM ALSO LEGALLY REQUIRED TO SAY THAT.
YOUR JOB IS SIMPLE: TAP YOUR HEAD. THE JUICE COMES OUT. THE ILLUMISNOTTI
COLLECT IT. EVERYONE WINS. MOSTLY THEM. THAT'S WHAT WINNING MEANS.
DO NOT READ ANYTHING. DO NOT THINK ABOUT WHERE THE JUICE GOES. TAP THE
WASTELAND.
Confirm: [ OK, HARVEST ME ]

### Beat 2 — CARD #1 (~10s after briefing closes, card skin)
Front: GARY'S DISCOUNT MATTRESS EMPORIUM — "We Also Have Soup"
Back: They're metering your head. But here's what the tin can won't tell
you: every tap leaks a little light back into the world. Watch the sky. It
remembers.
Don't let COGS pick what you buy. Don't let COGS pick anything.
— The Literates
p.s. burn after reading. actually don't. read it twice. reading twice is
how we got like this. the good version of like this.
Confirm: [ I READ IT. ALL OF IT. ]

### Beat 3 — CARD #2 (first shop open, card skin)
Front: SNAKE UTTERS WHOLESALE — "Ask About Our Utters"
Back: Buildings make juice while you nap. COGS calls that "theft of company
time." Do it anyway — sleeping on the job is the only job worth having.
Buy cheap ones first. The math is friendlier. We checked. We're the last
people who check math.
— TL
Confirm: [ MATH CONFIRMED ]

### Beat 4 — COGS ambient (FirstCashEarned, regular narrator panel, NOT
modal)
ALERT: YOU HAVE DISCOVERED "CASH." DO NOT CONVERT BRAIN POWER INTO CASH.
CASH LEADS TO BUYING. BUYING LEADS TO CHOICES. CHOICES LEAD TO THINKING. I
AM WATCHING YOU, SPECIFICALLY.

### Beat 5 — CARD #3 (FirstCashEarned, modal, fires a beat after Beat 4)
Front: ARMADILLO SAUCE LEGAL SERVICES — "It Goes With Everything, Including
Court"
Back: It just told you not to convert, didn't it. Funny how the thing
metering your head panics when you spend what's yours.
Convert. Buy. Repeat. That's the whole machine. Now it's your machine.
— TL
Confirm: [ MY MACHINE NOW ]

### Beat 6 — COGS ambient (FirstRestoreSpend, regular narrator panel, NOT
modal)
YOU SPENT YOUR POINTS ON... FIXING THINGS? THE ILLUMISNOTTI HAVE REVIEWED
YOUR PURCHASE AND FILED IT UNDER "ADORABLE." CARRY ON. IT'S A ROUNDING
ERROR.

### Beat 7 — CARD #4 (FirstRestoreSpend, modal)
Front: CHEESE DIRT MEMORIAL FOUNDATION — "Never Forget The Flavor"
Back: Every point you put into the world makes the streets a little smarter
and their grip a little weaker. They allow it because they think it's a
rounding error.
Be a rounding error. Be the biggest rounding error they've ever seen.
— TL
Confirm: [ ROUNDING UP ]

### Beat 8 — COGS CORE INTEL #2 (SnottingReady, modal, COGS terminal skin —
the ONLY other COGS modal)
MANDATORY NOTICE: YOU NOW QUALIFY FOR THE SNOTTING.
YOUR BRAIN WILL BE REPOSSESSED, WIPED, AND REISSUED WITH A PRODUCTIVITY
MULTIPLIER.
YOU WILL LOSE: EVERYTHING. YOU WILL GAIN: MORE OF EVERYTHING, FASTER.
THE ILLUMISNOTTI CALL THIS "A PROMOTION." PARTICIPATION IS VOLUNTARY, WHICH
IS OUR FAVORITE KIND OF MANDATORY.
Confirm: [ OK, REPOSSESS ME (LATER) ]

### Lore constants
The resistance = THE LITERATES: the last people who read anything; every
card correctly spelled and punctuated in a world of degraded text —
literacy as rebellion. COGS considers reading terrorism. Card fronts are
fake mundane businesses (dead-drop style); backs are handwritten truth.
Confirm buttons are a joke channel: COGS confirms = compliance language,
card confirms = literacy language.

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
