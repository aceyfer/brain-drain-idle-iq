# buildingId Migration Plan

Status: **Commit 1 LANDED** (`eaf25d2`, "refactor(buildings): add stable buildingId to
BuildingData"). 17 files, 18 insertions, 0 deletions — `BuildingData.cs` field + 16 asset
values. Not pushed. Commits 2-3 (the actual migration logic) remain unstarted and
unapproved.

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

**No progress loss for any save that exists today.** `BuildingSaveEntry` gets `buildingId`
**added**, not renamed — `buildingName` stays in the struct permanently, matching this
codebase's existing convention (`tapMultiplier`/shop-multiplier zero-fill guards, the
Profanity Dialogue Pack fallback at `SaveManager.cs:302-329` — none of these have ever been
deleted after shipping).

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
template today, so **step 2 succeeds for 100% of real saves that exist right now.**

**One-time or permanent?** Both. Per save file: once loaded (via name-fallback) and saved
again, it carries a real `buildingId` and takes the fast path thereafter — a one-time silent
upgrade per save. The **fallback code itself stays in the codebase permanently**, matching
established convention — there's no way to know whether a given save has ever round-tripped
through the new code (test devices, restored backups can resurface indefinitely).

**Honest remaining risk:** safe for any save loaded at least once before `buildingName` is
ever renamed again. The scenario that *would* lose a building's level: a save never loaded
again until *after* some future rename lands (e.g., reinstalled from an old cloud backup
after a stage-dependent-name update ships) — narrow, real, not a risk today.

## 4. Verification steps

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

### AMENDMENT 3 — rebirth-cycle verification (added)

Steps 1-11 never exercised a Snotting cycle, but `ResetBuildings()` and
`ClearActiveBuildingLocks()` operate on the same `buildingLevels` dictionary being re-keyed.
Confirmed by reading `UpgradeManager.cs:239-277`: `ClearActiveBuildingLocks()` clears
`activeBuildingLocks` (a `List<(double bpps, double cps, float restoreAtTime)>` — **not**
keyed by `buildingName`/`buildingId` at all, stores raw suppressed amounts only, so it is
structurally unaffected by this migration) and unsubscribes the lock-restore tick;
`ResetBuildings()` calls `buildingLevels.Clear()` (key-type-agnostic — wipes the whole
dictionary regardless of whether it's keyed by name or id) then `ClearActiveBuildingLocks()`.
This is the fix from commit `9d1cbdb` ("fix: rebirth — discard active building locks on
Snotting (was injecting pre-reset production into the new run); stop lock tick when locks
drain") — its doc comment at `:234-238` explicitly states the reason: a pending
`RestoreIdleBPPS`/`RestoreCashPerSecond` firing after reset would inject a pre-reset
building's production onto the zeroed baseline of the new run as phantom income.

Add, after step 11:

12. Perform a full rebirth (Snotting) post-migration. Confirm `buildingLevels` clears
    completely — zero entries survive under either `buildingId` or leftover old-`buildingName`
    keys (a dictionary-key-format mismatch during the migration edit could theoretically leave
    stale entries under the wrong key type; `Clear()` should make this impossible, but verify
    directly by dumping the dictionary before and after).
13. Confirm `activeBuildingLocks` is empty and the lock-restore tick is unsubscribed after the
    reset (trigger a building-lock event — e.g. "Ministry Inspection" — immediately before
    rebirthing, then rebirth, then confirm no phantom BPPS/CPS appears in the new run once the
    lock's original `restoreAtTime` would have elapsed). This is the exact regression `9d1cbdb`
    fixed — confirm it still holds post-migration, since `LockRandomBuildingFor` now resolves
    its `owned` list via `GetBuildingLevel(building)` reading `buildingId` instead of
    `buildingName`.
14. Re-buy several buildings post-rebirth; confirm levels land correctly under `buildingId`
    keys with no collision against anything left over from the pre-rebirth run.
15. Save and reload once more after the post-rebirth re-buys; confirm the fresh save round-trips
    cleanly (fast `buildingId` path, correct counts).

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
  saves load/save identically, field just round-trips empty. **Landed** as a single-line
  change (`public string buildingId;`, first field in the struct); staged, not yet committed,
  pending your Play Mode verification against a real save.
- **Commit 3**: the actual migration — `LoadBuildingLevels`'s id-first/name-fallback logic,
  `TryBuyBuilding`/`GetBuildingLevel`/dictionary keys switched to `buildingId`, `SaveGame`
  writing both fields. The one commit where behavior changes; full verification sequence
  (steps 1-15 above, including the Amendment 3 rebirth pass) concentrates here, ideally
  against a copy of the real save file.

  **HARD REQUIREMENT — the single most likely way Commit 3 breaks saves:** `JsonUtility` does
  not run field initializers or zero-fill a missing `string` field to `""` on deserialization
  — it leaves it at the C# default for a reference type, which is `null`. Every save that
  exists today, the very first time it's loaded after Commit 2 lands, will deserialize
  `buildingId` as `null`, not `""`. Commit 3's fallback branch in `LoadBuildingLevels`
  **must** test `string.IsNullOrEmpty(entry.buildingId)`. A bare `entry.buildingId == ""`
  comparison would never be true for any save that exists right now — every current save
  would incorrectly take the id-first path with a `null` id, fail to resolve any template,
  and silently fail to restore every building's level (the exact failure mode §3 describes
  as "genuinely orphaned," but triggered for 100% of saves instead of the intended 0%). §3's
  algorithm description above already uses the correct `string.IsNullOrEmpty` form — this
  callout exists so Commit 3's actual code can't regress to the naive comparison.
- **Optional Commit 4** (not required for `buildingId` to work): tighten orphan-entry
  handling (drop + warn instead of silently accumulating) — isolated so it can be
  reviewed/reverted independently of the migration itself.

## Outstanding blockers before Commit 2

- None recorded yet. Commit 2 (add `buildingId` to `BuildingSaveEntry`) has not been scoped
  for a go-ahead; awaiting explicit approval before any further code changes.
