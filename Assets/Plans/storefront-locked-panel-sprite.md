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
- Certain storefront content is classified/locked until rebirth or special milestones. The "Classified/Locked Storefront Panel" will be used as a placeholder background for locked shop slots.

# UI
The classified/locked storefront panel sprite will be used as the background container/frame for locked shop menus or classified deals inside the Canvas.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Reference Asset FileInstanceID: `82194` (StorefrontPanel.png)
- Asset to generate: `Assets/_Game/Sprites/UI/StorefrontPanelLocked.png`
- Import Settings:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Pixels Per Unit: 100
  - Filter Mode: Bilinear
  - Compression: Uncompressed (to avoid banding on color details)

# Implementation Steps

### Step 1: Model Selection and Reference Identification
- **Description**: Target a generative AI model that supports image editing/reference guiding (such as `flux-2-dev` or `gpt-image-1-5`). Use the existing storefront panel's FileInstanceID (`82194`) as `referenceImageInstanceId` to preserve the exact outer layout structure.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Locked Storefront Panel Sprite
- **Description**: Call `Unity.AssetGeneration.GenerateAsset` to create the locked panel sprite using the previous panel as a reference.
  - **Model ID**: `flux-2-dev`
  - **Prompt**: "A 2D game UI panel sprite, based on the reference layout shape. The panel is desaturated to 20% color (almost grayscale), with a large cartoon padlock icon centered on the panel, a diagonal red 'CLASSIFIED' stamp across the face of the panel, and a cartoon metal chain looped around the outer border. Transparent background. The border and panel are mostly grayscale, while the padlock and the red stamp are in full color. No other text elements, no shadows."
  - **Dimensions**: 1024x512 (which matches the 2:1 aspect ratio of the 400x200 panel)
  - **Reference ID**: 82194
  - **Save Path**: `Assets/_Game/Sprites/UI/StorefrontPanelLocked.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Remove Background from Generated Panel
- **Description**: Use `RemoveImageBackground` (Photoroom Background Removal) to isolate the locked storefront panel cleanly, ensuring any ambient noise from generation is discarded and transparency is perfect.
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
- Locate `StorefrontPanelLocked.png` in the Unity Project under `Assets/_Game/Sprites/UI/`.
- Inspect using the Unity Editor Inspector:
  - Check the aspect ratio matches landscape (2:1).
  - Ensure the panel is mostly grayscale (20% desaturated) but has the cartoon padlock, the diagonal red "CLASSIFIED" stamp, and the metal chain around the border.
  - Verify that the background is fully transparent and there are no stray black/white artifacts around the outer borders.
  - Ensure import settings are set to Sprite (2D and UI) with no compression.
