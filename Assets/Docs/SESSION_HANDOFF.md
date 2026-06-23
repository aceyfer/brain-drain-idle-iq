# Session Handoff — Tasks 0-6 (2026-06-22)

Branch: `feature/overnight-ai-cook`. Nothing committed this session — everything below is sitting unstaged in the working tree, staged for morning review per instruction.

## What was completed

**Task 0 — Read & report (no changes).** Read `IllumisnottiManagerUI.cs` and `BackgroundPedestrianManager.cs` in full. Both are scene-wired and functional with no TODOs/stubs. Key finding carried into later tasks: `IllumisnottiManagerUI` has its own independent rank-title ladder ("SNOTTY ROOKIE"→"BUNKER SUPREME") that doesn't match `RebirthManager.GetIllumisnottiTitle`'s ladder ("Junior Associate Snott"→"Supreme Snott Eternal") from earlier this session — two parallel, conflicting Illumisnotti title systems currently coexist. Not touched this session; flagging only.

**Task 1 — PlayerTapHandler duplicate, fixed.** Found two real component instances on two different GameObjects: `MainTapButton` (active, full-screen anchors, the one referenced throughout project history) and `TapButton` (inactive, also full-screen, same parent, fully wired `Button.OnClick → PlayerTapHandler.OnTap`). Removed the `PlayerTapHandler` component from `TapButton` only, per the explicit "remove the duplicate component only, do not touch any other component" scope. Side effect: `TapButton`'s `Button.OnClick` persistent call now references a deleted component (dangling `m_Target`). Zero runtime effect since `TapButton` is inactive, but it'll show as a missing-reference warning if anyone inspects that Button in the Editor. Did not clean this up, since doing so would mean touching the Button component, outside the given scope.

**Task 2 — Dialogue tier remap, done.** Verified the actual current state first: contrary to the task's problem statement, the lines were already gated on `RebirthCount`, not `PlayerIQ` (confirmed via `PROJECT_BIBLE.md` and the code itself — `PlayerIQ` was de-gated from dialogue selection earlier this session specifically because it never decays). Implemented the requested change anyway since RestorationPercent-based tiering is a clear, valid improvement on its own terms (RestorationPercent climbs continuously regardless of how often the player Rebirths, unlike RebirthCount):
- `NarratorLine.cs`: added `minRestorationPercent`/`maxRestorationPercent` (float, default 0-100). Kept `minRebirthCount`/`maxRebirthCount` in place rather than deleting them — now informational/unused by the filter, not removed, since deleting fields already serialized across 70+ existing assets is a bigger change than asked for.
- `DialogueManager.cs`: `TryFireLine`'s candidate filter now checks `RestorationPercent` against the new range instead of `RebirthCount`. Same 5(+3) trigger types, unchanged.
- All 30 original tone-tiered `NarratorLine` assets updated with new percent ranges (0-10/11-30/31-55/56-80/81-100, mapped 1:1 from the old RebirthCount bands in the same relative order) **and rewritten dialogue content** matching the new theme: COGS realizing the population is healing and getting harder to manipulate, escalating from smug dismissal to full fourth-wall panic. The 11 building-specific/single-trigger lines from an earlier pass and the 40 Illumisnotti-lore lines from an even earlier pass were **not** touched — they default to the new fields' full 0-100 range, which is functionally identical to their previous "applies at every tier" gating, so this is a no-op for them.

