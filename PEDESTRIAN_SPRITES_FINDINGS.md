# PEDESTRIAN SPRITES FINDINGS — Stage1 corruption audit + backup folder reclassification

Read-only investigation, **2026-08-12**. No code changed, no assets moved/modified, nothing
staged, nothing committed. One report for this investigation thread (mirrors
`CODEX_FINDINGS.md`'s convention: replace, don't accumulate, if this topic is revisited from
scratch later).

## Full pedestrian-sprite distortion audit — 2026-08-20 (all 35 Ped*_Stage*.png)

Systematic scan of all 35 `Ped*_Stage*.png` files, completed outside this session using the
corrected near-black-blob heuristic established below, with every flag visually verified by eye
to rule out false positives from legitimately dark clothing. Results reported here and acted on
in this session.

**New confirmed defects, beyond the already-known Ped4_Stage1 (interim patch) and Ped5_Stage1
(severe, regen needed) covered in the status update below:**

| Ped/Stage | Severity | Description |
|---|---|---|
| Ped1_Stage2 | Severe | Large jagged void across the sweatshirt torso |
| Ped2_Stage2 | Moderate | Blotchy void patches on both shirt shoulders |
| Ped2_Stage3 | Moderate | Same shoulder void pattern as Stage2 |
| Ped4_Stage2 | Severe | Large void through chest/hood |
| Ped4_Stage3 | Severe | Large void through chest |
| Ped4_Stage4 | Mild | Thin jagged transparent notch at sleeve edge |
| Ped6_Stage3 | Mild | Small void bitten out of the hairline |
| Ped6_Stage4 | Mild | Same hairline void pattern as Stage3 |

**Confirmed clean, do not touch:** Ped1_Stage5, Ped3 (all stages), Ped4_Stage5, Ped4_Stage6,
Ped6_Stage1, Ped6_Stage2, Ped6_Stage5, Ped6_Stage6, Ped2_Stage1, Ped2_Stage4, Ped2_Stage5,
Ped2_Stage6, Ped5_Stage2 through Stage6.

**Decision (2026-08-20):** per the zero-tolerance no-visible-distortion rule (`PROJECT_BIBLE.md`
§2 rule 6), every confirmed-defective stage above is temporarily repointed to reference a clean
stage's art, so nothing broken can appear during the streamer playtest, while full regeneration
gets scoped separately. **The corrupted PNG files themselves are left untouched on disk** —
same policy already established for Ped4/Ped5 — they remain the regen reference/source. This is
a sprite-*reference* swap only, never a file edit.

**Swap mapping applied, commit `6b9fb03`:**

| Defective stage | Now references |
|---|---|
| Ped1_Stage2 | Ped1_Stage3's art |
| Ped2_Stage2 | Ped2_Stage1's art |
| Ped2_Stage3 | Ped2_Stage1's art |
| Ped4_Stage2 | Ped4_Stage5's art |
| Ped4_Stage3 | Ped4_Stage5's art |
| Ped4_Stage4 | Ped4_Stage5's art |
| Ped5_Stage1 | Ped5_Stage2's art |
| Ped6_Stage3 | Ped6_Stage2's art |
| Ped6_Stage4 | Ped6_Stage2's art |

**Mechanism, confirmed before any edit was made:** pedestrian stage→sprite wiring is split
across two independent systems. (1) `BackgroundPedestrianManager.cs`'s live spawn path reads
each `Ped{N}_Prefab.prefab`'s `SpriteRenderer.m_Sprite` directly and only ever shows Stage1 art
— this is the only stage actually visible in the shipped game today. (2) Each
`Ped{N}_AnimController.controller` has `Walk_Stage1`–`Walk_Stage6` states driven by an int
`StageIndex` parameter, each state's `Ped{N}_Walk_Stage{X}.anim` clip keyframing `m_Sprite` via
a `PPtrCurve`. **No code anywhere sets `StageIndex`** (confirmed via repo-wide grep), so every
`Walk_Stage2`–`Walk_Stage6` state is currently dead/unreachable. Practical consequence: 8 of
these 9 swaps (everything except Ped5_Stage1) currently have zero visible effect in the shipped
game, since that whole progression system is unwired — done anyway per the standing policy,
since they cost nothing and mean nothing is broken the moment that system does get wired up.
**Ped5_Stage1 is the one swap with live effect** — its sprite is read directly off
`Ped5_Prefab.prefab`, which is listed in `PROJECT_BIBLE.md` §5 Protected Zones
(`Assets/_Game/Prefabs/Pedestrians/` — no edits); editing it was an **explicit, approved
one-field exception** (`SpriteRenderer.m_Sprite` only, nothing else in the prefab touched).

