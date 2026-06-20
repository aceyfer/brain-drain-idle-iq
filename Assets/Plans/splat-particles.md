# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: An idle clicker game where players tap to earn Brains and restore decaying IQ, upgrading various satirical elements. Tapping triggers a juice splat effect.
- Players: Single player
- Inspiration / Reference Games: Cookie Clicker, early internet browser adware pop-ups, Retro clickers.
- Tone / Art Direction: Neon, high-contrast, chaotic retro 2000s cartoon/cel-shaded style.
- Target Platform: iOS / Mobile
- Screen Orientation / Resolution: Portrait 1080x1920 (16:9)
- Render Pipeline: UniversalRP (URP)

# Game Mechanics
## Core Gameplay Loop
- Players tap the main button or screen to generate currency, causing a shower of cartoon pink splat/goo particles to spray outwards from the pointer location.

## Controls and Input Methods
- Touch/mouse tapping triggers particle generation at input coordinates.

# UI
- Splat particles are spawned on top of the main HUD/Canvas layers and are fully click-through.

# Key Asset & Context
- Active Scene: `Assets/Scenes/SampleScene.unity`
- Asset to generate: `Assets/_Game/Sprites/Particles/GooSplats.png`
- Import Settings:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Multiple (sliced into 8 sprites in a 4x2 grid)
  - Pixels Per Unit: 100
  - Filter Mode: Point or Bilinear
  - Compression: Uncompressed
- Target files to modify:
  - `Assets/_Game/Scripts/Systems/AnimationController.cs` (to load and randomly pick from the 8 sliced splat sprites instead of a procedurally drawn circle)

# Implementation Steps

### Step 1: Model Selection and Setup
- **Description**: Inspect and select a generative AI model that supports high-quality 2D sprites with custom resolution support (such as `flux-2-dev` or `gpt-image-1`).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Generate Pink Splat Sprite Sheet
- **Description**: Call the asset generation tool to create a 2D sprite sheet containing an array of 8 cartoon pink splat/goo shapes arranged in a neat grid.
  - **Prompt**: "2D sprite sheet of cartoon pink splat/goo particle shapes. 8 variations arranged in a neat 4x2 grid, each splat variation situated in its own grid cell with transparent space around it. Shapes vary between circular splats, star-shaped splats, and teardrop shapes. Style: thick black outlines, flat hot pink fill, cartoon comic-book style. Isolated on transparent background, no text, no shadows, no frames."
  - **Dimensions**: 1024x512 (to match the 2:1 aspect ratio of the 256x128 sheet at high fidelity)
  - **Save Path**: `Assets/_Game/Sprites/Particles/GooSplats.png`
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Remove Background from Generated Sprite Sheet
- **Description**: Run background removal on `GooSplats.png` using the `photoroom-bg-removal` model to ensure the grid of splats is cleanly isolated with absolute alpha transparency.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Configure Sprite Multi-Mode and Slice the Sprite Sheet
- **Description**: Write and execute a custom C# editor script using `RunCommand` to slice `GooSplats.png` programmatically.
  - Change Sprite Import Mode to `Multiple`.
  - Slice the sprite sheet into an array of 8 sub-sprites in a 4x2 grid (each grid slot is 1/4 width and 1/2 height).
  - Apply `Bilinear` filtering and `Uncompressed` compression.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

### Step 5: Update AnimationController to Use Generated Splat Sprites
- **Description**: Refactor `AnimationController.cs`'s `SpawnSplatParticles` and `GetSplatSprite` methods:
  - Load the sliced sub-sprites from `Assets/_Game/Sprites/Particles/GooSplats.png` using `AssetDatabase.LoadAllAssetsAtPath()`.
  - In `SpawnSplatParticles()`, randomly pick one of the 8 sliced sub-sprites for each individual particle spawned.
  - Fall back to the procedural pink circle if the sprite sheet is missing (maintaining compatibility).
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

# Verification & Testing
- Locate the sliced sprite sheet `GooSplats.png` in the Unity Project panel.
- Inspect the sub-sprites to verify they are sliced into 8 distinct variations in a 4x2 grid.
- Enter Play Mode in Unity.
- Click/tap repeatedly on the screen.
- Verify that a chaotic variety of star-shaped, circular, and teardrop-shaped cartoon hot pink splats spray out from the pointer, replacing the previous simple pink circle particle.
