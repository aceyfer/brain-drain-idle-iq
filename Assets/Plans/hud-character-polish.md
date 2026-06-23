# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by AcEclipse Games. Polish HUD visibility and character silhouette aspect ratio.
- Players: Single player
- Tone / Art Direction: Satirical retro-dystopian cartoon, high-contrast colors, funny caricature visuals.
- Target Platform: iOS / Mobile
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
- The `PlayerCharacter_Anchor` contains the active character sprite standing at street level.
- The top HUD area displays the player's Brain Power (Cognitive Juice) and global IQ.

# UI / Layout
- **CurrencyHeader**: Configured with a dark semi-transparent background card (`RoundedRect8`) and a crisp black outline so that Brain Power and IQ are easily readable against the detailed city skyline art.
- **PlayerCharacter_Anchor**: Scaled to a taller, narrower proportion (`120x300`) to represent a realistic, funny human silhouette.
- **Upright Sprite Sheets**: Re-run the editor sprite generator menu to ensure all 5 appearance tiers are generated upright and point-filtered.

# Key Asset & Context
- Scene: `Assets/Scenes/SampleScene.unity`
- Background Sprite: `Assets/_Game/Sprites/UI/Generated/RoundedRect8.png`
- Generator: `BrainDrain.EditorTools.PlaceholderArtGenerator.GenerateAll()`

# Implementation Steps

### Step 1: Polish top HUD (CurrencyHeader) legible background
- **Description**: 
  - Locate `Canvas/CustomSafeArea/CurrencyHeader` in `SampleScene.unity`.
  - Add or configure `UnityEngine.UI.Image` component on `CurrencyHeader`.
  - Set its sprite to `RoundedRect8.png` and type to `Sliced` (pixelsPerUnitMultiplier = 2f).
  - Set its background color to `#0F0A18` with 85% opacity (`new Color(0.06f, 0.04f, 0.1f, 0.85f)`).
  - Add or configure `UnityEngine.UI.Outline` component on `CurrencyHeader`. Set color to solid black (`#000000`) and effectDistance to `(4f, -4f)`.
  - Position it slightly lower or keep it centered at `anchoredPosition=(0, -40)`, `sizeDelta=(-60, 160)` to give a cleaner layout with high contrast.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Regenerate character sprites and resize character anchor
- **Description**:
  - Run the menu generator `BrainDrain.EditorTools.PlaceholderArtGenerator.GenerateAll()` in editor-mode to refresh all character sprite assets on disk upright.
  - Select `Canvas/CustomSafeArea/PlayerCharacter_Anchor`.
  - Keep its anchors at bottom-center `min=(0.5f, 0f), max=(0.5f, 0f), pivot=(0.5f, 0f)` and anchoredPosition at `(0f, 60f)`.
  - Set its sizeDelta to `new Vector2(120f, 300f)` to make the silhouette taller and narrower.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- Read back `CurrencyHeader` properties: verify it has an active Image background, dark semi-transparent color, and active Outline component.
- Read back `PlayerCharacter_Anchor` size: verify it is exactly `120x300` positioned at bottom-center.
- Enter Play Mode:
  - Verify that top text elements (Cognitive Juice and IQ) are readable against the dark semi-transparent container.
  - Verify the player character stands upright on the street level and is properly proportioned.
