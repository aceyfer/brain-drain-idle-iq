# PEDESTRIAN SPRITES FINDINGS — Stage1 corruption audit + backup folder reclassification

Read-only investigation, **2026-08-12**. No code changed, no assets moved/modified, nothing
staged, nothing committed. One report for this investigation thread (mirrors
`CODEX_FINDINGS.md`'s convention: replace, don't accumulate, if this topic is revisited from
scratch later).

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