All nine swaps repoint a `{fileID, guid}` sprite reference inside a `.anim` clip or the one
approved prefab field — no PNG file was deleted, renamed, or overwritten; git diff confirmed
exactly these 9 files changed, each only on the sprite-reference lines.

## Status update — 2026-08-20

- **Ped4: INTERIM PATCH SHIPPED, FULL REGEN STILL REQUIRED BEFORE STREAMER PLAYTEST.**
  The sleeve/shoulder rendering defect identified below (largest single contiguous near-black
  blob, 18.5% of subject area) was repaired via inpainting —
  `Assets/_Game/Sprites/Pedestrians/Ped4_Stage1.png` replaced on disk (same filename, same
  1008x1776 dimensions, same RGBA mode, `.meta` unchanged), committed `1638884`. Legs and the
  rest of the sprite untouched. **Reclassified 2026-08-20, no longer treated as final ship
  quality**: under close pixel-level zoom, the patched region reads visibly softer/blurrier
  than the surrounding fabric grain — a mismatch a mobile screen would hide but a PC/Steam
  close-up won't. This fails the new zero-tolerance no-visible-AI-distortion bar (see
  `PROJECT_BIBLE.md` §2 rule 6). The inpaint remains a legitimate development-time stopgap, not
  a defect anymore in the corruption-audit sense below, but Ped4 now needs the same full art
  regeneration as Ped5, not just the patch, before it can ship.
- **Ped5: NOT PATCHED — flagged for full art regeneration.** Direct backup comparison
  (`Pedestrians_backup_20260625/Ped5_Stage1.png`) confirms the defect is **baked into the
  original AI-generated source photo**, present identically in the backup — not a `rembg`
  alpha-removal artifact. It is significantly more severe than Ped4 was: the void wraps across
  most of the jersey graphic *and* eats into the arm/shoulder itself, both as opaque near-black
  pixels and as over-aggressive alpha cuts. Since the defect predates background removal,
  inpainting the live file alone can't be sourced from the backup the way a `rembg`-only
  artifact could — decision made 2026-08-20: **do not patch, regenerate the art instead.**

## Executive summary

- `Assets/_Game/Sprites/Pedestrians_backup_20260625/` is **not a broken/stale duplicate of the
  live sprite set.** It is the **pre-`rembg`-background-removal source archive** — same subject
  renders as the live set, fully opaque, black canvas background never cut to transparency.
  **Do not delete it.** It is the regeneration path for any live sprite found to be damaged.
- Using a corrected near-black-opaque threshold (not pure `RGB(0,0,0)`), the live Stage1
  batch shows real, non-trivial near-black content, worst on **Ped4** (largest single
  contiguous blob = 18.5% of subject area) and **Ped5** (9.6%). Ped2/Ped3/Ped6 show smaller
  largest-blob percentages but hundreds of scattered small regions, more consistent with edge
  matte fringing than one big defect. **See "Status update — 2026-08-20" above: Ped4 is now
  fixed, Ped5 is flagged for regeneration, not a patch.**
- **Ped1 has no Stage1 art anywhere in the project — live directory, flat backup files, and
  the backup's alternate per-stage subfolder tree are all empty for it.** It was either never
  generated or lost before the 2026-06-25 backup was made; there is no recovery path from
  anything currently on disk.

## Corrected near-black-opaque measurement (supersedes the earlier pure-black metric)

Earlier pass measured pixels at literal `RGB(0,0,0)` — wrong threshold, since background-removal
artifacts are near-black matte fringing, not pure black; that metric returned ~0.05% for live
files and told us nothing. Re-run with `alpha > 200 AND max(R,G,B) < 40` (and again at `< 25`),
plus 8-connected-component labeling to separate "one big blob" from "scattered noise."

### `max(R,G,B) < 40`, `alpha > 200`

| Ped | LIVE: % of image | % of subject | region count | largest region (% of subject) | BACKUP: % of image | largest region |
|---|---|---|---|---|---|---|
| 2 | 3.80% | 9.85% | 469 | 3.11% (21,452px) | 63.38% | 62.66% (whole-canvas background) |
| 3 | 2.37% | 7.01% | 330 | 1.18% (7,151px) | 29.88% | 22.13% |
| 4 | 12.41% | **38.22%** | 366 | **18.47% (107,318px)** | 39.57% | 25.39% |
| 5 | 6.59% | 28.13% | 314 | 9.61% (40,298px) | 78.98% | 77.68% (whole-canvas background) |
| 6 | 5.08% | 17.15% | 674 | 4.48% (23,764px) | 37.16% | 25.48% |

### `max(R,G,B) < 25` (stricter), `alpha > 200`

