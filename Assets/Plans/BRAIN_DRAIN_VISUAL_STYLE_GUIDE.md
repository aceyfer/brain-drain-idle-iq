# Brain Drain Visual Style Guide

**Version:** 1.0  
**Date:** 2026-09-15  
**Status:** Proposed visual authority for first-playable polish. This consolidates existing binding direction; it does not override gameplay or narrative rules in `PROJECT_BIBLE.md`.

## 1. North star

**Corporate dystopia sold through a broken Saturday-morning propaganda machine.**

Brain Drain should look funny at first glance, sinister on the second, and increasingly hopeful as the player restores the world. The interface belongs to the same world as the art: a battered corporate control console that has been hijacked by a neon resistance movement.

The target is not generic cyberpunk. It is **satirical retro-dystopian cartoon design over cinematic environmental depth**:

- bold, readable silhouettes;
- thick, clean outlines on foreground illustrations;
- grim industrial materials and damaged broadcast textures;
- small, purposeful neon accents rather than neon everywhere;
- propaganda typography and ridiculous fictional brands;
- environmental restoration expressed through light, air, vegetation, and visual order.

## 2. Binding constraints

These rules consolidate `PROJECT_BIBLE.md` §2 and must survive every art pass:

1. Portrait mobile is the primary composition. Judge artwork at phone size first and PC zoom second.
2. UI icons and buttons must be substantially smaller than the current presentation. Large art cannot crowd currency, dialogue, shop, or restoration information.
3. COGS and pedestrian dialogue require high contrast, generous padding, restrained effects, and immediate readability.
4. Existing first-playable entities remain the set: backgrounds Stages 0–5, COGS 1–6, and the existing pedestrians. Do not add new characters or entity families before first playable.
5. Character-facing art must contain no visible generation defects, warped anatomy, voids, accidental merges, illegible pseudo-text, or blurred repairs.
6. Fictional brands and slogans must be original. Do not ship recognizable trademark or franchise imitations.
7. Restoration Points are presented as **RP**. The real-money store is the **God Shop**. Visual labels must follow current player-facing terminology.

## 3. The visual grammar

### Shape language

- **Corporate control:** rigid rectangles, clipped corners, horizontal status bands, stamped labels, warning chevrons.
- **Resistance / recovered intelligence:** imperfect hand-painted marks, taped labels, cyan diagnostic lines, green life signals.
- **Brain matter / temptation:** rounded lobes, bubbles, splats, viscous curves, hot pink fluid.
- **Illumisnotty power:** triangles, eyes, gold seals, radial geometry, ceremonial symmetry.
- **World restoration:** increasingly open shapes and breathing room. Early stages feel boxed in; late stages expose sky and long curves.

### Line and edge rules

- Foreground buildings, props, and character cutouts: clean 4–8 px equivalent dark outline at 1024 px source size.
- UI containers: 1–3 px crisp border, not illustration-style black ink.
- Never mix soft airbrushed cutout edges with hard cartoon outlines in the same visual layer.
- Avoid white halos around transparent sprites. Check every export against black, magenta, and cyan backgrounds.

### Surface language

- Early game: oily metal, scratched plastic, condensation, dirty glass, CRT scanlines, cheap vinyl labels.
- Mid game: repaired panels, exposed wiring, reclaimed wood/metal, cleaner glass, visible plants.
- Late game: maintained metal, ceramic, glass, vegetation, clean water, solar surfaces, warm natural light.
- Texture is evidence of the world state. Do not add grime uniformly as decoration.

## 4. Core palette

Use color by function. The neon colors are signals, not general fills.

| Role | Color | Hex | Usage |
|---|---|---:|---|
| Void | Near black | `#080A0D` | Deep background, modal surround |
| Panel | Carbon | `#11151B` | Primary UI surface |
| Raised panel | Gunmetal | `#1B232C` | Cards, tabs, inset modules |
| Primary text | Bone white | `#F2F0E8` | Main labels and values |
| Secondary text | Cool gray | `#9BA8B5` | Descriptions, metadata |
| Disabled | Ash | `#59616A` | Locked/inactive content |
| Intelligence | Electric cyan | `#00DDEB` | BP, selection, diagnostics |
| Brain / corruption | Toxic pink | `#FF2A91` | IQ loss, goo, temptation, warnings |
| Life / restoration | Signal green | `#75F04C` | RP progress, successful recovery |
| Cash | Acid gold | `#F5C542` | Cash and purchasable value |
| Danger | Emergency red | `#F04444` | Destructive/negative state only |
| Illumisnotty | Tarnished gold | `#C99A38` | Snotting, seals, elite hierarchy |
| Late-world sky | Clear blue | `#69BCEB` | Stages 4–5 environment, sparingly in UI |

