# buildingId Migration Plan

Status: **All three commits LANDED and pushed to origin/main.**
- Commit 1: `eaf25d2` — `buildingId` field on `BuildingData` + 16 asset values.
- Commit 2: `51f66f1` — `buildingId` field on `BuildingSaveEntry`, additive/unread.
- Commit 3: `65db0f8` — ownership keyed by `buildingId` throughout (`LoadBuildingLevels`,
  `TryBuyBuilding`, `GetBuildingLevel`, `SaveGame`'s DTO write); no fallback, hard-required;
  verified via the 7-step list in §4, including a rebirth pass via `DebugCheats.ForceRebirth()`
  (the real HUD Snotting button was locked at verification time — see the unrelated-issues
  note at the bottom of this file) and a `MaxAllBuildings()` stress pass, both clean.

Migration complete. `DialogueManager`'s narrator matching remains on `buildingName` by design
— Amendment 1's hard prerequisite gate — and is the one open item before any Phase 2
stage-dependent-naming work can begin.

## Goal

Split identity from display text on `BuildingData` by introducing a stable `buildingId`
field, so `buildingName` can later become stage-dependent (per the parked per-stage shop
copy idea in `CODEX_FINDINGS.md`) without breaking saves, `UpgradeManager` lookups, or
narrator-line matching. This phase (Commits 1-3 below) must produce **ZERO visible change
in game**.

## Pattern being followed

`CashShopItemData.itemId` / `OutfitData.outfitId`: a plain hand-authored `string` field,
snake_case, chosen once at asset-creation time, **not** an auto-generated GUID, **not**
required to match the display name. No automatic uniqueness enforcement anywhere —
`CashShopManager` uses `HashSet<string>.Contains`, `WardrobeManager.FindById` does a linear
scan. Uniqueness is pure author discipline. `buildingId` follows this exactly.

## 1. Type and assignment

`buildingId` → plain `string` field on `BuildingData`, default `""`, tooltip mirroring
`CashShopItemData.itemId`'s own doc comment ("Stable save key, independent of
buildingName").

Values for the 16 existing assets are derived from each asset's **file name**, not its
current `buildingName` — the two have already diverged once for real:
`RealityTVSyndicate.asset`'s `buildingName` field is currently `"The Great Reversal"`; the
asset file itself kept its original identity while the display text was renamed. `buildingId`
should track that stable identity (`reality_tv_syndicate`), not today's display string.

**Uniqueness/stability guarantee, stated plainly: there isn't a technical one.** Same as
`itemId`/`outfitId` today — nothing stops a future Inspector edit to the field itself. The
guarantee is behavioral: author it once, never touch it again; `buildingName` stays the only
field that's free to change. A one-time manual duplicate check across all 16 values before
commit is the only actual verification available, matching how `itemId`/`outfitId` were
verified.

### Commit 1 result — the 16 assigned ids (locked, do not revisit)

| Asset filename | buildingId | buildingName (current display) |
|---|---|---|
| BrainRotThinkTank.asset | brain_rot_think_tank | Brain-Rot Think Tank |
| CranialMicrowave.asset | cranial_microwave | Tinfoil Headband |
| CryoSludgeEspresso.asset | cryo_sludge_espresso | Cryo Plunge Tank |
| CryptoBroCompound.asset | crypto_bro_compound | Crypto-Bro Compound |
| DefrostDrip.asset | defrost_drip | StupAid H2O |
| DoomscrollBillboard.asset | doomscroll_billboard | Doomscroll Billboard |
| DoomscrollEngine.asset | doomscroll_engine | Loose Change Collective |
| HOAProtectionRacket.asset | hoa_protection_racket | The Laundromat |
| IQOverclockChip.asset | iq_overclock_chip | Pineal Overclock |
| JumperCables.asset | jumper_cables | Apex Brain Greens |
| LemonadeGriftStand.asset | lemonade_grift_stand | Charity Shell |
| PodcasterSoundboard.asset | podcaster_soundboard | Podcaster Soundboard |
| RealityTVSyndicate.asset | reality_tv_syndicate | The Great Reversal |
| SynapseSpaceHeater.asset | synapse_space_heater | Hyperbolic Brain Chamber |
| TheLiteralLibrary.asset | the_literal_library | The Literal Library |
| UndergroundEconomy.asset | underground_economy | Snott Market Exchange |

**Design decision, recorded so it isn't re-litigated later:** `buildingId` is intentionally
filename-derived and **permanently decoupled** from display text, because `buildingName` is
expected to vary across all six World Restoration stages once Phase 2 lands. Old/unused
filenames (`RealityTVSyndicate`, `DoomscrollEngine`, others) are available raw material for
Phase 2 naming, not a prescribed ladder — reuse a prior name only where it genuinely fits
that stage; otherwise author a new one. The governing rule is that each stage's name must
feel distinct from the others while staying recognizably the same building across the
progression — an evolution, not a replacement. Deriving `buildingId` from the filename
rather than from any single stage's display text is what keeps the id stable regardless of
which name ends up showing at a given restoration stage.

**Open issue for Phase 2 authoring — flagged, not solved:** `DoomscrollBillboard.asset`
(displays `"Doomscroll Billboard"`) and `DoomscrollEngine.asset` (displays `"Loose Change
Collective"`) are two distinct buildings with two distinct `buildingId`s
(`doomscroll_billboard` / `doomscroll_engine`). If a future per-stage evolution ladder ever
renames Billboard's display text toward something like "Doomscroll Engine" at a later stage,
it would land two different buildings on the same display concept simultaneously — confusing
even though their ids stay perfectly distinct underneath. Whoever authors the per-stage
`buildingName` ladders in Phase 2 needs to check for this kind of cross-building name
collision explicitly; nothing in the id system itself prevents it, since ids and display
text are deliberately unrelated.

## 2. Every buildingName-as-key call site

Corrects and completes `CODEX_FINDINGS.md`'s list (`UpgradeManager.cs`, `SaveManager.cs`,
`DialogueManager.cs`, "7 narrator lines") — that list was **incomplete** (missed
`ShopUIController.cs` and `ShopQuery.cs`) and undercounted the narrator-line asset count.

