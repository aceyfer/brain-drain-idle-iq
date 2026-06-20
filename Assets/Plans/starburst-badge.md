# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, upgrading various satirical elements.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s cartoon/cel-shaded style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Players tap to generate Brains/IQ, advancing levels and unlocking retro brain-drain devices and storefront panels.
- Visual badges (like a starburst "FREE!" badge) can highlight temporary boosts, free offers, or deal elements in the Shop or Rebirth menu.

## Controls and Input Methods
- Touch input on UI elements, tapping badges or close buttons.

# UI
The starburst badge sprite will be used as a promotional sticker or visual overlay on various store items or popup models. Text (such as "FREE!", "IQ!", or "99% OFF") will be dynamically layered on top using TextMeshPro in Unity.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Asset to generate: `Assets/_Game/Sprites/UI/StarburstBadge.png`
- Import Settings:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Pixels Per Unit: 100
  - Filter Mode: Bilinear
  - Compression: Uncompressed (to avoid color distortion or banding on flat vibrant yellow/red colors)

# Implementation Steps

### Step 1: Model Selection and Setup
- **Description**: Target a generative AI model that supports high-quality 2D UI game assets with custom resolution support (such as `flux-2-dev` or `game-ui-essentials-2`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Starburst Badge Sprite
- **Description**: Call `Unity.AssetGeneration.GenerateAsset` to create the starburst badge sprite. Use a high-quality square resolution (1024x1024px) for the generation.
  - **Model ID**: `flux-2-dev`
  - **Prompt**: "A 2D cartoon starburst badge sprite. Style: classic infomercial badge, thick black outline, bright yellow starburst shape with a clean red circular text area in the exact center. No text on the badge, no text elements. Square canvas. Flat colors, thick outlines, cel-shaded, slightly asymmetric points on the starburst for a hand-drawn feel. Isolated on transparent background, no shadows."
  - **Dimensions**: 1024x1024
  - **Save Path**: `Assets/_Game/Sprites/UI/StarburstBadge.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Remove Background from Generated Sprite
- **Description**: Use `RemoveImageBackground` (Photoroom Background Removal) to isolate the starburst badge cleanly, ensuring absolute transparency.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Configure Sprite Import Settings
- **Description**: Apply texture importer overrides to the newly generated storefront panel to make it render sharply inside Unity.
  - Texture Type -> Sprite (2D and UI)
  - Sprite Mode -> Single
  - Sprite Pixels Per Unit -> 100
  - Filter Mode -> Bilinear
  - Compression -> Uncompressed
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- Locate `StarburstBadge.png` in the Unity Project under `Assets/_Game/Sprites/UI/`.
- Inspect using the Unity Editor Inspector:
  - Check the aspect ratio is square (1:1).
  - Ensure the border has a thick black outline, flat yellow starburst shapes, and a red center.
  - Verify that the background is fully transparent and there are no stray black/white artifacts around the outer borders.
  - Ensure import settings are set to Sprite (2D and UI) with no compression.