### Color discipline

- One dominant signal color per component; a second may indicate state.
- Pink and green together are the recognizable Brain Drain accent pair, but they should not occupy equal visual weight everywhere.
- Cyan identifies player intelligence and interface control. Green identifies world healing. Do not interchange them.
- Gold is split by meaning: bright acid gold for Cash, darker ceremonial gold for Illumisnotty/Snotting.
- Body text must meet strong small-screen contrast. Never place gray text directly on detailed scenery.

## 5. Typography

Existing fonts remain the first-playable set:

- **Oswald Bold:** numbers, HUD resources, compact headings, button labels.
- **Bangers:** short satirical punches, COGS emphasis, propaganda fragments—not paragraphs.
- **Anton:** major milestone cards, warnings, stage reveals, Snotting ceremony.
- **Roboto Bold:** descriptions, settings, explanatory text, accessibility-critical copy.

Rules:

- Use no more than two type families on one screen.
- Use uppercase for short labels only. Sentence case is mandatory for descriptions and dialogue.
- Effects must not reduce glyph interiors: maximum one shadow or one outline, never glow + outline + shadow together on small copy.
- Numbers receive the clearest contrast and largest size in resource displays.
- Minimum mobile body target: approximately 30–34 px at the 1080×1920 reference canvas; validate on the smallest supported device.

## 6. Layer hierarchy

Every gameplay screen should separate into four readable depth layers:

1. **World:** cinematic background, lowest contrast behind UI.
2. **Actors/props:** COGS portrait, pedestrians, featured building art.
3. **Interaction:** shop cards, tap target, restoration control, resource HUD.
4. **Urgent overlay:** one modal or interruption at a time.

Do not allow multiple dark scrims to stack. When a modal is open, the active modal remains fully legible, the immediate parent context remains faintly identifiable, and unrelated overlays are suppressed.

## 7. Restoration-stage art direction

The six backgrounds should read as one civilization recovering, even where the camera location changes. Reuse visual anchors across stages: the same distant tower language, transit spine, broadcast architecture, or skyline silhouette.

| Stage | State | Light | Palette | Environmental evidence |
|---:|---|---|---|---|
| 0 | Current opening art: cryotube in a black room | Artificial cold light only | Black, steel blue, frost white | Existing Leo/Leonardo composition; retain or replace based on visual quality, not the stage label alone |
| 1 | Smog-Choked Sprawl | Neon and sodium at night | Soot, orange, toxic magenta | Trash mountains, ads, smoke, wet decay |
| 2 | Patchwork Recovery | Pre-dawn/flat overcast | Muted blue, rust, work-light amber | Scaffolds, cleanup, repaired utilities |
| 3 | Green Shoots | Overcast brightening | Concrete, moss, restrained green | Community gardens, reclaimed streets |
| 4 | Renewed Skyline | First clear daylight | Soft blue, leaf green, warm concrete | Open sky, reliable transit, clean water |
| 5 | Utopia Achieved | Warm golden natural light | Clear blue, rich green, solar gold | Integrated nature, elegant infrastructure |

Stages 0–3 must not show a visible sun or unmistakable direct sunlight. Stage 4 is the first daylight reveal. Stage 5 earns warmth without becoming sterile luxury advertising.

Stage-lore authority — Aceyfer clarification, 2026-09-15: the current first-stage artwork is the cryotube in a black room from the Leo/Leonardo art set. That is a description of the existing opening image, not proof that the written stage name or lore must dictate the composition. Stage names, lore, and older art briefs are supporting context—not binding visual law. Prefer the strongest coherent existing artwork and revise the lore/label when necessary; do not force new objects into a composition merely because a stage description mentions them. A new or replacement cryotube is therefore not required. If the existing tube is retained, keep it as part of its integrated Leo composition rather than adding a separate tube asset.

Background rules:

- Keep important landmarks within the central safe composition; the arch/frame masks outer edges.
- Remove or regenerate malformed signage. All readable signage must be intentional and original.
- Reserve quiet regions behind HUD and dialogue. High-frequency detail belongs around, not beneath, text.
- Match perspective, haze, and black levels across the set so crossfades feel deliberate.

## 8. Foreground illustration style

Buildings and collectible props should use the visual language already suggested by `BrainJuiceExtractor.png`, refined into a consistent set:

- satirical industrial cartoon;
- strong silhouette at thumbnail size;
- clean black/dark-navy outlines;
- limited materials per asset;
- one bright focal substance or display;
- one readable joke, not many tiny pseudo-labels;
- mechanical details that imply function;
- transparent background with clean alpha.