| Site | Current use | Becomes |
|---|---|---|
| `UpgradeManager.buildingLevels` (`Dictionary<string,int>`, `:33`) | Live ownership key | Keyed by `buildingId` |
| `BuildingSaveEntry.buildingName` (`UpgradeManager.cs:11`) | Save-file DTO key | New `buildingId` field **added alongside**, `buildingName` field kept (Q3) |
| `UpgradeManager.GetBuildingLevel` (`:67-73`) | `building.buildingName` dict lookup | `building.buildingId` |
| `UpgradeManager.TryBuyBuilding` (`:134`, `:162-163`) | Guard + dict write | `building.buildingId` |
| `UpgradeManager.LoadBuildingLevels` (`:295-317`) | Store-into-dict (`:300`) + BPPS/CPS re-derivation lookup (`:309`) | `buildingId`-first with `buildingName` fallback (Q3) |
| `SaveManager.SaveGame` (`:489-492`) | Writes dict keys into `BuildingSaveEntry.buildingName` | Writes both `buildingId` and `buildingName` |
| `ShopUIController.cs:808` (sort tie-break) | Cosmetic display order only | Leave as-is; not a data key |
| `ShopUIController.cs:830` (`slot.name = "UpgradeSlot_{buildingName}"`) | GameObject debug label only | Leave as-is |
| `ShopQuery.cs:153/154/290` (`BuildBuildingItemId`) | Same coupling pattern, confirmed **dead code** (behind `ShopTabView`/`ShopUIController` guard) | Leave alone per your prior explicit instruction on this file |
| `LockRandomBuildingFor` (`UpgradeManager.cs:179-212`) | Iterates `buildingTemplates`, calls `GetBuildingLevel(building)` | **No separate change needed** — inherits the migration automatically via `GetBuildingLevel` |
| `DialogueManager.cs:334` → `TryFireLine` → `:455` match against `NarratorLine.buildingName` | Exact-string content match against 11 authored assets | **Stays on `buildingName` in this phase — see Amendment 1 reclassification below** |

