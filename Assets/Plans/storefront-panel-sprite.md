# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, upgrading various satirical 2000s and retro-style elements.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s cartoon/cel-shaded style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Players tap to generate Brains/IQ, advancing levels and unlocking retro brain-drain devices and storefront panels.
- Upgrades and shops will be displayed inside themed cartoon storefront UI panels, such as the scummy strip-mall storefront panel.

## Controls and Input Methods
- Touch input on UI elements, scrolling through the storefront, and tapping purchase buttons.

# UI
The storefront panel sprite will be used as the background container/frame for shop menus, storefront upgrades, or other scummy retro deal layouts inside the main Canvas.
- **Header area**: A small neon sign area at the top of the panel, designed to hold dynamically positioned titles (like "UPGRADES" or "SPECIAL DEALS").
- **Content area**: A larger rectangular box below the header for displaying actual gameplay slot listings, prices, and buttons.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Asset to generate: `Assets/_Game/Sprites/UI/StorefrontPanel.png`
- Import Settings:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Pixels Per Unit: 100
  - Filter Mode: Point (no filter) or Bilinear
  - Compression: Uncompressed (to avoid banding on glowing neon green/hot pink colors)

# Implementation Steps

### Step 1: Model Selection and Setup
- **Description**: Inspect and select a generative AI model that supports high-quality 2D UI game assets with custom resolution support (such as `flux-2-dev` or `game-ui-essentials-2`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Storefront Panel Sprite
- **Description**: Call `Unity.AssetGeneration.GenerateAsset` to create the storefront panel sprite with an aspect ratio matching 400x200px (using 1024x512px).
  - **Model ID**: `flux-2-dev`
  - **Prompt**: "2D game UI panel sprite. Style: cartoon storefront window, thick black outlines, cel-shaded flat colors, retro cartoon. Panel is roughly rectangular in landscape orientation, looking like a scummy storefront with a small neon sign header area at the top and a larger content panel area below. Hot pink background panel with a glowing neon green border. Isolated on transparent background, no text, no shadows."
  - **Dimensions**: 1024x512
  - **Save Path**: `Assets/_Game/Sprites/UI/StorefrontPanel.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Remove Background from Generated Panel
- **Description**: Use `RemoveImageBackground` (Photoroom Background Removal) to isolate the storefront panel cleanly, ensuring any ambient noise from generation is discarded and transparency is perfect.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Configure Sprite Import Settings
- **Description**: Apply texture importer overrides to the newly generated storefront panel to make it render sharply inside Unity.
  - Texture Type -> Sprite (2D and UI)
  - Sprite Mode -> Single
  - Sprite Pixels Per Unit -> 100
  - Filter Mode -> Bilinear (clean, anti-aliased look)
  - Compression -> Uncompressed (to protect high-vibrancy glowing neon green and hot pink gradients from artifacting)
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- Locate `StorefrontPanel.png` in the Unity Project under `Assets/_Game/Sprites/UI/`.
- Inspect using the Unity Editor Inspector:
  - Check the aspect ratio matches landscape (~2:1).
  - Ensure the border has a thick black outline, neon green glow, and surrounds a hot pink inner panel.
  - Verify that the background is fully transparent and there are no stray black/white artifacts around the outer borders.
  - Ensure import settings are set to Sprite (2D and UI) with no compression.
