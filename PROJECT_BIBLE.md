
# PROJECT BIBLE — Brain Drain: Idle IQ

*The front door. Read this first, every session. Deep architecture lives in CLAUDE.md — this file tells you what the game is, what's true, what's broken, what's protected, and what "done" means. If this file and any other doc disagree, this file wins. Last updated: 2026-07-05 (Phase 1 complete).*

---

## 1. What this game is

A satirical mobile idle-clicker by **AcEclipse Games**. You play the evil mogul harvesting **Brain Power** from a dumbed-down population, building a corporate empire, rising against — and ironically becoming — the **Illumisnotty** shadow elite via "The Snotting" (prestige/rebirth). A narrator, **COGS**, roasts everyone as the world slowly heals from dystopia to utopia.

- **Engine:** Unity 6000.4.8f1, URP 2D
- **Platform:** iOS, portrait, 1080×1920
- **Repo:** `github.com/aceyfer/brain-drain-idle-iq`, branch `main` = source of truth
- **Active dev environment: the PC** (`C:\Users\aceyf\Brain Drain`). The Mac copy is a stale failed transfer — do not treat it as current.
- **AI roles:** Claude (chat) = architect/reviewer against the GitHub clone. Codex / Claude Code = hands on the local repo and Unity Editor. **Never run two AI agents on the repo concurrently** — historical "mystery concurrent activity" came from exactly that.

## 2. Art & UI Direction — GLOBAL, BINDING

**Theme: Futuristic Cyberpunk × Idiocracy.** High-tech, low-brainpower. Neon corporate dystopia run by idiots. Raw satire. Clean and punchy, never muddy or over-detailed.

**Rules every visual change must obey:**
1. **Mobile readability first.** Everything must read crisply on a physical phone at 1080×1920 portrait. If it's ambiguous at arm's length on a 6" screen, it fails.
2. **UI downsize mandate.** Current UI icons are far too large and clash with the theme. All icon/button art gets a severe size reduction as part of the first-playable polish pass.
3. **Chat/narrator bubbles** (COGS dialogue, pedestrian chatter): aggressively optimized for small-screen legibility — high contrast, generous padding, restrained font effects.
4. **The 3-Tab Unified Shop Panel.** The main Shop icon opens ONE panel with exactly three tabs:
   - Tab 1: **BP Store** (Brain Power → buildings)
   - Tab 2: **Cash Store**
   - Tab 3: **RP Store** (Restoration Points)
   This is a *presentation* unification only. The three backend managers (`UpgradeManager`, `CashShopManager`, `PointsShopManager`) remain separate — do NOT merge their code. "Points" is renamed **Restoration Points (RP)** in all player-facing text; C# names stay unchanged (same convention as The Snotting rename).