**Narrator-line asset count correction:** 11 assets carry a non-empty `buildingName` (7
`BuildingPurchase_*` + 4 `Tier*_BuildingPurchase_*` duplicates layered on top for 4 of the
same buildings across different `RestorationPercent` tone tiers), referencing 7 distinct
buildings. `BuildingPurchase_RealityTVSyndicate.asset` and
`Tier20to39_BuildingPurchase_RealityTV.asset` both correctly carry
`buildingName: "The Great Reversal"` — live proof a rename already happened once and was
kept in sync by hand across two separate assets, with no build-time check enforcing it.

### AMENDMENT 1 — dialogue matcher reclassified as a hard prerequisite gate

Originally filed as a "future TODO." That undersold the risk: the entire point of
`buildingId` is enabling a later stage-dependent `buildingName`. The moment `buildingName`
becomes stage-dependent, all 11 `NarratorLine.buildingName` matches in
`DialogueManager.TryFireLine` (`:455`, `line.buildingName == buildingName`) go stale —
**silently**. A non-match fires no line and raises no error or warning; the building-purchase
flavor text for those 7 buildings just quietly stops appearing, with nothing in the Console
to catch it.

**HARD PREREQUISITE GATE: Phase 2 (making `buildingName` stage-dependent) may not begin
until `DialogueManager`'s building-purchase matching moves off `buildingName` and onto
`buildingId`, with all 11 `NarratorLine` assets' matching field updated accordingly.** This
is not optional cleanup — it is a blocking dependency of Phase 2, recorded here so it can't
get lost between now and whenever Phase 2 is scoped. **Not migrated now** — `buildingName`
isn't being removed in this phase, only supplemented, so `DialogueManager` has nothing to
gain from moving early. The gate exists purely so a future session doesn't start Phase 2
without first satisfying it.

## 3. Save migration

**DECIDED 2026-08-12 — the fallback described below is dropped. `buildingId` is hard-required.**
Reasoning: every save written from `51f66f1` (Commit 2) onward already carries the
`buildingId` key. Once Commit 3 populates it correctly, no save produced by this codebase
will ever lack an id again — the name-fallback branch would be permanently dead code
guarding a case that structurally cannot occur once Commit 3 ships. This project is
pre-launch with no live player saves; the only save in existence is a disposable dev save,
wiped and re-maxed every test session. Accepted cost: that dev save has `buildingId: ""` on
every entry (confirmed directly in its JSON after Commit 2 landed) and loses its building
levels once, on first load after Commit 3 lands. That's fine — there is no progression state
worth protecting here. If this project reaches a point where real player saves exist and this
decision needs revisiting, that's a new, explicit decision to make then, not something to
guess back into from this file.

**Original design (superseded, kept for record only — not implemented):** the paragraphs
below described a permanent `buildingId`-first/`buildingName`-fallback lookup, written before
"no live saves to protect" was recognized as the actual project state. None of this shipped.

<details>
<summary>Superseded fallback design</summary>

`BuildingSaveEntry` would get `buildingId` **added**, not renamed — `buildingName` stays in
the struct permanently, matching this codebase's existing convention (`tapMultiplier`/shop-
multiplier zero-fill guards, the Profanity Dialogue Pack fallback at `SaveManager.cs:302-329`
— none of these have ever been deleted after shipping).

`UpgradeManager.LoadBuildingLevels`, per entry:
1. If `!string.IsNullOrEmpty(entry.buildingId)` → resolve the template by `buildingId` (linear
   scan over `buildingTemplates`, same cost class as `WardrobeManager.FindById`).
2. Else (`buildingId` is `null` or `""` — the "predates this field" signal, same shape as the
   existing `tapMultiplier <= 0` guard) → fall back to matching `entry.buildingName` against
   `buildingTemplates[i].buildingName`, the current unmodified lookup, kept as-is.
3. If neither matches → genuinely orphaned. Recommend logging a warning and dropping it,
   rather than today's actual behavior (silently stored unindexed, re-persisted into the
   save file forever — the bug `CODEX_FINDINGS.md` surfaced).

