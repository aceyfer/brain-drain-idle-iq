# Leonardo AI Mesh Overlay — Generation Spec

**Status:** logged 2026-08-31, NOT generated, NOT imported. This is a brief for Aceyfer to run through Leonardo AI (external tool, outside this session's reach) and then import via the existing `ArtImportTool.cs` pipeline. No code work is needed to consume the result — the import tool and its target objects already exist.

## Why this exists

Part of the standing art request: *"...and any mesh to spice things up."* Everything else in that request (button borders, Top Bar accent art, sidewalk) was proceduraly drawn in C# via `ArtExpansionTool.cs` because it's simple geometric/gradient work a script can draw pixel-by-pixel. A crack/tattoo/grime "mesh" overlay is organic, irregular detail — the kind of texture that looks obviously algorithmic if drawn procedurally and looks *right* out of an actual generative art model. That's a Leonardo AI job, not an Editor-tool job.

## What "mesh" means here

A semi-transparent overlay texture — cracks, grime streaks, corrosion, tattoo-like marks — layered on top of existing flat-color background art (`TopBG`, `BottomBG`) to break up their current flat/procedural look. Not a 3D mesh; "mesh" in the original ask reads as "textured overlay/mesh of detail," consistent with "spice things up" being the very next clause.

## Target surfaces

- **TopBG** — currently a flat panel plus the new 256x40 `TopBarAccent_Stage{0-5}` gradient strip (`ArtExpansionTool.GenerateTopBarStageArt`). The strip is small and stretches to fill TopBG's actual on-screen rect; TopBG itself has no detail texture behind it.
- **BottomBG** — the equivalent flat panel at the bottom of the screen, above the sidewalk/button row. Same situation: flat color, no surface detail.
- Do **not** target the six full-screen `BG1.jpg`...`BG6.png` World Restoration backdrops (`BackgroundStageView.stageSprites[]`) — those are already complete, camera-composed illustrations, not flat panels needing a detail pass. Layering a generic overlay on top of finished illustrations will look like a mistake, not polish.

## The 6-stage narrative arc

Everything else in this art pass (borders, Top Bar accent colors, pedestrian wobble-to-hover) already follows a consistent "grimy/decayed early → clean/glowing late" progression tied to `WorldRestorationStage.stageIndex` (0-5). The mesh overlay should follow the exact same arc so all the layered art reads as one coherent world, not mismatched pieces:

| Stage | Theme | Overlay content | Opacity (suggested) |
|---|---|---|---|
| 0 | Rock bottom | Heavy cracking, rust streaks, spray-paint-like grime, scattered small graffiti/tattoo marks | 0.85–1.0 |
| 1 | Barely holding | Cracking reduced, streaks lighter, fewer marks | 0.65–0.8 |
| 2 | Stabilizing | Visible but patchy cracking, grime thinning | 0.45–0.6 |
| 3 | Recovering | Faint hairline cracks only, streaks mostly gone | 0.25–0.4 |
| 4 | Healing | Barely-there texture, a few faint marks as "scars" | 0.1–0.2 |
| 5 | Utopia | Little to none — maybe one faint gold hairline crack as a nostalgic scar, otherwise clean | 0–0.08 |

This mirrors the exact banding already used for `BackgroundPedestrianManager.StageWobbleAmplitudeMultiplier` (1.0 → 0 across stages) and the border/top-bar stage palettes — reuse that logic, don't invent a new curve.

## Leonardo AI generation settings

- **Aspect / canvas:** generate square, 1024x1024. `ArtImportTool` can downscale and auto-crop afterward, and a square tile is easiest to repeat/tile across TopBG's and BottomBG's wide, short rects if a single overlay needs to stretch across a non-square area.
- **Background:** generate on a **flat solid color background** the art itself never uses — pure black (`#000000`) or pure green (`#00FF00`) — so `ArtImportTool`'s Chroma Key step can cleanly key it out. Do not generate on a transparent/checkered background; Leonardo doesn't reliably export usable alpha, and the import tool's chroma-key step exists specifically to solve this.
- **Style:** match the project's tone per the Bible: "Sci-fi / Corporate dystopia, cartoon-photoreal hybrid UI." Keep the crack/grime style graphic and readable at small on-screen size, not photoreal noise that turns to mud when scaled down onto a UI panel.
- **Line weight:** favor bold, high-contrast cracks/streaks over fine detail — this is a small UI panel, not a full-screen illustration; fine detail disappears at runtime.

## Draft prompts (starting point — refine to taste in Leonardo)

**Stage 0 (heaviest):**
> Flat 2D game UI texture overlay, cracked concrete surface with rust stains and grime streaks, scattered spray-paint-style graffiti tattoo marks, cartoon-photoreal hybrid style, high contrast bold linework, corporate-dystopia sci-fi tone, isolated on pure black background, no gradients in the background, tileable edges

**Stage 2 (midpoint):**
> Flat 2D game UI texture overlay, patchy faded cracking, light grime streaks, thinning graffiti marks, surface partially healed, cartoon-photoreal hybrid style, bold linework, isolated on pure black background, tileable edges

**Stage 5 (near-clean):**
> Flat 2D game UI texture overlay, almost entirely clean surface, one faint hairline gold crack remaining as a nostalgic scar, cartoon-photoreal hybrid style, subtle, isolated on pure black background, tileable edges

**Negative prompt (all stages):** `photo, photorealistic, 3d render, watermark, text, logo, signature, complex gradient background, vignette, blur`

Generate all 6 stages as separate images so each can carry its own opacity/detail level rather than trying to fade one texture in code (matches how every other stage-art asset in this project is 6 discrete files, not one file plus a shader fade).

## Import pipeline (existing tool, no code changes needed)

1. Open `Tools > Eighth Kind > Art Import` in the Unity Editor (`ArtImportTool.cs`).
2. Point **Source Folder** at wherever the 6 Leonardo exports land (defaults to the Windows user's Downloads folder already).
3. Select each stage image in turn and enable:
   - **Chroma Key (BG Removal):** on, Key Color = whatever solid background color was used for generation (black or green), tolerance/feather to taste using the live Before/After preview.
   - **Auto-Crop Padding:** on, small margin (4-8px) to trim the now-transparent border.
   - **Downscale:** on, Max Size 512 — a detail overlay does not need 1024px on screen; keep the smaller footprint.
4. Set **Output Folder** to `Assets/_Game/Sprites/UI/Generated` — same folder every other piece of this art pass already uses, keeps everything together.
5. Click **Import Selected Image** for each of the 6 stages. This applies the tool's standard import settings automatically (Sprite (2D/UI), Single, PPU 100, Bilinear, Clamp, AlphaIsTransparency on) — do not hand-edit these afterward.

## Wiring after import (small follow-up task, not yet scoped)

Once the 6 sprites exist, they need a small script (or a new `ArtExpansionTool` menu command, matching the existing "wire what was generated" pattern used for borders/Top Bar/sidewalk) to:
- Add an `Image` child to `TopBG` and to `BottomBG`, `Image.Type.Simple`, stretched to fill the parent, sibling-ordered above the flat background but below any text/buttons.
- Swap the sprite per `WorldRestorationStage.stageIndex` via the same `OnRestorationStageChanged` event every other stage-reactive UI piece already subscribes to (see `UniversalButtonBorderApplier`/`AccentBarStageView` for the exact pattern to copy).
- Set the `Image.color` alpha per the opacity table above, per stage (either baked into each PNG's own alpha, or driven by `Image.color.a` in code — baking into the PNG is simpler and matches how every other generated sprite in this pass already carries its own final look rather than relying on a runtime tint).

This wiring step was not implemented yet — write it once actual Leonardo exports exist, since a mock/placeholder sprite would need to be redone anyway and there's nothing to preview against without real art in hand.
