# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: A satirical mobile idle clicker game where players tap to earn Brains, climb "Idiocracy" ranks, and purchase absurd brain-melting buildings.
- Players: Single player
- Inspiration / Reference Games: Adventure Capitalist, Idiocracy, Cookie Clicker.
- Tone / Art Direction: Satirical 2000s cartoon/cel-shaded style, thick black outlines, neon high-contrast colors, muted neon palette.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (reference)
- Render Pipeline: UniversalRP (2D)

# Game Mechanics
## Core Gameplay Loop
- Tap the main brain to earn Brains and increase idle IQ/Brain Power.
- Spend accumulated Brains in the shop to purchase and upgrade buildings.
- Progressing through ranks unlocks visual updates and changes, supported by a parallax background layer in the far depth to give the scene a sense of scale and atmosphere.

## Controls and Input Methods
- Simple touch/click interactions on mobile screen button controls.

# UI
- The far depth skyline layer will sit in the background behind other diorama elements and UI components, providing a continuous, repeating, or wide scrollable panoramic vista.
- It will feature absurd corporate skyscrapers, smog, and glowing billboards, visualised through a wide banner format.

# Key Asset & Context
- Target Asset: `Assets/_Game/Sprites/Backgrounds/CitySkylineFar.png`
- Import Configuration:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Mesh Type: Full Rect (to prevent sprite-tight-mesh clipping in wide panoramas)
  - Wrap Mode: Repeat (allows seamless horizontal scrolling if required)
  - Filter Mode: Bilinear (smooth scaling)
  - Compression: Uncompressed (maintains the gradients of the muted neon and smoggy atmosphere without blocky compression artifacts)

# Implementation Steps

### Step 1: Model Selection and Setup
- **Description**: Identify and configure the best available generative AI model supporting custom dimensions and 2D sprite/background textures (e.g., `flux-2-dev` or `gpt-image-1`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Skyline Banner Layer
- **Description**: Call `Unity.AssetGeneration.GenerateAsset` to produce the panoramic background banner at a high-fidelity wide resolution (such as 2048x512px or 1920x512px, matching the ~3:1 aspect ratio constraint of 1920x600px).
  - **Prompt**: "2D mobile game far background parallax layer. A smoggy dystopian cartoon cityscape skyline at far depth. Absurd corporate skyscrapers with glowing neon billboard signs advertising fake brain products (such as 'BRAIN SLUDGE' or glowing brains). Style: thick black outlines, Adventure Capitalist meets Idiocracy, flat cel-shaded colors, muted neon color palette. Transparent sky background, no ground/floor elements, skyline only. Panoramic wide banner format."
  - **Save Path**: `Assets/_Game/Sprites/Backgrounds/CitySkylineFar.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Extract Background Transparency
- **Description**: Use `RemoveImageBackground` (Photoroom Background Removal) to cleanly separate the cityscape skyline from the generated sky/ambient colors, converting the sky into a perfect transparent alpha layer.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Configure Texture Importer Settings
- **Description**: Set custom TextureImporter properties on the imported PNG file to ensure seamless wrapping and uncompressed vibrant colors.
  - Set texture type to Sprite (2D and UI).
  - Set Sprite Mode to Single.
  - Set Wrap Mode to Repeat.
  - Set Compression to Uncompressed.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
- Locate the asset at `Assets/_Game/Sprites/Backgrounds/CitySkylineFar.png` in the project.
- Open the sprite in the Unity Inspector and verify:
  - Width and height aspect ratio is approximately 3.2:1 (wide banner format).
  - Background is fully transparent with clean, crisp edges on the skyscrapers.
  - Outlines are thick and cartoonish, consistent with the Idiocracy meets Adventure Capitalist art direction.
  - Muted neon details are vibrant and free of compression artifacts.
