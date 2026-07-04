# Session Handoff — Brain Drain: Idle IQ

Branch: `feature/overnight-ai-cook`. Nothing in this session was committed — everything below is sitting unstaged/untracked in the working tree. `OVERNIGHT_REPORT.md` (project root) covers an earlier slice of this same session in more narrative detail; this doc supersedes it for file-list purposes and adds everything since.

## 1. What this session built

Starting point: a working tap → Brains → buildings → IQ-decay idle clicker loop.

Ending point: tap → **Brain Power** (renamed from Brains) → 7 buildings → **PlayerIQ** (decay removed, now a pure-upward stat) → **Rebirth** (permanent multipliers) → a second/third currency tier (**Cash**/**Points**) → a **save system** → **random chaos events** → **tap/UI animation polish** → a **dialogue/narrator system** gated on Rebirth tier → a **COGS portrait progression** → a **12-chapter narrative arc** → **iOS safe-area handling**. Plus a late pass making the new singleton managers self-bootstrapping so a missing scene GameObject doesn't silently disable a whole system.

## 2. Files created this session

**Core (`Assets/_Game/Scripts/Core/`)**
- `PlayerIQManager.cs` (+ `.meta`) — third name for this script in this session (`IQDecaySystem` → `WorldProgressionManager` → `PlayerIQManager`), same preserved script GUID throughout so the scene's existing component kept resolving correctly across all three renames.

**Systems (`Assets/_Game/Scripts/Systems/`)**
- `SaveManager.cs` — JSON persistence (`PlayerData` struct) to `Application.persistentDataPath/braindrain_save.json`. `[DefaultExecutionOrder(-200)]`.
- `AnimationController.cs` — coroutine-based (no DOTween/LeanTween in project) singleton: tap squash/stretch, idle breathing, goo splat particles (procedural placeholder sprite), affordable-slot pulse, popup spawn shake, high-IQ celebration flash.
- `RandomEventManager.cs` / `BrainRotEventData.cs` — **modified**, not new (existed from an earlier slice of this session); tuned interval to 90–180s, added `OnEventResolved`.
- `RebirthManager.cs` — **modified**; added Cash/Points rebirth bonuses, `OnRebirthCountChanged`, save-on-rebirth, self-bootstrapping `Instance`.
- `NarratorLine.cs` — ScriptableObject; originally IQ-range gated, **regated to `minRebirthCount`/`maxRebirthCount`** partway through the session (see §5).
- `DialogueManager.cs` — 5-trigger dialogue picker; refactored mid-session to add `EnqueueDirectLine(string, duration)` for ad-hoc lines (used by `ChapterManager`'s `cogsReactionLine`) alongside the original `NarratorLine`-pool-driven trigger system.
- `COGSStage.cs` — ScriptableObject for narrator-portrait progression stages.
- `COGSPortraitController.cs` — resolves current `COGSStage` from `RebirthCount`, fires `OnStageChanged`.
- `ChapterData.cs` — ScriptableObject for the 12-chapter arc.
- `ChapterManager.cs` — sequential chapter unlock (checked every 10s + on currency/rebirth events), `CurrentTitle`, `OnChapterUnlocked`, `OnNamePromptRequested` (chapter 12 special case).

**UI (`Assets/_Game/Scripts/UI/`)**
- `DialogueDisplayUI.cs` — slide-in/hold/slide-out dialogue panel; font degrades with `PlayerIQ` (see §5 — this is now inconsistent with the RebirthCount-gated content); subscribes to `COGSPortraitController.OnStageChanged` for the avatar slot.
- `SafeAreaManager.cs` — iOS notch/Dynamic Island inset. Creates a child `"SafeArea"` RectTransform (the Canvas's own RectTransform can't be inset directly — Unity overwrites it every frame).
- `RandomEventUIController.cs` — **modified**, not new; popup spawn-shake wiring added this session.
- `UIBlockDebugger.cs` — **not mine**, appeared mid-session from elsewhere (click-raycast debug helper, harmless, left alone).

**Data assets**
- `Assets/_Game/Buildings/UndergroundEconomy.asset` — 7th building, Cash-only (`baseBrainPowerPerSecond: 0`, `baseCashPerSecond: 0.5`), unlocks at 500 CumulativeBrainPower.
- `Assets/_Game/Events/` — 5 new `BrainRotEventData` assets (`PhilosophyProfessorIncident`, `MogulsBuyoutOffer`, `DoomscrollEngineViralLoop`, `BuildingInspectorBribe`, `QuarterlyBrainHarvestQuota`). Total event pool is now 8.
- `Assets/_Game/Dialogue/` — 30 `NarratorLine` assets (5 RebirthCount tiers × 6 trigger types) + `Assets/_Game/Dialogue/COGSStages/` — 6 `COGSStage` assets (thresholds 0/1/3/6/11/20, all `portraitSprite` null).
- `Assets/_Game/Chapters/` — 12 `ChapterData` assets.

**Docs**
- `OVERNIGHT_REPORT.md` — written mid-session, covers the audit-task portion in detail.
- `SESSION_HANDOFF.md` — this file.

## 3. Files modified this session (selected, non-trivial)

- `CurrencyManager.cs` — the big one. Added the Cash/Points tiers (`AddCashPerSecond`, `AddCash`, `ConvertCashToPoints`, `OnCashChanged`/`OnPointsChanged` as **`UnityEvent<double>`**, deliberately inconsistent with every other event in the class being a C# `event Action<T>` — that was explicit spec, not an oversight). `ExecuteRebirth`/`LoadState` signatures extended (3 and 8 params respectively).
- `UpgradeManager.cs` — purchases now register Cash-per-second alongside Brain-Power-per-second; unlock gating fully moved to `CumulativeBrainPower` (no more decay-level gating).
- `BuildingData.cs` — added `baseCashPerSecond`; `unlockPlayerLevel` → `unlockCumulativeBrainPower` happened earlier in the session.
- `GameManager.cs` — three save triggers (`OnApplicationPause(true)`, `OnApplicationFocus(false)`, `OnApplicationQuit()`), 60s autosave folded into the existing tick, and (very last edit this session) proactively touches `SaveManager.Instance` in `Awake()` so the save system can't end up silently never-instantiated.
- `HUDController.cs` — gained `CumulativeBrainPower`/`rebirthCount`/BPPS/Cash/Points readouts and a CONVERT button. Still does **not** have the tap-button (`mainTapButton`) wiring that was asked for several turns ago — that request got superseded by the Brain Power rename before it was implemented and was never picked back up. Confirmed via grep just now: no `mainTapButton` field exists anywhere.
- `PlayerTapHandler.cs` — gained `tapVisualTarget` (optional, falls back to its own transform — which is wrong, since the tap target is an invisible full-screen button) and `particleContainer` for `AnimationController` hooks.
- `CLAUDE.md` — rewritten multiple times this session to track the architecture; should currently be accurate as of the last edit (the self-bootstrapping pass at the very end was **not** back-ported into `CLAUDE.md` — worth doing next session if it matters).

## 4. Pending Inspector / scene wiring

None of this can be done from a script edit safely — either because it's scene-hierarchy surgery (risk of clobbering hand-built UI while the Editor has it open) or because it's literally "drag this asset into that list," which only exists in the `.unity`/prefab serialized data.

**Asset pools that need dragging into Inspector lists** (the scripts now self-bootstrap if the GameObject is missing, but an auto-created instance still has an *empty* list — these are separate problems):
- `RandomEventManager.potentialEvents` — only has whichever of the 8 events were wired before this session; the 5 newest definitely aren't in there yet.
- `DialogueManager.narratorLines` — all 30 assets.
- `COGSPortraitController.stages` — all 6 `COGSStage` assets (and they all have `portraitSprite: null` — no art exists yet either).
- `ChapterManager.chapters` — all 12 `ChapterData` assets.
- `UpgradeManager.buildingTemplates` — `UndergroundEconomy.asset` specifically (the other 6 should already be wired from earlier in the session).

**New UI fields needing references:**
- `HUDController`: `cumulativeBrainPowerCounterText`, `rebirthCountText`, `bppsText`, `cashText`, `pointsText`, `convertButton`, `hudCanvasGroup`, `celebrationFlashOverlay`.
- `DialogueDisplayUI`: `panelRect`, `lineText`, `avatarImage` (64×64 slot, no art yet), `lowIQFontAsset` (optional "Comic Sans equivalent," doesn't exist as an asset).
- `PlayerTapHandler`: `tapVisualTarget` (real visible sprite — currently falls back to the invisible tap button, which animates nothing visible) and `particleContainer` (optional).

**Scene-hierarchy work:**
- `SafeAreaManager` needs to be attached to the `Canvas` GameObject specifically (confirmed that's the right one — there's also a disabled Canvas and a separate `ChaosPopUpCanvas` you don't want this on). Then existing HUD content needs re-parenting under the `"SafeArea"` child it creates — anything still directly under `Canvas` won't respect the inset. Did not attempt this myself; pure scene surgery.

**Known-empty/never-wired managers** (self-bootstrap now, but confirm via Play Mode whether something already placed them manually before assuming):
- As of the last scene check this session, only `RebirthManager`, `GameManager`, `CurrencyManager`, `UpgradeManager`, and `PlayerIQManager` (via the renamed-but-same-GUID component) were actually present in `SampleScene.unity`. `SaveManager`, `DialogueManager`, `RandomEventManager`, `COGSPortraitController`, `ChapterManager`, `AnimationController`, `SafeAreaManager`, `DialogueDisplayUI` were **not** — worth re-checking at the start of next session in case manual placement happened after this check.

## 5. Design decisions / known gaps worth remembering

- **`DialogueDisplayUI`'s font-degradation effect is keyed off `PlayerIQ`** (clean at 100, chaotic by 20), but **which line plays is keyed off `RebirthCount`** (the original IQ-tier gating was migrated mid-session once it became clear `PlayerIQ` only ever increases and can't represent a degrading tone). These two are now inconsistent with each other — line *content* degrades with rebirths, line *presentation* doesn't follow. Not reconciled.
- **`BrainRotEventData.multiplierSpike`** has existed since early this session and is still never read by anything. Left alone each time it's come up.
- **`ChapterUnlockConditionType.PointsSpent`** is aliased to `CurrentPoints` (current balance) — there's no actual "lifetime points spent" tracker because nothing spends Points yet. Will silently diverge in meaning if a real Points-spending mechanic gets built later (your call when that happens).
- **`ChapterUnlockConditionType.WorldRestorationPercent`** always evaluates `false` — that condition type references a system (`WorldRestorationScore`) that was fully removed earlier in the session. None of the 12 given chapters use it, so it's not blocking anything today.
- **Chapter 12's name-prompt detection** is a literal string match on `playerTitle == "[Awaiting Name]"` — the given `ChapterData` schema has no dedicated bool flag for "this chapter needs a name prompt." Works for one chapter; would be worth a real field if more get added.
- **Building unlock thresholds**: real values are `TheLiteralLibrary`=0, `DoomscrollEngine`=0, `PodcasterSoundboard`=10,000, `CryptoBroCompound`=65,000, `RealityTVSyndicate`=185,000, `BrainRotThinkTank`=725,000, `UndergroundEconomy`=500 — these are tuned design values, not each building's `baseCost`. This has been a recurring point of confusion across task requests this session; worth double-checking against whatever spec you're working from next time before assuming a number is wrong.
- **Unexplained scene/asset changes not made by me**: 3 TextMesh Pro font assets show as modified, and `Assets/_Game/Animations/`, `Materials/`, `Sprites/`, `GeneratedAssets/`, and a dozen `Assets/Plans/*.md` files appeared over the course of the session from what looks like other concurrent activity. Never investigated; flagged each time one showed up. Worth a look in a fresh session.

## 6. Next build queue (as given, not yet started)

1. **Player Character + idle state machine.** No existing script to extend — this is net-new. Natural hook point: `COGSPortraitController.OnStageChanged` already exists and is explicitly designed for "world visual and outfit systems will listen to this later" (per the request that built it) — a character/idle-state system would likely want to listen to the same event rather than duplicate `RebirthManager.OnRebirthCountChanged` tracking independently.
2. **Outfit/Wardrobe system.** Same integration point as above — `COGSPortraitController.OnStageChanged` was built anticipating this exact system. `COGSStage` currently only carries `portraitSprite`; an outfit system will likely need its own data (probably a new field on `COGSStage`, or a parallel ScriptableObject keyed the same way) — no outfit-specific fields exist yet.
3. **World Visual Restoration.** Worth clarifying at the start of next session whether this is meant to resurrect the "world restoration" concept that was deliberately removed and replaced by `PlayerIQ` earlier this session (see §5's `WorldRestorationPercent` note) — if so, that's a real design reversal worth discussing explicitly before building, the same way the `PlayerIQ`→`RebirthCount` dialogue migration got flagged before being done.

Nothing in this section has been started. No code or assets exist for any of the three yet.

## 7. Addendum (2026-06-21, later same session): debug testing system + economy rebalance

Everything in sections 1-6 above is stale (Player Character, Wardrobe, and World Restoration — section 6's "next build queue" — all got built later in this same session) and **`CLAUDE.md` is the authoritative, current architecture doc** — read that first, not this file, for how any of it actually works now. This section exists purely to flag the two things built *most recently*, in case they're the source of anything that looks broken right now.

**A. Editor-only progression testing system** (`#if UNITY_EDITOR` throughout, compiles out of all builds): `Systems/DebugCheats.cs` (shared cheat logic), `UI/DebugCheatPanel.cs` (triple-tap `HUDController.PlayerIQText` to open a self-built panel), `Editor/TestingMenuShortcuts.cs` (`BrainDrain/Testing` menu), plus a `SaveManager.KeepSaveEditorPrefsKey` EditorPrefs toggle + an `EditorApplication.playModeStateChanged` hook that force-saves on Stop. Full detail in `CLAUDE.md`'s "Editor-only progression testing system" section.

**B. Economy rebalance** — a numeric pacing simulation (`balance_sim.js`, project root, not part of the game) found the pre-Rebirth building ladder cleared and the Rebirth button unlocked within about 30 minutes of play, World Restoration's thresholds didn't represent a real long-term arc, and tapping lost almost all relevance to idle income within ~15 minutes. Four changes in response, **all already saved to disk, last confirmed compiling clean via a live Editor.log check with zero new `error CS` lines**:
1. All 7 buildings' `costMultiplier`: `1.15` → `1.21` (`Assets/_Game/Buildings/*.asset`).
2. All 6 `WorldRestorationStage.pointsRequired`: rescaled 10x to `0/2,500/10,000/50,000/250,000/1,000,000` (`Assets/_Game/Restoration/*.asset`).
3. `RebirthUIController.pointsSpentUnlockThreshold`: `1,000` → `50,000` (the REBIRTH button's visibility gate).
4. New: `PlayerTapHandler.AddTapMultiplier` (permanent, +5% per Rebirth via `RebirthManager`) and `CurrencyManager.GetIQProductionMultiplier()` (idle BPPS/CPS scaled by `PlayerIQ / 100`, capped at 1 — ties offline-IQ-decay to a real idle-income penalty on return). Both required a new `tapMultiplier` field in `SaveManager.PlayerData`, with a load-time migration guard (a deserialized `0` from a pre-existing save means "field didn't exist yet," treated as `1`, not a real zeroed-out multiplier).

**If something looks broken right now and wasn't before this addendum**: check whether it's actually B.3 (Rebirth button correctly *not* appearing yet — it now needs 50,000 points spent on Restoration instead of 1,000) before assuming it's a regression. Full reasoning and the simulation numbers behind these values are in `CLAUDE.md`'s "Economy rebalance (2026-06-21)" section.
