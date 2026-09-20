# World Restoration Backdrop Art Spec — 6-Stage Progression

> **SUPERSEDED FOR LAUNCH (2026-09-15):** the current BG1-BG6 files were replaced after this spec was written and now form a viable cryotube → dystopia → repair → greenery → renewed city → utopia arc. Do not regenerate six scenes from the prompts below. The binding launch plan is `Assets/Plans/FAST_SIX_STAGE_EVOLUTION_PLAN.md`. Keep this file only as historical prompt/reference material; stage lore is not visual law.

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

**Lighting rule (added 2026-09-15):** stages 0–3 must not show a visible sun or unambiguous direct sunlight — artificial light only (neon signage, sodium/work lamps) or flat overcast/pre-dawn haze, no sun disc, no sunbeams. Stage 4 is the first stage where real daylight appears. This is deliberate: the sun breaking through is the visual proof that restoration is actually working, not just background dressing.

1. **Current Stage 0 art** — the existing opening composition is a cryotube in a black room from the Leo/Leonardo art set. **Aceyfer clarification, 2026-09-15:** this older stage brief and the “Cryo Chamber” lore are not binding visual law. Keep the current composition if it is the strongest coherent opening, or change the stage presentation and supporting lore together if another image works better. Do not add or generate a separate cryotube merely to satisfy the stage name.
2. **Smog-Choked Sprawl (Stage 1)** — night, not day: a sprawling trash-mountain megacity lit entirely by neon signage and sodium lamps, no visible sky or stars, smog and light pollution standing in for a sun that hasn't broken through in years. Junk-mountain silhouettes tower over the skyline, faint scaffolding/cranes barely visible in the glow, a distant smokestack pumping exhaust lit orange from below. Every surface is a sales pitch — dense, garish, satirical advertising signage covering the junk-mountains and foreground structures. Reference: Aceyfer's Grok/Leonardo mood reference nails this tone exactly (signage density, junk-mountain scale, wet-street light bloom) — match its mood and palette, but invent original fictional brand names/logos instead of reproducing it verbatim. Its "Mtn Dew," "Wall·E Mart," and Idiocracy-style "Brawndo" echoes are real trademarked/copyrighted material and cannot ship as-is. Suggested original substitutes in the same satirical register: a soda/energy-drink brand called **"GULPZILLA"** (purple/orange color scheme, not green/red), a big-box store sign reading **"SCRAPMART 24HR"** (no Wall-E reference), an electrolyte-joke brand called **"VOLTQUENCH"** instead of the Brawndo echo, and a tagline like **"Outlives Nature, Guaranteed!"** instead of the Nature Made riff. "Chem-Tal Energy Drink," "Family KTV," "99¢ Big-Ass Bottles," and "Doomscroll" are already original and fine to keep or reuse.
3. **Patchwork Recovery Zone (Stage 2)** — same city, visibly mid-cleanup, transitioning toward dawn: roughly half the neon signage now dark and dead rather than lit, temporary work lights and scaffolding lamps replacing some of the old commercial glow, at least one crane or work crew visible, sky a flat pre-dawn blue-gray with the first hint of horizon light — still no sun disc, smog visibly thinning but not gone.
4. **Green Shoots Initiative (Stage 3)** — visible plant life returning: rooftop gardens, a reclaimed lot turned into a community green space, cleaner building facades, most remaining old signage now dark or painted over/repurposed. Sky brightens further toward a pale dawn gold-gray — still overcast/hazy enough that no direct sun or defined sun disc is visible. Some rough edges remain (a patched wall, an old dead billboard) so it doesn't jump too far ahead of stage 2.
5. **Renewed Skyline (Stage 4)** — the first stage with real daylight: a genuinely rebuilt, modern skyline under a clear blue sky with direct, unambiguous sunlight, clean glass-and-steel towers, organized streets, greenery integrated into architecture (vertical gardens, tree-lined boulevards). Aspirational but not yet fantastical — this is "a good city," not sci-fi yet.
6. **Utopia Achieved (Stage 5)** — the futuristic paradise payoff: sleek elevated architecture, visible clean-energy elements (solar, wind, maybe elevated transit), lush integrated greenery, bright optimistic sunlight (golden hour or clear midday, sun clearly visible or unmistakably the light source), a sense of abundance and calm. This is the "we did it" image the whole 6-stage arc has been building toward.

## Import steps (once images exist)

Same pipeline as every other art pass this project: run new images through `Assets/_Game/Scripts/Editor/ArtImportTool.cs` (`Tools > Eighth Kind > Art Import`) with the standard sprite settings it already applies (Sprite/Single, 100 PPU, Bilinear, 2048 max size). Simplest path: keep the same six filenames (`BG1.jpg` → `BG6.png`, or normalize all to `.png`) so `BackgroundStageView.stageSprites[]`'s existing serialized references keep pointing at the right slots with zero code or scene changes — just overwrite the asset content and let Unity reimport. If filenames change, the array needs manual re-assignment in the Inspector, one extra step but not a blocker.

## Not yet decided

Whether "explore a new world instead of the same street" (Aceyfer's phrase) means anything beyond "each stage is visibly a different place" — e.g. whether stage 0's cryo-chamber-to-street transition needs its own intermediate beat, or whether future stages might eventually get more than one photo each. Current scope above is six photos, one per existing stage slot, which already satisfies "not the same street" since all six are meant to be distinct scenes. If Aceyfer wants multiple rotating photos *within* a single stage later, that's a separate, bigger scope change to `BackgroundStageView` (an array-of-arrays plus a rotation trigger) — not assumed here.