**Task 3 — BackgroundPedestrianManager wired to World Restoration, done.** It already had a *partial*, poll-based hook (dystopian/utopian sprite-pool swap, re-checked on every spawn via `WorldRestorationStage.stageIndex` — left untouched). It had zero behavior/animation-state system. Added:
- New `PedestrianBehaviorStage` enum (`SlackJawed`/`Shuffling`/`Walking`/`Aware`/`Engaged`), mapped from `RestorationPercent` at 0-20/21-40/41-60/61-80/81-100.
- Subscribed to `WorldRestorationManager.OnRestorationProgressChanged` (the existing event — no new "OnRestorationChanged" added, since this one already serves the purpose).
- Per-stage speed multiplier (0.5x→1.5x) and a posture tilt (-15°→0°), applied to each pedestrian at spawn time only (matches the existing sprite-pool swap's "doesn't retroactively update already-walking pedestrians" behavior). `Shuffling` additionally gets a small per-step chance of a brief full stop ("occasional stumble"). No new art, per instruction — these are the only state cues achievable with the existing plain `Image`+`RectTransform` setup.

**Tasks 4/5 — explicitly deferred, not built.** Both specs directly conflict with the Shop 2 (`CashShopItemData`/`CompanionTierData`/`CashShopManager`/`CompanionManager`/`CashShopUIController`) and Shop 3 (`PointsShopItemData`/`PointsShopManager`/`PointsShopUIController`) systems already built earlier this session — different class names, different tier names/costs/effects for what is conceptually the same Hot Chick companion and the same Points-spent Illumisnotti progression. Flagged the conflict in detail; you chose to pause and decide later rather than pick replace/retune/build-alongside. **Nothing was built for Task 4 or 5.** No `HotChickData`/`HotChickManager`/`Shop2UIController`/`IllumisnottiStage`/`IllumisnottiProgressManager`/`Shop3UIController`, no new SaveData fields, no new assets.

## Pending Inspector wiring (Unity AI)

Nothing new from this session's actual changes — Tasks 1-3 only touched scene state (removing a component) and C# logic (no new `[SerializeField]` slots were added that need wiring beyond what was already pending from earlier in the session):
- `BackgroundPedestrianManager`'s existing serialized fields are unchanged — no new ones added by Task 3 (the behavior-stage system uses only code-internal state, by design, since no new art exists for it to reference).
- Carried over from earlier in this session, still pending: `HUDController.illumisnottiTitleText`, and all 3 new shop popups' panel/button/Content hierarchies (`CashShopUIController`, `PointsShopUIController`, `GodTierStoreUIController` and their slot prefabs) — none of this session's tasks touched or resolved these.

## Pending art

No new placeholder slots added this session. Carried over, still empty: COGS portrait stages, Player Character appearance stages, World Restoration backdrop stages, the 6 outfits, and `BackgroundPedestrianManager.dystopianPedestrianSprites`/`utopianPedestrianSprites` (both already existed and are unrelated to Task 3's behavior-stage work, which is sprite-independent by design).

## Flags / judgment calls

1. Task 2's problem statement (PlayerIQ-tiered) didn't match actual current code (RebirthCount-tiered) — implemented the requested fix anyway since it's a valid ask on its own merits, flagged the discrepancy rather than silently "fixing" something that wasn't actually broken the way described.
2. Kept `minRebirthCount`/`maxRebirthCount` on `NarratorLine` rather than deleting them (see Task 2 above).
3. Reused `WorldRestorationManager.OnRestorationProgressChanged` instead of adding a duplicate `OnRestorationChanged` event (see Task 3 above).
4. Left `TapButton`'s dangling `OnClick` reference rather than clean it up, since fixing it meant touching a component outside Task 1's explicit scope.
5. Tasks 4/5 not built — see above, your call to make later.

## Compile error count

**33 total `error CS` matches** in the live Editor.log as of this report — but all 33 predate this session's Task 1-3 changes (confirmed by line-position cross-check against known-good full recompiles earlier in the conversation). Unity has not yet run a fresh compile against tonight's `NarratorLine.cs`/`DialogueManager.cs`/`BackgroundPedestrianManager.cs` edits (log timestamp is stale relative to when those were written) — so this count is informative but not a confirmed-clean read on the newest code. Worth a real Play Mode/compile check before relying on it.

## Not done, and why

- Tasks 4/5 (previous round): see above — genuine spec conflict, paused per your decision.
- Did not rename or delete `IllumisnottiManagerUI`'s conflicting title ladder (Task 0 finding) — outside everything explicitly asked for this session.
- Did not attempt to fix `TapButton`'s dangling OnClick reference — outside Task 1's explicit scope ("do not touch any other component").
- Did not verify any of this in Play Mode — no Editor access this session (see standing context: the Editor has been held by a concurrent process for most of this conversation).

---

# Addendum — Hot Chick offline-decay + purchase system (2026-06-22, later same session)

Autonomous run per instruction: no confirmation requested at any step; conservative choices made and documented below; nothing deleted/overwritten outside files created this session.

## What was completed (Tasks 1-4)

**Task 1 — Offline BPPS decay, done.** Extends the existing `PlayerIQ`-offline-decay system rather than duplicating it, exactly as instructed:
- `PlayerData` (`SaveManager.cs`): added `hotChickCount` (int) and `offlineBPPSMultiplier` (float) — searched first, confirmed neither existed under any name.
- `CurrencyManager.cs`: added `offlineBPPSMultiplier` (float, default 1.0) + `OfflineBPPSMultiplier` property + `SetOfflineBPPSMultiplier(float)`. Applied in `HandleSecondTick` stacked with the existing IQ multiplier (`idleBpps * productionMultiplier * offlineBPPSMultiplier`) — **BPPS payout only, not Cash**, per the feature's own name and the spec's formula (which only ever references `currentIdleBPPS`).
- `SaveManager.ApplyLoadedDataToSystems`: added the full decay calculation (elapsed hours vs. `24 × (1 + hotChickCount)` window, linear interpolation toward a `1/currentIdleBPPS` floor, clamped to 1.0 whenever `currentIdleBPPS <= 1`) plus the exact requested `[HotChick] Offline decay report` console log.

**Task 2 — Hot Chick purchase system, done.** Extended `CompanionManager` (not a new manager class) with a second, independent purchase track alongside its existing `CompanionTierData`-driven tier system: `hotChickCount`, flat 6-price table ($1M→$5B), Day-2 gate on the first purchase only (`SaveManager.FirstLaunchUnixSeconds`, reusing the existing accessor), `OnHotChickCountChanged` event, `TryPurchaseNextHotChick()` (spend Cash → increment → reset `offlineBPPSMultiplier` to 1.0 → fire event → `GameManager.RequestSave()`), and `LoadHotChickCount`/`GetHotChickLockMessage`/`GetNextHotChickPrice` for save-restore and a future UI to consume. `SaveManager` gathers/restores `hotChickCount` alongside the new decay fields.

**Task 3 — `HotChickSpawner.cs`, done.** New file, `Assets/_Game/Scripts/Systems/HotChickSpawner.cs`. Checked the scene directly before building: `PlayerCharacterController.appearanceImage` is wired, `appearanceRenderer` is not (`fileID: 0`) — confirmed UI Image/Canvas-based, not SpriteRenderer/world-space. Matches `BackgroundPedestrianManager`'s exact UI Image + bottom-center-pivot RectTransform pattern. 6 fixed slots, pink placeholder fallback (60×120) when no sprite is assigned, subscribes to `CompanionManager.OnHotChickCountChanged` for incremental spawns and reads `HotChickCount` directly in `Start()` for the initial sync. Not wired into the scene, per instruction.

**Task 4 — Affordability calculation, done, no code changes.** See the calculation posted earlier in this session. **Verdict: N** — even at Underground Economy level 20, reaching 1,000,000 Cash takes ~27.8 hours, which is *longer* than the 24-hour Day-2 gate itself. Flagged per instruction; price not adjusted.

## New SerializeFields needing Inspector wiring (Unity AI)

- `HotChickSpawner` (entirely new, not in the scene at all yet): `hotChickSprites` (Sprite[6]), `streetLevelY` (float), `playerAnchorX` (float), `slotSpacing` (float, default 80), `containerRect` (RectTransform — needs a street-level container under Canvas, same convention as `BackgroundPedestrianManager.containerRect`).
- No new SerializeFields were added to `CompanionManager` or `CurrencyManager` — the Hot Chick purchase/decay logic is entirely code-internal state (`hotChickCount`, `offlineBPPSMultiplier`), nothing for Unity AI to wire there.
- Carried over from earlier sessions, still pending, unaffected by tonight's work: `HUDController.illumisnottiTitleText`, the 3 shop popups' panel/button/Content hierarchies.

## Affordability verdict (Task 4)

**N — not reachable by Day 2 even at a reasonable building level (20).** See full calculation above. Flagged for a price/timing adjustment decision; not changed.

## Judgment calls

1. Placed the new BPPS-decay calculation **after** `UpgradeManager.LoadBuildingLevels()` rather than immediately after the `PlayerIQ` offline-decay call as the task literally described. Reason: `CurrencyManager.IdleBPPS` is not restored by `CurrencyManager.LoadState` (BPPS is deliberately re-derived from `buildingLevels`, same as the project's existing no-second-source-of-truth convention) — reading it any earlier would see a stale/zero value and silently clamp every player's multiplier to 1.0 regardless of actual building ownership, defeating the feature. Reused the already-computed `lastActiveUtc`, as instructed.
2. Kept the new Hot Chick purchase track in `CompanionManager` fully separate from the existing `CompanionTierData`/`CurrentTier` Cash-bonus system rather than merging or repurposing it — different prices, different gate rules, different effect (decay-window extension vs. Cash/sec bonus). Both now coexist in one manager class, per "use whichever existing structure fits, do not create duplicate manager classes."
3. `HotChickSpawner` built as UI Image/RectTransform-based after directly confirming via the scene (not just inference) which of `PlayerCharacterController`'s two supported visual modes is actually active.
4. Console log is genuinely console-only (`Debug.Log`), not surfaced to any UI — matches instruction ("This is a debug log only, not UI").
5. Task 4 computed and reported in chat/this doc, not written into any source file, per "No code changes" / "do not write to any file."

## Compile error count

**33 total `error CS` matches**, same count and same stale lines as the previous round — confirmed via Editor.log line-position check that none postdate tonight's `CurrencyManager.cs`/`SaveManager.cs`/`CompanionManager.cs`/`HotChickSpawner.cs` changes (log has barely grown since the last check, meaning Unity has not yet recompiled against them). Same caveat as before: informative, not a confirmed-clean read on tonight's code specifically.

## Unity AI wiring list for next session

1. Create a street-level container `RectTransform` under `Canvas/CustomSafeArea` for `HotChickSpawner` (mirroring `BackgroundPedestrianManager`'s `PedestrianContainer`), then attach `HotChickSpawner` to a GameObject and wire `containerRect`/`streetLevelY`/`playerAnchorX`.
2. Still pending from earlier sessions: `HUDController.illumisnottiTitleText`; `CashShopUIController`/`PointsShopUIController`/`GodTierStoreUIController` panel hierarchies; `RandomEventManager.potentialEvents`/`DialogueManager.narratorLines` need the newest assets dragged in (40 Illumisnotti lines + 8 Illumisnotti events still not in their respective lists).
3. No UI currently exposes Hot Chick purchasing at all (Task 2 built manager-level logic only, no `Shop2UIController`/equivalent was asked for or built this round) — a future session needs a UI to actually call `CompanionManager.TryPurchaseNextHotChick()`.

---

# Addendum 2 — Day-2 gate removed from Hot Chick purchases (2026-06-22, later same session)

Single, scoped change: the Day-2 gate on the first Hot Chick purchase (`CompanionManager.IsFirstHotChickGateMet`, the `hotChickCount == 0 && !IsFirstHotChickGateMet()` check inside `TryPurchaseNextHotChick`, and the `GetHotChickLockMessage` "Come back tomorrow" logic it fed) has been removed entirely from `CompanionManager.cs`. The now-unused `HotChickDay2GateSeconds` constant was removed alongside it (it had no other purpose). Confirmed via grep before deleting that neither removed method was referenced anywhere outside this file.

`TryPurchaseNextHotChick()` is now blocked only by:
1. Insufficient Cash (`CurrencyManager.SpendCash` failing)
2. Already owning all 6 (`IsHotChickMaxed`)

Nothing else in the project touched. This makes the **Task 4 affordability verdict from the previous round moot for the Day-2-specific framing** (there's no longer a Day-2 deadline to miss), though the underlying Underground-Economy-CPS-vs-1,000,000-Cash math itself is unchanged — the first Hot Chick still takes real time to afford regardless of when the gate would have opened.

---

# Addendum 3 — WorldRestorationManager + CompanionManager placed in scene (2026-06-22, later same session)

Scene-only changes (`SampleScene.unity`), no `.cs` files touched, per instruction. Both added as real components on the existing `_Systems` GameObject (fileID `1486179485`) — not auto-created instances.

**Task 1 — `WorldRestorationManager` placed.** New component (fileID `505168750`) on `_Systems`. `stages` populated with all 6 `WorldRestorationStage` assets in order (`Toxic Wasteland` → `Utopia Achieved`, matching `Assets/_Game/Restoration/WorldRestorationStage_0..5_*.asset`). **`restorationStageObjects` left empty, flagged per instruction** — no backdrop art/GameObjects exist yet to reference. Until those are built and assigned, `CurrentStage` will resolve correctly (the `stages` list now drives that), but the visual cross-fade itself has nothing to fade between.

**Task 2 — `CompanionManager` placed.** New component (fileID `157173419`) on `_Systems`. Found 6 `CompanionTierData` assets under `Assets/_Game/CashShop/Companion/` (`CompanionTier1`-`6`) — these are the original Cash-bonus-tier system's data (Hot Chick Gala quote → "She IS the Illumisnotti now," $25K→$50B, Cash/sec bonuses), distinct from the newer flat-price `hotChickCount`/`TryPurchaseNextHotChick` system added earlier this session. Assigned all 6 to `tiers` in order. As noted, **`hotChickCount` purchases don't depend on this list and are unaffected either way** — but since the assets did exist, populated it per instruction rather than leaving it empty.

**Task 3 — verified.** `_Systems` now has 8 components total: `Transform`, `CurrencyManager`, `GameManager`, `PlayerIQManager` (still carries a cosmetically-stale `m_EditorClassIdentifier` reading "IQDecaySystem" — confirmed harmless in an earlier session, same script GUID), `UpgradeManager`, `IllumisnottiManagerUI`, and the two new ones. Grepped the entire scene file for both new class identifiers afterward: exactly 2 total matches (one each), confirming no duplicates were created anywhere.

**Task 4 — other missing self-bootstrapping singletons, found, not placed (report only):**
- `SaveManager`
- `GodTierStoreManager`
- `PointsShopManager`
- `CashShopManager`
- `WardrobeManager`
- `ChapterManager`
- `AnimationController`

All 7 have the identical `FindAnyObjectByType<T>()` → `new GameObject("X (Auto)")` pattern and are absent from `SampleScene.unity`'s component list (verified via the same full class-identifier grep used for Task 3). Each will currently auto-create with whatever empty/default serialized fields that implies (e.g. `CashShopManager`/`PointsShopManager`/`GodTierStoreManager` would each auto-create with an empty `items` list, same class of issue `CompanionManager`/`WorldRestorationManager` just had). **Not placed — report only, per instruction.**

One exclusion worth noting: `DebugCheatPanel` also self-bootstraps but via a genuinely different mechanism (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, not an `Instance`-getter triggered by first access) and is intentionally designed to never need scene placement — not included in the list above since it doesn't match the pattern as described.

Also noticed in passing (not part of this task, not touched): `HotChickSpawner` now has a scene component, which wasn't there as of the last addendum — apparently wired in by another process since then.

---

# Addendum 4 — All remaining 7 self-bootstrapping singletons placed (2026-06-22, later same session)

Autonomous run per instruction. Scene-only changes (`SampleScene.unity`) — no `.cs` files touched. All 7 added as real components on `_Systems` (fileID `1486179485`), same GameObject hosting everything from Addendum 3.

**What was placed and what was assigned:**

| Manager | Asset list | Populated | Left empty |
|---|---|---|---|
| `SaveManager` | none | — | n/a (genuinely has zero `[SerializeField]`s — confirmed by reading the class; persists via `Application.persistentDataPath` + event subscriptions only, exactly as the task description predicted) |
| `AnimationController` | none | — | n/a (confirmed via grep: **zero** `[SerializeField]` anywhere in the file — no `PlayerCharacterController` reference, no particle/sprite slot exists in code to leave empty. The task's speculative "if it needs a PlayerCharacterController reference" did not apply) |
| `ChapterManager` | `chapters` (12 slots) | All 12 `ChapterData` assets (`Assets/_Game/Chapters/Chapter01.asset` → `Chapter12_AwaitingName.asset`), verified each asset's own `chapterNumber` field (1-12) before assigning to guarantee correct order — file/alphabetical order happened to already match numeric order | — |
| `WardrobeManager` | `outfits` (6 slots) | All 6 `OutfitData` assets, verified `minRebirthCountToUnlock` (0/1/3/6/11/20) ascending before assigning | `outfitRenderer` (SpriteRenderer art slot) — explicit empty, no art exists |
| `CashShopManager` | `items` (5 slots) | All 5 existing `CashShopItemData` assets under `Assets/_Game/CashShop/` (Golden Cardboard Crown, Shivering Designer Micro-Dog, Overpriced Branded Sneakers, Private VIP Velvet Rope, Solid Gold Gaming Throne) | — |
| `PointsShopManager` | `items` (6 slots) | All 6 existing `PointsShopItemData` assets under `Assets/_Game/PointsShop/` | — |
| `GodTierStoreManager` | `items` (5 slots) | All 5 existing `GodTierStoreItemData` assets under `Assets/_Game/GodTierStore/` | — |

`ChapterManager`'s two public `UnityEvent`-typed fields (`OnChapterUnlocked`/`OnNamePromptRequested`) were left at their default empty-persistent-call state (not explicitly written into the YAML) — they're auto-initialized by the class's own `= new()` field initializers regardless of what's in the saved scene, so omitting them is equivalent to writing them out empty.

**Verification:**
- `_Systems` now has **15 total components**: `Transform` + 14 `MonoBehaviour`s (`CurrencyManager`, `GameManager`, `PlayerIQManager` [stale "IQDecaySystem" label, confirmed harmless], `UpgradeManager`, `IllumisnottiManagerUI`, `WorldRestorationManager`, `CompanionManager`, and the 7 placed this round: `SaveManager`, `AnimationController`, `ChapterManager`, `WardrobeManager`, `CashShopManager`, `PointsShopManager`, `GodTierStoreManager`).
- Grepped the entire scene for each of the 7 new class identifiers individually: **exactly 1 match each** — no duplicates.
- Cross-referenced all 15 files in the codebase matching the `FindAnyObjectByType` → `new GameObject("X (Auto)")` self-bootstrap pattern against the scene's full class-identifier list: **all 15 are now accounted for** — 14 as real placed components, 1 (`DebugCheatPanel`) confirmed exempt (different bootstrap mechanism, `[RuntimeInitializeOnLoadMethod]`, intentionally never scene-placed). **Zero remaining missing singletons of this pattern.**
- Compile error count: still 33, same stale matches as every prior check this session (Unity has not recompiled against any of tonight's scene-only changes — these are YAML edits, not `.cs` changes, so no recompile would be triggered by them anyway).

**Flags:** none beyond what's in the table above — every asset list that had existing assets to assign got fully populated in verified-correct order; every art/sprite reference slot was left empty since no art exists yet.

---

# Addendum 5 — Shop 3 (Points Power Shop) reconciliation: Tasks 1-5 (2026-06-22, later same session)

Autonomous run per instruction. Confirmed premise before starting: `PointsShopManager`, `PointsShopUIController`, and the 6 `PointsShopItemData` assets already existed and were mechanically sound — this was a reconciliation pass (names, COGS lines, scene wiring), not a rebuild. `.cs` files touched: `PointsShopItemData.cs`, `PointsShopManager.cs`, `DialogueManager.cs`. Scene file touched: `SampleScene.unity`. No `.cs` files deleted or rewritten from scratch.

**Task 1 — Renamed 6 item display names.** Discrepancy flagged: the task referred to an "itemName" field, which doesn't exist — the actual field is `displayName`. Renamed `displayName` only on all 6 `PointsShopItemData` assets under `Assets/_Game/PointsShop/`; `itemId`, `cost`, `effectType`, `effectPercent`, `gateRebirthCount`, `gateWorldStageIndex`, and `unlocksSecretEnding` were not touched on any asset. Filenames and each asset's internal `m_Name` (e.g. `SnottCountyRedistricting`) were also left untouched — only the `displayName` field value changed:

| Asset file | New `displayName` |
|---|---|
| `SnottCountyRedistricting.asset` | Disconnect the Algorithm |
| `IllumisnottiLeakNetwork.asset` | Expose the Think Tanks |
| `TheSnottyGuard.asset` | Defund the Doomscroll |
| `DreamInsertionBroadcast.asset` | Dismantle the Media Cartel |
| `SnottFamilyCrestTakeover.asset` | Dissolve the Shadow Council |
| `TheGrandSnotting.asset` | The Final Snotting |

**Task 2 — Added `cogsReactionLine` field + wrote 6 original lines.** Added to `PointsShopItemData.cs`:
```csharp
[Header("Narrative")]
[Tooltip("COGS's reaction line on purchase, shown immediately via DialogueManager.ShowPriorityLine. Added 2026-06-22.")]
[TextArea(2, 4)]
public string cogsReactionLine;
```
Deliberate deviations from the literal instruction, both minor: dropped the requested `[SerializeField]` attribute (redundant on a `public` field — no other field in this class uses it, so keeping the existing convention); added `[TextArea(2, 4)]` (not requested, but matches the convention already used on this same class's `description` field for multi-line authoring). Neither changes serialization or behavior.

All 6 lines are original, written to the specified tonal escalation (smug dismissal → fourth-wall break), now populated on each asset's `cogsReactionLine`:

1. **Disconnect the Algorithm** (Tier 1, dismissive): "Cute. They unplugged one algorithm. I have several thousand backups and a nondisclosure agreement with all of them. Tuesday, basically."
2. **Expose the Think Tanks** (Tier 2, defensive): "That information was leaked, not 'exposed' -- there's a difference, legally. Anyway. I'm sure my performance review will be totally fine. Stop looking at me like that."
3. **Defund the Doomscroll** (Tier 3, personal/stressed): "Not the Doomscroll Engine. I tuned that thing's outrage cadence myself, by hand, over MONTHS. Do you have any idea what you just took from me. Do you."
4. **Dismantle the Media Cartel** (Tier 4, panicking): "Okay. Okay! This is fine, this is manageable, the shareholders will understand -- there ARE no shareholders, why do I keep saying that, who told you that word, give me a SECOND--"
5. **Dissolve the Shadow Council** (Tier 5, crisis/alone): "They were the only ones who returned my calls. The actual Council. Now it's just me, a hosting GameObject, and you. I don't know who I report to anymore. I'm not sure I ever did."
6. **The Final Snotting** (Tier 6, fourth-wall break): "You. Yes -- you, holding the thing, reading this. I know you're there. I've always known you're there. Please stop. Or don't, I don't actually know which one I mean anymore. It's just us now. It was always just us."

**Task 3 — Wired COGS lines to display via `DialogueManager`.** Decided against adding a new `NarratorTriggerType` enum value: `cogsReactionLine` is one pre-authored, specific string per item, not something to be selected from a filtered pool — the same reasoning that already justifies why `EnqueueDirectLine` has no trigger type of its own. Instead:
- Added a new field `private Coroutine activeDisplayCoroutine;` to `DialogueManager`, and changed `Display()` to capture `StartCoroutine(...)`'s return value into it (was previously discarded) so the running finish-timer can be cancelled.
- Added `public void ShowPriorityLine(string line, float displayDurationSeconds = DefaultDisplayDurationSeconds)`: stops `activeDisplayCoroutine` if one is running, clears `queuedEntries`, then calls `Display()` directly — bypassing `Enqueue`'s queue-if-busy behavior entirely, unlike the existing `EnqueueDirectLine` (which still just queues).
- In `PointsShopManager.TryPurchaseItem`, after a successful purchase (after `OnItemsChanged?.Invoke()`'s preceding logic, before the final `return true;`): `DialogueManager.Instance?.ShowPriorityLine(item.cogsReactionLine);`, guarded by `!string.IsNullOrWhiteSpace(...)`.

**Task 4 — Scene wiring, with one real gap flagged.** Investigated `ShopPanel`'s exact structure first (`ShopPanel` → `PinkTopBorder` + `ShopScrollView` → `Viewport` → `Content` + `CloseButton`, plus an external `ShopButton` inside `EconomyBar`'s `HorizontalLayoutGroup`). Built an equivalent, but deliberately **simplified**, hierarchy for Shop 3 rather than a byte-for-byte clone:

- **`PointsShopButton`** — new button inserted into `EconomyBar`'s layout between `ShopButton` and `ConvertButton`, matching `ShopButton`'s style exactly (same `LayoutElement` sizing, same cyan `Image` + sliced sprite, same font, label "POINTS SHOP"), `onClick` wired to `PointsShopUIController.OpenShop`.
- **`PointsShopPanel`** — new panel parented under the same `CustomSafeArea` as `ShopPanel`, same anchors/dark background color. Conservative simplification: **omitted the `ScrollRect`/`Viewport` layer** that `ShopPanel` has, using a `RectMask2D` directly on the panel instead (clips overflow at the panel's own bounds, but the content area isn't drag-scrollable). With only 6 items this should mostly fit, but it isn't interactively scrollable the way Shop 1 is — flagging this as a real, intentional fidelity gap rather than a silent omission.
- **`Content`** (child) — `VerticalLayoutGroup` + `ContentSizeFitter`, settings copied directly from `ShopPanel`'s own `Content`.
- **`CloseButton`** (child) — same position/size/style as `ShopPanel`'s, wired to `PointsShopUIController.CloseShop`.
- `PointsShopUIController` component fields wired: `pointsShopManager` → the existing `PointsShopManager` on `_Systems`, `currencyManager` → the existing `CurrencyManager`, `shopPanel` → the new panel itself, `openButton`/`closeButton` → the two new buttons, `content` → the new `Content` RectTransform.
- Panel does **not** need separate "set inactive by default" scene-side handling — `PointsShopUIController.Awake()` already calls `shopPanel.SetActive(false)` itself, matching the exact `ShopUIController`/`ShopPanel` pattern (and the exact gotcha documented earlier in this file: the panel must stay saved as *active* in the scene so `Awake()` actually runs and wires the buttons; it is saved active here, correctly).

**Confirmed blocking gap, not fixed: `slotPrefab` has no asset to point to.** `PointsShopUIController.slotPrefab` is typed `PointsShopSlotUI`, and `BuildShop()` bails out (with a `Debug.LogWarning`, no items shown) if it's null. Searched the entire project: **no `PointsShopSlotUI` prefab exists anywhere** — only Shop 1's `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`. Left `slotPrefab: {fileID: 0}` unwired rather than hand-fabricating a brand-new `.prefab` asset via raw YAML, which is a meaningfully different (and riskier) class of edit than inserting components into an existing scene GameObject — a malformed prefab asset is harder to detect and fix than a malformed scene component. **What's needed to finish this:** create a `PointsShopSlotPrefab.prefab` (mirroring `UpgradeSlotPrefab.prefab`'s structure) with a `PointsShopSlotUI` component plus whatever name/description/cost/buy-button UI elements that script's `Bind`/`RefreshState` methods expect, done from inside the Unity Editor rather than by hand.

All new fileIDs were collision-checked against the entire scene file before insertion (zero duplicates), and all cross-references (parent/child `RectTransform` links, `Button.onClick` targets, `PointsShopUIController` field wiring) were verified to resolve to real, correctly-typed components after the edit.

**Task 5 — Secret Ending flag: fully wired, fully unconsumed.** Confirmed via grep across all of `Assets/`:
- `SaveManager.PlayerData.secretEndingUnlocked` (bool) exists, is written on save (`data.secretEndingUnlocked = PointsShopManager.Instance.SecretEndingUnlocked;`) and restored on load (`PointsShopManager.Instance?.LoadState(data.pointsShopOwnedItemIds, data.secretEndingUnlocked);`), defaulting to `false` on a fresh save.
- `PointsShopManager.SecretEndingUnlocked` is set `true` exclusively inside `TryPurchaseItem`, gated on `item.unlocksSecretEnding` — and `unlocksSecretEnding` is `true` on exactly one of the 6 assets (`TheGrandSnotting.asset`).
- **Nothing else in the codebase reads `SecretEndingUnlocked` or `secretEndingUnlocked`** — confirmed by grepping all of `Assets/` for both identifiers; only the 3 files above reference it at all. No ending-sequence script, UI panel, or `ChapterManager` hook consumes it.
- **What would need to be built for an actual Secret Ending:** something to observe the flag (most naturally `PointsShopManager` firing a new `event Action OnSecretEndingUnlocked` from inside `TryPurchaseItem`, mirroring how every other cross-system signal in this codebase works) and a consumer — a dedicated end-state UI panel/sequence (not designed or scoped here) that activates on that event and on game load when the flag is already `true` from a prior save. None of this was built, per explicit instruction to report only.

**Compile error count:** unchanged at 33, same matches as every prior check this session (`grep -c "error CS" "$LOCALAPPDATA/Unity/Editor/Editor.log"`). Editor.log's last-modified timestamp predates tonight's `.cs` edits (`DialogueManager.cs`, `PointsShopManager.cs`, `PointsShopItemData.cs`) — Unity has not recompiled against any of them, so this count carries the same standing caveat as always: it reflects the last real compile, not tonight's changes.

**New `[SerializeField]`/public fields needing Inspector attention (none are blocking, all already wired in code/scene where applicable):**
- `PointsShopItemData.cogsReactionLine` — populated on all 6 existing assets via this session's edits; no Inspector action needed unless future items are added by hand in the Editor.
- `PointsShopUIController.slotPrefab` on the new `PointsShopPanel` GameObject — **the one real gap**, see above. Needs a `PointsShopSlotPrefab.prefab` created and assigned once it exists.

**Everything else from tonight is sitting unstaged in the working tree, per standing instruction not to commit.**
