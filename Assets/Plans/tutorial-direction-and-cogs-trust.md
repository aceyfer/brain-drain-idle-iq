# Tutorial direction & "don't trust early COGS" nudge system

Status: **Proposed — not started.** This is a design/scoping document only; nothing below has
been implemented. Written after live playtesting surfaced the SNOTTING-button silent-fail bug
and prompted the broader question of how the game gives players direction.

## Correction before anything else: the SNOTTING bug is already fixed

`building-id-migration.md`'s "Unrelated issues found" section flags "the SNOTTING trigger button
gives zero feedback when clicked while locked" as an open, unfixed problem. It no longer is.
`RebirthUIController.cs` was patched (commit `068e46e6`, "fix(ui): give the locked SNOTTING
button real feedback on tap") to keep the button always-`interactable` and move the unlock gate
into `OpenModal()` (`RebirthUIController.cs:208-214`), so a locked tap now calls
`PlaySnottingDenial()` (`:237-256`): `AnimationController.PlayDenialShake(rt)` fires on every tap,
plus a randomly-picked, non-repeating denial line (`:266-289`) via
`DialogueManager.Instance.ShowPriorityLine(line)`, rate-limited by
`SnottingDenialLineCooldownSeconds` (1.5s) so rapid tapping can't flood the dialogue queue.

If you saw zero feedback in a recent playtest, the most likely explanation is you were testing a
build from before that commit landed, not that the fix regressed. Worth a quick re-check in your
next Play Mode pass, but I wouldn't spend time re-fixing this — the code already does the right
thing.

## Goal

Two related but separable problems, both raised by the same playtest:

1. **Reactive**: when a locked/unaffordable action is tapped, does the player get told why? (The
   Snotting button now does. Other locked interactions may not — see below.)
2. **Proactive**: does the game ever point at what to do *next*, rather than waiting for the
   player to tap the wrong thing first? Right now, nothing does this outside the one-time FTUE
   script.

Both should also serve the existing narrative premise: COGS is a hostile, lying narrator in its
early stages and a trustworthy one later (`COGSStage.cs`'s own design note, `:11-13`: "COGS
progresses from a corrupted, cynical and hostile Stage 1 to a godlike, clear, and supportive
Stage 6"). Player-direction copy is a natural place to keep reinforcing that arc instead of
spending it once in the first ten minutes and never again.

## What already exists (inventory, so this isn't proposed from scratch)

- **`FTUEManager.cs`** already runs a "COGS lies to you" narrative: Beat 4 (`:69-70`) tells the
  player not to convert Brain Power to Cash ("CHOICE IS AN ERROR STATE"); Beat 6 (`:73-74`) tells
  them spending on restoration is filed under "harmless." Both are COGS actively steering the
  player away from correct play. THE LITERATES resistance cards (`IntelCardCatalog.cs`,
  delivered via `EnqueueCard`) run in parallel, implicitly contradicting COGS. **All of this fires
  at most once per beat, ever** (`bootBriefingSeen`/`cashBeatSeen`/etc., `FTUEManager.cs:140-156`)
  — it's a first-play script, not a recurring system.
- **`RebirthUIController.cs`** is the only place in the codebase that implements "locked tap →
  shake + narrator denial line" (confirmed via `grep -rn "PlayDenialShake\|Denial"` across
  `Assets/_Game/Scripts` — one hit outside `AnimationController.cs` itself). This is the pattern
  to reuse, not reinvent.
- **`AnimationController.PlayAffordablePulse`/`StopAffordablePulse`** (`AnimationController.cs:1081-1093`)
  already exists as a ready-built "highlight this row" animation — but it is wired to always call
  `Stop` in `UpgradeSlotUI.UpdateAffordablePulse` (`UpgradeSlotUI.cs:210-217`), with the comment
  **"Disabled per user request (too flashy)."** So a nudge mechanism was built, tried, and
  deliberately turned off — worth knowing before proposing a new one that might hit the same
  objection.
- **`UpgradeSlotUI.cs:157-179`** shows locked rows (never yet affordable) with the copy "Access
  restricted by the Ministry." — flavor text, COGS-adjacent in tone but not attributed to COGS,
  and static (no pointer, no "go do X to unlock this").
- **Art assets that exist today** (`Assets/_Game/Sprites/UI/`): `StorefrontPanelLocked.png`
  (hand-authored locked-panel art), `StarburstBadge.png`, `ui_pill.png`, `restoration_fill.png`,
  `VintageTV.png`. **Nothing resembling a pointer, arrow, spotlight ring, or dimmed-background
  mask exists yet.**
- **`PlaceholderArtGenerator.cs`** already contains a small procedural-texture toolkit
  (`FillCircle`, `FillEllipse`, `DrawThickLine`, `DrawPolyline`, `SaveTextureAsSprite`,
  `:325-395`+) used today to generate the 6 COGS portrait expressions in code rather than by hand.
  This is relevant to the art-cost question below.

## The actual reactive gap

`UpgradeSlotUI.cs:196-199`: an unaffordable-but-unlocked building row keeps `buyButton.interactable
= true` "so the player can attempt purchase," and the comment says the manager "silently rejects
if unaffordable." That's the exact same failure class the Snotting button used to have — a tap
that does nothing observable — just never patched here. This is the one concrete, scoped bug fix
worth doing regardless of anything else in this doc: give `UpgradeSlotUI`'s buy button the same
`PlayDenialShake` treatment `RebirthUIController` already has. No new art required — it reuses an
existing animation call on the row's existing `Image`/`RectTransform`.

## The proactive gap, and the art-cost tradeoff you flagged

Nothing today points at what to do next. Two ways to build that, at very different costs:

**Option A — retune, don't rebuild (low cost, no new art).** `PlayAffordablePulse` already exists
and is already wired to every shop row's background `Image`; it's just permanently suppressed.
Revisiting *why* it read as "too flashy" (speed, color, scale range) and shipping a subtler
version costs tuning time, not art or new code paths. This covers "which row should I buy" nudges
for free.

**Option B — a real pointer/spotlight system (the extra work you're flagging).** Anything beyond
re-tinting an existing row — an arrow pointing at a button, a glow ring around it, a dimmed mask
over the rest of the screen — needs an asset that doesn't exist in this project yet. Two ways to
get it, in increasing cost order:
  1. Extend `PlaceholderArtGenerator.cs`'s existing procedural toolkit with one or two new draw
     functions (an arrow via `DrawPolyline`, a ring via `FillEllipse` minus an inner
     `FillEllipse`) and a generator menu item, the same way the COGS portraits were made. Net new
     work, but small and consistent with how this project already produces placeholder art —
     no external art tool or hand-drawing needed.
  2. Hand-author real pointer/spotlight art (matching `StorefrontPanelLocked.png`'s approach) if
     the procedural look isn't good enough. More polished, more expensive, and probably not worth
     it before Option A/B.1 are tried and evaluated in Play Mode.

Recommendation: **do the `UpgradeSlotUI` denial-shake fix and Option A first** — both are
same-day, code-only changes that reuse things already built. Treat Option B as a follow-up scoped
separately once it's clear the pulse-only nudge isn't enough.

**STATUS (2026-08-30): done.** Both landed — `UpgradeSlotUI.HandleBuyClicked` now shakes on an
unaffordable tap via the same `AnimationController.PlayDenialShake` `RebirthUIController` uses,
and `UpdateAffordablePulse` is re-enabled with a narrower alpha range (0.82-1.0 vs. the old
0.4-1.0), a slower period (1.3s vs. 1.0s), and a state-transition guard (`isPulsing`) so the
coroutine isn't restarted on every tap-driven `RefreshState` call — that restart-on-every-refresh
behavior, not the alpha range alone, was almost certainly the real "too flashy" cause originally.
Codex verified both in Play Mode: no flicker/stutter during active tapping, and 5 simultaneous
affordable rows read as a "soft glow," not busy.

Codex's testing pass also caught a real, separate bug while in there: the SNOTTING button's
`img.raycastTarget`/`innerFillImage.raycastTarget` were still gated by `unlocked`, so the locked
state had zero raycastable graphic and taps fell through to `MainTapButton` underneath —
`OpenModal()` never fired, meaning the "always interactable, gate inside OpenModal" fix
(referenced at the top of this doc) had been silently defeated by an unrelated line a few
lines below it. Fixed by hardcoding both to `true`; re-verified working in Play Mode afterward.

**Also resolved:** the Doomscroll naming-collision risk flagged below was spot-checked directly
against all 16 buildings' authored `evolutions` — computed each building's resolved display name
at every stage 0-5 the way `GetDisplayName` actually does, and cross-checked for reuse. None
found; no two buildings ever share a display name at any stage.

## Narrative track — making "don't listen to COGS" a running bit, not a one-time beat

Right now the "COGS lies" premise is spent entirely in the first session (`FTUEManager` Beats
4/6, each fires once ever). Given `COGSStage.cs` already models a 6-stage hostile→supportive arc
keyed off `RebirthCount`, there's room to let a *few* more COGS lines — gated to
`COGSStage` index 0-1 specifically, not just first-play — reinforce the "don't listen yet" idea
recurringly, then visibly soften as the player advances stages. This would live alongside
`DialogueManager`'s existing narrator-line pool (already stage-gated per building purchase, per
the `buildingId`/stage-evolution work) rather than inside `FTUEManager`, since `FTUEManager` is
explicitly a once-ever script and this needs to be able to repeat early and taper off.

Not scoped further here — this is a copywriting/authoring task (new `NarratorLine` assets with
`minRestorationPercent`/stage gating) rather than a systems change, and probably wants its own
short pass once the reactive/proactive UI pieces above are settled.

**STATUS (2026-08-30): copy pass done, one manual step remains.** Added 4 new `NarratorLine`
assets in `Assets/_Game/Dialogue/`, each directly undercutting one of COGS's established
early-game lies, all gated to `minRestorationPercent: 0, maxRestorationPercent: 20` (a
deliberately custom early-only window — verified against the existing `TierNtoM` assets first,
since that naming is inverted: `Tier80to100_*` is actually the *earliest* band, `Tier1to19_*` the
latest; see `minRestorationPercent`/`maxRestorationPercent` on each, not the filename number):

- `CashConverted_NoErrorStateAfterAll.asset` (triggerType 6/CashConverted) — "Cash converted to
  Restoration Points. No error state triggered. I will not be revising my earlier statement."
  Undercuts FTUE Beat 4 ("CHOICE IS AN ERROR STATE").
- `FirstRestoreSpend_StillNotHarmless.asset` (triggerType 9) — "Resources allocated to restoration
  again. Still filed under harmless. I am not re-filing it. Re-filing implies the first filing was
  wrong." Undercuts FTUE Beat 6 ("FILED UNDER: HARMLESS").
- `IQMilestone_WouldTellYou.asset` (triggerType 4) — "PlayerIQ rising. This is fine. This is not a
  problem. I would tell you if it were a problem."
- `EventOutcome_MutedTheResistance.asset` (triggerType 3) — "There is a resistance cell insisting
  you deserve the truth. I have muted them. You are welcome." Directly references THE LITERATES,
  which up to now only ever contradicted COGS implicitly.

All 4 keep the established COGS early-voice conventions (composed, dry, proper capitalization, no
emoji — this is not the glitchy late-game voice). Files + matching `.meta`s (fresh GUIDs, no
collisions with existing assets) are committed to
`C:\Users\aceyf\Brain Drain\Assets\_Game\Dialogue\`.

**Update: the manual Inspector step is also done.** Rather than hand-edit the scene's YAML, I drove
the actual Unity Editor (already open on this project) and wired it through the Inspector directly:
selected `DialogueManager` in `SampleScene`, grew `Narrator Lines` from 118 to 122 (Unity duplicates
the last element into new slots on a size-field resize, so 118-121 all briefly pointed at
`Tier80to100_Rebirth` before being corrected), then set each of the 4 new slots via the object
picker — Element 118 `CashConverted_NoErrorStateAfterAll`, 119 `FirstRestoreSpend_StillNotHarmless`,
120 `IQMilestone_WouldTellYou`, 121 `EventOutcome_MutedTheResistance` — and saved the scene
(Ctrl+S; title bar's unsaved-changes asterisk cleared). Verified each slot by reading back its
resolved name before saving. (I also checked whether Unity's own "Unity AI" editor assistant could
do this step instead — it isn't installed in this project, and installing it means agreeing to
Unity's AI Terms of Service, so I asked first; you chose to have me wire it directly rather than
install and use Unity AI, which is what happened above.) These 4 lines are now live and will play
in-game the next time their trigger conditions fire.

## Open questions before implementation starts

- Is Option B (pointer/spotlight nudges) in scope now, or is Option A + the denial-shake fix
  enough for this pass? **Resolved 2026-08-30: you chose Option B, procedural art.**
- If Option B goes ahead, procedural (extend `PlaceholderArtGenerator.cs`) or hand-authored art?
  **Resolved: procedural, extending `PlaceholderArtGenerator.cs` — see below.**
- Which specific moments most need a proactive nudge — first affordable building, first shop tab
  switch, first restoration spend, first Snotting-ready state? **Resolved for this pass: first
  affordable building (the earliest, most universal onboarding moment). The other three remain
  open for a future pass — the pointer component itself is generic (`PointAt(RectTransform)`), so
  adding another call site later is cheap.**

## Option B build (2026-08-30): done, arrow-only, one moment wired

**What shipped**, driving the live Unity Editor directly (already open on this machine) rather
than hand-editing YAML, the same way the narrator-line wiring above was done:

- **`PlaceholderArtGenerator.cs`** gained a `BrainDrain/Generate Placeholder Art/Nudge Pointer`
  menu item. It procedurally draws a black-outlined gold arrow (`NudgeArrow.png`, same two-pass
  outline technique the COGS portraits already use) via two new helpers, `FillTriangle`/
  `IsInsideTriangle`, then finds-or-creates a `UINudgePointer (Generated)` object directly under
  the scene's root `Canvas`, gives it an `Image` sourcing that sprite (`raycastTarget` off, starts
  disabled), and adds a `UINudgePointer` component — fully idempotent, re-running it updates
  rather than duplicates. Ran it live: confirmed via the Inspector that the object, Image, sprite
  reference, and script component all wired correctly, then saved the scene (Ctrl+S).
- **`UINudgePointer.cs`** (new, `Assets/_Game/Scripts/UI/`): a generic, reusable "look here"
  pointer. `PointAt(RectTransform target)` shows it and repositions every `LateUpdate` to hover
  above the target's top edge with a gentle sine bob; `Hide()` dismisses it. Self-subscribes to
  `UpgradeManager.OnBuildingPurchased` and auto-hides on *any* purchase — a deliberate simplifying
  assumption documented in the class comment, since the only current caller is itself about a
  building purchase. Unlike `FTUEManager`/`RandomChatterManager`'s `Instance`, this one does
  *not* auto-create its hosting object on first access, since a runtime-created one would have no
  sprite — only the Editor generator above can supply that, so a null `Instance` just means the
  generator hasn't been run yet in this project (it now has).
- **`UpgradeSlotUI.cs`**: added `MaybeShowFirstAffordableNudge`, called from `RefreshState` right
  after the existing pulse-transition logic. Fires `UINudgePointer.Instance?.PointAt(...)` at the
  buy button the first time any row computes `affordable == true` while
  `boundManager.BuildingLevels.Count == 0` (i.e. the player owns nothing yet) — session-only, not
  persisted via SaveManager, gated by a static bool so a reloaded save with existing buildings
  never re-triggers it and it fires at most once per app launch.
- Deliberately **arrow-only, one call site.** A ring/spotlight variant was scoped in the original
  Option B write-up but not built — nothing consumes it yet, and adding one later is the same
  `FillEllipse`-minus-inner-`FillEllipse` trick the portrait outline already demonstrates. The
  other three candidate moments (shop tab switch, first restoration spend, first Snotting-ready)
  are still open — `UINudgePointer.PointAt` is generic enough that wiring another caller is cheap
  whenever one of those is prioritized.
- **Play Mode testing (2026-08-30/31, via Codex) found a real bug and it's now fixed.** Three
  rounds of testing:
  1. First pass reported "arrow never appears at all." Rather than guess at a fix blind, added
     temporary `[NudgeDebug]` logging to `UINudgePointer.PointAt`/`RepositionOverTarget` and
     `UpgradeSlotUI.MaybeShowFirstAffordableNudge` and asked for a clean re-test with the Console
     open.
  2. Second pass's logs showed the nudge actually *was* firing correctly (`screenPointToLocalOk=
     True`, correct row, self-consistent coordinate math) — so "never appears" was actually "does
     appear, but looks wrong": the arrow landed up near the shop's tab bar/close-button area
     instead of cleanly over the row's buy button. Hand-verified the entire
     `WorldToScreenPoint`/`ScreenPointToLocalPointInRectangle` chain arithmetically against the
     logged numbers (including the Canvas's 0.28 = 302/1080 scale factor) and confirmed the
     transform math itself was correct — ruling out a wrong-target or coordinate-math bug.
  3. Third pass confirmed, via the exact Hierarchy path
     (`Canvas/CustomSafeArea/ShopRoot/Tab_BP/Tab_BP_ScrollView/Viewport/Content/UpgradeSlot_Apex
     Brain Greens/BuyColumn/BuyButton`), that the target genuinely is the correct row's real buy
     button, sitting immediately under the tab bar — the first visible row in the scroll list, by
     construction, has almost no headroom above it.
  
  **Root cause**: `UINudgePointer`'s RectTransform pivot is bottom-center, so `anchoredPosition`
  only tracks the arrow's *tip* — its visual top edge extends a further full `sizeDelta.y` (85.3
  units) above that. The real vertical footprint intruding above a target's top edge is
  `VerticalOffsetPixels (56) + arrow height (85.3) + bob (up to 10)` ≈ up to 151 units on the
  1920-tall reference canvas, not the ~56-66 units the original offset math assumed — enough to
  collide with the tab bar for a row with little headroom, regardless of which row it is.
  
  **Fix**: `PointAt` now takes an optional `clampToVisibleArea` `RectTransform`. When set,
  `RepositionOverTarget` computes that area's top edge in the same canvas-local space (via the
  same `WorldToScreenPoint`/`ScreenPointToLocalPointInRectangle` technique, applied to the area's
  4 world corners) and clamps the arrow's Y so its visual top edge never rises above it, with a
  12px padding. `UpgradeSlotUI.MaybeShowFirstAffordableNudge` now looks up the target's enclosing
  `ScrollRect` and passes its `.viewport` as that clamp area — the same boundary that already
  visually clips the row, so the arrow can never poke out past what's actually visible, at any row
  position, not just the first one. This is a general fix, not a one-off tuned offset for this
  specific row. Removed the temporary `[NudgeDebug]` logging now that the root cause is confirmed
  and fixed. Verified a clean Editor compile (0 errors) after the change.

- **Regression (2026-08-31): "no gold arrow" over the shop, and the real root cause.** After the
  clamp fix above, the user reported the arrow wasn't appearing at all when the shop panel was
  open — despite working fine over the plain HUD. Rather than guess again, this was chased with
  direct live-Editor testing (Reset Save → Play → Add 10K Brain Power → open Shop, repeated with
  targeted `Debug.Log` instrumentation added and removed round by round) instead of blind patches.
  Three interim fixes turned out to be **necessary but not sufficient** on their own:
  1. `UINudgePointer.Instance`'s lookup needed `includeInactive` on the `GetComponentInParent`
     path it used internally, since the pointer's own object can transiently sit under an inactive
     ancestor.
  2. `LateUpdate`'s "is the target still around" check had to use `activeSelf`, not
     `activeInHierarchy` — the nudge legitimately fires while the shop panel is closed (it's
     computed continuously, same as the affordable-pulse logic), and `activeInHierarchy` was false
     for every ancestor of a row inside a closed panel, permanently hiding the arrow before the
     player ever opened the shop. `activeSelf` only reflects the row's own toggle, so a genuinely
     removed/pooled row still correctly hides the pointer.
  3. `RepositionOverTarget` was made to call `SetAsLastSibling()` every frame, on the theory that
     something else might be re-ordering siblings within the Canvas.
  
  All three were confirmed correct and necessary, but the arrow *still* stayed invisible
  specifically over shop rows. Two further hypotheses were tested live and definitively ruled out
  rather than assumed:
  - **Sibling-order race condition** — theorized another script's `LateUpdate` might call
    `SetAsLastSibling()` after ours, every frame, silently winning the race. Instrumented to log if
    we were ever *not* last-sibling at the start of our own reposition call; this log never fired
    across a full test pass, proving sibling order was correct every single frame the bug still
    reproduced. Ruled out.
  - **`anchorMin`/`anchorMax` flipping mid-session** — an early reading showed `(0.5, 0)` in one
    session and `(0.5, 0.5)` in another, suggesting something was mutating the anchor at runtime
    (a `LayoutGroup` on an ancestor was also checked for and found absent). Added frame-exact
    change-detection logging; in a controlled test the anchor was `(0.5, 0.5)` from frame one and
    never changed for the rest of the session, yet the arrow was still visible over the HUD and
    still invisible over the shop row throughout — proving the anchor value had no causal effect
    on the actual symptom. Ruled out. (Left `PlaceholderArtGenerator.cs`'s existing anchor value
    unchanged since it was shown to be irrelevant to this bug.)
  
  **Actual root cause**: logging the *target* row's own Canvas ancestry (not the pointer's) found
  it sitting under `Tab_BP`, which has its own nested `Canvas` component with
  `overrideSorting = true, sortingOrder = 1` (`ShopUIController.TabContentSortingOrder`, used so
  shop tabs layer correctly over one another). A nested Canvas with `overrideSorting` renders as
  an entirely separate, globally-sorted pass, ordered against *every other Canvas in the scene* —
  including its own ancestors — purely by `sortingOrder`, completely bypassing normal
  Transform-sibling-order rules. `UINudgePointer` lived directly under the root Canvas
  (`sortingOrder = 0`, no override), so `Tab_BP`'s entire subtree — every shop row — always drew
  on top of it regardless of sibling index. This is exactly why `SetAsLastSibling()` never helped:
  it only affects ordering *within* one Canvas, and is powerless against a sibling Canvas with an
  explicit higher `sortingOrder`. It also explains why the arrow rendered fine over plain HUD
  content, which has no such override.
  
  **Fix**: gave `UINudgePointer`'s own GameObject a `Canvas` component with
  `overrideSorting = true, sortingOrder = 10` — above the shop's `1` but below
  `IntelCardUI.OverlaySortingOrder` (`500`), so a modal popup still correctly covers the arrow.
  Applied in both `UINudgePointer.Awake()` (defensive, holds regardless of scene state) and
  `PlaceholderArtGenerator.cs`'s `WireNudgePointerObject()` (so a freshly-generated object is
  correct without needing Play mode). All temporary `[NudgeDebug2]`–`[NudgeDebug7]` logging has
  been removed from the shipped code.
  
  **Verified fixed via direct live Editor testing** (not just a compile check): Reset Save → Play
  → dismiss FTUE → Add 10K Brain Power → open Shop → the gold arrow renders correctly positioned
  and clamped above the "Apex Brain Greens" row regardless of whether the shop was open or closed
  at the moment it first became affordable; bob animation confirmed across consecutive frames;
  purchasing the row correctly incremented owned count and dismissed the arrow; 0 console errors
  throughout.

## Non-goals for this pass

- Not touching `FTUEManager.cs`'s existing one-time beats — they work and are out of scope.
- Not re-fixing the Snotting button — already fixed (see top of this doc).
- Not building a ring/spotlight variant, or wiring the other three candidate nudge moments
  (shop tab switch, first restoration spend, first Snotting-ready) — the component supports it
  cheaply later, but nothing asked for it yet.
