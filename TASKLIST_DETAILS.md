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

## §6 BG1.jpg import
Select `Assets/_Game/Sprites/Backgrounds/BG1.jpg` → Inspector → Texture Type: **Sprite (2D and UI)** → Apply. Editor-side task (Aceyfer). Kills `[BackgroundStageView] stageSprites[0] is missing`. No commit needed beyond the .meta change — include with doc-sync commit.

## §7 Doc sync
- Bible §6 economy table: 7 → 16 buildings (add JumperCables, DefrostDrip, CranialMicrowave, SynapseSpaceHeater, CryoSludgeEspresso, IQOverclockChip, LemonadeGriftStand, DoomscrollBillboard, HOAProtectionRacket).
- CLAUDE.md inventory: add Gary system, PremiumShopManager, chapter system (COGSStage/ChapterManager/IllumisnottiManagerUI), pedestrian stack, EventBus.
- Note shop-family split: BP/Cash/RP tab shop (ShopUIController) vs Shop-3/Points family (PointsShopPanel/CashShopPanel/ConvertPanel/PremiumShopPanel) — different systems, don't conflate.

## §8 Unguarded Editor tools (12)
SceneManagerWiring, ShopPanelLayoutFix, PlaceholderArtGenerator, PedestrianAlphaTest, HUDMobileOverhaul, FixCOGSDialogueLayout, ConsolidateShopButton, COGSPortraitWireFix, RemoveMissingSceneScripts, MainUIControllerWireFix, PopulateBuildingTemplates, VisualPolishFix (6 items). Add the same isPlaying/isPlayingOrWillChangePlaymode guard pattern to each entry point. One batch commit. Pattern rule (Bible): wrap Editor-only API calls in try/catch at the chokepoint too — flags alone race.

## §9 PremiumShopManager vs GodTierStoreManager
Relationship undocumented. Read-only audit: do both claim premium purchases? Who owns what? Outcome feeds §10. Document verdict in Bible; consolidate later if duplicated.

## §10 DECISION — No premium soft currency ("neurons")
**Decision (2026-07-09, Aceyfer):** "Neurons" premium currency was never approved and is rejected. ALL premium purchases are direct real-world currency. Example: Bad Words Pack = **$5.00 USD**, not 50 neurons.
**Rationale:** owner intent; simpler economy; no soft-currency obfuscation layer.
**Actions:** grep specs + code + UI strings for "neuron" (any casing); remove/replace with direct pricing; check PremiumShopManager/GodTierStoreManager price fields; update Bible Settled Decisions.
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

---

## DECISION LOG
- 2026-07-09 — Shop collapse: ShopUIController is the sole shop system; ShopTabView dormant + detached until virtualization AND real purchase routing exist. Guard in code is the switch.
- 2026-07-09 — No premium soft currency; all premium purchases direct real-world pricing (§10).
- 2026-07-09 — Scene YAML surgery (reparents/anchor-sensitive) = Editor-hands work, never raw YAML edits by agents. Component strips allowed only with verified zero dangling references.
- Standing — Claude Code is sole code editor. One change, one commit. Diagnose before changing. No destructive git without explicit confirmation. Editor/Inspector work = Aceyfer (+ Unity AI when credits allow).

## CHANGELOG (this session, 2026-07-09)
- Landed: commits 1–3 (NumberFormatter sub-1 currency, Gary bubble tap-blocking + hold-time, HUDController rank cold-boot sync).
- Staged, holding: commit 4 (Awake BuildShop + scene-wide guard + Canvas toggle + collapse C#), commit 5 (9 buildings wired + scene), ShopThreeTabWireFix guard, AutoSceneFixes hardening.
- Editor work done: 3× ShopTabView components removed (verified clean), ShopTabBar reparented under ShopPanel (PosY −3, last child).
- Found & fixed en route: ShopTabView guard was a structural no-op (ancestry vs existence); ShopUIController never toggled Canvas.enabled; tab bar sibling order lost raycasts to MainTapButton; AutoSceneFixes delayCall race.
- Closed as pre-existing/harmless: COGSPortraitController duplicate-destroy warning, BG1 warning (fix queued §6).
- Resolved: CanvasGuard fight (§1) — play-mode guard, not a nested-canvas incompatibility. Stray element (§2) — was Tab_BP stuck in WorldSpace renderMode, same root cause as §1's revert target. FindObjectsSortMode (§3) — closed by inference, source never identified (see §3 note).
- Full re-test (§4) passed green. Commit train (§5) landed in full — 9 commits, see §5 for hashes and order.
- Tasks 1–5 (Shop Collapse Endgame) all complete as of this commit. Next up per TASKLIST.md: §6 (BG1 texture import), §7 (doc sync), §8 (batch-guard remaining 12 unguarded Editor tools), §9 (PremiumShopManager vs GodTierStoreManager audit).