| Ped | LIVE: % of subject | largest region (% of subject) | BACKUP: % of subject | largest region |
|---|---|---|---|---|
| 2 | 5.92% | 0.80% | 61.76% | 61.05% |
| 3 | 4.01% | 0.62% | 28.86% | 21.94% |
| 4 | 29.01% | 11.47% | 36.53% | 25.02% |
| 5 | 22.81% | 8.93% | 77.71% | 77.34% |
| 6 | 9.78% | 2.35% | 34.90% | 24.78% |

**Reading this**: Ped4 and Ped5 are the two live files with a genuinely large *single* blob
(18.5% / 9.6% of subject area respectively at the looser threshold) rather than just scattered
dark-edge noise — those two are the strongest corruption candidates. Ped2/Ped3/Ped6 have
non-trivial total near-black coverage but it's spread across 300-670+ small regions with small
largest-blob percentages (1-4.5%), a pattern more consistent with normal matting edge fringe
than a corrupted fill region — though this reading is inferred from the numbers, not confirmed
by eye.

Backup files' "largest region" is trivially close to 100% of the frame for Ped2/Ped5 because
the entire canvas is one connected opaque-black background by construction (no alpha channel
at all, confirmed separately) — expected, not a defect signal, exactly as anticipated going in.

### Ped5 top-3 near-black-opaque region bounding boxes (`max(R,G,B) < 40` threshold)

Image is 1008w × 1776h.

**LIVE Ped5_Stage1.png** (subject area = 419,488px, 314 total regions):
1. 40,298px — rows 582–1077, cols 496–710 (h=495, w=214) — lower-middle vertical band, right
   of center
2. 17,009px — rows 340–587, cols 387–656 (h=247, w=269) — upper-middle band
3. 10,905px — rows 1006–1227, cols 646–727 (h=221, w=81) — lower band, narrow, right side

All three sit well inside the frame, not hugging the image border — consistent with being over
the subject rather than a background-edge artifact, but I can't confirm *which* body part
without viewing the image myself.

**BACKUP Ped5_Stage1.png** (subject area = 1,790,208px = full canvas, 152 regions):
1. 1,390,597px — rows 0–1776, cols 0–1008 (the entire canvas) — the baked black background,
   not a defect
2. 9,929px — rows 965–1254, cols 533–608
3. 6,356px — rows 962–1201, cols 391–555

## Backup folder — corrected classification

Direct pixel comparison (Ped5 corners + center, both files): backup's four corners are
`(0,0,0,255)` — opaque black — while live's four corners are `(0,0,0,0)` — genuinely
transparent. Both files' *center* (subject) pixel is identical: `(79,83,86)`, alpha 254 live /
255 backup. Same subject render, same RGB, only the alpha channel differs. Backup was never
background-removed — it's the input to that process, not an independent or corrupted copy.

**Consequence for future work**: if a live Stage1 (or any stage) sprite needs regenerating
because the live copy is damaged, the backup folder is the source to re-run `rembg` against,
not something to discard. Confirmed via `git ls-files` (prior session) that both directories
are fully tracked in the repo, not a stray local-only folder.

## Ped1 Stage1 — confirmed, does not exist anywhere on disk

Checked three locations:
1. Live flat file: `Assets/_Game/Sprites/Pedestrians/Ped1_Stage1.png` — absent, no file, no
   `.meta`.
2. Backup flat file: `Assets/_Game/Sprites/Pedestrians_backup_20260625/Ped1_Stage1.png` — present
   but 0 bytes (confirmed prior session), `.meta` present and non-empty (Unity imported a
   0-byte source successfully).
3. **Backup's alternate per-stage subfolder tree**
   (`Pedestrians_backup_20260625/Ped1_SlackJawMale/Stage1/` through `Stage6/`) — checked all
   six `StageN/` subfolders under `Ped1_SlackJawMale/`: **all six are empty, zero files.** For
   comparison, `Ped5_EngagedMale/`'s equivalent six `StageN/` subfolders are *also* all empty —
   this entire alternate subdirectory tree (one such folder exists per ped, named after
   `PedestrianBehaviorStage` enum values: `SlackJawMale`, `ShufflingFemale`, `WalkingMale`,
   `AwareFemale`, `EngagedMale`, `LegendaryFemale`) is vestigial/never-populated across the
   board, not specifically hollowed out for Ped1.

**Conclusion: Ped1 Stage1 art was either never generated or was lost before the 2026-06-25
backup snapshot was taken — there is no recovery path from anything currently in the repo.**
It would need to be generated fresh. In the meantime `Ped1_Prefab.prefab`'s `Image.m_Sprite`
is wired to `Ped1_Stage2.png` (confirmed prior session), silently masking the gap rather than
showing a broken/null reference in-game.
