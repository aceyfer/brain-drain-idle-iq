# Codex → Claude Code Handoff: Visual Style Guide

**Date:** 2026-09-15  
**Work state:** Documentation only. No game asset, scene, prefab, script, package, or import setting was changed.

**Update — Pass A implemented later on 2026-09-15:** the original work-state sentence above describes the guide-writing checkpoint. The first visual implementation pass now changes exactly `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs` and `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`; details are recorded below. No scene was intentionally edited by Codex.

## Start here

The working documents are:

`Assets/Plans/BRAIN_DRAIN_VISUAL_STYLE_GUIDE.md`

`Assets/Plans/STAGE1_VISUAL_SLICE_IMPLEMENTATION_MAP.md`

Claude Code should treat the first as the consolidated first-playable visual direction and the second as the exact runtime/scene ownership map. Both are subordinate only to explicit newer decisions from Aceyfer and `PROJECT_BIBLE.md`. Do **not** restart with another broad art audit. If the two creative choices in the map's approval gate remain accepted, begin with Pass A.

## What Codex inspected

- Live Unity Game view in `Assets/Scenes/SampleScene.unity`, running in Unity 6000.4.8f1.
- Existing Stage backgrounds: `Assets/_Game/Sprites/Backgrounds/BG1.jpg` and `BG2.png`–`BG6.png`.
- Representative foreground/UI art:
  - `Assets/_Game/Sprites/Buildings/BrainJuiceExtractor.png`
  - `Assets/_Game/Sprites/Characters/COGs/COGs1.png`
  - `Assets/_Game/Sprites/UI/VintageTV.png`
  - `Assets/_Game/Sprites/Particles/GooSplats.png`
- Binding direction and constraints in `PROJECT_BIBLE.md` §2, §3, §6, §8, and §10.
- Historical/current plan context in:
  - `Assets/Plans/ui-visual-overhaul.md`
  - `Assets/Plans/stage-ui-visual-systems.md`
  - `Assets/Plans/character-art-redesign.md`
  - `Assets/Plans/world-restoration-backdrop-art-spec.md`

## Main findings incorporated into the guide

1. The six-background restoration arc is the strongest current visual idea, but style continuity needs explicit rules so it reads as one recovering civilization.
2. Current asset families clash: cinematic/photoreal backgrounds, glossy realistic COGS portraits, outlined cartoon buildings, pixel pedestrians, and flat neon UI.
3. The guide does **not** demand that every layer use the same rendering technique. It defines how cinematic world art and cartoon foreground art coexist through hierarchy, outline, rim light, contrast, and semantic color.
4. Live UI black levels were too low for reliable reading in the inspected Game view. Multiple visible overlays competed simultaneously.
5. During inspection, Settings, dialogue chrome, and the “COGS docked you 0 IQ… Watch an ad” prompt were visibly stacked. This is recorded as presentation evidence only; no logic diagnosis or fix was attempted in this documentation task.
6. The first-playable constraint against new character/entity families remains binding. This guide prioritizes refinement and unification of existing assets.
7. The zero-tolerance rule for character-art defects is preserved as a production checklist, not relaxed for mobile resolution.

## Decisions made

- North star: **“Corporate dystopia sold through a broken Saturday-morning propaganda machine.”**
- World rendering may remain cinematic; foreground buildings/props remain clean satirical cartoons.
- Cyan = intelligence/control, pink = brain corruption/temptation, green = restoration/life, bright gold = Cash, tarnished gold = Illumisnotty/Snotting.
- Stages 0–3 use no visible sun/direct sunlight; Stage 4 earns the first daylight reveal.
- COGS progression should share crop, eye position, head scale, camera angle, and identity cues across all six portraits.
- UI uses restrained neon as information, not decoration.
- First implementation is a single Stage 1 gameplay vertical slice, not a scene-wide restyle.

## Recommended next task for Claude Code — updated after mapping

The implementation map is complete. Do not reproduce it. Begin with **Pass A — prefab/code-owned row styling** from `STAGE1_VISUAL_SLICE_IMPLEMENTATION_MAP.md`:

1. Reconfirm the active unified shop path uses `UpgradeSlotPrefab.prefab` for both BP and Cash building rows.
2. Implement the row's carbon surface, semantic identity rail, compact hierarchy, and state colors in the prefab and the runtime owner `UpgradeSlotUI.cs`.
3. Do not run any legacy `Fix*`, `*WireFix`, or `*Overhaul` menu command.
4. Verify locked, unaffordable, affordable, and owned states in Play Mode.
5. Produce focused diffs and portrait screenshots, then update or supersede this handoff.

## Questions remaining for Claude Code—not a broad audit

1. Confirm whether the Cash Investments rows in the active unified shop use `UpgradeSlotUI` or `CashShopSlotUI`; the map deliberately leaves this as the final pre-edit check.
2. Locate the controllers bound to `AdwareEventPopup` and `RewardedAdRecoveryPopup` and determine the smallest existing-controller modal-exclusion change.
3. Which generated UI sprites are genuinely referenced by the Stage 1 slice, and which are unused remnants?
4. Does the current `SkylineBG` crop/mask leave stable quiet zones for HUD/dialogue, or should only the receiver geometry change?

Answer each when its relevant pass begins. Do not delay Pass A for the overlay/background questions. Do not regenerate artwork or run scene-wide editor tools implicitly.

## Verification expectations for future implementation

- Preserve all unrelated working-tree changes.
- Never stage persistent TMP font-asset noise.
- Pair new Unity assets/scripts with their `.meta` files.
- Inspect every scene diff structurally, not only by visual diff hunks.
- Verify at 1080×1920 and at a smaller phone-equivalent view.
- Capture the default gameplay state, shop, dialogue, restoration, and one modal.
- Confirm no overlapping overlays and no loss of input/raycast behavior.
- Update this handoff or create a dated successor after every material visual implementation pass.

## Implementation checkpoint — Pass A building rows

Codex confirmed that the active unified shop instantiates the same `UpgradeSlotUI slotPrefab` into either `bpContent` or `cashContent` (`ShopUIController.cs:877-919`). `CashShopSlotUI` is not the controller for these unified BP/Cash building rows; it belongs to the separate legacy/companion Cash Shop path and was not changed.

Changed files:

- `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs`
- `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab`

Implemented:

- Replaced full-card state tinting with a stable carbon card (`#11151B`, high opacity).
- Wired the existing `LeftAccent` Image into a new serialized `identityRail` field.
- BP rows use electric cyan `#00DDEB`; Cash rows use acid gold `#F5C542`.
- Locked rows use ash `#59616A`; affordable price state uses restoration green `#75F04C`; unavailable price state uses cool gray `#9BA8B5`.
- Reduced runtime-owned typography: name 30, description 22, count 22, normal price 26, locked requirement 24.
- Reduced row height from 240 to 210 reference-canvas units.
- Reduced outer padding from 20/20/15/15 to 16/16/12/12 and spacing from 15 to 12.
- Increased the semantic rail width from 4 to 8 so it remains legible after the row-height reduction.
- Preserved affordability pulse, denial shake, first-affordable nudge, labels, purchase routing, and interaction behavior.

Verification performed:

- `git diff --check` passed for the two implementation files and all new documentation.
- Focused diff: 38 insertions, 25 deletions across the prefab and controller.
- No `error CS` line associated with `UpgradeSlotUI.cs` was present in the inspected Unity Editor log after refresh.

Verification still required in a clean Play Mode cycle:

1. BP and Cash tabs both populate with the new rail colors.
2. Locked, unaffordable, affordable, and owned rows remain readable.
3. The narrower row still fits the longest current description at the smallest supported portrait size.
4. Affordable pulsing does not visually overpower the semantic rail.
5. Buy-button, row denial shake, and first-affordable nudge still target the intended rects.

Claude Code should start from this checkpoint rather than re-implementing Pass A. If Pass A passes visual QA, proceed to Pass B in `STAGE1_VISUAL_SLICE_IMPLEMENTATION_MAP.md`.

## Implementation checkpoint — Pass B HUD/navigation + completed Pass A QA

Codex continued with the code-owned HUD/navigation pass and deliberately did not edit the already-dirty `SampleScene.unity`.