Every one of the 16 current `buildingName` values still exists verbatim on its matching
template today, so step 2 would have succeeded for 100% of real saves that existed at the
time this was written.

</details>

**Actual implemented behavior (Commit 3):** `LoadBuildingLevels` requires `buildingId`
unconditionally. Per entry: if `string.IsNullOrEmpty(entry.buildingId)`, log a warning naming
the entry's stale `buildingName` and drop it — no fallback attempt. This is deliberate,
visible handling (not a silent skip, not a throw that would take down the whole `LoadGame()`
pipeline for one bad entry) — see §5's Commit 3 write-up for the exact code. `BuildingSaveEntry.buildingName`
is kept in the struct as a save-file readability aid only (§5), not as a fallback key —
nothing at load time reads it anymore.

## 4. Verification steps

**TRIMMED 2026-08-12, matching the no-fallback decision in §3.** The original 15-step list
below was built around proving old-format saves survive a fallback path. That's no longer a
goal — there's nothing to fall back to, and no live save worth protecting. Kept in the
collapsed section for record; the list actually used for Commit 3 is the 7 steps beneath it.

<details>
<summary>Superseded 15-step list (assumed a fallback that was never built)</summary>

1. Compile check — Console clean.
2. **Before any code change**, copy the real save file
   (`Application.persistentDataPath/braindrain_save.json`) to a safe location as a known-good
   snapshot.
3. With the current build, record ground truth: open that JSON, note every
   `buildingLevels` entry's `buildingName`/`level`.
4. Apply the Commit 1-3 changes.
5. Enter Play Mode with `KeepSaveEditorPrefsKey` still `true` (do not wipe the save) — tests
   whether the old-format save loads through the fallback path correctly.
6. In the Shop UI, confirm every building's `OWNED: N` matches step 3's recorded values
   exactly.
7. Confirm (via an explicit log line on the fallback branch) that the fallback path was
   actually exercised, not silently skipped.
8. Trigger a save, reopen the JSON, confirm entries now carry both `buildingId` (correctly
   resolved) and `buildingName`.
9. Re-enter Play Mode a second time (save has now round-tripped once) — confirm the fast
   `buildingId` path is used, counts still match.
10. Buy one more level of some building; confirm the dictionary write lands under
    `buildingId` with no duplicate entry under the old name-key.
11. Run `DebugCheats.MaxAllBuildings()` (Editor menu) as a stress pass across the full
    16-building roster.
12. Perform a full rebirth (Snotting) post-migration. Confirm `buildingLevels` clears
    completely — zero entries survive under either `buildingId` or leftover old-`buildingName`
    keys.
13. Confirm `activeBuildingLocks` is empty and the lock-restore tick is unsubscribed after the
    reset (trigger a building-lock event immediately before rebirthing, then rebirth, then
    confirm no phantom BPPS/CPS appears once the lock's original `restoreAtTime` would have
    elapsed) — the `9d1cbdb` regression check.
14. Re-buy several buildings post-rebirth; confirm levels land correctly under `buildingId`
    keys with no collision against anything left over from the pre-rebirth run.
15. Save and reload once more after the post-rebirth re-buys; confirm the fresh save round-trips
    cleanly.

</details>

**Actual Commit 3 verification list (7 steps):**

1. Compile clean.
2. Load the current dev save once. Confirm the warning log fires exactly 3 times (once per
   entry, all currently `buildingId: ""`) and all three prior building levels are gone — the
   accepted, expected one-time loss per §3's decision.
3. Buy a few buildings fresh. Confirm `OWNED: N` and BPPS/CPS totals are correct.
4. Save, reload once. Confirm the same levels persist — the only path now, so this is the real
   round-trip proof.