To coexist with cinematic backgrounds, ground illustrated assets using a subtle local shadow and a narrow rim light sampled from the current stage. Do not blur or repaint the full asset into photorealism.

## 9. COGS

COGS must feel authored for Brain Drain rather than like a generic robot portrait.

Keep:

- recognizable mechanical face;
- direct eye contact;
- green circuitry as a recurring identity cue;
- portrait framing suitable for dialogue.

Strengthen:

- corporate narrator identity through stamped serials, broadcast framing, status lights, and controlled facial asymmetry;
- stage progression through maintenance state, expression, casing, and light—not six unrelated robot designs;
- satirical character through judgmental brows, uncomfortable pauses, cracked composure, and propaganda-screen presentation.

COGS art should use consistent crop, head scale, camera angle, background value, and eye position across all six stages. Avoid embedded watermarks, sparkles, meaningless circuit text, or rendering defects.

## 10. UI components

### HUD

- Resource values first; labels secondary.
- Use small icon + number groups rather than oversized illustrated badges.
- Give each currency its semantic color, but keep text mostly bone white.
- The top bar should occupy the minimum height that remains readable.

### Buttons

- Default: carbon fill, crisp border, bone-white label.
- Selected: cyan border and a restrained inner glow.
- Affordable/purchase-ready: gold value plus a small green state cue.
- Locked: desaturated, but still readable; lock reason must remain visible.
- Destructive/decline: red only when the action is genuinely destructive or negative.
- Minimum interactive target may be large while the visible icon remains small.

### Cards and shop rows

- Left: asset thumbnail or compact state icon.
- Center: name, one-line function, owned level.
- Right: price and action.
- Preserve one dominant reading path. Avoid multiple equally bright borders.
- BP Upgrades should feel clinical/self-improvement; Cash Investments should feel improvised/laundered; God Shop should feel ceremonial and predatory.

### Dialogue

- COGS portrait and nameplate form one component.
- Use an opaque or near-opaque text field; scenery never competes with dialogue.
- Bangers is permitted only for a short emphasized phrase. Main dialogue stays in Roboto Bold or another clean face.
- One dialogue system owns the screen at a time. Prevent dialogue, settings, adware parody, and shop modals from visually stacking.

### Restoration

- Restoration green represents verified improvement, not generic confirmation.
- Show the stage transition through the world first, UI celebration second.
- Progress bars require a stable dark track, luminous but narrow fill, and an explicit numeric/percentage label.

## 11. Motion and effects

- Tap feedback: fast compression, one floating value, one splat/burst; complete in roughly 0.35–0.55 seconds.
- Affordable state: slow restrained pulse; never flash continuously.
- COGS interruption: CRT sync tear or scanline lock, under 0.25 seconds.
- Stage change: background crossfade plus gradual atmospheric cleanup; avoid a generic white flash.
- Snotting: ceremonial gold geometry contaminated by pink/green goo—the joke should remain present.
- Respect reduced-motion options where available.

## 12. Audio-to-visual correspondence

- Cyan actions: dry interface tick/electrical chirp.
- Pink corruption: wet pop, distorted synth, CRT crackle.
- Green restoration: clean mechanical confirmation plus organic texture.
- Gold purchase/Snotting: cheap triumphant sting that becomes genuinely majestic only late in progression.

This section defines visual intent only; it does not authorize new audio work.

## 13. Asset production checklist

Every final asset must pass:

- reads at intended on-device size;
- correct semantic palette;
- clean silhouette and alpha edge;
- no malformed anatomy, objects, signage, hands, faces, or cables;
- no watermark or embedded generator mark;
- no unintended copyrighted/trademarked reference;
- correct portrait-safe composition;
- consistent scale/crop with its asset family;
- tested over both dark and bright restoration stages;
- import settings appropriate to its role and resolution;
- named consistently and accompanied by its Unity `.meta` file when added.

## 14. First implementation slice

Apply this guide first to a single Stage 1 gameplay screen before global rollout:

1. Establish world/UI separation and readable black levels.
2. Normalize the top resource HUD.
3. Reduce visible navigation/icon scale while retaining tap targets.
4. Restyle one BP building row and one Cash building row.
5. Normalize COGS portrait/dialogue presentation.
6. Validate tap feedback, restoration progress, and one modal.
7. Capture portrait screenshots at reference size and a smaller real-device equivalent.

Only after this slice is approved should the same component system propagate to every screen.

## 15. Explicit non-goals

- No new gameplay systems.
- No new character/entity families before first playable.
- No replacement of the current economy terminology or progression rules.
- No global “make everything neon” pass.
- No destructive regeneration of existing source art without preserving provenance and approval.
- No scene-wide styling tool run until its hardcoded values are checked against the current live scene.