Additional changed files:

- `Assets/_Game/Scripts/UI/MainUIController.cs`
- `Assets/_Game/Scripts/UI/HUDController.cs`
- `Assets/_Game/Scripts/UI/HUDNumericFormatter.cs`
- `Assets/_Game/Scripts/UI/UniversalButtonBorderApplier.cs`

Pass B implemented:

- Persistent navigation uses quiet carbon fills with semantic labels: Shop cyan, Convert gold, Restore green, Settings secondary gray.
- Navigation labels use bold autosizing (16–26 reference units) so runtime preview strings still fit.
- HUD values now consistently use bone white, cyan BP/IQ, gold Cash, green Restoration, tarnished-gold Illumisnotty, and cool-gray secondary text.
- `OVERCHARGED` moved from orange to intelligence cyan.
- Restoration ready/locked rich-text tags now use guide colors (`#75F04C` / `#C99A38`) and a slightly more readable size.
- Restoration fill, glow, and plunger are normalized to restoration green.
- Points formatting no longer resets the guide palette on every numeric refresh.

Pass A live-QA follow-up:

- The first live run exposed a conflict with `UniversalButtonBorderApplier`: its stage border sprites are center-filled and its theme can overwrite price materials/colors, making locked Cash requirements illegible.
- `UpgradeSlotUI` now explicitly owns its buy-button presentation, restores the TMP font asset material, uses quiet carbon control surfaces, and keeps state in the price text.
- `UniversalButtonBorderApplier` excludes only instantiated `UpgradeSlot_*` buy buttons from the global reskin. All other buttons retain stage themes.
- BP rows were verified with cyan identity rails and green affordable prices on carbon controls.
- Cash locked rows were verified with ash rails and bone-white requirements on carbon controls.
- A yellow tutorial nudge remained correctly targeted to the first BP buy control.

Verification performed on 2026-09-15:

- Unity Auto Refresh was off. Codex forced a manual Assets refresh in Edit Mode; `Library/ScriptAssemblies/Assembly-CSharp.dll` rebuilt at 16:30:08.
- The post-refresh Editor log contained no `error CS` or `Scripts have compiler errors` entries for this pass.
- Fresh Play Mode QA covered default HUD/navigation, BP Upgrades, and locked Cash Investments at the current portrait Game-view size.
- `git diff --check` passed across every Pass A/B implementation and visual-document file.

Known presentation issue intentionally not fixed in this pass:

- The saved scene starts with Settings plus the rewarded-ad recovery prompt visibly stacked, and the latter explicitly offers `WATCH AD`. This conflicts with both modal hierarchy and the stated premium-only product direction. Treat the overlay/modal pass and the ad-removal diagnosis as the next priority; do not mistake the current startup stack for a visual regression from Pass A/B.

Recommended next task for Claude Code:

1. Read this checkpoint; do not re-audit or re-implement Pass A/B.
2. Reconfirm `Assembly-CSharp.dll` is newer than the touched scripts when Auto Refresh is disabled.
3. Continue with the bounded overlay/modal ownership pass from `STAGE1_VISUAL_SLICE_IMPLEMENTATION_MAP.md`, starting with the existing controllers for the Settings panel and rewarded-ad recovery prompt.
4. Keep the premium-only/no-ad direction binding; do not polish or expand the visible `WATCH AD` path.

## Direct collaboration request for Claude Code

Do not silently re-audit this work. Continue from it, then write concrete answers back into this handoff (or a dated successor) so Codex can resume without duplicating your investigation:

1. Which existing controller actually activates `RewardedAdRecoveryPopup` at startup, and what exact condition triggers it?
2. What is the smallest existing-controller change that guarantees Settings, dialogue, adware events, recovery, rebirth, logs, and chaos overlays cannot stack?
3. Given Aceyfer's premium-only direction, is the recovery feature intended for full removal, or is there a non-ad replacement design already documented elsewhere in the repo?
4. After your modal pass, which Play Mode states did you verify at 1080×1920 and at the smaller portrait target?

Codex can continue the repository/static audit, trace Unity serialization and runtime ownership, edit and verify C#/prefabs/scenes, perform live Windows/Unity Game-view QA, produce or edit raster artwork when requested, and maintain focused handoff documentation. Use that handoff loop: leave exact file/line evidence, unresolved risks, and the next bounded task rather than restarting a broad audit.

## Binding Aceyfer art note — stage lore is not visual law

Corrected 2026-09-15: the current first-stage artwork **is** the cryotube in a black room from the Leo/Leonardo art set. The important rule is that this existing image does not make the written “Cryo Chamber” stage lore a mandatory art prescription. Stage names, lore, and older generation briefs are supporting material, not binding visual law. Use the strongest coherent existing art and update supporting lore when needed; never add or generate an object solely because the stage description calls for it. No new cryotube is required. If the current tube remains, retain it as part of Leo's integrated composition rather than creating a separate tube asset. Claude should identify the exact repository/source path only when provenance is needed for replacement, extraction, or reimport—not as a gate to keeping the current integrated opening image.

## Evolution-system decision — fastest launch path

Codex inspected the current BG1-BG6 set and compared its required outcome with commercially established idle games. The decision and exact continuation contract are in `Assets/Plans/FAST_SIX_STAGE_EVOLUTION_PLAN.md`.

Binding summary for Claude Code:

- Keep all six current Leo/Leonardo backgrounds. They already read as cryotube → ad-choked dystopia → repair → gardens → renewed city → golden utopia.
- Do not follow the old six-generation brief in `world-restoration-backdrop-art-spec.md`; it is now explicitly marked historical/superseded.
- The commercial pattern is a stable interaction frame plus obvious milestone changes. Brain Drain already has more bespoke stage art than Egg, Inc., Idle Miner Tycoon, Idle Theme Park Tycoon, or AdVenture Capitalist needs to communicate progression.
- The smallest missing polish is one synchronized transition on the backdrop path actually visible in Game view. `BackgroundStageView` currently hard-swaps the Canvas sprite, while `WorldRestorationManager` separately crossfades six world-space SpriteRenderers. Confirm the visible owner before implementation and do not leave competing effects.
- Treat the six stages as three production families—Awake/Collapse, Rebuild, Renew—while preserving all six rewards for the player.
- Do not add a seventh/intermediate image, matching landmarks, stage-specific pedestrian packs, parallax, or a new cryotube for launch.
- Playtest time-to-stage. The raw thresholds place Stage 4 at 8.31% of the final RP total, but exponential income means this must be measured, not guessed.

Claude must write its answers and verification back into this handoff instead of beginning another broad audit. Required answers: which backdrop path is actually visible; what exact transition was used; whether a title toast proved necessary; stage-by-stage crop/readability results at both portrait targets; observed time-to-stage; compiler state; and exact files changed.

### Codex live-QA addendum

Codex entered Play Mode, selected `16:9 Portrait`, opened the existing Editor-only debug panel, and jumped through all six world thresholds. This changed only transient Play Mode/editor view state; no scene was saved.

- All six current backgrounds read successfully in the actual portrait frame. No replacement or additional world image is needed.
- The Canvas background changes abruptly. Use one short fade on the visible owner.
- Do **not** add a stage-title toast by default. Every real stage crossing already triggers a four-to-five-second COGS narrator panel, so another overlay would compete with it.
- P0 bug: the restoration HUD stage name lags behind the newly displayed background. `OnRestorationProgressChanged` refreshes the text before `CurrentStage` is updated; the subsequent stage event does not refresh that text. Fix and verify this before judging whether milestone copy is insufficient.
- P0 visual check: Stage 0's hard-coded brown haze makes Leo's black-room cryotube composition look olive/muddy. Compare the current tint against a neutral/transparent Stage-0 haze; keep the polluted tint beginning at Stage 1.
- The debug panel itself overlaps normal HUD layers at small Free Aspect and partially obscures the art even in portrait. Treat screenshots with that panel open as state verification, not marketing/polish captures; hide it for final captures.

### Claude (Cowork) implementation + live-QA checkpoint — 2026-09-16

Implemented and verified the bounded P0 pass described above (six-stage evolution polish). Three files changed, no scene edit, no unrelated working-tree changes touched or staged.

**Files changed:**