5. Full rebirth (Snotting): confirm `buildingLevels` clears completely, confirm no phantom
   BPPS/CPS reappears after a pre-rebirth building lock's `restoreAtTime` would have elapsed
   (the `9d1cbdb` regression check — see below for why it's unaffected by this migration).
6. Re-buy after rebirth, confirm correct under `buildingId` keys.
7. `DebugCheats.MaxAllBuildings()` stress pass across all 16, confirm no exceptions.

**Rebirth/lock interaction, confirmed structurally, not just by re-running the old check:**
`ResetBuildings()` (`UpgradeManager.cs`) calls `buildingLevels.Clear()` — content-agnostic,
works identically regardless of what strings key the dictionary. `ClearActiveBuildingLocks()`
clears `activeBuildingLocks`, a `List<(double bpps, double cps, float restoreAtTime)>` that
stores only pre-resolved numeric deltas — never keyed by `buildingName`/`buildingId` at all,
so it's structurally untouched by this migration regardless of key type. `LockRandomBuildingFor`
builds its `owned` list via `GetBuildingLevel(building)`, which now resolves through
`buildingId` internally — but `LockRandomBuildingFor`'s own code is unchanged, since it never
touched the key directly. The `9d1cbdb` fix (discard pending locks before rebirth so a
delayed restore can't inject phantom income into the reset run) is unaffected by construction,
not just by re-test — but step 5 above still empirically re-confirms it once rather than
resting on reasoning alone.

## 5. Order of operations — smallest safe commits

- **Commit 1**: add `buildingId` field to `BuildingData.cs` + populate the 16 `.asset` values.
  Zero `.cs` logic reads it yet. Verify: diff shows only the new field and 16 data additions,
  Play Mode behavior byte-identical to before.

  **AMENDMENT 2 — authoring method for Commit 1 (confirm before starting):** direct YAML
  edits to disk, written by me — both the new field on `BuildingData.cs` and the matching
  field/value insertion into all 16 `.asset` files, done in the same pass. **Unity must be
  fully closed for the duration of these edits** — `block-unity-scene-writes` only guards
  `.unity` files, not `.asset`, so there is no tooling safety net here; an open Editor could
  reimport or reserialize mid-write and corrupt or silently drop the hand-typed field. After
  the edits are complete, Unity reopens, recompiles the script, and reimports the 16 assets —
  Unity binds new YAML fields to matching C# field names automatically on load, no Inspector
  data entry required. Verification after reopen: all 16 assets show the expected
  `Building Id` value in the Inspector, no console errors, no collateral change to any other
  field. **Waiting on your confirmation of Unity's closed state before touching anything.**

- **Commit 2**: add `buildingId` field to `BuildingSaveEntry` in `UpgradeManager.cs` (**not**
  `SaveManager.cs` — that was an error in this section, corrected 2026-08-12; the struct is
  declared at `UpgradeManager.cs:8-13`, confirmed by reading the file directly. §2's table
  above always had this right). Additive struct field, still unread/unwritten. Verify: old
  saves load/save identically, field just round-trips empty. **Landed** (`51f66f1`,
  2026-08-12) as a single-line change (`public string buildingId;`, first field in the
  struct). Structural check against the live dev save confirmed `buildingId` present exactly
  once per `buildingLevels` entry, serialized as `""` (not `null` — see the write-side
  correction below), all levels/names intact, no other key affected. No pre-change backup was
  available to diff against; not a blocker in this project's current state, see the open
  question below.

  **Correction to the `null` claim above:** confirmed against the real save file after
  Commit 2 landed — `JsonUtility` deserializes a missing string field as `null` on *read* (as
  stated), but coerces `null` back to `""` on *write* (a separate, known `JsonUtility` quirk —
  it does not emit JSON `null` for strings). So the observed on-disk value is `"buildingId": ""`,
  not `"buildingId": null`. Doesn't change the HARD REQUIREMENT below — `string.IsNullOrEmpty`
  handles both — but the plan's own earlier prediction of the literal on-disk value was wrong
  and is corrected here rather than left standing.

- **DECIDED 2026-08-12 — see §3 for full reasoning.** The permanent `buildingName`-fallback
  path was never built. Dropped in favor of hard-requiring `buildingId`, on the grounds that
  this project is pre-launch with no live player saves worth the permanent legacy branch.

- **Commit 3**: the actual migration, no-fallback design — `LoadBuildingLevels` hard-requires
  `buildingId` (warn-and-drop any entry missing it, see §3), `TryBuyBuilding`/`GetBuildingLevel`/
  dictionary keys switched to `buildingId`, `SaveGame` writes `buildingId` as the real key and
  resolves `buildingName` fresh via the new `UpgradeManager.GetBuildingNameById` helper for
  save-file readability only. The one commit where behavior changes; verification is §4's
  trimmed 7-step list, not the superseded 15-step one. **Landed** (staged, not yet committed —
  see commit hash once it lands) as a single commit: five edited sites in `UpgradeManager.cs`
  (`BuildingSaveEntry` doc comment, `GetBuildingLevel`, new `GetBuildingNameById`,
  `TryBuyBuilding` guard + write, `LoadBuildingLevels` load + re-derivation loops) plus one
  edited site in `SaveManager.cs` (`SaveGame`'s dictionary-to-DTO write). Writer sweep run
  first, 2026-08-12: `buildingLevels` is a `private readonly Dictionary`, mutated only at the
  four sites already covered by this edit list (`TryBuyBuilding`, `LoadBuildingLevels`'s two
  `Clear()` calls, `LoadBuildingLevels`'s populate line) — confirmed via full-repo grep,
  including all 19 Editor `MenuItem` entry points (zero matches) and `DebugCheats.MaxAllBuildings()`
  (routes through the real `TryBuyBuilding` pathway by its own prior design, doesn't touch the
  dictionary directly). Nothing else to migrate.

  **HARD REQUIREMENT — the single most likely way this commit breaks the (disposable) dev save
  in an unintended way:** `JsonUtility` does not run field initializers or zero-fill a missing
  `string` field to `""` on deserialization — it leaves it at the C# default for a reference
  type, `null` — but coerces that `null` back to `""` on the following *write*, a separate
  `JsonUtility` quirk (confirmed empirically: the dev save's `buildingId` values read `""` on
  disk after Commit 2, not `null`). `LoadBuildingLevels`'s drop-path **must** test
  `string.IsNullOrEmpty(entry.buildingId)`, not a bare `== ""` comparison, to correctly catch
  both forms. Implemented this way — see the code.
- **Optional Commit 4** (not required for `buildingId` to work): tighten orphan-entry
  handling (drop + warn instead of silently accumulating) — isolated so it can be
  reviewed/reverted independently of the migration itself.

## Outstanding blockers

None. All three commits landed and pushed (`eaf25d2`, `51f66f1`, `65db0f8`). Migration
complete pending Phase 2's own future scoping, which is gated on Amendment 1 (§2).

## Unrelated issues found during Commit 3 verification (not fixed — recorded only)

Both surfaced incidentally while running the 7-step verification list against a real Play
Mode session. Neither is caused by, or related to, the `buildingId` migration — recorded here
only because this is where they were found, not because they belong to this plan's scope.

**1. The SNOTTING trigger button gives zero feedback when clicked while locked.**
`RebirthUIController.ApplyTriggerButtonVisibility()` sets `Button.interactable = false` while
`CumulativePointsSpentOnRestoration < RebirthManager.SnottingUnlockThreshold` (5,658,229 RP).
A non-interactable Unity `Button` silently swallows clicks — no shake, no toast, no log, no
error. The button visually looks pressable (color/label do communicate "locked," but nothing
fires on the actual click attempt to confirm the player even registered as trying). This is
exactly what caused the apparent "rebirth did nothing" scare earlier in this session — the
dev save's 983 RP was nowhere near the threshold, the click never reached `TriggerRebirth()`,
and there was no feedback to explain why. Real UX trap, needs its own investigation
(some kind of locked-tap feedback — shake, toast, or an explicit log at minimum).

**2. The RESTORATION bar does not appear to update visually.** Observed during the same
session: converting 50% BP→Cash via CONVERT produced no visible change to the bar. Both
CONVERT and something else apparently need to be clicked before RESTORE becomes available.
Possibly no fill animation exists at all — the bar reads as a flat blue background with no
indication of current progress. Not investigated further — flagged for its own separate
diagnosis pass, not attempted here.
