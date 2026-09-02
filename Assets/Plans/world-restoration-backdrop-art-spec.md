# World Restoration Backdrop Art Spec — 6-Stage Progression

**Status:** spec only, 2026-09-01. No art generated or imported yet. Written the same way as the mesh-overlay and IAP specs — pending Aceyfer generating the actual images and running them through the existing import pipeline.

## Correction to an earlier finding first

§55 in TASKLIST.md flagged "only 3 real WorldRestorationStage assets (0/2/5) exist, so stages 1/3/4 can never occur in real play." I checked this directly today (live Hierarchy inspection + grepping the actual saved scene) and that finding was wrong — all 6 `WorldRestorationStage` assets exist on disk **and** are wired into `WorldRestorationManager.stages` in the live scene, indices 0 through 5, no gaps:

| Index | stageName (as authored in the asset, not the filename) | pointsRequired |
|---|---|---:|
| 0 | Cryo Chamber | 0 |
| 1 | Smog-Choked Sprawl | 20,000 |
| 2 | Patchwork Recovery Zone | 2,132,885 |
| 3 | Green Shoots Initiative | 5,658,229 |
| 4 | Renewed Skyline | 20,764,149 |
| 5 | Utopia Achieved | 250,000,000 |

(Note the stage-0 asset's filename says `ToxicWasteland` but its actual `stageName` field says `Cryo Chamber` — a stale filename from before a rename, harmless, not worth fixing on its own.)

So this is purely an **art content** problem, not also a configuration gap — good news, since it means generating 6 real images and re-importing them is the entire task; no code or scene wiring needs to change.

## What's live today

`BackgroundStageView.cs` on `SkylineBG` holds a `stageSprites[]` array (already sized 6, one slot per stage above) and swaps `Image.sprite` on `WorldRestorationManager.OnRestorationStageChanged`. The current sprites are `Assets/_Game/Sprites/Backgrounds/BG1.jpg` through `BG6.png` — real photographic images, not procedural placeholders, but thematically mismatched (BG1 is a neon cyberpunk alley, not a "Cryo Chamber," and none of the 6 currently reflect the actual stage names above). Format: portrait, 1008×1776 px (~9:16), matching this game's mobile portrait aspect. The image renders full-bleed behind the archway-shaped border/frame art from §55, so keep the visually important content centered — the frame masks the outer edges.

## The arc Aceyfer wants

Referencing the *Idiocracy* wasteland-sprawl shot: a garbage-choked, decaying, patched-together civilization at the low end, climbing to a clean, gleaming, futuristic paradise at the high end — six real distinct scenes, each stage its own place, not a filtered/recolored version of the same street.

## Six generation prompts

Each should be shot as a wide establishing/skyline view (matching the *Idiocracy* reference framing), portrait-cropped to 1008×1776 or generated wider and cropped, photoreal or near-photoreal VFX-matte style (matching the reference's look, not a painterly/illustrated style), consistent light/season across the set where possible so the progression reads as the same city healing rather than six unrelated locations.

1. **Cryo Chamber (Stage 0)** — actually an interior, not a cityscape: a dim, clinical cryo-stasis chamber/pod bay, cold blue-white light, frost on glass, a single occupied pod in frame, everything else dark and dormant. This is the game's opening image before the player ever sees the outside world.
2. **Smog-Choked Sprawl (Stage 1)** — the *Idiocracy* reference tier: a sprawling, garbage-heaped, patched-corrugated-metal cityscape under a thick brown-yellow smog haze, leaning/collapsing skyscrapers propped with scaffolding, a distant smokestack pumping black exhaust, junk piles and shanty rooftops in the foreground.
3. **Patchwork Recovery Zone (Stage 2)** — same city, visibly mid-cleanup: smog thinning to a hazy gray-gold, some scaffolding replaced with real repair work, a few cleared lots next to still-standing junk piles, at least one crane or work crew visible, sky starting to show real blue at the horizon.
4. **Green Shoots Initiative (Stage 3)** — visible plant life returning: rooftop gardens, a reclaimed lot turned into a community green space, cleaner building facades, clearer sky, still some rough edges (a patched wall, an old billboard) so it doesn't jump too far ahead of stage 2.
5. **Renewed Skyline (Stage 4)** — a genuinely rebuilt, modern skyline: clean glass-and-steel towers, clear blue sky, organized streets, greenery integrated into architecture (vertical gardens, tree-lined boulevards), aspirational but not yet fantastical — this is "a good city," not sci-fi yet.
6. **Utopia Achieved (Stage 5)** — the futuristic paradise payoff: sleek elevated architecture, visible clean-energy elements (solar, wind, maybe elevated transit), lush integrated greenery, bright optimistic lighting (golden hour or clear midday), a sense of abundance and calm. This is the "we did it" image the whole 6-stage arc has been building toward.

## Import steps (once images exist)

Same pipeline as every other art pass this project: run new images through `Assets/_Game/Scripts/Editor/ArtImportTool.cs` (`Tools > Eighth Kind > Art Import`) with the standard sprite settings it already applies (Sprite/Single, 100 PPU, Bilinear, 2048 max size). Simplest path: keep the same six filenames (`BG1.jpg` → `BG6.png`, or normalize all to `.png`) so `BackgroundStageView.stageSprites[]`'s existing serialized references keep pointing at the right slots with zero code or scene changes — just overwrite the asset content and let Unity reimport. If filenames change, the array needs manual re-assignment in the Inspector, one extra step but not a blocker.

## Not yet decided

Whether "explore a new world instead of the same street" (Aceyfer's phrase) means anything beyond "each stage is visibly a different place" — e.g. whether stage 0's cryo-chamber-to-street transition needs its own intermediate beat, or whether future stages might eventually get more than one photo each. Current scope above is six photos, one per existing stage slot, which already satisfies "not the same street" since all six are meant to be distinct scenes. If Aceyfer wants multiple rotating photos *within* a single stage later, that's a separate, bigger scope change to `BackgroundStageView` (an array-of-arrays plus a rotation trigger) — not assumed here.