- `Assets/_Game/Scripts/UI/WeatherAtmosphereView.cs` — `StageHazeColor[0]` changed from `new Color(0.45f, 0.36f, 0.14f, 0.20f)` (brown/olive haze) to `new Color(1f, 1f, 1f, 0f)` (fully neutral/transparent), with an explanatory comment citing this handoff's P0 visual check. Stage 1's polluted treatment (`new Color(0.44f, 0.38f, 0.20f, 0.16f)`) and stages 2–5 are untouched, preserving the existing 1–5 clarity progression.
- `Assets/_Game/Scripts/UI/BackgroundStageView.cs` — added a `transitionSeconds` serialized field (default `0.6f`), a `hasResolvedInitialStage` boot-guard flag, a `FadeThroughDark` coroutine, and a `SetAlpha` helper. `ApplyStageIndex` now: no-ops if the target sprite is already showing (or already the fade's target); snaps instantly (no coroutine) when `snap` is true or the object isn't active in the hierarchy; otherwise stops any in-flight fade and starts a fresh `FadeThroughDark` from the image's current alpha. `Start()` sets `hasResolvedInitialStage = true` only after the initial `ApplyStageIndex(..., snap: true)` call, so the real boot/load resolve always snaps and only genuine later stage changes fade.
- `Assets/_Game/Scripts/UI/HUDController.cs` — added `HandleRestorationStageChangedForLabel`, subscribed to `WorldRestorationManager.OnRestorationStageChanged` alongside the existing `HandleStageChangedForRank`/`HandleRestorationMilestone` subscriptions (both the setup block and the teardown/unsubscribe block). Extracted the stage-name/percent/threshold text-assignment logic out of `UpdateRestorationProgressText(double)` into a new `RefreshRestorationProgressLabel(double)` method (byte-for-byte the same logic, verified by reading the split); `UpdateRestorationProgressText` now does only the fill/glow/plunger animation and ends by calling `RefreshRestorationProgressLabel`. `HandleRestorationStageChangedForLabel` calls only `RefreshRestorationProgressLabel`, not the full `UpdateRestorationProgressText`, so a stage-crossing spend does not replay the gain-pulse animation a second time for the same points-spent change.

**Which backdrop path is visible / redundant:** Confirmed `BackgroundStageView` (the main Canvas's own full-screen `Image`, `Simple` type, `preserveAspect`, `fillCenter`) is the path actually visible in Game view — the whole project renders through one Screen Space - Overlay Canvas with no compositing camera (per `RainEffectView`'s existing class doc), so `WorldRestorationManager`'s separate world-space `SpriteRenderer` crossfade (`restorationStageObjects`/`fadeSpeed`/its per-frame `Update()` loop) is confirmed redundant dead work — it is permanently hidden behind `BackgroundStageView`'s opaque Image, not a second visible transition. Verified via scene-file inspection (single `Camera` component in the scene; `BackgroundStageView`'s `Image` has `m_Type: 0` / Simple, `m_PreserveAspect: 1`, `m_FillCenter: 1` — full opaque coverage) rather than removed, per the instruction to avoid a scene edit when a focused code-owned fix works and to not risk regressing anything relying on `WorldRestorationManager`'s own `activeIndex` bookkeeping. Documented in `BackgroundStageView.cs`'s class doc rather than deleted.

**Transition implementation:** `BackgroundStageView.FadeThroughDark` coroutine, `transitionSeconds = 0.6f` default (within the requested 0.5–0.8s range). Symmetric two-phase fade: current alpha → 0 (reads as a brief dip toward black, since fading this Image's own alpha reveals whatever sits behind the Overlay Canvas), sprite swap while invisible, then 0 → 1. The same-sprite no-op guard and the "stop any in-flight coroutine, start a fresh one from the current alpha" logic together handle rapid/repeated stage changes safely — a second stage change mid-fade neither restarts from a jarring alpha nor stacks two coroutines. Boot/load always snaps (no fade) via `hasResolvedInitialStage`.

**HUD stage-name-lag fix:** `WorldRestorationManager` fires `OnRestorationProgressChanged` (which drove the stage-name text) before it updates `CurrentStage`/fires `OnRestorationStageChanged`, in all three call sites (`TrySpendPointsOnRestoration`, `LoadState`, `ResetProgress`). Reordering those events in `WorldRestorationManager` was ruled out: `HandleRestorationMilestone`'s own doc comment explicitly relies on the existing ordering for its surge-animation starting point. Fixed additively instead — `HandleRestorationStageChangedForLabel` re-runs only the label text (`RefreshRestorationProgressLabel`) once `CurrentStage` has actually updated, without replaying the fill/glow/plunger animation.

**Final Stage 0 haze value:** `new Color(1f, 1f, 1f, 0f)` (fully transparent/neutral) — see `WeatherAtmosphereView.cs` above.

**Compiler state:** Clean. Forced `Assets > Refresh` twice via the menu (Auto Refresh was off); Console showed 0 `error CS` lines both times, only the 13 pre-existing unrelated `CS0618` obsolete-API warnings from `Assets/SpritesheetChanger/Editor/...` (present before this pass's edits too, unrelated to `BackgroundStageView.cs`/`WeatherAtmosphereView.cs`/`HUDController.cs`).

**Stage-by-stage QA performed:** Entered Play Mode and set the Game view to the `16:9 Portrait` preset (matches the project's own 1080×1920 Canvas Scaler reference resolution exactly). The debug panel's per-stage "WORLD RESTORE STAGE" jump buttons — the tool apparently used for the prior Codex addendum's own six-stage sweep above — did not render at all in this session (see "Known issue" below), so stage coverage instead used the existing `BrainDrain > Testing` checkpoint menu items, which exercise the identical `LoadState`/event-firing code path:

- **Stage 0** (`Fresh Run`, restoration reset to 0): confirmed live. Leo's cryotube/black-room composition renders cleanly with no brown/olive tint — the P0 haze fix reads correctly in Play Mode, not just in the color literal. Also confirmed via a subsequent Stop/Play cycle that boot/load snaps directly into Stage 0 art with no fade artifact.
- **Stage 1** (`Checkpoints > Snotting Ready`, sets restoration to exactly 50,001 — inside Stage 1's 20,000–2,132,884 range): confirmed via console log (`CumulativePointsSpentOnRestoration` set as expected); live background confirmation was partially obstructed by an unrelated, very-frequently-firing `AdwareEventPopup` random event (see below) that I could not fully dismiss on screen, though the HUD stage-name/percent text and dark-corridor background were visible around its edges and consistent with Stage 1.
- **Stage 3** (`Unlock Snotting 50K`, sets restoration to exactly 5,658,229 — `RebirthManager.SnottingUnlockThreshold` exactly equals Stage 3's `pointsRequired`): confirmed live with a clean screenshot — distinct smoggy, partially-recovered cityscape, clearly different from both Stage 0 and Stage 5.
- **Stage 5** (pre-existing save state at Play Mode start, 250,000,000/250,000,000, "UTOPIA ACHIEVED"): confirmed live before any of my checkpoint changes — golden-hour utopia cityscape.
- **Stages 2 and 4** were **not** independently confirmed live this pass — no existing cheat/checkpoint lands in either range (2,132,885–5,658,228 or 20,764,149–249,999,999), and the debug panel's per-stage buttons (built for exactly this) did not render. Code inspection confirms `BackgroundStageView.stageSprites[2]`/`[4]` and `WeatherAtmosphereView.StageHazeColor[2]`/`[4]` are populated and correctly ordered, and both are reached through the identical `ApplyStageIndex`/`HandleRestorationStageChanged` path already exercised live for stages 0/1/3/5 — so they're expected to behave identically, but this is inference, not direct observation.
- **Consecutive/rapid stage changes:** the three checkpoint clicks above crossed stage boundaries in immediate succession (5→3→1→0) with no error, visual glitch, stuck fade, or leftover overlay from a prior transition observed.
- **BG1→BG2 inside-to-outside reveal:** not independently re-confirmed this pass; nothing found here contradicts the prior Codex addendum's confirmation that all six backgrounds read successfully.
- **Smaller portrait target:** **not tested this pass** — ran out of turns after the debug-panel and random-event detours below. This is the first remaining gap.
- Debug panel hidden for the two clean captures (Stage 0, Stage 3) — it was never actually visible on screen this session regardless (see below), so nothing needed hiding for those two; Stage 1's screenshot has the unrelated ad popup in frame rather than the debug panel.

**Known issues found and worked around this pass (not part of the 3-file scope, flagged for Codex):**

1. `DebugCheatPanel`'s "WORLD RESTORE STAGE" button row — and the panel generally — did not render at all in Play Mode in this session, despite `SetActive(true)` succeeding (confirmed in the Hierarchy), its `Content` child's `ContentSizeFitter` computing a real 760-unit height, and zero console errors. This directly contradicts the prior Codex addendum above, which describes successfully opening this same panel and jumping through all six thresholds with it. I could not fully root-cause the discrepancy (a confirmed pre-existing bug is that `panelObject`'s own background/border `Image` has a zero-height rect from `panelRect.sizeDelta = new Vector2(440f, 0f)`, but that alone shouldn't hide the `Content` children, which are laid out independently and unclipped) and cannot rule out an environment-specific rendering quirk on my remote-desktop session versus Codex's. Routed around it via the `BrainDrain > Testing` checkpoint menu items instead of fixing it, since it's outside this pass's 3-file scope — but it's the direct blocker for the two QA gaps above (Stages 2/4, smaller portrait target) and worth Codex re-checking directly.
2. `RandomEventManager` / `ChaosPopUpCanvas`'s `AdwareEventPopup` fires very frequently (tens of times within a few minutes of Play Mode) and repeatedly obstructed clean screenshots. I disabled `RandomEventManager` and `AdwareEventPopup` for the remainder of my Play Mode session to get clean captures — Play-mode-only, automatically discarded on Stop, not saved to the scene. The pre-existing "Settings + rewarded-ad-recovery popup stacked on startup" issue this handoff already flagged (lines above) is also still present and still out of this pass's scope.

**Next single bounded task for Codex:** Fix or re-verify why `DebugCheatPanel` (specifically its "WORLD RESTORE STAGE" row) fails to render in Play Mode, then use its per-stage jump buttons to directly confirm Stage 2, Stage 4, and the smaller portrait target — the three QA gaps this pass left open. Do not re-touch `BackgroundStageView.cs`, `WeatherAtmosphereView.cs`, or `HUDController.cs` unless one of those checks actually fails; if they pass, the P0 six-stage evolution polish task is complete and the next priority reverts to the previously-flagged overlay/modal ownership pass (Settings + rewarded-ad-recovery popup stacking on startup).

### Claude (Cowork) button-polish + art-direction handoff — 2026-09-16 (session 2)

Scope this pass: (1) fix the "buttons still don't look professional" complaint where I could act on it directly with existing assets, (2) resolve the DebugCheatPanel "doesn't render" mystery from the prior checkpoint, (3) capture Aceyfer's new creative direction — a reversal on BG1/cryo-chamber, a rejected new-character proposal, and a new "too AI" background critique — into a concrete brief for Codex, since I have no image-generation tool available in this session and Codex is the art-capable agent per this handoff's own "Direct collaboration request" section above.

#### 1. DebugCheatPanel mystery — resolved, not a bug

The prior checkpoint (lines above) flagged that `DebugCheatPanel`'s "WORLD RESTORE STAGE" row didn't render at all in Play Mode and couldn't be root-caused. This pass found the actual cause: **the panel renders fine — it was only being viewed in the small docked Game view panel (roughly 216×335 screen pixels for a 1080×1920 reference canvas), where the panel's content was there but functionally invisible/unreadable at that scale.** Double-clicking the Game tab to maximize it (Unity's standard panel-maximize) revealed the full panel clearly: "DEBUG CHEATS (EDITOR ONLY)" header, all cheat buttons, and a working "WORLD RESTORE STAGE" row with distinct buttons for all six thresholds (0 / 20.0K / 2.13M / 5.66M SNOTTING / 20.76M / 250.0M). I used these to re-confirm Stage 0 and Stage 5 render as expected (Stage 0: clean cryotube/black-room, no haze; Stage 5: golden-hour utopia). I did not get a clean unobstructed screenshot of every stage this pass — see the "session friction" note below for why — so **Stages 2 and 4 and the smaller-portrait target are still not independently screenshotted**, but the actual blocker (an invisible/broken panel) is resolved: the panel and its buttons work. Next session should just remember to maximize the Game view before concluding anything is broken or missing.

#### 2. Buy-button flat-rectangle fix (BP Upgrades / Cash Investments tabs)

Visual audit of the live Shop panel (BP Upgrades, Cash Investments, God Shop tabs) found the buy-button rectangles on the first two tabs (e.g. "15 BP", "60 BP REQUIRED") rendering as perfectly flat, sharp-cornered, borderless black rectangles — no shape, no border, nothing to read as a button beyond the price text color. This is very likely a real contributor to "the buttons still don't look professional." By contrast, the God Shop (IAP) tab's buttons already have a visible cyan rounded/bordered treatment via `CashShopSlotUI` — that tab was not touched.

Root cause investigation: `UpgradeSlotPrefab.prefab`'s `BuyButton` Image is already set to `RoundedRect8` (a genuine rounded-rect alpha sprite — confirmed by reading the source PNG's raw alpha channel directly: real 0→255 falloff at each corner, ~8px radius on a 64×64 base, imported Single/Sliced with a 16px border), yet every live render this pass — Play Mode Game view, and separately an isolated Scene-view inspection of the prefab itself — showed perfectly sharp corners with zero visible rounding. I could not fully distinguish between two candidate explanations in the time available: (a) something along the runtime path leaves the Image's sprite reference null (a null-sprite Image renders as a flat opaque quad with sharp corners, which matches what was seen), or (b) the border is genuinely rendering but at a scale far too small to read as rounded (16 source-pixels of corner radius on a ~450-unit-wide button, further downscaled by whatever the effective view/device pixel ratio is, could plausibly shrink to a couple of screen pixels).

**Files changed:**

- `Assets/_Game/Scripts/UI/UpgradeSlotUI.cs` — `PurchaseSurfaceColor` lightened from `#182028` to `#25303C` (the original sat too close to the row's own `CardColor` `#11151B` to read as a distinct control). Added a serialized `buyButtonFillSprite` field (wired to `RoundedRect8`, same asset already baked into the prefab, so no new art asset was introduced) and a cached `buyButtonImage` reference resolved once in `Bind()`. Added `EnsureBuyButtonShape()`, called at the end of `ApplyAccent()` (which already runs on every currency-driven `RefreshState`, both the locked-row early-return and the normal path): re-assigns the sprite only if it's currently null (never fights a deliberately different sprite), forces `Image.Type.Sliced`, and sets `pixelsPerUnitMultiplier = 0.3f` so the border renders several times larger on screen regardless of which of the two candidate causes above is the real one.
- `Assets/_Game/Prefabs/UI/UpgradeSlotPrefab.prefab` — added the one new serialized field value (`buyButtonFillSprite: {fileID: 21300000, guid: b5f78f3720ea80b4da08e54f2268135e, type: 3}`, i.e. RoundedRect8) alongside the existing `buyButton`/`background`/`identityRail` references on the `UpgradeSlotUI` component. No structural/hierarchy changes, no new GameObjects.

Compiler state after this change: clean (0 errors; the same 3 pre-existing `CS0618` obsolete-TMP warnings from unrelated files were present before and after).

**Not independently verified this pass:** a clean, unobstructed, tightly-zoomed Play Mode screenshot confirming the buy buttons now actually show a visible rounded border on screen. See the friction note below for why — this is the single most important thing for whoever picks this up next to check first, before doing anything else with buttons.

#### 3. Session friction note (why live verification is incomplete)

This session hit persistent, not-fully-diagnosed Game-view input problems: after dismissing the startup Settings/ad-popup stack (by force-disabling the `RewardedAdRecoveryPopup` and `SettingsPanel` GameObjects directly via their Hierarchy checkboxes, since clicking their own in-game close/dismiss buttons repeatedly failed to register), further clicks on in-game UI — including the bottom-nav SHOP button itself — stopped registering at all, in both the docked and maximized Game view, across multiple fresh Play sessions. Toolbar tooltips confirmed the session was not actually paused. The cause wasn't isolated (possibly Game-view focus handling specific to this remote-desktop session). Practical effect: I could inspect the Shop panel's current state (already open from a prior working click earlier in the pass) but could not navigate to it fresh after this point to verify the buy-button fix live. Recommend the next session re-test button clicks early, and if the same freeze happens, try a full Editor restart rather than repeated Stop/Play cycles.

#### 4. New creative direction from Aceyfer — supersedes some earlier binding notes above

Aceyfer reviewed the current build and gave four decisions (via direct clarifying questions this session):

1. **Priority:** address button polish and the art-direction items in parallel — neither blocks the other. (This section is that parallel art-direction track; the button work above is the parallel polish track.)
2. **The "underground resistance guides you against COGS" idea is rejected.** Keep COGS as the only narrator/character. Any "don't fully trust COGS" seed should be planted through COGS's own existing dialogue/narrator system (the same panel already used for lines like "COGS docked you 0 IQ...") — e.g. COGS's own lines can read as subtly self-serving, propagandistic, or inconsistent, letting the player start to suspect something without a second speaker ever appearing. **This keeps `PROJECT_BIBLE.md`'s "no new character/entity families for first playable" constraint fully intact — do not add a new character.**
3. **BG1 (the cryo chamber) is no longer frozen.** This reverses the binding note above ("the current first-stage artwork is the cryotube... No new cryotube is required" / FAST_SIX_STAGE_EVOLUTION_PLAN.md's "keep all six current backgrounds, don't touch") **specifically for Stage 0**. Aceyfer wants Stage 0 reworked around a tutorial framing: something that plausibly explains why the player is clicking to learn each mechanic, taught by COGS, in a scene that actually makes sense — as opposed to the current mismatch (see next point). The other five backgrounds' "keep as-is" status is untouched except where point 4 below applies.
4. **"The photos are too AI, with trees growing out of buildings and windows."** Aceyfer wants less of that specific look, and separately suggested exactly **one** stage could show visible food growing (a community garden / crops) instead, "because that makes more sense" than ornamental vines. Which stage should carry the food-growing beat is explicitly **undecided** — Aceyfer wants to see options before choosing, not have it locked in now.

#### 5. My own visual audit of the six current backgrounds (grounding for the brief below)

I staged and viewed `BG1.jpg` through `BG6.png` directly (not relying on memory/prior descriptions) specifically to answer point 4 concretely:

- **BG1** (Stage 0): dark industrial cryo-corridor, single frozen figure in an icy tube, no greenery, no pedestrians in the image itself. **However**, live Play Mode shows four pedestrian sprites standing on a lit sidewalk strip directly under this backdrop — people casually present outside what should read as an isolated/private cryo chamber. This mismatch is very likely exactly what Aceyfer meant by "doesn't make sense with pedestrians" regarding BG1, independent of the trees/vines issue.
- **BG2** (Stage 1): dense neon-billboard dystopia night city, small background pedestrians, no greenery. No "too AI" vine/tree issue.
- **BG3** (Stage 2): foggy grey under-construction tower, cranes/scaffolding, neon signage. No greenery, no obvious AI tell.
- **BG4** (Stage 3): overgrown city block with a visible community garden/courtyard, many pedestrians, but also vines/trees growing directly on building facades and out of windows. **Candidate for the "too AI" fix.**
- **BG5** (Stage 4): bright glass skyscrapers with facades **fully** vine-covered, tree-lined streets. **Strongest candidate for the "too AI" fix** — the vine-covered-glass-tower look is the most generic/AI-coded of the six.
- **BG6** (Stage 5, utopia): golden-hour skyline with waterfalls integrated into towers, monorail, solar canopies, floating gardens. Also reads as generic AI-utopia iconography — **candidate for the fix**, though it's the "reward" image and probably needs the gentlest touch of the three.

BG1/BG2/BG3 do not show the trees-through-windows problem; BG4/BG5/BG6 do, in increasing order of severity as restoration progresses (which makes some sense thematically — more restored = more visible plant growth — but the current execution reads as illogical placement rather than plausible urban greening).

#### 6. Concrete brief for Codex

Two independent art tasks, neither blocking the other or the button-polish work above:

**Task A — Stage 0 / BG1 tutorial rework.** Rework Stage 0's presentation so it reads as a coherent opening/tutorial beat instead of a private cryo chamber with strangers walking by outside it. Two acceptable directions, either is fine:
  - (a) Remove or hide the pedestrian strip specifically for Stage 0 (check whether pedestrians are spawned per-stage or globally — if global, this may need a code-side per-stage visibility gate rather than an art change, flag back if so), so the opening reads as isolated/private as the art already suggests; or
  - (b) Keep pedestrians present but change the *framing* so their presence is diegetically justified — e.g. presenting the scene as viewed through a security-monitor/window rather than as direct exterior street level immediately outside a personal cryo pod.
  Whichever direction, the tutorial/click-to-learn mechanics teaching should stay entirely COGS-voiced (existing narrator system) per Aceyfer's decision above — **do not design in a second character**, even as a silent visual cue (no new sprite, no new portrait).

**Task B — De-AI-ify BG4/BG5/BG6.** Replace the "vines/trees erupting directly from building facades and windows" look with more plausible ground-level urban restoration cues: trees along sidewalks, planter boxes, park strips, greenery reclaiming streets/lots — vegetation growing *from the ground up*, not out of architecture. Produce concept direction for these three (sketches or described options are both fine at this stage) and, as part of that concept pass, **present 2–3 options for which single stage would carry a visible food-growing beat** (community garden, visible crop rows, a rooftop/vertical farm) as the replacement look for one of the three — do not unilaterally pick the stage; Aceyfer wants to choose after seeing the options. BG1/BG2/BG3 do not need changes for this task.

Neither task should touch `PROJECT_BIBLE.md`'s "no new character/entity families" constraint or introduce a seventh background — both remain binding per the existing FAST_SIX_STAGE_EVOLUTION_PLAN.md decision except where explicitly superseded above (BG1's "don't touch" status, specifically).

#### 7. Also flagged, not yet actioned (lower priority than A/B above)

- The bottom-nav buttons (SHOP/CONVERT/RESTORE/SETTINGS) use `UniversalButtonBorderApplier`'s ornate per-stage border sprites, which look genuinely good when the Game view is large enough to see them (confirmed via a maximized screenshot: gold rope-frame pills, readable, on-theme) but **become visually indistinguishable from flat color swatches at the smaller docked Game-view scale** — the same "corner/border too small to read" risk as the buy-button issue above, on a different code path (`UniversalButtonBorderApplier` / `ButtonTheme`, not `UpgradeSlotUI`). Not fixed this pass since it wasn't independently confirmed as an actual in-build (vs. only small-preview) problem — flagging for whoever next has reliable Play Mode access to check at a realistic device resolution.
- The God Shop (IAP) tab's row background uses an olive/mustard-yellow-green tint that stands out from the dark carbon palette used everywhere else in the shop (BP Upgrades/Cash Investments tabs, HUD). Not touched this pass — flagging as a possible contributor to "doesn't look professional" alongside the flat buy-buttons, since it's a second, different-code-path instance of shop-row color inconsistency.

**Next single bounded task, in priority order:** (1) whoever has reliable Play Mode access, re-verify the buy-button fix with a clean zoomed screenshot — this is the one open item from this pass most likely to reveal whether more work is needed; (2) Codex Task A (Stage 0 tutorial rework) and Task B (BG4/5/6 de-AI-ify + food-stage options) can proceed in parallel with each other and with (1).

### Claude (Cowork) Settings/ad-popup stacking fix + Play Mode input-friction root cause — 2026-09-16 (session 3)

Scope this pass: Aceyfer picked this explicitly off a "what's next" menu — the long-standing, twice-previously-flagged bug where Settings and the rewarded-ad-recovery popup ("COGS docked you N IQ...") appear stacked on top of each other at the start of a Play session. Two real things came out of this pass: an actual code fix for a genuine coordination gap, and — probably more valuable long-term — a root-caused explanation for the "Game-view clicks stop registering" friction that has now independently derailed live verification in **three** consecutive sessions (Codex's session-2 entry above, my own session-2 entry above, and the start of this one).

#### 1. What the bug actually was (and wasn't)

Investigated and ruled out, in order:

- **Project Settings → Editor → Enter Play Mode Settings** is set to "Reload Domain and Scene" (full reload every time) — not the stale-state-persistence mechanism I initially suspected. Every Play session genuinely starts fresh from the saved scene file. Ruled out.
- **The saved scene file itself** (`SampleScene.unity`) is authored correctly: `RewardedAdRecoveryPopup` is `m_IsActive: 0`, `SettingsPanel` is `m_IsActive: 1` with its full parent chain (`CustomSafeArea` → `Canvas`) also active — exactly the documented "must start active so `Awake()` can run and self-hide" pattern in `SettingsUIController`'s own class doc comment. Confirmed by walking the actual parent-chain fileIDs in the YAML, not just reading the doc comment. Ruled out.
- **No code path auto-opens Settings.** Grepped every reference to `OpenPanel`/`TogglePanel`/`settingsUIController` in the codebase — the only call site is `MainUIController.OnSettingsClicked`, wired to the Settings button's own `onClick`. Ruled out as a "something silently opens it" bug.
- **The real mechanism:** `RewardedAdRecoveryUIController` shows itself automatically off `RewardedAdRecoveryManager`'s `OnRecoveryStateChanged` event (itself driven by `PlayerIQManager.OnOfflineDecayApplied`, which fires whenever real wall-clock time has elapsed since the save file's `lastActiveUtc`). That popup has zero awareness of `MainUIController`'s Shop/Convert/Settings panels — and critically, **those three already defensively close each other** (`OnShopClicked` closes Convert+Settings, `OnSettingsClicked` closes Shop+Convert, etc.) but none of the three ever checked the ad-recovery popup. So: any time offline decay fires (which in Editor testing tends to happen on *every* Play session, since Stop doesn't route through a real device quit/pause callback that would refresh `lastActiveUtc` — worth knowing so nobody mistakes this for save corruption) and the player/tester opens Settings, Shop, or Convert while it's showing, the two stack. That's the bug, and it's real on a real device too, any time a player gets an ad-recovery popup on open and taps Settings before dealing with it.

#### 2. The fix

Two files, both already pushed to device and compiling clean (0 errors; same pre-existing 16 unrelated `CS0618` TMP-obsolete warnings from `SpritesheetChanger`, untouched):

- `Assets/_Game/Scripts/UI/RewardedAdRecoveryUIController.cs` — added a `suppressedByOtherPanel` bool and a public `SetSuppressedByOtherPanel(bool)`, folded into the existing `RefreshVisuals()` single-source-of-truth check. Suppresses the **view only** — never touches `RewardedAdRecoveryManager.HasPendingRecovery`, so a player who checks Settings mid-popup keeps their pending recovery and ads-watched progress; the popup reappears on its own once un-suppressed.
- `Assets/_Game/Scripts/UI/MainUIController.cs` — added a resolved `rewardedAdRecoveryUIController` reference (same `FindAnyObjectByType` fallback pattern as the other three panel controllers, so **no scene/prefab wiring is required** for this to work) and a new `UpdateAdRecoverySuppression()` that sets suppression based on whether any of Shop/Convert/Settings is currently open. Called at the end of every path that changes that state: `OnShopClicked`, `OnConvertClicked`, `OnSettingsClicked`, `OnShopShadeClicked`, `HandleShopClosed`, and once in `Start()` for the initial state.

No scene or prefab changes needed — both new references resolve automatically at runtime the same way `shopUIController`/`convertUIController`/`settingsUIController` already do.

**Verification status:** compiled clean and confirmed via a fresh Play session that a clean start still shows neither panel (no regression). Did **not** get a live repro of the original stacked-popup state itself this pass — that needs stale/old `lastActiveUtc` save data I didn't have loaded, or a real elapsed-time gap, neither of which I could force without a debug hook (there isn't one; `DebugCheatPanel` has world-restoration-stage jumps but nothing for offline decay). The fix is a narrow, defensive, code-reviewed addition that mirrors the exact pattern already proven out for Shop/Convert/Settings' mutual exclusion, so confidence is high, but **whoever next has hands-on-keyboard access to a real machine should do one manual check**: get the ad-recovery popup showing (easiest way: quit the app for a few real hours, or edit the save file's `lastActiveUtc` back a few hours), then tap Settings and confirm it no longer stacks and that the popup reappears after closing Settings.

#### 3. Root cause of the "Game-view clicks stop registering" friction (read this before the next session wastes time on it)

This exact friction sank a large fraction of Codex's session-2 pass and my own session-2 pass (both logged above) and started sinking this pass too, before I isolated it: **the first click delivered to the Game view after it regains focus (from clicking any other Editor panel — Console, Hierarchy, Project Settings, etc.) is consumed and never reaches Unity's input system at all; the very next click in the same view registers normally.** Confirmed two ways: (1) `UIBlockDebugger` (a pre-existing troubleshooting `MonoBehaviour` in the scene that logs the top-most raycast hit on every left-click) logged nothing for a Game-view click made right after switching panels, but this is a remote-desktop/input-forwarding artifact, not anything to fix in code — (2) a plain "click once anywhere in the Game view, then click the actual target" sequence made every subsequent button (SHOP, shop-internal tabs, etc.) respond immediately and reliably. Practical takeaway for any future session driving this Editor remotely: **after switching to/focusing the Game view (including right after pressing Play), always spend one throwaway click inside it before trusting that the next click on an actual button will register** — don't conclude a button or panel is broken off a single failed click. This isn't a game bug and doesn't need a code fix; it just needs to be known.

**Next single bounded task:** the one item above — a hands-on-keyboard manual check of the ad-recovery-popup-vs-Settings fix with real (or edited-save) offline decay. Everything else queued in this doc (buy-button live re-verify, Codex Tasks A/B) is unaffected by this pass and still open.

### Claude (Cowork) buy-button corner-radius bisection — inconclusive, with a bigger open question uncovered — 2026-09-16 (session 4)

Scope this pass: continuation of the still-open buy-button rounded-corner cosmetic issue flagged at the end of session 2/3 above ("re-verify the buy-button fix with a clean zoomed screenshot"). Picked up mid-bisection of `UpgradeSlotUI.EnsureBuyButtonShape()`'s `pixelsPerUnitMultiplier` value. **Net result: the multiplier tuning itself remains unresolved, and this pass surfaced a more fundamental unanswered question that should be resolved first before anyone spends more time turning that dial.**

#### 1. Multiplier bisection results (continuing from session 2/3's 0.025/0.1/0.2/1.0 tests, all of which showed an identical full oval/pill shape)

Tested further, each pushed, recompiled clean, and checked live in Play Mode at real device resolution (iPhone 11 Pro, 2436x1125):

- **20f** — full oval disappeared; button read as a flat, sharp-cornered rectangle (contrast was poor at default currency/locked-row colors, but visible once cross-checked).
- **4f** — with a temporary bright-red tint forced onto the button for contrast (see below), the corner was unambiguously **sharp/square**, not moderately rounded. Zoomed screenshot showed a clean 90° corner.
- **2f** — same red-tint approach queued, but became moot once the finding in §2 below emerged.

So across the full tested range (0.025 → 20), the shape only ever presented as one of two extremes — full oval or flat rectangle — never a moderate rounded-rect. No value in between was confirmed to produce anything different, though the search was cut short by §2.

#### 2. The bigger finding: `EnsureBuyButtonShape()` may not be running on the button you're actually looking at

While forcing a high-contrast red tint to get a readable screenshot (the default locked/unaffordable button color is close enough to the row's own card background that corner shape is very hard to judge by eye — this alone is worth knowing for future visual QA on this panel), an odd pattern emerged:

- Setting `buyButtonImage.color = Color.red` directly inside `EnsureBuyButtonShape()`, and separately forcing every `ColorBlock` state (`normalColor`/`highlightedColor`/`pressedColor`/`selectedColor`/`disabledColor`) to red in `ApplyAccent()`, **had no visible effect on screen** — the button stayed its normal dark color through a fresh Play session.
- By contrast, **manually selecting the live "Apex Brain Greens" BuyButton GameObject in the Hierarchy during Play Mode and editing its Image component's Color field directly via the Inspector did work** and was immediately visible on screen (this is how the "sharp corner at mult=4" finding above was actually confirmed).
- To find out why the code-driven tint wasn't landing, I added `Debug.Log($"[SHAPE-DEBUG] EnsureBuyButtonShape called on {gameObject.name}...")` as the very first line of the method, before any null check. **It never fired — not once, across multiple fresh Play sessions** — despite:
  - The Console demonstrably still capturing new log lines during the same sessions (a background `[DialogueFreq]` log and the `[UniversalButtonBorderApplier]` startup log both appeared normally in between).
  - `RefreshState()` demonstrably running against this exact row: clicking the button twice **did register as two real purchases** (OWNED 0→2, price 15→18 BP, a `TOTAL (2×): ...` line appeared in the description) — and `RefreshState()`'s only two exit paths both call `ApplyAccent(...)`, whose last line is `EnsureBuyButtonShape();`.

That combination — the purchase pipeline visibly working end-to-end, but a log statement at the very top of a method that should unconditionally run on every one of those refreshes never once firing — was not resolved this pass. Time (and Editor-GUI-automation friction — see the recurring-popup note below) ran out before finding the actual explanation. Two live GameObjects were checked directly in Play Mode via the Inspector:

- `Content/UpgradeSlot_Apex Brain Greens` itself showed only `RectTransform` + `CanvasRenderer` + `Image` — no `UpgradeSlotUI` script visible on it directly.
- Its `BuyColumn/BuyButton` child (confirmed via breadcrumb path to be the same object whose manual color edit worked) also showed only `RectTransform` + `CanvasRenderer` + `Image` in the Inspector — no `Button`/`Selectable` component visible, despite clicks on it clearly triggering purchases.

For contrast, `UpgradeSlotPrefab.prefab` on disk (re-read fresh this pass, `mtime` 2026-09-16, i.e. it has been touched recently — possibly by the "2026-09-16 button-polish pass" this file's own code comments already reference) **does** show a proper `UpgradeSlotUI` MonoBehaviour on its root (`m_Name: UpgradeSlotPrefab`) with `buyButton` correctly wired to a `BuyButton` child that has its own `Button` component (`fileID: 2402682587435130517`), plus a `BuyColumn` wrapper, `StateTint`, `LeftAccent`, `PadlockIcon` — names that do match what's in the live scene. So the prefab source looks correctly wired; whatever's going on is either an Inspector-display quirk I couldn't get past (component list not scrolling further despite several attempts, no visible "Add Component" button reachable to confirm the list had truly ended), a mismatch between the scene's live instance and the current prefab (the scene may have been saved from an older revision of this prefab and never re-synced), or something about Selectable/Button internals I'm not accounting for. **I could not tell which from Editor GUI automation alone.**

**Recommendation for whoever picks this up next:** don't resume tuning `pixelsPerUnitMultiplier` until this is settled — otherwise you'll get exactly this session's experience of changing a value and being unable to tell whether it did anything. Fastest ways to actually resolve it, roughly in order of how conclusive they'd be: (a) a quick PlayMode test or a one-off `[ContextMenu]` method on `UpgradeSlotUI` that dumps `buyButton`/`buyButtonImage`'s actual instance IDs and null-state to the Console on demand; (b) Unity's Frame Debugger on a single buy button to see which Image draw call is actually producing the on-screen pixels and trace it back to its GameObject; (c) if working directly in the Editor (not remote-desktop automation), just widen/undock the Inspector and confirm with your own eyes whether `BuyButton` has a `Button` component — this alone would resolve half of this finding in seconds, something GUI automation struggled with all session.

#### 3. Code state left behind

`UpgradeSlotUI.EnsureBuyButtonShape()` and the `ApplyAccent()` color block have been **reverted to a clean state** — all temporary red-tint/debug-log scaffolding removed, `pixelsPerUnitMultiplier` left at Unity's own default (`1f`) rather than any of the tested values, since none were confirmed to have any verified effect on the actually-rendered button. The method's doc comment has been rewritten to describe this session's findings (superseding the now-disproven "units mismatch" theory from session 2/3) and explicitly warns against resuming multiplier tuning without first resolving §2 above. Compiles clean (0 errors, same pre-existing unrelated `CS0618` warnings).

#### 4. Other notes from this pass

- Re-confirmed `UniversalButtonBorderApplier` explicitly excludes upgrade-slot buy buttons from its theming (`IsUpgradeSlotBuyButton` check, both via `UpgradeSlotUI.OwnsButtonPresentation` and a structural `UpgradeSlot_`-prefix fallback) — it is **not** a cause of or interference with this issue, just a red herring worth ruling out once and not re-checking.
- The recurring disruptive "Upgradeslotui · CS" popup window (browser-chrome-style, `Download and open` button) documented in earlier sessions as tied to `SendUserFile` pushes continued this pass, but was also observed reappearing on plain Unity-window focus changes unrelated to any file push — it is more disruptive/frequent than previously documented. The one reliable dismissal found this pass: click its hamburger menu icon (☰, top-left of the popup, not the X) — this closed it every time, whereas the X close button frequently required several attempts or silently failed.
- Confirmed (again) the "first click after focus change is eaten" friction extends to the Inspector's own color-swatch field and Hierarchy expand triangles, not just Game-view buttons as previously documented — plan for a throwaway first click on any newly-focused Editor panel before trusting the next click.

**Next single bounded task:** resolve §2 above (confirm whether `EnsureBuyButtonShape`/`ApplyAccent` genuinely execute against the on-screen buy buttons) using one of the more conclusive methods listed there, ideally by whoever next has direct (non-remote-automated) Editor access. Only after that's settled does re-attempting the corner-radius multiplier bisection make sense.

### Claude (Cowork) — God Tier Store timed-item "buy repeatedly + THE WALLET" feature — 2026-09-16 (session 5)

Aceyfer's request (paraphrased): the real-money "cash items that decay after hours" (i.e. the God Tier Store's Brain Freeze family, and similar future timed items) need to support being bought more than once — including buying the *same* 24-hour item twice back to back — and there needs to be a "GOAT wallet"-style separate view (his term) showing each owned timed item plus its time remaining. He also flagged "badwords can be toggled" as something to double check.

#### 1. What actually needed building vs. what already existed

Investigated before writing anything, since "the cash shop" is ambiguous in this project (there are three: Shop 1 buildings, `CashShopManager`'s 5 permanent one-time items, and the `GodTierStoreManager` real-money items). Findings:

- `CashShopManager`'s 5 items (Golden Cardboard Crown etc.) are **all permanent one-time purchases** — no duration concept exists there at all, and none of its authored `.asset` items are time-limited despite one having "24-Hour" in a display name coincidence in the God Tier Store (see next point). Not touched.
- The actual "decay after hours" items are three `GodTierStoreItemData` assets under `Assets/_Game/GodTierStore/`: `BrainFreeze` (24h), `BrainFreeze48.asset` (itemId `brain_freeze_72`, display name "Brain Freeze: 72" — **the filename says 48 but the data says 72 hours**, a pre-existing authoring inconsistency, left as-is since it's a content/naming question for Aceyfer, not a code bug) and `DeepFreeze` (168h). All three already had `isConsumable = 1`, and `GodTierStoreManager.StubPurchase` already never adds consumables to `ownedItemIds` — so **repeat purchases of the same item were already technically unblocked in code before this session**. What didn't exist: any record of *which* purchases were active or their individual remaining time — `PlayerIQManager` only tracks one merged/stacked `brainFreezeExpiryUnixSeconds`, and nothing in the UI layer read `PlayerIQManager.IsBrainFreezeActive`/`BrainFreezeSecondsRemaining` at all (confirmed via a project-wide grep) — so there was **no player-facing visibility into Brain Freeze remaining time anywhere**, and no way to tell "two purchases" apart from "one."
- "Badwords can be toggled" is **already fully implemented and working** — `GodTierStoreSlotUI.HandleBuyClicked()` has a dedicated branch: once `bad_words_pack` is owned, tapping its row again calls `RandomChatterManager.ToggleProfanity(!chatter.ProfanityEnabled)` instead of re-purchasing, and `RefreshState()` shows `OWNED · ON` / `OWNED · OFF` accordingly. No code change made here — just confirming for the record, since an older dead-code comment in `CashShopSlotUI.cs` (profanity_pack branches, already marked dead 2026-07-10) claims "the God Shop slot UI must provide an equivalent" as an open TODO. **That comment is stale — the equivalent already exists in `GodTierStoreSlotUI`.** Did not fix the stale comment; not worth the churn for a comment that's merely out of date, not actively misleading anyone into re-doing the work (unlike the wiring comment below, which actively would).
- **Also stale and worth flagging explicitly:** both `CashShopUIController.cs`'s and `GodTierStoreUIController.cs`'s class doc comments say "SCENE WIRING NOT YET DONE ... no panel/button/Content hierarchy exists in SampleScene.unity yet." For the God Tier Store half this is **no longer true** — the 4 real items (`GodShopSlot_bad_words_pack`, `GodShopSlot_brain_freeze`, `GodShopSlot_brain_freeze_72`, `GodShopSlot_deep_freeze`) are live in the scene today, reachable via the SHOP button → the existing 3-tab bar's rightmost **RP tab** (`ShopRoot/Tab_RP/...`) — i.e. someone wired the *content* into the existing three-tab shop rather than ever using `GodTierStoreUIController`'s own described openButton/closeButton popup pattern. Whether `GodTierStoreUIController` (the component) is even in the scene at all was not established either way — the Hierarchy search for "GodTier" found nothing, but see the Inspector caveat below for why that's inconclusive. **Don't trust that class's doc comment about wiring status going forward** — verify live instead, the way this session did.

#### 2. What was built

- **`GodTierStoreManager.cs`**: new top-level `[Serializable] struct ActiveTimedPurchase { itemId, expiryUnixSeconds }` (same precedent as `UpgradeManager.BuildingSaveEntry` — a plain DTO reused directly as the runtime ledger entry, not a separate wrapper type). New `activeTimedPurchases` list + pruning `ActiveTimedPurchases` property (drops expired entries on every read). `StubPurchase` now appends a new independent entry whenever `item.isConsumable && item.freezeDurationHours > 0f` — **deliberately additive, never keyed/deduped by itemId**, so buying the same 24-hour item twice produces two separate entries/countdowns, not one merged one. This is purely a display ledger sitting *alongside* the existing gameplay effect — `PlayerIQManager`'s single merged/stacked `brainFreezeExpiryUnixSeconds` (the actual IQ-floor timer) is completely untouched and still the sole source of truth for gameplay; nothing about how the floor itself is computed changed. `LoadState` gained an optional trailing parameter (`IEnumerable<ActiveTimedPurchase> restoredActiveTimedPurchases = null`, backward compatible) that restores the ledger and drops anything already expired. Also added an `#if UNITY_EDITOR` `[ContextMenu("DEBUG: Buy Brain Freeze (24h)")]` hook (`DebugBuyBrainFreeze`) for exercising this without needing the live shop UI — same precedent as `DailyEngagementCapManager.DebugBurnFullRateAllowance`; left in place, it's genuinely useful and compiles out of builds.
- **`SaveManager.cs`**: new `PlayerData.activeTimedPurchases` field (`List<ActiveTimedPurchase>`), guarded with the same `??=` null-fill pattern as the other owned-item lists, gathered on save from `GodTierStoreManager.Instance.ActiveTimedPurchases`, passed into `GodTierStoreManager.LoadState`, and defaulted in `CreateDefaultData()`.
- **New file `Assets/_Game/Scripts/UI/TimedPurchaseWalletUI.cs`** — "THE WALLET." Fully code-built, self-bootstrapping, no prefab/scene wiring, following `PocketPanelUI.cs`'s exact proven pattern (same viewport/RectMask2D/VerticalLayoutGroup/ContentSizeFitter construction, same `CanvasGroup`-alpha show/hide, same "find `LogOpenButton`, anchor relative to it" button placement). Its own open button is anchored **two button-slots below Dia-Log** (computed straight off `LogOpenButton`'s own rect, not off `PocketOpenButton` — deliberately, since `Start()` order between this class and `PocketPanelUI` isn't guaranteed) so it stacks under POCKET without a hard dependency on Pocket having built first. Lists every `GodTierStoreManager.ActiveTimedPurchase`, resolving each one's display name fresh from `GodTierStoreManager.Items` by itemId every rebuild (never caches a name at purchase time). Refreshes on `GodTierStoreManager.OnItemsChanged` and once a second via `GameManager.OnSecondTick` while open (both no-op while closed); a full `RebuildList()` on every tick is fine at this scale (a handful of active items, max) and doubles as the mechanism that drops a row the instant its countdown reaches zero (pruning happens inside `ActiveTimedPurchases`'s own getter).

#### 3. Verification (live Play Mode, real device resolution)

Full end-to-end test performed live rather than just via the debug hook, since it turned out the RP tab is genuinely reachable (see §1's stale-comment note):

1. Compiled clean (0 errors) after one fixup — first push referenced `GodTierStoreManager.ActiveTimedPurchase` as a nested type from `SaveManager.cs`; it's actually a top-level struct in the same namespace, so the qualifier was wrong (`CS0426`). Fixed, recompiled clean, only pre-existing unrelated `CS0618` TMP-obsolete warnings remained (same family already noted in earlier sessions' entries above, present in `IntelCardUI`/`DebugCheatPanel`/`PocketPanelUI` too — `TimedPurchaseWalletUI` copies the same `enableWordWrapping` idiom from `PocketPanelUI`, so it picked up the identical warning, not a new one).
2. Entered Play Mode, opened SHOP → RP tab, bought **Brain Freeze** once. HUD `IQ` jumped to 200 as expected (`ApplyBrainFreeze`'s own behavior, unchanged). Opened WALLET (new button, confirmed rendered directly under POCKET) — showed exactly one row, "Brain Freeze / 23h 58m remaining," ticking down live second-to-second.
3. Bought **Brain Freeze a second time** (same item, back to back). Reopened WALLET: **two independent rows**, both "Brain Freeze," both showing their own ~23h5xm countdown. This is the literal scenario Aceyfer described ("if they want to buy 2 24hr items they should be able to") and it works.
4. Empty state ("THE WALLET IS EMPTY. BUY A TIMED ITEM TO SEE IT HERE.") confirmed showing correctly before any purchase.

Did not verify the badwords-toggle UI pixel-for-pixel live (zoomed screenshots were too low-resolution to read the `OWNED · ON`/`OWNED · OFF` price text at this scene's mobile-simulated scale) — but that code path was not touched this session and was already read/confirmed correct directly from source (§1), so this is a documentation gap, not an unverified behavior change.

#### 4. Another Inspector-under-reporting data point (relevant to session 4's §2 mystery above)

While hunting for `GodTierStoreManager`'s GameObject to test via its Inspector context menu, selecting the live `GodShopSlot_brain_freeze` row (and separately `ShopRoot` itself) during Play Mode showed **only `RectTransform` + `Canvas Renderer` + `Image`** in the Inspector — no `GodTierStoreSlotUI` script visible on the row, no controller script visible on `ShopRoot`, despite both very clearly having working script-driven behavior (the row's buy button visibly works; `ShopRoot`'s tab-switching visibly works). This is the **same symptom** session 4's §2 documented for `UpgradeSlotUI`/`BuyButton` on a completely different part of the UI, which is a useful data point: it's now been seen on at least two unrelated GameObjects in two different sessions, which further supports session 4's suspicion that this is a **remote-desktop/Inspector-rendering limitation of this automation setup**, not something wrong with any particular prefab or script. Whoever eventually chases session 4's §2 mystery with direct (non-remote) Editor access should keep this in mind — the fix, if any is even needed, is likely environmental, not per-GameObject.

**Next single bounded task:** none required — this feature is complete and verified. If Aceyfer wants it extended (e.g. a generic "any consumable with a duration" ledger instead of the current Brain-Freeze-family-specific `freezeDurationHours` check in `StubPurchase`), that's a small, well-contained follow-up once a second kind of timed consumable actually exists.