5. **The Protected Sprite Lock.** The only gameplay character/entity sprites in the first playable are:
   - **Pedestrians 1–6** (six pedestrians × six stages)
   - **Stages 1–6** (world restoration backdrops)
   - **COGS 1–6** (narrator portrait progression)
   These may be visually *upgraded/polished* to merge with the art style. **No new character or entity sprites may be introduced** before first playable ships.
   - Inventory (2026-07-05): COGS 1–6 ✅ in repo. Pedestrians: 35/36 in repo — Ped1 stage 1 uses Ped1_Stage2 as an approved workaround (see blocker #8). Stage 1-6 backdrops ✅ produced and wired (BG1.jpg, BG2-6.png).

## 3. Definition of Done — First Playable

The first playable is DONE when, on a fresh save, a player can: tap and feel satisfying feedback → buy all 7 buildings → earn Cash → convert to RP → spend RP on World Restoration and see the world visibly change → unlock and perform one Snotting → reload the app with everything intact — all on a readable, uncrowded portrait screen with COGS commenting throughout. Nothing else counts.

## 4. Current Blockers (live list — update status here, nowhere else)

| # | Blocker | Status |
|---|---------|--------|
| 1 | Global Light 2D applied to zero sorting layers — world rendered black | ✅ **CLOSED** 2026-07-03, commit `c03e9a0`. Mask now covers Background/World/Default/Characters. World and pedestrians confirmed lit in Play Mode. |
| 2 | `WorldRestorationManager.restorationStageObjects` empty — 6 backdrop GameObjects need wiring | ✅ **CLOSED** 2026-07-05, commits `7e9a098` + `c2682aa`. Backdrops live under `_DioramaContainer/RestorationBackdrops`; SpriteRenderers use sorting layer `Background`, order `-20`; array wired Stage1→Stage6. |
| 3 | `COGSWorldPortraitUI` absent from scene — COGS has no persistent on-screen presence | ✅ **CLOSED** 2026-07-05, commit `3810660`. COGS portrait UI is present and wired in scene. |
| 4 | `GameManager.playerIQManager` / `.currencyManager` unwired (silent fallback works; wire explicitly) | ✅ **CLOSED** 2026-07-05, commit `caad434`. Explicit scene refs are wired. |
| 5 | `illumisnottiTitleText` unwired + two conflicting title ladders | ✅ **CLOSED** 2026-07-03, commits `331b08b` + `2ef7f79`. SNOTTY ROOKIE ladder is official (see §7.11); HUD text wired; badge and HUD confirmed showing identical titles. |
| 6 | `PremiumShopManager` absent from scene entirely (not merely unwired); `PremiumShopUIController.premiumShopManager` = fileID 0 | ✅ **CLOSED** 2026-07-05, commit `7d8620a`. Manager added to `_Systems`; premium shop UI ref wired. Logic remains protected (§5). |
| 7 | Portrait middle UI crowded / unreadable | OPEN — resolved by §2 downsize + 3-tab shop (Phase 2) |
| 8 | Ped1 stage-1 art missing (`Ped1_Stage1.png` deleted; scene + `Ped1_Walk_Stage1.anim` + BackgroundPedestrianManager slot referenced dead GUID `50befc083ac09734f96d06ddddd6342e`) | ✅ **CLOSED / WORKAROUND IN PLACE** 2026-07-03, commit `8c1138d`. All dead refs repointed to `Ped1_Stage2.png` (GUID `5b666186fd7406e489f8cfd79a112cf2`, sprite fileID `-9157900419861997769`). Real Stage 1 art regeneration deferred post-testing. |
| 9 | Play Mode exit warning: "Some objects were not cleaned up" | ✅ **CLOSED** 2026-07-05, merged commits `9639860` + `828dc36`. Singleton teardown guards prevent `(Auto)` manager resurrection during exit; SaveManager duplicate quit hook fixed; two Play Mode cycles passed and warning confirmed gone. |

## 5. Protected Zones — NEVER touch without explicit written approval

- `Assets/_Game/Sprites/Pedestrians/` and `Pedestrians_backup_20260625/` — no edits
- `Assets/_Game/Prefabs/Pedestrians/` — no edits
- **Player saves** — never reset or delete
- **Git history** — never reset; never force-push; never `git restore` working changes
- **Premium Shop / God Tier Store logic** — `StubPurchase` stays a stub until a real IAP pass; wiring references is fine, logic changes are not
- **Economy values** — all tuned numbers in §6 are final for first playable
- **Never commit:** TMP font assets (they will show as eternally dirty — ignore them), `_Recovery/`, Rive package noise, Unity-generated junk
- **Do not resurrect IQ decay** (`IQDecaySystem` was deliberately removed; offline-decay-on-load is the only sanctioned decay)

## 6. The Economy — final numbers (matches `.asset` files; if they diverge, the asset files won)

```
TAP ──▶ Brain Power ──▶ BP Store (7 buildings + infrastructure)
                             ├─▶ idle BPPS  ├─▶ PlayerIQ (+1/purchase; 1:1 infra)
                             └─▶ Underground Economy ──▶ Cash
Cash ──▶ Cash Store (items, Hot Chick)  or  convert ──▶ Restoration Points (RP)
RP ──▶ RP Store + World Restoration (dystopia → utopia)
Real Money ──▶ God Tier Store (stubbed, post-first-playable)
```

**Buildings** (unlock gate = *cumulative* BP ever earned — NOT baseCost; these two have been confused repeatedly, check which one you're looking at):

| Building | Unlock (cumulative BP) | baseCost | costMult | Income |
|---|---|---|---|---|
| The Literal Library | 0 | 15 | 1.25 | 0.1 BPPS |
| Doomscroll Engine | 0 | 10 | 1.21 | 0.3 BPPS |
| Underground Economy | 500 | 75 | 1.38 | 5.0 CPS (only early Cash source) |
| Podcaster Soundboard | 25,000 | 150 | 1.21 | 5 BPPS |
| Crypto Bro Compound | 110,000 | 1,200 | 1.32 | 60 BPPS + 10 CPS |
| Reality TV Syndicate | 185,000 | 15,000 | 1.21 | 320 BPPS + 40 CPS |
| Brain Rot Think Tank | 725,000 | 200,000 | 1.21 | 4,500 BPPS + 200 CPS |

**Key tuned values (final):**
- PlayerIQ starts 100, climbs forever; offline-only decay toward floor 60 over 8h; idle income scaled by IQ/100 (cap 1.0). *(Observed working 2026-07-03: a decayed save showed IQ 68 — intended behavior, not a bug.)*
- Snotting (Rebirth): per-Snotting permanent +5% BP mult, +10% Cash mult, +5% RP conversion, +5% tap mult; full current-run wipe
- Snotting button visibility gate: **50,000 RP spent on Restoration**
- World Restoration stage thresholds (RP spent): 0 / 2,500 / 10,000 / 50,000 / 250,000 / 1,000,000
- Dialogue tone tiers gate on **RestorationPercent** (0–10 / 11–30 / 31–55 / 56–80 / 81–100), not RebirthCount, not PlayerIQ

## 7. Settled Decisions (do not relitigate; do not "fix")

1. IQ decay (runtime) is dead. Offline-decay-on-load is intentional and stays.
2. "The Snotting" and "Restoration Points" are player-facing text renames ONLY — C# identifiers keep original names (`RebirthManager`, `ConvertCashToPoints`, etc.).
3. The 2026-06-21 economy rebalance values are final (simulated via `balance_sim.js`).
4. The 3-tab unified shop (§2.4) supersedes the earlier "keep shops as separate popups" layout. Backends stay separate.
5. The Illumisnotty are never revealed on-screen. (Their *ranks* ARE the player's Snotting titles — see 11.)
6. DOTween is in the project (`Assets/Plugins/Demigiant/DOTween`) for button punch / floating text / slides; simple loops stay coroutines.
7. `OnCashChanged`/`OnPointsChanged` are UnityEvents (AddListener), everything else is C# `event Action<T>` — both deliberate; don't "standardize."
8. The parked alternate Hot Chick / Illumisnotti-progression spec stays PARKED — the built `CompanionManager`/`PointsShopManager` systems stand. Do not build the parked spec.
9. Save-migration guard: JsonUtility zero-fills missing fields — a loaded `0` in a newer field means "old save," restore the default (see `tapMultiplier`).
10. Load order: `GodTierStoreManager.LoadState` runs before `PlayerIQManager.LoadStateWithOfflineDecay` (Corporate Cloak hours must land first). Don't reorder.
11. **Title ladder (decided 2026-07-03): the SNOTTY ROOKIE ladder is the one and only Illumisnotti title ladder**, served by `RebirthManager.GetIllumisnottiTitle` to both the HUD and the Illumisnotti badge:
    0–1 SNOTTY ROOKIE · 2–3 UNDER-SNOT ELITE · 4–5 BUNKER BUREAUCRAT · 6–10 ILLUMISNOTTY INTERN · 11+ BUNKER SUPREME.
    Title is visible from Snotting 0 (no blank state). Player-facing spelling is "Illumisnotty".
12. Ped1 stage 1 intentionally reuses the Stage 2 sprite until new art is produced (first-playable workaround).
13. **Premium = direct real currency only, via `GodTierStoreManager` exclusively** (decided 2026-07-09/10, `TASKLIST_DETAILS.md` §10/§16). Neuron premium currency was purged repo-wide (`939222f`–`34841b7`); `PremiumShopManager`/`PremiumShopUIController`/`PremiumShopSlotUI` are deleted, not dormant. **No soft-currency path to premium content may ever exist** — the 2,500-Cash `ProfanityPack` was killed outright rather than repriced specifically because it was such a path, independent of whether anything was literally named "neurons." The store is called the **God Shop**; its tab UI is Phase B of §16, not yet built.

## 8. Known scar tissue (bugs already paid for — read before debugging)

- **Editor.log lies.** It retains stale compile errors from old sessions. Only the live Unity Console after a fresh recompile is a valid compile oracle. (Burned 2026-07-03: 12 phantom errors.)
- **Unity smuggles unrelated changes into scene saves.** Saving the scene for one fix can serialize unrelated RectTransform/layout drift, or bake in a runtime-only mutation as if it were permanent authored data. ALWAYS `git diff` the scene before committing; if the diff isn't only your change, rebuild from the last checkpoint and reapply. (Burned 2026-07-03 during the Light 2D fix; third instance 2026-07-09, see the shop-empty-except-one-row entry below.)
- **AutoSceneFixes delayCall timing hole.** Its play-mode guard at the top of `RunFixes()` can pass during the enter-play transition, letting scene-saving fixes run mid-play (`InvalidOperationException` on `MarkSceneDirty`). Re-check guards at the moment of action in all three fix methods + `MarkAndSaveScene()` narrow the window but don't close it — every re-check reads the same `EditorApplication.isPlayingOrWillChangePlaymode` flag synchronously in the same call stack as the outer check, so a stale flag at the outer check fools every inner check too (confirmed recurring 2026-07-09, during the shop-collapse Play-mode testing). `MarkAndSaveScene()` is now backstopped with a try/catch around the actual `MarkSceneDirty`/`SaveScene` calls, not just another flag check, and logs which of the three fix methods requested the save so a skipped save is traceable. Pattern rule: any `[InitializeOnLoadMethod]`/`delayCall` editor automation must re-check play state immediately before mutating/saving the scene, **and** should not rely on flag checks alone at the true chokepoint — wrap the actual Editor-only API call in try/catch too.
- **ShopPanel Awake() trap:** never save a UI panel inactive in the scene — `Awake()` never runs, button listeners never register, everything silently dies. Panels must self-hide in `Awake()` after wiring. (`AutoSceneFixes.cs` guards this in edit mode.)
- **Self-bootstrapping singletons resurrect during teardown** — see blocker #9. Any `Instance` getter that auto-creates a GameObject must refuse to create while quitting/tearing down.
- **baseCost ≠ unlock threshold:** task specs have repeatedly confused these; verify against §6 before "correcting" any number.
- **Dangling reference:** inactive `TapButton`'s `Button.OnClick` points at a removed component — harmless, known, ignore the Inspector warning.
- **`UIBlockDebugger.cs`** uses legacy Input and will throw under the new Input System if it ever runs — quarantined, not fixed, pending explicit ask.
- **DebugCheats.MaxAllBuildings** must route through real purchases — calling `LoadBuildingLevels` on a live game double-counts BPPS/CPS.
- **Shop is dual-wired to the same GameObjects.** `ShopUIController`'s `bpTabPanel`/`cashTabPanel`/`rpTabPanel`/`bpContent`/`cashContent`/`rpContent` and `ShopTabView`'s three per-tab `content`/`tabCanvas` fields point at the *identical* scene GameObjects (`Tab_BP`/`Tab_Cash`/`Tab_RP`), not two separate hierarchies — confirmed by fileID. `ShopUIController` owns runtime presentation and the real economy (routes through `UpgradeManager.TryBuyBuilding`); `ShopTabView` is a newer, event-driven/virtualized reimplementation that stays intentionally dormant until it gets real purchase routing (its `StubPurchase` only logs today) — the guard at the top of `ShopTabView.Awake()` (checks whether a `ShopUIController` exists scene-wide) is the switch between the two. Do not remove that guard until `ShopTabView` is actually promoted to replace `ShopUIController`. **Decided 2026-07-09: the RP tab (`rpTabPanel`/`rpContent`, World Restoration stage rows) is cut** — that third tab slot becomes the Premium real-currency store instead (see `TASKLIST_DETAILS.md` §16); World Restoration progression itself stays in the game, just not presented as a shop tab. `ShopUIController`'s RP-specific code (`BuildRestorationTab`, the lazy-rebuild retry in `SelectTab`) is left in place as harmless dead code for now, not ripped out.
- **ShopRoot is a sibling of shopPanel, not a descendant.** `Tab_BP`/`Tab_Cash`/`Tab_RP`/`ShopTabBar` all live under `ShopRoot`, a separate GameObject `ShopUIController.shopPanel` never references. `shopPanel.SetActive(false)` alone does not hide any of them — `ShopUIController` now resolves `shopRoot` automatically (from `bpTabPanel`'s own parent) and activates/deactivates it in lockstep with `shopPanel` in `Awake`/`OpenShop`/`CloseShop`. Also fixed in the same pass: `ShopRoot` sat before `MainTapButton` in `CustomSafeArea`'s sibling order, so anything nested inside it lost every raycast to `MainTapButton`'s full-screen target — fixed via a pure sibling reorder (`ShopRoot`'s entry moved to after `MainTapButton`'s), not a reparent.
- **Correction to commit `27fb988`'s message:** it claims *"Reparented ShopTabBar from ShopRoot into ShopPanel"* — that reparent never actually persisted to `SampleScene.unity`, confirmed by diffing `ShopTabBar`'s RectTransform against the session's first commit (`f475305`) and finding it byte-identical. `ShopTabBar` remains under `ShopRoot` by design as of commit `c25fa63`, which fixed the same underlying symptoms (visibility + raycast loss) via `ShopRoot` active-state ownership + sibling reorder instead. Do not trust `27fb988`'s commit message on this point.
- **Pattern rule: verify, don't trust, after any Editor-hands step.** A "scene saved" report from a human doing Editor-side work (reparents, Inspector wiring, anchor edits) is not confirmation the change is actually in the file — the 27fb988 case above shows a reparent that was reported done and saved, then referenced in a commit message, and never actually landed. Before referencing an Editor-hands step in a commit message or marking a task complete, `git diff`/re-read the actual scene file and confirm the specific field/hierarchy change is present. Applies symmetrically to the agent's own claims too — don't write a commit message describing an outcome without having just verified it against the diff.
- **Shop appeared empty except one row per tab (third scene-smuggling instance).** All 16 buildings were correctly instantiated into `bpContent`/`cashContent` every time (`BuildBuildingTabs()` has no unlock/lock filter), but `Content`'s `VerticalLayoutGroup` was saved `m_Enabled: 0` on all three tabs — with no active layout group, every row landed at the same default prefab position and fully overlapped, leaving only the topmost visible/clickable. Cause: `ShopUIController.DisableShopLayoutAfterInit()` (now removed) was a coroutine meant to let the layout group position everything once, then disable it for perf. It was started from `Awake()`, and later in that same `Awake()`, `shopPanel.SetActive(false)` ran — which kills all coroutines on that GameObject before the paused coroutine could resume, so in current code it could never actually run. Yet the scene had it disabled anyway: at some earlier point it *did* complete, and that runtime-only `.enabled = false` mutation got baked into a scene save, exactly like the original 2026-07-03 Light 2D incident. Fix: re-enabled all three `VerticalLayoutGroup`s in the scene, and deleted `DisableShopLayoutAfterInit`/`DisableLayoutComponents` entirely rather than fixing their timing — a live layout group recalculating 16 static rows is a negligible cost, not worth a mechanism that silently corrupts saved scene state whenever it races a deactivation. Pattern rule reinforced: never gate a component's `.enabled` state on a coroutine that runs on a GameObject that might deactivate before the coroutine resumes.
- **Tab geometry and draw/raycast order are owned in code, not the scene, on principle now.** After the row-visibility fix, two more scene-authored assumptions turned out to be wrong or unreliable: `Tab_BP`/`Cash`/`RP` were anchored full-screen while `shopPanel` is bottom-60% (two independently-sized overlapping rects — `shopPanel`'s own backdrop then covered 100% of tab content once geometry was corrected, since it renders after `ShopRoot` in sibling order). Rather than hand-editing scene anchors again (see the `27fb988` correction above), `ShopUIController.NormalizeTabGeometry()` now sets each tab panel's anchors to match `shopPanel` exactly in code at `Awake()`, and `NormalizeDrawOrder()`/`SetTabPanelPresentation` set `Canvas.overrideSorting`/`sortingOrder` explicitly (tab content above the backdrop and `MainTapButton`, `closeButton` above the tab content in turn) — independent of sibling order entirely. One further gotcha worth remembering: a `sortingOrder` assigned once in `Awake()` did **not** reliably carry through for a `Canvas` left `.enabled = false` from boot until its first real enable (Cash/RP, since only the default-active tab, BP, was ever enabled at that Awake moment) — it has to be reasserted every time visibility is toggled, in the same place `.enabled` itself is already owned (`SetTabPanelPresentation`), not just once at startup. `shopPanel`'s own background `Image.raycastTarget` was deliberately left `true` throughout — explicit instruction: it's the catch-all for any gap in the scroll content (row spacing, area below the last row) so a tap can't fall through to `MainTapButton`, the original bug this whole chain started from.
- **Scene-smuggling instance #4 — RESOLVED 2026-07-12: CRLF/LF churn, not scene corruption.** What originally looked like an Editor save baking unrelated "Win"-window geometry drift and `PointsConversionGroup` fileID churn into a PremiumShop-only deletion was root-caused via a recovery-backup investigation on 2026-07-12: (a) **the phantom whole-file diffs were CRLF/LF churn** — the working tree's `core.autocrlf=true` writes `SampleScene.unity` with CRLF line endings, while Unity itself saves LF-only; comparing a CRLF-checked-out `HEAD` copy against a raw LF Editor-recovered backup makes every single line look changed (the line-ending byte differs) even when the YAML content is identical — confirmed by normalizing both files to LF-only and finding only **7** real differing lines out of 53,557. (b) **the real drift, once isolated, is those same 7 fully benign lines**: one dropped `restorationSlotPrefab: {fileID: 0}` (an orphaned RP-tab-era serialized field, already tracked as known-dead in `TASKLIST_DETAILS.md` §19) plus 6x `m_WasSpriteAssigned: 0 → 1` flips on the background-stage objects — a direct, expected downstream effect of the §6 BG1-BG6 sprite-mode heal (`36c76c1`), not corruption. The original "Win"/`PointsConversionGroup` fileID churn reported at the time was almost certainly this same CRLF-driven diff-shuffle phantom effect (see the next entry), not a distinct Unity behavior that needed its own fix.
- **Audit method upgrade: eyeball diff review is unreliable on `SampleScene.unity`, use math instead.** The scene contains 423 near-identical `Win`-popup blocks (per the instance-#4 investigation); a large deletion elsewhere in the file shifts diff hunk boundaries enough that many of these unrelated, unchanged objects get represented as phantom remove/add pairs — reading the hunks by eye reliably produces false positives (objects that look "changed" but are actually just relocated in the diff) and, worse, can hide a real change inside the noise. Verify scene diffs by **ordered-subsequence + multiset comparison of fileIDs** (extract all `!u!N &ID` headers from both the `-` and `+` sides, take the set difference to find IDs present on only one side — those are the true adds/deletes; anything appearing on both sides is reshuffle noise, not a real change) or by **comparing against an explicitly expected fileID/name set** enumerated before the edit — not by reading hunks top to bottom and judging by eye. **This rule stays in force permanently**, independent of the instance-#4 root cause turning out to be CRLF noise rather than a Unity save bug — CRLF churn is exactly the kind of thing that produces phantom whole-file diffs eyeball review would misread as real drift, which is the whole reason this rule exists; it does not become optional now that one specific alarm was a false positive.
- **Scene-save embargo update (2026-07-12): lift is conditioned on a `.gitattributes` `eol=lf` normalization commit, not yet lifted.** Since instance #4's root cause is CRLF churn rather than a genuine Unity resave bug, the actual fix is a one-time `.gitattributes` line-ending normalization commit (`eol=lf`, scoped at minimum to `*.unity`) — separate work, requires Unity closed, lands only on Aceyfer's explicit go. **Until that commit lands, the scene-save embargo from instance #4 remains fully in force**: no Editor scene save lands in a commit without a full math-verified audit first (see the entry above). The first post-lift scene save is *expected* to change exactly the same 7 lines identified in the recovery-backup audit (the dropped `restorationSlotPrefab` field + 6x `m_WasSpriteAssigned` flips) and nothing else — if it changes anything beyond those 7 lines, treat that as a new, real instance requiring its own root-cause investigation, not more CRLF noise.
- **Process rule: "Unity reloaded" means quit-and-relaunch, and re-verify HEAD after any session interruption.** The console's own timestamps are the tell for whether a genuine reload happened, not an assumption. Separately: after any session interruption (context/session limit, handoff between sessions, a gap in the conversation), re-verify the actual `git log`/`HEAD` state before trusting any tracker document's claims — `TASKLIST_DETAILS.md`'s own changelog has twice claimed a commit landed when it hadn't (see the `27fb988` correction, and the `0b5048a` patch that was generated but not applied before a session limit hit). No task step is ✅ without a commit hash next to it, and no commit hash should be written down without having just run `git log` to confirm it's actually there.
- **Cross-AI patch handoffs must be generated against a mirror of the receiving repo's exact `HEAD`, and stash-verified.** A patch (the B3-v1 rebirth-trigger-suppression patch, §16 Phase B) was generated on a stale mirror that predated the B2 commit already landed on `main` — its hunks tried to re-add a constant B2 had already introduced, and `git apply` correctly failed atomically (nothing partially written; the failure itself was the catch, not a review step). Confirmed the fix was simple — regenerate against current `HEAD`, apply clean — but the fact it could happen at all means the generating side's mirror had drifted from the receiving side's actual state without either side noticing until the apply itself failed. **Rule going forward: before generating a patch for another AI/session to apply, confirm the local mirror is at the exact commit the receiving repo is actually on, not an assumed or cached one.** Companion rule, receiving side: **the chat-side mirror commits the moment a hash is reported back** — don't let the mirror's own `HEAD` lag behind what's actually been pushed, or the next handoff repeats this exact failure mode.

## 9. First-Playable Checklist

**Phase 1 — Runs correctly: PHASE 1 COMPLETE 2026-07-05**
☑ Light 2D (blocker 1) · ☑ title ladder + HUD title (blocker 5) · ☑ Ped1 repoint workaround (blocker 8) · ☑ wire GameManager refs (blocker 4) · ☑ add + wire COGSWorldPortraitUI (blocker 3) · ☑ add PremiumShopManager to `_Systems` + wire UI ref (blocker 6) · ☑ verify (2 Play Mode cycles) + merge singleton teardown guards (blocker 9, commits 9639860/828dc36) · ☑ produce and wire Stage 1–6 backdrops (blocker 2)
**Phase 2 — Reads correctly:**
☐ UI icon downsize pass (§2.2) · ☐ build 3-tab unified shop panel (§2.4) · ☐ COGS/chatter bubble readability pass (§2.3) · ☐ middle-screen decongestion (blocker 7) · ☐ Play Mode pass: every HUD readout updates (BP, cumulative, BPPS, Cash, RP, IQ, Snottings, Restoration %, title)
**Phase 3 — Feels correct:**
☐ tap feedback end-to-end (punch + floating "+X" + splat) · ☐ audit the manual-wiring backlog (event pool ×16, narrator lines ×70+, COGS stages ×6, chapters ×12, SafeArea re-parenting) — verify what's actually still unwired · ☐ full loop test per §3 Definition of Done, including save/reload

**Everything not on this checklist is deferred (§10).**

## 10. Deferred — post-first-playable (good ideas, wrong time)

Real IAP for God Tier Store · secret ending sequence (flag exists) · COGS voicepack + Y2K theme swap · Wardrobe/outfit art + UI polish · COGSWorldPortraitUI visuals disabled pending COGS art restyle — object must stay ACTIVE, it hosts the COGSPortraitController singleton · dialogue font-degradation reconciliation (presentation still keys off PlayerIQ) · 12-chapter content review · `BrainRotEventData.multiplierSpike` (never read) · `ChapterUnlockConditionType.PointsSpent` re-pointing · regenerate real Ped1 Stage 1 art · new character/entity sprites of any kind (§2.5).

Scar tissue: never SetActive(false) an object hosting a self-bootstrapping singleton; hide visuals instead.

## 11. Doc Map

- **PROJECT_BIBLE.md** (this file) — the front door; state, rules, boundaries. Wins all conflicts.
- **CLAUDE.md** — deep architecture reference: every manager, event pattern, gotcha, and the full rebalance/Illumisnotti write-ups. Accurate as of 2026-06-22; trust it for *how things work*, trust the Bible for *what's true now*.
- **Assets/Docs/** — the single home for session handoffs going forward. New sessions append here, never to root.
- **Assets/Plans/*.md** — 34+ historical point-in-time plans. Design archaeology only; code has evolved past them. Never treat as current truth.
- **Assets/Docs/archive/** — retired docs (root `SESSION_HANDOFF.md`, `OVERNIGHT_REPORT.md`; archived on branch, commit `03d5939`), superseded by their own addenda; useful content harvested into §7–§8. Kept for archaeology, never for guidance.
